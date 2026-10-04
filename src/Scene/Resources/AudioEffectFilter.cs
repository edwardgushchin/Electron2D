namespace Electron2D;

/// <summary>Configures a reusable stereo frequency filter; the base resource uses low-pass processing.</summary>
/// <remarks>Each instance owns four prepared stages per channel. Live settings affect the next block without
/// clearing history; disabling or bypassing retains it. Resources are borrowed and copies contain configuration.
/// Prefer a concrete filter for a named response. Only shelf filters consume Gain.</remarks>
public class AudioEffectFilter : AudioEffect
{
    /// <summary>Selects the cutoff steepness preset and number of cascaded filter stages.</summary>
    public enum FilterDB
    {
        /// <summary>First preset, one stage.</summary>
        Filter6DB = 0,
        /// <summary>Second preset, two stages.</summary>
        Filter12DB = 1,
        /// <summary>Third preset, three stages.</summary>
        Filter18DB = 2,
        /// <summary>Fourth preset, four stages.</summary>
        Filter24DB = 3
    }
    private readonly object _gate = new();
    private readonly AudioFilterKernel.Mode _mode;
    private float _cutoff = 2000, _resonance = .5f, _gain = 1;
    private FilterDB _db;
    /// <summary>Creates low-pass configuration with 2000 Hz cutoff, 0.5 resonance, unity gain and first preset.</summary>
    public AudioEffectFilter() : this(AudioFilterKernel.Mode.LowPass) { }
    internal AudioEffectFilter(AudioFilterKernel.Mode mode) => _mode = mode;
    /// <summary>Gets or sets the requested frequency threshold in hertz.</summary>
    /// <value>2000 initially; finite values below one clamp to one. Processing bounds it below actual Nyquist.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float CutoffHZ
    {
        get { lock (_gate) { ThrowIfDisposed(); return _cutoff; } }
        set { Finite(value); value = Math.Max(1, value); lock (_gate) { ThrowIfDisposed(); if (_cutoff == value) return; _cutoff = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the finite resonance/width control.</summary>
    /// <value>0.5 initially. Pass/notch/shelf processing uses a positive quality floor; BandLimit uses it as
    /// the lower band edge and bounds that edge below the effective upper cutoff.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Resonance
    {
        get { lock (_gate) { ThrowIfDisposed(); return _resonance; } }
        set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_resonance == value) return; _resonance = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the finite shelf gain coefficient.</summary>
    /// <value>One initially; shelf processing floors it at 0.001 and distributes it across selected stages.
    /// Other concrete filters ignore this value.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Gain
    {
        get { lock (_gate) { ThrowIfDisposed(); return _gain; } }
        set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_gain == value) return; _gain = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the cutoff steepness preset.</summary>
    /// <value>Filter6DB initially; presets select one through four prepared stages.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public FilterDB DB
    {
        get { lock (_gate) { ThrowIfDisposed(); return _db; } }
        set { if (value is < FilterDB.Filter6DB or > FilterDB.Filter24DB) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_db == value) return; _db = value; } EmitChanged(); }
    }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal AudioFilterKernel.Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_mode, _cutoff, _resonance, _gain, (int)_db + 1); } }
    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectFilterInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectFilter, float>(nameof(CutoffHZ), p => p.CutoffHZ, (p, v) => p.CutoffHZ = v, _ => 2000, stored: true),
        new PropertyDescriptor<AudioEffectFilter, float>(nameof(Resonance), p => p.Resonance, (p, v) => p.Resonance = v, _ => .5f, stored: true),
        new PropertyDescriptor<AudioEffectFilter, float>(nameof(Gain), p => p.Gain, (p, v) => p.Gain = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectFilter, FilterDB>(nameof(DB), p => p.DB, (p, v) => p.DB = v, _ => FilterDB.Filter6DB, stored: true)
    ];
    /// <inheritdoc />
    /// <remarks>Pass filters omit the unused Gain from authoring descriptors; other types retain it.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(this is AudioEffectLowPassFilter or AudioEffectHighPassFilter or AudioEffectBandPassFilter ? Properties.Where(p => p.Name != nameof(Gain)) : Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectFilter();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var settings = Snapshot(); var other = (AudioEffectFilter)target;
        lock (other._gate) { other.ThrowIfDisposed(); other._cutoff = settings.Cutoff; other._resonance = settings.Resonance; other._gain = settings.Gain; other._db = (FilterDB)(settings.Stages - 1); }
    }
}

