# Resident GPU body state

Last updated: 2026-10-08

## Implemented boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) is the first independent
device-state component under [ADR 0054](../decisions/physics.md#adr-0054). It creates
no CPU solver world. Body slots, generation changes, sparse edits, force/mass/gravity/
damping integration and explicit result gathering execute through one offline GPU
pipeline. Authored values and read results use scene units. The original
[GPUPhysicsWorld stage host](../classes/GPUPhysicsWorld.md) remains separate.

This component is not a complete physics backend. It has no shapes or contacts,
constraints, automatic sleep, CCD, scene/server selection or network snapshots.
Its integration-only timings cannot be compared with full CPU physics or reported
as window FPS. These missing consumers must be connected to resident state before
the independent GPU objective is satisfied.

## Storage, transfers and waits

| Storage/transfer | Purpose and current cost |
| --- | --- |
| Device bodies | 80 bytes per retained slot; authoritative pose, velocity, force, mass/damping and generation/role data. |
| CPU slot metadata | 16-byte payload per slot for generation, free-list and pending-command routing; no live poses or velocities. |
| Pending edit staging | 112 bytes per reserved command. At most one coalesced command per slot; entries are cleared after successful publication. Allocation follows capacity growth, not every frame. |
| Growth | Copy prior body slots GPU-to-GPU; no body download/upload reconstruction. Record copied bytes and wait for resource replacement. |
| Ordinary unchanged tick | 4-byte status reset upload, 32-byte compute uniform and 4-byte error-result download. No body-state traffic. |
| Explicit selected read | 16-byte generation/store-qualified request and 32-byte pose/velocity result per requested body, plus status and dispatch uniforms. |
| Per-tick fence wait | Required by this synchronous stage's finite-result/error publication contract. Its measured time is recorded separately from total submission/map/dispatch work. |

At 65,536 slots, body payload is 5 MiB and retained CPU metadata/command payload is
8 MiB; consumed command storage contains no live-state mirror. GPU command/request/
result scratch payload totals 10 MiB plus the status word. Upload/download transfer
capacity totals 10 MiB plus eight bytes. These are payload capacities, excluding
object/driver overhead. Further asynchronous publication requires an explicit
error/freshness contract, rather than silently removing the wait.

The SDL compute API supports device work without a window; the actual buffer,
command and fence pattern follows [SDL's GPU workflow](https://wiki.libsdl.org/SDL3/CategoryGPU).
The current Vulkan loader still needs SDL video initialization. Shared
GPUPhysicsDevice ownership retains that reference and applies the same platform
environment policy as DisplayServer.

## Verification and measurement method

Run `ELECTRON2D_TEST_GPU_RESIDENT_BODY=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after the Release test build with source native dependencies. The full GPU suite
also runs these tests. The kernel is compiled with the repository's pinned glslang
and validated with its spirv-val against Vulkan 1.0.

Functional checks cover semi-implicit constant force/gravity and torque at 120 Hz,
static/kinematic/rotation-locked roles, positive/negative damping, sparse edits,
setter/impulse order, actual evolved state surviving growth, generation reuse,
foreign-store identity with matching numeric slots/generations, owner guards and
failed-state rejection after nonfinite device integration.

The measured workload contains 65,536 moving rigid body records, no collisions,
zero gravity and velocity (0.25, 0.5) scene units/s. Warmup is 384 ticks; 256 ticks
at 1/120 s are sampled. Managed allocation is measured on the owner thread.
All 65,536 positions are then checked through a diagnostic full read outside the
timing/allocation window; its cost is reported separately. The position tolerance
is 0.02 scene units after 640 float-precision additions near coordinates 0–255.
Smaller force/torque checks use 0.002 scene units and 0.001 rad after 120 ticks.
This is one platform's body-stage evidence, not full-backend, networking, native
allocation or cross-platform acceptance.

Measured on Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti, 2026-10-08:

| Run | Tick p50 | Tick p95 | Mean fence wait | Diagnostic full read |
| --- | ---: | ---: | ---: | ---: |
| Dedicated resident test | 0.0489 ms | 0.0606 ms | 0.0419 ms | 1.7368 ms |
| Resident test inside full GPU suite | 0.0448 ms | 0.0688 ms | 0.0437 ms | 0.9892 ms |

Both runs report zero warmed managed bytes per tick and the unchanged-tick payload
listed above. The full read requests 1 MiB of handle data and downloads 2 MiB of
state, plus status/uniform payload. Logs are
`/tmp/electron2d-resident-bodies-final.log` and
`/tmp/electron2d-resident-bodies-gpu-suite.log`. These two runs use the same source
and shader; no CPU physics speedup or window-FPS conclusion follows from them.
The SPIR-V SHA-256 is
`266ebba6cffa52ea3e70118813982b2985716bfa23f455c8d8d7c73585e3972c`.

## Native lifecycle observation

The first compute-only run passed motion/edits and failure checks, then hung during
repeated SDL video initialization with GdkDisplayManager registration errors. The
process inherited GDK_BACKEND=x11 in a Wayland session. GPU initialization had
bypassed the environment preparation already used by DisplayServer. Both paths
now call that shared helper; the same resident run and full GPU suite then finish.
Local logs retain the failed run and follow-ups under
`/tmp/electron2d-resident-bodies-{glib-failure,final,gpu-suite}.log`.
Nonfatal gtk_disable_setlocale warnings can still appear on repeated initialization.
The earlier distinct GLib impossible-allocation observation in GPU status remains
open; no stack evidence establishes that it had the same cause.
