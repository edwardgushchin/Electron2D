using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a two-component floating-point vector for coordinates, sizes, directions, and numeric pairs.</summary>
/// <remarks>
/// The value uses single-precision components and screen-space direction constants where positive Y points down.
/// Ordinary arithmetic preserves IEEE 754 NaN and infinity values. The zero-initialized value is <see cref="Zero"/>.
/// Numeric operations do not allocate managed memory; string formatting allocates a string.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector2 : IEquatable<Vector2>
{
    private const float NormalizedEpsilon = 0.001f;

    private static readonly Vector2 ZeroValue = new(0f, 0f);
    private static readonly Vector2 OneValue = new(1f, 1f);
    private static readonly Vector2 InfValue = new(float.PositiveInfinity, float.PositiveInfinity);
    private static readonly Vector2 UpValue = new(0f, -1f);
    private static readonly Vector2 DownValue = new(0f, 1f);
    private static readonly Vector2 RightValue = new(1f, 0f);
    private static readonly Vector2 LeftValue = new(-1f, 0f);

    /// <summary>Identifies one vector component.</summary>
    public enum Axis
    {
        /// <summary>Identifies the horizontal X component.</summary>
        X = 0,

        /// <summary>Identifies the vertical Y component.</summary>
        Y = 1,
    }

    /// <summary>Gets or sets the horizontal component.</summary>
    public float X;

    /// <summary>Gets or sets the vertical component.</summary>
    public float Y;

    /// <summary>Gets the zero vector.</summary>
    /// <value><c>(0, 0)</c>.</value>
    public static Vector2 Zero => ZeroValue;

    /// <summary>Gets the vector whose components are both one.</summary>
    /// <value><c>(1, 1)</c>.</value>
    public static Vector2 One => OneValue;

    /// <summary>Gets the vector whose components are both positive infinity.</summary>
    /// <value><c>(+Infinity, +Infinity)</c>.</value>
    public static Vector2 Inf => InfValue;

    /// <summary>Gets the upward screen-space unit vector.</summary>
    /// <value><c>(0, -1)</c>.</value>
    public static Vector2 Up => UpValue;

    /// <summary>Gets the downward screen-space unit vector.</summary>
    /// <value><c>(0, 1)</c>.</value>
    public static Vector2 Down => DownValue;

    /// <summary>Gets the rightward unit vector.</summary>
    /// <value><c>(1, 0)</c>.</value>
    public static Vector2 Right => RightValue;

    /// <summary>Gets the leftward unit vector.</summary>
    /// <value><c>(-1, 0)</c>.</value>
    public static Vector2 Left => LeftValue;

    /// <summary>Gets or sets a component by axis index.</summary>
    /// <param name="index">Zero for <see cref="X"/> or one for <see cref="Y"/>.</param>
    /// <value>The selected component.</value>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not zero or one.</exception>
    public float this[int index]
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

    /// <summary>Initializes a vector from horizontal and vertical components.</summary>
    /// <param name="x">The horizontal component.</param>
    /// <param name="y">The vertical component.</param>
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Initializes a floating-point vector from an integer vector.</summary>
    /// <param name="value">The integer vector whose components are converted exactly to single precision when representable.</param>
    /// <remarks>Large integer components can lose low-order precision during conversion.</remarks>
    public Vector2(Vector2i value)
    {
        X = value.X;
        Y = value.Y;
    }

    /// <summary>Deconstructs the vector into its two components.</summary>
    /// <param name="x">Receives <see cref="X"/>.</param>
    /// <param name="y">Receives <see cref="Y"/>.</param>
    public readonly void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }

    /// <summary>Returns a vector containing the absolute value of each component.</summary>
    /// <returns>The componentwise absolute value.</returns>
    public readonly Vector2 Abs() => new(Mathf.Abs(X), Mathf.Abs(Y));

    /// <summary>Returns the angle from the positive X axis to this vector.</summary>
    /// <returns>The clockwise screen-space angle in radians.</returns>
    public readonly float Angle() => Mathf.Atan2(Y, X);

    /// <summary>Returns the signed angle from this vector to another vector.</summary>
    /// <param name="to">The destination vector.</param>
    /// <returns>The signed angle in radians in the range from negative pi through positive pi.</returns>
    public readonly float AngleTo(Vector2 to) => Mathf.Atan2(Cross(to), Dot(to));

    /// <summary>Returns the angle of the line from this point to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The clockwise screen-space angle from the positive X axis, in radians.</returns>
    public readonly float AngleToPoint(Vector2 to) => Mathf.Atan2(to.Y - Y, to.X - X);

    /// <summary>Returns the ratio of the horizontal component to the vertical component.</summary>
    /// <returns><c>X / Y</c>, including normal IEEE 754 division behavior.</returns>
    public readonly float Aspect() => X / Y;

    /// <summary>Returns the derivative at a parameter on a cubic Bézier curve.</summary>
    /// <param name="control1">The first control point.</param>
    /// <param name="control2">The second control point.</param>
    /// <param name="end">The curve end point.</param>
    /// <param name="t">The curve parameter; values outside zero through one extrapolate.</param>
    /// <returns>The curve derivative at <paramref name="t"/>.</returns>
    public readonly Vector2 BezierDerivative(Vector2 control1, Vector2 control2, Vector2 end, float t) => new(
        Mathf.BezierDerivative(X, control1.X, control2.X, end.X, t),
        Mathf.BezierDerivative(Y, control1.Y, control2.Y, end.Y, t));

    /// <summary>Returns a point on a cubic Bézier curve.</summary>
    /// <param name="control1">The first control point.</param>
    /// <param name="control2">The second control point.</param>
    /// <param name="end">The curve end point.</param>
    /// <param name="t">The curve parameter; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated curve point.</returns>
    public readonly Vector2 BezierInterpolate(Vector2 control1, Vector2 control2, Vector2 end, float t) => new(
        Mathf.BezierInterpolate(X, control1.X, control2.X, end.X, t),
        Mathf.BezierInterpolate(Y, control1.Y, control2.Y, end.Y, t));

    /// <summary>Returns this vector bounced from a line with a given unit normal.</summary>
    /// <param name="normal">The normalized line normal.</param>
    /// <returns>The vector reflected across the line's normal plane.</returns>
    /// <remarks>The method assumes that <paramref name="normal"/> is normalized and does not validate it.</remarks>
    public readonly Vector2 Bounce(Vector2 normal) => -Reflect(normal);

    /// <summary>Rounds both components upward toward positive infinity.</summary>
    /// <returns>The componentwise ceiling.</returns>
    public readonly Vector2 Ceil() => new(Mathf.Ceil(X), Mathf.Ceil(Y));

    /// <summary>Clamps each component between corresponding vector bounds.</summary>
    /// <param name="min">The componentwise lower bounds.</param>
    /// <param name="max">The componentwise upper bounds.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException">A lower bound is greater than its corresponding upper bound.</exception>
    public readonly Vector2 Clamp(Vector2 min, Vector2 max) => new(
        Mathf.Clamp(X, min.X, max.X),
        Mathf.Clamp(Y, min.Y, max.Y));

    /// <summary>Clamps both components between scalar bounds.</summary>
    /// <param name="min">The lower bound.</param>
    /// <param name="max">The upper bound.</param>
    /// <returns>The clamped vector.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public readonly Vector2 Clamp(float min, float max) => new(Mathf.Clamp(X, min, max), Mathf.Clamp(Y, min, max));

    /// <summary>Returns the scalar two-dimensional cross product.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The signed parallelogram area; positive means <paramref name="with"/> is clockwise in screen space.</returns>
    public readonly float Cross(Vector2 with) => (X * with.Y) - (Y * with.X);

    /// <summary>Performs Catmull-Rom cubic interpolation between this vector and another.</summary>
    /// <param name="b">The destination vector.</param>
    /// <param name="preA">The control vector before this vector.</param>
    /// <param name="postB">The control vector after <paramref name="b"/>.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated vector.</returns>
    public readonly Vector2 CubicInterpolate(Vector2 b, Vector2 preA, Vector2 postB, float weight) => new(
        Mathf.CubicInterpolate(X, b.X, preA.X, postB.X, weight),
        Mathf.CubicInterpolate(Y, b.Y, preA.Y, postB.Y, weight));

    /// <summary>Performs time-aware Barry-Goldman cubic interpolation.</summary>
    /// <param name="b">The destination vector.</param>
    /// <param name="preA">The control vector before this vector.</param>
    /// <param name="postB">The control vector after <paramref name="b"/>.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <param name="bTime">The time assigned to <paramref name="b"/> relative to this vector at time zero.</param>
    /// <param name="preATime">The time assigned to <paramref name="preA"/>.</param>
    /// <param name="postBTime">The time assigned to <paramref name="postB"/>.</param>
    /// <returns>The time-aware interpolated vector.</returns>
    public readonly Vector2 CubicInterpolateInTime(
        Vector2 b,
        Vector2 preA,
        Vector2 postB,
        float weight,
        float bTime,
        float preATime,
        float postBTime) => new(
            Mathf.CubicInterpolateInTime(X, b.X, preA.X, postB.X, weight, bTime, preATime, postBTime),
            Mathf.CubicInterpolateInTime(Y, b.Y, preA.Y, postB.Y, weight, bTime, preATime, postBTime));

    /// <summary>Returns the normalized direction from this point to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The normalized difference, or <see cref="Zero"/> when the difference is zero or non-finite.</returns>
    public readonly Vector2 DirectionTo(Vector2 to) => (to - this).Normalized();

    /// <summary>Returns the squared Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The squared distance.</returns>
    public readonly float DistanceSquaredTo(Vector2 to)
    {
        var difference = to - this;
        return difference.LengthSquared();
    }

    /// <summary>Returns the Euclidean distance to another point.</summary>
    /// <param name="to">The destination point.</param>
    /// <returns>The distance.</returns>
    public readonly float DistanceTo(Vector2 to) => Mathf.Sqrt(DistanceSquaredTo(to));

    /// <summary>Returns the dot product with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns><c>X * with.X + Y * with.Y</c>.</returns>
    public readonly float Dot(Vector2 with) => (X * with.X) + (Y * with.Y);

    /// <summary>Rounds both components downward toward negative infinity.</summary>
    /// <returns>The componentwise floor.</returns>
    public readonly Vector2 Floor() => new(Mathf.Floor(X), Mathf.Floor(Y));

    /// <summary>Creates an approximately unit vector from an angle.</summary>
    /// <param name="angle">The clockwise screen-space angle in radians.</param>
    /// <returns><c>(cos(angle), sin(angle))</c>.</returns>
    public static Vector2 FromAngle(float angle)
    {
        var (sine, cosine) = Mathf.SinCos(angle);
        return new Vector2(cosine, sine);
    }

    /// <summary>Returns the componentwise reciprocal.</summary>
    /// <returns><c>(1 / X, 1 / Y)</c>, including normal IEEE 754 zero-division behavior.</returns>
    public readonly Vector2 Inverse() => new(1f / X, 1f / Y);

    /// <summary>Tests both components for scale-aware approximate equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when both components are approximately equal.</returns>
    public readonly bool IsEqualApprox(Vector2 other) =>
        Mathf.IsEqualApprox(X, other.X) && Mathf.IsEqualApprox(Y, other.Y);

    /// <summary>Tests whether both components are finite.</summary>
    /// <returns><see langword="true"/> when neither component is NaN or infinity.</returns>
    public readonly bool IsFinite() => Mathf.IsFinite(X) && Mathf.IsFinite(Y);

    /// <summary>Tests whether the squared length is approximately one.</summary>
    /// <returns><see langword="true"/> when the vector is approximately unit length.</returns>
    public readonly bool IsNormalized() => Mathf.Abs(LengthSquared() - 1f) < NormalizedEpsilon;

    /// <summary>Tests whether both components are approximately zero.</summary>
    /// <returns><see langword="true"/> when each component's absolute value is below the comparison tolerance.</returns>
    public readonly bool IsZeroApprox() => Mathf.IsZeroApprox(X) && Mathf.IsZeroApprox(Y);

    /// <summary>Returns the Euclidean length.</summary>
    /// <returns>The square root of <see cref="LengthSquared"/>.</returns>
    public readonly float Length() => Mathf.Sqrt(LengthSquared());

    /// <summary>Returns the squared Euclidean length.</summary>
    /// <returns><c>X * X + Y * Y</c>.</returns>
    public readonly float LengthSquared() => (X * X) + (Y * Y);

    /// <summary>Linearly interpolates or extrapolates toward another vector.</summary>
    /// <param name="to">The destination vector.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The componentwise linear interpolation.</returns>
    public readonly Vector2 Lerp(Vector2 to, float weight) => this + ((to - this) * weight);

    /// <summary>Limits the vector to a maximum length.</summary>
    /// <param name="length">The maximum length.</param>
    /// <returns>This vector unchanged when already shorter, otherwise a vector with the requested length.</returns>
    /// <remarks>Non-finite inputs have undefined numeric results. A negative limit reverses a nonzero vector.</remarks>
    public readonly Vector2 LimitLength(float length = 1f)
    {
        var currentLength = Length();
        return currentLength > 0f && length < currentLength ? this / currentLength * length : this;
    }

    /// <summary>Returns the componentwise maximum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector2 Max(Vector2 with) => new(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y));

    /// <summary>Returns the componentwise maximum with a scalar.</summary>
    /// <param name="with">The scalar compared with both components.</param>
    /// <returns>The componentwise maximum.</returns>
    public readonly Vector2 Max(float with) => new(Mathf.Max(X, with), Mathf.Max(Y, with));

    /// <summary>Returns the axis containing the greatest component.</summary>
    /// <returns><see cref="Axis.X"/> when components are equal; otherwise the greatest component's axis.</returns>
    public readonly Axis MaxAxisIndex() => X < Y ? Axis.Y : Axis.X;

    /// <summary>Returns the componentwise minimum with another vector.</summary>
    /// <param name="with">The other vector.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector2 Min(Vector2 with) => new(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y));

    /// <summary>Returns the componentwise minimum with a scalar.</summary>
    /// <param name="with">The scalar compared with both components.</param>
    /// <returns>The componentwise minimum.</returns>
    public readonly Vector2 Min(float with) => new(Mathf.Min(X, with), Mathf.Min(Y, with));

    /// <summary>Returns the axis containing the least component.</summary>
    /// <returns><see cref="Axis.Y"/> when components are equal; otherwise the least component's axis.</returns>
    public readonly Axis MinAxisIndex() => X < Y ? Axis.X : Axis.Y;

    /// <summary>Moves toward another vector by a fixed distance without passing it.</summary>
    /// <param name="to">The destination vector.</param>
    /// <param name="delta">The signed distance to move. Negative values move away.</param>
    /// <returns>The moved vector, or <paramref name="to"/> when within the step or source proximity threshold.</returns>
    /// <remarks>A separation below <c>0.00001</c> returns <paramref name="to"/> even for a negative step.</remarks>
    public readonly Vector2 MoveToward(Vector2 to, float delta)
    {
        var difference = to - this;
        var distance = difference.Length();
        return distance <= delta || distance < 0.00001f ? to : this + (difference / distance * delta);
    }

    /// <summary>Returns this vector scaled to unit length.</summary>
    /// <returns>A normalized vector, or <see cref="Zero"/> when the input is zero or non-finite.</returns>
    /// <remarks>Near-zero finite inputs can lose precision.</remarks>
    public readonly Vector2 Normalized()
    {
        if (!IsFinite()) return Zero;
        var lengthSquared = LengthSquared();
        return lengthSquared == 0f ? Zero : this / Mathf.Sqrt(lengthSquared);
    }

    /// <summary>Returns a perpendicular vector rotated 90 degrees counter-clockwise in screen space.</summary>
    /// <returns><c>(Y, -X)</c> with the same length as this vector.</returns>
    public readonly Vector2 Orthogonal() => new(Y, -X);

    /// <summary>Applies positive modulus to both components.</summary>
    /// <param name="mod">The scalar divisor.</param>
    /// <returns>The componentwise canonical remainder using the divisor's sign.</returns>
    public readonly Vector2 PosMod(float mod) => new(Mathf.PosMod(X, mod), Mathf.PosMod(Y, mod));

    /// <summary>Applies componentwise positive modulus.</summary>
    /// <param name="mod">The component divisors.</param>
    /// <returns>The componentwise canonical remainders using each divisor's sign.</returns>
    public readonly Vector2 PosMod(Vector2 mod) => new(Mathf.PosMod(X, mod.X), Mathf.PosMod(Y, mod.Y));

    /// <summary>Projects this vector onto another vector.</summary>
    /// <param name="onNormal">The projection direction; it does not need to be normalized.</param>
    /// <returns>A vector parallel to <paramref name="onNormal"/>.</returns>
    /// <remarks>A zero projection direction produces NaN components through IEEE 754 division.</remarks>
    public readonly Vector2 Project(Vector2 onNormal) => onNormal * (Dot(onNormal) / onNormal.LengthSquared());

    /// <summary>Reflects this vector across a line with a given unit normal.</summary>
    /// <param name="normal">The normalized line normal.</param>
    /// <returns><c>2 * Dot(normal) * normal - this</c>.</returns>
    /// <remarks>
    /// This line-reflection convention is the inverse sign of the plane-normal reflection commonly named
    /// <c>reflect</c> by other math libraries. The method assumes a normalized normal and does not validate it.
    /// The normal is multiplied by two before applying the dot scalar, retaining finite components near float limits.
    /// </remarks>
    public readonly Vector2 Reflect(Vector2 normal) => (2f * normal * Dot(normal)) - this;

    /// <summary>Rotates this vector by an angle.</summary>
    /// <param name="angle">The clockwise screen-space angle in radians.</param>
    /// <returns>The rotated vector.</returns>
    public readonly Vector2 Rotated(float angle)
    {
        var (sine, cosine) = Mathf.SinCos(angle);
        return new Vector2((X * cosine) - (Y * sine), (X * sine) + (Y * cosine));
    }

    /// <summary>Rounds both components to the nearest integer with midpoint-to-even behavior.</summary>
    /// <returns>The componentwise rounded vector.</returns>
    public readonly Vector2 Round() => new(Mathf.Round(X), Mathf.Round(Y));

    /// <summary>Returns the sign of each component.</summary>
    /// <returns>Components containing negative one, zero, or positive one.</returns>
    /// <exception cref="ArithmeticException">A component is NaN.</exception>
    public readonly Vector2 Sign() => new(Mathf.Sign(X), Mathf.Sign(Y));

    /// <summary>Spherically interpolates direction while linearly interpolating length.</summary>
    /// <param name="to">The destination vector.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The spherical interpolation, or linear interpolation when either vector has zero length.</returns>
    public readonly Vector2 Slerp(Vector2 to, float weight)
    {
        var startLengthSquared = LengthSquared();
        var endLengthSquared = to.LengthSquared();
        if (startLengthSquared == 0f || endLengthSquared == 0f)
            return Lerp(to, weight);

        var startLength = Mathf.Sqrt(startLengthSquared);
        var resultLength = Mathf.Lerp(startLength, Mathf.Sqrt(endLengthSquared), weight);
        return Rotated(AngleTo(to) * weight) * (resultLength / startLength);
    }

    /// <summary>Removes the component along a line normal.</summary>
    /// <param name="normal">The normalized line normal.</param>
    /// <returns>The component perpendicular to <paramref name="normal"/>.</returns>
    /// <remarks>The method assumes that <paramref name="normal"/> is normalized and does not validate it.</remarks>
    public readonly Vector2 Slide(Vector2 normal) => this - (normal * Dot(normal));

    /// <summary>Snaps each component to the nearest multiple of the corresponding step.</summary>
    /// <param name="step">The componentwise step. A zero component leaves the corresponding value unchanged.</param>
    /// <returns>The snapped vector.</returns>
    public readonly Vector2 Snapped(Vector2 step) => new(Mathf.Snapped(X, step.X), Mathf.Snapped(Y, step.Y));

    /// <summary>Snaps both components to the nearest multiple of a scalar step.</summary>
    /// <param name="step">The scalar step. Zero leaves both values unchanged.</param>
    /// <returns>The snapped vector.</returns>
    public readonly Vector2 Snapped(float step) => new(Mathf.Snapped(X, step), Mathf.Snapped(Y, step));

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise sum.</returns>
    public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);

    /// <summary>Returns a vector unchanged.</summary>
    /// <param name="value">The vector.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static Vector2 operator +(Vector2 value) => value;

    /// <summary>Subtracts two vectors componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The componentwise difference.</returns>
    public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);

    /// <summary>Negates both components.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>The componentwise negation.</returns>
    public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);

    /// <summary>Multiplies a vector by a scalar.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector2 operator *(Vector2 vector, float scalar) => new(vector.X * scalar, vector.Y * scalar);

    /// <summary>Multiplies a scalar by a vector.</summary>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector2 operator *(float scalar, Vector2 vector) => vector * scalar;

    /// <summary>Multiplies two vectors componentwise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise product.</returns>
    public static Vector2 operator *(Vector2 left, Vector2 right) => new(left.X * right.X, left.Y * right.Y);

    /// <summary>Divides a vector by a scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Vector2 operator /(Vector2 vector, float divisor) => new(vector.X / divisor, vector.Y / divisor);

    /// <summary>Divides two vectors componentwise.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The IEEE 754 componentwise quotient.</returns>
    public static Vector2 operator /(Vector2 left, Vector2 right) => new(left.X / right.X, left.Y / right.Y);

    /// <summary>Returns the truncated remainder of both components by a scalar.</summary>
    /// <param name="vector">The dividend.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The componentwise remainder. Use <see cref="PosMod(float)"/> for canonical negative handling.</returns>
    public static Vector2 operator %(Vector2 vector, float divisor) => new(vector.X % divisor, vector.Y % divisor);

    /// <summary>Returns the truncated componentwise remainder of two vectors.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The component divisors.</param>
    /// <returns>The componentwise remainder. Use <see cref="PosMod(Vector2)"/> for canonical negative handling.</returns>
    public static Vector2 operator %(Vector2 left, Vector2 right) => new(left.X % right.X, left.Y % right.Y);

    /// <summary>Tests both components for exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when both corresponding components are exactly equal.</returns>
    public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);

    /// <summary>Tests whether either component differs under exact equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when at least one corresponding component differs.</returns>
    public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Vector2 left, Vector2 right) => left.X == right.X ? left.Y < right.Y : left.X < right.X;

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Vector2 left, Vector2 right) => left.X == right.X ? left.Y > right.Y : left.X > right.X;

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Vector2 left, Vector2 right) => left.X == right.X ? left.Y <= right.Y : left.X < right.X;

    /// <summary>Compares vectors lexicographically by X and then Y.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Vector2 left, Vector2 right) => left.X == right.X ? left.Y >= right.Y : left.X > right.X;

    /// <summary>Tests whether another object is an exactly equal vector.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a vector with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector2 other && Equals(other);

    /// <summary>Tests both components for exact equality.</summary>
    /// <param name="other">The vector to compare.</param>
    /// <returns><see langword="true"/> when both corresponding components are exactly equal.</returns>
    public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;

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

}
