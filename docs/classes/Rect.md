# Rect

Last updated: 2026-09-22

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Rect.cs`](../../src/Core/Math/Rect.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Rect`

> Represents a floating-point two-dimensional axis-aligned rectangle.

## Description

Represents a floating-point two-dimensional axis-aligned rectangle.

`Rect` is a mutable 16-byte axis-aligned floating-point rectangle composed of two sequential `Electron2D.Vector2` values: `Position` and `Size`. It provides backend-independent containment, overlap, intersection, enclosure, expansion, growth, merge, support mapping, finite-value checks, and exact or approximate comparison.

The value owns no resources, identity, handles, callbacks, or managed references and does not derive from `ElectronObject`. Zero initialization is the empty rectangle at the origin. Ordinary construction and mutation retain negative, zero, NaN, and infinite components; callers normalize negative sizes explicitly with `Abs()` when an operation requires non-negative size.

The rectangle is defined by a position and size and is commonly used for fast overlap tests.
Most geometric operations assume non-negative size components. Call [`Rect.Abs`](Rect.md#m-electron2d-rect-abs) before those
operations when a rectangle may have a negative width or height.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var bounds = new Rect(new Vector2(10f, 20f), new Vector2(80f, 40f));
if (bounds.HasPoint(pointerPosition))
    HandlePointer();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Rect(Vector2 position, Vector2 size)`](#m-electron2d-rect-ctor-electron2d-vector2-electron2d-vector2) | Initializes a rectangle from a position and size. |
| [`public Rect(Vector2 position, float width, float height)`](#m-electron2d-rect-ctor-electron2d-vector2-system-single-system-single) | Initializes a rectangle from a position, width, and height. |
| [`public Rect(float x, float y, Vector2 size)`](#m-electron2d-rect-ctor-system-single-system-single-electron2d-vector2) | Initializes a rectangle from position coordinates and a size. |
| [`public Rect(float x, float y, float width, float height)`](#m-electron2d-rect-ctor-system-single-system-single-system-single-system-single) | Initializes a rectangle from position coordinates, width, and height. |

## Properties

| Member | Description |
| --- | --- |
| [`public Vector2 Position { get; set; }`](#p-electron2d-rect-position) | Gets or sets the beginning corner, usually the top-left point. |
| [`public Vector2 Size { get; set; }`](#p-electron2d-rect-size) | Gets or sets the width and height measured from [`Rect.Position`](Rect.md#p-electron2d-rect-position). |
| [`public Vector2 End { get; set; }`](#p-electron2d-rect-end) | Gets or sets the ending corner. |
| [`public float Area { get; }`](#p-electron2d-rect-area) | Gets the signed rectangle area. |

## Methods

| Member | Description |
| --- | --- |
| [`public Rect Abs()`](#m-electron2d-rect-abs) | Returns an equivalent rectangle with a non-negative size and top-left position. |
| [`public bool Encloses(Rect other)`](#m-electron2d-rect-encloses-electron2d-rect) | Tests whether this rectangle completely encloses another rectangle. |
| [`public Rect Expand(Vector2 point)`](#m-electron2d-rect-expand-electron2d-vector2) | Expands the rectangle's edges when necessary to include a point. |
| [`public Vector2 GetCenter()`](#m-electron2d-rect-getcenter) | Gets the center point. |
| [`public Vector2 GetSupport(Vector2 direction)`](#m-electron2d-rect-getsupport-electron2d-vector2) | Gets the rectangle vertex farthest along a direction. |
| [`public Rect Grow(float amount)`](#m-electron2d-rect-grow-system-single) | Returns a copy extended equally on every side. |
| [`public Rect GrowIndividual(float left, float top, float right, float bottom)`](#m-electron2d-rect-growindividual-system-single-system-single-system-single-system-single) | Returns a copy extended independently on each side. |
| [`public Rect GrowSide(Side side, float amount)`](#m-electron2d-rect-growside-electron2d-side-system-single) | Returns a copy extended on one side. |
| [`public bool HasArea()`](#m-electron2d-rect-hasarea) | Tests whether both size components are strictly positive. |
| [`public bool HasPoint(Vector2 point)`](#m-electron2d-rect-haspoint-electron2d-vector2) | Tests whether a point lies in the rectangle's half-open area. |
| [`public Rect Intersection(Rect other)`](#m-electron2d-rect-intersection-electron2d-rect) | Returns the intersection with another rectangle. |
| [`public bool Intersects(Rect other, bool includeBorders = false)`](#m-electron2d-rect-intersects-electron2d-rect-system-boolean) | Tests whether this rectangle overlaps another rectangle. |
| [`public bool IsFinite()`](#m-electron2d-rect-isfinite) | Tests whether all position and size components are finite. |
| [`public bool IsEqualApprox(Rect other)`](#m-electron2d-rect-isequalapprox-electron2d-rect) | Tests position and size for scale-aware approximate equality. |
| [`public Rect Merge(Rect other)`](#m-electron2d-rect-merge-electron2d-rect) | Returns the smallest edge-aligned rectangle enclosing this rectangle and another. |
| [`public override bool Equals(object obj)`](#m-electron2d-rect-equals-system-object) | Tests whether another object is an exactly equal rectangle. |
| [`public bool Equals(Rect other)`](#m-electron2d-rect-equals-electron2d-rect) | Tests position and size for exact component equality. |
| [`public override int GetHashCode()`](#m-electron2d-rect-gethashcode) | Returns a hash code based on position and size. |
| [`public override string ToString()`](#m-electron2d-rect-tostring) | Formats position and size using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-rect-tostring-system-string) | Formats position and size with a numeric format and invariant culture. |

## Operators

| Member | Description |
| --- | --- |
| [`public static bool operator ==(Rect left, Rect right)`](#m-electron2d-rect-op-equality-electron2d-rect-electron2d-rect) | Tests both position and size for exact component equality. |
| [`public static bool operator !=(Rect left, Rect right)`](#m-electron2d-rect-op-inequality-electron2d-rect-electron2d-rect) | Tests whether either position or size differs under exact component equality. |

## Constructor Descriptions

<a id="m-electron2d-rect-ctor-electron2d-vector2-electron2d-vector2"></a>
### `public Rect(Vector2 position, Vector2 size)`

Initializes a rectangle from a position and size.

**Parameters**

- `position`: The beginning corner.
- `size`: The width and height.

<a id="m-electron2d-rect-ctor-electron2d-vector2-system-single-system-single"></a>
### `public Rect(Vector2 position, float width, float height)`

Initializes a rectangle from a position, width, and height.

**Parameters**

- `position`: The beginning corner.
- `width`: The width.
- `height`: The height.

<a id="m-electron2d-rect-ctor-system-single-system-single-electron2d-vector2"></a>
### `public Rect(float x, float y, Vector2 size)`

Initializes a rectangle from position coordinates and a size.

**Parameters**

- `x`: The horizontal position.
- `y`: The vertical position.
- `size`: The width and height.

<a id="m-electron2d-rect-ctor-system-single-system-single-system-single-system-single"></a>
### `public Rect(float x, float y, float width, float height)`

Initializes a rectangle from position coordinates, width, and height.

**Parameters**

- `x`: The horizontal position.
- `y`: The vertical position.
- `width`: The width.
- `height`: The height.

## Property Descriptions

<a id="p-electron2d-rect-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets the beginning corner, usually the top-left point.

**Value:** The rectangle origin. It is normally componentwise less than or equal to [`Rect.End`](Rect.md#p-electron2d-rect-end).

<a id="p-electron2d-rect-size"></a>
### `public Vector2 Size { get; set; }`

Gets or sets the width and height measured from [`Rect.Position`](Rect.md#p-electron2d-rect-position).

**Value:** The rectangle size. Non-negative components are required by most geometric operations.

**Remarks:** Assignment changes [`Rect.End`](Rect.md#p-electron2d-rect-end) because the end is computed from position plus size.

<a id="p-electron2d-rect-end"></a>
### `public Vector2 End { get; set; }`

Gets or sets the ending corner.

**Value:** [`Rect.Position`](Rect.md#p-electron2d-rect-position) plus [`Rect.Size`](Rect.md#p-electron2d-rect-size).

**Remarks:** Assignment changes [`Rect.Size`](Rect.md#p-electron2d-rect-size) while preserving [`Rect.Position`](Rect.md#p-electron2d-rect-position).

<a id="p-electron2d-rect-area"></a>
### `public float Area { get; }`

Gets the signed rectangle area.

**Value:** `Size.X * Size.Y`.

**Remarks:** A positive product does not replace [`Rect.HasArea`](Rect.md#m-electron2d-rect-hasarea) because two negative components also have a positive product.

## Method Descriptions

<a id="m-electron2d-rect-abs"></a>
### `public Rect Abs()`

Returns an equivalent rectangle with a non-negative size and top-left position.

**Returns:** The normalized rectangle.

<a id="m-electron2d-rect-encloses-electron2d-rect"></a>
### `public bool Encloses(Rect other)`

Tests whether this rectangle completely encloses another rectangle.

**Parameters**

- `other`: The candidate enclosed rectangle.

**Returns:** `true` when both edges of `other` lie within or on this rectangle.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`Rect.Abs`](Rect.md#m-electron2d-rect-abs) first.

<a id="m-electron2d-rect-expand-electron2d-vector2"></a>
### `public Rect Expand(Vector2 point)`

Expands the rectangle's edges when necessary to include a point.

**Parameters**

- `point`: The point to include.

**Returns:** The expanded rectangle.

**Remarks:** A point exactly on an existing edge does not change the rectangle.

<a id="m-electron2d-rect-getcenter"></a>
### `public Vector2 GetCenter()`

Gets the center point.

**Returns:** `Position + Size / 2`.

<a id="m-electron2d-rect-getsupport-electron2d-vector2"></a>
### `public Vector2 GetSupport(Vector2 direction)`

Gets the rectangle vertex farthest along a direction.

**Parameters**

- `direction`: The support direction.

**Returns:** The selected vertex. A zero direction component selects the position edge for that axis.

**Remarks:** This support mapping is suitable for collision-detection algorithms and assumes a non-negative size.

<a id="m-electron2d-rect-grow-system-single"></a>
### `public Rect Grow(float amount)`

Returns a copy extended equally on every side.

**Parameters**

- `amount`: The amount added outward on each side; negative values shrink the rectangle.

**Returns:** The grown or shrunk rectangle.

<a id="m-electron2d-rect-growindividual-system-single-system-single-system-single-system-single"></a>
### `public Rect GrowIndividual(float left, float top, float right, float bottom)`

Returns a copy extended independently on each side.

**Parameters**

- `left`: The amount added outward on the left.
- `top`: The amount added outward on the top.
- `right`: The amount added outward on the right.
- `bottom`: The amount added outward on the bottom.

**Returns:** The grown or shrunk rectangle.

<a id="m-electron2d-rect-growside-electron2d-side-system-single"></a>
### `public Rect GrowSide(Side side, float amount)`

Returns a copy extended on one side.

**Parameters**

- `side`: The side to extend.
- `amount`: The amount added outward; a negative value shrinks that side.

**Returns:** The grown or shrunk rectangle. An undefined `side` leaves the rectangle unchanged.

<a id="m-electron2d-rect-hasarea"></a>
### `public bool HasArea()`

Tests whether both size components are strictly positive.

**Returns:** `true` when width and height are greater than zero.

<a id="m-electron2d-rect-haspoint-electron2d-vector2"></a>
### `public bool HasPoint(Vector2 point)`

Tests whether a point lies in the rectangle's half-open area.

**Parameters**

- `point`: The point to test.

**Returns:** `true` when the point is on or after the left/top edges and strictly before the right/bottom edges.

**Remarks:** Negative size components are unsupported; normalize with [`Rect.Abs`](Rect.md#m-electron2d-rect-abs) first.

<a id="m-electron2d-rect-intersection-electron2d-rect"></a>
### `public Rect Intersection(Rect other)`

Returns the intersection with another rectangle.

**Parameters**

- `other`: The other rectangle.

**Returns:** The intersection, or `default` when the rectangles do not intersect.

**Remarks:** Touching outer borders alone return `default`. A zero-size rectangle strictly inside another
rectangle is considered intersecting and produces a zero-size result at its own position. Negative size
components are unsupported.

<a id="m-electron2d-rect-intersects-electron2d-rect-system-boolean"></a>
### `public bool Intersects(Rect other, bool includeBorders = false)`

Tests whether this rectangle overlaps another rectangle.

**Parameters**

- `other`: The other rectangle.
- `includeBorders`: Whether touching borders count as an intersection.

**Returns:** `true` when the rectangles overlap under the selected border rule.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`Rect.Abs`](Rect.md#m-electron2d-rect-abs) first.

<a id="m-electron2d-rect-isfinite"></a>
### `public bool IsFinite()`

Tests whether all position and size components are finite.

**Returns:** `true` when no component is NaN or infinity.

<a id="m-electron2d-rect-isequalapprox-electron2d-rect"></a>
### `public bool IsEqualApprox(Rect other)`

Tests position and size for scale-aware approximate equality.

**Parameters**

- `other`: The other rectangle.

**Returns:** `true` when every component is approximately equal.

<a id="m-electron2d-rect-merge-electron2d-rect"></a>
### `public Rect Merge(Rect other)`

Returns the smallest edge-aligned rectangle enclosing this rectangle and another.

**Parameters**

- `other`: The other rectangle.

**Returns:** The merged rectangle.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`Rect.Abs`](Rect.md#m-electron2d-rect-abs) first.

<a id="m-electron2d-rect-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an exactly equal rectangle.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is a rectangle with exactly equal components.

<a id="m-electron2d-rect-equals-electron2d-rect"></a>
### `public bool Equals(Rect other)`

Tests position and size for exact component equality.

**Parameters**

- `other`: The other rectangle.

**Returns:** `true` when all four components are exactly equal.

<a id="m-electron2d-rect-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on position and size.

**Returns:** The component hash code.

<a id="m-electron2d-rect-tostring"></a>
### `public override string ToString()`

Formats position and size using invariant culture.

**Returns:** A string containing the position followed by the size.

<a id="m-electron2d-rect-tostring-system-string"></a>
### `public string ToString(string format)`

Formats position and size with a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default format.

**Returns:** A string containing the position followed by the size.

**Exceptions**

- `FormatException`: `format` is invalid.

## Operator Descriptions

<a id="m-electron2d-rect-op-equality-electron2d-rect-electron2d-rect"></a>
### `public static bool operator ==(Rect left, Rect right)`

Tests both position and size for exact component equality.

**Parameters**

- `left`: The first rectangle.
- `right`: The second rectangle.

**Returns:** `true` when all four components are exactly equal.

<a id="m-electron2d-rect-op-inequality-electron2d-rect-electron2d-rect"></a>
### `public static bool operator !=(Rect left, Rect right)`

Tests whether either position or size differs under exact component equality.

**Parameters**

- `left`: The first rectangle.
- `right`: The second rectangle.

**Returns:** `true` when at least one component differs.

## Geometry invariants and error behavior

- `Position` and `Size` are preserved exactly. The type does not silently normalize, clamp, reject, or reorder components.
- `Abs()` is the only normalization operation. `Encloses`, `GetSupport`, `HasPoint`, `Intersection`, `Intersects`, and `Merge` document non-negative size as their input contract.
- Point containment is intentionally half-open. This permits adjacent positive rectangles to partition space without sharing the right or bottom edge.
- Border-only rectangle contact is excluded by default and can be included only through `Intersects(..., includeBorders: true)`. A zero-size rectangle strictly inside another still passes the edge-order intersection test and produces a zero-size intersection at its position.
- Growth is algebraic and may create zero or negative size. No exception is raised for over-shrinking.
- Geometry methods expose ordinary IEEE 754 propagation rather than throwing for NaN or infinity. `IsFinite()` is the explicit validation operation.
- `GrowSide` mirrors the four known [`Side`](Side.md) values and treats an undefined value as a no-op.
- Explicit conversion to [`RectI`](RectI.md) rejects a non-finite or out-of-range component with `ArgumentOutOfRangeException`.
- The only intentionally exposed exception is `FormatException` when `ToString(string?)` receives an invalid numeric format.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition beyond normal value assignment. Copies are independent. Read-only calculations are safe across threads when each caller owns its value; concurrent writes to the same storage location remain an ordinary unsynchronized C# data race.

Construction, geometry, comparison, and hashing are value-only and allocate no managed memory after JIT warmup. String formatting allocates. Sequential layout is required and locally verified as 16 bytes, but native backend ABI equivalence is not promised; backend conversion must remain explicit.

## Dependencies and integration

The public type depends on canonical scalar [`Mathf`](Mathf.md), [`Vector2`](Vector2.md), [`RectI`](RectI.md), [`Transform`](Transform.md), [`Side`](Side.md), globalization, and interop metadata. [`ConfigFile`](ConfigFile.md) stores only finite rectangles using the exact nested `Position.X/Y` and `Size.X/Y` schema. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `Rect` directly as a reference-free value.

There is no dependency on Scene, rendering, SDL, input, audio, physics, resources, scripting, or an editor. The complete integer sibling and typed conversions are implemented without depending on their future pixel, atlas, image-region, or grid consumers. Language-specific boolean truth conversion is permanently excluded from the typed C# surface.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies layout and defaults, all four constructors, mutable properties, signed area, normalization, enclosure, expansion, center/support mapping, every growth mode and undefined side, half-open containment, overlap/border/separation behavior, intersection and merge, exact/approximate/NaN/infinity behavior, hashing, both `RectI` conversions and invalid conversion, invariant formatting and failure, strict configuration serialization and malformed-input rollback, packed-scene storage, and zero warmed numeric allocation.

Execution is currently verified on Linux/.NET 8. Native backend interop and the full six-target matrix remain unverified.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate Transform type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
