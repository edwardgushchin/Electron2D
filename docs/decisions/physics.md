# Electron2D physics decisions

Last updated: 2026-09-26

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
- Add public PhysicsServer or RID placeholders around the first scene bodies: their resource-identity and direct-space contracts need a complete separate vertical slice.
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

Last updated: 2026-09-26

- Status: Accepted
- Scope: Executable area gravity/damping, typed world defaults and body field response
- Depends on: [0055](#adr-0055), [0019](core-data-io.md#adr-0019), [0014](resources.md#adr-0014)

### Context

Area monitoring supplies shape geometry and directional filtering but initially did not affect simulated bodies. The pinned 2D reference resolves gravity, linear damping and angular damping independently in descending area-priority order, then adds any unstopped world default and finally combines or replaces body damping. It applies damping to velocity before force integration. The managed backend offers one world gravity and a different built-in damping equation, so simply exposing field properties would not execute the accepted behavior.

### Decision

- Add typed `ProjectSettings` keys for the 2D default gravity strength/vector and linear/angular damping, with pinned defaults 980, (0, 1), 0.1 and 1. A SceneTree physics world samples active overrides when its first body or area attaches; the existing world retains that snapshot.
- Give `Area` the five numeric `SpaceOverride` modes and finite signed gravity/damping fields, including transformed point gravity, constant-strength or inverse-square falloff, shared direction/point storage and integer priority. Field participation is independent of `Monitoring` and `Monitorable`; an area's mask still tests the body's layer. Resolve each channel in descending priority before the backend world step, using current shape overlap rather than the preceding event snapshot.
- Preserve the backend's sampled world gravity, then apply each current dynamic body's difference from it as a mass-scaled force. Apply the pinned `max(0, 1 - delta * totalDamp)` linear/angular velocity factors before solver stepping and keep backend damping at zero. `RigidBody.DampMode` selects Combine or Replace separately for each channel. `PhysicsBody.GetGravity()` reports the last resolved vector after body gravity scaling on RigidBody; CharacterBody reports the same selected world/Area gravity without automatically applying it to caller-owned Velocity. Detached and StaticBody queries return zero.
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

Last updated: 2026-09-26

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

Rigid and static bodies gain capsule contact response, and areas gain capsule sensors and field geometry. The public type adds no backend handle and uses the existing borrowed-resource lifetime. Managed tests cover linked dimensions, degenerate values, rotation, live edits, packing and warmed allocation. Separation-ray floor behavior, native allocator accounting, other platforms and owner acceptance remain separate work.

<a id="adr-0060"></a>
## ADR 0060: Fixed-step kinematic platform motion

Last updated: 2026-09-26

- Status: Accepted
- Scope: AnimatableBody scene role and synchronized kinematic movement
- Depends on: [0054](#adr-0054), [0059](#adr-0059), [0008](scene.md#adr-0008)

### Context

Static bodies constrain a rigid body but teleport when their scene transform changes, so a moving platform has no contact velocity. The pinned AnimatableBody2D inherits StaticBody2D, uses a kinematic backend body, estimates linear/angular velocity from manual movement, and defaults to delaying the scene transform until the next physics frame. Box2D.NET offers kinematic bodies and a target-transform operation that derives both velocities for a fixed step. The direct sweep and CharacterBody movement contracts are now defined under ADRs 0063 and 0067.

### Decision

- Allow `StaticBody` inheritance and add `AnimatableBody : StaticBody` with stored `SyncToPhysics=true`. It borrows existing collision shapes and the inherited surface material. Its backend body is kinematic with zero mass: contacts and external forces cannot displace it, but movement affects dynamic bodies through contact velocity.
- After node physics callbacks and before the world step, send the latest finite unit-scale, zero-skew target to the backend for that step. With synchronization enabled, a caller's local transform edit records the global target, restores the last solved scene transform, and presents the backend result after the step. Zero delta does not consume the target. With synchronization disabled, the scene transform changes immediately while the backend reaches it during the next step. Disabling synchronization with a pending target presents that target immediately. Preserve the exact derived node type and flag in PackedScene.
- Keep the existing body fixture and material synchronization, but avoid teleporting this kinematic backend when its scene transform changes. A shared internal transform hook leaves rigid/static behavior intact. Clear pending movement on tree exit; reentry captures the current scene pose. Invalid scaled/skewed targets reject after restoring the prior scene pose. A throwing user transform callback does not erase a valid pending target. Warm active and idle kinematic paths reuse existing backend storage and allocate zero managed bytes in the checked Linux/.NET 8 profile.

### Consequences

Games can animate moving platforms and doors that push or carry dynamic bodies, using typed scene transforms. CharacterBody now follows moving platforms and consumes kinematic sweeps under ADR 0067. Native allocation, other platforms and owner visual acceptance remain unverified.

<a id="adr-0061"></a>
## ADR 0061: Two-sided segment fixtures and zero-area body mass

Last updated: 2026-09-26

- Status: Accepted
- Scope: SegmentShape resource and executable static, dynamic and area fixtures
- Depends on: [0054](#adr-0054), [0059](#adr-0059), [0013](resources.md#adr-0013)

### Context

Circle, capsule and rectangle fixtures cannot form a zero-width sloped terrain edge. The pinned SegmentShape2D stores two independent local endpoints, emits only on a changed endpoint, and reports their tight bounds. Box2D.NET provides a two-sided segment fixture, but rejects an endpoint separation at or below its linear slop. A segment has zero area in that backend, so a dynamic body with only segments otherwise receives zero mass and cannot advance under gravity.

Joint2D/PinJoint2D was compared as another gameplay slice. Its public RID needs typed joint/body/world identity, and the current Box2D revolute API has no verified direct mapping for Joint2D positional `bias` or PinJoint2D linear-anchor `softness`; its angular spring controls a different degree of freedom. Typed kinematic sweeps need an owned result with stable shape indices and an initial-overlap recovery rule. These exact member triggers remain in coverage while independent segment collision can execute now.

### Decision

- Map SegmentShape2D to `SegmentShape : Shape` with A=(0,0), B=(0,10), exact tight local bounds, independent Resource copying and finite endpoints. Reject values whose endpoint subtraction would overflow before mutation. An equal endpoint assignment has no change event. Rotate and translate both endpoints through a direct CollisionShape child before creating a two-sided backend fixture.
- Represent zero or sub-slop segments with a zero-radius point fixture at the midpoint, while retaining the exact public endpoints and bounds. Never keep the backend's null segment ID. At the current unit scale, its 0.005 m slop is 0.5 scene units. The same resource can be borrowed by static/dynamic bodies and areas.
- For dynamic bodies whose fixtures all have zero area, retain the requested positive mass. Segment mass is distributed by segment length; the center is length-weighted and rotational inertia follows the thin-rod and parallel-axis formulas. A body without fixtures keeps its requested mass with zero inertia. Reject mass/inertia whose inverse exceeds the finite solver range before backend mutation. Warmed unchanged segment contact/area paths allocate zero managed bytes on the checked Linux/.NET 8 profile.

### Consequences

Games can construct two-sided terrain edges and line sensors and attach segments to moving bodies. Live edits, rotation, short/point limits, resource copying, scene packing and dynamic mass/inertia have executable checks. Compound chain geometry, joint tuning/RID, separation-ray participation, native allocator accounting, other platforms and owner acceptance remain separate work.

<a id="adr-0062"></a>
## ADR 0062: Compound convex collision fixtures

Last updated: 2026-09-25

- Status: Accepted
- Scope: Convex polygon resource, point-cloud hull and multiple fixtures per CollisionShape
- Depends on: [0054](#adr-0054), [0061](#adr-0061), [0013](resources.md#adr-0013)

### Context

Existing circle, capsule, segment and rectangle fixtures cannot reproduce an arbitrary solid convex body. The pinned convex polygon resource stores perimeter points, accepts either winding and computes a hull from point clouds. Box2D.NET limits one polygon fixture to eight vertices, while the public contour has no eight-vertex limit. The previous Shape-to-fixture method returned only one ID, so a shape with more vertices could not be represented without dropping geometry or splitting its public identity.

### Decision

- Map ConvexPolygonShape2D to `ConvexPolygonShape : Shape`. `Points` defaults empty, returns/accepts caller-owned arrays, retains perimeter order and may repeat the first vertex at the end. Assignment emits a resource change even for equal contents; empty input removes all fixtures. A typed `ReadOnlySpan<Vector2>` point-cloud method uses the existing convex-hull operation and stores its closed contour. Validate finite, nondegenerate convex perimeter input, including edge crossings and backend-size limits, before committing a new resource revision.
- Let each Shape append zero, one or several internal fixture IDs to the existing body/area owner list. The four existing concrete shapes still append one. Split a larger convex contour into a nonoverlapping fan of pieces with at most eight vertices each, sharing only seam edges. Precompute/validate all local hulls before resource mutation, then apply each child's position/rotation to those hulls on fixture rebuild. The body or area still borrows one public Shape resource, and object-level events deduplicate backend pieces by owner.
- Keep copies of public points and internal hull pieces independent across Resource duplication. Multiple dynamic fixtures contribute area-derived mass/inertia through the existing body mass policy. Warmed unchanged contact and area scans reuse fixture storage and allocate zero managed bytes on the checked Linux/.NET 8 path. No vendored source is changed.

### Consequences

Games can collide and sense with a solid convex contour containing more than eight vertices; direct clockwise/counterclockwise assignment and point-cloud hull generation are executable. `CollisionPolygon` and concave polygon decomposition can reuse the compound-fixture mechanism in later slices. Native allocator counts, other platforms and owner visual acceptance remain unverified.

<a id="adr-0063"></a>
## ADR 0063: Shared RID identity and world-scoped physics queries

Last updated: 2026-09-26

- Status: Accepted
- Scope: Public identity, ownership and access model for 2D physics server resources and direct queries
- Depends on: [0001](product.md#adr-0001), [0004](product.md#adr-0004), [0008](scene.md#adr-0008), [0054](#adr-0054), [0028](rendering.md#adr-0028)

### Context

Before this slice, the scene ran an internal Box2D world but had no public RID, PhysicsServer, World2D or direct-space state. The applicable reference query parameters exclude RIDs and may select a shape by RID; results identify a collider by both object and RID, plus a shape index. Server-created physics resources need an identity even without scene nodes. A scene-object-only query surface would leave those contracts and later renderer/navigation RID consumers unresolved.

### Decision

- Introduce one public opaque, backend-neutral RID value type shared by server domains. Its default/zero value is empty; identity is session-local and never exposes a Box2D ID. The owning server validates resource kind and liveness separately from the value type's nonzero validity test. Released IDs must not resolve to a later resource after internal slot reuse. Fixture rebuilds do not change the owning collision object's RID.
- Name the public two-dimensional server `PhysicsServer` and its live direct-space view `PhysicsDirectSpaceState`. The shorter names do not add a 3D physics domain; their existing typed methods, nested `BodyMode`, ownership and RID behavior remain unchanged. Update consumers, XML, class pages and coverage mappings together; do not retain duplicate public compatibility types.
- PhysicsServer owns the physics resource registry and real spaces, bodies, areas and shapes. Register the existing SceneTree physics world as a server space; scene membership and teardown retain the lifetime of scene-owned resources. Explicitly created server resources have an executable creation/use/free lifecycle. The scene and server access the same solver state; no parallel scene-only physics world is introduced.
- Expose that scene space through the applicable World2D.Space and World2D.DirectSpaceState roles and CanvasItem world access. These physics members may execute before World2D canvas and navigation-map members, which retain their own exact coverage gaps. A direct-space state is a view of its owning live space, not a second world.
- Keep ordinary gameplay object-oriented: scene nodes and typed ray/shape query objects expose collider references where available. Direct query parameters also retain RID exclusions and shape RID selection; typed C# result values retain collider RID and stable shape-owner index even when no scene CollisionObject exists. Specify each operation's no-hit, ordering, copy and maximum-result behavior in its implementing slice. Do not add Variant, dynamic dictionaries, public Box2D types or backend IDs.
- Build the public server and query layer in connected executable slices: shared RID/space/body/shape lifetime, world access, ray/point queries, direct shape sweeps and scene query nodes, then shape-index events and remaining applicable server methods. RayCast and ShapeCast consume the same direct-space view; other absent declarations retain their own implementation gates. An accepted architecture does not mark any absent declaration Implemented. Attached queries respect scene owner-thread and backend world-lock boundaries.
- The first executing slice registers each CollisionObject RID for its managed lifetime and each SceneTree's existing PhysicsSpace as one server space. Fixture tags carry collider RID and direct shape-owner index through compound shape rebuilds. Server-created spaces, bodies, Areas and the six existing Shape families attach to the same solver; explicit spaces use typed host stepping. World2D exposes Space and DirectSpaceState, and attached CanvasItems share that physics view. Typed ray/point parameters copy RID exclusions and results carry RID, optional scene collider, instance ID and shape index. A direct query prepares pending scene geometry before searching and rejects off-owner or in-step access.
- Scan the current fixture lists for ray, point and shape queries so a collider with collision mask zero remains eligible by its layer. The backend world query's reciprocal filter would incorrectly remove it. Ray results choose nearest fraction with RID/index tie order; point and shape results deduplicate by owner and apply their cap after RID/index ordering. Shape queries accept caller-owned resources or live shape RIDs. Resource assignment lazily registers a borrowed server RID; owner disposal invalidates it, while geometry edits mark server fixtures dirty before a later query. `IntersectShape` includes swept intersections. `CastMotion` ignores initial overlap and brackets the first new hit with eight refinements. `CollideShape` returns ordered query/collider point pairs and `GetRestInfo` selects the deepest contact with point velocity. Result records remain typed and keep RID/index even for server-only colliders. The current path is linear in fixture count, with a future broad-phase optimization gated by measured large-world cost. Point canvas-instance filtering, viewport world transitions, joints and wider server methods keep explicit coverage gaps; no vendored source changes are needed.
- ShapeCast borrows its configured Shape, converts its local target to global motion, casts once to find the earliest safe/unsafe bracket, and gathers typed rest contacts at that impact pose. Each returned collider RID is excluded from later rest queries until `MaxResults` is reached. Zero local motion queries the current pose and retains zero fractions. Leaving a collision parent removes its automatic RID exclusion in both scene query nodes. Automatic sampling runs in the internal fixed physics lane; a forced update can run while disabled. Options and borrowed shape survive PackedScene, while contact snapshots and explicit exceptions do not. The node reuses query and exclusion storage on warmed frames. Debug rendering and virtual tile collider projection keep separate exact coverage gaps.
- Test motion for scene and server body RIDs in their registered space, using a supplied finite unit-scale global pose, displacement, recovery margin and copied RID/instance exclusions. Scan current body fixtures with reciprocal collision filters; Areas do not block. Recover initial penetration in up to four steps, then bracket the first new impact with eight sweep refinements. A deep residual overlap stops motion at zero safe fraction. Return typed collider/local shape-owner indices, point, outward normal, depth, point velocity, safe/unsafe fractions, travel and remainder. A completed miss clears stale contact fields. Scene `TestMove` and test-only `MoveAndCollide` leave the pose unchanged; regular `MoveAndCollide` applies safe travel. Separation-ray participation remains blocked until its shape resource exists. Reuse candidate storage and indexed fixture scans so warmed unchanged scene tests allocate no managed bytes.
- Store each body collision exception as a one-sided, insertion-ordered RID entry. A pair is excluded when either body lists the other's RID, both in solver contacts and body motion tests; Area sensing and direct ray/point/shape queries retain their own filtering contracts. Scene methods take typed PhysicsBody nodes, while server methods accept arbitrary target RIDs (including empty or later freed values) and require a live body owner. Scene enumeration copies the list and projects server-only or freed entries as null slots. A list edit marks the owner's fixtures dirty so existing backend contacts are recreated with the correct pre-solve setting before the next step or query; merely toggling the flag on a live fixture does not update a contact's cached flag. Owner disposal/free drops its list. Keep the shared registry lock on the small per-body list until large-world contact profiling shows a throughput problem; no vendored code change is needed.

### Consequences

Game code can query its current world through typed scene access, scene query nodes or direct body motion without manually managing RIDs, while advanced code can create and test resources through PhysicsServer, including server-only colliders in the SceneTree space. RID numeric values are not reused, and a freed RID no longer resolves at its owning server. PhysicsQueryTests checks shared identity and lifecycle; PhysicsShapeQueryTests checks direct sweeps; PhysicsMotionTests checks body movement; PhysicsCollisionExceptionTests checks unilateral scene/server entries, live contact edits, motion filtering, stale targets and warmed allocation; RayCastTests and ShapeCastTests check scene snapshots. Native allocation accounting, other platforms, independent viewport worlds, virtual tile collision owners, physics debug drawing and owner acceptance remain unverified.

### Rejected alternatives

- Replace RID parameters and results with only CollisionObject/Shape references: server-created resources have no required scene object, and this would remove applicable API under ADR 0004.
- Add a physics-only PhysicsRID: it duplicates the reference's cross-server identity role needed by rendering and navigation.
- Publish inert RID/PhysicsServer placeholders or wait for every server method before the first query: either choice delays an executable, auditable physics-query slice without changing its required ownership contract.

<a id="adr-0064"></a>
## ADR 0064: Hollow paired-segment collision resource

Last updated: 2026-09-25

- Status: Accepted
- Scope: ConcavePolygonShape resource and hollow multi-segment body/area fixtures
- Depends on: [0061](#adr-0061), [0062](#adr-0062), [0013](resources.md#adr-0013)

### Context

SegmentShape supplies one two-sided edge, while a level boundary or line sensor needs several independent edges in one reusable resource. The pinned ConcavePolygonShape2D stores an array of endpoint pairs and has no solid interior, even if those pairs enclose an area. The current internal Shape contract can append several fixture IDs to one body or area. Box2D.NET rejects short segment fixtures, so the same numerical fallback used by SegmentShape is required for each pair.

CollisionPolygon2D was compared as the adjacent scene-node feature. ConvexPolygonShape and ConcavePolygonShape supply its two build geometries. ADR 0066 now owns the direct scene slot and conversion; ADR 0065 supplies one-way body contacts. The one-way margin now executes in typed body sweep recovery under ADR 0063.

### Decision

- Map ConcavePolygonShape2D to `ConcavePolygonShape : Shape` with an empty default and a caller-owned `Vector2[] Segments` copy boundary. Require an even number of finite endpoints with finite per-pair and overall bounds. Each consecutive pair defines one independent edge; empty input removes all fixtures. Assignment emits `Changed` even for equal content. `GetRect()` spans every stored endpoint and returns default when empty. Resource duplication copies the array independently.
- Reuse the internal multiple-fixture owner lists for bodies and areas. Transform each endpoint pair by its direct CollisionShape child, then create a two-sided segment or zero-radius point fixture using the SegmentShape backend tolerance rule. The public resource remains hollow; an object fully inside a closed set of edges is not reported as overlapping until it touches an edge. Body/area object events still deduplicate by owner.
- Keep the dynamic zero-area mass/inertia policy from ADR 0061 for bodies that borrow paired segments, while documenting that hollow multi-segment shapes are primarily level/static geometry. Warmed unchanged body/area scans allocate zero managed bytes in the checked Linux/.NET 8 profile. No vendored source is changed.

### Consequences

Games can use reusable multi-edge terrain, open contours and hollow line sensors. The geometry and owning resource are executable; ADR 0066 integrates the separate CollisionPolygon scene node. Native allocator accounting, other platforms and owner acceptance remain unverified.

<a id="adr-0065"></a>
## ADR 0065: One-way scene-body contacts

Last updated: 2026-09-25

- Status: Accepted
- Scope: CollisionShape one-way flag and local direction on scene physics bodies
- Depends on: [0054](#adr-0054), [0061](#adr-0061), [0008](scene.md#adr-0008)

### Context

Two-sided fixtures prevent a body from passing through a platform and landing on its opposite face. The pinned scene shape permits a local one-way direction, while the selected backend provides a pre-solve callback for awake dynamic-body contacts. The pinned rigid-body pair chooses the valid side when contact first appears and retains that decision until separation. Its one-way margin participates in kinematic motion/recovery queries, not in the rigid-body contact-side test.

### Decision

- `CollisionShape.OneWayCollision` defaults to false and `OneWayCollisionDirection` defaults to `(0, 1)`. A finite nonzero direction is normalized before mutation; zero rejects contact from every side when the flag is enabled. Direction rotates with the shape's local pose and its parent body. Area children retain these scene properties for packing but remain sensor-only and warn that one-way response does not apply.
- Mark only enabled body fixtures for Box2D pre-solve. Per-fixture user data carries the local contact direction; the world callback compares the first contact normal to its current body rotation. Keep the initial allowed/denied decision for the shape pair until no contact is observed. Fixture rebuilds change backend IDs and therefore discard old pair decisions without changing public node identity. The current world uses its default single-worker task system; callback state is world-owned and unavailable to user code.
- Expose finite nonnegative `OneWayCollisionMargin`, default one scene unit, on CollisionShape and CollisionPolygon. Typed body motion accepts initial recovery against the solid side only up to the larger of this value and the query's safe margin; a deeper one-way overlap is ignored. Fixed-step pre-solve continues to use the established side decision. The value survives PackedScene, and edits rebuild fixture tags before a later motion query. No vendored source changes are needed.

### Consequences

Static, kinematic and dynamic scene-body fixtures accept contacts from the configured side and allow traversal from the other side. Existing contact reports see only solved contacts, and Area monitoring continues to sense crossings. OneWayCollisionTests verifies fixed-step side selection; PhysicsMotionTests verifies sweep direction, margin depth and recovery. Native allocator counts, other platforms and owner visual acceptance remain unverified.

<a id="adr-0066"></a>
## ADR 0066: Direct scene collision polygons

Last updated: 2026-09-25

- Status: Accepted
- Scope: CollisionPolygon scene node, solid convex decomposition and closed hollow edges
- Depends on: [0008](scene.md#adr-0008), [0054](#adr-0054), [0062](#adr-0062), [0064](#adr-0064), [0065](#adr-0065)

### Context

Games need an editable concave or convex polygon child directly under a physics body or Area. The two resource geometries and one-way body-contact callback exist, but CollisionObject previously accepted only CollisionShape children. Nesting a hidden CollisionShape under the polygon would not provide a direct owner and would introduce scene state that users did not request. C# cannot declare a nested enum and a property with the same `BuildMode` identifier in one class.

### Decision

- Map the reference node to `CollisionPolygon : Entity` as a sibling of CollisionShape. A small internal `ICollisionGeometry` contract lets both direct children register with the same body/Area fixture lists; no public backend type or shape-owner stub is added. Name the typed enum `PolygonBuildMode` with Solids=0 and Segments=1, preserving the public `BuildMode` property name.
- Copy the caller's finite local `Polygon` array and validate finite overall bounds before mutation. Empty/insufficient or undecomposable contours remain editable and contribute no fixture in the affected mode, with configuration warnings. Solid mode uses `Geometry.DecomposePolygonInConvex` and owned ConvexPolygonShape resources for every part; Segments mode closes the contour into consecutive endpoint pairs in one owned ConcavePolygonShape. Both modes reuse the existing resource-to-Box2D fixture path, child translation/rotation and pre-rebuild scale/skew validation.
- Rebuild live fixtures for contour, mode, disabled and transform edits. A body child uses the existing one-way flag/direction pre-solve behavior and the one-way margin in typed body-motion recovery; Area remains a two-sided sensor and warns when one-way is enabled. The node owns/disposes generated resources and stores the typed contour/mode/options in PackedScene. A warning subscriber failure after a valid setter does not prevent the next fixture rebuild.

### Consequences

One node now supplies solid concave terrain, hollow closed boundaries or sensors without a hidden scene child. CollisionPolygonTests checks array ownership, errors, malformed editable contours, solid/segment mode, concave missing space, body contact, one-way traversal, live edits, warning failure, PackedScene and 64 warmed contact frames without managed allocation on Linux/.NET 8. Native allocator counts, other platforms and owner visual acceptance remain unverified.

<a id="adr-0067"></a>
## ADR 0067: Character sliding and platform following

Last updated: 2026-09-26

- Status: Accepted
- Scope: Caller-driven CharacterBody grounded/floating motion and typed slide snapshots
- Depends on: [0008](scene.md#adr-0008), [0054](#adr-0054), [0056](#adr-0056), [0060](#adr-0060), [0063](#adr-0063), [0065](#adr-0065)

### Context

The shared PhysicsBody motion test, typed KinematicCollision result, RID exceptions and kinematic backend exist. Games still lack the spatial character role that turns desired velocity into floor/wall/ceiling classification, repeated slide motion, floor snap and moving-platform following. The selected backend permits kinematic target motion but direct queries must see a character's new scene pose before the following solver step. A query that ignores an initially touching inclined segment can tunnel through that slope.

### Decision

- Map the spatial character to `CharacterBody : PhysicsBody`, keeping the Entity and CollisionObject branches intact. Keep `MotionMode` and `PlatformOnLeave` as properties; name their typed C# enum types `CharacterMotionMode` and `CharacterPlatformOnLeave` because C# cannot give a nested enum and property the same identifier. Preserve numeric enum values, applicable defaults and PackedScene state. Do not store transient contacts, flags or platform RID in PackedScene.
- Reuse the current server body-motion test for each slide without constructing a public result per fixed frame. In grounded mode classify normals against normalized UpDirection and FloorMaxAngle, apply stop/constant-speed/block-on-wall/ceiling controls, and snap a previously grounded body down only when not facing upward. Floating mode treats contacts as walls and applies WallMinSlideAngle. MaxSlides bounds the loop; copied KinematicCollision objects expose individual and last contacts. Desired Velocity remains caller-owned; the body reports Area/world gravity but game code chooses whether to add it. Validate finite motion and public options before mutation.
- Sample the last floor or wall body's point velocity and collision layer before movement. Floor/wall layer masks gate carry; movement caused by a platform excludes that platform RID, then a departure policy adds all, only upward, or none of its velocity to Velocity. Keep these operations on the space owner thread and use the existing body/Area field and RID lifetimes.
- After character scene movement, direct queries prepare its backend fixture at the new pose immediately. Retain the previous solved pose separately; before the next fixed step reset a temporarily prepared query pose and send the final scene pose through the backend kinematic target operation. This preserves contact velocity and same-frame query identity. The current registered-body scan for platform point velocity is linear; replace it only after measured large-world cost. Fix the shared body-motion kernel to report initial contact when movement points inward along a touching slope rather than skipping that pair. Warmed unchanged movement reuses candidate, platform-exclusion and slide-result storage.
- Keep CharacterBody class, MoveAndSlide and ApplyFloorSnap Partial for separation-ray-specific floor behavior until SeparationRayShape2D and its slide-on-slope motion option execute. The other own members can be Implemented with executable evidence for current shapes; inherited virtual tile collision-object gaps remain on their declaring result types.

### Consequences

Caller-driven grounded and floating characters can move along current shapes, snap to floors, follow moving floors and walls, inspect typed contacts, and read selected gravity without public backend types. CharacterBodyTests covers default/invalid/packed state, floor/wall/ceiling and floating movement, slope/ceiling/wall controls, slide caps, platform masks/leave policies, Area gravity, immediate queries, fixed-lane sync and warmed allocation. PhysicsMotionTests guards the touching-slope regression. Native allocation, other platforms, separation-ray behavior, large-world throughput and owner visual acceptance remain unverified.

### Rejected alternatives

- Use StaticBody teleportation for scene movement: it loses kinematic contact velocity and leaves the backend at an old pose during a same-frame query.
- Add a second character-only collision world: it would split RID identity, shape owners, masks and Area fields from the shared SceneTree space.
