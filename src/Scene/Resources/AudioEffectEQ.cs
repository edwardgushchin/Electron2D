namespace Electron2D;

/// <summary>Configures a fixed-band stereo graphic equalizer.</summary>
/// <remarks>The directly constructed base uses six bands. Every bus stereo pair receives independent prepared
/// filter histories. Indexed gain edits take effect at the next processed block without clearing those histories.</remarks>
public class AudioEffectEQ : AudioEffect
{
    private static readonly float[] SixBands = [32, 100, 320, 1000, 3200, 10000];
    private static readonly float[] TenBands = [31.25f, 62.5f, 125, 250, 500, 1000, 2000, 4000, 8000, 16000];
    private static readonly float[] TwentyOneBands = [22, 32, 44, 63, 90, 125, 175, 250, 350, 500, 700, 1000, 1400, 2000, 2800, 4000, 5600, 8000, 11000, 16000, 22000];
    private readonly object _gate = new();
    private readonly float[] _frequencies;
    private readonly float[] _gains;
    private readonly PropertyDescriptor[] _bandProperties;

    /// <summary>Creates a six-band equalizer with zero decibel gain on every band.</summary>
    public AudioEffectEQ() : this(6) { }

    internal AudioEffectEQ(int count)
    {
        _frequencies = count switch { 6 => SixBands, 10 => TenBands, 21 => TwentyOneBands, _ => throw new ArgumentOutOfRangeException(nameof(count)) };
        _gains = new float[_frequencies.Length];
        _bandProperties = new PropertyDescriptor[_frequencies.Length];
        for (var i = 0; i < _frequencies.Length; i++)
        {
            var index = i;
            _bandProperties[i] = new PropertyDescriptor<AudioEffectEQ, float>($"BandDB/{(int)_frequencies[i]}HZ",
                effect => effect.GetBandGainDB(index), (effect, value) => effect.SetBandGainDB(index, value), _ => 0, stored: true);
        }
    }

    /// <summary>Gets this preset's immutable number of bands.</summary>
    /// <returns>Six, ten or twenty-one.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int GetBandCount() { lock (_gate) { ThrowIfDisposed(); return _gains.Length; } }

    /// <summary>Gets the current decibel gain for a band.</summary>
    /// <param name="bandIndex">Zero-based band index.</param>
    /// <returns>Zero by default; negative infinity denotes silence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the preset.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetBandGainDB(int bandIndex)
    {
        lock (_gate) { ThrowIfDisposed(); CheckBand(bandIndex); return _gains[bandIndex]; }
    }

    /// <summary>Sets a band's decibel gain for the next processed block.</summary>
    /// <param name="bandIndex">Zero-based band index.</param>
    /// <param name="volumeDB">Finite gain with representable linear conversion, or negative infinity for silence.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index or gain is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetBandGainDB(int bandIndex, float volumeDB)
    {
        if (float.IsNaN(volumeDB) || volumeDB == float.PositiveInfinity || !float.IsFinite(Mathf.DBToLinear(volumeDB)))
            throw new ArgumentOutOfRangeException(nameof(volumeDB));
        lock (_gate)
        {
            ThrowIfDisposed(); CheckBand(bandIndex);
            if (_gains[bandIndex] == volumeDB) return;
            _gains[bandIndex] = volumeDB;
        }
        EmitChanged();
    }

    private void CheckBand(int bandIndex)
    {
        if ((uint)bandIndex >= (uint)_gains.Length) throw new ArgumentOutOfRangeException(nameof(bandIndex));
    }

    internal ReadOnlySpan<float> Frequencies => _frequencies;
    internal void CopyLinearGains(Span<float> destination)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            for (var i = 0; i < _gains.Length; i++) destination[i] = Mathf.DBToLinear(_gains[i]);
        }
    }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectEQInstance(this);
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(_bandProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectEQ();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        lock (_gate) { ThrowIfDisposed(); _gains.CopyTo(((AudioEffectEQ)target)._gains, 0); }
    }
}

/// <summary>Six-band graphic equalizer using the shared indexed gain API.</summary>
public sealed class AudioEffectEQ6 : AudioEffectEQ
{
    /// <summary>Creates the six-band preset.</summary>
    public AudioEffectEQ6() : base(6) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectEQ6();
}

