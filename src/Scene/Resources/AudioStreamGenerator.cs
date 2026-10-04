using System.Numerics;

namespace Electron2D;

/// <summary>Creates independent bounded stereo PCM queues for procedural sound producers.</summary>
/// <remarks>Playback borrows this resource. BufferLength determines queue capacity only at playback creation;
/// rate changes affect the next mix. Input uses the prepared recording frequency and opens a paused input
/// device at mode selection and playback creation when necessary. Producer methods copy finite samples into prepared storage.</remarks>
public sealed class AudioStreamGenerator : AudioStream
{
    /// <summary>Selects the source frame sampling frequency.</summary>
    public enum AudioStreamGeneratorMixRate
    {
        /// <summary>Uses the current output mix frequency.</summary>
        Output = 0,
        /// <summary>Uses the prepared recording-device frequency.</summary>
        Input = 1,
        /// <summary>Uses this resource's MixRate.</summary>
        Custom = 2,
        /// <summary>Exclusive selector bound, never a usable mode.</summary>
        Max = 3
    }
    private readonly object _gate = new();
    private float _mixRate = 44100, _bufferLength = .5f;
    private AudioStreamGeneratorMixRate _mode = AudioStreamGeneratorMixRate.Custom;
    /// <summary>Creates a custom-rate 44100 Hz generator with a half-second requested buffer.</summary>
    public AudioStreamGenerator() { }
    /// <summary>Gets or sets the positive finite custom sampling frequency in Hz.</summary>
    /// <value>44100 initially; Output ignores this value.</value>
    /// <exception cref="ArgumentOutOfRangeException">The frequency is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float MixRate
    {
        get { lock (_gate) { ThrowIfDisposed(); return _mixRate; } }
        set { lock (_gate) { ThrowIfDisposed(); Positive(value); _mixRate = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the requested queue duration in seconds.</summary>
    /// <value>0.5 initially. Existing playback queues never resize.</value>
    /// <remarks>Playback creation truncates rate times duration to source frames, rounds storage up to a
    /// strictly larger power of two and reserves one empty slot. Requests exceeding 2^24 storage frames reject at creation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The duration is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float BufferLength
    {
        get { lock (_gate) { ThrowIfDisposed(); return _bufferLength; } }
        set { lock (_gate) { ThrowIfDisposed(); Positive(value); _bufferLength = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the source sampling-rate selection.</summary>
    /// <value>Custom initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is outside Output/Input/Custom.</exception>
    /// <exception cref="InvalidOperationException">Input frequency preparation is off-owner or the recording device is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AudioStreamGeneratorMixRate MixRateMode
    {
        get { lock (_gate) { ThrowIfDisposed(); return _mode; } }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (value is < AudioStreamGeneratorMixRate.Output or >= AudioStreamGeneratorMixRate.Max) throw new ArgumentOutOfRangeException(nameof(value));
                if (value == AudioStreamGeneratorMixRate.Input) _ = AudioServer.GetInputMixRate();
                _mode = value;
            }
            EmitChanged();
        }
    }
    private static void Positive(float value) { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal float TargetRate
    {
        get { lock (_gate) { ThrowIfDisposed(); return _mode switch { AudioStreamGeneratorMixRate.Output => AudioServer.GetMixRate(), AudioStreamGeneratorMixRate.Input => AudioServer.Service.PreparedInputMixRate, _ => _mixRate }; } }
    }
    internal float PrepareRate() { lock (_gate) { ThrowIfDisposed(); if (_mode == AudioStreamGeneratorMixRate.Input) _ = AudioServer.GetInputMixRate(); return TargetRate; } }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_mode == AudioStreamGeneratorMixRate.Input) _ = AudioServer.GetInputMixRate(); var requested = TargetRate * _bufferLength;
            if (!float.IsFinite(requested) || requested >= 1 << 24) throw new ArgumentOutOfRangeException(nameof(BufferLength), "The requested generator queue exceeds 2^24 storage frames.");
            var count = (uint)requested;
            return new AudioStreamGeneratorPlayback(this, count == 0 ? 1 : 1 << (BitOperations.Log2(count) + 1));
        }
    }
    /// <inheritdoc />
    protected override string OnGetStreamName() => "UserFeed";
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioStreamGenerator, float>(nameof(MixRate), p => p.MixRate, (p, v) => p.MixRate = v, _ => 44100, stored: true),
        new PropertyDescriptor<AudioStreamGenerator, float>(nameof(BufferLength), p => p.BufferLength, (p, v) => p.BufferLength = v, _ => .5f, stored: true),
        new PropertyDescriptor<AudioStreamGenerator, AudioStreamGeneratorMixRate>(nameof(MixRateMode), p => p.MixRateMode, (p, v) => p.MixRateMode = v, _ => AudioStreamGeneratorMixRate.Custom, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamGenerator();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        float rate, length; AudioStreamGeneratorMixRate mode;
        lock (_gate) { ThrowIfDisposed(); rate = _mixRate; length = _bufferLength; mode = _mode; }
        var other = (AudioStreamGenerator)target; lock (other._gate) { other.ThrowIfDisposed(); other._mixRate = rate; other._bufferLength = length; other._mode = mode; }
    }
}
