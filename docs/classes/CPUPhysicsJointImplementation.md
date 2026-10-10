# CPUPhysicsJointImplementation

Last updated: 2026-10-11

**Declaration:** `internal sealed partial class CPUPhysicsJointImplementation : PhysicsJointImplementation` · **Source:** [CPUPhysicsJointImplementation.cs](../../src/Servers/Physics/CPUPhysicsJointImplementation.cs) · **Component:** [Physics joints](../components/physics-joints.md#runtime-flow-and-invariants)

## Ownership and execution

Attach and ApplySolverPolicy consume immutable [PhysicsJointConfiguration](PhysicsJointConfiguration.md) values in engine units. They do not borrow the mutable resource runtime; effective spring rest length is sampled before solver compilation.

Owns native joint/endpoint IDs, exact compiled local frames and transient spring points/impulse. Creates revolute, wheel or filter constraints and applies the existing CPU units, compliance/bias/caps and wake/reacquire rules. Its prepare/validate/apply spring phase remains a CPU-specific endpoint used by the complete CPU solver interval. An empty second pin endpoint borrows the lazy anchor from CPUPhysicsWorldBackend.

The selected world creates a fresh implementation. Body/world transfer retires the old constraint and later reconnects using the stored engine frames. Native/resident handles remain internal. Library context guards precede public changes; no implementation retargets another world.

## Lifecycle and verification

The CPU world registers this attachment by shared joint identity. Its native spring phases resolve this owner even through a selected wrapper. Detach retires only a matching entry in finally, preserving a later replacement. World release clears retained references.

Creation failure attempts retirement before the retained wrapper is cleared; cleanup failure is aggregated with the creation error. Detach clears all owned handles/state in finally, including failed-world release. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks rejected internal creation, all three roles, cross-world suspension/reconnect, fresh implementations, exact retained frame/policy metadata, clear and world-anchor ownership. Common scene/server joint, checkpoint, snapshot and separate-process network checks retain their own numerical and acceptance limits.

## Limits and decisions

This internal operation family is not public backend registration. Server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103). Native allocator, foreign platform and real-window FPS remain separate gates.
