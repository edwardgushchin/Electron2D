namespace Electron2D;

/// <summary>Applies a live decibel gain to each stereo bus frame.</summary>
/// <remarks>Each instance borrows this resource and ramps changes over its next processed block.</remarks>
public sealed class AudioEffectAmplify : AudioEffect
{
    private readonly object _gate = new();
    private float _volumeDB;

    /// <summary>Creates an effect at unity gain.</summary>
    public AudioEffectAmplify() { }

    /// <summary>Gets or sets the gain in decibels.</summary>
    /// <value>Zero initially; negative infinity represents silence.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is invalid or its linear gain overflows.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float VolumeDB
    {
        get { lock (_gate) { ThrowIfDisposed(); return _volumeDB; } }
        set
        {
            if (float.IsNaN(value) || value == float.PositiveInfinity || !float.IsFinite(Mathf.DBToLinear(value)))
                throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_volumeDB == value) return; _volumeDB = value; }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the nonnegative linear gain.</summary>
    /// <value>One initially; zero represents silence.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain is negative or nonfinite.</exception>
    public float VolumeLinear
    {
        get => Mathf.DBToLinear(VolumeDB);
        set
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            VolumeDB = value == 0 ? float.NegativeInfinity : Mathf.LinearToDB(value);
        }
    }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectAmplifyInstance(this);

    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectAmplify, float>(nameof(VolumeDB), p => p.VolumeDB, (p, v) => p.VolumeDB = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectAmplify, float>(nameof(VolumeLinear), p => p.VolumeLinear, (p, v) => p.VolumeLinear = v, _ => 1)
    ];

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectAmplify();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        ((AudioEffectAmplify)target)._volumeDB = VolumeDB;
}

internal sealed class AudioEffectAmplifyInstance : AudioEffectInstance
{
    private readonly AudioEffectAmplify _source;
    private float _previousDB;

    internal AudioEffectAmplifyInstance(AudioEffectAmplify source) { _source = source; _previousDB = source.VolumeDB; }

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var nextDB = _source.VolumeDB;
        var gain = Mathf.DBToLinear(_previousDB);
        var increment = source.Length == 0 ? 0 : (Mathf.DBToLinear(nextDB) - gain) / source.Length;
        for (var i = 0; i < source.Length; i++)
        {
            destination[i] = source[i] * gain;
            gain += increment;
        }
        _previousDB = nextDB;
    }
}

/// <summary>Moves stereo bus audio between left and right channels.</summary>
public sealed class AudioEffectPanner : AudioEffect
{
    private readonly object _gate = new();
    private float _pan;

    /// <summary>Creates a centered panner.</summary>
    public AudioEffectPanner() { }

    /// <summary>Gets or sets pan; minus one is left and plus one is right.</summary>
    /// <value>Zero initially. Finite raw values outside the normal range are retained and clamped while processing.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Pan
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pan; } }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_pan == value) return; _pan = value; }
            EmitChanged();
        }
    }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectPannerInstance(this);

    private static readonly PropertyDescriptor[] Properties =
    [new PropertyDescriptor<AudioEffectPanner, float>(nameof(Pan), p => p.Pan, (p, v) => p.Pan = v, _ => 0, stored: true)];

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectPanner();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        ((AudioEffectPanner)target)._pan = Pan;
}

internal sealed class AudioEffectPannerInstance : AudioEffectInstance
{
    private readonly AudioEffectPanner _source;

    internal AudioEffectPannerInstance(AudioEffectPanner source) => _source = source;

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var pan = _source.Pan;
        var left = Math.Clamp(1f - pan, 0, 1);
        var right = Math.Clamp(1f + pan, 0, 1);
        for (var i = 0; i < source.Length; i++)
        {
            var frame = source[i];
            destination[i] = new(frame.X * left + frame.Y * (1 - right),
                frame.Y * right + frame.X * (1 - left));
        }
    }
}
