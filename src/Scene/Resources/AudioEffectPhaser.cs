namespace Electron2D;

/// <summary>Mixes bus audio with six swept all-pass stages per stereo channel.</summary>
/// <remarks>Each instance owns separate left/right filter and feedback histories. Live controls are
/// sampled once per block; a bus borrows this resource and owns its processing instances.</remarks>
public sealed class AudioEffectPhaser : AudioEffect
{
    private readonly object _gate = new();
    private float _rangeMinHZ = 440, _rangeMaxHZ = 1600, _rateHZ = .5f, _feedback = .7f, _depth = 1;

    /// <summary>Creates the default six-stage phaser configuration.</summary>
    public AudioEffectPhaser() { }

    /// <summary>Gets or sets the lower oscillator frequency endpoint in hertz.</summary>
    /// <value>440 initially; valid from ten through ten thousand.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float RangeMinHZ { get { lock (_gate) { ThrowIfDisposed(); return _rangeMinHZ; } } set { Range(value, 10, 10000); lock (_gate) { ThrowIfDisposed(); if (_rangeMinHZ == value) return; _rangeMinHZ = value; } EmitChanged(); } }
    /// <summary>Gets or sets the upper oscillator frequency endpoint in hertz.</summary>
    /// <value>1600 initially; valid from ten through ten thousand. Endpoints may be reversed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float RangeMaxHZ { get { lock (_gate) { ThrowIfDisposed(); return _rangeMaxHZ; } } set { Range(value, 10, 10000); lock (_gate) { ThrowIfDisposed(); if (_rangeMaxHZ == value) return; _rangeMaxHZ = value; } EmitChanged(); } }
    /// <summary>Gets or sets the sweep rate in hertz.</summary>
    /// <value>0.5 initially; valid from 0.01 through twenty.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float RateHZ { get { lock (_gate) { ThrowIfDisposed(); return _rateHZ; } } set { Range(value, .01f, 20); lock (_gate) { ThrowIfDisposed(); if (_rateHZ == value) return; _rateHZ = value; } EmitChanged(); } }
    /// <summary>Gets or sets the filtered signal fed back into the six-stage chain.</summary>
    /// <value>0.7 initially; valid from 0.1 through 0.9.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Feedback { get { lock (_gate) { ThrowIfDisposed(); return _feedback; } } set { Range(value, .1f, .9f); lock (_gate) { ThrowIfDisposed(); if (_feedback == value) return; _feedback = value; } EmitChanged(); } }
    /// <summary>Gets or sets the filtered signal mixed back into the source.</summary>
    /// <value>One initially; valid from 0.1 through four.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Depth { get { lock (_gate) { ThrowIfDisposed(); return _depth; } } set { Range(value, .1f, 4); lock (_gate) { ThrowIfDisposed(); if (_depth == value) return; _depth = value; } EmitChanged(); } }

    private static void Range(float value, float minimum, float maximum)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
    }
    internal readonly record struct Settings(float RangeMinHZ, float RangeMaxHZ, float RateHZ, float Feedback, float Depth);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_rangeMinHZ, _rangeMaxHZ, _rateHZ, _feedback, _depth); } }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectPhaserInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectPhaser, float>(nameof(RangeMinHZ), e => e.RangeMinHZ, (e, v) => e.RangeMinHZ = v, _ => 440, stored: true),
        new PropertyDescriptor<AudioEffectPhaser, float>(nameof(RangeMaxHZ), e => e.RangeMaxHZ, (e, v) => e.RangeMaxHZ = v, _ => 1600, stored: true),
        new PropertyDescriptor<AudioEffectPhaser, float>(nameof(RateHZ), e => e.RateHZ, (e, v) => e.RateHZ = v, _ => .5f, stored: true),
        new PropertyDescriptor<AudioEffectPhaser, float>(nameof(Feedback), e => e.Feedback, (e, v) => e.Feedback = v, _ => .7f, stored: true),
        new PropertyDescriptor<AudioEffectPhaser, float>(nameof(Depth), e => e.Depth, (e, v) => e.Depth = v, _ => 1, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectPhaser();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var settings = Snapshot(); var copy = (AudioEffectPhaser)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed(); copy._rangeMinHZ = settings.RangeMinHZ; copy._rangeMaxHZ = settings.RangeMaxHZ;
            copy._rateHZ = settings.RateHZ; copy._feedback = settings.Feedback; copy._depth = settings.Depth;
        }
    }
}

internal sealed class AudioEffectPhaserInstance : AudioEffectInstance
{
    private readonly AudioEffectPhaser _source;
    private readonly float _rate;
    private readonly float[] _left = new float[6], _right = new float[6];
    private float _phase, _leftFeedback, _rightFeedback;

    internal AudioEffectPhaserInstance(AudioEffectPhaser source)
    {
        _source = source; _rate = AudioServer.GetMixRate();
        if (!float.IsFinite(_rate) || _rate <= 0) throw new ArgumentOutOfRangeException(nameof(source), "Output mix rate must be finite and positive.");
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;
    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var dmin = settings.RangeMinHZ / (_rate * .5f);
        var dmax = settings.RangeMaxHZ / (_rate * .5f);
        var increment = MathF.Tau * (settings.RateHZ / _rate);
        for (var i = 0; i < source.Length; i++)
        {
            _phase += increment;
            if (_phase >= MathF.Tau) _phase -= MathF.Tau;
            var d = dmin + (dmax - dmin) * ((MathF.Sin(_phase) + 1) * .5f);
            var coefficient = (1 - d) / (1 + d);
            var input = source[i];
            var left = Stage(input.X + _leftFeedback * settings.Feedback, coefficient, _left);
            var right = Stage(input.Y + _rightFeedback * settings.Feedback, coefficient, _right);
            _leftFeedback = left; _rightFeedback = right;
            var output = new Vector2(input.X + left * settings.Depth, input.Y + right * settings.Depth);
            if (!output.IsFinite() || !float.IsFinite(left) || !float.IsFinite(right))
            {
                Array.Clear(_left); Array.Clear(_right); _leftFeedback = _rightFeedback = _phase = 0;
                throw new ArithmeticException("Phaser output exceeded finite PCM storage.");
            }
            destination[i] = output;
        }
    }

    private static float Stage(float input, float coefficient, float[] history)
    {
        for (var i = 5; i >= 0; i--)
        {
            var output = input * -coefficient + history[i];
            history[i] = output * coefficient + input;
            if (!float.IsFinite(history[i])) return float.NaN;
            input = output;
        }
        return input;
    }
}
