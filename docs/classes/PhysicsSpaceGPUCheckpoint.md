# PhysicsSpace.GPUCheckpoint

Last updated: 2026-10-11

**Declaration:** `internal sealed class PhysicsSpace.GPUCheckpoint : PhysicsBackendCheckpoint` · **Source:** [PhysicsSpace.GPUCheckpoint.cs](../../src/Servers/Physics/PhysicsSpace.GPUCheckpoint.cs) · **Component:** [Selected checkpoint ownership](../components/physics-backends.md#selected-checkpoint-ownership)

## Description and ownership

The selected GPU world creates this same-world history object after preparing pending body/Area/joint/exception policy. It owns a GPUPhysicsBodyStore.Checkpoint and already published host observation data: reports/ranges, publication epochs, pending wake state and generation-qualified collider caches. Concrete cache copies retain actual store attachments; common PhysicsReplayEntry contains no GPU cache or native handles.

Construction captures the device kernel and observers. Failed observer construction disposes the kernel. Capture reuses high-water array/dictionary capacity. Restore first validates and restores the device kernel, then reports/epochs/cache values before common scene/server backing state is restored. No new body-state download or full physical CPU mirror is introduced. Kernel command flushing and copy waits remain part of checkpoint cost.

## Internal operation family

| Operation | Contract |
| --- | --- |
| `GPUCheckpoint(PhysicsSpace space, GPUPhysicsBodyStore store)` | Capture implementation history for the current immutable GPU owner. |
| `long DeviceCapacityBytes { get; }` | Retained native device history capacity. |
| `void Capture()` | Replace kernel and published observer copies without advancing time. |
| `void ValidateRestore()` | Reject disposed history or mismatched device configuration before mutation. |
| `void Restore()` | Restore actual device state and exact published caches, preserving current public identities. |
| `void Dispose()` | Release kernel and clear retained references in finally; repeated release does nothing. |

The common caller validates owner thread, completed interval, authoring and attachment membership, then borrows point/world lifetime during selected operations. Preflight rejection preserves motion; a begun restore error fails the source world. The point is retired even when implementation cleanup throws.

## Verification and limits

[Selected checkpoint tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedCheckpoint.cs) wrap real CPU/GPU kernels, inject recoverable prepare/capture/validation and release failures, reject recursive or destructive access, restore actual motion and check 64 warmed capture/restore cycles at zero owner/all-thread allocation. Shared public/common-world checkpoint tests cover contacts, Area events, force/target state, joints, one-way CCD, sleep, configuration and lifetime. Portable network state remains a separate wire contract. Public registration, native allocator and foreign-platform acceptance remain open.
