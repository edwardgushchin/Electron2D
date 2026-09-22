# Mathf

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Mathf.cs`](../../src/Core/Math/Mathf.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class Mathf`

> Provides scalar constants and common mathematical operations for engine code and games.

## Description

Provides scalar constants and common mathematical operations for engine code and games.

`Mathf` is the engine's stateless scalar-math surface. It centralizes single- and double-precision transcendental functions, angle conversion and shortest-path arithmetic, interpolation, approximation, rounding, wrapping, audio-scale conversion, and integer helpers. It owns no mutable state, resource, handle, callback, or lifecycle.

Single precision is the primary engine scalar contract. Double overloads exist where the audited typed API provides them. `Pi`, `Tau`, `E`, `Sqrt2`, `Inf`, `NaN`, and `Epsilon` are single-precision constants; `Epsilon` is exactly `1e-6f`. Double approximate comparisons use an internal `1e-14` threshold.

Angles use radians unless a member explicitly names degrees. Single-precision overloads are the
primary engine-scalar API; double-precision overloads are provided for calculations that need a
wider range or tighter tolerance. All members are stateless and thread-safe; normal nonthrowing
calls are allocation-free after JIT warmup.
Floating-point members preserve normal IEEE 754 NaN, infinity, and signed-zero behavior unless
their individual contract states otherwise.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
float angle = Mathf.DegToRad(90f);
float value = Mathf.Lerp(0f, 10f, 0.25f);
```

## Methods

| Member | Description |
| --- | --- |
| [`public static int Abs(int value)`](#m-electron2d-mathf-abs-system-int32) | Returns the absolute value of an integer. |
| [`public static float Abs(float value)`](#m-electron2d-mathf-abs-system-single) | Returns the absolute value of a single-precision number. |
| [`public static double Abs(double value)`](#m-electron2d-mathf-abs-system-double) | Returns the absolute value of a double-precision number. |
| [`public static float Acos(float value)`](#m-electron2d-mathf-acos-system-single) | Returns the arc cosine in radians. |
| [`public static double Acos(double value)`](#m-electron2d-mathf-acos-system-double) | Returns the double-precision arc cosine in radians. |
| [`public static float Acosh(float value)`](#m-electron2d-mathf-acosh-system-single) | Returns the inverse hyperbolic cosine. |
| [`public static double Acosh(double value)`](#m-electron2d-mathf-acosh-system-double) | Returns the double-precision inverse hyperbolic cosine. |
| [`public static float AngleDifference(float from, float to)`](#m-electron2d-mathf-angledifference-system-single-system-single) | Returns the shortest signed angular difference from one angle to another. |
| [`public static double AngleDifference(double from, double to)`](#m-electron2d-mathf-angledifference-system-double-system-double) | Returns the shortest double-precision signed angular difference. |
| [`public static float Asin(float value)`](#m-electron2d-mathf-asin-system-single) | Returns the arc sine in radians. |
| [`public static double Asin(double value)`](#m-electron2d-mathf-asin-system-double) | Returns the double-precision arc sine in radians. |
| [`public static float Asinh(float value)`](#m-electron2d-mathf-asinh-system-single) | Returns the inverse hyperbolic sine. |
| [`public static double Asinh(double value)`](#m-electron2d-mathf-asinh-system-double) | Returns the double-precision inverse hyperbolic sine. |
| [`public static float Atan(float value)`](#m-electron2d-mathf-atan-system-single) | Returns the arc tangent in radians. |
| [`public static double Atan(double value)`](#m-electron2d-mathf-atan-system-double) | Returns the double-precision arc tangent in radians. |
| [`public static float Atan2(float y, float x)`](#m-electron2d-mathf-atan2-system-single-system-single) | Returns the angle of a Cartesian direction in radians. |
| [`public static double Atan2(double y, double x)`](#m-electron2d-mathf-atan2-system-double-system-double) | Returns the double-precision angle of a Cartesian direction in radians. |
| [`public static float Atanh(float value)`](#m-electron2d-mathf-atanh-system-single) | Returns the inverse hyperbolic tangent. |
| [`public static double Atanh(double value)`](#m-electron2d-mathf-atanh-system-double) | Returns the double-precision inverse hyperbolic tangent. |
| [`public static float Ceil(float value)`](#m-electron2d-mathf-ceil-system-single) | Rounds upward toward positive infinity. |
| [`public static double Ceil(double value)`](#m-electron2d-mathf-ceil-system-double) | Rounds a double-precision value upward toward positive infinity. |
| [`public static int CeilToInt(float value)`](#m-electron2d-mathf-ceiltoint-system-single) | Rounds upward and converts to a 32-bit integer. |
| [`public static int CeilToInt(double value)`](#m-electron2d-mathf-ceiltoint-system-double) | Rounds a double-precision value upward and converts to a 32-bit integer. |
| [`public static int Clamp(int value, int min, int max)`](#m-electron2d-mathf-clamp-system-int32-system-int32-system-int32) | Restricts an integer to an inclusive interval. |
| [`public static float Clamp(float value, float min, float max)`](#m-electron2d-mathf-clamp-system-single-system-single-system-single) | Restricts a single-precision number to an inclusive interval. |
| [`public static double Clamp(double value, double min, double max)`](#m-electron2d-mathf-clamp-system-double-system-double-system-double) | Restricts a double-precision number to an inclusive interval. |
| [`public static float Cos(float angle)`](#m-electron2d-mathf-cos-system-single) | Returns the cosine of an angle in radians. |
| [`public static double Cos(double angle)`](#m-electron2d-mathf-cos-system-double) | Returns the double-precision cosine of an angle in radians. |
| [`public static float Cosh(float value)`](#m-electron2d-mathf-cosh-system-single) | Returns the hyperbolic cosine. |
| [`public static double Cosh(double value)`](#m-electron2d-mathf-cosh-system-double) | Returns the double-precision hyperbolic cosine. |
| [`public static float CubicInterpolate(float from, float to, float pre, float post, float weight)`](#m-electron2d-mathf-cubicinterpolate-system-single-system-single-system-single-system-single-system-single) | Performs Catmull-Rom cubic interpolation between two values. |
| [`public static double CubicInterpolate(double from, double to, double pre, double post, double weight)`](#m-electron2d-mathf-cubicinterpolate-system-double-system-double-system-double-system-double-system-double) | Performs double-precision Catmull-Rom cubic interpolation. |
| [`public static float CubicInterpolateAngle(float from, float to, float pre, float post, float weight)`](#m-electron2d-mathf-cubicinterpolateangle-system-single-system-single-system-single-system-single-system-single) | Performs shortest-path Catmull-Rom interpolation between angles. |
| [`public static double CubicInterpolateAngle(double from, double to, double pre, double post, double weight)`](#m-electron2d-mathf-cubicinterpolateangle-system-double-system-double-system-double-system-double-system-double) | Performs double-precision shortest-path Catmull-Rom interpolation between angles. |
| [`public static float CubicInterpolateInTime(float from, float to, float pre, float post, float weight, float toTime, float preTime, float postTime)`](#m-electron2d-mathf-cubicinterpolateintime-system-single-system-single-system-single-system-single-system-single-system-single-system-single-system-single) | Performs time-aware Barry-Goldman cubic interpolation. |
| [`public static double CubicInterpolateInTime(double from, double to, double pre, double post, double weight, double toTime, double preTime, double postTime)`](#m-electron2d-mathf-cubicinterpolateintime-system-double-system-double-system-double-system-double-system-double-system-double-system-double-system-double) | Performs double-precision time-aware Barry-Goldman cubic interpolation. |
| [`public static float CubicInterpolateAngleInTime(float from, float to, float pre, float post, float weight, float toTime, float preTime, float postTime)`](#m-electron2d-mathf-cubicinterpolateangleintime-system-single-system-single-system-single-system-single-system-single-system-single-system-single-system-single) | Performs shortest-path time-aware cubic interpolation between angles. |
| [`public static double CubicInterpolateAngleInTime(double from, double to, double pre, double post, double weight, double toTime, double preTime, double postTime)`](#m-electron2d-mathf-cubicinterpolateangleintime-system-double-system-double-system-double-system-double-system-double-system-double-system-double-system-double) | Performs double-precision shortest-path time-aware cubic interpolation between angles. |
| [`public static float BezierInterpolate(float start, float control1, float control2, float end, float weight)`](#m-electron2d-mathf-bezierinterpolate-system-single-system-single-system-single-system-single-system-single) | Evaluates a one-dimensional cubic Bezier curve. |
| [`public static double BezierInterpolate(double start, double control1, double control2, double end, double weight)`](#m-electron2d-mathf-bezierinterpolate-system-double-system-double-system-double-system-double-system-double) | Evaluates a double-precision one-dimensional cubic Bezier curve. |
| [`public static float BezierDerivative(float start, float control1, float control2, float end, float weight)`](#m-electron2d-mathf-bezierderivative-system-single-system-single-system-single-system-single-system-single) | Evaluates the derivative of a one-dimensional cubic Bezier curve. |
| [`public static double BezierDerivative(double start, double control1, double control2, double end, double weight)`](#m-electron2d-mathf-bezierderivative-system-double-system-double-system-double-system-double-system-double) | Evaluates the derivative of a double-precision one-dimensional cubic Bezier curve. |
| [`public static float DBToLinear(float decibels)`](#m-electron2d-mathf-dbtolinear-system-single) | Converts decibels to linear energy. |
| [`public static double DBToLinear(double decibels)`](#m-electron2d-mathf-dbtolinear-system-double) | Converts double-precision decibels to linear energy. |
| [`public static float DegToRad(float degrees)`](#m-electron2d-mathf-degtorad-system-single) | Converts degrees to radians. |
| [`public static double DegToRad(double degrees)`](#m-electron2d-mathf-degtorad-system-double) | Converts double-precision degrees to radians. |
| [`public static int DecimalCount(double value)`](#m-electron2d-mathf-decimalcount-system-double) | Returns the number of encoded decimal fractional digits. |
| [`public static int DecimalCount(decimal value)`](#m-electron2d-mathf-decimalcount-system-decimal) | Returns the number of encoded decimal fractional digits. |
| [`public static float Ease(float value, float curve)`](#m-electron2d-mathf-ease-system-single-system-single) | Applies an exponent-based easing curve to a normalized value. |
| [`public static double Ease(double value, double curve)`](#m-electron2d-mathf-ease-system-double-system-double) | Applies a double-precision exponent-based easing curve to a normalized value. |
| [`public static float Exp(float value)`](#m-electron2d-mathf-exp-system-single) | Raises the natural-logarithm base to a power. |
| [`public static double Exp(double value)`](#m-electron2d-mathf-exp-system-double) | Raises the natural-logarithm base to a double-precision power. |
| [`public static float Floor(float value)`](#m-electron2d-mathf-floor-system-single) | Rounds downward toward negative infinity. |
| [`public static double Floor(double value)`](#m-electron2d-mathf-floor-system-double) | Rounds a double-precision value downward toward negative infinity. |
| [`public static int FloorToInt(float value)`](#m-electron2d-mathf-floortoint-system-single) | Rounds downward and converts to a 32-bit integer. |
| [`public static int FloorToInt(double value)`](#m-electron2d-mathf-floortoint-system-double) | Rounds a double-precision value downward and converts to a 32-bit integer. |
| [`public static float InverseLerp(float from, float to, float value)`](#m-electron2d-mathf-inverselerp-system-single-system-single-system-single) | Returns the unbounded interpolation weight of a value within an interval. |
| [`public static double InverseLerp(double from, double to, double value)`](#m-electron2d-mathf-inverselerp-system-double-system-double-system-double) | Returns the double-precision unbounded interpolation weight of a value within an interval. |
| [`public static bool IsEqualApprox(float left, float right)`](#m-electron2d-mathf-isequalapprox-system-single-system-single) | Tests two single-precision values for scale-aware approximate equality. |
| [`public static bool IsEqualApprox(double left, double right)`](#m-electron2d-mathf-isequalapprox-system-double-system-double) | Tests two double-precision values for scale-aware approximate equality. |
| [`public static bool IsEqualApprox(float left, float right, float tolerance)`](#m-electron2d-mathf-isequalapprox-system-single-system-single-system-single) | Tests two single-precision values using a caller-supplied absolute tolerance. |
| [`public static bool IsEqualApprox(double left, double right, double tolerance)`](#m-electron2d-mathf-isequalapprox-system-double-system-double-system-double) | Tests two double-precision values using a caller-supplied absolute tolerance. |
| [`public static bool IsFinite(float value)`](#m-electron2d-mathf-isfinite-system-single) | Tests whether a single-precision value is neither NaN nor infinity. |
| [`public static bool IsFinite(double value)`](#m-electron2d-mathf-isfinite-system-double) | Tests whether a double-precision value is neither NaN nor infinity. |
| [`public static bool IsInf(float value)`](#m-electron2d-mathf-isinf-system-single) | Tests whether a single-precision value is positive or negative infinity. |
| [`public static bool IsInf(double value)`](#m-electron2d-mathf-isinf-system-double) | Tests whether a double-precision value is positive or negative infinity. |
| [`public static bool IsNaN(float value)`](#m-electron2d-mathf-isnan-system-single) | Tests whether a single-precision value is not a number. |
| [`public static bool IsNaN(double value)`](#m-electron2d-mathf-isnan-system-double) | Tests whether a double-precision value is not a number. |
| [`public static bool IsZeroApprox(float value)`](#m-electron2d-mathf-iszeroapprox-system-single) | Tests whether a single-precision value's magnitude is strictly below [`Mathf.Epsilon`](Mathf.md#f-electron2d-mathf-epsilon). |
| [`public static bool IsZeroApprox(double value)`](#m-electron2d-mathf-iszeroapprox-system-double) | Tests whether a double-precision value's magnitude is strictly below the double-precision epsilon. |
| [`public static float Lerp(float from, float to, float weight)`](#m-electron2d-mathf-lerp-system-single-system-single-system-single) | Linearly interpolates without clamping the weight. |
| [`public static double Lerp(double from, double to, double weight)`](#m-electron2d-mathf-lerp-system-double-system-double-system-double) | Linearly interpolates double-precision values without clamping the weight. |
| [`public static float LerpAngle(float from, float to, float weight)`](#m-electron2d-mathf-lerpangle-system-single-system-single-system-single) | Linearly interpolates between angles along their shortest path. |
| [`public static double LerpAngle(double from, double to, double weight)`](#m-electron2d-mathf-lerpangle-system-double-system-double-system-double) | Linearly interpolates between double-precision angles along their shortest path. |
| [`public static float LinearToDB(float linear)`](#m-electron2d-mathf-lineartodb-system-single) | Converts linear energy to decibels. |
| [`public static double LinearToDB(double linear)`](#m-electron2d-mathf-lineartodb-system-double) | Converts double-precision linear energy to decibels. |
| [`public static float Log(float value)`](#m-electron2d-mathf-log-system-single) | Returns the natural logarithm. |
| [`public static double Log(double value)`](#m-electron2d-mathf-log-system-double) | Returns the double-precision natural logarithm. |
| [`public static int Max(int left, int right)`](#m-electron2d-mathf-max-system-int32-system-int32) | Returns the larger integer. |
| [`public static float Max(float left, float right)`](#m-electron2d-mathf-max-system-single-system-single) | Returns the larger single-precision value. |
| [`public static double Max(double left, double right)`](#m-electron2d-mathf-max-system-double-system-double) | Returns the larger double-precision value. |
| [`public static int Min(int left, int right)`](#m-electron2d-mathf-min-system-int32-system-int32) | Returns the smaller integer. |
| [`public static float Min(float left, float right)`](#m-electron2d-mathf-min-system-single-system-single) | Returns the smaller single-precision value. |
| [`public static double Min(double left, double right)`](#m-electron2d-mathf-min-system-double-system-double) | Returns the smaller double-precision value. |
| [`public static float MoveToward(float from, float to, float delta)`](#m-electron2d-mathf-movetoward-system-single-system-single-system-single) | Moves a value toward a destination by a maximum delta. |
| [`public static double MoveToward(double from, double to, double delta)`](#m-electron2d-mathf-movetoward-system-double-system-double-system-double) | Moves a double-precision value toward a destination by a maximum delta. |
| [`public static int NearestPo2(int value)`](#m-electron2d-mathf-nearestpo2-system-int32) | Returns the smallest representable power of two not less than a positive integer. |
| [`public static int PosMod(int value, int divisor)`](#m-electron2d-mathf-posmod-system-int32-system-int32) | Returns a canonical integer remainder with the divisor's sign. |
| [`public static float PosMod(float value, float divisor)`](#m-electron2d-mathf-posmod-system-single-system-single) | Returns a canonical single-precision remainder with the divisor's sign. |
| [`public static double PosMod(double value, double divisor)`](#m-electron2d-mathf-posmod-system-double-system-double) | Returns a canonical double-precision remainder with the divisor's sign. |
| [`public static float Pow(float value, float power)`](#m-electron2d-mathf-pow-system-single-system-single) | Raises a value to a power. |
| [`public static double Pow(double value, double power)`](#m-electron2d-mathf-pow-system-double-system-double) | Raises a double-precision value to a power. |
| [`public static float RadToDeg(float radians)`](#m-electron2d-mathf-radtodeg-system-single) | Converts radians to degrees. |
| [`public static double RadToDeg(double radians)`](#m-electron2d-mathf-radtodeg-system-double) | Converts double-precision radians to degrees. |
| [`public static float Remap(float value, float inputFrom, float inputTo, float outputFrom, float outputTo)`](#m-electron2d-mathf-remap-system-single-system-single-system-single-system-single-system-single) | Maps a value linearly from one interval to another without clamping. |
| [`public static double Remap(double value, double inputFrom, double inputTo, double outputFrom, double outputTo)`](#m-electron2d-mathf-remap-system-double-system-double-system-double-system-double-system-double) | Maps a double-precision value linearly from one interval to another without clamping. |
| [`public static float RotateToward(float from, float to, float delta)`](#m-electron2d-mathf-rotatetoward-system-single-system-single-system-single) | Rotates an angle toward another angle without overshooting. |
| [`public static double RotateToward(double from, double to, double delta)`](#m-electron2d-mathf-rotatetoward-system-double-system-double-system-double) | Rotates a double-precision angle toward another angle without overshooting. |
| [`public static float Round(float value)`](#m-electron2d-mathf-round-system-single) | Rounds to the nearest integral floating-point value, with midpoint ties to even. |
| [`public static double Round(double value)`](#m-electron2d-mathf-round-system-double) | Rounds a double-precision value to the nearest integral value, with midpoint ties to even. |
| [`public static int RoundToInt(float value)`](#m-electron2d-mathf-roundtoint-system-single) | Rounds to the nearest integer, with midpoint ties to even. |
| [`public static int RoundToInt(double value)`](#m-electron2d-mathf-roundtoint-system-double) | Rounds a double-precision value to the nearest integer, with midpoint ties to even. |
| [`public static int Sign(int value)`](#m-electron2d-mathf-sign-system-int32) | Returns negative one, zero, or positive one according to an integer's sign. |
| [`public static int Sign(float value)`](#m-electron2d-mathf-sign-system-single) | Returns negative one, zero, or positive one according to a single-precision value's sign. |
| [`public static int Sign(double value)`](#m-electron2d-mathf-sign-system-double) | Returns negative one, zero, or positive one according to a double-precision value's sign. |
| [`public static float Sin(float angle)`](#m-electron2d-mathf-sin-system-single) | Returns the sine of an angle in radians. |
| [`public static double Sin(double angle)`](#m-electron2d-mathf-sin-system-double) | Returns the double-precision sine of an angle in radians. |
| [`public static ValueTuple<float, float> SinCos(float angle)`](#m-electron2d-mathf-sincos-system-single) | Returns the sine and cosine of an angle in one operation. |
| [`public static ValueTuple<double, double> SinCos(double angle)`](#m-electron2d-mathf-sincos-system-double) | Returns the double-precision sine and cosine of an angle in one operation. |
| [`public static float Sinh(float value)`](#m-electron2d-mathf-sinh-system-single) | Returns the hyperbolic sine. |
| [`public static double Sinh(double value)`](#m-electron2d-mathf-sinh-system-double) | Returns the double-precision hyperbolic sine. |
| [`public static float SmoothStep(float from, float to, float value)`](#m-electron2d-mathf-smoothstep-system-single-system-single-system-single) | Returns a cubic Hermite step between two edges. |
| [`public static double SmoothStep(double from, double to, double value)`](#m-electron2d-mathf-smoothstep-system-double-system-double-system-double) | Returns a double-precision cubic Hermite step between two edges. |
| [`public static float Sqrt(float value)`](#m-electron2d-mathf-sqrt-system-single) | Returns the principal square root. |
| [`public static double Sqrt(double value)`](#m-electron2d-mathf-sqrt-system-double) | Returns the double-precision principal square root. |
| [`public static int StepDecimals(double step)`](#m-electron2d-mathf-stepdecimals-system-double) | Estimates the position of the first significant fractional decimal digit. |
| [`public static float Snapped(float value, float step)`](#m-electron2d-mathf-snapped-system-single-system-single) | Snaps a value to the nearest multiple of a step. |
| [`public static double Snapped(double value, double step)`](#m-electron2d-mathf-snapped-system-double-system-double) | Snaps a double-precision value to the nearest multiple of a step. |
| [`public static float Tan(float angle)`](#m-electron2d-mathf-tan-system-single) | Returns the tangent of an angle in radians. |
| [`public static double Tan(double angle)`](#m-electron2d-mathf-tan-system-double) | Returns the double-precision tangent of an angle in radians. |
| [`public static float Tanh(float value)`](#m-electron2d-mathf-tanh-system-single) | Returns the hyperbolic tangent. |
| [`public static double Tanh(double value)`](#m-electron2d-mathf-tanh-system-double) | Returns the double-precision hyperbolic tangent. |
| [`public static int Wrap(int value, int min, int max)`](#m-electron2d-mathf-wrap-system-int32-system-int32-system-int32) | Wraps an integer into a half-open interval. |
| [`public static float Wrap(float value, float min, float max)`](#m-electron2d-mathf-wrap-system-single-system-single-system-single) | Wraps a single-precision value into a half-open interval. |
| [`public static double Wrap(double value, double min, double max)`](#m-electron2d-mathf-wrap-system-double-system-double-system-double) | Wraps a double-precision value into a half-open interval. |
| [`public static float PingPong(float value, float length)`](#m-electron2d-mathf-pingpong-system-single-system-single) | Generates a triangle wave between zero and a length. |
| [`public static double PingPong(double value, double length)`](#m-electron2d-mathf-pingpong-system-double-system-double) | Generates a double-precision triangle wave between zero and a length. |

## Constants

| Member | Description |
| --- | --- |
| [`public const float Tau = 6.2831855f`](#f-electron2d-mathf-tau) | The ratio of a circle's circumference to its radius. |
| [`public const float Pi = 3.1415927f`](#f-electron2d-mathf-pi) | The ratio of a circle's circumference to its diameter. |
| [`public const float Inf = Infinityf`](#f-electron2d-mathf-inf) | Positive single-precision infinity. |
| [`public const float NaN = NaNf`](#f-electron2d-mathf-nan) | A single-precision value that is not a number. |
| [`public const float E = 2.7182817f`](#f-electron2d-mathf-e) | The base of the natural logarithm. |
| [`public const float Sqrt2 = 1.4142135f`](#f-electron2d-mathf-sqrt2) | The positive square root of two. |
| [`public const float Epsilon = 1E-06f`](#f-electron2d-mathf-epsilon) | The default absolute single-precision comparison tolerance. |

## Method Descriptions

<a id="m-electron2d-mathf-abs-system-int32"></a>
### `public static int Abs(int value)`

Returns the absolute value of an integer.

**Parameters**

- `value`: The input value.

**Returns:** The nonnegative magnitude.

**Exceptions**

- `OverflowException`: `value` is `Int32.MinValue`.

<a id="m-electron2d-mathf-abs-system-single"></a>
### `public static float Abs(float value)`

Returns the absolute value of a single-precision number.

**Parameters**

- `value`: The input value.

**Returns:** The nonnegative magnitude, or NaN when the input is NaN.

<a id="m-electron2d-mathf-abs-system-double"></a>
### `public static double Abs(double value)`

Returns the absolute value of a double-precision number.

**Parameters**

- `value`: The input value.

**Returns:** The nonnegative magnitude, or NaN when the input is NaN.

<a id="m-electron2d-mathf-acos-system-single"></a>
### `public static float Acos(float value)`

Returns the arc cosine in radians.

**Parameters**

- `value`: A cosine value in the inclusive range negative one through one.

**Returns:** An angle from zero through [`Mathf.Pi`](Mathf.md#f-electron2d-mathf-pi), or NaN for an out-of-range input.

<a id="m-electron2d-mathf-acos-system-double"></a>
### `public static double Acos(double value)`

Returns the double-precision arc cosine in radians.

**Parameters**

- `value`: A cosine value in the inclusive range negative one through one.

**Returns:** An angle from zero through pi, or NaN for an out-of-range input.

<a id="m-electron2d-mathf-acosh-system-single"></a>
### `public static float Acosh(float value)`

Returns the inverse hyperbolic cosine.

**Parameters**

- `value`: A value greater than or equal to one.

**Returns:** The inverse hyperbolic cosine, or NaN for an input below one.

<a id="m-electron2d-mathf-acosh-system-double"></a>
### `public static double Acosh(double value)`

Returns the double-precision inverse hyperbolic cosine.

**Parameters**

- `value`: A value greater than or equal to one.

**Returns:** The inverse hyperbolic cosine, or NaN for an input below one.

<a id="m-electron2d-mathf-angledifference-system-single-system-single"></a>
### `public static float AngleDifference(float from, float to)`

Returns the shortest signed angular difference from one angle to another.

**Parameters**

- `from`: The starting angle in radians.
- `to`: The destination angle in radians.

**Returns:** A difference in the inclusive range negative pi through pi.

**Remarks:** For opposite angles, the result is negative pi when `from` is smaller than `to` and positive pi otherwise.

<a id="m-electron2d-mathf-angledifference-system-double-system-double"></a>
### `public static double AngleDifference(double from, double to)`

Returns the shortest double-precision signed angular difference.

**Parameters**

- `from`: The starting angle in radians.
- `to`: The destination angle in radians.

**Returns:** A difference in the inclusive range negative pi through pi.

<a id="m-electron2d-mathf-asin-system-single"></a>
### `public static float Asin(float value)`

Returns the arc sine in radians.

**Parameters**

- `value`: A sine value in the inclusive range negative one through one.

**Returns:** An angle from negative pi over two through positive pi over two, or NaN for an out-of-range input.

<a id="m-electron2d-mathf-asin-system-double"></a>
### `public static double Asin(double value)`

Returns the double-precision arc sine in radians.

**Parameters**

- `value`: A sine value in the inclusive range negative one through one.

**Returns:** An angle from negative pi over two through positive pi over two, or NaN for an out-of-range input.

<a id="m-electron2d-mathf-asinh-system-single"></a>
### `public static float Asinh(float value)`

Returns the inverse hyperbolic sine.

**Parameters**

- `value`: The input value.

**Returns:** The inverse hyperbolic sine.

<a id="m-electron2d-mathf-asinh-system-double"></a>
### `public static double Asinh(double value)`

Returns the double-precision inverse hyperbolic sine.

**Parameters**

- `value`: The input value.

**Returns:** The inverse hyperbolic sine.

<a id="m-electron2d-mathf-atan-system-single"></a>
### `public static float Atan(float value)`

Returns the arc tangent in radians.

**Parameters**

- `value`: The tangent value.

**Returns:** An angle from negative pi over two through positive pi over two.

<a id="m-electron2d-mathf-atan-system-double"></a>
### `public static double Atan(double value)`

Returns the double-precision arc tangent in radians.

**Parameters**

- `value`: The tangent value.

**Returns:** An angle from negative pi over two through positive pi over two.

<a id="m-electron2d-mathf-atan2-system-single-system-single"></a>
### `public static float Atan2(float y, float x)`

Returns the angle of a Cartesian direction in radians.

**Parameters**

- `y`: The vertical component.
- `x`: The horizontal component.

**Returns:** The quadrant-aware angle from negative pi through positive pi.

<a id="m-electron2d-mathf-atan2-system-double-system-double"></a>
### `public static double Atan2(double y, double x)`

Returns the double-precision angle of a Cartesian direction in radians.

**Parameters**

- `y`: The vertical component.
- `x`: The horizontal component.

**Returns:** The quadrant-aware angle from negative pi through positive pi.

<a id="m-electron2d-mathf-atanh-system-single"></a>
### `public static float Atanh(float value)`

Returns the inverse hyperbolic tangent.

**Parameters**

- `value`: A value in the inclusive range negative one through one.

**Returns:** The inverse hyperbolic tangent; the endpoints produce infinities and out-of-range inputs produce NaN.

<a id="m-electron2d-mathf-atanh-system-double"></a>
### `public static double Atanh(double value)`

Returns the double-precision inverse hyperbolic tangent.

**Parameters**

- `value`: A value in the inclusive range negative one through one.

**Returns:** The inverse hyperbolic tangent; the endpoints produce infinities and out-of-range inputs produce NaN.

<a id="m-electron2d-mathf-ceil-system-single"></a>
### `public static float Ceil(float value)`

Rounds upward toward positive infinity.

**Parameters**

- `value`: The input value.

**Returns:** The smallest integral floating-point value not less than the input.

<a id="m-electron2d-mathf-ceil-system-double"></a>
### `public static double Ceil(double value)`

Rounds a double-precision value upward toward positive infinity.

**Parameters**

- `value`: The input value.

**Returns:** The smallest integral floating-point value not less than the input.

<a id="m-electron2d-mathf-ceiltoint-system-single"></a>
### `public static int CeilToInt(float value)`

Rounds upward and converts to a 32-bit integer.

**Parameters**

- `value`: The input value.

**Returns:** The ceiling converted using unchecked managed numeric conversion semantics.

**Remarks:** NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.

<a id="m-electron2d-mathf-ceiltoint-system-double"></a>
### `public static int CeilToInt(double value)`

Rounds a double-precision value upward and converts to a 32-bit integer.

**Parameters**

- `value`: The input value.

**Returns:** The ceiling converted using unchecked managed numeric conversion semantics.

**Remarks:** NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.

<a id="m-electron2d-mathf-clamp-system-int32-system-int32-system-int32"></a>
### `public static int Clamp(int value, int min, int max)`

Restricts an integer to an inclusive interval.

**Parameters**

- `value`: The value to restrict.
- `min`: The inclusive lower bound.
- `max`: The inclusive upper bound.

**Returns:** The restricted value.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-mathf-clamp-system-single-system-single-system-single"></a>
### `public static float Clamp(float value, float min, float max)`

Restricts a single-precision number to an inclusive interval.

**Parameters**

- `value`: The value to restrict.
- `min`: The inclusive lower bound.
- `max`: The inclusive upper bound.

**Returns:** The restricted value.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-mathf-clamp-system-double-system-double-system-double"></a>
### `public static double Clamp(double value, double min, double max)`

Restricts a double-precision number to an inclusive interval.

**Parameters**

- `value`: The value to restrict.
- `min`: The inclusive lower bound.
- `max`: The inclusive upper bound.

**Returns:** The restricted value.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-mathf-cos-system-single"></a>
### `public static float Cos(float angle)`

Returns the cosine of an angle in radians.

**Parameters**

- `angle`: The angle in radians.

**Returns:** The cosine.

<a id="m-electron2d-mathf-cos-system-double"></a>
### `public static double Cos(double angle)`

Returns the double-precision cosine of an angle in radians.

**Parameters**

- `angle`: The angle in radians.

**Returns:** The cosine.

<a id="m-electron2d-mathf-cosh-system-single"></a>
### `public static float Cosh(float value)`

Returns the hyperbolic cosine.

**Parameters**

- `value`: The input value.

**Returns:** The hyperbolic cosine.

<a id="m-electron2d-mathf-cosh-system-double"></a>
### `public static double Cosh(double value)`

Returns the double-precision hyperbolic cosine.

**Parameters**

- `value`: The input value.

**Returns:** The hyperbolic cosine.

<a id="m-electron2d-mathf-cubicinterpolate-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float CubicInterpolate(float from, float to, float pre, float post, float weight)`

