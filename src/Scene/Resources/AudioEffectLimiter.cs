namespace Electron2D;

/// <summary>Applies a legacy soft-clip curve and a sample ceiling to stereo bus audio.</summary>
/// <remarks>Channels are processed independently without delay or history. Existing instances borrow this
/// resource and read one coherent setting snapshot per block. Prefer <see cref="AudioEffectHardLimiter"/>
/// for linked lookahead limiting. Buses borrow the resource and own their per-pair instances.</remarks>
public sealed class AudioEffectLimiter : AudioEffect
{
    private readonly object _gate = new();
    private float _thresholdDB, _ceilingDB = -.1f, _softClipDB = 2, _softClipRatio = 10;

    /// <summary>Creates the default curve with a -0.1 dB ceiling and zero threshold.</summary>
    public AudioEffectLimiter() { }

    /// <summary>Gets or sets the decibel threshold used for gain compensation.</summary>
    /// <value>Zero initially; the ordinary authoring range is -30 through 0 dB.</value>
    /// <remarks>Compensation is the linear gain of CeilingDB minus ThresholdDB.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float ThresholdDB { get { lock (_gate) { ThrowIfDisposed(); return _thresholdDB; } } set => Set(ref _thresholdDB, value); }

    /// <summary>Gets or sets the maximum absolute output sample in decibels.</summary>
    /// <value>-0.1 initially; the ordinary authoring range is -20 through -0.1 dB.</value>
    /// <remarks>Finite controls outside the ordinary range are retained. An underflowed linear ceiling produces silence.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or its linear ceiling overflows.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float CeilingDB
    {
        get { lock (_gate) { ThrowIfDisposed(); return _ceilingDB; } }
        set { if (!float.IsFinite(Mathf.DBToLinear(value))) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _ceilingDB, value); }
    }

    /// <summary>Gets or sets the decibel control for the soft-clip branch.</summary>
    /// <value>Two initially; the ordinary authoring range is zero through six dB.</value>
    /// <remarks>The branch starts above the linear amplitude of minus this value after gain compensation.
    /// The legacy curve need not be continuous at that boundary.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float SoftClipDB { get { lock (_gate) { ThrowIfDisposed(); return _softClipDB; } } set => Set(ref _softClipDB, value); }

    /// <summary>Gets or sets a retained legacy authoring value with no effect on PCM.</summary>
    /// <value>Ten initially; the ordinary authoring range is three through twenty.</value>
    /// <remarks>This setting participates in change notification and resource copying, but the curve does not read it.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float SoftClipRatio { get { lock (_gate) { ThrowIfDisposed(); return _softClipRatio; } } set => Set(ref _softClipRatio, value); }

    private void Set(ref float field, float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        lock (_gate) { ThrowIfDisposed(); if (field == value) return; field = value; }
        EmitChanged();
    }
    internal readonly record struct Settings(float ThresholdDB, float CeilingDB, float SoftClipDB, float SoftClipRatio);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_thresholdDB, _ceilingDB, _softClipDB, _softClipRatio); } }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectLimiterInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectLimiter, float>(nameof(ThresholdDB), e => e.ThresholdDB, (e, v) => e.ThresholdDB = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectLimiter, float>(nameof(CeilingDB), e => e.CeilingDB, (e, v) => e.CeilingDB = v, _ => -.1f, stored: true),
        new PropertyDescriptor<AudioEffectLimiter, float>(nameof(SoftClipDB), e => e.SoftClipDB, (e, v) => e.SoftClipDB = v, _ => 2, stored: true),
        new PropertyDescriptor<AudioEffectLimiter, float>(nameof(SoftClipRatio), e => e.SoftClipRatio, (e, v) => e.SoftClipRatio = v, _ => 10, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectLimiter();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = Snapshot(); var copy = (AudioEffectLimiter)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._thresholdDB = state.ThresholdDB; copy._ceilingDB = state.CeilingDB; copy._softClipDB = state.SoftClipDB; copy._softClipRatio = state.SoftClipRatio; }
    }
}

internal sealed class AudioEffectLimiterInstance(AudioEffectLimiter source) : AudioEffectInstance
{
    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> input, Span<Vector2> destination)
    {
        var settings = source.Snapshot();
        var ceiling = Mathf.DBToLinear(settings.CeilingDB);
        if (ceiling == 0) { destination.Clear(); return; }
        var gain = Mathf.DBToLinear((double)settings.CeilingDB - settings.ThresholdDB);
        var startDB = -(double)settings.SoftClipDB;
        var start = Mathf.DBToLinear(startDB);
        // A branch starting above the ceiling can only end at the final cap. Skipping it also avoids
        // its zero denominator at startDB == CeilingDB + 25 for raw finite authoring controls.
        var multiplier = start < ceiling ? Math.Abs(((double)settings.CeilingDB - startDB) / (settings.CeilingDB + 25d - startDB)) : 0;
        for (var i = 0; i < input.Length; i++)
        {
            var frame = input[i];
            destination[i] = new(Sample(frame.X, gain, ceiling, start, multiplier, settings.CeilingDB),
                Sample(frame.Y, gain, ceiling, start, multiplier, settings.CeilingDB));
        }
    }

    private static float Sample(float input, double gain, float ceiling, double start, double multiplier, float ceilingDB)
    {
        if (input == 0) return 0;
        var magnitude = Math.Abs((double)input) * gain;
        if (start < ceiling && magnitude > start)
            magnitude = start + (multiplier == 0 ? 1 : Mathf.DBToLinear((Mathf.LinearToDB(magnitude) - ceilingDB) * multiplier));
        return (float)Math.Min(ceiling, magnitude) * (input < 0 ? -1 : 1);
    }
}
