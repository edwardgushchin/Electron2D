# Physics areas component

Last updated: 2026-09-24

## Scope and owned types

[`Area`](../classes/Area.md) is the sensor branch of [`CollisionObject`](../classes/CollisionObject.md). Direct [`CollisionShape`](../classes/CollisionShape.md) children borrow circle or rectangle geometry and become nonresponding backend fixtures. A SceneTree's internal physics space owns the backend body and the fixed-step overlap scan.

## Runtime flow

Body callbacks and the four-substep backend world advance before areas scan. An area's mask tests each candidate's layer, regardless of the candidate's mask. Another area also needs to be monitorable. Backend bounding boxes reject separated shape pairs; backend shape distance checks candidates exactly. Multiple overlapping shape pairs produce one object-level result. Snapshots and entered/exited transitions are committed once per nonzero fixed step. Event delivery runs after backend stepping so handlers can change the scene; removal of an object clears other areas' snapshots immediately.

## Dependencies, invariants and limits

The first profile accepts unit global scale and zero skew while active. Shape/resource, filtering and monitoring edits take effect on the next step. Borrowed resources remain caller-owned; tree exit and disposal release backend handles. The pairwise scan has quadratic candidate growth; add a spatial candidate index when measured large-scene cost requires it. Tile-map virtual collision bodies, gravity/damping priority reduction, audio-bus routing and typed shape/RID events remain separate coverage work.

[AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks monitoring, directional masks, snapshot/event timing, multi-shape deduplication, moving-body passage without response, scene packing, lifecycle, callback exceptions and 64 warmed steady and empty frames each with zero managed allocations on Linux/.NET 8. Native allocator counts, other platforms and owner visual acceptance remain unverified. [ADR 0055](../decisions/physics.md#adr-0055) records the execution boundary.
