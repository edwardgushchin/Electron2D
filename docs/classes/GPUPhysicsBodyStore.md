# GPUPhysicsBodyStore

[Resident policy flag diagnostics](../components/gpu-resident-bodies.md#scalar-body-edit-flags)
check actual CCD/report/sleep bits before and after integration, unrelated locks
and lanes, and strict warmed allocation limits. A scalar-edit candidate did not
correct the native failure and is removed. Temporary staged GPU readbacks locate
the remaining boundary; corrected native execution is still required.

[Direct command-policy access](../components/gpu-resident-bodies.md#scalar-body-edit-flags)
reads the scalar policy word before the aggregate command copy. The two-command,
five-body regression retains existing report/sleep state and requested CCD mode;
corrected AVX-512 target execution remains required.

[Maximum finite point-force reconstruction](../components/physics-areas.md#maximum-finite-point-force-reconstruction)
scales exponent 128 in two finite steps and explicitly retains signs when splitting
components, authored coordinates and strength, while preserving real-overflow rejection.
Both axes and signs execute through the complete point-field contract; corrected
native Windows acceptance remains required.

[Portable body-motion traversal](../components/gpu-resident-motion-queries.md)
shares the recovery, sweep and resting scan call site while preserving iteration
bounds, result buffers and the optimized Vulkan bytecode. Local numerical checks
and complete target execution remain distinct acceptance evidence.

Portable recovery carries recovery state only. Translation retains its bracket
across candidate calls in the existing result prefix; final sampling retains the
deepest accepted contact there before result publication. The four portable
stages use offline optimization and first-vertex bounds. Recovery also retains
its reduction counts/depth, pair flags, correction and convergence input across
geometry calls in the existing prefix. Portable geometry returns both contact
snapshots by value to recovery/final sampling, with the primary point used for
moving refinement. The linked component retains the failed storage-only native
control and separates local checks from target acceptance of the return boundary.
Portable recovery projects its retained planes from the main stage after the
geometry scan returns, preserving the same formula and device passes. The
Windows trace reaches accepted planes but skips the former in-scan projection;
ordinary Windows x64 joint and functional-motion preflights now pass with the new
call boundary. Full executable and final-source platform acceptance remain open.
Ordered motion binds three writable buffers for recovery/translation and four
for final output, matching native reflection. The ARM64 driver control exposed
the former extra descriptor; corrected target execution remains required.

[Shape-query operation kernels](../components/gpu-resident-shape-queries.md#backend-compiler-profiles)
retain separate Intersect/Contacts/Rest/Cast pipelines. Mixed batches use one
compute pass and submission with disjoint original result segments, unchanged
device layouts and result transfers, and accounted per-operation uniforms.

[Query distance scratch](../components/gpu-resident-shape-queries.md#backend-compiler-profiles)
retains three explicit difference-vector slots without dynamic private-array indexing. The
[spring force kernel](../components/physics-joint-policies.md) explicitly skips
its elastic product after complete axial decay. Device records, physical budgets
and public results retain their existing layouts and limits.

[Portable pair hashing](../components/gpu-physics.md#portable-pair-hashing) preserves
the canonical pair/history hash through half-word products and explicit carries.
The shared kernels require neither shaderInt64 nor extended integer multiplication.

[Shared pair/contact completion](../components/gpu-resident-bodies.md#shared-paircontact-completion-2026-10-22)
keeps the pair count on device until contact generation completes, validates both
status records with one wait and preserves bounded pair/contact capacity recovery.
It adds an eight-byte contact status and prepares cold contact scratch even for
empty pair output; the component records its measured capacity cost. No host
simulation mirror is added.

[Shared state/contact completion](../components/gpu-resident-reports.md#shared-completion-for-body-state-and-contacts)
records contact selection with selected body-state publication, sharing one
fence. Changed-body publication retains its separate report read and bounded
dense/sparse policy. Caller spans, generations, all contact fields, caps/order and failed
store handling retain their existing contracts.

[Active joint-batch convergence](../components/physics-joints.md#active-joint-batch-convergence)
uses the actual joint incidence degree after colored contacts complete, preserving
configured sweep counts and the full-degree fallback without host projection or
new device storage. The mixed compound contact/sleep/CCD fixture covers this path.
[Cached active joint degree](../components/physics-joints.md#cached-active-joint-degree)
now reuses the existing 48-byte packed body record, with bounded topology-change
checks and paired dense-joint measurements. The larger hardware regression
failed with device loss and remains open.

[Saturated angular friction](../components/gpu-contact-solver.md#saturated-angular-friction-2026-10-20) now solves coupled normal/tangent contact impulses on device, including a preserved authoritative-replay case that exhausted the unchanged CCD interval budget.

[Buffer memory observations](../components/gpu-memory.md) are implemented in [GPUPhysicsBodyStore.Memory.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.Memory.cs). They track all requested storage/transfer resources, growth overlap and checkpoint copies through native release, without a simulation mirror or extra GPU work. Complete driver/native and managed memory remains outside this counter.

Last updated: 2026-10-23

[Resident Area sorting](../components/physics-areas.md#resident-area-sorting-in-nativeaot) now uses one cached explicit comparison over the existing active prefix. This removes a reproduced 48-byte NativeAOT allocation per dirty-profile sort while retaining priority/traversal order, buffers and transfer/wait policy. The full changed-parameter fixture keeps a strict zero managed-byte gate; native allocation and target GPU performance remain separate.

[Validated color reuse](../components/gpu-contact-colors.md#validated-device-color-cache-2026-10-18) seeds schedules through retained device contact keys, then checks every current row and dynamic endpoint. Any miss/conflict rebuilds the complete schedule. `ContactColorReuseCount` counts accepted seeds; `ContactColorCacheStorageBytes` reports 48-byte record and four-byte table-slot capacity. This adds no CPU contact mirror, portable payload or cache-specific readback. Local GPU checkpoints retain this scheduling cache and its logical lookup size for bounded replay. A reused subset can leave empty color ranges; `ContactColorCount` is their exclusive upper index, and only populated ranges solve. `ContactColorRounds` is zero when validation needs no productive proposal round. Exact native/driver overhead and final hardware acceptance remain outside these counters.

[Reuse before proposals](../components/gpu-contact-colors.md#reused-schedules-without-empty-proposals-2026-10-22)
validates a previously reused schedule before encoding proposal rounds. Stable
reuse removes empty dispatches without another wait; a miss runs the complete
initial budget and can add one completion check. Every current key/endpoint,
configured sweep and overflow fallback remains validated. No storage or public API
is added; whole-step and mass-scene performance acceptance remain open.

[Contact proposal budgets](../components/gpu-contact-colors.md#row-bounded-proposal-batches-2026-10-18) use the current row count to bound initial and extended schedule batches. `ContactColorProposalRoundCount` is a cumulative internal count of encoded complete proposal rounds, including device-empty padding. Reading it creates no GPU work, waits or state mirror; it is diagnostic metadata, not checkpoint state. Color validation, configured solver sweeps and full overflow response remain unchanged.

[Angular normal/friction coupling](../components/gpu-contact-solver.md#angular-normalfriction-coupling-2026-10-16) now preserves oblique rotating-ray response through colored and high-degree resident contacts, without increasing solver sweeps or the CCD budget. The public reset/full-frame fixture checks actual motion and reported impulse balance.

[Portable reconstruction](../components/physics-snapshots.md#temporary-reference-lifetime)
prepares spatial/sleep inputs for a shapeless world before applying its first
snapshot. Capture/apply at tick zero preserves subsequent physical motion.

**Declaration:** `internal sealed unsafe partial class GPUPhysicsBodyStore : IDisposable`

**Source:** [GPUPhysicsBodyStore.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.cs),
[geometry](../../src/Servers/Physics/GPUPhysicsBodyStore.Shapes.cs),
[world queries](../../src/Servers/Physics/GPUPhysicsBodyStore.Queries.cs),
[changed body publication](../../src/Servers/Physics/GPUPhysicsBodyStore.Changes.cs),
[shape queries](../../src/Servers/Physics/GPUPhysicsBodyStore.ShapeQueries.cs),
[body-motion queries](../../src/Servers/Physics/GPUPhysicsBodyStore.MotionQueries.cs),
[mass](../../src/Servers/Physics/GPUPhysicsBodyStore.Mass.cs),
[Area fields](../../src/Servers/Physics/GPUPhysicsBodyStore.Fields.cs),
[kinematic targets](../../src/Servers/Physics/GPUPhysicsBodyStore.Kinematic.cs),
[live body parameters](../../src/Servers/Physics/GPUPhysicsBodyStore.Parameters.cs),
[sleep](../../src/Servers/Physics/GPUPhysicsBodyStore.Sleep.cs),
[continuous collision](../../src/Servers/Physics/GPUPhysicsBodyStore.Continuous.cs),
[owner pipeline preparation](../../src/Servers/Physics/GPUPhysicsBodyStore.Pipelines.cs),
[spatial work](../../src/Servers/Physics/GPUPhysicsBodyStore.Spatial.cs),
[contacts](../../src/Servers/Physics/GPUPhysicsBodyStore.Contacts.cs),
[solver](../../src/Servers/Physics/GPUPhysicsBodyStore.Solver.cs),
[contact colors](../../src/Servers/Physics/GPUPhysicsBodyStore.Colors.cs),
[joints](../../src/Servers/Physics/GPUPhysicsBodyStore.Joints.cs),
[collision exceptions](../../src/Servers/Physics/GPUPhysicsBodyStore.Exceptions.cs),
[one-way episodes](../../src/Servers/Physics/GPUPhysicsBodyStore.OneWay.cs),
[body kernel](../../src/Servers/Physics/Shaders/PhysicsResidentBodies.comp.glsl),
[shape kernel](../../src/Servers/Physics/Shaders/PhysicsResidentShapes.comp.glsl),
[contact kernel](../../src/Servers/Physics/Shaders/PhysicsResidentContacts.comp.glsl),
[constraint preparation/history](../../src/Servers/Physics/Shaders/PhysicsResidentSolve.comp.glsl),
[impulse update](../../src/Servers/Physics/Shaders/PhysicsResidentUpdate.comp.glsl),
[body gather](../../src/Servers/Physics/Shaders/PhysicsResidentGather.comp.glsl)
**Component:** [Resident GPU body state](../components/gpu-resident-bodies.md)

`ReadPoses(ReadOnlySpan<BodyHandle>, Span<System.Numerics.Vector4>)` selects only position and cosine/sine basis. It validates every handle and output capacity, flushes pending edits/masses, gathers the requested sequence, then downloads 16 bytes per request plus eight status bytes with one fence. Duplicate requests may exceed resident population; capacity grows for the request count without adding physical bodies. The existing snapshot read retains its exact 64-byte ABI; integer result storage preserves mode/flag bits. A submission/read failure retains the terminal store policy.

## Responsibility

`PrepareSimulationPipelines(in FieldParameters defaults)` prepares potentially
needed numerical kernels on the original owner before a solver interval borrows
the store. Authored body/shape, field, joint/exception, CCD/kinematic and report
counts select the existing per-store handles. Contact existence is a GPU result,
so two or more shapes prepare possible contact/color kernels without reading
contacts back. Empty stores prepare none. This operation does not simulate,
resize buffers, submit commands or wait for GPU results; warm calls reuse handles.
See [pipeline ownership and retirement](../components/physics-retirement-memory.md#owner-pipeline-preparation).

[Collider attachments](../../src/Servers/Physics/GPUPhysicsBodyStore.Colliders.cs)
are retained by common identity in this store. Creation binds the actual store;
NativeCollider resolves private handles and caches for the existing GPU driver.
Retirement checks attachment reference identity, including failed zero-handle
creation and stale detach. Disposal clears the table. This host identity lookup
does not mirror physical state or submit device work.

Own authoritative device pose/velocity state without creating a Box2D world or
retaining CPU live-state arrays. This internal foundation implements body storage,
edits, automatic/custom mass profiles, center-aware integration, shared geometry, broad-phase pairs and narrow-phase contact
points, material response, contact impulses, pin/groove/spring solving and warm history. PhysicsServer and World now select it through the shared attachment adapters;
[backend integration](../components/physics-backends.md) documents publication and
verification limits. Complete conformance, performance and network replay remain open. See [resident contact response](../components/gpu-contact-solver.md).

Discrete restitution retains incoming normal speed when the current separation can
close within the solve interval. Speculative braking therefore cannot erase a
forthcoming elastic impact; future and receding pairs do not rebound early.
CCD retains its contact threshold. Unequal-mass momentum, energy and completed-frame
impulse checks are described in the [contact solver](../components/gpu-contact-solver.md#solve-and-history).

| Operation | Contract |
| --- | --- |
| `ReadChanges`, `BodySlotCount` | Consume latest observable changes into a retained span sized for all slots. Initial bodies, changed pose/velocity/fields/policy, replacements and removals carry generations; untouched tails remain intact. Device history is private to this single publisher. See [semantics and transfer costs](../components/gpu-body-publication.md). |
| `ChangeDeviceCapacityBytes`, `ChangeTransferCapacityBytes` | Optional history/output and transfer payload capacities, excluding driver overhead. |
| `RetainQueryGeometry`, `QueryGeometry.Dispose` | Borrow a shared resource for standalone query use without a collider or body; edits remain live and owner/disposal/store identity validate before submission. |
| `TestMotion` | Batched supplied-pose recovery and first impact over attached shapes, moving-mask/target-layer filtering, explicit collider/object, pair and joint exclusions, one-way/ray rules, unchanged live poses and typed travel/contact results. See [motion queries](../components/gpu-resident-motion-queries.md). |
| `QueryShapes` | Batched intersections, surface contact pairs, deepest rest contacts with point velocity and new-motion brackets. Explicit masks/exclusions/body/sensor selection, logical caps and unchanged caller tails; see [shape query semantics and limits](../components/gpu-resident-shape-queries.md). |
| `Query`, `SetQueryIdentity` | Batched resident ray/point queries with authored logical identities, layer/body/sensor/canvas filters, exclusion spans and stable capped results. Flushes edits without simulation, retains device geometry and leaves unused caller tails unchanged. See [queries](../components/gpu-resident-queries.md). |
| `QuerySubmissionCount`, `QuerySpatialSubmissionCount` | Search submissions and separate query-only tree preparation; neither enumerates simulation pairs. |
| `Add(BodyDefinition)` | Validate finite authored values, allocate a generation-qualified slot and queue its initial device record. |
| `Remove(BodyHandle)` | Invalidate identity and remove all attached shapes, joints and live exception edges immediately; queue device removal. Reuse gets a fresh generation. |
| `SetCollisionPriority`, `GetCollisionPriority` | Retain finite positive recovery weight in the spare surface lane; sparse edits preserve sleep/history and query spatial residency. |
| `SetMode`, `GetMode` | Change/read the authored solver role without replacing handles or attachments. Nondynamic transitions clear motion, RigidLinear clears angular motion, and dynamic restoration retains configured mass/forces/CCD. |
| `SetIntegrationPolicy`, `GetIntegrationPolicy` | Change/read scalar gravity, authored signed damping and its Combine/Replace modes, dynamic rotation lock, default-force omission and retained scene field eligibility during static/frozen roles. Coalesced edits preserve call order; see [resident body parameters](../components/gpu-resident-parameters.md). |
| `SetPose`, `SetVelocity`, `SetConstantForce`, `ApplyImpulse` | Coalesce edits per slot while preserving setter/impulse order. Velocity assignment supersedes earlier queued impulses; later impulses accumulate. |
| `ApplyForce` | Accumulate resolved world-axis transient force and center torque until the next eligible outer tick. Static/sleeping entry retains it; kinematic or omitted integration consumes it. See [lifetime, traffic and error boundaries](../components/gpu-resident-forces.md). |
| `SetKinematicTarget` | Replace the pending world destination for a kinematic body. Reads/zero-time work retain it; target derivation and continuous path response execute on device. See [kinematic targets](../components/gpu-resident-kinematic.md). |
| `SetAreaFields`, `RemoveAreaFields` | Bind/remove authored Area profiles on static sensor owners; current membership, independent priority channels, field changes and body policy execute on GPU. See [resident fields](../components/gpu-resident-fields.md). |
| `StepFields`, `SimulateFields` | Integration-only/full-step variants accepting directional or point world defaults with signed damping. Plain vector-gravity calls retain zero default damping. |
| `FieldSubmissionCount`, `FieldMS`, `FieldWaitMS` | Field reduction batches and inclusive membership/definition/reduction time plus its included waits. |
| `Step` | Flush pending edits and integrate live bodies on GPU. Static poses stay fixed, kinematics ignore forces/gravity, rigid bodies use mass/inertia/gravity/signed damping, RigidLinear locks rotation. |
| `Simulate` | Split force/contact/pose substeps with physical impulse solving and separate penetration correction. Damping is applied once before the outer tick's force integration; default-force omission preserves contacts and impulses. Defaults: four substeps, captured project iteration count (sixteen), margin 2, captured contact slack/bias (0.3/0.8), correction speed 200 and bounce threshold 100 in scene units. Inherited joint bias remains separately captured (0.2). |
| `SolveConstraints` | Solve contacts, pins, grooves and springs together and prepare correction scratch without advancing pose. Warm history remains device-local and versioned. |
| `AddJoint`, `SetJoint`, `GetJointDefinition`, `RemoveJoint` | Own generation-safe device connections and authored settings, validated local frames, per-joint bias/softness/force/correction policies and independent collision vetoes; endpoint removal unlinks dependent joints. See [resident joints](../components/gpu-resident-joints.md). |
| `SetCollisionException`, `HasCollisionException` | Retain directed live-body exceptions; either direction vetoes solid pairs. Joint contributions remain independent and sensors retain directional masks. Endpoint removal unlinks incoming/outgoing edges; see [resident exceptions](../components/gpu-resident-exceptions.md). |
| `ExceptionUploadBytes`, `FilterSubmissionCount`, `FilterMS`, `FilterWaitMS` | Sparse exception bytes, combined joint/exception update batches, their total time and included fence waits. Unchanged filters stay resident. |
| `SetMassProfile`, `GetMassProfile`, `GetMassProperties` | Change/read authored kilograms, zero/explicit inertia and nullable auto/custom center; resolve geometry without moving origin/velocity. See [resident mass](../components/gpu-resident-mass.md). |
| `SetShapeOneWay`, `GetShapeOneWay`, `OneWayPairCount` | Normalized local direction and finite recovery margin; GPU piece-pair side decisions include rejected episodes and expire on separation or shape/resource revision. Sensors ignore the policy; see [resident one-way contacts](../components/gpu-resident-one-way.md). |
| `SetShapeMaterial` | Journal finite signed friction/bounce using the existing rough/absorbent convention. |
| `SetSleeping`, `SetCanSleep`, `GetCanSleep`, `SetSleepSettings`, `GetSleepSettings` | Device dynamic sleep policy, ordered explicit sleep/wake and connected automatic sleep; see [resident sleep](../components/gpu-resident-sleep.md). |
| `SetCCDMode`, `GetCCDMode` | Internal Disabled/CastRay/CastShape policy with independent GPU swept bounds, contact intervals and no CPU trajectory mirror; see [CCD](../components/gpu-resident-ccd.md). |
| `CCDQueryCount`, `CCDIntervalCount`, `CCDWaitMS` | TOI dispatches, split/refinement intervals and TOI summary waits. |
| `ActiveSimulationBodyCount` | Last completed simulation count of awake dynamics and moving nondynamic surfaces. Version-checked zero enables an unchanged idle-world skip. |
| `Read` | Validate caller-owned handles and destination, flush edits without advancing time and gather only requested poses/velocities/sleep, role and effective lock/omission flags. The record is now 64 bytes including last resolved gravity/damping and initialization state. |
| `AddShape`, `RemoveShape` | Borrow a shared Shape resource, retain one GPU geometry record per resource and a generation-qualified attachment per shape slot. Body deletion invalidates attachments; resource disposal makes their bounds inactive. |
| `SetShapePose`, `SetShapeFilter` | Coalesce unit-scale local placement and 32-bit layer/mask/sensor edits. |
| `FindPairs` | Flush authored edits without advancing time; derive bounds from device poses, refit/sort the device tree and retain complete canonical shape pairs on GPU. Return only count/error status. An optional nonnegative scene-unit margin expands both bounds by half the margin. Reuse unchanged results; grow/retry an immutable batch on overflow. |
| `ReadPairs`, `ReadShapeBounds` | Explicit diagnostic read into caller-owned pair storage or one nullable bound. Pair order is unspecified; generations and store identity are retained. |
| `FindContacts` | Derive complete contact points from resident pairs, shapes and bodies. Retain normal, signed separation, local anchors and features on GPU; return only count/error. Physical contacts use the optional margin and latched one-way decisions, sensors require exact overlap. Accepted and rejected one-way piece episodes retain separate device history, published only after complete capacity recovery. Output capacity recovery repeats no integration. |
| `ReadContacts` | Explicitly copy 64-byte point records into caller-owned storage. Point order is unspecified; records contain local store slot/generation identities, not portable network IDs. |
| `Dispose` | Release buffers, pipeline and device reference on the owner thread, including after failure. |

Handles contain slot, generation and store identity. Stale, foreign, removed and
off-thread operations reject before dispatch. Input validation precedes edits.
The shader validates finite results and returns an error word; once submitted work
fails, body reads and mutations reject until disposal. Diagnostic counters remain
readable. There is no CPU recalculation.

CPU arrays retain identity/attachment metadata, borrowed authored resources and pending
commands; consumed commands are cleared. No evolving CPU bounds/tree/pair/contact array is retained. Capacity growth copies evolved state GPU-to-GPU. Native/managed scratch
capacity is retained for zero-allocation warmed work. Reads can also grow scratch
capacity; duplicate requests preserve caller order.

`UploadBytes` counts explicit buffer uploads; `UniformBytes` separately counts
compute settings, `ReadbackBytes` downloaded payloads, `DeviceCopyBytes` growth
copies, and `WaitMS` the cumulative fence wait. These exclude driver protocol and
native allocator overhead. GeometryUploadBytes and ShapeUploadBytes distinguish
resource and attachment edits; BroadPhaseSubmissionCount and PairCapacityRetries
expose query/recovery work. ContactPointCount, ContactSubmissionCount and
ContactCapacityRetries report the narrow-phase boundary. Solver coefficients use
64-byte records of precomputed contact Jacobians; separate 32-byte records hold
iterated impulses. Update/gather kernels bind only their own inputs/outputs.
ProfileSolverPasses (default false) inserts diagnostic dispatch fences and records
seven cumulative SolverPassMS values; it changes batching, and its times include
submission overhead rather than isolated GPU execution. See the component page for measured scope and limits.

## Verification

GPUPhysicsBodyStoreTests checks physical integration, signed damping, edit order,
GPU-preserving growth, reused/foreign handles, owner/lifetime guards and a real
nonfinite GPU result that invalidates the store. The 65,536-body case validates
every body after 384 warmup and 256 sampled ticks. A full diagnostic read occurs
outside the step measurement; explicit two-body reads verify selective transfer.

GPUPhysicsSpatialTests checks all seven current geometry families, composed transforms,
large convex contours, shared revision/disposal with throwing observers, body/shape
reuse, growth, invalid device bounds and complete unordered pairs. A dense 96-body
case forces pair storage recovery; 36 randomized worlds compare masks, sensors and
roles with a brute-force AABB oracle across refits and resorting. Moving grids of
4,096 and 65,536 bodies check every neighbor pair outside the warmed measurement.
GPUPhysicsContactStoreTests additionally checks 1,620 rotated pair cases against
resource collision regions across all seven current geometry families, both polygon
windings and both directed-ray slope modes. It checks local-anchor/normal/depth
invariants, speculative/sensor differences, mutable/disposed geometry, identity,
feature stability, complete concave-piece output and failed-state rejection after
nonfinite device intermediates. Moving grids verify every point against actual GPU
poses outside the timed window; details and measured transfer costs are in the
component report. Joint collision vetoes and explicit live-body exceptions now execute on the device;
public GPU exception projection and sensor/contact event publication now execute through PhysicsSpace. Contact
impulses/material response and warm history now execute through the solver component.

GPUPhysicsSolverStoreTests covers analytic momentum/energy/inertia, signed materials,
stationary linear/angular surfaces, directed-ray response, history reuse/invalidation/
growth, 64/257-point incident lists and all-sensor transitions, separate correction, failed-state rejection, a ten-second eight-box stack and
complete gravity-loaded 4,096/65,536-circle populations. SolverSubmissionCount,
WarmStartedPointCount, SolverMS and SolverWaitMS expose actual work; full backend,
networking and window performance remain open.

GPUPhysicsJointStoreTests covers independent pin/groove/spring response, momentum,
static/world anchors, device history growth, collision-veto contribution lifetime,
combined contact response, failed-state rejection and 4,096 warmed world pins.
JointCount and JointUploadBytes expose authored population/traffic; no joint warm
state is mirrored on the CPU. Its component report states the remaining public
settings and integration limits.

GPUPhysicsMassStoreTests compares all current shape mass profiles against public
CPU getters and checks device motion/contact/joint lever arms at custom/automatic
centers, profile/impulse order, resource revisions and zero-allocation warm edits.
AuthoredBodyCapacityBytes measures only retained CPU body-slot/command payload.
Local centers use a separate 8-byte device record and no hot full-state mirror.

GPUPhysicsSleepStoreTests verifies contact/joint components, scoped wake after support removal, generation reuse, ordered commands, body/world policy and zero-allocation active sleep cycles. Selected Snapshot now includes Sleeping, CanSleep and SleepTime; the current 64-byte record also includes resolved fields. PhysicsSpace consumes it before public sleep/contact callbacks.

GPUPhysicsJointPolicyTests checks internal per-joint bias, vector correction/force
caps, inverse-mass pin softness, shared physical/correction budgets, original
substep budgeting through CCD, rejected edits and warmed allocation. The independent GPU public-world adapter now executes; full policy-family conformance remains open; see [joint policy verification](../components/gpu-resident-joints.md#policy-verification-2026-10-09).

GPUPhysicsExceptionStoreTests verifies directed and joint contributions, sensor policy,
endpoint reuse, connected wakeup, contact-history retirement and both CCD modes.
Its 8,192-body/4,096-entry workload checks complete filtered populations and
128 warmed live-edit ticks with zero managed allocations; see the exception report.

GPUPhysicsTransientForceTests compares single-use force/torque with the public CPU
API and covers substeps, sleep, omission, CCD, joints, growth and generation reuse.
The resident force buffer adds 16 device bytes per body; sparse commands are now
176 bytes after kinematic target/surface fields. Warmed owner-thread allocations remain zero in the checked workload.

Static/kinematic velocity is now virtual surface motion. SetPose teleports and
cancels pending targets; Snapshot.Velocity combines actual target travel with
virtual motion. GPUPhysicsKinematicTests verifies this against the public CPU
contract and checks target/idle/continuous/contact/joint behavior and allocation.

GPUPhysicsFieldTests compares priority, point/damping/body modes, actual geometry,
wake/lifetime/error behavior against public CPU operations and measures a complete
4,096-receiver field population with zero warmed owner-thread allocations.

Contact publication is opt-in through CaptureContactReports. ReadContactReports
selects each requested body's incident contacts on device with independent limits,
returning observed generations, positions, receiver normal/impulse and final point
velocities. Completed snapshots survive later authoring and zero-time calls.
ReportPublicationCount/ReportReadCount count completed frame/read batches;
ReportDeviceCapacityBytes/ReportTransferCapacityBytes expose retained report storage. Selected rows are packed on device and bounded by frame records and request multiplicity, retaining one completion. Host validation/scatter preserves original caller segments and allows aliased limits/counts; unused slots stay untouched. Scratch epochs/counts retain only duplicate-request bookkeeping, not numerical state. No CPU
contact mirror is retained; [publication storage and traffic](../components/gpu-resident-reports.md)
are separate from integration/solver benchmarks. GPUPhysicsReportTests checks the
shared public momentum/reporting contract and the internal publication lifecycle.

## Continuous collision policy

The shared public CCDMode now supplies the scene/server setting and resident GPU
configuration. CPU worlds inspect solved motion before publication and preserve
remaining tick time and nominal force/joint budgets through impact continuations.
The runtime stores policy across attachments and roles and projects it into the selected CPU or independent GPU world. See [implementation and verification](../components/cpu-continuous-collision.md).

WorldBoundaryShape is retained as an analytic plane (geometry kind 7). Its tree
flag bypasses finite bounds; free-side support culling limits candidates. Device
manifolds, ray/point/shape/body-motion queries and translating/rotating CCD use the
plane equation. WorldBoundaryTests additionally verifies sensors, sleeping support,
resource edits and removal. See the [boundary report](../components/physics-shapes.md#infinite-world-boundaries).
Public scene/server worlds consume this same resident store through PhysicsSpace; complete numerical/platform/performance acceptance remains open.

GetSleepSettings and SetSleepSettings now use shared PhysicsSleepSettings, sampled
from project defaults at store creation. Thresholds and duration use strict comparisons;
zero speed thresholds disable automatic sleep. Changed policy queues ordinary ordered
wake commands for live dynamics, so zero-time reads see the wake and a later explicit
sleep wins. Unchanged settings retain state. [World sleep policy](../components/physics-sleep.md)
records the per-edit transfer cost and shared CPU checks.

ContactPolicy captures shared world defaults and queues ordinary wake commands on edits. Shape policy uses an existing device word and an independent epoch; no vertex or pose mirror is introduced. Solver uniforms are 80 bytes and carry the nominal-tick exponent and separate inherited joint bias. See [contact policy](../components/physics-contact-policy.md).

GetSolverIterations/SetSolverIterations expose the internal captured world policy.
Simulate, SimulateFields and SolveConstraints use it when their optional iterations
argument is absent; an explicit diagnostic argument affects only that call.
Changed world counts use ordered dynamic wake commands, preserving a later explicit
sleep edit. Equal/invalid writes do not invalidate history or wake bodies. Counts
flow unchanged into CCD continuations; force/time budgets remain separate.

Resident contact history now uses 96-byte records, including true local boundary
anchors. Hashing groups a shape/piece pair; feature identity is preferred, with a
nearby-anchor fallback bounded by world recycle radius and maximum separation.
A claim word in existing geometry metadata gives each old impulse at most one new
consumer. Matching/claims stay on GPU; no history readback or CPU pose mirror is
introduced. The existing 80-byte solver uniform carries both new distances in its
last two spare components. [Contact history limits](../components/physics-contact-policy.md#contact-history-limits)
documents storage and verification.

CollisionPriority metadata occupies Surface.W without changing the 96-byte body
or 176-byte command layout. A separate command bit changes only that lane; role and
surface-velocity edits preserve it. Priority-only batches do not advance spatial
version, wake bodies or retire solver history. Motion recovery keeps at most 32
weighted planes in a 512-byte/request device-only output-buffer tail and normalizes large weights without a
subnormal reciprocal. This adds no CPU physical-state mirror or contact readback.

The motion shader reuses geometry call sites across overlap, ray refinement and
impact phases to reduce cold driver compilation. Query precision, recovery passes,
buffers and transfers are unchanged; [motion-query verification](../components/gpu-resident-motion-queries.md#cold-pipeline-preparation)
separates startup cost from warmed query throughput.

Shared-world adapters can set an explicit stable Area traversal rank, separate
actual solver velocity from virtual surface velocity, update default joint bias,
and publish external wake propagation without advancing time. These internal
operations preserve existing direct-store defaults.

## Bounded contact diagnostics

[GPUPhysicsBodyStore.DebugContacts.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.DebugContacts.cs)
prepares a point buffer, 8-byte status and retained transfer storage only on demand.
The solver records a clear and compaction pass before advancing poses. The shader
validates shape/body generations and finite world positions, and omits sensors,
nonpenetrating and sleeping pairs. The last solver batch replaces earlier samples.

ReadDebugContacts copies status plus a previous-count-sized prefix, then the exact
remaining tail when the result grows. All bytes, submissions and waits contribute
to the ordinary counters. A failed result leaves the store failed; disabling remains
permitted for cleanup without authorizing subsequent reads or steps. Zero limit
skips diagnostic compute and readback. Capacity is retained until disposal. The
[component report](../components/physics-debug.md#contact-point-snapshots) distinguishes
these explicit diagnostic transfers from ordinary simulation and body publication.

## Independent velocity-component commands

Internal `SetSolverLinearVelocity(BodyHandle, Vector2)` and
`SetSolverAngularVelocity(BodyHandle, float)` validate and queue only their respective
components. They preserve the other component and its pending impulses on device,
keep virtual surfaces separate, and obey full-write/sleep/lock/role ordering. The
command and body layouts are unchanged; two command-mask bits carry the selection.
No state read or publication is needed to preserve the other component. Read/query
and step paths retain the existing owner/failure/generation guards and freshness.
See [component writes and measurements](../components/gpu-resident-bodies.md#component-velocity-writes).

`GetRotationLocked(BodyHandle)` derives current lock policy from the existing authored
role/integration slot after the ordinary owner/generation/failure checks; it requires
no GPU readback. `ChangePublicationCount` counts successful nonempty-world change
publication operations for diagnostics, including publications with zero changed
records. It changes no stream semantics or public API. The common world now
[omits pre-step publication without a consumer](../components/physics-backends.md#conditional-body-publication).

## Integration submission batching

The [contact scheduler](../components/gpu-contact-colors.md) colors and compacts
prepared constraints on device, verifies that a color has no shared dynamic body,
and dispatches independent ranges in sequence. Coupled joint rows follow each sweep.
Color overflow retains all constraints on the GPU Jacobi path. ContactColorCount,
ContactColorRounds and ContactColorFallbacks are internal diagnostics. Derived
schedule scratch is rebuilt after checkpoint restore and has no public identity. Proposal batches use the prior completed round count plus one (minimum four), extend unfinished graphs in batches of sixteen and retain the existing 32-color/64-round fallback.

Colored solving packs coefficients/impulses by color and velocity/correction/mass/
surface data into 48-byte body records. Joints consume those current velocities;
final values return to the owning records before integration, history and reports.
All scratch remains on device, is reused after warmup and released by the store.
The [layout measurements](../components/gpu-contact-colors.md#packed-iterative-state)
state the additional capacity and diagnostic layout controls.

SimulateFields records velocity integration before spatial kernels, or before the
solver for shape-free worlds. Discrete position integration and connected sleep
follow report capture in the solver command buffer. Body and solver/spatial status
remain separate and are validated at the shared fence. Continuous-impact position
intervals retain their own submissions. No new host body-state mirror or asynchronous
public-state lifetime is introduced. See [measured batching](../components/gpu-contact-solver.md#integration-batching-measurements).

## Local replay checkpoint foundation

`CreateCheckpoint()` returns a source-bound internal
[Checkpoint](GPUPhysicsCheckpoint.md) retaining device-local motion, force, sleep,
contact, one-way and joint history for the current authored configuration. Restore
republishes bodies and invalidates derived query/debug caches. It is separate from
observable `Snapshot`/`ReadChanges` data and is not a public, CPU or portable network
checkpoint. [Scope, validation and measurements](../components/gpu-checkpoints.md)
keep those remaining requirements explicit.

[Report-only contacts](../components/physics-report-only.md) now include kinematic/static and kinematic/kinematic pairs on CPU/GPU when either endpoint enables reporting. Impulses and positional response are zero; two static bodies remain ineligible. Caps, captured owners/indices, frozen-body scene events and checkpoint metadata share the existing contact contract.

Idle kinematic velocity clearing and unchanged targets do not generate a wake
when real motion is already zero. Actual platform movement/stopping still wakes
contacts, while explicit assignments retain their wake and ordering semantics.
See [resident kinematics](../components/gpu-resident-kinematic.md#idle-platform-wake-policy).

## Capacity growth without new managed wrappers

Private storage and transfer buffers now retain their existing RenderHandle and device reference while adopting a larger native allocation. Preserved storage still copies its old capacity and completes the copy before retirement; copy/wait accounting and capacity-recovery rules are unchanged. Failed native creation leaves the old allocation owned, pre-adoption failures retire the candidate, and an old-release failure leaves the adopted replacement available for terminal cleanup. No public resource or borrowed native identity is retargeted.

[The full scene pipeline fixture](../../tests/Electron2D.Tests/PhysicsPipelinePerformance.cs) now runs explicit CPU/GPU World selection, actual RigidBody monitors and interpolation through the same PhysicsFrame workload. The 64-body 256-warmup/64-sample control previously allocated 1344 owner/all-thread bytes when twelve resident buffers crossed their capacity after warmup. Reusing wrappers removed those managed allocations while preserving the same physical gates and population. Native allocations and copies still occur and retain their separate costs. [The private lifetime test](../../tests/Electron2D.Tests/RenderHandleReplacementTests.cs) covers parent retention, closed adoption, cleanup failure and exact release.

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.

Body changes use an adaptive bounded download for dense output and count-first exact-prefix download for sparse output. The previous successful count is only a scheduling hint; validation, output-prefix ownership and terminal post-submission failure policy are unchanged. See [publication transfer costs](../components/gpu-body-publication.md).


Contact-color and packed-state resources are prepared before solver command recording. A scoped internal allocation diagnostic can initialize each new storage buffer; tests use NaN and finite nonzero contents in separate worlds to verify first-use initialization. [Preparation and limits](../components/gpu-contact-colors.md#scratch-initialization-and-preparation-order).

The public GPU adapter retains one rebindable query-geometry lease. Alternating live resources reuses its wrapper and retires the previous reference; past resources are not accumulated. ConvexPolygonShapeTests exercises 128 warmed overlap/contact/rest/motion cycles between shared and standalone geometry on CPU/GPU with zero owner/all-thread bytes, preserving current contour results and initial-overlap fractions. GPUPhysicsShapeQueryTests checks successful replacement, null/disposed/thread rejection with preserved previous geometry, and retired lease failure. Geometry uploads and query waits retain their existing separate costs.

The original owner may grant one exact numerical thread a temporary exclusive borrow while waiting for Solve. Only that thread or the owner can reach private state then; acquisition/use/submission of each command stay together. Borrowing ends before public reads/publication and device retirement remains on the original owner. [Separate-thread GPU execution](../components/physics-thread.md) is implemented but unverified; its native gate remains open.

## Compact callback activity (2026-10-14)

ReadActivity returns one uint active flag per resident slot (zero for dead/static/sleeping), validates output capacity, applies queued edits and reuses the packed result buffer. It reads no body requests, poses or velocities. The independent stage-6 shader computes these flags on device; failure retains the same terminal submission policy as selected reads.

The resident point-field reducer preserves guarded float operation order with GLSL precise intermediates and propagates reconstruction validity separately from the value. Values beyond the finite output range do not enter subsequent arithmetic as infinity sentinels. A later Replace/ReplaceCombine contribution resets validity with the channel; Combine retains an invalid earlier contribution. Final invalid fields fail before body-field publication or solver motion through the existing eight-byte status read and terminal GPU-world policy. Mantissa/exponent normalization avoids reciprocals outside the normal range for large/tiny coordinates. No CPU mirror, buffer growth, new readback or wait is introduced. [Hardware verification](../components/physics-areas.md#hardware-point-field-guards-2026-10-14).


For resident CCD against a rotating plane, resting-contact advancement now bounds
the actual plane-support acceleration and its third derivative, with a bounded
fourth-derivative remainder over the remaining interval. A convex quadratic bound
uses its endpoint maximum across all finite support vertices. The earlier
radius-only curvature overestimate exhausted 128 intervals for a radius-one circle
near a 20-rad/s dynamic plane despite representable contact motion. Interval and
shader iteration budgets, .001 CCD tolerance, failure behavior and device-resident
execution remain unchanged. The same new scenario and existing continuous suites
verify the correction; no CPU numerical fallback or mass-state readback is added.


The offset-center directed arc also exposed the resident circle-entry quadratic:
its float discriminant cancelled large products and retained an exit/tangent
report without the required entry impulse. The shared resident ray-circle kernel
now projects onto a normalized ray basis, computes the half chord from perpendicular
distance and forms the entry normal in that basis. It retains the same facing
threshold, geometry, budgets and device-float portability. All users of the shared
kernel, including capsule caps, contacts, queries, motion and CCD, receive the
same correction. No FP64 requirement, CPU geometry fallback or extra readback is
introduced. Full continuous/query regressions remain separate from provider
acceptance and hardware performance.


The complete offset-center angular scenario still required a CCD correction after
the stable circle-root change: a near separated proxy without a directed entry
was incorrectly treated as no hit for the entire remaining interval. The resident
CCD kernel now retains the containment exclusion and continues projected-velocity/
curvature refinement until a real directed entry, keeping its existing 256-iteration
and 128-interval budgets. The first speed-only continuation exhausted refinement
and was rejected. A focused --directed-angular case uses only the public API and
retains contact/energy assertions; its successful GPU probe records a real nonzero
entry impulse. Earlier cohorts before this correction are not final acceptance.


[Moving one-way recovery](../components/gpu-resident-motion-queries.md#moving-one-way-recovery)
now includes kinematic/dynamic linear approach over the last physical step on
both production backends, including private/portable restoration. Static surface,
receding/tangent and angular-only velocities do not enlarge recovery depth.
PhysicsMovingOneWayTests checks raw/scene callers, roles, rotated slots, six finite
source families and eight 128-warmup/128-measured complete-frame brackets at zero
managed allocation. Native/platform/hardware/performance acceptance remains open.

## Collider sensor query reuse

QueryGeometry retains ordinary terminal Dispose/Replace behavior. A separate
internal RebindColliderSlot path lets its sole collider owner reuse a released
wrapper, validating owner thread, live source and the same store. Every clear
releases geometry; empty cached wrappers hold no Shape reference. See [sensor edit
ownership and checks](../components/physics-queries.md#failed-raw-shape-edits-and-warmed-reuse).

[One-way motion entry](../components/physics-queries.md#one-way-body-motion-entry) selects new impacts from their actual contact side. Tangent/receding front-corner hits are distinct from releasing a receding initial overlap; invalid sides cannot hide a later obstacle. Recovery depth and separation-ray source policy retain their contracts.
