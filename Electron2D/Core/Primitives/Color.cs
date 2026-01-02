namespace Electron2D;

/// <summary>
/// Упакованный цвет в формате 0xRRGGBBAA.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    #region Instance fields
    private readonly uint _value;
    #endregion

    #region Constructors
    /// <summary>
    /// Создаёт цвет из упакованного значения 0xRRGGBBAA.
    /// </summary>
    public Color(uint value) => _value = value;

    /// <summary>
    /// Создаёт цвет из RGB-компонент (alpha = 255).
    /// </summary>
    public Color(byte red, byte green, byte blue)
        : this(red, green, blue, 255)
    {
    }

    /// <summary>
    /// Создаёт цвет из RGBA-компонент.
    /// </summary>
    public Color(byte red, byte green, byte blue, byte alpha)
    {
        _value =
            ((uint)red << 24) |
            ((uint)green << 16) |
            ((uint)blue << 8) |
            alpha;
    }

    /// <summary>
    /// Создаёт цвет из float-компонент в диапазоне [0..1] (значения будут clamped).
    /// </summary>
    public Color(float red, float green, float blue, float alpha = 1f)
    {
        var r = ToByteClamped(red);
        var g = ToByteClamped(green);
        var b = ToByteClamped(blue);
        var a = ToByteClamped(alpha);

        _value =
            ((uint)r << 24) |
            ((uint)g << 16) |
            ((uint)b << 8) |
            a;
    }
    #endregion

    #region Properties
    /// <summary>Упакованное значение 0xRRGGBBAA.</summary>
    public uint Value => _value;

    public byte Red => (byte)((_value >> 24) & 0xFF);
    public byte Green => (byte)((_value >> 16) & 0xFF);
    public byte Blue => (byte)((_value >> 8) & 0xFF);
    public byte Alpha => (byte)(_value & 0xFF);
    #endregion

    #region Public API
    /// <summary>
    /// Возвращает новый цвет с добавлением <paramref name="delta"/> к компонентам RGB
    /// (с насыщением в диапазоне 0..255). Alpha не изменяется.
    /// </summary>
    public Color AddRGB(int delta)
    {
        var r = Red + delta;
        r = r switch
        {
            < 0 => 0,
            > 255 => 255,
            _ => r
        };

        var g = Green + delta;
        g = g switch
        {
            < 0 => 0,
            > 255 => 255,
            _ => g
        };

        var b = Blue + delta;
        b = b switch
        {
            < 0 => 0,
            > 255 => 255,
            _ => b
        };

        return new Color((uint)((r << 24) | (g << 16) | (b << 8) | Alpha));
    }

    public bool Equals(Color other) => _value == other._value;

    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(Color left, Color right) => left.Equals(right);

    public static bool operator !=(Color left, Color right) => !left.Equals(right);
    #endregion

    #region Private helpers
    private static byte ToByteClamped(float v)
    {
        if (float.IsNaN(v)) v = 0f;

        v = v switch
        {
            < 0f => 0f,
            > 1f => 1f,
            _ => v
        };

        return (byte)(v * 255f + 0.5f);
    }
    #endregion
}