/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Globalization;

namespace Electron2D;

public struct Color : IEquatable<Color>
{
    public static readonly Color Black = new(0f, 0f, 0f, 1f);
    public static readonly Color White = new(1f, 1f, 1f, 1f);
    public static readonly Color Transparent = new(0f, 0f, 0f, 0f);

    public Color(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public float R { get; set; }

    public float G { get; set; }

    public float B { get; set; }

    public float A { get; set; }

    public Color Lerp(Color to, float weight)
    {
        return new Color(
            Mathf.Lerp(R, to.R, weight),
            Mathf.Lerp(G, to.G, weight),
            Mathf.Lerp(B, to.B, weight),
            Mathf.Lerp(A, to.A, weight));
    }

    public Color Clamp(Color min, Color max)
    {
        return new Color(
            Mathf.Clamp(R, min.R, max.R),
            Mathf.Clamp(G, min.G, max.G),
            Mathf.Clamp(B, min.B, max.B),
            Mathf.Clamp(A, min.A, max.A));
    }

    public Color Lightened(float amount)
    {
        return new Color(
            Mathf.Lerp(R, 1f, amount),
            Mathf.Lerp(G, 1f, amount),
            Mathf.Lerp(B, 1f, amount),
            A);
    }

    public Color Darkened(float amount)
    {
        return new Color(R * (1f - amount), G * (1f - amount), B * (1f - amount), A);
    }

    public bool IsEqualApprox(Color color)
    {
        return Mathf.IsEqualApprox(R, color.R) &&
            Mathf.IsEqualApprox(G, color.G) &&
            Mathf.IsEqualApprox(B, color.B) &&
            Mathf.IsEqualApprox(A, color.A);
    }

    public string ToHtml(bool includeAlpha = true)
    {
        var result = $"{ToByte(R):x2}{ToByte(G):x2}{ToByte(B):x2}";
        return includeAlpha ? $"{result}{ToByte(A):x2}" : result;
    }

    public static Color FromHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var text = html.StartsWith('#') ? html[1..] : html;
        if (text.Length != 6 && text.Length != 8)
        {
            throw new FormatException("HTML color must be RRGGBB or RRGGBBAA.");
        }

        try
        {
            var r = FromByte(text[0..2]);
            var g = FromByte(text[2..4]);
            var b = FromByte(text[4..6]);
            var a = text.Length == 8 ? FromByte(text[6..8]) : 1f;
            return new Color(r, g, b, a);
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new FormatException("HTML color contains invalid hexadecimal digits.", exception);
        }
    }

    public bool Equals(Color other)
    {
        return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
    }

    public override bool Equals(object? obj)
    {
        return obj is Color other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public override string ToString()
    {
        return $"({MathFormatting.Format(R)}, {MathFormatting.Format(G)}, {MathFormatting.Format(B)}, {MathFormatting.Format(A)})";
    }

    public static Color operator +(Color left, Color right)
    {
        return new Color(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
    }

    public static Color operator -(Color left, Color right)
    {
        return new Color(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);
    }

    public static Color operator *(Color left, Color right)
    {
        return new Color(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
    }

    public static Color operator *(Color color, float scalar)
    {
        return new Color(color.R * scalar, color.G * scalar, color.B * scalar, color.A * scalar);
    }

    public static Color operator *(float scalar, Color color)
    {
        return color * scalar;
    }

    public static Color operator /(Color color, float scalar)
    {
        return new Color(color.R / scalar, color.G / scalar, color.B / scalar, color.A / scalar);
    }

    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
    }

    private static int ToByte(float value)
    {
        return Mathf.Clamp((int)MathF.Round(Mathf.Clamp(value, 0f, 1f) * 255f, MidpointRounding.AwayFromZero), 0, 255);
    }

    private static float FromByte(string value)
    {
        return byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
    }
}
