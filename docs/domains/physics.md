# Physics domain

Last updated: 2026-10-10

[Public joint conformance](../components/physics-joint-policies.md#public-cpu-gpu-conformance) now runs
pin/groove/spring, server identity/lifecycle and solver policies through explicitly
selected CPU and independent GPU worlds. GPU motion queries now honor joint vetoes;
finite spring caps and complete decay handle extreme coefficients before range
validation. Full physics, platform and performance acceptance remain open.

[GPU host preparation](../components/gpu-host-preparation.md) batches changed body
policies before shared pre-step snapshots and reuses the current resident rotation
basis for pose reads. Explicit public CPU/GPU tests cover authored edits, CCD,
callbacks, replay and reentry; full contract/performance acceptance remains open.

[Per-body CCD](../components/cpu-continuous-collision.md) now exposes shared Disabled/CastRay/CastShape policy through RigidBody and PhysicsServer. CPU checks solved trajectories before publication and retains force budgets and frame impulses across impact intervals. The common CCD, analytic boundary and directed-ray suites now execute on explicitly selected CPU/GPU worlds, including unchanged sleeping motors, remaining-time budgets and physical ray-tip contacts. Broader physics acceptance remains open.

[Resident body-motion queries](../components/gpu-resident-motion-queries.md) now execute supplied-pose recovery and sweeps on GPU, with reciprocal masks, one-way/ray policies, explicit exclusions and center-aware hit velocity. CPU full-contour recovery and directed containment now avoid internal polygon seams. The public GPU motion adapter now executes; full CharacterBody conformance remains open.

Body runtime mass/forces and direct views now use engine-valued attachment operations.
Concrete solver state and contact traversal belong to PhysicsColliderBackend;
queued callbacks and views validate a per-collider attachment version. Public
GPU-world selection/binding now executes through the shared adapter.

[Resident shape queries](../components/gpu-resident-shape-queries.md) now execute intersections, contact pairs, deepest rest information and motion brackets over standalone leased geometry on GPU. CPU compound casts and directed-query containment now ignore internal decomposition seams. Public GPU queries now execute through the shared world.

[Resident ray and point queries](../components/gpu-resident-queries.md) now search current device geometry with stable logical result caps, masks, exclusions and authored canvas filtering. Query-only tree preparation skips simulation pair generation; the public world/query adapter now executes.

[Resident contact publication](../components/gpu-resident-reports.md) now retains complete outer-tick normal/friction impulses, transient contacts and capped per-body snapshots on the independent GPU store. Public GPU direct-state/event projection now executes.

[Resident Area fields](../components/gpu-resident-fields.md) now reduce directional/point gravity and independent damping on device, using current deduplicated sensor membership, body Combine/Replace policy and scoped changed-field waking. Mixed sensor/body pair work is distributed across receiver queries; public GPU field/event projection now executes.

[Kinematic target/surface separation](../components/gpu-resident-kinematic.md) now executes on the independent GPU store, including full-shape paths against default-CCD dynamics, exact target/idle poses and contact/joint velocity separation. Public scene/server integration now executes; full conformance remains open.

## Square atlas tile integration

[Tile resources and layers](../components/tiles.md) now connect square atlas authoring and typed storage to actual merged static/kinematic bodies in CPU/GPU worlds, with tile-owner queries/events, runtime data callbacks and real retained-canvas output. Other tile layouts, terrain, animation, navigation and occlusion remain explicit gaps under [ADR 0101](../decisions/tiles.md#adr-0101).

## Physical skeletal integration

[PhysicalBone](../classes/PhysicalBone.md) adds real skeletal rigid bodies with inherited forces, freeze, contacts and authored joints. Static followers use effective zero collision filters while preserving configured filters; the idle modification consumer publishes solved poses to the rig.

## Jiggle controller integration

Jiggle uses the existing direct-world ray query during Physics execution, with body/layer filtering and fixture preparation, to revert a blocked dynamic-point candidate. It does not add a physics body, joint or whole-bone collision shape. Collider misuse in Idle fails explicitly.

## Responsibility

Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

Physics owns the executable 2D rigid-body, collision-shape, surface-material and area-monitoring profiles. A SceneTree retains distinct viewport-selected worlds and a fallback for viewport-free scenes. Each selects a CPU Box2D.NET space or the independent resident GPU backend, stepped once during the fixed physics lane, with dynamic transforms and overlap snapshots published before timers and tweens. CPU is the default and remains available without a GPU. The managed CPU implementation is vendored and internal to `Electron2D.dll` under [ADR 0012](../decisions/product.md#adr-0012); [ADR 0054](../decisions/physics.md#adr-0054) defines explicit backend choice and startup fallback.

## Component inventory

| Component | Production types | State |
| --- | --- | --- |
| [Collision shapes](../components/physics-shapes.md) | [`Shape`](../classes/Shape.md), [`CircleShape`](../classes/CircleShape.md), [`CapsuleShape`](../classes/CapsuleShape.md), [`SegmentShape`](../classes/SegmentShape.md), [`ConvexPolygonShape`](../classes/ConvexPolygonShape.md), [`ConcavePolygonShape`](../classes/ConcavePolygonShape.md), [`RectangleShape`](../classes/RectangleShape.md), [`CollisionShape`](../classes/CollisionShape.md), [`CollisionPolygon`](../classes/CollisionPolygon.md), [`PolygonBuildMode`](../classes/PolygonBuildMode.md) | Reusable shapes, borrowed placement, owned solid/hollow scene polygons, live fixture updates and one-way body-contact direction and motion-recovery margin executable; standalone Shape collision methods execute; directed ray solver impulses/materials/sleep execute; analytic world boundaries now execute; custom solver bias and retained debug color execute |
| [World sleep policy](../components/physics-sleep.md) | [`PhysicsSleepSettings`](../classes/PhysicsSleepSettings.md), [`PhysicsServer`](../classes/PhysicsServer.md), [`ProjectSettings`](../classes/ProjectSettings.md) | Typed project/world thresholds and duration, separate speed checks, ordered wakes and zero warmed allocation; public GPU binding now executes; full conformance remains open |
| [Scene physics bodies](../components/physics-bodies.md) | [`CollisionObject`](../classes/CollisionObject.md), [`PhysicsBody`](../classes/PhysicsBody.md), [`KinematicCollision`](../classes/KinematicCollision.md), [`CharacterBody`](../classes/CharacterBody.md), [`CharacterMotionMode`](../classes/CharacterMotionMode.md), [`CharacterPlatformOnLeave`](../classes/CharacterPlatformOnLeave.md), [`RigidBody`](../classes/RigidBody.md), [`StaticBody`](../classes/StaticBody.md), [`AnimatableBody`](../classes/AnimatableBody.md), [`PhysicsMaterial`](../classes/PhysicsMaterial.md) | Dynamic/static/kinematic motion, grounded/floating character sliding, platform carry, body sweeps, contacts, forces, fields and filtering executable; wider body/server contracts incomplete |
| [Scene physics joints](../components/physics-joints.md) | [`Joint`](../classes/Joint.md), [`PinJoint`](../classes/PinJoint.md), [`GrooveJoint`](../classes/GrooveJoint.md), [`DampedSpringJoint`](../classes/DampedSpringJoint.md) | Shared scene/server joint RIDs, revolute/guide/spring kernels, collision suppression, angular limits and motor executable; positional bias/correction caps and pin softness execute; retained debug drawing executes |
| [Physics server and direct queries](../components/physics-queries.md) | [`RID`](../classes/RID.md), [`PhysicsServer`](../classes/PhysicsServer.md), [`World`](../classes/World.md), [`PhysicsDirectSpaceState`](../classes/PhysicsDirectSpaceState.md), [`RayCast`](../classes/RayCast.md), [`ShapeCast`](../classes/ShapeCast.md), typed ray/point/shape/motion parameters and results | Shared scene/server space identity, resource lifecycle, direct and body motion queries, cached scene ray/shape casts executable; canvas/navigation RIDs execute; collider canvas filtering and wider server methods remain incomplete |
| [Physics areas](../components/physics-areas.md) | [`Area`](../classes/Area.md), [`Area.SpaceOverride`](../classes/Area.SpaceOverride.md) | Directional monitoring, snapshots, object events and priority gravity/damping fields executable; audio routing and logical shape events execute; tile-layer object and shape payloads execute through the shared association contract |

## Completion boundary

Scene/server collider lifetime, engine-valued creation, pose/motion/sleep access
and resolved body fields now use shared internal adapters. This preserves the
existing public contract while isolating vendor-dependent operations. Built-in
shape resources now expose borrowed scene-unit geometry internally; CPU fixture,
query and mass compilation is isolated in PhysicsShapeBackend. Independent GPU
ownership and public backend selection remain open. Joint identity, sampled frames
and settings now also sit outside vendor storage; PhysicsJointBackend owns current
constraint handles and CPU spring evaluation. A separate GPUPhysicsBodyStore now
retains and integrates device body state, shared shape geometry, transformed bounds,
complete broad-phase pairs and narrow-phase contact points without a CPU world or
CPU spatial/contact mirror. Contact impulse/material response, warm history and
separate correction now execute on GPU. Joints, automatic mass-center profiles, sleep,
CCD and public selection/publication still need to be connected.

Physics readiness requires both CPU/Box2D.NET behavior and a complete selectable
independent GPU world. The [source audit](../components/physics-contract-audit.md)
and [generated declaration ledger](../coverage/physics-status.md) separate current
implementation, inherited requirements and missing behavior. Box2D internal storage,
ordering and bitwise CPU/GPU equality do not constrain the GPU architecture. GPU-stage conformance alone does not close the common API contracts.
Acceptance includes per-body CCD modes, stationary linear/angular surface velocity,
world-boundary response, independent GPU joint-policy integration,
shape/body/world solver settings, object/shape mouse picking, contact impulse totals
and truncation, virtual tile-owner propagation, canvas-filtered point queries,
unified shape/joint debug drawing, remaining typed server state/identity operations,
and backend extension/registration. Authoritative networking additionally requires
fixed-tick input and snapshots, restore/replay, predicted/confirmed events, remote
interpolation and separate-process CPU-server/GPU-client verification under
[ADR 0094](../decisions/networking.md#adr-0094); these remain unimplemented. The owning [coverage tables](../coverage/index.md),
including [bodies](../coverage/classes/RigidBody2D.md),
[surfaces](../coverage/classes/StaticBody2D.md) and
[server operations](../coverage/classes/PhysicsServer2D.md), retain the detailed
acceptance evidence and gaps. This paragraph records the full completion boundary,
not implemented functionality or a replacement for those tables.

## Public surface

`RigidBody`, `StaticBody`, `AnimatableBody` and `CharacterBody` inherit the spatial `Entity` role through `PhysicsBody` and `CollisionObject`; `Area` is the parallel `CollisionObject` branch. `CollisionShape : Entity` must be their direct child to supply a borrowed Shape resource. Circle, capsule, segment, convex polygon, concave segment collection and rectangle resources expose their pinned dimensions and local bounds. Bodies and areas expose 32 collision-layer/mask bits. RigidBody, StaticBody and AnimatableBody can borrow a PhysicsMaterial with friction, bounce, rough and absorbent settings. A dynamic body exposes force/torque, damping, sleep, contact reports and field gravity. CharacterBody exposes caller-driven `Velocity`, grounded/floating slide and floor snap, platform carry and the inherited last-step `GetGravity()` query without automatically applying gravity. Areas expose monitoring, overlap events and priority gravity/damping fields.

`PinJoint : Joint : Entity` connects two distinct PhysicsBody paths at one spatial anchor. It supports connected-body collision policy, relative angular limits, angular motor speed and a tunable torque cap. SceneTree owns the backend joint with its physics world; body/joint exit releases that handle before body destruction. Stable scene/server joint identity and lifecycle execute under ADR 0087; tuning gaps remain explicit in coverage.

`GrooveJoint : Joint : Entity` connects an anchor on body B to a finite guide on body A. Its signed local-Y Length can be reversed or zero, and InitialOffset chooses the sampled body-B anchor. The solver preserves free rotation, updates live length limits and rebuilds on an offset change. Authored guide/anchor diagnostics now use the retained canvas under ADR 0100.

## Runtime flow and invariants

`DampedSpringJoint : Joint : Entity` connects sampled body-local anchors through Hooke force and axial drag. Its native filter joint owns island/collision linkage while Box2D retains integration and contact solving. Spring responses include rotational anchor leverage, pure damping, equal opposite impulse and preflight of all springs before application. Each kinematic world interval uses its actual duration; there is one outer scene callback/event cycle. See [physics joint decisions](../decisions/physics-joints.md#adr-0086).

Each scene CollisionObject registers one opaque RID, independent of Box2D fixture IDs. SceneTree registers its lazily created world with PhysicsServer; a server-only body or Area can attach to that same space, and an independent server space can be stepped explicitly. CanvasItem.GetWorld returns the SceneTree's shared World physics view. Ray, point and shape queries prepare pending scene geometry, test collider layers independent of their masks, honor RID exclusions, and return typed results with both collider RID and stable shape-owner index. Shape resources lazily borrow a server RID for direct queries; geometry edits update their server fixtures before querying and disposal releases that RID. Direct shape overlap includes motion, casts return safe/unsafe fractions, and contact/rest queries expose typed manifold results. Server-only results have no scene Collider. CPU query scans are linear in current fixtures; GPU queries traverse resident geometry, and attached queries require the space owner thread outside a solver step.

Body motion queries use the same registered space and current body fixtures but apply reciprocal response filters. A test pose recovers from initial penetration, then sweeps to the first eligible body contact. Safe/unsafe fractions and contact data retain both direct shape-owner indices; scene `MoveAndCollide` applies only safe travel. One-way child margins govern accepted recovery depth. Server `BodyTestMotion` leaves the body in place and accepts RID/instance exclusions. Separation rays always participate in recovery; motion includes sliding rays automatically and non-sliding rays when CollideSeparationRay is true. Character snap enables that flag under [ADR 0068](../decisions/physics.md#adr-0068).

Body collision-exception lists use stable scene/server RIDs. Either body's entry suppresses a pair from ordinary solver contacts and body motion tests while Area sensing stays independent. A list edit updates the selected backend before a later query or step so active contacts adopt the new rule; GPU changes retain existing shape handles. Scene enumeration returns an insertion-order copy with null entries for server-only or freed RIDs.

One scene unit maps to 0.01 Box2D meters. Typed project defaults provide downward gravity 980 scene units/s² and linear/angular damping 0.1/1 per second, sampled when a physics world is created. The scene physics callback lane runs first; current area/body shape overlaps then resolve gravity and damping by descending priority, and persistent body force/torque joins the accumulator before a four-substep backend step. Body damping applies `max(0, 1 - delta * totalDamp)` before force integration. Linear forces/impulses convert by 0.01 and torque/angular impulses by 0.0001. Updated positions and velocities reach scene nodes through one unit-scale global transform. AnimatableBody sends a kinematic target before the solver and optionally presents its solved pose afterward. CharacterBody derives a target from caller-driven slide motion; direct queries see its new pose immediately, while the fixed solver derives contact velocity from the previous solved pose. Current touching manifolds then produce capped contact-point counts and object-level transitions; sleep and contact callbacks precede area monitoring events, timers, tweens and interpolation capture. Bodies and areas register on tree entry and leave on exit/disposal. Shape, collision-filter and material edits rebuild fixtures before the next step or direct query. Shape and material resources remain caller-owned.

The current geometry profile accepts translated/rotated bodies and areas with unit global scale and zero skew. Active scaled/skewed body, area or shape transforms fail explicitly and can be corrected before a later step. Invalid mass, dimensions, velocity, damping and bit indices reject before changing state. Engine-owned warmed resting-contact, active-contact, freely moving and steady area-monitoring frames in the checked Linux/.NET 8 setup allocate zero managed bytes. Backend types do not appear in the public/protected Electron2D assembly surface.

Direct body views now keep backend-neutral contact values and current-attachment
validation; backend reads, unit conversion and contact traversal reside in
PhysicsColliderBackend. Both CPU and independent GPU worlds now use this attachment boundary.

Scene bodies, Areas and raw server colliders now share PhysicsColliderBackend body/fixture ownership. It centralizes creation, filtering/material/tag setup, transform validation and failure-aware destruction; execution selects CPU fixtures or independent resident GPU handles. Full cross-family conformance remains open.

Initial body configuration and scene motion roles now use engine values and
PhysicsServer.BodyMode. The shared collider adapter owns vendor definition/unit
conversion. Tests cover static/kinematic/dynamic roles, rotation locks, freeze,
disabled participation, CharacterBody and physical-bone activation/reentry.

## Verification and limits

GPU physics is being developed under [ADR 0054](../decisions/physics.md#adr-0054)
alongside the retained CPU backend. The internal compute host
currently executes resident broad-phase tree construction/refit/traversal, built-in pair filtering with resident shape/joint metadata and contact lookup, contact ID allocation/initialization, adjacency construction/disjoint-contact removal and resident contact-driven island merging/unlinking with parallel ordered contact lists, compact publication and complete host validation using independent packed CPU flags and disconnected-island splitting, integration, resident circle/capsule/segment/polygon geometry and manifolds, material/contact-state updates, collision-batch constraint coloring, fused body-pose/sleep-eligibility/fast-body finalization, and contact/revolute/wheel preparation and solving; this historical stage host remains CPU-owned. Public GPU worlds use the independent
resident store described below. [The implementation status](../components/gpu-physics.md) separates
these executing stages from the required full GPU world and startup fallback.
The independent GPUPhysicsBodyStore now has a [contact response path](../components/gpu-contact-solver.md)
with persistent history, compact contact Jacobians, separate iterated impulses
and specialized update/gather kernels. [Device joints](../components/gpu-resident-joints.md)
now share that solve loop and retain pin/groove/spring settings, warm impulses and
collision vetoes without a CPU solver. [Explicit directed body exceptions](../components/gpu-resident-exceptions.md)
now use the same device table for discrete and continuous collisions; live-body
removal cleans incident edges without scanning all authored exceptions. [Resident one-way contacts](../components/gpu-resident-one-way.md)
now retain initial side decisions per geometry-piece pair and share them with CCD,
including rotating separation/recontact within one step. [Live body policies](../components/gpu-resident-parameters.md)
now update roles, gravity/damping, dynamic rotation locks and automatic integration
omission without a pose/velocity mirror; CPU field and mode checks cover the shared
settings semantics. [Transient forces](../components/gpu-resident-forces.md) now
retain pending force/torque and outer-tick eligibility on device, with CPU response
checks and no body-state download for consumption. Public force projection remains
open. [Mass profiles](../components/gpu-resident-mass.md)
now reuse shared authored-geometry normalization and supply center-aware GPU motion
and constraint preparation. Its stability/allocation/population checks
and opt-in fenced diagnostics do not establish public-backend or window-FPS acceptance.

[PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks direct shape RID/resource selection, live edits, swept overlap, safe/unsafe motion, manifold contact pairs, rest velocity, compound and hollow geometry, filters, off-owner rejection and warmed unchanged casts/rest queries without managed allocation. Native allocation, other platforms and owner visual acceptance remain unverified.

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks server and scene body travel, typed local/collider shape identities, instance exclusions, reciprocal masks, one-way recovery and deep-overlap stopping, plus warmed unchanged motion tests without managed allocation. Native allocation, other platforms and owner visual acceptance remain unverified.

[CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks grounded/floating branches, floor/wall/ceiling classification, slope/ceiling/wall controls, slide caps, snap, platform floor/wall masks and departure policies, Area gravity, PackedScene, immediate query pose, fixed-step backend synchronization and warmed idle slides without managed allocation. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) verifies ray sliding/snap and directed/reverse queries. [SeparationRayDynamicsTests](../../tests/Electron2D.Tests/SeparationRayDynamicsTests.cs) now verifies ordinary CPU directed manifolds, materials, mass/impulse transfer, sleep, events and warmed zero allocation under [ADR 0068](../decisions/physics.md#adr-0068). Public independent-GPU binding now executes; complete family conformance remains open.

[PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks unilateral scene/server RID lists, regular contacts, active-pair edits, motion-test filtering, stale targets, thread ownership and warmed steady frames without managed allocation. Native allocator and other platforms remain unverified.

[ShapeCastTests](../../tests/Electron2D.Tests/ShapeCastTests.cs) checks fixed-frame and forced shape sampling, multiple contacts, caps, exceptions, filters, Area/body results, packing, rotation, zero motion, failure recovery and warmed active frames. Retained scene-query drawing executes under ADR 0100; virtual tile collision owners remain a coverage gap.

[RayCastTests](../../tests/Electron2D.Tests/RayCastTests.cs) checks fixed-frame scene snapshots, force/pause behavior, parent and RID exceptions, inside hits, Area/body masks, PackedScene, failure recovery, server-only identities and 64 warmed active frames with zero managed allocation. Virtual tile collision-object results remain [RayCast2D coverage](../coverage/classes/RayCast2D.md) gaps; native allocator and other-platform ray behavior are unverified.

[CollisionPolygonTests](../../tests/Electron2D.Tests/CollisionPolygonTests.cs) checks direct body/Area geometry, solid concave decomposition, hollow edges, one-way response, live edits, failure recovery, PackedScene and warmed contact allocation. Its one-way margin now gates typed body motion recovery under [ADR 0066](../decisions/physics.md#adr-0066).

[OneWayCollisionTests](../../tests/Electron2D.Tests/OneWayCollisionTests.cs) checks the accepted and pass-through sides, rotation, live changes, packing, Area sensing and warmed active-contact allocation on Linux/.NET 8. Its one-way margin now executes in body motion recovery under [ADR 0065](../decisions/physics.md#adr-0065).

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks rectangle and circle geometry, falling/contact response, impulse, filter changes, live shape edits, freeze and velocity, rejected transforms, tree exit/re-entry, managed shape copying, PackedScene state, disposal and the warmed allocation boundary. [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks coupled capsule geometry, body contact, area detection and warmed allocation. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks two-sided line contacts, area sensors, dynamic mass/inertia and warmed allocation. [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks compound solid contours, body/area fixtures and warmed allocation. [ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks paired hollow terrain/sensor fixtures and warmed allocation. [AnimatableBodyTests](../../tests/Electron2D.Tests/AnimatableBodyTests.cs) checks moving platforms, mode switching, recovery, packing and warmed idle/active kinematic frames. [PhysicsMaterialTests](../../tests/Electron2D.Tests/PhysicsMaterialTests.cs) checks defaults, mixing, live updates and disposal, duplication and PackedScene ownership. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks directional detection, events, geometry changes and lifecycle. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks field modes, point falloff, signed damping, sampled project settings, sleep wakeup, packing and failure recovery. [RigidBodyForceTests](../../tests/Electron2D.Tests/RigidBodyForceTests.cs) checks force/torque semantics, first-step mass geometry, packing and invalid rollback. [RigidBodyContactTests](../../tests/Electron2D.Tests/RigidBodyContactTests.cs) checks contact points, multi-shape deduplication, removal/callback failure, sleep, packing and warmed allocations. Release build and the public-surface exporter verify the single-DLL boundary. The device contact check passed on Android arm64, Android TV arm and an isolated browser-wasm prototype; see the [platform matrix](../platform-verification.md). Other physics capabilities on those targets, untested platforms, owner visual acceptance, native allocator accounting and larger-world performance remain unverified.

Standalone Shape methods, other shape resources, kinematic bodies, area audio integration, remaining joint solver tuning, remaining direct-space/server methods, shape-index contact events and remaining RigidBody modes retain exact incomplete coverage rows. [ADR 0063](../decisions/physics.md#adr-0063) defines shared RID identity, world access and typed results; direct shape operations and scene ShapeCast now execute under that decision. An executing backend does not make unrelated rows complete.

## Interactive example

[WaterPlayground](../../examples/WaterPlayground/README.md) is a single resizable water playground. Its public-API consumer solves particle density constraints on selectable CPU/GPU paths, and exchanges impulses with a duck, boat, bucket, wheel/lift, gate and buoyant or sinking cargo in the public CPU rigid-body space. The [simulation contract](../components/water-playground.md) records units, ownership and numerical limits. The former story selector and inspectors are removed; this example does not close runtime API coverage rows.

## Decisions

- [0054: Box2D-backed scene bodies](../decisions/physics.md#adr-0054)
- [0055: Directional scene area monitoring](../decisions/physics.md#adr-0055)
- [0056: Area field priority and body damping](../decisions/physics.md#adr-0056)
- [0057: Positioned and persistent rigid-body forces](../decisions/physics.md#adr-0057)
- [0058: Rigid-body contact and sleep snapshots](../decisions/physics.md#adr-0058)
- [0059: Capsule collision resource](../decisions/physics.md#adr-0059)
- [0063: Shared RID and world-scoped queries](../decisions/physics.md#adr-0063)
- [0060: Fixed-step kinematic platform motion](../decisions/physics.md#adr-0060)
- [0061: Segment fixtures and zero-area mass](../decisions/physics.md#adr-0061)
- [0062: Compound convex fixtures](../decisions/physics.md#adr-0062)
- [0064: Hollow paired-segment resource](../decisions/physics.md#adr-0064)
- [0012: Managed dependency vendoring](../decisions/product.md#adr-0012)
- [0008: Spatial scene inheritance](../decisions/scene.md#adr-0008)
- [0014: Resource lifetime and hot paths](../decisions/resources.md#adr-0014)

[Shape collision methods](../classes/Shape.md#collide) now expose resource-only overlap, independent swept regions and up to sixteen boundary pairs, without a world or RID. They share shape-family geometry and the directed ray kernel while retaining distinct motion semantics under [ADR 0069](../decisions/physics.md#adr-0069). Full convex contours do not expose internal fixture boundaries. ShapeCollisionTests covers all ordinary family pairs, special exclusions, contact cap/lifetime and warmed zero managed allocation on Linux/.NET 10.

[PhysicsDirectBodyState](../classes/PhysicsDirectBodyState.md) and RigidBody custom integration now expose live owner-thread fields and immutable solved contact values after native stepping. PhysicsBodyRuntime owns body callback/persistent-force configuration; PhysicsSpace snapshots callback entries, aggregates failures and resynchronizes scene state. Persistent direct views without requested or previously captured contacts no longer activate the callback/contact snapshot lane; assigning a contact cap clears the previous point count immediately. Server runtimes retain their registered owner with an explicit release guard; scene owners remain weak. Typed userdata adapters allocate at registration. Sixty-four warmed active callback frames allocate zero managed bytes on Linux/.NET 10; whole-step normal/tangent impulses are complete; virtual tile identities remain Partial under [ADR 0070](../decisions/physics.md#adr-0070).

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration and warmed zero managed allocation under [ADR 0071](../decisions/physics.md#adr-0071).

Logical shape-pair monitoring now uses a shared PhysicsShapePairTracker and committed transition queues. Native compound fixtures are deduplicated by RID and remote/local global slot indices; object/shape entry and departure ordering is preserved. Scene Area scans include server colliders with nullable payloads and explicit server Area monitorability. Callback failures continue later queued events; monitor policy changes inside in/out callbacks reject. ShapePairEventTests checks steady and active entry/exit paths without warmed managed allocation. [Physics monitoring decisions](../decisions/physics-monitoring.md) now own active ADRs 0055/0058; stable legacy anchors route there.

## Disabled collision branches

[CollisionObject.DisableMode](../classes/CollisionObject.md#disablemode) applies effective inherited ProcessMode.Disabled to physics: Remove/default omits fixtures and fields, MakeStatic retains stationary Body contact response and active Area sensors/fields, KeepActive continues the normal solver. Pause alone does not apply the policy. Stable RID/owner configuration survives removal, attachment-bound views expire, and peer departure events commit synchronously. Configured Freeze and body roles remain separate; kinematic static movement teleports without contact velocity. [CollisionDisableModeTests](../../tests/Electron2D.Tests/CollisionDisableModeTests.cs) covers transitions, query/contact/sensor behavior, storage, failure recovery and 64 warmed mixed solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and visual acceptance remain unverified. [ADR 0072](../decisions/physics.md#adr-0072) owns this contract.

## Shared body mass profile

[RigidBody](../classes/RigidBody.md#centerofmassmode) stores automatic/custom local center and zero/explicit scene-unit inertia. Typed [PhysicsServer](../classes/PhysicsServer.md) mass methods execute for scene and server identities, using one-kilogram normalization instead of raw fixture density. The internal [PhysicsMass](../../src/Servers/Physics/PhysicsMass.cs) calculation validates before profile/native commit, measures native solid primitives independently of body density, applies parallel-axis inertia or the segment-only rod fallback, and refreshes native shape extents around the selected center. The body runtime retains configured values and resolved geometry separately from static inverse values. Mode/reset property-list callbacks run after commit; profile changes leave scene origin and velocity unchanged. [PhysicsMassProfileTests](../../tests/Electron2D.Tests/PhysicsMassProfileTests.cs) covers force response, shared lifecycle, numeric/thread/phase rejection and 64 warmed changes plus solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner acceptance remain unverified. [ADR 0073](../decisions/physics-mass.md#adr-0073) owns the mass policy and typed parameter projection.

## Shared force state

The body runtime now owns one-step pending force/torque and routes scene/direct-view actions through all 13 typed [PhysicsServer](../classes/PhysicsServer.md) force/impulse methods. Detached impulses resolve the same mass primitives from reused resource proxies, without a world/body allocation. Pending values survive removal/static/dormant participation, respect the latest Sleeping assignment and are consumed once on eligible integration; omission discards them, while constant totals remain configured. Removal captures current dynamic velocity. Native fixture creation skips intermediate automatic mass updates so attach/rebuild cannot inject a center-shift velocity. [PhysicsServerForceTests](../../tests/Electron2D.Tests/PhysicsServerForceTests.cs) checks lifecycle, units, every primitive family, numeric/phase/thread rejection and 64 warmed attached/detached actions plus solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and visual acceptance remain unverified. [ADR 0074](../decisions/physics-forces.md#adr-0074) owns this contract; axis velocity remains separate body-state work.

## Frozen kinematic motion

[RigidBody.FreezeMode](../classes/RigidBody.md#freezemode) selects Static/default teleports or manually driven Kinematic targets while Freeze remains true. The latter ignores gravity/forces, exposes zero physical inverse values and restores configured dynamic mass/locks on unfreeze. Exact native history and unchanged-target guards avoid decoded-angle idle drift; synchronized AnimatableBody uses the same idle rule. The internal [kinematic integration path](../../src/Servers/Physics/PhysicsSpace.Kinematic.cs) subdivides native calls by travel and collider extents, replays captured force/torque for full outer duration and retains one callback/event cycle. Cost grows with travel and small geometry; native TOI is the measured-performance upgrade path. Contact impulse views aggregate every native interval of the outer frame through the shared CPU/GPU accumulator. [RigidFreezeModeTests](../../tests/Electron2D.Tests/RigidFreezeModeTests.cs) checks physical path contacts and 64 warmed active subdivisions with zero managed allocation on Linux/.NET 10. Native allocation, broad-world performance, other platforms and owner visual acceptance remain unverified. [ADR 0075](../decisions/physics.md#adr-0075) owns this policy and the stationary-surface channel.

## Complete typed body parameters

[PhysicsServer](../classes/PhysicsServer.md) now connects all ten material/mass/field parameter capabilities to concrete typed getter/setter pairs. Signed coefficients use actual fixture material and rough/absorbent combines; per-body writes isolate borrowed resources and reload on material revision/disposal. The internal [runtime field policy](../../src/Servers/Physics/PhysicsBodyRuntime.Parameters.cs) scales selected gravity and resolves Combine/Replace signed damping before automatic motion, validating totals and preserving wakeup/omission/role semantics. Scene RigidBody descriptors remain bidirectional; CharacterBody and direct state report matching scaled gravity. [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) checks actual contact/field response, ownership and failure paths, with 64 warmed field/read/solver iterations at zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner acceptance remain unverified. [ADR 0076](../decisions/physics-mass.md#adr-0076) owns typed selector adaptation and current behavior.

## Server Area sensor receivers

Typed [PhysicsServer](../classes/PhysicsServer.md) body/Area monitor callbacks now use the existing exact overlap kernel for scene/server receiver identities. The internal [Area runtime](../../src/Servers/Physics/PhysicsAreaRuntime.cs) stores optional delegates, logical pair snapshots and epochs; the [space scanner](../../src/Servers/Physics/PhysicsSpace.AreaMonitoring.cs) commits before callback dispatch, filters other layers directionally and retains monitorable gating. Payload includes other RID, stable scene InstanceID or zero, and both logical indices. Receiver reentry resets history while retaining registration; peer removal/free, reentrant removal and callback failures drain without stale entry replay. Scene snapshots remain independent. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) checks 64 warmed entry/exit cycles with zero managed allocation on Linux/.NET 10. Native allocation, large-world performance, other platforms and owner acceptance remain unverified. [ADR 0077](../decisions/physics-monitoring.md#adr-0077) owns current semantics; server Area fields retain their next separate slice.

Typed PhysicsServer Area field getter/setters now share scene/server profiles and address a space default Area by its Space RID. Bounded fields use current exact overlap and descending mixed priority; monitor flags/callbacks are independent. Initial space defaults sample ProjectSettings, then explicit live edits apply on the next nonzero step without changing other worlds. Point/default gravity, body damping/scaling, wakeup and failure recovery are verified by [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) under [ADR 0056](../decisions/physics-fields.md#adr-0056). Native allocation, other platforms and owner visual acceptance remain unverified.

Server-only connections share the scene joint kernels and body-local anchors through [PhysicsJointRuntime](../classes/PhysicsJointRuntime.md). Cold membership changes reconnect pending resources, while final body free clears dependents. Raw same-role scene RID configuration and concrete settings project bidirectionally; related-world phase/owner checks and independent collision contributions are defined by [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Indexed PhysicsServer geometry](../classes/PhysicsServer.md#shape-slots) now shares the scene/server logical slots, effective local poses and native fixture/query path under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). Raw replacement/pose/disabled/one-way edits do not rewrite child configuration; group/child edits reclaim corresponding overrides. Shape free/replacement follows shared RID/view ownership and related-world phase guards. Effective poses also feed mass geometry. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies real geometry, body/Area lifetime and one-way contacts/motion on Linux/.NET 10.

[Physics world activity](../classes/PhysicsServer.md#activity) is now independent of scene scheduling under [ADR 0089](../decisions/physics-activity.md#adr-0089). SceneTree activates its lazily created world; explicit SpaceCreate defaults inactive and requires SpaceSetActive(true). Global/local false skips simulation, force consumption and solver callbacks without clearing native state or accumulating elapsed time. Queries/configuration/cleanup and scene callbacks/timers continue. [PhysicsActivityTests](../../tests/Electron2D.Tests/PhysicsActivityTests.cs) checks the profile and warmed allocation on Linux/.NET 10.

Prepared point/shape/contact query destinations and character slide snapshots can be reused without copied output allocation. Rigid contact limits prepare monitoring storage. Solver contact compaction and sleep/wake transitions reuse cached backend buffers; caches are released with their world. The retired multi-story sandbox measured owner-thread allocations with UI and debug drawing; those historical results do not measure the current water example. Native/GPU allocation was outside that gate.

## Backend throughput

The internal world selects retained workers for large awake populations and eight-lane SIMD contact arithmetic, plus four-lane polygon support searches with scalar reduction order. Public physics callback timing and four substeps remain unchanged. [The measured contract](../components/box2d-performance.md) separates raw backend throughput from scene synchronization, rendering and foreign-platform acceptance.

Rigid-body monitoring and direct-state contact values share one traversal of the finished solver. Each world captures awake body motion once for contact-point velocities. Weak fixture owner tags avoid registry lookups during collection. Large worlds collect private per-body snapshots on retained workers, then queue changes in body order and invoke all user callbacks on the scene owner. Pair order, contact caps, retained values after fixture edits and zero-allocation warm steps are preserved. See the [performance measurements](../components/box2d-performance.md) for the full SceneTree benchmark and its limits.

The developing GPU path retains generated manifold geometry for constraint preparation,
using batch provenance and feature identity across graph moves with step-scoped validity.
Feature matching and normal/tangent/rolling warm-start reuse execute on GPU,
reading current previous-solve slots or compact cold histories. Managed material
callbacks, contact transitions and the CPU contact mirror still require readback. Geometry overrides cover contacts awakened after collision collection;
see [GPU physics](../components/gpu-physics.md) for transfer and conformance evidence.

The former sandbox Smash workload measured a large sleeping fragment wall. Its [performance report](../components/physics-sandbox-performance.md) remains historical evidence. The independent [GPU Smash developer preview](../components/gpu-smash-preview.md) remains test-only and has no dependency on the new water example. Solver storage regressions are retained in PhysicsSolverStorageTests.

## Executable viewport worlds

[The world contract](../components/worlds.md) combines canvas and physics ownership, default/shared/explicit viewport binding, transition notification and real native pixel verification. NavigationMap registers a real active authored-region map under ADR 0097; further navigation capabilities retain operation-specific prerequisites.

## Stationary surface velocity

StaticBody linear/angular surface velocity now executes on CPU and the developing
GPU solver. Its endpoint velocity drives contact response, point queries and
character platform carry while the pose stays fixed; AnimatableBody combines it
with actual target motion. [PhysicsSurfaceVelocityTests](../../tests/Electron2D.Tests/PhysicsSurfaceVelocityTests.cs)
verifies configuration, scene/server lifecycle, state views and warmed allocation.
This closes the stationary-surface prerequisite, not the wider completion boundary.

## Typed scene/server body state

PhysicsServer now shares transform, linear/angular velocity, sleeping, automatic
sleep permission and axis velocity with scene bodies and direct views. Raw
kinematic targets execute along a path on the next nonzero active step while
virtual surface velocity stays separate. PhysicsServerStateTests exercises CPU/GPU
state, reentry, wakeup, guards and warmed allocation. Remaining server identity,
filter getters, process/shape type and backend registration operations are still
separate gaps in the full completion boundary.

## Complete reported contact impulses

CPU scalar/SIMD and GPU solvers now expose total normal and signed tangent impulses.
The outer frame combines internal kinematic intervals, suppresses stale sleeping
impulses, and applies the same bounded deepest-point selection to direct state and
RigidBody monitoring. PhysicsContactImpulseTests checks momentum and allocation;
square-atlas tile identities now execute through the shared association contract and TileMapLayerTests.

Independent [resident sleep](../components/gpu-resident-sleep.md) now builds dynamic contact/joint components, sleeps eligible groups, wakes old/current neighbours after edits, and skips a device-confirmed unchanged inactive world. It retains no CPU island/velocity mirror. Internal [GPU CCD](../components/gpu-resident-ccd.md) now executes per-body ray/full-shape sweeps and impact intervals. [Resident joint policies](../components/gpu-resident-joints.md#joint-solver-policies) execute internally. Public backend selection, independent GPU joint/CCD adapters and sleep/contact-event publication now execute. Full conformance and network replay remain open.

[WorldBoundaryShape](../classes/WorldBoundaryShape.md) supplies an analytic infinite
half-plane throughout CPU scene/server collision, Area sensing, queries and CCD.
The independent GPU store implements matching geometry and shared query contracts;
public GPU binding now executes; full conformance remains open. [WorldBoundaryTests](../../tests/Electron2D.Tests/WorldBoundaryTests.cs)
checks far-away contacts, lifetime, sleep/edit/removal and zero warmed managed
allocation. [Measured overhead and traffic](../components/physics-shapes.md#infinite-world-boundaries)
do not establish large-scene throughput or rendered acceptance.

[World sleep policy](../components/physics-sleep.md) supplies typed project defaults
and public per-space linear/angular quiet thresholds plus duration. Public CPU and
independent GPU tests share strict threshold/delay semantics, body-size independence,
ordered wakes, invalid-state preservation and warmed allocation. Public GPU-world binding and contact/iteration settings now execute. Full family conformance remains open.

[World and shape contact correction](../components/physics-contact-policy.md) now
shares validated defaults, live policy propagation and inherited shape storage across
CPU and independent GPU solvers under [ADR 0098](../decisions/physics-contacts.md#adr-0098).
Public independent-GPU world selection now executes; networking remains open.

[Solver iteration settings](../components/physics-contact-policy.md#solver-iterations)
now control contact and joint convergence through typed project/world operations on
CPU and the independent GPU store. Default sixteen, live wake semantics, shared
numerical/temporal checks and bounded CPU stage storage replace the fixed one-pass
CPU policy. Public independent-GPU binding now executes; complete family conformance remains open.

[Contact history limits](../components/physics-contact-policy.md#contact-history-limits)
complete the typed space-parameter family: recycle radius and maximum separation
control local-anchor impulse reuse on CPU and independent GPU, while fresh geometry,
queries and event identities remain unchanged. Public GPU world binding now executes; the
remaining physics/server/networking capabilities are still open.

[Collision priority](../components/physics-contact-policy.md#collision-priority)
now weights penetration recovery through scene/server body operations and independent
resident GPU motion queries. Storage, owner/step validation and live edits preserve
collider identity and physical sleep/impulses. Public independent-GPU world binding now executes; complete family conformance and the other audited requirements remain open.


[Canvas association](../components/physics-queries.md#canvas-association) now connects
Body/Area attach/get operations, scene CanvasLayer entry/exit and exact point-query
filtering, including default zero and full 64-bit keys. Shared/independent worlds,
reparenting, live edits, raw colliders and warmed allocation have CPU/GPU checks.
The GPU fix removes the former zero-as-wildcard behavior; public GPU-world binding now executes; full canvas-family conformance remains open.

[Object associations](../components/physics-object-bindings.md) now connect typed Body/Area object assignment, sampled IDs and weak targets across direct/scene queries, motion, contacts and monitoring. Public GPU-world binding now executes; actual tile collider generation and networking remain open.

[Changed GPU body publication](../components/gpu-body-publication.md) compares observable
state on device and publishes generation-qualified changes/removals into common world
caches, avoiding downloads for unchanged bodies. This is a single-consumer latest-state
stream; lifecycle event logs and network replay remain open.

[Backend selection and shared worlds](../components/physics-backends.md) now expose
CPU/GPU World and server-space creation, startup fallback diagnostics, independent
resident body/shape/joint attachments and common query/event publication. Complete
conformance, performance and networking acceptance remain open.

## Completed-step diagnostics

[Physics statistics](../components/physics-statistics.md) publishes active body,
collision candidate and active constraint-island counts through PhysicsServer and
Performance. CPU and independent GPU worlds use their actual solver state; shared
worlds count once, inactive worlds are omitted, and cached reads neither allocate
nor synchronize a device. Public CPU/GPU and headless CPU lifetime/error checks are
recorded in the component; this does not close the remaining physics/network goal.

## Physical pointer events

[Physics picking](../components/physics-picking.md) connects CollisionObject and
Viewport through actual CPU/GPU point queries, with shape-index input, object/shape
hover, stored eligibility, canvas transforms, handled-state routing and cleanup.
It has common and native-window checks. Broader GPU conformance, tile/backend
extensions, networking and whole-application performance requirements stay open.

[Physics canvas diagnostics](../components/physics-debug.md) now connect Shape.Draw,
CollisionShape.DebugColor and SceneTree.DebugCollisionsHint to retained shape/cast/joint
geometry and bounded contact-point snapshots on both renderers and explicit
CPU/GPU worlds (ADR 0100). Contact color/limits are sampled typed settings; GPU
reads only selected coordinates. Square-atlas tile owners now execute; full extensions, networking and broader physics acceptance remain open.

GPU linear/angular setters now queue independent command components instead of
reading the other velocity component back first. Scene/server and direct-state
semantics, impulses, sleep and surface motion retain their shared contract.
[Component-write measurements](../components/gpu-resident-bodies.md#component-velocity-writes)
separate the complete small-scene speedup, capped real-window output and remaining
massive-scene/native/network acceptance.

GPU pre-step body publication is now conditional on actual activity consumers;
completion remains observable and caches distinguish intermediate selected reads
from change-publication state. [Public-world measurements up to 65,536 bodies](../components/physics-backends.md#conditional-body-publication)
report exact traffic, zero warmed physics allocations, phase costs and variable
native/window timing without treating this fixture as full performance acceptance.

GPU contact-report preparation now reads retained runtimes from attached scene and
server body lists instead of resolving every RID through the shared registry.
[Receiver preparation](../components/physics-backends.md#contact-receiver-preparation)
preserves live caps, non-rigid receivers, sensor exclusion and attachment lifetime.

Public body-parameter conformance now runs on explicit CPU and GPU worlds, including
material response, signed fields, persistent force/torque, omission and reattachment.
[Discrete GPU restitution](../components/gpu-contact-solver.md#solve-and-history)
preserves incoming impact speed before speculative braking; future and receding pairs
remain free of premature rebound. Shared API and warm allocation checks cover this path.

An internal [GPU replay checkpoint](../components/gpu-checkpoints.md) now retains
resident body/force/contact/sleep/joint history without CPU state readback for an
unchanged authored configuration. An internal [CPU solver checkpoint](../components/cpu-checkpoints.md)
now restores managed contact, joint, sleep, sensor and spatial history with stable
object identities. The internal [common-world checkpoint](../components/physics-space-checkpoints.md)
connects both kernels to scene/server backing state, pending forces/targets, direct
contact views and overlap history, with silent pose restoration and fixed-configuration
guards. `PhysicsServer.SpaceCreateCheckpoint` now exposes that local state as a
public `PhysicsCheckpoint`, paired with `SpaceGetTick`; empty active intervals
advance, skipped intervals retain time, and restoring rewinds only its world.
These verify local same-world replay only. Portable authoritative snapshots,
topology/lifecycle rewind, portable identity, network correction and
predicted/confirmed event handling remain open.

## Portable authoritative state

[PhysicsSnapshot and PhysicsSnapshotMap](../components/physics-snapshots.md) transfer
compatible physical and observer state between CPU/GPU worlds using caller-assigned
network IDs and generations. Capture/apply retain tick, contacts, one-way episodes,
sleep clocks, forces, targets and joint authoring while rebuilding private solver
caches. Matching authored worlds are required; local RIDs never become wire IDs.
The executable transport, lifecycle, prediction and confirmed-event integration
and its remaining acceptance limits are recorded in the contract audit.

[PhysicsNetwork](../components/physics-network-example.md) is the executable public-API
authority/prediction example: separate CPU/GPU processes, numbered input, incarnation
and owner validation, lifecycle correction, confirmed events and numerical
presentation interpolation. Its measured scope does not close rendered-window or
whole-engine performance acceptance.

[GPU integration batching](../components/gpu-contact-solver.md#integration-batching-measurements)
shares force/spatial and discrete solver/position/sleep submissions while preserving
finite-status validation, all substeps, contact reports and CCD interval decisions.
The diagnostic separated path measures the same kernels and workload.

[Resident contact colors](../components/gpu-contact-colors.md) improve dense-pile
convergence through a device schedule and compact color ranges. Public CPU/GPU
headless and native-window workloads now measure penetration and energy alongside
full-step latency, allocations and actual frame/tick rates. The corrected massive
world performance target remains open.

The same colored solver now uses packed contact ranges and compact shared body
velocities for contact/joint iteration, then restores owning records before reports,
history and integration. This adds device scratch and no host state mirror; compare
full-step and native-window results in the contact-layout report.
