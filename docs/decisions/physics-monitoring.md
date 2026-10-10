# Electron2D physics monitoring decisions

Last updated: 2026-10-08

This bounded document owns scene/server Area overlap and RigidBody contact monitoring. [The decision index](index.md) routes other physics decisions. The stable legacy anchors in physics.md route here.

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
- Use pairwise scans for this first area profile, with no managed allocations in warmed unchanged scans on the checked Linux/.NET 8 path. Upgrade to a spatial candidate index when large-scene measurement warrants it. The current solver impulse accumulators and reporting epochs are recorded in ADR 0070. Shape-index events now use typed RID/global logical shape indices; gravity/damping priority reduction is implemented by ADR 0056, while audio-bus routing has a separate integration trigger in coverage.

### Consequences

Games can use typed `AreaEntered`, `AreaExited`, `BodyEntered` and `BodyExited` events and query the last fixed-step overlap snapshot for bodies and areas. A body with collision mask zero is still detectable through its layer. Scene packing preserves the area role and monitoring flags. Other platforms, native allocation, broad-world performance and owner game acceptance remain unverified.

### Shape-pair monitoring extension

Track collider RID, sampled instance ID and weak object association, Area/body kind and both global logical shape indices. Scan all eligible scene and server fixtures with the existing exact overlap kernel; deduplicate compound fixtures by logical pair. First pair entry queues object entry before shape entry. Ordinary last-pair exit queues object exit before that pair exit; scene-tree departure queues object exit then retained shape exits. Unassigned server-only payloads carry RID and null object; explicitly assigned live nodes participate in typed scene snapshots. Server Areas default non-monitorable; AreaSetMonitorable opts them in.

Commit pair/object snapshots before callbacks and never replay transitions after user failures. Continue later queued events after a handler throws. Monitoring/Monitorable changes during an in/out callback reject; equal assignments remain harmless. Freeing a server collider emits retained pair exits and finishes registry cleanup even if a handler fails. Reuse pair sets/counts/event lists; indexed native shape scans avoid boxed IReadOnlyList enumerators. Steady and active entry/exit paths are checked after warmup. Body-shape tile-map payloads remain Partial until typed tile-body integration; Area/Area shape payloads execute for the current complete Area branch.


<a id="adr-0058"></a>
## ADR 0058: Rigid-body contact and sleep snapshots

Last updated: 2026-10-07

