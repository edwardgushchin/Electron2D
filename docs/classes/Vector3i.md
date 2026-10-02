# Vector3i

Last updated: 2026-10-02

- **Source:** [`src/Core/Math/Vector3i.cs`](../../src/Core/Math/Vector3i.cs)
- **Declaration:** `public struct Vector3i`
- **Namespace:** `Electron2D`

## Description

`Vector3i` is a mutable sequential three-component 32-bit integer numeric value with X/Y/Z storage. It is independent of scene nodes and three-dimensional rendering. Its layout is 12 bytes. Ordinary numeric operations do not allocate managed memory; formatting and configuration serialization do.

The value can be copied directly into typed packed-scene properties. [`ConfigFile`](ConfigFile.md) uses a strict integer X/Y/Z schema and rejects fractional fields. Shader uniforms use [`Vector3`](Vector3.md) for float3 and [`Vector3i`](Vector3i.md) for signed or unsigned int3. [`Color`](Color.md) remains an RGB alias for float3 when the value has color semantics.

All 56 declared members and the type row have managed behavioral audits on Linux/.NET 8. `VerifyVector3iValues` checks constants, mutable copies, typed conversion errors, first/last axis ties, component operations, negative and zero snapping steps, checked 64-bit squared norms, full-range finite lengths, wrapping ordinary arithmetic, division/remainder failures, ordering and strict persistence. Native ABI and other platforms remain unverified.

## Example

```csharp
var value = new Vector3i(1, 2, 3);
var doubled = value * 2;
```

## API

