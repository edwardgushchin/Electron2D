namespace Electron2D;

/// <summary>Provides scalar constants and common mathematical operations for engine code and games.</summary>
/// <remarks>
/// Angles use radians unless a member explicitly names degrees. Single-precision overloads are the
/// primary engine-scalar API; double-precision overloads are provided for calculations that need a
/// wider range or tighter tolerance. All members are stateless and thread-safe; normal nonthrowing
/// calls are allocation-free after JIT warmup.
/// Floating-point members preserve normal IEEE 754 NaN, infinity, and signed-zero behavior unless
/// their individual contract states otherwise.
/// </remarks>
public static class Mathf
{
    private const float FloatEpsilon = 0.000001f;
    private const double DoubleEpsilon = 0.00000000000001d;
    private const float DegreesToRadiansFloat = 0.0174532925199432957692369077f;
    private const double DegreesToRadiansDouble = 0.0174532925199432957692369077d;
    private const float RadiansToDegreesFloat = 57.295779513082320876798154814f;
    private const double RadiansToDegreesDouble = 57.295779513082320876798154814d;

    /// <summary>The ratio of a circle's circumference to its radius.</summary>
    public const float Tau = 6.2831853071795864769252867666f;

    /// <summary>The ratio of a circle's circumference to its diameter.</summary>
    public const float Pi = 3.1415926535897932384626433833f;

    /// <summary>Positive single-precision infinity.</summary>
    public const float Inf = float.PositiveInfinity;

    /// <summary>A single-precision value that is not a number.</summary>
    /// <remarks>This value is unequal to every value, including itself.</remarks>
    public const float NaN = float.NaN;

    /// <summary>The base of the natural logarithm.</summary>
    public const float E = 2.7182818284590452353602874714f;

    /// <summary>The positive square root of two.</summary>
    public const float Sqrt2 = 1.4142135623730950488016887242f;

    /// <summary>The default absolute single-precision comparison tolerance.</summary>
    public const float Epsilon = FloatEpsilon;

    /// <summary>Returns the absolute value of an integer.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The nonnegative magnitude.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="int.MinValue"/>.</exception>
    public static int Abs(int value) => Math.Abs(value);

    /// <summary>Returns the absolute value of a single-precision number.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The nonnegative magnitude, or NaN when the input is NaN.</returns>
    public static float Abs(float value) => MathF.Abs(value);

    /// <summary>Returns the absolute value of a double-precision number.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The nonnegative magnitude, or NaN when the input is NaN.</returns>
    public static double Abs(double value) => Math.Abs(value);

    /// <summary>Returns the arc cosine in radians.</summary>
    /// <param name="value">A cosine value in the inclusive range negative one through one.</param>
    /// <returns>An angle from zero through <see cref="Pi"/>, or NaN for an out-of-range input.</returns>
    public static float Acos(float value) => MathF.Acos(value);

    /// <summary>Returns the double-precision arc cosine in radians.</summary>
    /// <param name="value">A cosine value in the inclusive range negative one through one.</param>
    /// <returns>An angle from zero through pi, or NaN for an out-of-range input.</returns>
    public static double Acos(double value) => Math.Acos(value);

    /// <summary>Returns the inverse hyperbolic cosine.</summary>
    /// <param name="value">A value greater than or equal to one.</param>
    /// <returns>The inverse hyperbolic cosine, or NaN for an input below one.</returns>
    public static float Acosh(float value) => MathF.Acosh(value);

    /// <summary>Returns the double-precision inverse hyperbolic cosine.</summary>
    /// <param name="value">A value greater than or equal to one.</param>
    /// <returns>The inverse hyperbolic cosine, or NaN for an input below one.</returns>
    public static double Acosh(double value) => Math.Acosh(value);

    /// <summary>Returns the shortest signed angular difference from one angle to another.</summary>
    /// <param name="from">The starting angle in radians.</param>
    /// <param name="to">The destination angle in radians.</param>
    /// <returns>A difference in the inclusive range negative pi through pi.</returns>
    /// <remarks>For opposite angles, the result is negative pi when <paramref name="from"/> is smaller than <paramref name="to"/> and positive pi otherwise.</remarks>
    public static float AngleDifference(float from, float to)
    {
        var difference = (to - from) % MathF.Tau;
        return ((2f * difference) % MathF.Tau) - difference;
    }

    /// <summary>Returns the shortest double-precision signed angular difference.</summary>
    /// <param name="from">The starting angle in radians.</param>
    /// <param name="to">The destination angle in radians.</param>
    /// <returns>A difference in the inclusive range negative pi through pi.</returns>
    public static double AngleDifference(double from, double to)
    {
        var difference = (to - from) % Math.Tau;
        return ((2d * difference) % Math.Tau) - difference;
    }

    /// <summary>Returns the arc sine in radians.</summary>
    /// <param name="value">A sine value in the inclusive range negative one through one.</param>
    /// <returns>An angle from negative pi over two through positive pi over two, or NaN for an out-of-range input.</returns>
    public static float Asin(float value) => MathF.Asin(value);

    /// <summary>Returns the double-precision arc sine in radians.</summary>
    /// <param name="value">A sine value in the inclusive range negative one through one.</param>
    /// <returns>An angle from negative pi over two through positive pi over two, or NaN for an out-of-range input.</returns>
    public static double Asin(double value) => Math.Asin(value);

    /// <summary>Returns the inverse hyperbolic sine.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The inverse hyperbolic sine.</returns>
    public static float Asinh(float value) => MathF.Asinh(value);

    /// <summary>Returns the double-precision inverse hyperbolic sine.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The inverse hyperbolic sine.</returns>
    public static double Asinh(double value) => Math.Asinh(value);

    /// <summary>Returns the arc tangent in radians.</summary>
    /// <param name="value">The tangent value.</param>
    /// <returns>An angle from negative pi over two through positive pi over two.</returns>
    public static float Atan(float value) => MathF.Atan(value);

