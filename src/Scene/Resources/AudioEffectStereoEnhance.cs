namespace Electron2D;

/// <summary>Widens stereo bus audio through side gain and a delayed channel or center signal.</summary>
/// <remarks>Each instance owns a prepared delay line for one stereo pair. Live edits use one settings
/// snapshot per block and retain the delay history. A bus borrows this resource and owns its instances.</remarks>
public sealed class AudioEffectStereoEnhance : AudioEffect
{
    private readonly object _gate = new();
    private float _panPullout = 1, _timePulloutMS, _surround;

    /// <summary>Creates a neutral stereo effect with no delay or surround contribution.</summary>
    public AudioEffectStereoEnhance() { }

    /// <summary>Gets or sets the gain applied to the stereo side signal.</summary>
    /// <value>One initially; zero mixes both channels to their center. Valid from zero through four.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside zero through four or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float PanPullout
    {
        get { lock (_gate) { ThrowIfDisposed(); return _panPullout; } }
        set { Validate(value, 4); lock (_gate) { ThrowIfDisposed(); if (_panPullout == value) return; _panPullout = value; } EmitChanged(); }
    }

    /// <summary>Gets or sets the right-channel or surround-center delay in milliseconds.</summary>
    /// <value>Zero initially; valid from zero through fifty milliseconds.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside zero through fifty or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float TimePulloutMS
    {
        get { lock (_gate) { ThrowIfDisposed(); return _timePulloutMS; } }
        set { Validate(value, 50); lock (_gate) { ThrowIfDisposed(); if (_timePulloutMS == value) return; _timePulloutMS = value; } EmitChanged(); }
    }

    /// <summary>Gets or sets the delayed center contribution to opposite stereo polarities.</summary>
    /// <value>Zero initially; valid from zero through one. Positive values select surround mode.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside zero through one or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Surround
    {
        get { lock (_gate) { ThrowIfDisposed(); return _surround; } }
        set { Validate(value, 1); lock (_gate) { ThrowIfDisposed(); if (_surround == value) return; _surround = value; } EmitChanged(); }
    }

    private static void Validate(float value, float maximum)
    {
        if (!float.IsFinite(value) || value < 0 || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
    }

    internal readonly record struct Settings(float PanPullout, float TimePulloutMS, float Surround);
    internal Settings Snapshot() { lock (_gate) { ThrowIfDisposed(); return new(_panPullout, _timePulloutMS, _surround); } }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectStereoEnhanceInstance(this);

    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectStereoEnhance, float>(nameof(PanPullout), e => e.PanPullout, (e, v) => e.PanPullout = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectStereoEnhance, float>(nameof(TimePulloutMS), e => e.TimePulloutMS, (e, v) => e.TimePulloutMS = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectStereoEnhance, float>(nameof(Surround), e => e.Surround, (e, v) => e.Surround = v, _ => 0, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectStereoEnhance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var settings = Snapshot(); var copy = (AudioEffectStereoEnhance)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed(); copy._panPullout = settings.PanPullout;
            copy._timePulloutMS = settings.TimePulloutMS; copy._surround = settings.Surround;
        }
    }
}

internal sealed class AudioEffectStereoEnhanceInstance : AudioEffectInstance
{
    private readonly AudioEffectStereoEnhance _source;
    private readonly float[] _ring;
    private readonly int _mask;
    private readonly float _rate;
    private int _position;

    internal AudioEffectStereoEnhanceInstance(AudioEffectStereoEnhance source)
    {
        _source = source;
        _rate = AudioServer.GetMixRate();
        var required = .052 * _rate;
        if (!double.IsFinite(required) || required <= 0 || required >= 1 << 22)
            throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared stereo delay storage.");
        var frames = (int)required;
        var size = 1;
        while (size <= frames) size <<= 1;
        _ring = new float[size]; _mask = size - 1;
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var delay = (int)(settings.TimePulloutMS / 1000.0 * _rate);
        for (var i = 0; i < source.Length; i++)
        {
            var input = source[i];
            var center = ((double)input.X + input.Y) * .5;
            var left = settings.PanPullout == 1 ? input.X : center + (input.X - center) * settings.PanPullout;
            var right = settings.PanPullout == 1 ? input.Y : center + (input.Y - center) * settings.PanPullout;
            var slot = _position & _mask;
            if (settings.Surround > 0)
            {
                _ring[slot] = (float)((left + right) * .5);
                var delayed = _ring[unchecked((_position - delay) & _mask)] * settings.Surround;
                left += delayed; right -= delayed;
            }
            else
            {
                _ring[slot] = (float)right;
                right = _ring[unchecked((_position - delay) & _mask)];
            }
            if (!double.IsFinite(left) || !double.IsFinite(right) || Math.Abs(left) > float.MaxValue || Math.Abs(right) > float.MaxValue || !float.IsFinite(_ring[slot]))
            {
                Array.Clear(_ring); _position = 0;
                throw new ArithmeticException("Stereo enhancement output exceeded finite PCM storage.");
            }
            destination[i] = new((float)left, (float)right);
            _position = unchecked(_position + 1);
        }
    }
}
