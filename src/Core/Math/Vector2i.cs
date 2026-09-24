using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a two-component integer vector for pixels, grids, tile coordinates, and integer pairs.</summary>
/// <remarks>
/// Arithmetic uses 32-bit signed integers. Addition, subtraction, multiplication, and negation wrap on overflow.
/// Division and remainder follow C# truncated-division rules. The zero-initialized value is <see cref="Zero"/>.
/// Numeric operations do not allocate managed memory; string formatting allocates a string.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector2i : IEquatable<Vector2i>
{
    private static readonly Vector2i MinValueValue = new(int.MinValue, int.MinValue);
    private static readonly Vector2i MaxValueValue = new(int.MaxValue, int.MaxValue);
    private static readonly Vector2i ZeroValue = new(0, 0);
    private static readonly Vector2i OneValue = new(1, 1);
    private static readonly Vector2i UpValue = new(0, -1);
    private static readonly Vector2i DownValue = new(0, 1);
    private static readonly Vector2i RightValue = new(1, 0);
    private static readonly Vector2i LeftValue = new(-1, 0);

    /// <summary>Identifies one vector component.</summary>
    public enum Axis
    {
        /// <summary>Identifies the horizontal X component.</summary>
        X = 0,

        /// <summary>Identifies the vertical Y component.</summary>
        Y = 1,
    }

    /// <summary>Gets or sets the horizontal component.</summary>
    public int X;

    /// <summary>Gets or sets the vertical component.</summary>
    public int Y;

    /// <summary>Gets the vector containing the minimum 32-bit integer in both components.</summary>
    /// <value><c>(int.MinValue, int.MinValue)</c>.</value>
    public static Vector2i MinValue => MinValueValue;

    /// <summary>Gets the vector containing the maximum 32-bit integer in both components.</summary>
    /// <value><c>(int.MaxValue, int.MaxValue)</c>.</value>
    public static Vector2i MaxValue => MaxValueValue;

    /// <summary>Gets the zero vector.</summary>
    /// <value><c>(0, 0)</c>.</value>
    public static Vector2i Zero => ZeroValue;

    /// <summary>Gets the vector whose components are both one.</summary>
    /// <value><c>(1, 1)</c>.</value>
    public static Vector2i One => OneValue;

    /// <summary>Gets the upward screen-space unit vector.</summary>
    /// <value><c>(0, -1)</c>.</value>
    public static Vector2i Up => UpValue;

    /// <summary>Gets the downward screen-space unit vector.</summary>
    /// <value><c>(0, 1)</c>.</value>
    public static Vector2i Down => DownValue;

    /// <summary>Gets the rightward unit vector.</summary>
    /// <value><c>(1, 0)</c>.</value>
    public static Vector2i Right => RightValue;

    /// <summary>Gets the leftward unit vector.</summary>
    /// <value><c>(-1, 0)</c>.</value>
    public static Vector2i Left => LeftValue;

    /// <summary>Gets or sets a component by axis index.</summary>
    /// <param name="index">Zero for <see cref="X"/> or one for <see cref="Y"/>.</param>
    /// <value>The selected component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not zero or one.</exception>
    public int this[int index]
    {
        readonly get => index switch
        {
            0 => X,
            1 => Y,
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Initializes an integer vector from horizontal and vertical components.</summary>
    /// <param name="x">The horizontal component.</param>
    /// <param name="y">The vertical component.</param>
    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Initializes an integer vector by truncating a finite floating-point vector toward zero.</summary>
    /// <param name="value">The floating-point vector to convert.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public Vector2i(Vector2 value)
    {
        ValidateConvertible(value.X, nameof(value));
        ValidateConvertible(value.Y, nameof(value));
        X = (int)value.X;
        Y = (int)value.Y;
    }

    /// <summary>Deconstructs the vector into its two components.</summary>
    /// <param name="x">Receives <see cref="X"/>.</param>
    /// <param name="y">Receives <see cref="Y"/>.</param>
    public readonly void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    /// <summary>Returns a vector containing the absolute value of each component.</summary>
    /// <returns>The componentwise absolute value.</returns>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/>.</exception>
    public readonly Vector2i Abs() => new(Mathf.Abs(X), Mathf.Abs(Y));

    /// <summary>Returns the ratio of the horizontal component to the vertical component.</summary>
    /// <returns><c>X / Y</c> as a floating-point value, including IEEE 754 zero-division behavior.</returns>
    public readonly float Aspect() => X / (float)Y;

    /// <summary>Clamps each component between corresponding vector bounds.</summary>
    /// <param name="min">The componentwise lower bounds.</param>
    /// <param name="max">The componentwise upper bounds.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException">A lower bound is greater than its corresponding upper bound.</exception>
    public readonly Vector2i Clamp(Vector2i min, Vector2i max) => new(
        Mathf.Clamp(X, min.X, max.X),
        Mathf.Clamp(Y, min.Y, max.Y));

    /// <summary>Clamps both components between scalar bounds.</summary>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public readonly Vector2i Clamp(int min, int max) => new(Mathf.Clamp(X, min, max), Mathf.Clamp(Y, min, max));

    /// <summary>Returns the squared Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The exact squared distance when it fits in a signed 64-bit integer.</returns>
    /// <remarks>Coordinate differences are widened before subtraction. This operation does not use wrapping vector subtraction.</remarks>
    /// <exception cref="OverflowException">The squared distance exceeds <see cref="long.MaxValue"/>.</exception>
    public readonly long DistanceSquaredTo(Vector2i to)
    {
        var dx = (long)to.X - X;
        var dy = (long)to.Y - Y;
        return checked((dx * dx) + (dy * dy));
    }

    /// <summary>Returns the Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The nonnegative distance, rounded to single precision.</returns>
    /// <remarks>Coordinate differences and squared terms use widened arithmetic, so this remains finite even when <see cref="DistanceSquaredTo(Vector2i)"/> exceeds the signed 64-bit range.</remarks>
    public readonly float DistanceTo(Vector2i to)
    {
        var dx = (long)to.X - X;
        var dy = (long)to.Y - Y;
        return (float)Mathf.Sqrt(((double)dx * dx) + ((double)dy * dy));
    }

    /// <summary>Returns the Euclidean length.</summary>
    /// <returns>The nonnegative length, rounded to single precision.</returns>
    /// <remarks>Squared terms use widened arithmetic, so this remains finite even when <see cref="LengthSquared"/> exceeds the signed 64-bit range.</remarks>
    public readonly float Length() => (float)Mathf.Sqrt(((double)X * X) + ((double)Y * Y));

    /// <summary>Returns the squared Euclidean length.</summary>
    /// <returns>The exact squared length when it fits in a signed 64-bit integer.</returns>
    /// <exception cref="OverflowException">The squared length exceeds <see cref="long.MaxValue"/>.</exception>
    public readonly long LengthSquared() => checked(((long)X * X) + ((long)Y * Y));

    /// <summary>Returns the componentwise maximum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector2i Max(Vector2i with) => new(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y));

    /// <summary>Returns the componentwise maximum with a scalar.</summary>
    /// <param name="with">The scalar compared with both components.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector2i Max(int with) => new(Mathf.Max(X, with), Mathf.Max(Y, with));

    /// <summary>Returns the axis containing the greatest component.</summary>
    /// <returns><see cref="Axis.X"/> when components are equal; otherwise the greatest component's axis.</returns>
    public readonly Axis MaxAxisIndex() => X < Y ? Axis.Y : Axis.X;

    /// <summary>Returns the componentwise minimum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector2i Min(Vector2i with) => new(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y));

    /// <summary>Returns the componentwise minimum with a scalar.</summary>
    /// <param name="with">The scalar compared with both components.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector2i Min(int with) => new(Mathf.Min(X, with), Mathf.Min(Y, with));

    /// <summary>Returns the axis containing the least component.</summary>
    /// <returns><see cref="Axis.Y"/> when components are equal; otherwise the least component's axis.</returns>
    public readonly Axis MinAxisIndex() => X < Y ? Axis.X : Axis.Y;

    /// <summary>Returns the sign of each component.</summary>
    /// <returns>Components containing negative one, zero, or positive one.</returns>
    public readonly Vector2i Sign() => new(Mathf.Sign(X), Mathf.Sign(Y));

    /// <summary>Snaps each component to the nearest multiple of the corresponding step.</summary>
    /// <param name="step">The componentwise step. A zero component leaves the corresponding value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    /// <remarks>Midpoint ties go toward larger values for a positive step and smaller values for a negative step.</remarks>
    /// <exception cref="OverflowException">A snapped component is outside the 32-bit signed integer range.</exception>
    public readonly Vector2i Snapped(Vector2i step) => new(Snap(X, step.X), Snap(Y, step.Y));

    /// <summary>Snaps both components to the nearest multiple of a scalar step.</summary>
    /// <param name="step">The scalar step. Zero leaves both values unchanged.</param>
    /// <returns>The snapped vector.</returns>
    /// <remarks>Midpoint ties go toward larger values for a positive step and smaller values for a negative step.</remarks>
    /// <exception cref="OverflowException">A snapped component is outside the 32-bit signed integer range.</exception>
    public readonly Vector2i Snapped(int step) => new(Snap(X, step), Snap(Y, step));

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The wrapping componentwise sum.</returns>
    public static Vector2i operator +(Vector2i left, Vector2i right) =>
        new(unchecked(left.X + right.X), unchecked(left.Y + right.Y));

    /// <summary>Returns a vector unchanged.</summary>
    /// <param name="value">The vector.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static Vector2i operator +(Vector2i value) => value;

    /// <summary>Subtracts two vectors componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The wrapping componentwise difference.</returns>
    public static Vector2i operator -(Vector2i left, Vector2i right) =>
        new(unchecked(left.X - right.X), unchecked(left.Y - right.Y));

    /// <summary>Negates both components.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>The wrapping componentwise negation.</returns>
    public static Vector2i operator -(Vector2i value) => new(unchecked(-value.X), unchecked(-value.Y));

    /// <summary>Multiplies a vector by an integer scalar.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector2i operator *(Vector2i vector, int scalar) =>
        new(unchecked(vector.X * scalar), unchecked(vector.Y * scalar));

    /// <summary>Multiplies an integer scalar by a vector.</summary>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector2i operator *(int scalar, Vector2i vector) => vector * scalar;

    /// <summary>Multiplies an integer vector by a floating-point scalar.</summary>
    /// <param name="vector">The integer vector.</param>
    /// <param name="scalar">The floating-point multiplier.</param>
    /// <returns>A floating-point componentwise product.</returns>
    public static Vector2 operator *(Vector2i vector, float scalar) => new(vector.X * scalar, vector.Y * scalar);

    /// <summary>Multiplies a floating-point scalar by an integer vector.</summary>
    /// <param name="scalar">The floating-point multiplier.</param>
    /// <param name="vector">The integer vector.</param>
    /// <returns>A floating-point componentwise product.</returns>
    public static Vector2 operator *(float scalar, Vector2i vector) => vector * scalar;

    /// <summary>Multiplies two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The wrapping componentwise product.</returns>
    public static Vector2i operator *(Vector2i left, Vector2i right) =>
        new(unchecked(left.X * right.X), unchecked(left.Y * right.Y));

    /// <summary>Divides both components by an integer scalar using truncated division.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static Vector2i operator /(Vector2i vector, int divisor) => new(vector.X / divisor, vector.Y / divisor);

    /// <summary>Divides an integer vector by a floating-point scalar.</summary>
    /// <param name="vector">The integer dividend.</param>
    /// <param name="divisor">The floating-point divisor.</param>
    /// <returns>The IEEE 754 floating-point componentwise quotient.</returns>
    public static Vector2 operator /(Vector2i vector, float divisor) => new(vector.X / divisor, vector.Y / divisor);

    /// <summary>Divides two vectors componentwise using truncated division.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise quotient.</returns>
    /// <exception cref="DivideByZeroException">A component of <paramref name="right"/> is zero.</exception>
    /// <exception cref="OverflowException">A dividend component is <see cref="int.MinValue"/> and its divisor is negative one.</exception>
    public static Vector2i operator /(Vector2i left, Vector2i right) => new(left.X / right.X, left.Y / right.Y);

    /// <summary>Returns the truncated remainder of both components by an integer scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise remainder.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException">A component is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static Vector2i operator %(Vector2i vector, int divisor) => new(vector.X % divisor, vector.Y % divisor);

    /// <summary>Returns the truncated componentwise remainder of two vectors.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise remainder.</returns>
    /// <exception cref="DivideByZeroException">A component of <paramref name="right"/> is zero.</exception>
    /// <exception cref="OverflowException">A minimum-valued dividend component has a divisor of negative one.</exception>
    public static Vector2i operator %(Vector2i left, Vector2i right) => new(left.X % right.X, left.Y % right.Y);

    /// <summary>Tests both components for exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when both corresponding components are equal.</returns>
    public static bool operator ==(Vector2i left, Vector2i right) => left.Equals(right);

    /// <summary>Tests whether either component differs.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when at least one corresponding component differs.</returns>
    public static bool operator !=(Vector2i left, Vector2i right) => !left.Equals(right);

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Vector2i left, Vector2i right) => left.X == right.X ? left.Y < right.Y : left.X < right.X;

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Vector2i left, Vector2i right) => left.X == right.X ? left.Y > right.Y : left.X > right.X;

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Vector2i left, Vector2i right) => left.X == right.X ? left.Y <= right.Y : left.X < right.X;

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Vector2i left, Vector2i right) => left.X == right.X ? left.Y >= right.Y : left.X > right.X;

    /// <summary>Converts an integer vector to a floating-point vector.</summary>
    /// <param name="value">The integer vector to convert.</param>
    /// <returns>A floating-point vector with corresponding components.</returns>
    /// <remarks>Large integer components can lose low-order precision.</remarks>
    public static implicit operator Vector2(Vector2i value) => new(value);

    /// <summary>Converts a finite in-range floating-point vector by truncating each component toward zero.</summary>
    /// <param name="value">The floating-point vector to convert.</param>
    /// <returns>The truncated integer vector.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public static explicit operator Vector2i(Vector2 value) => new(value);

    /// <summary>Tests whether another object is an equal integer vector.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an integer vector with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector2i other && Equals(other);

    /// <summary>Tests both components for exact equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when both corresponding components are equal.</returns>
    public readonly bool Equals(Vector2i other) => X == other.X && Y == other.Y;

    /// <summary>Returns a hash code based on both components.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Formats both components using invariant culture.</summary>
    /// <returns>A parenthesized component pair.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats both components with a numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format, or <see langword="null"/> for the default format.</param>
    /// <returns>A parenthesized component pair.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) =>
        $"({X.ToString(format, CultureInfo.InvariantCulture)}, {Y.ToString(format, CultureInfo.InvariantCulture)})";

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
            throw new ArgumentOutOfRangeException(parameterName, "Vector2 components must be finite 32-bit signed integer values.");
    }
}
