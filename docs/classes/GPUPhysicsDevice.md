# GPUPhysicsDevice

Last updated: 2026-10-08

**Declaration:** `internal sealed class GPUPhysicsDevice : IDisposable`

**Source:** [GPUPhysicsDevice.cs](../../src/Servers/Physics/GPUPhysicsDevice.cs)
**Component:** [GPU physics](../components/gpu-physics.md)

Retain the active GPU renderer's compute device or create an independent SDL GPU
device. No window or renderer is created. SDL video lifetime remains retained for
the device, because the current Vulkan loader uses it. The existing GPU stage host
and independent resident body store share this device/pipeline ownership code.
Pipelines load checked offline SPIR-V resources through ShaderCompiler.

Initialization reuses DisplayServer's existing GTK/Wayland environment preparation.
An inherited X11-only GDK choice in a Wayland session is corrected before SDL video
starts; failure or a different selected video driver restores the prior choice.
This process-local policy does not change the desktop theme or shell environment.
Device/driver names are diagnostic strings and are never parsed for capabilities.

Constructor failure releases any acquired device and SDL video reference. Ordinary
owners dispose pipelines/buffers before the device reference. The GPU suite checks
compute lifetime with both renderers and after failed physics work. Repeated
resident-store creation also exercises the compute-only initialization path.
