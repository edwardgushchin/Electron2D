# Physics contract and backend audit

Last updated: 2026-10-09

[Per-body CCD](cpu-continuous-collision.md) now exposes shared Disabled/CastRay/CastShape policy through RigidBody and PhysicsServer. CPU checks solved trajectories before publication and retains force budgets and frame impulses across impact intervals; public independent-GPU binding and missing shape-family response remain open.

[Resident body-motion queries](gpu-resident-motion-queries.md) now execute supplied-pose recovery and sweeps on GPU, with reciprocal masks, one-way/ray policies, explicit exclusions and center-aware hit velocity. CPU full-contour recovery and directed containment now avoid internal polygon seams. Public GPU body-motion/CharacterBody binding remains open.

[Resident shape queries](gpu-resident-shape-queries.md) now execute intersections, contact pairs, deepest rest information and motion brackets over standalone leased geometry on GPU. CPU compound casts and directed-query containment now ignore internal decomposition seams. Public GPU query/world binding remains open.

[Resident ray and point queries](gpu-resident-queries.md) now search current device geometry with stable logical result caps, masks, exclusions and authored canvas filtering. Query-only tree preparation skips simulation pair generation; the public world/query adapter remains open.

[Resident contact publication](gpu-resident-reports.md) now retains complete outer-tick normal/friction impulses, transient contacts and capped per-body snapshots on the independent GPU store. Public direct-state/event projection remains open.

[Resident Area fields](gpu-resident-fields.md) now reduce directional/point gravity and independent damping on device, using current deduplicated sensor membership, body Combine/Replace policy and scoped changed-field waking. Mixed sensor/body pair work is distributed across receiver queries; public GPU field/event projection remains open.

[Kinematic target/surface separation](gpu-resident-kinematic.md) now executes on the independent GPU store, including full-shape paths against default-CCD dynamics, exact target/idle poses and contact/joint velocity separation. Public scene/server integration remains open.

## Baseline and scope