Performs Catmull-Rom cubic interpolation between two values.

**Parameters**

- `from`: The starting value.
- `to`: The destination value.
- `pre`: The control value before `from`.
- `post`: The control value after `to`.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** The interpolated value.

<a id="m-electron2d-mathf-cubicinterpolate-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double CubicInterpolate(double from, double to, double pre, double post, double weight)`

Performs double-precision Catmull-Rom cubic interpolation.

**Parameters**

- `from`: The starting value.
- `to`: The destination value.
- `pre`: The control value before `from`.
- `post`: The control value after `to`.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** The interpolated value.

<a id="m-electron2d-mathf-cubicinterpolateangle-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float CubicInterpolateAngle(float from, float to, float pre, float post, float weight)`

Performs shortest-path Catmull-Rom interpolation between angles.

**Parameters**

- `from`: The starting angle.
- `to`: The destination angle.
- `pre`: The preceding control angle.
- `post`: The following control angle.
- `weight`: The interpolation weight.

**Returns:** The interpolated angle in radians.

<a id="m-electron2d-mathf-cubicinterpolateangle-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double CubicInterpolateAngle(double from, double to, double pre, double post, double weight)`

Performs double-precision shortest-path Catmull-Rom interpolation between angles.

**Parameters**

- `from`: The starting angle.
- `to`: The destination angle.
- `pre`: The preceding control angle.
- `post`: The following control angle.
- `weight`: The interpolation weight.

