# Physics contract and backend audit

Last updated: 2026-10-08

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
| Server bodies | [PhysicsServerCollider](../../src/Servers/Physics/PhysicsServerCollider.cs) delegates body/fixture lifetime and live motion access to the shared component; [PhysicsBodyRuntime](../../src/Servers/Physics/PhysicsBodyRuntime.cs) still implements vendor state/forces | Authored server slots and scene slots use one fixture path, while resolved scene/server gravity and damping share PhysicsBodyRuntime. Remaining backend state/query/geometry operations need independent implementations behind the common identity/lifetime contract. |
| Joints | [PhysicsJointRuntime](../../src/Servers/Physics/PhysicsJointRuntime.cs) stores B2 joint/body IDs and constructs revolute, wheel or filter joints | CPU joint kernels and GPU stage packets exist; independent GPU joint ownership and spring execution are absent. |
| Queries/contacts | [PhysicsBodyRuntime.View](../../src/Servers/Physics/PhysicsBodyRuntime.View.cs) now owns body-view backend reads and traversal; [PhysicsDirectBodyState](../../src/Servers/Physics/PhysicsDirectBodyState.cs) retains engine values and current-view identity. Direct-space queries and contact publication still read vendor state | Body-view extraction preserves existing semantics; backend world, query and joint ownership remain coupled and require further work. |
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
| Joint RID creation/free and reconfiguration | PhysicsServerJointTests and PhysicsJointRuntime cover real pin/groove/spring kernels and lifetime | Old FreeRID text incorrectly listed all joints as absent; correction/softness/caps still missing. |
| World/canvas identity, area audio and logical shape events | WorldTests, AudioSpatialTests and ShapePairEventTests support implemented coverage | Old triggers claiming absent world canvases, all audio or all shape events were stale. Collider canvas filtering and tile event owners remain open. |
| Directed separation-ray resource | SeparationRayShape and its factory, special queries, Area sensing and character recovery/snap execute | Old server text incorrectly described the whole family as absent. Ordinary dynamic response remains incomplete. |

Fresh audit verification rebuilds the checked revision and reruns the CPU surface,
body-state and contact-impulse suites. Their existing GPU variants exercise the
stage-hosted prototype, not an independent backend. Other named suites are source
evidence for their recorded coverage; they are not all rerun by this documentation
slice. No new whole-backend performance measurement is claimed.

The live-state extraction additionally passes the 24-suite collider CPU group,
the current full GPU suite and PhysicsParallelTests (288 bodies, 256 fixed steps).
PhysicsBodyStateTests guards writes during solved-pose notifications; scene/server
field suites cover the merged gravity/damping path and their warmed allocation
checks. The earlier intermittent native GLib failure remains an open observation
in GPU status; passing later lifetime tests does not establish its cause.

## Open behavior groups

This table groups the complete declaration ledger into implementation work. A
group remains open until all its applicable members, inherited consumers and both
backend paths pass the shared public contract. An Implemented declaration for the
current CPU host does not certify new geometry, tile owners or independent GPU use.

| Group | Missing behavior and affected families |
| --- | --- |
| Independent backend ownership and selection | Separate CPU/Box2D and resident GPU worlds; explicit selection independent of rendering; independently configurable startup fallback; observable requested/actual selection; defined failed-world state after a started GPU step; no hidden CPU replay. Extract scene/server body, shape, joint, direct-state and query consumers from B2 ownership. |
| Continuous collision | RigidBody/PhysicsServer per-body Disabled, CastRay and CastShape. A global continuous flag or bullet bit does not implement three modes. Preserve other bodies' policies, moving/rotating geometry, fast kinematics, material/contact/event semantics and dynamic separation-ray interaction. GPU CCD must execute on GPU. |
| Shapes | WorldBoundary resource, factory, type, data, queries and body/Area response; complete dynamic SeparationRay impulses, friction/restitution, sleep and contact data; custom solver bias; complete shape-type reporting. Extension-defined geometry must follow the accepted extension contract; a custom type enum is not a working custom-shape API. |
| Constraints | Joint bias and common force/impulse/correction-speed caps; positional PinJoint softness; apply equivalent meanings across pin/groove/spring and scene/server settings. GPU spring force/preflight, constraint lifetime and solving cannot remain CPU-only. Existing angular spring parameters are not positional compliance. |
| Solver/world settings | CollisionObject collision_priority plus server setter/getter; shape custom bias; all nine SpaceParameter capabilities: recycle radius, maximum separation, allowed penetration, contact/constraint bias, linear/angular sleep thresholds, time-to-sleep and solver iterations. Project-level defaults, separate-thread behavior and engine selection retain their exact coverage/adaptation rules. Hardcoded tolerances/substep counts do not implement writable settings. |
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
| Authoritative snapshots | Typed property encode/decode and packet budgets | Complete body/contact/sleep/joint state semantics, public capture/apply, validation and atomic error handling, backend-neutral wire payload, measured GPU publication. Pose/velocity property replication alone is insufficient. |
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