    /// <summary>Returns the double-precision arc tangent in radians.</summary>
    /// <param name="value">The tangent value.</param>
    /// <returns>An angle from negative pi over two through positive pi over two.</returns>
    public static double Atan(double value) => Math.Atan(value);

    /// <summary>Returns the angle of a Cartesian direction in radians.</summary>
    /// <param name="y">The vertical component.</param>
    /// <param name="x">The horizontal component.</param>
    /// <returns>The quadrant-aware angle from negative pi through positive pi.</returns>
    public static float Atan2(float y, float x) => MathF.Atan2(y, x);

    /// <summary>Returns the double-precision angle of a Cartesian direction in radians.</summary>
    /// <param name="y">The vertical component.</param>
    /// <param name="x">The horizontal component.</param>
    /// <returns>The quadrant-aware angle from negative pi through positive pi.</returns>
    public static double Atan2(double y, double x) => Math.Atan2(y, x);

    /// <summary>Returns the inverse hyperbolic tangent.</summary>
    /// <param name="value">A value in the inclusive range negative one through one.</param>
    /// <returns>The inverse hyperbolic tangent; the endpoints produce infinities and out-of-range inputs produce NaN.</returns>
    public static float Atanh(float value) => MathF.Atanh(value);

    /// <summary>Returns the double-precision inverse hyperbolic tangent.</summary>
    /// <param name="value">A value in the inclusive range negative one through one.</param>
    /// <returns>The inverse hyperbolic tangent; the endpoints produce infinities and out-of-range inputs produce NaN.</returns>
    public static double Atanh(double value) => Math.Atanh(value);

    /// <summary>Rounds upward toward positive infinity.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The smallest integral floating-point value not less than the input.</returns>
    public static float Ceil(float value) => MathF.Ceiling(value);

    /// <summary>Rounds a double-precision value upward toward positive infinity.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The smallest integral floating-point value not less than the input.</returns>
    public static double Ceil(double value) => Math.Ceiling(value);

    /// <summary>Rounds upward and converts to a 32-bit integer.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The ceiling converted using unchecked managed numeric conversion semantics.</returns>
    /// <remarks>NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.</remarks>
    public static int CeilToInt(float value) => unchecked((int)MathF.Ceiling(value));

    /// <summary>Rounds a double-precision value upward and converts to a 32-bit integer.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The ceiling converted using unchecked managed numeric conversion semantics.</returns>
    /// <remarks>NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.</remarks>
    public static int CeilToInt(double value) => unchecked((int)Math.Ceiling(value));

    /// <summary>Restricts an integer to an inclusive interval.</summary>
    /// <param name="value">The value to restrict.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>The restricted value.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);

    /// <summary>Restricts a single-precision number to an inclusive interval.</summary>
    /// <param name="value">The value to restrict.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>The restricted value.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);

    /// <summary>Restricts a double-precision number to an inclusive interval.</summary>
    /// <param name="value">The value to restrict.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>The restricted value.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);

    /// <summary>Returns the cosine of an angle in radians.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>The cosine.</returns>
    public static float Cos(float angle) => MathF.Cos(angle);

    /// <summary>Returns the double-precision cosine of an angle in radians.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>The cosine.</returns>
    public static double Cos(double angle) => Math.Cos(angle);

    /// <summary>Returns the hyperbolic cosine.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The hyperbolic cosine.</returns>
    public static float Cosh(float value) => MathF.Cosh(value);

    /// <summary>Returns the double-precision hyperbolic cosine.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The hyperbolic cosine.</returns>
    public static double Cosh(double value) => Math.Cosh(value);

    /// <summary>Performs Catmull-Rom cubic interpolation between two values.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="pre">The control value before <paramref name="from"/>.</param>
    /// <param name="post">The control value after <paramref name="to"/>.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated value.</returns>
    public static float CubicInterpolate(float from, float to, float pre, float post, float weight)
    {
        var squared = weight * weight;
        return 0.5f * ((2f * from) + ((-pre + to) * weight) +
            (((2f * pre) - (5f * from) + (4f * to) - post) * squared) +
            ((-pre + (3f * from) - (3f * to) + post) * squared * weight));
    }

    /// <summary>Performs double-precision Catmull-Rom cubic interpolation.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="pre">The control value before <paramref name="from"/>.</param>
    /// <param name="post">The control value after <paramref name="to"/>.</param>
    /// <param name="weight">The interpolation weight; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated value.</returns>
    public static double CubicInterpolate(double from, double to, double pre, double post, double weight)
    {
        var squared = weight * weight;
        return 0.5d * ((2d * from) + ((-pre + to) * weight) +
            (((2d * pre) - (5d * from) + (4d * to) - post) * squared) +
            ((-pre + (3d * from) - (3d * to) + post) * squared * weight));
    }

    /// <summary>Performs shortest-path Catmull-Rom interpolation between angles.</summary>
    /// <param name="from">The starting angle.</param>
    /// <param name="to">The destination angle.</param>
    /// <param name="pre">The preceding control angle.</param>
    /// <param name="post">The following control angle.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The interpolated angle in radians.</returns>
    public static float CubicInterpolateAngle(float from, float to, float pre, float post, float weight)
    {
        var fromRotation = from % MathF.Tau;
        var preDifference = (pre - fromRotation) % MathF.Tau;
        var preRotation = fromRotation + ((2f * preDifference) % MathF.Tau) - preDifference;
        var toDifference = (to - fromRotation) % MathF.Tau;
        var toRotation = fromRotation + ((2f * toDifference) % MathF.Tau) - toDifference;
        var postDifference = (post - toRotation) % MathF.Tau;
        var postRotation = toRotation + ((2f * postDifference) % MathF.Tau) - postDifference;
        return CubicInterpolate(fromRotation, toRotation, preRotation, postRotation, weight);
    }

