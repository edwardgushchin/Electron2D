# GPUPhysicsBodyStore

Last updated: 2026-10-08

**Declaration:** `internal sealed unsafe partial class GPUPhysicsBodyStore : IDisposable`

**Source:** [GPUPhysicsBodyStore.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.cs),
[geometry](../../src/Servers/Physics/GPUPhysicsBodyStore.Shapes.cs),
[spatial work](../../src/Servers/Physics/GPUPhysicsBodyStore.Spatial.cs),
[body kernel](../../src/Servers/Physics/Shaders/PhysicsResidentBodies.comp.glsl),
[shape kernel](../../src/Servers/Physics/Shaders/PhysicsResidentShapes.comp.glsl)
**Component:** [Resident GPU body state](../components/gpu-resident-bodies.md)

## Responsibility

Own authoritative device pose/velocity state without creating a Box2D world or
retaining CPU live-state arrays. This internal foundation implements body storage,
edits, integration, shared geometry and broad-phase pair generation. It is not yet
selectable through PhysicsServer and does not perform exact contacts, joints, sleep,
CCD or network replay.

| Operation | Contract |
| --- | --- |
| `Add(BodyDefinition)` | Validate finite authored values, allocate a generation-qualified slot and queue its initial device record. |
| `Remove(BodyHandle)` | Invalidate identity and remove all attached shapes immediately; queue device removal. Reuse gets a fresh generation. |
| `SetPose`, `SetVelocity`, `SetConstantForce`, `ApplyImpulse` | Coalesce edits per slot while preserving setter/impulse order. Velocity assignment supersedes earlier queued impulses; later impulses accumulate. |
| `Step` | Flush pending edits and integrate live bodies on GPU. Static poses stay fixed, kinematics ignore forces/gravity, rigid bodies use mass/inertia/gravity/signed damping, RigidLinear locks rotation. |
| `Read` | Validate caller-owned handles and destination, flush edits without advancing time and gather only requested poses/velocities. |
| `AddShape`, `RemoveShape` | Borrow a shared Shape resource, retain one GPU geometry record per resource and a generation-qualified attachment per shape slot. Body deletion invalidates attachments; resource disposal makes their bounds inactive. |
| `SetShapePose`, `SetShapeFilter` | Coalesce unit-scale local placement and 32-bit layer/mask/sensor edits. |
| `FindPairs` | Flush authored edits without advancing time; derive bounds from device poses, refit/sort the device tree and retain complete canonical shape pairs on GPU. Return only count/error status. Reuse unchanged results; grow/retry an immutable batch on overflow. |
| `ReadPairs`, `ReadShapeBounds` | Explicit diagnostic read into caller-owned pair storage or one nullable bound. Pair order is unspecified; generations and store identity are retained. |
| `Dispose` | Release buffers, pipeline and device reference on the owner thread, including after failure. |

Handles contain slot, generation and store identity. Stale, foreign, removed and
off-thread operations reject before dispatch. Input validation precedes edits.
The shader validates finite results and returns an error word; once submitted work
fails, body reads and mutations reject until disposal. Diagnostic counters remain
readable. There is no CPU recalculation.

CPU arrays retain identity/attachment metadata, borrowed authored resources and pending
commands; consumed commands are cleared. No evolving CPU bounds/tree/pair array is retained. Capacity growth copies evolved state GPU-to-GPU. Native/managed scratch
capacity is retained for zero-allocation warmed work. Reads can also grow scratch
capacity; duplicate requests preserve caller order.

`UploadBytes` counts explicit buffer uploads; `UniformBytes` separately counts
compute settings, `ReadbackBytes` downloaded payloads, `DeviceCopyBytes` growth
copies, and `WaitMS` the cumulative fence wait. These exclude driver protocol and
native allocator overhead. GeometryUploadBytes and ShapeUploadBytes distinguish
resource and attachment edits; BroadPhaseSubmissionCount and PairCapacityRetries
expose query/recovery work. See the component page for measured scope and limits.

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
This is conservative broad phase only: concave/ray response, joint vetoes, explicit
body exceptions and exact sensor/contact events still require subsequent stages.
