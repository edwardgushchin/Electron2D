# Vector4

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Vector4.cs`](../../src/Core/Math/Vector4.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Vector4`

> Represents a four-component floating-point vector for numeric tuples.

## Description

Represents a four-component floating-point vector for numeric tuples.

`Vector4` is an engine-owned mutable four-component single-precision numeric tuple. It is suitable for generic four-value data and future typed GPU boundaries without creating 3D or 4D scene geometry. Sequential X/Y/Z/W layout is verified as 16 bytes. It owns no identity, handle, callback, or lifecycle.

Ordinary arithmetic preserves IEEE 754 NaN and infinity values. The zero-initialized value is [`Vector4.Zero`](Vector4.md#p-electron2d-vector4-zero).
Numeric operations do not allocate managed memory; string formatting allocates a string.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var weights = new Vector4(1f, 0.5f, 0.25f, 0f);
var normalized = weights.Normalized();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Vector4(float x, float y, float z, float w)`](#m-electron2d-vector4-ctor-system-single-system-single-system-single-system-single) | Initializes a vector from four components. |
| [`public Vector4(Vector4I value)`](#m-electron2d-vector4-ctor-electron2d-vector4i) | Initializes a floating-point vector from an integer vector. |

## Properties

| Member | Description |
| --- | --- |
| [`public static Vector4 Zero { get; }`](#p-electron2d-vector4-zero) | Gets the zero vector. |
| [`public static Vector4 One { get; }`](#p-electron2d-vector4-one) | Gets the vector whose components are all one. |
| [`public static Vector4 Inf { get; }`](#p-electron2d-vector4-inf) | Gets the vector whose components are all positive infinity. |
| [`public float this[int index] { get; set; }`](#p-electron2d-vector4-item-system-int32) | Gets or sets a component by axis index. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Deconstruct(out float x, out float y, out float z, out float w)`](#m-electron2d-vector4-deconstruct-system-single-byref-system-single-byref-system-single-byref-system-single-byref) | Deconstructs the vector into its four components. |
| [`public Vector4 Abs()`](#m-electron2d-vector4-abs) | Returns the componentwise absolute value. |
| [`public Vector4 Ceil()`](#m-electron2d-vector4-ceil) | Rounds every component upward toward positive infinity. |
| [`public Vector4 Clamp(Vector4 min, Vector4 max)`](#m-electron2d-vector4-clamp-electron2d-vector4-electron2d-vector4) | Clamps each component between corresponding vector bounds. |
| [`public Vector4 Clamp(float min, float max)`](#m-electron2d-vector4-clamp-system-single-system-single) | Clamps every component between scalar bounds. |
| [`public Vector4 CubicInterpolate(Vector4 b, Vector4 preA, Vector4 postB, float weight)`](#m-electron2d-vector4-cubicinterpolate-electron2d-vector4-electron2d-vector4-electron2d-vector4-system-single) | Performs Catmull-Rom cubic interpolation between this vector and another. |
| [`public Vector4 CubicInterpolateInTime(Vector4 b, Vector4 preA, Vector4 postB, float weight, float bTime, float preATime, float postBTime)`](#m-electron2d-vector4-cubicinterpolateintime-electron2d-vector4-electron2d-vector4-electron2d-vector4-system-single-system-single-system-single-system-single) | Performs time-aware Barry-Goldman cubic interpolation. |
| [`public Vector4 DirectionTo(Vector4 to)`](#m-electron2d-vector4-directionto-electron2d-vector4) | Returns the normalized direction from this point to another point. |
| [`public float DistanceSquaredTo(Vector4 to)`](#m-electron2d-vector4-distancesquaredto-electron2d-vector4) | Returns the squared Euclidean distance to another point. |
| [`public float DistanceTo(Vector4 to)`](#m-electron2d-vector4-distanceto-electron2d-vector4) | Returns the Euclidean distance to another point. |
| [`public float Dot(Vector4 with)`](#m-electron2d-vector4-dot-electron2d-vector4) | Returns the dot product with another vector. |
| [`public Vector4 Floor()`](#m-electron2d-vector4-floor) | Rounds every component downward toward negative infinity. |
| [`public Vector4 Inverse()`](#m-electron2d-vector4-inverse) | Returns the componentwise reciprocal. |
| [`public bool IsFinite()`](#m-electron2d-vector4-isfinite) | Tests whether every component is finite. |
| [`public bool IsNormalized()`](#m-electron2d-vector4-isnormalized) | Tests whether the squared length is approximately one. |
| [`public float Length()`](#m-electron2d-vector4-length) | Returns the Euclidean length. |
| [`public float LengthSquared()`](#m-electron2d-vector4-lengthsquared) | Returns the squared Euclidean length. |
| [`public Vector4 Lerp(Vector4 to, float weight)`](#m-electron2d-vector4-lerp-electron2d-vector4-system-single) | Linearly interpolates or extrapolates toward another vector. |
| [`public Vector4 Max(Vector4 with)`](#m-electron2d-vector4-max-electron2d-vector4) | Returns the componentwise maximum with another vector. |
| [`public Vector4 Max(float with)`](#m-electron2d-vector4-max-system-single) | Returns the componentwise maximum with a scalar. |
| [`public Vector4.Axis MaxAxisIndex()`](#m-electron2d-vector4-maxaxisindex) | Returns the axis containing the greatest component. |
| [`public Vector4 Min(Vector4 with)`](#m-electron2d-vector4-min-electron2d-vector4) | Returns the componentwise minimum with another vector. |
| [`public Vector4 Min(float with)`](#m-electron2d-vector4-min-system-single) | Returns the componentwise minimum with a scalar. |
| [`public Vector4.Axis MinAxisIndex()`](#m-electron2d-vector4-minaxisindex) | Returns the axis containing the least component. |
| [`public Vector4 Normalized()`](#m-electron2d-vector4-normalized) | Returns this vector scaled to unit length. |
| [`public Vector4 PosMod(float mod)`](#m-electron2d-vector4-posmod-system-single) | Applies positive modulus to every component. |
| [`public Vector4 PosMod(Vector4 mod)`](#m-electron2d-vector4-posmod-electron2d-vector4) | Applies componentwise positive modulus. |
| [`public Vector4 Round()`](#m-electron2d-vector4-round) | Rounds every component to the nearest integer using midpoint-to-even behavior. |
| [`public Vector4 Sign()`](#m-electron2d-vector4-sign) | Returns the sign of every component. |
| [`public Vector4 Snapped(Vector4 step)`](#m-electron2d-vector4-snapped-electron2d-vector4) | Snaps each component to the nearest multiple of the corresponding step. |
| [`public Vector4 Snapped(float step)`](#m-electron2d-vector4-snapped-system-single) | Snaps every component to the nearest multiple of a scalar step. |
| [`public override bool Equals(object obj)`](#m-electron2d-vector4-equals-system-object) | Tests whether another object is an exactly equal vector. |
| [`public bool Equals(Vector4 other)`](#m-electron2d-vector4-equals-electron2d-vector4) | Tests every component for exact equality. |
| [`public bool IsEqualApprox(Vector4 other)`](#m-electron2d-vector4-isequalapprox-electron2d-vector4) | Tests every component for scale-aware approximate equality. |
| [`public bool IsZeroApprox()`](#m-electron2d-vector4-iszeroapprox) | Tests whether every component is approximately zero. |
| [`public override int GetHashCode()`](#m-electron2d-vector4-gethashcode) | Returns a hash code based on all components. |
| [`public override string ToString()`](#m-electron2d-vector4-tostring) | Formats every component using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-vector4-tostring-system-string) | Formats every component with a numeric format and invariant culture. |

## Enumerations

| Member | Description |
| --- | --- |
| [`public enum Vector4.Axis`](#t-electron2d-vector4-axis) | Identifies one vector component. |

## Constants

| Member | Description |
| --- | --- |
| [`Vector4.Axis.X = 0`](#f-electron2d-vector4-axis-x) | Identifies the X component. |
| [`Vector4.Axis.Y = 1`](#f-electron2d-vector4-axis-y) | Identifies the Y component. |
| [`Vector4.Axis.Z = 2`](#f-electron2d-vector4-axis-z) | Identifies the Z component. |
| [`Vector4.Axis.W = 3`](#f-electron2d-vector4-axis-w) | Identifies the W component. |

## Fields

| Member | Description |
| --- | --- |
| [`public float X`](#f-electron2d-vector4-x) | Gets or sets the X component. |
| [`public float Y`](#f-electron2d-vector4-y) | Gets or sets the Y component. |
| [`public float Z`](#f-electron2d-vector4-z) | Gets or sets the Z component. |
| [`public float W`](#f-electron2d-vector4-w) | Gets or sets the W component. |

## Operators

| Member | Description |
| --- | --- |
| [`public static Vector4 operator +(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-addition-electron2d-vector4-electron2d-vector4) | Adds two vectors componentwise. |
| [`public static Vector4 operator +(Vector4 value)`](#m-electron2d-vector4-op-unaryplus-electron2d-vector4) | Returns a vector unchanged. |
| [`public static Vector4 operator -(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-subtraction-electron2d-vector4-electron2d-vector4) | Subtracts two vectors componentwise. |
| [`public static Vector4 operator -(Vector4 value)`](#m-electron2d-vector4-op-unarynegation-electron2d-vector4) | Negates every component. |
| [`public static Vector4 operator *(Vector4 vector, float scalar)`](#m-electron2d-vector4-op-multiply-electron2d-vector4-system-single) | Multiplies a vector by a scalar. |
| [`public static Vector4 operator *(float scalar, Vector4 vector)`](#m-electron2d-vector4-op-multiply-system-single-electron2d-vector4) | Multiplies a scalar by a vector. |
| [`public static Vector4 operator *(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-multiply-electron2d-vector4-electron2d-vector4) | Multiplies two vectors componentwise. |
| [`public static Vector4 operator /(Vector4 vector, float divisor)`](#m-electron2d-vector4-op-division-electron2d-vector4-system-single) | Divides every component by a scalar. |
| [`public static Vector4 operator /(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-division-electron2d-vector4-electron2d-vector4) | Divides two vectors componentwise. |
| [`public static Vector4 operator %(Vector4 vector, float divisor)`](#m-electron2d-vector4-op-modulus-electron2d-vector4-system-single) | Returns the truncated remainder of every component by a scalar. |
| [`public static Vector4 operator %(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-modulus-electron2d-vector4-electron2d-vector4) | Returns the truncated componentwise remainder of two vectors. |
| [`public static bool operator ==(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-equality-electron2d-vector4-electron2d-vector4) | Tests every component for exact equality. |
| [`public static bool operator !=(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-inequality-electron2d-vector4-electron2d-vector4) | Tests whether any component differs. |
| [`public static bool operator <(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-lessthan-electron2d-vector4-electron2d-vector4) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static bool operator >(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-greaterthan-electron2d-vector4-electron2d-vector4) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static bool operator <=(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-lessthanorequal-electron2d-vector4-electron2d-vector4) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static bool operator >=(Vector4 left, Vector4 right)`](#m-electron2d-vector4-op-greaterthanorequal-electron2d-vector4-electron2d-vector4) | Compares vectors lexicographically by X, Y, Z, then W. |

## Constructor Descriptions

<a id="m-electron2d-vector4-ctor-system-single-system-single-system-single-system-single"></a>
### `public Vector4(float x, float y, float z, float w)`

Initializes a vector from four components.

**Parameters**

- `x`: The X component.
- `y`: The Y component.
- `z`: The Z component.
- `w`: The W component.

<a id="m-electron2d-vector4-ctor-electron2d-vector4i"></a>
### `public Vector4(Vector4I value)`

Initializes a floating-point vector from an integer vector.

**Parameters**

- `value`: The integer vector to convert.

**Remarks:** Large integer components can lose low-order precision during conversion.

## Property Descriptions

<a id="p-electron2d-vector4-zero"></a>
### `public static Vector4 Zero { get; }`

Gets the zero vector.

**Value:** `(0, 0, 0, 0)`.

<a id="p-electron2d-vector4-one"></a>
### `public static Vector4 One { get; }`

Gets the vector whose components are all one.

**Value:** `(1, 1, 1, 1)`.

<a id="p-electron2d-vector4-inf"></a>
### `public static Vector4 Inf { get; }`

Gets the vector whose components are all positive infinity.

**Value:** `(+Infinity, +Infinity, +Infinity, +Infinity)`.

<a id="p-electron2d-vector4-item-system-int32"></a>
### `public float this[int index] { get; set; }`

Gets or sets a component by axis index.

**Parameters**

- `index`: An index from zero through three for X, Y, Z, or W.

**Value:** The selected component.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is outside zero through three.

## Method Descriptions

<a id="m-electron2d-vector4-deconstruct-system-single-byref-system-single-byref-system-single-byref-system-single-byref"></a>
### `public void Deconstruct(out float x, out float y, out float z, out float w)`

Deconstructs the vector into its four components.

**Parameters**

- `x`: Receives [`Vector4.X`](Vector4.md#f-electron2d-vector4-x).
- `y`: Receives [`Vector4.Y`](Vector4.md#f-electron2d-vector4-y).
- `z`: Receives [`Vector4.Z`](Vector4.md#f-electron2d-vector4-z).
- `w`: Receives [`Vector4.W`](Vector4.md#f-electron2d-vector4-w).

<a id="m-electron2d-vector4-abs"></a>
### `public Vector4 Abs()`

Returns the componentwise absolute value.

**Returns:** A vector with nonnegative components, except that NaN remains NaN.

<a id="m-electron2d-vector4-ceil"></a>
### `public Vector4 Ceil()`

Rounds every component upward toward positive infinity.

**Returns:** The componentwise ceiling.

<a id="m-electron2d-vector4-clamp-electron2d-vector4-electron2d-vector4"></a>
### `public Vector4 Clamp(Vector4 min, Vector4 max)`

Clamps each component between corresponding vector bounds.

**Parameters**

- `min`: The componentwise lower bounds.
- `max`: The componentwise upper bounds.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: A lower bound is greater than its corresponding upper bound.

<a id="m-electron2d-vector4-clamp-system-single-system-single"></a>
### `public Vector4 Clamp(float min, float max)`

Clamps every component between scalar bounds.

**Parameters**

- `min`: The lower bound.
- `max`: The upper bound.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-vector4-cubicinterpolate-electron2d-vector4-electron2d-vector4-electron2d-vector4-system-single"></a>
### `public Vector4 CubicInterpolate(Vector4 b, Vector4 preA, Vector4 postB, float weight)`

Performs Catmull-Rom cubic interpolation between this vector and another.

**Parameters**

- `b`: The destination vector.
- `preA`: The control vector before this vector.
- `postB`: The control vector after `b`.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** The interpolated vector.

<a id="m-electron2d-vector4-cubicinterpolateintime-electron2d-vector4-electron2d-vector4-electron2d-vector4-system-single-system-single-system-single-system-single"></a>
### `public Vector4 CubicInterpolateInTime(Vector4 b, Vector4 preA, Vector4 postB, float weight, float bTime, float preATime, float postBTime)`

Performs time-aware Barry-Goldman cubic interpolation.

**Parameters**

- `b`: The destination vector.
- `preA`: The control vector before this vector.
- `postB`: The control vector after `b`.
- `weight`: The interpolation weight.
- `bTime`: The time assigned to `b` relative to this vector at zero.
- `preATime`: The time assigned to `preA`.
- `postBTime`: The time assigned to `postB`.

**Returns:** The time-aware interpolated vector.

<a id="m-electron2d-vector4-directionto-electron2d-vector4"></a>
### `public Vector4 DirectionTo(Vector4 to)`

Returns the normalized direction from this point to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The normalized difference, or [`Vector4.Zero`](Vector4.md#p-electron2d-vector4-zero) when both points are equal.

<a id="m-electron2d-vector4-distancesquaredto-electron2d-vector4"></a>
### `public float DistanceSquaredTo(Vector4 to)`

Returns the squared Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The squared distance.

<a id="m-electron2d-vector4-distanceto-electron2d-vector4"></a>
### `public float DistanceTo(Vector4 to)`

Returns the Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The distance.

<a id="m-electron2d-vector4-dot-electron2d-vector4"></a>
### `public float Dot(Vector4 with)`

Returns the dot product with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The sum of the four component products.

<a id="m-electron2d-vector4-floor"></a>
### `public Vector4 Floor()`

Rounds every component downward toward negative infinity.

**Returns:** The componentwise floor.

<a id="m-electron2d-vector4-inverse"></a>
### `public Vector4 Inverse()`

Returns the componentwise reciprocal.

**Returns:** `(1 / X, 1 / Y, 1 / Z, 1 / W)`, including IEEE 754 zero-division behavior.

<a id="m-electron2d-vector4-isfinite"></a>
### `public bool IsFinite()`

Tests whether every component is finite.

**Returns:** `true` when no component is NaN or infinity.

<a id="m-electron2d-vector4-isnormalized"></a>
### `public bool IsNormalized()`

Tests whether the squared length is approximately one.

**Returns:** `true` when the vector is approximately unit length.

<a id="m-electron2d-vector4-length"></a>
### `public float Length()`

Returns the Euclidean length.

**Returns:** The square root of [`Vector4.LengthSquared`](Vector4.md#m-electron2d-vector4-lengthsquared).

<a id="m-electron2d-vector4-lengthsquared"></a>
### `public float LengthSquared()`

Returns the squared Euclidean length.

**Returns:** The sum of the four squared components.

<a id="m-electron2d-vector4-lerp-electron2d-vector4-system-single"></a>
### `public Vector4 Lerp(Vector4 to, float weight)`

Linearly interpolates or extrapolates toward another vector.

**Parameters**

- `to`: The destination vector.
- `weight`: The interpolation weight.

**Returns:** The componentwise linear interpolation.

<a id="m-electron2d-vector4-max-electron2d-vector4"></a>
### `public Vector4 Max(Vector4 with)`

Returns the componentwise maximum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector4-max-system-single"></a>
### `public Vector4 Max(float with)`

Returns the componentwise maximum with a scalar.

**Parameters**

- `with`: The scalar compared with every component.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector4-maxaxisindex"></a>
### `public Vector4.Axis MaxAxisIndex()`

Returns the axis containing the greatest component.

**Returns:** [`Vector4.Axis.X`](Vector4.md#f-electron2d-vector4-axis-x) when all components are equal; otherwise the first greatest axis.

<a id="m-electron2d-vector4-min-electron2d-vector4"></a>
### `public Vector4 Min(Vector4 with)`

Returns the componentwise minimum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector4-min-system-single"></a>
### `public Vector4 Min(float with)`

Returns the componentwise minimum with a scalar.

**Parameters**

- `with`: The scalar compared with every component.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector4-minaxisindex"></a>
### `public Vector4.Axis MinAxisIndex()`

Returns the axis containing the least component.

**Returns:** [`Vector4.Axis.W`](Vector4.md#f-electron2d-vector4-axis-w) when all components are equal; otherwise the last least axis.

<a id="m-electron2d-vector4-normalized"></a>
### `public Vector4 Normalized()`

Returns this vector scaled to unit length.

**Returns:** A normalized vector, or [`Vector4.Zero`](Vector4.md#p-electron2d-vector4-zero) when the squared length is exactly zero.

<a id="m-electron2d-vector4-posmod-system-single"></a>
### `public Vector4 PosMod(float mod)`

Applies positive modulus to every component.

**Parameters**

- `mod`: The scalar divisor.

**Returns:** The componentwise canonical remainder using the divisor's sign.

<a id="m-electron2d-vector4-posmod-electron2d-vector4"></a>
### `public Vector4 PosMod(Vector4 mod)`

Applies componentwise positive modulus.

**Parameters**

- `mod`: The component divisors.

**Returns:** The componentwise canonical remainders using each divisor's sign.

<a id="m-electron2d-vector4-round"></a>
### `public Vector4 Round()`

Rounds every component to the nearest integer using midpoint-to-even behavior.

**Returns:** The componentwise rounded vector.

<a id="m-electron2d-vector4-sign"></a>
### `public Vector4 Sign()`

Returns the sign of every component.

**Returns:** Components containing negative one, zero, or positive one.

**Exceptions**

- `ArithmeticException`: A component is NaN.

<a id="m-electron2d-vector4-snapped-electron2d-vector4"></a>
### `public Vector4 Snapped(Vector4 step)`

Snaps each component to the nearest multiple of the corresponding step.

**Parameters**

- `step`: The componentwise step. A zero component leaves the corresponding value unchanged.

**Returns:** The snapped vector.

<a id="m-electron2d-vector4-snapped-system-single"></a>
### `public Vector4 Snapped(float step)`

Snaps every component to the nearest multiple of a scalar step.

**Parameters**

- `step`: The scalar step. Zero leaves every value unchanged.

**Returns:** The snapped vector.

<a id="m-electron2d-vector4-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an exactly equal vector.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is a vector with equal components.

<a id="m-electron2d-vector4-equals-electron2d-vector4"></a>
### `public bool Equals(Vector4 other)`

Tests every component for exact equality.

**Parameters**

- `other`: The vector to compare.

**Returns:** `true` when all corresponding components are equal.

<a id="m-electron2d-vector4-isequalapprox-electron2d-vector4"></a>
### `public bool IsEqualApprox(Vector4 other)`

Tests every component for scale-aware approximate equality.

**Parameters**

- `other`: The vector to compare.

**Returns:** `true` when all corresponding components are approximately equal.

<a id="m-electron2d-vector4-iszeroapprox"></a>
### `public bool IsZeroApprox()`

Tests whether every component is approximately zero.

**Returns:** `true` when every component is within the zero tolerance.

<a id="m-electron2d-vector4-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on all components.

**Returns:** The component hash code.

<a id="m-electron2d-vector4-tostring"></a>
### `public override string ToString()`

Formats every component using invariant culture.

**Returns:** A parenthesized component tuple.

<a id="m-electron2d-vector4-tostring-system-string"></a>
### `public string ToString(string format)`

Formats every component with a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default.

**Returns:** A parenthesized component tuple.

**Exceptions**

- `FormatException`: `format` is invalid.

## Enumeration Descriptions

<a id="t-electron2d-vector4-axis"></a>
### `public enum Vector4.Axis`

Identifies one vector component.

## Constant Descriptions

<a id="f-electron2d-vector4-axis-x"></a>
### `Vector4.Axis.X = 0`

Identifies the X component.

<a id="f-electron2d-vector4-axis-y"></a>
### `Vector4.Axis.Y = 1`

Identifies the Y component.

<a id="f-electron2d-vector4-axis-z"></a>
### `Vector4.Axis.Z = 2`

Identifies the Z component.

<a id="f-electron2d-vector4-axis-w"></a>
### `Vector4.Axis.W = 3`

Identifies the W component.

## Field Descriptions

<a id="f-electron2d-vector4-x"></a>
### `public float X`

Gets or sets the X component.

<a id="f-electron2d-vector4-y"></a>
### `public float Y`

Gets or sets the Y component.

<a id="f-electron2d-vector4-z"></a>
### `public float Z`

Gets or sets the Z component.

<a id="f-electron2d-vector4-w"></a>
### `public float W`

Gets or sets the W component.

## Operator Descriptions

<a id="m-electron2d-vector4-op-addition-electron2d-vector4-electron2d-vector4"></a>
### `public static Vector4 operator +(Vector4 left, Vector4 right)`

Adds two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The componentwise sum.

<a id="m-electron2d-vector4-op-unaryplus-electron2d-vector4"></a>
### `public static Vector4 operator +(Vector4 value)`

Returns a vector unchanged.

**Parameters**

- `value`: The vector.

**Returns:** `value`.

<a id="m-electron2d-vector4-op-subtraction-electron2d-vector4-electron2d-vector4"></a>
### `public static Vector4 operator -(Vector4 left, Vector4 right)`

Subtracts two vectors componentwise.

**Parameters**

- `left`: The minuend.
- `right`: The subtrahend.

**Returns:** The componentwise difference.

<a id="m-electron2d-vector4-op-unarynegation-electron2d-vector4"></a>
### `public static Vector4 operator -(Vector4 value)`

Negates every component.

**Parameters**

- `value`: The vector to negate.

**Returns:** The componentwise negation.

<a id="m-electron2d-vector4-op-multiply-electron2d-vector4-system-single"></a>
### `public static Vector4 operator *(Vector4 vector, float scalar)`

Multiplies a vector by a scalar.

**Parameters**

- `vector`: The vector.
- `scalar`: The scalar multiplier.

**Returns:** The componentwise product.

<a id="m-electron2d-vector4-op-multiply-system-single-electron2d-vector4"></a>
### `public static Vector4 operator *(float scalar, Vector4 vector)`

Multiplies a scalar by a vector.

**Parameters**

- `scalar`: The scalar multiplier.
- `vector`: The vector.

**Returns:** The componentwise product.

<a id="m-electron2d-vector4-op-multiply-electron2d-vector4-electron2d-vector4"></a>
### `public static Vector4 operator *(Vector4 left, Vector4 right)`

Multiplies two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The componentwise product.

<a id="m-electron2d-vector4-op-division-electron2d-vector4-system-single"></a>
### `public static Vector4 operator /(Vector4 vector, float divisor)`

Divides every component by a scalar.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The IEEE 754 componentwise quotient.

<a id="m-electron2d-vector4-op-division-electron2d-vector4-electron2d-vector4"></a>
### `public static Vector4 operator /(Vector4 left, Vector4 right)`

Divides two vectors componentwise.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The IEEE 754 componentwise quotient.

<a id="m-electron2d-vector4-op-modulus-electron2d-vector4-system-single"></a>
### `public static Vector4 operator %(Vector4 vector, float divisor)`

Returns the truncated remainder of every component by a scalar.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The componentwise remainder.

<a id="m-electron2d-vector4-op-modulus-electron2d-vector4-electron2d-vector4"></a>
### `public static Vector4 operator %(Vector4 left, Vector4 right)`

Returns the truncated componentwise remainder of two vectors.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The componentwise remainder.

<a id="m-electron2d-vector4-op-equality-electron2d-vector4-electron2d-vector4"></a>
### `public static bool operator ==(Vector4 left, Vector4 right)`

Tests every component for exact equality.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when all corresponding components are equal.

<a id="m-electron2d-vector4-op-inequality-electron2d-vector4-electron2d-vector4"></a>
### `public static bool operator !=(Vector4 left, Vector4 right)`

Tests whether any component differs.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when a corresponding component differs.

<a id="m-electron2d-vector4-op-lessthan-electron2d-vector4-electron2d-vector4"></a>
### `public static bool operator <(Vector4 left, Vector4 right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts before `right`.

<a id="m-electron2d-vector4-op-greaterthan-electron2d-vector4-electron2d-vector4"></a>
### `public static bool operator >(Vector4 left, Vector4 right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts after `right`.

<a id="m-electron2d-vector4-op-lessthanorequal-electron2d-vector4-electron2d-vector4"></a>
### `public static bool operator <=(Vector4 left, Vector4 right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort after `right`.

<a id="m-electron2d-vector4-op-greaterthanorequal-electron2d-vector4-electron2d-vector4"></a>
### `public static bool operator >=(Vector4 left, Vector4 right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort before `right`.

## Numeric invariants and error behavior

- Arithmetic retains IEEE 754 behavior. Zero scalar/component division produces infinity or NaN; zero remainder or positive modulus produces NaN; reciprocal preserves signed zero through signed infinity.
- Exact zero normalization and equal-point direction return `Zero`. Non-finite normalization follows ordinary managed floating-point propagation. `IsNormalized` uses tolerance `0.001`; approximate component predicates use the internal tolerance (`1e-6f`) with exact equality first.
- Maximum-axis ties choose the first maximum; minimum-axis ties choose the last minimum. NaN is skipped by ordered comparisons, and an initial NaN therefore keeps X.
- Relational operators compare X, then Y, then Z, then W directly. If the first differing component is NaN, all four relational results are false; no artificial total ordering is introduced.
- `Round` is midpoint-to-even. `Snapped` uses `floor(value / step + 0.5) * step`; zero steps preserve components. `Sign` throws `ArithmeticException` for NaN.
- Clamp throws `ArgumentException` for reversed bounds; the indexer throws `ArgumentOutOfRangeException`; invalid formats throw `FormatException`.

## Lifecycle, threading, dependencies, and allocation

Copies are independent, with no state transition. Numeric operations allocate no managed memory after warmup; formatting allocates. Independent copies are thread-safe to read or mutate independently; shared writes are unsynchronized.

The type depends on canonical scalar [`MathF`](MathF.md), formatting/layout primitives, and [`Vector4I`](Vector4I.md). [`ConfigFile`](ConfigFile.md) accepts only finite values and persists exact `X/Y/Z/W` fields. [`PackedScene`](PackedScene.md) stores the value directly.

## Coverage, verification, and limitations

All pure value behavior from the audited reference API is implemented. Multiplication by a 3D projection matrix is permanently excluded because Electron2D has no 3D projection type. Universal-value truth conversion is excluded by the typed C# architecture. No renderer or shader binding exists yet, so this type makes no GPU integration claim.

The executable harness covers layout, constants, indexing, conversions, every method/operator family, interpolation, zero/non-finite behavior, NaN ordering, axis ties, formatting, strict persistence, packed-scene storage, and warmed allocation-free math. Execution is Linux/.NET 8 only.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