    /// <summary>Performs double-precision shortest-path Catmull-Rom interpolation between angles.</summary>
    /// <param name="from">The starting angle.</param>
    /// <param name="to">The destination angle.</param>
    /// <param name="pre">The preceding control angle.</param>
    /// <param name="post">The following control angle.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The interpolated angle in radians.</returns>
    public static double CubicInterpolateAngle(double from, double to, double pre, double post, double weight)
    {
        var fromRotation = from % Math.Tau;
        var preDifference = (pre - fromRotation) % Math.Tau;
        var preRotation = fromRotation + ((2d * preDifference) % Math.Tau) - preDifference;
        var toDifference = (to - fromRotation) % Math.Tau;
        var toRotation = fromRotation + ((2d * toDifference) % Math.Tau) - toDifference;
        var postDifference = (post - toRotation) % Math.Tau;
        var postRotation = toRotation + ((2d * postDifference) % Math.Tau) - postDifference;
        return CubicInterpolate(fromRotation, toRotation, preRotation, postRotation, weight);
    }

    /// <summary>Performs time-aware Barry-Goldman cubic interpolation.</summary>
    /// <param name="from">The starting value at time zero.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="pre">The preceding control value.</param>
    /// <param name="post">The following control value.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <param name="toTime">The destination time.</param>
    /// <param name="preTime">The preceding control time.</param>
    /// <param name="postTime">The following control time.</param>
    /// <returns>The time-aware interpolated value.</returns>
    public static float CubicInterpolateInTime(
        float from, float to, float pre, float post, float weight, float toTime, float preTime, float postTime)
    {
        var time = Lerp(0f, toTime, weight);
        var a1 = Lerp(pre, from, preTime == 0f ? 0f : (time - preTime) / -preTime);
        var a2 = Lerp(from, to, toTime == 0f ? 0.5f : time / toTime);
        var a3 = Lerp(to, post, postTime - toTime == 0f ? 1f : (time - toTime) / (postTime - toTime));
        var b1 = Lerp(a1, a2, toTime - preTime == 0f ? 0f : (time - preTime) / (toTime - preTime));
        var b2 = Lerp(a2, a3, postTime == 0f ? 1f : time / postTime);
        return Lerp(b1, b2, toTime == 0f ? 0.5f : time / toTime);
    }

    /// <summary>Performs double-precision time-aware Barry-Goldman cubic interpolation.</summary>
    /// <param name="from">The starting value at time zero.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="pre">The preceding control value.</param>
    /// <param name="post">The following control value.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <param name="toTime">The destination time.</param>
    /// <param name="preTime">The preceding control time.</param>
    /// <param name="postTime">The following control time.</param>
    /// <returns>The time-aware interpolated value.</returns>
    public static double CubicInterpolateInTime(
        double from, double to, double pre, double post, double weight, double toTime, double preTime, double postTime)
    {
        var time = Lerp(0d, toTime, weight);
        var a1 = Lerp(pre, from, preTime == 0d ? 0d : (time - preTime) / -preTime);
        var a2 = Lerp(from, to, toTime == 0d ? 0.5d : time / toTime);
        var a3 = Lerp(to, post, postTime - toTime == 0d ? 1d : (time - toTime) / (postTime - toTime));
        var b1 = Lerp(a1, a2, toTime - preTime == 0d ? 0d : (time - preTime) / (toTime - preTime));
        var b2 = Lerp(a2, a3, postTime == 0d ? 1d : time / postTime);
        return Lerp(b1, b2, toTime == 0d ? 0.5d : time / toTime);
    }

    /// <summary>Performs shortest-path time-aware cubic interpolation between angles.</summary>
    /// <param name="from">The starting angle at time zero.</param>
    /// <param name="to">The destination angle.</param>
    /// <param name="pre">The preceding control angle.</param>
    /// <param name="post">The following control angle.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <param name="toTime">The destination time.</param>
    /// <param name="preTime">The preceding control time.</param>
    /// <param name="postTime">The following control time.</param>
    /// <returns>The time-aware interpolated angle.</returns>
    public static float CubicInterpolateAngleInTime(
        float from, float to, float pre, float post, float weight, float toTime, float preTime, float postTime)
    {
        var fromRotation = from % MathF.Tau;
        var preDifference = (pre - fromRotation) % MathF.Tau;
        var preRotation = fromRotation + ((2f * preDifference) % MathF.Tau) - preDifference;
        var toDifference = (to - fromRotation) % MathF.Tau;
        var toRotation = fromRotation + ((2f * toDifference) % MathF.Tau) - toDifference;
        var postDifference = (post - toRotation) % MathF.Tau;
        var postRotation = toRotation + ((2f * postDifference) % MathF.Tau) - postDifference;
        return CubicInterpolateInTime(
            fromRotation, toRotation, preRotation, postRotation, weight, toTime, preTime, postTime);
    }

    /// <summary>Performs double-precision shortest-path time-aware cubic interpolation between angles.</summary>
    /// <param name="from">The starting angle at time zero.</param>
    /// <param name="to">The destination angle.</param>
    /// <param name="pre">The preceding control angle.</param>
    /// <param name="post">The following control angle.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <param name="toTime">The destination time.</param>
    /// <param name="preTime">The preceding control time.</param>
    /// <param name="postTime">The following control time.</param>
    /// <returns>The time-aware interpolated angle.</returns>
    public static double CubicInterpolateAngleInTime(
        double from, double to, double pre, double post, double weight, double toTime, double preTime, double postTime)
    {
        var fromRotation = from % Math.Tau;
        var preDifference = (pre - fromRotation) % Math.Tau;
        var preRotation = fromRotation + ((2d * preDifference) % Math.Tau) - preDifference;
        var toDifference = (to - fromRotation) % Math.Tau;
        var toRotation = fromRotation + ((2d * toDifference) % Math.Tau) - toDifference;
        var postDifference = (post - toRotation) % Math.Tau;
        var postRotation = toRotation + ((2d * postDifference) % Math.Tau) - postDifference;
        return CubicInterpolateInTime(
            fromRotation, toRotation, preRotation, postRotation, weight, toTime, preTime, postTime);
    }

