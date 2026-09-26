# Physics areas component

Last updated: 2026-09-26

## Scope and owned types

[`Area`](../classes/Area.md) is the sensor branch of [`CollisionObject`](../classes/CollisionObject.md). Direct [`CollisionShape`](../classes/CollisionShape.md) children borrow circle, capsule, segment, convex polygon, concave segment collection, separation ray or rectangle geometry and become nonresponding backend fixtures. A SceneTree's internal physics space owns the backend body and the fixed-step overlap scan.

## Runtime flow

Direct CollisionPolygon children contribute owned solid convex pieces or closed hollow edge sensors alongside borrowed CollisionShape children. Polygon one-way settings do not change Area overlap detection.

Body callbacks and the four-substep backend world advance before areas scan. An area's mask tests each candidate's layer, regardless of the candidate's mask. Another area also needs to be monitorable. Backend bounding boxes reject separated shape pairs; backend shape distance checks candidates exactly. Multiple overlapping shape pairs produce one object-level result. Snapshots and entered/exited transitions are committed once per nonzero fixed step. Event delivery runs after backend stepping so handlers can change the scene; removal of an object clears other areas' snapshots immediately.

Before that backend step, each RigidBody resolves currently overlapping area fields by greatest priority first. Gravity, linear damping and angular damping have independent Disabled/Combine/CombineReplace/Replace/ReplaceCombine modes. An unstopped channel includes the sampled world default; body gravity scale and damping modes apply afterward. CharacterBody resolves the same gravity channel for its inherited `GetGravity()` query but does not automatically change its caller-owned `Velocity`. Point gravity transforms the area's local point and supports constant strength or inverse-square falloff. Field participation does not require `Monitoring` or `Monitorable` and does not wait for the event snapshot.

## Dependencies, invariants and limits

An Area owns a stable [RID](../classes/RID.md). A [direct ray or point query](physics-queries.md) includes its sensor fixtures only when `CollideWithAreas` is enabled; the query's layer mask tests this area's layer even when its own mask is zero. Server-created Areas share the Box2D space for direct queries, while this component's typed object-level overlap events still cover scene collision objects only.

An Area uses nonresponding sensor fixtures; a child CollisionShape's one-way body-contact setting does not filter either approach side. The child warns about the ineffective setting, while its scene properties remain packable.

The current profile accepts unit global scale and zero skew while active. Shape/resource, filtering, monitoring and field edits take effect on the next step. Borrowed resources remain caller-owned; tree exit and disposal release backend handles. The pairwise scan has quadratic candidate growth; add a spatial candidate index when measured large-scene cost requires it. Tile-map virtual collision bodies, audio-bus routing and typed shape/RID events remain separate coverage work.

[AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks monitoring and event timing. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks field modes, point falloff, sampled defaults, damping and warmed RigidBody frames; [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks world/Area gravity selection on a kinematic character. Native allocator counts, other platforms and owner visual acceptance remain unverified. [ADRs 0055 and 0056](../decisions/physics.md#adr-0056) record the execution boundary.

Separation-ray overlap uses exact endpoints and the directed query kernel rather than a GJK segment overlap. Containment and ray-ray pairs are rejected. This also governs field-area selection. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) checks directed Area entry and containment exit under [ADR 0068](../decisions/physics.md#adr-0068).

Scene Area field reduction now includes server-created dynamic/kinematic bodies through the shared shape-overlap/mask reducer. Dynamic server gravity/damping uses the same selected field, omission policy and field-change wakeup; server-only Area field configuration and object monitoring remain separate capabilities. PhysicsBodyStateTests checks this integration.

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration and warmed zero managed allocation under [ADR 0071](../decisions/physics.md#adr-0071).

Logical shape-pair monitoring now uses a shared PhysicsShapePairTracker and committed transition queues. Native compound fixtures are deduplicated by RID and remote/local global slot indices; object/shape entry and departure ordering is preserved. Scene Area scans include server colliders with nullable payloads and explicit server Area monitorability. Callback failures continue later queued events; monitor policy changes inside in/out callbacks reject. ShapePairEventTests checks steady and active entry/exit paths without warmed managed allocation. [Physics monitoring decisions](../decisions/physics-monitoring.md) now own active ADRs 0055/0058; stable legacy anchors route there.
