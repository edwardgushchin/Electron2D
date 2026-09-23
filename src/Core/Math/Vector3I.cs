using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a three-component integer vector for masks and numeric tuples.</summary>
/// <remarks>
/// Addition, subtraction, multiplication, and negation use wrapping 32-bit arithmetic.
/// Division and remainder follow C# truncated-division rules. The zero-initialized value is <see cref="Zero"/>.
/// Numeric operations do not allocate managed memory; string formatting allocates a string.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector3I : IEquatable<Vector3I>
{
    private static readonly Vector3I MinValueValue = new(int.MinValue, int.MinValue, int.MinValue);
    private static readonly Vector3I MaxValueValue = new(int.MaxValue, int.MaxValue, int.MaxValue);
    private static readonly Vector3I ZeroValue = new(0, 0, 0);
    private static readonly Vector3I OneValue = new(1, 1, 1);

    /// <summary>Gets the positive X unit vector.</summary>
    public static Vector3I Right => new(1, 0, 0);
    /// <summary>Gets the negative X unit vector.</summary>
    public static Vector3I Left => new(-1, 0, 0);
    /// <summary>Gets the positive Y unit vector.</summary>
    public static Vector3I Up => new(0, 1, 0);
    /// <summary>Gets the negative Y unit vector.</summary>
    public static Vector3I Down => new(0, -1, 0);
    /// <summary>Gets the negative Z unit vector.</summary>
    public static Vector3I Forward => new(0, 0, -1);
    /// <summary>Gets the positive Z unit vector.</summary>
    public static Vector3I Back => new(0, 0, 1);

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
    public int X;

    /// <summary>Gets or sets the Y component.</summary>
    public int Y;

    /// <summary>Gets or sets the Z component.</summary>
    public int Z;

    /// <summary>Gets the vector containing the minimum 32-bit integer in every component.</summary>
    /// <value><c>(int.MinValue, int.MinValue, int.MinValue)</c>.</value>
    public static Vector3I MinValue => MinValueValue;

    /// <summary>Gets the vector containing the maximum 32-bit integer in every component.</summary>
    /// <value><c>(int.MaxValue, int.MaxValue, int.MaxValue)</c>.</value>
    public static Vector3I MaxValue => MaxValueValue;

    /// <summary>Gets the zero vector.</summary>
    /// <value><c>(0, 0, 0)</c>.</value>
    public static Vector3I Zero => ZeroValue;

    /// <summary>Gets the vector whose components are all one.</summary>
    /// <value><c>(1, 1, 1)</c>.</value>
    public static Vector3I One => OneValue;

    /// <summary>Gets or sets a component by axis index.</summary>
    /// <param name="index">An index from zero through two for X, Y, or Z.</param>
    /// <value>The selected component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside zero through two.</exception>
    public int this[int index]
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

    /// <summary>Initializes an integer vector from three components.</summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vector3I(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Initializes an integer vector by truncating a finite floating-point vector toward zero.</summary>
    /// <param name="value">The floating-point vector to convert.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public Vector3I(Vector3 value)
    {
        ValidateConvertible(value.X, nameof(value));
        ValidateConvertible(value.Y, nameof(value));
        ValidateConvertible(value.Z, nameof(value));
        X = (int)value.X;
        Y = (int)value.Y;
        Z = (int)value.Z;
    }

    /// <summary>Deconstructs the vector into its three components.</summary>
    /// <param name="x">Receives <see cref="X"/>.</param>
    /// <param name="y">Receives <see cref="Y"/>.</param>
    /// <param name="z">Receives <see cref="Z"/>.</param>
    public readonly void Deconstruct(out int x, out int y, out int z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    /// <summary>Returns the componentwise absolute value.</summary>
    /// <returns>A vector with nonnegative components.</returns>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/>.</exception>
    public readonly Vector3I Abs() => new(Mathf.Abs(X), Mathf.Abs(Y), Mathf.Abs(Z));

    /// <summary>Clamps each component between corresponding vector bounds.</summary>
    /// <param name="min">The componentwise lower bounds.</param>
    /// <param name="max">The componentwise upper bounds.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException">A lower bound is greater than its corresponding upper bound.</exception>
    public readonly Vector3I Clamp(Vector3I min, Vector3I max) => new(
        Mathf.Clamp(X, min.X, max.X),
        Mathf.Clamp(Y, min.Y, max.Y),
        Mathf.Clamp(Z, min.Z, max.Z));

    /// <summary>Clamps every component between scalar bounds.</summary>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public readonly Vector3I Clamp(int min, int max) => new(
        Mathf.Clamp(X, min, max), Mathf.Clamp(Y, min, max), Mathf.Clamp(Z, min, max));

    /// <summary>Returns the squared Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The exact squared distance when it fits in a signed 64-bit integer.</returns>
    /// <remarks>Coordinate differences are widened before subtraction. This operation does not use wrapping vector subtraction.</remarks>
    /// <exception cref="OverflowException">The squared distance exceeds <see cref="long.MaxValue"/>.</exception>
    public readonly long DistanceSquaredTo(Vector3I to)
    {
        var dx = (long)to.X - X;
        var dy = (long)to.Y - Y;
        var dz = (long)to.Z - Z;
        return checked((dx * dx) + (dy * dy) + (dz * dz));
    }

    /// <summary>Returns the Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The nonnegative distance, rounded to single precision.</returns>
    /// <remarks>Coordinate differences and squared terms use widened arithmetic, so this remains finite even when <see cref="DistanceSquaredTo(Vector3I)"/> exceeds the signed 64-bit range.</remarks>
    public readonly float DistanceTo(Vector3I to)
    {
        var dx = (long)to.X - X;
        var dy = (long)to.Y - Y;
        var dz = (long)to.Z - Z;
        return (float)Mathf.Sqrt(((double)dx * dx) + ((double)dy * dy) + ((double)dz * dz));
    }

    /// <summary>Returns the Euclidean length.</summary>
    /// <returns>The nonnegative length, rounded to single precision.</returns>
    /// <remarks>Squared terms use widened arithmetic, so this remains finite even when <see cref="LengthSquared"/> exceeds the signed 64-bit range.</remarks>
    public readonly float Length() => (float)Mathf.Sqrt(((double)X * X) + ((double)Y * Y) + ((double)Z * Z));

    /// <summary>Returns the squared Euclidean length.</summary>
    /// <returns>The exact squared length when it fits in a signed 64-bit integer.</returns>
    /// <exception cref="OverflowException">The squared length exceeds <see cref="long.MaxValue"/>.</exception>
    public readonly long LengthSquared() => checked(((long)X * X) + ((long)Y * Y) + ((long)Z * Z));

    /// <summary>Returns the componentwise maximum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector3I Max(Vector3I with) => new(
        Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z));

    /// <summary>Returns the componentwise maximum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector3I Max(int with) => new(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with));

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
    public readonly Vector3I Min(Vector3I with) => new(
        Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z));

    /// <summary>Returns the componentwise minimum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector3I Min(int with) => new(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with));

    /// <summary>Returns the axis containing the least component.</summary>
    /// <returns><see cref="Axis.Z"/> when all components are equal; otherwise the last least axis.</returns>
    public readonly Axis MinAxisIndex()
    {
        var index = Axis.X;
        var value = X;
        for (var current = 1; current < 3; current++)
        {
            if (this[current] <= value)
            {
                index = (Axis)current;
                value = this[current];
            }
        }

        return index;
    }

    /// <summary>Returns the sign of every component.</summary>
    /// <returns>Components containing negative one, zero, or positive one.</returns>
    public readonly Vector3I Sign() => new(Mathf.Sign(X), Mathf.Sign(Y), Mathf.Sign(Z));

    /// <summary>Snaps each component to the nearest multiple of the corresponding step.</summary>
    /// <param name="step">The componentwise step. A zero component leaves the corresponding value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    /// <exception cref="OverflowException">A snapped component is outside the 32-bit signed integer range.</exception>
    public readonly Vector3I Snapped(Vector3I step) => new(
        Snap(X, step.X), Snap(Y, step.Y), Snap(Z, step.Z));

    /// <summary>Snaps every component to the nearest multiple of a scalar step.</summary>
    /// <param name="step">The scalar step. Zero leaves every value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    /// <exception cref="OverflowException">A snapped component is outside the 32-bit signed integer range.</exception>
    public readonly Vector3I Snapped(int step) => new(Snap(X, step), Snap(Y, step), Snap(Z, step));

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The wrapping componentwise sum.</returns>
    public static Vector3I operator +(Vector3I left, Vector3I right) => new(
        unchecked(left.X + right.X), unchecked(left.Y + right.Y), unchecked(left.Z + right.Z));

    /// <summary>Returns a vector unchanged.</summary>
    /// <param name="value">The vector.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static Vector3I operator +(Vector3I value) => value;

    /// <summary>Subtracts two vectors componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The wrapping componentwise difference.</returns>
    public static Vector3I operator -(Vector3I left, Vector3I right) => new(
        unchecked(left.X - right.X), unchecked(left.Y - right.Y), unchecked(left.Z - right.Z));

    /// <summary>Negates every component.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>The wrapping componentwise negation.</returns>
    public static Vector3I operator -(Vector3I value) => new(
        unchecked(-value.X), unchecked(-value.Y), unchecked(-value.Z));

    /// <summary>Multiplies a vector by an integer scalar.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector3I operator *(Vector3I vector, int scalar) => new(
        unchecked(vector.X * scalar), unchecked(vector.Y * scalar), unchecked(vector.Z * scalar));

    /// <summary>Multiplies an integer scalar by a vector.</summary>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector3I operator *(int scalar, Vector3I vector) => vector * scalar;

    /// <summary>Multiplies an integer vector by a floating-point scalar.</summary>
    /// <param name="vector">The integer vector.</param>
    /// <param name="scalar">The floating-point multiplier.</param>
    /// <returns>A floating-point componentwise product.</returns>
    public static Vector3 operator *(Vector3I vector, float scalar) => new(
        vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    /// <summary>Multiplies a floating-point scalar by an integer vector.</summary>
    /// <param name="scalar">The floating-point multiplier.</param>
    /// <param name="vector">The integer vector.</param>
    /// <returns>A floating-point componentwise product.</returns>
    public static Vector3 operator *(float scalar, Vector3I vector) => vector * scalar;

    /// <summary>Multiplies two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector3I operator *(Vector3I left, Vector3I right) => new(
        unchecked(left.X * right.X), unchecked(left.Y * right.Y), unchecked(left.Z * right.Z));

    /// <summary>Divides every component by an integer scalar using truncated division.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static Vector3I operator /(Vector3I vector, int divisor) => new(
        vector.X / divisor, vector.Y / divisor, vector.Z / divisor);

    /// <summary>Divides an integer vector by a floating-point scalar.</summary>
    /// <param name="vector">The integer dividend.</param>
    /// <param name="divisor">The floating-point divisor.</param>
    /// <returns>The IEEE 754 floating-point componentwise quotient.</returns>
    public static Vector3 operator /(Vector3I vector, float divisor) => new(
        vector.X / divisor, vector.Y / divisor, vector.Z / divisor);

    /// <summary>Divides two vectors componentwise using truncated division.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise quotient.</returns>
    /// <exception cref="DivideByZeroException">A component of <paramref name="right"/> is zero.</exception>
    /// <exception cref="OverflowException">A minimum-valued dividend component has a divisor of negative one.</exception>
    public static Vector3I operator /(Vector3I left, Vector3I right) => new(
        left.X / right.X, left.Y / right.Y, left.Z / right.Z);

    /// <summary>Returns the truncated remainder of every component by an integer scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise remainder.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static Vector3I operator %(Vector3I vector, int divisor) => new(
        vector.X % divisor, vector.Y % divisor, vector.Z % divisor);

    /// <summary>Returns the truncated componentwise remainder of two vectors.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise remainder.</returns>
    /// <exception cref="DivideByZeroException">A component of <paramref name="right"/> is zero.</exception>
    /// <exception cref="OverflowException">A minimum-valued dividend component has a divisor of negative one.</exception>
    public static Vector3I operator %(Vector3I left, Vector3I right) => new(
        left.X % right.X, left.Y % right.Y, left.Z % right.Z);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public static bool operator ==(Vector3I left, Vector3I right) => left.Equals(right);

    /// <summary>Tests whether any component differs.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when a corresponding component differs.</returns>
    public static bool operator !=(Vector3I left, Vector3I right) => !left.Equals(right);

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Vector3I left, Vector3I right) => Compare(left, right) < 0;

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Vector3I left, Vector3I right) => Compare(left, right) > 0;

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Vector3I left, Vector3I right) => Compare(left, right) <= 0;

    /// <summary>Compares vectors lexicographically by X, Y, then Z.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Vector3I left, Vector3I right) => Compare(left, right) >= 0;

    /// <summary>Converts an integer vector to a floating-point vector.</summary>
    /// <param name="value">The integer vector to convert.</param>
    /// <returns>A floating-point vector with corresponding components.</returns>
    /// <remarks>Large integer components can lose low-order precision.</remarks>
    public static implicit operator Vector3(Vector3I value) => new(value);

    /// <summary>Converts a finite in-range floating-point vector by truncating every component toward zero.</summary>
    /// <param name="value">The floating-point vector to convert.</param>
    /// <returns>The truncated integer vector.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public static explicit operator Vector3I(Vector3 value) => new(value);

    /// <summary>Tests whether another object is an equal integer vector.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an integer vector with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector3I other && Equals(other);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public readonly bool Equals(Vector3I other) => X == other.X && Y == other.Y && Z == other.Z;

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

    private static int Compare(Vector3I left, Vector3I right)
    {
        if (left.X != right.X)
            return left.X < right.X ? -1 : 1;
        if (left.Y != right.Y)
            return left.Y < right.Y ? -1 : 1;
        return left.Z.CompareTo(right.Z);
    }

    private static int Snap(int value, int step)
    {
        if (step == 0)
            return value;
        var snapped = Mathf.Snapped((double)value, step);
        return checked((int)snapped);
    }

    private static void ValidateConvertible(float component, string parameterName)
    {
        if (!Mathf.IsFinite(component) || (double)component < int.MinValue || (double)component > int.MaxValue)
            throw new ArgumentOutOfRangeException(parameterName, "Vector components must be finite 32-bit signed integer values.");
    }
}