    /// <summary>Evaluates a one-dimensional cubic Bezier curve.</summary>
    /// <param name="start">The start value.</param>
    /// <param name="control1">The first control value.</param>
    /// <param name="control2">The second control value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="weight">The curve parameter; values outside zero through one extrapolate.</param>
    /// <returns>The curve value.</returns>
    public static float BezierInterpolate(float start, float control1, float control2, float end, float weight)
    {
        var inverse = 1f - weight;
        var inverseSquared = inverse * inverse;
        var weightSquared = weight * weight;
        return (start * inverseSquared * inverse) + (control1 * inverseSquared * weight * 3f) +
            (control2 * inverse * weightSquared * 3f) + (end * weightSquared * weight);
    }

    /// <summary>Evaluates a double-precision one-dimensional cubic Bezier curve.</summary>
    /// <param name="start">The start value.</param>
    /// <param name="control1">The first control value.</param>
    /// <param name="control2">The second control value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="weight">The curve parameter; values outside zero through one extrapolate.</param>
    /// <returns>The curve value.</returns>
    public static double BezierInterpolate(double start, double control1, double control2, double end, double weight)
    {
        var inverse = 1d - weight;
        var inverseSquared = inverse * inverse;
        var weightSquared = weight * weight;
        return (start * inverseSquared * inverse) + (control1 * inverseSquared * weight * 3d) +
            (control2 * inverse * weightSquared * 3d) + (end * weightSquared * weight);
    }

    /// <summary>Evaluates the derivative of a one-dimensional cubic Bezier curve.</summary>
    /// <param name="start">The start value.</param>
    /// <param name="control1">The first control value.</param>
    /// <param name="control2">The second control value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="weight">The curve parameter.</param>
    /// <returns>The derivative at the parameter.</returns>
    public static float BezierDerivative(float start, float control1, float control2, float end, float weight)
    {
        var inverse = 1f - weight;
        return (3f * (control1 - start) * inverse * inverse) +
            (6f * (control2 - control1) * inverse * weight) +
            (3f * (end - control2) * weight * weight);
    }

    /// <summary>Evaluates the derivative of a double-precision one-dimensional cubic Bezier curve.</summary>
    /// <param name="start">The start value.</param>
    /// <param name="control1">The first control value.</param>
    /// <param name="control2">The second control value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="weight">The curve parameter.</param>
    /// <returns>The derivative at the parameter.</returns>
    public static double BezierDerivative(double start, double control1, double control2, double end, double weight)
    {
        var inverse = 1d - weight;
        return (3d * (control1 - start) * inverse * inverse) +
            (6d * (control2 - control1) * inverse * weight) +
            (3d * (end - control2) * weight * weight);
    }

    /// <summary>Converts decibels to linear energy.</summary>
    /// <param name="decibels">The decibel value.</param>
    /// <returns>The corresponding linear energy.</returns>
    public static float DbToLinear(float decibels) => MathF.Exp(decibels * 0.11512925464970228420089957273422f);

    /// <summary>Converts double-precision decibels to linear energy.</summary>
    /// <param name="decibels">The decibel value.</param>
    /// <returns>The corresponding linear energy.</returns>
    public static double DbToLinear(double decibels) => Math.Exp(decibels * 0.11512925464970228420089957273422d);

    /// <summary>Converts degrees to radians.</summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static float DegToRad(float degrees) => degrees * DegreesToRadiansFloat;

    /// <summary>Converts double-precision degrees to radians.</summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static double DegToRad(double degrees) => degrees * DegreesToRadiansDouble;

    /// <summary>Returns the number of encoded decimal fractional digits.</summary>
    /// <param name="value">A finite value representable by <see cref="decimal"/>.</param>
    /// <returns>The scale retained by conversion to <see cref="decimal"/>.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is non-finite or outside the decimal range.</exception>
    public static int DecimalCount(double value) => DecimalCount((decimal)value);

    /// <summary>Returns the number of encoded decimal fractional digits.</summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The stored decimal scale from zero through twenty-eight.</returns>
    public static int DecimalCount(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        return (bits[3] >> 16) & 0x7f;
    }

    /// <summary>Applies an exponent-based easing curve to a normalized value.</summary>
    /// <param name="value">The input, clamped to zero through one.</param>
    /// <param name="curve">Zero for constant zero, one for linear, positive values for in or out easing, and negative values for in-out easing.</param>
    /// <returns>The eased value.</returns>
    public static float Ease(float value, float curve)
    {
        value = Clamp(value, 0f, 1f);
        if (curve > 0f)
            return curve < 1f ? 1f - MathF.Pow(1f - value, 1f / curve) : MathF.Pow(value, curve);
        if (curve < 0f)
            return value < 0.5f
                ? MathF.Pow(value * 2f, -curve) * 0.5f
                : ((1f - MathF.Pow(1f - ((value - 0.5f) * 2f), -curve)) * 0.5f) + 0.5f;
        return 0f;
    }

    /// <summary>Applies a double-precision exponent-based easing curve to a normalized value.</summary>
    /// <param name="value">The input, clamped to zero through one.</param>
    /// <param name="curve">Zero for constant zero, one for linear, positive values for in or out easing, and negative values for in-out easing.</param>
    /// <returns>The eased value.</returns>
    public static double Ease(double value, double curve)
    {
        value = Clamp(value, 0d, 1d);
        if (curve > 0d)
            return curve < 1d ? 1d - Math.Pow(1d - value, 1d / curve) : Math.Pow(value, curve);
        if (curve < 0d)
            return value < 0.5d
                ? Math.Pow(value * 2d, -curve) * 0.5d
                : ((1d - Math.Pow(1d - ((value - 0.5d) * 2d), -curve)) * 0.5d) + 0.5d;
        return 0d;
    }

