# Geometry values component

Last updated: 2026-09-21

## Scope

This Core component provides backend-independent, allocation-free, floating-point axis-aligned rectangle geometry. It supplies spatial primitives needed by future rendering, UI, collision, and culling systems without creating any of those higher-level domains.

## Owned types

| Type | Role |
| --- | --- |
| [`Rect2`](../classes/Rect2.md) | Mutable sequential rectangle value and complete typed geometry operations |
| [`Side`](../classes/Side.md) | Stable identity for the four rectangle edges |

Production sources are [`src/Core/Math/Rect2.cs`](../../src/Core/Math/Rect2.cs) and [`src/Core/Math/Side.cs`](../../src/Core/Math/Side.cs).

## Runtime flow

1. Callers construct or copy `Rect2` values using `System.Numerics.Vector2` or four floats.
2. Ordinary mutation stores position and size without validation or hidden normalization.
3. Callers use `Abs()` explicitly before operations whose contract requires non-negative size.
4. Geometry operations return values or booleans and do not touch global state.
5. Typed persistence captures finite rectangles through a strict configuration schema or direct packed-scene value copying.

## Dependencies

- .NET vector, numeric, globalization, and interop-layout primitives.
- Existing typed `ConfigFile`, property descriptor, and packed-scene integration boundaries.
- No external package, native library, Scene, renderer, SDL, input, audio, physics, scripting, resource, or editor dependency.

## Invariants

- `Rect2` remains a sequential 16-byte value containing position then size.
- Most geometry operations require non-negative size; the type never normalizes implicitly.
- Point containment includes left/top and excludes right/bottom.
- Intersection excludes outer border-only contact by default; `Intersects` can opt into border contact, while a zero-size rectangle strictly inside another retains its position as an empty intersection.
- Ordinary math retains IEEE 754 values; finite persistence is a separate boundary.
- Numeric hot paths do not allocate after warmup.
- `Side` numeric values remain left `0`, top `1`, right `2`, bottom `3`.

## Current implementation status

Implemented and verified. The delivered surface covers all rectangle behavior independent of missing geometry types: storage, constructors, area/end views, normalization, containment, enclosure, expansion, center/support mapping, growth, intersection, merge, finite/exact/approximate comparison, hashing, and invariant formatting.

## Exclusions and deferred integration

- A constructor from an integer rectangle is deferred until `Rect2I` exists.
- Rectangle transform operators are deferred until the required standalone `Transform2D` type is implemented under ADR 0026.
- Boolean truth conversion is permanently excluded because it is not an appropriate typed C# contract.
- No renderer clip/scissor conversion, UI layout, viewport, image region, atlas, broad-phase, collision shape, or native structure conversion exists yet. Those integrations belong to their owning future domains and are not represented by placeholders.
- No 3D box, face, or axis API is in scope.

## Verification

The executable harness covers every current member family, negative/zero sizes, boundary inclusion, undefined enum input, malformed/non-finite persistence, packed-scene copying, IEEE equality edges, and warmed allocation behavior. Verification is Linux/.NET 8 only; native and five-platform behavior is not yet exercised.

## Decisions

- [0001: Typed C# without Variant](../decisions/0001-typed-csharp-without-variant.md)
- [0014: Managed lifetime and realtime allocation](../decisions/0014-managed-resource-lifetime.md)
- [0017: Source-tree module layout](../decisions/0017-source-tree-layout.md)
- [0025: Typed axis-aligned rectangle geometry](../decisions/0025-typed-rectangle-geometry.md)
- [0026: Separate Transform2D type](../decisions/0026-separate-transform2d-type.md)
