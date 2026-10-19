# Native physics window allocation

Last updated: 2026-10-19

## Cause and retained behavior

The component-edit native companion at `0ff17cc9` observed 9600 owner-managed
bytes over 240 fixed ticks, on both CPU and software-GPU physics. Temporary
phase counters narrowed all 40 bytes per tick to SceneTree passive picking:
event pumping, process callbacks, rendering, timing, node physics callbacks and
the selected solver measured zero within their respective brackets.

Viewport.GetMousePosition checks HasFeature(Mouse). HasFeature previously
marshaled SDL's current driver name on every call, including a seven-character
Wayland string each tick. DisplayServer now retains the already read driver
identity for each main/child display context. Device presence, touch emulation,
rendering attachment and discovered icon capability still use live state.
Service, owner-thread and native ownership guards remain unchanged. No public
API, physics work, callback or query is removed. Temporary phase counters are
absent from the final runtime.

## Regression and measured boundary

PhysicsVelocityEditNativeTests uses Engine.Run, a real 640×520 GPU-rendered
Wayland window, 256 static and 256 dynamic circles, and 256 pose/linear/angular
writes on every physics tick. It explicitly checks enabled physics picking,
queues initial native mouse motion, then checks that passive picking is warm.
After the first rendered frame it performs two blocking full collections, draining
finalizers after each, then retains 96 physics warmups. It measures an uninterrupted four-second owner-thread
interval between FramePostDraw callbacks. This includes event pumping, complete
scene physics, process callbacks, rendering and frame timing. Image capture and
report formatting follow the bracket. The assertion requires exactly zero
managed bytes, without subtraction or a retry allowance.

The exact final test assembly is used with the baseline runtime and the corrected
runtime. Evidence, source/PDB binding, process maps and commands are retained in
`bin/physics-window-allocations/2026-10-19/`. The final samples below identify
local software execution only; native allocator totals are unmeasured.

The first strict unprepared candidate measured 80 owner bytes over 239 CPU ticks
and failed. A subsequent instrumented investigation measured zero in every phase
on CPU and software GPU; that does not identify the 80-byte source. The final
fixture now follows the repository's strict-allocation setup contract, completing
collections and finalizers before warmup. No runtime GC policy changes. The failed
sample remains in first-unprepared-validation.json and root-window-managed/.
The 80-byte cause and ordinary unprepared-host repeatability remain open; later
prepared samples must not be treated as their diagnosis.

## Final prepared sample

The final consumer SHA-256 is
64cb611e943fc8c8f7261e38c2bd9133f467bbb26a69b76ab2b43805d2eb5af4.
The baseline runtime fails with 9640 bytes across 241 CPU ticks, exactly 40 bytes
per tick. The corrected runtime passes the same consumer's strict assertion.

| Physics backend | Measured s | Frames / physics ticks | Actual FPS / ticks per s | Frame p50 / p95 / p99 ms | Owner managed B |
| --- | ---: | --- | --- | --- | ---: |
| CPU | 4.008 | 215 / 243 | 53.65 / 60.63 | 16.3492 / 38.3672 / 47.5937 | 0 |
| Software GPU | 4.248 | 33 / 144 | 7.77 / 33.90 | 49.6863 / 406.3207 / 436.7144 | 0 |

The GPU interval transfers 6,532,992 upload / 123,840 readback bytes, with 3168
submissions and 3111.901 ms accumulated wait. The CPU interval has no GPU physics
traffic. Formatting/source-audit work ran concurrently on this desktop; the GPU
host did not sustain the requested physics rate. These are allocation diagnostics,
not an isolated throughput comparison or performance acceptance.

Five final successful processes cover this paired window, native picking across
GPU/compatibility renderers and CPU/GPU/registered physics, CPU picking without a
GPU/device, software-GPU picking, and CPU/GPU scene/server component edits.
One separate expected-failure process is the baseline RED. Three assembly/PDB
proofs match 3428 authored source files. The public surface is unchanged.
The runtime Release build, format checks, compiled API coverage check and wiki
checks are recorded separately in required-checks.json.

To reproduce with the retained final payload, run
`python3 -B bin/physics-window-allocations/2026-10-19/run.py root qualified-root gpu wayland reproduction-window ELECTRON2D_TEST_PHYSICS_VELOCITY_EDITS_NATIVE=1`.
The runner selects the private lavapipe ICD and rejects hardware drivers from
process-group maps. Keep driver/source receipt prerequisites from that artifact.

## Limits

Private lavapipe performs all exercised GPU physics and rendering. Process-group
maps reject hardware drivers. The RTX 3090 Ti is not exercised while its power
cable remains unverified. Actual compositor VSync stays enabled despite the
request to disable it; MaxFPS is zero and the requested physics rate is 60 Hz.
These small-scene FPS figures do not establish a target-hardware GPU advantage,
repeatable performance, foreign-platform or NativeAOT acceptance. The broader
physics and multiprocess-network goal remains open. Physics declaration counts
are unchanged: I1266/P72/B17/U14/X92, 1266/1369 applicable declarations = 92.48%,
with 103 applicable declarations still open. This ratio is API coverage, not
whole-product readiness.
