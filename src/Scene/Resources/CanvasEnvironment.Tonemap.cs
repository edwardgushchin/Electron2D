namespace Electron2D;

public sealed partial class CanvasEnvironment
{
    /// <summary>Selects the operator for linear HDR canvas color.</summary>
    public enum ToneMapper
    {
        /// <summary>Applies exposure without compressing highlights.</summary>
        Linear = 0,
        /// <summary>Uses the extended Reinhard curve.</summary>
        Reinhard = 1,
        /// <summary>Uses a filmic curve with white normalization.</summary>
        Filmic = 2,
        /// <summary>Uses the fitted ACES curve and color transforms.</summary>
        ACES = 3,
        /// <summary>Uses AgX color transforms and a variable-range contrast curve.</summary>
        AGX = 4
    }

    private TonemapSettings _tonemap = TonemapSettings.Default;

    /// <summary>Gets or sets the tone operator used by HDR viewports selecting this environment.</summary>
    /// <value>Linear initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The operator is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public ToneMapper TonemapMode
    {
        get => Read(ref _tonemap.Mode);
        set
        {
            ValidateToneMapper(value);
            Set(ref _tonemap.Mode, value);
        }
    }

    /// <summary>Gets or sets the linear multiplier applied before tone mapping and glow extraction.</summary>
    /// <value>One initially. Zero produces black.</value>
    /// <exception cref="ArgumentOutOfRangeException">The exposure is negative or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float TonemapExposure
    {
        get => Read(ref _tonemap.Exposure);
        set
        {
            ValidateToneValue(value, nameof(value), allowZero: true);
            Set(ref _tonemap.Exposure, value);
        }
    }

    /// <summary>Gets or sets the input white point for Reinhard, Filmic and ACES.</summary>
    /// <value>One initially.</value>
    /// <remarks>Reinhard uses at least the containing output's linear maximum. Filmic and ACES use at least one.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The white point is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float TonemapWhite
    {
        get => Read(ref _tonemap.White);
        set
        {
            ValidateToneValue(value, nameof(value));
            Set(ref _tonemap.White, value);
        }
    }

    /// <summary>Gets or sets the input white point for AgX.</summary>
    /// <value>16.29 initially.</value>
    /// <remarks>The operator uses at least two and scales this white point by the containing output's linear maximum.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The white point is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float TonemapAGXWhite
    {
        get => Read(ref _tonemap.AGXWhite);
        set
        {
            ValidateToneValue(value, nameof(value));
            Set(ref _tonemap.AGXWhite, value);
        }
    }

    /// <summary>Gets or sets the contrast of the AgX tone curve around middle gray.</summary>
    /// <value>1.25 initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The contrast is nonpositive or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float TonemapAGXContrast
    {
        get => Read(ref _tonemap.AGXContrast);
        set
        {
            ValidateToneValue(value, nameof(value));
            Set(ref _tonemap.AGXContrast, value);
        }
    }

    internal static void ValidateToneValue(float value, string parameter, bool allowZero = false)
    {
        if (!float.IsFinite(value) || (allowZero ? value < 0 : value <= 0))
            throw new ArgumentOutOfRangeException(parameter);
    }

    internal static void ValidateToneMapper(ToneMapper mode)
    {
        if ((uint)mode > (uint)ToneMapper.AGX) throw new ArgumentOutOfRangeException(nameof(mode));
    }

    internal void SetServerTonemap(ToneMapper mode, float exposure, float white)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _tonemap.Mode = mode;
            _tonemap.Exposure = exposure;
            if (mode == ToneMapper.AGX) _tonemap.AGXWhite = white;
            else _tonemap.White = white;
        }
        EmitChanged();
    }

    private static readonly PropertyDescriptor[] TonemapProperties =
    [
        new PropertyDescriptor<CanvasEnvironment, ToneMapper>(nameof(TonemapMode), e => e.TonemapMode, (e, value) => e.TonemapMode = value, _ => ToneMapper.Linear, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(TonemapExposure), e => e.TonemapExposure, (e, value) => e.TonemapExposure = value, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(TonemapWhite), e => e.TonemapWhite, (e, value) => e.TonemapWhite = value, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(TonemapAGXWhite), e => e.TonemapAGXWhite, (e, value) => e.TonemapAGXWhite = value, _ => 16.29f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(TonemapAGXContrast), e => e.TonemapAGXContrast, (e, value) => e.TonemapAGXContrast = value, _ => 1.25f, stored: true)
    ];
}

internal struct TonemapSettings
{
    internal CanvasEnvironment.ToneMapper Mode;
    internal float Exposure, White, AGXWhite, AGXContrast;

    internal static TonemapSettings Default => new()
    {
        Exposure = 1,
        White = 1,
        AGXWhite = 16.29f,
        AGXContrast = 1.25f
    };
    internal readonly bool IsIdentity => Mode == CanvasEnvironment.ToneMapper.Linear && Exposure == 1;
}
