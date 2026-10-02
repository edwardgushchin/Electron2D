namespace Electron2D;

/// <summary>Feeds copied procedural stereo frames through a bounded queue and prepared cubic resampler.</summary>
/// <remarks>Created by AudioStreamGenerator. X is left, Y is right. Producer/control/mix operations share
/// one per-playback gate; prepared successful pushes and mixing allocate no storage. Underruns deliver
/// silence without ending playback. Stop retains queued frames/history; ClearBuffer is stopped-only.</remarks>
public sealed class AudioStreamGeneratorPlayback : AudioStreamPlaybackResampled
{
    private readonly AudioStreamGenerator _source;
    private Vector2[] _frames;
    private int _read, _write, _skips;
    private bool _active;
    private double _mixed;
    private float _rate;
    internal AudioStreamGeneratorPlayback(AudioStreamGenerator source, int length) { _source = source; _frames = new Vector2[length]; }
    private void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(_source.IsDisposed, _source); }
    private int Mask => _frames.Length - 1;
    private int Available => (_read - _write - 1) & Mask;
    /// <summary>Gets how many source stereo frames can be pushed without overflowing the queue.</summary>
    /// <returns>Free slots; zero when full. Prefetched resampling history is outside this queue.</returns>
    /// <exception cref="ObjectDisposedException">The playback or source is disposed.</exception>
    public int GetFramesAvailable() { lock (ResampleGate) { Check(); return Available; } }
    /// <summary>Checks whether a complete buffer fits.</summary>
    /// <param name="amount">Nonnegative frame count.</param>
    /// <returns>True when amount does not exceed current free slots.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The playback or source is disposed.</exception>
    public bool CanPushBuffer(int amount) { lock (ResampleGate) { Check(); ArgumentOutOfRangeException.ThrowIfNegative(amount); return amount <= Available; } }
    /// <summary>Copies one finite stereo frame into the queue when a free slot exists.</summary>
    /// <param name="frame">Finite left/right samples; values are not clamped to the unit range.</param>
    /// <returns>False on overflow without changing the queue; otherwise true.</returns>
    /// <exception cref="ArgumentException">A sample is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The playback or source is disposed.</exception>
    public bool PushFrame(Vector2 frame)
    {
        lock (ResampleGate) { Check(); if (!frame.IsFinite()) throw new ArgumentException("PCM frames must be finite.", nameof(frame)); if (Available == 0) return false; _frames[_write] = frame; _write = (_write + 1) & Mask; return true; }
    }
    /// <summary>Copies a complete finite frame span into the queue atomically.</summary>
    /// <param name="frames">Borrowed stereo samples, copied before returning.</param>
    /// <returns>False when the whole buffer does not fit, with no partial write; true for an empty buffer.</returns>
    /// <exception cref="ArgumentException">Any frame is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The playback or source is disposed.</exception>
    public bool PushBuffer(ReadOnlySpan<Vector2> frames)
    {
        lock (ResampleGate)
        {
            Check(); foreach (var frame in frames) if (!frame.IsFinite()) throw new ArgumentException("PCM frames must be finite.", nameof(frames));
            if (frames.Length > Available) return false;
            var first = Math.Min(frames.Length, _frames.Length - _write); frames[..first].CopyTo(_frames.AsSpan(_write)); frames[first..].CopyTo(_frames); _write = (_write + frames.Length) & Mask; return true;
        }
    }
    /// <summary>Gets the number of source-block underruns since the last Start.</summary>
    /// <returns>A nonnegative counter saturated at Int32.MaxValue.</returns>
    /// <exception cref="ObjectDisposedException">The playback or source is disposed.</exception>
    public int GetSkips() { lock (ResampleGate) { Check(); return _skips; } }
    /// <summary>Clears queued source frames and the generated-time counter while stopped.</summary>
    /// <remarks>Does not reset skips. The next Start resets interpolation history; active clear rejects.</remarks>
    /// <exception cref="InvalidOperationException">Playback is active.</exception>
    /// <exception cref="ObjectDisposedException">The playback or source is disposed.</exception>
    public void ClearBuffer() { lock (ResampleGate) { Check(); if (_active) throw new InvalidOperationException("Stop playback before clearing its generator queue."); _read = _write = 0; _mixed = 0; } }
    /// <inheritdoc />
    protected override void OnStart(double fromPosition)
    {
        lock (ResampleGate)
        {
            Check(); _rate = _source.PrepareRate();
            if (_mixed == 0) { _active = false; BeginResample(); }
            _skips = 0; _active = true; _mixed = 0;
        }
    }
    /// <inheritdoc />
    protected override void OnStop() { lock (ResampleGate) _active = false; }
    /// <inheritdoc />
    protected override bool OnIsPlaying() { lock (ResampleGate) { Check(); return _active; } }
    /// <inheritdoc />
    protected override double OnGetPlaybackPosition() { lock (ResampleGate) { Check(); return _mixed; } }
    /// <inheritdoc />
    protected override void OnSeek(double time) { lock (ResampleGate) Check(); }
    /// <inheritdoc />
    protected override float OnGetStreamSamplingRate() => _rate;
    /// <inheritdoc />
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        lock (ResampleGate) { Check(); if (!_active) { buffer.Clear(); return 0; } _rate = _source.TargetRate; return base.OnMix(buffer, rateScale); }
    }
    /// <inheritdoc />
    protected override int OnMixResampled(Span<Vector2> buffer)
    {
        lock (ResampleGate)
        {
            Check(); buffer.Clear(); if (!_active) return buffer.Length;
            _rate = _source.TargetRate;
            var count = Math.Min(buffer.Length, (_write - _read) & Mask); var first = Math.Min(count, _frames.Length - _read);
            _frames.AsSpan(_read, first).CopyTo(buffer); _frames.AsSpan(0, count - first).CopyTo(buffer[first..]); _read = (_read + count) & Mask;
            if (count < buffer.Length && _skips < int.MaxValue) _skips++;
            _mixed += buffer.Length / (double)_rate; return buffer.Length;
        }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (ResampleGate) { _active = false; _frames = []; } base.Dispose(disposing); }
}
