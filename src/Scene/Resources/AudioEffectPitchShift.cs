namespace Electron2D;

/// <summary>Raises or lowers stereo bus pitch without changing playback duration.</summary>
/// <remarks>Instances prepare independent left/right phase-vocoder histories at their creation-time FFT size.
/// Pitch scale and oversampling edits apply on the next block; changing oversampling resets prepared history.
/// Unit pitch passes PCM through without spectral latency.</remarks>
public sealed class AudioEffectPitchShift : AudioEffect
{
    private readonly object _gate = new();
    private float _pitchScale = 1;
    private int _oversampling = 4;
    private AudioFFTSize _fftSize = AudioFFTSize.Size2048;

    /// <summary>Creates a unit-pitch resource using fourfold overlap and a 2048-frame transform.</summary>
    public AudioEffectPitchShift() { }

    /// <summary>Gets or sets the positive pitch multiplier.</summary>
    /// <value>One initially, which passes PCM through unchanged.</value>
    /// <exception cref="ArgumentOutOfRangeException">The multiplier is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float PitchScale { get { lock (_gate) { ThrowIfDisposed(); return _pitchScale; } } set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_pitchScale == value) return; _pitchScale = value; } EmitChanged(); } }

    /// <summary>Gets or sets the transform overlap factor.</summary>
    /// <value>Four initially; supported factors are integers from four through thirty-two.</value>
    /// <exception cref="ArgumentOutOfRangeException">The factor is outside four through thirty-two.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int Oversampling { get { lock (_gate) { ThrowIfDisposed(); return _oversampling; } } set { if (value is < 4 or > 32) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_oversampling == value) return; _oversampling = value; } EmitChanged(); } }

    /// <summary>Gets or sets the shared transform-size preset for newly created instances.</summary>
    /// <value>Size2048 initially; Max is not selectable.</value>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is not a selectable preset.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AudioFFTSize FFTSize { get { lock (_gate) { ThrowIfDisposed(); return _fftSize; } } set { if (value is < AudioFFTSize.Size256 or >= AudioFFTSize.Max) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_fftSize == value) return; _fftSize = value; } EmitChanged(); } }

    internal readonly record struct Settings(float Scale, int Oversampling, AudioFFTSize Size);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_pitchScale, _oversampling, _fftSize); } }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectPitchShiftInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectPitchShift, float>(nameof(PitchScale), e => e.PitchScale, (e, v) => e.PitchScale = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectPitchShift, int>(nameof(Oversampling), e => e.Oversampling, (e, v) => e.Oversampling = v, _ => 4, stored: true),
        new PropertyDescriptor<AudioEffectPitchShift, AudioFFTSize>(nameof(FFTSize), e => e.FFTSize, (e, v) => e.FFTSize = v, _ => AudioFFTSize.Size2048, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectPitchShift();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var settings = Snapshot(); var copy = (AudioEffectPitchShift)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._pitchScale = settings.Scale; copy._oversampling = settings.Oversampling; copy._fftSize = settings.Size; }
    }
}

internal sealed class AudioEffectPitchShiftInstance : AudioEffectInstance
{
    private readonly AudioEffectPitchShift _source;
    private readonly Channel _left, _right;
    private readonly float _rate;
    private int _oversampling;
    private bool _bypassed = true;

    internal AudioEffectPitchShiftInstance(AudioEffectPitchShift source)
    {
        _source = source;
        var settings = source.Snapshot();
        _rate = AudioServer.GetMixRate();
        if (!float.IsFinite(_rate) || _rate <= 0 || _rate > 1_000_000) throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared pitch-shift storage.");
        var size = 256 << (int)settings.Size;
        _left = new Channel(size); _right = new Channel(size);
        _oversampling = settings.Oversampling;
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var bypass = MathF.Abs(settings.Scale - 1) <= .000001f;
        if (bypass)
        {
            if (!_bypassed) { _left.Reset(); _right.Reset(); _bypassed = true; }
            source.CopyTo(destination); return;
        }
        if (_bypassed || _oversampling != settings.Oversampling)
        {
            _left.Reset(); _right.Reset(); _oversampling = settings.Oversampling; _bypassed = false;
        }
        try
        {
            for (var i = 0; i < source.Length; i++)
            {
                var frame = source[i];
                destination[i] = new(_left.Process(frame.X, settings.Scale, _oversampling, _rate),
                    _right.Process(frame.Y, settings.Scale, _oversampling, _rate));
            }
        }
        catch (ArithmeticException) { _left.Reset(); _right.Reset(); throw; }
    }

    private sealed class Channel(int size)
    {
        private readonly float[] _input = new float[size], _output = new float[size], _work = new float[size * 2];
        private readonly float[] _lastPhase = new float[size / 2 + 1], _sumPhase = new float[size / 2 + 1];
        private readonly float[] _accumulated = new float[size * 2];
        private readonly float[] _analysisMagnitude = new float[size / 2 + 1], _analysisFrequency = new float[size / 2 + 1];
        private readonly float[] _synthesisMagnitude = new float[size / 2 + 1], _synthesisFrequency = new float[size / 2 + 1];
        private int _rover;

        internal float Process(float sample, float scale, int oversampling, float rate)
        {
            var step = size / oversampling;
            var latency = size - step;
            if (_rover == 0) _rover = latency;
            _input[_rover] = sample;
            var result = _output[_rover - latency];
            if (++_rover >= size) { _rover = latency; ProcessFrame(scale, oversampling, rate, step, latency); }
            if (!float.IsFinite(result)) throw new ArithmeticException("Pitch-shift output exceeded finite PCM storage.");
            return result;
        }

