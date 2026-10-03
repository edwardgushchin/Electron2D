namespace Electron2D;

/// <summary>Owns a copied finite mono/stereo PCM snapshot and native sample loop metadata.</summary>
/// <remarks>Returned by AudioStream.GenerateSample. The originating stream is borrowed; copied PCM and
/// loop settings are independent of later edits. Registration and native voices retain their own snapshot.
/// Data inspection returns a cold caller-owned copy; playback never decodes or copies an asset per quantum.</remarks>
public sealed class AudioSample : ElectronObject
{
    private float[] _data;
    private readonly object _gate = new();
    private (float[] Data, uint Begin, uint Length)? _native;
    private readonly AudioStream _stream;
    private readonly int _channels, _rate, _begin, _end;
    private readonly AudioLoopMode _loop;
    /// <summary>Creates a validated immutable PCM snapshot for an existing stream.</summary>
    /// <param name="stream">Borrowed live originating resource.</param>
    /// <param name="data">Finite interleaved channel samples; copied.</param>
    /// <param name="numChannels">One or two channels.</param>
    /// <param name="sampleRate">Positive source frequency in Hz.</param>
    /// <param name="loopMode">Defined sample traversal policy.</param>
    /// <param name="loopBegin">Inclusive first loop frame, ignored for Disabled.</param>
    /// <param name="loopEnd">Exclusive loop end, ignored for Disabled.</param>
    /// <exception cref="ArgumentException">PCM dimensions or frames are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Rate, selector or enabled loop bounds are invalid.</exception>
    /// <exception cref="ObjectDisposedException">The borrowed stream is disposed.</exception>
    public AudioSample(AudioStream stream, ReadOnlySpan<float> data, int numChannels, int sampleRate, AudioLoopMode loopMode = AudioLoopMode.Disabled, int loopBegin = 0, int loopEnd = 0)
    {
        ArgumentNullException.ThrowIfNull(stream); ObjectDisposedException.ThrowIf(stream.IsDisposed, stream);
        if (numChannels is not (1 or 2) || data.Length % numChannels != 0) throw new ArgumentException("Sample PCM requires complete mono/stereo frames.", nameof(data));
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (loopMode is < AudioLoopMode.Disabled or > AudioLoopMode.Backward) throw new ArgumentOutOfRangeException(nameof(loopMode));
        if (loopMode != AudioLoopMode.Disabled && (loopBegin < 0 || loopEnd <= loopBegin || loopEnd > data.Length / numChannels)) throw new ArgumentOutOfRangeException(nameof(loopEnd));
        foreach (var value in data) if (!float.IsFinite(value)) throw new ArgumentException("Sample PCM must be finite.", nameof(data));
        _stream = stream; _data = data.ToArray(); _channels = numChannels; _rate = sampleRate; _loop = loopMode; _begin = loopBegin; _end = loopEnd;
    }
    /// <summary>Gets the borrowed originating resource.</summary>
    /// <value>Borrowed source identity; not disposed by this object.</value>
    public AudioStream Stream { get { ThrowIfDisposed(); return _stream; } }
    /// <summary>Gets copied interleaved PCM.</summary>
    /// <value>Caller-owned interleaved copy.</value>
    public float[] Data { get { ThrowIfDisposed(); return (float[])_data.Clone(); } }
    /// <summary>Gets the source channel count.</summary>
    /// <value>One or two.</value>
    public int NumChannels { get { ThrowIfDisposed(); return _channels; } }
    /// <summary>Gets the source frequency in Hz.</summary>
    /// <value>Positive source Hz.</value>
    public int SampleRate { get { ThrowIfDisposed(); return _rate; } }
    /// <summary>Gets the copied loop policy.</summary>
    /// <value>Copied Disabled/Forward/PingPong/Backward selection.</value>
    public AudioLoopMode LoopMode { get { ThrowIfDisposed(); return _loop; } }
    /// <summary>Gets the inclusive loop begin frame.</summary>
    /// <value>Inclusive source frame index.</value>
    public int LoopBegin { get { ThrowIfDisposed(); return _begin; } }
    /// <summary>Gets the exclusive loop end frame.</summary>
    /// <value>Exclusive source frame boundary.</value>
    public int LoopEnd { get { ThrowIfDisposed(); return _end; } }
    internal float[] PCM { get { ThrowIfDisposed(); return _data; } }
    internal ReadOnlySpan<float> Samples { get { ThrowIfDisposed(); return _data; } }
    internal (float[] Data, uint Begin, uint Length) PrepareNative()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_native is { } prepared) return prepared;
            var data = _data; uint begin = 0, length = 0;
            if (_loop == AudioLoopMode.Forward) { begin = (uint)_begin; length = (uint)(_end - _begin); }
            else if (_loop == AudioLoopMode.PingPong)
            {
                var reverse = Math.Max(0, _end - _begin - 2); data = new float[checked((_end + reverse) * _channels)]; _data.AsSpan(0, _end * _channels).CopyTo(data);
                for (var i = 0; i < reverse; i++) _data.AsSpan((_end - 2 - i) * _channels, _channels).CopyTo(data.AsSpan((_end + i) * _channels));
                begin = (uint)_begin; length = (uint)(data.Length / _channels - _begin);
            }
            else if (_loop == AudioLoopMode.Backward)
            {
                data = new float[_end * _channels]; _data.AsSpan(0, _begin * _channels).CopyTo(data);
                for (var i = _begin; i < _end; i++) _data.AsSpan((_end - 1 - (i - _begin)) * _channels, _channels).CopyTo(data.AsSpan(i * _channels));
                begin = (uint)_begin; length = (uint)(_end - _begin);
            }
            return (_native = (data, begin, length)).Value;
        }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { _data = []; _native = null; } base.Dispose(disposing); }
}
