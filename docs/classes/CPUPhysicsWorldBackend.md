# CPUPhysicsWorldBackend

Last updated: 2026-10-10

**Declaration:** `internal sealed class CPUPhysicsWorldBackend : PhysicsWorldBackend` · **Source:** [CPUPhysicsWorldBackend.cs](../../src/Servers/Physics/CPUPhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

Owns one native CPU world, its retained PhysicsTaskScheduler and any diagnostic GPUPhysicsWorld stages. Creation applies the existing world definition, sleep/contact/iteration settings and pre-solve callback. Partial construction releases already-created native resources; the implementation does not initialize a renderer.

WorldID exposes a borrowed internal native identity; Tasks and StageGPU expose owned internal helpers. EnsureAccess rejects disposed or solver-owned state. Step selects PhysicsSpace.StepCPU; every CPU discrete and CCD interval reaches StepNative. EnableGPUIntegration and EnableGPUSolver retain the existing CPU-hosted diagnostic paths, which keep CPU identity. Dispose joins workers, removes native stage callbacks, releases diagnostic GPU resources and destroys the native world; cleanup errors are aggregated after attempting each owned resource. Repeated disposal is harmless.

## Ownership and verification

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

Native IDs and worker callbacks stay internal. Common body/shape/joint adapters still contain CPU/GPU branches; their complete extension boundary remains open. No new public backend capability is exported. Native allocator, foreign-platform execution, GPU speed advantage and real-window FPS remain separate gates. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).
