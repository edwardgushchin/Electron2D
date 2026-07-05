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

/// <summary>
/// Represents the rect2 value type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1-preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1-preview.
/// </since>
///
public struct Rect2 : IEquatable<Rect2>
{
    /// <summary>
    /// Initializes a new instance of the Rect2 type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="position">
    /// The position value.
    /// </param>
    ///
    /// <param name="size">
    /// The size value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Rect2(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    /// <summary>
    /// Initializes a new instance of the Rect2 type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="x">
    /// The X coordinate or component.
    /// </param>
    ///
    /// <param name="y">
    /// The Y coordinate or component.
    /// </param>
    ///
    /// <param name="width">
    /// The width value.
    /// </param>
    ///
    /// <param name="height">
    /// The height value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Rect2(float x, float y, float width, float height)
        : this(new Vector2(x, y), new Vector2(width, height))
    {
    }

    /// <summary>
    /// Gets or sets the position value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current position value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the size value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current size value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Vector2 Size { get; set; }

    /// <summary>
    /// Gets or sets the end value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current end value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Vector2 End
    {
        get => Position + Size;
        set => Size = value - Position;
    }

    /// <summary>
    /// Gets the area value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current area value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public float GetArea()
    {
        return Size.X * Size.Y;
    }

    /// <summary>
    /// Gets the center value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current center value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Vector2 GetCenter()
    {
        return Position + (Size / 2f);
    }

    /// <summary>
    /// Checks whether area is available.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public bool HasArea()
    {
        return Size.X > 0f && Size.Y > 0f;
    }

    /// <summary>
    /// Executes the abs operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Rect2 Abs()
    {
        var end = End;
        var min = Position.Min(end);
        var max = Position.Max(end);
        return new Rect2(min, max - min);
    }

    /// <summary>
    /// Checks whether point is available.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="point">
    /// The point value.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public bool HasPoint(Vector2 point)
    {
        return point.X >= Position.X &&
            point.Y >= Position.Y &&
            point.X < Position.X + Size.X &&
            point.Y < Position.Y + Size.Y;
    }

    /// <summary>
    /// Executes the encloses operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="rect">
    /// The rect value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public bool Encloses(Rect2 rect)
    {
        return rect.Position.X >= Position.X &&
            rect.Position.Y >= Position.Y &&
            rect.End.X <= End.X &&
            rect.End.Y <= End.Y;
    }

    /// <summary>
    /// Executes the intersects operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="rect">
    /// The rect value.
    /// </param>
    ///
    /// <param name="includeBorders">
    /// The include borders value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
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

    /// <summary>
    /// Executes the intersection operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="rect">
    /// The rect value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
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

    /// <summary>
    /// Executes the merge operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="rect">
    /// The rect value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Rect2 Merge(Rect2 rect)
    {
        var start = Position.Min(rect.Position);
        var end = End.Max(rect.End);
        return new Rect2(start, end - start);
    }

    /// <summary>
    /// Executes the expand operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="to">
    /// The to value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Rect2 Expand(Vector2 to)
    {
        var start = Position.Min(to);
        var end = End.Max(to);
        return new Rect2(start, end - start);
    }

    /// <summary>
    /// Executes the grow operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="amount">
    /// The amount value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public Rect2 Grow(float amount)
    {
        return new Rect2(Position - new Vector2(amount, amount), Size + new Vector2(amount * 2f, amount * 2f));
    }

    /// <summary>
    /// Checks whether equal approx is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="rect">
    /// The rect value.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public bool IsEqualApprox(Rect2 rect)
    {
        return Position.IsEqualApprox(rect.Position) && Size.IsEqualApprox(rect.Size);
    }

    /// <summary>
    /// Executes the equals operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="other">
    /// The other value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public bool Equals(Rect2 other)
    {
        return Position == other.Position && Size == other.Size;
    }

    /// <summary>
    /// Executes the equals operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="obj">
    /// The obj value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public override bool Equals(object? obj)
    {
        return obj is Rect2 other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current hash code value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Size);
    }

    /// <summary>
    /// Executes the to string operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public override string ToString()
    {
        return $"[P: {Position}, S: {Size}]";
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public static bool operator ==(Rect2 left, Rect2 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Rect2" />
    ///
    public static bool operator !=(Rect2 left, Rect2 right)
    {
        return !left.Equals(right);
    }
}
