using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a four-component integer vector for masks and numeric tuples.</summary>
/// <remarks>
/// Addition, subtraction, multiplication, and negation use wrapping 32-bit arithmetic.
/// Division and remainder follow C# truncated-division rules. The zero-initialized value is <see cref="Zero"/>.
/// Squared norms widen to checked 64-bit integers; ordinary lengths and distances use widened floating-point terms.
/// Float-to-integer conversion rejects non-finite and out-of-range components before assignment.
/// Component extrema select the first maximum and last minimum axis on ties.
/// Numeric operations do not allocate managed memory; string formatting allocates a string.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector4i : IEquatable<Vector4i>
{
    private static readonly Vector4i MinValueValue = new(int.MinValue, int.MinValue, int.MinValue, int.MinValue);
    private static readonly Vector4i MaxValueValue = new(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
    private static readonly Vector4i ZeroValue = new(0, 0, 0, 0);
    private static readonly Vector4i OneValue = new(1, 1, 1, 1);

    /// <summary>Gets or sets the X component.</summary>
    public int X;

    /// <summary>Gets or sets the Y component.</summary>
    public int Y;

    /// <summary>Gets or sets the Z component.</summary>
    public int Z;

    /// <summary>Gets or sets the W component.</summary>
    public int W;

    /// <summary>Gets the vector containing the minimum 32-bit integer in every component.</summary>
    /// <value><c>(int.MinValue, int.MinValue, int.MinValue, int.MinValue)</c>.</value>
    public static Vector4i MinValue => MinValueValue;

    /// <summary>Gets the vector containing the maximum 32-bit integer in every component.</summary>
    /// <value><c>(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)</c>.</value>
    public static Vector4i MaxValue => MaxValueValue;

    /// <summary>Gets the zero vector.</summary>
    /// <value><c>(0, 0, 0, 0)</c>.</value>
    public static Vector4i Zero => ZeroValue;

    /// <summary>Gets the vector whose components are all one.</summary>
    /// <value><c>(1, 1, 1, 1)</c>.</value>
    public static Vector4i One => OneValue;

    /// <summary>Gets or sets a component by axis index.</summary>
    /// <param name="index">An index from zero through three for X, Y, Z, or W.</param>
    /// <value>The selected component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside zero through three.</exception>
    public int this[int index]
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

    /// <summary>Initializes an integer vector from four components.</summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    public Vector4i(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>Initializes an integer vector by truncating a finite floating-point vector toward zero.</summary>
    /// <param name="value">The floating-point vector to convert.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public Vector4i(Vector4 value)
    {
        ValidateConvertible(value.X, nameof(value));
        ValidateConvertible(value.Y, nameof(value));
        ValidateConvertible(value.Z, nameof(value));
        ValidateConvertible(value.W, nameof(value));
        X = (int)value.X;
        Y = (int)value.Y;
        Z = (int)value.Z;
        W = (int)value.W;
    }

    /// <summary>Deconstructs the vector into its four components.</summary>
    /// <param name="x">Receives <see cref="X"/>.</param>
    /// <param name="y">Receives <see cref="Y"/>.</param>
    /// <param name="z">Receives <see cref="Z"/>.</param>
    /// <param name="w">Receives <see cref="W"/>.</param>
    public readonly void Deconstruct(out int x, out int y, out int z, out int w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    /// <summary>Returns the componentwise absolute value.</summary>
    /// <returns>A vector with nonnegative components.</returns>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/>.</exception>
    public readonly Vector4i Abs() => new(Mathf.Abs(X), Mathf.Abs(Y), Mathf.Abs(Z), Mathf.Abs(W));

    /// <summary>Clamps each component between corresponding vector bounds.</summary>
    /// <param name="min">The componentwise lower bounds.</param>
    /// <param name="max">The componentwise upper bounds.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException">A lower bound is greater than its corresponding upper bound.</exception>
    public readonly Vector4i Clamp(Vector4i min, Vector4i max) => new(
        Mathf.Clamp(X, min.X, max.X),
        Mathf.Clamp(Y, min.Y, max.Y),
        Mathf.Clamp(Z, min.Z, max.Z),
        Mathf.Clamp(W, min.W, max.W));

    /// <summary>Clamps every component between scalar bounds.</summary>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public readonly Vector4i Clamp(int min, int max) => new(
        Mathf.Clamp(X, min, max), Mathf.Clamp(Y, min, max), Mathf.Clamp(Z, min, max), Mathf.Clamp(W, min, max));

    /// <summary>Returns the squared Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The exact squared distance when it fits in a signed 64-bit integer.</returns>
    /// <remarks>Coordinate differences are widened before subtraction. This operation does not use wrapping vector subtraction.</remarks>
    /// <exception cref="OverflowException">The squared distance exceeds <see cref="long.MaxValue"/>.</exception>
    public readonly long DistanceSquaredTo(Vector4i to)
    {
        var dx = (long)to.X - X;
        var dy = (long)to.Y - Y;
        var dz = (long)to.Z - Z;
        var dw = (long)to.W - W;
        return checked((dx * dx) + (dy * dy) + (dz * dz) + (dw * dw));
    }

    /// <summary>Returns the Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The nonnegative distance, rounded to single precision.</returns>
    /// <remarks>Coordinate differences and squared terms use widened arithmetic, so this remains finite even when <see cref="DistanceSquaredTo(Vector4i)"/> exceeds the signed 64-bit range.</remarks>
    public readonly float DistanceTo(Vector4i to)
    {
        var dx = (long)to.X - X;
        var dy = (long)to.Y - Y;
        var dz = (long)to.Z - Z;
        var dw = (long)to.W - W;
        return (float)Mathf.Sqrt(((double)dx * dx) + ((double)dy * dy) + ((double)dz * dz) + ((double)dw * dw));
    }

    /// <summary>Returns the Euclidean length.</summary>
    /// <returns>The nonnegative length, rounded to single precision.</returns>
    /// <remarks>Squared terms use widened arithmetic, so this remains finite even when <see cref="LengthSquared"/> exceeds the signed 64-bit range.</remarks>
    public readonly float Length() => (float)Mathf.Sqrt(((double)X * X) + ((double)Y * Y) + ((double)Z * Z) + ((double)W * W));

    /// <summary>Returns the squared Euclidean length.</summary>
    /// <returns>The exact squared length when it fits in a signed 64-bit integer.</returns>
    /// <exception cref="OverflowException">The squared length exceeds <see cref="long.MaxValue"/>.</exception>
    public readonly long LengthSquared() => checked(((long)X * X) + ((long)Y * Y) + ((long)Z * Z) + ((long)W * W));

    /// <summary>Returns the componentwise maximum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector4i Max(Vector4i with) => new(
        Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z), Mathf.Max(W, with.W));

    /// <summary>Returns the componentwise maximum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector4i Max(int with) => new(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with), Mathf.Max(W, with));

    /// <summary>Returns the axis containing the greatest component.</summary>
    /// <returns><see cref="Vector4Axis.X"/> when all components are equal; otherwise the first greatest axis.</returns>
    public readonly Vector4Axis MaxAxisIndex()
    {
        var index = Vector4Axis.X;
        var value = X;
        for (var current = 1; current < 4; current++)
        {
            if (this[current] > value)
            {
                index = (Vector4Axis)current;
                value = this[current];
            }
        }

        return index;
    }

    /// <summary>Returns the componentwise minimum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector4i Min(Vector4i with) => new(
        Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z), Mathf.Min(W, with.W));

    /// <summary>Returns the componentwise minimum with a scalar.</summary>
    /// <param name="with">The scalar compared with every component.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector4i Min(int with) => new(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with), Mathf.Min(W, with));

    /// <summary>Returns the axis containing the least component.</summary>
    /// <returns><see cref="Vector4Axis.W"/> when all components are equal; otherwise the last least axis.</returns>
    public readonly Vector4Axis MinAxisIndex()
    {
        var index = Vector4Axis.X;
        var value = X;
        for (var current = 1; current < 4; current++)
        {
            if (this[current] <= value)
            {
                index = (Vector4Axis)current;
                value = this[current];
            }
        }

        return index;
    }

    /// <summary>Returns the sign of every component.</summary>
    /// <returns>Components containing negative one, zero, or positive one.</returns>
    public readonly Vector4i Sign() => new(Mathf.Sign(X), Mathf.Sign(Y), Mathf.Sign(Z), Mathf.Sign(W));

    /// <summary>Snaps each component to the nearest multiple of the corresponding step.</summary>
    /// <param name="step">The componentwise step. A zero component leaves the corresponding value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    /// <exception cref="OverflowException">A snapped component is outside the 32-bit signed integer range.</exception>
    public readonly Vector4i Snapped(Vector4i step) => new(
        Snap(X, step.X), Snap(Y, step.Y), Snap(Z, step.Z), Snap(W, step.W));

    /// <summary>Snaps every component to the nearest multiple of a scalar step.</summary>
    /// <param name="step">The scalar step. Zero leaves every value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    /// <exception cref="OverflowException">A snapped component is outside the 32-bit signed integer range.</exception>
    public readonly Vector4i Snapped(int step) => new(Snap(X, step), Snap(Y, step), Snap(Z, step), Snap(W, step));

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The wrapping componentwise sum.</returns>
    public static Vector4i operator +(Vector4i left, Vector4i right) => new(
        unchecked(left.X + right.X), unchecked(left.Y + right.Y),
        unchecked(left.Z + right.Z), unchecked(left.W + right.W));

    /// <summary>Returns a vector unchanged.</summary>
    /// <param name="value">The vector.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static Vector4i operator +(Vector4i value) => value;

    /// <summary>Subtracts two vectors componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The wrapping componentwise difference.</returns>
    public static Vector4i operator -(Vector4i left, Vector4i right) => new(
        unchecked(left.X - right.X), unchecked(left.Y - right.Y),
        unchecked(left.Z - right.Z), unchecked(left.W - right.W));

    /// <summary>Negates every component.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>The wrapping componentwise negation.</returns>
    public static Vector4i operator -(Vector4i value) => new(
        unchecked(-value.X), unchecked(-value.Y), unchecked(-value.Z), unchecked(-value.W));

    /// <summary>Multiplies a vector by an integer scalar.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector4i operator *(Vector4i vector, int scalar) => new(
        unchecked(vector.X * scalar), unchecked(vector.Y * scalar),
        unchecked(vector.Z * scalar), unchecked(vector.W * scalar));

    /// <summary>Multiplies an integer scalar by a vector.</summary>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector4i operator *(int scalar, Vector4i vector) => vector * scalar;

    /// <summary>Multiplies an integer vector by a floating-point scalar.</summary>
    /// <param name="vector">The integer vector.</param>
    /// <param name="scalar">The floating-point multiplier.</param>
    /// <returns>A floating-point componentwise product.</returns>
    public static Vector4 operator *(Vector4i vector, float scalar) => new(
        vector.X * scalar, vector.Y * scalar, vector.Z * scalar, vector.W * scalar);

    /// <summary>Multiplies a floating-point scalar by an integer vector.</summary>
    /// <param name="scalar">The floating-point multiplier.</param>
    /// <param name="vector">The integer vector.</param>
    /// <returns>A floating-point componentwise product.</returns>
    public static Vector4 operator *(float scalar, Vector4i vector) => vector * scalar;

    /// <summary>Multiplies two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector4i operator *(Vector4i left, Vector4i right) => new(
        unchecked(left.X * right.X), unchecked(left.Y * right.Y),
        unchecked(left.Z * right.Z), unchecked(left.W * right.W));

    /// <summary>Divides every component by an integer scalar using truncated division.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static Vector4i operator /(Vector4i vector, int divisor) => new(
        vector.X / divisor, vector.Y / divisor, vector.Z / divisor, vector.W / divisor);

    /// <summary>Divides an integer vector by a floating-point scalar.</summary>
    /// <param name="vector">The integer dividend.</param>
    /// <param name="divisor">The floating-point divisor.</param>
    /// <returns>The IEEE 754 floating-point componentwise quotient.</returns>
    public static Vector4 operator /(Vector4i vector, float divisor) => new(
        vector.X / divisor, vector.Y / divisor, vector.Z / divisor, vector.W / divisor);

    /// <summary>Divides two vectors componentwise using truncated division.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise quotient.</returns>
    /// <exception cref="DivideByZeroException">A component of <paramref name="right"/> is zero.</exception>
    /// <exception cref="OverflowException">A minimum-valued dividend component has a divisor of negative one.</exception>
    public static Vector4i operator /(Vector4i left, Vector4i right) => new(
        left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);

    /// <summary>Returns the truncated remainder of every component by an integer scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise remainder.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static Vector4i operator %(Vector4i vector, int divisor) => new(
        vector.X % divisor, vector.Y % divisor, vector.Z % divisor, vector.W % divisor);

    /// <summary>Returns the truncated componentwise remainder of two vectors.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise remainder.</returns>
    /// <exception cref="DivideByZeroException">A component of <paramref name="right"/> is zero.</exception>
    /// <exception cref="OverflowException">A minimum-valued dividend component has a divisor of negative one.</exception>
    public static Vector4i operator %(Vector4i left, Vector4i right) => new(
        left.X % right.X, left.Y % right.Y, left.Z % right.Z, left.W % right.W);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public static bool operator ==(Vector4i left, Vector4i right) => left.Equals(right);

    /// <summary>Tests whether any component differs.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when a corresponding component differs.</returns>
    public static bool operator !=(Vector4i left, Vector4i right) => !left.Equals(right);

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Vector4i left, Vector4i right) => Compare(left, right) < 0;

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Vector4i left, Vector4i right) => Compare(left, right) > 0;

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Vector4i left, Vector4i right) => Compare(left, right) <= 0;

    /// <summary>Compares vectors lexicographically by X, Y, Z, then W.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Vector4i left, Vector4i right) => Compare(left, right) >= 0;

    /// <summary>Converts an integer vector to a floating-point vector.</summary>
    /// <param name="value">The integer vector to convert.</param>
    /// <returns>A floating-point vector with corresponding components.</returns>
    /// <remarks>Large integer components can lose low-order precision.</remarks>
    public static implicit operator Vector4(Vector4i value) => new(value);

    /// <summary>Converts a finite in-range floating-point vector by truncating every component toward zero.</summary>
    /// <param name="value">The floating-point vector to convert.</param>
    /// <returns>The truncated integer vector.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public static explicit operator Vector4i(Vector4 value) => new(value);

    /// <summary>Tests whether another object is an equal integer vector.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an integer vector with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector4i other && Equals(other);

    /// <summary>Tests every component for exact equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when all corresponding components are equal.</returns>
    public readonly bool Equals(Vector4i other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

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

    private static int Compare(Vector4i left, Vector4i right)
    {
        if (left.X != right.X)
            return left.X < right.X ? -1 : 1;
        if (left.Y != right.Y)
            return left.Y < right.Y ? -1 : 1;
        if (left.Z != right.Z)
            return left.Z < right.Z ? -1 : 1;
        return left.W.CompareTo(right.W);
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
