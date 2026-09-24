# Collision shapes component

Last updated: 2026-09-25

## Scope and owned types

[`Shape`](../classes/Shape.md) is the abstract managed geometry resource. [`CircleShape`](../classes/CircleShape.md), [`CapsuleShape`](../classes/CapsuleShape.md), [`SegmentShape`](../classes/SegmentShape.md) and [`RectangleShape`](../classes/RectangleShape.md) provide concrete fixtures. [`CollisionShape`](../classes/CollisionShape.md) is a spatial scene child that borrows one Shape and contributes it to a direct `PhysicsBody` or `Area` parent. None of these public types expose backend handles.

## Runtime flow

Circle radius defaults to 10 scene units, capsule radius/full height to 10/30, segment endpoints to (0, 0)/(0, 10), and rectangle size to (20, 20). Capsule height and radius stay linked; MidHeight sets their central separation. The capsule accepts finite nonnegative dimensions, including zero and line/circle limits. Circle and rectangle require finite positive dimensions; segment endpoints and their difference must be finite and may coincide. All four report local bounds, publish geometry changes and support independent resource duplication; equal capsule radius/height writes do not publish, while an equal MidHeight write does. A CollisionShape starts with no resource and `Disabled=false`; assigning a live resource or toggling Disabled updates its direct collision owner's fixtures before the next physics step. Local position and rotation offset the fixture; scale/skew are rejected while active. The parent body or area owns its backend fixtures, while the caller owns the Shape resource.

Shape `Changed` and `Disposed` notifications mark the attached owner's fixture set dirty. Shape setters validate geometry before mutation; rebuild validates active transforms before destroying old fixtures. Configuration warnings report a missing direct collision owner or a missing/disposed resource. A detached CollisionShape is still a normal Entity and can be packed into a scene with its borrowed resource association.

## Dependencies and verification

Shapes use Core Vector2/Rect2/Resource contracts, the Entity scene hierarchy and internal Box2D.NET geometry. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, validation, bounds, duplication, live circle resizing, disabled fixtures, PackedScene restoration and borrowed lifetime; [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks coupled dimensions, rotation, body/area fixtures and warmed allocations. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks two-sided terrain, dynamic and sensor fixtures, mass and warmed allocations. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks direct area ownership and sensor edits. The capsule and segment sub-slop fixture adaptations are defined in [ADRs 0059 and 0061](../decisions/physics.md#adr-0061). One-way contact options, debug drawing and the inherited standalone Shape collision/sweep methods remain separate Blocked or Unimplemented rows in [coverage](../coverage/index.md).

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
- [0055: Directional scene area monitoring](../decisions/physics.md#adr-0055)
- [0059: Capsule collision geometry](../decisions/physics.md#adr-0059)
- [0061: Segment collision geometry](../decisions/physics.md#adr-0061)
