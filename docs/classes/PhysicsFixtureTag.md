# PhysicsFixtureTag

Last updated: 2026-10-09

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
