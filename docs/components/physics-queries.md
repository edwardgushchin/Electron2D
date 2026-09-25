# Physics server and direct queries component

Last updated: 2026-09-25

## Scope and owned types

[`RID`](../classes/RID.md) is the shared opaque identity value. [`PhysicsServer2D`](../classes/PhysicsServer2D.md) owns its physics resource registry. [`World2D`](../classes/World2D.md) exposes a SceneTree's registered space and [`PhysicsDirectSpaceState2D`](../classes/PhysicsDirectSpaceState2D.md). Mutable [`PhysicsRayQueryParameters2D`](../classes/PhysicsRayQueryParameters2D.md) and [`PhysicsPointQueryParameters2D`](../classes/PhysicsPointQueryParameters2D.md) configure queries; immutable [`PhysicsRayResult2D`](../classes/PhysicsRayResult2D.md) and [`PhysicsPointResult2D`](../classes/PhysicsPointResult2D.md) carry typed results. The server's [`BodyMode`](../classes/PhysicsServer2D.BodyMode.md) selects explicit body motion.

## Runtime flow

A SceneTree registers its existing Box2D space when the first scene collider enters or a CanvasItem asks for World2D. Each CollisionObject owns a stable RID from construction to disposal; fixture rebuilds attach that RID and a shape-owner index to all generated backend pieces. The server also creates explicit spaces, bodies, Areas and shapes. A server collider may attach to an explicit space or the SceneTree space. Explicit spaces advance through `SpaceStep`; a SceneTree advances its own space in the fixed physics lane. Freeing a resource removes its registry entry without reusing the numeric RID.

Direct ray and point views prepare pending scene geometry/poses, then scan current body, Area and explicit-server fixtures. Query masks inspect collider layers independent of those colliders' masks; optional flags include sensors or bodies, RID arrays exclude objects, and results retain both RID and scene object where one exists. Server-only results have a null scene collider and zero InstanceID. Ray hits choose nearest fraction with RID/index tie order. Point hits sort and deduplicate by RID/index before applying the result cap. A direct view cannot query during solver stepping or off the space owner thread.

## Dependencies, invariants and limits

The server builds on existing Shape resources, SceneTree owner-thread rules and the single internal Box2D.NET solver world. Its six shape creation families cover only shapes already implemented as resources; world-boundary, separation-ray, custom and joint resources remain separate slices. Canvas and navigation-map RIDs, independent viewport world assignment/notifications, canvas-instance point filtering, direct shape sweeps and wider server state/area/joint methods remain coverage gaps. Server-only colliders share query/solver state but are not represented in current scene Area object-event arrays or area field reduction. Query scans are linear in fixture count; the checked warmed scene physics paths remain allocation-free, while per-call query result allocations, native allocation, other platforms and large-world throughput have not been audited.

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks zero/copy/freed identities, shared scene/server spaces, all six server shape families, live geometry changes, ray/point filters and ordering, explicit stepping, cross-space moves, errors and teardown. [ADR 0063](../decisions/physics.md#adr-0063) defines the ownership and query contract.
