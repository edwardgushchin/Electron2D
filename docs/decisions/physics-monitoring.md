# Electron2D physics monitoring decisions

Last updated: 2026-09-26

This bounded document owns scene Area overlap and RigidBody contact monitoring. [The decision index](index.md) routes other physics decisions. The stable legacy anchors in physics.md route here.

<a id="adr-0055"></a>
## ADR 0055: Directional scene area monitoring

Last updated: 2026-09-26

- Status: Accepted
- Scope: Area overlap snapshots and object-level events
- Depends on: [0054](physics.md#adr-0054), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014)

### Context

The first physics slice owns body fixtures, but interactive game triggers need nonresponding area shapes and reliable body/area overlap reports. The reference's area mask tests the other object's layer without requiring the other's mask to include the area layer. The pinned backend's sensor-event and overlap-query filters require reciprocal mask agreement, which would omit valid area detections, including a body with mask zero.

### Decision

- Map the reference `Area2D` to `Area : CollisionObject : Entity`. A direct `CollisionShape` child belongs to either an `Area` or a `PhysicsBody` and borrows its Shape resource. The area owns a stationary backend body with sensor fixtures. It has no collision response.
- After each nonzero SceneTree fixed step and body synchronization, scan area-to-body and area-to-area shape pairs using the backend's broad bounding boxes and exact shape-distance proxy. An area's mask checks the other's layer; an area's `Monitoring` controls its own scan, and another area's `Monitorable` controls whether it can be a reported area. The other object's mask does not suppress detection. An area's own `Monitorable` does not stop its own monitoring.
- Keep one logical shape-pair snapshot and deduplicated object snapshot per area. Update snapshots once per step, deduplicate multiple touching shape pairs, then deliver entered/exited callbacks after backend stepping so handlers can mutate the scene. Removing an object clears other areas' snapshots and delivers exits without another step. Zero elapsed time retains the previous snapshot. Callback failures are collected without replaying committed transitions.
- Use pairwise scans for this first area profile, with no managed allocations in warmed unchanged scans on the checked Linux/.NET 8 path. Upgrade to a spatial candidate index when large-scene measurement warrants it. The backend source is unchanged. Shape-index events now use typed RID/global logical shape indices; gravity/damping priority reduction is implemented by ADR 0056, while audio-bus routing has a separate integration trigger in coverage.

### Consequences

Games can use typed `AreaEntered`, `AreaExited`, `BodyEntered` and `BodyExited` events and query the last fixed-step overlap snapshot for bodies and areas. A body with collision mask zero is still detectable through its layer. Scene packing preserves the area role and monitoring flags. Other platforms, native allocation, broad-world performance and owner game acceptance remain unverified.

### Shape-pair monitoring extension

Track collider RID, nullable scene object, Area/body kind and both global logical shape indices. Scan all eligible scene and server fixtures with the existing exact overlap kernel; deduplicate compound fixtures by logical pair. First pair entry queues object entry before shape entry. Ordinary last-pair exit queues object exit before that pair exit; scene-tree departure queues object exit then retained shape exits. Server-only payloads carry RID and null scene object and do not fabricate object-array results. Server Areas default non-monitorable; AreaSetMonitorable opts them in.

Commit pair/object snapshots before callbacks and never replay transitions after user failures. Continue later queued events after a handler throws. Monitoring/Monitorable changes during an in/out callback reject; equal assignments remain harmless. Freeing a server collider emits retained pair exits and finishes registry cleanup even if a handler fails. Reuse pair sets/counts/event lists; indexed native shape scans avoid boxed IReadOnlyList enumerators. Steady and active entry/exit paths are checked after warmup. Body-shape tile-map payloads remain Partial until typed tile-body integration; Area/Area shape payloads execute for the current complete Area branch.


<a id="adr-0058"></a>
## ADR 0058: Rigid-body contact and sleep snapshots

Last updated: 2026-09-25

- Status: Accepted
- Scope: Object-level RigidBody contact reports, point counts and solver sleep events
- Depends on: [0054](physics.md#adr-0054), [0055](#adr-0055), [0014](resources.md#adr-0014)

### Context

Rigid bodies respond to contact but had no typed way to observe the other bodies, cap reported contact points, or react when the solver changes sleep state. The pinned backend supplies transient begin/end events whose shape IDs can be invalid after fixture destruction. It also exposes the currently touching pairs and their manifolds after each unlocked step. The accepted reference differentiates a cached contact-point count from opt-in object-level monitoring, and does not emit its sleep signal for a direct `Sleeping` property assignment.

### Decision

- Add `RigidBody.ContactMonitor`, nonnegative `MaxContactsReported`, `GetContactCount()`, typed `GetCollidingBodies()`, object-level `BodyEntered`/`BodyExited` and `SleepingStateChanged`. Zero reported contacts is the default. Count points up to the configured cap after each step, while the object snapshot and entry/exit events also require monitoring. Disabling monitoring clears its object snapshot without synthesizing exits; disabling from inside its contact callback is rejected before mutation.
- Read current backend touching pairs and manifold point counts through a reusable per-body buffer after solver/body synchronization. Resolve other bodies through current backend body IDs, deduplicate shape pairs by scene body, then commit snapshots before callbacks. Emit sleep transitions before contact transitions; explicit `Sleeping` assignments update stored state without a solver event. Scene-body removal clears peers' snapshots and delivers exits without another step. Callback exceptions are aggregated while later queued contact and area events continue.
- Use current touching-pair data rather than transient backend end-event shape IDs, which may already be destroyed after shape, filter or body edits. Reuse buffers and sets so warmed resting, active-contact and no-contact fixed frames allocate zero managed bytes on the checked Linux/.NET 8 path. The backend source is unchanged.
- Shape-index contact signals now use typed RID/global logical owner indices through deduplicated contact-pair snapshots. Body-level results/signals remain Partial until tile-map virtual collision bodies are integrated; the exact reference priority for selecting manifold points when a cap is lower than simultaneous contacts and its upper cap still require a dedicated audit.

### Consequences

Games can respond to collision entry/exit and solver sleep, query current bodies and bounded contact points, and safely remove a collider from a callback. Attached owner-thread access, scene packing, filter and shape edits, callback failures and warmed allocations have executable checks. Native allocator counts, other platforms, large-world performance and owner acceptance remain unverified.

### Logical body-shape transitions

Retain only pairs contributing to the configured contact cap and monitor snapshot. Resolve scene body identity from native shape tags and emit BodyShapeEntered/Exited with RID, Node and remote/local global indices. Deduplicate multiple manifold points and compound fixtures. Object entry precedes its first pair entry; object exit precedes its last retained pair exit. Removal is handled from committed values rather than destroyed native shape IDs. ContactMonitor disabling from an object or shape callback rejects. Tile-map virtual body payloads and exact capped-contact priority remain Partial with the existing triggers.