**Returns:** The interpolated angle in radians.

<a id="m-electron2d-mathf-cubicinterpolateintime-system-single-system-single-system-single-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float CubicInterpolateInTime(float from, float to, float pre, float post, float weight, float toTime, float preTime, float postTime)`

Performs time-aware Barry-Goldman cubic interpolation.

**Parameters**

- `from`: The starting value at time zero.
- `to`: The destination value.
- `pre`: The preceding control value.
- `post`: The following control value.
- `weight`: The interpolation weight.
- `toTime`: The destination time.
- `preTime`: The preceding control time.
- `postTime`: The following control time.

**Returns:** The time-aware interpolated value.

<a id="m-electron2d-mathf-cubicinterpolateintime-system-double-system-double-system-double-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double CubicInterpolateInTime(double from, double to, double pre, double post, double weight, double toTime, double preTime, double postTime)`

Performs double-precision time-aware Barry-Goldman cubic interpolation.

**Parameters**

- `from`: The starting value at time zero.
- `to`: The destination value.
- `pre`: The preceding control value.
- `post`: The following control value.
- `weight`: The interpolation weight.
- `toTime`: The destination time.
- `preTime`: The preceding control time.
- `postTime`: The following control time.

**Returns:** The time-aware interpolated value.

<a id="m-electron2d-mathf-cubicinterpolateangleintime-system-single-system-single-system-single-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float CubicInterpolateAngleInTime(float from, float to, float pre, float post, float weight, float toTime, float preTime, float postTime)`