        private void ProcessFrame(float scale, int oversampling, float rate, int step, int latency)
        {
            var half = size / 2;
            var frequencyPerBin = rate / (double)size;
            var expected = Math.Tau * step / size;
            for (var k = 0; k < size; k++)
            {
                var window = .5 - .5 * Math.Cos(Math.Tau * k / size);
                _work[2 * k] = (float)(_input[k] * window); _work[2 * k + 1] = 0;
            }
            Transform(inverse: false);
            for (var k = 0; k <= half; k++)
            {
                var real = _work[2 * k]; var imaginary = _work[2 * k + 1];
                var magnitude = 2 * Math.Sqrt((double)real * real + (double)imaginary * imaginary);
                var phase = Math.Atan2(imaginary, real);
                var delta = phase - _lastPhase[k]; _lastPhase[k] = (float)phase;
                delta -= k * expected;
                var quadrant = (long)(delta / Math.PI);
                quadrant += quadrant >= 0 ? quadrant & 1 : -(quadrant & 1);
                delta -= Math.PI * quadrant;
                delta = oversampling * delta / Math.Tau;
                var frequency = k * frequencyPerBin + delta * frequencyPerBin;
                _analysisMagnitude[k] = (float)magnitude; _analysisFrequency[k] = (float)frequency;
                if (!float.IsFinite(_analysisMagnitude[k]) || !float.IsFinite(_analysisFrequency[k]))
                    throw new ArithmeticException("Pitch-shift analysis exceeded finite spectral storage.");
            }
            Array.Clear(_synthesisMagnitude); Array.Clear(_synthesisFrequency);
            for (var k = 0; k <= half; k++)
            {
                var shifted = k * (double)scale;
                if (shifted > half) continue;
                var bin = (int)shifted;
                _synthesisMagnitude[bin] += _analysisMagnitude[k];
                _synthesisFrequency[bin] = (float)(_analysisFrequency[k] * scale);
                if (!float.IsFinite(_synthesisMagnitude[bin]) || !float.IsFinite(_synthesisFrequency[bin]))
                    throw new ArithmeticException("Pitch-shift resynthesis exceeded finite spectral storage.");
            }
            for (var k = 0; k <= half; k++)
            {
                var frequency = (_synthesisFrequency[k] - k * frequencyPerBin) / frequencyPerBin;
                var delta = Math.Tau * frequency / oversampling + k * expected;
                _sumPhase[k] += (float)delta;
                var phase = _sumPhase[k];
                _work[2 * k] = (float)(_synthesisMagnitude[k] * Math.Cos(phase));
                _work[2 * k + 1] = (float)(_synthesisMagnitude[k] * Math.Sin(phase));
                if (!float.IsFinite(_work[2 * k]) || !float.IsFinite(_work[2 * k + 1]))
                    throw new ArithmeticException("Pitch-shift synthesis exceeded finite transform storage.");
            }
            Array.Clear(_work, size + 2, size - 2);
            Transform(inverse: true);
            for (var k = 0; k < size; k++)
            {
                var window = .5 - .5 * Math.Cos(Math.Tau * k / size);
                _accumulated[k] += (float)(2 * window * _work[2 * k] / (half * oversampling));
                if (!float.IsFinite(_accumulated[k])) throw new ArithmeticException("Pitch-shift accumulation exceeded finite PCM storage.");
            }
            Array.Copy(_accumulated, 0, _output, 0, step);
            Array.Copy(_accumulated, step, _accumulated, 0, size);
            Array.Copy(_input, step, _input, 0, latency);
        }

        private void Transform(bool inverse)
        {
            for (var i = 2; i < 2 * size - 2; i += 2)
            {
                var reverse = 0;
                for (var bit = 2; bit < 2 * size; bit <<= 1)
                {
                    if ((i & bit) != 0) reverse++;
                    reverse <<= 1;
                }
                if (i >= reverse) continue;
                (_work[i], _work[reverse]) = (_work[reverse], _work[i]);
                (_work[i + 1], _work[reverse + 1]) = (_work[reverse + 1], _work[i + 1]);
            }
            for (var length = 4; length <= 2 * size; length <<= 1)
            {
                var half = length >> 1;
                var real = 1f; var imaginary = 0f;
                var angle = (float)(Math.PI / (half >> 1));
                var rootReal = (float)Math.Cos(angle);
                var rootImaginary = (inverse ? 1 : -1) * (float)Math.Sin(angle);
                for (var offset = 0; offset < half; offset += 2)
                {
                    for (var i = offset; i < 2 * size; i += length)
                    {
                        var other = i + half;
                        var transformedReal = _work[other] * real - _work[other + 1] * imaginary;
                        var transformedImaginary = _work[other] * imaginary + _work[other + 1] * real;
                        var firstReal = _work[i]; var firstImaginary = _work[i + 1];
                        _work[other] = firstReal - transformedReal; _work[other + 1] = firstImaginary - transformedImaginary;
                        _work[i] = firstReal + transformedReal; _work[i + 1] = firstImaginary + transformedImaginary;
                    }
                    var nextReal = real * rootReal - imaginary * rootImaginary;
                    imaginary = real * rootImaginary + imaginary * rootReal; real = nextReal;
                }
            }
        }

        internal void Reset()
        {
            Array.Clear(_input); Array.Clear(_output); Array.Clear(_work); Array.Clear(_lastPhase); Array.Clear(_sumPhase);
            Array.Clear(_accumulated); Array.Clear(_analysisMagnitude); Array.Clear(_analysisFrequency);
            Array.Clear(_synthesisMagnitude); Array.Clear(_synthesisFrequency); _rover = 0;
        }
    }
}
