# GPUPhysicsDevice

Last updated: 2026-10-27

**Declaration:** `internal sealed class GPUPhysicsDevice : IDisposable`

**Source:** [GPUPhysicsDevice.cs](../../src/Servers/Physics/GPUPhysicsDevice.cs)
**Component:** [GPU physics](../components/gpu-physics.md)

Retain the active GPU renderer's compute device or create an independent SDL GPU
device. No window or renderer is created. SDL video lifetime remains retained for
the device, because the current Vulkan loader uses it. The existing GPU stage host
and independent resident body store share this device/pipeline ownership code.
Pipelines load checked offline SPIR-V resources through ShaderCompiler.
Metal and Direct3D 12 select the function-preserving `.portable.spv` resources for
resident CCD, shape and motion queries; Vulkan retains their optimized resources.
Both profiles come from the same source and preserve native reflection and workgroups.

The device retains its creating thread identity. The scoped internal
`PipelineCreationForTesting` hook observes the shader name, owner and actual
creation thread before native allocation. Normal runtime leaves it unset.
The selected world prepares cold numerical pipelines on that owner before
borrowing; existing per-store handles and disposal retain their lifetime.
See [the executable ownership check](../components/physics-retirement-memory.md#owner-pipeline-preparation).

Initialization reuses DisplayServer's existing GTK/Wayland environment preparation.
An inherited X11-only GDK choice in a Wayland session is corrected before SDL video
starts; failure or a different selected video driver restores the prior choice.
This process-local policy does not change the desktop theme or shell environment.
The device name is diagnostic. The native driver identity selects the offline
compiler profile; capability checks still come from the native API.

Constructor failure releases any acquired device and SDL video reference. Ordinary
owners dispose pipelines/buffers before the device reference. The GPU suite checks
compute lifetime with both renderers and after failed physics work. Repeated
resident-store creation also exercises the compute-only initialization path.

The [window device isolation trial](../components/gpu-resident-reports.md#window-device-isolation-trial-2026-10-27)
rejected unconditional separate physics devices after reproducible native window
retirement failures. The current renderer-borrowing policy remains in place.

## Canvas point integration

See [canvas point programs](../components/canvas-points.md) for native topology, the shared sized-point device capability and cold program validation. Authored sizes are preserved after the one-pixel default initialization; optional capabilities retain explicit rejection and device fallback.
