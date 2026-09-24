# Vector4I

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Vector4I.cs`](../../src/Core/Math/Vector4I.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Vector4I`

> Represents a four-component integer vector for masks and numeric tuples.

## Description

Represents a four-component integer vector for masks and numeric tuples.

`Vector4I` is an engine-owned mutable four-component 32-bit integer tuple for masks, packed channels, integer parameter groups, and future typed GPU boundaries. It does not represent 3D or 4D scene geometry. Sequential X/Y/Z/W layout is verified as 16 bytes and owns no resources or lifecycle.

Addition, subtraction, multiplication, and negation use wrapping 32-bit arithmetic.
Division and remainder follow C# truncated-division rules. The zero-initialized value is [`Vector4I.Zero`](Vector4I.md#p-electron2d-vector4i-zero).
Squared norms return signed 64-bit integers and reject results outside that range. Length and distance use widened floating-point arithmetic and remain finite for all 32-bit components.
Numeric operations do not allocate managed memory; string formatting allocates a string.

All 52 declared members and the type row have managed behavioral audits on Linux/.NET 8. `VerifyVector4IValues` covers four-coordinate copies and mutation, enum identities, checked squared norms near `long.MaxValue`, full-span finite lengths, vector/scalar boundaries, first/last axis ties, negative and zero snapping steps, wrapping arithmetic, integer failure paths, floating conversion, strict configuration and packed-scene storage. Native ABI and other platforms remain unverified.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var margins = new Vector4I(8, 8, 16, 16);
var doubled = margins * 2;
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Vector4I(int x, int y, int z, int w)`](#m-electron2d-vector4i-ctor-system-int32-system-int32-system-int32-system-int32) | Initializes an integer vector from four components. |
| [`public Vector4I(Vector4 value)`](#m-electron2d-vector4i-ctor-electron2d-vector4) | Initializes an integer vector by truncating a finite floating-point vector toward zero. |

## Properties

| Member | Description |
| --- | --- |
| [`public static Vector4I MinValue { get; }`](#p-electron2d-vector4i-minvalue) | Gets the vector containing the minimum 32-bit integer in every component. |
| [`public static Vector4I MaxValue { get; }`](#p-electron2d-vector4i-maxvalue) | Gets the vector containing the maximum 32-bit integer in every component. |
| [`public static Vector4I Zero { get; }`](#p-electron2d-vector4i-zero) | Gets the zero vector. |
| [`public static Vector4I One { get; }`](#p-electron2d-vector4i-one) | Gets the vector whose components are all one. |
| [`public int this[int index] { get; set; }`](#p-electron2d-vector4i-item-system-int32) | Gets or sets a component by axis index. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Deconstruct(out int x, out int y, out int z, out int w)`](#m-electron2d-vector4i-deconstruct-system-int32-byref-system-int32-byref-system-int32-byref-system-int32-byref) | Deconstructs the vector into its four components. |
| [`public Vector4I Abs()`](#m-electron2d-vector4i-abs) | Returns the componentwise absolute value. |
| [`public Vector4I Clamp(Vector4I min, Vector4I max)`](#m-electron2d-vector4i-clamp-electron2d-vector4i-electron2d-vector4i) | Clamps each component between corresponding vector bounds. |
| [`public Vector4I Clamp(int min, int max)`](#m-electron2d-vector4i-clamp-system-int32-system-int32) | Clamps every component between scalar bounds. |
| [`public long DistanceSquaredTo(Vector4I to)`](#m-electron2d-vector4i-distancesquaredto-electron2d-vector4i) | Returns the squared Euclidean distance to another point. |
| [`public float DistanceTo(Vector4I to)`](#m-electron2d-vector4i-distanceto-electron2d-vector4i) | Returns the Euclidean distance to another point. |
| [`public float Length()`](#m-electron2d-vector4i-length) | Returns the Euclidean length. |
| [`public long LengthSquared()`](#m-electron2d-vector4i-lengthsquared) | Returns the squared Euclidean length. |
| [`public Vector4I Max(Vector4I with)`](#m-electron2d-vector4i-max-electron2d-vector4i) | Returns the componentwise maximum with another vector. |
| [`public Vector4I Max(int with)`](#m-electron2d-vector4i-max-system-int32) | Returns the componentwise maximum with a scalar. |
| [`public Vector4I.Axis MaxAxisIndex()`](#m-electron2d-vector4i-maxaxisindex) | Returns the axis containing the greatest component. |
| [`public Vector4I Min(Vector4I with)`](#m-electron2d-vector4i-min-electron2d-vector4i) | Returns the componentwise minimum with another vector. |
| [`public Vector4I Min(int with)`](#m-electron2d-vector4i-min-system-int32) | Returns the componentwise minimum with a scalar. |
| [`public Vector4I.Axis MinAxisIndex()`](#m-electron2d-vector4i-minaxisindex) | Returns the axis containing the least component. |
| [`public Vector4I Sign()`](#m-electron2d-vector4i-sign) | Returns the sign of every component. |
| [`public Vector4I Snapped(Vector4I step)`](#m-electron2d-vector4i-snapped-electron2d-vector4i) | Snaps each component to the nearest multiple of the corresponding step. |
| [`public Vector4I Snapped(int step)`](#m-electron2d-vector4i-snapped-system-int32) | Snaps every component to the nearest multiple of a scalar step. |
| [`public override bool Equals(object obj)`](#m-electron2d-vector4i-equals-system-object) | Tests whether another object is an equal integer vector. |
| [`public bool Equals(Vector4I other)`](#m-electron2d-vector4i-equals-electron2d-vector4i) | Tests every component for exact equality. |
| [`public override int GetHashCode()`](#m-electron2d-vector4i-gethashcode) | Returns a hash code based on all components. |
| [`public override string ToString()`](#m-electron2d-vector4i-tostring) | Formats every component using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-vector4i-tostring-system-string) | Formats every component with a numeric format and invariant culture. |

## Enumerations

| Member | Description |
| --- | --- |
| [`public enum Vector4I.Axis`](#t-electron2d-vector4i-axis) | Identifies one vector component. |

## Constants

| Member | Description |
| --- | --- |
| [`Vector4I.Axis.X = 0`](#f-electron2d-vector4i-axis-x) | Identifies the X component. |
| [`Vector4I.Axis.Y = 1`](#f-electron2d-vector4i-axis-y) | Identifies the Y component. |
| [`Vector4I.Axis.Z = 2`](#f-electron2d-vector4i-axis-z) | Identifies the Z component. |
| [`Vector4I.Axis.W = 3`](#f-electron2d-vector4i-axis-w) | Identifies the W component. |

## Fields

| Member | Description |
| --- | --- |
| [`public int X`](#f-electron2d-vector4i-x) | Gets or sets the X component. |
| [`public int Y`](#f-electron2d-vector4i-y) | Gets or sets the Y component. |
| [`public int Z`](#f-electron2d-vector4i-z) | Gets or sets the Z component. |
| [`public int W`](#f-electron2d-vector4i-w) | Gets or sets the W component. |

## Operators

| Member | Description |
| --- | --- |
| [`public static Vector4I operator +(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-addition-electron2d-vector4i-electron2d-vector4i) | Adds two vectors componentwise. |
| [`public static Vector4I operator +(Vector4I value)`](#m-electron2d-vector4i-op-unaryplus-electron2d-vector4i) | Returns a vector unchanged. |
| [`public static Vector4I operator -(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-subtraction-electron2d-vector4i-electron2d-vector4i) | Subtracts two vectors componentwise. |
| [`public static Vector4I operator -(Vector4I value)`](#m-electron2d-vector4i-op-unarynegation-electron2d-vector4i) | Negates every component. |
| [`public static Vector4I operator *(Vector4I vector, int scalar)`](#m-electron2d-vector4i-op-multiply-electron2d-vector4i-system-int32) | Multiplies a vector by an integer scalar. |
| [`public static Vector4I operator *(int scalar, Vector4I vector)`](#m-electron2d-vector4i-op-multiply-system-int32-electron2d-vector4i) | Multiplies an integer scalar by a vector. |
| [`public static Vector4 operator *(Vector4I vector, float scalar)`](#m-electron2d-vector4i-op-multiply-electron2d-vector4i-system-single) | Multiplies an integer vector by a floating-point scalar. |
| [`public static Vector4 operator *(float scalar, Vector4I vector)`](#m-electron2d-vector4i-op-multiply-system-single-electron2d-vector4i) | Multiplies a floating-point scalar by an integer vector. |
| [`public static Vector4I operator *(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-multiply-electron2d-vector4i-electron2d-vector4i) | Multiplies two vectors componentwise. |
| [`public static Vector4I operator /(Vector4I vector, int divisor)`](#m-electron2d-vector4i-op-division-electron2d-vector4i-system-int32) | Divides every component by an integer scalar using truncated division. |
| [`public static Vector4 operator /(Vector4I vector, float divisor)`](#m-electron2d-vector4i-op-division-electron2d-vector4i-system-single) | Divides an integer vector by a floating-point scalar. |
| [`public static Vector4I operator /(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-division-electron2d-vector4i-electron2d-vector4i) | Divides two vectors componentwise using truncated division. |
| [`public static Vector4I operator %(Vector4I vector, int divisor)`](#m-electron2d-vector4i-op-modulus-electron2d-vector4i-system-int32) | Returns the truncated remainder of every component by an integer scalar. |
| [`public static Vector4I operator %(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-modulus-electron2d-vector4i-electron2d-vector4i) | Returns the truncated componentwise remainder of two vectors. |
| [`public static bool operator ==(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-equality-electron2d-vector4i-electron2d-vector4i) | Tests every component for exact equality. |
| [`public static bool operator !=(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-inequality-electron2d-vector4i-electron2d-vector4i) | Tests whether any component differs. |
| [`public static bool operator <(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-lessthan-electron2d-vector4i-electron2d-vector4i) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static bool operator >(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-greaterthan-electron2d-vector4i-electron2d-vector4i) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static bool operator <=(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-lessthanorequal-electron2d-vector4i-electron2d-vector4i) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static bool operator >=(Vector4I left, Vector4I right)`](#m-electron2d-vector4i-op-greaterthanorequal-electron2d-vector4i-electron2d-vector4i) | Compares vectors lexicographically by X, Y, Z, then W. |
| [`public static Vector4 operator implicit(Vector4I value)`](#m-electron2d-vector4i-op-implicit-electron2d-vector4i-electron2d-vector4) | Converts an integer vector to a floating-point vector. |
| [`public static Vector4I operator explicit(Vector4 value)`](#m-electron2d-vector4i-op-explicit-electron2d-vector4-electron2d-vector4i) | Converts a finite in-range floating-point vector by truncating every component toward zero. |

## Constructor Descriptions

<a id="m-electron2d-vector4i-ctor-system-int32-system-int32-system-int32-system-int32"></a>
### `public Vector4I(int x, int y, int z, int w)`

Initializes an integer vector from four components.

**Parameters**

- `x`: The X component.
- `y`: The Y component.
- `z`: The Z component.
- `w`: The W component.

<a id="m-electron2d-vector4i-ctor-electron2d-vector4"></a>
### `public Vector4I(Vector4 value)`

Initializes an integer vector by truncating a finite floating-point vector toward zero.

**Parameters**

- `value`: The floating-point vector to convert.

**Exceptions**

- `ArgumentOutOfRangeException`: A component is not finite or is outside the 32-bit signed integer range.

## Property Descriptions

<a id="p-electron2d-vector4i-minvalue"></a>
### `public static Vector4I MinValue { get; }`

Gets the vector containing the minimum 32-bit integer in every component.

**Value:** `(int.MinValue, int.MinValue, int.MinValue, int.MinValue)`.

<a id="p-electron2d-vector4i-maxvalue"></a>
### `public static Vector4I MaxValue { get; }`

Gets the vector containing the maximum 32-bit integer in every component.

**Value:** `(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)`.

<a id="p-electron2d-vector4i-zero"></a>
### `public static Vector4I Zero { get; }`

Gets the zero vector.

**Value:** `(0, 0, 0, 0)`.

<a id="p-electron2d-vector4i-one"></a>
### `public static Vector4I One { get; }`

Gets the vector whose components are all one.

**Value:** `(1, 1, 1, 1)`.

<a id="p-electron2d-vector4i-item-system-int32"></a>
### `public int this[int index] { get; set; }`

Gets or sets a component by axis index.

**Parameters**

- `index`: An index from zero through three for X, Y, Z, or W.

**Value:** The selected component.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is outside zero through three.

## Method Descriptions

<a id="m-electron2d-vector4i-deconstruct-system-int32-byref-system-int32-byref-system-int32-byref-system-int32-byref"></a>
### `public void Deconstruct(out int x, out int y, out int z, out int w)`

Deconstructs the vector into its four components.

**Parameters**

- `x`: Receives [`Vector4I.X`](Vector4I.md#f-electron2d-vector4i-x).
- `y`: Receives [`Vector4I.Y`](Vector4I.md#f-electron2d-vector4i-y).
- `z`: Receives [`Vector4I.Z`](Vector4I.md#f-electron2d-vector4i-z).
- `w`: Receives [`Vector4I.W`](Vector4I.md#f-electron2d-vector4i-w).

<a id="m-electron2d-vector4i-abs"></a>
### `public Vector4I Abs()`

Returns the componentwise absolute value.

**Returns:** A vector with nonnegative components.

**Exceptions**

- `OverflowException`: A component is `Int32.MinValue`.

<a id="m-electron2d-vector4i-clamp-electron2d-vector4i-electron2d-vector4i"></a>
### `public Vector4I Clamp(Vector4I min, Vector4I max)`

Clamps each component between corresponding vector bounds.

**Parameters**

- `min`: The componentwise lower bounds.
- `max`: The componentwise upper bounds.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: A lower bound is greater than its corresponding upper bound.

<a id="m-electron2d-vector4i-clamp-system-int32-system-int32"></a>
### `public Vector4I Clamp(int min, int max)`

Clamps every component between scalar bounds.

**Parameters**

- `min`: The lower bound.
- `max`: The upper bound.

**Returns:** The clamped vector.

**Exceptions**

- `ArgumentException`: `min` is greater than `max`.

<a id="m-electron2d-vector4i-distancesquaredto-electron2d-vector4i"></a>
### `public long DistanceSquaredTo(Vector4I to)`

Returns the squared Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The exact squared distance when it fits in a signed 64-bit integer. Component differences are widened before subtraction, so they do not use wrapping vector subtraction.

**Exceptions**

- `OverflowException`: The squared distance exceeds `long.MaxValue`.

<a id="m-electron2d-vector4i-distanceto-electron2d-vector4i"></a>
### `public float DistanceTo(Vector4I to)`

Returns the Euclidean distance to another point.

**Parameters**

- `to`: The destination point.

**Returns:** The nonnegative distance rounded to single precision. Widened coordinate differences and floating-point squares keep it finite even when [`Vector4I.DistanceSquaredTo(Vector4I)`](Vector4I.md#m-electron2d-vector4i-distancesquaredto-electron2d-vector4i) exceeds `long.MaxValue`.

<a id="m-electron2d-vector4i-length"></a>
### `public float Length()`

Returns the Euclidean length.

**Returns:** The nonnegative length rounded to single precision. Widened floating-point squares keep it finite even when [`Vector4I.LengthSquared`](Vector4I.md#m-electron2d-vector4i-lengthsquared) exceeds `long.MaxValue`.

<a id="m-electron2d-vector4i-lengthsquared"></a>
### `public long LengthSquared()`

Returns the squared Euclidean length.

**Returns:** The exact sum of the four squared components when it fits in a signed 64-bit integer; components are widened before multiplication.

**Exceptions**

- `OverflowException`: The squared length exceeds `long.MaxValue`.

<a id="m-electron2d-vector4i-max-electron2d-vector4i"></a>
### `public Vector4I Max(Vector4I with)`

Returns the componentwise maximum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector4i-max-system-int32"></a>
### `public Vector4I Max(int with)`

Returns the componentwise maximum with a scalar.

**Parameters**

- `with`: The scalar compared with every component.

**Returns:** The componentwise maximum.

<a id="m-electron2d-vector4i-maxaxisindex"></a>
### `public Vector4I.Axis MaxAxisIndex()`

Returns the axis containing the greatest component.

**Returns:** [`Vector4I.Axis.X`](Vector4I.md#f-electron2d-vector4i-axis-x) when all components are equal; otherwise the first greatest axis.

<a id="m-electron2d-vector4i-min-electron2d-vector4i"></a>
### `public Vector4I Min(Vector4I with)`

Returns the componentwise minimum with another vector.

**Parameters**

- `with`: The other vector.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector4i-min-system-int32"></a>
### `public Vector4I Min(int with)`

Returns the componentwise minimum with a scalar.

**Parameters**

- `with`: The scalar compared with every component.

**Returns:** The componentwise minimum.

<a id="m-electron2d-vector4i-minaxisindex"></a>
### `public Vector4I.Axis MinAxisIndex()`

Returns the axis containing the least component.

**Returns:** [`Vector4I.Axis.W`](Vector4I.md#f-electron2d-vector4i-axis-w) when all components are equal; otherwise the last least axis.

<a id="m-electron2d-vector4i-sign"></a>
### `public Vector4I Sign()`

Returns the sign of every component.

**Returns:** Components containing negative one, zero, or positive one.

<a id="m-electron2d-vector4i-snapped-electron2d-vector4i"></a>
### `public Vector4I Snapped(Vector4I step)`

Snaps each component to the nearest multiple of the corresponding step.

**Parameters**

- `step`: The componentwise step. A zero component leaves the corresponding value unchanged.

**Returns:** The snapped vector.

**Exceptions**

- `OverflowException`: A snapped component is outside the 32-bit signed integer range.

<a id="m-electron2d-vector4i-snapped-system-int32"></a>
### `public Vector4I Snapped(int step)`

Snaps every component to the nearest multiple of a scalar step.

**Parameters**

- `step`: The scalar step. Zero leaves every value unchanged.

**Returns:** The snapped vector.

**Exceptions**

- `OverflowException`: A snapped component is outside the 32-bit signed integer range.

<a id="m-electron2d-vector4i-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an equal integer vector.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is an integer vector with equal components.

<a id="m-electron2d-vector4i-equals-electron2d-vector4i"></a>
### `public bool Equals(Vector4I other)`

Tests every component for exact equality.

**Parameters**

- `other`: The vector to compare.

**Returns:** `true` when all corresponding components are equal.

<a id="m-electron2d-vector4i-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on all components.

**Returns:** The component hash code.

<a id="m-electron2d-vector4i-tostring"></a>
### `public override string ToString()`

Formats every component using invariant culture.

**Returns:** A parenthesized component tuple.

<a id="m-electron2d-vector4i-tostring-system-string"></a>
### `public string ToString(string format)`

Formats every component with a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default.

**Returns:** A parenthesized component tuple.

**Exceptions**

- `FormatException`: `format` is invalid.

## Enumeration Descriptions

<a id="t-electron2d-vector4i-axis"></a>
### `public enum Vector4I.Axis`

Identifies one vector component.

## Constant Descriptions

<a id="f-electron2d-vector4i-axis-x"></a>
### `Vector4I.Axis.X = 0`

Identifies the X component.

<a id="f-electron2d-vector4i-axis-y"></a>
### `Vector4I.Axis.Y = 1`

Identifies the Y component.

<a id="f-electron2d-vector4i-axis-z"></a>
### `Vector4I.Axis.Z = 2`

Identifies the Z component.

<a id="f-electron2d-vector4i-axis-w"></a>
### `Vector4I.Axis.W = 3`

Identifies the W component.

## Field Descriptions

<a id="f-electron2d-vector4i-x"></a>
### `public int X`

Gets or sets the X component.

<a id="f-electron2d-vector4i-y"></a>
### `public int Y`

Gets or sets the Y component.

<a id="f-electron2d-vector4i-z"></a>
### `public int Z`

Gets or sets the Z component.

<a id="f-electron2d-vector4i-w"></a>
### `public int W`

Gets or sets the W component.

## Operator Descriptions

<a id="m-electron2d-vector4i-op-addition-electron2d-vector4i-electron2d-vector4i"></a>
### `public static Vector4I operator +(Vector4I left, Vector4I right)`

Adds two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The wrapping componentwise sum.

<a id="m-electron2d-vector4i-op-unaryplus-electron2d-vector4i"></a>
### `public static Vector4I operator +(Vector4I value)`

Returns a vector unchanged.

**Parameters**

- `value`: The vector.

**Returns:** `value`.

<a id="m-electron2d-vector4i-op-subtraction-electron2d-vector4i-electron2d-vector4i"></a>
### `public static Vector4I operator -(Vector4I left, Vector4I right)`

Subtracts two vectors componentwise.

**Parameters**

- `left`: The minuend.
- `right`: The subtrahend.

**Returns:** The wrapping componentwise difference.

<a id="m-electron2d-vector4i-op-unarynegation-electron2d-vector4i"></a>
### `public static Vector4I operator -(Vector4I value)`

Negates every component.

**Parameters**

- `value`: The vector to negate.

**Returns:** The wrapping componentwise negation.

<a id="m-electron2d-vector4i-op-multiply-electron2d-vector4i-system-int32"></a>
### `public static Vector4I operator *(Vector4I vector, int scalar)`

Multiplies a vector by an integer scalar.

**Parameters**

- `vector`: The vector.
- `scalar`: The scalar multiplier.

**Returns:** The wrapping componentwise product.

<a id="m-electron2d-vector4i-op-multiply-system-int32-electron2d-vector4i"></a>
### `public static Vector4I operator *(int scalar, Vector4I vector)`

Multiplies an integer scalar by a vector.

**Parameters**

- `scalar`: The scalar multiplier.
- `vector`: The vector.

**Returns:** The wrapping componentwise product.

<a id="m-electron2d-vector4i-op-multiply-electron2d-vector4i-system-single"></a>
### `public static Vector4 operator *(Vector4I vector, float scalar)`

Multiplies an integer vector by a floating-point scalar.

**Parameters**

- `vector`: The integer vector.
- `scalar`: The floating-point multiplier.

**Returns:** A floating-point componentwise product.

<a id="m-electron2d-vector4i-op-multiply-system-single-electron2d-vector4i"></a>
### `public static Vector4 operator *(float scalar, Vector4I vector)`

Multiplies a floating-point scalar by an integer vector.

**Parameters**

- `scalar`: The floating-point multiplier.
- `vector`: The integer vector.

**Returns:** A floating-point componentwise product.

<a id="m-electron2d-vector4i-op-multiply-electron2d-vector4i-electron2d-vector4i"></a>
### `public static Vector4I operator *(Vector4I left, Vector4I right)`

Multiplies two vectors componentwise.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** The wrapping componentwise product.

<a id="m-electron2d-vector4i-op-division-electron2d-vector4i-system-int32"></a>
### `public static Vector4I operator /(Vector4I vector, int divisor)`

Divides every component by an integer scalar using truncated division.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The componentwise quotient.

**Exceptions**

- `DivideByZeroException`: `divisor` is zero.
- `OverflowException`: A component is `Int32.MinValue` and `divisor` is negative one.

<a id="m-electron2d-vector4i-op-division-electron2d-vector4i-system-single"></a>
### `public static Vector4 operator /(Vector4I vector, float divisor)`

Divides an integer vector by a floating-point scalar.

**Parameters**

- `vector`: The integer dividend.
- `divisor`: The floating-point divisor.

**Returns:** The IEEE 754 floating-point componentwise quotient.

<a id="m-electron2d-vector4i-op-division-electron2d-vector4i-electron2d-vector4i"></a>
### `public static Vector4I operator /(Vector4I left, Vector4I right)`

Divides two vectors componentwise using truncated division.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The componentwise quotient.

**Exceptions**

- `DivideByZeroException`: A component of `right` is zero.
- `OverflowException`: A minimum-valued dividend component has a divisor of negative one.

<a id="m-electron2d-vector4i-op-modulus-electron2d-vector4i-system-int32"></a>
### `public static Vector4I operator %(Vector4I vector, int divisor)`

Returns the truncated remainder of every component by an integer scalar.

**Parameters**

- `vector`: The dividend.
- `divisor`: The scalar divisor.

**Returns:** The componentwise remainder.

**Exceptions**

- `DivideByZeroException`: `divisor` is zero.
- `OverflowException`: A component is `Int32.MinValue` and `divisor` is negative one.

<a id="m-electron2d-vector4i-op-modulus-electron2d-vector4i-electron2d-vector4i"></a>
### `public static Vector4I operator %(Vector4I left, Vector4I right)`

Returns the truncated componentwise remainder of two vectors.

**Parameters**

- `left`: The dividend.
- `right`: The component divisors.

**Returns:** The componentwise remainder.

**Exceptions**

- `DivideByZeroException`: A component of `right` is zero.
- `OverflowException`: A minimum-valued dividend component has a divisor of negative one.

<a id="m-electron2d-vector4i-op-equality-electron2d-vector4i-electron2d-vector4i"></a>
### `public static bool operator ==(Vector4I left, Vector4I right)`

Tests every component for exact equality.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when all corresponding components are equal.

<a id="m-electron2d-vector4i-op-inequality-electron2d-vector4i-electron2d-vector4i"></a>
### `public static bool operator !=(Vector4I left, Vector4I right)`

Tests whether any component differs.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when a corresponding component differs.

<a id="m-electron2d-vector4i-op-lessthan-electron2d-vector4i-electron2d-vector4i"></a>
### `public static bool operator <(Vector4I left, Vector4I right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts before `right`.

<a id="m-electron2d-vector4i-op-greaterthan-electron2d-vector4i-electron2d-vector4i"></a>
### `public static bool operator >(Vector4I left, Vector4I right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` sorts after `right`.

<a id="m-electron2d-vector4i-op-lessthanorequal-electron2d-vector4i-electron2d-vector4i"></a>
### `public static bool operator <=(Vector4I left, Vector4I right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort after `right`.

<a id="m-electron2d-vector4i-op-greaterthanorequal-electron2d-vector4i-electron2d-vector4i"></a>
### `public static bool operator >=(Vector4I left, Vector4I right)`

Compares vectors lexicographically by X, Y, Z, then W.

**Parameters**

- `left`: The first vector.
- `right`: The second vector.

**Returns:** `true` when `left` does not sort before `right`.

<a id="m-electron2d-vector4i-op-implicit-electron2d-vector4i-electron2d-vector4"></a>
### `public static Vector4 operator implicit(Vector4I value)`

Converts an integer vector to a floating-point vector.

**Parameters**

- `value`: The integer vector to convert.

**Returns:** A floating-point vector with corresponding components.

**Remarks:** Large integer components can lose low-order precision.

<a id="m-electron2d-vector4i-op-explicit-electron2d-vector4-electron2d-vector4i"></a>
### `public static Vector4I operator explicit(Vector4 value)`

Converts a finite in-range floating-point vector by truncating every component toward zero.

**Parameters**

- `value`: The floating-point vector to convert.

**Returns:** The truncated integer vector.

**Exceptions**

- `ArgumentOutOfRangeException`: A component is not finite or is outside the 32-bit signed integer range.

## Numeric invariants and error behavior

- Ordinary integer addition, subtraction, multiplication, and negation wrap explicitly. Squared length and distance widen before multiplication and return `long`; a result above `long.MaxValue` throws `OverflowException`. Length and distance use widened floating-point arithmetic and remain finite at `int` endpoints.
- Division truncates toward zero and remainder has the dividend's sign. Zero scalar/component divisors throw `DivideByZeroException`; `int.MinValue / -1` and `int.MinValue % -1` throw `OverflowException`. Float division retains IEEE behavior.
- `Abs(int.MinValue)` throws `OverflowException`. Reversed clamp bounds throw `ArgumentException`; invalid indices throw `ArgumentOutOfRangeException`.
- Maximum-axis ties choose X and minimum-axis ties choose W. Snapping uses double intermediate arithmetic; midpoint ties go toward larger values for positive steps and smaller values for negative steps. Zero steps preserve the value and an out-of-range snapped result throws `OverflowException`.
- Float conversion truncates toward zero and rejects non-finite or out-of-range values with `ArgumentOutOfRangeException`; widening can lose precision above 2^24.
- Invalid formats throw `FormatException`.

## Lifecycle, threading, dependencies, and allocation

Copies are independent. Numeric operations allocate no managed memory after warmup; formatting allocates. Independent values are safe across threads; concurrent shared mutation is not synchronized.

The type depends on canonical scalar [`Mathf`](Mathf.md) for snapping and scalar operations, plus formatting/layout primitives and [`Vector4`](Vector4.md). [`ConfigFile`](ConfigFile.md) persists exact four-field 32-bit integer objects; [`PackedScene`](PackedScene.md) stores it directly.

## Coverage, verification, and limitations

The complete implementable pure-value surface is present. Universal-value truth conversion is permanently excluded, and there is no 3D projection or external numerics integration.

The executable harness covers layout, constants, indexing, conversions, every method/operator family, widened norms and their overflow boundaries, wraparound, division failures, axis ties, snapping, formatting, strict persistence, packed-scene storage, and warmed zero-allocation math. Execution is Linux/.NET 8 only.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
