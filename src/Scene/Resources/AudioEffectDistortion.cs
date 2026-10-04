namespace Electron2D;

/// <summary>Applies a selectable nonlinear curve to filtered stereo bus audio.</summary>
/// <remarks>Each instance owns independent left/right low-pass histories. The complementary high-frequency
/// signal bypasses the curve. Live resource edits take effect at the next block without resetting history.</remarks>
public sealed class AudioEffectDistortion : AudioEffect
{
    /// <summary>Selects the nonlinear sample transformation.</summary>
    public enum Mode
    {
        /// <summary>Power curve with a hard ceiling at unit amplitude.</summary>
        Clip = 0,
        /// <summary>Arctangent saturation.</summary>
        ATan = 1,
        /// <summary>Amplitude quantization from high to low resolution.</summary>
        LoFi = 2,
        /// <summary>Asymmetric transistor-style saturation.</summary>
        Overdrive = 3,
        /// <summary>Absolute sigmoid waveshaping.</summary>
        WaveShape = 4
    }

    private readonly object _gate = new();
    private Mode _mode;
    private float _preGain, _postGain, _drive, _keepHFHZ = 16000;

    /// <summary>Creates the default clip curve with zero drive and unity gains.</summary>
    public AudioEffectDistortion() { }

    /// <summary>Gets or sets the distortion mode.</summary>
    /// <value>Clip by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Mode DistortionMode { get { lock (_gate) { ThrowIfDisposed(); return _mode; } } set { if (value is < Mode.Clip or > Mode.WaveShape) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_mode == value) return; _mode = value; } EmitChanged(); } }
    /// <summary>Gets or sets the pre-curve decibel gain.</summary>
    /// <value>Zero initially; negative infinity silences the low-frequency input.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain cannot convert to finite amplitude.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float PreGain { get { lock (_gate) { ThrowIfDisposed(); return _preGain; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_preGain == value) return; _preGain = value; } EmitChanged(); } }
    /// <summary>Gets or sets the post-curve decibel gain.</summary>
    /// <value>Zero initially; negative infinity silences the curved component.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain cannot convert to finite amplitude.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float PostGain { get { lock (_gate) { ThrowIfDisposed(); return _postGain; } } set { ValidDB(value); lock (_gate) { ThrowIfDisposed(); if (_postGain == value) return; _postGain = value; } EmitChanged(); } }
    /// <summary>Gets or sets the positive cutoff separating curved and preserved frequencies.</summary>
    /// <value>16000 hertz initially; evaluated at the actual output mix rate.</value>
    /// <exception cref="ArgumentOutOfRangeException">The cutoff is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float KeepHFHZ { get { lock (_gate) { ThrowIfDisposed(); return _keepHFHZ; } } set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_keepHFHZ == value) return; _keepHFHZ = value; } EmitChanged(); } }
    /// <summary>Gets or sets curve intensity from zero through one.</summary>
    /// <value>Zero initially; even zero drive can alter samples.</value>
    /// <exception cref="ArgumentOutOfRangeException">The intensity is outside zero through one or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Drive { get { lock (_gate) { ThrowIfDisposed(); return _drive; } } set { if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_drive == value) return; _drive = value; } EmitChanged(); } }

    private static void ValidDB(float value) { if (float.IsNaN(value) || value == float.PositiveInfinity || !float.IsFinite(Mathf.DBToLinear(value))) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal readonly record struct Settings(Mode Mode, float PreGain, float PostGain, float KeepHFHZ, float Drive);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_mode, _preGain, _postGain, _keepHFHZ, _drive); } }
    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectDistortionInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectDistortion, Mode>(nameof(DistortionMode), e => e.DistortionMode, (e, v) => e.DistortionMode = v, _ => Mode.Clip, stored: true),
        new PropertyDescriptor<AudioEffectDistortion, float>(nameof(PreGain), e => e.PreGain, (e, v) => e.PreGain = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectDistortion, float>(nameof(PostGain), e => e.PostGain, (e, v) => e.PostGain = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectDistortion, float>(nameof(KeepHFHZ), e => e.KeepHFHZ, (e, v) => e.KeepHFHZ = v, _ => 16000, stored: true),
        new PropertyDescriptor<AudioEffectDistortion, float>(nameof(Drive), e => e.Drive, (e, v) => e.Drive = v, _ => 0, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectDistortion();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = Snapshot(); var copy = (AudioEffectDistortion)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._mode = state.Mode; copy._preGain = state.PreGain; copy._postGain = state.PostGain; copy._keepHFHZ = state.KeepHFHZ; copy._drive = state.Drive; }
    }
}