/// <summary>Ten-band graphic equalizer using the shared indexed gain API.</summary>
public sealed class AudioEffectEQ10 : AudioEffectEQ
{
    /// <summary>Creates the ten-band preset.</summary>
    public AudioEffectEQ10() : base(10) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectEQ10();
}

/// <summary>Twenty-one-band graphic equalizer using the shared indexed gain API.</summary>
public sealed class AudioEffectEQ21 : AudioEffectEQ
{
    /// <summary>Creates the twenty-one-band preset.</summary>
    public AudioEffectEQ21() : base(21) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectEQ21();
}

internal sealed class AudioEffectEQInstance : AudioEffectInstance
{
    private readonly AudioEffectEQ _source;
    private readonly EQBand[] _left, _right;
    private readonly float[] _gains;
    private float _rate;

    internal AudioEffectEQInstance(AudioEffectEQ source)
    {
        _source = source;
        _left = new EQBand[source.GetBandCount()]; _right = new EQBand[_left.Length]; _gains = new float[_left.Length];
        Prepare(AudioServer.Instance.GetMixRate());
    }

    private void Prepare(float rate)
    {
        var frequencies = _source.Frequencies;
        for (var i = 0; i < frequencies.Length; i++)
        {
            var center = frequencies[i];
            var octave = i == 0 ? Math.Log2(frequencies[1] / center) : i == frequencies.Length - 1
                ? Math.Log2(center / frequencies[i - 1])
                : (Math.Log2(frequencies[i + 1] / center) + Math.Log2(center / frequencies[i - 1])) * .5;
            var lower = Math.Round(center / Math.Pow(2, octave * .5), MidpointRounding.AwayFromZero);
            _left[i].Prepare(center, lower, rate);
            _right[i].Prepare(center, lower, rate);
        }
        _rate = rate;
    }

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var rate = AudioServer.Instance.GetMixRate();
        if (rate != _rate) Prepare(rate);
        _source.CopyLinearGains(_gains);
        for (var i = 0; i < source.Length; i++)
        {
            var input = source[i]; var left = 0f; var right = 0f;
            for (var band = 0; band < _gains.Length; band++)
            {
                left += _left[band].Process(input.X) * _gains[band];
                right += _right[band].Process(input.Y) * _gains[band];
            }
            if (!float.IsFinite(left) || !float.IsFinite(right))
            {
                Array.Clear(_left); Array.Clear(_right); _rate = 0;
                throw new ArithmeticException("Equalizer output exceeded finite PCM storage.");
            }
            destination[i] = new(left, right);
        }
    }

    private struct EQBand
    {
        private float _c1, _c2, _c3;
        private float _a2, _a3, _b2, _b3;

        internal void Prepare(double center, double lower, double rate)
        {
            const double sideGainSquared = .5;
            var theta = Math.Tau * center / rate; var thetaLower = Math.Tau * lower / rate;
            var cos = Math.Cos(theta); var cosLower = Math.Cos(thetaLower); var sinLower = Math.Sin(thetaLower);
            var a = sideGainSquared * cos * cos - 2 * sideGainSquared * cosLower * cos + sideGainSquared - sinLower * sinLower;
            var b = 2 * sideGainSquared * cosLower * cosLower + sideGainSquared * cos * cos - 2 * sideGainSquared * cosLower * cos - sideGainSquared + sinLower * sinLower;
            var c = .25 * sideGainSquared * cos * cos - .5 * sideGainSquared * cosLower * cos + .25 * sideGainSquared - .25 * sinLower * sinLower;
            var discriminant = b * b - 4 * a * c;
            if (a == 0 || discriminant < 0) throw new ArithmeticException("Equalizer band coefficients cannot be prepared.");
            var root = (-b + Math.Sqrt(discriminant)) / (2 * a);
            _c1 = (float)(.5 - root); _c2 = (float)(2 * root); _c3 = (float)((1 + 2 * root) * cos);
            if (!float.IsFinite(_c1) || !float.IsFinite(_c2) || !float.IsFinite(_c3)) throw new ArithmeticException("Equalizer band coefficients are not finite.");
        }

        internal float Process(float input)
        {
            var result = _c1 * (input - _a3) + _c3 * _b2 - _c2 * _b3;
            _a3 = _a2; _a2 = input; _b3 = _b2; _b2 = result;
            return result;
        }
    }
}
