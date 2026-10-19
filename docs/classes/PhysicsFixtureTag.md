# PhysicsFixtureTag

Last updated: 2026-10-20

**Declaration:** `internal sealed record PhysicsFixtureTag`

**Source:** [PhysicsFixtureTag.cs](../../src/Servers/Physics/PhysicsFixtureTag.cs)
**Component:** [Physics queries](../components/physics-queries.md)

Fixture metadata retains public collider RID, logical shape slot, optional one-way
and directed-ray data, and a weak scene owner. All pieces of a compound resource
share its logical identity. The optional CompoundContour borrows a large convex
resource through a weak reference and records its authored local pose. Motion
queries resolve its complete points after world preparation; missing/disposed
resources reject. No tag owns or disposes the application resource.

PhysicsMotionTests verifies full-contour recovery and directed containment;
GPUPhysicsMotionQueryTests compares the corresponding resident implementation.

The Source weak reference identifies the authored Shape for solver-policy updates. It does not extend resource lifetime; policy edits retain the fixture tag and logical index.


Owner borrows the shared collider adapter for current object association. Query/contact publication samples ObjectIdentity before callbacks; SceneOwner remains physical scene convenience. See [object associations](../components/physics-object-bindings.md).

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).

## Reuse during shape edits

Tags are private mutable compilation metadata. The owning CPU attachment retires
all fixtures before repopulating its retained tag list; published public results
contain sampled values, never these tags. Piece tags keep independent weak source
references and share only the current compound contour. Reuse refreshes every
logical, one-way and directed field. [Shape-edit checks](../components/physics-queries.md#failed-raw-shape-edits-and-warmed-reuse)
cover transformed compound/directed queries and stable Area effects.


[Special source motion](../components/physics-registered-adapter.md#special-source-motion)
retains represented family across physical pieces and live shape replacement.
Initial recovery/residual stopping still executes; ignored special displacement
cannot manufacture a new sweep, and two concave resources cannot contact through
private edge pieces. Shared public CPU/software-GPU/registered assertions cover
logical slots, analytical boundaries, fractions and zero warmed full-frame bytes.
