# PhysicsFixtureTag

Last updated: 2026-10-10

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
