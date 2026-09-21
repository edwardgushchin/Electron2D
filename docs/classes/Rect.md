# Rect

Last updated: 2026-09-21

## Declaration

- Source: [`Rect.cs`](../../src/Core/Math/Rect.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Rect : IEquatable<Rect>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Rect` is a mutable 16-byte axis-aligned floating-point rectangle composed of two sequential `Electron2D.Vector2` values: `Position` and `Size`. It provides backend-independent containment, overlap, intersection, enclosure, expansion, growth, merge, support mapping, finite-value checks, and exact or approximate comparison.

The value owns no resources, identity, handles, callbacks, or managed references and does not derive from `ElectronObject`. Zero initialization is the empty rectangle at the origin. Ordinary construction and mutation retain negative, zero, NaN, and infinite components; callers normalize negative sizes explicitly with `Abs()` when an operation requires non-negative size.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `Position` | Mutable beginning corner |
| `Size` | Mutable width and height; most spatial operations require non-negative components |
| `End` | Gets `Position + Size`; assignment preserves `Position` and derives `Size` |
| `Area` | Signed `Size.X * Size.Y`; it can be positive when both components are negative |
| `Rect(Vector2, Vector2)` | Stores position and size unchanged |
| `Rect(Vector2, float, float)` | Stores a position with explicit width and height |
| `Rect(float, float, Vector2)` | Stores explicit position coordinates and a size |
| `Rect(float, float, float, float)` | Stores four explicit components |
| `Abs()` | Moves the origin componentwise when required and returns non-negative size |
| `Encloses(Rect)` | Tests inclusive containment of both other edges |
| `Expand(Vector2)` | Returns the smallest extension of the current edges that includes the point |
| `GetCenter()` | Returns `Position + Size / 2` |
| `GetSupport(Vector2)` | Selects the farthest corner; a zero direction component selects the position edge |
| `Grow(float)` | Extends all four sides equally; negative values shrink |
| `GrowIndividual(float, float, float, float)` | Extends left, top, right, and bottom independently |
| `GrowSide(Side, float)` | Extends one side; an undefined enum value leaves the rectangle unchanged |
| `HasArea()` | Requires both size components to be strictly positive |
| `HasPoint(Vector2)` | Uses half-open containment: left/top included, right/bottom excluded |
| `Intersection(Rect)` | Returns overlap or `default` for separation or border-only contact; a contained zero-size rectangle retains its position |
| `Intersects(Rect, bool = false)` | Tests overlap, optionally counting border-only contact |
| `IsEqualApprox(Rect)` | Per-component scale-aware [`Mathf.Epsilon`](Mathf.md) (`1e-6f`), with exact equality first |
| `IsFinite()` | Requires all four components to be neither NaN nor infinity |
| `Merge(Rect)` | Returns the smallest axis-aligned rectangle containing both inputs |
| `Transform * Rect` | Transforms all four corners and returns their axis-aligned bounds |
| `Rect * Transform` | Applies the inverse orthonormal transform to all four corners and returns their axis-aligned bounds |
| `==`, `!=`, `Equals` | Exact position and size equality; NaN is unequal |
| `GetHashCode()` | Hashes position and size |
| `ToString()`, `ToString(string?)` | Invariant-culture `Position, Size` formatting |

Normal value assignment is the copy operation; an explicit copy constructor would add no C# behavior.

## Geometry invariants and error behavior

- `Position` and `Size` are preserved exactly. The type does not silently normalize, clamp, reject, or reorder components.
- `Abs()` is the only normalization operation. `Encloses`, `GetSupport`, `HasPoint`, `Intersection`, `Intersects`, and `Merge` document non-negative size as their input contract.
- Point containment is intentionally half-open. This permits adjacent positive rectangles to partition space without sharing the right or bottom edge.
- Border-only rectangle contact is excluded by default and can be included only through `Intersects(..., includeBorders: true)`. A zero-size rectangle strictly inside another still passes the edge-order intersection test and produces a zero-size intersection at its position.
- Growth is algebraic and may create zero or negative size. No exception is raised for over-shrinking.
- Geometry methods expose ordinary IEEE 754 propagation rather than throwing for NaN or infinity. `IsFinite()` is the explicit validation operation.
- `GrowSide` mirrors the four known [`Side`](Side.md) values and treats an undefined value as a no-op.
- The only intentionally exposed exception is `FormatException` when `ToString(string?)` receives an invalid numeric format.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition beyond normal value assignment. Copies are independent. Read-only calculations are safe across threads when each caller owns its value; concurrent writes to the same storage location remain an ordinary unsynchronized C# data race.

Construction, geometry, comparison, and hashing are value-only and allocate no managed memory after JIT warmup. String formatting allocates. Sequential layout is required and locally verified as 16 bytes, but native backend ABI equivalence is not promised; backend conversion must remain explicit.

## Dependencies and integration

The public type depends on canonical scalar [`Mathf`](Mathf.md), [`Vector2`](Vector2.md), [`Transform`](Transform.md), [`Side`](Side.md), globalization, and interop metadata. [`ConfigFile`](ConfigFile.md) stores only finite rectangles using the exact nested `Position.X/Y` and `Size.X/Y` schema. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `Rect` directly as a reference-free value.

There is no dependency on Scene, rendering, SDL, input, audio, physics, resources, scripting, or an editor. Integer-rectangle conversion remains absent until an integer rectangle type is justified and implemented. Language-specific boolean truth conversion is permanently excluded from the typed C# surface.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies layout and defaults, all four constructors, mutable properties, signed area, normalization, enclosure, expansion, center/support mapping, every growth mode and undefined side, half-open containment, overlap/border/separation behavior, intersection and merge, exact/approximate/NaN/infinity behavior, hashing, invariant formatting and failure, strict configuration serialization and malformed-input rollback, packed-scene storage, and zero warmed numeric allocation.

Execution is currently verified on Linux/.NET 8. Native backend interop and the full five-platform matrix remain unverified. Integer-rectangle conversion remains absent rather than represented by a stub.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate Transform type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
