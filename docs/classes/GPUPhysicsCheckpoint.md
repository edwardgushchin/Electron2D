# GPUPhysicsBodyStore.Checkpoint

Last updated: 2026-10-10

**Declaration:** `internal sealed class GPUPhysicsBodyStore.Checkpoint : IDisposable`

**Source:** [GPUPhysicsBodyStore.Checkpoint.cs](../../src/Servers/Physics/GPUPhysicsBodyStore.Checkpoint.cs)

**Component:** [Device-local replay checkpoints](../components/gpu-checkpoints.md)

An owner-thread, source-bound internal checkpoint of resident simulation state for
an unchanged authored configuration. Obtain it through `CreateCheckpoint()` on its
store; creation performs the first capture. It owns independent GPU buffers and
retained authored-configuration arrays. It exposes no device handle, portable bytes
or public game-facing replay API.

| Operation | Contract |
| --- | --- |
| `Capture()` | Replace saved device/metadata state without advancing time; flush queued body edits and prepare dirty authoring. Retain storage capacity. A failed recapture invalidates the saved point. |
| `Restore()` | Validate configuration before mutation, then restore device state and sleep/contact/joint histories. Discard pending future body commands, rebuild derived caches and republish live body state. A later copy failure makes the source unusable except for cleanup. |
| `DeviceCapacityBytes` | Reserved checkpoint GPU buffer bytes, independent of live-world storage. |
| `ManagedCapacityBytes` | Logical reserved authored/configuration array payload, excluding object headers and live simulation data. |
| `Dispose()` | Idempotently release buffers and authored arrays and remove the source reference; an executing copy or foreign live owner thread rejects. Source disposal also disposes its checkpoints. |

Motion/forces/targets/sleep can rewind. Body/shape/joint topology, body roles,
integration/mass policy, collision geometry/material/filter rules, Area fields and
world solver settings must still match the captured configuration. Query results
and debug output are transient; changed-body publication is reset. No failed GPU
world can be revived by restoration.

`GPUPhysicsCheckpointTests` exercises physical replay, completed contact reports,
CCD, one-way-history growth, exact restored body data, resource/thread/lifetime and
failure boundaries and warmed allocation. See the component for numeric tolerances,
copy/wait/storage measurements and the open CPU/public/network/lifecycle boundary.
