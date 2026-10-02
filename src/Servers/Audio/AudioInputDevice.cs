using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

internal sealed class AudioInputDevice : IDisposable
{
    private readonly object _gate = new();
    private readonly InputHandle _stream;
    private readonly Vector2[] _ring, _scratch;
    private long _written, _read;
    private int _generation;
    private bool _disposed;
    private int _failure;
    internal bool Active { get; private set; }
    internal int MixRate { get; }
    internal int Capacity => _ring.Length;
    internal long CallbackPasses, CallbackManagedBytes;
    internal nint Stream => _stream.DangerousGetHandle();
    internal AudioInputDevice(string name)
    {
        Check(SDL.InitSubSystem(SDL.InitFlags.Audio), "initialize input audio");
        nint stream = 0;
        try
        {
            var id = Resolve(name);
            Check(SDL.GetAudioDeviceFormat(id, out var preferred, out _), "query input format");
            var spec = new SDL.AudioSpec { Format = BitConverter.IsLittleEndian ? SDL.AudioFormat.AudioF32LE : SDL.AudioFormat.AudioF32BE, Channels = 2, Freq = preferred.Freq };
            stream = SDL.OpenAudioDeviceStream(id, in spec, null, 0);
            if (stream == 0) throw Failure("open input device");
            Check(SDL.GetAudioDeviceFormat(SDL.GetAudioStreamDevice(stream), out var actual, out var frames), "query opened input format");
            if (actual.Freq <= 0 || frames is <= 0 or > 262144) throw new NotSupportedException("The input frequency or quantum is outside the bounded capture profile.");
            MixRate = actual.Freq; spec.Freq = MixRate;
            Check(SDL.GetAudioStreamFormat(stream, out var input, out _), "query input stream");
            Check(SDL.SetAudioStreamFormat(stream, in input, in spec), "prepare stereo input conversion");
            _ring = new Vector2[frames * 4]; _scratch = new Vector2[frames];
            SDL.AudioStreamCallback callback = Capture;
            _stream = new InputHandle(stream, callback); stream = 0;
            Check(SDL.SetAudioStreamPutCallback(Stream, callback, 0), "install input callback");
        }
        catch
        {
            if (_stream is not null) _stream.Dispose();
            else { if (stream != 0) SDL.DestroyAudioStream(stream); SDL.QuitSubSystem(SDL.InitFlags.Audio); }
            throw;
        }
    }
    internal static string[] Devices()
    {
        Check(SDL.InitSubSystem(SDL.InitFlags.Audio), "initialize input enumeration");
        try
        {
            var ids = SDL.GetAudioRecordingDevices(out _) ?? throw Failure("enumerate input devices");
            var names = new List<string> { "Default" };
            foreach (var id in ids) { var name = SDL.GetAudioDeviceName(id) ?? throw Failure("query input name"); if (!names.Contains(name)) names.Add(name); }
            return names.ToArray();
        }
        finally { SDL.QuitSubSystem(SDL.InitFlags.Audio); }
    }
    private static uint Resolve(string name)
    {
        if (name == "Default") return SDL.AudioDeviceDefaultRecording;
        var ids = SDL.GetAudioRecordingDevices(out _) ?? throw Failure("enumerate input devices");
        foreach (var id in ids) if (SDL.GetAudioDeviceName(id) == name) return id;
        throw new ArgumentException("The requested input device is unavailable.", nameof(name));
    }
    internal void SetActive(bool active)
    {
        if (active == Active) return;
        if (active)
        {
            Check(SDL.ClearAudioStream(Stream), "clear input conversion");
            lock (_gate) { ObjectDisposedException.ThrowIf(_disposed, this); _written = _read = 0; _generation++; _failure = 0; }
            Check(SDL.ResumeAudioStreamDevice(Stream), "start input device");
        }
        else Check(SDL.PauseAudioStreamDevice(Stream), "stop input device");
        Active = active;
    }
    private void Capture(nint userdata, nint stream, int additionalAmount, int totalAmount)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            while (true)
            {
                var available = SDL.GetAudioStreamAvailable(stream);
                if (available < 0) { Volatile.Write(ref _failure, 1); break; }
                var bytes = Math.Min(available / 8, _scratch.Length) * 8;
                if (bytes == 0) break;
                var count = SDL.GetAudioStreamData(stream, MemoryMarshal.AsBytes(_scratch.AsSpan())[..bytes], bytes);
                if (count < 0 || count % 8 != 0) { Volatile.Write(ref _failure, 1); break; }
                if (count == 0) break;
                lock (_gate)
                {
                    if (_disposed) break;
                    foreach (var frame in _scratch.AsSpan(0, count / 8))
                    {
                        // Native float input can contain nonfinite samples; silence them at the trust boundary.
                        _ring[(int)(_written % Capacity)] = frame.IsFinite() ? new(Math.Clamp(frame.X, -1, 1), Math.Clamp(frame.Y, -1, 1)) : Vector2.Zero; _written++;
                    }
                    _read = Math.Max(_read, _written - Capacity);
                }
            }
        }
        catch { Volatile.Write(ref _failure, 1); }
        finally { Interlocked.Add(ref CallbackManagedBytes, GC.GetAllocatedBytesForCurrentThread() - before); Interlocked.Increment(ref CallbackPasses); }
    }
    internal void CheckFailure() { if (Volatile.Read(ref _failure) != 0) throw new InvalidOperationException("The native input callback failed; restart input capture."); }
    internal int Available { get { lock (_gate) { CheckFailure(); return (int)(_written - _read); } } }
    internal Vector2[] Read(int frames)
    {
        lock (_gate)
        {
            CheckFailure(); if (frames > _written - _read) return [];
            var result = new Vector2[frames]; Copy(result, ref _read); return result;
        }
    }
    private void Copy(Span<Vector2> output, ref long cursor)
    {
        var start = (int)(cursor % Capacity); var first = Math.Min(output.Length, Capacity - start);
        _ring.AsSpan(start, first).CopyTo(output); _ring.AsSpan(0, output.Length - first).CopyTo(output[first..]); cursor += output.Length;
    }
    internal void Mix(Span<Vector2> output, ref long cursor, ref int generation)
    {
        lock (_gate)
        {
            output.Clear(); if (_disposed) return; CheckFailure();
            if (generation != _generation) { generation = _generation; cursor = Math.Max(0, _written - Capacity); }
            if (_written < Math.Min((long)50 * MixRate / 1000, Capacity / 2)) { cursor = 0; return; }
            cursor = Math.Clamp(cursor, Math.Max(0, _written - Capacity), _written);
            Copy(output[..(int)Math.Min(output.Length, _written - cursor)], ref cursor);
        }
    }
    public void Dispose()
    {
        _stream.Dispose(); lock (_gate) { _disposed = true; Active = false; }
    }
    private static InvalidOperationException Failure(string action) => new($"Unable to {action}: {SDL.GetError()}");
    private static void Check(bool success, string action) { if (!success) throw Failure(action); }
    private sealed class InputHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly SDL.AudioStreamCallback _callback;
        internal InputHandle(nint value, SDL.AudioStreamCallback callback) : base(true) { _callback = callback; SetHandle(value); }
        // The stream owns its logical device; retain the delegate until native destruction has joined callbacks.
        protected override bool ReleaseHandle() { SDL.DestroyAudioStream(handle); SDL.QuitSubSystem(SDL.InitFlags.Audio); GC.KeepAlive(_callback); return true; }
    }
}
