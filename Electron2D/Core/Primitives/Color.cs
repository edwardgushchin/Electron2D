// FILE: Electron2D/Core/Primitives/Color.cs
namespace Electron2D;

public struct Color
{
    private uint _color;

    public Color(uint color) => _color = color;

    public Color(byte red, byte green, byte blue)
        : this(red, green, blue, 255)
    {
    }

    public Color(byte red, byte green, byte blue, byte alpha)
    {
        _color =
            ((uint)red   << 24) |
            ((uint)green << 16) |
            ((uint)blue  <<  8) |
            ((uint)alpha <<  0);
    }

    public Color(float red, float green, float blue, float alpha = 1f)
    {
        static byte ToByte(float v)
        {
            if (float.IsNaN(v)) v = 0f;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            return (byte)(v * 255f + 0.5f);
        }

        var r = ToByte(red);
        var g = ToByte(green);
        var b = ToByte(blue);
        var a = ToByte(alpha);

        _color =
            ((uint)r << 24) |
            ((uint)g << 16) |
            ((uint)b <<  8) |
            ((uint)a <<  0);
    }

    public byte Red   => (byte)((_color >> 24) & 0xFF);
    public byte Green => (byte)((_color >> 16) & 0xFF);
    public byte Blue  => (byte)((_color >>  8) & 0xFF);
    public byte Alpha => (byte)((_color >>  0) & 0xFF);
}