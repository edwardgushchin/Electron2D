# PhysicsSpace.Checkpoint

Last updated: 2026-10-10

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
