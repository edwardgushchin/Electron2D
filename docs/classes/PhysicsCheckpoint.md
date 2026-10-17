# PhysicsCheckpoint

Last updated: 2026-10-18

[Publication recovery](../components/physics-registered-adapter.md#failed-motion-publication) distinguishes local rewind from portable preparation. Successful local restore discards superseded motion/pose/mode publication without invoking the failed request. Portable apply first prepares pre-existing local authoring; a preparation failure leaves incoming state unapplied and the tick unchanged, and a later successful apply supplies authoritative state.

Capturing a moved character now retains its prepared query pose, query flag and
separate solver pose. These are mutable replay state rather than fixed authoring.
[The portable contact fixture](../components/physics-snapshots.md#portable-contact-and-motion-history)
checks restoration and actual query geometry before the next interval.

**Declaration:** `public sealed class PhysicsCheckpoint : IDisposable`

**Source:** [PhysicsCheckpoint.cs](../../src/Servers/Physics/PhysicsCheckpoint.cs)

**Component:** [Local physics-world checkpoints](../components/physics-space-checkpoints.md)

Create a caller-owned point with `PhysicsServer.SpaceCreateCheckpoint(space)`.
It is already captured; there is no public detached constructor. The point borrows
one source world, which also releases its points on disposal. Calls and first
release require the world's owner thread.

[Released history references](../components/physics-space-checkpoints.md#released-history-references)
leave no selected solver reference in a disposed point, including cleanup failure.
A surviving public wrapper does not keep a closed world's graph alive through
its private checkpoint; this is verified with weak references on CPU and GPU.

Recapture releases removed shape references from inactive array tails and replaces
shorter contact histories without retaining their retired identities. Current
shape/configuration validation and prepared array capacity remain available.

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

Capture and restore also reject while a caller-created direct-space extension hook borrows this world. Query nesting may inspect live state, but cannot replace its completed checkpoint basis during the invocation. [The external consumer](../../tests/PhysicsSpaceExtension.Consumer/Program.cs) verifies this guard for scene-owned and caller-owned CPU/GPU worlds.

## Selected checkpoint ownership

PrepareCheckpoint and the selected history operation family borrow library lifetime. GPU reports/epochs/collider caches stay in its concrete checkpoint; common authoring uses neutral field values. Failed capture is replaceable, validation rejection preserves state, begun restore failure is terminal, and cleanup failure still retires the point. See [the executing contract](../components/physics-backends.md#selected-checkpoint-ownership).