    /// <summary>Raises the natural-logarithm base to a power.</summary>
    /// <param name="value">The exponent.</param>
    /// <returns>The natural exponential.</returns>
    public static float Exp(float value) => MathF.Exp(value);

    /// <summary>Raises the natural-logarithm base to a double-precision power.</summary>
    /// <param name="value">The exponent.</param>
    /// <returns>The natural exponential.</returns>
    public static double Exp(double value) => Math.Exp(value);

    /// <summary>Rounds downward toward negative infinity.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The greatest integral floating-point value not greater than the input.</returns>
    public static float Floor(float value) => MathF.Floor(value);

    /// <summary>Rounds a double-precision value downward toward negative infinity.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The greatest integral floating-point value not greater than the input.</returns>
    public static double Floor(double value) => Math.Floor(value);

    /// <summary>Rounds downward and converts to a 32-bit integer.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The floor converted using unchecked managed numeric conversion semantics.</returns>
    /// <remarks>NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.</remarks>
    public static int FloorToInt(float value) => unchecked((int)MathF.Floor(value));

    /// <summary>Rounds a double-precision value downward and converts to a 32-bit integer.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The floor converted using unchecked managed numeric conversion semantics.</returns>
    /// <remarks>NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.</remarks>
    public static int FloorToInt(double value) => unchecked((int)Math.Floor(value));

    /// <summary>Returns the unbounded interpolation weight of a value within an interval.</summary>
    /// <param name="from">The start of the interval.</param>
    /// <param name="to">The end of the interval.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns><c>(value - from) / (to - from)</c>; equal bounds follow IEEE 754 division behavior.</returns>
    public static float InverseLerp(float from, float to, float value) => (value - from) / (to - from);

    /// <summary>Returns the double-precision unbounded interpolation weight of a value within an interval.</summary>
    /// <param name="from">The start of the interval.</param>
    /// <param name="to">The end of the interval.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns><c>(value - from) / (to - from)</c>; equal bounds follow IEEE 754 division behavior.</returns>
    public static double InverseLerp(double from, double to, double value) => (value - from) / (to - from);

    /// <summary>Tests two single-precision values for scale-aware approximate equality.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> for exact equality or a difference below the larger of <see cref="Epsilon"/> and <c>Epsilon * abs(left)</c>.</returns>
    public static bool IsEqualApprox(float left, float right)
    {
        if (left == right)
            return true;
        var tolerance = MathF.Max(FloatEpsilon * MathF.Abs(left), FloatEpsilon);
        return MathF.Abs(left - right) < tolerance;
    }

    /// <summary>Tests two double-precision values for scale-aware approximate equality.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> for exact equality or a difference below the double-precision relative tolerance.</returns>
    public static bool IsEqualApprox(double left, double right)
    {
        if (left == right)
            return true;
        var tolerance = Math.Max(DoubleEpsilon * Math.Abs(left), DoubleEpsilon);
        return Math.Abs(left - right) < tolerance;
    }

    /// <summary>Tests two single-precision values using a caller-supplied absolute tolerance.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <param name="tolerance">The strict upper bound for their absolute difference.</param>
    /// <returns><see langword="true"/> for exact equality or a difference strictly below <paramref name="tolerance"/>.</returns>
    /// <remarks>A negative or NaN tolerance only permits exact equality.</remarks>
    public static bool IsEqualApprox(float left, float right, float tolerance) =>
        left == right || MathF.Abs(left - right) < tolerance;

    /// <summary>Tests two double-precision values using a caller-supplied absolute tolerance.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <param name="tolerance">The strict upper bound for their absolute difference.</param>
    /// <returns><see langword="true"/> for exact equality or a difference strictly below <paramref name="tolerance"/>.</returns>
    /// <remarks>A negative or NaN tolerance only permits exact equality.</remarks>
    public static bool IsEqualApprox(double left, double right, double tolerance) =>
        left == right || Math.Abs(left - right) < tolerance;

    /// <summary>Tests whether a single-precision value is neither NaN nor infinity.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite value.</returns>
    public static bool IsFinite(float value) => float.IsFinite(value);

    /// <summary>Tests whether a double-precision value is neither NaN nor infinity.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a finite value.</returns>
    public static bool IsFinite(double value) => double.IsFinite(value);

    /// <summary>Tests whether a single-precision value is positive or negative infinity.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for either infinity.</returns>
    public static bool IsInf(float value) => float.IsInfinity(value);

    /// <summary>Tests whether a double-precision value is positive or negative infinity.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for either infinity.</returns>
    public static bool IsInf(double value) => double.IsInfinity(value);

    /// <summary>Tests whether a single-precision value is not a number.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for NaN.</returns>
    public static bool IsNaN(float value) => float.IsNaN(value);

    /// <summary>Tests whether a double-precision value is not a number.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for NaN.</returns>
    public static bool IsNaN(double value) => double.IsNaN(value);

    /// <summary>Tests whether a single-precision value's magnitude is strictly below <see cref="Epsilon"/>.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is approximately zero.</returns>
    public static bool IsZeroApprox(float value) => MathF.Abs(value) < FloatEpsilon;

    /// <summary>Tests whether a double-precision value's magnitude is strictly below the double-precision epsilon.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is approximately zero.</returns>
    public static bool IsZeroApprox(double value) => Math.Abs(value) < DoubleEpsilon;

    /// <summary>Linearly interpolates without clamping the weight.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The interpolated or extrapolated value.</returns>
    public static float Lerp(float from, float to, float weight) => from + ((to - from) * weight);

    /// <summary>Linearly interpolates double-precision values without clamping the weight.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="weight">The interpolation weight.</param>
    /// <returns>The interpolated or extrapolated value.</returns>
    public static double Lerp(double from, double to, double weight) => from + ((to - from) * weight);

