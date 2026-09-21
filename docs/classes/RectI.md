# RectI

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/RectI.cs`](../../src/Core/Math/RectI.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct RectI`

> Represents an integer two-dimensional axis-aligned rectangle.

## Description

Represents an integer two-dimensional axis-aligned rectangle.

`RectI` is a mutable 16-byte axis-aligned integer rectangle composed of two sequential [`Vector2I`](Vector2I.md) values: `Position` and `Size`. It provides backend-independent pixel, atlas, image-region, grid, and other integer-bound geometry.

The value owns no resources, identity, handles, callbacks, or managed references and does not derive from `ElectronObject`. Zero initialization is an empty rectangle at the origin. Construction and mutation preserve negative, zero, and overflowing 32-bit components; callers normalize negative sizes explicitly with `Abs()` when an operation requires non-negative size.

The rectangle is defined by a position and size and is commonly used for pixel, image-region,
atlas, and grid bounds. Most geometric operations assume non-negative size components. Call
[`RectI.Abs`](RectI.md#m-electron2d-recti-abs) before those operations when a rectangle may have a negative width or height.
Integer arithmetic uses unchecked 32-bit wraparound except where a documented managed operation throws.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var region = new RectI(new Vector2I(0, 0), new Vector2I(64, 64));
var clipped = region.Intersection(availableRegion);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public RectI(Vector2I position, Vector2I size)`](#m-electron2d-recti-ctor-electron2d-vector2i-electron2d-vector2i) | Initializes an integer rectangle from a position and size. |
| [`public RectI(Vector2I position, int width, int height)`](#m-electron2d-recti-ctor-electron2d-vector2i-system-int32-system-int32) | Initializes an integer rectangle from a position, width, and height. |
| [`public RectI(int x, int y, Vector2I size)`](#m-electron2d-recti-ctor-system-int32-system-int32-electron2d-vector2i) | Initializes an integer rectangle from position coordinates and a size. |
| [`public RectI(int x, int y, int width, int height)`](#m-electron2d-recti-ctor-system-int32-system-int32-system-int32-system-int32) | Initializes an integer rectangle from position coordinates, width, and height. |

## Properties

| Member | Description |
| --- | --- |
| [`public Vector2I Position { get; set; }`](#p-electron2d-recti-position) | Gets or sets the beginning corner, usually the top-left integer point. |
| [`public Vector2I Size { get; set; }`](#p-electron2d-recti-size) | Gets or sets the integer width and height measured from [`RectI.Position`](RectI.md#p-electron2d-recti-position). |
| [`public Vector2I End { get; set; }`](#p-electron2d-recti-end) | Gets or sets the ending corner. |
| [`public int Area { get; }`](#p-electron2d-recti-area) | Gets the signed integer rectangle area. |

## Methods

| Member | Description |
| --- | --- |
| [`public RectI Abs()`](#m-electron2d-recti-abs) | Returns an equivalent integer rectangle with a non-negative size and top-left position. |
| [`public bool Encloses(RectI other)`](#m-electron2d-recti-encloses-electron2d-recti) | Tests whether this integer rectangle completely encloses another rectangle. |
| [`public RectI Expand(Vector2I point)`](#m-electron2d-recti-expand-electron2d-vector2i) | Expands the integer rectangle's edges when necessary to include a point. |
| [`public Vector2I GetCenter()`](#m-electron2d-recti-getcenter) | Gets the integer center point. |
| [`public RectI Grow(int amount)`](#m-electron2d-recti-grow-system-int32) | Returns a copy extended equally on every side. |
| [`public RectI GrowIndividual(int left, int top, int right, int bottom)`](#m-electron2d-recti-growindividual-system-int32-system-int32-system-int32-system-int32) | Returns a copy extended independently on each side. |
| [`public RectI GrowSide(Side side, int amount)`](#m-electron2d-recti-growside-electron2d-side-system-int32) | Returns a copy extended on one side. |
| [`public bool HasArea()`](#m-electron2d-recti-hasarea) | Tests whether both size components are strictly positive. |
| [`public bool HasPoint(Vector2I point)`](#m-electron2d-recti-haspoint-electron2d-vector2i) | Tests whether an integer point lies in the rectangle's half-open area. |
| [`public RectI Intersection(RectI other)`](#m-electron2d-recti-intersection-electron2d-recti) | Returns the intersection with another integer rectangle. |
| [`public bool Intersects(RectI other)`](#m-electron2d-recti-intersects-electron2d-recti) | Tests whether this integer rectangle overlaps another rectangle. |
| [`public RectI Merge(RectI other)`](#m-electron2d-recti-merge-electron2d-recti) | Returns the smallest axis-aligned integer rectangle enclosing this rectangle and another. |
| [`public override bool Equals(object obj)`](#m-electron2d-recti-equals-system-object) | Tests whether another object is an equal integer rectangle. |
| [`public bool Equals(RectI other)`](#m-electron2d-recti-equals-electron2d-recti) | Tests position and size for exact component equality. |
| [`public override int GetHashCode()`](#m-electron2d-recti-gethashcode) | Returns a hash code based on position and size. |
| [`public override string ToString()`](#m-electron2d-recti-tostring) | Formats position and size using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-recti-tostring-system-string) | Formats position and size with an integer numeric format and invariant culture. |

## Operators

| Member | Description |
| --- | --- |
| [`public static bool operator ==(RectI left, RectI right)`](#m-electron2d-recti-op-equality-electron2d-recti-electron2d-recti) | Tests both position and size for exact component equality. |
| [`public static bool operator !=(RectI left, RectI right)`](#m-electron2d-recti-op-inequality-electron2d-recti-electron2d-recti) | Tests whether either position or size differs. |
| [`public static Rect operator implicit(RectI value)`](#m-electron2d-recti-op-implicit-electron2d-recti-electron2d-rect) | Converts an integer rectangle to a floating-point rectangle. |
| [`public static RectI operator explicit(Rect value)`](#m-electron2d-recti-op-explicit-electron2d-rect-electron2d-recti) | Converts a floating-point rectangle by truncating each position and size component toward zero. |

## Constructor Descriptions

<a id="m-electron2d-recti-ctor-electron2d-vector2i-electron2d-vector2i"></a>
### `public RectI(Vector2I position, Vector2I size)`

Initializes an integer rectangle from a position and size.

**Parameters**

- `position`: The beginning corner.
- `size`: The integer width and height.

<a id="m-electron2d-recti-ctor-electron2d-vector2i-system-int32-system-int32"></a>
### `public RectI(Vector2I position, int width, int height)`

Initializes an integer rectangle from a position, width, and height.

**Parameters**

- `position`: The beginning corner.
- `width`: The integer width.
- `height`: The integer height.

<a id="m-electron2d-recti-ctor-system-int32-system-int32-electron2d-vector2i"></a>
### `public RectI(int x, int y, Vector2I size)`

Initializes an integer rectangle from position coordinates and a size.

**Parameters**

- `x`: The horizontal position.
- `y`: The vertical position.
- `size`: The integer width and height.

<a id="m-electron2d-recti-ctor-system-int32-system-int32-system-int32-system-int32"></a>
### `public RectI(int x, int y, int width, int height)`

Initializes an integer rectangle from position coordinates, width, and height.

**Parameters**

- `x`: The horizontal position.
- `y`: The vertical position.
- `width`: The integer width.
- `height`: The integer height.

## Property Descriptions

<a id="p-electron2d-recti-position"></a>
### `public Vector2I Position { get; set; }`

Gets or sets the beginning corner, usually the top-left integer point.

**Value:** The rectangle origin. It is normally componentwise less than or equal to [`RectI.End`](RectI.md#p-electron2d-recti-end).

<a id="p-electron2d-recti-size"></a>
### `public Vector2I Size { get; set; }`

Gets or sets the integer width and height measured from [`RectI.Position`](RectI.md#p-electron2d-recti-position).

**Value:** The rectangle size. Non-negative components are required by most geometric operations.

**Remarks:** Assignment changes [`RectI.End`](RectI.md#p-electron2d-recti-end) because the end is computed from position plus size.

<a id="p-electron2d-recti-end"></a>
### `public Vector2I End { get; set; }`

Gets or sets the ending corner.

**Value:** [`RectI.Position`](RectI.md#p-electron2d-recti-position) plus [`RectI.Size`](RectI.md#p-electron2d-recti-size) using unchecked integer arithmetic.

**Remarks:** Assignment changes [`RectI.Size`](RectI.md#p-electron2d-recti-size) while preserving [`RectI.Position`](RectI.md#p-electron2d-recti-position).

<a id="p-electron2d-recti-area"></a>
### `public int Area { get; }`

Gets the signed integer rectangle area.

**Value:** `Size.X * Size.Y` using unchecked 32-bit arithmetic.

**Remarks:** A positive product does not replace [`RectI.HasArea`](RectI.md#m-electron2d-recti-hasarea) because two negative components also have a positive product.

## Method Descriptions

<a id="m-electron2d-recti-abs"></a>
### `public RectI Abs()`

Returns an equivalent integer rectangle with a non-negative size and top-left position.

**Returns:** The normalized rectangle.

**Exceptions**

- `OverflowException`: A size component is `Int32.MinValue`.

<a id="m-electron2d-recti-encloses-electron2d-recti"></a>
### `public bool Encloses(RectI other)`

Tests whether this integer rectangle completely encloses another rectangle.

**Parameters**

- `other`: The candidate enclosed rectangle.

**Returns:** `true` when both edges of `other` lie within or on this rectangle.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`RectI.Abs`](RectI.md#m-electron2d-recti-abs) first.

<a id="m-electron2d-recti-expand-electron2d-vector2i"></a>
### `public RectI Expand(Vector2I point)`

Expands the integer rectangle's edges when necessary to include a point.

**Parameters**

- `point`: The integer point to include.

**Returns:** The expanded rectangle.

**Remarks:** A point exactly on an existing edge does not change the rectangle.

<a id="m-electron2d-recti-getcenter"></a>
### `public Vector2I GetCenter()`

Gets the integer center point.

**Returns:** `Position + Size / 2`.

**Remarks:** Odd size components round toward [`RectI.Position`](RectI.md#p-electron2d-recti-position).

<a id="m-electron2d-recti-grow-system-int32"></a>
### `public RectI Grow(int amount)`

Returns a copy extended equally on every side.

**Parameters**

- `amount`: The integer amount added outward on each side; a negative value shrinks.

**Returns:** The grown or shrunk rectangle.

<a id="m-electron2d-recti-growindividual-system-int32-system-int32-system-int32-system-int32"></a>
### `public RectI GrowIndividual(int left, int top, int right, int bottom)`

Returns a copy extended independently on each side.

**Parameters**

- `left`: The amount added outward on the left.
- `top`: The amount added outward on the top.
- `right`: The amount added outward on the right.
- `bottom`: The amount added outward on the bottom.

**Returns:** The grown or shrunk rectangle.

<a id="m-electron2d-recti-growside-electron2d-side-system-int32"></a>
### `public RectI GrowSide(Side side, int amount)`

Returns a copy extended on one side.

**Parameters**

- `side`: The side to extend.
- `amount`: The amount added outward; a negative value shrinks that side.

**Returns:** The grown or shrunk rectangle. An undefined `side` leaves the rectangle unchanged.

<a id="m-electron2d-recti-hasarea"></a>
### `public bool HasArea()`

Tests whether both size components are strictly positive.

**Returns:** `true` when width and height are greater than zero.

<a id="m-electron2d-recti-haspoint-electron2d-vector2i"></a>
### `public bool HasPoint(Vector2I point)`

Tests whether an integer point lies in the rectangle's half-open area.

**Parameters**

- `point`: The point to test.

**Returns:** `true` when the point is on or after the left/top edges and before the right/bottom edges.

**Remarks:** Negative size components are unsupported; normalize with [`RectI.Abs`](RectI.md#m-electron2d-recti-abs) first.

<a id="m-electron2d-recti-intersection-electron2d-recti"></a>
### `public RectI Intersection(RectI other)`

Returns the intersection with another integer rectangle.

**Parameters**

- `other`: The other rectangle.

**Returns:** The intersection, or `default` when the rectangles do not intersect.

**Remarks:** Touching outer borders alone return `default`. A zero-size rectangle strictly inside another
rectangle is considered intersecting and produces a zero-size result at its own position. Negative size
components are unsupported.

<a id="m-electron2d-recti-intersects-electron2d-recti"></a>
### `public bool Intersects(RectI other)`

Tests whether this integer rectangle overlaps another rectangle.

**Parameters**

- `other`: The other rectangle.

**Returns:** `true` when the interiors overlap; touching outer borders are excluded.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`RectI.Abs`](RectI.md#m-electron2d-recti-abs) first.

<a id="m-electron2d-recti-merge-electron2d-recti"></a>
### `public RectI Merge(RectI other)`

Returns the smallest axis-aligned integer rectangle enclosing this rectangle and another.

**Parameters**

- `other`: The other rectangle.

**Returns:** The merged rectangle.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`RectI.Abs`](RectI.md#m-electron2d-recti-abs) first.

<a id="m-electron2d-recti-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an equal integer rectangle.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is an integer rectangle with equal components.

<a id="m-electron2d-recti-equals-electron2d-recti"></a>
### `public bool Equals(RectI other)`

Tests position and size for exact component equality.

**Parameters**

- `other`: The other rectangle.

**Returns:** `true` when all four components are equal.

<a id="m-electron2d-recti-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on position and size.

**Returns:** The component hash code.

<a id="m-electron2d-recti-tostring"></a>
### `public override string ToString()`

Formats position and size using invariant culture.

**Returns:** A string containing the position followed by the size.

<a id="m-electron2d-recti-tostring-system-string"></a>
### `public string ToString(string format)`

Formats position and size with an integer numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default format.

**Returns:** A string containing the position followed by the size.

**Exceptions**

- `FormatException`: `format` is invalid.

## Operator Descriptions

<a id="m-electron2d-recti-op-equality-electron2d-recti-electron2d-recti"></a>
### `public static bool operator ==(RectI left, RectI right)`

Tests both position and size for exact component equality.

**Parameters**

- `left`: The first rectangle.
- `right`: The second rectangle.

**Returns:** `true` when all four components are equal.

<a id="m-electron2d-recti-op-inequality-electron2d-recti-electron2d-recti"></a>
### `public static bool operator !=(RectI left, RectI right)`

Tests whether either position or size differs.

**Parameters**

- `left`: The first rectangle.
- `right`: The second rectangle.

**Returns:** `true` when at least one component differs.

<a id="m-electron2d-recti-op-implicit-electron2d-recti-electron2d-rect"></a>
### `public static Rect operator implicit(RectI value)`

Converts an integer rectangle to a floating-point rectangle.

**Parameters**

- `value`: The integer rectangle to convert.

**Returns:** A floating-point rectangle with corresponding position and size components.

**Remarks:** Large integer components can lose low-order precision.

<a id="m-electron2d-recti-op-explicit-electron2d-rect-electron2d-recti"></a>
### `public static RectI operator explicit(Rect value)`

Converts a floating-point rectangle by truncating each position and size component toward zero.

**Parameters**

- `value`: The floating-point rectangle to convert.

**Returns:** The truncated integer rectangle.

**Exceptions**

- `ArgumentOutOfRangeException`: A component is not finite or is outside the 32-bit signed integer range.

## Geometry invariants and error behavior

- `Position` and `Size` are preserved exactly. The type does not silently normalize, clamp, reject, or reorder components.
- `Abs()` is the only normalization operation. It throws `OverflowException` when either size component is `int.MinValue`.
- Ordinary addition, subtraction, multiplication, edge calculation, growth, and merge use unchecked 32-bit wraparound. Overflow is deterministic but can invalidate the usual ordered-edge preconditions.
- Point containment is half-open, so adjacent positive rectangles do not share their right or bottom edge.
- Border-only rectangle contact is excluded. A zero-size rectangle strictly inside another still passes the edge-order intersection test and produces a zero-size intersection at its position.
- Growth is algebraic and may create zero or negative size. `GrowSide` treats an undefined [`Side`](Side.md) value as a no-op.
- Explicit conversion from [`Rect`](Rect.md) delegates to the checked floating-to-integer vector conversion and rejects non-finite or out-of-range components.
- `ToString(string?)` exposes `FormatException` for an invalid integer format.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition beyond normal value assignment. Copies are independent. Read-only calculations are safe across threads when each caller owns its value; concurrent writes to the same storage location remain an ordinary unsynchronized C# data race.

Construction, geometry, conversion, comparison, and hashing are value-only and allocate no managed memory after JIT warmup. String formatting allocates. Sequential layout is required and locally verified as 16 bytes, but native backend ABI equivalence is not promised; backend conversion must remain explicit.

## Dependencies and integration

The public type depends on [`Vector2I`](Vector2I.md), [`Rect`](Rect.md), [`Side`](Side.md), invariant formatting, and interop metadata. [`ConfigFile`](ConfigFile.md) stores every integer component through the exact nested `Position.X/Y` and `Size.X/Y` schema. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `RectI` directly as a reference-free value.

There is no dependency on Scene geometry, rendering, SDL, input, audio, physics, resources, scripting, or an editor. Support mapping, approximate comparison, finite checks, transform operators, and optional border-inclusive intersection belong only to floating-point rectangle behavior and are not part of this integer contract. Language-specific boolean truth conversion is permanently excluded from the typed C# surface.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies layout and defaults, all four constructors, mutable properties, signed area and overflow, normalization and its minimum-integer failure, enclosure, expansion, center rounding, every growth mode and undefined side, half-open containment, overlap/border/separation behavior, intersection and merge, equality/hashing, both conversions and conversion failures, invariant formatting and failure, strict configuration serialization and malformed fields, packed-scene storage, and zero warmed numeric allocation.

Execution is currently verified on Linux/.NET 8. Native backend interop and the full Linux/Windows/macOS/Android/iOS host matrix remain unverified. Image, atlas, renderer, grid, tile, and UI consumers do not exist yet; the value itself is complete and does not stub those domains.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
