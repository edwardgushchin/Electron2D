# Physics areas component

Last updated: 2026-09-25

## Scope and owned types

[`Area`](../classes/Area.md) is the sensor branch of [`CollisionObject`](../classes/CollisionObject.md). Direct [`CollisionShape`](../classes/CollisionShape.md) children borrow circle, capsule, segment, convex polygon or rectangle geometry and become nonresponding backend fixtures. A SceneTree's internal physics space owns the backend body and the fixed-step overlap scan.

## Runtime flow

Body callbacks and the four-substep backend world advance before areas scan. An area's mask tests each candidate's layer, regardless of the candidate's mask. Another area also needs to be monitorable. Backend bounding boxes reject separated shape pairs; backend shape distance checks candidates exactly. Multiple overlapping shape pairs produce one object-level result. Snapshots and entered/exited transitions are committed once per nonzero fixed step. Event delivery runs after backend stepping so handlers can change the scene; removal of an object clears other areas' snapshots immediately.

Before that backend step, each dynamic body resolves currently overlapping area fields by greatest priority first. Gravity, linear damping and angular damping have independent Disabled/Combine/CombineReplace/Replace/ReplaceCombine modes. An unstopped channel includes the sampled world default; body gravity scale and damping modes apply afterward. Point gravity transforms the area's local point and supports constant strength or inverse-square falloff. Field participation does not require `Monitoring` or `Monitorable` and does not wait for the event snapshot.

## Dependencies, invariants and limits

The current profile accepts unit global scale and zero skew while active. Shape/resource, filtering, monitoring and field edits take effect on the next step. Borrowed resources remain caller-owned; tree exit and disposal release backend handles. The pairwise scan has quadratic candidate growth; add a spatial candidate index when measured large-scene cost requires it. Tile-map virtual collision bodies, absent CharacterBody gravity queries, audio-bus routing and typed shape/RID events remain separate coverage work.

[AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks monitoring, directional masks, snapshot/event timing, multi-shape deduplication, moving-body passage without response, scene packing, lifecycle, callback exceptions and 64 warmed steady and empty frames each with zero managed allocations. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks field modes, point falloff, sampled defaults, signed damping, sleep wakeup, packing, failure recovery and 64 warmed moving and sleeping-body frames each with zero managed allocations on Linux/.NET 8. Native allocator counts, other platforms and owner visual acceptance remain unverified. [ADRs 0055 and 0056](../decisions/physics.md#adr-0056) record the execution boundary.