    /// <summary>Linearly interpolates between angles along their shortest path.</summary>
    /// <param name="from">The starting angle in radians.</param>
    /// <param name="to">The destination angle in radians.</param>
    /// <param name="weight">The unbounded interpolation weight.</param>
    /// <returns>The interpolated angle.</returns>
    public static float LerpAngle(float from, float to, float weight) => from + (AngleDifference(from, to) * weight);

    /// <summary>Linearly interpolates between double-precision angles along their shortest path.</summary>
    /// <param name="from">The starting angle in radians.</param>
    /// <param name="to">The destination angle in radians.</param>
    /// <param name="weight">The unbounded interpolation weight.</param>
    /// <returns>The interpolated angle.</returns>
    public static double LerpAngle(double from, double to, double weight) => from + (AngleDifference(from, to) * weight);

    /// <summary>Converts linear energy to decibels.</summary>
    /// <param name="linear">The linear energy.</param>
    /// <returns>The decibel value; zero produces negative infinity and negative input produces NaN.</returns>
    public static float LinearToDb(float linear) => MathF.Log(linear) * 8.6858896380650365530225783783321f;

    /// <summary>Converts double-precision linear energy to decibels.</summary>
    /// <param name="linear">The linear energy.</param>
    /// <returns>The decibel value; zero produces negative infinity and negative input produces NaN.</returns>
    public static double LinearToDb(double linear) => Math.Log(linear) * 8.6858896380650365530225783783321d;

    /// <summary>Returns the natural logarithm.</summary>
    /// <param name="value">The input; zero produces negative infinity and negative input produces NaN.</param>
    /// <returns>The natural logarithm.</returns>
    public static float Log(float value) => MathF.Log(value);

    /// <summary>Returns the double-precision natural logarithm.</summary>
    /// <param name="value">The input; zero produces negative infinity and negative input produces NaN.</param>
    /// <returns>The natural logarithm.</returns>
    public static double Log(double value) => Math.Log(value);

    /// <summary>Returns the larger integer.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The larger value.</returns>
    public static int Max(int left, int right) => Math.Max(left, right);

    /// <summary>Returns the larger single-precision value.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The larger value, or NaN if either value is NaN.</returns>
    public static float Max(float left, float right) => MathF.Max(left, right);

    /// <summary>Returns the larger double-precision value.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The larger value, or NaN if either value is NaN.</returns>
    public static double Max(double left, double right) => Math.Max(left, right);

    /// <summary>Returns the smaller integer.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The smaller value.</returns>
    public static int Min(int left, int right) => Math.Min(left, right);

    /// <summary>Returns the smaller single-precision value.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The smaller value, or NaN if either value is NaN.</returns>
    public static float Min(float left, float right) => MathF.Min(left, right);

    /// <summary>Returns the smaller double-precision value.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The smaller value, or NaN if either value is NaN.</returns>
    public static double Min(double left, double right) => Math.Min(left, right);

    /// <summary>Moves a value toward a destination by a maximum delta.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="delta">The step; a negative step moves away.</param>
    /// <returns>The destination when within the step, otherwise the stepped value.</returns>
    /// <exception cref="ArithmeticException"><paramref name="from"/> or <paramref name="to"/> makes the signed difference NaN.</exception>
    public static float MoveToward(float from, float to, float delta) =>
        MathF.Abs(to - from) <= delta ? to : from + (MathF.Sign(to - from) * delta);

    /// <summary>Moves a double-precision value toward a destination by a maximum delta.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="delta">The step; a negative step moves away.</param>
    /// <returns>The destination when within the step, otherwise the stepped value.</returns>
    /// <exception cref="ArithmeticException"><paramref name="from"/> or <paramref name="to"/> makes the signed difference NaN.</exception>
    public static double MoveToward(double from, double to, double delta) =>
        Math.Abs(to - from) <= delta ? to : from + (Math.Sign(to - from) * delta);

