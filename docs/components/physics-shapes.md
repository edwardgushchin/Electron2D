# Collision shapes component

Last updated: 2026-09-24

## Scope and owned types

[`Shape`](../classes/Shape.md) is the abstract managed geometry resource. [`CircleShape`](../classes/CircleShape.md) and [`RectangleShape`](../classes/RectangleShape.md) provide the first concrete fixtures. [`CollisionShape`](../classes/CollisionShape.md) is a spatial scene child that borrows one Shape and contributes it to a direct `PhysicsBody` parent. None of these public types expose backend handles.

## Runtime flow

Circle radius defaults to 10 scene units and rectangle size to (20, 20). Both validate finite positive dimensions, publish `Changed` after mutation, report local bounds and support independent resource duplication. A CollisionShape starts with no resource and `Disabled=false`; assigning a live resource or toggling Disabled updates its parent body's fixtures before the next physics step. Local position and rotation offset the fixture; scale/skew are rejected while active. The parent body owns its backend fixtures, while the caller owns the Shape resource.

Shape `Changed` and `Disposed` notifications mark the attached body's fixture set dirty. Rebuild validates all active geometry before destroying old fixtures. Configuration warnings report a missing direct physics parent or a missing/disposed resource. A detached CollisionShape is still a normal Entity and can be packed into a scene with its borrowed resource association.

## Dependencies and verification

Shapes use Core Vector2/Rect2/Resource contracts, the Entity scene hierarchy and internal Box2D.NET geometry. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, validation, bounds, duplication, live circle resizing, disabled fixtures, PackedScene restoration and borrowed lifetime. One-way contact options, debug drawing and the inherited standalone Shape collision/sweep methods remain separate Blocked or Unimplemented rows in [coverage](../coverage/index.md).

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
