# Physics contract and backend audit

[Tile owner projections and failure cleanup](tiles.md) now execute through explicit CPU/GPU tests, including object deduplication, shape/RID payloads, canvas and weak lifetime, scene/server motion and callback-failed replacement/retirement. The formerly tile-blocked Area, KinematicCollision, PhysicsTestMotionResult and point-query rows are reconciled against actual production paths. This closes those recorded implementation triggers; full inherited, numerical-extension, network/platform and performance acceptance remain open.

Last updated: 2026-10-13

[Common space bindings](physics-registered-adapter.md#common-space-bindings) now execute all four Body/Area space
operations on scene and raw RIDs, including explicit target simulation, scene
reclaim, partial-attachment restoration, source preflight and lifetime guards.
The public-only CPU/GPU/registered consumer retains zero warmed frame allocation.
Registered numerical response and final physical/network/performance acceptance remain open.

[World policy borrowing](physics-registered-adapter.md#borrowed-world-policies)
now rejects all nine typed space parameter writes and local activation during
selected numerical hooks, including equal-value writes before nested dispatch.
Initial/live/nested hooks, ordinary callback edits and warmed edited full frames
execute through the separate public-only consumer. The four extension parameter/
activation census rows now map to their actual guarded paths as Partial; arbitrary
registered numerical policy response and full extension acceptance remain open.

[Selected observation and collision policy](physics-backends.md#selected-observation-and-collision-policy)
now routes resolved fields, collision-exception invalidation/synchronization and
portable preparation/batch observation through the actual selected owner. Common
server/replay/map code retains engine identities and values without GPU handles
or snapshots. Named registration and typed external dispatch now execute; complete numerical-extension acceptance remains open.

[Selected checkpoint ownership](physics-backends.md#selected-checkpoint-ownership)
now keeps GPU publication reports, epochs and collider caches with resident solver
history. Common replay uses engine field values and guarded selected preparation,
capture, validation, restore and release. Named registration now executes; complete numerical-extension acceptance remains open.

[Shared joint inputs and ownership](physics-backends.md#shared-joint-inputs-and-native-ownership)
now pass complete engine-unit configuration values to selected constraints. CPU
spring scheduling resolves actual native attachments from its owner, including
wrapped implementations; matching retirement preserves replacement attachments.
Named registration and typed external dispatch now execute; complete numerical-extension acceptance remains open.

[Shared collider inputs](physics-backends.md#shared-collider-shape-inputs)
now project scene/raw body and Area authoring into one selected implementation
span. Exact resources, transforms, logical disabled indices and one-way policy
remain observable; source preflight, recursive preparation and scratch lifetime
are guarded. Named registration and typed external dispatch now execute; complete numerical-extension acceptance remains open.

[Selected lifecycle](physics-backends.md#selected-lifecycle-and-activity)
now dispatches owner initialization, atomic local activation, query/event flushing
and end-sync with guarded context. Empty active intervals reach that owner;
mandatory observers and cleanup survive selected hook errors. Public registration and the mapped lifecycle/statistics family execute; complete
external numerical-operation acceptance remains open.

[GPU collider ownership](physics-backends.md#gpu-collider-ownership) removes
resident handles and concrete GPU casts from the common collider facade.
Actual attachments stay in the resident store; selected wrappers retain publication,
Area/query/joint behavior and local/portable restoration. Identity/priority edits
reach selected hooks. Named registration and typed operation dispatch now execute; full numerical operation acceptance stays open.

[CPU collider ownership](physics-backends.md#cpu-collider-ownership) removes native
body/shape aliases and CPU implementation casts from shared scene/server/collider
types. Actual native storage belongs to the CPU world; wrapped collider operations
retain real contacts, Area/CCD/query behavior, retirement and portable state. This
closes an internal ownership bypass, not full public registered numerical-operation acceptance.

[Selected direct-state construction](physics-backends.md#selected-direct-state-construction)
now executes internal owner-created query/state extensions through World/server
access and real scene/raw callbacks. Binding, cache identity, contact snapshots,
failed factory rollback, later callback delivery and zero warmed allocation execute
on CPU/GPU/no-device. Named registration and typed server-operation dispatch now execute; complete
numerical extension acceptance remains open. The stale tile-collider-object reason was reconciled against the
existing CPU/GPU TileMapLayer contact and object-association tests.

[Same-revision dense performance](physics-current-performance.md) retains the
raw-server trials at their named revisions. The
[frozen 16,384-body monitored scene](physics-scene-performance.md#16384-body-refresh-at-f3983555-2026-10-12)
now passes headless/window physical gates and zero warm physics/frame managed
allocation: GPU physics median is 26.0/23.8 percent lower and actual window FPS
19.1 percent higher in the paired trials. Their manifests cover all 71 copied
runtime files; the earlier window run with replaced shared binaries is rejected.
These close no declaration gap: complete registered numerical-operation acceptance,
isolated repeatability and native/foreign acceptance remain open. The goal requires
measured GPU advantage and real-window FPS, without a sustained 60 Hz threshold.

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
convergence and final dense-world performance acceptance remain open.

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

The built-in world ownership boundary now executes through a fresh internal CPU/GPU implementation object. It owns solver resources and complete interval dispatch, with independent world identities and failure-aware teardown. [Ownership checks](physics-backends.md#runtime-flow-and-ownership) cover 512 bodies, 64 warmed full steps at zero owner/all-thread managed allocation, native release despite an injected worker cleanup error and no-device CPU/fallback startup. Public manager registration, factory-selected direct states and bounded custom annulus geometry now execute through the independent public-only consumer. Complete external numerical-operation and inherited acceptance remain open; this ownership check alone does not establish them.

## Baseline and scope

Original audit baseline: `8e4f876d70782c7ec5473d3107c4c2d1b01ed902`.
The directional filter review recorded local `main` at `36d8dfcf3f5fedb2414446926cb1bd162c328109` plus the directional filter change described below; later slices are recorded above and in the generated ledger.
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

At the original census baseline, the physics families contained 892 declarations: 525 Implemented,
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

## Current requirement audit

The complete generated ledger remains the per-declaration list. Current own
physics families contain 686 Implemented, 151 Partial and 55 Excluded rows,
with no own Blocked or Unimplemented rows. Shared inherited declarations still
contain 142 Partial, 19 Blocked and 87 Unimplemented rows after typed indexed property projections; cross-domain consumers
now contain 76 Implemented, two Partial and no Unimplemented rows after the
captured numerical-thread implementation. These are declaration counts,
not a claim that all numerical behaviors pass. The exact inherited names and
prerequisites remain in the ledger rather than disappearing from this audit.

Fresh source review distinguishes concrete omissions from acceptance limits:

- Viewport's On (1) default now agrees in construction and Node property metadata,
  including Window/SubViewport. Shared real-body CPU/GPU and native nested pixels
  verify the Off-parent boundary, explicit modes, camera/reset and packed/copy/
  duplicate/revert semantics. The former default mismatch is closed; it is not
  an outstanding dependency of the networking presentation contract.
- Project physics_engine now has its executable typed capture contract, separate
  opt-in startup fallback, CPU/GPU/registered numerical checks, actual no-device
  startup and both native rendering paths. Existing explicit worlds retain their
  choices. [Selection evidence](physics-backends.md#project-physics-selection)
  closes that omission. [run_on_separate_thread](physics-thread.md) now captures
  a typed policy and executes complete CPU/provider Solve on a retained numerical
  lane, with readonly input, owner publication and joined failure/lifetime barriers.
  Its row remains Partial: GPU/native numerical execution and scheduling performance
  have not been accepted. Ordinary retained CPU workers alone do not close it.
- Common frequency/catch-up/jitter project keys, Engine clock settings and physical
  frame/count/fraction/phase observations now have audited public CPU evidence.
  [Clock conformance](engine-runtime.md#verification) uses actual world ticks and
  travel, live frame-boundary capture, feature overrides, zero scale, event ordering/
  failure and strict 0/0 warmed full frames. The eleven declaration rows no longer
  have structural-only reasons. These clock checks do not verify the separate numerical lane
  or GPU/native cadence.
  The compared source is the pinned [clock member/reference document](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Engine.xml)
  and [fixed-frame signal document](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTree.xml), retaining the accepted typed clock and event adaptations.
- Independent registered multiple-circle/annulus slots now retain transformed
  placement and original indices, with shared query/response/Area/CCD/private-history
  and same-peer cap checks. The former last-shape/slot-zero model is removed.
  [Angular compound motion](physics-registered-adapter.md#angular-compound-motion)
  now verifies mass-center/point-velocity projection and coupled rotating circle
  slots on CPU and the independent provider in serial/separate numerical modes.
  GPU, general polygon/capsule/multiple-piece and wider coupled CCD acceptance stays
  open; [compound evidence](physics-registered-adapter.md#compound-slot-numerics)
  defines the bounded fixture and numerical tolerances.
- [Curved custom contacts](physics-registered-adapter.md#curved-custom-contacts)
  now check inner/outer annulus arcs, motor-generated motion, a rotating counterpart,
  complete frame points/impulses and private replay in serial/separate provider
  modes. Corrected non-touching geometry no longer overwrites a valid manifold
  point with zero. Arbitrary custom algorithms and longer coupled/network/native
  acceptance remain open.
- [Mass profiles across roles](physics-registered-adapter.md#mass-profiles-across-roles)
  now verify automatic/custom center retention, direct point velocity, inverse
  dynamics and actual scene/raw geometry through all four roles in CPU/registered
  serial/separate modes. CPU density-only updates reuse unchanged fixtures, removing
  122880 measured role-write bytes while preserving the same full-frame bracket.
  GPU/native and broader role/network/performance acceptance remain open.
- [Portable contact/motion history](physics-snapshots.md#portable-contact-and-motion-history)
  now verifies real CPU/registered impulse and point copies, local peer identities,
  body/raw Area history and character slides. Query-prepared character capture is
  supported; version 3 retains its preparation flag. Invalid local/remote logical
  slots reject before state/event mutation, with zero warm full-frame transfer
  allocation. Wider geometry/joint/current GPU/native/network acceptance remains open.
- [Capture topology verification](physics-snapshots.md#capture-after-topology-edits)
  now guards source contact/overlap/slide links before packet replacement. Five real
  removal/clear cases preserve previous packets and recapture after ordinary
  history refresh on CPU/registered serial/separate owners. General replacement/
  reindex and current GPU/native/network/performance acceptance remain open.
- [Same-count authoring validation](physics-snapshots.md#same-count-authoring-validation)
  now checks seven actual mismatched geometry/slot profiles in CPU/registered
  serial/separate transfers, unchanged destination state/events/input, and recovery
  with real queries and continued contact/Area history. Disabled primitive-kind
  validation does not establish active rectangle simulation. Producer history after
  same-count edits and wider geometry/GPU/native/network/performance gates stay open.
- Shared inherited Object/Node/Resource/CanvasItem and skeletal editor requirements
  remain with their typed-API, scheduling, script/editor or rendering prerequisites.
  Existing built-in physics tests do not establish those separate declaring APIs.
- [Portable body roles](physics-snapshots.md#portable-body-roles) verify CPU/registered
  scene/raw round trips, automatic/custom centers and exactly one interval of
  pending force/torque, continued motion and real logical-slot queries. Serial/
  separate warm full-frame transport cycles require 0/0 managed bytes. This bounded
  same-process circle fixture leaves collision/joint, broader geometry and current
  GPU/network/native/performance gates open.
- Long/coupled workload, network lifecycle and real-window/performance acceptance
  must retain their own revision, inputs, numerical bounds and live evidence.
  A historical stage mirror or a narrower unit test cannot replace the full path.
- [CPU-only network profiles](physics-network-example.md#cpu-only-and-separate-numerical-profiles)
  verify three actual impaired ENet processes through tick 360 with serial/separate
  numerical execution, portable correction/replay, lifecycle/sleep and exact-once
  confirmed effects. The instrumented host observes the actual CPU lane; prepared
  prediction/replay retains strict 0/0 owner/all-thread bytes. Current mixed-backend,
  native and final performance gates stay open.

The explicit goal requirements are routed as follows; a named suite is executable
coverage of its stated cases, not proof of arbitrary external algorithms.

| Goal requirement | Current executable contract / evidence | Remaining proof or work |
| --- | --- | --- |
| Independent selection, CPU without GPU, startup fallback, requested/actual identity and terminal started-GPU failure | physics-backends; WorldTests, PhysicsBackendOwnershipTests; named registry and lifecycle consumer | Wider platform/device profiles and complete external owner semantics. |
| Resident broad phase, contacts, constraints/joints, integration, sleep and CCD | Resident store/kernel components; GPUPhysicsTests and public CPU/GPU suites | Coupled long workloads and cost/capacity accounting for required publication/query/network reads. |
| CCD modes; stationary static linear/angular surfaces; boundary and directed-ray response | ContinuousPhysicsTests, BoundaryShapeTests, SeparationRayShapeTests, PhysicsSurfaceVelocityTests; shared response/grazing controls | Provider multi-slot/rotated/coupled numerical cases; no bitwise equality condition. |
| Joint bias/softness/force, impulse and correction caps; shape bias/priority; world policies | Shared PhysicsJointPolicyTests, PhysicsContactPolicyTests, PhysicsCollisionPriorityTests, PhysicsSleepPolicyTests and typed space operations | Separate-thread simulation and external numerical policy coverage. |
| Picking, canvas filtering, shape/debug drawing, colors and hints | Picking/debug/native suites, direct-space and tile tests | Unverified native platforms and applicable inherited renderer/editor rows remain separate. |
| Full frame tangent impulses, deepest caps/ties, tile owners and logical identities | PhysicsContactImpulseTests, body/direct-state tests, TileMapLayerTests, registered contact frames | General polygon/capsule/multiple-piece provider geometry; bounded multiple-circle slots and private history are verified. |
| Scene/raw server states, axis velocity, layers/masks, shape type, process info, object/canvas IDs and registration | Shared state/force/binding/query suites plus complete selected operation hooks and public-only consumer | Exact open inherited/extension rows in the ledger; no placeholder exemption. |
| Bodies, CharacterBody, shapes, queries, forces, materials, Area, joints and physical bones | Full existing suite explicitly exercises independent CPU/GPU variants | Uncovered coupled/long-run cases and remaining external geometry acceptance. |
| Headless CPU authority, fixed ticks, numbered input, portable cross-process identity and snapshots | Public PhysicsNetwork example, checkpoint/snapshot suites and no-GPU process checks | Same-revision final process runs and bounded packet/history costs. |
| Prediction, correction/replay, remote interpolation, physical/presentation separation and confirmed effects | NetworkSession, PhysicsNetwork process checks and native window captures | Viewport default boundary is now verified; broader gameplay/topology coverage remains scoped. |
| Spawn/removal, ownership changes, late join, impaired delivery, CPU server with GPU client and divergence recovery | Three actual ENet processes, generation/authority checks, joints/sleep/event assertions | Current workload results must include all positive counters and convergence rather than startup alone. |
| Full step distributions, transfers/waits/allocations, real-window FPS and network replay/history cost | Full monitored-scene pipeline and instrumented public network example | Same-revision results, physical gates, retained all-body/features and repeatability scope; stage-only metrics cannot satisfy it. |
| Zero warmed managed allocations and massive-workload GPU advantage | Strict owner/all-thread brackets in complete workloads | Measured advantage for the declared workload; revise architecture on an actual failure, not a diagnostic ordering mismatch. |

## What current source actually runs

| Layer | Source evidence | Current boundary |
| --- | --- | --- |
| World | [PhysicsSpace](../../src/Servers/Physics/PhysicsSpace.cs) selects a CPU B2World or independent resident store; PhysicsServer and World expose requested/actual backend and startup fallback | CPU remains default. GPU creates no B2World. Full conformance, platform and performance acceptance remain open. |
| Scene bodies and shapes | [PhysicsColliderBackend](../../src/Servers/Physics/PhysicsColliderBackend.cs) retains selected implementation ownership, common attachment versions and observable state; native IDs/handles belong to CPU/GPU collider implementations | Both use the common scene lifecycle. The complete existing body/shape/owner/disable/freeze/character/physical-bone suites run on explicitly selected public CPU/GPU worlds. Inherited gaps and complete registered numerical geometry remain open. |
| Geometry resources | Shape.GetGeometry supplies engine-unit endpoints/radius and borrowed contours to CPU compilation or resident geometry | Shared resources, owner slots, RID/object/canvas identity and rebuild/disposal feed both implementations. Authoring validation may use CPU geometry; runtime GPU queries and contacts use device geometry. |
| Server bodies | PhysicsServerCollider and PhysicsBodyRuntime delegate lifetime/state/forces to the selected attachment adapter | Explicit CPU/GPU suites cover typed scene/raw state, force, mass, integration, contact, binding and lifetime operations. Named registered dispatch also executes. Complete independent provider geometry/policy semantics remain open rather than treating all parameter setters as absent. |
| Joints | PhysicsJointRuntime retains engine-valued frames/settings; PhysicsJointBackend delegates to selected CPU/GPU constraint owners | Shared public scene/server policy, pin/groove/spring, motor/stops, bias/softness/caps, reattachment and motion-veto suites execute on both independent built-ins. The bounded registered pin/groove consumer also executes physical checks; broader provider coupled geometry remains open. |
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
| Static linear/angular surface velocity with stationary pose | StaticBody stores both velocities; scalar/SIMD and experimental GPU contacts consume them. PhysicsSurfaceVelocityTests covers contacts, queries, carrying, waking, packing and lifetime | The complete shared PhysicsSurfaceVelocityTests now runs in the full CPU/GPU suite with explicit backend assertions; full physics/performance acceptance remains open. |
| Full tangential impulse and contact cap selection | CPU scalar/SIMD constraints accumulate totalTangentImpulse; GPU packets publish totals. PhysicsSpace aggregates subdivided intervals. Direct state retains deepest points, replacing the first shallowest only for a strictly deeper candidate | Preserve frame totals, signs, ties and all public payloads on the new backend. Square atlas tile owners now execute through the shared association path. |
| Axis velocity, angular velocity, sleeping and can-sleep | PhysicsServer.API and PhysicsServer.BodyState route to shared PhysicsBodyRuntime; PhysicsServerStateTests exercises scene/server state and wake/lifetime guards | The complete shared PhysicsServerStateTests now executes on explicitly selected public CPU/GPU worlds; broader inherited/extension and final acceptance remain open. |
| Joint RID creation/free and reconfiguration | PhysicsServerJointTests and PhysicsJointRuntime cover real pin/groove/spring kernels and lifetime | Old FreeRID text incorrectly listed all joints as absent. Shared public correction/softness/caps now execute with PhysicsJointPolicyTests; independent GPU-world integration now executes; full public joint-policy conformance remains open. |
| World/canvas identity, area audio and logical shape events | WorldTests, AudioSpatialTests and ShapePairEventTests support implemented coverage | Old triggers claiming absent world canvases, all audio or all shape events were stale. Collider canvas filtering and square atlas tile owners now execute on both public backends; remaining tile authoring/network topology retains its ledger. |
| Directed separation-ray resource | SeparationRayShape and its factory, special queries, Area sensing and character recovery/snap execute | Ordinary CPU directed manifolds now provide material/impulse response, sleep and contacts; old stage-GPU mode uploads these host constraints. Independent public GPU binding now executes; full cross-family conformance remains open. |

The original extraction audit exercised then-current CPU and stage-host controls.
Current shared public suites explicitly select independent CPU/GPU worlds; their
legacy stage controls are separate diagnostics. The full executable at the source
state committed as 7c37a291 passes both built-in variants and the registered consumer
with source/runtime fingerprints preserved in
bin/physics-registered-contact-frames/2026-10-13/final-verified/. That functional
run does not itself establish whole-backend performance or platform acceptance.

The body-runtime boundary now moves concrete mass/state/force/contact operations to
PhysicsColliderBackend. Common body mass properties use scene units, and views plus
queued callbacks use a monotonic adapter attachment version. All 37 grouped CPU
suites, the complete current GPU suite and the parallel contact check pass; the
new same-world reattachment-during-dispatch test covers both scene and server bodies.
At that extraction stage, common body-runtime vendor types were removed before
world/query ownership and independent GPU binding were completed. The selected
owners and public built-in paths listed above supersede those historical gaps.

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
preflight/apply ordering was preserved during extraction. Current public GPU spring
and policy execution is covered by the shared scene/server suites.

## Open behavior groups

This table groups the complete declaration ledger into implementation work. A
group remains open until all its applicable members, inherited consumers and both
backend paths pass the shared public contract. An Implemented declaration for the
current CPU host does not certify new geometry, tile owners or independent GPU use.

| Group | Missing behavior and affected families |
| --- | --- |
| Independent backend ownership and selection | Public CPU/Box2D and resident GPU worlds now execute with rendering-independent selection, startup-only fallback and requested/actual reporting. CPU startup without a device and failed GPU-step/no-replay teardown are exercised in physics-backends. Full conformance and performance acceptance remain open. |
| Continuous collision | Public CPU/GPU RigidBody/PhysicsServer modes now execute with force/motor budgets, frame impulses, directed-ray contacts and per-moving-body masks. Common CCD, ray and boundary suites exercise both implementations; wider cross-family trajectories and long-run acceptance remain open. |
| Shapes | WorldBoundary, SeparationRay, logical type/data access, body/Area response, resource queries and custom solver bias now execute on both public backends. Named provider ownership and immutable custom geometry now execute with an analytic annulus, custom mass/hole contacts, queries and private history. Built-ins explicitly reject unknown custom algorithms before mutation. Full provider geometry/feature semantics remain open; a custom identity alone is not their acceptance. |
| Constraints | Joint bias, common caps, linear PinJoint softness and the project/space correction default now execute through scene/server settings on CPU and stage GPU, with independent resident GPU kernels. The public GPU adapter owns resident constraints. The shared policy, scene pin/groove/spring and server family suites now pass on explicitly selected CPU/GPU worlds, including joint motion vetoes and extreme finite spring caps. Broader coupled workloads, inherited and extension contracts retain their independent gates. |
| Solver/world settings | CollisionObject collision_priority plus server getter/setter now execute in weighted motion recovery on public CPU and independent GPU; constraint bias and shape/world contact bias/slack now execute; solver iteration counts also execute through shared typed policy; all nine SpaceParameter capabilities now execute through concrete typed operations, including geometric contact recycling/separation. Linear/angular sleep thresholds and time-to-sleep now execute through typed public CPU operations and shared independent GPU policy (PhysicsSleepPolicyTests); public GPU binding now executes; full cross-family conformance remains open. Project-level defaults, separate-thread behavior and engine selection retain their exact coverage/adaptation rules. Hardcoded tolerances/substep counts do not implement writable settings. |
| Pointer interaction | [Completed under ADR 0099](physics-picking.md): scene callbacks/signals and differing defaults, viewport enable/first-only/sort and root project default, canvas/world routing, GUI/capture/exit, mutation/error/lifetime and warmed allocation. GPU queries now transfer predicted hit prefixes and exact missing tails; [measured picking cost](physics-picking.md#current-complete-frame-cost) keeps overall frame/FPS acceptance separate. |
| Tiles and object identity | TileSet physics layers/filter/priority/material; TileData polygons, one-way margins and constant surface velocities; TileMapLayer body-RID lookup, collision enable/visibility, quadrant grouping and kinematic mode. Carry tile owners through Area/RigidBody object and shape events, direct contacts, direct-space queries, ray/shape casts and kinematic/motion results. ADR 0101 now implements these policies for square atlas tiles, including actual generated bodies and owner projections through all listed consumers; TileMapLayerTests runs CPU/GPU and native rendering. Remaining general tile capabilities and tile-specific network topology remain open. |
| Server identity and queries | BodyGetCollisionLayer/Mask and shared scene/server/tile filter setters now execute under ADR 0102, with guarded authoring reads, immediate query edits, next-step monitoring and unchanged-assignment wake. Typed object/canvas associations, ShapeGetType, completed ProcessInfo and three Performance monitors now execute. Wider extension/state semantics and full conformance remain open. |
| Debug drawing | Authored shape/cast/joint and contact-point diagnostics now execute through retained canvas commands, including live hints, colors/outlines and bounded Space contact snapshots (physics-debug). Square atlas tile overlays execute under ADR 0101. Registered diagnostic capacity and bounded point/count dispatch now execute under ADR 0103. Wider visual/device, numerical-extension and full-frame performance acceptance remain open. |
| Backend registration and extensions | Register/default/factory selection, typed server operations, direct-state factories, lifecycle, exclusions, callbacks and resource lifetime now execute through the public-only consumer. Bounded numerical fields, material/surface response, CCD, sleep, pin/groove constraints and multi-peer frame history execute independently. Its numerical model now retains multiple transformed circle/annulus slots, disabled index gaps and per-feature contacts/queries/private history, including same-peer caps and CCD. Bounded rotating circle slots now pass mass-center, intermediate/coupled CCD and momentum checks on CPU/registered serial/separate modes. GPU, general polygon/capsule/multiple-piece and wider coupled CCD acceptance remain open. These are remaining verification/fixture gaps, not permission to expose inert library hooks. |
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
PhysicsServer is a sealed static facade, direct states support guarded caller-created extensions and RID allocation
is internal. Public typed query-result constructors now exist and preserve payload validation.
Shape now supports caller-defined resources through immutable exact ShapeGeometry and a protected geometry hook. Supported primitive/contour data executes on both backends; CustomShapeCreate and new solver-defined algorithms remain open. Selected world/collider/joint
ownership, world policy/capacity, completed statistics and selected full-step phases have been extracted, but registered third-party state/motion/query dispatch
is not implemented. These
are concrete integration prerequisites, not a reason to exclude extension behavior.
[ADR 0103](../decisions/physics-extensions.md#adr-0103) resolves the implementation
object/static-service role boundary and requires complete typed dispatch and
supporting construction/lifetime paths. Registration-only enum aliases and virtual
signatures with no scene/query consumer are rejected. Manager/server-extension declarations remain Blocked until complete public
implementation and executable acceptance. Caller-created direct-state families
execute; their class status remains Partial pending registered public integration.

Area implementation dispatch now moves CPU native overlap/priority reduction and
pre-step body preparation out of the shared space. Scene/raw observer scans and
audio containment reach the selected owner; GPU retains resident field reduction
and owns sensor/audio scratch. [Shared Area checks](physics-backends.md#area-implementation-ownership)
cover CPU/GPU fields, masks, logical notifications, audio and attachment lifetime
at zero warmed managed allocation. This closes an internal registration prerequisite;
the manager/server-extension census remains open until real registered implementations
execute the complete contract.

Native CPU scheduling/contact/CCD histories, selected private checkpoints and
portable one-way mapping now execute through their immutable world implementations.
Report-only pair/activity counters rewind with contacts before another solver step.
[The ownership contract](physics-backends.md#native-simulation-and-replay-ownership)
records replay, diagnostics, failure and allocation checks; registered public backend
factories and custom geometry remain open.

[Selected body integration preparation](physics-backends.md#selected-body-integration-preparation)
now removes direct CPU-only force/field calls from common PhysicsBodyRuntime and
PhysicsColliderBackend. Both implementations execute real scene/raw default-force
preparation; CPU field integration remains concrete and GPU stays resident. This is
an internal extension prerequisite, not public backend registration.

The live-space initialization boundary now has one internal construction transaction.
The server registers an immutable space RID before invoking the selected owner factory
outside the registry lock. The retained implementation is assigned before its initialization
callback can construct/query a guarded direct-space extension. Stepping, activation,
attachment changes, recursive initialization and release reject until completion. Failure
retires registry/view/ownership entries and releases retained native state; rejecting a
foreign implementation leaves its original world alive. CPU/GPU/no-device ownership
fixtures exercise these phases and all six real query hooks. This closes an internal
initialization prerequisite; public registered factories still do not execute.
Body-context preparation is independent of cached view acquisition: the protected
constructor prepares geometry/state and samples attachment identity without calling
BodyGetDirectStateCore or reopening its cache.
Existing direct-state constructors enforce ordinary off-solver access, while built-in
callback/contact views are prepared during the common interval. Registered factories
still need public factory integration, qualified callback-view construction phases
and the full selected operation bridge. Independent body binding closes
that cache-bootstrap prerequisite, not registered callback delivery. These are
implementation requirements rather than missing user permission; current
caller-created views do not prove the registered path.

## Internal named factory prerequisite, 2026-10-11

This section records the internal prerequisite before public registration shipped. Current public registration, custom geometry and portable continuation are described below; the earlier absence statements are historical.

The pinned manager's two methods and server extension's 140 methods have no deprecated owner/member exclusion. [Named selection](physics-backends.md#named-implementation-selection) now executes internal registration, strict default priorities, immutable lazy selections and real fresh scene/explicit owner construction. Selected wrappers alter real body impulse response and supply actual views on CPU/GPU/no-device. Public manager/backend/extension types, named entry points and a public-only third-party solver consumer now execute; the later registered-adapter slices below supersede the former 144-row Blocked census. Complete numerical operation acceptance remains open. No custom algorithm or final performance acceptance follows from these internal checks.

## Shape/body pair operation prerequisite, 2026-10-11

The pinned native body-pair implementation composes body and requested logical-slot poses and delegates to resource collision, including disabled geometry; it does not require a world. Both selected Electron2D owners now execute this typed internal family with real scene/raw/detached inputs, nested staging and validated output. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes rather than a full sixty-four-byte state record. A borrowed body/Area FreeRID now rejects before cleanup can retire its registry entry. The later registered adapter and custom-geometry slices below supersede the former public-declaration gap; complete numerical operation acceptance stays open.

The pinned native contact-depth-threshold setter is inert and its getter returns zero, but the abstract/extension header declares both methods without deprecation. That does not authorize an inert public mirror under ADR 0004: their useful typed extension contract remains to be resolved in the complete public family.

## Selected contact publication prerequisite, 2026-10-11

Factory-provided body contact hooks now execute on the owner after native worker capture, and their validated immutable output reaches scene monitors and raw callbacks. Cached reads do not rerun or double-transform hooks or resample later associations. Failure/stale/count/value guards isolate receivers while preserving solver commits and later callback delivery. This closes an internal publication integration gap; the full public manager/server-extension/custom algorithm census remains open.

## Scene pipeline and late native buffer growth, 2026-10-11

PhysicsPipelinePerformance now compares explicit CPU/GPU World selection through the same complete RigidBody/monitor/interpolation PhysicsFrame workload and checks public scene/direct-state identities, finite motion, active population, contact data, energy and penetration. The former CPU-only native-state/profile probe could not establish this path. Private resident storage/transfer growth now retains one managed wrapper and device reference while preserving native allocation/copy/fence behavior. A 64-body late-contact control allocated 1344 managed bytes before the fix and zero afterward. These measurements do not close public backend registration, native allocator accounting, other platforms or final real-window acceptance.

## Integration input ownership prerequisite, 2026-10-11

Default-force, CPU resolved-field and GPU parameter preparation now consume immutable engine-unit values instead of mutable PhysicsBodyRuntime/scene inputs. Queue/dirty-flag ownership stays common; numeric fields return complete values. Step, private checkpoint and explicit GPU sleep share the binding route. Selected scene/raw force tests exercise transformed actual response, omitted/frozen/sleep/rotation behavior, pre-application failure and reattachment with zero warmed managed allocation. This removes concrete integration input coupling, without closing public manager/server-extension or custom algorithms.

## Registered portable continuation, 2026-10-11

Public manager registration and the scoped backend/extension family now execute with the separate public-only consumer; custom annulus geometry also executes. The earlier internal prerequisites do not describe the current public boundary. Manager registration/default rows are Implemented; the extension owner remains Partial and its operation-specific rows retain open acceptance where the named behavior is absent or unverified.

Portable one-way output now uses the current declared count rather than retained buffer length. Shared capture/apply validation checks different bound colliders, enabled logical slots, valid nonnegative pieces and unique unordered pairs before encoding or mutation. The external consumer rejects malformed output without losing a previous packet, restores network identities into a different local creation order and checks numerical pass-through versus an accepted impulse. Generic one-way detection, complete operation-family acceptance, separate-process extension networking, foreign platforms and final GPU/window performance remain open.

## GPU publication transfer control, 2026-10-11

[Publication measurements](gpu-body-publication.md#full-path-control-2026-10-11)
compare full scene steps with identical assemblies and population. Dense output
combines status/results in one wait; sparse output keeps exact-prefix transfer.
Current complete caches may serve read-only callbacks, while callback writes and
separate cache invalidation retain fresh reads. Three 1,536-body repetitions save
one submission but do not show a reliable full-step improvement; GPU remains slower
than CPU on this monitored scene. All physical/managed-allocation gates pass.

The earlier 16,384-body CPU window and fresh GPU-only/headless controls passed,
while an initial CPU-then-GPU window sequence failed with severe escape/speed. This stability/lifecycle
case is open and prevents final rendered-family acceptance. It must be diagnosed
without reducing population or relaxing physical tolerances. These findings do not
close complete extension operations or final CPU/GPU performance acceptance.


[GPU scratch initialization](gpu-contact-colors.md#scratch-initialization-and-preparation-order) now exercises a 16,384-body NaN-initialized packed first step. Private color/packing resources are prepared before recording solver work. This does not close the sporadic uninstrumented window failure or final performance acceptance.


Registered [collider metadata](physics-registered-adapter.md#initial-collider-metadata) now reaches Attach-time object/canvas/priority hooks as well as live edits. Public body/Area queries, weak identities, transfers and initial-hook failures execute in a separate consumer. Ten extension rows now have explicit Partial projections; numerical priority and full operation acceptance remain open.

[Borrowed shape authoring](physics-registered-adapter.md#borrowed-shape-authoring) now rejects raw/scene body and Area slot/filter edits and shared server-shape replacement/release before mutation, including partial initial attachment. The independent consumer checks ordinary numerical query membership and zero warmed filter/query allocation. The complete 27-member shape/filter projection remains Partial; arbitrary numerical semantics and full extension acceptance remain open.

[Borrowed body authoring](physics-registered-adapter.md#borrowed-body-authoring) now guards scene/raw force, mass, state, integration and callback writes during numerical hooks, including direct-body views. Authored reads, ordinary callbacks and body-state forwarding retain access. The independent consumer exercises real impulses and zero warmed edited full-frame allocation; complete numerical/extension acceptance remains open.

[Registered Area fields](physics-registered-adapter.md#registered-area-fields) now exercise the complete typed profile/default shortcut through the selected hooks, including independent override/point/mask response and zero warmed edited full frames. Numerical-hook writes reject before mutation; authored reads and ordinary Area callback edits retain their guards and overlap history. Generic solver/extension acceptance remains open.

[Parameter reads](physics-registered-adapter.md#parameter-reads-and-preparation) project configured values without numerical preparation. Automatic inertia/center reads still synchronize current geometry outside numerical borrowing; preparation rejects inside a numerical hook. Initial/live scene/raw and zero-allocation public-only checks execute; full solver/extension acceptance remains open.

[Exact scene motion roles](physics-registered-adapter.md#exact-scene-motion-roles) now deliver RigidLinear for initial/live rotation lock, preserving freeze and disable precedence. The public-only consumer checks motion, guards, checkpoints and warmed full frames. The common server-mode integration below supersedes the former scene-RID prerequisite; complete numerical extension acceptance remains open.

[Common server modes](physics-registered-adapter.md#common-server-mode-family) now project all four modes through scene and raw RIDs, with independent scene descriptors, effective state/force response, pose publication, reentry and replay. CPU/GPU/registered checks share the public consumer; complete solver and final performance acceptance remain open.

[Common state pairs](physics-registered-adapter.md#common-state-family) now exercise all five typed branches on raw bodies and every scene body type, non-preparing initial/live reads, inherited pose/sleep numerical guards and detached/live wake requests. The two extension state rows remain Partial; complete numerical and final performance acceptance remain open.

[Integration/contact policy](physics-registered-adapter.md#integration-and-contact-publication-policy) now verifies omission, callbacks, contact caps and scene numerical guards with one public-only consumer on CPU/GPU/independent implementations. Six extension rows are Partial; full numerical and final performance acceptance stay open.

The pinned contact-depth-threshold pair has no deprecated XML metadata, and its built-in implementation remains inert. ADR 0070 now accepts an executable typed reporting policy rather than a no-op mirror. [Contact reporting depth](physics-registered-adapter.md#contact-reporting-depth), implemented at f6d8a6b9, retains scene/raw thresholds, signed scene-unit output, common projected filtering and history configuration. Public-only CPU/GPU/registered conformance checks .5/100 thresholds, registered equality, all scene body siblings, raw static reports, callbacks/lifetime and 64 warm edited frames at zero managed bytes. The two extension rows are Partial for complete numerical acceptance; they are no longer missing declarations or pending an applicable decision.

[Window checks at 567be307](physics-current-performance.md#window-checks-at-567be307) pass ordinary, NaN-filled and finite-nonzero buffer sequences with real rendered captures, 16,384 active bodies and zero warm physics/frame managed bytes. The resident first-step check now covers both dirty patterns. Native allocations, full scene-window/network performance and the earlier uninstrumented failure cause remain open. The goal requires measured GPU advantage on the target scenes; it does not specify a sustained 60 Hz acceptance threshold.


[Scoped scene/window allocation](physics-scene-performance.md#physics-and-native-event-allocation-boundaries)
now separates complete PhysicsFrame memory from native input, pose publication,
idle and render memory. Deterministic native mouse input reproduces 320 bytes per
event outside physics on CPU/GPU; these bytes remain in the full-window report.
The earlier aggregate rejection does not establish a physical-step allocation.
Physical invariants, observer quality and the zero-byte physical gate remain unchanged.


[Current separate-process networking checks](physics-network-example.md#stronger-process-acceptance-2026-10-12) now require actual denied GPU startup in both CPU workers, complete reported pose/linear/angular state, .45-radian drift recovery and a nonzero final angular response. The instrumented run and standalone public example exercise the named protocol gates; arbitrary topology, foreign platforms, native allocation and final network/window performance acceptance remain open.

[Motion-test exclusion helpers](physics-registered-adapter.md#motion-test-exclusions)
now execute exact guarded RID/object membership in the selected world's active
motion test, restoring scope after nested queries and errors. The public-only
CPU/GPU/registered fixture verifies nearest/farther/all/shared-object filtering,
real numerical hooks and 1024 outer queries plus 64 warm frames at 0/0 managed
bytes; registered samples also include nested motion/ray queries. The two helper
rows are Implemented; the broader motion operation, arbitrary geometry and complete
physics acceptance remain open.

[Notification delivery status](physics-registered-adapter.md#notification-delivery-status)
now shares world-scoped lifecycle state across CPU/GPU and registered implementations.
Body integration/state callbacks, contacts, Areas and sleep events are included;
exceptions and nested calls preserve the existing delivery/order/cleanup guarantees.


[Public shape-pair dispatch](physics-registered-adapter.md#public-shape-pair-dispatch) now reaches selected
CPU/GPU/registered owners, with immutable authored geometry reads, logical scene/raw
and detached slots, atomic output and zero warm full-frame/query allocation.
The mapped pair/data rows remain Partial until complete numerical acceptance.


[Motion result receiver identity](physics-registered-adapter.md#motion-result-receiver-identity)
preserves the moving RID authored by SetCollision through selected-query validation.
A same-world result for another moving body rejects before caller publication;
no-result calls enforce the same rule. SetMotion misses retain normal requested-body
binding, and rejection restores scope for the next valid query. The numerical
motion/geometry acceptance remains broader than this identity guarantee.


[Shared material response](gpu-physics.md#shared-material-response-and-allocation-evidence) closes the confirmed low-speed GPU bounce defect and adds explicit GPU coverage to the common material test. The isolated historical CPU allocation failure remains open; diagnostics preserve strict zero-byte checks.


[Shared mass-profile conformance](physics-bodies.md#shared-mass-profile-conformance)
closes the former CPU-only complete matrix: explicit GPU scene/raw physical paths,
analytic geometry, lifecycle/guard/storage and zero managed warm changes execute.
Native allocation and complete registered numerical acceptance remain open.


[Configured body reads during numerical hooks](physics-registered-adapter.md#parameter-reads-and-preparation)
use common retained authoring for all eighteen parameter/policy getters, including
actual selected Solve. Writes and numerical queries retain their rejection guards;
automatic geometry-dependent reads still need ordinary query access. The public-only
consumer verifies scene/raw ownership, failures and 128 full active frames at zero
owner/all-thread managed allocation. Complete numerical-extension acceptance stays open.


[Registered lifecycle census](physics-registered-adapter.md#registered-lifecycle-census)
now verifies initialization, local/global activation, complete interval order,
completed statistics, failure/cleanup paths and 128 moving full frames per scene/raw
role at zero managed allocation through a public-only independent implementation.
Eight former unmapped rows have their actual typed projections; complete numerical
extension and platform/performance acceptance remain open.


Registered contact diagnostics now prepare capacity before solving and relinquish capture on disable/detach; guarded synchronized count/point publication retains ADR 0100. Both SceneTree finalization entries preflight borrowed implementation lifetime before teardown. The three debug-space extension rows remain Partial until their wider acceptance is demonstrated; see [diagnostic capacity and publication](physics-registered-adapter.md#diagnostic-capacity-and-publication).


[Factory-selected direct views](physics-registered-adapter.md#factory-selected-direct-views) now execute through a public-only consumer, including cache identity, an initially empty body-contact snapshot, all six query families, immutable contact publication before scene/raw callbacks and failed-factory retry. Both extension direct-state rows remain Partial; full numerical, platform and performance acceptance stays open.

[Shape correction policy](physics-registered-adapter.md#borrowed-shape-correction-policy)
now rejects off-owner, numerical/step, geometry-supplier and recursive access writes
before value, epoch or observer mutation, including equal assignments. The shared
resource projection executes on CPU/GPU/registered scene/raw users and preserves
post-solver callback edits and zero warm allocation. Two extension bias rows move
from Unimplemented to Partial; independent registered correction and
full extension acceptance remain open.

[Geometric authoring preflight](physics-registered-adapter.md) now protects all built-in setters,
CustomShape and procedural authors before private data changes. Registered/CPU/GPU
checks cover 16 paths, immutable data/RID preservation, ordinary callback/query
edits and warmed full frames. Arbitrary private subclass writes require the
protected authoring protocol; full numerical-extension acceptance remains open.

[Shared shape factories and data lifetime](physics-registered-adapter.md#shared-shape-construction-and-data-lifetime) now execute through the
public-only registered/CPU/GPU consumer. Replacement errors preserve original
geometry, continue every rollback/retirement and retain all failures; incomplete
borrowers retry during guarded preparation. Ten formerly unmapped extension rows
project to the common resource role as Partial. Full numerical-extension and final
physical/network/performance acceptance remain open.

[Body construction and continuous policy](physics-registered-adapter.md#body-construction-and-continuous-policy)
now share selected CPU/GPU/registered ownership, detached/live modes, automatic
mass reset and guarded typed configuration. Valid CCD writes avoid enum metadata
allocation after collection; the public-only consumer checks 128 edited full
steps at 0/0 managed bytes. Registered numerical CCD and full acceptance remain open.

[Registered pointer delivery](physics-registered-adapter.md#registered-pointer-delivery)
now verifies scene Body/Area policy through selected point queries, numerical
guards and 128 changed-policy full frames at zero managed allocation. Native
CPU/GPU/registered worlds each pass with both renderers and target pixel checks;
full registered geometry and final acceptance remain open.

The [rendered PhysicsNetwork client](physics-network-example.md#rendered-client-window)
now draws remote interpolation and local correction smoothing in a real window
while CPU authority/late client stay headless. Public snapshot and pixel checks
verify visible separation without physical-state mutation; renderer choice and
window FPS/allocation reporting remain separate from physics/replay metrics.

[Shape-owner metadata access](physics-registered-adapter.md#shape-owner-metadata-access)
now checks actual space ownership/failure for tree-less scene attachments while
retaining numerical authored reads and cleanup. The shared CPU/GPU/registered
consumer exercises every collider role and 128 full scalar-read frames at 0/0
managed bytes. Complete geometry/disable and final acceptance remain open.

The complete PhysicsAreaFieldTests, PhysicsServerAreaFieldTests and PhysicsAreaMonitorTests now execute on explicitly selected CPU/GPU worlds. They retain all five gravity/damping modes, priorities, point transforms/falloff, default-world shortcuts, signed damping, directional masks, logical pair identity, callback failure/removal and lifecycle assertions. Initial unchanged child registration preserves a sleeping body; changed fields still wake it. Sixty-four measured moving, sleeping and edited-field frames and 64 monitor transition cycles (128 frames) require zero owner/all-thread managed bytes. CPU field validation failure can be corrected; a started GPU field-reduction failure is terminal, preserves the selected backend and last published scene field snapshot, and permits resource retirement. Native allocations, other platforms, visual and final performance acceptance remain open.

The former unimplemented SpaceParameter enum row is a representation exclusion under ADRs 0001/0087/0089/0098: all nine typed parameter pairs already execute. Its values remain excluded individually; no capability is removed by this ledger correction.

The 2026-10-12 Area candidate first failed the public-only registered diagnostic fixture with 0/2304 owner/all-thread managed bytes. Its unchanged source/runtime repeat passed the complete consumer. This single other-thread observation is not diagnosed or claimed fixed; strict zero-byte assertions remain, and its rejected/rechecked evidence is retained under bin/physics-area-contract/2026-10-12/. The shared Area and child-owner profiles independently passed their own zero-byte contracts.

The complete PhysicsMotionTests, PhysicsQueryTests and PhysicsShapeQueryTests now use explicitly selected public CPU/GPU worlds. The common suites retain scene/raw identities, view replacement and last-resource retirement, filters/exclusions, compound origin-inside and initial-overlap rules, hollow/segment/convex contact families, one-way recovery, motion fractions/travel and scene shape-owner payloads. Existing numerical bounds are unchanged. Warm gates include both owner/all-thread managed counters: 64 scene-motion, compound recovery/ray/cast/rest queries and 128 alternating span-query cycles require 0/0 bytes. Caller-owned World aliases may outlive the SceneTree; its colliders retire immediately and the view expires after the last World resource is released. Native allocations, foreign platforms, visual and final performance acceptance remain open.

Complete PhysicsBodyStateTests and PhysicsServerForceTests now execute on explicitly selected public CPU/GPU worlds, alongside the common PhysicsContactImpulseTests and report-only role suite. They retain state/field/force/center values, post-solver callback edits/errors/removal/reentry, borrowed-view lifetime, force omission and pending-input rules. Real scene/raw dynamic and kinematic fixtures expose six uncapped contacts and verify deepest 2/1/0 caps and the corresponding monitor objects without private list-order assumptions; static/static pairs remain ineligible. Momentum checks retain their .08/.1 scene-impulse bounds, equal/opposite and rotating-normal/transient/sleep assertions. Warm gates require 0/0 owner/all-thread bytes for contact/callback/force frames, zero-cap frames and live pose reads. CPU native scheduling/depth-injection checks remain separate diagnostics; legacy staged GPU controls do not establish independent GPU acceptance. Native/platform, visual and final network/performance gates remain open.

PhysicsServerStateTests now runs its complete typed state family on explicitly selected public CPU/GPU worlds: scene/raw/direct-view state, detached sleep and wake rules, axis projection, kinematic latest-target consumption, zero/inactive steps, detach/reentry, moving-support wakeup and scene-role presentation. PhysicsSurfaceVelocityTests also runs in the full CPU/GPU suite and the independent GPU-kinematic profile, retaining stationary normal/tangent/angular response, contact point velocity, character carry and additive target motion. Numerical bounds are unchanged. Both suites require 0/0 owner/all-thread managed bytes over 64 warmed active frames. Private native lock injection and historical staged controls remain separate CPU diagnostics. Native/platform, broader inherited/extension, networking and final performance acceptance remain open.

The complete OneWayCollisionTests, CollisionPolygonTests and PhysicsCollisionExceptionTests now execute on explicitly selected public CPU/GPU worlds. They retain opposite approaches, local/parent rotations, live side changes and zero direction, dynamic/kinematic platforms, Area sensor bypass, solid/hollow/concave polygon geometry, warning-error recovery and restored scene geometry. Unilateral scene/raw exceptions suppress both motion directions and solved response; duplicate entries, server-only/null and freed-target slots, live edits and reentry keep their existing rules. Position/velocity bounds are unchanged. Sixty-four warmed active one-way, polygon and exception frames plus 64 changed raw-exception/step cycles require 0/0 owner/all-thread managed bytes. Internal GPU store probes remain diagnostics; broader inherited/extension, network/platform, native allocations and final performance acceptance remain open.

The ordinary full suite now executes both CPU and independent GPU variants of all sixteen previously parameterized default-CPU families: AnimatableBody, CapsuleShape, CharacterBody, CollisionDisableMode, ConcavePolygonShape, ConvexPolygonShape, DampedSpringJoint, GrooveJoint, PhysicsBodyParameter, PhysicsServerJoint, PinJoint, RigidFreezeMode, SegmentShape, SeparationRayShape (including its dynamics suite), ShapeOwner and World. ELECTRON2D_TEST_SHARED_PHYSICS=cpu|gpu runs the complete same set explicitly; existing numerical and lifecycle/error assertions are unchanged. WorldTests retains the default-CPU/mixed-world cases and adds two explicitly selected isolated worlds, query identity/isolation, attachment-view invalidation and a shared once-per-tick step. Its 96 warmup/64 measured complete frames with lookups require 0/0 owner/all-thread bytes; one-frame 60-unit/s travel stays within .02 scene units, covering default damping and floating-point integration. Individual existing suite allocation scopes remain their documented limits. This closes the full-suite call-site omission, not remaining inherited/extension, native/platform, network and final performance acceptance.

The complete PhysicalBoneTests now executes on explicitly selected public CPU/GPU worlds, including command/follow and custom integration fixtures and restored graphs in a separate fresh process. It retains motion/filter identity, strength/disable/follow/freeze behavior, binding/hierarchy recovery, actual pin constraints, named commands, modifier failure rollback, phase/owner guards and existing numerical bounds. The ordinary full suite runs both backends; ELECTRON2D_TEST_PHYSICAL_BONE=1 with ELECTRON2D_PHYSICAL_BONE_BACKEND=cpu|gpu selects one. After 128 warmup frames, 128 complete physics/pose/weighted Polygon cycles require 0/0 owner/all-thread managed bytes. Native hosts select physics independently from ELECTRON2D_PHYSICAL_BONE_RENDERER=gpu|compatibility, reject renderer fallback and assert both actual selections. All four CPU/GPU-physics by hardware-renderer combinations verify real changed skin pixels and matching Bone/body pose; 64 prepared draw/velocity-update intervals require zero owner-thread managed bytes. This rendering interval is separate from full physical allocation measurement. Native allocations, large rigs, foreign platforms, human acceptance and final network/massive GPU/window FPS gates remain open.

The complete PhysicsBodyTests, RigidBodyForceTests, RigidBodyContactTests and ShapePairEventTests now execute on explicitly selected public CPU/GPU worlds, with actual backend assertions in every numerical scene. The ordinary full suite runs both variants; ELECTRON2D_TEST_SCENE_PHYSICS=cpu|gpu selects the same complete set. Existing force/impulse/center units and numerical bounds, freeze/lock/mass/geometry recovery, persistent force packing, sleep/wake notification policy, contact caps/deduplication and object-before-shape entry/departure ordering are unchanged. Scene/raw nullable payloads, compound logical slots, handler mutation/errors and committed-event no-replay remain asserted. All existing 64-frame warm gates now require 0/0 owner/all-thread managed bytes for resting/active/moving bodies, forces, monitored/no-contact frames and steady/transitioning shape pairs. Box2D SIMD and worker scheduling probes remain CPU diagnostics; they do not establish independent GPU acceptance. Native allocations, wider inherited/extension, foreign platforms, full networking and final GPU/window performance acceptance remain open.

The 2026-10-13 scene candidate's first full suite failed the existing CPU QueryOwnership allocation gate with 0/3984 owner/all-thread bytes. Its unchanged frozen runtime/source passed both the complete focused ownership suite and the full-suite repeat. This single other-thread observation has no diagnosed cause and is not claimed fixed; strict zero-byte assertions remain. Rejected and repeat logs are retained under bin/physics-scene-contract/2026-10-13/. The four shared scene suites separately passed their own strict allocation contracts on CPU/GPU.

The ordinary full suite runs PhysicsCCDTests, PhysicsJointPolicyTests and WorldBoundaryTests on both explicitly selected public CPU/GPU backends. ELECTRON2D_TEST_CONTINUOUS_PHYSICS=cpu|gpu selects this same complete set. Scene CCD/boundary fixtures and every raw boundary space assert the actual selection; existing numerical, lifecycle and error bounds are unchanged. CCD retains 64 warmup/128 reset-active steps and joint policy retains 128 warmup/128 edit-active frames, now requiring zero owner/all-thread managed bytes. Boundary retains its 128 warmup/128 active-step all-thread zero gate. Legacy CPU-host GPU stages and internal resident-store tests remain separate diagnostics. These bounded checks do not establish native allocations, foreign-platform acceptance, massive GPU advantage or window FPS.

The 2026-10-13 continuous candidate's initial registered-consumer run failed its
existing diagnostic-contact gate with 0/2304 owner/all-thread bytes. An unchanged
frozen consumer under startup EventPipe tracing passed all gates. The trace
sampled finalizer-thread GCMemoryInfoData allocations from SharedArrayPool.Trim
and PortableThreadPool.OnGen2GCCallback through Gen2GcCallback.Finalize, with no
engine frame on those stacks. This establishes a runtime background allocation
source, but sampled tracing did not observe the original failed interval and does
not establish that interval's cause. The complete uninstrumented frozen consumer
repeat also passed. No threshold or test scope was relaxed, and no runtime
allocation fix is claimed. Rejected and diagnostic evidence is retained under bin/physics-continuous-suite/2026-10-13/.

Prepared integration's attachment-local flag was measured in alternating baseline
and candidate headless runs on Linux/Wayland, Vulkan RTX 3090 Ti. All runs keep
65,536 awake circles plus three walls, dt=1/60, four substeps, 16 iterations,
240 warmup and 64 samples. Baseline/candidate policy-motion preparation means
were 12.0725/8.4108 ms, then 11.3485/8.1269 ms; full-step medians were
52.7323/45.1421 ms, then 48.9022/46.0570 ms. Every run passed 0/0 owner/all-thread
bytes, no escape and the existing energy/penetration limits. These A/B measurements
have run variance and do not isolate every change in device/contact scheduling.
The final source adds the preparation guard assertion and XML clarification;
its independent CPU/GPU window comparison uses the same frozen source/runtime.

| Final window backend | Physics p50/p95/p99 ms | Frame p50/p95/p99 ms | Actual FPS | Pose publication p50 ms |
| --- | --- | --- | ---: | ---: |
| CPU | 283.5741 / 637.8590 / 954.3179 | 305.9499 / 698.2644 / 1008.4203 | 2.6795 | 15.6415 |
| GPU | 51.9073 / 53.4363 / 54.1009 | 74.4509 / 76.4854 / 76.8478 | 13.4481 | 16.4236 |

The real 1152x800 window retains all 65,536 awake bodies; physics and whole measured
frames both pass 0/0 owner/all-thread managed bytes. GPU physics reports 232 upload
and 761 readback bytes per sample; whole-frame store traffic is 1,048,816 upload
and 1,049,345 readback bytes per tick, dominated by the explicit complete visual
pose batch. Mean physical GPU wait is 31.2324 ms. Contact-color fallbacks are zero;
maximum CPU/GPU penetration is .27031/.29711 under the .601 limit, with no escape
and bounded energy. Both captures were inspected and show the dense pile. Actual
VSync remains Enabled despite the requested Disabled policy. CPU tail latency is
noisy. This profile establishes a measured full GPU advantage on this dense scene,
not 60 Hz, a general game speedup, native allocation or foreign-device acceptance.
Evidence and full distributions are retained under
bin/physics-mass-current/2026-10-13/; final/provenance-before.json identifies source
7958bcf44c4472c947c1bcf7e2b713b0f3a734ef6a836c254ae3650034489fb0 and every frozen
runtime file. Remaining inherited/extension, networking and full performance gates
stay open.

The 2026-10-13 ledger reconciliation removes stale claims that named registration,
factory-selected body/space views, independent GPU body ownership and linear pin
softness are absent. Their current public-only and shared CPU/GPU suites provide
bounded evidence; no declaration status is promoted by this wording correction.
The remaining extension obligation is the complete applicable operation behavior,
including arbitrary geometry/compound numerical semantics and coupled constraint,
CCD, sleep/contact-history acceptance. A registered fixture is responsible for its
own numerical implementation; the runtime bridge must expose and validate its
complete capabilities. Shared inherited declarations remain on their declaring
families, and final performance/network/native gates remain distinct.

The registered annulus/circle consumer now evaluates resource sweeps independently.
For center segments A and B, collision truth requires their minimum separation
at most outerRadius+circleRadius and their maximum endpoint separation at least
innerRadius-circleRadius (clamped to zero). The continuous range between these
extrema covers the available independent center pairs. The annulus sweep is the
outer capsule minus the intersection of the endpoint inner disks; that lens is
empty once the annulus displacement reaches twice its inner radius. Contacts use
outer-capsule or inner-lens support boundaries and the circle's capsule boundary,
with first/second ownership reversed by the adapter.

The --custom profile verifies registered and CPU selection; ELECTRON2D_CUSTOM_GPU=1
adds explicitly selected GPU resource pairs. Each variant verifies twelve exact cases (equal/perpendicular/reversed
motion, retained/closed holes, outer/inner touching and misses), 512 cases seeded
43127 for contact-boundary validation, and 128 warmed standalone/selected cycles
at zero owner/all-thread managed bytes. Boundary checks allow .0001 scene units
for float projection; truth in the twelve cases is exact. This fixes the consumer's
synchronized-relative-path mismatch without changing the production API or solver.
Arbitrary custom geometry, annulus/annulus contacts, complete numerical-extension
and final performance/native/platform acceptance remain open.

The custom-sweep candidate's original full registered consumer again failed the
unchanged diagnostic-contact allocation gate before entering custom geometry,
with 0/2304 owner/all-thread bytes. The same frozen consumer passed under startup
EventPipe tracing; sampled stacks again show finalizer-thread SharedArrayPool.Trim
calling GC.GetGCMemoryInfo. They did not capture the failed interval, so the
specific cause remains open. Its complete uninstrumented frozen consumer repeat
also passed. Strict zero-byte assertions and operation counts are
unchanged. Original and diagnostic logs are retained under
bin/physics-custom-sweep/2026-10-13/; the custom/no-GPU and CPU/GPU pair profiles
separately passed their own unchanged warm gates.

Allocation warmup in PhysicsBackendOwnershipTests and the public-only registered
consumer now drains pending finalizers after the final forced collection. A shared
cold regression object re-registers after its first finalization and blocks the
second pass. Both former helpers returned while that pass was blocked (RED); both
corrected helpers wait until release and observe both completed passes. The same
check runs before ownership and full-consumer acceptance; --allocation-warmup
selects the consumer check alone.

This changes test preparation, not the physics runtime, measurement counters,
zero-byte assertions, capacities or warm/measured operation counts. Earlier traces
sampled .NET array-pool/thread-pool finalizer allocations. The missing final drain
is now a proved preparation defect and is corrected; the exact objects behind
historical untraced 0/2304 and 0/3984 intervals were not captured, so those interval
attributions remain limited. Finalizer drainage does not suppress engine allocations
or establish native allocation/performance acceptance.

Final linux-x64 Release/native-source validation on main at
337e114881abdf8cd7438d889b03d1dce0abc15f plus this change passed seven profiles:
the isolated no-GPU regression, three fresh complete registered-consumer runs,
CPU ownership without GPU, GPU ownership and the complete executable checks.
Ownership retained 512 bodies/64 full warmed steps at 0/0 owner/all-thread bytes
on both backends. Source and frozen runtime fingerprints remained identical;
logs and provenance are under bin/physics-allocation-warmup/2026-10-13/final-current/.
The earlier run passed executable checks but its provenance guard rejected
concurrent main/source changes; it is retained separately under final/.


The [independent pin/groove response](physics-registered-adapter.md#independent-pin-and-groove-response)
consumer now solves real anchor/finite-guide constraints with lever arms,
compliance, inherited bias, force budgets and motor/stop policies. A shared
registered/CPU/GPU public suite covers both joint owner roles, single-body and
off-center pins, reversed/zero guides, Replace damping, private next-step replay
and four 128-frame strict 0/0 allocation intervals. The bounded fixture integrates
poses once per outer interval; complete coupled geometry/CCD/sleep/history,
network, native and performance acceptance remains open.


[Independent private history](physics-registered-adapter.md#independent-private-history)
now preserves numerical sleep/force/contact/field state and retained one-way
choices, supports grown/shrunk recapture, and clears retired references. The common
registered/CPU/GPU public suite checks real force/torque replay, sleep and immediate
contacts at 0/0 bytes over 128 full-step/capture/restore cycles; registered decision
replay has its own 128-cycle zero budget. This bounded fixture does not close
arbitrary CCD/sleep/contact algorithms, portable networking or final acceptance.


[Independent automatic sleep](physics-registered-adapter.md#independent-automatic-sleep)
now consumes actual world thresholds and duration in the public-only numerical
fixture. Dynamic contact/joint groups sleep together; static supports do not
merge groups. Shared registered/CPU/GPU --sleep checks cover strict boundaries,
zero thresholds/duration, CanSleep, policy/impulse wake, inactive intervals and
quiet-clock rollback. Each path measures 1920 replay frames and 896 actual group
sleep/wake frames at strict 0/0 owner/all-thread managed bytes. General geometry,
CCD, portable networking, native totals and final performance remain open.


[Independent contact response](physics-registered-adapter.md#independent-contact-response)
now exercises signed bounce/friction, static normal/tangent/angular surface motion,
AnimatableBody inheritance, dynamic lever arms, full contact impulses and replay
through the same registered/CPU/GPU public assertions. Geometry edits and immediate
surface wake are checked. All three paths measure 128 edited material/surface and
capture/restore full-frame cycles at strict 0/0 owner/all-thread bytes. Shared
shape preparation uses indexed traversal; CPU keeps unchanged native fixtures
while updating material. General geometry/CCD, network, native totals and final
performance acceptance remain open.


[Independent numerical CCD](physics-registered-adapter.md#independent-numerical-ccd)
now advances the bounded circle/annulus model through current relative swept
intervals, consumes remaining time and retains full signed contact impulses.
Shared registered/CPU/GPU --continuous-response checks verify actual ray/shape
stopping, discrete control, relative impact, elastic remaining motion, filtering,
one outer callback/tick, force duration and 128 replay cycles at strict 0/0 bytes.
The registered controls also exercise hollow inner-wall rebounds and terminal
interval-budget failure. General geometry/rotation/coupled extension, network,
native totals and final performance acceptance remain open.


[Independent completed contact frames](physics-registered-adapter.md#independent-completed-contact-frames)
now retain every observed peer in the bounded single-shape fixture, sum signed
impulses across separated CCD impacts and restore complete numerical history.
Shared registered/CPU/GPU --contact-frames verifies momentum, report-only peers,
deepest caps, depth filters and encounter-order ties. Each path measures 128
transient replay cycles and 768 changed cap/depth frames at strict 0/0
owner/all-thread bytes. General feature/coupled geometry, portable networking,
native totals, rendering and final performance/platform acceptance remain open.