| Declaration | Contract |
| --- | --- |
| [`public Vector3i(Electron2D.Vector3 value)`](#member-1) | Initializes an integer vector by truncating a finite floating-point vector toward zero. |
| [`public Vector3i(System.Int32 x, System.Int32 y, System.Int32 z)`](#member-2) | Initializes an integer vector from three components. |
| [`public static Electron2D.Vector3i Back { get;  }`](#member-3) | Gets the positive Z unit vector. |
| [`public static Electron2D.Vector3i Down { get;  }`](#member-4) | Gets the negative Y unit vector. |
| [`public static Electron2D.Vector3i Forward { get;  }`](#member-5) | Gets the negative Z unit vector. |
| [`public System.Int32 this[System.Int32 index] { get; set; }`](#member-6) | Gets or sets a component by axis index. |
| [`public static Electron2D.Vector3i Left { get;  }`](#member-7) | Gets the negative X unit vector. |
| [`public static Electron2D.Vector3i MaxValue { get;  }`](#member-8) | Gets the vector containing the maximum 32-bit integer in every component. |
| [`public static Electron2D.Vector3i MinValue { get;  }`](#member-9) | Gets the vector containing the minimum 32-bit integer in every component. |
| [`public static Electron2D.Vector3i One { get;  }`](#member-10) | Gets the vector whose components are all one. |
| [`public static Electron2D.Vector3i Right { get;  }`](#member-11) | Gets the positive X unit vector. |
| [`public static Electron2D.Vector3i Up { get;  }`](#member-12) | Gets the positive Y unit vector. |
| [`public System.Int32 X`](#member-13) | Gets or sets the X component. |
| [`public System.Int32 Y`](#member-14) | Gets or sets the Y component. |
| [`public System.Int32 Z`](#member-15) | Gets or sets the Z component. |
| [`public static Electron2D.Vector3i Zero { get;  }`](#member-16) | Gets the zero vector. |
| [`public Electron2D.Vector3i Abs()`](#member-17) | Returns the componentwise absolute value. |
| [`public Electron2D.Vector3i Clamp(Electron2D.Vector3i min, Electron2D.Vector3i max)`](#member-18) | Clamps each component between corresponding vector bounds. |
| [`public Electron2D.Vector3i Clamp(System.Int32 min, System.Int32 max)`](#member-19) | Clamps every component between scalar bounds. |
| [`public System.Void Deconstruct(out System.Int32 x, out System.Int32 y, out System.Int32 z)`](#member-20) | Deconstructs the vector into its three components. |
| [`public System.Int64 DistanceSquaredTo(Electron2D.Vector3i to)`](#member-21) | Returns the squared Euclidean distance to another point. |
| [`public System.Single DistanceTo(Electron2D.Vector3i to)`](#member-22) | Returns the Euclidean distance to another point. |
| [`public virtual System.Boolean Equals(Electron2D.Vector3i other)`](#member-23) | Tests every component for exact equality. |
| [`public override System.Boolean Equals(System.Object obj)`](#member-24) | Tests whether another object is an equal integer vector. |
| [`public override System.Int32 GetHashCode()`](#member-25) | Returns a hash code based on all components. |
| [`public System.Single Length()`](#member-26) | Returns the Euclidean length. |
| [`public System.Int64 LengthSquared()`](#member-27) | Returns the squared Euclidean length. |
| [`public Electron2D.Vector3i Max(Electron2D.Vector3i with)`](#member-28) | Returns the componentwise maximum with another vector. |
| [`public Electron2D.Vector3i Max(System.Int32 with)`](#member-29) | Returns the componentwise maximum with a scalar. |
| [`public Electron2D.Vector3Axis MaxAxisIndex()`](#member-30) | Returns the axis containing the greatest component. |
| [`public Electron2D.Vector3i Min(Electron2D.Vector3i with)`](#member-31) | Returns the componentwise minimum with another vector. |
| [`public Electron2D.Vector3i Min(System.Int32 with)`](#member-32) | Returns the componentwise minimum with a scalar. |
| [`public Electron2D.Vector3Axis MinAxisIndex()`](#member-33) | Returns the axis containing the least component. |
| [`public Electron2D.Vector3i Sign()`](#member-34) | Returns the sign of every component. |
| [`public Electron2D.Vector3i Snapped(Electron2D.Vector3i step)`](#member-35) | Snaps each component to the nearest multiple of the corresponding step. |
| [`public Electron2D.Vector3i Snapped(System.Int32 step)`](#member-36) | Snaps every component to the nearest multiple of a scalar step. |
| [`public override System.String ToString()`](#member-37) | Formats every component using invariant culture. |
| [`public System.String ToString(System.String format)`](#member-38) | Formats every component with a numeric format and invariant culture. |
| [`public static Electron2D.Vector3i op_Addition(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-39) | Adds two vectors componentwise. |
| [`public static Electron2D.Vector3i op_Division(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-40) | Divides two vectors componentwise using truncated division. |
| [`public static Electron2D.Vector3i op_Division(Electron2D.Vector3i vector, System.Int32 divisor)`](#member-41) | Divides every component by an integer scalar using truncated division. |
| [`public static Electron2D.Vector3 op_Division(Electron2D.Vector3i vector, System.Single divisor)`](#member-42) | Divides an integer vector by a floating-point scalar. |
| [`public static System.Boolean op_Equality(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-43) | Tests every component for exact equality. |
| [`public static Electron2D.Vector3i op_Explicit(Electron2D.Vector3 value)`](#member-44) | Truncates finite in-range components toward zero. |
| [`public static System.Boolean op_GreaterThan(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-45) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static System.Boolean op_GreaterThanOrEqual(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-46) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static Electron2D.Vector3 op_Implicit(Electron2D.Vector3i value)`](#member-47) | Converts components to single precision; large values can lose precision. |
| [`public static System.Boolean op_Inequality(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-48) | Tests whether any component differs. |
| [`public static System.Boolean op_LessThan(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-49) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static System.Boolean op_LessThanOrEqual(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-50) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static Electron2D.Vector3i op_Modulus(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-51) | Returns the truncated componentwise remainder of two vectors. |
| [`public static Electron2D.Vector3i op_Modulus(Electron2D.Vector3i vector, System.Int32 divisor)`](#member-52) | Returns the truncated remainder of every component by an integer scalar. |
| [`public static Electron2D.Vector3i op_Multiply(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-53) | Multiplies two vectors componentwise. |
| [`public static Electron2D.Vector3i op_Multiply(Electron2D.Vector3i vector, System.Int32 scalar)`](#member-54) | Multiplies a vector by an integer scalar. |
| [`public static Electron2D.Vector3 op_Multiply(Electron2D.Vector3i vector, System.Single scalar)`](#member-55) | Multiplies an integer vector by a floating-point scalar. |
| [`public static Electron2D.Vector3i op_Multiply(System.Int32 scalar, Electron2D.Vector3i vector)`](#member-56) | Multiplies an integer scalar by a vector. |
| [`public static Electron2D.Vector3 op_Multiply(System.Single scalar, Electron2D.Vector3i vector)`](#member-57) | Multiplies a floating-point scalar by an integer vector. |
| [`public static Electron2D.Vector3i op_Subtraction(Electron2D.Vector3i left, Electron2D.Vector3i right)`](#member-58) | Subtracts two vectors componentwise. |
| [`public static Electron2D.Vector3i op_UnaryNegation(Electron2D.Vector3i value)`](#member-59) | Negates every component. |
| [`public static Electron2D.Vector3i op_UnaryPlus(Electron2D.Vector3i value)`](#member-60) | Returns a vector unchanged. |
| [`public enum Electron2D.Vector3Axis`](#member-61) | Identifies one vector component. |
| [`public const Electron2D.Vector3Axis X = 0`](#member-62) | Identifies the X component. |
| [`public const Electron2D.Vector3Axis Y = 1`](#member-63) | Identifies the Y component. |
| [`public const Electron2D.Vector3Axis Z = 2`](#member-64) | Identifies the Z component. |

## Member contracts

<a id="member-1"></a>
### `public Vector3i(Electron2D.Vector3 value)`

Initializes an integer vector by truncating a finite floating-point vector toward zero.

- `value`: The floating-point vector to convert.
- Throws `T:System.ArgumentOutOfRangeException`: A component is not finite or is outside the 32-bit signed integer range.

<a id="member-2"></a>
### `public Vector3i(System.Int32 x, System.Int32 y, System.Int32 z)`

Initializes an integer vector from three components.

- `x`: The X component.
- `y`: The Y component.
- `z`: The Z component.

<a id="member-3"></a>
### `public static Electron2D.Vector3i Back { get;  }`

Gets the positive Z unit vector.


<a id="member-4"></a>
### `public static Electron2D.Vector3i Down { get;  }`

Gets the negative Y unit vector.


<a id="member-5"></a>
### `public static Electron2D.Vector3i Forward { get;  }`

Gets the negative Z unit vector.


<a id="member-6"></a>
### `public System.Int32 this[System.Int32 index] { get; set; }`

Gets or sets a component by axis index.

- `index`: An index from zero through two for X, Y, or Z.
- Value: The selected component.
- Throws `T:System.ArgumentOutOfRangeException`: index is outside zero through two.

<a id="member-7"></a>
### `public static Electron2D.Vector3i Left { get;  }`

Gets the negative X unit vector.


<a id="member-8"></a>
### `public static Electron2D.Vector3i MaxValue { get;  }`

Gets the vector containing the maximum 32-bit integer in every component.

- Value: (int.MaxValue, int.MaxValue, int.MaxValue).

<a id="member-9"></a>
### `public static Electron2D.Vector3i MinValue { get;  }`

Gets the vector containing the minimum 32-bit integer in every component.

- Value: (int.MinValue, int.MinValue, int.MinValue).

<a id="member-10"></a>
### `public static Electron2D.Vector3i One { get;  }`

Gets the vector whose components are all one.

- Value: (1, 1, 1).

<a id="member-11"></a>
### `public static Electron2D.Vector3i Right { get;  }`

Gets the positive X unit vector.


<a id="member-12"></a>
### `public static Electron2D.Vector3i Up { get;  }`

Gets the positive Y unit vector.


<a id="member-13"></a>
### `public System.Int32 X`

Gets or sets the X component.


<a id="member-14"></a>
### `public System.Int32 Y`

Gets or sets the Y component.


<a id="member-15"></a>
### `public System.Int32 Z`

Gets or sets the Z component.


<a id="member-16"></a>
### `public static Electron2D.Vector3i Zero { get;  }`

Gets the zero vector.

- Value: (0, 0, 0).

<a id="member-17"></a>
### `public Electron2D.Vector3i Abs()`

Returns the componentwise absolute value.

- Returns: A vector with nonnegative components.
- Throws `T:System.OverflowException`: A component is F:System.Int32.MinValue.

<a id="member-18"></a>
### `public Electron2D.Vector3i Clamp(Electron2D.Vector3i min, Electron2D.Vector3i max)`

Clamps each component between corresponding vector bounds.

- `min`: The componentwise lower bounds.
- `max`: The componentwise upper bounds.
- Returns: The clamped vector.
- Throws `T:System.ArgumentException`: A lower bound is greater than its corresponding upper bound.

<a id="member-19"></a>
### `public Electron2D.Vector3i Clamp(System.Int32 min, System.Int32 max)`

Clamps every component between scalar bounds.

- `min`: The lower bound.
- `max`: The upper bound.
- Returns: The clamped vector.
- Throws `T:System.ArgumentException`: min is greater than max.

<a id="member-20"></a>
### `public System.Void Deconstruct(out System.Int32 x, out System.Int32 y, out System.Int32 z)`

Deconstructs the vector into its three components.

- `x`: Receives F:Electron2D.Vector3i.X.
- `y`: Receives F:Electron2D.Vector3i.Y.
- `z`: Receives F:Electron2D.Vector3i.Z.

<a id="member-21"></a>
### `public System.Int64 DistanceSquaredTo(Electron2D.Vector3i to)`

Returns the squared Euclidean distance to another point.

- `to`: The destination point.
- Returns: The exact squared distance when it fits in a signed 64-bit integer.
- Note: Coordinate differences are widened before subtraction. This operation does not use wrapping vector subtraction.
- Throws `T:System.OverflowException`: The squared distance exceeds F:System.Int64.MaxValue.

<a id="member-22"></a>
### `public System.Single DistanceTo(Electron2D.Vector3i to)`

Returns the Euclidean distance to another point.

- `to`: The destination point.
- Returns: The nonnegative distance, rounded to single precision.
- Note: Coordinate differences and squared terms use widened arithmetic, so this remains finite even when M:Electron2D.Vector3i.DistanceSquaredTo(Electron2D.Vector3i) exceeds the signed 64-bit range.

<a id="member-23"></a>
### `public virtual System.Boolean Equals(Electron2D.Vector3i other)`

Tests every component for exact equality.

- `other`: The vector to compare.
- Returns: true when all corresponding components are equal.

<a id="member-24"></a>
### `public override System.Boolean Equals(System.Object obj)`

Tests whether another object is an equal integer vector.

- `obj`: The object to compare.
- Returns: true when obj is an integer vector with equal components.

<a id="member-25"></a>
### `public override System.Int32 GetHashCode()`

Returns a hash code based on all components.

- Returns: The component hash code.

<a id="member-26"></a>
### `public System.Single Length()`

Returns the Euclidean length.

- Returns: The nonnegative length, rounded to single precision.
- Note: Squared terms use widened arithmetic, so this remains finite even when M:Electron2D.Vector3i.LengthSquared exceeds the signed 64-bit range.

<a id="member-27"></a>
### `public System.Int64 LengthSquared()`

Returns the squared Euclidean length.

- Returns: The exact squared length when it fits in a signed 64-bit integer.
- Throws `T:System.OverflowException`: The squared length exceeds F:System.Int64.MaxValue.

<a id="member-28"></a>
### `public Electron2D.Vector3i Max(Electron2D.Vector3i with)`

Returns the componentwise maximum with another vector.

- `with`: The other vector.
- Returns: The componentwise maximum.

<a id="member-29"></a>
### `public Electron2D.Vector3i Max(System.Int32 with)`

Returns the componentwise maximum with a scalar.

- `with`: The scalar compared with every component.
- Returns: The componentwise maximum.

<a id="member-30"></a>
### `public Electron2D.Vector3Axis MaxAxisIndex()`

Returns the axis containing the greatest component.

- Returns: F:Electron2D.Vector3Axis.X when all components are equal; otherwise the first greatest axis.

<a id="member-31"></a>
### `public Electron2D.Vector3i Min(Electron2D.Vector3i with)`

Returns the componentwise minimum with another vector.

- `with`: The other vector.
- Returns: The componentwise minimum.

<a id="member-32"></a>
### `public Electron2D.Vector3i Min(System.Int32 with)`

Returns the componentwise minimum with a scalar.

- `with`: The scalar compared with every component.
- Returns: The componentwise minimum.

<a id="member-33"></a>
### `public Electron2D.Vector3Axis MinAxisIndex()`

Returns the axis containing the least component.

- Returns: F:Electron2D.Vector3Axis.Z when all components are equal; otherwise the last least axis.

<a id="member-34"></a>
### `public Electron2D.Vector3i Sign()`

Returns the sign of every component.

- Returns: Components containing negative one, zero, or positive one.

<a id="member-35"></a>
### `public Electron2D.Vector3i Snapped(Electron2D.Vector3i step)`

Snaps each component to the nearest multiple of the corresponding step.

- `step`: The componentwise step. A zero component leaves the corresponding value unchanged.
- Returns: The snapped vector.
- Throws `T:System.OverflowException`: A snapped component is outside the 32-bit signed integer range.

<a id="member-36"></a>
### `public Electron2D.Vector3i Snapped(System.Int32 step)`

Snaps every component to the nearest multiple of a scalar step.

- `step`: The scalar step. Zero leaves every value unchanged.
- Returns: The snapped vector.
- Throws `T:System.OverflowException`: A snapped component is outside the 32-bit signed integer range.

<a id="member-37"></a>
### `public override System.String ToString()`

Formats every component using invariant culture.

- Returns: A parenthesized component tuple.

<a id="member-38"></a>
### `public System.String ToString(System.String format)`

Formats every component with a numeric format and invariant culture.

- `format`: A standard or custom numeric format, or null for the default.
- Returns: A parenthesized component tuple.
- Throws `T:System.FormatException`: format is invalid.

<a id="member-39"></a>
### `public static Electron2D.Vector3i op_Addition(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Adds two vectors componentwise.

- `left`: The first vector.
- `right`: The second vector.
- Returns: The wrapping componentwise sum.

<a id="member-40"></a>
### `public static Electron2D.Vector3i op_Division(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Divides two vectors componentwise using truncated division.

- `left`: The dividend.
- `right`: The component divisors.
- Returns: The componentwise quotient.
- Throws `T:System.DivideByZeroException`: A component of right is zero.
- Throws `T:System.OverflowException`: A minimum-valued dividend component has a divisor of negative one.

<a id="member-41"></a>
### `public static Electron2D.Vector3i op_Division(Electron2D.Vector3i vector, System.Int32 divisor)`

Divides every component by an integer scalar using truncated division.

- `vector`: The dividend.
- `divisor`: The scalar divisor.
- Returns: The componentwise quotient.
- Throws `T:System.DivideByZeroException`: divisor is zero.
- Throws `T:System.OverflowException`: A component is F:System.Int32.MinValue and divisor is negative one.

<a id="member-42"></a>
### `public static Electron2D.Vector3 op_Division(Electron2D.Vector3i vector, System.Single divisor)`

Divides an integer vector by a floating-point scalar.

- `vector`: The integer dividend.
- `divisor`: The floating-point divisor.
- Returns: The IEEE 754 floating-point componentwise quotient.

<a id="member-43"></a>
### `public static System.Boolean op_Equality(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Tests every component for exact equality.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when all corresponding components are equal.

<a id="member-44"></a>
### `public static Electron2D.Vector3i op_Explicit(Electron2D.Vector3 value)`

Converts a floating-point vector to an integer vector, truncating finite in-range components toward zero.

<a id="member-45"></a>
### `public static System.Boolean op_GreaterThan(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left sorts after right.

<a id="member-46"></a>
### `public static System.Boolean op_GreaterThanOrEqual(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left does not sort before right.

<a id="member-47"></a>
### `public static Electron2D.Vector3 op_Implicit(Electron2D.Vector3i value)`

Converts an integer vector to a floating-point vector; large components may lose low-order precision.

<a id="member-48"></a>
### `public static System.Boolean op_Inequality(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Tests whether any component differs.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when a corresponding component differs.

<a id="member-49"></a>
### `public static System.Boolean op_LessThan(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left sorts before right.

<a id="member-50"></a>
### `public static System.Boolean op_LessThanOrEqual(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left does not sort after right.

<a id="member-51"></a>
### `public static Electron2D.Vector3i op_Modulus(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Returns the truncated componentwise remainder of two vectors.

- `left`: The dividend.
- `right`: The component divisors.
- Returns: The componentwise remainder.
- Throws `T:System.DivideByZeroException`: A component of right is zero.
- Throws `T:System.OverflowException`: A minimum-valued dividend component has a divisor of negative one.

<a id="member-52"></a>
### `public static Electron2D.Vector3i op_Modulus(Electron2D.Vector3i vector, System.Int32 divisor)`

Returns the truncated remainder of every component by an integer scalar.

- `vector`: The dividend.
- `divisor`: The scalar divisor.
- Returns: The componentwise remainder.
- Throws `T:System.DivideByZeroException`: divisor is zero.
- Throws `T:System.OverflowException`: A component is F:System.Int32.MinValue and divisor is negative one.

<a id="member-53"></a>
### `public static Electron2D.Vector3i op_Multiply(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Multiplies two vectors componentwise.

- `left`: The first vector.
- `right`: The second vector.
- Returns: The wrapping componentwise product.

<a id="member-54"></a>
### `public static Electron2D.Vector3i op_Multiply(Electron2D.Vector3i vector, System.Int32 scalar)`

Multiplies a vector by an integer scalar.

- `vector`: The vector.
- `scalar`: The scalar multiplier.
- Returns: The wrapping componentwise product.

<a id="member-55"></a>
### `public static Electron2D.Vector3 op_Multiply(Electron2D.Vector3i vector, System.Single scalar)`

Multiplies an integer vector by a floating-point scalar.

- `vector`: The integer vector.
- `scalar`: The floating-point multiplier.
- Returns: A floating-point componentwise product.

<a id="member-56"></a>
### `public static Electron2D.Vector3i op_Multiply(System.Int32 scalar, Electron2D.Vector3i vector)`

Multiplies an integer scalar by a vector.

- `scalar`: The scalar multiplier.
- `vector`: The vector.
- Returns: The wrapping componentwise product.

<a id="member-57"></a>
### `public static Electron2D.Vector3 op_Multiply(System.Single scalar, Electron2D.Vector3i vector)`

Multiplies a floating-point scalar by an integer vector.

- `scalar`: The floating-point multiplier.
- `vector`: The integer vector.
- Returns: A floating-point componentwise product.

<a id="member-58"></a>
### `public static Electron2D.Vector3i op_Subtraction(Electron2D.Vector3i left, Electron2D.Vector3i right)`

Subtracts two vectors componentwise.

- `left`: The minuend.
- `right`: The subtrahend.
- Returns: The wrapping componentwise difference.

<a id="member-59"></a>
### `public static Electron2D.Vector3i op_UnaryNegation(Electron2D.Vector3i value)`

Negates every component.

- `value`: The vector to negate.
- Returns: The wrapping componentwise negation.

<a id="member-60"></a>
### `public static Electron2D.Vector3i op_UnaryPlus(Electron2D.Vector3i value)`

Returns a vector unchanged.

- `value`: The vector.
- Returns: value.

<a id="member-61"></a>
### `public enum Electron2D.Vector3Axis`

Identifies one vector component.


<a id="member-62"></a>
### `public const Electron2D.Vector3Axis X = 0`

Identifies the X component.


<a id="member-63"></a>
### `public const Electron2D.Vector3Axis Y = 1`

Identifies the Y component.


<a id="member-64"></a>
### `public const Electron2D.Vector3Axis Z = 2`

Identifies the Z component.


## Verification and limits

`tests/Electron2D.Tests/Program.cs` checks Vector3i layout, arithmetic, conversion boundaries, configuration and packed-scene storage. Shader material checks cover float3/int3/uint3 descriptors, RGB aliases and arrays. The current native gate is Linux Wayland; no other platform ABI or package claim follows from these managed tests.

## Decisions

- [ADR 0033: Dimensioned vector family](../decisions/core-math.md#adr-0033)
- [ADR 0004: 2D-only engine](../decisions/product.md#adr-0004)
