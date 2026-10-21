# CPUPhysicsWorldBackend

CPU dynamics and query/motion manifolds now share the [coincident rounded-core fix](../components/physics-contact-policy.md#coincident-rounded-cores), including transformed circle/capsule/segment contacts and collapsed/point compiler limits.

Cold world-query geometry now uses the [shared using-world borrow](../components/physics-queries.md#cold-query-geometry) before user suppliers run. Numerical writes, collider retirement/transfer and final world release reject; original supplier failures can retry. Existing pure input reads and nested query behavior are retained.

The query/Area partials now share body-local unrounded polygon containment through PhysicsShapeBackend. Direct points, ordinary ray inside-start policy, directed shape-query origin filtering and Area audio no longer inherit GJK zero-distance slack outside an edge. [Public CPU/GPU checks](../components/physics-point-containment.md) preserve logical identity and warmed allocation bounds.

Last updated: 2026-10-21

**Declaration:** `internal sealed partial class CPUPhysicsWorldBackend : PhysicsWorldBackend` · **Source:** [CPUPhysicsWorldBackend.cs](../../src/Servers/Physics/CPUPhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

The owner retains concrete native collider attachments keyed by common collider
identity. NativeCollider serves its field, query, Area, contact, CCD and joint paths;
common scene/server objects carry no CPU handles. Matching retirement removes an
entry, delayed stale retirement preserves a newer attachment and world disposal
clears the table. Selected wrappers can implement collider operations while this
owner keeps actual CPU storage. See [ownership](../components/physics-backends.md#cpu-collider-ownership).

Owns one native CPU world, its retained PhysicsTaskScheduler and any diagnostic GPUPhysicsWorld stages. Creation applies the existing world definition, sleep/contact/iteration settings and pre-solve callback. Partial construction releases already-created native resources; the implementation does not initialize a renderer.

WorldID exposes a borrowed internal native identity; Tasks and StageGPU expose owned internal helpers. EnsureAccess rejects disposed or solver-owned state. Step uses the common space boundary and CPU-owned phases; every CPU discrete and CCD interval reaches StepNative. EnableGPUIntegration and EnableGPUSolver retain the existing CPU-hosted diagnostic paths, which keep CPU identity. Dispose joins workers, removes native stage callbacks, releases diagnostic GPU resources and destroys the native world; cleanup errors are aggregated after attempting each owned resource. Repeated disposal is harmless.

The query partials own native ray/point scans, standalone query proxies, candidate collection, intersection/sweep refinement, manifold contact pairs and deepest rest selection. Retained lists serve all array/span overloads without output allocation for span calls. The CPU motion partial owns body candidate collection, weighted recovery, directed-ray/one-way/full-contour geometry and sweep refinement using the same Overlaps, Cast and GetManifold helpers. Dispose clears retained direct-query and motion candidates. Source: [CPU body motion](../../src/Servers/Physics/CPUPhysicsWorldBackend.Motion.cs).

CreateCollider constructs a fresh CPUPhysicsColliderImplementation for each body/Area attachment. The retained collider identity facade delegates all engine-unit body and shape operations to that concrete owner; reattachment never retargets its immutable world.

CreateJoint creates a fresh CPUPhysicsJointImplementation for each constraint attachment. Shared settings and physical RID stay in PhysicsJointRuntime. GetJointWorldBody lazily creates and reuses the CPU world's shape-free static anchor with ordinary lifetime/solver guards. Creating that endpoint also prepares solver and contact-motion storage for its added body slot, including a power-of-two collider population; [capacity regression and retained storage](../components/physics-retained-memory.md) record the exercised boundary. Generic policy/setters/portable-frame/release operations dispatch to that attachment.

Source partials: [policy](../../src/Servers/Physics/CPUPhysicsWorldBackend.Policy.cs), [capacity](../../src/Servers/Physics/CPUPhysicsWorldBackend.Capacity.cs), [statistics](../../src/Servers/Physics/CPUPhysicsWorldBackend.Statistics.cs).

## Joint input ownership

CreateJoint receives the retained common joint identity. Attachments consume [PhysicsJointConfiguration](PhysicsJointConfiguration.md) values rather than the mutable runtime. CPU retains native joint ownership by that identity, schedules actual spring phases and retires matching entries; GPU keeps resident execution. See [the shared input contract](../components/physics-backends.md#shared-joint-inputs-and-native-ownership).

## World policy, capacity and statistics

The selected owner now applies sleep/contact thresholds, solver iterations and world joint defaults. PhysicsSpace validates and retains authored values, then dispatches implementation work. CPU owns native threshold updates, waking, worker selection, sensor scratch, solver/arena capacity and native sensor/process-statistics traversal. GPU applies resident settings and epoch invalidation, prepares existing GPU publication/report scratch and reads completed store statistics; it creates no CPU motion-history mirror.

CPU capacity preparation keeps a conservative flag once any non-static attachment can sleep. Worlds whose non-static bodies have never enabled sleep skip repeated complete-body scans during attachment. Attachment, a mode change to a non-static role and enabling sleep prepare eligible dynamic and kinematic capacity immediately. Shared body-state initialization completes at world creation before the first dormant-island wake; see [kinematic capacity checks](../components/physics-sleep.md#kinematic-dormant-capacity). The flag never resets, so private checkpoint restores cannot introduce an unobserved sleeping role. [Pooled dormant storage](CPUPhysicsSleepStorage.md) replaces the fixed per-island body/contact/joint limits. Sleep semantics remain unchanged; split scratch is also prepared when sleep first becomes possible. Before each numerical interval, observed contact identity demand can grow the initial four-contact-per-body reservation to a retained power-of-two peak across active, dormant, graph, pair, bitset and scratch storage. Full-interval impulse buffers prepare two points per contact; joint preparation preserves that contact capacity. See [dense contact checks](../components/physics-sleep.md#observed-dense-contact-capacity).

The broad-phase contact-pair hash table now reserves two slots per prepared
contact, matching its half-full growth threshold and the existing four-contact-
per-prepared-body budget. Growth and rehashing happen during owner capacity
preparation, preserving live keys; ordinary numerical membership/insertion and
contact ordering are unchanged. This prevents later contacts within the prepared
budget from allocating a larger table in `Solve`. See [evolving-contact regression
and measured storage cost](../components/physics-scene-performance.md#cpu-contact-pair-capacity-2026-10-15).

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks typed policy, invalid values, independent worlds, CPU native settings, selected statistics, no GPU CPU-motion storage, live role changes and disposed-owner guards. Common sleep/contact/iteration/statistics, replay and network suites remain the behavioral acceptance boundary.

## Selected step phases

CPU step phases resolve world fields and body callbacks, select workers, execute native discrete/CCD/kinematic intervals, complete poses, collect contacts in joined ranges and scan Area observations. ResultsReady becomes true only after solve advances the tick. EndStep prunes one-way history on both success and failure. The [common phase/lifetime boundary](PhysicsWorldBackend.md#selected-step-phases) owns observer publication and ordered callbacks. Source: [CPU phases](../../src/Servers/Physics/CPUPhysicsWorldBackend.Step.cs).

Body-motion tests now dispatch through this selected owner using engine-unit inputs and RID exclusions. The [common contract and verification](PhysicsWorldBackend.md#body-motion-and-platform-lookup) records geometry, identity, lookup and allocation boundaries.

Frame impulse aggregation prepares retained point records and their feature index
for two points per contact in the existing four-contact-per-prepared-body budget.
Only worlds with an active contact receiver reserve these stores. This prevents a
later CCD/contact feature peak from growing the list during a warmed step; first
use or topology growth remains cold capacity work. The public network geometry
[profile](../components/physics-network-example.md#cross-family-geometry-under-correction-2026-10-15)
checks the real late-client regression rather than a capacity-field assertion.

## Ownership and verification

Native world destruction clears graph-block segment references and drops its
source graph-color array from the reusable static slot. Fixed step/worker shells
remain allocated; active-world scratch reuse is unchanged. The [world lifetime
regression](../components/cpu-checkpoints.md#retired-pool-payloads) checks actual
contact worlds after normal and failed intervals, including source destruction
while independent checkpoints are still held. The [retained memory report](../components/physics-retained-memory.md#static-cpu-world-pool)
accounts for the idle static pool without adding active world state twice.

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. It also checks all six direct-space operations, access/lifetime/failure guards and 64 warmed hit/miss query cycles at zero owner/all-thread allocation. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

Native IDs and worker callbacks stay internal. Common body/shape/joint adapters still contain CPU/GPU branches; their complete extension boundary remains open. No new public backend capability is exported. Native allocator and foreign-platform execution remain separate gates; recorded workload-specific GPU/window measurements do not establish general superiority. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Area implementation ownership

[The Area partial](../../src/Servers/Physics/CPUPhysicsWorldBackend.Areas.cs) owns retained stable priority scratch, exact native overlaps, scene/raw observer scans and indexed audio containment. [Body preparation](../../src/Servers/Physics/CPUPhysicsWorldBackend.BodyState.cs) resolves fields and damping before motion and solver integration. Native boundary/directed-ray overlap rules are preserved; Dispose clears field references. Shared event histories remain in PhysicsSpace. [Contract and checks](../components/physics-backends.md#area-implementation-ownership) cover both selected backends and no-device CPU.

## Native simulation and replay ownership

Native interval scheduling, CCD sweep/force/joint scratch, one-way episodes, material callbacks, contact impulses/point motion, report-only geometry/statistics and portable native mapping now belong to this owner. SolverCheckpoint combines CPUPhysicsCheckpoint with those required replay caches; report-only counters rewind immediately. Worker drain precedes sweep scratch release. [Contract and verification](../components/physics-backends.md#native-simulation-and-replay-ownership) describes the executed scope and remaining registration gates.

Private solver storage comprises ContinuousBody/ContinuousShape/ContinuousQuery
(sweeps, current shape ownership, bounds and synchronous candidates), ContinuousForce
and ContinuousJointBudget (nominal interval force/torque/motor usage), KinematicStepForce
(replayed native interval forces), BodyMotion (solved center/point velocity), FrameContact
(canonical logical encounter lists and outer-tick geometry/impulse totals), OneWayPair
(side episode and native contact-step age), ReportOnlyQuery (zero-impulse tree traversal)
and SolverCheckpoint (reusable private history). None escapes as public solver data;
only the history needed for observation/replay is captured, while per-step scratch
is rebuilt and released by the owner.

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).

## Selected checkpoint ownership

PrepareCheckpoint and the selected history operation family borrow library lifetime. GPU reports/epochs/collider caches stay in its concrete checkpoint; common authoring uses neutral field values. Failed capture is replaceable, validation rejection preserves state, begun restore failure is terminal, and cleanup failure still retires the point. See [the executing contract](../components/physics-backends.md#selected-checkpoint-ownership).

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

## Selected shape-pair operations

The internal resource/resource and body-slot/resource bridge now executes through the retained implementation with complete logical indices, composed global poses and validated nested caller-span results. Detached bodies use the same exact resource kernel without a solver; borrowed collider release is preflighted before registry cleanup. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes instead of complete motion/field/sleep state when no current cache exists. [The pair contract and checks](../components/physics-shape-pairs.md) distinguish this internal prerequisite from the pending complete public server-extension family.

## Value-only integration preparation

[Force and policy inputs](../components/physics-backends.md#value-only-body-integration-inputs) now separate engine-owned authoring/command state from numerical preparation. Default forces use PhysicsBodyForceConfiguration; CPU fields and GPU parameters use PhysicsBodyIntegrationConfiguration. Pending command retirement and GPU dirty flags stay in the binding layer; numerical methods receive finite authored values and return complete field/preparation results. CPU/GPU/no-device, replay, parameter and full operation checks remain the acceptance boundaries; public registration stays open.


[Selected diagnostic preparation](../components/physics-registered-adapter.md#diagnostic-capacity-and-publication) passes the contact-point cap to the numerical owner before solving, including registered implementations. Zero disables capture and remains available for failed-world cleanup; synchronized point/count reads retain the bounded shared snapshot. Full numerical and platform acceptance remain separate.


[Linear disk CCD](../components/cpu-continuous-collision.md#linear-disk-grazing-impacts)
uses exact double-precision entry roots for fixed-rotation disk/point pairs before
ordinary native response. It preserves leading-point ray policy and routes other
geometry through the existing general TOI. Shared grazing, impulse, remaining-time
and warmed replay checks execute CPU/GPU/registered variants; full native/platform
and performance acceptance remain open.

## Geometric query brackets

Built-in shape casts now target geometric boundaries in CPU conservative advancement and independent resident GPU query/motion sweeps, rather than a penetration-reduced solver target. Disk queries and linear-disk CCD share the CPU analytic fraction kernel; other represented proxies use geometric distance with 1e-5 scene-unit convergence tolerance. The internal CPU cast explicitly selects this query policy while native solver callers retain their existing policy. Sharp zero-radius contact retains the preceding finite normal. CastMotion keeps eight refinements: analytic first touch must lie between safe/unsafe with maximum width 1/256. The rejected disk 10/12 case returned .87109375/.875; the rejected rectangle .4 case returned .421875/.42578125. Ten circle/segment query combinations against rectangle, capsule, segment, convex and concave targets now verify rounded and sharp boundaries; each runs 128 warm queries at strict 0/0 managed bytes. Bit equality across backends is not required.

[Compound slot checks](../components/physics-registered-adapter.md#compound-slot-numerics) exercise the physical first-touch invariant and retained logical identities.

Solve may run on the captured retained numerical lane, using owner-prepared collider/joint inputs and the existing retained native workers. Started native discrete/CCD faults mark the CPU world failed, drain tasks and release lock/arena/context state before returning the error; pre-native validation keeps its existing retry policy. Owner publication follows the join, and failed world retirement still releases the lane and native world. [Thread checks](../components/physics-thread.md) preserve numerical and allocation gates.


Authored body, target, query/motion and joint-frame angles now use
PhysicsShapeBackend.RotationToBackend (ordinary sine/cosine) at the CPU boundary.
An approximate .37-rad conversion displaced a far analytic plane by about 7.3
scene units; the shared public consumer now verifies the rotated plane at tangent
distance 6,000 within .005 units. Numerical integration is unchanged. See
[boundary verification](../components/physics-registered-adapter.md#registered-boundary-numerics)
for the zero-allocation, lifecycle, impulse and remaining acceptance limits.

Scene-Area scans and external server-Area scans now use each scene body's
effective collision layer. This preserves zero-layer inactive PhysicalBone
followers while retaining configured public bits. [Filter conformance](../components/physics-filters.md#failed-updates-and-effective-sensing)
checks exact sensing, queries, ordinary edits and warm allocation on both owners.


[Special source motion](../components/physics-registered-adapter.md#special-source-motion)
retains represented family across physical pieces and live shape replacement.
Initial recovery/residual stopping still executes; ignored special displacement
cannot manufacture a new sweep, and two concave resources cannot contact through
private edge pieces. Shared public CPU/software-GPU/registered assertions cover
logical slots, analytical boundaries, fractions and zero warmed full-frame bytes.


[Special query displacement](../components/physics-queries.md#special-query-displacement)
retains represented concave/boundary/ray precedence across all four direct shape
operations, current initial contacts and finite boundary endpoints. CPU collinear
circle/core contacts retain a unit normal and corresponding surface points.

Contact publication now uses the shared PhysicsTaskScheduler.RunRanges owner/join
boundary. All ranges finish before errors escape; simultaneous owner/worker
failures preserve both exceptions. Numerical worker selection and callback order
are unchanged. See [publication verification](../components/physics-scene-performance.md#contact-publication-workers-2026-10-20).

[One-way motion entry](../components/physics-queries.md#one-way-body-motion-entry) selects new impacts from their actual contact side. Tangent/receding front-corner hits are distinct from releasing a receding initial overlap; invalid sides cannot hide a later obstacle. Recovery depth and separation-ray source policy retain their contracts.
