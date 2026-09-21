# RectI

Last updated: 2026-09-21

## Declaration

- Source: [`RectI.cs`](../../src/Core/Math/RectI.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct RectI : IEquatable<RectI>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`RectI` is a mutable 16-byte axis-aligned integer rectangle composed of two sequential [`Vector2I`](Vector2I.md) values: `Position` and `Size`. It provides backend-independent pixel, atlas, image-region, grid, and other integer-bound geometry.

The value owns no resources, identity, handles, callbacks, or managed references and does not derive from `ElectronObject`. Zero initialization is an empty rectangle at the origin. Construction and mutation preserve negative, zero, and overflowing 32-bit components; callers normalize negative sizes explicitly with `Abs()` when an operation requires non-negative size.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `Position` | Mutable beginning corner |
| `Size` | Mutable width and height; most spatial operations require non-negative components |
| `End` | Gets `Position + Size`; assignment preserves `Position` and derives `Size` |
| `Area` | Signed `Size.X * Size.Y` |
| `RectI(Vector2I, Vector2I)` | Stores position and size unchanged |
| `RectI(Vector2I, int, int)` | Stores a position with explicit width and height |
| `RectI(int, int, Vector2I)` | Stores explicit position coordinates and a size |
| `RectI(int, int, int, int)` | Stores four explicit components |
| `Abs()` | Moves the origin componentwise when required and returns non-negative size |
| `Encloses(RectI)` | Tests inclusive containment of both other edges |
| `Expand(Vector2I)` | Extends the current edges as needed to include the point |
| `GetCenter()` | Returns `Position + Size / 2`; integer division truncates toward zero |
| `Grow(int)` | Extends all four sides equally; negative values shrink |
| `GrowIndividual(int, int, int, int)` | Extends left, top, right, and bottom independently |
| `GrowSide(Side, int)` | Extends one side; an undefined enum value leaves the rectangle unchanged |
| `HasArea()` | Requires both size components to be strictly positive |
| `HasPoint(Vector2I)` | Uses half-open containment: left/top included, right/bottom excluded |
| `Intersection(RectI)` | Returns overlap or `default` for separation or border-only contact; a contained zero-size rectangle retains its position |
| `Intersects(RectI)` | Tests overlap and excludes border-only contact |
| `Merge(RectI)` | Returns the smallest axis-aligned integer rectangle containing both inputs |
| Implicit `RectI` to `Rect` | Converts all four integer components to single precision; large values can lose low-order precision |
| Explicit `Rect` to `RectI` | Truncates finite in-range components toward zero; invalid components throw `ArgumentOutOfRangeException` |
| `==`, `!=`, `Equals` | Exact position and size equality |
| `GetHashCode()` | Hashes position and size |
| `ToString()`, `ToString(string?)` | Invariant-culture `Position, Size` formatting |

Normal value assignment is the copy operation; an explicit copy constructor would add no C# behavior.

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
