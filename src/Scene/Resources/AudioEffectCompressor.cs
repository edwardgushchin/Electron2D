namespace Electron2D;

/// <summary>Compresses stereo bus audio using a linked peak detector and optional named sidechain.</summary>
/// <remarks>Each instance owns an independent envelope at its prepared output rate. Live controls apply
/// at the next block. Buses borrow this resource and own their instances; resource copies retain settings only.</remarks>
public sealed class AudioEffectCompressor : AudioEffect
{
    private readonly object _gate = new();
    private float _threshold, _ratio = 4, _gain, _attackUS = 20, _releaseMS = 250, _mix = 1;
    private string _sidechain = string.Empty;
    /// <summary>Creates a unity-threshold compressor with ratio four, 20 microseconds attack and 250 milliseconds release.</summary>
    public AudioEffectCompressor() { }
    /// <summary>Gets or sets the detector threshold in decibels.</summary>
    /// <value>Zero initially; ordinary authoring range -60 through zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Threshold { get { lock (_gate) { ThrowIfDisposed(); return _threshold; } } set => Set(ref _threshold, value); }
    /// <summary>Gets or sets the nonzero compression ratio.</summary>
    /// <value>Four initially; ordinary authoring range one through 48. One disables reduction; raw positive
    /// values below one expand, and raw negative values retain the transfer equation's stronger downward slope.</value>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is zero or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Ratio { get { lock (_gate) { ThrowIfDisposed(); return _ratio; } } set { if (value == 0) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _ratio, value); } }
    /// <summary>Gets or sets the wet signal's decibel makeup gain.</summary>
    /// <value>Zero initially; ordinary authoring range -20 through 20. Negative infinity silences the wet signal.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value cannot convert to finite linear amplitude.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Gain
    {
        get { lock (_gate) { ThrowIfDisposed(); return _gain; } }
        set
        {
            if (float.IsNaN(value) || !float.IsFinite(Mathf.DBToLinear(value))) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_gain == value) return; _gain = value; }
            EmitChanged();
        }
    }
    /// <summary>Gets or sets the nonnegative envelope attack in microseconds.</summary>
    /// <value>20 initially; ordinary authoring range 20 through 2000. Zero reacts immediately.</value>
    /// <exception cref="ArgumentOutOfRangeException">The time is negative or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float AttackUS { get { lock (_gate) { ThrowIfDisposed(); return _attackUS; } } set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _attackUS, value); } }
    /// <summary>Gets or sets the nonnegative envelope release in milliseconds.</summary>
    /// <value>250 initially; ordinary authoring range 20 through 2000. Zero releases immediately.</value>
    /// <exception cref="ArgumentOutOfRangeException">The time is negative or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float ReleaseMS { get { lock (_gate) { ThrowIfDisposed(); return _releaseMS; } } set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _releaseMS, value); } }
    /// <summary>Gets or sets wet/dry balance.</summary>
    /// <value>One initially; ordinary authoring range zero through one. Raw finite values extrapolate;
    /// zero supplies the exact original signal.</value>
    /// <exception cref="ArgumentOutOfRangeException">The balance is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Mix { get { lock (_gate) { ThrowIfDisposed(); return _mix; } } set => Set(ref _mix, value); }
    /// <summary>Gets or sets the exact named detector bus.</summary>
    /// <value>Empty initially, using this effect's input. Missing names resolve to Master.</value>
    /// <remarks>Bus-owned instances read the matching stereo pair's current quantum buffer: earlier buses
    /// include their effects/gain; later buses contain direct sources and already received sends. Standalone
    /// instances always detect their input. Assignment serializes with native mixing and rejects audio callback reentrancy.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">An audio callback attempts assignment.</exception>
    public string Sidechain
    {
        get { lock (_gate) { ThrowIfDisposed(); return _sidechain; } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); var server = AudioServer.Instance; var changed = false; server.Lock();
            try { lock (_gate) { ThrowIfDisposed(); if (_sidechain != value) { _sidechain = value; changed = true; } } }
            finally { server.Unlock(); }
            if (changed) EmitChanged();
        }
    }
    private void Set(ref float field, float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        lock (_gate) { ThrowIfDisposed(); if (field == value) return; field = value; }
        EmitChanged();
    }
    internal readonly record struct Settings(float Threshold, float Ratio, float Gain, float AttackUS, float ReleaseMS, float Mix, string Sidechain);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_threshold, _ratio, _gain, _attackUS, _releaseMS, _mix, _sidechain); } }
    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectCompressorInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectCompressor, float>(nameof(Threshold), e => e.Threshold, (e, v) => e.Threshold = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectCompressor, float>(nameof(Ratio), e => e.Ratio, (e, v) => e.Ratio = v, _ => 4, stored: true),
        new PropertyDescriptor<AudioEffectCompressor, float>(nameof(Gain), e => e.Gain, (e, v) => e.Gain = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectCompressor, float>(nameof(AttackUS), e => e.AttackUS, (e, v) => e.AttackUS = v, _ => 20, stored: true),
        new PropertyDescriptor<AudioEffectCompressor, float>(nameof(ReleaseMS), e => e.ReleaseMS, (e, v) => e.ReleaseMS = v, _ => 250, stored: true),
        new PropertyDescriptor<AudioEffectCompressor, float>(nameof(Mix), e => e.Mix, (e, v) => e.Mix = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectCompressor, string>(nameof(Sidechain), e => e.Sidechain, (e, v) => e.Sidechain = v, _ => string.Empty, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectCompressor();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = Snapshot(); var copy = (AudioEffectCompressor)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._threshold = state.Threshold; copy._ratio = state.Ratio; copy._gain = state.Gain; copy._attackUS = state.AttackUS; copy._releaseMS = state.ReleaseMS; copy._mix = state.Mix; copy._sidechain = state.Sidechain; }
    }
}

internal sealed class AudioEffectCompressorInstance : AudioEffectInstance
{
    private readonly AudioEffectCompressor _source;
    private readonly float _rate;
    private float _runDB;
    private int _pair = -1;
    internal AudioEffectCompressorInstance(AudioEffectCompressor source) { _source = source; _rate = AudioServer.Instance.GetMixRate(); }
    internal override void OnAttached(int pair) => _pair = pair;
    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> input, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var detector = _pair >= 0 && settings.Sidechain.Length != 0 ? AudioServer.Instance.ReadSidechain(settings.Sidechain, _pair, input.Length) : input;
        ProcessBlock(input, destination, detector, settings);
    }
    internal void ProcessBlock(ReadOnlySpan<Vector2> input, Span<Vector2> destination, ReadOnlySpan<Vector2> detector, AudioEffectCompressor.Settings settings)
    {
        var threshold = Mathf.DBToLinear(settings.Threshold); var makeup = Mathf.DBToLinear(settings.Gain);
        var attack = settings.AttackUS == 0 ? 0 : MathF.Exp(-1 / ((float)(settings.AttackUS / 1000000d) * _rate));
        var release = settings.ReleaseMS == 0 ? 0 : MathF.Exp(-1 / ((float)(settings.ReleaseMS / 1000d) * _rate));
        try
        {
            for (var i = 0; i < input.Length; i++)
            {
                var peak = Math.Max(Math.Abs(detector[i].X), Math.Abs(detector[i].Y));
                if (!float.IsFinite(peak)) throw new ArithmeticException("Compressor detector PCM must be finite.");
                var relative = peak / threshold;
                var over = peak == 0 ? 0 : float.IsFinite(relative) ? Math.Max(0, 2.08136898f * Mathf.LinearToDB(relative))
                    : (float)Math.Clamp(2.08136898f * (Mathf.LinearToDB((double)peak) - settings.Threshold), 0, float.MaxValue);
                _runDB = over + (over > _runDB ? attack : release) * (_runDB - over);
                var reduction = Mathf.DBToLinear(-_runDB * (settings.Ratio - 1) / settings.Ratio);
                var frame = input[i];
                if (settings.Mix == 0) destination[i] = frame;
                else if (makeup == 0) destination[i] = frame * (1 - settings.Mix);
                else destination[i] = new(Sample(frame.X, reduction, makeup, settings.Mix), Sample(frame.Y, reduction, makeup, settings.Mix));
                if (!destination[i].IsFinite()) throw new ArithmeticException("Compressor output exceeds finite PCM.");
            }
        }
        catch { _runDB = 0; destination.Clear(); throw; }
    }
    private static float Sample(float input, float reduction, float makeup, float mix) => input == 0 ? 0 : input * reduction * makeup * mix + input * (1 - mix);
}