internal sealed class AudioEffectDistortionInstance : AudioEffectInstance
{
    private readonly AudioEffectDistortion _source;
    private readonly float _rate;
    private float _left, _right;

    internal AudioEffectDistortionInstance(AudioEffectDistortion source)
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
        var coefficient = (float)Math.Exp(-Math.Tau * settings.KeepHFHZ / _rate);
        var inverse = 1 - coefficient;
        var pre = Mathf.DBToLinear(settings.PreGain);
        var post = Mathf.DBToLinear(settings.PostGain);
        var drive = settings.Drive;
        var atanMultiplier = Math.Pow(10, drive * drive * 3.0) - 1 + .001;
        var atanDivisor = 1 / (Math.Atan(atanMultiplier) * (1 + drive * 8));
        var loFiMultiplier = Math.Pow(2, 2 + (1 - drive) * 14);
        for (var i = 0; i < source.Length; i++)
        {
            var input = source[i];
            var left = Sample(input.X, ref _left, coefficient, inverse, pre, post, drive, atanMultiplier, atanDivisor, loFiMultiplier, settings.Mode);
            var right = Sample(input.Y, ref _right, coefficient, inverse, pre, post, drive, atanMultiplier, atanDivisor, loFiMultiplier, settings.Mode);
            if (!float.IsFinite(left) || !float.IsFinite(right) || !float.IsFinite(_left) || !float.IsFinite(_right))
            {
                _left = _right = 0;
                throw new ArithmeticException("Distortion output exceeded finite PCM storage.");
            }
            destination[i] = new(left, right);
        }
    }

    private static float Sample(float input, ref float history, float coefficient, float inverse, float pre, float post,
        float drive, double atanMultiplier, double atanDivisor, double loFiMultiplier, AudioEffectDistortion.Mode mode)
    {
        var low = input * inverse + coefficient * history;
        if ((BitConverter.SingleToUInt32Bits(low) & 0x7f800000u) < 0x08000000u) low = 0;
        history = low;
        var high = input - low;
        var value = (double)(low * pre);
        value = mode switch
        {
            AudioEffectDistortion.Mode.Clip => Math.Clamp(Math.Pow(Math.Abs(value), 1.0001 - drive) * Math.CopySign(1, value), -1, 1),
            AudioEffectDistortion.Mode.ATan => Math.Atan(value * atanMultiplier) * atanDivisor,
            AudioEffectDistortion.Mode.LoFi => Math.Floor(value * loFiMultiplier + .5) / loFiMultiplier,
            AudioEffectDistortion.Mode.Overdrive => Overdrive(value),
            AudioEffectDistortion.Mode.WaveShape => WaveShape(value, drive),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        return (float)(value * post + high);
    }

    private static double WaveShape(double value, float drive)
    {
        var k = (float)(2 * drive / (1.00001 - drive));
        return (1 + k) * value / (1 + k * Math.Abs(value));
    }

    private static double Overdrive(double value)
    {
        var x = value * .686306;
        var z = 1 + Math.Exp(-.75 * Math.Sqrt(Math.Abs(x)));
        if (x >= 0)
            return (1 - Math.Exp(-x * (z + 1))) / (1 + Math.Exp(-2 * x));
        var positive = Math.Exp(2 * x);
        return (positive - Math.Exp(x * (1 - z))) / (positive + 1);
    }
}
