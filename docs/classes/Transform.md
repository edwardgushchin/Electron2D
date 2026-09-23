# Transform

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Transform.cs`](../../src/Core/Math/Transform.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Transform`

> Represents a two-dimensional affine transformation as three column vectors.

## Description

Represents a two-dimensional affine transformation as three column vectors.

`Transform` is a mutable 24-byte affine-transform value composed of three sequential `Electron2D.Vector2` columns. `X` and `Y` are the basis axes; `Origin` is translation. The value represents clockwise screen-space rotation, translation, non-uniform scale, reflection, and skew without owning a renderer, native handle, identity, callback, or managed reference.

Zero initialization produces six zero components and is intentionally different from `Identity`. Ordinary value assignment copies all components. The type does not derive from `ElectronObject` and has no disposal lifecycle.

[`Transform.X`](Transform.md#f-electron2d-transform-x) and [`Transform.Y`](Transform.md#f-electron2d-transform-y) form the two-by-two basis; [`Transform.Origin`](Transform.md#f-electron2d-transform-origin) stores translation.
The value can represent translation, clockwise rotation in screen coordinates, non-uniform scale, reflection,
and skew. The zero-initialized value is a zero matrix, not [`Transform.Identity`](Transform.md#p-electron2d-transform-identity).

ShaderMaterial accepts this value for float2x2 parameters and arrays, storing only the X/Y basis. Parameter readers return zero Origin; unassigned matrix parameters start at Identity. See [matrix parameter mapping](../components/shader-materials.md#matrix-parameters) for source-language multiplication and layout rules. This does not change the struct's ordinary zero-initialization behavior.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var transform = new Transform(MathF.Pi / 4f, new Vector2(32f, 16f));
Vector2 worldPoint = transform * localPoint;
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Transform(Vector2 xAxis, Vector2 yAxis, Vector2 origin)`](#m-electron2d-transform-ctor-electron2d-vector2-electron2d-vector2-electron2d-vector2) | Initializes a transform from its three matrix columns. |
| [`public Transform(float xx, float xy, float yx, float yy, float ox, float oy)`](#m-electron2d-transform-ctor-system-single-system-single-system-single-system-single-system-single-system-single) | Initializes a transform from six column-major components. |
| [`public Transform(float rotation, Vector2 origin)`](#m-electron2d-transform-ctor-system-single-electron2d-vector2) | Initializes a rotation and translation transform. |
| [`public Transform(float rotation, Vector2 scale, float skew, Vector2 origin)`](#m-electron2d-transform-ctor-system-single-electron2d-vector2-system-single-electron2d-vector2) | Initializes a transform from rotation, scale, skew, and translation. |

## Properties

| Member | Description |
| --- | --- |
| [`public static Transform Identity { get; }`](#p-electron2d-transform-identity) | Gets the identity transform. |
| [`public static Transform FlipX { get; }`](#p-electron2d-transform-flipx) | Gets a transform that reflects across the vertical axis by negating horizontal coordinates. |
| [`public static Transform FlipY { get; }`](#p-electron2d-transform-flipy) | Gets a transform that reflects across the horizontal axis by negating vertical coordinates. |
| [`public float Rotation { get; }`](#p-electron2d-transform-rotation) | Gets the clockwise screen-space rotation in radians. |
| [`public Vector2 Scale { get; }`](#p-electron2d-transform-scale) | Gets the lengths of the basis axes with reflection encoded in the vertical component. |
| [`public float Skew { get; }`](#p-electron2d-transform-skew) | Gets the angular skew between the basis axes in radians. |
| [`public Vector2 this[int column] { get; set; }`](#p-electron2d-transform-item-system-int32) | Gets or sets a complete matrix column. |
| [`public float this[int column, int row] { get; set; }`](#p-electron2d-transform-item-system-int32-system-int32) | Gets or sets one matrix component using column-major coordinates. |

## Methods

| Member | Description |
| --- | --- |
| [`public Transform AffineInverse()`](#m-electron2d-transform-affineinverse) | Returns the general affine inverse. |
| [`public Vector2 BasisXform(Vector2 vector)`](#m-electron2d-transform-basisxform-electron2d-vector2) | Transforms a vector by the basis while ignoring translation. |
| [`public Vector2 BasisXformInv(Vector2 vector)`](#m-electron2d-transform-basisxforminv-electron2d-vector2) | Transforms a vector by the transposed basis while ignoring translation. |
| [`public float Determinant()`](#m-electron2d-transform-determinant) | Returns the determinant of the two-by-two basis. |
| [`public Transform InterpolateWith(Transform other, float weight)`](#m-electron2d-transform-interpolatewith-electron2d-transform-system-single) | Interpolates or extrapolates decomposed transform components. |
| [`public Transform Inverse()`](#m-electron2d-transform-inverse) | Returns the fast inverse for an orthonormal basis. |
| [`public bool IsConformal()`](#m-electron2d-transform-isconformal) | Tests whether the basis preserves angles up to uniform scale and optional reflection. |
| [`public bool IsEqualApprox(Transform other)`](#m-electron2d-transform-isequalapprox-electron2d-transform) | Tests all three columns for scale-aware approximate equality. |
| [`public bool IsFinite()`](#m-electron2d-transform-isfinite) | Tests whether every matrix component is finite. |
| [`public Transform LookingAt(Vector2 target)`](#m-electron2d-transform-lookingat-electron2d-vector2) | Returns a rotation-only transform turned toward a target through this transform's affine local space. |
| [`public Transform Orthonormalized()`](#m-electron2d-transform-orthonormalized) | Returns a transform with a Gram-Schmidt orthonormalized basis. |
| [`public Transform Rotated(float angle)`](#m-electron2d-transform-rotated-system-single) | Applies a rotation in the global or parent coordinate frame. |
| [`public Transform RotatedLocal(float angle)`](#m-electron2d-transform-rotatedlocal-system-single) | Applies a rotation in the local coordinate frame. |
| [`public Transform Scaled(Vector2 scale)`](#m-electron2d-transform-scaled-electron2d-vector2) | Applies componentwise scale in the global or parent coordinate frame. |
| [`public Transform ScaledLocal(Vector2 scale)`](#m-electron2d-transform-scaledlocal-electron2d-vector2) | Applies scale in the local coordinate frame. |
| [`public Transform Translated(Vector2 offset)`](#m-electron2d-transform-translated-electron2d-vector2) | Applies translation in the global or parent coordinate frame. |
| [`public Transform TranslatedLocal(Vector2 offset)`](#m-electron2d-transform-translatedlocal-electron2d-vector2) | Applies translation in the local coordinate frame. |
| [`public override bool Equals(object obj)`](#m-electron2d-transform-equals-system-object) | Tests whether another object is an exactly equal transform. |
| [`public bool Equals(Transform other)`](#m-electron2d-transform-equals-electron2d-transform) | Tests all matrix components for exact equality. |
| [`public override int GetHashCode()`](#m-electron2d-transform-gethashcode) | Returns a hash code based on all three columns. |
| [`public override string ToString()`](#m-electron2d-transform-tostring) | Formats the three columns using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-transform-tostring-system-string) | Formats the three columns with a numeric format and invariant culture. |

## Fields

| Member | Description |
| --- | --- |
| [`public Vector2 X`](#f-electron2d-transform-x) | Gets or sets the basis X axis, which is matrix column zero. |
| [`public Vector2 Y`](#f-electron2d-transform-y) | Gets or sets the basis Y axis, which is matrix column one. |
| [`public Vector2 Origin`](#f-electron2d-transform-origin) | Gets or sets the translation offset, which is matrix column two. |

## Operators

| Member | Description |
| --- | --- |
| [`public static Transform operator *(Transform left, Transform right)`](#m-electron2d-transform-op-multiply-electron2d-transform-electron2d-transform) | Composes a parent transform with a child transform. |
| [`public static Vector2 operator *(Transform transform, Vector2 point)`](#m-electron2d-transform-op-multiply-electron2d-transform-electron2d-vector2) | Transforms a point by the basis and translation. |
| [`public static Rect operator *(Transform transform, Rect rectangle)`](#m-electron2d-transform-op-multiply-electron2d-transform-electron2d-rect) | Transforms a rectangle and returns the axis-aligned bounds of its four transformed corners. |
| [`public static Vector2 operator *(Vector2 point, Transform transform)`](#m-electron2d-transform-op-multiply-electron2d-vector2-electron2d-transform) | Applies the inverse orthonormal transform to a point. |
| [`public static Rect operator *(Rect rectangle, Transform transform)`](#m-electron2d-transform-op-multiply-electron2d-rect-electron2d-transform) | Inverse-transforms a rectangle under an orthonormal-basis precondition. |
| [`public static Vector2[] operator *(Transform transform, Vector2[] points)`](#m-electron2d-transform-op-multiply-electron2d-transform-electron2d-vector2-array) | Transforms every point into a newly allocated array. |
| [`public static Vector2[] operator *(Vector2[] points, Transform transform)`](#m-electron2d-transform-op-multiply-electron2d-vector2-array-electron2d-transform) | Inverse-transforms every point by an orthonormal transform into a newly allocated array. |
| [`public static Transform operator *(Transform transform, float scalar)`](#m-electron2d-transform-op-multiply-electron2d-transform-system-single) | Multiplies every matrix component, including translation, by a scalar. |
| [`public static Transform operator /(Transform transform, float scalar)`](#m-electron2d-transform-op-division-electron2d-transform-system-single) | Divides every matrix component, including translation, by a scalar. |
| [`public static bool operator ==(Transform left, Transform right)`](#m-electron2d-transform-op-equality-electron2d-transform-electron2d-transform) | Tests all matrix components for exact equality. |
| [`public static bool operator !=(Transform left, Transform right)`](#m-electron2d-transform-op-inequality-electron2d-transform-electron2d-transform) | Tests whether any matrix component differs under exact equality. |

## Constructor Descriptions

<a id="m-electron2d-transform-ctor-electron2d-vector2-electron2d-vector2-electron2d-vector2"></a>
### `public Transform(Vector2 xAxis, Vector2 yAxis, Vector2 origin)`

Initializes a transform from its three matrix columns.

**Parameters**

- `xAxis`: The basis X axis.
- `yAxis`: The basis Y axis.
- `origin`: The translation offset.

<a id="m-electron2d-transform-ctor-system-single-system-single-system-single-system-single-system-single-system-single"></a>
### `public Transform(float xx, float xy, float yx, float yy, float ox, float oy)`

Initializes a transform from six column-major components.

**Parameters**

- `xx`: The X component of [`Transform.X`](Transform.md#f-electron2d-transform-x).
- `xy`: The Y component of [`Transform.X`](Transform.md#f-electron2d-transform-x).
- `yx`: The X component of [`Transform.Y`](Transform.md#f-electron2d-transform-y).
- `yy`: The Y component of [`Transform.Y`](Transform.md#f-electron2d-transform-y).
- `ox`: The X component of [`Transform.Origin`](Transform.md#f-electron2d-transform-origin).
- `oy`: The Y component of [`Transform.Origin`](Transform.md#f-electron2d-transform-origin).

<a id="m-electron2d-transform-ctor-system-single-electron2d-vector2"></a>
### `public Transform(float rotation, Vector2 origin)`

Initializes a rotation and translation transform.

**Parameters**

- `rotation`: The clockwise screen-space angle in radians.
- `origin`: The translation offset.

<a id="m-electron2d-transform-ctor-system-single-electron2d-vector2-system-single-electron2d-vector2"></a>
### `public Transform(float rotation, Vector2 scale, float skew, Vector2 origin)`

Initializes a transform from rotation, scale, skew, and translation.

**Parameters**

- `rotation`: The clockwise screen-space rotation in radians.
- `scale`: The horizontal and vertical scale factors.
- `skew`: The angular skew in radians.
- `origin`: The translation offset.

## Property Descriptions

<a id="p-electron2d-transform-identity"></a>
### `public static Transform Identity { get; }`

Gets the identity transform.

**Value:** A transform with unit basis axes and zero origin.

<a id="p-electron2d-transform-flipx"></a>
### `public static Transform FlipX { get; }`

Gets a transform that reflects across the vertical axis by negating horizontal coordinates.

**Value:** A transform with basis axes `(-1, 0)` and `(0, 1)`.

<a id="p-electron2d-transform-flipy"></a>
### `public static Transform FlipY { get; }`

Gets a transform that reflects across the horizontal axis by negating vertical coordinates.

**Value:** A transform with basis axes `(1, 0)` and `(0, -1)`.

<a id="p-electron2d-transform-rotation"></a>
### `public float Rotation { get; }`

Gets the clockwise screen-space rotation in radians.

**Value:** The angle of [`Transform.X`](Transform.md#f-electron2d-transform-x), measured from positive X toward positive Y.

<a id="p-electron2d-transform-scale"></a>
### `public Vector2 Scale { get; }`

Gets the lengths of the basis axes with reflection encoded in the vertical component.

**Value:** [`Transform.X`](Transform.md#f-electron2d-transform-x) length and signed [`Transform.Y`](Transform.md#f-electron2d-transform-y) length. A negative determinant makes the vertical component
negative; a zero or unordered determinant makes it zero.

<a id="p-electron2d-transform-skew"></a>
### `public float Skew { get; }`

Gets the angular skew between the basis axes in radians.

**Value:** Zero for an orthogonal basis, with reflection accounted for by the determinant sign.

<a id="p-electron2d-transform-item-system-int32"></a>
### `public Vector2 this[int column] { get; set; }`

Gets or sets a complete matrix column.

**Parameters**

- `column`: Zero for [`Transform.X`](Transform.md#f-electron2d-transform-x), one for [`Transform.Y`](Transform.md#f-electron2d-transform-y), or two for [`Transform.Origin`](Transform.md#f-electron2d-transform-origin).

**Value:** The selected column.

**Exceptions**

- `ArgumentOutOfRangeException`: `column` is outside zero through two.

<a id="p-electron2d-transform-item-system-int32-system-int32"></a>
### `public float this[int column, int row] { get; set; }`

Gets or sets one matrix component using column-major coordinates.

**Parameters**

- `column`: The matrix column from zero through two.
- `row`: The matrix row, zero for X or one for Y.

**Value:** The selected floating-point component.

**Exceptions**

- `ArgumentOutOfRangeException`: `column` is outside zero through two, or `row` is outside zero through one.

## Method Descriptions

<a id="m-electron2d-transform-affineinverse"></a>
### `public Transform AffineInverse()`

Returns the general affine inverse.

**Returns:** A transform that composes with this transform to produce the identity, within floating-point precision.

**Exceptions**

- `InvalidOperationException`: The basis determinant is exactly zero.

<a id="m-electron2d-transform-basisxform-electron2d-vector2"></a>
### `public Vector2 BasisXform(Vector2 vector)`

Transforms a vector by the basis while ignoring translation.

**Parameters**

- `vector`: The vector to transform.

**Returns:** The vector multiplied by the two-by-two basis.

<a id="m-electron2d-transform-basisxforminv-electron2d-vector2"></a>
### `public Vector2 BasisXformInv(Vector2 vector)`

Transforms a vector by the transposed basis while ignoring translation.

**Parameters**

- `vector`: The vector to transform.

**Returns:** The vector multiplied by the transposed basis.

**Remarks:** This is the inverse basis transform only when the basis is orthonormal. For scaled or skewed transforms, use
`transform.AffineInverse().BasisXform(vector)`.

<a id="m-electron2d-transform-determinant"></a>
### `public float Determinant()`

Returns the determinant of the two-by-two basis.

**Returns:** Zero for a singular basis, a negative value for a reflected basis, or a positive value otherwise.

<a id="m-electron2d-transform-interpolatewith-electron2d-transform-system-single"></a>
### `public Transform InterpolateWith(Transform other, float weight)`

Interpolates or extrapolates decomposed transform components.

**Parameters**

- `other`: The destination transform.
- `weight`: The interpolation weight; values outside zero through one extrapolate.

**Returns:** A transform built from shortest-path angle interpolation, linear scale, skew, and origin interpolation.

<a id="m-electron2d-transform-inverse"></a>
### `public Transform Inverse()`

Returns the fast inverse for an orthonormal basis.

**Returns:** The transposed basis and corresponding inverse translation.

**Remarks:** This method assumes rotation or reflection without scale or skew and does not validate that precondition. Use
[`Transform.AffineInverse`](Transform.md#m-electron2d-transform-affineinverse) for a general invertible affine transform.

<a id="m-electron2d-transform-isconformal"></a>
### `public bool IsConformal()`

Tests whether the basis preserves angles up to uniform scale and optional reflection.

**Returns:** `true` for approximately orthogonal axes of approximately equal length.

<a id="m-electron2d-transform-isequalapprox-electron2d-transform"></a>
### `public bool IsEqualApprox(Transform other)`

Tests all three columns for scale-aware approximate equality.

**Parameters**

- `other`: The transform to compare.

**Returns:** `true` when every corresponding component is approximately equal.

<a id="m-electron2d-transform-isfinite"></a>
### `public bool IsFinite()`

Tests whether every matrix component is finite.

**Returns:** `true` when no component is NaN or infinity.

<a id="m-electron2d-transform-lookingat-electron2d-vector2"></a>
### `public Transform LookingAt(Vector2 target)`

Returns a rotation-only transform turned toward a target through this transform's affine local space.

**Parameters**

- `target`: The global target point.

**Returns:** A transform with the same origin and adjusted rotation; scale and skew are removed.

**Exceptions**

- `InvalidOperationException`: The basis determinant is exactly zero.

**Remarks:** The target is inverse-transformed and compensated by the signed basis scale before its angle is added to the
current rotation. For a skewed source this differs from using the raw global angle from the origin.

<a id="m-electron2d-transform-orthonormalized"></a>
### `public Transform Orthonormalized()`

Returns a transform with a Gram-Schmidt orthonormalized basis.

**Returns:** A copy with unit perpendicular axes and the original origin.

**Remarks:** A zero or linearly dependent axis normalizes to zero instead of producing non-finite components.

<a id="m-electron2d-transform-rotated-system-single"></a>
### `public Transform Rotated(float angle)`

Applies a rotation in the global or parent coordinate frame.

**Parameters**

- `angle`: The clockwise screen-space angle in radians.

**Returns:** The rotation transform multiplied on the left of this transform.

<a id="m-electron2d-transform-rotatedlocal-system-single"></a>
### `public Transform RotatedLocal(float angle)`

Applies a rotation in the local coordinate frame.

**Parameters**

- `angle`: The clockwise screen-space angle in radians.

**Returns:** The rotation transform multiplied on the right of this transform.

<a id="m-electron2d-transform-scaled-electron2d-vector2"></a>
### `public Transform Scaled(Vector2 scale)`

Applies componentwise scale in the global or parent coordinate frame.

**Parameters**

- `scale`: The scale along global X and Y.

**Returns:** A copy whose basis rows and origin are scaled componentwise.

<a id="m-electron2d-transform-scaledlocal-electron2d-vector2"></a>
### `public Transform ScaledLocal(Vector2 scale)`

Applies scale in the local coordinate frame.

**Parameters**

- `scale`: The scale along the local basis axes.

**Returns:** A copy whose X and Y columns are multiplied by their corresponding factors.

<a id="m-electron2d-transform-translated-electron2d-vector2"></a>
### `public Transform Translated(Vector2 offset)`

Applies translation in the global or parent coordinate frame.

**Parameters**

- `offset`: The global offset.

**Returns:** A copy with the offset added directly to its origin.

<a id="m-electron2d-transform-translatedlocal-electron2d-vector2"></a>
### `public Transform TranslatedLocal(Vector2 offset)`

Applies translation in the local coordinate frame.

**Parameters**

- `offset`: The offset expressed in the current basis.

**Returns:** A copy with the basis-transformed offset added to its origin.

<a id="m-electron2d-transform-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an exactly equal transform.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is a transform with equal components.

<a id="m-electron2d-transform-equals-electron2d-transform"></a>
### `public bool Equals(Transform other)`

Tests all matrix components for exact equality.

**Parameters**

- `other`: The transform to compare.

**Returns:** `true` when every corresponding component is exactly equal.

<a id="m-electron2d-transform-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on all three columns.

**Returns:** The component hash code.

<a id="m-electron2d-transform-tostring"></a>
### `public override string ToString()`

Formats the three columns using invariant culture.

**Returns:** A string containing the X axis, Y axis, and origin.

<a id="m-electron2d-transform-tostring-system-string"></a>
### `public string ToString(string format)`

Formats the three columns with a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default format.

**Returns:** A string containing the X axis, Y axis, and origin.

**Exceptions**

- `FormatException`: `format` is invalid.

## Field Descriptions

<a id="f-electron2d-transform-x"></a>
### `public Vector2 X`

Gets or sets the basis X axis, which is matrix column zero.

**Remarks:** Its length contributes the horizontal scale and its direction defines the transform rotation.

<a id="f-electron2d-transform-y"></a>
### `public Vector2 Y`

Gets or sets the basis Y axis, which is matrix column one.

**Remarks:** Its length contributes the vertical scale and its direction relative to [`Transform.X`](Transform.md#f-electron2d-transform-x) defines skew.

<a id="f-electron2d-transform-origin"></a>
### `public Vector2 Origin`

Gets or sets the translation offset, which is matrix column two.

## Operator Descriptions

<a id="m-electron2d-transform-op-multiply-electron2d-transform-electron2d-transform"></a>
### `public static Transform operator *(Transform left, Transform right)`

Composes a parent transform with a child transform.

**Parameters**

- `left`: The parent transform applied second.
- `right`: The child transform applied first.

**Returns:** The composed transform.

<a id="m-electron2d-transform-op-multiply-electron2d-transform-electron2d-vector2"></a>
### `public static Vector2 operator *(Transform transform, Vector2 point)`

Transforms a point by the basis and translation.

**Parameters**

- `transform`: The transform to apply.
- `point`: The point in local coordinates.

**Returns:** The transformed point.

<a id="m-electron2d-transform-op-multiply-electron2d-transform-electron2d-rect"></a>
### `public static Rect operator *(Transform transform, Rect rectangle)`

Transforms a rectangle and returns the axis-aligned bounds of its four transformed corners.

**Parameters**

- `transform`: The affine transform to apply.
- `rectangle`: The rectangle to transform.

**Returns:** The smallest axis-aligned rectangle enclosing all four transformed corners.

**Remarks:** Rotation, reflection, non-uniform scale, skew, zero size, and negative size are supported. The result is
normalized even when `rectangle` has a negative size.

<a id="m-electron2d-transform-op-multiply-electron2d-vector2-electron2d-transform"></a>
### `public static Vector2 operator *(Vector2 point, Transform transform)`

Applies the inverse orthonormal transform to a point.

**Parameters**

- `point`: The point in transformed coordinates.
- `transform`: The orthonormal transform to invert.

**Returns:** The point expressed in the transform's local coordinates.

**Remarks:** For scale or skew, multiply the point by [`Transform.AffineInverse`](Transform.md#m-electron2d-transform-affineinverse) instead.

<a id="m-electron2d-transform-op-multiply-electron2d-rect-electron2d-transform"></a>
### `public static Rect operator *(Rect rectangle, Transform transform)`

Inverse-transforms a rectangle under an orthonormal-basis precondition.

**Parameters**

- `rectangle`: The rectangle in transformed coordinates.
- `transform`: The orthonormal transform to invert.

**Returns:** The axis-aligned bounds of the inverse-transformed rectangle corners.

**Remarks:** This operator is equivalent to `transform.Inverse() * rectangle`. For scale or skew, use
`transform.AffineInverse() * rectangle` instead.

<a id="m-electron2d-transform-op-multiply-electron2d-transform-electron2d-vector2-array"></a>
### `public static Vector2[] operator *(Transform transform, Vector2[] points)`

Transforms every point into a newly allocated array.

**Parameters**

- `transform`: The transform to apply.
- `points`: The source points.

**Returns:** A new array containing transformed points in the original order.

**Exceptions**

- `ArgumentNullException`: `points` is `null`.

<a id="m-electron2d-transform-op-multiply-electron2d-vector2-array-electron2d-transform"></a>
### `public static Vector2[] operator *(Vector2[] points, Transform transform)`

Inverse-transforms every point by an orthonormal transform into a newly allocated array.

**Parameters**

- `points`: The source points.
- `transform`: The orthonormal transform to invert.

**Returns:** A new array containing inverse-transformed points in the original order.

**Exceptions**

- `ArgumentNullException`: `points` is `null`.

**Remarks:** For scale or skew, multiply the points by [`Transform.AffineInverse`](Transform.md#m-electron2d-transform-affineinverse) instead.

<a id="m-electron2d-transform-op-multiply-electron2d-transform-system-single"></a>
### `public static Transform operator *(Transform transform, float scalar)`

Multiplies every matrix component, including translation, by a scalar.

**Parameters**

- `transform`: The transform to scale.
- `scalar`: The scalar multiplier.

**Returns:** The componentwise product.

<a id="m-electron2d-transform-op-division-electron2d-transform-system-single"></a>
### `public static Transform operator /(Transform transform, float scalar)`

Divides every matrix component, including translation, by a scalar.

**Parameters**

- `transform`: The transform to divide.
- `scalar`: The scalar divisor.

**Returns:** The IEEE 754 componentwise quotient.

<a id="m-electron2d-transform-op-equality-electron2d-transform-electron2d-transform"></a>
### `public static bool operator ==(Transform left, Transform right)`

Tests all matrix components for exact equality.

**Parameters**

- `left`: The first transform.
- `right`: The second transform.

**Returns:** `true` when every corresponding component is exactly equal.

<a id="m-electron2d-transform-op-inequality-electron2d-transform-electron2d-transform"></a>
### `public static bool operator !=(Transform left, Transform right)`

Tests whether any matrix component differs under exact equality.

**Parameters**

- `left`: The first transform.
- `right`: The second transform.

**Returns:** `true` when at least one corresponding component differs.

## Matrix and ordering invariants

- Columns are laid out as `X=(xx,xy)`, `Y=(yx,yy)`, and `Origin=(ox,oy)`. On paper, the affine matrix is `[xx yx ox; xy yy oy]`.
- A point becomes `(xx*x + yx*y + ox, xy*x + yy*y + oy)`.
- Positive angles turn positive X toward positive Y, which appears clockwise in the engine's screen coordinate convention.
- `left * right` applies `right` first. This is the parent/child order used by [`Entity`](Entity.md).
- `Scaled` scales matrix rows and Origin componentwise. `ScaledLocal` scales the X and Y columns by `scale.X` and `scale.Y` respectively and preserves Origin.
- `Scale` places a negative-determinant reflection sign on Y. A zero or unordered determinant produces a zero reported Y scale.
- `Inverse`, `BasisXformInv`, and the reverse vector/array operators assume an orthonormal basis and intentionally do not validate it. `AffineInverse` is the general path.
- `AffineInverse` rejects an exactly zero determinant with `InvalidOperationException`; a very small nonzero determinant is accepted and follows IEEE overflow behavior.
- `Orthonormalized` keeps zero or dependent axes as zero rather than creating non-finite values. It does not promise two unit axes for degenerate input.
- Ordinary math retains IEEE values. `IsFinite` is the explicit validation operation.

## Error behavior

- Either indexer throws `ArgumentOutOfRangeException` for an invalid column or row.
- `AffineInverse` and singular `LookingAt` throw `InvalidOperationException` when the determinant is exactly zero.
- Array operators throw `ArgumentNullException` for a null array.
- `ToString(string?)` throws `FormatException` for an invalid numeric format.
- Scalar division by zero does not throw; it produces ordinary IEEE infinity or NaN components.

## Lifecycle, threading, and allocation

There is no lifecycle beyond value construction, mutation, and copying. Independent copies are safe to use on different threads. Concurrent mutation of the same storage location is an ordinary unsynchronized C# data race.

Constructors, scalar math, decomposition, composition, point transformation, inversion, comparison, and hashing allocate no managed memory after JIT warmup. Array operators allocate one destination array. String formatting allocates. Sequential 24-byte layout is verified locally but does not promise ABI equivalence with a future native backend.

## Dependencies and integration

The public type depends on canonical scalar [`MathF`](MathF.md), [`Vector2`](Vector2.md), [`Rect`](Rect.md), globalization, and interop metadata. [`ConfigFile`](ConfigFile.md) stores finite transforms using exact nested `X.X/Y`, `Y.X/Y`, and `Origin.X/Y` fields. Typed property descriptors and [`PackedScene`](PackedScene.md) preserve the reference-free value directly.

`Entity.Transform`, `Entity.GlobalTransform`, point conversion, reparenting, and relative transforms use this value directly. There is no public implicit or explicit conversion to another numerics library; native or package adapters must remain localized at their future integration boundary.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies 24-byte layout, zero/identity/reflection values, constructors, both indexers and failures, matrix order, decomposition including negative scale/skew, point/basis/array/rectangle transforms, negative-size rectangle normalization, general and orthonormal inverse behavior, singular failures, global/local operation differences, shortest-angle interpolation and extrapolation, conformal/finite/exact/approximate behavior including NaN/infinity, degenerate orthonormalization, `LookingAt`, scalar arithmetic, culture-invariant formatting, strict configuration persistence and malformed input, packed-scene storage, and zero warmed allocation for numeric math.

Execution is currently verified only on Linux/.NET 8. The type has not been exercised through a renderer, native SDL backend, or six-target test matrix.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate Transform foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
