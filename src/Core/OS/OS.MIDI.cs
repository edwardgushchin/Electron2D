using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class OS
{
    private readonly MIDIInputOwner _midiInputs = new();

    /// <summary>Opens and subscribes to the currently available MIDI input sources.</summary>
    /// <remarks>Opening an active session is a no-op. Linux uses the system ALSA Sequencer independently of the
    /// window and audio backends. Sources added later require close/open. Source indices remain stable until close.
    /// Events arrive before the next engine frame's physics/process stages, or through Input.FlushBufferedEvents
    /// on the active MainLoop's owner thread. No source is a valid empty session.</remarks>
    /// <exception cref="NotSupportedException">The platform input adapter or system library is unavailable.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the native input service is denied.</exception>
    /// <exception cref="IOException">Native acquisition or subscription fails; partial acquisition is rolled back.</exception>
    public static void OpenMIDIInputs() => Service._midiInputs.Open();

    /// <summary>Returns a copy of the source names in the active MIDI input session, indexed by event Device.</summary>
    /// <returns>The session's source names, or an empty array when closed or no sources were present at open.</returns>
    /// <remarks>A disconnected source or lost native input fails the whole session explicitly; close or reopen
    /// to acknowledge the failure and reset application note state. Querying names does not consume events.</remarks>
    /// <exception cref="NotSupportedException">This host requires its native input adapter.</exception>
    /// <exception cref="IOException">The session previously failed during input delivery.</exception>
    public static string[] GetConnectedMIDIInputs() => Service._midiInputs.GetNames();

    /// <summary>Closes the active MIDI input session and discards its pending events.</summary>
    /// <remarks>Closing is repeat-safe, including after acquisition or delivery failure. Engine shutdown also
    /// closes the session after finalization, including when a user callback throws.</remarks>
    /// <exception cref="IOException">The native client reports a close failure after ownership was retired.</exception>
    public static void CloseMIDIInputs() => Service._midiInputs.Close();

    internal static void PumpMIDIInputs() => Service._midiInputs.Pump();

    private sealed class MIDIInputOwner
    {
        private readonly object _gate = new();
        private Session? _session;
        private IOException? _failure;
        private bool _pumping;

        private static void EnsurePlatform()
        {
            if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                throw new NotSupportedException("MIDI input requires this host's native adapter. The Linux adapter uses ALSA Sequencer.");
        }

        internal void Open()
        {
            EnsurePlatform();
            lock (_gate)
            {
                if (_session is not null) return;
                _failure = null;
                try { _session = Session.Open(); }
                catch (DllNotFoundException error) { throw new NotSupportedException("The system ALSA library is unavailable.", error); }
                catch (EntryPointNotFoundException error) { throw new NotSupportedException("The system ALSA Sequencer API is unavailable.", error); }
            }
        }

        internal string[] GetNames()
        {
            EnsurePlatform();
            lock (_gate)
            {
                if (_failure is not null) throw new IOException("The MIDI input session failed; close or reopen it before continuing.", _failure);
                return _session?.Names.ToArray() ?? [];
            }
        }

        internal void Close()
        {
            lock (_gate)
            {
                var previous = _session;
                _session = null;
                _failure = null;
                previous?.Close();
            }
        }

        internal void Pump()
        {
            lock (_gate)
            {
                if (_failure is not null) throw new IOException("The MIDI input session failed; close or reopen it before continuing.", _failure);
                var current = _session;
                if (current is null || Engine.MainLoop is not { } mainLoop) return;
                mainLoop.ValidateInputEventDispatch();
                if (_pumping) throw new InvalidOperationException("MIDI input delivery cannot be re-entered.");
                _pumping = true;
                try
                {
                    // Bound one frame's work; remaining events stay in ALSA's bounded input pool.
                    for (var count = 0; count < 4096 && ReferenceEquals(_session, current); count++)
                    {
                        InputEventMIDI? value;
                        try { value = current.Read(out var available); if (!available) break; }
                        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                        {
                            _session = null;
                            _failure = error as IOException ?? new IOException("MIDI input access was revoked.", error);
                            try { current.Close(); } catch (IOException closeError) { throw new AggregateException(error, closeError); }
                            throw;
                        }
                        using (value)
                            if (value is not null) Input.ParseInputEvent(value);
                    }
                }
                finally { _pumping = false; }
            }
        }

        private sealed class Session
        {
            private nint _handle;
            private readonly Dictionary<int, int> _sources = [];
            internal readonly List<string> Names = [];

            internal static Session Open()
            {
                var session = new Session();
                try
                {
                    Check(ALSA.Open(out session._handle, "default", 2, 1), "open sequencer");
                    Check(ALSA.SetName(session._handle, "Electron2D MIDI input"), "name input client");
                    var port = ALSA.CreatePort(session._handle, "MIDI input", 2 | 64, 2 | (1 << 20));
                    Check(port, "create input port");
                    // Announcement ordering detects removal/address reuse before subsequent source data.
                    Check(ALSA.Connect(session._handle, port, 0, 1), "subscribe to device lifetime notifications");
                    Check(ALSA.NewClientInfo(out var clientInfo), "allocate client query");
                    try
                    {
                        Check(ALSA.NewPortInfo(out var portInfo), "allocate port query");
                        try
                        {
                            ALSA.SetClient(clientInfo, -1);
                            int result;
                            while ((result = ALSA.NextClient(session._handle, clientInfo)) >= 0)
                            {
                                var client = ALSA.GetClient(clientInfo);
                                if (client == 0 || client == ALSA.ClientID(session._handle)) continue;
                                ALSA.SetPortClient(portInfo, client);
                                ALSA.SetPort(portInfo, -1);
                                int portResult;
                                while ((portResult = ALSA.NextPort(session._handle, portInfo)) >= 0)
                                {
                                    if ((ALSA.Capabilities(portInfo) & 33) != 33 || (ALSA.PortType(portInfo) & 2) == 0) continue;
                                    var sourcePort = ALSA.GetPort(portInfo);
                                    Check(ALSA.Connect(session._handle, port, client, sourcePort), "subscribe to MIDI source");
                                    session._sources.Add((client << 8) | sourcePort, session.Names.Count);
                                    session.Names.Add($"{Marshal.PtrToStringUTF8(ALSA.ClientName(clientInfo))}: {Marshal.PtrToStringUTF8(ALSA.PortName(portInfo))}");
                                }
                                if (portResult != -2) Check(portResult, "enumerate MIDI ports");
                            }
                            if (result != -2) Check(result, "enumerate MIDI clients");
                        }
                        finally { ALSA.FreePortInfo(portInfo); }
                    }
                    finally { ALSA.FreeClientInfo(clientInfo); }
                    return session;
                }
                catch
                {
                    if (session._handle != 0) { ALSA.Close(session._handle); session._handle = 0; }
                    throw;
                }
            }

            internal void Close()
            {
                var handle = _handle;
                _handle = 0;
                if (handle != 0) Check(ALSA.Close(handle), "close sequencer");
            }

            internal InputEventMIDI? Read(out bool available)
            {
                var result = ALSA.Read(_handle, out var pointer);
                available = result != -11;
                if (!available) return null;
                Check(result, "read MIDI input (device removal or input overflow requires reopening)");
                var e = Marshal.PtrToStructure<ALSA.Event>(pointer);
                if (e.SourceClient == 0)
                {
                    var address = (e.Channel << 8) | e.Note;
                    if ((e.Type == 64 && _sources.ContainsKey(address)) ||
                        (e.Type == 61 && _sources.Keys.Any(key => (key >> 8) == e.Channel)) ||
                        (e.Type == 67 && _sources.ContainsKey(address)))
                        throw new IOException("A MIDI source disconnected; the input session was closed. Reset note state and reopen inputs.");
                    return null;
                }
                if (!_sources.TryGetValue((e.SourceClient << 8) | e.SourcePort, out var device)) return null;
                // Variable payloads (including SysEx) and UMP are outside the MIDI 1 input contract.
                if ((e.Flags & 44) != 0) return null;
                var message = e.Type switch
                {
                    6 => MIDIMessage.NoteOn,
                    7 => MIDIMessage.NoteOff,
                    8 => MIDIMessage.Aftertouch,
                    10 => MIDIMessage.ControlChange,
                    11 => MIDIMessage.ProgramChange,
                    12 => MIDIMessage.ChannelPressure,
                    13 => MIDIMessage.PitchBend,
                    20 => MIDIMessage.SongPositionPointer,
                    21 => MIDIMessage.SongSelect,
                    22 => MIDIMessage.QuarterFrame,
                    30 => MIDIMessage.Start,
                    31 => MIDIMessage.Continue,
                    32 => MIDIMessage.Stop,
                    36 => MIDIMessage.TimingClock,
                    40 => MIDIMessage.TuneRequest,
                    41 => MIDIMessage.SystemReset,
                    42 => MIDIMessage.ActiveSensing,
                    _ => MIDIMessage.None,
                };
                if (message == MIDIMessage.None) return null;
                if ((int)message < 16 && (e.Channel > 15 ||
                    (e.Type is 6 or 7 or 8 && (e.Note > 127 || e.Velocity > 127)) ||
                    (e.Type == 10 && (e.Parameter > 127 || e.Value is < 0 or > 127)) ||
                    (e.Type is 11 or 12 && e.Value is < 0 or > 127) ||
                    (e.Type == 13 && e.Value is < -8192 or > 8191)))
                    throw new IOException("A MIDI source supplied an invalid channel message; the session was closed.");
                var value = new InputEventMIDI { Device = device, Message = message, Channel = (int)message < 16 ? e.Channel : 0 };
                switch (message)
                {
                    case MIDIMessage.NoteOn:
                    case MIDIMessage.NoteOff: value.Pitch = e.Note; value.Velocity = e.Velocity; break;
                    case MIDIMessage.Aftertouch: value.Pitch = e.Note; value.Pressure = e.Velocity; break;
                    case MIDIMessage.ControlChange: value.ControllerNumber = (int)e.Parameter; value.ControllerValue = e.Value; break;
                    case MIDIMessage.ProgramChange: value.Instrument = e.Value; break;
                    case MIDIMessage.ChannelPressure: value.Pressure = e.Value; break;
                    case MIDIMessage.PitchBend: value.Pitch = e.Value + 8192; break;
                }
                return value;
            }
        }

        private static void Check(int result, string operation)
        {
            if (result >= 0) return;
            var message = $"Could not {operation}: {Marshal.PtrToStringUTF8(ALSA.Error(result))} ({result}).";
            if (result is -1 or -13) throw new UnauthorizedAccessException(message);
            throw new IOException(message);
        }

        private static class ALSA
        {
            private const string Library = "libasound.so.2";
            // Legacy ALSA event ABI: four-byte header, eight-byte timestamp, two addresses, twelve-byte union.
            [StructLayout(LayoutKind.Explicit, Size = 28)]
            internal struct Event
            {
                [FieldOffset(0)] internal byte Type;
                [FieldOffset(1)] internal byte Flags;
                [FieldOffset(12)] internal byte SourceClient;
                [FieldOffset(13)] internal byte SourcePort;
                [FieldOffset(16)] internal byte Channel;
                [FieldOffset(17)] internal byte Note;
                [FieldOffset(18)] internal byte Velocity;
                [FieldOffset(20)] internal uint Parameter;
                [FieldOffset(24)] internal int Value;
            }
            [DllImport(Library, EntryPoint = "snd_seq_open", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int Open(out nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int streams, int mode);
            [DllImport(Library, EntryPoint = "snd_seq_close", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int Close(nint handle);
            [DllImport(Library, EntryPoint = "snd_seq_set_client_name", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int SetName(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
            [DllImport(Library, EntryPoint = "snd_seq_create_simple_port", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int CreatePort(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, uint capabilities, uint type);
            [DllImport(Library, EntryPoint = "snd_seq_client_id", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int ClientID(nint handle);
            [DllImport(Library, EntryPoint = "snd_seq_connect_from", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int Connect(nint handle, int port, int client, int sourcePort);
            [DllImport(Library, EntryPoint = "snd_seq_event_input", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int Read(nint handle, out nint value);
            [DllImport(Library, EntryPoint = "snd_strerror", CallingConvention = CallingConvention.Cdecl)]
            internal static extern nint Error(int result);
            [DllImport(Library, EntryPoint = "snd_seq_client_info_malloc", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int NewClientInfo(out nint info);
            [DllImport(Library, EntryPoint = "snd_seq_client_info_free", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void FreeClientInfo(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_query_next_client", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int NextClient(nint handle, nint info);
            [DllImport(Library, EntryPoint = "snd_seq_client_info_get_name", CallingConvention = CallingConvention.Cdecl)]
            internal static extern nint ClientName(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_malloc", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int NewPortInfo(out nint info);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_free", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void FreePortInfo(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_query_next_port", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int NextPort(nint handle, nint info);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_get_name", CallingConvention = CallingConvention.Cdecl)]
            internal static extern nint PortName(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_client_info_set_client", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void SetClient(nint info, int client);
            [DllImport(Library, EntryPoint = "snd_seq_client_info_get_client", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int GetClient(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_set_client", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void SetPortClient(nint info, int client);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_set_port", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void SetPort(nint info, int port);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_get_port", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int GetPort(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_get_capability", CallingConvention = CallingConvention.Cdecl)]
            internal static extern uint Capabilities(nint info);
            [DllImport(Library, EntryPoint = "snd_seq_port_info_get_type", CallingConvention = CallingConvention.Cdecl)]
            internal static extern uint PortType(nint info);
        }
    }
}
