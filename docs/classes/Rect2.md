# Rect2

Last updated: 2026-09-21

## Declaration

- Source: [`Rect2.cs`](../../src/Core/Math/Rect2.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Rect2 : IEquatable<Rect2>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Rect2` is a mutable 16-byte axis-aligned floating-point rectangle composed of two sequential `System.Numerics.Vector2` values: `Position` and `Size`. It provides backend-independent containment, overlap, intersection, enclosure, expansion, growth, merge, support mapping, finite-value checks, and exact or approximate comparison.

The value owns no resources, identity, handles, callbacks, or managed references and does not derive from `ElectronObject`. Zero initialization is the empty rectangle at the origin. Ordinary construction and mutation retain negative, zero, NaN, and infinite components; callers normalize negative sizes explicitly with `Abs()` when an operation requires non-negative size.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `Position` | Mutable beginning corner |
| `Size` | Mutable width and height; most spatial operations require non-negative components |
| `End` | Gets `Position + Size`; assignment preserves `Position` and derives `Size` |
| `Area` | Signed `Size.X * Size.Y`; it can be positive when both components are negative |
| `Rect2(Vector2, Vector2)` | Stores position and size unchanged |
| `Rect2(Vector2, float, float)` | Stores a position with explicit width and height |
| `Rect2(float, float, Vector2)` | Stores explicit position coordinates and a size |
| `Rect2(float, float, float, float)` | Stores four explicit components |
| `Abs()` | Moves the origin componentwise when required and returns non-negative size |
| `Encloses(Rect2)` | Tests inclusive containment of both other edges |
| `Expand(Vector2)` | Returns the smallest extension of the current edges that includes the point |
| `GetCenter()` | Returns `Position + Size / 2` |
| `GetSupport(Vector2)` | Selects the farthest corner; a zero direction component selects the position edge |
| `Grow(float)` | Extends all four sides equally; negative values shrink |
| `GrowIndividual(float, float, float, float)` | Extends left, top, right, and bottom independently |
| `GrowSide(Side, float)` | Extends one side; an undefined enum value leaves the rectangle unchanged |
| `HasArea()` | Requires both size components to be strictly positive |
| `HasPoint(Vector2)` | Uses half-open containment: left/top included, right/bottom excluded |
| `Intersection(Rect2)` | Returns overlap or `default` for separation or border-only contact; a contained zero-size rectangle retains its position |
| `Intersects(Rect2, bool = false)` | Tests overlap, optionally counting border-only contact |
| `IsEqualApprox(Rect2)` | Per-component relative epsilon `0.00001`, with exact equality first |
| `IsFinite()` | Requires all four components to be neither NaN nor infinity |
| `Merge(Rect2)` | Returns the smallest axis-aligned rectangle containing both inputs |
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

The public type depends on `System.Numerics.Vector2`, [`Side`](Side.md), globalization, and interop metadata. [`ConfigFile`](ConfigFile.md) stores only finite rectangles using the exact nested `Position.X/Y` and `Size.X/Y` schema. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `Rect2` directly as a reference-free value.

There is no dependency on Scene, rendering, SDL, input, audio, physics, resources, scripting, or an editor. A constructor from `Rect2I` is dependency-blocked until that complete type exists. Multiplication by the implemented [`Transform2D`](Transform2D.md) remains deliberately deferred to the explicit Node/Rect2 migration slice under ADR 0026 and ADR 0029 rather than being introduced as an isolated compatibility operator. Language-specific boolean truth conversion is permanently excluded from the typed C# surface.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies layout and defaults, all four constructors, mutable properties, signed area, normalization, enclosure, expansion, center/support mapping, every growth mode and undefined side, half-open containment, overlap/border/separation behavior, intersection and merge, exact/approximate/NaN/infinity behavior, hashing, invariant formatting and failure, strict configuration serialization and malformed-input rollback, packed-scene storage, and zero warmed numeric allocation.

Execution is currently verified on Linux/.NET 8. Native backend interop and the full five-platform matrix remain unverified. Integer-rectangle conversion and transform multiplication remain explicitly deferred to their missing value types rather than partially implemented here.

## Decisions

- [0001: Typed C# without Variant](../decisions/0001-typed-csharp-without-variant.md)
- [0014: Managed lifetime and realtime allocation](../decisions/0014-managed-resource-lifetime.md)
- [0017: Source-tree module layout](../decisions/0017-source-tree-layout.md)
- [0025: Typed axis-aligned rectangle geometry](../decisions/0025-typed-rectangle-geometry.md)
- [0026: Separate Transform2D type](../decisions/0026-separate-transform2d-type.md)
- [0029: Typed Transform2D value and affine semantics](../decisions/0029-typed-transform2d-value.md)
