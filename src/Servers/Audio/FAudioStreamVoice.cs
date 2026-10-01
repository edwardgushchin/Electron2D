using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using F = Electron2D.FAudioBindings.FAudio;

namespace Electron2D;

internal sealed unsafe class FAudioStreamVoice : IDisposable
{
    private readonly FAudioContext _context;
    private AudioStreamPlayback _playback;
    private readonly Vector2[] _frames;
    private readonly float[] _ring;
    private GCHandle _pin, _self;
    private Callback* _callback;
    private nint _voice, _send;
    private int _cursor;
    private bool _active, _paused, _ending;
    private float _pitch = 1, _volume = 1, _routingGain = 1;
    private AudioStreamPlayer.MixTarget _target;
    private Exception? _error;
    private readonly float[] _matrix;
    [StructLayout(LayoutKind.Sequential)]
    private struct Callback { internal F.FAudioVoiceCallback Functions; internal nint User; }
    internal FAudioStreamVoice(FAudioContext context, AudioStreamPlayback playback, nint send)
    {
        context.EnsureOwner(); _context = context; _send = send; _playback = playback; _frames = new Vector2[context.QuantumFrames]; _ring = new float[context.QuantumFrames * 2 * 4]; _matrix = new float[context.Channels * 2];
        lock (context.Gate) try
            {
                _pin = GCHandle.Alloc(_ring, GCHandleType.Pinned); _self = GCHandle.Alloc(this); _callback = (Callback*)NativeMemory.AllocZeroed((nuint)sizeof(Callback));
                _callback->User = GCHandle.ToIntPtr(_self); _callback->Functions.OnVoiceProcessingPassStart = (nint)(delegate* unmanaged[Cdecl]<nint, uint, void>)&Process;
                var format = new F.FAudioWaveFormatEx { wFormatTag = 3, nChannels = 2, nSamplesPerSec = (uint)context.MixRate, nAvgBytesPerSec = checked((uint)context.MixRate * 8), nBlockAlign = 8, wBitsPerSample = 32 };
                var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
                FAudioContext.Check(F.FAudio_CreateSourceVoice(context.Engine, out _voice, ref format, F.FAUDIO_VOICE_NOPITCH | F.FAUDIO_VOICE_NOSRC, 1, (nint)_callback, (nint)(&sends), 0), "create streamed source");
            }
            catch { Dispose(); throw; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Process(nint callback, uint bytesRequired)
    {
        var source = (FAudioStreamVoice)GCHandle.FromIntPtr(((Callback*)callback)->User).Target!;
        if (!source._active || source._paused) return;
        try
        {
            var mixed = source._ending ? 0 : source._playback.MixInto(source._frames, source._pitch);
            source._frames.AsSpan(mixed).Clear(); if (mixed != source._frames.Length) source._ending = true;
            var output = MemoryMarshal.Cast<Vector2, float>(source._frames);
            var begin = source._cursor * 2; var first = Math.Min(output.Length, source._ring.Length - begin); output[..first].CopyTo(source._ring.AsSpan(begin)); output[first..].CopyTo(source._ring);
            source._cursor = (source._cursor + source._frames.Length) % (source._ring.Length / 2);
        }
        catch (Exception error) { source._error = error; source._ending = true; source._ring.AsSpan().Clear(); }
    }
    private void Check() { _context.EnsureOwner(); ObjectDisposedException.ThrowIf(_voice == 0, this); }
    internal bool Finished { get { Check(); lock (_context.Gate) { if (_error is { } error) { _error = null; throw new InvalidOperationException("Audio stream mixing failed.", error); } return _active && !_paused && _ending; } } }
    internal bool Playing { get { Check(); lock (_context.Gate) return _active; } }
    internal bool Paused { get { Check(); lock (_context.Gate) return _active && _paused; } }
    internal double Position { get { Check(); lock (_context.Gate) { return _active ? _playback.GetPlaybackPosition() : 0; } } }
    internal void Play(double position)
    {
        Check(); lock (_context.Gate)
        {
            _active = false; _paused = false; StopNative(); _playback.Start(position); _ring.AsSpan().Clear(); _cursor = 0; _error = null; _ending = false; _paused = false;
            var buffer = new F.FAudioBuffer { AudioBytes = (uint)(_ring.Length * 4), pAudioData = _pin.AddrOfPinnedObject(), LoopCount = F.FAUDIO_LOOP_INFINITE, LoopLength = (uint)(_ring.Length / 2) };
            FAudioContext.Check(F.FAudioSourceVoice_SubmitSourceBuffer(_voice, ref buffer, 0), "prepare looping stream ring"); FAudioContext.Check(F.FAudioSourceVoice_Start(_voice, 0, 0), "start streamed source"); _active = true;
        }
    }
    internal void Stop()
    {
        Check(); lock (_context.Gate)
        {
            var active = _active; _active = false; _paused = false; try { if (active) _playback.Stop(); } finally { StopNative(); }
        }
    }
    private void StopNative()
    {
        FAudioContext.Check(F.FAudioSourceVoice_Stop(_voice, 0, 0), "stop streamed source"); FAudioContext.Check(F.FAudioSourceVoice_FlushSourceBuffers(_voice), "flush streamed source");
    }
    internal void Pause(bool value) { Check(); lock (_context.Gate) { _paused = value; FAudioContext.Check(value ? F.FAudioSourceVoice_Stop(_voice, 0, 0) : F.FAudioSourceVoice_Start(_voice, 0, 0), "pause streamed source"); } }
    internal void SetPitch(float value) { Check(); lock (_context.Gate) _pitch = value; }
    internal void SetVolume(float value, float routingGain) { Check(); lock (_context.Gate) { _volume = value; _routingGain = routingGain; SetMixTarget(_target); } }
    internal void SetSend(nint voice) { Check(); lock (_context.Gate) { _context.SetSend(_voice, voice); _send = voice; } }
    internal void SetMixTarget(AudioStreamPlayer.MixTarget target)
    {
        Check(); lock (_context.Gate)
        {
            _target = target; var count = _context.Channels; FillOutputMatrix(_matrix, count, target, _volume, _routingGain);
            fixed (float* matrix = _matrix) FAudioContext.Check(F.FAudioVoice_SetOutputMatrix(_voice, _send, 2, (uint)count, (nint)matrix, 0), "set source mix target");
        }
    }
    internal static void FillOutputMatrix(Span<float> matrix, int channels, AudioStreamPlayer.MixTarget target, float volume, float routingGain)
    {
        matrix.Clear(); var gain = volume * routingGain;
        if (channels == 2 || target != AudioStreamPlayer.MixTarget.Center) { matrix[0] = gain; matrix[3] = gain; }
        if (channels >= 4 && target != AudioStreamPlayer.MixTarget.Stereo)
        {
            matrix[4] = gain; matrix[7] = routingGain;
            if (target == AudioStreamPlayer.MixTarget.Surround)
                for (var i = 4; i < channels; i += 2) { matrix[i * 2] = gain; matrix[(i + 1) * 2 + 1] = gain; }
        }
    }
    internal void ReplacePlayback(AudioStreamPlayback playback)
    {
        Check(); lock (_context.Gate) { Stop(); var old = _playback; _playback = playback; old.Dispose(); }
    }
    internal AudioStreamPlayback Playback => _playback;
    public void Dispose()
    {
        lock (_context.Gate)
        {
            _active = false; if (_voice != 0) { F.FAudioVoice_DestroyVoice(_voice); _voice = 0; }
            if (_pin.IsAllocated) _pin.Free(); if (_self.IsAllocated) _self.Free(); if (_callback is not null) { NativeMemory.Free(_callback); _callback = null; }
            _context.ForgetSource(this);
        }
    }
}