- Status: Accepted
- Scope: Object-level RigidBody contact reports, point counts and solver sleep events
- Depends on: [0054](physics.md#adr-0054), [0055](#adr-0055), [0014](resources.md#adr-0014)

### Context

Rigid bodies respond to contact but had no typed way to observe the other bodies, cap reported contact points, or react when the solver changes sleep state. The pinned backend supplies transient begin/end events whose shape IDs can be invalid after fixture destruction. It also exposes the currently touching pairs and their manifolds after each unlocked step. The accepted reference differentiates a cached contact-point count from opt-in object-level monitoring, and does not emit its sleep signal for a direct `Sleeping` property assignment.

### Decision

- Add `RigidBody.ContactMonitor`, `MaxContactsReported` in zero through 4095, `GetContactCount()`, typed `GetCollidingBodies()`, object-level `BodyEntered`/`BodyExited` and `SleepingStateChanged`. Zero reported contacts is the default. Count points up to the configured cap after each step, while the object snapshot and entry/exit events also require monitoring. Disabling monitoring clears its object snapshot without synthesizing exits; disabling from inside its contact callback is rejected before mutation.
- Read touching-pair observations from the completed outer frame after solver/body synchronization; subdivided frames also retain contacts that ended in an earlier internal interval under ADR 0070. Fill point slots in backend encounter order, then replace the first shallowest slot only when a later point is strictly deeper; ties keep existing slots. A limit assignment clears the old point count. Share that selected direct-state snapshot with object/shape monitoring, reading manifolds by reference and capturing assigned object identity through weak fixture tags. Large worlds collect independent receiver snapshots on retained workers; join every range before committing the shared event queue in body order. Resolve other bodies through current backend body IDs, deduplicate object events by assigned instance and retain physical RID/shape pairs, then commit snapshots before callbacks. Emit sleep transitions before contact transitions; explicit `Sleeping` assignments update stored state without a solver event. Scene-body removal clears peers' snapshots and delivers exits without another step. Callback exceptions are aggregated while later queued contact and area events continue.
- Use current touching-pair data rather than transient backend end-event shape IDs, which may already be destroyed after shape, filter or body edits. Reuse buffers and sets so warmed resting, active-contact and no-contact fixed frames allocate zero managed bytes on the checked Linux/.NET 8 path. The current solver impulse accumulators and reporting epochs are recorded in ADR 0070.
- Shape-index contact signals now use typed RID/global logical owner indices through deduplicated contact-pair snapshots. Square-atlas TileMapLayer virtual body owners now execute through the shared association contract; depth selection and the exclusive 4096 upper bound are verified by PhysicsContactImpulseTests under ADR 0070.

### Consequences

Games can respond to collision entry/exit and solver sleep, query current bodies and bounded contact points, and safely remove a collider from a callback. Attached owner-thread access, scene packing, filter and shape edits, callback failures and warmed allocations have executable checks. Native allocator counts, other platforms, large-world performance and owner acceptance remain unverified.

### Logical body-shape transitions

Retain only pairs contributing to the configured contact cap and monitor snapshot. Capture assigned object identity from native shape tags and emit BodyShapeEntered/Exited with RID, Node and remote/local global indices. Deduplicate multiple manifold points and compound fixtures. Object entry precedes its first pair entry; object exit precedes its last retained pair exit. Removal is handled from committed values rather than destroyed native shape IDs. ContactMonitor disabling from an object or shape callback rejects. Square-atlas tile virtual-body payloads now execute; capped-contact priority uses the shared deepest-point selection.

<a id="adr-0077"></a>
## ADR 0077: Server Area receiver callbacks and directional masks

Last updated: 2026-09-26

- Status: Accepted
- Scope: Typed body/Area logical-pair observers for scene/server Area RIDs
- Depends on: [0055](#adr-0055), [0058](#adr-0058), [0063](physics.md#adr-0063), [0071](physics.md#adr-0071), [0014](resources.md#adr-0014)

### Context

Scene Areas already observe scene/server shapes through the exact overlap kernel, but a server-created Area cannot receive callbacks. CollisionMask was Blocked without an overlap consumer. The pinned receiver payload is status, other RID, object instance ID, other shape index and local shape index. Registering either callback clears both histories; receiver space changes clear them without synthetic self exits.

### Decision

- Expose nested PhysicsServer.AreaBodyStatus Added=0 and Removed=1 plus AreaSetMonitorCallback and AreaSetAreaMonitorCallback with typed Action<status,RID,ulong,int,int>. InstanceID is the sampled assigned object ID, or zero when unassigned; store RID/logical pair identity rather than native handles. Monitor callbacks are optional and retained until replacement, clear or receiver free. Re-registering even the same delegate clears both pair histories; current overlaps replay entry at the next nonzero scan.
- Integrate server receiver scans after ordinary scene overlap snapshots, using existing exact shape-pair geometry and fixture tags. Directional receiver mask tests the other layer; no reciprocal mask is required. The Area lane accepts only monitorable other Areas, default false for explicit server Areas. Deduplicate compound fixtures by both global logical indices. Add AreaSetCollisionMask and typed layer/mask/global-pose getters; project filter/pose/monitorable operations across all live scene/server Area RIDs.
- Keep scene-owned snapshots/events mandatory; external server callbacks on a scene Area are independent observers, rather than replacing its private node-owned receiver. They can observe even when scene Monitoring is disabled. This preserves typed host ownership under the same adaptation as body state observers. Source callback histories and node snapshots remain separate.
- Commit all receiver pair snapshots before dispatch; emit only logical shape changes with no object duplicate to raw callbacks. Continue later queued callbacks after failure and never replay committed transitions. Configuration writes from the receiver's own raw or scene in/out callback reject; reads remain available. Epoch changes suppress stale queued deliveries. Removing another collider immediately queues retained pair exits; in-callback removal drains safely and suppresses stale later entries. Free completes registry cleanup even when departure callbacks throw.
- Receiver detach/space change/free clears its own history silently, while peer receivers get ordinary departure changes. Callback registrations and masks survive reentry; world disposal clears event storage and attachment history. Enforce live Area kind, owner thread and non-stepping access. Use cached pair/change/event storage and indexed fixture loops; complexity follows the existing pairwise Area kernel. No vendor source changes.

### Consequences and verification

PhysicsAreaMonitorTests checks server/scene payloads and stable IDs, two local logical pairs, bit 32, directional masks, monitorable gating, callback registration/clear/reset, independent scene snapshots, detach/reentry/other free, failure continuation/no replay, recursive configuration rejection, receiver getters and in-callback removal. Sixty-four warmed active entry/exit cycles allocate zero managed bytes on Linux/.NET 10. Native allocation, broad-world performance, other platforms and owner visual acceptance remain unverified. Server Area fields now execute under [ADR 0056](physics-fields.md#adr-0056); instance/canvas attachment and remaining shape operations retain separate coverage gaps.


## Explicit object associations

ADR 0063 now supplies weak typed object bindings for raw and scene body/Area RIDs.
Physical shape-pair identity includes the sampled associated instance ID, so a
rebind reports the former and replacement identities independently. Object-level
counts use assigned instance plus body/Area role across all contributing RIDs.
Snapshots resolve live objects weakly; typed scene arrays/signals retain their
Entity/Area/Node roles. Body/Area server callbacks keep the captured numeric ID
when the target leaves the tree or is disposed.

Scene Node exit/reentry updates visible contact/overlap events immediately while
raw bodies remain attached. The shared tracker retains physical pairs separately
from node visibility. Server monitor callbacks react only to physical pair or
association changes, so tree membership does not fabricate raw pair exits. Event
queues retain their observed node briefly through delivery; persistent snapshots
and bindings are weak. Physical body removal forgets only that RID's contributions.
