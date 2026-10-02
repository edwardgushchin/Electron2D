namespace Electron2D;

/// <summary>Repeats stereo bus audio through two delayed taps and optional filtered feedback.</summary>
/// <remarks>Each effect instance owns independent prepared delay histories. Live resource edits affect the next
/// processed block without clearing those histories. A bus borrows this resource and owns its instances.</remarks>
public sealed class AudioEffectDelay : AudioEffect
{
    private readonly object _gate = new();
    private float _dry = 1;
    private bool _tap1Active = true, _tap2Active = true, _feedbackActive;
    private float _tap1DelayMS = 250, _tap1LevelDB = -6, _tap1Pan = .2f;
    private float _tap2DelayMS = 500, _tap2LevelDB = -12, _tap2Pan = -.4f;
    private float _feedbackDelayMS = 340, _feedbackLevelDB = -6, _feedbackLowpass = 16000;

    /// <summary>Creates default dry, two-tap configuration with feedback disabled.</summary>
    public AudioEffectDelay() { }

    /// <summary>Gets or sets the finite dry-signal amplitude multiplier.</summary>
    /// <value>One initially; the usual authored range is zero through one.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Dry { get { lock (_gate) { ThrowIfDisposed(); return _dry; } } set { ValidFinite(value); lock (_gate) { ThrowIfDisposed(); if (_dry == value) return; _dry = value; } EmitChanged(); } }
    /// <summary>Gets or sets whether the first delayed tap contributes output.</summary>
    /// <value>True initially.</value>
    public bool Tap1Active { get { lock (_gate) { ThrowIfDisposed(); return _tap1Active; } } set { lock (_gate) { ThrowIfDisposed(); if (_tap1Active == value) return; _tap1Active = value; } EmitChanged(); } }
    /// <summary>Gets or sets the first tap delay in milliseconds.</summary>
    /// <value>250 initially; valid from zero through 1500.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported delay range.</exception>
    public float Tap1DelayMS { get { lock (_gate) { ThrowIfDisposed(); return _tap1DelayMS; } } set { ValidDelay(value); lock (_gate) { ThrowIfDisposed(); if (_tap1DelayMS == value) return; _tap1DelayMS = value; } EmitChanged(); } }
    /// <summary>Gets or sets the first tap gain in decibels.</summary>
    /// <value>Minus six initially; negative infinity silences it.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain cannot be converted to finite linear amplitude.</exception>
    public float Tap1LevelDB { get { lock (_gate) { ThrowIfDisposed(); return _tap1LevelDB; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_tap1LevelDB == value) return; _tap1LevelDB = value; } EmitChanged(); } }
    /// <summary>Gets or sets the first tap's signed stereo pan.</summary>
    /// <value>0.2 initially; effective channel coefficients clamp to zero through one.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Tap1Pan { get { lock (_gate) { ThrowIfDisposed(); return _tap1Pan; } } set { ValidFinite(value); lock (_gate) { ThrowIfDisposed(); if (_tap1Pan == value) return; _tap1Pan = value; } EmitChanged(); } }
    /// <summary>Gets or sets whether the second delayed tap contributes output.</summary>
    /// <value>True initially.</value>
    public bool Tap2Active { get { lock (_gate) { ThrowIfDisposed(); return _tap2Active; } } set { lock (_gate) { ThrowIfDisposed(); if (_tap2Active == value) return; _tap2Active = value; } EmitChanged(); } }
    /// <summary>Gets or sets the second tap delay in milliseconds.</summary>
    /// <value>500 initially; valid from zero through 1500.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported delay range.</exception>
    public float Tap2DelayMS { get { lock (_gate) { ThrowIfDisposed(); return _tap2DelayMS; } } set { ValidDelay(value); lock (_gate) { ThrowIfDisposed(); if (_tap2DelayMS == value) return; _tap2DelayMS = value; } EmitChanged(); } }
    /// <summary>Gets or sets the second tap gain in decibels.</summary>
    /// <value>Minus twelve initially; negative infinity silences it.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain cannot be converted to finite linear amplitude.</exception>
    public float Tap2LevelDB { get { lock (_gate) { ThrowIfDisposed(); return _tap2LevelDB; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_tap2LevelDB == value) return; _tap2LevelDB = value; } EmitChanged(); } }
    /// <summary>Gets or sets the second tap's signed stereo pan.</summary>
    /// <value>Minus 0.4 initially; effective channel coefficients clamp to zero through one.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Tap2Pan { get { lock (_gate) { ThrowIfDisposed(); return _tap2Pan; } } set { ValidFinite(value); lock (_gate) { ThrowIfDisposed(); if (_tap2Pan == value) return; _tap2Pan = value; } EmitChanged(); } }
    /// <summary>Gets or sets whether filtered output feeds the echo line.</summary>
    /// <value>False initially; existing feedback history is retained when switched off.</value>
    public bool FeedbackActive { get { lock (_gate) { ThrowIfDisposed(); return _feedbackActive; } } set { lock (_gate) { ThrowIfDisposed(); if (_feedbackActive == value) return; _feedbackActive = value; } EmitChanged(); } }
    /// <summary>Gets or sets feedback repetition time in milliseconds.</summary>
    /// <value>340 initially; valid from zero through 1500. Zero reuses the previous feedback sample.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported delay range.</exception>
    public float FeedbackDelayMS { get { lock (_gate) { ThrowIfDisposed(); return _feedbackDelayMS; } } set { ValidDelay(value); lock (_gate) { ThrowIfDisposed(); if (_feedbackDelayMS == value) return; _feedbackDelayMS = value; } EmitChanged(); } }
    /// <summary>Gets or sets feedback gain in decibels.</summary>
    /// <value>Minus six initially; negative infinity silences feedback.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain cannot be converted to finite linear amplitude.</exception>
    public float FeedbackLevelDB { get { lock (_gate) { ThrowIfDisposed(); return _feedbackLevelDB; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_feedbackLevelDB == value) return; _feedbackLevelDB = value; } EmitChanged(); } }
    /// <summary>Gets or sets the positive low-pass threshold of feedback in hertz.</summary>
    /// <value>16000 initially; it is evaluated at the current output rate.</value>
    /// <exception cref="ArgumentOutOfRangeException">The threshold is nonpositive or nonfinite.</exception>
    public float FeedbackLowpass { get { lock (_gate) { ThrowIfDisposed(); return _feedbackLowpass; } } set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_feedbackLowpass == value) return; _feedbackLowpass = value; } EmitChanged(); } }

    private static void ValidFinite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void ValidDelay(float value) { if (!float.IsFinite(value) || value < 0 || value > 1500) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void ValidDB(float value) { if (float.IsNaN(value) || value == float.PositiveInfinity || !float.IsFinite(Mathf.DBToLinear(value))) throw new ArgumentOutOfRangeException(nameof(value)); }

    internal Settings Snapshot()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return new(_dry, _tap1Active, _tap1DelayMS, _tap1LevelDB, _tap1Pan, _tap2Active, _tap2DelayMS,
                _tap2LevelDB, _tap2Pan, _feedbackActive, _feedbackDelayMS, _feedbackLevelDB, _feedbackLowpass);
        }
    }

    internal readonly record struct Settings(float Dry, bool Tap1Active, float Tap1DelayMS, float Tap1LevelDB, float Tap1Pan,
        bool Tap2Active, float Tap2DelayMS, float Tap2LevelDB, float Tap2Pan,
        bool FeedbackActive, float FeedbackDelayMS, float FeedbackLevelDB, float FeedbackLowpass);

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectDelayInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Dry), e => e.Dry, (e, v) => e.Dry = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectDelay, bool>(nameof(Tap1Active), e => e.Tap1Active, (e, v) => e.Tap1Active = v, _ => true, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Tap1DelayMS), e => e.Tap1DelayMS, (e, v) => e.Tap1DelayMS = v, _ => 250, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Tap1LevelDB), e => e.Tap1LevelDB, (e, v) => e.Tap1LevelDB = v, _ => -6, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Tap1Pan), e => e.Tap1Pan, (e, v) => e.Tap1Pan = v, _ => .2f, stored: true),
        new PropertyDescriptor<AudioEffectDelay, bool>(nameof(Tap2Active), e => e.Tap2Active, (e, v) => e.Tap2Active = v, _ => true, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Tap2DelayMS), e => e.Tap2DelayMS, (e, v) => e.Tap2DelayMS = v, _ => 500, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Tap2LevelDB), e => e.Tap2LevelDB, (e, v) => e.Tap2LevelDB = v, _ => -12, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(Tap2Pan), e => e.Tap2Pan, (e, v) => e.Tap2Pan = v, _ => -.4f, stored: true),
        new PropertyDescriptor<AudioEffectDelay, bool>(nameof(FeedbackActive), e => e.FeedbackActive, (e, v) => e.FeedbackActive = v, _ => false, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(FeedbackDelayMS), e => e.FeedbackDelayMS, (e, v) => e.FeedbackDelayMS = v, _ => 340, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(FeedbackLevelDB), e => e.FeedbackLevelDB, (e, v) => e.FeedbackLevelDB = v, _ => -6, stored: true),
        new PropertyDescriptor<AudioEffectDelay, float>(nameof(FeedbackLowpass), e => e.FeedbackLowpass, (e, v) => e.FeedbackLowpass = v, _ => 16000, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectDelay();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = Snapshot(); var copy = (AudioEffectDelay)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed();
            copy._dry = state.Dry; copy._tap1Active = state.Tap1Active; copy._tap1DelayMS = state.Tap1DelayMS;
            copy._tap1LevelDB = state.Tap1LevelDB; copy._tap1Pan = state.Tap1Pan; copy._tap2Active = state.Tap2Active;
            copy._tap2DelayMS = state.Tap2DelayMS; copy._tap2LevelDB = state.Tap2LevelDB; copy._tap2Pan = state.Tap2Pan;
            copy._feedbackActive = state.FeedbackActive; copy._feedbackDelayMS = state.FeedbackDelayMS;
            copy._feedbackLevelDB = state.FeedbackLevelDB; copy._feedbackLowpass = state.FeedbackLowpass;
        }
    }
}

