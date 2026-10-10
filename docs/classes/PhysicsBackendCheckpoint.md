# PhysicsBackendCheckpoint

Last updated: 2026-10-11

**Declaration:** `internal abstract class PhysicsBackendCheckpoint : IDisposable` · **Source:** [PhysicsBackendCheckpoint.cs](../../src/Servers/Physics/PhysicsBackendCheckpoint.cs) · **Component:** [Physics backends](../components/physics-backends.md#native-simulation-and-replay-ownership)

## Description and internal contract

Selected implementation history owned by a common PhysicsSpace.Checkpoint. It never transfers to another world and exports no native handles. The caller validates the space's owner thread, idle phase, callbacks, authoring and membership before capture/restore; each implementation additionally validates its private history and solver lifetime. A failed restore marks the common world unusable. World release disposes all borrowed checkpoint contexts before releasing its implementation.

| Operation | Contract |
| --- | --- |
| `long DeviceCapacityBytes { get; }` | Retained device capacity for GPU history; zero for CPU history. |
| `void Capture()` | Replace this reusable completed snapshot, retaining prepared scratch. |
| `void ValidateRestore()` | Check lifetime, implementation configuration and native/device identities before mutation. |
| `void Restore()` | Rewind solver state and implementation-local observer caches without user callbacks. |
| `void Dispose()` | Release history and clear retained references; repeated disposal is harmless. |

## Concrete histories

CPUPhysicsWorldBackend.SolverCheckpoint owns CPUPhysicsCheckpoint and reusable copies of one-way episodes, BodyMotion arrays, FrameContact lists/indices/heads/tails, report-only pair/activity counts and native contact-step metadata. Native identity/configuration validation runs before restore. Captured report-only statistics are immediately restored along with contact data.

[PhysicsSpace.GPUCheckpoint](PhysicsSpaceGPUCheckpoint.md) wraps GPUPhysicsBodyStore.Checkpoint and retains GPU report/range/epoch/wake and collider-publication caches. Common logical scene observers remain in PhysicsSpace.Checkpoint; no additional full CPU solver mirror or state readback is created.

These types are internal, not registered public backend factories. [PhysicsReportOnlyTests](../../tests/Electron2D.Tests/PhysicsReportOnlyTests.cs) checks immediate contact/statistics replay and 64 warmed capture/restore/read cycles at zero owner/all-thread bytes on both implementations. [PhysicsCheckpointTests](../../tests/Electron2D.Tests/PhysicsCheckpointTests.cs) checks public world/scene identity, topology/authoring validation, lifetime, callbacks and warmed replay. Native allocator, foreign-platform execution and complete registration acceptance remain open.

Selected operations borrow world/point lifetime, including capacity reads and release. Recursive checkpoint access and release reject; cleanup failure still retires common point state. PrepareCheckpoint executes before kernel capture. See [ownership and failure checks](../components/physics-backends.md#selected-checkpoint-ownership).
