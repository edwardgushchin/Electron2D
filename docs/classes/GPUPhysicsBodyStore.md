# GPUPhysicsBodyStore

Last updated: 2026-10-08

**Declaration:** `internal sealed unsafe class GPUPhysicsBodyStore : IDisposable`

**Source:** [GPUPhysicsBodyStore.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.cs),
[resident kernel](../../src/Servers/Physics/Shaders/PhysicsResidentBodies.comp.glsl)
**Component:** [Resident GPU body state](../components/gpu-resident-bodies.md)

## Responsibility

Own authoritative device pose/velocity state without creating a Box2D world or
retaining CPU live-state arrays. This internal foundation implements body storage,
edits, integration and explicit reads. It is not yet selectable through PhysicsServer
and does not perform broad phase, contacts, joints, sleep, CCD or network replay.

| Operation | Contract |
| --- | --- |
| `Add(BodyDefinition)` | Validate finite authored values, allocate a generation-qualified slot and queue its initial device record. |
| `Remove(BodyHandle)` | Invalidate CPU identity immediately; queue device removal. Reuse gets a fresh generation. |
| `SetPose`, `SetVelocity`, `SetConstantForce`, `ApplyImpulse` | Coalesce edits per slot while preserving setter/impulse order. Velocity assignment supersedes earlier queued impulses; later impulses accumulate. |
| `Step` | Flush pending edits and integrate live bodies on GPU. Static poses stay fixed, kinematics ignore forces/gravity, rigid bodies use mass/inertia/gravity/signed damping, RigidLinear locks rotation. |
| `Read` | Validate caller-owned handles and destination, flush edits without advancing time and gather only requested poses/velocities. |
| `Dispose` | Release buffers, pipeline and device reference on the owner thread, including after failure. |

Handles contain slot, generation and store identity. Stale, foreign, removed and
off-thread operations reject before dispatch. Input validation precedes edits.
The shader validates finite results and returns an error word; once submitted work
fails, body reads and mutations reject until disposal. Diagnostic counters remain
readable. There is no CPU recalculation.

CPU arrays contain slot metadata and pending commands only; consumed commands are
cleared. Capacity growth copies evolved state GPU-to-GPU. Native/managed scratch
capacity is retained for zero-allocation warmed work. Reads can also grow scratch
capacity; duplicate requests preserve caller order.

`UploadBytes` counts explicit buffer uploads; `UniformBytes` separately counts
compute settings, `ReadbackBytes` downloaded payloads, `DeviceCopyBytes` growth
copies, and `WaitMS` the cumulative fence wait. These exclude driver protocol and
native allocator overhead. See the component page for measured scope and limits.

## Verification

GPUPhysicsBodyStoreTests checks physical integration, signed damping, edit order,
GPU-preserving growth, reused/foreign handles, owner/lifetime guards and a real
nonfinite GPU result that invalidates the store. The 65,536-body case validates
every body after 384 warmup and 256 sampled ticks. A full diagnostic read occurs
outside the step measurement; explicit two-body reads verify selective transfer.
