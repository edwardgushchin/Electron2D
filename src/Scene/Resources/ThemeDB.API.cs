namespace Electron2D;

public sealed partial class ThemeDB
{
    /// <summary>Gets the built-in theme resource for the currently implemented control families.</summary>
    /// <returns>The borrowed mutable theme, with the embedded font, Label/panel/tooltip styles, button, slider and scroll skins/hints, box/grid/flow constants and split-bar/touch skins.</returns>
    /// <exception cref="ObjectDisposedException">The service or its theme is disposed.</exception>
    public static Theme GetDefaultTheme() => Service.GetDefaultThemeCore();

    /// <summary>Occurs after a universal fallback assignment changes its value.</summary>
    public static event Action? FallbackChanged
    {
        add => Service.FallbackChangedCore += value;
        remove => Service.FallbackChangedCore -= value;
    }

    /// <summary>Gets or sets the final base-scale fallback when no theme defines a positive default.</summary>
    /// <value>One initially; finite signed values are preserved.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public static float FallbackBaseScale
    {
        get => Service.FallbackBaseScaleCore;
        set => Service.FallbackBaseScaleCore = value;
    }

    /// <summary>Gets or sets the borrowed font used when no theme provides a font.</summary>
    /// <value>The embedded Open Sans SemiBold resource initially. Null is an explicit empty fallback.</value>
    /// <remarks>The embedded font initializes its native face on the first text query.</remarks>
    /// <exception cref="ObjectDisposedException">The service or assigned font is disposed.</exception>
    public static Font? FallbackFont
    {
        get => Service.FallbackFontCore;
        set => Service.FallbackFontCore = value;
    }

    /// <summary>Gets or sets the final integer font-size fallback for typed size lookups.</summary>
    /// <value>Sixteen initially; signed values are preserved.</value>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public static int FallbackFontSize
    {
        get => Service.FallbackFontSizeCore;
        set => Service.FallbackFontSizeCore = value;
    }

    /// <summary>Gets or sets the borrowed fallback icon used for missing or null icon entries.</summary>
    /// <value>A lazily decoded sixteen-pixel error icon initially; null is an explicit empty fallback.</value>
    /// <exception cref="ObjectDisposedException">The service or assigned icon is disposed.</exception>
    public static Texture? FallbackIcon
    {
        get => Service.FallbackIconCore;
        set => Service.FallbackIconCore = value;
    }

    /// <summary>Gets or sets the borrowed fallback style used for missing or null style entries.</summary>
    /// <value>A hollow two-unit border with four-unit content margins initially. Null is allowed.</value>
    /// <exception cref="ObjectDisposedException">The service or assigned style is disposed.</exception>
    public static StyleBox? FallbackStyleBox
    {
        get => Service.FallbackStyleBoxCore;
        set => Service.FallbackStyleBoxCore = value;
    }

}
