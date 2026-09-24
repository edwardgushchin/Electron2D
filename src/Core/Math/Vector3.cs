using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a three-component floating-point vector for numeric tuples.</summary>
/// <remarks>
/// Ordinary arithmetic preserves IEEE 754 NaN and infinity values. The zero-initialized value is <see cref="Zero"/>.
/// Equality and lexicographic ordering compare components directly: NaN is unordered and signed zeros compare equal.
/// Integer scalar expressions convert to float before using the scalar operators.
/// Componentwise scalar methods follow <see cref="Mathf"/> rounding, NaN and typed-error behavior.
/// Numeric operations do not allocate managed memory; string formatting allocates a string.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector3 : IEquatable<Vector3>
{
    private const float NormalizedEpsilon = 0.001f;

    private static readonly Vector3 ZeroValue = new(0f, 0f, 0f);
    private static readonly Vector3 OneValue = new(1f, 1f, 1f);
    private static readonly Vector3 InfValue = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    /// <summary>Gets the positive X unit vector.</summary>
    public static Vector3 Right => new(1, 0, 0);
    /// <summary>Gets the negative X unit vector.</summary>
    public static Vector3 Left => new(-1, 0, 0);
    /// <summary>Gets the positive Y unit vector.</summary>
    public static Vector3 Up => new(0, 1, 0);
    /// <summary>Gets the negative Y unit vector.</summary>
    public static Vector3 Down => new(0, -1, 0);
    /// <summary>Gets the negative Z unit vector.</summary>
    public static Vector3 Forward => new(0, 0, -1);
    /// <summary>Gets the positive Z unit vector.</summary>
    public static Vector3 Back => new(0, 0, 1);

    /// <summary>Identifies one vector component.</summary>
    public enum Axis
    {
        /// <summary>Identifies the X component.</summary>
        X = 0,

        /// <summary>Identifies the Y component.</summary>
        Y = 1,

        /// <summary>Identifies the Z component.</summary>
        Z = 2,

    }

    /// <summary>Gets or sets the X component.</summary>
    public float X;

    /// <summary>Gets or sets the Y component.</summary>
    public float Y;

    /// <summary>Gets or sets the Z component.</summary>
    public float Z;

    /// <summary>Gets the zero vector.</summary>
    /// <value><c>(0, 0, 0)</c>.</value>
    public static Vector3 Zero => ZeroValue;

    /// <summary>Gets the vector whose components are all one.</summary>
    /// <value><c>(1, 1, 1)</c>.</value>
    public static Vector3 One => OneValue;

    /// <summary>Gets the vector whose components are all positive infinity.</summary>
    /// <value><c>(+Infinity, +Infinity, +Infinity)</c>.</value>
    public static Vector3 Inf => InfValue;

    /// <summary>Gets or sets a component by axis index.</summary>
    /// <param name="index">An index from zero through two for X, Y, or Z.</param>
    /// <value>The selected component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside zero through two.</exception>
    public float this[int index]
    {
        readonly get => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Initializes a vector from three components.</summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Initializes a floating-point vector from an integer vector.</summary>
    /// <param name="value">The integer vector to convert.</param>
    /// <remarks>Large integer components can lose low-order precision during conversion.</remarks>
    public Vector3(Vector3I value)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
    }

    /// <summary>Deconstructs the vector into its three components.</summary>
    /// <param name="x">Receives <see cref="X"/>.</param>
    /// <param name="y">Receives <see cref="Y"/>.</param>
    /// <param name="z">Receives <see cref="Z"/>.</param>
    public readonly void Deconstruct(out float x, out float y, out float z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    /// <summary>Returns the componentwise absolute value.</summary>
    /// <returns>A vector with nonnegative components, except that NaN remains NaN.</returns>
    public readonly Vector3 Abs() => new(Mathf.Abs(X), Mathf.Abs(Y), Mathf.Abs(Z));

    /// <summary>Returns the unsigned angle to another vector in radians.</summary>
    /// <param name="to">The other vector.</param>
    /// <returns>The angle in radians.</returns>
    public readonly float AngleTo(Vector3 to) => Mathf.Atan2(Cross(to).Length(), Dot(to));

    /// <summary>Returns the derivative of a cubic Bezier curve at a parameter.</summary>
    /// <param name="control1">The first control point.</param>
    /// <param name="control2">The second control point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="t">The curve parameter.</param>
    /// <returns>The curve derivative.</returns>
    public readonly Vector3 BezierDerivative(Vector3 control1, Vector3 control2, Vector3 end, float t) => new(
        Mathf.BezierDerivative(X, control1.X, control2.X, end.X, t),
        Mathf.BezierDerivative(Y, control1.Y, control2.Y, end.Y, t),
        Mathf.BezierDerivative(Z, control1.Z, control2.Z, end.Z, t));

    /// <summary>Returns a point on a cubic Bezier curve.</summary>
    /// <param name="control1">The first control point.</param>
    /// <param name="control2">The second control point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="t">The curve parameter.</param>
    /// <returns>The interpolated point.</returns>
    public readonly Vector3 BezierInterpolate(Vector3 control1, Vector3 control2, Vector3 end, float t) => new(
        Mathf.BezierInterpolate(X, control1.X, control2.X, end.X, t),
        Mathf.BezierInterpolate(Y, control1.Y, control2.Y, end.Y, t),
        Mathf.BezierInterpolate(Z, control1.Z, control2.Z, end.Z, t));

    /// <summary>Returns the vector bounced from a plane with the supplied unit normal.</summary>
    /// <param name="normal">The unit plane normal.</param>
    /// <returns>The bounced vector.</returns>
    public readonly Vector3 Bounce(Vector3 normal) => -Reflect(normal);

    /// <summary>Rounds every component upward toward positive infinity.</summary>
    /// <returns>The componentwise ceiling.</returns>
    public readonly Vector3 Ceil() => new(Mathf.Ceil(X), Mathf.Ceil(Y), Mathf.Ceil(Z));

    /// <summary>Clamps each component between corresponding vector bounds.</summary>
    /// <param name="min">The componentwise lower bounds.</param>
    /// <param name="max">The componentwise upper bounds.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException">A lower bound is greater than its corresponding upper bound.</exception>
    public readonly Vector3 Clamp(Vector3 min, Vector3 max) => new(
        Mathf.Clamp(X, min.X, max.X),
        Mathf.Clamp(Y, min.Y, max.Y),
        Mathf.Clamp(Z, min.Z, max.Z));

    /// <summary>Clamps every component between scalar bounds.</summary>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public readonly Vector3 Clamp(float min, float max) => new(
        Mathf.Clamp(X, min, max), Mathf.Clamp(Y, min, max), Mathf.Clamp(Z, min, max));

    /// <summary>Returns the cross product with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The vector perpendicular to both inputs.</returns>
    public readonly Vector3 Cross(Vector3 with) => new(
        Y * with.Z - Z * with.Y, Z * with.X - X * with.Z, X * with.Y - Y * with.X);

    /// <summary>Performs Catmull-Rom cubic interpolation between this vector and another.</summary>
    /// <param name="b">The destination vector.</param>
    /// <param name="preA">The control vector before this vector.</param>
    /// <param name="postB">The control vector after <paramref name="b"/>.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated vector.</returns>
    public readonly Vector3 CubicInterpolate(Vector3 b, Vector3 preA, Vector3 postB, float weight) => new(
        Mathf.CubicInterpolate(X, b.X, preA.X, postB.X, weight),
        Mathf.CubicInterpolate(Y, b.Y, preA.Y, postB.Y, weight),
        Mathf.CubicInterpolate(Z, b.Z, preA.Z, postB.Z, weight));

    /// <summary>Performs time-aware Barry-Goldman cubic interpolation.</summary>
    /// <param name="b">The destination vector.</param>
    /// <param name="preA">The control vector before this vector.</param>
    /// <param name="postB">The control vector after <paramref name="b"/>.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <param name="bTime">The time assigned to <paramref name="b"/> relative to this vector at zero.</param>
    /// <param name="preATime">The time assigned to <paramref name="preA"/>.</param>
    /// <param name="postBTime">The time assigned to <paramref name="postB"/>.</param>
    /// <returns>The time-aware interpolated vector.</returns>
    public readonly Vector3 CubicInterpolateInTime(
        Vector3 b,
        Vector3 preA,
        Vector3 postB,
        float weight,
        float bTime,
        float preATime,
        float postBTime) => new(
            Mathf.CubicInterpolateInTime(X, b.X, preA.X, postB.X, weight, bTime, preATime, postBTime),
            Mathf.CubicInterpolateInTime(Y, b.Y, preA.Y, postB.Y, weight, bTime, preATime, postBTime),
            Mathf.CubicInterpolateInTime(Z, b.Z, preA.Z, postB.Z, weight, bTime, preATime, postBTime));

    /// <summary>Returns the normalized direction from this point to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The normalized difference, or <see cref="Zero"/> when the difference is zero or non-finite.</returns>
    public readonly Vector3 DirectionTo(Vector3 to) => (to - this).Normalized();

    /// <summary>Returns the squared Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The squared distance.</returns>
    public readonly float DistanceSquaredTo(Vector3 to) => (to - this).LengthSquared();

    /// <summary>Returns the Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The distance.</returns>
    public readonly float DistanceTo(Vector3 to) => (to - this).Length();

    /// <summary>Returns the dot product with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The sum of the three component products.</returns>
    public readonly float Dot(Vector3 with) => (X * with.X) + (Y * with.Y) + (Z * with.Z);

    /// <summary>Rounds every component downward toward negative infinity.</summary>
    /// <returns>The componentwise floor.</returns>
    public readonly Vector3 Floor() => new(Mathf.Floor(X), Mathf.Floor(Y), Mathf.Floor(Z));

    /// <summary>Returns the componentwise reciprocal.</summary>
    /// <returns><c>(1 / X, 1 / Y, 1 / Z)</c>, including IEEE 754 zero-division behavior.</returns>
    public readonly Vector3 Inverse() => new(1f / X, 1f / Y, 1f / Z);

    /// <summary>Tests whether every component is finite.</summary>
    /// <returns><see langword="true"/> when no component is NaN or infinity.</returns>
    public readonly bool IsFinite() => Mathf.IsFinite(X) && Mathf.IsFinite(Y) && Mathf.IsFinite(Z);

    /// <summary>Tests whether the squared length is approximately one.</summary>
    /// <returns><see langword="true"/> when the vector is approximately unit length.</returns>
    public readonly bool IsNormalized() => Mathf.Abs(LengthSquared() - 1f) < NormalizedEpsilon;

    /// <summary>Returns the Euclidean length.</summary>
    /// <returns>The square root of <see cref="LengthSquared"/>.</returns>
    public readonly float Length() => Mathf.Sqrt(LengthSquared());

    /// <summary>Returns the squared Euclidean length.</summary>
    /// <returns>The sum of the three squared components.</returns>
    public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);

    /// <summary>Linearly interpolates or extrapolates toward another vector.</summary>
    /// <param name="to">The destination vector.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The componentwise linear interpolation.</returns>
    public readonly Vector3 Lerp(Vector3 to, float weight) => this + ((to - this) * weight);

    /// <summary>Restricts the vector length to a maximum.</summary>
    /// <param name="length">The maximum length.</param>
    /// <returns>The capped vector.</returns>
    /// <remarks>A negative limit reverses a nonzero vector. Scaling divides by the current length before multiplying by the limit.</remarks>
    public readonly Vector3 LimitLength(float length = 1f)
    {
        var current = Length();
        return current > 0f && length < current ? this / current * length : this;
    }

    /// <summary>Returns the componentwise maximum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector3 Max(Vector3 with) => new(
        Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z));

    /// <summary>Returns the componentwise maximum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector3 Max(float with) => new(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with));

    /// <summary>Returns the axis containing the greatest component.</summary>
    /// <returns><see cref="Axis.X"/> when all components are equal; otherwise the first greatest axis.</returns>
    public readonly Axis MaxAxisIndex()
    {
        var index = Axis.X;
        var value = X;
        for (var current = 1; current < 3; current++)
        {
            if (this[current] > value)
            {
                index = (Axis)current;
                value = this[current];
            }
        }

        return index;
    }

    /// <summary>Returns the componentwise minimum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector3 Min(Vector3 with) => new(
        Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z));

    /// <summary>Returns the componentwise minimum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector3 Min(float with) => new(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with));

    /// <summary>Returns the axis containing the least component.</summary>
    /// <returns><see cref="Axis.Z"/> when all components are equal; otherwise the last least axis.</returns>
    /// <remarks>NaN components follow the fixed X/Y/Z comparison branches, which may select an unordered axis.</remarks>
    public readonly Axis MinAxisIndex()
    {
        if (X < Y)
            return X < Z ? Axis.X : Axis.Z;
        return Y < Z ? Axis.Y : Axis.Z;
    }

    /// <summary>Moves toward another vector by a signed distance without passing it.</summary>
    /// <param name="to">The destination.</param>
    /// <param name="delta">The signed travel distance.</param>
    /// <returns>The moved vector.</returns>
    /// <remarks>A separation below <c>0.00001</c> returns <paramref name="to"/> even for a negative step.</remarks>
    public readonly Vector3 MoveToward(Vector3 to, float delta)
    {
        var difference = to - this;
        var distance = difference.Length();
        return distance <= delta || distance < 0.00001f ? to : this + (difference / distance * delta);
    }

    /// <summary>Returns this vector scaled to unit length.</summary>
    /// <returns>A normalized vector, or <see cref="Zero"/> when the input is zero or non-finite.</returns>
    public readonly Vector3 Normalized()
    {
        if (!IsFinite()) return Zero;
        var squaredLength = LengthSquared();
        return squaredLength == 0f ? Zero : this / Mathf.Sqrt(squaredLength);
    }

    /// <summary>Decodes an octahedrally packed unit vector from a two-component value.</summary>
    /// <param name="uv">The encoded components in the unit square.</param>
    /// <returns>The decoded unit vector.</returns>
    /// <remarks>Out-of-square components retain their source values; the fold correction is clamped to one before normalization.</remarks>
    public static Vector3 OctahedronDecode(Vector2 uv)
    {
        var x = uv.X * 2f - 1f;
        var y = uv.Y * 2f - 1f;
        var z = 1f - Mathf.Abs(x) - Mathf.Abs(y);
        var correction = Mathf.Clamp(-z, 0f, 1f);
        x += x >= 0f ? -correction : correction;
        y += y >= 0f ? -correction : correction;
        return new Vector3(x, y, z).Normalized();
    }

    /// <summary>Octahedrally packs a unit vector into a two-component value.</summary>
    /// <returns>The packed components in the unit square.</returns>
    /// <remarks>A zero vector has undefined packed coordinates and returns NaN components.</remarks>
    public readonly Vector2 OctahedronEncode()
    {
        var denominator = Mathf.Abs(X) + Mathf.Abs(Y) + Mathf.Abs(Z);
        var x = X / denominator;
        var y = Y / denominator;
        if (Z < 0f)
        {
            var oldX = x;
            x = (1f - Mathf.Abs(y)) * (oldX >= 0f ? 1f : -1f);
            y = (1f - Mathf.Abs(oldX)) * (y >= 0f ? 1f : -1f);
        }
        return new Vector2(x * .5f + .5f, y * .5f + .5f);
    }

    /// <summary>Applies positive modulus to every component.</summary>
    /// <param name="mod">The scalar divisor.</param>
    /// <returns>The componentwise canonical remainder using the divisor's sign.</returns>
    public readonly Vector3 PosMod(float mod) => new(
        Mathf.PosMod(X, mod), Mathf.PosMod(Y, mod), Mathf.PosMod(Z, mod));

    /// <summary>Applies componentwise positive modulus.</summary>
    /// <param name="mod">The component divisors.</param>
    /// <returns>The componentwise canonical remainders using each divisor's sign.</returns>
    public readonly Vector3 PosMod(Vector3 mod) => new(
        Mathf.PosMod(X, mod.X), Mathf.PosMod(Y, mod.Y), Mathf.PosMod(Z, mod.Z));

    /// <summary>Projects this vector onto another vector.</summary>
    /// <param name="onNormal">The projection direction.</param>
    /// <returns>The parallel component.</returns>
    public readonly Vector3 Project(Vector3 onNormal) => onNormal * (Dot(onNormal) / onNormal.LengthSquared());

    /// <summary>Reflects this vector across a plane with a unit normal.</summary>
    /// <param name="normal">The unit plane normal.</param>
    /// <returns>The reflected vector.</returns>
    /// <remarks>The normal is multiplied by two before applying the dot scalar, retaining finite components near float limits.</remarks>
    public readonly Vector3 Reflect(Vector3 normal) => (2f * normal * Dot(normal)) - this;

    /// <summary>Rotates this vector about a unit axis by an angle in radians.</summary>
    /// <param name="axis">The unit rotation axis.</param>
    /// <param name="angle">The rotation angle in radians.</param>
    /// <returns>The rotated vector.</returns>
    /// <remarks>Uses the source axis-angle matrix coefficient order without exposing a spatial matrix type.</remarks>
    public readonly Vector3 Rotated(Vector3 axis, float angle)
    {
        var squared = axis * axis;
        var cosine = Mathf.Cos(angle);
        var sine = Mathf.Sin(angle);
        var factor = 1f - cosine;
        var xy = axis.X * axis.Y * factor;
        var zs = axis.Z * sine;
        var xz = axis.X * axis.Z * factor;
        var ys = axis.Y * sine;
        var yz = axis.Y * axis.Z * factor;
        var xs = axis.X * sine;
        return new Vector3(
            (squared.X + cosine * (1f - squared.X)) * X + (xy - zs) * Y + (xz + ys) * Z,
            (xy + zs) * X + (squared.Y + cosine * (1f - squared.Y)) * Y + (yz - xs) * Z,
            (xz - ys) * X + (yz + xs) * Y + (squared.Z + cosine * (1f - squared.Z)) * Z);
    }

    /// <summary>Rounds every component to the nearest integer using midpoint-to-even behavior.</summary>
    /// <returns>The componentwise rounded vector.</returns>
    public readonly Vector3 Round() => new(Mathf.Round(X), Mathf.Round(Y), Mathf.Round(Z));

    /// <summary>Returns the sign of every component.</summary>
    /// <returns>Components containing negative one, zero, or positive one.</returns>
    /// <exception cref="ArithmeticException">A component is NaN.</exception>
    public readonly Vector3 Sign() => new(Mathf.Sign(X), Mathf.Sign(Y), Mathf.Sign(Z));

    /// <summary>Returns the signed angle to another vector about an axis.</summary>
    /// <param name="to">The other vector.</param>
    /// <param name="axis">The axis selecting the angle sign.</param>
    /// <returns>The signed angle in radians.</returns>
    public readonly float SignedAngleTo(Vector3 to, Vector3 axis)
    {
        var cross = Cross(to);
        var unsigned = Mathf.Atan2(cross.Length(), Dot(to));
        return cross.Dot(axis) < 0f ? -unsigned : unsigned;
    }

    /// <summary>Interpolates direction on the unit sphere and linearly interpolates length.</summary>
    /// <param name="to">The destination vector.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The interpolated vector, or linear interpolation for zero length or parallel inputs.</returns>
    /// <remarks>Squared-length and rotation-axis checks precede interpolation; non-finite values follow IEEE arithmetic.</remarks>
    public readonly Vector3 Slerp(Vector3 to, float weight)
    {
        var startSquared = LengthSquared();
        var endSquared = to.LengthSquared();
        if (startSquared == 0f || endSquared == 0f)
            return Lerp(to, weight);
        var axis = Cross(to);
        var axisSquared = axis.LengthSquared();
        if (axisSquared == 0f)
            return Lerp(to, weight);
        axis /= Mathf.Sqrt(axisSquared);
        var startLength = Mathf.Sqrt(startSquared);
        var resultLength = Mathf.Lerp(startLength, Mathf.Sqrt(endSquared), weight);
        return Rotated(axis, AngleTo(to) * weight) * (resultLength / startLength);
    }

    /// <summary>Removes the component along a unit normal.</summary>
    /// <param name="normal">The unit normal.</param>
    /// <returns>The remaining tangent component.</returns>
    public readonly Vector3 Slide(Vector3 normal) => this - normal * Dot(normal);

    /// <summary>Snaps each component to the nearest multiple of the corresponding step.</summary>
    /// <param name="step">The componentwise step. A zero component leaves the corresponding value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    public readonly Vector3 Snapped(Vector3 step) => new(
        Mathf.Snapped(X, step.X), Mathf.Snapped(Y, step.Y), Mathf.Snapped(Z, step.Z));

    /// <summary>Snaps every component to the nearest multiple of a scalar step.</summary>
    /// <param name="step">The scalar step. Zero leaves every value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    public readonly Vector3 Snapped(float step) => new(
        Mathf.Snapped(X, step), Mathf.Snapped(Y, step), Mathf.Snapped(Z, step));

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise sum.</returns>
    public static Vector3 operator +(Vector3 left, Vector3 right) => new(
        left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>Returns a vector unchanged.</summary>
    /// <param name="value">The vector.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static Vector3 operator +(Vector3 value) => value;

    /// <summary>Subtracts two vectors componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The componentwise difference.</returns>
    public static Vector3 operator -(Vector3 left, Vector3 right) => new(
        left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>Negates every component.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>The componentwise negation.</returns>
    public static Vector3 operator -(Vector3 value) => new(-value.X, -value.Y, -value.Z);

    /// <summary>Multiplies a vector by a scalar.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector3 operator *(Vector3 vector, float scalar) => new(
        vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    /// <summary>Multiplies a scalar by a vector.</summary>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector3 operator *(float scalar, Vector3 vector) => vector * scalar;

    /// <summary>Multiplies two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector3 operator *(Vector3 left, Vector3 right) => new(
        left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <summary>Divides every component by a scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Vector3 operator /(Vector3 vector, float divisor) => new(
        vector.X / divisor, vector.Y / divisor, vector.Z / divisor);

    /// <summary>Divides two vectors componentwise.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Vector3 operator /(Vector3 left, Vector3 right) => new(
        left.X / right.X, left.Y / right.Y, left.Z / right.Z);

    /// <summary>Returns the truncated remainder of every component by a scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise remainder.</returns>
    public static Vector3 operator %(Vector3 vector, float divisor) => new(
        vector.X % divisor, vector.Y % divisor, vector.Z % divisor);

    /// <summary>Returns the truncated componentwise remainder of two vectors.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise remainder.</returns>
    public static Vector3 operator %(Vector3 left, Vector3 right) => new(
        left.X % right.X, left.Y % right.Y, left.Z % right.Z);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);

    /// <summary>Tests whether any component differs.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when a corresponding component differs.</returns>
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Vector3 left, Vector3 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                return left.Z < right.Z;
            }
            return left.Y < right.Y;
        }
        return left.X < right.X;
    }

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Vector3 left, Vector3 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                return left.Z > right.Z;
            }
            return left.Y > right.Y;
        }
        return left.X > right.X;
    }

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Vector3 left, Vector3 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                return left.Z <= right.Z;
            }
            return left.Y < right.Y;
        }
        return left.X < right.X;
    }

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Vector3 left, Vector3 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                return left.Z >= right.Z;
            }
            return left.Y > right.Y;
        }
        return left.X > right.X;
    }

    /// <summary>Tests whether another object is an exactly equal vector.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a vector with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector3 other && Equals(other);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public readonly bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <summary>Tests every component for scale-aware approximate equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when all corresponding components are approximately equal.</returns>
    public readonly bool IsEqualApprox(Vector3 other) =>
        Mathf.IsEqualApprox(X, other.X) && Mathf.IsEqualApprox(Y, other.Y) &&
        Mathf.IsEqualApprox(Z, other.Z);

    /// <summary>Tests whether every component is approximately zero.</summary>
    /// <returns><see langword="true"/> when every component is within the zero tolerance.</returns>
    public readonly bool IsZeroApprox() =>
        Mathf.IsZeroApprox(X) && Mathf.IsZeroApprox(Y) &&
        Mathf.IsZeroApprox(Z);

    /// <summary>Returns a hash code based on all components.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>Formats every component using invariant culture.</summary>
    /// <returns>A parenthesized component tuple.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats every component with a numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format, or <see langword="null"/> for the default.</param>
    /// <returns>A parenthesized component tuple.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) =>
        $"({X.ToString(format, CultureInfo.InvariantCulture)}, {Y.ToString(format, CultureInfo.InvariantCulture)}, {Z.ToString(format, CultureInfo.InvariantCulture)})";

}
