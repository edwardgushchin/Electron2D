# PhysicsShapeCollision

Last updated: 2026-10-09

**Declaration:** `internal static class PhysicsShapeCollision`

**Source:** [PhysicsShapeCollision.cs](../../src/Servers/Physics/PhysicsShapeCollision.cs)
**Component:** [Physics queries](../components/physics-queries.md)

Evaluate implements resource-only collision and paired boundary contacts, using
shared geometry, transformed complete convex hulls, swept regions and directed-ray
policy. Per-thread buffers retain peak capacity. FullMotionContact reuses that
hull/SAT path for body-motion recovery and impact geometry whenever either body
has a partitioned convex shape. FullMotionRegionContains rejects directed-ray
origins inside a whole convex contour or its translation region before motion
queries inspect individual backend pieces. Primitive profiles and backend-unit
results are converted only at this internal boundary.

ShapeCollisionTests covers resource semantics; PhysicsMotionTests covers whole
contour recovery/containment and warmed allocation. GPUPhysicsMotionQueryTests
compares body motion with resident kernels. See [verification limits](../components/gpu-resident-motion-queries.md).
