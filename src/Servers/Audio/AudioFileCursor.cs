namespace Electron2D;

internal readonly record struct AudioMusicLoop(bool Loop, double Offset, double BPM, int Beats);

// ponytail: decoded PCM keeps repeated mixing bounded and allocation-free; indexed streaming is the upgrade for long assets.
internal sealed class AudioFileCursor(AudioStream source, AudioDecodedPCM pcm, Func<AudioMusicLoop> getLoop, Func<long>? validateVersion = null, Func<float>? samplingRate = null, long? capturedVersion = null)
{
    private readonly Vector2[] _fade = new Vector2[256];
    private int _fadeIndex = 256;
    private long _frame;
    private bool _active;
    private int _loops;
    private readonly long _version = capturedVersion ?? validateVersion?.Invoke() ?? 0;
    internal float Rate { get { var rate = samplingRate?.Invoke() ?? pcm.Rate; if (!float.IsFinite(rate) || rate <= 0) throw new InvalidOperationException("Playback requires a positive finite sampling rate."); return rate; } }
    internal bool Active { get { Check(); return _active; } }
    internal int Loops { get { Check(); return _loops; } }
    internal double Position { get { Check(); return _frame / (double)Rate; } }
    private void Check() { ObjectDisposedException.ThrowIf(source.IsDisposed, source); if (validateVersion is not null && validateVersion() != _version) throw new InvalidOperationException("The captured packet sequence changed."); }
    internal void Start(double position) { Check(); _active = true; Seek(position); _loops = 0; _fadeIndex = 256; }
    internal void Stop() => _active = false;
    internal void Seek(double position)
    {
        Check(); if (!_active) return; var length = pcm.Samples.Length / pcm.Channels;
        _frame = position >= length / (double)Rate ? 0 : (long)Math.Max(0, position * Rate);
    }
    internal int Mix(Span<Vector2> output, bool? loopingOverride)
    {
        Check(); var settings = getLoop(); var looping = loopingOverride ?? settings.Loop;
        var length = pcm.Samples.Length / pcm.Channels;
        var beatFrames = looping && settings.BPM > 0 && settings.Beats > 0 ? settings.Beats * (double)Rate * 60 / settings.BPM : double.PositiveInfinity;
        var beatEnd = beatFrames <= length ? (long)Math.Max(1, beatFrames) : -1;
        var mixed = 0; var emptyLoops = 0;
        while (mixed < output.Length && _active)
        {
            if (_frame >= length || beatEnd >= 0 && _frame >= beatEnd)
            {
                if (!looping || length == 0) { _active = false; break; }
                if (beatEnd >= 0 && _frame < length)
                {
                    _fade.AsSpan().Clear(); var amount = (int)Math.Min(256, length - _frame);
                    for (var i = 0; i < amount; i++) _fade[i] = ReadFrame(_frame + i);
                    _fadeIndex = 0;
                }
                Seek(settings.Offset); _loops++;
                if (_frame >= length || beatEnd >= 0 && _frame >= beatEnd)
                { if (++emptyLoops > 1) throw new InvalidOperationException("The loop offset cannot reach a playable frame."); }
                continue;
            }
            emptyLoops = 0; var frame = ReadFrame(_frame++);
            if (_fadeIndex < 256) { frame += _fade[_fadeIndex] * ((256 - _fadeIndex) / 256f); _fadeIndex++; }
            output[mixed++] = frame;
        }
        output[mixed..].Clear(); return mixed;
    }
    private Vector2 ReadFrame(long frame) { var offset = checked((int)frame * pcm.Channels); return new(pcm.Samples[offset], pcm.Samples[offset + Math.Min(1, pcm.Channels - 1)]); }
}