Performs shortest-path time-aware cubic interpolation between angles.

**Parameters**

- `from`: The starting angle at time zero.
- `to`: The destination angle.
- `pre`: The preceding control angle.
- `post`: The following control angle.
- `weight`: The interpolation weight.
- `toTime`: The destination time.
- `preTime`: The preceding control time.
- `postTime`: The following control time.

**Returns:** The time-aware interpolated angle.

<a id="m-electron2d-mathf-cubicinterpolateangleintime-system-double-system-double-system-double-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double CubicInterpolateAngleInTime(double from, double to, double pre, double post, double weight, double toTime, double preTime, double postTime)`

Performs double-precision shortest-path time-aware cubic interpolation between angles.

**Parameters**

- `from`: The starting angle at time zero.
- `to`: The destination angle.
- `pre`: The preceding control angle.
- `post`: The following control angle.
- `weight`: The interpolation weight.
- `toTime`: The destination time.
- `preTime`: The preceding control time.
- `postTime`: The following control time.

**Returns:** The time-aware interpolated angle.

<a id="m-electron2d-mathf-bezierinterpolate-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float BezierInterpolate(float start, float control1, float control2, float end, float weight)`

Evaluates a one-dimensional cubic Bezier curve.

**Parameters**

- `start`: The start value.
- `control1`: The first control value.
- `control2`: The second control value.
- `end`: The end value.
- `weight`: The curve parameter; values outside zero through one extrapolate.

