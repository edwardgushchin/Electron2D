# Vector2

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Vector2.cs`](../../src/Core/Math/Vector2.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Vector2`

> Represents a two-component floating-point vector for coordinates, sizes, directions, and numeric pairs.

## Description

Represents a two-component floating-point vector for coordinates, sizes, directions, and numeric pairs.

`Vector2` is the engine-owned mutable two-component single-precision value used for 2D positions, sizes, directions, velocities, axes, offsets, and numeric pairs. Its sequential layout is exactly two `float` fields in X/Y order and is verified as 8 bytes on the current .NET 8 host. It owns no identity, reference, callback, handle, or lifecycle.

The value uses single-precision components and screen-space direction constants where positive Y points down.
Ordinary arithmetic preserves IEEE 754 NaN and infinity values. The zero-initialized value is [`Vector2.Zero`](Vector2.md#p-electron2d-vector2-zero).
Numeric operations do not allocate managed memory; string formatting allocates a string.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var velocity = new Vector2(120f, -40f);
var nextPosition = position + velocity * delta;
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Vector2(float x, float y)`](#m-electron2d-vector2-ctor-system-single-system-single) | Initializes a vector from horizontal and vertical components. |
| [`public Vector2(Vector2I value)`](#m-electron2d-vector2-ctor-electron2d-vector2i) | Initializes a floating-point vector from an integer vector. |

## Properties

| Member | Description |
| --- | --- |
| [`public static Vector2 Zero { get; }`](#p-electron2d-vector2-zero) | Gets the zero vector. |
| [`public static Vector2 One { get; }`](#p-electron2d-vector2-one) | Gets the vector whose components are both one. |
| [`public static Vector2 Inf { get; }`](#p-electron2d-vector2-inf) | Gets the vector whose components are both positive infinity. |
| [`public static Vector2 Up { get; }`](#p-electron2d-vector2-up) | Gets the upward screen-space unit vector. |
| [`public static Vector2 Down { get; }`](#p-electron2d-vector2-down) | Gets the downward screen-space unit vector. |
| [`public static Vector2 Right { get; }`](#p-electron2d-vector2-right) | Gets the rightward unit vector. |
| [`public static Vector2 Left { get; }`](#p-electron2d-vector2-left) | Gets the leftward unit vector. |
| [`public float this[int index] { get; set; }`](#p-electron2d-vector2-item-system-int32) | Gets or sets a component by axis index. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Deconstruct(out float x, out float y)`](#m-electron2d-vector2-deconstruct-system-single-byref-system-single-byref) | Deconstructs the vector into its two components. |
| [`public Vector2 Abs()`](#m-electron2d-vector2-abs) | Returns a vector containing the absolute value of each component. |
| [`public float Angle()`](#m-electron2d-vector2-angle) | Returns the angle from the positive X axis to this vector. |
| [`public float AngleTo(Vector2 to)`](#m-electron2d-vector2-angleto-electron2d-vector2) | Returns the signed angle from this vector to another vector. |
| [`public float AngleToPoint(Vector2 to)`](#m-electron2d-vector2-angletopoint-electron2d-vector2) | Returns the angle of the line from this point to another point. |
| [`public float Aspect()`](#m-electron2d-vector2-aspect) | Returns the ratio of the horizontal component to the vertical component. |
| [`public Vector2 BezierDerivative(Vector2 control1, Vector2 control2, Vector2 end, float t)`](#m-electron2d-vector2-bezierderivative-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single) | Returns the derivative at a parameter on a cubic Bézier curve. |
| [`public Vector2 BezierInterpolate(Vector2 control1, Vector2 control2, Vector2 end, float t)`](#m-electron2d-vector2-bezierinterpolate-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single) | Returns a point on a cubic Bézier curve. |
| [`public Vector2 Bounce(Vector2 normal)`](#m-electron2d-vector2-bounce-electron2d-vector2) | Returns this vector bounced from a line with a given unit normal. |
| [`public Vector2 Ceil()`](#m-electron2d-vector2-ceil) | Rounds both components upward toward positive infinity. |
| [`public Vector2 Clamp(Vector2 min, Vector2 max)`](#m-electron2d-vector2-clamp-electron2d-vector2-electron2d-vector2) | Clamps each component between corresponding vector bounds. |
| [`public Vector2 Clamp(float min, float max)`](#m-electron2d-vector2-clamp-system-single-system-single) | Clamps both components between scalar bounds. |
| [`public float Cross(Vector2 with)`](#m-electron2d-vector2-cross-electron2d-vector2) | Returns the scalar two-dimensional cross product. |
| [`public Vector2 CubicInterpolate(Vector2 b, Vector2 preA, Vector2 postB, float weight)`](#m-electron2d-vector2-cubicinterpolate-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single) | Performs Catmull-Rom cubic interpolation between this vector and another. |
| [`public Vector2 CubicInterpolateInTime(Vector2 b, Vector2 preA, Vector2 postB, float weight, float bTime, float preATime, float postBTime)`](#m-electron2d-vector2-cubicinterpolateintime-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single-system-single-system-single-system-single) | Performs time-aware Barry-Goldman cubic interpolation. |
| [`public Vector2 DirectionTo(Vector2 to)`](#m-electron2d-vector2-directionto-electron2d-vector2) | Returns the normalized direction from this point to another point. |
| [`public float DistanceSquaredTo(Vector2 to)`](#m-electron2d-vector2-distancesquaredto-electron2d-vector2) | Returns the squared Euclidean distance to another point. |
| [`public float DistanceTo(Vector2 to)`](#m-electron2d-vector2-distanceto-electron2d-vector2) | Returns the Euclidean distance to another point. |
| [`public float Dot(Vector2 with)`](#m-electron2d-vector2-dot-electron2d-vector2) | Returns the dot product with another vector. |
| [`public Vector2 Floor()`](#m-electron2d-vector2-floor) | Rounds both components downward toward negative infinity. |
| [`public static Vector2 FromAngle(float angle)`](#m-electron2d-vector2-fromangle-system-single) | Creates an approximately unit vector from an angle. |
| [`public Vector2 Inverse()`](#m-electron2d-vector2-inverse) | Returns the componentwise reciprocal. |
| [`public bool IsEqualApprox(Vector2 other)`](#m-electron2d-vector2-isequalapprox-electron2d-vector2) | Tests both components for scale-aware approximate equality. |
| [`public bool IsFinite()`](#m-electron2d-vector2-isfinite) | Tests whether both components are finite. |
| [`public bool IsNormalized()`](#m-electron2d-vector2-isnormalized) | Tests whether the squared length is approximately one. |
| [`public bool IsZeroApprox()`](#m-electron2d-vector2-iszeroapprox) | Tests whether both components are approximately zero. |
| [`public float Length()`](#m-electron2d-vector2-length) | Returns the Euclidean length. |
| [`public float LengthSquared()`](#m-electron2d-vector2-lengthsquared) | Returns the squared Euclidean length. |
| [`public Vector2 Lerp(Vector2 to, float weight)`](#m-electron2d-vector2-lerp-electron2d-vector2-system-single) | Linearly interpolates or extrapolates toward another vector. |
| [`public Vector2 LimitLength(float length = 1f)`](#m-electron2d-vector2-limitlength-system-single) | Limits the vector to a maximum length. |
| [`public Vector2 Max(Vector2 with)`](#m-electron2d-vector2-max-electron2d-vector2) | Returns the componentwise maximum with another vector. |
| [`public Vector2 Max(float with)`](#m-electron2d-vector2-max-system-single) | Returns the componentwise maximum with a scalar. |
| [`public Vector2.Axis MaxAxisIndex()`](#m-electron2d-vector2-maxaxisindex) | Returns the axis containing the greatest component. |
| [`public Vector2 Min(Vector2 with)`](#m-electron2d-vector2-min-electron2d-vector2) | Returns the componentwise minimum with another vector. |
| [`public Vector2 Min(float with)`](#m-electron2d-vector2-min-system-single) | Returns the componentwise minimum with a scalar. |
| [`public Vector2.Axis MinAxisIndex()`](#m-electron2d-vector2-minaxisindex) | Returns the axis containing the least component. |
| [`public Vector2 MoveToward(Vector2 to, float delta)`](#m-electron2d-vector2-movetoward-electron2d-vector2-system-single) | Moves toward another vector by a fixed distance without passing it. |
| [`public Vector2 Normalized()`](#m-electron2d-vector2-normalized) | Returns this vector scaled to unit length. |
| [`public Vector2 Orthogonal()`](#m-electron2d-vector2-orthogonal) | Returns a perpendicular vector rotated 90 degrees counter-clockwise in screen space. |
| [`public Vector2 PosMod(float mod)`](#m-electron2d-vector2-posmod-system-single) | Applies positive modulus to both components. |
| [`public Vector2 PosMod(Vector2 mod)`](#m-electron2d-vector2-posmod-electron2d-vector2) | Applies componentwise positive modulus. |
| [`public Vector2 Project(Vector2 onNormal)`](#m-electron2d-vector2-project-electron2d-vector2) | Projects this vector onto another vector. |
| [`public Vector2 Reflect(Vector2 normal)`](#m-electron2d-vector2-reflect-electron2d-vector2) | Reflects this vector across a line with a given unit normal. |
| [`public Vector2 Rotated(float angle)`](#m-electron2d-vector2-rotated-system-single) | Rotates this vector by an angle. |
| [`public Vector2 Round()`](#m-electron2d-vector2-round) | Rounds both components to the nearest integer with midpoint-to-even behavior. |
| [`public Vector2 Sign()`](#m-electron2d-vector2-sign) | Returns the sign of each component. |
| [`public Vector2 Slerp(Vector2 to, float weight)`](#m-electron2d-vector2-slerp-electron2d-vector2-system-single) | Spherically interpolates direction while linearly interpolating length. |
| [`public Vector2 Slide(Vector2 normal)`](#m-electron2d-vector2-slide-electron2d-vector2) | Removes the component along a line normal. |
| [`public Vector2 Snapped(Vector2 step)`](#m-electron2d-vector2-snapped-electron2d-vector2) | Snaps each component to the nearest multiple of the corresponding step. |
| [`public Vector2 Snapped(float step)`](#m-electron2d-vector2-snapped-system-single) | Snaps both components to the nearest multiple of a scalar step. |
| [`public override bool Equals(object obj)`](#m-electron2d-vector2-equals-system-object) | Tests whether another object is an exactly equal vector. |
| [`public bool Equals(Vector2 other)`](#m-electron2d-vector2-equals-electron2d-vector2) | Tests both components for exact equality. |
| [`public override int GetHashCode()`](#m-electron2d-vector2-gethashcode) | Returns a hash code based on both components. |
| [`public override string ToString()`](#m-electron2d-vector2-tostring) | Formats both components using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-vector2-tostring-system-string) | Formats both components with a numeric format and invariant culture. |

## Enumerations

| Member | Description |
| --- | --- |
| [`public enum Vector2.Axis`](#t-electron2d-vector2-axis) | Identifies one vector component. |

## Constants

| Member | Description |
| --- | --- |
| [`Vector2.Axis.X = 0`](#f-electron2d-vector2-axis-x) | Identifies the horizontal X component. |
| [`Vector2.Axis.Y = 1`](#f-electron2d-vector2-axis-y) | Identifies the vertical Y component. |

## Fields

| Member | Description |
| --- | --- |
| [`public float X`](#f-electron2d-vector2-x) | Gets or sets the horizontal component. |
| [`public float Y`](#f-electron2d-vector2-y) | Gets or sets the vertical component. |

## Operators

| Member | Description |
| --- | --- |
| [`public static Vector2 operator +(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-addition-electron2d-vector2-electron2d-vector2) | Adds two vectors componentwise. |
| [`public static Vector2 operator +(Vector2 value)`](#m-electron2d-vector2-op-unaryplus-electron2d-vector2) | Returns a vector unchanged. |
| [`public static Vector2 operator -(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-subtraction-electron2d-vector2-electron2d-vector2) | Subtracts two vectors componentwise. |
| [`public static Vector2 operator -(Vector2 value)`](#m-electron2d-vector2-op-unarynegation-electron2d-vector2) | Negates both components. |
| [`public static Vector2 operator *(Vector2 vector, float scalar)`](#m-electron2d-vector2-op-multiply-electron2d-vector2-system-single) | Multiplies a vector by a scalar. |
| [`public static Vector2 operator *(float scalar, Vector2 vector)`](#m-electron2d-vector2-op-multiply-system-single-electron2d-vector2) | Multiplies a scalar by a vector. |
| [`public static Vector2 operator *(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-multiply-electron2d-vector2-electron2d-vector2) | Multiplies two vectors componentwise. |
| [`public static Vector2 operator /(Vector2 vector, float divisor)`](#m-electron2d-vector2-op-division-electron2d-vector2-system-single) | Divides a vector by a scalar. |
| [`public static Vector2 operator /(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-division-electron2d-vector2-electron2d-vector2) | Divides two vectors componentwise. |
| [`public static Vector2 operator %(Vector2 vector, float divisor)`](#m-electron2d-vector2-op-modulus-electron2d-vector2-system-single) | Returns the truncated remainder of both components by a scalar. |
| [`public static Vector2 operator %(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-modulus-electron2d-vector2-electron2d-vector2) | Returns the truncated componentwise remainder of two vectors. |
| [`public static bool operator ==(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-equality-electron2d-vector2-electron2d-vector2) | Tests both components for exact equality. |
| [`public static bool operator !=(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-inequality-electron2d-vector2-electron2d-vector2) | Tests whether either component differs under exact equality. |
| [`public static bool operator <(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-lessthan-electron2d-vector2-electron2d-vector2) | Compares vectors lexicographically by X and then Y. |
| [`public static bool operator >(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-greaterthan-electron2d-vector2-electron2d-vector2) | Compares vectors lexicographically by X and then Y. |
| [`public static bool operator <=(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-lessthanorequal-electron2d-vector2-electron2d-vector2) | Compares vectors lexicographically by X and then Y. |
| [`public static bool operator >=(Vector2 left, Vector2 right)`](#m-electron2d-vector2-op-greaterthanorequal-electron2d-vector2-electron2d-vector2) | Compares vectors lexicographically by X and then Y. |

## Constructor Descriptions

<a id="m-electron2d-vector2-ctor-system-single-system-single"></a>
### `public Vector2(float x, float y)`

Initializes a vector from horizontal and vertical components.

**Parameters**

- `x`: The horizontal component.
- `y`: The vertical component.

<a id="m-electron2d-vector2-ctor-electron2d-vector2i"></a>
### `public Vector2(Vector2I value)`

Initializes a floating-point vector from an integer vector.

**Parameters**

- `value`: The integer vector whose components are converted exactly to single precision when representable.

**Remarks:** Large integer components can lose low-order precision during conversion.

## Property Descriptions

<a id="p-electron2d-vector2-zero"></a>
### `public static Vector2 Zero { get; }`

Gets the zero vector.

**Value:** `(0, 0)`.

<a id="p-electron2d-vector2-one"></a>
### `public static Vector2 One { get; }`

Gets the vector whose components are both one.

**Value:** `(1, 1)`.

<a id="p-electron2d-vector2-inf"></a>
### `public static Vector2 Inf { get; }`

Gets the vector whose components are both positive infinity.

**Value:** `(+Infinity, +Infinity)`.

<a id="p-electron2d-vector2-up"></a>
### `public static Vector2 Up { get; }`

Gets the upward screen-space unit vector.

**Value:** `(0, -1)`.

<a id="p-electron2d-vector2-down"></a>
### `public static Vector2 Down { get; }`

Gets the downward screen-space unit vector.

**Value:** `(0, 1)`.

<a id="p-electron2d-vector2-right"></a>
### `public static Vector2 Right { get; }`

Gets the rightward unit vector.

**Value:** `(1, 0)`.

<a id="p-electron2d-vector2-left"></a>
### `public static Vector2 Left { get; }`

Gets the leftward unit vector.

**Value:** `(-1, 0)`.

<a id="p-electron2d-vector2-item-system-int32"></a>
### `public float this[int index] { get; set; }`

Gets or sets a component by axis index.

**Parameters**

- `index`: Zero for [`Vector2.X`](Vector2.md#f-electron2d-vector2-x) or one for [`Vector2.Y`](Vector2.md#f-electron2d-vector2-y).

**Value:** The selected component.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is not zero or one.

## Method Descriptions

<a id="m-electron2d-vector2-deconstruct-system-single-byref-system-single-byref"></a>
### `public void Deconstruct(out float x, out float y)`

Deconstructs the vector into its two components.

**Parameters**

- `x`: Receives [`Vector2.X`](Vector2.md#f-electron2d-vector2-x).
- `y`: Receives [`Vector2.Y`](Vector2.md#f-electron2d-vector2-y).

<a id="m-electron2d-vector2-abs"></a>
### `public Vector2 Abs()`

Returns a vector containing the absolute value of each component.

**Returns:** The componentwise absolute value.

<a id="m-electron2d-vector2-angle"></a>
### `public float Angle()`

Returns the angle from the positive X axis to this vector.

**Returns:** The clockwise screen-space angle in radians.

<a id="m-electron2d-vector2-angleto-electron2d-vector2"></a>
### `public float AngleTo(Vector2 to)`

Returns the signed angle from this vector to another vector.

**Parameters**

- `to`: The destination vector.

**Returns:** The signed angle in radians in the range from negative pi through positive pi.

<a id="m-electron2d-vector2-angletopoint-electron2d-vector2"></a>
### `public float AngleToPoint(Vector2 to)`

Returns the angle of the line from this point to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The clockwise screen-space angle from the positive X axis, in radians.

<a id="m-electron2d-vector2-aspect"></a>
### `public float Aspect()`

Returns the ratio of the horizontal component to the vertical component.

**Returns:** `X / Y`, including normal IEEE 754 division behavior.

<a id="m-electron2d-vector2-bezierderivative-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single"></a>
### `public Vector2 BezierDerivative(Vector2 control1, Vector2 control2, Vector2 end, float t)`

Returns the derivative at a parameter on a cubic Bézier curve.

**Parameters**

- `control1`: The first control point.
- `control2`: The second control point.
- `end`: The curve end point.
- `t`: The curve parameter; values outside zero through one extrapolate.

**Returns:** The curve derivative at `t`.

<a id="m-electron2d-vector2-bezierinterpolate-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single"></a>
### `public Vector2 BezierInterpolate(Vector2 control1, Vector2 control2, Vector2 end, float t)`

Returns a point on a cubic Bézier curve.

**Parameters**

- `control1`: The first control point.
- `control2`: The second control point.
- `end`: The curve end point.
- `t`: The curve parameter; values outside zero through one extrapolate.

**Returns:** The interpolated curve point.

<a id="m-electron2d-vector2-bounce-electron2d-vector2"></a>
### `public Vector2 Bounce(Vector2 normal)`

Returns this vector bounced from a line with a given unit normal.

**Parameters**

- `normal`: The normalized line normal.

**Returns:** The vector reflected across the line's normal plane.

**Remarks:** The method assumes that `normal` is normalized and does not validate it.

<a id="m-electron2d-vector2-ceil"></a>
### `public Vector2 Ceil()`

Rounds both components upward toward positive infinity.

**Returns:** The componentwise ceiling.

<a id="m-electron2d-vector2-clamp-electron2d-vector2-electron2d-vector2"></a>
### `public Vector2 Clamp(Vector2 min, Vector2 max)`

Clamps each component between corresponding vector bounds.

**Parameters**

- `min`: The componentwise lower bounds.
- `max`: The componentwise upper bounds.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: A lower bound is greater than its corresponding upper bound.

<a id="m-electron2d-vector2-clamp-system-single-system-single"></a>
### `public Vector2 Clamp(float min, float max)`

Clamps both components between scalar bounds.

**Parameters**

- `min`: The lower bound.
- `max`: The upper bound.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-vector2-cross-electron2d-vector2"></a>
### `public float Cross(Vector2 with)`

Returns the scalar two-dimensional cross product.

**Parameters**

- `with`: The other vector.

**Returns:** The signed parallelogram area; positive means `with` is clockwise in screen space.

<a id="m-electron2d-vector2-cubicinterpolate-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single"></a>
### `public Vector2 CubicInterpolate(Vector2 b, Vector2 preA, Vector2 postB, float weight)`

Performs Catmull-Rom cubic interpolation between this vector and another.

**Parameters**

- `b`: The destination vector.
- `preA`: The control vector before this vector.
- `postB`: The control vector after `b`.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** The interpolated vector.

<a id="m-electron2d-vector2-cubicinterpolateintime-electron2d-vector2-electron2d-vector2-electron2d-vector2-system-single-system-single-system-single-system-single"></a>
### `public Vector2 CubicInterpolateInTime(Vector2 b, Vector2 preA, Vector2 postB, float weight, float bTime, float preATime, float postBTime)`

Performs time-aware Barry-Goldman cubic interpolation.

**Parameters**

- `b`: The destination vector.
- `preA`: The control vector before this vector.
- `postB`: The control vector after `b`.
- `weight`: The interpolation weight; values outside zero through one extrapolate.
- `bTime`: The time assigned to `b` relative to this vector at time zero.
- `preATime`: The time assigned to `preA`.
- `postBTime`: The time assigned to `postB`.

**Returns:** The time-aware interpolated vector.

<a id="m-electron2d-vector2-directionto-electron2d-vector2"></a>
### `public Vector2 DirectionTo(Vector2 to)`

Returns the normalized direction from this point to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The normalized difference, or [`Vector2.Zero`](Vector2.md#p-electron2d-vector2-zero) when both points are equal.

<a id="m-electron2d-vector2-distancesquaredto-electron2d-vector2"></a>
### `public float DistanceSquaredTo(Vector2 to)`

Returns the squared Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The squared distance.

<a id="m-electron2d-vector2-distanceto-electron2d-vector2"></a>
### `public float DistanceTo(Vector2 to)`

Returns the Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The distance.

<a id="m-electron2d-vector2-dot-electron2d-vector2"></a>
### `public float Dot(Vector2 with)`

Returns the dot product with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** `X * with.X + Y * with.Y`.

<a id="m-electron2d-vector2-floor"></a>
### `public Vector2 Floor()`

Rounds both components downward toward negative infinity.

**Returns:** The componentwise floor.

<a id="m-electron2d-vector2-fromangle-system-single"></a>
### `public static Vector2 FromAngle(float angle)`

Creates an approximately unit vector from an angle.

**Parameters**

- `angle`: The clockwise screen-space angle in radians.

**Returns:** `(cos(angle), sin(angle))`.

<a id="m-electron2d-vector2-inverse"></a>
### `public Vector2 Inverse()`

Returns the componentwise reciprocal.

**Returns:** `(1 / X, 1 / Y)`, including normal IEEE 754 zero-division behavior.

<a id="m-electron2d-vector2-isequalapprox-electron2d-vector2"></a>
### `public bool IsEqualApprox(Vector2 other)`

Tests both components for scale-aware approximate equality.

**Parameters**

- `other`: The vector to compare.

**Returns:** `true` when both components are approximately equal.

<a id="m-electron2d-vector2-isfinite"></a>
### `public bool IsFinite()`

Tests whether both components are finite.

**Returns:** `true` when neither component is NaN or infinity.

<a id="m-electron2d-vector2-isnormalized"></a>
### `public bool IsNormalized()`

Tests whether the squared length is approximately one.

**Returns:** `true` when the vector is approximately unit length.

<a id="m-electron2d-vector2-iszeroapprox"></a>
### `public bool IsZeroApprox()`

Tests whether both components are approximately zero.

**Returns:** `true` when each component's absolute value is below the comparison tolerance.

<a id="m-electron2d-vector2-length"></a>
### `public float Length()`

Returns the Euclidean length.

**Returns:** The square root of [`Vector2.LengthSquared`](Vector2.md#m-electron2d-vector2-lengthsquared).

<a id="m-electron2d-vector2-lengthsquared"></a>
### `public float LengthSquared()`

Returns the squared Euclidean length.

**Returns:** `X * X + Y * Y`.

<a id="m-electron2d-vector2-lerp-electron2d-vector2-system-single"></a>
### `public Vector2 Lerp(Vector2 to, float weight)`

Linearly interpolates or extrapolates toward another vector.

**Parameters**

- `to`: The destination vector.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** The componentwise linear interpolation.

<a id="m-electron2d-vector2-limitlength-system-single"></a>
### `public Vector2 LimitLength(float length = 1f)`

Limits the vector to a maximum length.

**Parameters**

- `length`: The maximum length.

**Returns:** This vector unchanged when already shorter, otherwise a vector with the requested length.

**Remarks:** Non-finite inputs have undefined numeric results. A negative limit reverses a nonzero vector.

<a id="m-electron2d-vector2-max-electron2d-vector2"></a>
### `public Vector2 Max(Vector2 with)`

Returns the componentwise maximum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector2-max-system-single"></a>
### `public Vector2 Max(float with)`

Returns the componentwise maximum with a scalar.

**Parameters**

- `with`: The scalar compared with both components.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector2-maxaxisindex"></a>
### `public Vector2.Axis MaxAxisIndex()`

Returns the axis containing the greatest component.

**Returns:** [`Vector2.Axis.X`](Vector2.md#f-electron2d-vector2-axis-x) when components are equal; otherwise the greatest component's axis.

<a id="m-electron2d-vector2-min-electron2d-vector2"></a>
### `public Vector2 Min(Vector2 with)`

Returns the componentwise minimum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector2-min-system-single"></a>
### `public Vector2 Min(float with)`

Returns the componentwise minimum with a scalar.

**Parameters**

- `with`: The scalar compared with both components.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector2-minaxisindex"></a>
### `public Vector2.Axis MinAxisIndex()`

Returns the axis containing the least component.

**Returns:** [`Vector2.Axis.Y`](Vector2.md#f-electron2d-vector2-axis-y) when components are equal; otherwise the least component's axis.

<a id="m-electron2d-vector2-movetoward-electron2d-vector2-system-single"></a>
### `public Vector2 MoveToward(Vector2 to, float delta)`

Moves toward another vector by a fixed distance without passing it.

**Parameters**

- `to`: The destination vector.
- `delta`: The signed distance to move. Negative values move away.

**Returns:** The moved vector, or `to` when within the nonnegative step.

<a id="m-electron2d-vector2-normalized"></a>
### `public Vector2 Normalized()`

Returns this vector scaled to unit length.

**Returns:** A normalized vector, or [`Vector2.Zero`](Vector2.md#p-electron2d-vector2-zero) when the squared length is exactly zero.

**Remarks:** Near-zero and non-finite inputs can lose precision or produce non-finite components.

<a id="m-electron2d-vector2-orthogonal"></a>
### `public Vector2 Orthogonal()`

Returns a perpendicular vector rotated 90 degrees counter-clockwise in screen space.

**Returns:** `(Y, -X)` with the same length as this vector.

<a id="m-electron2d-vector2-posmod-system-single"></a>
### `public Vector2 PosMod(float mod)`

Applies positive modulus to both components.

**Parameters**

- `mod`: The scalar divisor.

**Returns:** The componentwise canonical remainder using the divisor's sign.

<a id="m-electron2d-vector2-posmod-electron2d-vector2"></a>
### `public Vector2 PosMod(Vector2 mod)`

Applies componentwise positive modulus.

**Parameters**

- `mod`: The component divisors.

**Returns:** The componentwise canonical remainders using each divisor's sign.

<a id="m-electron2d-vector2-project-electron2d-vector2"></a>
### `public Vector2 Project(Vector2 onNormal)`

Projects this vector onto another vector.

**Parameters**

- `onNormal`: The projection direction; it does not need to be normalized.

**Returns:** A vector parallel to `onNormal`.

**Remarks:** A zero projection direction produces NaN components through IEEE 754 division.

<a id="m-electron2d-vector2-reflect-electron2d-vector2"></a>
### `public Vector2 Reflect(Vector2 normal)`

Reflects this vector across a line with a given unit normal.

**Parameters**

- `normal`: The normalized line normal.

**Returns:** `2 * Dot(normal) * normal - this`.

**Remarks:** This line-reflection convention is the inverse sign of the plane-normal reflection commonly named
`reflect` by other math libraries. The method assumes a normalized normal and does not validate it.

<a id="m-electron2d-vector2-rotated-system-single"></a>
### `public Vector2 Rotated(float angle)`

Rotates this vector by an angle.

**Parameters**

- `angle`: The clockwise screen-space angle in radians.

**Returns:** The rotated vector.

<a id="m-electron2d-vector2-round"></a>
### `public Vector2 Round()`

Rounds both components to the nearest integer with midpoint-to-even behavior.

**Returns:** The componentwise rounded vector.

<a id="m-electron2d-vector2-sign"></a>
### `public Vector2 Sign()`

Returns the sign of each component.

**Returns:** Components containing negative one, zero, or positive one.

**Exceptions**

- `ArithmeticException`: A component is NaN.

<a id="m-electron2d-vector2-slerp-electron2d-vector2-system-single"></a>
### `public Vector2 Slerp(Vector2 to, float weight)`

Spherically interpolates direction while linearly interpolating length.

**Parameters**

- `to`: The destination vector.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** The spherical interpolation, or linear interpolation when either vector has zero length.

<a id="m-electron2d-vector2-slide-electron2d-vector2"></a>
### `public Vector2 Slide(Vector2 normal)`

Removes the component along a line normal.

**Parameters**

- `normal`: The normalized line normal.

**Returns:** The component perpendicular to `normal`.

**Remarks:** The method assumes that `normal` is normalized and does not validate it.

<a id="m-electron2d-vector2-snapped-electron2d-vector2"></a>
### `public Vector2 Snapped(Vector2 step)`

Snaps each component to the nearest multiple of the corresponding step.

**Parameters**

- `step`: The componentwise step. A zero component leaves the corresponding value unchanged.

**Returns:** The snapped vector.

<a id="m-electron2d-vector2-snapped-system-single"></a>
### `public Vector2 Snapped(float step)`

Snaps both components to the nearest multiple of a scalar step.

**Parameters**

- `step`: The scalar step. Zero leaves both values unchanged.

**Returns:** The snapped vector.

<a id="m-electron2d-vector2-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an exactly equal vector.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is a vector with equal components.

<a id="m-electron2d-vector2-equals-electron2d-vector2"></a>
### `public bool Equals(Vector2 other)`

Tests both components for exact equality.

**Parameters**

- `other`: The vector to compare.

**Returns:** `true` when both corresponding components are exactly equal.

<a id="m-electron2d-vector2-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on both components.

**Returns:** The component hash code.

<a id="m-electron2d-vector2-tostring"></a>
### `public override string ToString()`

Formats both components using invariant culture.

**Returns:** A parenthesized component pair.

<a id="m-electron2d-vector2-tostring-system-string"></a>
### `public string ToString(string format)`

Formats both components with a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default format.

**Returns:** A parenthesized component pair.

**Exceptions**

- `FormatException`: `format` is invalid.

## Enumeration Descriptions

<a id="t-electron2d-vector2-axis"></a>
### `public enum Vector2.Axis`

Identifies one vector component.

## Constant Descriptions

<a id="f-electron2d-vector2-axis-x"></a>
### `Vector2.Axis.X = 0`

Identifies the horizontal X component.

<a id="f-electron2d-vector2-axis-y"></a>
### `Vector2.Axis.Y = 1`

Identifies the vertical Y component.

## Field Descriptions

<a id="f-electron2d-vector2-x"></a>
### `public float X`

Gets or sets the horizontal component.

<a id="f-electron2d-vector2-y"></a>
### `public float Y`

Gets or sets the vertical component.

## Operator Descriptions

<a id="m-electron2d-vector2-op-addition-electron2d-vector2-electron2d-vector2"></a>
### `public static Vector2 operator +(Vector2 left, Vector2 right)`

Adds two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The componentwise sum.

<a id="m-electron2d-vector2-op-unaryplus-electron2d-vector2"></a>
### `public static Vector2 operator +(Vector2 value)`

Returns a vector unchanged.

**Parameters**

- `value`: The vector.

**Returns:** `value`.

<a id="m-electron2d-vector2-op-subtraction-electron2d-vector2-electron2d-vector2"></a>
### `public static Vector2 operator -(Vector2 left, Vector2 right)`

Subtracts two vectors componentwise.

**Parameters**

- `left`: The minuend.
- `right`: The subtrahend.

**Returns:** The componentwise difference.

<a id="m-electron2d-vector2-op-unarynegation-electron2d-vector2"></a>
### `public static Vector2 operator -(Vector2 value)`

Negates both components.

**Parameters**

- `value`: The vector to negate.

**Returns:** The componentwise negation.

<a id="m-electron2d-vector2-op-multiply-electron2d-vector2-system-single"></a>
### `public static Vector2 operator *(Vector2 vector, float scalar)`

Multiplies a vector by a scalar.

**Parameters**

- `vector`: The vector.
- `scalar`: The scalar multiplier.

**Returns:** The componentwise product.

<a id="m-electron2d-vector2-op-multiply-system-single-electron2d-vector2"></a>
### `public static Vector2 operator *(float scalar, Vector2 vector)`

Multiplies a scalar by a vector.

**Parameters**

- `scalar`: The scalar multiplier.
- `vector`: The vector.

**Returns:** The componentwise product.

<a id="m-electron2d-vector2-op-multiply-electron2d-vector2-electron2d-vector2"></a>
### `public static Vector2 operator *(Vector2 left, Vector2 right)`

Multiplies two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The componentwise product.

<a id="m-electron2d-vector2-op-division-electron2d-vector2-system-single"></a>
### `public static Vector2 operator /(Vector2 vector, float divisor)`

Divides a vector by a scalar.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The IEEE 754 componentwise quotient.

<a id="m-electron2d-vector2-op-division-electron2d-vector2-electron2d-vector2"></a>
### `public static Vector2 operator /(Vector2 left, Vector2 right)`

Divides two vectors componentwise.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The IEEE 754 componentwise quotient.

<a id="m-electron2d-vector2-op-modulus-electron2d-vector2-system-single"></a>
### `public static Vector2 operator %(Vector2 vector, float divisor)`

Returns the truncated remainder of both components by a scalar.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The componentwise remainder. Use [`Vector2.PosMod(Single)`](Vector2.md#m-electron2d-vector2-posmod-system-single) for canonical negative handling.

<a id="m-electron2d-vector2-op-modulus-electron2d-vector2-electron2d-vector2"></a>
### `public static Vector2 operator %(Vector2 left, Vector2 right)`

Returns the truncated componentwise remainder of two vectors.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The componentwise remainder. Use [`Vector2.PosMod(Vector2)`](Vector2.md#m-electron2d-vector2-posmod-electron2d-vector2) for canonical negative handling.

<a id="m-electron2d-vector2-op-equality-electron2d-vector2-electron2d-vector2"></a>
### `public static bool operator ==(Vector2 left, Vector2 right)`

Tests both components for exact equality.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when both corresponding components are exactly equal.

<a id="m-electron2d-vector2-op-inequality-electron2d-vector2-electron2d-vector2"></a>
### `public static bool operator !=(Vector2 left, Vector2 right)`

Tests whether either component differs under exact equality.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when at least one corresponding component differs.

<a id="m-electron2d-vector2-op-lessthan-electron2d-vector2-electron2d-vector2"></a>
### `public static bool operator <(Vector2 left, Vector2 right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts before `right`.

<a id="m-electron2d-vector2-op-greaterthan-electron2d-vector2-electron2d-vector2"></a>
### `public static bool operator >(Vector2 left, Vector2 right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts after `right`.

<a id="m-electron2d-vector2-op-lessthanorequal-electron2d-vector2-electron2d-vector2"></a>
### `public static bool operator <=(Vector2 left, Vector2 right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort after `right`.

<a id="m-electron2d-vector2-op-greaterthanorequal-electron2d-vector2-electron2d-vector2"></a>
### `public static bool operator >=(Vector2 left, Vector2 right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort before `right`.

## Numeric invariants and error behavior

- Ordinary arithmetic, division, remainder, reciprocal, projection, and interpolation retain IEEE 754 NaN and infinity propagation.
- `Normalized()` and `DirectionTo()` return `Zero` for an exactly zero squared length. `IsNormalized()` uses the engine's `0.001` unit-length tolerance; approximate component comparisons use [`Mathf.Epsilon`](Mathf.md) (`1e-6f`) with exact equality first.
- Angles are radians; positive angles rotate positive X toward positive Y and therefore appear clockwise in screen coordinates.
- `Reflect` follows the line-reflection convention `2 * Dot(normal) * normal - value`; `Bounce` negates it. `Reflect`, `Bounce`, and `Slide` require a normalized normal and do not validate it.
- `Project(Zero)` produces NaN components. `PosMod` with a nonzero divisor uses the divisor's sign; zero divisors produce IEEE NaN.
- `Round` uses midpoint-to-even. `Snapped` uses `floor(value / step + 0.5) * step`; a zero step leaves the component unchanged.
- Clamp overloads throw `ArgumentException` for reversed bounds. The indexer throws `ArgumentOutOfRangeException`; `Sign` throws `ArithmeticException` for NaN; an invalid format throws `FormatException`.
- Exact equality treats signed zeros as equal and NaN as unequal. Relational operators remain unordered when the first differing component is NaN.

## Lifecycle, threading, dependencies, and allocation

Copies are independent values. Concurrent reads of independent copies are safe; concurrent writes to the same storage location are an ordinary unsynchronized data race. Numeric operations allocate no managed memory after JIT warmup; string formatting allocates.

The type depends on canonical scalar [`Mathf`](Mathf.md) plus formatting and layout primitives. [`Rect`](Rect.md), [`Transform`](Transform.md), and [`Node`](Node.md) use it throughout their public 2D API. [`ConfigFile`](ConfigFile.md) persists finite values as exactly `X` and `Y`; [`PackedScene`](PackedScene.md) stores it directly through typed property descriptors.

## Coverage, verification, and limitations

The reference API audit classifies the numeric/geometric surface as implemented. Universal-value truth conversion is permanently excluded by the typed C# architecture. No automatic conversion to an external numerics type is exposed. No 3D vector or spatial API is present.

The executable harness covers layout, constants, index failures, construction and integer conversion failures, every method and operator family, interpolation, zero/non-finite behavior, ordering, formatting, strict persistence, packed-scene copying, and warmed zero-allocation math. Execution is Linux/.NET 8 only; native ABI and the full five-target matrix remain unverified.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
