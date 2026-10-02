namespace Electron2D;

/// <summary>Selects a power-of-two audio transform preset shared by spectrum analysis and pitch shifting.</summary>
public enum AudioFFTSize
{
    /// <summary>256 frequency bins from a 512-frame window.</summary>
    Size256 = 0,
    /// <summary>512 frequency bins from a 1024-frame window.</summary>
    Size512 = 1,
    /// <summary>1024 frequency bins from a 2048-frame window.</summary>
    Size1024 = 2,
    /// <summary>2048 frequency bins from a 4096-frame window.</summary>
    Size2048 = 3,
    /// <summary>4096 frequency bins from an 8192-frame window.</summary>
    Size4096 = 4,
    /// <summary>Nonselectable upper enum bound.</summary>
    Max = 5
}

/// <summary>Passes bus audio through while preparing queryable stereo frequency magnitudes.</summary>
/// <remarks>Instances capture Hann-windowed PCM and publish a spectrum after each complete transform window.
/// Configuration edits affect newly created instances; existing instances retain their prepared size and history.</remarks>
public sealed class AudioEffectSpectrumAnalyzer : AudioEffect
{
    private readonly object _gate = new();
    private float _bufferLength = 2;
    private AudioFFTSize _fftSize = AudioFFTSize.Size1024;

    /// <summary>Creates the default two-second, 1024-bin analyzer configuration.</summary>
    public AudioEffectSpectrumAnalyzer() { }

    /// <summary>Gets or sets the retained spectrum history duration in seconds.</summary>
    /// <value>Two seconds initially; finite from 0.1 through 4 seconds.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float BufferLength { get { lock (_gate) { ThrowIfDisposed(); return _bufferLength; } } set { if (!float.IsFinite(value) || value < .1f || value > 4) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_bufferLength == value) return; _bufferLength = value; } EmitChanged(); } }

    /// <summary>Gets or sets the shared audio FFT size preset for new instances.</summary>
    /// <value>Size1024 initially; Max is not selectable.</value>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is not a selectable preset.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AudioFFTSize FFTSize { get { lock (_gate) { ThrowIfDisposed(); return _fftSize; } } set { if (value is < AudioFFTSize.Size256 or >= AudioFFTSize.Max) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_fftSize == value) return; _fftSize = value; } EmitChanged(); } }

    internal (float Length, AudioFFTSize Size) Snapshot() { lock (_gate) { ThrowIfDisposed(); return (_bufferLength, _fftSize); } }
    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectSpectrumAnalyzerInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectSpectrumAnalyzer, float>(nameof(BufferLength), e => e.BufferLength, (e, v) => e.BufferLength = v, _ => 2, stored: true),
        new PropertyDescriptor<AudioEffectSpectrumAnalyzer, AudioFFTSize>(nameof(FFTSize), e => e.FFTSize, (e, v) => e.FFTSize = v, _ => AudioFFTSize.Size1024, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectSpectrumAnalyzer();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = Snapshot(); var copy = (AudioEffectSpectrumAnalyzer)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._bufferLength = state.Length; copy._fftSize = state.Size; }
    }
}

/// <summary>Owns a prepared stereo spectrum and answers linear magnitude queries for a bus effect.</summary>
/// <remarks>AudioServer returns borrowed attached instances. Queries synchronize with the audio callback and
/// return the latest complete FFT; processing is pass-through. Structural effect edits invalidate borrowed instances.</remarks>
public sealed class AudioEffectSpectrumAnalyzerInstance : AudioEffectInstance
{
    /// <summary>Selects how magnitudes are combined across the requested frequency bins.</summary>
    public enum MagnitudeMode
    {
        /// <summary>Arithmetic mean over the inclusive bin range.</summary>
        Average = 0,
        /// <summary>Maximum over the inclusive bin range.</summary>
        Max = 1
    }

    private readonly object _spectrumGate = new();
    private readonly AudioEffectSpectrumAnalyzer _source;
    private readonly float[] _temporal;
    private readonly Vector2[] _history;
    private readonly int _size, _windowFrames, _historyCount;
    private readonly float _rate;
    private int _temporalPosition, _spectrumPosition;

    internal AudioEffectSpectrumAnalyzerInstance(AudioEffectSpectrumAnalyzer source)
    {
        _source = source;
        var settings = source.Snapshot();
        _size = 256 << (int)settings.Size;
        _windowFrames = _size * 2;
        _rate = AudioServer.Instance.GetMixRate();
        if (!float.IsFinite(_rate) || _rate <= 0 || _rate > 1_000_000) throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared spectrum storage.");
        _historyCount = (int)(settings.Length / (_size / _rate)) + 1;
        _history = new Vector2[checked(_historyCount * _size)];
        _temporal = new float[_size * 8];
    }

