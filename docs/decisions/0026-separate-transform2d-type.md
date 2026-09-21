# ADR 0026: Separate Transform2D foundational type

Last updated: 2026-09-21

## Status

Accepted and fulfilled for the standalone value by ADR 0029. This decision partially supersedes ADR 0008 only where that ADR rejected a separate `Transform2D` type. Migration of `Node` and `Rect2` remains pending; the unified `Node` hierarchy and all other ADR 0008 decisions remain accepted.

## Context

ADR 0008 chose `System.Numerics.Matrix3x2` directly for the first complete unified `Node` implementation and explicitly avoided a speculative transform wrapper. That kept the initial scene slice executable, but it does not express the final engine API direction: Electron2D requires a dedicated 2D transform value with a stable engine-owned contract and direct integration with geometry types such as `Rect2`.

At the time of this decision, the requirement was architectural rather than a claim that the type already existed. ADR 0029 has since delivered the standalone production type. `Node.Transform` and `Node.GlobalTransform` still remain `Matrix3x2` values.

## Decision

Electron2D provides a separate public foundational value type named `Transform2D`. ADR 0029 records its audited public surface, XML/living documentation, error/threading/allocation contracts, persistence, executable tests, and post-implementation audit.

The type owns the engine-facing 2D affine-transform vocabulary, including basis/origin representation, composition, forward and inverse point/vector transformation, affine inversion, and finite/approximate checks. Rectangle transformation belongs to the pending integration slice because it changes `Rect2`; its absence does not make the standalone transform a placeholder.

Until the migration slice is complete:

- `Matrix3x2` remains the truthful current `Node` API and implementation;
- `Rect2` transform multiplication remains explicitly dependency-blocked;
- no implicit compatibility shim or misleading operator is added;
- current behavior must not be documented as already migrated.

The separate migration must update `Node`, `Rect2`, tests, XML documentation, class/component/domain documents, inventory, and the ADR chain atomically. Packed-scene/property/config persistence for standalone `Transform2D` values is already implemented and verified by ADR 0029.

Electron2D remains 2D-only. No generic 3D-capable `Transform` or `Transform3D` type is authorized by this decision.

## Consequences

- The final public geometry vocabulary will not expose `Matrix3x2` as the permanent engine abstraction.
- Current code stays complete and honest while the remaining migration is tracked explicitly rather than represented by a shim.
- `Rect2` ships all independent geometry and can add transform operators in the deliberate migration now that both operand contracts exist.
- The future migration may be source-breaking for current `Node` transform consumers and therefore requires an explicit compatibility and release decision during implementation.
- Standard-library numerics may still be used internally or through explicit conversions, but they do not replace the required engine-owned type.

## Rejected alternatives

- **Keep `Matrix3x2` permanently:** rejected by the required engine API direction and missing direct geometry contract.
- **Add an empty wrapper now:** rejected because it would be a misleading compatibility stub and violate the definition of done.
- **Implement `Transform2D` inside the `Rect2` task:** rejected because it is an independent foundational type with a substantial API, invariants, and migration impact that required its own complete vertical slice, now recorded by ADR 0029.
- **Add a generic or 3D transform family:** rejected because Electron2D is exclusively 2D.

## Verification boundary

ADR 0029 fulfills and verifies the standalone value requirement. Current tests verify that value independently alongside `Node`'s existing `Matrix3x2` contract and `Rect2`'s transform-independent geometry. They do not claim that Node migration or rectangle transformation is complete.

## Related decision

- [0029: Typed Transform2D value and affine semantics](0029-typed-transform2d-value.md)
