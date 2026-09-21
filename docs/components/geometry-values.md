# Geometry values component

Last updated: 2026-09-21

## Scope

This Core component currently provides backend-independent, allocation-free floating-point rectangle and affine-transform geometry. ADR 0032 requires an engine-owned `Vector` and the final `Vector`/`Rect`/`Transform` vocabulary across every domain; that migration is accepted but not yet implemented.

## Owned types

| Type | Role |
| --- | --- |
| [`Rect2`](../classes/Rect2.md) | Mutable sequential rectangle value and complete typed geometry operations |
| [`Transform2D`](../classes/Transform2D.md) | Mutable sequential affine value with basis/origin math and typed composition |
| [`Side`](../classes/Side.md) | Stable identity for the four rectangle edges |

Production sources are [`src/Core/Math/Rect2.cs`](../../src/Core/Math/Rect2.cs), [`src/Core/Math/Transform2D.cs`](../../src/Core/Math/Transform2D.cs), and [`src/Core/Math/Side.cs`](../../src/Core/Math/Side.cs).

## Runtime flow

1. Callers construct or copy `Rect2` and `Transform2D` values from `System.Numerics.Vector2` values or explicit floats.
2. Ordinary mutation stores components without validation, hidden rectangle normalization, or transform canonicalization.
3. Rectangle callers use `Abs()` explicitly before operations whose contract requires non-negative size; transform callers select the general or orthonormal inverse contract explicitly.
4. Geometry operations return values or booleans and do not touch global state.
5. Typed persistence captures finite rectangles and transforms through strict configuration schemas or direct packed-scene value copying.

## Dependencies

- .NET vector, numeric, globalization, and interop-layout primitives.
- Existing typed `ConfigFile`, property descriptor, and packed-scene integration boundaries.
- No external package, native library, Scene, renderer, SDL, input, audio, physics, scripting, resource, or editor dependency.

## Invariants

- `Rect2` remains a sequential 16-byte value containing position then size.
- `Transform2D` remains a sequential 24-byte value containing X, Y, then Origin columns; the all-zero default is not identity.
- Most geometry operations require non-negative size; the type never normalizes implicitly.
- Point containment includes left/top and excludes right/bottom.
- Intersection excludes outer border-only contact by default; `Intersects` can opt into border contact, while a zero-size rectangle strictly inside another retains its position as an empty intersection.
- Ordinary math retains IEEE 754 values; finite persistence is a separate boundary.
- Numeric hot paths do not allocate after warmup.
- Transform multiplication applies the right operand first; global operations left-multiply and local operations right-multiply.
- General inversion rejects an exactly singular basis, while transpose-based inverse operations retain an explicit orthonormal-basis precondition.
- `Side` numeric values remain left `0`, top `1`, right `2`, bottom `3`.

## Current implementation status

Implemented and verified. The delivered surface covers complete rectangle behavior independent of missing geometry types and complete standalone affine behavior: storage, constructors, decomposition, composition, point/basis transformation, general and orthonormal inversion, interpolation, local/global operations, finite/conformal/exact/approximate comparison, hashing, and invariant formatting.

## Exclusions and deferred integration

- The engine-owned `Vector` required by ADR 0032 is not implemented. Current production geometry still uses `System.Numerics.Vector2`; future engine components must not treat that temporary surface as the permanent contract.
- A constructor from an integer rectangle is deferred until a complete `RectI` exists under the final unsuffixed vocabulary.
- Rectangle transform operators and migration of `Node` from `Matrix3x2` are deferred to the explicit integration slice under ADR 0026 and ADR 0029; the standalone transform type is complete without claiming either migration.
- Boolean truth conversion is permanently excluded because it is not an appropriate typed C# contract.
- No renderer clip/scissor conversion, UI layout, viewport, image region, atlas, broad-phase, collision shape, or native structure conversion exists yet. Those integrations belong to their owning future domains and are not represented by placeholders.
- No 3D box, face, or axis API is in scope.

## Verification

The executable harness covers every current rectangle and transform member family, negative/zero sizes, boundary inclusion, undefined enum input, affine order and decomposition, singular/orthonormal inverse boundaries, malformed/non-finite persistence, packed-scene copying, IEEE equality edges, and warmed allocation behavior. Verification is Linux/.NET 8 only; native and five-platform behavior is not yet exercised.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate Transform2D type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
- [0032: Engine-owned unsuffixed 2D math vocabulary](../decisions/core-math.md#adr-0032)
