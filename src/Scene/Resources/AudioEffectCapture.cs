using System.Numerics;

namespace Electron2D;

/// <summary>Copies stereo bus PCM into a bounded ring without changing the audio.</summary>
/// <remarks>The resource owns one ring shared by its instances. First Instantiate fixes its power-of-two
/// storage at the current output rate; one slot is reserved. Later instantiation clears queued data but retains
/// cumulative counters and capacity. Each processing block is copied whole or discarded whole. Reads and
/// processing serialize independently of the native graph. Copies contain configuration, never captured data.</remarks>
public sealed class AudioEffectCapture : AudioEffect
{
    private readonly object _gate = new();
    private float _length = .1f;
    private Vector2[]? _ring;
    private int _read, _write;
    private long _pushed, _discarded;
    /// <summary>Creates an uninitialized capture resource.</summary>
    public AudioEffectCapture() { }
    /// <summary>Gets or sets the requested initial ring duration in seconds.</summary>
    /// <value>0.1 initially; positive finite values. Edits never resize initialized storage.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive and finite.</exception>
    public float BufferLength
    {
        get { lock (_gate) { ThrowIfDisposed(); return _length; } }
        set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_length == value) return; _length = value; } EmitChanged(); }
    }
    private int Available => _ring is null ? 0 : (_write - _read) & (_ring.Length - 1);
    /// <summary>Gets whether a complete requested block is available.</summary>
    /// <param name="frames">Nonnegative stereo frame count.</param>
    /// <returns>True for zero or enough queued frames.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    public bool CanGetBuffer(int frames) { ArgumentOutOfRangeException.ThrowIfNegative(frames); lock (_gate) { ThrowIfDisposed(); return Available >= frames; } }
    /// <summary>Copies and consumes exactly the requested queued frames in FIFO order.</summary>
    /// <param name="frames">Nonnegative stereo frame count.</param>
    /// <returns>A caller-owned array, or empty for zero/insufficient frames without consumption.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    public Vector2[] GetBuffer(int frames)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frames);
        lock (_gate)
        {
            ThrowIfDisposed(); if (frames == 0 || Available < frames) return [];
            var result = new Vector2[frames]; for (var i = 0; i < frames; i++) { result[i] = _ring![_read]; _read = (_read + 1) & (_ring.Length - 1); }
            return result;
        }
    }
    /// <summary>Discards queued frames without resetting capacity or cumulative counters.</summary>
    public void ClearBuffer() { lock (_gate) { ThrowIfDisposed(); _read = _write; } }
    /// <summary>Gets the current readable stereo frame count.</summary>
    /// <returns>Zero before initialization or while empty.</returns>
    public int GetFramesAvailable() { lock (_gate) { ThrowIfDisposed(); return Available; } }
    /// <summary>Gets total ring storage, including the reserved slot.</summary>
    /// <returns>Zero before first instantiation; otherwise the prepared power-of-two size.</returns>
    public int GetBufferLengthFrames() { lock (_gate) { ThrowIfDisposed(); return _ring?.Length ?? 0; } }
    /// <summary>Gets the cumulative number of successfully copied stereo frames.</summary>
    /// <returns>A nonnegative 64-bit count, retained across clearing and instantiation.</returns>
    public long GetPushedFrames() { lock (_gate) { ThrowIfDisposed(); return _pushed; } }
    /// <summary>Gets the cumulative number of frames rejected due to insufficient free space.</summary>
    /// <returns>A nonnegative 64-bit count; failed blocks leave previously queued data intact.</returns>
    public long GetDiscardedFrames() { lock (_gate) { ThrowIfDisposed(); return _discarded; } }
    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Initial storage would exceed 2^27 frames (1 GiB).</exception>
    protected override AudioEffectInstance OnInstantiate()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_ring is null)
            {
                var requested = AudioServer.Instance.GetMixRate() * _length;
                if (!float.IsFinite(requested) || requested >= 1 << 27) throw new ArgumentOutOfRangeException(nameof(BufferLength), "Capture storage exceeds 2^27 frames.");
                var count = (uint)requested; _ring = new Vector2[count == 0 ? 1 : 1 << (BitOperations.Log2(count) + 1)];
            }
            _read = _write; return new AudioEffectCaptureInstance(this);
        }
    }
    internal void Capture(ReadOnlySpan<Vector2> source)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (source.Length > _ring!.Length - 1 - Available) { _discarded = Add(_discarded, source.Length); return; }
            foreach (var frame in source) { _ring[_write] = frame; _write = (_write + 1) & (_ring.Length - 1); }
            _pushed = Add(_pushed, source.Length);
        }
    }
    private static long Add(long value, int count) => value > long.MaxValue - count ? long.MaxValue : value + count;
    private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<AudioEffectCapture, float>(nameof(BufferLength), p => p.BufferLength, (p, v) => p.BufferLength = v, _ => .1f, stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectCapture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) { lock (_gate) { ThrowIfDisposed(); ((AudioEffectCapture)target)._length = _length; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) _ring = null; base.Dispose(disposing); }
}

internal sealed class AudioEffectCaptureInstance(AudioEffectCapture source) : AudioEffectInstance
{
    protected override void OnProcess(ReadOnlySpan<Vector2> input, Span<Vector2> output) { source.Capture(input); input.CopyTo(output); }
    protected override bool OnProcessSilence() => true;
}
