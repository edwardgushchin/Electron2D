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

public struct Rect2 : IEquatable<Rect2>
{
    public Rect2(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public Rect2(float x, float y, float width, float height)
        : this(new Vector2(x, y), new Vector2(width, height))
    {
    }

    public Vector2 Position { get; set; }

    public Vector2 Size { get; set; }

    public Vector2 End
    {
        get => Position + Size;
        set => Size = value - Position;
    }

    public float GetArea()
    {
        return Size.X * Size.Y;
    }

    public Vector2 GetCenter()
    {
        return Position + (Size / 2f);
    }

    public bool HasArea()
    {
        return Size.X > 0f && Size.Y > 0f;
    }

    public Rect2 Abs()
    {
        var end = End;
        var min = Position.Min(end);
        var max = Position.Max(end);
        return new Rect2(min, max - min);
    }

    public bool HasPoint(Vector2 point)
    {
        return point.X >= Position.X &&
            point.Y >= Position.Y &&
            point.X < Position.X + Size.X &&
            point.Y < Position.Y + Size.Y;
    }

    public bool Encloses(Rect2 rect)
    {
        return rect.Position.X >= Position.X &&
            rect.Position.Y >= Position.Y &&
            rect.End.X <= End.X &&
            rect.End.Y <= End.Y;
    }

    public bool Intersects(Rect2 rect, bool includeBorders = false)
    {
        if (includeBorders)
        {
            return Position.X <= rect.End.X &&
                End.X >= rect.Position.X &&
                Position.Y <= rect.End.Y &&
                End.Y >= rect.Position.Y;
        }

        return Position.X < rect.End.X &&
            End.X > rect.Position.X &&
            Position.Y < rect.End.Y &&
            End.Y > rect.Position.Y;
    }

    public Rect2 Intersection(Rect2 rect)
    {
        var start = Position.Max(rect.Position);
        var end = End.Min(rect.End);
        var size = end - start;
        if (size.X < 0f || size.Y < 0f)
        {
            return new Rect2(start, Vector2.Zero);
        }

        return new Rect2(start, size);
    }

    public Rect2 Merge(Rect2 rect)
    {
        var start = Position.Min(rect.Position);
        var end = End.Max(rect.End);
        return new Rect2(start, end - start);
    }

    public Rect2 Expand(Vector2 to)
    {
        var start = Position.Min(to);
        var end = End.Max(to);
        return new Rect2(start, end - start);
    }

    public Rect2 Grow(float amount)
    {
        return new Rect2(Position - new Vector2(amount, amount), Size + new Vector2(amount * 2f, amount * 2f));
    }

    public bool IsEqualApprox(Rect2 rect)
    {
        return Position.IsEqualApprox(rect.Position) && Size.IsEqualApprox(rect.Size);
    }

    public bool Equals(Rect2 other)
    {
        return Position == other.Position && Size == other.Size;
    }

    public override bool Equals(object? obj)
    {
        return obj is Rect2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Size);
    }

    public override string ToString()
    {
        return $"[P: {Position}, S: {Size}]";
    }

    public static bool operator ==(Rect2 left, Rect2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rect2 left, Rect2 right)
    {
        return !left.Equals(right);
    }
}
