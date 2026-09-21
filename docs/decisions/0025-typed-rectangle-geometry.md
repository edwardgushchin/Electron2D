# ADR 0025: Typed axis-aligned rectangle geometry

Last updated: 2026-09-21

## Status

Accepted.

## Context

Electron2D needs a foundational floating-point rectangle before rendering, UI, culling, images, collision, and navigation are designed. The official 4.7.2 stable `Rect2` contract and typed C# implementation were audited together with the native `rect2.h` behavior and global `Side` values. The relevant surface includes mutable position/size/end, signed area, normalization, containment, intersection, enclosure, growth, support mapping, merge, finite and approximate checks, formatting, four edge identities, an integer-rectangle conversion, transform multiplication, and a language-specific boolean conversion.

The repository already uses `System.Numerics.Vector2` throughout unified 2D nodes. Adding a second vector type solely for rectangle parity would duplicate arithmetic and introduce conversion noise. Conversely, substituting `System.Drawing.RectangleF` would bring an unsuitable framework identity and different semantics.

## Decision

`Electron2D.Rect2` is a mutable `[Serializable]`, sequential, 16-byte `struct` implementing `IEquatable<Rect2>`. It stores two private `System.Numerics.Vector2` fields and exposes the complete stable typed C# rectangle surface that does not depend on missing Electron2D types. `Electron2D.Side` is a separate four-value enum with stable numeric identities.

The following contracts are fixed:

- ordinary construction/mutation preserves negative, zero, NaN, and infinite components; normalization is explicit through `Abs()`;
- methods that rely on ordered edges document non-negative size rather than normalizing silently;
- point containment is half-open at the right and bottom edges;
- outer border-only contact is excluded by default, while `Intersects` has an explicit border-inclusion option; a zero-size rectangle strictly inside another retains its position as an empty intersection;
- algebraic growth may over-shrink and create non-positive size;
- an undefined `Side` passed to `GrowSide` is a no-op, matching the audited typed implementation;
- exact equality exposes IEEE NaN behavior; approximate equality checks exact equality first and otherwise uses the repository's scale-aware `0.00001` tolerance;
- numeric formatting is invariant-culture;
- `ConfigFile` accepts only finite rectangles and persists exactly nested `Position.X/Y` and `Size.X/Y` fields, rejecting missing, duplicate, unknown, nonnumeric, or non-finite input;
- typed property descriptors and packed scenes store/copy `Rect2` directly because it contains no managed references;
- a `Rect2I` constructor is deferred until that complete engine type exists; transform multiplication remains deferred to the explicit Node/Rect2 migration recorded by ADR 0026 and ADR 0029;
- language-specific boolean truth conversion is permanently excluded.

No renderer, UI, physics, or platform abstraction is created by this decision.

## Consequences

- Core gains allocation-free rectangle geometry usable by future domains without acquiring dependencies on them.
- Existing `Vector2` transforms and positions interoperate directly with rectangle positions, sizes, centers, support points, and query points.
- Negative sizes remain representable and observable; callers must decide when normalization is appropriate.
- Configuration persistence rejects non-finite values even though ordinary runtime geometry retains them.
- A future `Rect2I` implementation and the pending migration to the now-implemented standalone `Transform2D` must add and verify the deferred rectangle members rather than retrofitting unrelated .NET types.
- Sequential layout is useful for predictable managed storage but does not promise native backend ABI equivalence.

## Rejected alternatives

- **Wait for a renderer or physics backend:** rejected because rectangle math and typed persistence are independently useful foundational behavior.
- **Create a custom `Vector2`:** rejected because `System.Numerics.Vector2` already supplies the required portable value contract and is established throughout the repository.
- **Use `System.Drawing.RectangleF`:** rejected because its API/semantics differ and the dependency is inappropriate for the runtime target matrix.
- **Normalize on every construction:** rejected because it destroys intentional negative-size values and diverges from the audited contract.
- **Add placeholder `Rect2I` or `Transform2D` types:** rejected because empty compatibility shells would violate the repository definition of done. ADR 0026 required a real standalone `Transform2D` vertical slice, now fulfilled by ADR 0029.
- **Serialize every public property automatically:** rejected because computed `End` and `Area` would create a redundant, unstable, ambiguous schema.

## Verification

`tests/Electron2D.Tests/Program.cs` covers layout/defaults, every implemented constructor/member/operator, negative/zero size boundaries, all side values and undefined input, half-open containment, overlap and border behavior, IEEE values, invariant formatting, strict configuration shape and failure rollback, packed-scene copying, and zero allocations in a warmed geometry loop.

Verification is currently Linux/.NET 8. Native structure equivalence, renderer/physics use, integer rectangles, transform multiplication, and the full five-platform matrix remain unavailable.

## Related decision

- [0026: Separate Transform2D type](0026-separate-transform2d-type.md)
- [0029: Typed Transform2D value and affine semantics](0029-typed-transform2d-value.md)
