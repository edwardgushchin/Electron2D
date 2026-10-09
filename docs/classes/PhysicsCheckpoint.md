# PhysicsCheckpoint

Last updated: 2026-10-10

**Declaration:** `public sealed class PhysicsCheckpoint : IDisposable`

**Source:** [PhysicsCheckpoint.cs](../../src/Servers/Physics/PhysicsCheckpoint.cs)

**Component:** [Local physics-world checkpoints](../components/physics-space-checkpoints.md)

Create a caller-owned point with `PhysicsServer.SpaceCreateCheckpoint(space)`.
It is already captured; there is no public detached constructor. The point borrows
one source world, which also releases its points on disposal. Calls and first
release require the world's owner thread.

| Member | Contract |
| --- | --- |
| `IsDisposed` | True after explicit release or source-world disposal. |
| `Tick` | The last successfully captured world-local simulation tick. |
| `Capture()` | Replace the state and tick without advancing time; reuse prepared storage. |
| `Restore()` | Validate fixed configuration/identity and silently restore simulation, observer history and tick. |
| `Dispose()` | Idempotently release storage without releasing the world or game objects. |

This is local replay storage, not a portable network packet. Restoring does not
rewind SceneTree callbacks, timers, game objects' custom fields or another world.
A fixed duration remains the caller's responsibility when interpreting ticks as
network time. Replayed future callbacks happen again; effect confirmation is a
separate networking responsibility. See the component for complete capture,
restoration, error, lifetime and allocation boundaries.
