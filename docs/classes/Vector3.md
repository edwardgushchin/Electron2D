# Vector3

Last updated: 2026-10-02

- **Source:** [`src/Core/Math/Vector3.cs`](../../src/Core/Math/Vector3.cs)
- **Declaration:** `public struct Vector3`
- **Namespace:** `Electron2D`

## Description

`Vector3` is a mutable sequential three-component single-precision numeric value with X/Y/Z storage. It is independent of scene nodes and three-dimensional rendering. Its layout is 12 bytes. Ordinary numeric operations do not allocate managed memory; formatting and configuration serialization do.

The value can be copied directly into typed packed-scene properties. [`ConfigFile`](ConfigFile.md) uses a strict X/Y/Z schema; floating-point persistence rejects nonfinite components. Shader uniforms use [`Vector3`](Vector3.md) for float3 and [`Vector3i`](Vector3i.md) for signed or unsigned int3. [`Color`](Color.md) remains an RGB alias for float3 when the value has color semantics.

All 84 applicable declared members and the type row have managed semantic audits on Linux/.NET 8. Core values/operators, componentwise scalar behavior, normalization, movement, geometry, interpolation and octahedral packing are covered by VerifyVector3CoreValues, VerifyVector3ComponentMethods, VerifyVector3Geometry and earlier vector checks. `Reflect` and `Bounce` preserve finite values near float limits by multiplying the normal before the dot scalar. `Rotated` computes the pinned axis-angle matrix rows internally; 10,000 deterministic finite comparisons against that formula found no float differences, and `Slerp` follows the pinned squared-length/axis order with finite and non-finite samples.

The ten model-orientation constants and Basis/Quaternion/Transform3D members remain Excluded under ADR 0004. This numeric tuple adds no 3D scene type. Native ABI and other-platform math/runtime behavior remain unverified.

## Example

```csharp
var value = new Vector3(1, 2, 3);
var doubled = value * 2;
```

## API

