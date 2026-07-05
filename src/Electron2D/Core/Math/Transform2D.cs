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
/// Represents the transform2 d value type.
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
public struct Transform2D : IEquatable<Transform2D>
{
    /// <summary>
    /// Represents the identity value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Transform2D" />
    ///
    public static readonly Transform2D Identity = new(Vector2.Right, Vector2.Down, Vector2.Zero);
    /// <summary>
    /// Represents the flip x value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Transform2D" />
    ///
    public static readonly Transform2D FlipX = new(new Vector2(-1f, 0f), Vector2.Down, Vector2.Zero);
    /// <summary>
    /// Represents the flip y value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Transform2D" />
    ///
    public static readonly Transform2D FlipY = new(Vector2.Right, new Vector2(0f, -1f), Vector2.Zero);

    /// <summary>
    /// Initializes a new instance of the Transform2D type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="xAxis">
    /// The x axis value.
    /// </param>
    ///
    /// <param name="yAxis">
    /// The y axis value.
    /// </param>
    ///
    /// <param name="origin">
    /// The origin value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D(Vector2 xAxis, Vector2 yAxis, Vector2 origin)
    {
        X = xAxis;
        Y = yAxis;
        Origin = origin;
    }

    /// <summary>
    /// Initializes a new instance of the Transform2D type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="rotation">
    /// The rotation value.
    /// </param>
    ///
    /// <param name="origin">
    /// The origin value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D(float rotation, Vector2 origin)
    {
        var sine = MathF.Sin(rotation);
        var cosine = MathF.Cos(rotation);
        X = new Vector2(cosine, sine);
        Y = new Vector2(-sine, cosine);
        Origin = origin;
    }

    /// <summary>
    /// Initializes a new instance of the Transform2D type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="xx">
    /// The xx value.
    /// </param>
    ///
    /// <param name="xy">
    /// The xy value.
    /// </param>
    ///
    /// <param name="yx">
    /// The yx value.
    /// </param>
    ///
    /// <param name="yy">
    /// The yy value.
    /// </param>
    ///
    /// <param name="ox">
    /// The ox value.
    /// </param>
    ///
    /// <param name="oy">
    /// The oy value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D(float xx, float xy, float yx, float yy, float ox, float oy)
        : this(new Vector2(xx, xy), new Vector2(yx, yy), new Vector2(ox, oy))
    {
    }

    /// <summary>
    /// Gets or sets the x value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current x value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Vector2 X { get; set; }

    /// <summary>
    /// Gets or sets the y value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current y value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Vector2 Y { get; set; }

    /// <summary>
    /// Gets or sets the origin value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current origin value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Vector2 Origin { get; set; }

    /// <summary>
    /// Executes the determinant operation.
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
    /// <seealso cref="Transform2D" />
    ///
    public float Determinant()
    {
        return (X.X * Y.Y) - (X.Y * Y.X);
    }

    /// <summary>
    /// Executes the xform operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
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
    /// <seealso cref="Transform2D" />
    ///
    public Vector2 Xform(Vector2 value)
    {
        return BasisXform(value) + Origin;
    }

    /// <summary>
    /// Executes the basis xform operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
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
    /// <seealso cref="Transform2D" />
    ///
    public Vector2 BasisXform(Vector2 value)
    {
        return new Vector2(
            (X.X * value.X) + (Y.X * value.Y),
            (X.Y * value.X) + (Y.Y * value.Y));
    }

    /// <summary>
    /// Executes the affine inverse operation.
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
    /// <seealso cref="Transform2D" />
    ///
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

    /// <summary>
    /// Executes the inverse operation.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D Inverse()
    {
        return AffineInverse();
    }

    /// <summary>
    /// Executes the translated operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="offset">
    /// The offset value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D Translated(Vector2 offset)
    {
        var result = this;
        result.Origin += offset;
        return result;
    }

    /// <summary>
    /// Executes the scaled operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="scale">
    /// The scale value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D Scaled(Vector2 scale)
    {
        return new Transform2D(
            new Vector2(X.X * scale.X, X.Y * scale.Y),
            new Vector2(Y.X * scale.X, Y.Y * scale.Y),
            new Vector2(Origin.X * scale.X, Origin.Y * scale.Y));
    }

    /// <summary>
    /// Executes the rotated operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="angle">
    /// The angle value.
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
    /// <seealso cref="Transform2D" />
    ///
    public Transform2D Rotated(float angle)
    {
        return new Transform2D(angle, Vector2.Zero) * this;
    }

    /// <summary>
    /// Checks whether equal approx is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="transform">
    /// The transform value.
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
    /// <seealso cref="Transform2D" />
    ///
    public bool IsEqualApprox(Transform2D transform)
    {
        return X.IsEqualApprox(transform.X) &&
            Y.IsEqualApprox(transform.Y) &&
            Origin.IsEqualApprox(transform.Origin);
    }

    /// <summary>
    /// Checks whether finite is true.
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
    /// <seealso cref="Transform2D" />
    ///
    public bool IsFinite()
    {
        return X.IsFinite() && Y.IsFinite() && Origin.IsFinite();
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
    /// <seealso cref="Transform2D" />
    ///
    public bool Equals(Transform2D other)
    {
        return X == other.X && Y == other.Y && Origin == other.Origin;
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
    /// <seealso cref="Transform2D" />
    ///
    public override bool Equals(object? obj)
    {
        return obj is Transform2D other && Equals(other);
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
    /// <seealso cref="Transform2D" />
    ///
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Origin);
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
    /// <seealso cref="Transform2D" />
    ///
    public override string ToString()
    {
        return $"[X: {X}, Y: {Y}, O: {Origin}]";
    }

    /// <summary>
    /// Applies the <c>*</c> operator.
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
    /// <seealso cref="Transform2D" />
    ///
    public static Transform2D operator *(Transform2D left, Transform2D right)
    {
        return new Transform2D(
            left.BasisXform(right.X),
            left.BasisXform(right.Y),
            left.Xform(right.Origin));
    }

    /// <summary>
    /// Applies the <c>*</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="transform">
    /// The transform value.
    /// </param>
    ///
    /// <param name="vector">
    /// The vector value.
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
    /// <seealso cref="Transform2D" />
    ///
    public static Vector2 operator *(Transform2D transform, Vector2 vector)
    {
        return transform.Xform(vector);
    }

    /// <summary>
    /// Applies the <c>*</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="transform">
    /// The transform value.
    /// </param>
    ///
    /// <param name="rect">
    /// The rect value.
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
    /// <seealso cref="Transform2D" />
    ///
    public static Rect2 operator *(Transform2D transform, Rect2 rect)
    {
        var p1 = transform.Xform(rect.Position);
        var p2 = transform.Xform(new Vector2(rect.End.X, rect.Position.Y));
        var p3 = transform.Xform(rect.End);
        var p4 = transform.Xform(new Vector2(rect.Position.X, rect.End.Y));
        return new Rect2(p1, Vector2.Zero).Expand(p2).Expand(p3).Expand(p4);
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
    /// <seealso cref="Transform2D" />
    ///
    public static bool operator ==(Transform2D left, Transform2D right)
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
    /// <seealso cref="Transform2D" />
    ///
    public static bool operator !=(Transform2D left, Transform2D right)
    {
        return !left.Equals(right);
    }
}