/// <summary>Attenuates frequencies above the cutoff with independent stereo history.</summary>
public sealed class AudioEffectLowPassFilter : AudioEffectFilter
{
    /// <summary>Creates the default low-pass response.</summary>
    public AudioEffectLowPassFilter() : base(AudioFilterKernel.Mode.LowPass) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectLowPassFilter();
}

/// <summary>Attenuates frequencies below the cutoff with independent stereo history.</summary>
public sealed class AudioEffectHighPassFilter : AudioEffectFilter
{
    /// <summary>Creates the default high-pass response.</summary>
    public AudioEffectHighPassFilter() : base(AudioFilterKernel.Mode.HighPass) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectHighPassFilter();
}

/// <summary>Passes a band near the cutoff and attenuates frequencies outside it.</summary>
public sealed class AudioEffectBandPassFilter : AudioEffectFilter
{
    /// <summary>Creates the default band-pass response.</summary>
    public AudioEffectBandPassFilter() : base(AudioFilterKernel.Mode.BandPass) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectBandPassFilter();
}

/// <summary>Attenuates a band near the cutoff while passing lower and higher frequencies.</summary>
public sealed class AudioEffectNotchFilter : AudioEffectFilter
{
    /// <summary>Creates the default notch response.</summary>
    public AudioEffectNotchFilter() : base(AudioFilterKernel.Mode.Notch) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectNotchFilter();
}

/// <summary>Rejects a broad band between the resonance lower edge and effective cutoff upper edge.</summary>
/// <remarks>Processing preserves the bandwidth denominator and uses its complementary band-rejection numerator.
/// The response differs from the narrower quality-controlled notch.</remarks>
public sealed class AudioEffectBandLimitFilter : AudioEffectFilter
{
    /// <summary>Creates the default broad band-rejection response.</summary>
    public AudioEffectBandLimitFilter() : base(AudioFilterKernel.Mode.BandLimit) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectBandLimitFilter();
}

/// <summary>Changes low-frequency gain while retaining the high-frequency shelf.</summary>
public sealed class AudioEffectLowShelfFilter : AudioEffectFilter
{
    /// <summary>Creates the default low-shelf response.</summary>
    public AudioEffectLowShelfFilter() : base(AudioFilterKernel.Mode.LowShelf) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectLowShelfFilter();
}

/// <summary>Changes high-frequency gain while retaining the low-frequency shelf.</summary>
public sealed class AudioEffectHighShelfFilter : AudioEffectFilter
{
    /// <summary>Creates the default high-shelf response.</summary>
    public AudioEffectHighShelfFilter() : base(AudioFilterKernel.Mode.HighShelf) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectHighShelfFilter();
}

internal sealed class AudioEffectFilterInstance : AudioEffectInstance
{
    private readonly AudioEffectFilter _source;
    private readonly AudioFilterKernel.History[] _history = new AudioFilterKernel.History[8];
    private AudioFilterKernel.Settings _settings;
    private AudioFilterKernel.Coefficients _coefficients;
    private float _rate;
    internal AudioEffectFilterInstance(AudioEffectFilter source)
    {
        _source = source; _settings = source.Snapshot(); _rate = AudioServer.GetMixRate(); _coefficients = AudioFilterKernel.Prepare(_settings, _rate);
    }
    protected override void OnProcess(ReadOnlySpan<Vector2> input, Span<Vector2> output)
    {
        var settings = _source.Snapshot(); var rate = AudioServer.GetMixRate();
        if (settings.Cutoff != _settings.Cutoff || settings.Resonance != _settings.Resonance || settings.Gain != _settings.Gain || settings.Stages != _settings.Stages || rate != _rate) { var coefficients = AudioFilterKernel.Prepare(settings, rate); _coefficients = coefficients; _settings = settings; _rate = rate; }
        for (var i = 0; i < input.Length; i++)
        {
            var frame = input[i];
            for (var stage = 0; stage < settings.Stages; stage++) { frame.X = _history[stage].Process(frame.X, _coefficients); frame.Y = _history[stage + 4].Process(frame.Y, _coefficients); }
            if (!float.IsFinite(frame.X) || !float.IsFinite(frame.Y)) { Array.Clear(_history); throw new ArithmeticException("Filter output exceeded finite PCM storage."); }
            output[i] = frame;
        }
    }
}
