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

public struct Transform2D : IEquatable<Transform2D>
{
    public static readonly Transform2D Identity = new(Vector2.Right, Vector2.Down, Vector2.Zero);
    public static readonly Transform2D FlipX = new(new Vector2(-1f, 0f), Vector2.Down, Vector2.Zero);
    public static readonly Transform2D FlipY = new(Vector2.Right, new Vector2(0f, -1f), Vector2.Zero);

    public Transform2D(Vector2 xAxis, Vector2 yAxis, Vector2 origin)
    {
        X = xAxis;
        Y = yAxis;
        Origin = origin;
    }

    public Transform2D(float rotation, Vector2 origin)
    {
        var sine = MathF.Sin(rotation);
        var cosine = MathF.Cos(rotation);
        X = new Vector2(cosine, sine);
        Y = new Vector2(-sine, cosine);
        Origin = origin;
    }

    public Transform2D(float xx, float xy, float yx, float yy, float ox, float oy)
        : this(new Vector2(xx, xy), new Vector2(yx, yy), new Vector2(ox, oy))
    {
    }

    public Vector2 X { get; set; }

    public Vector2 Y { get; set; }

    public Vector2 Origin { get; set; }

    public float Determinant()
    {
        return (X.X * Y.Y) - (X.Y * Y.X);
    }

    public Vector2 Xform(Vector2 value)
    {
        return BasisXform(value) + Origin;
    }

    public Vector2 BasisXform(Vector2 value)
    {
        return new Vector2(
            (X.X * value.X) + (Y.X * value.Y),
            (X.Y * value.X) + (Y.Y * value.Y));
    }

    public Transform2D AffineInverse()
    {
        var determinant = Determinant();
        if (Mathf.IsZeroApprox(determinant))
        {
            throw new InvalidOperationException("Transform2D basis is not invertible.");
        }

        var inverse = new Transform2D(
            Y.Y / determinant,
            -X.Y / determinant,
            -Y.X / determinant,
            X.X / determinant,
            0f,
            0f);
        inverse.Origin = inverse.BasisXform(-Origin);
        return inverse;
    }

    public Transform2D Inverse()
    {
        return AffineInverse();
    }

    public Transform2D Translated(Vector2 offset)
    {
        var result = this;
        result.Origin += offset;
        return result;
    }

    public Transform2D Scaled(Vector2 scale)
    {
        return new Transform2D(
            new Vector2(X.X * scale.X, X.Y * scale.Y),
            new Vector2(Y.X * scale.X, Y.Y * scale.Y),
            new Vector2(Origin.X * scale.X, Origin.Y * scale.Y));
    }

    public Transform2D Rotated(float angle)
    {
        return new Transform2D(angle, Vector2.Zero) * this;
    }

    public bool IsEqualApprox(Transform2D transform)
    {
        return X.IsEqualApprox(transform.X) &&
            Y.IsEqualApprox(transform.Y) &&
            Origin.IsEqualApprox(transform.Origin);
    }

    public bool IsFinite()
    {
        return X.IsFinite() && Y.IsFinite() && Origin.IsFinite();
    }

    public bool Equals(Transform2D other)
    {
        return X == other.X && Y == other.Y && Origin == other.Origin;
    }

    public override bool Equals(object? obj)
    {
        return obj is Transform2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Origin);
    }

    public override string ToString()
    {
        return $"[X: {X}, Y: {Y}, O: {Origin}]";
    }

    public static Transform2D operator *(Transform2D left, Transform2D right)
    {
        return new Transform2D(
            left.BasisXform(right.X),
            left.BasisXform(right.Y),
            left.Xform(right.Origin));
    }

    public static Vector2 operator *(Transform2D transform, Vector2 vector)
    {
        return transform.Xform(vector);
    }

    public static Rect2 operator *(Transform2D transform, Rect2 rect)
    {
        var p1 = transform.Xform(rect.Position);
        var p2 = transform.Xform(new Vector2(rect.End.X, rect.Position.Y));
        var p3 = transform.Xform(rect.End);
        var p4 = transform.Xform(new Vector2(rect.Position.X, rect.End.Y));
        return new Rect2(p1, Vector2.Zero).Expand(p2).Expand(p3).Expand(p4);
    }

    public static bool operator ==(Transform2D left, Transform2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Transform2D left, Transform2D right)
    {
        return !left.Equals(right);
    }
}
