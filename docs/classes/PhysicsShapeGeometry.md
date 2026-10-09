# PhysicsShapeGeometry

Last updated: 2026-10-08

**Declaration:** `internal readonly ref struct PhysicsShapeGeometry`

**Source:** [PhysicsShapeGeometry.cs](../../src/Servers/Physics/PhysicsShapeGeometry.cs)
**Component:** [Collision shapes](../components/physics-shapes.md)

## Contract

Shape.GetGeometry returns a temporary view of local geometry in scene units. The
view contains a kind, endpoints or box corners, a radius, a borrowed vertex span
and the directed-ray slope flag. It contains no world identity, vendor handles,
backend structs or compiled fixture partitions. It never copies a contour.

Circle, capsule, segment, rectangle, convex contour, paired concave edges and
separation ray and infinite world boundary implement the same internal resource operation. Callers consume
the span synchronously while the resource is live and unchanged. A disposed
resource rejects access. The internal abstract operation preserves the current
built-in-only shape implementation boundary; public extension support remains open.

[PhysicsShapeBackend](PhysicsShapeBackend.md) compiles the view for CPU fixtures,
mass geometry and world-query proxies. PhysicsShapeCollision also consumes it
directly for standalone resource collisions, retaining the full convex contour
instead of backend partitions. That CPU collision kernel still has internal backend
numeric/proxy dependencies. GPUPhysicsBodyStore consumes the same source for
resident geometry, broad-phase bounds and narrow-phase contact points without
a CPU solver world or fixture-size contour partition. This view does not implement an independent GPU world.

## Verification

The shape, query, motion, mass and standalone collision suites execute the geometry
families through public APIs, including existing warmed allocation checks.
ConvexPolygonShapeTests additionally checks shared borrowers, independent resource
copies, rejected edits and 128 warmed alternating-resource queries with zero
managed allocation. No new whole-world performance or device acceptance is claimed.

For WorldBoundary, A stores the normalized outward normal and Radius stores the
normalized signed plane offset. This tagged representation has no finite collision
radius or contour; consumers must dispatch on Kind before using those fields.