    /// <summary>Returns the latest complete stereo magnitudes in a finite frequency range.</summary>
    /// <param name="fromHZ">First frequency in hertz; values outside the spectrum clamp to its edge bins.</param>
    /// <param name="toHZ">Last frequency in hertz; reversed endpoints are accepted.</param>
    /// <param name="mode">Average or maximum over the inclusive bin range.</param>
    /// <returns>Left and right linear magnitudes; zero before the first complete FFT.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A frequency is nonfinite or the mode is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public Vector2 GetMagnitudeForFrequencyRange(float fromHZ, float toHZ, MagnitudeMode mode = MagnitudeMode.Max)
    {
        if (!float.IsFinite(fromHZ) || !float.IsFinite(toHZ) || mode is < MagnitudeMode.Average or > MagnitudeMode.Max)
            throw new ArgumentOutOfRangeException(nameof(fromHZ), "Frequencies and magnitude mode must be valid.");
        _ = _source.Snapshot();
        lock (_spectrumGate)
        {
            ThrowIfDisposed();
            var scale = _size / (_rate * .5);
            var begin = (int)Math.Clamp(fromHZ * scale, 0, _size - 1);
            var end = (int)Math.Clamp(toHZ * scale, 0, _size - 1);
            if (begin > end) (begin, end) = (end, begin);
            var offset = _spectrumPosition * _size;
            var left = 0f; var right = 0f;
            for (var bin = begin; bin <= end; bin++)
            {
                var value = _history[offset + bin];
                if (mode == MagnitudeMode.Average) { left += value.X; right += value.Y; }
                else { left = Math.Max(left, value.X); right = Math.Max(right, value.Y); }
            }
            if (mode == MagnitudeMode.Average) { var count = end - begin + 1; left /= count; right /= count; }
            return new(left, right);
        }
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;
    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        _ = _source.Snapshot();
        lock (_spectrumGate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                var frame = source[i]; destination[i] = frame;
                var window = (float)(.5 - .5 * Math.Cos(Math.Tau * _temporalPosition / _windowFrames));
                var left = _temporalPosition * 2;
                var right = (_temporalPosition + _windowFrames) * 2;
                _temporal[left] = window * frame.X; _temporal[left + 1] = 0;
                _temporal[right] = window * frame.Y; _temporal[right + 1] = 0;
                if (++_temporalPosition == _windowFrames) PublishSpectrum();
            }
        }
    }

    private void PublishSpectrum()
    {
        Transform(_temporal, 0, _windowFrames);
        Transform(_temporal, _windowFrames * 2, _windowFrames);
        var next = (_spectrumPosition + 1) % _historyCount;
        var offset = next * _size;
        for (var bin = 0; bin < _size; bin++)
        {
            var left = bin * 2; var right = _windowFrames * 2 + left;
            var leftMagnitude = Math.Sqrt((double)_temporal[left] * _temporal[left] + (double)_temporal[left + 1] * _temporal[left + 1]) / _size;
            var rightMagnitude = Math.Sqrt((double)_temporal[right] * _temporal[right] + (double)_temporal[right + 1] * _temporal[right + 1]) / _size;
            if (!double.IsFinite(leftMagnitude) || !double.IsFinite(rightMagnitude) || leftMagnitude > float.MaxValue || rightMagnitude > float.MaxValue)
            {
                Array.Clear(_history); Array.Clear(_temporal); _temporalPosition = _spectrumPosition = 0;
                throw new ArithmeticException("Spectrum transform exceeded finite magnitude storage.");
            }
            _history[offset + bin] = new((float)leftMagnitude, (float)rightMagnitude);
        }
        _spectrumPosition = next; _temporalPosition = 0;
    }

    private static void Transform(float[] data, int offset, int frames)
    {
        for (int i = 1, reverse = 0; i < frames; i++)
        {
            var bit = frames >> 1;
            while ((reverse & bit) != 0) { reverse ^= bit; bit >>= 1; }
            reverse ^= bit;
            if (i >= reverse) continue;
            var a = offset + i * 2; var b = offset + reverse * 2;
            (data[a], data[b]) = (data[b], data[a]);
            (data[a + 1], data[b + 1]) = (data[b + 1], data[a + 1]);
        }
        for (var length = 2; length <= frames; length <<= 1)
        {
            var angle = -Math.Tau / length;
            var rootReal = (float)Math.Cos(angle); var rootImaginary = (float)Math.Sin(angle);
            for (var start = 0; start < frames; start += length)
            {
                var real = 1f; var imaginary = 0f;
                for (var i = 0; i < length / 2; i++)
                {
                    var a = offset + (start + i) * 2; var b = a + length;
                    var otherReal = data[b] * real - data[b + 1] * imaginary;
                    var otherImaginary = data[b] * imaginary + data[b + 1] * real;
                    var firstReal = data[a]; var firstImaginary = data[a + 1];
                    data[a] = firstReal + otherReal; data[a + 1] = firstImaginary + otherImaginary;
                    data[b] = firstReal - otherReal; data[b + 1] = firstImaginary - otherImaginary;
                    var nextReal = real * rootReal - imaginary * rootImaginary;
                    imaginary = real * rootImaginary + imaginary * rootReal; real = nextReal;
                }
            }
        }
    }
}
