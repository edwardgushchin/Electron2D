namespace Electron2D.Graphics;

public readonly struct Color : IEquatable<Color>
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; }

    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color operator +(Color a, Color b) =>
        new(
            (byte)Math.Min(a.R + b.R, 255),
            (byte)Math.Min(a.G + b.G, 255),
            (byte)Math.Min(a.B + b.B, 255),
            (byte)Math.Min(a.A + b.A, 255)
        );

    public static Color operator -(Color a, Color b) =>
        new(
            (byte)Math.Max(a.R - b.R, 0),
            (byte)Math.Max(a.G - b.G, 0),
            (byte)Math.Max(a.B - b.B, 0),
            (byte)Math.Max(a.A - b.A, 0)
        );
    
    public static Color operator *(Color a, byte b) =>
        new(
            (byte)Math.Min(a.R * b, 255),
            (byte)Math.Min(a.G * b, 255),
            (byte)Math.Min(a.B * b, 255),
            (byte)Math.Min(a.A * b, 255)
        );
    
    public static Color operator /(Color a, byte b) =>
        b == 0
            ? new Color(0,0,0,0)
            : new Color(
                (byte)(a.R / b),
                (byte)(a.G / b),
                (byte)(a.B / b),
                (byte)(a.A / b)
            );
    
    #region Colors
    public static Color Transparent => new Color(0, 0, 0, 0);
    
    public static Color Black => new Color(0, 0, 0, 255);
    
    public static Color White => new Color(255, 255, 255, 255);
    
    public static Color Red => new Color(255, 0, 0, 255);
    
    public static Color Lime => new Color(0, 255, 0, 255);
    
    public static Color Blue => new Color(0, 0, 255, 255);
    
    public static Color Yellow => new Color(255, 255, 0, 255);
    public static Color Cyan => new Color(0, 255, 255, 255);
    public static Color Magenta => new Color(255, 0, 255, 255);
    public static Color Silver => new Color(192, 192, 192, 255);
    public static Color Gray => new Color(128, 128, 128, 255);
    
    public static Color Maroon => new Color(128, 0, 0, 255);
    
    public static Color Olive => new Color(128, 128, 0, 255);
    
    public static Color Green => new Color(0, 128, 0, 255);
    
    public static Color Purple => new Color(128, 0, 128, 255);
    
    public static Color Teal => new Color(0, 128, 128, 255);
    
    public static Color Navy => new Color(0, 0, 128, 255);
    
    public static Color AliceBlue => new Color(240, 248, 255, 255);
    
    public static Color AntiqueWhite => new Color(250, 235, 215, 255);
    
    public static Color Aqua => new Color(0, 255, 255, 255);
    
    public static Color Aquamarine => new Color(127, 255, 212, 255);
    
    public static Color Azure => new Color(240, 255, 255, 255);
    
    public static Color Beige => new Color(245, 245, 220, 255);
    
    public static Color Bisque => new Color(255, 228, 196, 255);
    
    public static Color BlanchedAlmond => new Color(255, 235, 205, 255);
    
    public static Color BlueViolet => new Color(138, 43, 226, 255);
    
    public static Color Brown => new Color(165, 42, 42, 255);
    
    public static Color BurlyWood => new Color(222, 184, 135, 255);
    
    public static Color CadetBlue => new Color(95, 158, 160, 255);
    
    public static Color Chartreuse => new Color(127, 255, 0, 255);
    
    public static Color Chocolate => new Color(210, 105, 30, 255);
    
    public static Color Coral => new Color(255, 127, 80, 255);
    
    public static Color CornflowerBlue => new Color(100, 149, 237, 255);
    
    public static Color Cornsilk => new Color(255, 248, 220, 255);
    
    public static Color Crimson => new Color(220, 20, 60, 255);
    
    public static Color DarkBlue => new Color(0, 0, 139, 255);
    
    public static Color DarkCyan => new Color(0, 139, 139, 255);
    
    public static Color DarkGoldenrod => new Color(184, 134, 11, 255);
    
    public static Color DarkGray => new Color(169, 169, 169, 255);
    
    public static Color DarkGreen => new Color(0, 100, 0, 255);
    
    public static Color DarkKhaki => new Color(189, 183, 107, 255);
    
    public static Color DarkMagenta => new Color(139, 0, 139, 255);
    
    public static Color DarkOliveGreen => new Color(85, 107, 47, 255);
    
    public static Color DarkOrange => new Color(255, 140, 0, 255);
    
    public static Color DarkOrchid => new Color(153, 50, 204, 255);
    
    public static Color DarkRed => new Color(139, 0, 0, 255);
    
    public static Color DarkSalmon => new Color(233, 150, 122, 255);
    
    public static Color DarkSeaGreen => new Color(143, 188, 143, 255);
    
    public static Color DarkSlateBlue => new Color(72, 61, 139, 255);
    
    public static Color DarkSlateGray => new Color(47, 79, 79, 255);
    
    public static Color DarkTurquoise => new Color(0, 206, 209, 255);
    
    public static Color DarkViolet => new Color(148, 0, 211, 255);
    
    public static Color DeepPink => new Color(255, 20, 147, 255);
    
    public static Color DeepSkyBlue => new Color(0, 191, 255, 255);
    
    public static Color DimGray => new Color(105, 105, 105, 255);
    
    public static Color DodgerBlue => new Color(30, 144, 255, 255);
    
    public static Color Firebrick => new Color(178, 34, 34, 255);
    
    public static Color FloralWhite => new Color(255, 250, 240, 255);
    
    public static Color ForestGreen => new Color(34, 139, 34, 255);
    
    public static Color Fuchsia => new Color(255, 0, 255, 255);
    
    public static Color Gainsboro => new Color(220, 220, 220, 255);
    
    public static Color GhostWhite => new Color(248, 248, 255, 255);
    
    public static Color Gold => new Color(255, 215, 0, 255);
    
    public static Color Goldenrod => new Color(218, 165, 32, 255);

    public static Color Indigo => new Color(75, 0, 130, 255);
    
    public static Color Ivory => new Color(255, 255, 240, 255);
    
    public static Color Khaki => new Color(240, 230, 140, 255);
    
    public static Color Lavender => new Color(230, 230, 250, 255);
    
    public static Color LavenderBlush => new Color(255, 240, 245, 255);
    
    public static Color LawnGreen => new Color(124, 252, 0, 255);
    
    public static Color LemonChiffon => new Color(255, 250, 205, 255);
    
    public static Color LightBlue => new Color(173, 216, 230, 255);
    
    public static Color LightCoral => new Color(240, 128, 128, 255);
    
    public static Color LightCyan => new Color(224, 255, 255, 255);
    
    public static Color LightGoldenrodYellow => new Color(250, 250, 210, 255);
    
    public static Color LightGray => new Color(211, 211, 211, 255);
    
    public static Color LightGreen => new Color(144, 238, 144, 255);
    
    public static Color LightPink => new Color(255, 182, 193, 255);
    
    public static Color LightSalmon => new Color(255, 160, 122, 255);
    
    public static Color LightSeaGreen => new Color(32, 178, 170, 255);
    
    public static Color LightSkyBlue => new Color(135, 206, 250, 255);
    
    public static Color LightSlateGray => new Color(119, 136, 153, 255);
    
    public static Color LightSteelBlue => new Color(176, 196, 222, 255);
    
    public static Color LightYellow => new Color(255, 255, 224, 255);
    
    public static Color LimeGreen => new Color(50, 205, 50, 255);
    
    public static Color Linen => new Color(250, 240, 230, 255);
    
    public static Color MediumAquamarine => new Color(102, 205, 170, 255);
    
    public static Color MediumBlue => new Color(0, 0, 205, 255);
    
    public static Color MediumOrchid => new Color(186, 85, 211, 255);
    
    public static Color MediumPurple => new Color(147, 112, 219, 255);
    
    public static Color MediumSeaGreen => new Color(60, 179, 113, 255);
    
    public static Color MediumSlateBlue => new Color(123, 104, 238, 255);
    
    public static Color MediumSpringGreen => new Color(0, 250, 154, 255);
    
    public static Color MediumTurquoise => new Color(72, 209, 204, 255);
    
    public static Color MediumVioletRed => new Color(199, 21, 133, 255);
    
    public static Color MidnightBlue => new Color(25, 25, 112, 255);
    
    public static Color MintCream => new Color(245, 255, 250, 255);
    
    public static Color MistyRose => new Color(255, 228, 225, 255);
    
    public static Color Moccasin => new Color(255, 228, 181, 255);
    
    public static Color NavajoWhite => new Color(255, 222, 173, 255);
    
    public static Color OldLace => new Color(253, 245, 230, 255);
    
    public static Color Orange => new Color(255, 165, 0, 255);
    
    public static Color OrangeRed => new Color(255, 69, 0, 255);
    
    public static Color Orchid => new Color(218, 112, 214, 255);
    
    public static Color PaleGoldenrod => new Color(238, 232, 170, 255);
    
    public static Color PaleGreen => new Color(152, 251, 152, 255);
    
    public static Color PaleTurquoise => new Color(175, 238, 238, 255);
    
    public static Color PaleVioletRed => new Color(219, 112, 147, 255);
    
    public static Color PapayaWhip => new Color(255, 239, 213, 255);
    
    public static Color PeachPuff => new Color(255, 218, 185, 255);
    
    public static Color Peru => new Color(205, 133, 63, 255);
    
    public static Color Pink => new Color(255, 192, 203, 255);
    
    public static Color Plum => new Color(221, 160, 221, 255);
    
    public static Color PowderBlue => new Color(176, 224, 230, 255);
    
    public static Color RebeccaPurple => new Color(102, 51, 153, 255);
    #endregion
    
    public static bool operator ==(Color left, Color right) => 
        left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;
    
    public static bool operator !=(Color left, Color right) => !(left == right);

    public bool Equals(Color left, Color right)
    {
        return left == right;
    }

    public override bool Equals(object? obj)
    {
        return obj is Color other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public bool Equals(Color other)
    {
        return this == other;
    }
}