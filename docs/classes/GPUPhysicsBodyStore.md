# GPUPhysicsBodyStore

Last updated: 2026-10-09

**Declaration:** `internal sealed unsafe partial class GPUPhysicsBodyStore : IDisposable`

**Source:** [GPUPhysicsBodyStore.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.cs),
[geometry](../../src/Servers/Physics/GPUPhysicsBodyStore.Shapes.cs),
[mass](../../src/Servers/Physics/GPUPhysicsBodyStore.Mass.cs),
[sleep](../../src/Servers/Physics/GPUPhysicsBodyStore.Sleep.cs),
[continuous collision](../../src/Servers/Physics/GPUPhysicsBodyStore.Continuous.cs),
[spatial work](../../src/Servers/Physics/GPUPhysicsBodyStore.Spatial.cs),
[contacts](../../src/Servers/Physics/GPUPhysicsBodyStore.Contacts.cs),
[solver](../../src/Servers/Physics/GPUPhysicsBodyStore.Solver.cs),
[joints](../../src/Servers/Physics/GPUPhysicsBodyStore.Joints.cs),
[body kernel](../../src/Servers/Physics/Shaders/PhysicsResidentBodies.comp.glsl),
[shape kernel](../../src/Servers/Physics/Shaders/PhysicsResidentShapes.comp.glsl),
[contact kernel](../../src/Servers/Physics/Shaders/PhysicsResidentContacts.comp.glsl),
[constraint preparation/history](../../src/Servers/Physics/Shaders/PhysicsResidentSolve.comp.glsl),
[impulse update](../../src/Servers/Physics/Shaders/PhysicsResidentUpdate.comp.glsl),
[body gather](../../src/Servers/Physics/Shaders/PhysicsResidentGather.comp.glsl)
**Component:** [Resident GPU body state](../components/gpu-resident-bodies.md)

## Responsibility

Own authoritative device pose/velocity state without creating a Box2D world or
retaining CPU live-state arrays. This internal foundation implements body storage,
edits, automatic/custom mass profiles, center-aware integration, shared geometry, broad-phase pairs and narrow-phase contact
points, material response, contact impulses, pin/groove/spring solving and warm history. It is not yet
selectable through PhysicsServer; joint bias/softness/general caps, public state/event publication
and network replay remain open. See [resident contact response](../components/gpu-contact-solver.md).

| Operation | Contract |
| --- | --- |
| `Add(BodyDefinition)` | Validate finite authored values, allocate a generation-qualified slot and queue its initial device record. |
| `Remove(BodyHandle)` | Invalidate identity and remove all attached shapes and joints immediately; queue device removal. Reuse gets a fresh generation. |
| `SetPose`, `SetVelocity`, `SetConstantForce`, `ApplyImpulse` | Coalesce edits per slot while preserving setter/impulse order. Velocity assignment supersedes earlier queued impulses; later impulses accumulate. |
| `Step` | Flush pending edits and integrate live bodies on GPU. Static poses stay fixed, kinematics ignore forces/gravity, rigid bodies use mass/inertia/gravity/signed damping, RigidLinear locks rotation. |
| `Simulate` | Split force/contact/pose substeps with physical impulse solving and separate penetration correction. Defaults: four substeps, sixteen iterations, margin 2, allowed penetration 0.5, correction factor 0.2, correction speed 200 and bounce threshold 100 in scene units. |
| `SolveConstraints` | Solve contacts, pins, grooves and springs together and prepare correction scratch without advancing pose. Warm history remains device-local and versioned. |
| `AddJoint`, `SetJoint`, `GetJointDefinition`, `RemoveJoint` | Own generation-safe device connections and authored settings, validated local frames and independent collision vetoes; endpoint removal unlinks dependent joints. See [resident joints](../components/gpu-resident-joints.md). |
| `SetMassProfile`, `GetMassProfile`, `GetMassProperties` | Change/read authored kilograms, zero/explicit inertia and nullable auto/custom center; resolve geometry without moving origin/velocity. See [resident mass](../components/gpu-resident-mass.md). |
| `SetShapeMaterial` | Journal finite signed friction/bounce using the existing rough/absorbent convention. |
| `SetSleeping`, `SetCanSleep`, `GetCanSleep`, `SetSleepSettings`, `GetSleepSettings` | Device dynamic sleep policy, ordered explicit sleep/wake and connected automatic sleep; see [resident sleep](../components/gpu-resident-sleep.md). |
| `SetCCDMode`, `GetCCDMode` | Internal Disabled/CastRay/CastShape policy with independent GPU swept bounds, contact intervals and no CPU trajectory mirror; see [CCD](../components/gpu-resident-ccd.md). |
| `CCDQueryCount`, `CCDIntervalCount`, `CCDWaitMS` | TOI dispatches, split/refinement intervals and TOI summary waits. |
| `ActiveSimulationBodyCount` | Last completed simulation count of awake dynamics and moving nondynamic surfaces. Version-checked zero enables an unchanged idle-world skip. |
| `Read` | Validate caller-owned handles and destination, flush edits without advancing time and gather only requested poses/velocities. |
| `AddShape`, `RemoveShape` | Borrow a shared Shape resource, retain one GPU geometry record per resource and a generation-qualified attachment per shape slot. Body deletion invalidates attachments; resource disposal makes their bounds inactive. |
| `SetShapePose`, `SetShapeFilter` | Coalesce unit-scale local placement and 32-bit layer/mask/sensor edits. |
| `FindPairs` | Flush authored edits without advancing time; derive bounds from device poses, refit/sort the device tree and retain complete canonical shape pairs on GPU. Return only count/error status. An optional nonnegative scene-unit margin expands both bounds by half the margin. Reuse unchanged results; grow/retry an immutable batch on overflow. |
| `ReadPairs`, `ReadShapeBounds` | Explicit diagnostic read into caller-owned pair storage or one nullable bound. Pair order is unspecified; generations and store identity are retained. |
| `FindContacts` | Derive complete contact points from resident pairs, shapes and bodies. Retain normal, signed separation, local anchors and features on GPU; return only count/error. Physical contacts use the optional margin, sensors require exact overlap. Output capacity recovery repeats no integration. |
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
component report. Joint collision vetoes now execute on the device;
explicit body exceptions and sensor/contact event publication remain unconnected. Contact
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

GPUPhysicsSleepStoreTests verifies contact/joint components, scoped wake after support removal, generation reuse, ordered commands, body/world policy and zero-allocation active sleep cycles. Selected Snapshot now includes Sleeping, CanSleep and SleepTime in 48 bytes. It is internal state publication, not public event delivery.
