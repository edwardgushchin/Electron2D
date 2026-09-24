# Rect2i

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Rect2i.cs`](../../src/Core/Math/Rect2i.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Rect2i`

> Represents an integer two-dimensional axis-aligned rectangle.

## Description

Represents an integer two-dimensional axis-aligned rectangle.

`Rect2i` is a mutable 16-byte axis-aligned integer rectangle composed of two sequential [`Vector2i`](Vector2i.md) values: `Position` and `Size`. It provides backend-independent pixel, atlas, image-region, grid, and other integer-bound geometry.

The value owns no resources, identity, handles, callbacks, or managed references and does not derive from `ElectronObject`. Zero initialization is an empty rectangle at the origin. Construction and mutation preserve negative, zero, and overflowing 32-bit components; callers normalize negative sizes explicitly with `Abs()` when an operation requires non-negative size.

The rectangle is defined by a position and size and is commonly used for pixel, image-region,
atlas, and grid bounds. Most geometric operations assume non-negative size components. Call
[`Rect2i.Abs`](Rect2i.md#m-electron2d-rect2i-abs) before those operations when a rectangle may have a negative width or height.
Integer arithmetic uses unchecked 32-bit wraparound except where a documented managed operation throws.

All 23 mapped members and the type row have a pinned-source, ADR 0033/0035 and Linux/.NET 8 managed audit. `Abs` adds the negative part of each size component before taking its magnitude, preserving wrapped positions even if a positive component makes `End` wrap. A size of `int.MinValue` raises `OverflowException` under the accepted managed boundary. `VerifyIntegerRectangles` covers copy/mutation, wrapped area/end/growth, signed center rounding, half-open edges, empty intersection, typed conversion and failures, strict persistence, packed-scene copying and warmed allocation behavior. Native ABI and other platforms remain unverified.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var region = new Rect2i(new Vector2i(0, 0), new Vector2i(64, 64));
var clipped = region.Intersection(availableRegion);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Rect2i(Vector2i position, Vector2i size)`](#m-electron2d-rect2i-ctor-electron2d-vector2i-electron2d-vector2i) | Initializes an integer rectangle from a position and size. |
| [`public Rect2i(Vector2i position, int width, int height)`](#m-electron2d-rect2i-ctor-electron2d-vector2i-system-int32-system-int32) | Initializes an integer rectangle from a position, width, and height. |
| [`public Rect2i(int x, int y, Vector2i size)`](#m-electron2d-rect2i-ctor-system-int32-system-int32-electron2d-vector2i) | Initializes an integer rectangle from position coordinates and a size. |
| [`public Rect2i(int x, int y, int width, int height)`](#m-electron2d-rect2i-ctor-system-int32-system-int32-system-int32-system-int32) | Initializes an integer rectangle from position coordinates, width, and height. |

## Properties

| Member | Description |
| --- | --- |
| [`public Vector2i Position { get; set; }`](#p-electron2d-rect2i-position) | Gets or sets the beginning corner, usually the top-left integer point. |
| [`public Vector2i Size { get; set; }`](#p-electron2d-rect2i-size) | Gets or sets the integer width and height measured from [`Rect2i.Position`](Rect2i.md#p-electron2d-rect2i-position). |
| [`public Vector2i End { get; set; }`](#p-electron2d-rect2i-end) | Gets or sets the ending corner. |
| [`public int Area { get; }`](#p-electron2d-rect2i-area) | Gets the signed integer rectangle area. |

## Methods

| Member | Description |
| --- | --- |
| [`public Rect2i Abs()`](#m-electron2d-rect2i-abs) | Returns an equivalent integer rectangle with a non-negative size and top-left position. |
| [`public bool Encloses(Rect2i other)`](#m-electron2d-rect2i-encloses-electron2d-rect2i) | Tests whether this integer rectangle completely encloses another rectangle. |
| [`public Rect2i Expand(Vector2i point)`](#m-electron2d-rect2i-expand-electron2d-vector2i) | Expands the integer rectangle's edges when necessary to include a point. |
| [`public Vector2i GetCenter()`](#m-electron2d-rect2i-getcenter) | Gets the integer center point. |
| [`public Rect2i Grow(int amount)`](#m-electron2d-rect2i-grow-system-int32) | Returns a copy extended equally on every side. |
| [`public Rect2i GrowIndividual(int left, int top, int right, int bottom)`](#m-electron2d-rect2i-growindividual-system-int32-system-int32-system-int32-system-int32) | Returns a copy extended independently on each side. |
| [`public Rect2i GrowSide(Side side, int amount)`](#m-electron2d-rect2i-growside-electron2d-side-system-int32) | Returns a copy extended on one side. |
| [`public bool HasArea()`](#m-electron2d-rect2i-hasarea) | Tests whether both size components are strictly positive. |
| [`public bool HasPoint(Vector2i point)`](#m-electron2d-rect2i-haspoint-electron2d-vector2i) | Tests whether an integer point lies in the rectangle's half-open area. |
| [`public Rect2i Intersection(Rect2i other)`](#m-electron2d-rect2i-intersection-electron2d-rect2i) | Returns the intersection with another integer rectangle. |
| [`public bool Intersects(Rect2i other)`](#m-electron2d-rect2i-intersects-electron2d-rect2i) | Tests whether this integer rectangle overlaps another rectangle. |
| [`public Rect2i Merge(Rect2i other)`](#m-electron2d-rect2i-merge-electron2d-rect2i) | Returns the smallest axis-aligned integer rectangle enclosing this rectangle and another. |
| [`public override bool Equals(object obj)`](#m-electron2d-rect2i-equals-system-object) | Tests whether another object is an equal integer rectangle. |
| [`public bool Equals(Rect2i other)`](#m-electron2d-rect2i-equals-electron2d-rect2i) | Tests position and size for exact component equality. |
| [`public override int GetHashCode()`](#m-electron2d-rect2i-gethashcode) | Returns a hash code based on position and size. |
| [`public override string ToString()`](#m-electron2d-rect2i-tostring) | Formats position and size using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-rect2i-tostring-system-string) | Formats position and size with an integer numeric format and invariant culture. |

## Operators

| Member | Description |
| --- | --- |
| [`public static bool operator ==(Rect2i left, Rect2i right)`](#m-electron2d-rect2i-op-equality-electron2d-rect2i-electron2d-rect2i) | Tests both position and size for exact component equality. |
| [`public static bool operator !=(Rect2i left, Rect2i right)`](#m-electron2d-rect2i-op-inequality-electron2d-rect2i-electron2d-rect2i) | Tests whether either position or size differs. |
| [`public static Rect2 operator implicit(Rect2i value)`](#m-electron2d-rect2i-op-implicit-electron2d-rect2i-electron2d-rect2) | Converts an integer rectangle to a floating-point rectangle. |
| [`public static Rect2i operator explicit(Rect2 value)`](#m-electron2d-rect2i-op-explicit-electron2d-rect2-electron2d-rect2i) | Converts a floating-point rectangle by truncating each position and size component toward zero. |

## Constructor Descriptions

<a id="m-electron2d-rect2i-ctor-electron2d-vector2i-electron2d-vector2i"></a>
### `public Rect2i(Vector2i position, Vector2i size)`

Initializes an integer rectangle from a position and size.

**Parameters**

- `position`: The beginning corner.
- `size`: The integer width and height.

<a id="m-electron2d-rect2i-ctor-electron2d-vector2i-system-int32-system-int32"></a>
### `public Rect2i(Vector2i position, int width, int height)`

Initializes an integer rectangle from a position, width, and height.

**Parameters**

- `position`: The beginning corner.
- `width`: The integer width.
- `height`: The integer height.

<a id="m-electron2d-rect2i-ctor-system-int32-system-int32-electron2d-vector2i"></a>
### `public Rect2i(int x, int y, Vector2i size)`

Initializes an integer rectangle from position coordinates and a size.

**Parameters**

- `x`: The horizontal position.
- `y`: The vertical position.
- `size`: The integer width and height.

<a id="m-electron2d-rect2i-ctor-system-int32-system-int32-system-int32-system-int32"></a>
### `public Rect2i(int x, int y, int width, int height)`

Initializes an integer rectangle from position coordinates, width, and height.

**Parameters**

- `x`: The horizontal position.
- `y`: The vertical position.
- `width`: The integer width.
- `height`: The integer height.

## Property Descriptions

<a id="p-electron2d-rect2i-position"></a>
### `public Vector2i Position { get; set; }`

Gets or sets the beginning corner, usually the top-left integer point.

**Value:** The rectangle origin. It is normally componentwise less than or equal to [`Rect2i.End`](Rect2i.md#p-electron2d-rect2i-end).

<a id="p-electron2d-rect2i-size"></a>
### `public Vector2i Size { get; set; }`

Gets or sets the integer width and height measured from [`Rect2i.Position`](Rect2i.md#p-electron2d-rect2i-position).

**Value:** The rectangle size. Non-negative components are required by most geometric operations.

**Remarks:** Assignment changes [`Rect2i.End`](Rect2i.md#p-electron2d-rect2i-end) because the end is computed from position plus size.

<a id="p-electron2d-rect2i-end"></a>
### `public Vector2i End { get; set; }`

Gets or sets the ending corner.

**Value:** [`Rect2i.Position`](Rect2i.md#p-electron2d-rect2i-position) plus [`Rect2i.Size`](Rect2i.md#p-electron2d-rect2i-size) using unchecked integer arithmetic.

**Remarks:** Assignment changes [`Rect2i.Size`](Rect2i.md#p-electron2d-rect2i-size) while preserving [`Rect2i.Position`](Rect2i.md#p-electron2d-rect2i-position).

<a id="p-electron2d-rect2i-area"></a>
### `public int Area { get; }`

Gets the signed integer rectangle area.

**Value:** `Size.X * Size.Y` using unchecked 32-bit arithmetic.

**Remarks:** A positive product does not replace [`Rect2i.HasArea`](Rect2i.md#m-electron2d-rect2i-hasarea) because two negative components also have a positive product.

## Method Descriptions

<a id="m-electron2d-rect2i-abs"></a>
### `public Rect2i Abs()`

Returns an equivalent integer rectangle with a non-negative size and top-left position.

**Returns:** The normalized rectangle.

**Exceptions**

- `OverflowException`: A size component is `Int32.MinValue`.

<a id="m-electron2d-rect2i-encloses-electron2d-rect2i"></a>
### `public bool Encloses(Rect2i other)`

Tests whether this integer rectangle completely encloses another rectangle.

**Parameters**

- `other`: The candidate enclosed rectangle.

**Returns:** `true` when both edges of `other` lie within or on this rectangle.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`Rect2i.Abs`](Rect2i.md#m-electron2d-rect2i-abs) first.

<a id="m-electron2d-rect2i-expand-electron2d-vector2i"></a>
### `public Rect2i Expand(Vector2i point)`

Expands the integer rectangle's edges when necessary to include a point.

**Parameters**

- `point`: The integer point to include.

**Returns:** The expanded rectangle.

**Remarks:** A point exactly on an existing edge does not change the rectangle.

<a id="m-electron2d-rect2i-getcenter"></a>
### `public Vector2i GetCenter()`

Gets the integer center point.

**Returns:** `Position + Size / 2`.

**Remarks:** Odd size components round toward [`Rect2i.Position`](Rect2i.md#p-electron2d-rect2i-position).

<a id="m-electron2d-rect2i-grow-system-int32"></a>
### `public Rect2i Grow(int amount)`

Returns a copy extended equally on every side.

**Parameters**

- `amount`: The integer amount added outward on each side; a negative value shrinks.

**Returns:** The grown or shrunk rectangle.

<a id="m-electron2d-rect2i-growindividual-system-int32-system-int32-system-int32-system-int32"></a>
### `public Rect2i GrowIndividual(int left, int top, int right, int bottom)`

Returns a copy extended independently on each side.

**Parameters**

- `left`: The amount added outward on the left.
- `top`: The amount added outward on the top.
- `right`: The amount added outward on the right.
- `bottom`: The amount added outward on the bottom.

**Returns:** The grown or shrunk rectangle.

<a id="m-electron2d-rect2i-growside-electron2d-side-system-int32"></a>
### `public Rect2i GrowSide(Side side, int amount)`

Returns a copy extended on one side.

**Parameters**

- `side`: The side to extend.
- `amount`: The amount added outward; a negative value shrinks that side.

**Returns:** The grown or shrunk rectangle. An undefined `side` leaves the rectangle unchanged.

<a id="m-electron2d-rect2i-hasarea"></a>
### `public bool HasArea()`

Tests whether both size components are strictly positive.

**Returns:** `true` when width and height are greater than zero.

<a id="m-electron2d-rect2i-haspoint-electron2d-vector2i"></a>
### `public bool HasPoint(Vector2i point)`

Tests whether an integer point lies in the rectangle's half-open area.

**Parameters**

- `point`: The point to test.

**Returns:** `true` when the point is on or after the left/top edges and before the right/bottom edges.

**Remarks:** Negative size components are unsupported; normalize with [`Rect2i.Abs`](Rect2i.md#m-electron2d-rect2i-abs) first.

<a id="m-electron2d-rect2i-intersection-electron2d-rect2i"></a>
### `public Rect2i Intersection(Rect2i other)`

Returns the intersection with another integer rectangle.

**Parameters**

- `other`: The other rectangle.

**Returns:** The intersection, or `default` when the rectangles do not intersect.

**Remarks:** Touching outer borders alone return `default`. A zero-size rectangle strictly inside another
rectangle is considered intersecting and produces a zero-size result at its own position. Negative size
components are unsupported.

<a id="m-electron2d-rect2i-intersects-electron2d-rect2i"></a>
### `public bool Intersects(Rect2i other)`

Tests whether this integer rectangle overlaps another rectangle.

**Parameters**

- `other`: The other rectangle.

**Returns:** `true` when the interiors overlap; touching outer borders are excluded.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`Rect2i.Abs`](Rect2i.md#m-electron2d-rect2i-abs) first.

<a id="m-electron2d-rect2i-merge-electron2d-rect2i"></a>
### `public Rect2i Merge(Rect2i other)`

Returns the smallest axis-aligned integer rectangle enclosing this rectangle and another.

**Parameters**

- `other`: The other rectangle.

**Returns:** The merged rectangle.

**Remarks:** Negative size components are unsupported; normalize either rectangle with [`Rect2i.Abs`](Rect2i.md#m-electron2d-rect2i-abs) first.

<a id="m-electron2d-rect2i-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an equal integer rectangle.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is an integer rectangle with equal components.

<a id="m-electron2d-rect2i-equals-electron2d-rect2i"></a>
### `public bool Equals(Rect2i other)`

Tests position and size for exact component equality.

**Parameters**

- `other`: The other rectangle.

**Returns:** `true` when all four components are equal.

<a id="m-electron2d-rect2i-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on position and size.

**Returns:** The component hash code.

<a id="m-electron2d-rect2i-tostring"></a>
### `public override string ToString()`

Formats position and size using invariant culture.

**Returns:** A string containing the position followed by the size.

<a id="m-electron2d-rect2i-tostring-system-string"></a>
### `public string ToString(string format)`

Formats position and size with an integer numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format, or `null` for the default format.

**Returns:** A string containing the position followed by the size.

**Exceptions**

- `FormatException`: `format` is invalid.

## Operator Descriptions

<a id="m-electron2d-rect2i-op-equality-electron2d-rect2i-electron2d-rect2i"></a>
### `public static bool operator ==(Rect2i left, Rect2i right)`

Tests both position and size for exact component equality.

**Parameters**

- `left`: The first rectangle.
- `right`: The second rectangle.

**Returns:** `true` when all four components are equal.

<a id="m-electron2d-rect2i-op-inequality-electron2d-rect2i-electron2d-rect2i"></a>
### `public static bool operator !=(Rect2i left, Rect2i right)`

Tests whether either position or size differs.

**Parameters**

- `left`: The first rectangle.
- `right`: The second rectangle.

**Returns:** `true` when at least one component differs.

<a id="m-electron2d-rect2i-op-implicit-electron2d-rect2i-electron2d-rect2"></a>
### `public static Rect2 operator implicit(Rect2i value)`

Converts an integer rectangle to a floating-point rectangle.

**Parameters**

- `value`: The integer rectangle to convert.

**Returns:** A floating-point rectangle with corresponding position and size components.

**Remarks:** Large integer components can lose low-order precision.

<a id="m-electron2d-rect2i-op-explicit-electron2d-rect2-electron2d-rect2i"></a>
### `public static Rect2i operator explicit(Rect2 value)`

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
- Explicit conversion from [`Rect2`](Rect2.md) delegates to the checked floating-to-integer vector conversion and rejects non-finite or out-of-range components.
- `ToString(string?)` exposes `FormatException` for an invalid integer format.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition beyond normal value assignment. Copies are independent. Read-only calculations are safe across threads when each caller owns its value; concurrent writes to the same storage location remain an ordinary unsynchronized C# data race.

Construction, geometry, conversion, comparison, and hashing are value-only and allocate no managed memory after JIT warmup. String formatting allocates. Sequential layout is required and locally verified as 16 bytes, but native backend ABI equivalence is not promised; backend conversion must remain explicit.

## Dependencies and integration

The public type depends on [`Vector2i`](Vector2i.md), [`Rect2`](Rect2.md), [`Side`](Side.md), invariant formatting, and interop metadata. [`ConfigFile`](ConfigFile.md) stores every integer component through the exact nested `Position.X/Y` and `Size.X/Y` schema. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `Rect2i` directly as a reference-free value.

There is no dependency on Scene geometry, rendering, SDL, input, audio, physics, resources, scripting, or an editor. Support mapping, approximate comparison, finite checks, transform operators, and optional border-inclusive intersection belong only to floating-point rectangle behavior and are not part of this integer contract. Language-specific boolean truth conversion is permanently excluded from the typed C# surface.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies layout and defaults, all four constructors, mutable properties, signed area and overflow, normalization and its minimum-integer failure, enclosure, expansion, center rounding, every growth mode and undefined side, half-open containment, overlap/border/separation behavior, intersection and merge, equality/hashing, both conversions and conversion failures, invariant formatting and failure, strict configuration serialization and malformed fields, packed-scene storage, and zero warmed numeric allocation.

Execution is currently verified on Linux/.NET 8. Native backend interop and the full Windows/macOS/Linux (X11/Wayland)/Android/iOS/Web host matrix remain unverified. Image, atlas, renderer, grid, tile, and UI consumers do not exist yet; the value itself is complete and does not stub those domains.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
