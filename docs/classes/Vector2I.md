# Vector2I

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Vector2I.cs`](../../src/Core/Math/Vector2I.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Vector2I`

> Represents a two-component integer vector for pixels, grids, tile coordinates, and integer pairs.

## Description

Represents a two-component integer vector for pixels, grids, tile coordinates, and integer pairs.

`Vector2I` is the engine-owned mutable two-component 32-bit integer value for pixels, grid and tile coordinates, texture dimensions, chunk addresses, integer pairs, and [`RectI`](RectI.md) geometry. Sequential X/Y layout is verified as 8 bytes. It owns no resources or lifecycle.

Arithmetic uses 32-bit signed integers. Addition, subtraction, multiplication, and negation wrap on overflow.
Division and remainder follow C# truncated-division rules. The zero-initialized value is [`Vector2I.Zero`](Vector2I.md#p-electron2d-vector2i-zero).
Squared norms return signed 64-bit integers and reject results outside that range. Length and distance use widened floating-point arithmetic and remain finite for all 32-bit components.
Numeric operations do not allocate managed memory; string formatting allocates a string.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var cell = new Vector2I(4, 7);
var neighbor = cell + Vector2I.Right;
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Vector2I(int x, int y)`](#m-electron2d-vector2i-ctor-system-int32-system-int32) | Initializes an integer vector from horizontal and vertical components. |
| [`public Vector2I(Vector2 value)`](#m-electron2d-vector2i-ctor-electron2d-vector2) | Initializes an integer vector by truncating a finite floating-point vector toward zero. |

## Properties

| Member | Description |
| --- | --- |
| [`public static Vector2I MinValue { get; }`](#p-electron2d-vector2i-minvalue) | Gets the vector containing the minimum 32-bit integer in both components. |
| [`public static Vector2I MaxValue { get; }`](#p-electron2d-vector2i-maxvalue) | Gets the vector containing the maximum 32-bit integer in both components. |
| [`public static Vector2I Zero { get; }`](#p-electron2d-vector2i-zero) | Gets the zero vector. |
| [`public static Vector2I One { get; }`](#p-electron2d-vector2i-one) | Gets the vector whose components are both one. |
| [`public static Vector2I Up { get; }`](#p-electron2d-vector2i-up) | Gets the upward screen-space unit vector. |
| [`public static Vector2I Down { get; }`](#p-electron2d-vector2i-down) | Gets the downward screen-space unit vector. |
| [`public static Vector2I Right { get; }`](#p-electron2d-vector2i-right) | Gets the rightward unit vector. |
| [`public static Vector2I Left { get; }`](#p-electron2d-vector2i-left) | Gets the leftward unit vector. |
| [`public int this[int index] { get; set; }`](#p-electron2d-vector2i-item-system-int32) | Gets or sets a component by axis index. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Deconstruct(out int x, out int y)`](#m-electron2d-vector2i-deconstruct-system-int32-byref-system-int32-byref) | Deconstructs the vector into its two components. |
| [`public Vector2I Abs()`](#m-electron2d-vector2i-abs) | Returns a vector containing the absolute value of each component. |
| [`public float Aspect()`](#m-electron2d-vector2i-aspect) | Returns the ratio of the horizontal component to the vertical component. |
| [`public Vector2I Clamp(Vector2I min, Vector2I max)`](#m-electron2d-vector2i-clamp-electron2d-vector2i-electron2d-vector2i) | Clamps each component between corresponding vector bounds. |
| [`public Vector2I Clamp(int min, int max)`](#m-electron2d-vector2i-clamp-system-int32-system-int32) | Clamps both components between scalar bounds. |
| [`public long DistanceSquaredTo(Vector2I to)`](#m-electron2d-vector2i-distancesquaredto-electron2d-vector2i) | Returns the squared Euclidean distance to another point. |
| [`public float DistanceTo(Vector2I to)`](#m-electron2d-vector2i-distanceto-electron2d-vector2i) | Returns the Euclidean distance to another point. |
| [`public float Length()`](#m-electron2d-vector2i-length) | Returns the Euclidean length. |
| [`public long LengthSquared()`](#m-electron2d-vector2i-lengthsquared) | Returns the squared Euclidean length. |
| [`public Vector2I Max(Vector2I with)`](#m-electron2d-vector2i-max-electron2d-vector2i) | Returns the componentwise maximum with another vector. |
| [`public Vector2I Max(int with)`](#m-electron2d-vector2i-max-system-int32) | Returns the componentwise maximum with a scalar. |
| [`public Vector2I.Axis MaxAxisIndex()`](#m-electron2d-vector2i-maxaxisindex) | Returns the axis containing the greatest component. |
| [`public Vector2I Min(Vector2I with)`](#m-electron2d-vector2i-min-electron2d-vector2i) | Returns the componentwise minimum with another vector. |
| [`public Vector2I Min(int with)`](#m-electron2d-vector2i-min-system-int32) | Returns the componentwise minimum with a scalar. |
| [`public Vector2I.Axis MinAxisIndex()`](#m-electron2d-vector2i-minaxisindex) | Returns the axis containing the least component. |
| [`public Vector2I Sign()`](#m-electron2d-vector2i-sign) | Returns the sign of each component. |
| [`public Vector2I Snapped(Vector2I step)`](#m-electron2d-vector2i-snapped-electron2d-vector2i) | Snaps each component to the nearest multiple of the corresponding step. |
| [`public Vector2I Snapped(int step)`](#m-electron2d-vector2i-snapped-system-int32) | Snaps both components to the nearest multiple of a scalar step. |
| [`public override bool Equals(object obj)`](#m-electron2d-vector2i-equals-system-object) | Tests whether another object is an equal integer vector. |
| [`public bool Equals(Vector2I other)`](#m-electron2d-vector2i-equals-electron2d-vector2i) | Tests both components for exact equality. |
| [`public override int GetHashCode()`](#m-electron2d-vector2i-gethashcode) | Returns a hash code based on both components. |
| [`public override string ToString()`](#m-electron2d-vector2i-tostring) | Formats both components using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-vector2i-tostring-system-string) | Formats both components with a numeric format and invariant culture. |

## Enumerations

| Member | Description |
| --- | --- |
| [`public enum Vector2I.Axis`](#t-electron2d-vector2i-axis) | Identifies one vector component. |

## Constants

| Member | Description |
| --- | --- |
| [`Vector2I.Axis.X = 0`](#f-electron2d-vector2i-axis-x) | Identifies the horizontal X component. |
| [`Vector2I.Axis.Y = 1`](#f-electron2d-vector2i-axis-y) | Identifies the vertical Y component. |

## Fields

| Member | Description |
| --- | --- |
| [`public int X`](#f-electron2d-vector2i-x) | Gets or sets the horizontal component. |
| [`public int Y`](#f-electron2d-vector2i-y) | Gets or sets the vertical component. |

## Operators

| Member | Description |
| --- | --- |
| [`public static Vector2I operator +(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-addition-electron2d-vector2i-electron2d-vector2i) | Adds two vectors componentwise. |
| [`public static Vector2I operator +(Vector2I value)`](#m-electron2d-vector2i-op-unaryplus-electron2d-vector2i) | Returns a vector unchanged. |
| [`public static Vector2I operator -(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-subtraction-electron2d-vector2i-electron2d-vector2i) | Subtracts two vectors componentwise. |
| [`public static Vector2I operator -(Vector2I value)`](#m-electron2d-vector2i-op-unarynegation-electron2d-vector2i) | Negates both components. |
| [`public static Vector2I operator *(Vector2I vector, int scalar)`](#m-electron2d-vector2i-op-multiply-electron2d-vector2i-system-int32) | Multiplies a vector by an integer scalar. |
| [`public static Vector2I operator *(int scalar, Vector2I vector)`](#m-electron2d-vector2i-op-multiply-system-int32-electron2d-vector2i) | Multiplies an integer scalar by a vector. |
| [`public static Vector2 operator *(Vector2I vector, float scalar)`](#m-electron2d-vector2i-op-multiply-electron2d-vector2i-system-single) | Multiplies an integer vector by a floating-point scalar. |
| [`public static Vector2 operator *(float scalar, Vector2I vector)`](#m-electron2d-vector2i-op-multiply-system-single-electron2d-vector2i) | Multiplies a floating-point scalar by an integer vector. |
| [`public static Vector2I operator *(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-multiply-electron2d-vector2i-electron2d-vector2i) | Multiplies two vectors componentwise. |
| [`public static Vector2I operator /(Vector2I vector, int divisor)`](#m-electron2d-vector2i-op-division-electron2d-vector2i-system-int32) | Divides both components by an integer scalar using truncated division. |
| [`public static Vector2 operator /(Vector2I vector, float divisor)`](#m-electron2d-vector2i-op-division-electron2d-vector2i-system-single) | Divides an integer vector by a floating-point scalar. |
| [`public static Vector2I operator /(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-division-electron2d-vector2i-electron2d-vector2i) | Divides two vectors componentwise using truncated division. |
| [`public static Vector2I operator %(Vector2I vector, int divisor)`](#m-electron2d-vector2i-op-modulus-electron2d-vector2i-system-int32) | Returns the truncated remainder of both components by an integer scalar. |
| [`public static Vector2I operator %(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-modulus-electron2d-vector2i-electron2d-vector2i) | Returns the truncated componentwise remainder of two vectors. |
| [`public static bool operator ==(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-equality-electron2d-vector2i-electron2d-vector2i) | Tests both components for exact equality. |
| [`public static bool operator !=(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-inequality-electron2d-vector2i-electron2d-vector2i) | Tests whether either component differs. |
| [`public static bool operator <(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-lessthan-electron2d-vector2i-electron2d-vector2i) | Compares vectors lexicographically by X and then Y. |
| [`public static bool operator >(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-greaterthan-electron2d-vector2i-electron2d-vector2i) | Compares vectors lexicographically by X and then Y. |
| [`public static bool operator <=(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-lessthanorequal-electron2d-vector2i-electron2d-vector2i) | Compares vectors lexicographically by X and then Y. |
| [`public static bool operator >=(Vector2I left, Vector2I right)`](#m-electron2d-vector2i-op-greaterthanorequal-electron2d-vector2i-electron2d-vector2i) | Compares vectors lexicographically by X and then Y. |
| [`public static Vector2 operator implicit(Vector2I value)`](#m-electron2d-vector2i-op-implicit-electron2d-vector2i-electron2d-vector2) | Converts an integer vector to a floating-point vector. |
| [`public static Vector2I operator explicit(Vector2 value)`](#m-electron2d-vector2i-op-explicit-electron2d-vector2-electron2d-vector2i) | Converts a finite in-range floating-point vector by truncating each component toward zero. |

## Constructor Descriptions

<a id="m-electron2d-vector2i-ctor-system-int32-system-int32"></a>
### `public Vector2I(int x, int y)`

Initializes an integer vector from horizontal and vertical components.

**Parameters**

- `x`: The horizontal component.
- `y`: The vertical component.

<a id="m-electron2d-vector2i-ctor-electron2d-vector2"></a>
### `public Vector2I(Vector2 value)`

Initializes an integer vector by truncating a finite floating-point vector toward zero.

**Parameters**

- `value`: The floating-point vector to convert.

**Exceptions**

- `ArgumentOutOfRangeException`: A component is not finite or is outside the 32-bit signed integer range.

## Property Descriptions

<a id="p-electron2d-vector2i-minvalue"></a>
### `public static Vector2I MinValue { get; }`

Gets the vector containing the minimum 32-bit integer in both components.

**Value:** `(int.MinValue, int.MinValue)`.

<a id="p-electron2d-vector2i-maxvalue"></a>
### `public static Vector2I MaxValue { get; }`

Gets the vector containing the maximum 32-bit integer in both components.

**Value:** `(int.MaxValue, int.MaxValue)`.

<a id="p-electron2d-vector2i-zero"></a>
### `public static Vector2I Zero { get; }`

Gets the zero vector.

**Value:** `(0, 0)`.

<a id="p-electron2d-vector2i-one"></a>
### `public static Vector2I One { get; }`

Gets the vector whose components are both one.

**Value:** `(1, 1)`.

<a id="p-electron2d-vector2i-up"></a>
### `public static Vector2I Up { get; }`

Gets the upward screen-space unit vector.

**Value:** `(0, -1)`.

<a id="p-electron2d-vector2i-down"></a>
### `public static Vector2I Down { get; }`

Gets the downward screen-space unit vector.

**Value:** `(0, 1)`.

<a id="p-electron2d-vector2i-right"></a>
### `public static Vector2I Right { get; }`

Gets the rightward unit vector.

**Value:** `(1, 0)`.

<a id="p-electron2d-vector2i-left"></a>
### `public static Vector2I Left { get; }`

Gets the leftward unit vector.

**Value:** `(-1, 0)`.

<a id="p-electron2d-vector2i-item-system-int32"></a>
### `public int this[int index] { get; set; }`

Gets or sets a component by axis index.

**Parameters**

- `index`: Zero for [`Vector2I.X`](Vector2I.md#f-electron2d-vector2i-x) or one for [`Vector2I.Y`](Vector2I.md#f-electron2d-vector2i-y).

**Value:** The selected component.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is not zero or one.

## Method Descriptions

<a id="m-electron2d-vector2i-deconstruct-system-int32-byref-system-int32-byref"></a>
### `public void Deconstruct(out int x, out int y)`

Deconstructs the vector into its two components.

**Parameters**

- `x`: Receives [`Vector2I.X`](Vector2I.md#f-electron2d-vector2i-x).
- `y`: Receives [`Vector2I.Y`](Vector2I.md#f-electron2d-vector2i-y).

<a id="m-electron2d-vector2i-abs"></a>
### `public Vector2I Abs()`

Returns a vector containing the absolute value of each component.

**Returns:** The componentwise absolute value.

**Exceptions**

- `OverflowException`: A component is `Int32.MinValue`.

<a id="m-electron2d-vector2i-aspect"></a>
### `public float Aspect()`

Returns the ratio of the horizontal component to the vertical component.

**Returns:** `X / Y` as a floating-point value, including IEEE 754 zero-division behavior.

<a id="m-electron2d-vector2i-clamp-electron2d-vector2i-electron2d-vector2i"></a>
### `public Vector2I Clamp(Vector2I min, Vector2I max)`

Clamps each component between corresponding vector bounds.

**Parameters**

- `min`: The componentwise lower bounds.
- `max`: The componentwise upper bounds.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: A lower bound is greater than its corresponding upper bound.

<a id="m-electron2d-vector2i-clamp-system-int32-system-int32"></a>
### `public Vector2I Clamp(int min, int max)`

Clamps both components between scalar bounds.

**Parameters**

- `min`: The lower bound.
- `max`: The upper bound.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-vector2i-distancesquaredto-electron2d-vector2i"></a>
### `public long DistanceSquaredTo(Vector2I to)`

Returns the squared Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The exact squared distance when it fits in a signed 64-bit integer. Component differences are widened before subtraction, so they do not use wrapping vector subtraction.

**Exceptions**

- `OverflowException`: The squared distance exceeds `long.MaxValue`.

<a id="m-electron2d-vector2i-distanceto-electron2d-vector2i"></a>
### `public float DistanceTo(Vector2I to)`

Returns the Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The nonnegative distance rounded to single precision. Widened coordinate differences and floating-point squares keep it finite even when [`Vector2I.DistanceSquaredTo(Vector2I)`](Vector2I.md#m-electron2d-vector2i-distancesquaredto-electron2d-vector2i) exceeds `long.MaxValue`.

<a id="m-electron2d-vector2i-length"></a>
### `public float Length()`

Returns the Euclidean length.

**Returns:** The nonnegative length rounded to single precision. Widened floating-point squares keep it finite even when [`Vector2I.LengthSquared`](Vector2I.md#m-electron2d-vector2i-lengthsquared) exceeds `long.MaxValue`.

<a id="m-electron2d-vector2i-lengthsquared"></a>
### `public long LengthSquared()`

Returns the squared Euclidean length.

**Returns:** The exact `X * X + Y * Y` when it fits in a signed 64-bit integer; components are widened before multiplication.

**Exceptions**

- `OverflowException`: The squared length exceeds `long.MaxValue`.

<a id="m-electron2d-vector2i-max-electron2d-vector2i"></a>
### `public Vector2I Max(Vector2I with)`

Returns the componentwise maximum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector2i-max-system-int32"></a>
### `public Vector2I Max(int with)`

Returns the componentwise maximum with a scalar.

**Parameters**

- `with`: The scalar compared with both components.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector2i-maxaxisindex"></a>
### `public Vector2I.Axis MaxAxisIndex()`

Returns the axis containing the greatest component.

**Returns:** [`Vector2I.Axis.X`](Vector2I.md#f-electron2d-vector2i-axis-x) when components are equal; otherwise the greatest component's axis.

<a id="m-electron2d-vector2i-min-electron2d-vector2i"></a>
### `public Vector2I Min(Vector2I with)`

Returns the componentwise minimum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector2i-min-system-int32"></a>
### `public Vector2I Min(int with)`

Returns the componentwise minimum with a scalar.

**Parameters**

- `with`: The scalar compared with both components.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector2i-minaxisindex"></a>
### `public Vector2I.Axis MinAxisIndex()`

Returns the axis containing the least component.

**Returns:** [`Vector2I.Axis.Y`](Vector2I.md#f-electron2d-vector2i-axis-y) when components are equal; otherwise the least component's axis.

<a id="m-electron2d-vector2i-sign"></a>
### `public Vector2I Sign()`

Returns the sign of each component.

**Returns:** Components containing negative one, zero, or positive one.

<a id="m-electron2d-vector2i-snapped-electron2d-vector2i"></a>
### `public Vector2I Snapped(Vector2I step)`

Snaps each component to the nearest multiple of the corresponding step.

**Parameters**

- `step`: The componentwise step. A zero component leaves the corresponding value unchanged.

**Returns:** The snapped vector.

Midpoint ties go toward larger values for a positive step and smaller values for a negative step.

**Exceptions**

- `OverflowException`: A snapped component is outside the 32-bit signed integer range.

<a id="m-electron2d-vector2i-snapped-system-int32"></a>
### `public Vector2I Snapped(int step)`

Snaps both components to the nearest multiple of a scalar step.

**Parameters**

- `step`: The scalar step. Zero leaves both values unchanged.

**Returns:** The snapped vector.

Midpoint ties go toward larger values for a positive step and smaller values for a negative step.

**Exceptions**

- `OverflowException`: A snapped component is outside the 32-bit signed integer range.

<a id="m-electron2d-vector2i-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an equal integer vector.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is an integer vector with equal components.

<a id="m-electron2d-vector2i-equals-electron2d-vector2i"></a>
### `public bool Equals(Vector2I other)`

Tests both components for exact equality.

**Parameters**

- `other`: The vector to compare.

**Returns:** `true` when both corresponding components are equal.

<a id="m-electron2d-vector2i-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on both components.

**Returns:** The component hash code.

<a id="m-electron2d-vector2i-tostring"></a>
### `public override string ToString()`

Formats both components using invariant culture.

**Returns:** A parenthesized component pair.

<a id="m-electron2d-vector2i-tostring-system-string"></a>
### `public string ToString(string format)`

Formats both components with a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default format.

**Returns:** A parenthesized component pair.

**Exceptions**

- `FormatException`: `format` is invalid.

## Enumeration Descriptions

<a id="t-electron2d-vector2i-axis"></a>
### `public enum Vector2I.Axis`

Identifies one vector component.

## Constant Descriptions

<a id="f-electron2d-vector2i-axis-x"></a>
### `Vector2I.Axis.X = 0`

Identifies the horizontal X component.

<a id="f-electron2d-vector2i-axis-y"></a>
### `Vector2I.Axis.Y = 1`

Identifies the vertical Y component.

## Field Descriptions

<a id="f-electron2d-vector2i-x"></a>
### `public int X`

Gets or sets the horizontal component.

<a id="f-electron2d-vector2i-y"></a>
### `public int Y`

Gets or sets the vertical component.

## Operator Descriptions

<a id="m-electron2d-vector2i-op-addition-electron2d-vector2i-electron2d-vector2i"></a>
### `public static Vector2I operator +(Vector2I left, Vector2I right)`

Adds two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The wrapping componentwise sum.

<a id="m-electron2d-vector2i-op-unaryplus-electron2d-vector2i"></a>
### `public static Vector2I operator +(Vector2I value)`

Returns a vector unchanged.

**Parameters**

- `value`: The vector.

**Returns:** `value`.

<a id="m-electron2d-vector2i-op-subtraction-electron2d-vector2i-electron2d-vector2i"></a>
### `public static Vector2I operator -(Vector2I left, Vector2I right)`

Subtracts two vectors componentwise.

**Parameters**

- `left`: The minuend.
- `right`: The subtrahend.

**Returns:** The wrapping componentwise difference.

<a id="m-electron2d-vector2i-op-unarynegation-electron2d-vector2i"></a>
### `public static Vector2I operator -(Vector2I value)`

Negates both components.

**Parameters**

- `value`: The vector to negate.

**Returns:** The wrapping componentwise negation.

<a id="m-electron2d-vector2i-op-multiply-electron2d-vector2i-system-int32"></a>
### `public static Vector2I operator *(Vector2I vector, int scalar)`

Multiplies a vector by an integer scalar.

**Parameters**

- `vector`: The vector.
- `scalar`: The scalar multiplier.

**Returns:** The wrapping componentwise product.

<a id="m-electron2d-vector2i-op-multiply-system-int32-electron2d-vector2i"></a>
### `public static Vector2I operator *(int scalar, Vector2I vector)`

Multiplies an integer scalar by a vector.

**Parameters**

- `scalar`: The scalar multiplier.
- `vector`: The vector.

**Returns:** The wrapping componentwise product.

<a id="m-electron2d-vector2i-op-multiply-electron2d-vector2i-system-single"></a>
### `public static Vector2 operator *(Vector2I vector, float scalar)`

Multiplies an integer vector by a floating-point scalar.

**Parameters**

- `vector`: The integer vector.
- `scalar`: The floating-point multiplier.

**Returns:** A floating-point componentwise product.

<a id="m-electron2d-vector2i-op-multiply-system-single-electron2d-vector2i"></a>
### `public static Vector2 operator *(float scalar, Vector2I vector)`

Multiplies a floating-point scalar by an integer vector.

**Parameters**

- `scalar`: The floating-point multiplier.
- `vector`: The integer vector.

**Returns:** A floating-point componentwise product.

<a id="m-electron2d-vector2i-op-multiply-electron2d-vector2i-electron2d-vector2i"></a>
### `public static Vector2I operator *(Vector2I left, Vector2I right)`

Multiplies two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The wrapping componentwise product.

<a id="m-electron2d-vector2i-op-division-electron2d-vector2i-system-int32"></a>
### `public static Vector2I operator /(Vector2I vector, int divisor)`

Divides both components by an integer scalar using truncated division.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The componentwise quotient.

**Exceptions**

- `DivideByZeroException`: `divisor` is zero.
- `OverflowException`: A component is `Int32.MinValue` and `divisor` is negative one.

<a id="m-electron2d-vector2i-op-division-electron2d-vector2i-system-single"></a>
### `public static Vector2 operator /(Vector2I vector, float divisor)`

Divides an integer vector by a floating-point scalar.

**Parameters**

- `vector`: The integer dividend.
- `divisor`: The floating-point divisor.

**Returns:** The IEEE 754 floating-point componentwise quotient.

<a id="m-electron2d-vector2i-op-division-electron2d-vector2i-electron2d-vector2i"></a>
### `public static Vector2I operator /(Vector2I left, Vector2I right)`

Divides two vectors componentwise using truncated division.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The componentwise quotient.

**Exceptions**

- `DivideByZeroException`: A component of `right` is zero.
- `OverflowException`: A dividend component is `Int32.MinValue` and its divisor is negative one.

<a id="m-electron2d-vector2i-op-modulus-electron2d-vector2i-system-int32"></a>
### `public static Vector2I operator %(Vector2I vector, int divisor)`

Returns the truncated remainder of both components by an integer scalar.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The componentwise remainder.

**Exceptions**

- `DivideByZeroException`: `divisor` is zero.
- `OverflowException`: A component is `Int32.MinValue` and `divisor` is negative one.

<a id="m-electron2d-vector2i-op-modulus-electron2d-vector2i-electron2d-vector2i"></a>
### `public static Vector2I operator %(Vector2I left, Vector2I right)`

Returns the truncated componentwise remainder of two vectors.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The componentwise remainder.

**Exceptions**

- `DivideByZeroException`: A component of `right` is zero.
- `OverflowException`: A minimum-valued dividend component has a divisor of negative one.

<a id="m-electron2d-vector2i-op-equality-electron2d-vector2i-electron2d-vector2i"></a>
### `public static bool operator ==(Vector2I left, Vector2I right)`

Tests both components for exact equality.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when both corresponding components are equal.

<a id="m-electron2d-vector2i-op-inequality-electron2d-vector2i-electron2d-vector2i"></a>
### `public static bool operator !=(Vector2I left, Vector2I right)`

Tests whether either component differs.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when at least one corresponding component differs.

<a id="m-electron2d-vector2i-op-lessthan-electron2d-vector2i-electron2d-vector2i"></a>
### `public static bool operator <(Vector2I left, Vector2I right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts before `right`.

<a id="m-electron2d-vector2i-op-greaterthan-electron2d-vector2i-electron2d-vector2i"></a>
### `public static bool operator >(Vector2I left, Vector2I right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts after `right`.

<a id="m-electron2d-vector2i-op-lessthanorequal-electron2d-vector2i-electron2d-vector2i"></a>
### `public static bool operator <=(Vector2I left, Vector2I right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort after `right`.

<a id="m-electron2d-vector2i-op-greaterthanorequal-electron2d-vector2i-electron2d-vector2i"></a>
### `public static bool operator >=(Vector2I left, Vector2I right)`

Compares vectors lexicographically by X and then Y.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort before `right`.

<a id="m-electron2d-vector2i-op-implicit-electron2d-vector2i-electron2d-vector2"></a>
### `public static Vector2 operator implicit(Vector2I value)`

Converts an integer vector to a floating-point vector.

**Parameters**

- `value`: The integer vector to convert.

**Returns:** A floating-point vector with corresponding components.

**Remarks:** Large integer components can lose low-order precision.

<a id="m-electron2d-vector2i-op-explicit-electron2d-vector2-electron2d-vector2i"></a>
### `public static Vector2I operator explicit(Vector2 value)`

Converts a finite in-range floating-point vector by truncating each component toward zero.

**Parameters**

- `value`: The floating-point vector to convert.

**Returns:** The truncated integer vector.

**Exceptions**

- `ArgumentOutOfRangeException`: A component is not finite or is outside the 32-bit signed integer range.

## Numeric invariants and error behavior

- Addition, subtraction, multiplication, and negation explicitly wrap in 32-bit two's-complement arithmetic. Squared length and distance widen before multiplication: `(50000, 0).LengthSquared()` is `2500000000L`, and its `Length()` is `50000f`. Squared results above `long.MaxValue` throw `OverflowException`; length and distance remain finite even at `int` endpoints.
- Integer division truncates toward zero; remainder has the dividend's sign. A zero scalar or component divisor throws `DivideByZeroException`; `int.MinValue / -1` and `int.MinValue % -1` throw `OverflowException`.
- `Abs` throws `OverflowException` for `int.MinValue`. Clamp overloads throw `ArgumentException` for reversed bounds. The indexer throws `ArgumentOutOfRangeException`.
- Maximum-axis ties choose X; minimum-axis ties choose Y. Snapping uses double intermediate arithmetic; midpoint ties go toward larger values for positive steps and smaller values for negative steps. Zero steps preserve the value and an out-of-range snapped result throws `OverflowException`.
- Float conversion truncates toward zero and rejects non-finite or out-of-range input with `ArgumentOutOfRangeException`. Conversion to `Vector2` can lose integer precision above 2^24.
- Invalid numeric formats throw `FormatException`.

## Lifecycle, threading, dependencies, and allocation

Copies are independent. Numeric operations allocate no managed memory after warmup; formatting allocates. Independent copies can be used concurrently; shared mutation is unsynchronized.

The type depends on canonical scalar [`Mathf`](Mathf.md) for snapping and scalar operations, plus formatting/layout primitives and its paired [`Vector2`](Vector2.md). [`RectI`](RectI.md) uses it for position, size, and integer geometry. [`ConfigFile`](ConfigFile.md) persists exactly two 32-bit integer fields; [`PackedScene`](PackedScene.md) stores it directly.

## Coverage, verification, and limitations

The complete implementable reference surface is present. Universal-value truth conversion is permanently excluded. There is no external-numerics conversion or 3D counterpart.

The executable harness covers layout, constants, indexing, construction/conversion, every method and operator family, widened norms and their overflow boundaries, wraparound, division failures, ordering, snapping, invariant formatting, strict persistence, packed-scene storage, and allocation-free warmed math. Execution is Linux/.NET 8 only.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
