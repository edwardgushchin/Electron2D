# GPUPhysicsJointImplementation

Last updated: 2026-10-10

**Declaration:** `internal sealed class GPUPhysicsJointImplementation : PhysicsJointImplementation` · **Source:** [GPUPhysicsJointImplementation.cs](../../src/Servers/Physics/GPUPhysicsJointImplementation.cs) · **Component:** [Physics joints](../components/physics-joints.md#runtime-flow-and-invariants)

## Ownership and execution

Owns one resident constraint handle and definition. Applies pin/groove/spring, collision vetoes, motor/limits, bias/softness/caps and portable frames through the independent store. Spring impulses execute on device; the implementation exposes no fake CPU spring phase. A world pin uses the virtual empty endpoint without creating a CPU body.

The selected world creates a fresh implementation. Body/world transfer retires the old constraint and later reconnects using the stored engine frames. Native/resident handles remain internal. Library context guards precede public changes; no implementation retargets another world.

## Lifecycle and verification

Creation failure attempts retirement before the retained wrapper is cleared; cleanup failure is aggregated with the creation error. Detach clears all owned handles/state in finally, including failed-world release. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks rejected internal creation, all three roles, cross-world suspension/reconnect, fresh implementations, exact retained frame/policy metadata, clear and world-anchor ownership. Common scene/server joint, checkpoint, snapshot and separate-process network checks retain their own numerical and acceptance limits.

## Limits and decisions

This internal operation family is not public backend registration. Server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103). Native allocator, foreign platform and real-window FPS remain separate gates.
