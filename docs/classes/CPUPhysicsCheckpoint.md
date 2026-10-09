# CPUPhysicsCheckpoint

Last updated: 2026-10-10

**Declaration:** `internal sealed class CPUPhysicsCheckpoint : IDisposable`

**Source:** [CPUPhysicsCheckpoint.cs](../../src/Servers/Physics/CPUPhysicsCheckpoint.cs)

**Component:** [CPU solver checkpoints](../components/cpu-checkpoints.md)

An owner-thread checkpoint of one live managed CPU solver. Construction captures
immediately. Storage contains independent persistent solver data and retained
borrowed user-data/callback references; the backend world ID remains internal.

| Operation | Behavior |
| --- | --- |
| Constructor | Validate the idle source and capture its state. |
| `Capture()` | Replace the saved state without advancing time, retaining high-water storage. A failed capture copy invalidates the saved point. |
| `Restore()` | Reject changed world/object identity or callback/task configuration; reserve nested storage, then restore persistent solver state. |
| `Dispose()` | Idempotently release retained storage/references on the owner thread, including after source destruction. |

Bodies, contacts, joints, sleep/islands, sensor histories, spatial/pair data and
completed event buffers rewind together. This is not a public PhysicsSpace or
SceneTree checkpoint, a copy of user objects, a lifecycle log or a network payload.
See the component for executable tests, measured costs and remaining integration.