| Declaration | Contract |
| --- | --- |
| [`public Vector3(Electron2D.Vector3i value)`](#member-1) | Initializes a floating-point vector from an integer vector. |
| [`public Vector3(System.Single x, System.Single y, System.Single z)`](#member-2) | Initializes a vector from three components. |
| [`public static Electron2D.Vector3 Back { get;  }`](#member-3) | Gets the positive Z unit vector. |
| [`public static Electron2D.Vector3 Down { get;  }`](#member-4) | Gets the negative Y unit vector. |
| [`public static Electron2D.Vector3 Forward { get;  }`](#member-5) | Gets the negative Z unit vector. |
| [`public static Electron2D.Vector3 Inf { get;  }`](#member-6) | Gets the vector whose components are all positive infinity. |
| [`public System.Single this[System.Int32 index] { get; set; }`](#member-7) | Gets or sets a component by axis index. |
| [`public static Electron2D.Vector3 Left { get;  }`](#member-8) | Gets the negative X unit vector. |
| [`public static Electron2D.Vector3 One { get;  }`](#member-9) | Gets the vector whose components are all one. |
| [`public static Electron2D.Vector3 Right { get;  }`](#member-10) | Gets the positive X unit vector. |
| [`public static Electron2D.Vector3 Up { get;  }`](#member-11) | Gets the positive Y unit vector. |
| [`public System.Single X`](#member-12) | Gets or sets the X component. |
| [`public System.Single Y`](#member-13) | Gets or sets the Y component. |
| [`public System.Single Z`](#member-14) | Gets or sets the Z component. |
| [`public static Electron2D.Vector3 Zero { get;  }`](#member-15) | Gets the zero vector. |
| [`public Electron2D.Vector3 Abs()`](#member-16) | Returns the componentwise absolute value. |
| [`public System.Single AngleTo(Electron2D.Vector3 to)`](#member-17) | Returns the unsigned angle to another vector in radians. |
| [`public Electron2D.Vector3 BezierDerivative(Electron2D.Vector3 control1, Electron2D.Vector3 control2, Electron2D.Vector3 end, System.Single t)`](#member-18) | Returns the derivative of a cubic Bezier curve at a parameter. |
| [`public Electron2D.Vector3 BezierInterpolate(Electron2D.Vector3 control1, Electron2D.Vector3 control2, Electron2D.Vector3 end, System.Single t)`](#member-19) | Returns a point on a cubic Bezier curve. |
| [`public Electron2D.Vector3 Bounce(Electron2D.Vector3 normal)`](#member-20) | Returns the vector bounced from a plane with the supplied unit normal. |
| [`public Electron2D.Vector3 Ceil()`](#member-21) | Rounds every component upward toward positive infinity. |
| [`public Electron2D.Vector3 Clamp(Electron2D.Vector3 min, Electron2D.Vector3 max)`](#member-22) | Clamps each component between corresponding vector bounds. |
| [`public Electron2D.Vector3 Clamp(System.Single min, System.Single max)`](#member-23) | Clamps every component between scalar bounds. |
| [`public Electron2D.Vector3 Cross(Electron2D.Vector3 with)`](#member-24) | Returns the cross product with another vector. |
| [`public Electron2D.Vector3 CubicInterpolate(Electron2D.Vector3 b, Electron2D.Vector3 preA, Electron2D.Vector3 postB, System.Single weight)`](#member-25) | Performs Catmull-Rom cubic interpolation between this vector and another. |
| [`public Electron2D.Vector3 CubicInterpolateInTime(Electron2D.Vector3 b, Electron2D.Vector3 preA, Electron2D.Vector3 postB, System.Single weight, System.Single bTime, System.Single preATime, System.Single postBTime)`](#member-26) | Performs time-aware Barry-Goldman cubic interpolation. |
| [`public System.Void Deconstruct(out System.Single x, out System.Single y, out System.Single z)`](#member-27) | Deconstructs the vector into its three components. |
| [`public Electron2D.Vector3 DirectionTo(Electron2D.Vector3 to)`](#member-28) | Returns the normalized direction from this point to another point. |
| [`public System.Single DistanceSquaredTo(Electron2D.Vector3 to)`](#member-29) | Returns the squared Euclidean distance to another point. |
| [`public System.Single DistanceTo(Electron2D.Vector3 to)`](#member-30) | Returns the Euclidean distance to another point. |
| [`public System.Single Dot(Electron2D.Vector3 with)`](#member-31) | Returns the dot product with another vector. |
| [`public virtual System.Boolean Equals(Electron2D.Vector3 other)`](#member-32) | Tests every component for exact equality. |
| [`public override System.Boolean Equals(System.Object obj)`](#member-33) | Tests whether another object is an exactly equal vector. |
| [`public Electron2D.Vector3 Floor()`](#member-34) | Rounds every component downward toward negative infinity. |
| [`public override System.Int32 GetHashCode()`](#member-35) | Returns a hash code based on all components. |
| [`public Electron2D.Vector3 Inverse()`](#member-36) | Returns the componentwise reciprocal. |
| [`public System.Boolean IsEqualApprox(Electron2D.Vector3 other)`](#member-37) | Tests every component for scale-aware approximate equality. |
| [`public System.Boolean IsFinite()`](#member-38) | Tests whether every component is finite. |
| [`public System.Boolean IsNormalized()`](#member-39) | Tests whether the squared length is approximately one. |
| [`public System.Boolean IsZeroApprox()`](#member-40) | Tests whether every component is approximately zero. |
| [`public System.Single Length()`](#member-41) | Returns the Euclidean length. |
| [`public System.Single LengthSquared()`](#member-42) | Returns the squared Euclidean length. |
| [`public Electron2D.Vector3 Lerp(Electron2D.Vector3 to, System.Single weight)`](#member-43) | Linearly interpolates or extrapolates toward another vector. |
| [`public Electron2D.Vector3 LimitLength(System.Single length = 1f)`](#member-44) | Restricts the vector length to a maximum. |
| [`public Electron2D.Vector3 Max(Electron2D.Vector3 with)`](#member-45) | Returns the componentwise maximum with another vector. |
| [`public Electron2D.Vector3 Max(System.Single with)`](#member-46) | Returns the componentwise maximum with a scalar. |
| [`public Electron2D.Vector3Axis MaxAxisIndex()`](#member-47) | Returns the axis containing the greatest component. |
| [`public Electron2D.Vector3 Min(Electron2D.Vector3 with)`](#member-48) | Returns the componentwise minimum with another vector. |
| [`public Electron2D.Vector3 Min(System.Single with)`](#member-49) | Returns the componentwise minimum with a scalar. |
| [`public Electron2D.Vector3Axis MinAxisIndex()`](#member-50) | Returns the axis containing the least component. |
| [`public Electron2D.Vector3 MoveToward(Electron2D.Vector3 to, System.Single delta)`](#member-51) | Moves toward another vector by a signed distance without passing it. |
| [`public Electron2D.Vector3 Normalized()`](#member-52) | Returns this vector scaled to unit length. |
| [`public static Electron2D.Vector3 OctahedronDecode(Electron2D.Vector2 uv)`](#member-53) | Decodes an octahedrally packed unit vector from a two-component value. |
| [`public Electron2D.Vector2 OctahedronEncode()`](#member-54) | Octahedrally packs a unit vector into a two-component value. |
| [`public Electron2D.Vector3 PosMod(Electron2D.Vector3 mod)`](#member-55) | Applies componentwise positive modulus. |
| [`public Electron2D.Vector3 PosMod(System.Single mod)`](#member-56) | Applies positive modulus to every component. |
| [`public Electron2D.Vector3 Project(Electron2D.Vector3 onNormal)`](#member-57) | Projects this vector onto another vector. |
| [`public Electron2D.Vector3 Reflect(Electron2D.Vector3 normal)`](#member-58) | Reflects this vector across a plane with a unit normal. |
| [`public Electron2D.Vector3 Rotated(Electron2D.Vector3 axis, System.Single angle)`](#member-59) | Rotates this vector about a unit axis by an angle in radians. |
| [`public Electron2D.Vector3 Round()`](#member-60) | Rounds every component to the nearest integer using midpoint-to-even behavior. |
| [`public Electron2D.Vector3 Sign()`](#member-61) | Returns the sign of every component. |
| [`public System.Single SignedAngleTo(Electron2D.Vector3 to, Electron2D.Vector3 axis)`](#member-62) | Returns the signed angle to another vector about an axis. |
| [`public Electron2D.Vector3 Slerp(Electron2D.Vector3 to, System.Single weight)`](#member-63) | Interpolates direction on the unit sphere and linearly interpolates length. |
| [`public Electron2D.Vector3 Slide(Electron2D.Vector3 normal)`](#member-64) | Removes the component along a unit normal. |
| [`public Electron2D.Vector3 Snapped(Electron2D.Vector3 step)`](#member-65) | Snaps each component to the nearest multiple of the corresponding step. |
| [`public Electron2D.Vector3 Snapped(System.Single step)`](#member-66) | Snaps every component to the nearest multiple of a scalar step. |
| [`public override System.String ToString()`](#member-67) | Formats every component using invariant culture. |
| [`public System.String ToString(System.String format)`](#member-68) | Formats every component with a numeric format and invariant culture. |
| [`public static Electron2D.Vector3 op_Addition(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-69) | Adds two vectors componentwise. |
| [`public static Electron2D.Vector3 op_Division(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-70) | Divides two vectors componentwise. |
| [`public static Electron2D.Vector3 op_Division(Electron2D.Vector3 vector, System.Single divisor)`](#member-71) | Divides every component by a scalar. |
| [`public static System.Boolean op_Equality(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-72) | Tests every component for exact equality. |
| [`public static System.Boolean op_GreaterThan(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-73) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static System.Boolean op_GreaterThanOrEqual(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-74) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static System.Boolean op_Inequality(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-75) | Tests whether any component differs. |
| [`public static System.Boolean op_LessThan(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-76) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static System.Boolean op_LessThanOrEqual(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-77) | Compares vectors lexicographically by X, Y, then Z. |
| [`public static Electron2D.Vector3 op_Modulus(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-78) | Returns the truncated componentwise remainder of two vectors. |
| [`public static Electron2D.Vector3 op_Modulus(Electron2D.Vector3 vector, System.Single divisor)`](#member-79) | Returns the truncated remainder of every component by a scalar. |
| [`public static Electron2D.Vector3 op_Multiply(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-80) | Multiplies two vectors componentwise. |
| [`public static Electron2D.Vector3 op_Multiply(Electron2D.Vector3 vector, System.Single scalar)`](#member-81) | Multiplies a vector by a scalar. |
| [`public static Electron2D.Vector3 op_Multiply(System.Single scalar, Electron2D.Vector3 vector)`](#member-82) | Multiplies a scalar by a vector. |
| [`public static Electron2D.Vector3 op_Subtraction(Electron2D.Vector3 left, Electron2D.Vector3 right)`](#member-83) | Subtracts two vectors componentwise. |
| [`public static Electron2D.Vector3 op_UnaryNegation(Electron2D.Vector3 value)`](#member-84) | Negates every component. |
| [`public static Electron2D.Vector3 op_UnaryPlus(Electron2D.Vector3 value)`](#member-85) | Returns a vector unchanged. |
| [`public enum Electron2D.Vector3Axis`](#member-86) | Identifies one vector component. |
| [`public const Electron2D.Vector3Axis X = 0`](#member-87) | Identifies the X component. |
| [`public const Electron2D.Vector3Axis Y = 1`](#member-88) | Identifies the Y component. |
| [`public const Electron2D.Vector3Axis Z = 2`](#member-89) | Identifies the Z component. |

## Member contracts

<a id="member-1"></a>
### `public Vector3(Electron2D.Vector3i value)`

Initializes a floating-point vector from an integer vector.

- `value`: The integer vector to convert.
- Note: Large integer components can lose low-order precision during conversion.

<a id="member-2"></a>
### `public Vector3(System.Single x, System.Single y, System.Single z)`

Initializes a vector from three components.

- `x`: The X component.
- `y`: The Y component.
- `z`: The Z component.

<a id="member-3"></a>
### `public static Electron2D.Vector3 Back { get;  }`

Gets the positive Z unit vector.


<a id="member-4"></a>
### `public static Electron2D.Vector3 Down { get;  }`

Gets the negative Y unit vector.


<a id="member-5"></a>
### `public static Electron2D.Vector3 Forward { get;  }`

Gets the negative Z unit vector.


<a id="member-6"></a>
### `public static Electron2D.Vector3 Inf { get;  }`

Gets the vector whose components are all positive infinity.

- Value: (+Infinity, +Infinity, +Infinity).

<a id="member-7"></a>
### `public System.Single this[System.Int32 index] { get; set; }`

Gets or sets a component by axis index.

- `index`: An index from zero through two for X, Y, or Z.
- Value: The selected component.
- Throws `T:System.ArgumentOutOfRangeException`: index is outside zero through two.

<a id="member-8"></a>
### `public static Electron2D.Vector3 Left { get;  }`

Gets the negative X unit vector.


<a id="member-9"></a>
### `public static Electron2D.Vector3 One { get;  }`

Gets the vector whose components are all one.

- Value: (1, 1, 1).

<a id="member-10"></a>
### `public static Electron2D.Vector3 Right { get;  }`

Gets the positive X unit vector.


<a id="member-11"></a>
### `public static Electron2D.Vector3 Up { get;  }`

Gets the positive Y unit vector.


<a id="member-12"></a>
### `public System.Single X`

Gets or sets the X component.


<a id="member-13"></a>
### `public System.Single Y`

Gets or sets the Y component.


<a id="member-14"></a>
### `public System.Single Z`

Gets or sets the Z component.


<a id="member-15"></a>
### `public static Electron2D.Vector3 Zero { get;  }`

Gets the zero vector.

- Value: (0, 0, 0).

<a id="member-16"></a>
### `public Electron2D.Vector3 Abs()`

Returns the componentwise absolute value.

- Returns: A vector with nonnegative components, except that NaN remains NaN.

<a id="member-17"></a>
### `public System.Single AngleTo(Electron2D.Vector3 to)`

Returns the unsigned angle to another vector in radians.

- `to`: The other vector.
- Returns: The angle in radians.

<a id="member-18"></a>
### `public Electron2D.Vector3 BezierDerivative(Electron2D.Vector3 control1, Electron2D.Vector3 control2, Electron2D.Vector3 end, System.Single t)`

Returns the derivative of a cubic Bezier curve at a parameter.

- `control1`: The first control point.
- `control2`: The second control point.
- `end`: The end point.
- `t`: The curve parameter.
- Returns: The curve derivative.

<a id="member-19"></a>
### `public Electron2D.Vector3 BezierInterpolate(Electron2D.Vector3 control1, Electron2D.Vector3 control2, Electron2D.Vector3 end, System.Single t)`

Returns a point on a cubic Bezier curve.

- `control1`: The first control point.
- `control2`: The second control point.
- `end`: The end point.
- `t`: The curve parameter.
- Returns: The interpolated point.

<a id="member-20"></a>
### `public Electron2D.Vector3 Bounce(Electron2D.Vector3 normal)`

Returns the vector bounced from a plane with the supplied unit normal.

- `normal`: The unit plane normal.
- Returns: The bounced vector.

<a id="member-21"></a>
### `public Electron2D.Vector3 Ceil()`

Rounds every component upward toward positive infinity.

- Returns: The componentwise ceiling.

<a id="member-22"></a>
### `public Electron2D.Vector3 Clamp(Electron2D.Vector3 min, Electron2D.Vector3 max)`

Clamps each component between corresponding vector bounds.

- `min`: The componentwise lower bounds.
- `max`: The componentwise upper bounds.
- Returns: The clamped vector.
- Throws `T:System.ArgumentException`: A lower bound is greater than its corresponding upper bound.

<a id="member-23"></a>
### `public Electron2D.Vector3 Clamp(System.Single min, System.Single max)`

Clamps every component between scalar bounds.

- `min`: The lower bound.
- `max`: The upper bound.
- Returns: The clamped vector.
- Throws `T:System.ArgumentException`: min is greater than max.

<a id="member-24"></a>
### `public Electron2D.Vector3 Cross(Electron2D.Vector3 with)`

Returns the cross product with another vector.

- `with`: The other vector.
- Returns: The vector perpendicular to both inputs.

<a id="member-25"></a>
### `public Electron2D.Vector3 CubicInterpolate(Electron2D.Vector3 b, Electron2D.Vector3 preA, Electron2D.Vector3 postB, System.Single weight)`

Performs Catmull-Rom cubic interpolation between this vector and another.

- `b`: The destination vector.
- `preA`: The control vector before this vector.
- `postB`: The control vector after b.
- `weight`: The interpolation weight; values outside zero through one extrapolate.
- Returns: The interpolated vector.

<a id="member-26"></a>
### `public Electron2D.Vector3 CubicInterpolateInTime(Electron2D.Vector3 b, Electron2D.Vector3 preA, Electron2D.Vector3 postB, System.Single weight, System.Single bTime, System.Single preATime, System.Single postBTime)`

Performs time-aware Barry-Goldman cubic interpolation.

- `b`: The destination vector.
- `preA`: The control vector before this vector.
- `postB`: The control vector after b.
- `weight`: The interpolation weight.
- `bTime`: The time assigned to b relative to this vector at zero.
- `preATime`: The time assigned to preA.
- `postBTime`: The time assigned to postB.
- Returns: The time-aware interpolated vector.

<a id="member-27"></a>
### `public System.Void Deconstruct(out System.Single x, out System.Single y, out System.Single z)`

Deconstructs the vector into its three components.

- `x`: Receives F:Electron2D.Vector3.X.
- `y`: Receives F:Electron2D.Vector3.Y.
- `z`: Receives F:Electron2D.Vector3.Z.

<a id="member-28"></a>
### `public Electron2D.Vector3 DirectionTo(Electron2D.Vector3 to)`

Returns the normalized direction from this point to another point.

- `to`: The destination point.
- Returns: The normalized difference, or P:Electron2D.Vector3.Zero when the difference is zero or non-finite.

<a id="member-29"></a>
### `public System.Single DistanceSquaredTo(Electron2D.Vector3 to)`

Returns the squared Euclidean distance to another point.

- `to`: The destination point.
- Returns: The squared distance.

<a id="member-30"></a>
### `public System.Single DistanceTo(Electron2D.Vector3 to)`

Returns the Euclidean distance to another point.

- `to`: The destination point.
- Returns: The distance.

<a id="member-31"></a>
### `public System.Single Dot(Electron2D.Vector3 with)`

Returns the dot product with another vector.

- `with`: The other vector.
- Returns: The sum of the three component products.

<a id="member-32"></a>
### `public virtual System.Boolean Equals(Electron2D.Vector3 other)`

Tests every component for exact equality.

- `other`: The vector to compare.
- Returns: true when all corresponding components are equal.

<a id="member-33"></a>
### `public override System.Boolean Equals(System.Object obj)`

Tests whether another object is an exactly equal vector.

- `obj`: The object to compare.
- Returns: true when obj is a vector with equal components.

<a id="member-34"></a>
### `public Electron2D.Vector3 Floor()`

Rounds every component downward toward negative infinity.

- Returns: The componentwise floor.

<a id="member-35"></a>
### `public override System.Int32 GetHashCode()`

Returns a hash code based on all components.

- Returns: The component hash code.

<a id="member-36"></a>
### `public Electron2D.Vector3 Inverse()`

Returns the componentwise reciprocal.

- Returns: (1 / X, 1 / Y, 1 / Z), including IEEE 754 zero-division behavior.

<a id="member-37"></a>
### `public System.Boolean IsEqualApprox(Electron2D.Vector3 other)`

Tests every component for scale-aware approximate equality.

- `other`: The vector to compare.
- Returns: true when all corresponding components are approximately equal.

<a id="member-38"></a>
### `public System.Boolean IsFinite()`

Tests whether every component is finite.

- Returns: true when no component is NaN or infinity.

<a id="member-39"></a>
### `public System.Boolean IsNormalized()`

Tests whether the squared length is approximately one.

- Returns: true when the vector is approximately unit length.

<a id="member-40"></a>
### `public System.Boolean IsZeroApprox()`

Tests whether every component is approximately zero.

- Returns: true when every component is within the zero tolerance.

<a id="member-41"></a>
### `public System.Single Length()`

Returns the Euclidean length.

- Returns: The square root of M:Electron2D.Vector3.LengthSquared.

<a id="member-42"></a>
### `public System.Single LengthSquared()`

Returns the squared Euclidean length.

- Returns: The sum of the three squared components.

<a id="member-43"></a>
### `public Electron2D.Vector3 Lerp(Electron2D.Vector3 to, System.Single weight)`

Linearly interpolates or extrapolates toward another vector.

- `to`: The destination vector.
- `weight`: The interpolation weight.
- Returns: The componentwise linear interpolation.

<a id="member-44"></a>
### `public Electron2D.Vector3 LimitLength(System.Single length = 1f)`

Restricts the vector length to a maximum.

- `length`: The maximum length.
- Returns: The capped vector.
- A negative cap reverses a nonzero vector; division by current length precedes multiplication by the cap.

<a id="member-45"></a>
### `public Electron2D.Vector3 Max(Electron2D.Vector3 with)`

Returns the componentwise maximum with another vector.

- `with`: The other vector.
- Returns: The componentwise maximum.

<a id="member-46"></a>
### `public Electron2D.Vector3 Max(System.Single with)`

Returns the componentwise maximum with a scalar.

- `with`: The scalar compared with every component.
- Returns: The componentwise maximum.

<a id="member-47"></a>
### `public Electron2D.Vector3Axis MaxAxisIndex()`

Returns the axis containing the greatest component.

- Returns: F:Electron2D.Vector3Axis.X when all components are equal; otherwise the first greatest axis.

<a id="member-48"></a>
### `public Electron2D.Vector3 Min(Electron2D.Vector3 with)`

Returns the componentwise minimum with another vector.

- `with`: The other vector.
- Returns: The componentwise minimum.

<a id="member-49"></a>
### `public Electron2D.Vector3 Min(System.Single with)`

Returns the componentwise minimum with a scalar.

- `with`: The scalar compared with every component.
- Returns: The componentwise minimum.

<a id="member-50"></a>
### `public Electron2D.Vector3Axis MinAxisIndex()`

Returns the axis containing the least component.

- Returns: F:Electron2D.Vector3Axis.Z when all components are equal; otherwise the last least axis.
- NaN components follow the pinned X/Y/Z branches and may select an unordered axis.

<a id="member-51"></a>
### `public Electron2D.Vector3 MoveToward(Electron2D.Vector3 to, System.Single delta)`

Moves toward another vector by a signed distance without passing it.

- `to`: The destination.
- `delta`: The signed travel distance.
- Returns: The moved vector, or `to` at a separation below `0.00001` even for a negative step.

<a id="member-52"></a>
### `public Electron2D.Vector3 Normalized()`

Returns this vector scaled to unit length.

- Returns: A normalized vector, or P:Electron2D.Vector3.Zero when the input is zero or non-finite.

<a id="member-53"></a>
### `public static Electron2D.Vector3 OctahedronDecode(Electron2D.Vector2 uv)`

Decodes an octahedrally packed unit vector from a two-component value.

- `uv`: The encoded components; values outside the unit square retain their source coordinates.
- Returns: The decoded unit vector.
- The fold correction clamps to `[0,1]` before normalization.

<a id="member-54"></a>
### `public Electron2D.Vector2 OctahedronEncode()`

Octahedrally packs a unit vector into a two-component value.

- Returns: The packed components in the unit square.
- Encoding a zero vector returns NaN components.

<a id="member-55"></a>
### `public Electron2D.Vector3 PosMod(Electron2D.Vector3 mod)`

Applies componentwise positive modulus.

- `mod`: The component divisors.
- Returns: The componentwise canonical remainders using each divisor's sign.

<a id="member-56"></a>
### `public Electron2D.Vector3 PosMod(System.Single mod)`

Applies positive modulus to every component.

- `mod`: The scalar divisor.
- Returns: The componentwise canonical remainder using the divisor's sign.

<a id="member-57"></a>
### `public Electron2D.Vector3 Project(Electron2D.Vector3 onNormal)`

Projects this vector onto another vector.

- `onNormal`: The projection direction.
- Returns: The parallel component.

<a id="member-58"></a>
### `public Electron2D.Vector3 Reflect(Electron2D.Vector3 normal)`

Reflects this vector across a plane with a unit normal.

- `normal`: The unit plane normal.
- Returns: The reflected vector.
- The normal is multiplied by two before the dot scalar to retain finite components near float limits.

<a id="member-59"></a>
### `public Electron2D.Vector3 Rotated(Electron2D.Vector3 axis, System.Single angle)`

Rotates this vector about a unit axis by an angle in radians.

- `axis`: The unit rotation axis.
- `angle`: The rotation angle in radians.
- Returns: The rotated vector.
- Uses the pinned axis-angle matrix coefficient order internally without exposing a spatial matrix type.

<a id="member-60"></a>
### `public Electron2D.Vector3 Round()`

Rounds every component to the nearest integer using midpoint-to-even behavior.

- Returns: The componentwise rounded vector.

<a id="member-61"></a>
### `public Electron2D.Vector3 Sign()`

Returns the sign of every component.

- Returns: Components containing negative one, zero, or positive one.
- Throws `T:System.ArithmeticException`: A component is NaN.

<a id="member-62"></a>
### `public System.Single SignedAngleTo(Electron2D.Vector3 to, Electron2D.Vector3 axis)`

Returns the signed angle to another vector about an axis.

- `to`: The other vector.
- `axis`: The axis selecting the angle sign.
- Returns: The signed angle in radians.

<a id="member-63"></a>
### `public Electron2D.Vector3 Slerp(Electron2D.Vector3 to, System.Single weight)`

Interpolates direction on the unit sphere and linearly interpolates length.

- `to`: The destination vector.
- `weight`: The interpolation weight.
- Returns: The interpolated vector, or linear interpolation for zero length or parallel inputs.
- Squared-length and rotation-axis checks precede interpolation; non-finite values follow IEEE arithmetic.

<a id="member-64"></a>
### `public Electron2D.Vector3 Slide(Electron2D.Vector3 normal)`

Removes the component along a unit normal.

- `normal`: The unit normal.
- Returns: The remaining tangent component.

<a id="member-65"></a>
### `public Electron2D.Vector3 Snapped(Electron2D.Vector3 step)`

Snaps each component to the nearest multiple of the corresponding step.

- `step`: The componentwise step. A zero component leaves the corresponding value unchanged.
- Returns: The snapped vector.

<a id="member-66"></a>
### `public Electron2D.Vector3 Snapped(System.Single step)`

Snaps every component to the nearest multiple of a scalar step.

- `step`: The scalar step. Zero leaves every value unchanged.
- Returns: The snapped vector.

<a id="member-67"></a>
### `public override System.String ToString()`

Formats every component using invariant culture.

- Returns: A parenthesized component tuple.

<a id="member-68"></a>
### `public System.String ToString(System.String format)`

Formats every component with a numeric format and invariant culture.

- `format`: A standard or custom numeric format, or null for the default.
- Returns: A parenthesized component tuple.
- Throws `T:System.FormatException`: format is invalid.

<a id="member-69"></a>
### `public static Electron2D.Vector3 op_Addition(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Adds two vectors componentwise.

- `left`: The first vector.
- `right`: The second vector.
- Returns: The componentwise sum.

<a id="member-70"></a>
### `public static Electron2D.Vector3 op_Division(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Divides two vectors componentwise.

- `left`: The dividend.
- `right`: The component divisors.
- Returns: The IEEE 754 componentwise quotient.

<a id="member-71"></a>
### `public static Electron2D.Vector3 op_Division(Electron2D.Vector3 vector, System.Single divisor)`

Divides every component by a scalar.

- `vector`: The dividend.
- `divisor`: The scalar divisor.
- Returns: The IEEE 754 componentwise quotient.

<a id="member-72"></a>
### `public static System.Boolean op_Equality(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Tests every component for exact equality.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when all corresponding components are equal.

<a id="member-73"></a>
### `public static System.Boolean op_GreaterThan(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left sorts after right.

<a id="member-74"></a>
### `public static System.Boolean op_GreaterThanOrEqual(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left does not sort before right.

<a id="member-75"></a>
### `public static System.Boolean op_Inequality(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Tests whether any component differs.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when a corresponding component differs.

<a id="member-76"></a>
### `public static System.Boolean op_LessThan(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left sorts before right.

<a id="member-77"></a>
### `public static System.Boolean op_LessThanOrEqual(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Compares vectors lexicographically by X, Y, then Z.

- `left`: The first vector.
- `right`: The second vector.
- Returns: true when left does not sort after right.

<a id="member-78"></a>
### `public static Electron2D.Vector3 op_Modulus(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Returns the truncated componentwise remainder of two vectors.

- `left`: The dividend.
- `right`: The component divisors.
- Returns: The componentwise remainder.

<a id="member-79"></a>
### `public static Electron2D.Vector3 op_Modulus(Electron2D.Vector3 vector, System.Single divisor)`

Returns the truncated remainder of every component by a scalar.

- `vector`: The dividend.
- `divisor`: The scalar divisor.
- Returns: The componentwise remainder.

<a id="member-80"></a>
### `public static Electron2D.Vector3 op_Multiply(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Multiplies two vectors componentwise.

- `left`: The first vector.
- `right`: The second vector.
- Returns: The componentwise product.

<a id="member-81"></a>
### `public static Electron2D.Vector3 op_Multiply(Electron2D.Vector3 vector, System.Single scalar)`

Multiplies a vector by a scalar.

- `vector`: The vector.
- `scalar`: The scalar multiplier.
- Returns: The componentwise product.

<a id="member-82"></a>
### `public static Electron2D.Vector3 op_Multiply(System.Single scalar, Electron2D.Vector3 vector)`

Multiplies a scalar by a vector.

- `scalar`: The scalar multiplier.
- `vector`: The vector.
- Returns: The componentwise product.

<a id="member-83"></a>
### `public static Electron2D.Vector3 op_Subtraction(Electron2D.Vector3 left, Electron2D.Vector3 right)`

Subtracts two vectors componentwise.

- `left`: The minuend.
- `right`: The subtrahend.
- Returns: The componentwise difference.

<a id="member-84"></a>
### `public static Electron2D.Vector3 op_UnaryNegation(Electron2D.Vector3 value)`

Negates every component.

- `value`: The vector to negate.
- Returns: The componentwise negation.

<a id="member-85"></a>
### `public static Electron2D.Vector3 op_UnaryPlus(Electron2D.Vector3 value)`

Returns a vector unchanged.

- `value`: The vector.
- Returns: value.

<a id="member-86"></a>
### `public enum Electron2D.Vector3Axis`

Identifies one vector component.


<a id="member-87"></a>
### `public const Electron2D.Vector3Axis X = 0`

Identifies the X component.


<a id="member-88"></a>
### `public const Electron2D.Vector3Axis Y = 1`

Identifies the Y component.


<a id="member-89"></a>
### `public const Electron2D.Vector3Axis Z = 2`

Identifies the Z component.


## Verification and limits

`tests/Electron2D.Tests/Program.cs` checks Vector3 layout, arithmetic, conversion boundaries, configuration and packed-scene storage. Shader material checks cover float3/int3/uint3 descriptors, RGB aliases and arrays. The current native gate is Linux Wayland; no other platform ABI or package claim follows from these managed tests.

## Decisions

- [ADR 0033: Dimensioned vector family](../decisions/core-math.md#adr-0033)
- [ADR 0004: 2D-only engine](../decisions/product.md#adr-0004)
