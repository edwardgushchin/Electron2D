using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a two-dimensional affine transformation as three column vectors.</summary>
/// <remarks>
/// <see cref="X"/> and <see cref="Y"/> form the two-by-two basis; <see cref="Origin"/> stores translation.
/// The value can represent translation, clockwise rotation in screen coordinates, non-uniform scale, reflection,
/// and skew. The zero-initialized value is a zero matrix, not <see cref="Identity"/>.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Transform : IEquatable<Transform>
{
    private static readonly Transform IdentityValue = new(1f, 0f, 0f, 1f, 0f, 0f);
    private static readonly Transform FlipXValue = new(-1f, 0f, 0f, 1f, 0f, 0f);
    private static readonly Transform FlipYValue = new(1f, 0f, 0f, -1f, 0f, 0f);

    /// <summary>Gets or sets the basis X axis, which is matrix column zero.</summary>
    /// <remarks>Its length contributes the horizontal scale and its direction defines the transform rotation.</remarks>
    public Vector2 X;

    /// <summary>Gets or sets the basis Y axis, which is matrix column one.</summary>
    /// <remarks>Its length contributes the vertical scale and its direction relative to <see cref="X"/> defines skew.</remarks>
    public Vector2 Y;

    /// <summary>Gets or sets the translation offset, which is matrix column two.</summary>
    public Vector2 Origin;

    /// <summary>Gets the identity transform.</summary>
    /// <value>A transform with unit basis axes and zero origin.</value>
    public static Transform Identity => IdentityValue;

    /// <summary>Gets a transform that reflects across the vertical axis by negating horizontal coordinates.</summary>
    /// <value>A transform with basis axes <c>(-1, 0)</c> and <c>(0, 1)</c>.</value>
    public static Transform FlipX => FlipXValue;

    /// <summary>Gets a transform that reflects across the horizontal axis by negating vertical coordinates.</summary>
    /// <value>A transform with basis axes <c>(1, 0)</c> and <c>(0, -1)</c>.</value>
    public static Transform FlipY => FlipYValue;

    /// <summary>Gets the clockwise screen-space rotation in radians.</summary>
    /// <value>The angle of <see cref="X"/>, measured from positive X toward positive Y.</value>
    public readonly float Rotation => Mathf.Atan2(X.Y, X.X);

    /// <summary>Gets the lengths of the basis axes with reflection encoded in the vertical component.</summary>
    /// <value>
    /// <see cref="X"/> length and signed <see cref="Y"/> length. A negative determinant makes the vertical component
    /// negative; a zero or unordered determinant makes it zero.
    /// </value>
    public readonly Vector2 Scale => new(X.Length(), Sign(Determinant()) * Y.Length());

    /// <summary>Gets the angular skew between the basis axes in radians.</summary>
    /// <value>Zero for an orthogonal basis, with reflection accounted for by the determinant sign.</value>
    public readonly float Skew =>
        Mathf.Acos(NormalizedOrZero(X).Dot(Sign(Determinant()) * NormalizedOrZero(Y))) - (Mathf.Pi * 0.5f);

    /// <summary>Gets or sets a complete matrix column.</summary>
    /// <param name="column">Zero for <see cref="X"/>, one for <see cref="Y"/>, or two for <see cref="Origin"/>.</param>
    /// <value>The selected column.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="column"/> is outside zero through two.</exception>
    public Vector2 this[int column]
    {
        readonly get => column switch
        {
            0 => X,
            1 => Y,
            2 => Origin,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),
        };
        set
        {
            switch (column)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Origin = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(column));
            }
        }
    }

    /// <summary>Gets or sets one matrix component using column-major coordinates.</summary>
    /// <param name="column">The matrix column from zero through two.</param>
    /// <param name="row">The matrix row, zero for X or one for Y.</param>
    /// <value>The selected floating-point component.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="column"/> is outside zero through two, or <paramref name="row"/> is outside zero through one.
    /// </exception>
    public float this[int column, int row]
    {
        readonly get
        {
            var vector = this[column];
            return row switch
            {
                0 => vector.X,
                1 => vector.Y,
                _ => throw new ArgumentOutOfRangeException(nameof(row)),
            };
        }
        set
        {
            var vector = this[column];
            switch (row)
            {
                case 0:
                    vector.X = value;
                    break;
                case 1:
                    vector.Y = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(row));
            }

            this[column] = vector;
        }
    }

    /// <summary>Initializes a transform from its three matrix columns.</summary>
    /// <param name="xAxis">The basis X axis.</param>
    /// <param name="yAxis">The basis Y axis.</param>
    /// <param name="origin">The translation offset.</param>
    public Transform(Vector2 xAxis, Vector2 yAxis, Vector2 origin)
    {
        X = xAxis;
        Y = yAxis;
        Origin = origin;
    }

    /// <summary>Initializes a transform from six column-major components.</summary>
    /// <param name="xx">The X component of <see cref="X"/>.</param>
    /// <param name="xy">The Y component of <see cref="X"/>.</param>
    /// <param name="yx">The X component of <see cref="Y"/>.</param>
    /// <param name="yy">The Y component of <see cref="Y"/>.</param>
    /// <param name="ox">The X component of <see cref="Origin"/>.</param>
    /// <param name="oy">The Y component of <see cref="Origin"/>.</param>
    public Transform(float xx, float xy, float yx, float yy, float ox, float oy)
        : this(new Vector2(xx, xy), new Vector2(yx, yy), new Vector2(ox, oy))
    {
    }

    /// <summary>Initializes a rotation and translation transform.</summary>
    /// <param name="rotation">The clockwise screen-space angle in radians.</param>
    /// <param name="origin">The translation offset.</param>
    public Transform(float rotation, Vector2 origin)
    {
        var (sine, cosine) = Mathf.SinCos(rotation);
        X = new Vector2(cosine, sine);
        Y = new Vector2(-sine, cosine);
        Origin = origin;
    }

    /// <summary>Initializes a transform from rotation, scale, skew, and translation.</summary>
    /// <param name="rotation">The clockwise screen-space rotation in radians.</param>
    /// <param name="scale">The horizontal and vertical scale factors.</param>
    /// <param name="skew">The angular skew in radians.</param>
    /// <param name="origin">The translation offset.</param>
    public Transform(float rotation, Vector2 scale, float skew, Vector2 origin)
    {
        var (rotationSine, rotationCosine) = Mathf.SinCos(rotation);
        var (skewedSine, skewedCosine) = Mathf.SinCos(rotation + skew);
        X = new Vector2(rotationCosine * scale.X, rotationSine * scale.X);
        Y = new Vector2(-skewedSine * scale.Y, skewedCosine * scale.Y);
        Origin = origin;
    }

    /// <summary>Returns the general affine inverse.</summary>
    /// <returns>A transform that composes with this transform to produce the identity, within floating-point precision.</returns>
    /// <exception cref="InvalidOperationException">The basis determinant is exactly zero.</exception>
    public readonly Transform AffineInverse()
    {
        var determinant = Determinant();
        if (determinant == 0f)
            throw new InvalidOperationException("A transform with a zero determinant cannot be inverted.");

        var inverseDeterminant = 1f / determinant;
        var inverse = new Transform(
            Y.Y * inverseDeterminant,
            -X.Y * inverseDeterminant,
            -Y.X * inverseDeterminant,
            X.X * inverseDeterminant,
            0f,
            0f);
        inverse.Origin = inverse.BasisXform(-Origin);
        return inverse;
    }

    /// <summary>Transforms a vector by the basis while ignoring translation.</summary>
    /// <param name="vector">The vector to transform.</param>
    /// <returns>The vector multiplied by the two-by-two basis.</returns>
    public readonly Vector2 BasisXform(Vector2 vector) => new(TDotX(vector), TDotY(vector));

    /// <summary>Transforms a vector by the transposed basis while ignoring translation.</summary>
    /// <param name="vector">The vector to transform.</param>
    /// <returns>The vector multiplied by the transposed basis.</returns>
    /// <remarks>
    /// This is the inverse basis transform only when the basis is orthonormal. For scaled or skewed transforms, use
    /// <c>transform.AffineInverse().BasisXform(vector)</c>.
    /// </remarks>
    public readonly Vector2 BasisXformInv(Vector2 vector) => new(X.Dot(vector), Y.Dot(vector));

    /// <summary>Returns the determinant of the two-by-two basis.</summary>
    /// <returns>Zero for a singular basis, a negative value for a reflected basis, or a positive value otherwise.</returns>
    public readonly float Determinant() => (X.X * Y.Y) - (X.Y * Y.X);

    /// <summary>Interpolates or extrapolates decomposed transform components.</summary>
    /// <param name="other">The destination transform.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>A transform built from shortest-path angle interpolation, linear scale, skew, and origin interpolation.</returns>
    public readonly Transform InterpolateWith(Transform other, float weight) => new(
        Mathf.LerpAngle(Rotation, other.Rotation, weight),
        Scale.Lerp(other.Scale, weight),
        Mathf.LerpAngle(Skew, other.Skew, weight),
        Origin.Lerp(other.Origin, weight));

    /// <summary>Returns the fast inverse for an orthonormal basis.</summary>
    /// <returns>The transposed basis and corresponding inverse translation.</returns>
    /// <remarks>
    /// This method assumes rotation or reflection without scale or skew and does not validate that precondition. Use
    /// <see cref="AffineInverse"/> for a general invertible affine transform.
    /// </remarks>
    public readonly Transform Inverse()
    {
        var inverse = this;
        (inverse.X.Y, inverse.Y.X) = (inverse.Y.X, inverse.X.Y);
        inverse.Origin = inverse.BasisXform(-inverse.Origin);
        return inverse;
    }

    /// <summary>Tests whether the basis preserves angles up to uniform scale and optional reflection.</summary>
    /// <returns><see langword="true"/> for approximately orthogonal axes of approximately equal length.</returns>
    public readonly bool IsConformal() =>
        (Mathf.IsEqualApprox(X.X, Y.Y) && Mathf.IsEqualApprox(X.Y, -Y.X)) ||
        (Mathf.IsEqualApprox(X.X, -Y.Y) && Mathf.IsEqualApprox(X.Y, Y.X));

    /// <summary>Tests all three columns for scale-aware approximate equality.</summary>
    /// <param name="other">The transform to compare.</param>
    /// <returns><see langword="true"/> when every corresponding component is approximately equal.</returns>
    public readonly bool IsEqualApprox(Transform other) =>
        X.IsEqualApprox(other.X) && Y.IsEqualApprox(other.Y) && Origin.IsEqualApprox(other.Origin);

    /// <summary>Tests whether every matrix component is finite.</summary>
    /// <returns><see langword="true"/> when no component is NaN or infinity.</returns>
    public readonly bool IsFinite() => X.IsFinite() && Y.IsFinite() && Origin.IsFinite();

    /// <summary>Returns a rotation-only transform turned toward a target through this transform's affine local space.</summary>
    /// <param name="target">The global target point; defaults to the zero vector.</param>
    /// <returns>A transform with the same origin and adjusted rotation; scale and skew are removed.</returns>
    /// <exception cref="InvalidOperationException">The basis determinant is exactly zero.</exception>
    /// <remarks>
    /// The target is inverse-transformed and compensated by the signed basis scale before its angle is added to the
    /// current rotation. For a skewed source this differs from using the raw global angle from the origin.
    /// </remarks>
    public readonly Transform LookingAt(Vector2 target = default)
    {
        var localTarget = AffineInverse() * target;
        var scaledTarget = localTarget * Scale;
        return new Transform(Rotation + Mathf.Atan2(scaledTarget.Y, scaledTarget.X), Origin);
    }

    /// <summary>Returns a transform with a Gram-Schmidt orthonormalized basis.</summary>
    /// <returns>A copy with unit perpendicular axes and the original origin.</returns>
    /// <remarks>A zero or linearly dependent axis normalizes to zero instead of producing non-finite components.</remarks>
    public readonly Transform Orthonormalized()
    {
        var xAxis = NormalizedOrZero(X);
        var yAxis = NormalizedOrZero(Y - (xAxis * xAxis.Dot(Y)));
        return new Transform(xAxis, yAxis, Origin);
    }

    /// <summary>Applies a rotation in the global or parent coordinate frame.</summary>
    /// <param name="angle">The clockwise screen-space angle in radians.</param>
    /// <returns>The rotation transform multiplied on the left of this transform.</returns>
    public readonly Transform Rotated(float angle) => new Transform(angle, Vector2.Zero) * this;

    /// <summary>Applies a rotation in the local coordinate frame.</summary>
    /// <param name="angle">The clockwise screen-space angle in radians.</param>
    /// <returns>The rotation transform multiplied on the right of this transform.</returns>
    public readonly Transform RotatedLocal(float angle) => this * new Transform(angle, Vector2.Zero);

    /// <summary>Applies componentwise scale in the global or parent coordinate frame.</summary>
    /// <param name="scale">The scale along global X and Y.</param>
    /// <returns>A copy whose basis rows and origin are scaled componentwise.</returns>
    public readonly Transform Scaled(Vector2 scale) => new(X * scale, Y * scale, Origin * scale);

    /// <summary>Applies scale in the local coordinate frame.</summary>
    /// <param name="scale">The scale along the local basis axes.</param>
    /// <returns>A copy whose X and Y columns are multiplied by their corresponding factors.</returns>
    public readonly Transform ScaledLocal(Vector2 scale) => new(X * scale.X, Y * scale.Y, Origin);

    /// <summary>Applies translation in the global or parent coordinate frame.</summary>
    /// <param name="offset">The global offset.</param>
    /// <returns>A copy with the offset added directly to its origin.</returns>
    public readonly Transform Translated(Vector2 offset) => new(X, Y, Origin + offset);

    /// <summary>Applies translation in the local coordinate frame.</summary>
    /// <param name="offset">The offset expressed in the current basis.</param>
    /// <returns>A copy with the basis-transformed offset added to its origin.</returns>
    public readonly Transform TranslatedLocal(Vector2 offset) => new(X, Y, Origin + BasisXform(offset));

    /// <summary>Composes a parent transform with a child transform.</summary>
    /// <param name="left">The parent transform applied second.</param>
    /// <param name="right">The child transform applied first.</param>
    /// <returns>The composed transform.</returns>
    public static Transform operator *(Transform left, Transform right) => new(
        left.BasisXform(right.X),
        left.BasisXform(right.Y),
        left * right.Origin);

    /// <summary>Transforms a point by the basis and translation.</summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="point">The point in local coordinates.</param>
    /// <returns>The transformed point.</returns>
    public static Vector2 operator *(Transform transform, Vector2 point) => transform.BasisXform(point) + transform.Origin;

    /// <summary>Transforms a rectangle and returns the axis-aligned bounds of its four transformed corners.</summary>
    /// <param name="transform">The affine transform to apply.</param>
    /// <param name="rectangle">The rectangle to transform.</param>
    /// <returns>The smallest axis-aligned rectangle enclosing all four transformed corners.</returns>
    /// <remarks>
    /// Rotation, reflection, non-uniform scale, skew, zero size, and negative size are supported. The result is
    /// normalized even when <paramref name="rectangle"/> has a negative size.
    /// </remarks>
    public static Rect2 operator *(Transform transform, Rect2 rectangle)
    {
        var origin = transform * rectangle.Position;
        var xEdge = transform.X * rectangle.Size.X;
        var yEdge = transform.Y * rectangle.Size.Y;
        var opposite = origin + xEdge + yEdge;
        var minimum = origin.Min(origin + xEdge).Min((origin + yEdge).Min(opposite));
        var maximum = origin.Max(origin + xEdge).Max((origin + yEdge).Max(opposite));
        return new Rect2(minimum, maximum - minimum);
    }

    /// <summary>Applies the inverse orthonormal transform to a point.</summary>
    /// <param name="point">The point in transformed coordinates.</param>
    /// <param name="transform">The orthonormal transform to invert.</param>
    /// <returns>The point expressed in the transform's local coordinates.</returns>
    /// <remarks>For scale or skew, multiply the point by <see cref="AffineInverse"/> instead.</remarks>
    public static Vector2 operator *(Vector2 point, Transform transform) => transform.BasisXformInv(point - transform.Origin);

    /// <summary>Inverse-transforms a rectangle under an orthonormal-basis precondition.</summary>
    /// <param name="rectangle">The rectangle in transformed coordinates.</param>
    /// <param name="transform">The orthonormal transform to invert.</param>
    /// <returns>The axis-aligned bounds of the inverse-transformed rectangle corners.</returns>
    /// <remarks>
    /// This operator is equivalent to <c>transform.Inverse() * rectangle</c>. For scale or skew, use
    /// <c>transform.AffineInverse() * rectangle</c> instead.
    /// </remarks>
    public static Rect2 operator *(Rect2 rectangle, Transform transform) => transform.Inverse() * rectangle;

    /// <summary>Transforms every point into a newly allocated array.</summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="points">The source points.</param>
    /// <returns>A new array containing transformed points in the original order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> is <see langword="null"/>.</exception>
    public static Vector2[] operator *(Transform transform, Vector2[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var result = new Vector2[points.Length];
        for (var index = 0; index < points.Length; index++)
            result[index] = transform * points[index];
        return result;
    }

    /// <summary>Inverse-transforms every point by an orthonormal transform into a newly allocated array.</summary>
    /// <param name="points">The source points.</param>
    /// <param name="transform">The orthonormal transform to invert.</param>
    /// <returns>A new array containing inverse-transformed points in the original order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> is <see langword="null"/>.</exception>
    /// <remarks>For scale or skew, multiply the points by <see cref="AffineInverse"/> instead.</remarks>
    public static Vector2[] operator *(Vector2[] points, Transform transform)
    {
        ArgumentNullException.ThrowIfNull(points);
        var result = new Vector2[points.Length];
        for (var index = 0; index < points.Length; index++)
            result[index] = points[index] * transform;
        return result;
    }

    /// <summary>Multiplies every matrix component, including translation, by a scalar.</summary>
    /// <param name="transform">The transform to scale.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The componentwise product.</returns>
    public static Transform operator *(Transform transform, float scalar) =>
        new(transform.X * scalar, transform.Y * scalar, transform.Origin * scalar);

    /// <summary>Divides every matrix component, including translation, by a scalar.</summary>
    /// <param name="transform">The transform to divide.</param>
    /// <param name="scalar">The scalar divisor.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Transform operator /(Transform transform, float scalar) =>
        new(transform.X / scalar, transform.Y / scalar, transform.Origin / scalar);

    /// <summary>Tests all matrix components for exact equality.</summary>
    /// <param name="left">The first transform.</param>
    /// <param name="right">The second transform.</param>
    /// <returns><see langword="true"/> when every corresponding component is exactly equal.</returns>
    public static bool operator ==(Transform left, Transform right) => left.Equals(right);

    /// <summary>Tests whether any matrix component differs under exact equality.</summary>
    /// <param name="left">The first transform.</param>
    /// <param name="right">The second transform.</param>
    /// <returns><see langword="true"/> when at least one corresponding component differs.</returns>
    public static bool operator !=(Transform left, Transform right) => !left.Equals(right);

    /// <summary>Tests whether another object is an exactly equal transform.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a transform with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Transform other && Equals(other);

    /// <summary>Tests all matrix components for exact equality.</summary>
    /// <param name="other">The transform to compare.</param>
    /// <returns><see langword="true"/> when every corresponding component is exactly equal.</returns>
    public readonly bool Equals(Transform other) => X == other.X && Y == other.Y && Origin == other.Origin;

    /// <summary>Returns a hash code based on all three columns.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Origin);

    /// <summary>Formats the three columns using invariant culture.</summary>
    /// <returns>A string containing the X axis, Y axis, and origin.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats the three columns with a numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format, or <see langword="null"/> for the default format.</param>
    /// <returns>A string containing the X axis, Y axis, and origin.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) =>
        $"[X: {X.ToString(format)}, Y: {Y.ToString(format)}, O: {Origin.ToString(format)}]";

    private readonly float TDotX(Vector2 vector) => (X.X * vector.X) + (Y.X * vector.Y);

    private readonly float TDotY(Vector2 vector) => (X.Y * vector.X) + (Y.Y * vector.Y);

    private static float Sign(float value) => value > 0f ? 1f : value < 0f ? -1f : 0f;

    private static Vector2 NormalizedOrZero(Vector2 value)
    {
        var length = value.Length();
        return length == 0f ? Vector2.Zero : value / length;
    }

}
