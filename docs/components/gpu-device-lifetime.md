# GPU device and window lifetime

Last updated: 2026-10-27

GPUPhysicsDevice and GPUCanvasBackend acquire independent leases on one active
process-local SDL GPU device, regardless of startup order. Local RenderingDevice
instances retain separate logical resource namespaces on that same native device.
CPU-only physics never acquires it. The first lease negotiates the driver/features;
subsequent leases retain those capabilities until all resources retire. Native
driver-hint changes require retiring every active GPU consumer.

GPUDeviceFactory serializes acquisition and final destruction. Each lease refers
directly to the canonical owner, avoiding chains through older consumers. Native
resources retain their own lease. Canvas resources also retain their window;
compute leases never retain that canvas/window. Final release destroys the native
device synchronously and clears the owner, without an idle cache, leaked instance
or retired-device queue. SafeHandle finalization is the ordinary fallback for an
abandoned lease. Logical owner/thread guards and per-world resident data remain
unchanged. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and
[ADR 0014](../decisions/resources.md#adr-0014).

The source-bound CLR and strict NativeAOT public matrices each complete all 16 combinations of video
driver, startup order, retirement order and numerical-thread mode. This includes
the two formerly crashing Wayland profiles. The historical controls below explain
why independent native instances were rejected; they are not current production
ownership. Shared-device queue contention and broader platform qualification
remain separate acceptance requirements. Declaration coverage stays 1358/1369
(99.20%); this fix adds no public declaration.

## Direct native lifetime control

The [standalone C probe](../../tests/Electron2D.Tests/Native/GPUDeviceLifetime.c)
reproduces the failure using only SDL 3.4.18 device/window creation and retirement,
before any shader, simulation or frame submission. The source-bound 18-profile
matrix on NVIDIA 615.71.09 exercises both creation orders, both retirement orders,
an extra Vulkan-loader reference and video-reference controls. Twelve processes
exit normally; six Wayland profiles end with SIGSEGV. These direct two-device failures remain reproducible outside the shared production policy.

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
These controls describe the baseline before shared ownership.

## Public space and window retirement

The [public-only fixture](../../tests/PhysicsBackendExtension.Consumer/DeviceWindowLifetimeChecks.cs)
reproduced the failure on the previous engine, without the reverted
independent-device patch. It explicitly creates a GPU space, advances a circle
body at 60 scene units/s for 1/60 second, and runs a separate CPU-world window
through Engine.Run with the GPU renderer. The pose must remain finite and advance
one unit within .01 scene units, allowing float/unit conversion. When the
renderer retires first, another tick must advance the same body to two units.
The selected physical backend must actually be GPU, with fallback disabled.

The previous frozen CLR cohort covered both video drivers, both startup/retirement orders
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

## Shared ownership verification

The current cohort is under `bin/physics-device-lifetime/2026-10-27/`:
`shared-public-matrix.json` and `shared-native-sampled-matrix.json` record 16/16
normal exits each, with NVIDIA mappings and no software Vulkan. The native
executable uses full trimming and warnings-as-errors. A first NativeAOT sampling
attempt completed ten profiles normally but missed the short-lived NVIDIA mapping
in one X11 process. That profile remained unqualified; the complete replacement
matrix samples maps every 50 ms without changing the fixture or executable.

The public `--churn` profile retains a local RenderingDevice buffer across three
Engine.Run lifetimes and checks its data after each. The common pipeline fixture
still covers cold preparation failures, owner/separate numerical lanes and 128
warmed full GPU intervals with exactly zero owner/all-thread managed bytes.
The existing local RenderingDevice and rendered-canvas consumers also pass,
including the renderer's direct texture sharing and independent RID guards.
These checks do not qualify mass-scene speed, external/native heap allocations
or foreign platform behavior.

The internal GPUDeviceLifetimeTests fixture renders two actual frames in each of
three Engine.Run lifetimes. It checks the real SDL window identity is absent
immediately after each run, while local buffer data remains usable. It also
checks foreign logical-device RIDs reject, an abandoned lease is collected and a
different logical owner retains the same physical device. Both Wayland and X11
profiles pass. The independent debugger control of public `--churn` exits normally
and observes exactly one SDL_DestroyGPUDevice and three SDL_DestroyWindow calls;
no destroy is skipped. Receipts are `shared-internal-ready-*/check.json` and
`shared-retirement-trace-fixed/runner.log`. These observations establish the
exercised release path, not a general native-heap leak proof.
