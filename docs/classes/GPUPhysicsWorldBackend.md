# GPUPhysicsWorldBackend

Last updated: 2026-10-10

**Declaration:** `internal sealed class GPUPhysicsWorldBackend : PhysicsWorldBackend` · **Source:** [GPUPhysicsWorldBackend.cs](../../src/Servers/Physics/GPUPhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

Owns one independent resident GPUPhysicsBodyStore, with no native CPU world or CPU workers. Creation applies existing sleep/contact/iteration/default-bias settings. The store keeps physical state and executes the resident pipeline. A failed constructor releases the created store before startup fallback can be considered.

GPUStore is the owned resident implementation. Kind and Requested remain GPU for its lifetime. EnsureAccess rejects disposal or terminal store failure. Step selects the complete PhysicsSpace.StepGPU publication/callback/event path. Dispose releases the resident store once; CPU-only native facets and diagnostic stage controls reject.

## Ownership and verification

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

Startup fallback is handled only by the internal creation factory. A started GPU interval never selects or executes a CPU replacement. Shared registrations, authored metadata, readback consumers and event delivery remain in PhysicsSpace; public registered extensions are still absent. Native allocator, foreign-platform execution, GPU speed advantage and real-window FPS remain separate gates. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).
