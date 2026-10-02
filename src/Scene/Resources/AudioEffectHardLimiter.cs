namespace Electron2D;

/// <summary>Delays stereo bus audio briefly to reduce peaks before they exceed a configured ceiling.</summary>
/// <remarks>Each instance owns an independent two-millisecond lookahead line and gain history. Live settings
/// take effect on the next processed block. A bus borrows this resource and owns its per-pair instances.</remarks>
public sealed class AudioEffectHardLimiter : AudioEffect
{
    private readonly object _gate = new();
    private float _ceilingDB = -.3f, _preGainDB, _release = .1f;

    /// <summary>Creates a limiter with a -0.3 dB ceiling, unity pre-gain and 0.1-second release.</summary>
    public AudioEffectHardLimiter() { }

    /// <summary>Gets or sets the output ceiling in decibels.</summary>
    /// <value>-0.3 initially; the ordinary authoring range is -24 through 0 dB.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value cannot be converted to finite linear amplitude.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float CeilingDB { get { lock (_gate) { ThrowIfDisposed(); return _ceilingDB; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_ceilingDB == value) return; _ceilingDB = value; } EmitChanged(); } }

    /// <summary>Gets or sets pre-limiter gain in decibels.</summary>
    /// <value>Zero initially; the ordinary authoring range is -24 through 24 dB.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value cannot be converted to finite linear amplitude.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float PreGainDB { get { lock (_gate) { ThrowIfDisposed(); return _preGainDB; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_preGainDB == value) return; _preGainDB = value; } EmitChanged(); } }

    /// <summary>Gets or sets the gain-reduction release time in seconds.</summary>
    /// <value>0.1 initially; positive finite values, ordinarily 0.01 through 3 seconds.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Release { get { lock (_gate) { ThrowIfDisposed(); return _release; } } set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_release == value) return; _release = value; } EmitChanged(); } }

    private static void ValidDB(float value) { if (float.IsNaN(value) || value == float.PositiveInfinity || !float.IsFinite(Mathf.DBToLinear(value))) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal readonly record struct Settings(float CeilingDB, float PreGainDB, float Release);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_ceilingDB, _preGainDB, _release); } }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectHardLimiterInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectHardLimiter, float>(nameof(CeilingDB), e => e.CeilingDB, (e, v) => e.CeilingDB = v, _ => -.3f, stored: true),
        new PropertyDescriptor<AudioEffectHardLimiter, float>(nameof(PreGainDB), e => e.PreGainDB, (e, v) => e.PreGainDB = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectHardLimiter, float>(nameof(Release), e => e.Release, (e, v) => e.Release = v, _ => .1f, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectHardLimiter();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var settings = Snapshot(); var copy = (AudioEffectHardLimiter)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._ceilingDB = settings.CeilingDB; copy._preGainDB = settings.PreGainDB; copy._release = settings.Release; }
    }
}

internal sealed class AudioEffectHardLimiterInstance : AudioEffectInstance
{
    private const float Attack = .002f, Sustain = .02f;
    private readonly AudioEffectHardLimiter _source;
    private readonly Vector2[] _delay;
    private readonly float[] _buckets;
    private readonly int _bucketSize, _bucketSpan;
    private readonly float _inverseRate;
    private int _sampleCursor, _bucketCursor;
    private float _gain = 1, _gainTarget = 1, _releaseFactor, _attackFactor;

    internal AudioEffectHardLimiterInstance(AudioEffectHardLimiter source)
    {
        _source = source;
        var rate = AudioServer.Instance.GetMixRate();
        if (!float.IsFinite(rate) || rate <= 0 || rate > 1_000_000) throw new ArgumentOutOfRangeException(nameof(source), "Output mix rate exceeds prepared limiter storage.");
        _inverseRate = 1 / rate;
        _delay = new Vector2[checked((int)Math.Ceiling(rate * Attack) + 1)];
        _bucketSpan = checked((int)Math.Ceiling(rate * (Attack + Sustain) + 1));
        _bucketSize = Math.Max(1, (int)(rate * Attack));
        _buckets = new float[(_bucketSpan + _bucketSize - 1) / _bucketSize];
        Array.Fill(_buckets, 1);
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var ceiling = Mathf.DBToLinear(settings.CeilingDB);
        var preGain = Mathf.DBToLinear(settings.PreGainDB);
        for (var i = 0; i < source.Length; i++)
        {
            var input = source[i] * preGain;
            if (!input.IsFinite()) { Reset(); throw new ArithmeticException("Limiter pre-gain produced nonfinite PCM."); }
            var largest = MathF.Max(MathF.Abs(input.X), MathF.Abs(input.Y));
            _releaseFactor = MathF.Min(MathF.Max(0, _releaseFactor - _inverseRate), settings.Release);
            _gain = _releaseFactor > 0 ? Lerp(_gainTarget, 1, 1 - _releaseFactor / settings.Release) : 1;
            if (largest * _gain > ceiling)
            {
                _gainTarget = ceiling / largest;
                _releaseFactor = settings.Release;
                _attackFactor = Attack;
            }
            _attackFactor = MathF.Max(0, _attackFactor - _inverseRate);
            if (_attackFactor > 0) _gain = Lerp(_gainTarget, _gain, 1 - _attackFactor / Attack);

            var bucket = _bucketCursor / _bucketSize;
            if (_bucketCursor % _bucketSize == 0) _buckets[bucket] = 1;
            _buckets[bucket] = MathF.Min(_buckets[bucket], _gain);
            _bucketCursor = (_bucketCursor + 1) % _bucketSpan;
            foreach (var candidate in _buckets) _gain = MathF.Min(_gain, candidate);

            var delayed = _delay[_sampleCursor]; _delay[_sampleCursor] = input;
            _sampleCursor = (_sampleCursor + 1) % _delay.Length;
            var output = delayed * _gain;
            if (!output.IsFinite()) { Reset(); throw new ArithmeticException("Limiter output exceeded finite PCM storage."); }
            destination[i] = new(MathF.CopySign(MathF.Min(MathF.Abs(output.X), ceiling), output.X),
                MathF.CopySign(MathF.Min(MathF.Abs(output.Y), ceiling), output.Y));
        }
    }

    private static float Lerp(float from, float to, float weight) => from + (to - from) * weight;
    private void Reset()
    {
        Array.Clear(_delay); Array.Fill(_buckets, 1);
        _sampleCursor = _bucketCursor = 0;
        _gain = _gainTarget = 1; _releaseFactor = _attackFactor = 0;
    }
}
