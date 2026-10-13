# PhysicsSpace.Checkpoint

Last updated: 2026-10-13

Portable common-entry writers now share logical shape-range checks with readers.
The capture measurement pass rejects stale references after slot removal before
replacing a complete packet; ordinary history refresh permits recapture. See
[topology verification](../components/physics-snapshots.md#capture-after-topology-edits).

Common CharacterBody replay retains prepared query state with its solver pose;
capture/validation no longer reject that mutable flag. The backend point and common
entry restore it together. Portable contact/history links additionally validate
both endpoints' logical shape ranges before restoring any world state; see
[the executing fixture](../components/physics-snapshots.md#portable-contact-and-motion-history).

**Declaration:** `internal sealed class PhysicsSpace.Checkpoint : IDisposable`

**Source:** [PhysicsSpace.Checkpoint.cs](../../src/Servers/Physics/PhysicsSpace.Checkpoint.cs)

**Component:** [Local physics-world checkpoints](../components/physics-space-checkpoints.md)

`PhysicsSpace.CreateCheckpoint()` returns an immediately captured owner-thread
rewind point. `Capture()` replaces it while reusing capacity. `Restore()` validates
same-world identities/configuration, restores kernel and attached observer state,
and silently resets physics poses/interpolation. `Dispose()` releases retained
storage; world disposal also releases every point. A failed capture invalidates
that point. A failed restore after mutation begins fails the world.

`PhysicsReplayEntry` owns typed scene/server backing-state copies and configuration
guards; `PhysicsReplayCopy` provides reusable collection copies. Per-family internal
replay records keep private fields in their owning classes. No user callbacks,
script fields or portable identifiers are cloned. See the component for event,
hierarchy, lifecycle and networking boundaries.

The game-facing owner is [PhysicsCheckpoint](PhysicsCheckpoint.md), created by
`PhysicsServer.SpaceCreateCheckpoint`; this internal point also retains the captured
world tick for that API.

## Native simulation and replay ownership

Private solver capture/validate/restore/dispose now uses one PhysicsBackendCheckpoint supplied by the immutable selected owner. CPU history includes native state, one-way episodes, solved motion, frame contacts and report-only pair/activity statistics; GPU history stays device-local. Shared scene/raw authoring and published observer data retain their existing validation and ordering. [Implementation ownership](../components/physics-backends.md#native-simulation-and-replay-ownership).

## Selected checkpoint ownership

PrepareCheckpoint and the selected history operation family borrow library lifetime. GPU reports/epochs/collider caches stay in its concrete checkpoint; common authoring uses neutral field values. Failed capture is replaceable, validation rejection preserves state, begun restore failure is terminal, and cleanup failure still retires the point. See [the executing contract](../components/physics-backends.md#selected-checkpoint-ownership).
