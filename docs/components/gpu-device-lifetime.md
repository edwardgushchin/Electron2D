# GPU device and window lifetime

Last updated: 2026-10-27

GPUPhysicsDevice borrows an active GPU renderer device or creates its own device
when the renderer is absent. A GPU space started before native rendering therefore
has a separate device. Its retirement while another device owns a Wayland window
is an open native acceptance failure on the measured NVIDIA profile. The checks
below isolate that failure; they do not change the current ownership policy.

## Direct native lifetime control

The [standalone C probe](../../tests/Electron2D.Tests/Native/GPUDeviceLifetime.c)
reproduces the failure using only SDL 3.4.18 device/window creation and retirement,
before any shader, simulation or frame submission. The source-bound 18-profile
matrix on NVIDIA 615.71.09 exercises both creation orders, both retirement orders,
an extra Vulkan-loader reference and video-reference controls. Twelve processes
exit normally; six Wayland profiles end with SIGSEGV. These failures remain open.

| Native video driver | Render device retires first | Compute device retires first |
| --- | --- | --- |
| Wayland | Normal exit | SIGSEGV |
| X11 | Normal exit | Normal exit |

An extra loader reference does not repair the failure, and mapped shared-library
paths remain unchanged across compute retirement. The system SDL 3.4.18 and
global library loading reproduce it too; default Vulkan layers and corrected
GDK/Wayland initialization retain the failure. Disassembly locates the failing
call inside swapchain destruction, before surface destruction. This agrees with
the [pinned SDL release path](https://github.com/libsdl-org/SDL/blob/release-3.4.18/src/gpu/vulkan/SDL_gpu_vulkan.c#L9193).

In two debugger interventions, skipping only compute's `vkDestroyInstance`, or
both its device and instance destruction, lets the same process finish normally.
These interventions intentionally leave native objects alive and are diagnostic
controls, not fixes or resource-lifetime acceptance. The evidence narrows the
Wayland dependency to instance retirement while another device owns a swapchain;
it does not qualify a replacement ownership policy. Raw terminal receipts,
library/source/binary hashes, maps and debugger logs are retained under
`bin/physics-device-lifetime/2026-10-27/`. See the [probe instructions](../../tools/physics/README.md#native-gpu-device-retirement).
Physics declarations remain 1358/1369 (99.20%); no runtime behavior is changed.

## Public space and window retirement

The [public-only fixture](../../tests/PhysicsBackendExtension.Consumer/DeviceWindowLifetimeChecks.cs)
also reproduces the failure on the current engine, without the reverted
independent-device patch. It explicitly creates a GPU space, advances a circle
body at 60 scene units/s for 1/60 second, and runs a separate CPU-world window
through Engine.Run with the GPU renderer. The pose must remain finite and advance
one unit within .01 scene units, allowing float/unit conversion. When the
renderer retires first, another tick must advance the same body to two units.
The selected physical backend must actually be GPU, with fallback disabled.

The frozen CLR cohort covers both video drivers, both startup/retirement orders
and both numerical-thread modes: fourteen profiles exit normally, while two
Wayland profiles fail with SIGSEGV after FreeRID returns. Both failures start the
GPU space before the renderer and free it while the native window remains alive;
owner and separate-thread solving give the same result. Starting the space after
the renderer or retiring the renderer first completes in the exercised profiles.
All eight X11 profiles complete. These are recorded boundaries, not a completed
resource-lifetime contract or a prescribed application workaround.

`--device-window-lifetime [--render-first] [--render-retire-first] [--threaded]`
selects the public fixture. Manual SpaceStep requires an explicitly created space;
a scene-owned World.Space is deliberately excluded by the existing guard. The
consumer never calls private native-window methods. Source/PDB fingerprints and
terminal receipts are in `bin/physics-device-lifetime/2026-10-27/`. NativeAOT,
physical speed and native-heap acceptance are not established by this matrix.
