# Collision shapes component

Last updated: 2026-09-25

## Scope and owned types

[`Shape`](../classes/Shape.md) is the abstract managed geometry resource. [`CircleShape`](../classes/CircleShape.md), [`CapsuleShape`](../classes/CapsuleShape.md), [`SegmentShape`](../classes/SegmentShape.md), [`ConvexPolygonShape`](../classes/ConvexPolygonShape.md), [`ConcavePolygonShape`](../classes/ConcavePolygonShape.md) and [`RectangleShape`](../classes/RectangleShape.md) provide concrete fixtures. [`CollisionShape`](../classes/CollisionShape.md) is a spatial scene child that borrows one Shape and contributes it to a direct `PhysicsBody` or `Area` parent. None of these public types expose backend handles.

## Runtime flow

Circle radius defaults to 10 scene units, capsule radius/full height to 10/30, segment endpoints to (0, 0)/(0, 10), and rectangle size to (20, 20). Capsule height and radius stay linked; MidHeight sets their central separation. The capsule accepts finite nonnegative dimensions, including zero and line/circle limits. Circle and rectangle require finite positive dimensions; segment endpoints and their difference must be finite and may coincide. All six report local bounds, publish geometry changes and support independent resource duplication; equal capsule radius/height writes do not publish, while an equal MidHeight write does. A CollisionShape starts with no resource, `Disabled=false`, `OneWayCollision=false` and local direction `(0, 1)`; assigning a live resource or changing either flag updates its direct collision owner's fixtures before the next physics step. Local position and rotation offset the fixture; scale/skew are rejected while active. The parent body or area owns its backend fixtures, while the caller owns the Shape resource.

A convex polygon starts empty, accepts convex perimeter arrays or a point cloud, and can append several solid fixtures beyond eight vertices while preserving one public resource. A concave polygon stores independent endpoint pairs and appends hollow line fixtures; a body fully inside closed edges does not overlap them. A body child can mark all its fixtures one-way and choose a normalized local pass-through direction; the physics world keeps each pair's initial contact side until separation. An Area child keeps the flag for scene state but remains a two-sided sensor. Shape `Changed` and `Disposed` notifications mark the attached owner's fixture set dirty. Shape setters validate geometry before mutation; rebuild validates active transforms before destroying old fixtures. Configuration warnings report a missing direct collision owner, a missing/disposed resource, or an ineffective Area one-way setting. A detached CollisionShape is still a normal Entity and can be packed into a scene with its borrowed resource association.

## Dependencies and verification

Shapes use Core Vector2/Rect2/Resource contracts, the Entity scene hierarchy and internal Box2D.NET geometry. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, validation, bounds, duplication, live circle resizing, disabled fixtures, PackedScene restoration and borrowed lifetime; [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks coupled dimensions, rotation, body/area fixtures and warmed allocations. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks two-sided terrain, dynamic and sensor fixtures, mass and warmed allocations. [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks hulls, compound fixtures, dynamic contacts, area sensing and warmed allocations. [ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks paired edges, hollow contact and sensors. [OneWayCollisionTests](../../tests/Electron2D.Tests/OneWayCollisionTests.cs) checks initial contact side, rotation, live changes, packing, Area sensing and warmed allocation. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks direct area ownership and sensor edits. The capsule and segment sub-slop fixture adaptations are defined in [ADRs 0059 and 0061](../decisions/physics.md#adr-0061); [ADR 0065](../decisions/physics.md#adr-0065) defines one-way body contacts. One-way sweep margin, debug drawing and inherited standalone Shape collision/sweep methods remain separate Blocked or Unimplemented rows in [coverage](../coverage/index.md).

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
- [0055: Directional scene area monitoring](../decisions/physics.md#adr-0055)
- [0059: Capsule collision geometry](../decisions/physics.md#adr-0059)
- [0061: Segment collision geometry](../decisions/physics.md#adr-0061)
- [0062: Compound convex fixtures](../decisions/physics.md#adr-0062)
- [0064: Hollow paired-segment resource](../decisions/physics.md#adr-0064)
- [0065: One-way scene-body contacts](../decisions/physics.md#adr-0065)
