# Physics contract and backend audit

Last updated: 2026-10-10

[Public joint conformance](physics-joint-policies.md#public-cpu-gpu-conformance) now runs
pin/groove/spring, server identity/lifecycle and solver policies through explicitly
selected CPU and independent GPU worlds. GPU motion queries now honor joint vetoes;
finite spring caps and complete decay handle extreme coefficients before range
validation. Full physics, platform and performance acceptance remain open.

[Changed body publication](gpu-body-publication.md) now supplies compact device-compared
state to the shared GPU world, with generation/removal semantics and measured
traffic/waits. [Public backend selection](physics-backends.md), portable snapshots and
the [separate-process network example](physics-network-example.md) now execute;
full conformance and performance acceptance remain open.

[GPU integration batching](gpu-contact-solver.md#integration-batching-measurements)
now removes eight integration fences from an ordinary four-substep discrete tick.
The public independent-pair benchmark records 15 rather than 23 submissions, exact
status traffic and zero warmed managed allocations. This preserves kernels,
iterations, reports, sleep and CCD decisions; dense-pile and real-window performance
targets are still open.

[Dense contact verification](gpu-contact-colors.md) now detects excessive particle
penetration through the full public CPU/GPU path and an actual native window.
Device contact coloring fixes the observed 65,536-body Jacobi collapse without
reducing population or iterations. Colored contact solving, including joint-world
integration, remains GPU-resident. Overflow schedules use complete Jacobi; their
convergence and the corrected dense-world 60 Hz/FPS target remain open.

Packed iterative GPU state now reduces repeated scattered contact/body reads without
changing the solver equations or iteration count. Contact/joint coupling, reports,
checkpoints and portable snapshots are exercised with that layout. Its measured
benefit and retained device capacity are recorded with the dense-world workload;
this internal storage change closes no additional public declaration gap.

[GPU host preparation](gpu-host-preparation.md) now synchronizes authored integration
edits before shared pre-step publication and reads current GPU pose bases directly.
The 32-body live-policy/callback regression reduces submissions from 107 to 11
without changing delivered velocities. Public CPU/GPU live-edit, transform and
CCD checks cover this internal optimization; no declaration state changes.

[Per-body CCD](cpu-continuous-collision.md) now exposes shared Disabled/CastRay/CastShape policy through RigidBody and PhysicsServer. CPU checks solved trajectories before publication and retains force budgets and frame impulses across impact intervals; the independent public backend now receives the same CCD policy. Full cross-backend shape-family acceptance remains open.

[Resident body-motion queries](gpu-resident-motion-queries.md) now execute supplied-pose recovery and sweeps on GPU, with moving-mask/target-layer filtering, one-way/ray policies, explicit exclusions and center-aware hit velocity. CPU full-contour recovery and directed containment now avoid internal polygon seams. Public GPU body-motion and CharacterBody now route to these kernels; full CharacterBody conformance remains open.

[Resident shape queries](gpu-resident-shape-queries.md) now execute intersections, contact pairs, deepest rest information and motion brackets over standalone leased geometry on GPU. CPU compound casts and directed-query containment now ignore internal decomposition seams. Public GPU direct-space and scene casts now use these kernels.

[Resident ray and point queries](gpu-resident-queries.md) now search current device geometry with stable logical result caps, masks, exclusions and authored canvas filtering. Query-only tree preparation skips simulation pair generation; the public GPU world/query adapter now uses these kernels.

[Resident contact publication](gpu-resident-reports.md) now retains complete outer-tick normal/friction impulses, transient contacts and capped per-body snapshots on the independent GPU store. Public GPU direct-state/contact-event projection now uses these reports.

[Resident Area fields](gpu-resident-fields.md) now reduce directional/point gravity and independent damping on device, using current deduplicated sensor membership, body Combine/Replace policy and scoped changed-field waking. Mixed sensor/body pair work is distributed across receiver queries; public GPU field/event projection now executes through the shared world.

[Kinematic target/surface separation](gpu-resident-kinematic.md) now executes on the independent GPU store, including full-shape paths against default-CCD dynamics, exact target/idle poses and contact/joint velocity separation. Public scene/server target and surface adapters now use the resident store.

The built-in world ownership boundary now executes through a fresh internal CPU/GPU implementation object. It owns solver resources and complete interval dispatch, with independent world identities and failure-aware teardown. [Ownership checks](physics-backends.md#runtime-flow-and-ownership) cover 512 bodies, 64 warmed full steps at zero owner/all-thread managed allocation, native release despite an injected worker cleanup error and no-device CPU/fallback startup. This does not implement the public manager, complete server/direct-state extension family or custom geometry; their census rows remain open.

## Baseline and scope

Original audit baseline: `8e4f876d70782c7ec5473d3107c4c2d1b01ed902`.
Current filter review continues local `main` at `36d8dfcf3f5fedb2414446926cb1bd162c328109` plus the directional filter change described below.
The historical `119bd016` gap list is superseded by current source and coverage.
The repository pins Godot `4.7.2-stable`, commit
`ed1daf0bf001b61586d9930840f2f1394092c079`; this audit does not substitute a newer
online reference. [ADR 0054](../decisions/physics-backends.md#adr-0054) defines the revised
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
| World | [PhysicsSpace](../../src/Servers/Physics/PhysicsSpace.cs) selects a CPU B2World or independent resident store; PhysicsServer and World expose requested/actual backend and startup fallback | CPU remains default. GPU creates no B2World. Full conformance, platform and performance acceptance remain open. |
| Scene bodies and shapes | [PhysicsColliderBackend](../../src/Servers/Physics/PhysicsColliderBackend.cs) owns CPU fixture IDs or GPU body/shape handles, current attachment versions and observable state | Both use the common scene lifecycle. GPU solved-pose publication, motion/query routing, contacts and Area callbacks execute. The complete existing scene suite still needs cross-backend acceptance. |
| Geometry resources | Shape.GetGeometry supplies engine-unit endpoints/radius and borrowed contours to CPU compilation or resident geometry | Shared resources, owner slots, RID/object/canvas identity and rebuild/disposal feed both implementations. Authoring validation may use CPU geometry; runtime GPU queries and contacts use device geometry. |
| Server bodies | PhysicsServerCollider and PhysicsBodyRuntime delegate lifetime/state/forces to the selected attachment adapter | Public raw GPU bodies now use resident poses, mass, force/field policy, contacts and queries. Shared server scenarios execute on both backends; full parameter-family conformance remains open. |
| Joints | PhysicsJointRuntime retains engine-valued frames/settings; PhysicsJointBackend selects CPU handles or independent resident pin/groove/spring handles | Public scene/server settings now reach either implementation. Shared public pin/motor, groove, spring and reattachment tests pass; full joint-policy acceptance on the public GPU path remains open. |
| Queries/contacts | Direct-space/body state and scene motion/casts route through CPU operations or resident kernels/reports | Public GPU queries return common RID/object/slot metadata; direct-state contacts and queued events reuse attachment versions and common dispatch. Full cross-family conformance remains open. |
| Independent device body/geometry state | [GPUPhysicsBodyStore](../../src/Servers/Physics/GPUPhysicsBodyStore.cs) retains authoritative state and the complete independent device simulation | Sparse edits, geometry, contacts, solver, joints, mass, sleep, CCD, fields and queries execute. Public worlds now consume compact state publication and capped contact reports. Portable snapshots and the separate-process network example execute; full costs, device profiles and exhaustive conformance remain open. |
| GPU experiment | [GPU implementation status](gpu-physics.md) retains historical stage measurements | The older stage host still uses Box2D mirrors as a diagnostic control. The selectable independent GPU world does not use that host. Stage success is not full-backend acceptance. |

The public RID/resource/object layer can retain its contract. No B2 type should
cross the public API. Internal data, graph topology, pair order, solver schedules
and numerical methods may change. Only documented public event sequencing and
query ordering are constraints; existing diagnostic hashes are not such promises.

## Rechecked historical items

| Requirement | Current evidence | Remaining obligation |
| --- | --- | --- |
| Static linear/angular surface velocity with stationary pose | StaticBody stores both velocities; scalar/SIMD and experimental GPU contacts consume them. PhysicsSurfaceVelocityTests covers contacts, queries, carrying, waking, packing and lifetime | Preserve on the independent GPU backend; not missing CPU work. |
| Full tangential impulse and contact cap selection | CPU scalar/SIMD constraints accumulate totalTangentImpulse; GPU packets publish totals. PhysicsSpace aggregates subdivided intervals. Direct state retains deepest points, replacing the first shallowest only for a strictly deeper candidate | Preserve frame totals, signs, ties and all public payloads on the new backend. Square atlas tile owners now execute through the shared association path. |
| Axis velocity, angular velocity, sleeping and can-sleep | PhysicsServer.API and PhysicsServer.BodyState route to shared PhysicsBodyRuntime; PhysicsServerStateTests exercises scene/server state and wake/lifetime guards | Already implemented on the CPU host and stage experiment; independent GPU preservation is open. |
| Joint RID creation/free and reconfiguration | PhysicsServerJointTests and PhysicsJointRuntime cover real pin/groove/spring kernels and lifetime | Old FreeRID text incorrectly listed all joints as absent. Shared public correction/softness/caps now execute with PhysicsJointPolicyTests; independent GPU-world integration now executes; full public joint-policy conformance remains open. |
| World/canvas identity, area audio and logical shape events | WorldTests, AudioSpatialTests and ShapePairEventTests support implemented coverage | Old triggers claiming absent world canvases, all audio or all shape events were stale. Collider canvas filtering and square atlas tile owners now execute on both public backends; remaining tile authoring/network topology retains its ledger. |
| Directed separation-ray resource | SeparationRayShape and its factory, special queries, Area sensing and character recovery/snap execute | Ordinary CPU directed manifolds now provide material/impulse response, sleep and contacts; old stage-GPU mode uploads these host constraints. Independent public GPU binding now executes; full cross-family conformance remains open. |

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
| Independent backend ownership and selection | Public CPU/Box2D and resident GPU worlds now execute with rendering-independent selection, startup-only fallback and requested/actual reporting. CPU startup without a device and failed GPU-step/no-replay teardown are exercised in physics-backends. Full conformance and performance acceptance remain open. |
| Continuous collision | Public CPU/GPU RigidBody/PhysicsServer modes now execute with force/motor budgets, frame impulses, directed-ray contacts and per-moving-body masks. Common CCD, ray and boundary suites exercise both implementations; wider cross-family trajectories and long-run acceptance remain open. |
| Shapes | WorldBoundary, SeparationRay, logical type/data access, body/Area response, resource queries and custom solver bias now execute on both public backends. Extension-defined geometry remains dependent on a working extension contract; a reserved enum identity is not custom-shape support. |
| Constraints | Joint bias, common caps, linear PinJoint softness and the project/space correction default now execute through scene/server settings on CPU and stage GPU, with independent resident GPU kernels. The public GPU adapter owns resident constraints. The shared policy, scene pin/groove/spring and server family suites now pass on explicitly selected CPU/GPU worlds, including joint motion vetoes and extreme finite spring caps. Broader coupled workloads, inherited and extension contracts retain their independent gates. |
| Solver/world settings | CollisionObject collision_priority plus server getter/setter now execute in weighted motion recovery on public CPU and independent GPU; constraint bias and shape/world contact bias/slack now execute; solver iteration counts also execute through shared typed policy; all nine SpaceParameter capabilities now execute through concrete typed operations, including geometric contact recycling/separation. Linear/angular sleep thresholds and time-to-sleep now execute through typed public CPU operations and shared independent GPU policy (PhysicsSleepPolicyTests); public GPU binding now executes; full cross-family conformance remains open. Project-level defaults, separate-thread behavior and engine selection retain their exact coverage/adaptation rules. Hardcoded tolerances/substep counts do not implement writable settings. |
| Pointer interaction | [Completed under ADR 0099](physics-picking.md): scene callbacks/signals and differing defaults, viewport enable/first-only/sort and root project default, canvas/world routing, GUI/capture/exit, mutation/error/lifetime and warmed allocation. GPU queries now transfer predicted hit prefixes and exact missing tails; [measured picking cost](physics-picking.md#current-complete-frame-cost) keeps overall frame/FPS acceptance separate. |
| Tiles and object identity | TileSet physics layers/filter/priority/material; TileData polygons, one-way margins and constant surface velocities; TileMapLayer body-RID lookup, collision enable/visibility, quadrant grouping and kinematic mode. Carry tile owners through Area/RigidBody object and shape events, direct contacts, direct-space queries, ray/shape casts and kinematic/motion results. ADR 0101 now implements these policies for square atlas tiles, including actual generated bodies and owner projections through all listed consumers; TileMapLayerTests runs CPU/GPU and native rendering. Remaining general tile capabilities and tile-specific network topology remain open. |
| Server identity and queries | BodyGetCollisionLayer/Mask and shared scene/server/tile filter setters now execute under ADR 0102, with guarded authoring reads, immediate query edits, next-step monitoring and unchanged-assignment wake. Typed object/canvas associations, ShapeGetType, completed ProcessInfo and three Performance monitors now execute. Wider extension/state semantics and full conformance remain open. |
| Debug drawing | Authored shape/cast/joint and contact-point diagnostics now execute through retained canvas commands, including live hints, colors/outlines and bounded Space contact snapshots (physics-debug). Square atlas tile overlays execute under ADR 0101. Extension diagnostics, wider visual/device and full-frame performance acceptance remain open. |
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
Changed-body publications are observable latest state, not replay history. An
internal [GPU checkpoint](gpu-checkpoints.md) now restores device-local motion,
forces, contacts, sleep and joint history within a fixed authored configuration.
A separate internal [CPU checkpoint](cpu-checkpoints.md) now restores persistent
solver/contact/sleep/joint/sensor state with stable authored identities. The internal
[common-world checkpoint](physics-space-checkpoints.md) now also restores attached
scene/server forces, targets, observer contacts and overlap history without callbacks.
The public `PhysicsCheckpoint` factory and world-local tick counter now expose
that local replay point to game code. [Portable snapshots](physics-snapshots.md) now
add bounded capture/apply between compatible CPU/GPU worlds and explicit identity
bindings. The [PhysicsNetwork example](physics-network-example.md) now exercises authoritative
lifecycle and reconciliation in separate processes. General game-state rollback,
rendered presentation and the complete performance/platform gates remain open.

| Required integration | Existing prerequisite | Open acceptance |
| --- | --- | --- |
| Dedicated authority | PhysicsNetwork CPU authority executes separate-process fixed ticks with display/GPU unavailable and validates numbered peer-owned commands | The required Linux headless authority and input validation scenario is verified; broader host/game deployment is outside that evidence. |
| Portable identity/lifecycle | PhysicsNetwork manifests verify portable IDs/generations, removal/recreation, control epochs and late join | The required lifecycle scenario is verified. Other game recipes and speculative client spawning need application policy; keep RIDs and backend identities private. |
| Authoritative snapshots | Public PhysicsSnapshot/PhysicsSnapshotMap with portable IDs, CPU/GPU capture/apply, observer history, one-way/sleep state and measured publication | PhysicsNetwork verifies token admission, lifecycle correction and CPU-authority/GPU-client convergence under adverse delivery. Internet authentication and other game authoring policies are outside the example. |
| Prediction/correction | Fixed scene physics lane, public forces/state operations and tick-qualified local PhysicsCheckpoint | The example now verifies bounded input replay and injected-divergence correction for CPU/GPU clients. Cross-platform numerical verification remains open; custom nonphysical game state needs application rollback policy. |
| Remote presentation | Existing transform interpolation infrastructure | The example numerically verifies a 16-snapshot interpolation ring and local smoothing without moving solved bodies; rendered/network-window acceptance remains open. |
| Events | Current owner-thread physical event ordering | PhysicsNetwork verifies phase-separated callbacks and exactly-once confirmed contact/Area records across replay, generations and ownership. The checked contact/Area effects satisfy the example; other game effects need their own policy and evidence. |
| Network testing/performance | Existing socket/ENet/replication tests and bounded buffers | PhysicsNetwork covers these scenarios with real ENet processes and application-level impairment. Routed/OS-level loss, broader game workloads, cross-platform runs and the full GPU/window performance targets remain open. |

These capabilities extend the physics objective beyond reference declaration
coverage. Remaining acceptance gates stay open even if the generated API ledger eventually has no
open rows. [ADR 0094](../decisions/networking.md#adr-0094) and ADR 0054 record the
required integration; the public local-checkpoint prerequisite does not close the
portable authoritative networking rows.

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

World contact bias/slack and Shape.CustomSolverBias now execute under [ADR 0098](../decisions/physics-contacts.md#adr-0098), with policy/storage/response tests on CPU and independent resident GPU. Iteration controls now execute with PhysicsSolverIterationTests; geometric contact recycling/max separation now execute with PhysicsContactPersistenceTests; public independent-GPU binding now executes. This addition does not close the full backend or networking goal.

## Pointer-input completion

[Physics picking](physics-picking.md) now implements CollisionObject input eligibility,
virtual callbacks and pointer signals, PhysicsBody's false default, and the three
Viewport picking policies on CPU/GPU. Native SDL delivery also executes with both
renderers. Remaining debug drawing, tile owners, backend extensions and network
requirements are unchanged. Metadata readback capacity remains a measured GPU
optimization target; this slice does not close the full physics goal.

## Directional filter review

[ADR 0102](../decisions/physics-filters.md#adr-0102) now supplies shared body
layer/mask getters/setters without geometry recompilation and removes the old
reciprocal-mask restriction from physical response and motion/CCD. The common
PhysicsFilterTests execute on CPU/GPU; TileMapLayerTests exercise generated
identities. Full details, numeric bounds and prepared allocation intervals are in
[filter verification](physics-filters.md). Older SeparationRay/CCD filter fixtures
that zeroed only a static target mask assumed reciprocal response; they now check
both the moving-mask rejection and the target-mask non-veto cases. RigidBody
contact-departure fixtures now reject both pair directions explicitly.

The pinned body-pair implementation also reports contacts between interacting
nonresponsive roles when reporting is enabled. The broad phase excludes two static
bodies; kinematic/static and kinematic/kinematic reporting now executes through the
existing CPU/GPU contact pipelines. [Report-only verification](physics-report-only.md)
covers either receiver, cap changes, frozen scene events, quiet activity, identity,
restore and warmed allocations. This does not close the server-extension or full
inherited contract.

## Explicit pose consumer, 2026-10-10

The owned `PhysicsServer.BodyGetTransform(space, bodies, transforms)` overload now provides an executable caller-owned pose batch. It keeps exact scalar scene/raw/static projection, validates the complete sequence before output mutation and preserves shared thread/solver/lifetime/failure guards. Dynamic raw GPU reads gather only 16-byte poses, independently of full motion/field/sleep publication. The CPU/GPU public suite checks compact counters, duplicates beyond resident population, replay/portable cache behavior and zero warmed allocation. The massive window fixture consumes this public API once per fixed tick; broader platform/native allocation and complete extension acceptance remain open.

## Backend extension audit, 2026-10-10

The pinned census contains two manager members, 140 server-extension members,
48 direct-body-state members and seven direct-space members, plus their four
owning types. No owning type/member deprecated metadata excludes these families;
the old one-way compatibility binding remains separately excluded. Current
PhysicsServer is a sealed static facade, direct states are sealed and RID allocation
is internal. Public typed query-result constructors now exist and preserve payload validation.
Shape still has an internal abstract geometry member. Selected world/collider/joint
ownership, world policy/capacity, completed statistics and selected full-step phases have been extracted, but registered third-party state/motion/query dispatch
is not implemented. These
are concrete integration prerequisites, not a reason to exclude extension behavior.
[ADR 0103](../decisions/physics-extensions.md#adr-0103) resolves the implementation
object/static-service role boundary and requires complete typed dispatch and
supporting construction/lifetime paths. Registration-only enum aliases and virtual
signatures with no scene/query consumer are rejected. The four extension-family
coverage pages remain Blocked until implementation and executable acceptance.