Source baseline: local `main` at `8e4f876d70782c7ec5473d3107c4c2d1b01ed902`.
The historical `119bd016` gap list is superseded by current source and coverage.
The repository pins Godot `4.7.2-stable`, commit
`ed1daf0bf001b61586d9930840f2f1394092c079`; this audit does not substitute a newer
online reference. [ADR 0054](../decisions/physics.md#adr-0054) defines the revised
CPU and independent GPU objective. This is an audit baseline, not a completion claim.

The [generated declaration ledger](../coverage/physics-status.md) takes all
descendants of collision objects, shapes, joints, direct-state and server families,
plus casts, motion/query parameters/results, material, world and physical-bone/jiggle
consumers. It follows their full inheritance chain and adds viewport picking,
scene debug, clock/project settings, physics process monitors and tile physics. Every selected open
declaration is listed with its current state/reason, including overloads and enums.
Shared ancestors are counted once rather than once per body class.
`tools/coverage/physics_report.py` defines the explicit scope; the ordinary
coverage classifier supplies all states. Its regression check verifies the report
against the class tables and catches omitted new descendants or duplicate IDs.

At this baseline, the physics families contain 892 declarations: 525 Implemented,
45 Partial, 241 Blocked, 41 Unimplemented and 40 Excluded. Shared inherited families
contain 491 declarations; selected cross-domain consumers contain 78. These counts
include class/enum rows and generic inherited API; they are not feature counts or
percentages. The generated ledger is authoritative when these baseline totals age.

Exclusion applies to the recorded representation or deprecated API, not an entire
capability. For example, numeric Variant selectors for body state are replaced by
five typed getter/setter pairs. Pin parameter selectors do not excuse missing
positional softness. Deprecated TileMap is not reintroduced; TileMapLayer supplies
the current collision-owner integration. Renderer particle effects and 3D solver
API are outside this contract. General tile authoring, shared scene/resource and
rendering prerequisites retain their owning coverage pages; missing inherited
operations remain open in the ledger.

## What current source actually runs

| Layer | Source evidence | Current boundary |
| --- | --- | --- |
| World | [PhysicsSpace](../../src/Servers/Physics/PhysicsSpace.cs) creates a B2WorldId unconditionally; EnableGPUSolver assigns callbacks on that same world | Every publicly created world uses the CPU host. There is no public independent GPU selection/fallback contract. |
| Scene bodies and shapes | [PhysicsColliderBackend](../../src/Servers/Physics/PhysicsColliderBackend.cs) now owns body/fixture IDs and creation/rebuild/destruction for PhysicsBody, Area and raw colliders | Ownership, creation and live pose/velocity/sleep operations are consolidated in the adapter; scene motion roles and operation values use engine types. Solved-pose notifications precede motion sampling. Backend state and shape construction still use Box2D; independent GPU ownership remains open. |
| Geometry resources | Shape.GetGeometry returns engine-unit endpoints/radius and borrowed contours; PhysicsShapeBackend owns CPU compilation and weak convex caches | Resources contain no vendor types; fixture, query and mass consumers share the CPU adapter. Polygon validation still compiles CPU hulls during authoring. Standalone collision consumes the neutral view but retains backend numeric types internally. Independent GPU geometry/contacts now execute internally; common public-world ownership remains open. |
| Server bodies | [PhysicsServerCollider](../../src/Servers/Physics/PhysicsServerCollider.cs) delegates body/fixture lifetime and live motion access to the shared component; [PhysicsBodyRuntime](../../src/Servers/Physics/PhysicsBodyRuntime.cs) now retains only engine-valued mass/field/force policy and delegates concrete state/forces to the attachment adapter | Authored server slots and scene slots use one fixture path, while resolved scene/server gravity and damping share PhysicsBodyRuntime. Remaining backend state/query/geometry operations need independent implementations behind the common identity/lifetime contract. |
| Joints | [PhysicsJointRuntime](../../src/Servers/Physics/PhysicsJointRuntime.cs) retains engine-valued frames/settings and identity; [PhysicsJointBackend](../../src/Servers/Physics/PhysicsJointBackend.cs) owns concrete handles, creation, updates and spring evaluation | Scene/runtime code has no vendor types. CPU joint kernels and GPU stage packets exist. Independent resident pin/groove/spring ownership and solving now execute internally; their shared public scene/server GPU adapter remains absent. |
| Queries/contacts | [PhysicsColliderBackend](../../src/Servers/Physics/PhysicsColliderBackend.cs) now owns body-view backend reads and traversal; [PhysicsDirectBodyState](../../src/Servers/Physics/PhysicsDirectBodyState.cs) retains engine values and current-view identity. Direct-space queries and contact publication still read vendor state | Views and queued callbacks use per-adapter attachment versions independent of native handles. World, query and joint binding still require independent GPU integration. |
| Independent device body/geometry state | [GPUPhysicsBodyStore](../../src/Servers/Physics/GPUPhysicsBodyStore.cs) creates no B2World and retains authoritative poses/velocities on GPU | Sparse edits, generations, device-preserving growth, [transient force lifetime](gpu-resident-forces.md), integration, shared geometry, GPU-derived AABBs, complete candidate pairs and resident narrow-phase contact points execute. Moving grids verify 4,096/65,536 records and unordered pair sets with zero warmed managed bytes. Contact impulse/material response, warm history and separate penetration correction now execute and retain complete loaded populations. Resident pin/groove/spring constraints, warm history and joint collision vetoes now execute and have analytic, lifecycle and allocation tests. Shared automatic/custom mass profiles and center-aware device motion/constraints now execute with public CPU profile comparisons. Device contact/joint components, sleep/wake, prior-neighbour invalidation and an unchanged inactive-world skip now execute internally; public sleep-event publication is still absent. Internal per-body ray/full-shape CCD, swept filtering, rotational checks and impact intervals now execute; public CPU/scene/server CCD now executes with full interval contact impulses; independent GPU public binding remains open. Internal joint bias/compliance/force/correction policies now execute with a shared original-substep CCD budget. Public CPU/stage-GPU joint policies now execute; explicit directed body exceptions now execute in the shared GPU pair filter with joint contributions, sensor separation, endpoint cleanup, sleep/wake and CCD checks; piece-level one-way episodes now retain accepted/rejected decisions, revision invalidation and CCD separation boundaries on the device; live role/integration policies now execute with ordered sparse edits, configured mass/force/CCD retention and once-per-tick damping checked against public CPU state; independent public-world integration remains missing; the solver report records the current performance ceiling. |
| GPU experiment | [GPU implementation status](gpu-physics.md) records resident trees/geometry/graphs and integration/contact/joint kernels | The solver remains hosted by Box2D with CPU mirrors, packing, validation and waits. Stage success is not independent-backend acceptance. |

The public RID/resource/object layer can retain its contract. No B2 type should
cross the public API. Internal data, graph topology, pair order, solver schedules
and numerical methods may change. Only documented public event sequencing and
query ordering are constraints; existing diagnostic hashes are not such promises.

## Rechecked historical items

| Requirement | Current evidence | Remaining obligation |
| --- | --- | --- |
| Static linear/angular surface velocity with stationary pose | StaticBody stores both velocities; scalar/SIMD and experimental GPU contacts consume them. PhysicsSurfaceVelocityTests covers contacts, queries, carrying, waking, packing and lifetime | Preserve on the independent GPU backend; not missing CPU work. |
| Full tangential impulse and contact cap selection | CPU scalar/SIMD constraints accumulate totalTangentImpulse; GPU packets publish totals. PhysicsSpace aggregates subdivided intervals. Direct state retains deepest points, replacing the first shallowest only for a strictly deeper candidate | Preserve frame totals, signs, ties and all public payloads on the new backend. Virtual tile owners remain missing. |
| Axis velocity, angular velocity, sleeping and can-sleep | PhysicsServer.API and PhysicsServer.BodyState route to shared PhysicsBodyRuntime; PhysicsServerStateTests exercises scene/server state and wake/lifetime guards | Already implemented on the CPU host and stage experiment; independent GPU preservation is open. |
| Joint RID creation/free and reconfiguration | PhysicsServerJointTests and PhysicsJointRuntime cover real pin/groove/spring kernels and lifetime | Old FreeRID text incorrectly listed all joints as absent. Shared public correction/softness/caps now execute with PhysicsJointPolicyTests; independent GPU-world integration remains open. |
| World/canvas identity, area audio and logical shape events | WorldTests, AudioSpatialTests and ShapePairEventTests support implemented coverage | Old triggers claiming absent world canvases, all audio or all shape events were stale. Collider canvas filtering and tile event owners remain open. |
| Directed separation-ray resource | SeparationRayShape and its factory, special queries, Area sensing and character recovery/snap execute | Ordinary CPU directed manifolds now provide material/impulse response, sleep and contacts; old stage-GPU mode uploads these host constraints. Independent public GPU binding remains open. |

Fresh audit verification rebuilds the checked revision and reruns the CPU surface,
body-state and contact-impulse suites. Their existing GPU variants exercise the
stage-hosted prototype, not an independent backend. Other named suites are source
evidence for their recorded coverage; they are not all rerun by this documentation
slice. No new whole-backend performance measurement is claimed.

The body-runtime boundary now moves concrete mass/state/force/contact operations to
PhysicsColliderBackend. Common body mass properties use scene units, and views plus
queued callbacks use a monotonic adapter attachment version. All 37 grouped CPU
suites, the complete current GPU suite and the parallel contact check pass; the
new same-world reattachment-during-dispatch test covers both scene and server bodies.
This removes common body-runtime vendor types, not the remaining CPU world/query
ownership or the need to bind the independent GPU store.

The live-state extraction additionally passes the 24-suite collider CPU group,
the current full GPU suite and PhysicsParallelTests (288 bodies, 256 fixed steps).
PhysicsBodyStateTests guards writes during solved-pose notifications; scene/server
field suites cover the merged gravity/damping path and their warmed allocation
checks. The earlier intermittent native GLib failure remains an open observation
in GPU status; passing later lifetime tests does not establish its cause.

Geometry extraction subsequently passes the expanded 28-suite CPU group and full
current GPU suite. Shared/copy polygon tests cover successful and rejected live
edits plus allocation-free warmed query reuse. Public shape declarations and the
open behavior counts are unchanged.

Joint extraction additionally passes the 31-suite CPU group and full current GPU
suite, including one shared public-response regression for sampled rotated frames,
world replacement, angular limits/motor and invalid replacement. The CPU spring
preflight/apply ordering is preserved in the adapter; its GPU execution remains open.

## Open behavior groups

This table groups the complete declaration ledger into implementation work. A
group remains open until all its applicable members, inherited consumers and both
backend paths pass the shared public contract. An Implemented declaration for the
current CPU host does not certify new geometry, tile owners or independent GPU use.

| Group | Missing behavior and affected families |
| --- | --- |
| Independent backend ownership and selection | Separate CPU/Box2D and resident GPU worlds; explicit selection independent of rendering; independently configurable startup fallback; observable requested/actual selection; defined failed-world state after a started GPU step; no hidden CPU replay. Extract scene/server body, shape, joint, direct-state and query consumers from B2 ownership. |
| Continuous collision | CPU RigidBody/PhysicsServer modes now execute from solved trajectories with force/motor budgets and full frame impulses. Resident GPU CCD shares the enum and executes independently. Remaining: public GPU binding and common cross-backend acceptance; WorldBoundaryTests now verifies analytic boundary geometry on both implementations. |
| Shapes | WorldBoundary resource, factory, type/data access, queries and body/Area response now execute on CPU and internally on GPU. Custom solver bias now executes under ADR 0098. Public GPU binding remains open. Extension-defined geometry must follow the accepted extension contract; a custom type enum is not a working custom-shape API. |
| Constraints | Joint bias, common caps, linear PinJoint softness and the project/space correction default now execute through scene/server settings on CPU and stage GPU, with independent resident GPU kernels. Connect the latter to the public world; GPU spring ownership/solving must not remain stage-hosted CPU work. |
| Solver/world settings | CollisionObject collision_priority plus server setter/getter; constraint bias and shape/world contact bias/slack now execute; remaining SpaceParameter capabilities include recycle radius, maximum separation and solver iterations. Linear/angular sleep thresholds and time-to-sleep now execute through typed public CPU operations and shared independent GPU policy (PhysicsSleepPolicyTests); public GPU binding remains open. Project-level defaults, separate-thread behavior and engine selection retain their exact coverage/adaptation rules. Hardcoded tolerances/substep counts do not implement writable settings. |
| Pointer interaction | CollisionObject input_pickable, InputEvent hook and event, object/shape mouse enter/exit hooks/events; PhysicsBody's differing default; viewport enable/first-only/sort and project picking default. Verify canvas transforms, shape indices, filtering, focus/removal and event lifetime. |
| Tiles and object identity | TileSet physics layers/filter/priority/material; TileData polygons, one-way margins and constant surface velocities; TileMapLayer body-RID lookup, collision enable/visibility, quadrant grouping and kinematic mode. Carry tile owners through Area/RigidBody object and shape events, direct contacts, direct-space queries, ray/shape casts and kinematic/motion results. Current scene CollisionObject/null-server-only projection is insufficient. |
| Server identity and queries | Body collision-layer/mask getters; body/Area attach/get object instance IDs and canvas instance IDs; point-query canvas filtering using actual collider association; ShapeGetType; ProcessInfo and Performance monitors for active objects/collision pairs/islands. Share semantics with scene owners and reject dead/foreign IDs correctly. |
| Debug drawing | Shape.Draw using existing canvas RIDs, CollisionShape.DebugColor, SceneTree.DebugCollisionsHint, scene ray/shape casts and joint drawing plus project debug colors/limits/outlines. Sandbox's intentionally removed debug UI does not remove engine API obligations. |
| Backend registration and extensions | PhysicsServer manager register/default/factory behavior; the whole server extension family and direct body/space state extension families, including step/sync/end-sync/flush/finish, exclusion helpers, callbacks and resource lifetime. Adapt to typed C# under ADRs 0004/0095 without exporting vendor handles or adding inert stubs. |
| Inherited contract | All remaining Partial/Blocked/Unimplemented declarations on Node, Entity/CanvasItem, Object/RefCounted/Resource, SkeletonModification and relevant shared ancestors remain listed separately. Existing role mappings, packing, ownership, process disable, worlds, transforms and callbacks must survive both implementations. Generic inherited gaps must not disappear behind a class marked Implemented. |

## Transfers, mirrors and waits

The old prototype's CPU mirrors serve its Box2D host, CPU queries/CCD, callback
access and independent validation. This explains their present use; it does not
justify retaining them in the independent backend. The following is a baseline
cost ledger, with absent measurements explicitly open. Values come from retained
stage-control runs documented in [GPU status](gpu-physics.md), not new audit runs.
Different stage runs must not be summed into a total transfer estimate.

| Transfer/synchronization | Existing use | Measured cost / remaining audit |
| --- | --- | --- |
| Full body inputs and numeric finalization results | Host supplies finalization inputs and consumes poses/sleep eligibility/fast-body outputs | 64 B input + 64 B result/body: 186,810,368 B for 1,459,456 body finalizations over 64 samples. Solver packing was 4.36–4.38 ms vs 3.51–3.52 ms in CPU-finalization controls; readback/publication 5.14–5.18 ms vs 4.79–4.84 ms. These are combined host-stage timings, not isolated shader timestamps. |
| Constraint coloring occupancy snapshot/results | Preserve host greedy order and occupancy mirror | 26,659,600 B, 405,412 operations/64 submissions; dispatch/readback 3.62–3.73 ms. This ordering is diagnostic, not a new-backend requirement. |
| Resident island graph publication plus complete host validation | Synchronize Box2D graph topology; detect corrupt result graphs | 99,232,864 B/64 samples (52,704,224 upload + 46,528,640 readback) in the recorded resident graph run. Later packed validation reduced host preparation/validation to about 1.8/4.2 ms; validation still visits the complete CPU graph. Public event needs do not require identical island lists. |
| CPU broad-phase tree and rank walk | CPU queries/CCD, sorting GPU candidates to exact host order | Per-stage counters exist; no complete current-revision bytes/time accounting for all mirror maintenance. Remove rank preservation unless a specific public ordering guarantee needs it. |
| Geometry/manifold/contact/joint uploads and result publication | Host authoring, callbacks, contact graph and solver stage boundaries | Partial stage metrics exist; cumulative full-step useful/capacity bytes and every wait are not yet audited. Residency must avoid unchanged uploads and full solver-state downloads. |
| Scene transforms, direct state, events and synchronous queries | Actual public consumers with freshness/lifetime requirements | Required publication/query semantics are retained. Their minimum payload, callback-induced waits, opt-out paths and total cost are not yet measured for an independent backend. |

The finalization pair used 65,537 Smash bodies (65,536 fragments and a striker),
32 warmup and 64 sampled headless impact steps. GPU-finalization runs averaged
175.77/179.13 ms (p95 252.68/254.57); CPU-finalization controls averaged
177.30/174.61 ms (p95 245.03/241.82), with zero measured warmed managed allocations.
Both sides used the other GPU stages. This is **not** complete CPU-versus-GPU
comparison, sustained all-awake throughput, window FPS or evidence of GPU advantage.
The retained artifacts are `bin/physics-sandbox/profile-Release-{gpu,cpu}-body-finalization-rounded-{a,b}.json`.

## Authoritative networking audit

The expanded goal additionally requires normal and client-server games, including
a headless CPU server correcting a GPU client. Existing [scene multiplayer](scene-multiplayer.md),
[replication](scene-replication.md) and [ENet](enet.md) are reusable prerequisites.
`SceneMultiplayer.Replication.cs` already has per-source spawn identities, a packet
sequence, authority/schema checks and typed property groups. Its publication loop
uses monotonic time and applies decoded properties; those mechanisms do not provide
fixed physics ticks, an authoritative physics restore point or input replay.
Current GPU snapshot counters concern internal upload mirrors, not portable game
snapshots. No current public physics capture/restore/reconciliation family was
found in this audit.

| Required integration | Existing prerequisite | Open acceptance |
| --- | --- | --- |
| Dedicated authority | Headless SceneTree stepping and CPU physics; transports do not require a renderer | Separate CPU server process with no window/renderer and GPU unavailable; command ownership/validation and fixed tick acknowledgements. |
| Portable identity/lifecycle | SceneMultiplayer has source/spawn IDs and typed codecs; RIDs are process-local | Explicit physics network identity across processes and correction, create/despawn generations, control transfer and late-join world state; never serialize vendor IDs or RIDs as identity. |
| Authoritative snapshots | Typed property encode/decode and packet budgets | Complete body/contact/one-way-side/sleep/joint state semantics, public capture/apply, validation and atomic error handling, backend-neutral wire payload, measured GPU publication. Pose/velocity property replication alone is insufficient. |
| Prediction/correction | Fixed scene physics lane and public forces/state operations | Bounded tick/input/state history, acknowledgement pruning, restore/replay of unconfirmed inputs and divergence recovery on CPU and GPU; documented numerical/reproducibility bounds. |
| Remote presentation | Existing transform interpolation infrastructure | Snapshot-buffer interpolation for remote objects and correction smoothing separated from authoritative/locally predicted physical state. |
| Events | Current owner-thread physical event ordering | Distinguish predicted/confirmed events and retain stable event identity across replay so collisions/triggers do not repeat game effects; reconcile despawn and authority changes. |
| Network testing/performance | Existing socket/ENet/replication tests and bounded buffers | Separate server/client processes under delay, jitter, loss and reordering; CPU-server/GPU-client collisions, joints, sleep/wake, late join and correction; executable public example; snapshot bytes, history memory and replay latency/allocation measurements. |

These capabilities extend the physics objective beyond reference declaration
coverage. They stay open here even if the generated API ledger eventually has no
open rows. [ADR 0094](../decisions/networking.md#adr-0094) and ADR 0054 record the
required integration; no new public API is declared by this documentation slice.

## Acceptance and continuation

1. Keep the CPU runtime working while extracting vendor ownership from common
   scene/server identities, geometry, body state, joints and queries. Choose the
   smallest connected extraction that preserves real scene and server behavior;
   do not publish a selector that merely relabels the existing hybrid experiment.
2. Implement authoritative GPU storage and parallel broad/contact/constraint,
   integration, sleep and CCD work. CPU publication serves actual public access;
   each remaining mirror/readback/wait needs a measured consumer-based rationale.
3. Close every open behavior group through shared executable public tests on both
   full backends. Exact checks apply to RID/object identity, lifetime, filters and
   promised event transitions. Numerical tests document units, timestep, error
   bounds and physical rationale: e.g. displacement vs collision tolerance,
   momentum vs accumulated impulse, joint drift vs compliance/time horizon. A
   single global epsilon or a Box2D byte hash cannot replace those assertions.
4. Exercise startup unavailability with fallback enabled/disabled, explicit CPU on
   capable hardware, GPU physics with each renderer, callback exceptions/reentry,
   partial device-step failure and disposal. Verify real rendering separately
   from headless simulation and distinguish platform/hardware acceptance.
5. Benchmark full backends at the same revision/workload with warmup, body/contact
   counts, awake/sleeping population, substeps/settings, seeds and environment.
   Record full-step mean/median/p95/p99/max, CPU preparation/publication, GPU work,
   transfers and wait distributions, owner/all-thread managed allocations. Check
   physical invariants over long runs; require zero warmed engine allocations and
   measurable GPU benefit without reducing bodies or features. Single-stage
   controls remain labeled diagnostics. No unmeasured mirror/wait is free.

The goal remains open until both complete implementations are publicly selectable,
the applicable contract is closed and the semantic, failure, allocation and full
   performance gates pass. This audit changes no runtime behavior or public API.

World contact bias/slack and Shape.CustomSolverBias now execute under [ADR 0098](../decisions/physics-contacts.md#adr-0098), with policy/storage/response tests on CPU and independent resident GPU. Contact recycling/max separation, iteration controls and public independent-GPU binding remain open. This addition does not close the full backend or networking goal.
