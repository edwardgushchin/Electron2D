using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using F = Electron2D.FAudioBindings.FAudio;

namespace Electron2D;

internal sealed unsafe class FAudioStreamVoice : IDisposable
{
    private readonly FAudioContext _context;
    private AudioStreamPlayback _playback;
    private FAudioSampleVoice? _sample;
    private readonly Vector2[] _frames;
    private readonly float[] _ring, _output, _tail, _previousMatrix;
    private readonly Vector2[] _lookahead = new Vector2[64];
    private bool _tailPending;
    private GCHandle _pin, _self;
    private Callback* _callback;
    private nint _voice, _send;
    private int _cursor;
    private bool _active, _paused, _ending;
    private float _pitch = 1, _volume = 1, _routingGain = 1;
    private AudioStreamPlayer.MixTarget _target;
    private bool _spatial;
    private float _spatialLeft, _spatialRight;
    private Exception? _error;
    private readonly float[] _matrix;
    [StructLayout(LayoutKind.Sequential)]
    private struct Callback { internal F.FAudioVoiceCallback Functions; internal nint User; }
    internal FAudioStreamVoice(FAudioContext context, AudioStreamPlayback playback, nint send)
    {
        context.EnsureOwner(); _context = context; _send = send; _playback = playback; if (playback.GetSamplePlayback() is { } request) { _frames = []; _ring = _output = _tail = _matrix = _previousMatrix = []; _sample = context.CreateSample(request, AudioServer.Service.GetSample(request.Stream), send); _sample.Wrapped = true; return; }
        _frames = new Vector2[context.QuantumFrames + 64]; _ring = new float[context.QuantumFrames * context.Channels * 4]; _output = new float[context.QuantumFrames * context.Channels]; _tail = new float[_output.Length]; _matrix = new float[context.Channels * 2]; _previousMatrix = new float[_matrix.Length];
        lock (context.Gate) try
            {
                _pin = GCHandle.Alloc(_ring, GCHandleType.Pinned); _self = GCHandle.Alloc(this); _callback = (Callback*)NativeMemory.AllocZeroed((nuint)sizeof(Callback));
                _callback->User = GCHandle.ToIntPtr(_self); _callback->Functions.OnVoiceProcessingPassStart = (nint)(delegate* unmanaged[Cdecl]<nint, uint, void>)&Process;
                var format = new F.FAudioWaveFormatEx { wFormatTag = 3, nChannels = (ushort)context.Channels, nSamplesPerSec = (uint)context.MixRate, nAvgBytesPerSec = checked((uint)context.MixRate * (uint)context.Channels * 4), nBlockAlign = (ushort)(context.Channels * 4), wBitsPerSample = 32 };
                var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
                FAudioContext.Check(F.FAudio_CreateSourceVoice(context.Engine, out _voice, ref format, F.FAUDIO_VOICE_NOPITCH | F.FAUDIO_VOICE_NOSRC, 1, (nint)_callback, (nint)(&sends), 0), "create streamed source");
            }
            catch { Dispose(); throw; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Process(nint callback, uint bytesRequired)
    {
        var source = (FAudioStreamVoice)GCHandle.FromIntPtr(((Callback*)callback)->User).Target!;
        try
        {
            source.MixBlock(source._output);
            var begin = source._cursor * source._context.Channels;
            source._output.CopyTo(source._ring.AsSpan(begin));
            source._cursor = (source._cursor + source._context.QuantumFrames) % (source._ring.Length / source._context.Channels);
        }
        catch (Exception error) { source._error = error; source._ending = true; source._ring.AsSpan().Clear(); source._tail.AsSpan().Clear(); source._tailPending = false; }
    }
    // Caller holds the context gate; the same prepared block path serves mixing and owner transitions.
    internal void MixBlock(Span<float> output)
    {
        output.Clear();
        if (_active && !_paused && !_ending)
        {
            ReadFrames(); RouteFrames(output, fading: false);
            _matrix.CopyTo(_previousMatrix, 0);
        }
        if (_tailPending)
        {
            for (var i = 0; i < output.Length; i++) output[i] += _tail[i];
            _tail.AsSpan().Clear(); _tailPending = false;
        }
        foreach (var sample in output) if (!float.IsFinite(sample)) throw new ArithmeticException("Audio source gain overflowed finite PCM.");
    }
    private void ReadFrames()
    {
        var count = _context.QuantumFrames;
        _lookahead.CopyTo(_frames, 0);
        int mixed;
        AudioServer.EnterAudioProcessing();
        try { mixed = _playback.MixInto(_frames.AsSpan(64, count), _pitch); }
        finally { AudioServer.ExitAudioProcessing(); }
        _frames.AsSpan(64 + mixed).Clear();
        if (mixed != count)
        {
            var gain = 1f;
            for (var i = mixed; i < count; i++) { gain *= .94f; _frames[i] *= gain; }
            _ending = true; _lookahead.AsSpan().Clear();
        }
        else _frames.AsSpan(count, 64).CopyTo(_lookahead);
    }
    private void RouteFrames(Span<float> output, bool fading)
    {
        var count = _context.QuantumFrames;
        for (var frame = 0; frame < count; frame++)
        {
            var weight = (float)frame / count;
            for (var channel = 0; channel < _context.Channels; channel++)
            {
                var index = channel * 2;
                var left = (1 - weight) * _previousMatrix[index] + (fading ? 0 : weight * _matrix[index]);
                var right = (1 - weight) * _previousMatrix[index + 1] + (fading ? 0 : weight * _matrix[index + 1]);
                var sample = _frames[frame].X * left + _frames[frame].Y * right;
                if (!float.IsFinite(sample)) throw new ArithmeticException("Audio source gain overflowed finite PCM.");
                output[frame * _context.Channels + channel] += sample;
            }
        }
    }
    private void PrepareTail()
    {
        if (!_active || _paused || _ending) return;
        ReadFrames(); _output.AsSpan().Clear(); RouteFrames(_output, fading: true);
        for (var i = 0; i < _tail.Length; i++) if (!float.IsFinite(_tail[i] + _output[i])) throw new ArithmeticException("Overlapping audio tails overflowed finite PCM.");
        for (var i = 0; i < _tail.Length; i++) _tail[i] += _output[i];
        _tailPending = true;
        _previousMatrix.AsSpan().Clear();
    }
    internal bool SamplingPaused => _paused || !_active;
    internal void ConfigureSampleChild(FAudioSampleVoice sample)
    {
        sample.SetPitch(_pitch); sample.SetVolume(_volume, AudioServer.Service.ResolveSourceGain(sample.RequestedBus, 1)); sample.SetTarget(_target);
        if (_spatial) sample.SetSpatial(_spatialLeft, _spatialRight); sample.Pause(_paused || !_active);
    }
    private void RefreshSampleChildren() => _playback.UpdateNativeOwner(this);
    private void Check() { _context.EnsureOwner(); ObjectDisposedException.ThrowIf(_voice == 0, this); }
    internal bool Finished { get { if (_sample is { } sample) return sample.Finished; Check(); lock (_context.Gate) { if (_error is { } error) { _error = null; throw new InvalidOperationException("Audio stream mixing failed.", error); } return _active && !_paused && _ending; } } }
    internal bool Playing { get { if (_sample is { } sample) return sample.Playing; Check(); lock (_context.Gate) return _active; } }
    internal nint ActiveSend => _sample is not null ? 0 : (_active && !_paused) || _tailPending ? _send : 0;
    internal void MarkActivity(FAudioBusEffect.Activity activity)
    {
        if (_spatial) { activity.Use(0); return; }
        if (_context.Channels == 2 || _target != AudioStreamPlayer.MixTarget.Center) activity.Use(0);
        if (_context.Channels >= 4 && _target != AudioStreamPlayer.MixTarget.Stereo)
            for (var pair = 1; pair < _context.Channels / 2; pair++) if (pair == 1 || _target == AudioStreamPlayer.MixTarget.Surround) activity.Use(pair);
    }
    internal bool Paused { get { if (_sample is { } sample) return sample.Paused; Check(); lock (_context.Gate) return _active && _paused; } }
    internal double Position { get { if (_sample is { } sample) return sample.Position; Check(); lock (_context.Gate) { return _active ? _playback.GetPlaybackPosition() : 0; } } }
    internal void Play(double position)
    {
        if (_sample is { } sample) { sample.Play(position); return; }
        Check(); lock (_context.Gate)
        {
            _active = false; _paused = false; StopNative(); _playback.Start(position); _ring.AsSpan().Clear(); _lookahead.AsSpan().Clear(); _matrix.CopyTo(_previousMatrix, 0); _cursor = 0; _error = null; _ending = false; _paused = false;
            var buffer = new F.FAudioBuffer { AudioBytes = (uint)(_ring.Length * 4), pAudioData = _pin.AddrOfPinnedObject(), LoopCount = F.FAUDIO_LOOP_INFINITE, LoopLength = (uint)(_ring.Length / _context.Channels) };
            FAudioContext.Check(F.FAudioSourceVoice_SubmitSourceBuffer(_voice, ref buffer, 0), "prepare looping stream ring"); FAudioContext.Check(F.FAudioSourceVoice_Start(_voice, 0, 0), "start streamed source"); _active = true; RefreshSampleChildren();
        }
    }
    internal void Stop()
    {
        if (_sample is { } sample) { sample.Stop(); return; }
        Check(); lock (_context.Gate)
        {
            var active = _active; List<Exception>? errors = null;
            try { PrepareTail(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            _active = false; _paused = false; _lookahead.AsSpan().Clear(); _previousMatrix.AsSpan().Clear();
            try { if (active) _playback.Stop(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Audio source stopping failed.", errors);
        }
    }
    private void StopNative()
    {
        FAudioContext.Check(F.FAudioSourceVoice_Stop(_voice, 0, 0), "stop streamed source"); FAudioContext.Check(F.FAudioSourceVoice_FlushSourceBuffers(_voice), "flush streamed source");
    }
    internal void Pause(bool value)
    {
        if (_sample is { } sample) { sample.Pause(value); return; }
        Check(); lock (_context.Gate)
        {
            if (_paused == value) return;
            try { if (value) PrepareTail(); }
            finally { _paused = value; if (value) _previousMatrix.AsSpan().Clear(); RefreshSampleChildren(); }
        }
    }
    internal void SetPitch(float value) { if (_sample is { } sample) { sample.SetPitch(value); _pitch = value; return; } Check(); lock (_context.Gate) { _pitch = value; RefreshSampleChildren(); } }
    internal void SetVolume(float value, float routingGain)
    {
        if (_sample is { } sample) { sample.SetVolume(value, routingGain); _volume = value; _routingGain = routingGain; return; }
        Check(); lock (_context.Gate)
        {
            var gateChanged = _routingGain != routingGain;
            _volume = value; _routingGain = routingGain; SetMixTarget(_target);
            if (gateChanged) { _matrix.CopyTo(_previousMatrix, 0); if (routingGain == 0) { _tail.AsSpan().Clear(); _tailPending = false; } }
        }
    }
    internal void SetSend(nint voice) { if (_sample is { } sample) { sample.SetSend(voice); _send = voice; return; } Check(); lock (_context.Gate) { _context.SetSend(_voice, voice); _send = voice; } }
    internal void SetMixTarget(AudioStreamPlayer.MixTarget target)
    {
        if (_sample is { } sample) { sample.SetTarget(target); _target = target; return; }
        Check(); lock (_context.Gate)
        {
            _target = target; var count = _context.Channels;
            if (_spatial)
            {
                _matrix.AsSpan().Clear();
                _matrix[0] = _volume * _routingGain * _spatialLeft;
                _matrix[3] = _volume * _routingGain * _spatialRight;
            }
            else FillOutputMatrix(_matrix, count, target, _volume, _routingGain);
            RefreshSampleChildren();
        }
    }
    internal void SetSpatial(float left, float right)
    {
        if (_sample is { } sample) { sample.SetSpatial(left, right); _spatial = true; _spatialLeft = left; _spatialRight = right; return; }
        Check();
        if (!float.IsFinite(left) || !float.IsFinite(right) || left < 0 || right < 0)
            throw new ArgumentOutOfRangeException(nameof(left));
        lock (_context.Gate) { _spatial = true; _spatialLeft = left; _spatialRight = right; SetMixTarget(_target); }
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
    internal void MoveTailTo(FAudioStreamVoice target)
    {
        if (_sample is not null || target._sample is not null) { _context.EnsureOwner(); lock (_context.Gate) { _tail.AsSpan().Clear(); _tailPending = false; } return; }
        Check(); target.Check(); lock (_context.Gate)
        {
            if (!_tailPending) return;
            for (var i = 0; i < _tail.Length; i++) if (!float.IsFinite(target._tail[i] + _tail[i])) throw new ArithmeticException("Retired audio tails overflowed finite PCM.");
            for (var i = 0; i < _tail.Length; i++) target._tail[i] += _tail[i];
            target._tailPending = true; _tail.AsSpan().Clear(); _tailPending = false;
        }
    }
    internal void ReplacePlayback(AudioStreamPlayback playback)
    {
        if (_sample is { } sample)
        {
            if (playback.GetSamplePlayback() is not { } request) throw new InvalidOperationException("Sample/stream mode changed without releasing its slot.");
            var next = _context.CreateSample(request, AudioServer.Service.GetSample(request.Stream), _send); next.Wrapped = true;
            try { next.SetPitch(_pitch); next.SetVolume(_volume, _routingGain); next.SetTarget(_target); if (_spatial) next.SetSpatial(_spatialLeft, _spatialRight); } catch { next.Dispose(); throw; }
            sample.Dispose(); var old = _playback; old.SceneOwner = null; old.UpdateNativeOwner(null); _playback = playback; playback.SceneOwner = this; _sample = next; old.Dispose(); return;
        }
        Check(); lock (_context.Gate) { Stop(); var old = _playback; old.SceneOwner = null; old.UpdateNativeOwner(null); _playback = playback; playback.SceneOwner = this; RefreshSampleChildren(); old.Dispose(); }
    }
    internal AudioStreamPlayback Playback => _playback;
    public void Dispose()
    {
        lock (_context.Gate)
        {
            _playback.SceneOwner = null; _playback.UpdateNativeOwner(null); _sample?.Dispose(); _sample = null; _active = false; if (_voice != 0) { F.FAudioVoice_DestroyVoice(_voice); _voice = 0; }
            if (_pin.IsAllocated) _pin.Free(); if (_self.IsAllocated) _self.Free(); if (_callback is not null) { NativeMemory.Free(_callback); _callback = null; }
            _context.ForgetSource(this);
        }
    }
}
