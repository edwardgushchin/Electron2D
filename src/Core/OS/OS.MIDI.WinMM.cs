using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class OS
{
    private sealed partial class MIDIInputOwner
    {
        private sealed partial class WinMMInputSession : Session
        {
            private const int QueueCapacity = 4096;
            private const uint InvalidHandle = 5;
            private const uint InputClose = 0x3C2;
            private const uint InputData = 0x3C3;
            private const uint InputError = 0x3C5;
            private const uint InputMoreData = 0x3CC;
            private const uint CallbackFunction = 0x30000;
            private const uint ReportBacklog = 0x20;

            private static readonly ConcurrentDictionary<nuint, (WinMMInputSession Session, int Device)> Callbacks = new();
            private static long _lastToken;

            private readonly object _queueGate = new();
            private readonly Queue<(int Device, uint Packed)> _queue = new();
            private readonly List<Port> _ports = [];
            private uint _failure;
            private bool _closed;

            internal static unsafe WinMMInputSession Open()
            {
                var session = new WinMMInputSession();
                try
                {
                    var count = Native.GetCount();
                    if (count != 0) session._queue.EnsureCapacity(QueueCapacity);
                    session._ports.EnsureCapacity(checked((int)count));
                    for (uint device = 0; device < count; device++)
                    {
                        Check(Native.GetCaps(device, out var caps, (uint)sizeof(Caps)), "query source");
                        var length = new ReadOnlySpan<char>(caps.Name, 32).IndexOf('\0');
                        session.Names.Add(new string(caps.Name, 0, length < 0 ? 32 : length));

                        // Monotonic tokens prevent a retired driver callback from addressing a later session.
                        var token = checked((nuint)Interlocked.Increment(ref _lastToken));
                        var port = new Port(token);
                        session._ports.Add(port);
                        if (!Callbacks.TryAdd(token, (session, (int)device)))
                            throw new InvalidOperationException("WinMM MIDI callback token already in use.");
                        var result = Native.Open(out port.Handle, device, &OnInput, token, CallbackFunction | ReportBacklog);
                        Check(result, "open source");
                        port.Interface = GetDeviceInterface(device);
                        Check(Native.Start(port.Handle), "start source");
                    }

                    session.ValidateSources();
                    return session;
                }
                catch (Exception error)
                {
                    try { session.Close(); }
                    catch (IOException cleanup) { throw new AggregateException(error, cleanup); }
                    throw;
                }
            }

            internal override unsafe void ValidateSources()
            {
                foreach (var port in _ports)
                {
                    var disconnected = port.Interface is { } path
                        ? GetInterfaceEnabled(path) != true
                        : Native.GetCaps((nuint)port.Handle, out _, (uint)sizeof(Caps)) != 0;
                    if (disconnected)
                        throw new IOException("A WinMM MIDI source disconnected; reset note state and reopen inputs.");
                }
            }

            internal override InputEventMIDI? Read(out bool available)
            {
                (int Device, uint Packed) entry;
                lock (_queueGate)
                {
                    if (_failure != 0)
                    {
                        var reason = _failure switch
                        {
                            InputClose => "a source disconnected",
                            InputError => "a source supplied an invalid message",
                            _ => "the input queue overflowed",
                        };
                        throw new IOException($"WinMM MIDI input failed because {reason}; reset note state and reopen inputs.");
                    }

                    available = _queue.TryDequeue(out entry);
                }

                return available ? Decode(entry.Device, entry.Packed) : null;
            }

            internal override void Close()
            {
                lock (_queueGate)
                {
                    _closed = true;
                    _queue.Clear();
                }

                List<IOException>? failures = null;
                foreach (var port in _ports)
                {
                    Callbacks.TryRemove(port.Token, out _);
                    if (port.Handle == 0) continue;
                    var result = Native.Close(port.Handle);
                    if (result != 0 && result != InvalidHandle)
                    {
                        // No SysEx buffers are submitted. Retry after reset if a driver still refuses to close.
                        Native.Reset(port.Handle);
                        result = Native.Close(port.Handle);
                        if (result != 0 && result != InvalidHandle)
                            (failures ??= []).Add(new IOException($"Could not close WinMM MIDI input (MMRESULT {result})."));
                    }
                }

                if (failures is not null)
                    throw new IOException("One or more WinMM MIDI sources failed to close.", new AggregateException(failures));
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void OnInput(nint handle, uint message, nuint token, nuint data, nuint timestamp)
            {
                if (message is not (InputData or InputMoreData or InputError or InputClose) ||
                    !Callbacks.TryGetValue(token, out var target)) return;

                var session = target.Session;
                lock (session._queueGate)
                {
                    if (session._closed || session._failure != 0) return;
                    if (message is InputError or InputClose)
                        session._failure = message;
                    else if (session._queue.Count == QueueCapacity)
                        session._failure = InputMoreData;
                    else
                        // MIM_MOREDATA carries a valid message. WinMM signals backlog, not loss, with it.
                        session._queue.Enqueue((target.Device, (uint)data));
                }
            }

            private static InputEventMIDI? Decode(int device, uint packed)
            {
                var status = (byte)packed;
                var first = (byte)(packed >> 8);
                var second = (byte)(packed >> 16);
                var message = (MIDIMessage)(status < 0xF0 ? status >> 4 : status);
                var length = message switch
                {
                    MIDIMessage.NoteOff or MIDIMessage.NoteOn or MIDIMessage.Aftertouch or MIDIMessage.ControlChange or
                        MIDIMessage.PitchBend or MIDIMessage.SongPositionPointer => 2,
                    MIDIMessage.ProgramChange or MIDIMessage.ChannelPressure or MIDIMessage.QuarterFrame or MIDIMessage.SongSelect => 1,
                    MIDIMessage.TuneRequest or MIDIMessage.TimingClock or MIDIMessage.Start or MIDIMessage.Continue or
                        MIDIMessage.Stop or MIDIMessage.ActiveSensing or MIDIMessage.SystemReset => 0,
                    _ => -1,
                };
                if (length < 0) return null;
                if ((length > 0 && first > 127) || (length == 2 && second > 127))
                    throw new IOException("A WinMM MIDI source supplied an invalid message; reset note state and reopen inputs.");

                var value = new InputEventMIDI { Device = device, Message = message, Channel = status < 0xF0 ? status & 15 : 0 };
                switch (message)
                {
                    case MIDIMessage.NoteOff:
                    case MIDIMessage.NoteOn: value.Pitch = first; value.Velocity = second; break;
                    case MIDIMessage.Aftertouch: value.Pitch = first; value.Pressure = second; break;
                    case MIDIMessage.ControlChange: value.ControllerNumber = first; value.ControllerValue = second; break;
                    case MIDIMessage.ProgramChange: value.Instrument = first; break;
                    case MIDIMessage.ChannelPressure: value.Pressure = first; break;
                    case MIDIMessage.PitchBend: value.Pitch = first | second << 7; break;
                }
                return value;
            }

            private static unsafe string? GetDeviceInterface(uint device)
            {
                const uint queryInterface = 0x80C;
                const uint queryInterfaceSize = 0x80D;
                uint size = 0;
                if (Native.Message((nint)device, queryInterfaceSize, (nuint)(&size), 0) != 0 ||
                    size < sizeof(char) || size > 65536 || size % sizeof(char) != 0) return null;

                var buffer = new char[size / sizeof(char)];
                fixed (char* pointer = buffer)
                    if (Native.Message((nint)device, queryInterface, (nuint)pointer, size) != 0) return null;

                var length = Array.IndexOf(buffer, '\0');
                if (length <= 0) return null;
                var path = new string(buffer, 0, length);
                // Legacy software drivers may return identifiers outside the Configuration Manager's PnP namespace.
                return GetInterfaceEnabled(path).HasValue ? path : null;
            }

            private static bool? GetInterfaceEnabled(string path)
            {
                var key = new PropertyKey { Format = new Guid(0x026e516e, 0xb814, 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22), ID = 3 };
                uint size = 1;
                return Native.GetInterfaceProperty(path, in key, out var type, out var enabled, ref size, 0) == 0 &&
                    type == 0x11 && size == 1 ? enabled != 0 : null;
            }

            private static void Check(uint result, string operation)
            {
                if (result != 0) throw new IOException($"Could not {operation} for WinMM MIDI input (MMRESULT {result}).");
            }

            private sealed class Port(nuint token)
            {
                internal readonly nuint Token = token;
                internal nint Handle;
                internal string? Interface;
            }

            [StructLayout(LayoutKind.Sequential)]
            private unsafe struct Caps
            {
                internal ushort Manufacturer;
                internal ushort Product;
                internal uint Version;
                internal fixed char Name[32];
                internal uint Support;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PropertyKey
            {
                internal Guid Format;
                internal uint ID;
            }

            private static unsafe partial class Native
            {
                private const string WinMM = "winmm.dll";

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInGetNumDevs")]
                internal static partial uint GetCount();

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInGetDevCapsW")]
                internal static partial uint GetCaps(nuint device, out Caps caps, uint size);

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInOpen")]
                internal static partial uint Open(out nint handle, uint device,
                    delegate* unmanaged[Stdcall]<nint, uint, nuint, nuint, nuint, void> callback, nuint token, uint flags);

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInStart")]
                internal static partial uint Start(nint handle);

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInReset")]
                internal static partial uint Reset(nint handle);

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInClose")]
                internal static partial uint Close(nint handle);

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport(WinMM, EntryPoint = "midiInMessage")]
                internal static partial uint Message(nint device, uint message, nuint first, nuint second);

                [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_Interface_PropertyW", StringMarshalling = StringMarshalling.Utf16)]
                internal static partial uint GetInterfaceProperty(string path, in PropertyKey key, out uint type, out byte value, ref uint size, uint flags);
            }
        }
    }
}