    /// <summary>Returns the smallest representable power of two not less than a positive integer.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>Zero for ordinary nonpositive values, the input for a power of two, or the next power of two; overflow wraps exactly as 32-bit arithmetic.</returns>
    public static int NearestPo2(int value)
    {
        unchecked
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }
    }

    /// <summary>Returns a canonical integer remainder with the divisor's sign.</summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>A remainder between zero and the divisor in the divisor's direction.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="int.MinValue"/> and <paramref name="divisor"/> is negative one.</exception>
    public static int PosMod(int value, int divisor)
    {
        var remainder = value % divisor;
        return (remainder < 0 && divisor > 0) || (remainder > 0 && divisor < 0)
            ? remainder + divisor
            : remainder;
    }

    /// <summary>Returns a canonical single-precision remainder with the divisor's sign.</summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>A remainder in the divisor's direction; a zero divisor produces NaN.</returns>
    public static float PosMod(float value, float divisor)
    {
        var remainder = value % divisor;
        return (remainder < 0f && divisor > 0f) || (remainder > 0f && divisor < 0f)
            ? remainder + divisor
            : remainder;
    }

    /// <summary>Returns a canonical double-precision remainder with the divisor's sign.</summary>
    /// <param name="value">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>A remainder in the divisor's direction; a zero divisor produces NaN.</returns>
    public static double PosMod(double value, double divisor)
    {
        var remainder = value % divisor;
        return (remainder < 0d && divisor > 0d) || (remainder > 0d && divisor < 0d)
            ? remainder + divisor
            : remainder;
    }

    /// <summary>Raises a value to a power.</summary>
    /// <param name="value">The base.</param>
    /// <param name="power">The exponent.</param>
    /// <returns>The power result under IEEE 754 rules.</returns>
    public static float Pow(float value, float power) => MathF.Pow(value, power);

    /// <summary>Raises a double-precision value to a power.</summary>
    /// <param name="value">The base.</param>
    /// <param name="power">The exponent.</param>
    /// <returns>The power result under IEEE 754 rules.</returns>
    public static double Pow(double value, double power) => Math.Pow(value, power);

    /// <summary>Converts radians to degrees.</summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static float RadToDeg(float radians) => radians * RadiansToDegreesFloat;

    /// <summary>Converts double-precision radians to degrees.</summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static double RadToDeg(double radians) => radians * RadiansToDegreesDouble;

    /// <summary>Maps a value linearly from one interval to another without clamping.</summary>
    /// <param name="value">The input value.</param>
    /// <param name="inputFrom">The input interval start.</param>
    /// <param name="inputTo">The input interval end.</param>
    /// <param name="outputFrom">The output interval start.</param>
    /// <param name="outputTo">The output interval end.</param>
    /// <returns>The mapped value; equal input bounds follow IEEE 754 division behavior.</returns>
    public static float Remap(float value, float inputFrom, float inputTo, float outputFrom, float outputTo) =>
        Lerp(outputFrom, outputTo, InverseLerp(inputFrom, inputTo, value));

    /// <summary>Maps a double-precision value linearly from one interval to another without clamping.</summary>
    /// <param name="value">The input value.</param>
    /// <param name="inputFrom">The input interval start.</param>
    /// <param name="inputTo">The input interval end.</param>
    /// <param name="outputFrom">The output interval start.</param>
    /// <param name="outputTo">The output interval end.</param>
    /// <returns>The mapped value; equal input bounds follow IEEE 754 division behavior.</returns>
    public static double Remap(double value, double inputFrom, double inputTo, double outputFrom, double outputTo) =>
        Lerp(outputFrom, outputTo, InverseLerp(inputFrom, inputTo, value));

    /// <summary>Rotates an angle toward another angle without overshooting.</summary>
    /// <param name="from">The starting angle in radians.</param>
    /// <param name="to">The destination angle in radians.</param>
    /// <param name="delta">The step; a negative value rotates toward the opposite angle.</param>
    /// <returns>The stepped angle.</returns>
    public static float RotateToward(float from, float to, float delta)
    {
        var difference = AngleDifference(from, to);
        var magnitude = MathF.Abs(difference);
        return from + (Math.Clamp(delta, magnitude - MathF.PI, magnitude) * (difference >= 0f ? 1f : -1f));
    }

    /// <summary>Rotates a double-precision angle toward another angle without overshooting.</summary>
    /// <param name="from">The starting angle in radians.</param>
    /// <param name="to">The destination angle in radians.</param>
    /// <param name="delta">The step; a negative value rotates toward the opposite angle.</param>
    /// <returns>The stepped angle.</returns>
    public static double RotateToward(double from, double to, double delta)
    {
        var difference = AngleDifference(from, to);
        var magnitude = Math.Abs(difference);
        return from + (Math.Clamp(delta, magnitude - Math.PI, magnitude) * (difference >= 0d ? 1d : -1d));
    }

    /// <summary>Rounds to the nearest integral floating-point value, with midpoint ties to even.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The rounded value.</returns>
    public static float Round(float value) => MathF.Round(value);

    /// <summary>Rounds a double-precision value to the nearest integral value, with midpoint ties to even.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The rounded value.</returns>
    public static double Round(double value) => Math.Round(value);

    /// <summary>Rounds to the nearest integer, with midpoint ties to even.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The rounded value converted using unchecked managed numeric conversion semantics.</returns>
    /// <remarks>NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.</remarks>
    public static int RoundToInt(float value) => unchecked((int)MathF.Round(value));

    /// <summary>Rounds a double-precision value to the nearest integer, with midpoint ties to even.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The rounded value converted using unchecked managed numeric conversion semantics.</returns>
    /// <remarks>NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.</remarks>
    public static int RoundToInt(double value) => unchecked((int)Math.Round(value));

    /// <summary>Returns negative one, zero, or positive one according to an integer's sign.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The sign.</returns>
    public static int Sign(int value) => Math.Sign(value);

    /// <summary>Returns negative one, zero, or positive one according to a single-precision value's sign.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The sign.</returns>
    /// <exception cref="ArithmeticException"><paramref name="value"/> is NaN.</exception>
    public static int Sign(float value) => MathF.Sign(value);

    /// <summary>Returns negative one, zero, or positive one according to a double-precision value's sign.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The sign.</returns>
    /// <exception cref="ArithmeticException"><paramref name="value"/> is NaN.</exception>
    public static int Sign(double value) => Math.Sign(value);

    /// <summary>Returns the sine of an angle in radians.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>The sine.</returns>
    public static float Sin(float angle) => MathF.Sin(angle);

    /// <summary>Returns the double-precision sine of an angle in radians.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>The sine.</returns>
    public static double Sin(double angle) => Math.Sin(angle);

    /// <summary>Returns the sine and cosine of an angle in one operation.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>A tuple containing sine followed by cosine.</returns>
    public static (float Sin, float Cos) SinCos(float angle) => MathF.SinCos(angle);

    /// <summary>Returns the double-precision sine and cosine of an angle in one operation.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>A tuple containing sine followed by cosine.</returns>
    public static (double Sin, double Cos) SinCos(double angle) => Math.SinCos(angle);

    /// <summary>Returns the hyperbolic sine.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The hyperbolic sine.</returns>
    public static float Sinh(float value) => MathF.Sinh(value);

    /// <summary>Returns the double-precision hyperbolic sine.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The hyperbolic sine.</returns>
    public static double Sinh(double value) => Math.Sinh(value);

    /// <summary>Returns a cubic Hermite step between two edges.</summary>
    /// <param name="from">The first edge.</param>
    /// <param name="to">The second edge.</param>
    /// <param name="value">The value to normalize between the edges.</param>
    /// <returns>A value from zero through one, or <paramref name="from"/> when the edges are approximately equal.</returns>
    public static float SmoothStep(float from, float to, float value)
    {
        if (IsEqualApprox(from, to))
            return from;
        var normalized = Clamp((value - from) / (to - from), 0f, 1f);
        return normalized * normalized * (3f - (2f * normalized));
    }

    /// <summary>Returns a double-precision cubic Hermite step between two edges.</summary>
    /// <param name="from">The first edge.</param>
    /// <param name="to">The second edge.</param>
    /// <param name="value">The value to normalize between the edges.</param>
    /// <returns>A value from zero through one, or <paramref name="from"/> when the edges are approximately equal.</returns>
    public static double SmoothStep(double from, double to, double value)
    {
        if (IsEqualApprox(from, to))
            return from;
        var normalized = Clamp((value - from) / (to - from), 0d, 1d);
        return normalized * normalized * (3d - (2d * normalized));
    }

    /// <summary>Returns the principal square root.</summary>
    /// <param name="value">A nonnegative value.</param>
    /// <returns>The square root, or NaN for a negative input.</returns>
    public static float Sqrt(float value) => MathF.Sqrt(value);

    /// <summary>Returns the double-precision principal square root.</summary>
    /// <param name="value">A nonnegative value.</param>
    /// <returns>The square root, or NaN for a negative input.</returns>
    public static double Sqrt(double value) => Math.Sqrt(value);

    /// <summary>Estimates the position of the first significant fractional decimal digit.</summary>
    /// <param name="step">The input step.</param>
    /// <returns>An index from zero through eight; integer and very small fractional inputs return zero.</returns>
    public static int StepDecimals(double step)
    {
        var fraction = Math.Abs(step) - (int)Math.Abs(step);
        if (fraction >= 0.9999d) return 0;
        if (fraction >= 0.09999d) return 1;
        if (fraction >= 0.009999d) return 2;
        if (fraction >= 0.0009999d) return 3;
        if (fraction >= 0.00009999d) return 4;
        if (fraction >= 0.000009999d) return 5;
        if (fraction >= 0.0000009999d) return 6;
        if (fraction >= 0.00000009999d) return 7;
        if (fraction >= 0.000000009999d) return 8;
        return 0;
    }

    /// <summary>Snaps a value to the nearest multiple of a step.</summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="step">The grid step; zero returns <paramref name="value"/> unchanged.</param>
    /// <returns><c>floor(value / step + 0.5) * step</c>.</returns>
    public static float Snapped(float value, float step) =>
        step == 0f ? value : MathF.Floor((value / step) + 0.5f) * step;

    /// <summary>Snaps a double-precision value to the nearest multiple of a step.</summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="step">The grid step; zero returns <paramref name="value"/> unchanged.</param>
    /// <returns><c>floor(value / step + 0.5) * step</c>.</returns>
    public static double Snapped(double value, double step) =>
        step == 0d ? value : Math.Floor((value / step) + 0.5d) * step;

    /// <summary>Returns the tangent of an angle in radians.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>The tangent.</returns>
    public static float Tan(float angle) => MathF.Tan(angle);

    /// <summary>Returns the double-precision tangent of an angle in radians.</summary>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>The tangent.</returns>
    public static double Tan(double angle) => Math.Tan(angle);

    /// <summary>Returns the hyperbolic tangent.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The hyperbolic tangent.</returns>
    public static float Tanh(float value) => MathF.Tanh(value);

    /// <summary>Returns the double-precision hyperbolic tangent.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The hyperbolic tangent.</returns>
    public static double Tanh(double value) => Math.Tanh(value);

    /// <summary>Wraps an integer into a half-open interval.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value, or <paramref name="min"/> when both bounds are equal.</returns>
    /// <remarks>Arithmetic uses unchecked 32-bit semantics; extreme bounds may overflow the interval width.</remarks>
    /// <exception cref="OverflowException">Unchecked bound arithmetic produces the exceptional <see cref="int.MinValue"/> remainder by negative one.</exception>
    public static int Wrap(int value, int min, int max)
    {
        unchecked
        {
            var range = max - min;
            return range == 0 ? min : min + ((((value - min) % range) + range) % range);
        }
    }

    /// <summary>Wraps a single-precision value into a half-open interval.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value, or <paramref name="min"/> when the interval width is approximately zero.</returns>
    public static float Wrap(float value, float min, float max)
    {
        var range = max - min;
        return IsZeroApprox(range) ? min : min + ((((value - min) % range) + range) % range);
    }

    /// <summary>Wraps a double-precision value into a half-open interval.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value, or <paramref name="min"/> when the interval width is approximately zero.</returns>
    public static double Wrap(double value, double min, double max)
    {
        var range = max - min;
        return IsZeroApprox(range) ? min : min + ((((value - min) % range) + range) % range);
    }

    /// <summary>Generates a triangle wave between zero and a length.</summary>
    /// <param name="value">The wave position.</param>
    /// <param name="length">The endpoint magnitude; negative values behave like their positive magnitude.</param>
    /// <returns>The reflected periodic value, or zero when <paramref name="length"/> is zero.</returns>
    public static float PingPong(float value, float length)
    {
        if (length == 0f)
            return 0f;
        var normalized = (value - length) / (length * 2f);
        var fraction = normalized - MathF.Floor(normalized);
        return MathF.Abs((fraction * length * 2f) - length);
    }

    /// <summary>Generates a double-precision triangle wave between zero and a length.</summary>
    /// <param name="value">The wave position.</param>
    /// <param name="length">The endpoint magnitude; negative values behave like their positive magnitude.</param>
    /// <returns>The reflected periodic value, or zero when <paramref name="length"/> is zero.</returns>
    public static double PingPong(double value, double length)
    {
        if (length == 0d)
            return 0d;
        var normalized = (value - length) / (length * 2d);
        var fraction = normalized - Math.Floor(normalized);
        return Math.Abs((fraction * length * 2d) - length);
    }
}
