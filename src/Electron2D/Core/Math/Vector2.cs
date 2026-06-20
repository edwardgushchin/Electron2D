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
namespace Electron2D;

public struct Vector2 : IEquatable<Vector2>
{
    public static readonly Vector2 Zero = new(0f, 0f);
    public static readonly Vector2 One = new(1f, 1f);
    public static readonly Vector2 Inf = new(float.PositiveInfinity, float.PositiveInfinity);
    public static readonly Vector2 Left = new(-1f, 0f);
    public static readonly Vector2 Right = new(1f, 0f);
    public static readonly Vector2 Up = new(0f, -1f);
    public static readonly Vector2 Down = new(0f, 1f);

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; }

    public float Y { get; set; }

    public float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    public float LengthSquared()
    {
        return (X * X) + (Y * Y);
    }

    public Vector2 Normalized()
    {
        var length = Length();
        return length == 0f ? Zero : this / length;
    }

    public bool IsNormalized()
    {
        return Mathf.IsEqualApprox(LengthSquared(), 1f);
    }

    public float Dot(Vector2 with)
    {
        return (X * with.X) + (Y * with.Y);
    }

    public float Cross(Vector2 with)
    {
        return (X * with.Y) - (Y * with.X);
    }

    public float Angle()
    {
        return MathF.Atan2(Y, X);
    }

    public float AngleTo(Vector2 to)
    {
        return MathF.Atan2(Cross(to), Dot(to));
    }

    public float DistanceTo(Vector2 to)
    {
        return (to - this).Length();
    }

    public float DistanceSquaredTo(Vector2 to)
    {
        return (to - this).LengthSquared();
    }

    public Vector2 DirectionTo(Vector2 to)
    {
        return (to - this).Normalized();
    }

    public Vector2 Lerp(Vector2 to, float weight)
    {
        return new Vector2(Mathf.Lerp(X, to.X, weight), Mathf.Lerp(Y, to.Y, weight));
    }

    public Vector2 Rotated(float angle)
    {
        var sine = MathF.Sin(angle);
        var cosine = MathF.Cos(angle);
        return new Vector2((X * cosine) - (Y * sine), (X * sine) + (Y * cosine));
    }

    public Vector2 Abs()
    {
        return new Vector2(MathF.Abs(X), MathF.Abs(Y));
    }

    public Vector2 Floor()
    {
        return new Vector2(MathF.Floor(X), MathF.Floor(Y));
    }

    public Vector2 Ceil()
    {
        return new Vector2(MathF.Ceiling(X), MathF.Ceiling(Y));
    }

    public Vector2 Round()
    {
        return new Vector2(MathF.Round(X, MidpointRounding.AwayFromZero), MathF.Round(Y, MidpointRounding.AwayFromZero));
    }

    public Vector2 Sign()
    {
        return new Vector2(MathF.Sign(X), MathF.Sign(Y));
    }

    public Vector2 Min(Vector2 with)
    {
        return new Vector2(MathF.Min(X, with.X), MathF.Min(Y, with.Y));
    }

    public Vector2 Max(Vector2 with)
    {
        return new Vector2(MathF.Max(X, with.X), MathF.Max(Y, with.Y));
    }

    public Vector2 Clamp(Vector2 min, Vector2 max)
    {
        return new Vector2(Mathf.Clamp(X, min.X, max.X), Mathf.Clamp(Y, min.Y, max.Y));
    }

    public bool IsEqualApprox(Vector2 other)
    {
        return Mathf.IsEqualApprox(X, other.X) && Mathf.IsEqualApprox(Y, other.Y);
    }

    public bool IsZeroApprox()
    {
        return Mathf.IsZeroApprox(X) && Mathf.IsZeroApprox(Y);
    }

    public bool IsFinite()
    {
        return Mathf.IsFinite(X) && Mathf.IsFinite(Y);
    }

    public bool Equals(Vector2 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({MathFormatting.Format(X)}, {MathFormatting.Format(Y)})";
    }

    public static Vector2 operator +(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X + right.X, left.Y + right.Y);
    }

    public static Vector2 operator -(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X - right.X, left.Y - right.Y);
    }

    public static Vector2 operator -(Vector2 value)
    {
        return new Vector2(-value.X, -value.Y);
    }

    public static Vector2 operator *(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X * right.X, left.Y * right.Y);
    }

    public static Vector2 operator *(Vector2 value, float scalar)
    {
        return new Vector2(value.X * scalar, value.Y * scalar);
    }

    public static Vector2 operator *(float scalar, Vector2 value)
    {
        return value * scalar;
    }

    public static Vector2 operator /(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X / right.X, left.Y / right.Y);
    }

    public static Vector2 operator /(Vector2 value, float scalar)
    {
        return new Vector2(value.X / scalar, value.Y / scalar);
    }

    public static bool operator ==(Vector2 left, Vector2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2 left, Vector2 right)
    {
        return !left.Equals(right);
    }
}