**Returns:** The curve value.

<a id="m-electron2d-mathf-bezierinterpolate-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double BezierInterpolate(double start, double control1, double control2, double end, double weight)`

Evaluates a double-precision one-dimensional cubic Bezier curve.

**Parameters**

- `start`: The start value.
- `control1`: The first control value.
- `control2`: The second control value.
- `end`: The end value.
- `weight`: The curve parameter; values outside zero through one extrapolate.

**Returns:** The curve value.

<a id="m-electron2d-mathf-bezierderivative-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float BezierDerivative(float start, float control1, float control2, float end, float weight)`

Evaluates the derivative of a one-dimensional cubic Bezier curve.

**Parameters**

- `start`: The start value.
- `control1`: The first control value.
- `control2`: The second control value.
- `end`: The end value.
- `weight`: The curve parameter.

**Returns:** The derivative at the parameter.

<a id="m-electron2d-mathf-bezierderivative-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double BezierDerivative(double start, double control1, double control2, double end, double weight)`

Evaluates the derivative of a double-precision one-dimensional cubic Bezier curve.

**Parameters**

- `start`: The start value.
- `control1`: The first control value.
- `control2`: The second control value.
- `end`: The end value.
- `weight`: The curve parameter.

**Returns:** The derivative at the parameter.

<a id="m-electron2d-mathf-dbtolinear-system-single"></a>
### `public static float DBToLinear(float decibels)`

Converts decibels to linear energy.

**Parameters**

- `decibels`: The decibel value.

**Returns:** The corresponding linear energy.

<a id="m-electron2d-mathf-dbtolinear-system-double"></a>
### `public static double DBToLinear(double decibels)`

Converts double-precision decibels to linear energy.

**Parameters**

- `decibels`: The decibel value.

**Returns:** The corresponding linear energy.

<a id="m-electron2d-mathf-degtorad-system-single"></a>
### `public static float DegToRad(float degrees)`

Converts degrees to radians.

**Parameters**

- `degrees`: The angle in degrees.

**Returns:** The angle in radians.

<a id="m-electron2d-mathf-degtorad-system-double"></a>
### `public static double DegToRad(double degrees)`

Converts double-precision degrees to radians.

**Parameters**

- `degrees`: The angle in degrees.

**Returns:** The angle in radians.

<a id="m-electron2d-mathf-decimalcount-system-double"></a>
### `public static int DecimalCount(double value)`

Returns the number of encoded decimal fractional digits.

**Parameters**

- `value`: A finite value representable by `Decimal`.

**Returns:** The scale retained by conversion to `Decimal`.

**Exceptions**

- `OverflowException`: `value` is non-finite or outside the decimal range.

<a id="m-electron2d-mathf-decimalcount-system-decimal"></a>
### `public static int DecimalCount(decimal value)`

Returns the number of encoded decimal fractional digits.

**Parameters**

- `value`: The decimal value.

**Returns:** The stored decimal scale from zero through twenty-eight.

<a id="m-electron2d-mathf-ease-system-single-system-single"></a>
### `public static float Ease(float value, float curve)`

Applies an exponent-based easing curve to a normalized value.

**Parameters**

- `value`: The input, clamped to zero through one.
- `curve`: Zero for constant zero, one for linear, positive values for in or out easing, and negative values for in-out easing.

**Returns:** The eased value.

<a id="m-electron2d-mathf-ease-system-double-system-double"></a>
### `public static double Ease(double value, double curve)`

Applies a double-precision exponent-based easing curve to a normalized value.

**Parameters**

- `value`: The input, clamped to zero through one.
- `curve`: Zero for constant zero, one for linear, positive values for in or out easing, and negative values for in-out easing.

**Returns:** The eased value.

<a id="m-electron2d-mathf-exp-system-single"></a>
### `public static float Exp(float value)`

Raises the natural-logarithm base to a power.

**Parameters**

- `value`: The exponent.

**Returns:** The natural exponential.

<a id="m-electron2d-mathf-exp-system-double"></a>
### `public static double Exp(double value)`

Raises the natural-logarithm base to a double-precision power.

**Parameters**

- `value`: The exponent.

**Returns:** The natural exponential.

<a id="m-electron2d-mathf-floor-system-single"></a>
### `public static float Floor(float value)`

Rounds downward toward negative infinity.

**Parameters**

- `value`: The input value.

**Returns:** The greatest integral floating-point value not greater than the input.

<a id="m-electron2d-mathf-floor-system-double"></a>
### `public static double Floor(double value)`

Rounds a double-precision value downward toward negative infinity.

**Parameters**

- `value`: The input value.

**Returns:** The greatest integral floating-point value not greater than the input.

<a id="m-electron2d-mathf-floortoint-system-single"></a>
### `public static int FloorToInt(float value)`

Rounds downward and converts to a 32-bit integer.

**Parameters**

- `value`: The input value.

**Returns:** The floor converted using unchecked managed numeric conversion semantics.

**Remarks:** NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.

<a id="m-electron2d-mathf-floortoint-system-double"></a>
### `public static int FloorToInt(double value)`

Rounds a double-precision value downward and converts to a 32-bit integer.

**Parameters**

- `value`: The input value.

**Returns:** The floor converted using unchecked managed numeric conversion semantics.

**Remarks:** NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.

<a id="m-electron2d-mathf-inverselerp-system-single-system-single-system-single"></a>
### `public static float InverseLerp(float from, float to, float value)`

Returns the unbounded interpolation weight of a value within an interval.

**Parameters**

- `from`: The start of the interval.
- `to`: The end of the interval.
- `value`: The value to locate.

**Returns:** `(value - from) / (to - from)`; equal bounds follow IEEE 754 division behavior.

<a id="m-electron2d-mathf-inverselerp-system-double-system-double-system-double"></a>
### `public static double InverseLerp(double from, double to, double value)`

Returns the double-precision unbounded interpolation weight of a value within an interval.

**Parameters**

- `from`: The start of the interval.
- `to`: The end of the interval.
- `value`: The value to locate.

**Returns:** `(value - from) / (to - from)`; equal bounds follow IEEE 754 division behavior.

<a id="m-electron2d-mathf-isequalapprox-system-single-system-single"></a>
### `public static bool IsEqualApprox(float left, float right)`

