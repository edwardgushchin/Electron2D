using System.Runtime.InteropServices;
using F = Electron2D.FAudioBindings.FAudio;

namespace Electron2D;

internal sealed unsafe class FAudioSampleVoice : IDisposable
{
    private readonly FAudioContext _context;
    private readonly AudioSamplePlayback _request;
    private readonly AudioSample _sample;
    private readonly float[] _data, _raw, _matrix, _nativeMatrix;
    private GCHandle _pin, _rawPin;
    private nint _voice, _send;
    private uint _loopBegin, _loopLength;
    private bool _active, _paused, _ending;
    private float _pitch = 1, _volume = 1, _routing = 1;
    private bool _spatial;
    private float _left, _right;
    private readonly int _rate;
    private bool _looping;
    private AudioStreamPlayer.MixTarget _target;
    private double _offset;
    private ulong _startSamples;
    private Exception? _error;
    internal FAudioSampleVoice(FAudioContext context, AudioSamplePlayback request, AudioSample sample, nint send)
    {
        context.EnsureOwner(); request.Check(); _context = context; _request = request; _sample = sample; _send = send; _rate = sample.SampleRate;
        if (sample.SampleRate < F.FAUDIO_MIN_SAMPLE_RATE || sample.SampleRate > F.FAUDIO_MAX_SAMPLE_RATE) throw new NotSupportedException("Native samples require 1000 through 200000 Hz.");
        var channels = sample.NumChannels;
        (_data, _loopBegin, _loopLength) = sample.PrepareNative(); _raw = sample.PCM; if (_data.LongLength * sizeof(float) > F.FAUDIO_MAX_BUFFER_BYTES || _raw.LongLength * sizeof(float) > F.FAUDIO_MAX_BUFFER_BYTES) throw new NotSupportedException("Native sample buffers must not exceed 2 GiB."); _matrix = new float[context.Channels * 2]; _nativeMatrix = new float[context.Channels * channels];
        try
        {
            _pin = GCHandle.Alloc(_data, GCHandleType.Pinned); if (!ReferenceEquals(_raw, _data)) _rawPin = GCHandle.Alloc(_raw, GCHandleType.Pinned);
            var format = new F.FAudioWaveFormatEx { wFormatTag = 3, nChannels = (ushort)channels, nSamplesPerSec = (uint)sample.SampleRate, nAvgBytesPerSec = checked((uint)sample.SampleRate * (uint)channels * 4), nBlockAlign = (ushort)(channels * 4), wBitsPerSample = 32 };
            var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
            FAudioContext.Check(F.FAudio_CreateSourceVoice(context.Engine, out _voice, ref format, 0, F.FAUDIO_MAX_FREQ_RATIO, 0, (nint)(&sends), 0), "create sample source");
            request.Native = this; RefreshPitch(); RefreshMatrix();
        }
        catch { Dispose(); throw; }
    }
    private void Check() { _context.EnsureOwner(); ObjectDisposedException.ThrowIf(_voice == 0, this); }
    internal bool Wrapped;
    internal bool Playing => _active;
    internal bool IsPlaying { get { lock (_context.Gate) { if (_error is { } error) { _error = null; throw new InvalidOperationException("Sample playback failed.", error); } if (_active && !_paused && !_ending && State.BuffersQueued == 0) _ending = true; return _active && !_ending && State.BuffersQueued != 0; } } }
    internal bool Paused => _active && _paused;
    internal nint ActiveSend => _active && !_paused && !_ending ? _send : 0;
    private F.FAudioVoiceState State { get { F.FAudioSourceVoice_GetState(_voice, out var state, 0); return state; } }
    internal bool Finished { get { Check(); if (_error is { } error) { _error = null; throw new InvalidOperationException("Sample playback failed.", error); } if (_active && !_paused && State.BuffersQueued == 0) _ending = true; return _active && !_paused && _ending; } }
    internal double Position
    {
        get
        {
            lock (_context.Gate)
            {
                ObjectDisposedException.ThrowIf(_voice == 0, this); if (!_active) return 0;
                var state = State; if (!_looping && state.BuffersQueued == 0) return _raw.Length / (double)(_sample.NumChannels * _rate);
                var cursor = _offset * _rate + (state.SamplesPlayed >= _startSamples ? state.SamplesPlayed - _startSamples : state.SamplesPlayed);
                var loopLength = _loopLength != 0 ? _loopLength : (uint)(_raw.Length / _sample.NumChannels);
                if (_looping && loopLength > 0 && cursor >= _loopBegin + loopLength) cursor = _loopBegin + (cursor - _loopBegin) % loopLength;
                if (_looping && _sample.LoopMode == AudioLoopMode.Backward && cursor >= _loopBegin) cursor = _sample.LoopEnd - 1 - (cursor - _loopBegin);
                else if (_looping && _sample.LoopMode == AudioLoopMode.PingPong && cursor >= _sample.LoopEnd) cursor = _sample.LoopEnd - 2 - (cursor - _sample.LoopEnd);
                return Math.Max(0, cursor / _rate);
            }
        }
    }
    internal void CheckSource()
    {
        if (_active && _request.Stream.IsDisposed && _error is null)
        { _error = new ObjectDisposedException(nameof(AudioStream)); _ending = true; F.FAudioSourceVoice_Stop(_voice, 0, 0); }
    }
    internal void Play(double position) { Check(); lock (_context.Gate) PlayCore(position); }
    private void PlayCore(double position)
    {
        Check(); _request.Check(); StopCore(); _request.SetOffset(position);
        var loop = _request.Owner?.LoopingOverride ?? _sample.LoopMode != AudioLoopMode.Disabled; _looping = loop;
        var data = loop ? _data : _raw; var frames = (uint)(data.Length / _sample.NumChannels);
        var loopBegin = _loopLength != 0 ? _loopBegin : 0; var loopLength = _loopLength != 0 ? _loopLength : frames;
        var seconds = Math.Max(0, position);
        if (loop && loopLength > 0) { var end = (_loopLength != 0 ? _sample.LoopEnd : frames) / (double)_rate; var start = loopBegin / (double)_rate; if (seconds >= end) seconds = start + (seconds - start) % (end - start); }
        var requested = Math.Min(seconds, frames / (double)_rate) * _rate;
        if (loop && loopLength > 0 && requested >= _sample.LoopEnd && _loopLength != 0) requested = loopBegin + (requested - loopBegin) % (_sample.LoopEnd - loopBegin);
        else if (loop && loopLength > 0 && requested >= frames) requested %= frames;
        var begin = (uint)Math.Min(requested, frames);
        if (loop && _sample.LoopMode == AudioLoopMode.Backward && begin >= loopBegin) begin = loopBegin + (uint)(_sample.LoopEnd - 1) - begin;
        _offset = begin / (double)_rate; _startSamples = State.SamplesPlayed;
        _ending = begin >= frames; _active = true; _paused = false; _error = null;
        if (_ending) return;
        var pointer = !loop && _rawPin.IsAllocated ? _rawPin.AddrOfPinnedObject() : _pin.AddrOfPinnedObject();
        var buffer = new F.FAudioBuffer { AudioBytes = checked((uint)data.Length * 4), pAudioData = pointer, Flags = F.FAUDIO_END_OF_STREAM, PlayBegin = begin, LoopBegin = loop ? loopBegin : 0, LoopLength = loop ? loopLength : 0, LoopCount = loop ? F.FAUDIO_LOOP_INFINITE : 0 };
        FAudioContext.Check(F.FAudioSourceVoice_SubmitSourceBuffer(_voice, ref buffer, 0), "submit complete sample"); FAudioContext.Check(F.FAudioSourceVoice_Start(_voice, 0, 0), "start sample");
    }
    internal void PauseQueued(bool paused) { lock (_context.Gate) PauseCore(paused); }
    internal string RequestedBus => _request.Bus;
    internal void StopQueued() { lock (_context.Gate) { _active = _paused = false; FAudioContext.Check(F.FAudioSourceVoice_Stop(_voice, 0, 0), "stop queued sample"); } }
    internal void Stop() { Check(); lock (_context.Gate) StopCore(); }
    private void StopCore() { _active = _paused = false; FAudioContext.Check(F.FAudioSourceVoice_Stop(_voice, 0, 0), "stop sample"); FAudioContext.Check(F.FAudioSourceVoice_FlushSourceBuffers(_voice), "flush sample"); }
    internal void Pause(bool value) { Check(); lock (_context.Gate) PauseCore(value); }
    private void PauseCore(bool value) { if (_paused == value) return; FAudioContext.Check(value ? F.FAudioSourceVoice_Stop(_voice, 0, 0) : F.FAudioSourceVoice_Start(_voice, 0, 0), "pause sample"); _paused = value; }
    internal void SetPitch(float value) { Check(); lock (_context.Gate) SetPitchCore(value); }
    private void SetPitchCore(float value) { var old = _pitch; _pitch = value; try { RefreshPitch(); } catch { _pitch = old; throw; } }
    internal void RefreshPitch() { Check(); var ratio = (double)_pitch * _request.PitchScale * AudioServer.Instance.PlaybackSpeedScale; if (ratio < F.FAUDIO_MIN_FREQ_RATIO || ratio > F.FAUDIO_MAX_FREQ_RATIO) throw new NotSupportedException("Native sample pitch requires a ratio of 1/1024 through 1024."); FAudioContext.Check(F.FAudioSourceVoice_SetFrequencyRatio(_voice, (float)ratio, 0), "set sample pitch"); }
    internal void SetVolume(float value, float routing) { Check(); lock (_context.Gate) SetVolumeCore(value, routing); }
    private void SetVolumeCore(float value, float routing) { var old = (_volume, _routing); _volume = value; _routing = routing; try { RefreshMatrix(); } catch { (_volume, _routing) = old; throw; } }
    internal void SetTarget(AudioStreamPlayer.MixTarget value) { Check(); lock (_context.Gate) { _target = value; RefreshMatrix(); } }
    internal void SetSpatial(float left, float right) { Check(); lock (_context.Gate) { _spatial = true; _left = left; _right = right; RefreshMatrix(); } }
    internal void SetSend(nint send) { Check(); lock (_context.Gate) { _context.SetSend(_voice, send); _send = send; RefreshMatrix(); } }
    internal void RefreshLooping() { Check(); lock (_context.Gate) { if (_active && !_ending) { var paused = _paused; PlayCore(Position); if (paused) PauseCore(true); } } }
    internal void RefreshBus() { SetSend(AudioServer.Instance.ResolveBus(_request.Bus)); if (!Wrapped) { _routing = AudioServer.Instance.ResolveSourceGain(_request.Bus, 1); RefreshMatrix(); } }
    internal void RefreshStandaloneGain() { if (!Wrapped) { _routing = AudioServer.Instance.ResolveSourceGain(_request.Bus, 1); RefreshMatrix(); } }
    internal void RefreshMatrix()
    {
        Check(); if (_spatial) { Array.Clear(_matrix); _matrix[0] = _volume * _routing * _left; _matrix[3] = _volume * _routing * _right; }
        else FAudioStreamVoice.FillOutputMatrix(_matrix, _context.Channels, _target, _volume, _routing);
        for (var channel = 0; channel < _context.Channels; channel++)
        {
            var pair = _request.Gain(channel / 2); var gain = channel % 2 == 0 ? pair.X : pair.Y;
            for (var source = 0; source < _sample.NumChannels; source++)
            {
                var coefficient = _sample.NumChannels == 1 ? _matrix[channel * 2] + _matrix[channel * 2 + 1] : _matrix[channel * 2 + source];
                var value = coefficient * gain; if (!float.IsFinite(value)) throw new ArithmeticException("Sample gain exceeds finite matrix coefficients."); _nativeMatrix[channel * _sample.NumChannels + source] = value;
            }
        }
        fixed (float* matrix = _nativeMatrix) FAudioContext.Check(F.FAudioVoice_SetOutputMatrix(_voice, _send, (uint)_sample.NumChannels, (uint)_context.Channels, (nint)matrix, 0), "set sample matrix");
    }
    internal void MarkActivity(FAudioBusEffect.Activity activity) { if (_spatial || _context.Channels == 2 || _target != AudioStreamPlayer.MixTarget.Center) activity.Use(0); if (!_spatial && _context.Channels >= 4 && _target != AudioStreamPlayer.MixTarget.Stereo) for (var i = 1; i < _context.Channels / 2; i++) if (i == 1 || _target == AudioStreamPlayer.MixTarget.Surround) activity.Use(i); }
    public void Dispose() { lock (_context.Gate) { _active = false; if (_voice != 0) { F.FAudioVoice_DestroyVoice(_voice); _voice = 0; } if (_pin.IsAllocated) _pin.Free(); if (_rawPin.IsAllocated) _rawPin.Free(); if (ReferenceEquals(_request.Native, this)) _request.Native = null; _context.ForgetSample(this); } }
}
