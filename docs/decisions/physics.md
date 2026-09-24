# Electron2D physics decisions

Last updated: 2026-09-25

This bounded document owns executable two-dimensional bodies, shapes and areas. [The decision index](index.md) routes other domains.

<a id="adr-0054"></a>
## ADR 0054: Box2D-backed scene bodies and collision shapes

Last updated: 2026-09-24

- Status: Accepted
- Scope: First typed scene-body and shape vertical slice
- Depends on: [0012](product.md#adr-0012), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014)

### Context

The fixed physics frame, scene-node hierarchy, typed resources and retained renderer exist, but the physics domain was absent. ADR 0012 selected vendored managed Box2D.NET; this is the first executable use. A public physics server, direct-space query API, area monitoring, joints, kinematic sweeps and all shape families require separate decisions or slices. Adding backend source without body behavior was rejected by ADR 0012.

### Decision

- Vendor all 233 C# source files of `ikpil/Box2D.NET` tag `3.1.654` at commit `5efc96def866edbb4e5a9368d84de5bf8c2dcaca` under `src/Vendor/Box2D.NET`, retaining the MIT license and [patch record](../../src/Vendor/Box2D.NET/VENDOR.md). Compile them into the single `Electron2D.dll`; make namespace-level backend declarations internal and expose no backend type through public/protected Electron2D signatures. Nullable and malformed upstream XML-comment compiler diagnostics are scoped to vendored files.
- Map the reference's `Shape2D`, `CircleShape2D`, `RectangleShape2D`, `CollisionShape2D`, `CollisionObject2D`, `PhysicsBody2D`, `RigidBody2D` and `StaticBody2D` to `Shape`, `CircleShape`, `RectangleShape`, `CollisionShape`, `CollisionObject`, `PhysicsBody`, `RigidBody` and `StaticBody`. Preserve `RigidBody : PhysicsBody : CollisionObject : Entity` and the parallel `StaticBody` branch; `CollisionShape : Entity` is a direct collision-object child that borrows its shape resource. ADR 0055 extends that child role to Area. These names remove only the redundant dimensional suffix, not inherited responsibilities.
- A `SceneTree` lazily owns one internal Box2D world. Direct body entry creates a backend body; direct collision-shape children add circle/rectangle fixtures. Shape changes, disables and collision-layer/mask edits rebuild fixtures before the next step. Tree exit and disposal release bodies, fixtures and world, while borrowed Shape resources remain caller-owned. Failed geometry validation rejects before replacing existing fixtures.
- One scene unit is 0.01 Box2D meters. Default downward gravity is 980 scene units per second squared. Scene fixed physics callbacks run before a four-substep world step; body transforms and velocities synchronize back before timers, tweens and the physics-interpolation end snapshot. Geometry accepts translation and rotation with unit global scale and zero skew; unsupported scaled/skewed active physics transforms fail explicitly. This is an initial profile, not a claim that every inherited or own physics member is complete.
- The initial public body profile includes circle/rectangle dimensions and bounds, managed shape copying, collision-shape assignment and enablement, 32 collision-layer/mask bits, dynamic/static contact response, mass, gravity scale, linear/angular velocity and damping, sleep, freeze, rotation lock, central force and impulse. Other applicable members retain operation-specific Partial, Unimplemented or Blocked coverage rows.
- Engine-owned warmed resting-contact, active-contact and moving-body fixed steps allocate zero managed bytes in the checked Linux/.NET 8 profile. The pinned Box2D.NET port needed per-world reuse of its step context and graph-color block array plus zero-overflow-contact fast exits; the applicable current-upstream buffer reuse is proposed in [ikpil/Box2D.NET#101](https://github.com/ikpil/Box2D.NET/pull/101). No allocation claim is made for unmeasured native/platform paths or user callbacks.

### Consequences

Games can attach a `RigidBody` and `StaticBody` with `CollisionShape` children, advance the existing fixed physics frame, observe gravity and contact response, and apply impulses without backend types or a second managed assembly. Resource copies, scene packing, lifecycle transitions and invalid-input rollback have executable checks. ADR 0055 adds areas separately. Other target platforms, cross-platform packaging, broad geometry/query types, scene world sharing across viewports, and owner visual acceptance remain unverified.

### Rejected alternatives

- Ship Box2D.NET as a managed package or expose its types publicly: ADRs 0004 and 0012 require one Electron2D-owned managed surface.
- Add public PhysicsServer2D or RID placeholders around the first scene bodies: their resource-identity and direct-space contracts need a complete separate vertical slice.
- Write a custom rigid-body solver: ADR 0012 selected the managed Box2D.NET backend.

<a id="adr-0055"></a>
## ADR 0055: Directional scene area monitoring

Last updated: 2026-09-24

- Status: Accepted
- Scope: Area overlap snapshots and object-level events
- Depends on: [0054](#adr-0054), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014)

### Context

The first physics slice owns body fixtures, but interactive game triggers need nonresponding area shapes and reliable body/area overlap reports. The reference's area mask tests the other object's layer without requiring the other's mask to include the area layer. The pinned backend's sensor-event and overlap-query filters require reciprocal mask agreement, which would omit valid area detections, including a body with mask zero.

### Decision

- Map the reference `Area2D` to `Area : CollisionObject : Entity`. A direct `CollisionShape` child belongs to either an `Area` or a `PhysicsBody` and borrows its Shape resource. The area owns a stationary backend body with sensor fixtures. It has no collision response.
- After each nonzero SceneTree fixed step and body synchronization, scan area-to-body and area-to-area shape pairs using the backend's broad bounding boxes and exact shape-distance proxy. An area's mask checks the other's layer; an area's `Monitoring` controls its own scan, and another area's `Monitorable` controls whether it can be a reported area. The other object's mask does not suppress detection. An area's own `Monitorable` does not stop its own monitoring.
- Keep one object-level overlap snapshot per area. Update snapshots once per step, deduplicate multiple touching shape pairs, then deliver entered/exited callbacks after backend stepping so handlers can mutate the scene. Removing an object clears other areas' snapshots and delivers exits without another step. Zero elapsed time retains the previous snapshot. Callback failures are collected without replaying committed transitions.
- Use pairwise scans for this first area profile, with no managed allocations in warmed unchanged scans on the checked Linux/.NET 8 path. Upgrade to a spatial candidate index when large-scene measurement warrants it. The backend source is unchanged. Shape-index events require typed RID/shape-owner identity; gravity/damping priority reduction is implemented by ADR 0056, while audio-bus routing has a separate integration trigger in coverage.

### Consequences

Games can use typed `AreaEntered`, `AreaExited`, `BodyEntered` and `BodyExited` events and query the last fixed-step overlap snapshot for bodies and areas. A body with collision mask zero is still detectable through its layer. Scene packing preserves the area role and monitoring flags. Other platforms, native allocation, broad-world performance and owner game acceptance remain unverified.

<a id="adr-0056"></a>
## ADR 0056: Area field priority and body damping

Last updated: 2026-09-24

- Status: Accepted
- Scope: Executable area gravity/damping, typed world defaults and body field response
- Depends on: [0055](#adr-0055), [0019](core-data-io.md#adr-0019), [0014](resources.md#adr-0014)

### Context

Area monitoring supplies shape geometry and directional filtering but initially did not affect simulated bodies. The pinned 2D reference resolves gravity, linear damping and angular damping independently in descending area-priority order, then adds any unstopped world default and finally combines or replaces body damping. It applies damping to velocity before force integration. The managed backend offers one world gravity and a different built-in damping equation, so simply exposing field properties would not execute the accepted behavior.

### Decision

- Add typed `ProjectSettings` keys for the 2D default gravity strength/vector and linear/angular damping, with pinned defaults 980, (0, 1), 0.1 and 1. A SceneTree physics world samples active overrides when its first body or area attaches; the existing world retains that snapshot.
- Give `Area` the five numeric `SpaceOverride` modes and finite signed gravity/damping fields, including transformed point gravity, constant-strength or inverse-square falloff, shared direction/point storage and integer priority. Field participation is independent of `Monitoring` and `Monitorable`; an area's mask still tests the body's layer. Resolve each channel in descending priority before the backend world step, using current shape overlap rather than the preceding event snapshot.
- Preserve the backend's sampled world gravity, then apply each current dynamic body's difference from it as a mass-scaled force. Apply the pinned `max(0, 1 - delta * totalDamp)` linear/angular velocity factors before solver stepping and keep backend damping at zero. `RigidBody.DampMode` selects Combine or Replace separately for each channel. `PhysicsBody.GetGravity()` reports the last resolved vector after body gravity scaling on RigidBody; detached and StaticBody queries return zero. CharacterBody must inherit this field query when its kinematic slice is implemented.
- Validate all public numeric and enum inputs before mutation and reject a nonfinite resolved field or motion before changing body velocity. A changed resolved field wakes a sleeping body. Reuse area-order scratch storage and existing shape-distance scans; the checked warmed moving and sleeping-body field paths allocate zero managed bytes on Linux/.NET 8. No vendored source is changed.

### Consequences

Overlapping areas can create zero-gravity zones, directional and point attraction, and independent linear/angular damping with priority and stopping modes. Packed scenes retain field and body-mode state. Current world defaults are sampled at creation; native allocator counts, other platforms, large-scene cost and owner visual acceptance remain unverified. Solver/contact behavior remains subject to the backend rather than a claim of complete physics-server parity.

### Rejected alternatives

- Add only field properties without simulation effects: these would be inert compatibility stubs.
- Change the shared world gravity or use its built-in damping for each area: those backend controls cannot represent simultaneous per-body fields or the pinned damping equation.

<a id="adr-0057"></a>
## ADR 0057: Positioned and persistent rigid-body forces

Last updated: 2026-09-24

- Status: Accepted
- Scope: RigidBody force, impulse, torque and axis-velocity vertical slice
- Depends on: [0054](#adr-0054), [0056](#adr-0056), [0008](scene.md#adr-0008)

### Context

The first body profile exposed central one-step force and impulse, but game code lacked positioned actions, torque, persistent forces and axis velocity. The pinned reference treats a position argument as an unrotated world-axis offset from the body origin, stores a positioned constant force as a force vector plus a moment computed at addition, and replaces only the velocity component parallel to the chosen axis. The internal backend exposes all required operations but uses meters rather than scene units.

### Decision

- Expose RigidBody `ApplyForce`, `ApplyImpulse`, `ApplyTorque`, `ApplyTorqueImpulse`, `AddConstantCentralForce`, `AddConstantForce`, `AddConstantTorque`, `ConstantForce`, `ConstantTorque` and `SetAxisVelocity`. An optional zero impulse on `ApplyCentralImpulse` matches the pinned default. Persistent totals are typed scene state, remain across fixed steps and scene packing, and stop when cleared. A frozen body stores them without applying them until unfrozen.
- Use 0.01 meters per scene unit for force and linear impulse, 0.0001 squared meters for torque and angular impulse, and convert positioned arguments from the body's current backend origin in world axes. `AddConstantForce` adds its moment about the current center of mass once at the call; a later rotation does not recompute that stored moment. Validate finite inputs, accumulated totals and generated positions/moments before applying an action.
- Synchronize pending body pose and child fixtures before public force/impulse/torque actions. This makes a newly attached or edited shape's mass and center of mass available immediately rather than waiting for the next fixed step. Apply persistent force/torque after area field reduction and before solver stepping. Wake bodies on explicit actions as in the pinned API; persistent force application itself does not repeatedly wake a resting body.
- Synchronize a simulated body's position and rotation back to its scene node in one unit-scale global transform assignment. Reconstructing rotation from the previous decomposed scale accumulated float error and eventually rejected an otherwise valid rotating body. Warmed active force/torque frames allocate zero managed bytes in the checked Linux/.NET 8 profile; native allocation, other platforms and game acceptance remain unverified.

### Consequences

Games can push at an offset, add a one-time impact, spin a body, maintain propulsion and set a jump-axis velocity without public backend types. Shifted-center, unit-conversion, freeze/reentry, invalid-input rollback, packing and repeated-rotation paths have executable checks. Contact monitoring, custom integration, continuous collision, custom mass distribution and physics server/RID methods remain separate coverage work.

<a id="adr-0058"></a>
## ADR 0058: Rigid-body contact and sleep snapshots

Last updated: 2026-09-25

- Status: Accepted
- Scope: Object-level RigidBody contact reports, point counts and solver sleep events
- Depends on: [0054](#adr-0054), [0055](#adr-0055), [0014](resources.md#adr-0014)

### Context

Rigid bodies respond to contact but had no typed way to observe the other bodies, cap reported contact points, or react when the solver changes sleep state. The pinned backend supplies transient begin/end events whose shape IDs can be invalid after fixture destruction. It also exposes the currently touching pairs and their manifolds after each unlocked step. The accepted reference differentiates a cached contact-point count from opt-in object-level monitoring, and does not emit its sleep signal for a direct `Sleeping` property assignment.

### Decision

- Add `RigidBody.ContactMonitor`, nonnegative `MaxContactsReported`, `GetContactCount()`, typed `GetCollidingBodies()`, object-level `BodyEntered`/`BodyExited` and `SleepingStateChanged`. Zero reported contacts is the default. Count points up to the configured cap after each step, while the object snapshot and entry/exit events also require monitoring. Disabling monitoring clears its object snapshot without synthesizing exits; disabling from inside its contact callback is rejected before mutation.
- Read current backend touching pairs and manifold point counts through a reusable per-body buffer after solver/body synchronization. Resolve other bodies through current backend body IDs, deduplicate shape pairs by scene body, then commit snapshots before callbacks. Emit sleep transitions before contact transitions; explicit `Sleeping` assignments update stored state without a solver event. Scene-body removal clears peers' snapshots and delivers exits without another step. Callback exceptions are aggregated while later queued contact and area events continue.
- Use current touching-pair data rather than transient backend end-event shape IDs, which may already be destroyed after shape, filter or body edits. Reuse buffers and sets so warmed resting, active-contact and no-contact fixed frames allocate zero managed bytes on the checked Linux/.NET 8 path. The backend source is unchanged.
- Shape-index contact signals remain dependent on public typed RID/shape-owner identity. Body-level results/signals remain Partial until tile-map virtual collision bodies are integrated; the exact reference priority for selecting manifold points when a cap is lower than simultaneous contacts and its upper cap still require a dedicated audit.

### Consequences

Games can respond to collision entry/exit and solver sleep, query current bodies and bounded contact points, and safely remove a collider from a callback. Attached owner-thread access, scene packing, filter and shape edits, callback failures and warmed allocations have executable checks. Native allocator counts, other platforms, large-world performance and owner acceptance remain unverified.

<a id="adr-0059"></a>
## ADR 0059: Capsule collision resource and fixture geometry

Last updated: 2026-09-25

- Status: Accepted
- Scope: Concrete capsule Shape resource for existing body and area fixtures
- Depends on: [0054](#adr-0054), [0055](#adr-0055), [0013](resources.md#adr-0013)

### Context

The first scene-body profile supports circle and rectangle fixtures. A vertical capsule is a common game-body shape and the next geometry prerequisite for kinematic movement. The pinned resource links radius, total height and middle height; the vendored backend accepts capsules but returns a null shape ID when the segment between cap centers does not exceed its linear slop. Passing that ID to existing fixture ownership would invalidate collision scans.

### Decision

- Add `CapsuleShape : Shape` with default radius 10, full height 30 and derived middle height 10. Radius growth raises height to at least the diameter; height reduction lowers radius to at most half height. Setting middle height derives the full height and emits a resource change even for an equal value. Equal radius/height writes do not emit. Accept finite nonnegative dimensions, including circle and zero-radius segment limits; reject overflow before changing coupled state.
- Keep exact resource dimensions, local bounds and independent Resource copying. Direct CollisionShape children borrow a capsule and rebuild body or Area fixtures on edits using the existing geometry revision contract. Rotate the cap-center segment with the child's local rotation; convert scene units to backend meters by 0.01.
- Represent zero and sub-slop middle segments as circles in the backend, while keeping exact public values and bounds. At the current backend scale the threshold is 0.005 m, or 0.5 scene units. This is an accepted solver-tolerance adaptation for collision response; above the threshold use an actual capsule fixture. Never retain the backend's null shape ID. Reuse the existing no-allocation fixture and area paths.

### Consequences

Rigid and static bodies gain capsule contact response, and areas gain capsule sensors and field geometry. The public type adds no backend handle and uses the existing borrowed-resource lifetime. Managed tests cover linked dimensions, degenerate values, rotation, live edits, packing and warmed allocation. Direct-space sweeps, character movement, native allocator accounting, other platforms and owner acceptance remain separate work.

<a id="adr-0060"></a>
## ADR 0060: Fixed-step kinematic platform motion

Last updated: 2026-09-25

- Status: Accepted
- Scope: AnimatableBody scene role and synchronized kinematic movement
- Depends on: [0054](#adr-0054), [0059](#adr-0059), [0008](scene.md#adr-0008)

### Context

Static bodies constrain a rigid body but teleport when their scene transform changes, so a moving platform has no contact velocity. The pinned AnimatableBody2D inherits StaticBody2D, uses a kinematic backend body, estimates linear/angular velocity from manual movement, and defaults to delaying the scene transform until the next physics frame. Box2D.NET offers kinematic bodies and a target-transform operation that derives both velocities for a fixed step. Direct-space sweeps and CharacterBody movement still require separate result and recovery contracts.

### Decision

- Allow `StaticBody` inheritance and add `AnimatableBody : StaticBody` with stored `SyncToPhysics=true`. It borrows existing collision shapes and the inherited surface material. Its backend body is kinematic with zero mass: contacts and external forces cannot displace it, but movement affects dynamic bodies through contact velocity.
- After node physics callbacks and before the world step, send the latest finite unit-scale, zero-skew target to the backend for that step. With synchronization enabled, a caller's local transform edit records the global target, restores the last solved scene transform, and presents the backend result after the step. Zero delta does not consume the target. With synchronization disabled, the scene transform changes immediately while the backend reaches it during the next step. Disabling synchronization with a pending target presents that target immediately. Preserve the exact derived node type and flag in PackedScene.
- Keep the existing body fixture and material synchronization, but avoid teleporting this kinematic backend when its scene transform changes. A shared internal transform hook leaves rigid/static behavior intact. Clear pending movement on tree exit; reentry captures the current scene pose. Invalid scaled/skewed targets reject after restoring the prior scene pose. A throwing user transform callback does not erase a valid pending target. Warm active and idle kinematic paths reuse existing backend storage and allocate zero managed bytes in the checked Linux/.NET 8 profile.

### Consequences

Games can animate moving platforms and doors that push or carry dynamic bodies, using typed scene transforms. CharacterBody platform following and kinematic sweeps remain future slices. Native allocation, other platforms and owner visual acceptance remain unverified.
