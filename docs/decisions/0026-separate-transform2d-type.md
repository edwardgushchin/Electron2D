# ADR 0026: Separate Transform2D foundational type

Last updated: 2026-09-21

## Status

Accepted. This decision partially supersedes ADR 0008 only where that ADR rejected a separate `Transform2D` type. The unified `Node` hierarchy and all other ADR 0008 decisions remain accepted.

## Context

ADR 0008 chose `System.Numerics.Matrix3x2` directly for the first complete unified `Node` implementation and explicitly avoided a speculative transform wrapper. That kept the initial scene slice executable, but it does not express the final engine API direction: Electron2D requires a dedicated 2D transform value with a stable engine-owned contract and direct integration with geometry types such as `Rect2`.

The requirement is architectural, not a claim that the type already exists. The current source, generated XML, class inventory, and executable tests contain no `Transform2D` production type. `Node.Transform` and `Node.GlobalTransform` currently remain `Matrix3x2` values.

## Decision

Electron2D will provide a separate public foundational value type named `Transform2D`. It must be implemented through the repository's full production-ready vertical-slice process, including official API and inheritance/value-semantic coverage, XML documentation, living documentation, error/threading/allocation contracts, positive and negative tests, and a post-implementation audit.

The eventual type must own the engine-facing 2D affine-transform vocabulary, including basis/origin representation, composition, forward and inverse point/vector transformation, affine inversion, finite/approximate checks, and rectangle transformation. Its exact public layout and typed C# adaptations are intentionally left to that implementation audit; this ADR does not predeclare unverified members or add placeholders.

Until that vertical slice is complete:

- `Matrix3x2` remains the truthful current `Node` API and implementation;
- `Rect2` transform multiplication remains explicitly dependency-blocked;
- no empty `Transform2D`, implicit compatibility shim, or misleading operator is added;
- current behavior must not be documented as already migrated.

After `Transform2D` is production-ready, a separate migration must update `Node`, `Rect2`, packed-scene/property persistence as applicable, tests, XML documentation, class/component/domain documents, inventory, and the ADR chain atomically.

Electron2D remains 2D-only. No generic 3D-capable `Transform` or `Transform3D` type is authorized by this decision.

## Consequences

- The final public geometry vocabulary will not expose `Matrix3x2` as the permanent engine abstraction.
- Current code stays complete and honest while the missing type is tracked as a concrete dependency rather than represented by a stub.
- `Rect2` can ship all independent geometry now and add transform operators only when both operand contracts exist.
- The future migration may be source-breaking for current `Node` transform consumers and therefore requires an explicit compatibility and release decision during implementation.
- Standard-library numerics may still be used internally or through explicit conversions, but they do not replace the required engine-owned type.

## Rejected alternatives

- **Keep `Matrix3x2` permanently:** rejected by the required engine API direction and missing direct geometry contract.
- **Add an empty wrapper now:** rejected because it would be a misleading compatibility stub and violate the definition of done.
- **Implement `Transform2D` inside the `Rect2` task:** rejected because it is an independent foundational type with a substantial API, invariants, and migration impact that require their own complete vertical slice.
- **Add a generic or 3D transform family:** rejected because Electron2D is exclusively 2D.

## Verification boundary

This ADR records a required future type and the current dependency boundary. It introduces no executable transform behavior. Current tests continue to verify `Node`'s existing `Matrix3x2` contract and `Rect2`'s transform-independent geometry; future `Transform2D` behavior cannot be claimed verified until its implementation task is complete.
