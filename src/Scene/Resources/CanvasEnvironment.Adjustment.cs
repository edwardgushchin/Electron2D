namespace Electron2D;

public sealed partial class CanvasEnvironment
{
    private CanvasAdjustmentSettings _adjustment = CanvasAdjustmentSettings.Default;

    /// <summary>Gets or sets whether this environment adjusts GPU canvas color after glow and tone mapping.</summary>
    /// <value>False initially.</value>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public bool AdjustmentEnabled
    {
        get => Read(ref _adjustment.Enabled);
        set => Set(ref _adjustment.Enabled, value);
    }

    /// <summary>Gets or sets the linear brightness multiplier.</summary>
    /// <value>One initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The multiplier is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float AdjustmentBrightness
    {
        get => Read(ref _adjustment.Brightness);
        set
        {
            ValidateAdjustmentValue(value, nameof(value));
            Set(ref _adjustment.Brightness, value);
        }
    }

    /// <summary>Gets or sets the contrast multiplier around the encoded mid-gray value.</summary>
    /// <value>One initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The multiplier is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float AdjustmentContrast
    {
        get => Read(ref _adjustment.Contrast);
        set
        {
            ValidateAdjustmentValue(value, nameof(value));
            Set(ref _adjustment.Contrast, value);
        }
    }

    /// <summary>Gets or sets the saturation multiplier around the mean of the encoded RGB channels.</summary>
    /// <value>One initially. Zero produces gray.</value>
    /// <exception cref="ArgumentOutOfRangeException">The multiplier is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float AdjustmentSaturation
    {
        get => Read(ref _adjustment.Saturation);
        set
        {
            ValidateAdjustmentValue(value, nameof(value));
            Set(ref _adjustment.Saturation, value);
        }
    }

    /// <summary>Gets or sets the optional color-correction ramp sampled after brightness, contrast and saturation.</summary>
    /// <value>Null initially.</value>
    /// <remarks>The renderer samples the first row of a filterable color texture independently for each RGB channel.
    /// Lookup coordinates use the nominal encoded range from zero to one. The texture's alpha is ignored.</remarks>
    /// <exception cref="ObjectDisposedException">This environment or the supplied texture is disposed.</exception>
    public Texture? AdjustmentColorCorrection
    {
        get => Read(ref _adjustment.Lookup);
        set
        {
            ObjectDisposedException.ThrowIf(value is { IsDisposed: true }, value!);
            Set(ref _adjustment.Lookup, value);
        }
    }

    internal static void ValidateAdjustmentValue(float value, string parameter)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(parameter);
    }

    internal void SetServerAdjustment(CanvasAdjustmentSettings settings)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _adjustment = settings;
        }
        EmitChanged();
    }

    private static readonly PropertyDescriptor[] AdjustmentProperties =
    [
        new PropertyDescriptor<CanvasEnvironment, bool>(nameof(AdjustmentEnabled), e => e.AdjustmentEnabled, (e, value) => e.AdjustmentEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(AdjustmentBrightness), e => e.AdjustmentBrightness, (e, value) => e.AdjustmentBrightness = value, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(AdjustmentContrast), e => e.AdjustmentContrast, (e, value) => e.AdjustmentContrast = value, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(AdjustmentSaturation), e => e.AdjustmentSaturation, (e, value) => e.AdjustmentSaturation = value, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, Texture?>(nameof(AdjustmentColorCorrection), e => e.AdjustmentColorCorrection, (e, value) => e.AdjustmentColorCorrection = value, _ => null, stored: true)
    ];
}

internal struct CanvasAdjustmentSettings
{
    internal bool Enabled;
    internal float Brightness, Contrast, Saturation;
    internal Texture? Lookup;

    internal static CanvasAdjustmentSettings Default => new() { Brightness = 1, Contrast = 1, Saturation = 1 };
    internal readonly bool HasScalarChanges => Brightness != 1 || Contrast != 1 || Saturation != 1;
    internal readonly bool Active => Enabled && (HasScalarChanges || Lookup is { IsDisposed: false });
}
