# PhysicsShapeCollision

Last updated: 2026-10-11

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

FullMotionContact now clips the incident edge against a selected full-contour face
and emits up to two actual boundary points, deepest first. This preserves face
contributions in weighted body-motion recovery while keeping single-point fallback,
caller-owned results and reused hull buffers. See [collision priority](../components/physics-contact-policy.md#collision-priority).

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).

## Selected shape-pair operations

The internal resource/resource and body-slot/resource bridge now executes through the retained implementation with complete logical indices, composed global poses and validated nested caller-span results. Detached bodies use the same exact resource kernel without a solver; borrowed collider release is preflighted before registry cleanup. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes instead of complete motion/field/sleep state when no current cache exists. [The pair contract and checks](../components/physics-shape-pairs.md) distinguish this internal prerequisite from the pending complete public server-extension family.

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.