internal sealed class AudioEffectDelayInstance : AudioEffectInstance
{
    private readonly AudioEffectDelay _source;
    private readonly Vector2[] _ring, _feedback;
    private readonly int _mask;
    private int _ringPosition, _feedbackPosition;
    private Vector2 _history;
    private readonly float _rate;

    internal AudioEffectDelayInstance(AudioEffectDelay source)
    {
        _source = source;
        _rate = AudioServer.Instance.GetMixRate();
        var frames = 3.1 * _rate;
        if (!double.IsFinite(frames) || frames <= 0 || frames >= 1 << 22)
            throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared delay storage.");
        var required = (int)Math.Ceiling(frames);
        var size = 1;
        while (size <= required)
        {
            if (size >= 1 << 22) throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared delay storage.");
            size <<= 1;
        }
        _ring = new Vector2[size]; _feedback = new Vector2[size]; _mask = size - 1;
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var tap1Delay = (int)(settings.Tap1DelayMS / 1000f * _rate);
        var tap2Delay = (int)(settings.Tap2DelayMS / 1000f * _rate);
        var feedbackDelay = (int)(settings.FeedbackDelayMS / 1000f * _rate);
        var tap1Gain = settings.Tap1Active ? Mathf.DBToLinear(settings.Tap1LevelDB) : 0;
        var tap2Gain = settings.Tap2Active ? Mathf.DBToLinear(settings.Tap2LevelDB) : 0;
        var feedbackGain = settings.FeedbackActive ? Mathf.DBToLinear(settings.FeedbackLevelDB) : 0;
        var tap1Left = tap1Gain * Math.Clamp(1 - settings.Tap1Pan, 0, 1);
        var tap1Right = tap1Gain * Math.Clamp(1 + settings.Tap1Pan, 0, 1);
        var tap2Left = tap2Gain * Math.Clamp(1 - settings.Tap2Pan, 0, 1);
        var tap2Right = tap2Gain * Math.Clamp(1 + settings.Tap2Pan, 0, 1);
        var lowpass = (float)Math.Exp(-Math.Tau * settings.FeedbackLowpass / _rate);
        var lowpassInverse = 1 - lowpass;
        for (var i = 0; i < source.Length; i++)
        {
            var input = source[i];
            _ring[_ringPosition & _mask] = input;
            var tap1 = _ring[unchecked((_ringPosition - tap1Delay) & _mask)];
            var tap2 = _ring[unchecked((_ringPosition - tap2Delay) & _mask)];
            var delayed = _feedback[_feedbackPosition];
            var output = new Vector2(input.X * settings.Dry + tap1.X * tap1Left + tap2.X * tap2Left + delayed.X,
                input.Y * settings.Dry + tap1.Y * tap1Right + tap2.Y * tap2Right + delayed.Y);
            var feedback = output * (feedbackGain * lowpassInverse) + _history * lowpass;
            if (!output.IsFinite() || !feedback.IsFinite())
            {
                Array.Clear(_ring); Array.Clear(_feedback); _ringPosition = _feedbackPosition = 0; _history = Vector2.Zero;
                throw new ArithmeticException("Delay output exceeded finite PCM storage.");
            }
            if (Math.Abs(feedback.X) < 1e-20f) feedback.X = 0;
            if (Math.Abs(feedback.Y) < 1e-20f) feedback.Y = 0;
            _history = feedback;
            _feedback[_feedbackPosition] = feedback;
            destination[i] = output;
            _ringPosition = unchecked(_ringPosition + 1);
            if (++_feedbackPosition >= feedbackDelay) _feedbackPosition = 0;
        }
    }
}
