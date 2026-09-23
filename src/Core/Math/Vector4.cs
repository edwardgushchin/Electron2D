using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a four-component floating-point vector for numeric tuples.</summary>
/// <remarks>
/// Ordinary arithmetic preserves IEEE 754 NaN and infinity values. The zero-initialized value is <see cref="Zero"/>.
/// Numeric operations do not allocate managed memory; string formatting allocates a string.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector4 : IEquatable<Vector4>
{
    private const float NormalizedEpsilon = 0.001f;

    private static readonly Vector4 ZeroValue = new(0f, 0f, 0f, 0f);
    private static readonly Vector4 OneValue = new(1f, 1f, 1f, 1f);
    private static readonly Vector4 InfValue = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    /// <summary>Identifies one vector component.</summary>
    public enum Axis
    {
        /// <summary>Identifies the X component.</summary>
        X = 0,

        /// <summary>Identifies the Y component.</summary>
        Y = 1,

        /// <summary>Identifies the Z component.</summary>
        Z = 2,

        /// <summary>Identifies the W component.</summary>
        W = 3,
    }

    /// <summary>Gets or sets the X component.</summary>
    public float X;

    /// <summary>Gets or sets the Y component.</summary>
    public float Y;

    /// <summary>Gets or sets the Z component.</summary>
    public float Z;

    /// <summary>Gets or sets the W component.</summary>
    public float W;

    /// <summary>Gets the zero vector.</summary>
    /// <value><c>(0, 0, 0, 0)</c>.</value>
    public static Vector4 Zero => ZeroValue;

    /// <summary>Gets the vector whose components are all one.</summary>
    /// <value><c>(1, 1, 1, 1)</c>.</value>
    public static Vector4 One => OneValue;

    /// <summary>Gets the vector whose components are all positive infinity.</summary>
    /// <value><c>(+Infinity, +Infinity, +Infinity, +Infinity)</c>.</value>
    public static Vector4 Inf => InfValue;

    /// <summary>Gets or sets a component by axis index.</summary>
    /// <param name="index">An index from zero through three for X, Y, Z, or W.</param>
    /// <value>The selected component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside zero through three.</exception>
    public float this[int index]
    {
        readonly get => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            3 => W,
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
                case 3:
                    W = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Initializes a vector from four components.</summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>Initializes a floating-point vector from an integer vector.</summary>
    /// <param name="value">The integer vector to convert.</param>
    /// <remarks>Large integer components can lose low-order precision during conversion.</remarks>
    public Vector4(Vector4I value)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
        W = value.W;
    }

    /// <summary>Deconstructs the vector into its four components.</summary>
    /// <param name="x">Receives <see cref="X"/>.</param>
    /// <param name="y">Receives <see cref="Y"/>.</param>
    /// <param name="z">Receives <see cref="Z"/>.</param>
    /// <param name="w">Receives <see cref="W"/>.</param>
    public readonly void Deconstruct(out float x, out float y, out float z, out float w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    /// <summary>Returns the componentwise absolute value.</summary>
    /// <returns>A vector with nonnegative components, except that NaN remains NaN.</returns>
    public readonly Vector4 Abs() => new(MathF.Abs(X), MathF.Abs(Y), MathF.Abs(Z), MathF.Abs(W));

    /// <summary>Rounds every component upward toward positive infinity.</summary>
    /// <returns>The componentwise ceiling.</returns>
    public readonly Vector4 Ceil() => new(MathF.Ceil(X), MathF.Ceil(Y), MathF.Ceil(Z), MathF.Ceil(W));

    /// <summary>Clamps each component between corresponding vector bounds.</summary>
    /// <param name="min">The componentwise lower bounds.</param>
    /// <param name="max">The componentwise upper bounds.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException">A lower bound is greater than its corresponding upper bound.</exception>
    public readonly Vector4 Clamp(Vector4 min, Vector4 max) => new(
        MathF.Clamp(X, min.X, max.X),
        MathF.Clamp(Y, min.Y, max.Y),
        MathF.Clamp(Z, min.Z, max.Z),
        MathF.Clamp(W, min.W, max.W));

    /// <summary>Clamps every component between scalar bounds.</summary>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public readonly Vector4 Clamp(float min, float max) => new(
        MathF.Clamp(X, min, max), MathF.Clamp(Y, min, max), MathF.Clamp(Z, min, max), MathF.Clamp(W, min, max));

    /// <summary>Performs Catmull-Rom cubic interpolation between this vector and another.</summary>
    /// <param name="b">The destination vector.</param>
    /// <param name="preA">The control vector before this vector.</param>
    /// <param name="postB">The control vector after <paramref name="b"/>.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated vector.</returns>
    public readonly Vector4 CubicInterpolate(Vector4 b, Vector4 preA, Vector4 postB, float weight) => new(
        MathF.CubicInterpolate(X, b.X, preA.X, postB.X, weight),
        MathF.CubicInterpolate(Y, b.Y, preA.Y, postB.Y, weight),
        MathF.CubicInterpolate(Z, b.Z, preA.Z, postB.Z, weight),
        MathF.CubicInterpolate(W, b.W, preA.W, postB.W, weight));

    /// <summary>Performs time-aware Barry-Goldman cubic interpolation.</summary>
    /// <param name="b">The destination vector.</param>
    /// <param name="preA">The control vector before this vector.</param>
    /// <param name="postB">The control vector after <paramref name="b"/>.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <param name="bTime">The time assigned to <paramref name="b"/> relative to this vector at zero.</param>
    /// <param name="preATime">The time assigned to <paramref name="preA"/>.</param>
    /// <param name="postBTime">The time assigned to <paramref name="postB"/>.</param>
    /// <returns>The time-aware interpolated vector.</returns>
    public readonly Vector4 CubicInterpolateInTime(
        Vector4 b,
        Vector4 preA,
        Vector4 postB,
        float weight,
        float bTime,
        float preATime,
        float postBTime) => new(
            MathF.CubicInterpolateInTime(X, b.X, preA.X, postB.X, weight, bTime, preATime, postBTime),
            MathF.CubicInterpolateInTime(Y, b.Y, preA.Y, postB.Y, weight, bTime, preATime, postBTime),
            MathF.CubicInterpolateInTime(Z, b.Z, preA.Z, postB.Z, weight, bTime, preATime, postBTime),
            MathF.CubicInterpolateInTime(W, b.W, preA.W, postB.W, weight, bTime, preATime, postBTime));

    /// <summary>Returns the normalized direction from this point to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The normalized difference, or <see cref="Zero"/> when both points are equal.</returns>
    public readonly Vector4 DirectionTo(Vector4 to) => (to - this).Normalized();

    /// <summary>Returns the squared Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The squared distance.</returns>
    public readonly float DistanceSquaredTo(Vector4 to) => (to - this).LengthSquared();

    /// <summary>Returns the Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The distance.</returns>
    public readonly float DistanceTo(Vector4 to) => (to - this).Length();

    /// <summary>Returns the dot product with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The sum of the four component products.</returns>
    public readonly float Dot(Vector4 with) => (X * with.X) + (Y * with.Y) + (Z * with.Z) + (W * with.W);

    /// <summary>Rounds every component downward toward negative infinity.</summary>
    /// <returns>The componentwise floor.</returns>
    public readonly Vector4 Floor() => new(MathF.Floor(X), MathF.Floor(Y), MathF.Floor(Z), MathF.Floor(W));

    /// <summary>Returns the componentwise reciprocal.</summary>
    /// <returns><c>(1 / X, 1 / Y, 1 / Z, 1 / W)</c>, including IEEE 754 zero-division behavior.</returns>
    public readonly Vector4 Inverse() => new(1f / X, 1f / Y, 1f / Z, 1f / W);

    /// <summary>Tests whether every component is finite.</summary>
    /// <returns><see langword="true"/> when no component is NaN or infinity.</returns>
    public readonly bool IsFinite() => MathF.IsFinite(X) && MathF.IsFinite(Y) && MathF.IsFinite(Z) && MathF.IsFinite(W);

    /// <summary>Tests whether the squared length is approximately one.</summary>
    /// <returns><see langword="true"/> when the vector is approximately unit length.</returns>
    public readonly bool IsNormalized() => MathF.Abs(LengthSquared() - 1f) < NormalizedEpsilon;

    /// <summary>Returns the Euclidean length.</summary>
    /// <returns>The square root of <see cref="LengthSquared"/>.</returns>
    public readonly float Length() => MathF.Sqrt(LengthSquared());

    /// <summary>Returns the squared Euclidean length.</summary>
    /// <returns>The sum of the four squared components.</returns>
    public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z) + (W * W);

    /// <summary>Linearly interpolates or extrapolates toward another vector.</summary>
    /// <param name="to">The destination vector.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The componentwise linear interpolation.</returns>
    public readonly Vector4 Lerp(Vector4 to, float weight) => this + ((to - this) * weight);

    /// <summary>Returns the componentwise maximum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector4 Max(Vector4 with) => new(
        MathF.Max(X, with.X), MathF.Max(Y, with.Y), MathF.Max(Z, with.Z), MathF.Max(W, with.W));

    /// <summary>Returns the componentwise maximum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector4 Max(float with) => new(MathF.Max(X, with), MathF.Max(Y, with), MathF.Max(Z, with), MathF.Max(W, with));

    /// <summary>Returns the axis containing the greatest component.</summary>
    /// <returns><see cref="Axis.X"/> when all components are equal; otherwise the first greatest axis.</returns>
    public readonly Axis MaxAxisIndex()
    {
        var index = Axis.X;
        var value = X;
        for (var current = 1; current < 4; current++)
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
    public readonly Vector4 Min(Vector4 with) => new(
        MathF.Min(X, with.X), MathF.Min(Y, with.Y), MathF.Min(Z, with.Z), MathF.Min(W, with.W));

    /// <summary>Returns the componentwise minimum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector4 Min(float with) => new(MathF.Min(X, with), MathF.Min(Y, with), MathF.Min(Z, with), MathF.Min(W, with));

    /// <summary>Returns the axis containing the least component.</summary>
    /// <returns><see cref="Axis.W"/> when all components are equal; otherwise the last least axis.</returns>
    public readonly Axis MinAxisIndex()
    {
        var index = Axis.X;
        var value = X;
        for (var current = 1; current < 4; current++)
        {
            if (this[current] <= value)
            {
                index = (Axis)current;
                value = this[current];
            }
        }

        return index;
    }

    /// <summary>Returns this vector scaled to unit length.</summary>
    /// <returns>A normalized vector, or <see cref="Zero"/> when the squared length is exactly zero.</returns>
    public readonly Vector4 Normalized()
    {
        var squaredLength = LengthSquared();
        return squaredLength == 0f ? Zero : this / MathF.Sqrt(squaredLength);
    }

    /// <summary>Applies positive modulus to every component.</summary>
    /// <param name="mod">The scalar divisor.</param>
    /// <returns>The componentwise canonical remainder using the divisor's sign.</returns>
    public readonly Vector4 PosMod(float mod) => new(
        MathF.PosMod(X, mod), MathF.PosMod(Y, mod), MathF.PosMod(Z, mod), MathF.PosMod(W, mod));

    /// <summary>Applies componentwise positive modulus.</summary>
    /// <param name="mod">The component divisors.</param>
    /// <returns>The componentwise canonical remainders using each divisor's sign.</returns>
    public readonly Vector4 PosMod(Vector4 mod) => new(
        MathF.PosMod(X, mod.X), MathF.PosMod(Y, mod.Y), MathF.PosMod(Z, mod.Z), MathF.PosMod(W, mod.W));

    /// <summary>Rounds every component to the nearest integer using midpoint-to-even behavior.</summary>
    /// <returns>The componentwise rounded vector.</returns>
    public readonly Vector4 Round() => new(MathF.Round(X), MathF.Round(Y), MathF.Round(Z), MathF.Round(W));

    /// <summary>Returns the sign of every component.</summary>
    /// <returns>Components containing negative one, zero, or positive one.</returns>
    /// <exception cref="ArithmeticException">A component is NaN.</exception>
    public readonly Vector4 Sign() => new(MathF.Sign(X), MathF.Sign(Y), MathF.Sign(Z), MathF.Sign(W));

    /// <summary>Snaps each component to the nearest multiple of the corresponding step.</summary>
    /// <param name="step">The componentwise step. A zero component leaves the corresponding value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    public readonly Vector4 Snapped(Vector4 step) => new(
        MathF.Snapped(X, step.X), MathF.Snapped(Y, step.Y), MathF.Snapped(Z, step.Z), MathF.Snapped(W, step.W));

    /// <summary>Snaps every component to the nearest multiple of a scalar step.</summary>
    /// <param name="step">The scalar step. Zero leaves every value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    public readonly Vector4 Snapped(float step) => new(
        MathF.Snapped(X, step), MathF.Snapped(Y, step), MathF.Snapped(Z, step), MathF.Snapped(W, step));

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise sum.</returns>
    public static Vector4 operator +(Vector4 left, Vector4 right) => new(
        left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    /// <summary>Returns a vector unchanged.</summary>
    /// <param name="value">The vector.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static Vector4 operator +(Vector4 value) => value;

    /// <summary>Subtracts two vectors componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The componentwise difference.</returns>
    public static Vector4 operator -(Vector4 left, Vector4 right) => new(
        left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    /// <summary>Negates every component.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>The componentwise negation.</returns>
    public static Vector4 operator -(Vector4 value) => new(-value.X, -value.Y, -value.Z, -value.W);

    /// <summary>Multiplies a vector by a scalar.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector4 operator *(Vector4 vector, float scalar) => new(
        vector.X * scalar, vector.Y * scalar, vector.Z * scalar, vector.W * scalar);

    /// <summary>Multiplies a scalar by a vector.</summary>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector4 operator *(float scalar, Vector4 vector) => vector * scalar;

    /// <summary>Multiplies two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector4 operator *(Vector4 left, Vector4 right) => new(
        left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);

    /// <summary>Divides every component by a scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Vector4 operator /(Vector4 vector, float divisor) => new(
        vector.X / divisor, vector.Y / divisor, vector.Z / divisor, vector.W / divisor);

    /// <summary>Divides two vectors componentwise.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Vector4 operator /(Vector4 left, Vector4 right) => new(
        left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);

    /// <summary>Returns the truncated remainder of every component by a scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise remainder.</returns>
    public static Vector4 operator %(Vector4 vector, float divisor) => new(
        vector.X % divisor, vector.Y % divisor, vector.Z % divisor, vector.W % divisor);

    /// <summary>Returns the truncated componentwise remainder of two vectors.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise remainder.</returns>
    public static Vector4 operator %(Vector4 left, Vector4 right) => new(
        left.X % right.X, left.Y % right.Y, left.Z % right.Z, left.W % right.W);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public static bool operator ==(Vector4 left, Vector4 right) => left.Equals(right);

    /// <summary>Tests whether any component differs.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when a corresponding component differs.</returns>
    public static bool operator !=(Vector4 left, Vector4 right) => !left.Equals(right);

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Vector4 left, Vector4 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                if (left.Z == right.Z)
                    return left.W < right.W;
                return left.Z < right.Z;
            }
            return left.Y < right.Y;
        }
        return left.X < right.X;
    }

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Vector4 left, Vector4 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                if (left.Z == right.Z)
                    return left.W > right.W;
                return left.Z > right.Z;
            }
            return left.Y > right.Y;
        }
        return left.X > right.X;
    }

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Vector4 left, Vector4 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                if (left.Z == right.Z)
                    return left.W <= right.W;
                return left.Z < right.Z;
            }
            return left.Y < right.Y;
        }
        return left.X < right.X;
    }

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Vector4 left, Vector4 right)
    {
        if (left.X == right.X)
        {
            if (left.Y == right.Y)
            {
                if (left.Z == right.Z)
                    return left.W >= right.W;
                return left.Z > right.Z;
            }
            return left.Y > right.Y;
        }
        return left.X > right.X;
    }

    /// <summary>Tests whether another object is an exactly equal vector.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a vector with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector4 other && Equals(other);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public readonly bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    /// <summary>Tests every component for scale-aware approximate equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when all corresponding components are approximately equal.</returns>
    public readonly bool IsEqualApprox(Vector4 other) =>
        MathF.IsEqualApprox(X, other.X) && MathF.IsEqualApprox(Y, other.Y) &&
        MathF.IsEqualApprox(Z, other.Z) && MathF.IsEqualApprox(W, other.W);

    /// <summary>Tests whether every component is approximately zero.</summary>
    /// <returns><see langword="true"/> when every component is within the zero tolerance.</returns>
    public readonly bool IsZeroApprox() =>
        MathF.IsZeroApprox(X) && MathF.IsZeroApprox(Y) &&
        MathF.IsZeroApprox(Z) && MathF.IsZeroApprox(W);

    /// <summary>Returns a hash code based on all components.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <summary>Formats every component using invariant culture.</summary>
    /// <returns>A parenthesized component tuple.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats every component with a numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format, or <see langword="null"/> for the default.</param>
    /// <returns>A parenthesized component tuple.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) =>
        $"({X.ToString(format, CultureInfo.InvariantCulture)}, {Y.ToString(format, CultureInfo.InvariantCulture)}, {Z.ToString(format, CultureInfo.InvariantCulture)}, {W.ToString(format, CultureInfo.InvariantCulture)})";

}
