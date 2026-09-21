using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>
/// Represents a color using floating-point red, green, blue, and alpha components.
/// </summary>
/// <remarks>
/// Components usually range from <c>0</c> to <c>1</c>, but values outside that range are retained for
/// overbright and high-dynamic-range calculations. RGB components are normally nonlinear sRGB values;
/// alpha is always linear. The zero-initialized value is transparent black.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Color : IEquatable<Color>
{
    /// <summary>Gets or sets the red component, typically from <c>0</c> to <c>1</c>.</summary>
    public float R;

    /// <summary>Gets or sets the green component, typically from <c>0</c> to <c>1</c>.</summary>
    public float G;

    /// <summary>Gets or sets the blue component, typically from <c>0</c> to <c>1</c>.</summary>
    public float B;

    /// <summary>Gets or sets the linear alpha component, where <c>0</c> is transparent and <c>1</c> is opaque.</summary>
    public float A;

    /// <summary>Gets or sets <see cref="R"/> on an integer scale where <c>255</c> represents <c>1</c>.</summary>
    /// <value>The rounded value of <see cref="R"/> multiplied by 255, or a value divided by 255 when set.</value>
    /// <remarks>Finite values are not clamped to byte range; overflow saturates to an <see cref="int"/> endpoint and NaN becomes zero.</remarks>
    public int R8
    {
        readonly get => ToIntegerScale(R);
        set => R = value / 255f;
    }

    /// <summary>Gets or sets <see cref="G"/> on an integer scale where <c>255</c> represents <c>1</c>.</summary>
    /// <value>The rounded value of <see cref="G"/> multiplied by 255, or a value divided by 255 when set.</value>
    /// <remarks>Finite values are not clamped to byte range; overflow saturates to an <see cref="int"/> endpoint and NaN becomes zero.</remarks>
    public int G8
    {
        readonly get => ToIntegerScale(G);
        set => G = value / 255f;
    }

    /// <summary>Gets or sets <see cref="B"/> on an integer scale where <c>255</c> represents <c>1</c>.</summary>
    /// <value>The rounded value of <see cref="B"/> multiplied by 255, or a value divided by 255 when set.</value>
    /// <remarks>Finite values are not clamped to byte range; overflow saturates to an <see cref="int"/> endpoint and NaN becomes zero.</remarks>
    public int B8
    {
        readonly get => ToIntegerScale(B);
        set => B = value / 255f;
    }

    /// <summary>Gets or sets <see cref="A"/> on an integer scale where <c>255</c> represents <c>1</c>.</summary>
    /// <value>The rounded value of <see cref="A"/> multiplied by 255, or a value divided by 255 when set.</value>
    /// <remarks>Finite values are not clamped to byte range; overflow saturates to an <see cref="int"/> endpoint and NaN becomes zero.</remarks>
    public int A8
    {
        readonly get => ToIntegerScale(A);
        set => A = value / 255f;
    }

    /// <summary>Gets or sets the HSV hue, typically from <c>0</c> to <c>1</c>.</summary>
    /// <value>The hue of this color; achromatic colors report <c>0</c>.</value>
    /// <remarks>Setting the value reconstructs RGB through <see cref="FromHsv(float, float, float, float)"/> and preserves alpha.</remarks>
    public float H
    {
        readonly get
        {
            ToHsv(out var hue, out _, out _);
            return hue;
        }
        set => this = FromHsv(value, S, V, A);
    }

    /// <summary>Gets or sets the HSV saturation, typically from <c>0</c> to <c>1</c>.</summary>
    /// <value>The ratio of RGB chroma to the greatest RGB component, or <c>0</c> when the greatest component is zero.</value>
    /// <remarks>Setting the value reconstructs RGB through <see cref="FromHsv(float, float, float, float)"/> and preserves alpha.</remarks>
    public float S
    {
        readonly get
        {
            ToHsv(out _, out var saturation, out _);
            return saturation;
        }
        set => this = FromHsv(H, value, V, A);
    }

    /// <summary>Gets or sets the HSV value component, typically from <c>0</c> to <c>1</c>.</summary>
    /// <value>The greatest RGB component.</value>
    /// <remarks>Setting the value reconstructs RGB through <see cref="FromHsv(float, float, float, float)"/> and preserves alpha.</remarks>
    public float V
    {
        readonly get => Mathf.Max(R, Mathf.Max(G, B));
        set => this = FromHsv(H, S, value, A);
    }

    /// <summary>Gets or sets the perceptual OKHSL hue, from <c>0</c> to <c>1</c>.</summary>
    /// <value>The normalized perceptual hue; achromatic colors report <c>0</c>.</value>
    /// <remarks>Setting the value reconstructs RGB through <see cref="FromOkHsl(float, float, float, float)"/> and preserves alpha.</remarks>
    public float OkHslH
    {
        readonly get => GetOkHsl().H;
        set => this = FromOkHsl(value, OkHslS, OkHslL, A);
    }

    /// <summary>Gets or sets the perceptual OKHSL saturation, from <c>0</c> to <c>1</c>.</summary>
    /// <value>The normalized perceptual saturation; achromatic colors report <c>0</c>.</value>
    /// <remarks>Setting the value reconstructs RGB through <see cref="FromOkHsl(float, float, float, float)"/> and preserves alpha.</remarks>
    public float OkHslS
    {
        readonly get => GetOkHsl().S;
        set => this = FromOkHsl(OkHslH, value, OkHslL, A);
    }

    /// <summary>Gets or sets the perceptual OKHSL lightness, from <c>0</c> to <c>1</c>.</summary>
    /// <value>The normalized perceptual lightness.</value>
    /// <remarks>Setting the value reconstructs RGB through <see cref="FromOkHsl(float, float, float, float)"/> and preserves alpha.</remarks>
    public float OkHslL
    {
        readonly get => GetOkHsl().L;
        set => this = FromOkHsl(OkHslH, OkHslS, value, A);
    }

    /// <summary>Gets the relative light intensity of a linear-space RGB color.</summary>
    /// <value><c>0.2126 × R + 0.7152 × G + 0.0722 × B</c>; alpha is ignored.</value>
    /// <remarks>Call <see cref="SrgbToLinear"/> first when the stored RGB components are sRGB encoded.</remarks>
    public readonly float Luminance => (0.2126f * R) + (0.7152f * G) + (0.0722f * B);

    /// <summary>Gets or sets a component by RGBA index.</summary>
    /// <param name="index"><c>0</c> for red, <c>1</c> for green, <c>2</c> for blue, or <c>3</c> for alpha.</param>
    /// <value>The selected floating-point component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the range <c>0..3</c>.</exception>
    public float this[int index]
    {
        readonly get => index switch
        {
            0 => R,
            1 => G,
            2 => B,
            3 => A,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
        set
        {
            switch (index)
            {
                case 0:
                    R = value;
                    break;
                case 1:
                    G = value;
                    break;
                case 2:
                    B = value;
                    break;
                case 3:
                    A = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Initializes an RGBA color from floating-point components.</summary>
    /// <param name="r">The red component, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="g">The green component, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="b">The blue component, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="a">The linear alpha component, where <c>0</c> is transparent and <c>1</c> is opaque.</param>
    /// <remarks>Values are stored unchanged and are not clamped.</remarks>
    public Color(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>Initializes a color from another color's RGB components and a replacement alpha value.</summary>
    /// <param name="color">The color whose RGB components are copied.</param>
    /// <param name="alpha">The replacement alpha component.</param>
    /// <remarks>Omitting <paramref name="alpha"/> produces opaque output; ordinary assignment copies all four components.</remarks>
    public Color(Color color, float alpha = 1f)
        : this(color.R, color.G, color.B, alpha)
    {
    }

    /// <summary>Initializes a color from packed <c>0xRRGGBBAA</c> bytes.</summary>
    /// <param name="rgba">The packed 32-bit RGBA value.</param>
    public Color(uint rgba)
    {
        A = (rgba & 0xff) / 255f;
        B = ((rgba >> 8) & 0xff) / 255f;
        G = ((rgba >> 16) & 0xff) / 255f;
        R = ((rgba >> 24) & 0xff) / 255f;
    }

    /// <summary>Initializes a color from packed <c>0xRRRRGGGGBBBBAAAA</c> words.</summary>
    /// <param name="rgba">The packed 64-bit RGBA value.</param>
    public Color(ulong rgba)
    {
        A = (rgba & 0xffff) / 65535f;
        B = ((rgba >> 16) & 0xffff) / 65535f;
        G = ((rgba >> 32) & 0xffff) / 65535f;
        R = ((rgba >> 48) & 0xffff) / 65535f;
    }

    /// <summary>Initializes a color from an HTML hexadecimal code or a standard color name.</summary>
    /// <param name="code">A 3-, 4-, 6-, or 8-digit hexadecimal code with optional <c>#</c>, or a name from <see cref="Colors"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="code"/> is neither a valid hexadecimal code nor a known name.</exception>
    public Color(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        this = HtmlIsValid(code) ? FromHtml(code) : Named(code);
    }

    /// <summary>Initializes a color from an HTML hexadecimal code or standard name and replaces its alpha.</summary>
    /// <param name="code">A hexadecimal code or a name from <see cref="Colors"/>.</param>
    /// <param name="alpha">The replacement alpha component.</param>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="code"/> is neither a valid hexadecimal code nor a known name.</exception>
    public Color(string code, float alpha)
        : this(code)
    {
        A = alpha;
    }

    /// <summary>Blends <paramref name="over"/> as a foreground color over this background color.</summary>
    /// <param name="over">The straight-alpha foreground color.</param>
    /// <returns>The source-over composite. An exactly zero output alpha produces transparent black.</returns>
    public readonly Color Blend(Color over)
    {
        var backgroundWeight = 1f - over.A;
        var alpha = (A * backgroundWeight) + over.A;
        if (alpha == 0f)
            return default;

        return new Color(
            ((R * A * backgroundWeight) + (over.R * over.A)) / alpha,
            ((G * A * backgroundWeight) + (over.G * over.A)) / alpha,
            ((B * A * backgroundWeight) + (over.B * over.A)) / alpha,
            alpha);
    }

    /// <summary>Clamps each component between the corresponding components of two colors.</summary>
    /// <param name="min">The componentwise minimum, or transparent black when omitted.</param>
    /// <param name="max">The componentwise maximum, or opaque white when omitted.</param>
    /// <returns>A componentwise-clamped color.</returns>
    /// <exception cref="ArgumentException">A minimum component is greater than its matching maximum component.</exception>
    public readonly Color Clamp(Color? min = null, Color? max = null)
    {
        var minimum = min ?? default;
        var maximum = max ?? Colors.White;
        return new Color(
            Mathf.Clamp(R, minimum.R, maximum.R),
            Mathf.Clamp(G, minimum.G, maximum.G),
            Mathf.Clamp(B, minimum.B, maximum.B),
            Mathf.Clamp(A, minimum.A, maximum.A));
    }

    /// <summary>Darkens the RGB components by a ratio while preserving alpha.</summary>
    /// <param name="amount">The darkening ratio, normally from <c>0</c> to <c>1</c>.</param>
    /// <returns>A color whose RGB components are multiplied by <c>1 - amount</c>.</returns>
    /// <remarks>The ratio is not clamped and therefore supports extrapolation.</remarks>
    public readonly Color Darkened(float amount) => new(R * (1f - amount), G * (1f - amount), B * (1f - amount), A);

    /// <summary>Inverts the RGB components while preserving alpha.</summary>
    /// <returns><c>(1 - R, 1 - G, 1 - B, A)</c>.</returns>
    public readonly Color Inverted() => new(1f - R, 1f - G, 1f - B, A);

    /// <summary>Lightens the RGB components toward one by a ratio while preserving alpha.</summary>
    /// <param name="amount">The lightening ratio, normally from <c>0</c> to <c>1</c>.</param>
    /// <returns>A color linearly moved toward white in RGB space.</returns>
    /// <remarks>The ratio is not clamped and therefore supports extrapolation.</remarks>
    public readonly Color Lightened(float amount) => new(
        R + ((1f - R) * amount),
        G + ((1f - G) * amount),
        B + ((1f - B) * amount),
        A);

    /// <summary>Linearly interpolates every component toward another color.</summary>
    /// <param name="to">The destination color.</param>
    /// <param name="weight">The interpolation weight, normally from <c>0</c> to <c>1</c>.</param>
    /// <returns>The componentwise interpolation.</returns>
    /// <remarks>The weight is not clamped and therefore supports extrapolation.</remarks>
    public readonly Color Lerp(Color to, float weight) => new(
        R + ((to.R - R) * weight),
        G + ((to.G - G) * weight),
        B + ((to.B - B) * weight),
        A + ((to.A - A) * weight));

    /// <summary>Converts linear RGB components to nonlinear sRGB while preserving alpha.</summary>
    /// <returns>The sRGB-encoded color.</returns>
    public readonly Color LinearToSrgb() => new(
        LinearChannelToSrgb(R),
        LinearChannelToSrgb(G),
        LinearChannelToSrgb(B),
        A);

    /// <summary>Converts nonlinear sRGB components to linear RGB while preserving alpha.</summary>
    /// <returns>The linear-space color.</returns>
    public readonly Color SrgbToLinear() => new(
        SrgbChannelToLinear(R),
        SrgbChannelToLinear(G),
        SrgbChannelToLinear(B),
        A);

    /// <summary>Packs the color into <c>0xAABBGGRR</c>.</summary>
    /// <returns>An unsigned 32-bit ABGR value with one rounded byte per component.</returns>
    /// <remarks>Components are clamped to <c>0..1</c>; NaN becomes zero.</remarks>
    public readonly uint ToAbgr32() => Pack32(A, B, G, R);

    /// <summary>Packs the color into <c>0xAAAABBBBGGGGRRRR</c>.</summary>
    /// <returns>An unsigned 64-bit ABGR value with one rounded word per component.</returns>
    /// <remarks>Components are clamped to <c>0..1</c>; NaN becomes zero.</remarks>
    public readonly ulong ToAbgr64() => Pack64(A, B, G, R);

    /// <summary>Packs the color into <c>0xAARRGGBB</c>.</summary>
    /// <returns>An unsigned 32-bit ARGB value with one rounded byte per component.</returns>
    /// <remarks>Components are clamped to <c>0..1</c>; NaN becomes zero.</remarks>
    public readonly uint ToArgb32() => Pack32(A, R, G, B);

    /// <summary>Packs the color into <c>0xAAAARRRRGGGGBBBB</c>.</summary>
    /// <returns>An unsigned 64-bit ARGB value with one rounded word per component.</returns>
    /// <remarks>Components are clamped to <c>0..1</c>; NaN becomes zero.</remarks>
    public readonly ulong ToArgb64() => Pack64(A, R, G, B);

    /// <summary>Packs the color into <c>0xRRGGBBAA</c>.</summary>
    /// <returns>An unsigned 32-bit RGBA value with one rounded byte per component.</returns>
    /// <remarks>Components are clamped to <c>0..1</c>; NaN becomes zero.</remarks>
    public readonly uint ToRgba32() => Pack32(R, G, B, A);

    /// <summary>Packs the color into <c>0xRRRRGGGGBBBBAAAA</c>.</summary>
    /// <returns>An unsigned 64-bit RGBA value with one rounded word per component.</returns>
    /// <remarks>Components are clamped to <c>0..1</c>; NaN becomes zero.</remarks>
    public readonly ulong ToRgba64() => Pack64(R, G, B, A);

    /// <summary>Formats the color as lowercase hexadecimal RGBA or RGB without a leading hash sign.</summary>
    /// <param name="includeAlpha">Whether to append the alpha byte.</param>
    /// <returns>Six or eight hexadecimal digits. Each component is clamped to <c>0..1</c> and rounded to a byte.</returns>
    public readonly string ToHtml(bool includeAlpha = true)
    {
        Span<char> text = stackalloc char[includeAlpha ? 8 : 6];
        WriteHexByte(text, 0, R);
        WriteHexByte(text, 2, G);
        WriteHexByte(text, 4, B);
        if (includeAlpha)
            WriteHexByte(text, 6, A);

        return new string(text);
    }

    /// <summary>Parses a color from HTML-style hexadecimal RGB or RGBA text.</summary>
    /// <param name="rgba">Three, four, six, or eight hexadecimal digits, optionally prefixed by one <c>#</c>.</param>
    /// <returns>The parsed color; formats without alpha produce an alpha value of <c>1</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length or a character is invalid.</exception>
    /// <remarks>An empty span returns opaque black for parity with the typed API contract; <see cref="HtmlIsValid"/> still reports it as invalid.</remarks>
    public static Color FromHtml(ReadOnlySpan<char> rgba)
    {
        if (rgba.IsEmpty)
            return Colors.Black;

        if (rgba[0] == '#')
            rgba = rgba[1..];

        var shorthand = rgba.Length is 3 or 4;
        var hasAlpha = rgba.Length is 4 or 8;
        if (rgba.Length is not (3 or 4 or 6 or 8))
            throw new ArgumentOutOfRangeException(nameof(rgba), $"The hexadecimal color has invalid length {rgba.Length}.");

        var divisor = shorthand ? 15f : 255f;
        var stride = shorthand ? 1 : 2;
        var red = ParseHex(rgba, 0, shorthand);
        var green = ParseHex(rgba, stride, shorthand);
        var blue = ParseHex(rgba, stride * 2, shorthand);
        var alpha = hasAlpha ? ParseHex(rgba, stride * 3, shorthand) : (int)divisor;
        if (red < 0 || green < 0 || blue < 0 || alpha < 0)
            throw new ArgumentOutOfRangeException(nameof(rgba), "The color contains a non-hexadecimal character.");

        return new Color(red / divisor, green / divisor, blue / divisor, alpha / divisor);
    }

    /// <summary>Constructs a color from 8-bit integer components.</summary>
    /// <param name="r8">The red byte.</param>
    /// <param name="g8">The green byte.</param>
    /// <param name="b8">The blue byte.</param>
    /// <param name="a8">The alpha byte.</param>
    /// <returns>The components divided by 255.</returns>
    public static Color Color8(byte r8, byte g8, byte b8, byte a8 = byte.MaxValue) =>
        new(r8 / 255f, g8 / 255f, b8 / 255f, a8 / 255f);

    /// <summary>Constructs a color from HSV components.</summary>
    /// <param name="hue">The hue, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="saturation">The saturation, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="value">The value or brightness, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="alpha">The alpha component, typically from <c>0</c> to <c>1</c>.</param>
    /// <returns>The equivalent RGBA color.</returns>
    /// <remarks>Inputs are not clamped. Hue is periodic for ordinary nonnegative values.</remarks>
    public static Color FromHsv(float hue, float saturation, float value, float alpha = 1f)
    {
        if (saturation == 0f)
            return new Color(value, value, value, alpha);

        hue = (hue * 6f) % 6f;
        var sector = (int)hue;
        var fraction = hue - sector;
        var low = value * (1f - saturation);
        var falling = value * (1f - (saturation * fraction));
        var rising = value * (1f - (saturation * (1f - fraction)));
        return sector switch
        {
            0 => new Color(value, rising, low, alpha),
            1 => new Color(falling, value, low, alpha),
            2 => new Color(low, value, rising, alpha),
            3 => new Color(low, falling, value, alpha),
            4 => new Color(rising, low, value, alpha),
            _ => new Color(value, low, falling, alpha),
        };
    }

    /// <summary>Computes this color's HSV components in one pass.</summary>
    /// <param name="hue">Receives the hue, or <c>0</c> for an achromatic color.</param>
    /// <param name="saturation">Receives the saturation.</param>
    /// <param name="value">Receives the greatest RGB component.</param>
    public readonly void ToHsv(out float hue, out float saturation, out float value)
    {
        var maximum = Mathf.Max(R, Mathf.Max(G, B));
        var minimum = Mathf.Min(R, Mathf.Min(G, B));
        var delta = maximum - minimum;
        if (delta == 0f)
        {
            hue = 0f;
        }
        else
        {
            hue = R == maximum
                ? (G - B) / delta
                : G == maximum
                    ? 2f + ((B - R) / delta)
                    : 4f + ((R - G) / delta);
            hue /= 6f;
            if (hue < 0f)
                hue += 1f;
        }

        saturation = maximum == 0f ? 0f : 1f - (minimum / maximum);
        value = maximum;
    }

    /// <summary>Constructs a color from perceptually uniform OKHSL components.</summary>
    /// <param name="hue">The perceptual hue, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="saturation">The perceptual saturation, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="lightness">The perceptual lightness, typically from <c>0</c> to <c>1</c>.</param>
    /// <param name="alpha">The alpha component, typically from <c>0</c> to <c>1</c>.</param>
    /// <returns>The equivalent sRGB color, componentwise clamped to <c>0..1</c>, including alpha.</returns>
    public static Color FromOkHsl(float hue, float saturation, float lightness, float alpha = 1f)
    {
        var rgb = OkColor.ToSrgb(hue, saturation, lightness);
        return new Color(
            ClampOkComponent(rgb.R),
            ClampOkComponent(rgb.G),
            ClampOkComponent(rgb.B),
            ClampOkComponent(alpha));
    }

    /// <summary>Decodes a shared-exponent RGBE9995 value.</summary>
    /// <param name="rgbe">Nine-bit red, green, and blue mantissas followed by a five-bit exponent.</param>
    /// <returns>The decoded linear RGB color with opaque alpha.</returns>
    public static Color FromRgbe9995(uint rgbe)
    {
        var multiplier = Mathf.Pow(2f, ((rgbe >> 27) & 0x1f) - 24f);
        return new Color(
            (rgbe & 0x1ff) * multiplier,
            ((rgbe >> 9) & 0x1ff) * multiplier,
            ((rgbe >> 18) & 0x1ff) * multiplier);
    }

    /// <summary>Parses hexadecimal text or a standard name, returning a fallback on failure.</summary>
    /// <param name="text">The candidate hexadecimal code or color name.</param>
    /// <param name="defaultColor">The value returned when no color matches.</param>
    /// <returns>The parsed color or <paramref name="defaultColor"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static Color FromString(string text, Color defaultColor)
    {
        ArgumentNullException.ThrowIfNull(text);
        return HtmlIsValid(text) ? FromHtml(text) : Named(text, defaultColor);
    }

    /// <summary>Tests whether a span is valid HTML-style hexadecimal color text.</summary>
    /// <param name="color">The candidate text.</param>
    /// <returns><see langword="true"/> for 3, 4, 6, or 8 hexadecimal digits with an optional leading <c>#</c>; otherwise <see langword="false"/>.</returns>
    /// <remarks>Whitespace and multiple hash signs are not accepted.</remarks>
    public static bool HtmlIsValid(ReadOnlySpan<char> color)
    {
        if (color.IsEmpty)
            return false;

        if (color[0] == '#')
            color = color[1..];

        if (color.Length is not (3 or 4 or 6 or 8))
            return false;

        foreach (var character in color)
        {
            if (HexValue(character) < 0)
                return false;
        }

        return true;
    }

    /// <summary>Adds matching components.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns>The componentwise sum.</returns>
    public static Color operator +(Color left, Color right) =>
        new(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);

    /// <summary>Returns a color unchanged.</summary>
    /// <param name="color">The color.</param>
    /// <returns><paramref name="color"/>.</returns>
    public static Color operator +(Color color) => color;

    /// <summary>Subtracts matching components.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The componentwise difference.</returns>
    public static Color operator -(Color left, Color right) =>
        new(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);

    /// <summary>Complements all four components.</summary>
    /// <param name="color">The color to complement.</param>
    /// <returns><c>(1 - R, 1 - G, 1 - B, 1 - A)</c>.</returns>
    public static Color operator -(Color color) => Colors.White - color;

    /// <summary>Multiplies every component by a scalar.</summary>
    /// <param name="color">The color.</param>
    /// <param name="scale">The scalar multiplier.</param>
    /// <returns>The scaled color.</returns>
    public static Color operator *(Color color, float scale) =>
        new(color.R * scale, color.G * scale, color.B * scale, color.A * scale);

    /// <summary>Multiplies every component by a scalar.</summary>
    /// <param name="scale">The scalar multiplier.</param>
    /// <param name="color">The color.</param>
    /// <returns>The scaled color.</returns>
    public static Color operator *(float scale, Color color) => color * scale;

    /// <summary>Multiplies matching components.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns>The componentwise product.</returns>
    public static Color operator *(Color left, Color right) =>
        new(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);

    /// <summary>Divides every component by a scalar using IEEE 754 floating-point semantics.</summary>
    /// <param name="color">The dividend color.</param>
    /// <param name="scale">The scalar divisor.</param>
    /// <returns>The scaled quotient; zero divisors can produce infinities or NaN.</returns>
    public static Color operator /(Color color, float scale) =>
        new(color.R / scale, color.G / scale, color.B / scale, color.A / scale);

    /// <summary>Divides matching components using IEEE 754 floating-point semantics.</summary>
    /// <param name="left">The dividend color.</param>
    /// <param name="right">The divisor color.</param>
    /// <returns>The componentwise quotient; zero divisors can produce infinities or NaN.</returns>
    public static Color operator /(Color left, Color right) =>
        new(left.R / right.R, left.G / right.G, left.B / right.B, left.A / right.A);

    /// <summary>Tests all components for exact floating-point equality.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when every component is exactly equal.</returns>
    public static bool operator ==(Color left, Color right) => left.Equals(right);

    /// <summary>Tests whether any component differs under exact floating-point equality.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when at least one component differs.</returns>
    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    /// <summary>Compares colors lexicographically in red, green, blue, alpha order.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Color left, Color right) => IsLessThan(left, right);

    /// <summary>Compares colors lexicographically in red, green, blue, alpha order.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Color left, Color right) => IsGreaterThan(left, right);

    /// <summary>Compares colors lexicographically in red, green, blue, alpha order.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Color left, Color right) => left == right || left < right;

    /// <summary>Compares colors lexicographically in red, green, blue, alpha order.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Color left, Color right) => left == right || left > right;

    /// <summary>Tests whether another object is an exactly equal color.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a color with exactly equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Color other && Equals(other);

    /// <summary>Tests all components for exact floating-point equality.</summary>
    /// <param name="other">The other color.</param>
    /// <returns><see langword="true"/> when every component is exactly equal.</returns>
    public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <summary>Tests all components for scale-aware approximate equality.</summary>
    /// <param name="other">The other color.</param>
    /// <returns><see langword="true"/> when every component is within the scale-aware <see cref="Mathf.Epsilon"/> tolerance.</returns>
    public readonly bool IsEqualApprox(Color other) =>
        Mathf.IsEqualApprox(R, other.R) &&
        Mathf.IsEqualApprox(G, other.G) &&
        Mathf.IsEqualApprox(B, other.B) &&
        Mathf.IsEqualApprox(A, other.A);

    /// <summary>Returns a hash code based on all four components.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <summary>Formats all four components using invariant culture.</summary>
    /// <returns>A string in the form <c>(R, G, B, A)</c>.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats all four components using a numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format string, or <see langword="null"/> for the default format.</param>
    /// <returns>A string in the form <c>(R, G, B, A)</c>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) =>
        $"({R.ToString(format, CultureInfo.InvariantCulture)}, {G.ToString(format, CultureInfo.InvariantCulture)}, {B.ToString(format, CultureInfo.InvariantCulture)}, {A.ToString(format, CultureInfo.InvariantCulture)})";

    private readonly OkColor.Hsl GetOkHsl()
    {
        var hsl = OkColor.FromSrgb(R, G, B);
        return new OkColor.Hsl(
            ClampOkComponent(hsl.H),
            ClampOkComponent(hsl.S),
            ClampOkComponent(hsl.L));
    }

    private static float ClampOkComponent(float value) => Mathf.IsNaN(value) ? 0f : Mathf.Clamp(value, 0f, 1f);

    private static int ToIntegerScale(float component)
    {
        if (Mathf.IsNaN(component))
            return 0;

        var scaled = Mathf.Round(component * 255f);
        if (scaled >= int.MaxValue)
            return int.MaxValue;
        if (scaled <= int.MinValue)
            return int.MinValue;

        return (int)scaled;
    }

    private static float LinearChannelToSrgb(float channel) =>
        channel < 0.0031308f ? 12.92f * channel : (1.055f * Mathf.Pow(channel, 1f / 2.4f)) - 0.055f;

    private static float SrgbChannelToLinear(float channel) =>
        channel < 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);

    private static uint Pack32(float first, float second, float third, float fourth) =>
        ((uint)ToByte(first) << 24) | ((uint)ToByte(second) << 16) | ((uint)ToByte(third) << 8) | ToByte(fourth);

    private static ulong Pack64(float first, float second, float third, float fourth) =>
        ((ulong)ToWord(first) << 48) | ((ulong)ToWord(second) << 32) | ((ulong)ToWord(third) << 16) | ToWord(fourth);

    private static byte ToByte(float component)
    {
        if (Mathf.IsNaN(component) || component <= 0f)
            return 0;
        if (component >= 1f)
            return byte.MaxValue;

        return (byte)Mathf.Round(component * byte.MaxValue);
    }

    private static ushort ToWord(float component)
    {
        if (Mathf.IsNaN(component) || component <= 0f)
            return 0;
        if (component >= 1f)
            return ushort.MaxValue;

        return (ushort)Mathf.Round(component * ushort.MaxValue);
    }

    private static void WriteHexByte(Span<char> destination, int offset, float component)
    {
        const string digits = "0123456789abcdef";
        var value = ToByte(component);
        destination[offset] = digits[value >> 4];
        destination[offset + 1] = digits[value & 0xf];
    }

    private static int ParseHex(ReadOnlySpan<char> text, int offset, bool shorthand)
    {
        var high = HexValue(text[offset]);
        if (shorthand || high < 0)
            return high;

        var low = HexValue(text[offset + 1]);
        return low < 0 ? -1 : (high * 16) + low;
    }

    private static int HexValue(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };

    private static Color Named(string name)
    {
        if (Colors.TryGetNamed(name, out var color))
            return color;

        throw new ArgumentOutOfRangeException(nameof(name), name, "No standard color has this name.");
    }

    private static Color Named(string name, Color defaultColor) =>
        Colors.TryGetNamed(name, out var color) ? color : defaultColor;

    private static bool IsLessThan(Color left, Color right)
    {
        if (left.R == right.R)
        {
            if (left.G == right.G)
            {
                if (left.B == right.B)
                    return left.A < right.A;

                return left.B < right.B;
            }

            return left.G < right.G;
        }

        return left.R < right.R;
    }

    private static bool IsGreaterThan(Color left, Color right)
    {
        if (left.R == right.R)
        {
            if (left.G == right.G)
            {
                if (left.B == right.B)
                    return left.A > right.A;

                return left.B > right.B;
            }

            return left.G > right.G;
        }

        return left.R > right.R;
    }

}