Tests two single-precision values for scale-aware approximate equality.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** `true` for exact equality or a difference below the larger of [`Mathf.Epsilon`](Mathf.md#f-electron2d-mathf-epsilon) and `Epsilon * abs(left)`.

<a id="m-electron2d-mathf-isequalapprox-system-double-system-double"></a>
### `public static bool IsEqualApprox(double left, double right)`

Tests two double-precision values for scale-aware approximate equality.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** `true` for exact equality or a difference below the double-precision relative tolerance.

<a id="m-electron2d-mathf-isequalapprox-system-single-system-single-system-single"></a>
### `public static bool IsEqualApprox(float left, float right, float tolerance)`

Tests two single-precision values using a caller-supplied absolute tolerance.

**Parameters**

- `left`: The first value.
- `right`: The second value.
- `tolerance`: The strict upper bound for their absolute difference.

**Returns:** `true` for exact equality or a difference strictly below `tolerance`.

**Remarks:** A negative or NaN tolerance only permits exact equality.

<a id="m-electron2d-mathf-isequalapprox-system-double-system-double-system-double"></a>
### `public static bool IsEqualApprox(double left, double right, double tolerance)`

Tests two double-precision values using a caller-supplied absolute tolerance.

**Parameters**

- `left`: The first value.
- `right`: The second value.
- `tolerance`: The strict upper bound for their absolute difference.

**Returns:** `true` for exact equality or a difference strictly below `tolerance`.

**Remarks:** A negative or NaN tolerance only permits exact equality.

<a id="m-electron2d-mathf-isfinite-system-single"></a>
### `public static bool IsFinite(float value)`

Tests whether a single-precision value is neither NaN nor infinity.

**Parameters**

- `value`: The value to test.

**Returns:** `true` for a finite value.

<a id="m-electron2d-mathf-isfinite-system-double"></a>
### `public static bool IsFinite(double value)`

Tests whether a double-precision value is neither NaN nor infinity.

**Parameters**

- `value`: The value to test.

**Returns:** `true` for a finite value.

<a id="m-electron2d-mathf-isinf-system-single"></a>
### `public static bool IsInf(float value)`

Tests whether a single-precision value is positive or negative infinity.

**Parameters**

- `value`: The value to test.

**Returns:** `true` for either infinity.

<a id="m-electron2d-mathf-isinf-system-double"></a>
### `public static bool IsInf(double value)`

Tests whether a double-precision value is positive or negative infinity.

**Parameters**

- `value`: The value to test.

**Returns:** `true` for either infinity.

<a id="m-electron2d-mathf-isnan-system-single"></a>
### `public static bool IsNaN(float value)`

Tests whether a single-precision value is not a number.

**Parameters**

- `value`: The value to test.

**Returns:** `true` for NaN.

<a id="m-electron2d-mathf-isnan-system-double"></a>
### `public static bool IsNaN(double value)`

Tests whether a double-precision value is not a number.

**Parameters**

- `value`: The value to test.

**Returns:** `true` for NaN.

<a id="m-electron2d-mathf-iszeroapprox-system-single"></a>
### `public static bool IsZeroApprox(float value)`

Tests whether a single-precision value's magnitude is strictly below [`Mathf.Epsilon`](Mathf.md#f-electron2d-mathf-epsilon).

**Parameters**

- `value`: The value to test.

**Returns:** `true` when the value is approximately zero.

<a id="m-electron2d-mathf-iszeroapprox-system-double"></a>
### `public static bool IsZeroApprox(double value)`

Tests whether a double-precision value's magnitude is strictly below the double-precision epsilon.

**Parameters**

- `value`: The value to test.

**Returns:** `true` when the value is approximately zero.

<a id="m-electron2d-mathf-lerp-system-single-system-single-system-single"></a>
### `public static float Lerp(float from, float to, float weight)`

Linearly interpolates without clamping the weight.

**Parameters**

- `from`: The starting value.
- `to`: The destination value.
- `weight`: The interpolation weight.

**Returns:** The interpolated or extrapolated value.

<a id="m-electron2d-mathf-lerp-system-double-system-double-system-double"></a>
### `public static double Lerp(double from, double to, double weight)`

Linearly interpolates double-precision values without clamping the weight.

**Parameters**

- `from`: The starting value.
- `to`: The destination value.
- `weight`: The interpolation weight.

**Returns:** The interpolated or extrapolated value.

<a id="m-electron2d-mathf-lerpangle-system-single-system-single-system-single"></a>
### `public static float LerpAngle(float from, float to, float weight)`

Linearly interpolates between angles along their shortest path.

**Parameters**

- `from`: The starting angle in radians.
- `to`: The destination angle in radians.
- `weight`: The unbounded interpolation weight.

**Returns:** The interpolated angle.

<a id="m-electron2d-mathf-lerpangle-system-double-system-double-system-double"></a>
### `public static double LerpAngle(double from, double to, double weight)`

Linearly interpolates between double-precision angles along their shortest path.

**Parameters**

- `from`: The starting angle in radians.
- `to`: The destination angle in radians.
- `weight`: The unbounded interpolation weight.

**Returns:** The interpolated angle.

<a id="m-electron2d-mathf-lineartodb-system-single"></a>
### `public static float LinearToDB(float linear)`

Converts linear energy to decibels.

**Parameters**

- `linear`: The linear energy.

**Returns:** The decibel value; zero produces negative infinity and negative input produces NaN.

<a id="m-electron2d-mathf-lineartodb-system-double"></a>
### `public static double LinearToDB(double linear)`

Converts double-precision linear energy to decibels.

**Parameters**

- `linear`: The linear energy.

**Returns:** The decibel value; zero produces negative infinity and negative input produces NaN.

<a id="m-electron2d-mathf-log-system-single"></a>
### `public static float Log(float value)`

Returns the natural logarithm.

**Parameters**

- `value`: The input; zero produces negative infinity and negative input produces NaN.

**Returns:** The natural logarithm.

<a id="m-electron2d-mathf-log-system-double"></a>
### `public static double Log(double value)`

Returns the double-precision natural logarithm.

**Parameters**

- `value`: The input; zero produces negative infinity and negative input produces NaN.

**Returns:** The natural logarithm.

<a id="m-electron2d-mathf-max-system-int32-system-int32"></a>
### `public static int Max(int left, int right)`

Returns the larger integer.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** The larger value.

<a id="m-electron2d-mathf-max-system-single-system-single"></a>
### `public static float Max(float left, float right)`

Returns the larger single-precision value.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** The larger value, or NaN if either value is NaN.

<a id="m-electron2d-mathf-max-system-double-system-double"></a>
### `public static double Max(double left, double right)`

Returns the larger double-precision value.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** The larger value, or NaN if either value is NaN.

<a id="m-electron2d-mathf-min-system-int32-system-int32"></a>
### `public static int Min(int left, int right)`

Returns the smaller integer.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** The smaller value.

<a id="m-electron2d-mathf-min-system-single-system-single"></a>
### `public static float Min(float left, float right)`

Returns the smaller single-precision value.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** The smaller value, or NaN if either value is NaN.

<a id="m-electron2d-mathf-min-system-double-system-double"></a>
### `public static double Min(double left, double right)`

Returns the smaller double-precision value.

**Parameters**

- `left`: The first value.
- `right`: The second value.

**Returns:** The smaller value, or NaN if either value is NaN.

<a id="m-electron2d-mathf-movetoward-system-single-system-single-system-single"></a>
### `public static float MoveToward(float from, float to, float delta)`

Moves a value toward a destination by a maximum delta.

**Parameters**

- `from`: The starting value.
- `to`: The destination value.
- `delta`: The step; a negative step moves away.

**Returns:** The destination when within the step, otherwise the stepped value.

**Exceptions**

- `ArithmeticException`: `from` or `to` makes the signed difference NaN.

<a id="m-electron2d-mathf-movetoward-system-double-system-double-system-double"></a>
### `public static double MoveToward(double from, double to, double delta)`

Moves a double-precision value toward a destination by a maximum delta.

**Parameters**

- `from`: The starting value.
- `to`: The destination value.
- `delta`: The step; a negative step moves away.

**Returns:** The destination when within the step, otherwise the stepped value.

**Exceptions**

- `ArithmeticException`: `from` or `to` makes the signed difference NaN.

<a id="m-electron2d-mathf-nearestpo2-system-int32"></a>
### `public static int NearestPo2(int value)`

Returns the smallest representable power of two not less than a positive integer.

**Parameters**

- `value`: The input value.

**Returns:** Zero for ordinary nonpositive values, the input for a power of two, or the next power of two; overflow wraps exactly as 32-bit arithmetic.

<a id="m-electron2d-mathf-posmod-system-int32-system-int32"></a>
### `public static int PosMod(int value, int divisor)`

Returns a canonical integer remainder with the divisor's sign.

**Parameters**

- `value`: The dividend.
- `divisor`: The divisor.

**Returns:** A remainder between zero and the divisor in the divisor's direction.

**Exceptions**

- `DivideByZeroException`: `divisor` is zero.
- `OverflowException`: `value` is `Int32.MinValue` and `divisor` is negative one.

<a id="m-electron2d-mathf-posmod-system-single-system-single"></a>
### `public static float PosMod(float value, float divisor)`

Returns a canonical single-precision remainder with the divisor's sign.

**Parameters**

- `value`: The dividend.
- `divisor`: The divisor.

**Returns:** A remainder in the divisor's direction; a zero divisor produces NaN.

<a id="m-electron2d-mathf-posmod-system-double-system-double"></a>
### `public static double PosMod(double value, double divisor)`

Returns a canonical double-precision remainder with the divisor's sign.

**Parameters**

- `value`: The dividend.
- `divisor`: The divisor.

**Returns:** A remainder in the divisor's direction; a zero divisor produces NaN.

<a id="m-electron2d-mathf-pow-system-single-system-single"></a>
### `public static float Pow(float value, float power)`

Raises a value to a power.

**Parameters**

- `value`: The base.
- `power`: The exponent.

**Returns:** The power result under IEEE 754 rules.

<a id="m-electron2d-mathf-pow-system-double-system-double"></a>
### `public static double Pow(double value, double power)`

Raises a double-precision value to a power.

**Parameters**

- `value`: The base.
- `power`: The exponent.

**Returns:** The power result under IEEE 754 rules.

<a id="m-electron2d-mathf-radtodeg-system-single"></a>
### `public static float RadToDeg(float radians)`

Converts radians to degrees.

**Parameters**

- `radians`: The angle in radians.

**Returns:** The angle in degrees.

<a id="m-electron2d-mathf-radtodeg-system-double"></a>
### `public static double RadToDeg(double radians)`

Converts double-precision radians to degrees.

**Parameters**

- `radians`: The angle in radians.

**Returns:** The angle in degrees.

<a id="m-electron2d-mathf-remap-system-single-system-single-system-single-system-single-system-single"></a>
### `public static float Remap(float value, float inputFrom, float inputTo, float outputFrom, float outputTo)`

Maps a value linearly from one interval to another without clamping.

**Parameters**

- `value`: The input value.
- `inputFrom`: The input interval start.
- `inputTo`: The input interval end.
- `outputFrom`: The output interval start.
- `outputTo`: The output interval end.

**Returns:** The mapped value; equal input bounds follow IEEE 754 division behavior.

<a id="m-electron2d-mathf-remap-system-double-system-double-system-double-system-double-system-double"></a>
### `public static double Remap(double value, double inputFrom, double inputTo, double outputFrom, double outputTo)`

Maps a double-precision value linearly from one interval to another without clamping.

**Parameters**

- `value`: The input value.
- `inputFrom`: The input interval start.
- `inputTo`: The input interval end.
- `outputFrom`: The output interval start.
- `outputTo`: The output interval end.

**Returns:** The mapped value; equal input bounds follow IEEE 754 division behavior.

<a id="m-electron2d-mathf-rotatetoward-system-single-system-single-system-single"></a>
### `public static float RotateToward(float from, float to, float delta)`

Rotates an angle toward another angle without overshooting.

**Parameters**

- `from`: The starting angle in radians.
- `to`: The destination angle in radians.
- `delta`: The step; a negative value rotates toward the opposite angle.

**Returns:** The stepped angle.

<a id="m-electron2d-mathf-rotatetoward-system-double-system-double-system-double"></a>
### `public static double RotateToward(double from, double to, double delta)`

Rotates a double-precision angle toward another angle without overshooting.

**Parameters**

- `from`: The starting angle in radians.
- `to`: The destination angle in radians.
- `delta`: The step; a negative value rotates toward the opposite angle.

**Returns:** The stepped angle.

<a id="m-electron2d-mathf-round-system-single"></a>
### `public static float Round(float value)`

Rounds to the nearest integral floating-point value, with midpoint ties to even.

**Parameters**

- `value`: The input value.

**Returns:** The rounded value.

<a id="m-electron2d-mathf-round-system-double"></a>
### `public static double Round(double value)`

Rounds a double-precision value to the nearest integral value, with midpoint ties to even.

**Parameters**

- `value`: The input value.

**Returns:** The rounded value.

<a id="m-electron2d-mathf-roundtoint-system-single"></a>
### `public static int RoundToInt(float value)`

Rounds to the nearest integer, with midpoint ties to even.

**Parameters**

- `value`: The input value.

**Returns:** The rounded value converted using unchecked managed numeric conversion semantics.

**Remarks:** NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.

<a id="m-electron2d-mathf-roundtoint-system-double"></a>
### `public static int RoundToInt(double value)`

Rounds a double-precision value to the nearest integer, with midpoint ties to even.

**Parameters**

- `value`: The input value.

**Returns:** The rounded value converted using unchecked managed numeric conversion semantics.

**Remarks:** NaN, infinity, and an out-of-range result convert to the runtime-defined unchecked integer sentinel.

<a id="m-electron2d-mathf-sign-system-int32"></a>
### `public static int Sign(int value)`

Returns negative one, zero, or positive one according to an integer's sign.

**Parameters**

- `value`: The input value.

**Returns:** The sign.

<a id="m-electron2d-mathf-sign-system-single"></a>
### `public static int Sign(float value)`

Returns negative one, zero, or positive one according to a single-precision value's sign.

**Parameters**

- `value`: The input value.

**Returns:** The sign.

**Exceptions**

- `ArithmeticException`: `value` is NaN.

<a id="m-electron2d-mathf-sign-system-double"></a>
### `public static int Sign(double value)`

Returns negative one, zero, or positive one according to a double-precision value's sign.

**Parameters**

- `value`: The input value.

**Returns:** The sign.

**Exceptions**

- `ArithmeticException`: `value` is NaN.

<a id="m-electron2d-mathf-sin-system-single"></a>
### `public static float Sin(float angle)`

Returns the sine of an angle in radians.

**Parameters**

- `angle`: The angle in radians.

**Returns:** The sine.

<a id="m-electron2d-mathf-sin-system-double"></a>
### `public static double Sin(double angle)`

Returns the double-precision sine of an angle in radians.

**Parameters**

- `angle`: The angle in radians.

**Returns:** The sine.

<a id="m-electron2d-mathf-sincos-system-single"></a>
### `public static ValueTuple<float, float> SinCos(float angle)`

Returns the sine and cosine of an angle in one operation.

**Parameters**

- `angle`: The angle in radians.

**Returns:** A tuple containing sine followed by cosine.

<a id="m-electron2d-mathf-sincos-system-double"></a>
### `public static ValueTuple<double, double> SinCos(double angle)`

Returns the double-precision sine and cosine of an angle in one operation.

**Parameters**

- `angle`: The angle in radians.

**Returns:** A tuple containing sine followed by cosine.

<a id="m-electron2d-mathf-sinh-system-single"></a>
### `public static float Sinh(float value)`

Returns the hyperbolic sine.

**Parameters**

- `value`: The input value.

**Returns:** The hyperbolic sine.

<a id="m-electron2d-mathf-sinh-system-double"></a>
### `public static double Sinh(double value)`

Returns the double-precision hyperbolic sine.

**Parameters**

- `value`: The input value.

**Returns:** The hyperbolic sine.

<a id="m-electron2d-mathf-smoothstep-system-single-system-single-system-single"></a>
### `public static float SmoothStep(float from, float to, float value)`

Returns a cubic Hermite step between two edges.

**Parameters**

- `from`: The first edge.
- `to`: The second edge.
- `value`: The value to normalize between the edges.

**Returns:** A value from zero through one, or `from` when the edges are approximately equal.

<a id="m-electron2d-mathf-smoothstep-system-double-system-double-system-double"></a>
### `public static double SmoothStep(double from, double to, double value)`

Returns a double-precision cubic Hermite step between two edges.

**Parameters**

- `from`: The first edge.
- `to`: The second edge.
- `value`: The value to normalize between the edges.

**Returns:** A value from zero through one, or `from` when the edges are approximately equal.

<a id="m-electron2d-mathf-sqrt-system-single"></a>
### `public static float Sqrt(float value)`

Returns the principal square root.

**Parameters**

- `value`: A nonnegative value.

**Returns:** The square root, or NaN for a negative input.

<a id="m-electron2d-mathf-sqrt-system-double"></a>
### `public static double Sqrt(double value)`

Returns the double-precision principal square root.

**Parameters**

- `value`: A nonnegative value.

**Returns:** The square root, or NaN for a negative input.

<a id="m-electron2d-mathf-stepdecimals-system-double"></a>
### `public static int StepDecimals(double step)`

Estimates the position of the first significant fractional decimal digit.

**Parameters**

- `step`: The input step.

**Returns:** An index from zero through eight; integer and very small fractional inputs return zero.

<a id="m-electron2d-mathf-snapped-system-single-system-single"></a>
### `public static float Snapped(float value, float step)`

Snaps a value to the nearest multiple of a step.

**Parameters**

- `value`: The value to snap.
- `step`: The grid step; zero returns `value` unchanged.

**Returns:** `floor(value / step + 0.5) * step`.

<a id="m-electron2d-mathf-snapped-system-double-system-double"></a>
### `public static double Snapped(double value, double step)`

Snaps a double-precision value to the nearest multiple of a step.

**Parameters**

- `value`: The value to snap.
- `step`: The grid step; zero returns `value` unchanged.

**Returns:** `floor(value / step + 0.5) * step`.

<a id="m-electron2d-mathf-tan-system-single"></a>
### `public static float Tan(float angle)`

Returns the tangent of an angle in radians.

**Parameters**

- `angle`: The angle in radians.

**Returns:** The tangent.

<a id="m-electron2d-mathf-tan-system-double"></a>
### `public static double Tan(double angle)`

Returns the double-precision tangent of an angle in radians.

**Parameters**

- `angle`: The angle in radians.

**Returns:** The tangent.

<a id="m-electron2d-mathf-tanh-system-single"></a>
### `public static float Tanh(float value)`

Returns the hyperbolic tangent.

**Parameters**

- `value`: The input value.

**Returns:** The hyperbolic tangent.

<a id="m-electron2d-mathf-tanh-system-double"></a>
### `public static double Tanh(double value)`

Returns the double-precision hyperbolic tangent.

**Parameters**

- `value`: The input value.

**Returns:** The hyperbolic tangent.

<a id="m-electron2d-mathf-wrap-system-int32-system-int32-system-int32"></a>
### `public static int Wrap(int value, int min, int max)`

Wraps an integer into a half-open interval.

**Parameters**

- `value`: The value to wrap.
- `min`: The inclusive lower bound.
- `max`: The exclusive upper bound.

**Returns:** The wrapped value, or `min` when both bounds are equal.

**Exceptions**

- `OverflowException`: Unchecked bound arithmetic produces the exceptional `Int32.MinValue` remainder by negative one.

**Remarks:** Arithmetic uses unchecked 32-bit semantics; extreme bounds may overflow the interval width.

<a id="m-electron2d-mathf-wrap-system-single-system-single-system-single"></a>
### `public static float Wrap(float value, float min, float max)`

Wraps a single-precision value into a half-open interval.

**Parameters**

- `value`: The value to wrap.
- `min`: The inclusive lower bound.
- `max`: The exclusive upper bound.

**Returns:** The wrapped value, or `min` when the interval width is approximately zero.

<a id="m-electron2d-mathf-wrap-system-double-system-double-system-double"></a>
### `public static double Wrap(double value, double min, double max)`

Wraps a double-precision value into a half-open interval.

**Parameters**

- `value`: The value to wrap.
- `min`: The inclusive lower bound.
- `max`: The exclusive upper bound.

**Returns:** The wrapped value, or `min` when the interval width is approximately zero.

<a id="m-electron2d-mathf-pingpong-system-single-system-single"></a>
### `public static float PingPong(float value, float length)`

Generates a triangle wave between zero and a length.

**Parameters**

- `value`: The wave position.
- `length`: The endpoint magnitude; negative values behave like their positive magnitude.

**Returns:** The reflected periodic value, or zero when `length` is zero.

<a id="m-electron2d-mathf-pingpong-system-double-system-double"></a>
### `public static double PingPong(double value, double length)`

Generates a double-precision triangle wave between zero and a length.

**Parameters**

- `value`: The wave position.
- `length`: The endpoint magnitude; negative values behave like their positive magnitude.

**Returns:** The reflected periodic value, or zero when `length` is zero.

## Constant Descriptions

<a id="f-electron2d-mathf-tau"></a>
### `public const float Tau = 6.2831855f`

The ratio of a circle's circumference to its radius.

<a id="f-electron2d-mathf-pi"></a>
### `public const float Pi = 3.1415927f`

The ratio of a circle's circumference to its diameter.

<a id="f-electron2d-mathf-inf"></a>
### `public const float Inf = Infinityf`

Positive single-precision infinity.

<a id="f-electron2d-mathf-nan"></a>
### `public const float NaN = NaNf`

A single-precision value that is not a number.

**Remarks:** This value is unequal to every value, including itself.

<a id="f-electron2d-mathf-e"></a>
### `public const float E = 2.7182817f`

The base of the natural logarithm.

<a id="f-electron2d-mathf-sqrt2"></a>
### `public const float Sqrt2 = 1.4142135f`

The positive square root of two.

<a id="f-electron2d-mathf-epsilon"></a>
### `public const float Epsilon = 1E-06f`

The default absolute single-precision comparison tolerance.

## Numeric invariants and error behavior

- Floating-point operations retain normal IEEE 754 behavior. Domain errors generally produce NaN; zero logarithms produce negative infinity; floating modulus by zero produces NaN.
- Calculated approximate equality first accepts exact equality so equal infinities pass, then compares a strict absolute difference against `max(epsilon * abs(left), epsilon)`. NaN never compares approximately equal. Explicit negative or NaN tolerances allow only exact equality.
- Float zero approximation is strict `abs(value) < 1e-6f`; double zero approximation is strict `abs(value) < 1e-14`. A value exactly on the threshold is false.
- `Abs(int.MinValue)` throws `OverflowException`. Reversed `Clamp` bounds throw `ArgumentException`.
- `Sign(float/double)` throws `ArithmeticException` for NaN. `MoveToward` exposes the same exception when its signed difference is NaN.
- `PosMod(int)` throws `DivideByZeroException` for zero and `OverflowException` for `int.MinValue % -1`. Floating overloads do not throw for a zero divisor.
- `DecimalCount(double)` throws `OverflowException` for NaN, infinity, or values outside the decimal range. The decimal overload reports retained scale, including trailing zeroes.
- Integer-returning rounding overloads deliberately use unchecked managed conversion for NaN, infinity, and out-of-range values; callers must validate when those inputs are possible.
- `NearestPo2` and integer `Wrap` deliberately retain unchecked 32-bit arithmetic. Power-of-two overflow wraps; the extreme `int.MinValue % -1` wrap case can throw `OverflowException`.
- `StepDecimals` is the audited threshold estimator, not a general decimal parser: it returns zero through eight and also returns zero for integral or smaller-than-table fractions.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition. Every call is independent and safe across threads. Methods do not retain references or mutate process state.

Normal nonthrowing calls allocate zero managed memory after JIT warmup; thrown managed exceptions allocate normally. Tuple results are value tuples and decimal inspection uses stack storage. Runtime transcendental accuracy and unchecked floating-to-integer sentinel values remain properties of the target .NET runtime; the current executable verification covers Linux/.NET 8 only.

## Dependencies and interactions

Implementation uses only .NET scalar mathematics and decimal bit access. [`Vector2`](Vector2.md), [`Vector4`](Vector4.md), their integer counterparts, [`Color`](Color.md), [`Rect`](Rect.md), [`Transform`](Transform.md), and [`Entity`](Entity.md) route matching scalar operations through `Mathf`. Internal color math continues to call the BCL directly only for cube root because no audited `Mathf` member exists for it.

The migration intentionally corrected the former `1e-5f` component-comparison tolerance to the canonical `Mathf.Epsilon` contract. Electron2D has no released compatibility baseline, so known incorrect pre-release behavior is corrected rather than preserved behind a second tolerance or compatibility path.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` reflects the exact seven-constant/127-overload surface and exercises every method family across float and double paths. Tests cover ordinary results, extrapolation, opposite angles, coincident times, NaN/infinity, strict epsilon boundaries, reversed clamps, integer zero division/overflow, unchecked rounding conversion, decimal failures, wrapping, power-of-two overflow, downstream geometry migration, and warmed zero allocation.

The implementation was audited against the official 4.7.2 stable typed C# scalar-math sources and their inheritance-independent global math contract. There is no generic-math facade, random-number generation, vector overload, configurable epsilon, fast-approximation mode, SIMD API, or cube-root member. Those are absent rather than speculative wrappers.

## Decisions

- [0004: 2D scene-oriented API in one assembly](../decisions/product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
