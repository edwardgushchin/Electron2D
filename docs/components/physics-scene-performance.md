# Complete scene physics measurements

Last updated: 2026-10-15

The [scoped allocator profile](#scoped-native-allocation-profiling-2026-10-14)
adds all-thread libc request accounting to the complete monitored scene, with
separate ordinary timing and software-driver limits. Older trials below keep
their own revisions and workloads.

## Scoped native allocation profiling, 2026-10-14

Schema 5 adds an optional calibrated Linux/glibc interposer and an explicit
serial/separate numerical-thread selector to the existing full-scene benchmark.
The runtime API and coverage classifications are unchanged. The focused selector
runs before unrelated native-package inventory checks; the full test runner retains
those checks. This establishes the exercised physics dependencies, not complete
native-package acceptance. [Probe build and semantics](../../tools/physics/README.md)
include its native calibration and precise counter boundaries.

Eight fresh processes ran sequentially on the candidate based on main
`3ac5b7129eb94fedf00fc4160a78ff6396c91302`, with 128 bodies, 512 warm ticks and
128 measured ticks each. All retain the full monitored scene described below:
144 Hz, four substeps, sixteen iterations, four walls, eight-point contact cap,
entry/exit handlers, interpolation, materials and disabled sleeping. No stage
control is enabled. This is a bounded diagnostic population, not the target mass
workload. All frames require strict 0/0 owner/all-thread managed bytes.

CPU processes have no display and an unavailable GPU driver/ICD. GPU processes
use offscreen Vulkan with an explicitly pinned lavapipe ICD and four driver workers.
Process maps confirmed lavapipe only on GPU profiles, no NVIDIA driver on any
profile, probe presence only on instrumented profiles, and exactly zero/one
retained Electron2D solver thread according to the setting. The physical device
is not exercised: its power-cable compatibility remains unverified after Xid 79.

### Complete frame costs

Times are milliseconds; all eight physical/publication/contact gates passed.
Ordinary timing profiles have neither LD_PRELOAD nor the probe flag. Instrumented
profiles include the shared all-thread counter overhead and remain separate.
These desktop samples are not isolated statistical repetitions.

| Profile | Mean | p50 / p95 / p99 / max | Managed owner / all threads |
| --- | ---: | --- | --- |
| cpu-timing | 0.8113 | 0.8121 / 0.8207 / 0.8376 / 0.8439 | 0 / 0 |
| cpu-thread-timing | 0.8838 | 0.8597 / 1.0079 / 1.0515 / 1.2998 | 0 / 0 |
| gpu-timing | 20.2987 | 20.0453 / 22.2432 / 23.6950 / 24.1431 | 0 / 0 |
| gpu-thread-timing | 20.6490 | 20.6743 / 22.9345 / 23.7647 / 23.9732 | 0 / 0 |
| cpu-alloc | 0.8092 | 0.8081 / 0.8166 / 0.8304 / 0.8411 | 0 / 0 |
| cpu-thread-alloc | 0.9068 | 0.8956 / 1.0008 / 1.0596 / 1.0676 | 0 / 0 |
| gpu-alloc | 20.4077 | 20.3304 / 22.7175 / 23.6799 / 23.9740 | 0 / 0 |
| gpu-thread-alloc | 21.2294 | 21.1172 / 23.4137 / 27.4503 / 27.6835 | 0 / 0 |

| GPU profile | Upload / readback bytes per tick | Uniform bytes per tick | Submissions per tick | Summed waits p50 / p95 / p99 / max, ms |
| --- | --- | ---: | ---: | --- |
| gpu-timing | 2328 / 93824.625 | 44330.00 | 19.226562 | 19.0624 / 21.1426 / 22.5986 / 22.8798 |
| gpu-thread-timing | 2328 / 93832.500 | 45148.00 | 19.281250 | 19.6339 / 21.7685 / 22.7132 / 22.7714 |
| gpu-alloc | 2328 / 93840.375 | 45560.75 | 19.335938 | 19.3401 / 21.5417 / 22.4882 / 22.7271 |
| gpu-thread-alloc | 2328 / 93829.125 | 45076.25 | 19.257812 | 19.8836 / 22.1022 / 26.0013 / 26.3142 |

The readback serves live scene pose/motion fields and full monitored contact
publication. Waits are already included in complete frame costs. These are complete
CPU versus complete GPU-backend paths; lavapipe itself executes on the CPU. Their
ratio establishes no physical-GPU advantage. No real window is opened and no FPS
is reported. Maximum escape/penetration across all profiles are .006592/.002849
scene units, below the unchanged .601 limit; maximum speed is 66.166 below 1000,
and final energy remains below one percent of initial energy, within the 102-percent
ceiling. Contact retention is 450–465 points with actual entry/exit delivery.
Backend trajectories and hashes are diagnostics, not equality gates.

### Native request traffic

Totals below cover the 128 complete measured PhysicsFrame windows across all
process threads. Allocations includes successful realloc requests; RequestedBytes
includes their full requested size, even for in-place resize. Frees counts explicit
non-null free and glibc realloc-to-zero releases. Reallocations includes failed and
zero-size calls; all measured Failures are zero.

| Instrumented profile | Allocations | Requested bytes | Frees | Reallocations | Failures |
| --- | ---: | ---: | ---: | ---: | ---: |
| cpu-alloc | 0 | 0 | 0 | 0 | 0 |
| cpu-thread-alloc | 0 | 0 | 0 | 0 | 0 |
| gpu-alloc | 1190334 | 619391040 | 1178663 | 16621 | 0 |
| gpu-thread-alloc | 1173586 | 612519776 | 1161992 | 16524 | 0 |

CPU recorded zero tracked libc traffic in this warmed fixture, including the
separate numerical thread. Software Vulkan recorded roughly 9,200–9,300 successful
requests and 4.8 MB of requested allocator traffic per frame. The global process
window includes CLR, SDL and driver work and does not attribute these requests to
engine buffers. This is neither retained/peak memory nor a leak measurement;
direct mappings, private allocator paths and device memory are outside the probe.
Setup, teardown, window pumping and rendering are also outside these windows.
A call completing after the bracket is outside its count. General native-memory
and target-driver acceptance remain open.

The native check verifies exact 4 allocations/131 requested bytes/3 frees/1 resize,
worker-thread capture, nested-window refusal, inactive-end refusal, failed resize
with retained original pointer and zero-size release. The managed preflight checks
actual preload attachment, 40-byte counter ABI and the exact smoke calibration.
A fresh process with the library discoverable but no LD_PRELOAD fails explicitly
before scene setup, preventing false zero reports.

### Frozen evidence and remaining gates

The isolated runtime keeps its platform assets; unrelated stale root native
libraries are excluded from the copy, without changing primary build outputs.
The exercised mesh dependency is retained in its native asset directory.
All loaded files and probe bytes remain unchanged across the eight processes.
The sorted source manifest covers tracked/untracked nonignored src, tests, tools,
examples and the runtime project; its fingerprint hashes path, NUL, contents, NUL.

| Evidence | SHA-256 |
| --- | --- |
| 4170 source inputs | `a056c6e99c15d58e1d7333f17d3a273f84db14934302fa5d5ac14b1cbfa03031` |
| Runtime DLL | `478663e9834c5c94bb28fdabada4ba647f6e6df0d4e8d22d1bc450b02d933e28` |
| Test DLL | `c991f9e63ffed1fe7ccc10ed0e21b95881880a487782c1efdbef2bd4929b8946` |
| Native probe | `9d981e6a9992fe5b131dd939462baa660c8fbb74a30b89a3e71f1c50e859eaa3` |

Ignored evidence is under `bin/physics-native-profile/2026-10-14/final2`: eight
reports/logs, process proofs, source-before/after, source/runtime/probe manifests,
runner and missing-preload rejection. The probe uses gcc 16.2.1 with strict C11,
O2, no builtins, all warnings as errors. Reproduction uses the documented probe
build, fresh process, explicit backend/thread and 128/512/128 count/warm/sample
settings; select a verified driver deliberately when exercising GPU.

This closes scoped allocator instrumentation and this small software-driver
measurement, preserving broader native-memory, target mass-scene advantage,
current hardware/native-window FPS, complete API, network and platform gates.

## Workload and provenance

[PhysicsPipelinePerformance](../../tests/Electron2D.Tests/PhysicsPipelinePerformance.cs) now measures the same complete public scene path on explicit CPU and GPU World instances. Every dynamic object is a RigidBody with a CollisionShape, ContactMonitor, an eight-point contact cap, real entry/exit handlers and SceneTree physics interpolation. PhysicsFrame advances exactly one 1/144-second tick; both implementations retain four substeps and sixteen iterations. Sleeping is disabled. The trials in this section use the default headless mode, so these are physics-frame costs rather than FPS; the native-window mode below is separate.

Both paired trials use deterministic circles of radius 6 and mass .2, friction .25, bounce .35, gravity 980, body linear/angular damping .05/.1 and zero world damping. Initial positions have fifty columns, horizontal/vertical spacing 20/12.5 and fixed initial horizontal velocities. Four static walls enclose the complete population; the floor/side heights scale with the authored rows rather than omitting bodies. Each implementation warms for 1600 ticks. The 1536-body trial measures 1024 ticks and the 4096-body trial measures 256. CPU then GPU run sequentially in each process. Profiling is enabled for both; it is included in measured costs.

The candidate is based on main `096935f790cab3bde4ced2b2ebb27290de465f5d` with this slice's three runtime files and scene/lifetime tests. It was freshly built with source-native dependencies and measured without changing source or the loaded assemblies between the paired runs. The source fingerprint hashes sorted C# paths under src and tests/Electron2D.Tests, excluding bin/obj, as path, NUL, contents, NUL; it includes the new lifetime test. Assembly fingerprints also cover the embedded shader resources.

| Evidence | SHA-256 |
| --- | --- |
| C# source | `8862648139363c92233f4703b736d24a21b07fea01f08d723fb69c6f46457b30` |
| Loaded runtime DLL | `42638402d03b1196aa66260378069178f6e9e90bcff363c82d09c2a24e1d36b3` |
| Test DLL | `367438cde04881ae4e0a53834b40301d47dcde4d9dba506ec87ebc991e82621a` |

The live host inventory is Linux x64, AMD Ryzen 7 5700X, .NET SDK 10.0.101, NVIDIA GeForce RTX 3090 Ti and driver 615.71.09. These are single paired trials on a shared desktop, with normal scheduling/driver variability, not isolated statistical repetitions.

## Complete frame timing and managed memory

Times are milliseconds. The bracket includes the complete PhysicsFrame, scene synchronization, contact monitoring, event delivery and interpolation history; final diagnostic reads, physical validation and JSON generation are outside it.

| Bodies | Backend | Samples | Mean | p50 / p95 / p99 / max | Owner / all-thread managed bytes |
| --- | --- | ---: | ---: | --- | --- |
| 1536 | CPU | 1024 | 7.0691 | 6.9258 / 8.2387 / 9.2706 / 11.1364 | 0 / 0 |
| 1536 | GPU | 1024 | 11.5485 | 11.3031 / 13.4977 / 15.7336 / 19.5270 | 0 / 0 |
| 4096 | CPU | 256 | 20.4895 | 20.3501 / 23.1384 / 24.4376 / 25.4044 | 0 / 0 |
| 4096 | GPU | 256 | 20.2668 | 20.1052 / 22.7320 / 23.4901 / 23.8632 | 0 / 0 |

At 1536 bodies GPU p50 is 1.632 times the CPU cost. At 4096 bodies the medians differ by about 1.2 percent, less than the within-trial tail variation. This does not establish a repeatable GPU advantage for ordinary monitored scenes. The [older 65,536 raw-body and window results](physics-current-performance.md) retain their own revision and different workload; their larger advantage must not be generalized to this scene path or declared final acceptance.

| Bodies | Backend | Solver mean | Result synchronization mean | Scene/contact publication mean | Frame work outside recorded SpaceStep phases mean |
| --- | --- | ---: | ---: | ---: | ---: |
| 1536 | CPU | 4.9542 | 0.0004 | 1.1431 | 0.5287 |
| 1536 | GPU | 8.3819 | 0.5555 | 1.6925 | 0.6355 |
| 4096 | CPU | 12.0823 | 0.0011 | 4.4814 | 2.3060 |
| 4096 | GPU | 11.4694 | 1.1188 | 4.9752 | 2.0364 |

The last column is the complete-frame mean minus the sum of the eight instrumented space phases. It includes scene-tree work and measurement overhead and is not an isolated interpolation measurement. All eight means are retained in each raw JSON. GPU waits below are already included in those timings.

## GPU transfers and waits

| Bodies | Upload / readback bytes per tick | Uniform bytes per tick | Submissions per tick | Summed wait p50 / p95 / p99 / max, ms |
| --- | --- | ---: | ---: | --- |
| 1536 | 24856 / 1112864 | 60958.4 | 20 | 4.4877 / 5.4027 / 5.8464 / 6.1989 |
| 4096 | 65816 / 2966304 | 62684.0 | 20 | 7.5552 / 8.6512 / 9.3054 / 9.3942 |

The scene and contact observers require current motion/fields and completed contacts for every monitored body. Their bulk publications are measured above; these figures include capacity-sized contact destinations and current changed-state publication. They do not represent an unobserved headless raw-body world, renderer traffic, or network payload. Host snapshot storage serves these public consumers; reducing that traffic requires preserving the full observed state and point cap and measuring any extra selection/copy/fence cost. The reports expose a concrete cost to address rather than an assumption that batching alone establishes performance acceptance.

## Physical and publication checks

Every requested body remains awake and finite. Each body's scene position and velocities match the direct-state publication exactly; reconstructed unit bases allow 1e-6 dimensionless error. An initially exact basis assertion rejected GPU differences of about 2.15e-7 after float angle reconstruction; position and velocity differences were zero. The reported maximum preserves the evidence for this justified roundoff tolerance.

Contact monitor counts match direct snapshots within the unchanged cap; collider RIDs and actual scene objects remain valid and all reported vectors are finite. Spatial neighbor checks inspect the full population. Pair penetration and wall escape must not exceed twice the configured .3 contact allowance plus .001 scene-unit floating-point slack (.601); speed must remain below 1000 scene units/s and final energy below 102 percent of initial energy. Contact entry and actual retained contacts must occur. These checks establish this workload's invariants, not identical trajectories or all physical capabilities. State/contact/event hashes are diagnostic and are never compared across backends as acceptance conditions.

| Bodies | Backend | Pair penetration | Wall escape | Max speed | Final / initial energy | Retained contacts | Max unit-basis difference |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1536 | CPU | 0.144402 | 0.057129 | 0.120870 | 0.434608 | 7419 | 0 |
| 1536 | GPU | 0.122564 | 0.114685 | 0.067078 | 0.428126 | 7618 | 2.15e-07 |
| 4096 | CPU | 0.274915 | 0.207642 | 0.005528 | 0.489862 | 20661 | 0 |
| 4096 | GPU | 0.216037 | 0.110107 | 0.028975 | 0.485015 | 20944 | 2.17e-07 |

## Late growth regression and ownership

The 64-body control uses 256 warmup and 64 measured ticks. Before the fix, GPU physical gates passed but the solver phase allocated 1344 owner/all-thread bytes. A temporary growth diagnostic identified twelve buffers crossing 64 slots (the history table crossing 128), with twelve 112-byte managed wrapper/delegate allocations. These were native pair/contact/sleep/constraint/history/report/color/packed buffers; monitor or interpolation storage was not the source.

Private storage and transfer buffers now adopt larger native allocations through their existing RenderHandle. Copy-preserving growth still completes the old-data copy before retirement; no solver interval is replayed. One device reference and release delegate survive growth. Closed adoption retires the incoming resource; old-release failure leaves the replacement owned for cleanup. This path is confined to exclusively owned private buffers and does not retarget published graphics resources. The unchanged 64-body control then passed with zero owner/all-thread bytes on both backends. [Lifetime tests](../../tests/Electron2D.Tests/RenderHandleReplacementTests.cs) exercise exact retirement, retained parent lifetime, closed wrappers and cleanup exceptions.

Native allocations, device copies and their waits still occur. Managed zero is not native zero, and the single before/after smoke timings are not a controlled speedup claim. Temporary growth/pose prints were removed; retained phase allocation counters remain in the performance report.

## Reproduction and remaining gates

```bash
ELECTRON2D_TEST_PHYSICS_PERFORMANCE=1 ELECTRON2D_PHYSICS_PERFORMANCE_COUNT=1536 ELECTRON2D_PHYSICS_PERFORMANCE_WARMUP=1600 ELECTRON2D_PHYSICS_PERFORMANCE_SAMPLES=1024 ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND=both ELECTRON2D_PHYSICS_PERFORMANCE_OUTPUT=bin/physics-scene-pipeline/2026-10-11/reproduce-1536 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll
```

Use count 4096 and samples 256 for the second trial; all other physical settings stay the same. Backend cpu/gpu selects one explicit implementation; both is the default. The former CPU-only native-state/profile probe is replaced by this shared public-state protocol.

Ignored evidence is in bin/physics-scene-pipeline/2026-10-11: original smoke, allocation-phases, allocation-growth, fixed-smoke, rejected exact-publication diagnostics, final-validated/1536 and /4096, revision-before/after JSON, host inventory and verification logs. Each final run records its exact command/environment/exit and binary/source fingerprints; both paired processes returned zero. Public backend registration and complete custom algorithms, repeatable ordinary-scene GPU advantage, native allocator accounting, other platforms and final real-window/network performance remain open under ADR 0054 and ADR 0103.

## Adaptive proposal batches, 2026-10-11

A controlled comparison based on main `89d6c24a` changes only the initial contact proposal budget from previous completed rounds plus four (minimum eight) to plus one (minimum four). Unfinished graphs still extend by sixteen and retain the same completion/conflict checks, 32-color/64-round fallback, contact rows and sixteen solver sweeps. The benchmark additionally reports final color/round counts and cumulative fallbacks; these final counts are diagnostics rather than sample distributions.

The same 1536-body workload above ran with 1600 warmup and 1024 measured frames. One control and two candidate paired trials returned zero; executable hashes stayed unchanged during each run and five-second process samples found no other build/test processes. These are sequential desktop trials with scheduling variability, not an isolated statistical experiment.

| Trial | CPU p50 / p95 / p99, ms | GPU p50 / p95 / p99, ms | GPU submissions per tick | Owner / all-thread bytes |
| --- | --- | --- | ---: | --- |
| Original budget control | 7.0722 / 9.1946 / 11.5407 | 10.5366 / 12.4585 / 13.3977 | 19 | 0 / 0 |
| Adaptive budget | 7.0024 / 8.0185 / 8.5251 | 10.0061 / 11.6528 / 12.4242 | 19.1895 | 0 / 0 |
| Adaptive repeat | 6.8748 / 7.9657 / 8.6419 | 10.2630 / 12.0836 / 13.0053 | 19.1631 | 0 / 0 |

GPU mean frame times were 10.6297 ms for the control and 10.0991/10.3902 ms for the candidates. Median summed device waits were 3.9664 ms and 3.8414/4.0310 ms. All three uploaded 24,856 bytes per tick; readback was 1,113,184 bytes for the control and 1,113,211.3/1,113,207.5 bytes for the candidates. The extra status reads account for the slight traffic increase; full observed motion/field/contact publication remains required by this workload.

The candidate GPU medians were 2.6–5.0 percent below the control in these trials. More frequent completion checks can add a status fence for a growing graph; the measured submission count records that cost. Both candidates retained zero fallbacks in this workload, all physical/publication gates and 0/0 warm managed bytes. Their maximum pair penetration was .154352/.131744 and wall escape .125427/.105042 scene units against the unchanged .601 limit. No cross-backend trajectory equality was required.

Resident solver checks also passed the 16,384-body dirty-scratch initialization, momentum/energy/material/ray/joint checks, changing 64/257-support fallback graph and 4096/65,536-body contact workloads. This does not establish GPU advantage over CPU: the 1536-body monitored scene in those trials costs more on GPU, and a rendered-window comparison was not run for that change. The wider numerical-extension/network/platform acceptance remains open.

Ignored evidence is in `bin/physics-scene-pipeline/2026-10-11/adaptive-color-rounds`: `control`, `paired`, `repeat`, source diff/hash and solver logs. A prior trial overlapping rendering integration is diagnostic only and is excluded from this comparison.

## 4096-body check at 567be307

With 1600 warmup and 256 measured complete frames, CPU/GPU p50 was 19.1687/18.8702 ms, p95 22.0300/21.2113 ms and p99 25.2851/22.2909 ms. Both returned zero and retained 0/0 owner/all-thread bytes; penetration/escape were .274915/.207642 and .234089/.132935 against the unchanged .601 limit. This roughly 1.6 percent median difference from one desktop pair does not establish a repeatable advantage. Evidence is `bin/physics-scene-pipeline/2026-10-11/567be307-4096`. The separate [16,384 server-body window checks](physics-current-performance.md#window-checks-at-567be307) retain their distinct workload and measurement scope.

## Monitored scene checks at d70110a6

The unchanged complete scene protocol above ran on main d70110a628221fa8f3925d1473380f8bd782c04b. Both implementations used 1600 warmup ticks. Two 4096-body pairs measured 256 and 1024 frames; one 16,384-body pair measured 256. Every pair completed with exit zero and valid physical/publication gates, with all requested bodies, eight monitored contacts and interpolation retained. These are sequential shared-desktop trials, not isolated statistical repetitions.

Loaded runtime SHA-256 was `db84c287ededc10fdc059132df8d38319911b7a14facd2f7f7141775497f1bdc`; test DLL was `ab6523a7a66cf23e3dda028888459299e6b406ba5b59be352848960addc9a14c`. These measurements precede the empty-scan change below.

| Bodies / samples | Backend | Mean, ms | p50 / p95 / p99 / max, ms | Owner / all-thread bytes |
| --- | --- | ---: | --- | --- |
| 4096 / 256 | CPU | 20.1142 | 20.0298 / 22.5672 / 24.0379 / 25.9840 | 0 / 0 |
| 4096 / 256 | GPU | 18.7077 | 18.5367 / 21.3748 / 23.3528 / 27.9854 | 0 / 0 |
| 4096 / 1024 | CPU | 19.5380 | 19.3491 / 21.1144 / 22.3172 / 38.8692 | 0 / 0 |
| 4096 / 1024 | GPU | 18.3105 | 18.1863 / 20.5982 / 22.2982 / 33.1032 | 0 / 0 |
| 16384 / 256 | CPU | 162.3769 | 109.8843 / 352.5091 / 502.0655 / 505.0431 | 0 / 0 |
| 16384 / 256 | GPU | 65.8914 | 65.5029 / 72.9492 / 82.5432 / 100.4959 | 0 / 0 |

GPU medians were 7.5 and 6.0 percent lower in the two 4096-body pairs, and 40.4 percent lower at 16,384. The 16,384 CPU tail is substantial and is retained above; its cause is not established. This demonstrates an advantage in these measured full monitored workloads, with desktop variability and final acceptance still open. It does not establish rendered FPS or all-scene superiority.

| Bodies / samples | GPU upload / readback B per tick | Uniform B / submissions per tick | Summed wait p50 / p95 / p99 / max, ms |
| --- | --- | --- | --- |
| 4096 / 256 | 65816 / 2966640.3125 | 61768.7500 / 19.113281 | 6.5177 / 7.9977 / 8.7323 / 9.4362 |
| 4096 / 1024 | 65816 / 2966640.8750 | 61644.4688 / 19.117188 | 6.5114 / 7.9679 / 8.6487 / 9.5292 |
| 16384 / 256 | 262424 / 11863144.4375 | 63757.1250 / 19.058594 | 19.6330 / 23.7507 / 33.5729 / 48.1518 |

These waits are already included in complete-frame cost. Readback serves current scene/contact observers and includes capacity-sized destinations; native allocator accounting and reduced publication traffic remain open. All six results have penetration/escape below .601, finite motion below 1000 and final energy below 1.02 times initial energy. At 16,384, CPU penetration/escape is .291941/.299999 and GPU .257734/.192383. Full phase means, motion/energy/contact evidence and diagnostic hashes remain in the raw reports.

| Raw report under bin/physics-scene-pipeline/2026-10-11 | SHA-256 |
| --- | --- |
| `d70110a6-4096/CPU.json` | `5ea969309da765a44fad862f65053e790ea0761ef20068ad3de899d8947141c5` |
| `d70110a6-4096/GPU.json` | `cf5062cd6820b2b5362da112c109d5858e3bb1e8f3ab356891108ef1820bb9fd` |
| `d70110a6-repeat-4096/CPU.json` | `7abb52dfbd91355e91788af2ee8dda5473cc55414e358c4e653526c713eb8696` |
| `d70110a6-repeat-4096/GPU.json` | `2204957c8b621c3ddcefd20e100a6c06bb916920554e64ec0790e19a47ebbb45` |
| `d70110a6-16384/CPU.json` | `a1fbea3293250374103c630b7542f4e838ab7c68ddf1e156077308e90d6079a4` |
| `d70110a6-16384/GPU.json` | `bccf3e05d234fff0689b7ab12546bb0717cf942a288a4c33521f9311030e6496` |

## Empty hierarchy guard scans

Physics authoring and structural/lifetime checks previously traversed ancestor/descendant nodes even when no actual solver attachment or geometry preflight existed. Conservative process-wide attachment and geometry-borrow presence now skips only that empty case. Tree extension hierarchy guards execute first; any scene/raw attachment or preflight retains the existing actual-space, thread, borrow and external-object checks. Attachment cleanup and preflight finally blocks balance the counters even on failure. The optimization adds no public API and changes no solver work, physical settings or observer quality.

A separate lifecycle-only CPU control/candidate pair built the same 16,384 RigidBody/CollisionShape scene. The control temporarily removed only the two early returns; counters, assertions and diagnostic timing remained. Both processes returned zero.

| Lifecycle-only trial | Scene setup, ms | Cleanup, ms |
| --- | ---: | ---: |
| Control | 18917.9187 | 17836.5843 |
| Empty scan skip | 3970.6192 | 10137.4176 |

Setup includes body authoring and SceneTree construction/attachment. Cleanup explicitly disposes tree, walls, root, resources and world. Lifecycle-only exits before simulation: it cannot establish warmed step time, allocation, physical gates or FPS. One sequential pair demonstrates the cold cost reduction; it is not a repeated statistical benchmark. The remaining attached hierarchy checks still scan conservatively while any world has attachments.

Reproduce using the existing scene command with ELECTRON2D_PHYSICS_PERFORMANCE_LIFECYCLE_ONLY=1, COUNT=16384 and BACKEND=cpu. Output adds separate CPU/GPU-lifecycle.json even during full simulations. Ignored evidence includes lifecycle-control-16384, lifecycle-candidate-16384 and their build/run logs. CPU/GPU ownership checks execute first preflight mutation rejection, failed source geometry, balanced detach and world retirement.

## Empty-scan candidate full-frame regression

The unchanged 4096-body protocol also ran with 1600 warmup and 256 measured frames after restoring the two early returns. Both CPU/GPU completed with exit zero, valid physical/publication gates and 0/0 owner/all-thread managed bytes. Loaded runtime/test SHA-256 were `26b4f54d8eb142f0707d31e1b1e4779178bec4c8403c62db6a28dfd0d8fed41d` / `fb48b52ef838d3638c66a55cfb3c410cd9ce2fa79fc6d3946b76b0e7586b6cfc`.

| Backend | Mean, ms | p50 / p95 / p99 / max, ms |
| --- | ---: | --- |
| CPU | 36.1473 | 21.2365 / 136.6726 / 233.7914 / 289.1697 |
| GPU | 27.3332 | 19.8834 / 68.1241 / 73.7828 / 74.4669 |

CPU and GPU tails increased in this run; their cause is unproven. This run establishes contract/allocation regression coverage for the guard change, not an improvement in solver or frame latency. The earlier d70110a6 comparisons retain their own executable identities. Raw reports and separate lifecycle timing are under bin/physics-scene-pipeline/2026-10-11/guard-candidate-4096. Real rendered-window FPS, network performance, native allocator accounting, other platforms and complete goal acceptance were not established by this slice.

## Full monitored scene in a native window

The existing fixture also accepts ELECTRON2D_PHYSICS_PERFORMANCE_WINDOW=1.
It authors the same requested RigidBody/CollisionShape population, four walls,
materials, eight-contact monitors, handlers and scene interpolation history.
The requested physics backend remains independent of the explicitly selected GPU
renderer. Window and lifecycle-only modes cannot be combined.

The fixed-frame test host invokes exactly one complete SceneTree.PhysicsFrame at
1/144 second, publishes every current scene pose into an explicit current/previous
MultiMesh buffer pair, invokes ProcessFrame and submits through the native Window
renderer. It pumps native events and checks early closure. This uses the retained
native open/pump/render/close path; it does not exercise Engine.Run's real-time
catch-up scheduler. Simulation time advances by one fixed tick per presented frame
even when wall time is slower. The presentation interpolation fraction is recorded;
the fixed host currently uses zero, selecting the previous completed pose.

Schema 3 separates complete PhysicsFrame timing/managed bytes from whole measured
window-loop timing/managed bytes. The frame bracket includes native event pumping,
physics, CPU scene-pose publication, idle processing, rendering/presentation and
allocation/profiling probes. scenePosePublication measures the full current/previous
buffer publication; render measures the native renderer call after idle processing.
Their sum need not equal the complete frame cost. Physics transfer/wait counters
retain their original scope and exclude graphics transfers. Native allocation and
isolated graphics transfer/wait accounting remain unmeasured.

Capture and final physical/publication validation occur after measurement. Capture
checks an instance for every body, each current record against the corresponding
physical pose, exact 1152-by-800 image dimensions and actual particle pixels.
The PNG and physical JSON are saved for each backend. VSync records both requested
and reported modes: the 64-body Linux/Wayland smoke requested Disabled but the
renderer reported Enabled. Its roughly 145 FPS is consequently not an uncapped
throughput claim. Both smoke implementations passed physical gates and zero warm
physics/window owner/all-thread managed bytes.

The lifecycle cleanup field retains its tree/body/resource/world scope; native
window/renderer closing follows through the display owner's disposal and is not
included in that field. The final capture is an additional explicit GPU image
readback outside the sampled physics and frame brackets.

### Native window results at 9d4fbc56

These measurements precede later renderer integration into main; the retained fingerprints identify their actual executable revision. Both pairs used 1600 warmup ticks and 256 measured frames with the full protocol above. The CPU and GPU phases ran sequentially in each process and returned zero. C# source and runtime/test binaries stayed unchanged throughout both pairs; no assembly rebuild occurred between implementations. These are shared-desktop single pairs, not isolated repeated statistical experiments. The host is Linux/Wayland x64, AMD Ryzen 7 5700X, NVIDIA GeForce RTX 3090 Ti, driver 615.71.09 and .NET 10.0.101. Requested VSync was Disabled and the reported mode was Enabled in every case.

| Evidence | SHA-256 |
| --- | --- |
| C# source | `0dae5b17d9a81503f1ea00d2ce018cb8d5dfa3c420fbe09d85ac7704dcb653d2` |
| Electron2D.Tests.dll | `492ac51351c868d615912d058000321e823d83d016640b31d7021a90cfa3e71a` |
| Electron2D.dll | `e537c2c758254366e739dae38405ade459114c0ef3ff3fd46b3f22d7091a34ff` |

| Bodies | Backend | Physics mean, ms | Physics p50 / p95 / p99 / max, ms | Whole window-loop FPS |
| --- | --- | ---: | --- | ---: |
| 4096 | CPU | 21.1888 | 20.9972 / 23.0557 / 23.6570 / 24.1162 | 30.5369 |
| 4096 | GPU | 22.9258 | 22.6867 / 26.0698 / 27.6144 / 28.9203 | 29.2994 |
| 16384 | CPU | 102.7040 | 101.9317 / 109.5691 / 115.5848 / 122.0989 | 5.9766 |
| 16384 | GPU | 82.8894 | 76.8210 / 101.3891 / 105.3836 / 114.2046 | 6.9794 |

| Bodies | Backend | Full frame p50 / p95 / p99 / max, ms | Pose publication p50, ms | Native render p50 / p95, ms |
| --- | --- | --- | ---: | --- |
| 4096 | CPU | 32.4149 / 35.5415 / 36.1951 / 37.2560 | 0.1960 | 11.0129 / 12.5058 |
| 4096 | GPU | 33.7265 / 37.4940 / 39.9930 / 42.0875 | 0.1387 | 10.6899 / 12.3055 |
| 16384 | CPU | 166.1431 / 175.6557 / 196.6900 / 226.7950 | 0.9372 | 62.4581 / 66.6599 |
| 16384 | GPU | 138.5962 / 163.0043 / 167.2275 / 174.4873 | 0.6355 | 59.0048 / 62.0318 |

All four cases passed physical/publication, full-instance and real image checks; physics and complete window-loop owner/all-thread managed bytes were 0/0. At 4096 bodies GPU physics p50 was 8.0 percent higher and window FPS 4.1 percent lower than CPU. At 16,384 GPU physics p50 was 24.6 percent lower and window FPS 16.8 percent higher. The mass advantage is measured under this fixed host and presentation policy; it does not certify real-time catch-up performance, every scene or the full objective. Native rendering around 59–62 ms remains a substantial separate cost at 16,384. Its internal causes have not been isolated.

| Bodies | GPU upload / readback B per tick | Uniform B / submissions per tick | Summed physics wait p50 / p95 / p99 / max, ms |
| --- | --- | --- | --- |
| 4096 | 65880 / 2966724.3125 | 62414.2500 / 21.113281 | 10.1147 / 12.6731 / 13.2576 / 15.7378 |
| 16384 | 262488 / 11863227.8750 | 64456.1250 / 21.054688 | 24.8422 / 50.7493 / 54.8919 / 61.7190 |

Physics-store traffic/submission totals span the complete loop and include demanded observation from publication/render consumers. The wait distribution samples the complete PhysicsFrame bracket only; any additional store waits during drawing remain inside frame/render time without a separate wait distribution. Final validation/capture traffic and waits are excluded. Native graphics bytes and separate graphics waits are not included. All four cases retain penetration/escape below .601, finite speed below 1000 and energy below 1.02 times initial energy. At 16,384 CPU penetration/escape was .291941/.299999 and GPU .276851/.176327. Raw reports preserve phase means, timing distributions, contact counts and state/contact hashes.

| Raw report under bin/physics-scene-window/2026-10-11 | SHA-256 |
| --- | --- |
| `paired-4096/CPU.json` | `f29f1f411475735634a28bac9f2f366801fe646036f386297b030a63dc024092` |
| `paired-4096/GPU.json` | `71c49496806e805f92840487f227d0a3abee8a93b8cb297e645cba76f79275ba` |
| `paired-16384/CPU.json` | `a47527111265885688695dc1f0a442274043b37bb56be5dba99653cd88778a6b` |
| `paired-16384/GPU.json` | `9c18e00f62702bc9dc5a01679c3719083ae5ba599bd40a0f90ccf843c55e983c` |

Reproduce after the source-native test build:

```bash
ELECTRON2D_TEST_PHYSICS_PERFORMANCE=1 ELECTRON2D_PHYSICS_PERFORMANCE_WINDOW=1 ELECTRON2D_PHYSICS_PERFORMANCE_COUNT=16384 ELECTRON2D_PHYSICS_PERFORMANCE_WARMUP=1600 ELECTRON2D_PHYSICS_PERFORMANCE_SAMPLES=256 ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND=both ELECTRON2D_PHYSICS_PERFORMANCE_OUTPUT=bin/physics-scene-window/reproduce-16384 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll
```

Use count 4096 for the smaller paired workload. The ignored directories contain provenance.json with exact command/environment, start/end times, exit status and source/binary fingerprints, CPU/GPU physical/lifecycle reports and CPU/GPU-window.png. The source fingerprint hashes sorted C# paths under src and tests/Electron2D.Tests, excluding bin/obj, as path, NUL, contents, NUL.

After main advanced to 7d72122e077a5c86e2ff129fb85132f04830d995, a fresh source-native
build and the 64-body window pair passed again with valid physical/image/instance
checks and 0/0 physics and window-loop managed bytes. Its runtime/test SHA-256 are
93ed104c97284d89f2535a13ce35df134e151bdca7ae8184d258b421a9c0339d /
4b70511c0a171d4750f4166a13f9753a2088cf0e671d718bfe72f94e26dbebee.
Evidence is under bin/physics-scene-window/2026-10-11/integrated-smoke-64. This
functional regression does not refresh the large-scene performance numbers above.

## Physics and native event allocation boundaries

The first fresh 16,384-body CPU window run based on 3bc28190 passed physical and
publication invariants and recorded 0/0 PhysicsFrame managed bytes, but the whole
window loop allocated 320 owner/all-thread bytes. Schema 3 rejected the aggregate
and stopped before GPU; this is a failed run, not a completed CPU/GPU pair. Evidence
is bin/physics-scene-window/2026-10-11/3bc28190-refresh. The original event/segment
was not instrumented, so its exact source is unproven.

Schema 4 retains the complete loop byte counts and explicitly separates
physicsManagedZero from frameManagedZero. The requested zero-allocation physics
gate covers the whole PhysicsFrame, including scene publication, monitoring,
callbacks and interpolation history on owner/all threads. Window event pumping,
pose-buffer publication, idle processing and native rendering expose separate
owner-byte totals. Frame allocation remains visible even when the physical gate
passes; headless mode additionally retains its aggregate zero-byte gate. No
physical settings, objects, contacts, events or rendering work are removed.

A deterministic native mouse-motion probe adds one SDL event before each pump
with ELECTRON2D_PHYSICS_PERFORMANCE_INJECT_INPUT=1. In the 64-body CPU/GPU
256-warmup/64-sample probe, each implementation allocated exactly 20,480 bytes in
the pump, or 320 per event. Pose publication, idle processing, native rendering
and the entire physics bracket each allocated zero. The public input event is a
fresh owned object in the native dispatch path. Both physical/instance/image
checks passed; frameManagedZero is explicitly false in both reports. This
reproduces a possible 320-byte event source without claiming the uninstrumented
large run's exact cause. Injection is a diagnostic workload and remains disabled
in performance comparisons. The probe is under injected-input-64.

### Scoped 16,384-body refresh on 3bc28190

The same 1600-warmup/256-sample native-window workload ran after the schema-4 instrumentation change. Both implementations returned zero, retained every body and passed physical/publication/instance/image checks. Source and loaded runtime/test fingerprints were identical before/after the paired process. The fixed-host/presentation/VSync limits above remain unchanged; deterministic input injection was disabled.

| Evidence | SHA-256 |
| --- | --- |
| C# source | `d51a89b862477009b61fda2614d946dff279d328799ba840b9cbd29408d46e5b` |
| Electron2D.Tests.dll | `d88988aceb2603a026b74b2d5b1a370f62a1ad97158489534439baffd4f74756` |
| Electron2D.dll | `2328a2d993315b88941b6cccb3c7bf5c3cf57dd8dfb543bd0c3a37e63857ad26` |

| Backend | Physics mean, ms | Physics p50 / p95 / p99 / max, ms | Full frame p50 / p95 / p99 / max, ms | Window-loop FPS |
| --- | ---: | --- | --- | ---: |
| CPU | 127.1079 | 121.4342 / 144.8258 / 446.5245 / 481.3297 | 193.8159 / 239.6429 / 663.7224 / 1076.8348 | 4.9026 |
| GPU | 98.2498 | 85.0174 / 166.5107 / 385.0471 / 479.4756 | 159.2044 / 278.1808 / 566.2099 / 784.8720 | 5.7116 |

Both physical and whole-loop owner/all-thread allocations were 0/0 in this repetition; all four extra window segment counters were zero. GPU physics median was 30.0 percent lower and FPS 16.5 percent higher in this single shared-desktop pair. Large tails remain visible and their cause is unproven. This is measured progress on the preserved mass scene, not complete objective acceptance. Native render p50 was 70.0257 ms on CPU and 70.5925 ms on GPU.

GPU physical wait p50/p95/p99/max was 25.1897/59.7602/90.5693/112.2805 ms. Store upload/readback was 262488/11863229.5625 B per tick, uniform 64605.875 B and submissions 21.06640625. The established counter scopes exclude final validation/capture and separate graphics traffic. CPU/GPU penetration/escape was .291941/.299999 and .279062/.156250, below the unchanged .601 limit. Native allocation, isolated graphics costs, real-time scheduler and final numerical/network acceptance remain open.

| Raw physical report under scoped-16384 | SHA-256 |
| --- | --- |
| `CPU.json` | `5645f8da53fdd4f5e639392ae6f8cc0164be27e957f97b1c0fd25b82c311b7c1` |
| `GPU.json` | `8fb21cba48cfaa8a4fd90c44aa361bf2a5815d035fe57e77b74aa06da4f1b1e3` |

The ignored directory bin/physics-scene-window/2026-10-11/scoped-16384 contains the exact provenance/command, full physical/lifecycle reports, PNG captures and terminal log. The earlier 3bc28190-refresh rejection is retained separately; it is not relabeled as a passing pair.

## 16,384-body refresh at f3983555, 2026-10-12

This trial retains the complete RigidBody/CollisionShape fixture above: 16,384
awake bodies, four walls, radius 6, mass .2, the same materials/damping/gravity,
eight-contact monitors, entry/exit handlers and interpolation. Each backend warms
for 1600 ticks and measures 256 complete PhysicsFrame calls at 1/144 second,
four substeps and sixteen solver iterations. Physics profiling stays enabled;
publication control and native input injection are disabled. No physical capability,
population or observer is removed to obtain the comparison.

The clean source revision is main `f3983555575dd16ab31cbafaace697a363d80ccf`.
After a fresh Release linux-x64 source-native build, all 71 executable/dependency
files were copied to an independent ignored runtime directory, without hardlinks.
The measurements execute that copy, with every file fingerprint retained in the
manifest. This prevents another chat's build from replacing measured assemblies.
The source fingerprint uses the sorted C# path/NUL/content/NUL protocol above;
runtime fingerprints also cover embedded physics and graphics shader resources.
The host remains Linux/Wayland x64, Ryzen 7 5700X, RTX 3090 Ti, driver 615.71.09
and .NET SDK 10.0.101. These are sequential pairs on a shared desktop.

| Evidence | SHA-256 |
| --- | --- |
| C# source | `10528540ff65aa4874d00b97a045c0b6ce6b37aa6cb32a59b3c6218d0260f154` |
| Electron2D.Tests.dll | `4371780e96f143dc7017935ac7e2045d207270df403bcb524350b9e08df3eb57` |
| Electron2D.dll | `641e8442e552ed10f2b24090b3fb08d0fc89a99fab721a1cc6cdb4d49b9b1dd1` |

### Complete headless scene

| Backend | Mean, ms | p50 / p95 / p99 / max, ms | Owner / all-thread managed bytes |
| --- | ---: | --- | --- |
| CPU | 91.1890 | 89.3855 / 104.3433 / 128.1832 / 138.1512 | 0 / 0 |
| GPU | 67.6875 | 66.1131 / 82.3523 / 96.7675 / 104.8345 | 0 / 0 |

GPU median is 26.0 percent lower and mean 25.8 percent lower in this pair.
These are complete physics-frame costs, not FPS or isolated solver throughput.
CPU/GPU solver means are 50.3323/22.3261 ms; result synchronization means
.0012/5.2630 ms and scene/contact publication means 21.2535/25.9772 ms.
The publication cost includes all observed bodies and selected contacts.

GPU uploads/readbacks are 262424/11863146.6875 bytes per tick, uniforms
63709.5 bytes and submissions 19.07421875. Summed wait p50/p95/p99/max is
18.1057/34.6409/44.9031/52.8950 ms, already included in the step. Readback
serves current poses, direct-state fields and contacts for every observed body;
capacity-sized destinations remain part of its measured cost. These counters
exclude renderer traffic, final validation and JSON generation.

Both implementations pass the unchanged physical/publication gates. CPU/GPU
penetration is .291941/.292492, escape .299999/.237793 against .601, speed
.197081/.026373 and retained contacts 86019/88106. Unit-basis differences are
0/2.38e-7 against 1e-6; final energies remain below 1.02 times initial energy.
State/contact/event hashes remain diagnostic, without cross-backend equality gates.

Cold scene setup CPU/GPU costs 4003.8716/3570.8523 ms; cleanup costs
51604.8785/51596.0172 ms. Those separate lifecycle brackets are outside warmed
physics timing and allocation. Their remaining cost is visible rather than folded
into a warm-step claim.

### Native window on the same frozen runtime

| Backend | Physics mean, ms | Physics p50 / p95 / p99 / max, ms | Full frame p50 / p95 / p99 / max, ms | Window-loop FPS |
| --- | ---: | --- | --- | ---: |
| CPU | 97.4403 | 95.2527 / 113.2923 / 135.2082 / 178.2517 | 159.1198 / 188.5953 / 210.1101 / 252.4444 | 6.1883 |
| GPU | 77.3591 | 72.6124 / 99.8078 / 109.7514 / 116.5524 | 133.6631 / 160.2246 / 177.5044 / 186.1306 | 7.3688 |

GPU physics median is 23.8 percent lower and measured FPS 19.1 percent higher
in this pair. Physics and entire window-loop owner/all-thread managed totals are
0/0 on each backend; pump/publication/process/render owner counters are also zero.
Both actual 1152-by-800 captures were visually inspected and show the dense pile;
the fixture verifies one rendered instance for every physical body and all current
pose records. Requested VSync is Disabled but reported VSync is Enabled on both.
These are real-window measurements under the fixed-frame test host, not an
uncapped claim or an Engine.Run catch-up-scheduler test.

CPU/GPU pose-publication p50 is .9721/.7235 ms and render p50 is
61.5951/56.3873 ms. GPU physics wait p50/p95/p99/max is
22.7524/50.4636/58.6181/66.7947 ms; upload/readback is
262424/11863144.4375 bytes per tick, uniforms 63607.75 bytes and submissions
19.05859375. Those physics counters include their waits in complete step time
and exclude graphics traffic and the final image capture. Physics/publication
checks pass: CPU/GPU penetration is .291941/.282163, escape .299999/.240112
against .601 and retained contacts 86019/88402. All motion/energy/basis checks
retain the same tolerances; matching backend trajectories are not required.

### Evidence, reproduction and scope

The independent runtime, before/final manifests, per-backend physical/lifecycle
JSON, native-window PNGs, exact process outcomes and logs are retained under the
ignored directory bin/physics-performance-refresh/2026-10-12/frozen-f3983555.
Reproduce from a fresh source-native Release build copied into its own runtime
directory; do not overwrite a retained measured copy. The measured headless command
is:

```bash
ELECTRON2D_TEST_PHYSICS_PERFORMANCE=1 ELECTRON2D_PHYSICS_PERFORMANCE_COUNT=16384 ELECTRON2D_PHYSICS_PERFORMANCE_WARMUP=1600 ELECTRON2D_PHYSICS_PERFORMANCE_SAMPLES=256 ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND=both ELECTRON2D_PHYSICS_PERFORMANCE_OUTPUT=bin/physics-performance-refresh/2026-10-12/frozen-f3983555/headless dotnet bin/physics-performance-refresh/2026-10-12/frozen-f3983555/runtime/Electron2D.Tests.dll
```

Window mode adds ELECTRON2D_PHYSICS_PERFORMANCE_WINDOW=1 and changes the output
directory to window. It retains the fixed-frame host, interpolation fraction zero
and separate physics/frame/allocation/capture boundaries described above.

An earlier headless pair at 368d29d3 had unchanged source/runtime/test fingerprints
at its verification points and passed physical/managed gates: CPU/GPU p50 was
92.1341/66.5457 ms, but p99 was 108.2136/263.1903 ms. Its later window pair
was rejected for same-revision acceptance: another chat changed main to f3983555
and replaced shared assemblies before that process completed. Those window
numbers are excluded rather than relabeled as frozen-runtime evidence.
The original artifacts and rejection reason remain in the parent directory.

This mass-scene evidence changes no declaration coverage status. Single desktop
pairs, including the earlier GPU tail regression, do not establish an isolated
repeatability guarantee or explain scheduling/driver outliers. Native allocation,
isolated renderer transfers/waits, real-time catch-up scheduling, other platforms,
complete extension/numerical acceptance and human acceptance remain open. The
goal requires measured GPU advantage and real-window FPS, without a sustained
60 Hz threshold. The complete physics objective remains open under
[ADR 0054](../decisions/physics-backends.md#adr-0054) and
[ADR 0103](../decisions/physics-extensions.md#adr-0103).

| Raw report under frozen-f3983555 | SHA-256 |
| --- | --- |
| headless/CPU.json | `e8490f128a9b76986e384574e52fc1657dcef04ae9e0f40efc796728638528e4` |
| headless/GPU.json | `eb2a63af4b6f1ff9166c9f2d22b3bc7b78f886a6424d8f87306145670cccb16a` |
| window/CPU.json | `ce8f8b1ba0655be75fc1764d8cf4a0b0195753d7af515e5c42c0dcd7e2d0bbea` |
| window/GPU.json | `f103d348dc5191a41f299ad30715117d2b54f6804a6cd278086444c344b492e6` |


## Frozen complete scene at 7c37a291, 2026-10-13

A source-verified copy of the Release runtime from 7c37a291 ran both explicit
CPU/GPU implementations in the same monitored scene: 16,384 awake scene bodies,
four walls, 144 Hz, four substeps, sixteen iterations, friction .25, bounce .35,
eight reported contacts per body, real enter/exit callbacks and interpolation.
Each pair used 1600 warmup ticks and 256 measured complete ticks; neither path
reduced the body count, physical settings or observation features.

| Profile | CPU step p50 / p95 / p99 ms | GPU step p50 / p95 / p99 ms | CPU / GPU actual window FPS | Owner / all-thread bytes |
| --- | --- | --- | --- | --- |
| Headless complete scene | 126.2022 / 399.5967 / 448.7577 | 81.2019 / 178.9792 / 211.0549 | — | 0 / 0 on both |
| 1152×800 native window | 105.0550 / 119.3940 / 124.5442 | 97.9140 / 116.6219 / 130.5440 | 5.8356 / 6.1555 | 0 / 0 on both |

The headless median ratio is 1.554 CPU/GPU; the window
physics median ratio is 1.073. Actual window throughput differs by
5.48 percent in this single desktop pair. These results show
a measured advantage for this workload, with substantial latency variability;
they do not establish repeatability, a general GPU win or sustained 60 Hz.

| GPU complete path | Upload B/tick | Readback B/tick | Submissions/tick | Wait p50 / p95 / p99 ms |
| --- | ---: | ---: | ---: | --- |
| Headless | 262424 | 11863151.1875 | 19.1055 | 11.3960 / 15.5534 / 19.7629 |
| Window | 262488 | 11863226.7500 | 21.0469 | 35.9675 / 51.7473 / 64.5518 |

The GPU remains the authoritative numerical owner. This deliberately observed
scene requires current logical poses, point velocities and capped contact values
for all receivers before callbacks. The roughly 11.86 MB read is the combined
publication/report path, not proof that internal broad-phase/constraint topology
is mirrored. It is a measured cost, not a claim of minimal traffic. In headless
profiling the GPU solve, result synchronization and body/contact completion means
are 17.9460, 7.9470 and 54.7095 ms.
The full public path includes those host costs; a kernel-only timing cannot
replace the total step distribution.

| Window component p50 / p95 / p99 ms | CPU | GPU |
| --- | --- | --- |
| Full frame | 168.5976 / 188.0159 / 193.2391 | 162.7397 / 186.0620 / 196.8254 |
| Pose publication to drawing | 1.0183 / 1.2426 / 1.4295 | 1.0684 / 1.4173 / 1.9720 |
| Draw/present | 62.4491 / 68.0236 / 70.4011 | 63.7471 / 68.4804 / 72.7594 |

Both windows requested Disabled VSync and reported Enabled. The fixed-frame host
uses one physics tick per draw and fraction zero; the FPS includes actual drawing
and presentation but is not a catch-up scheduling or refresh-rate guarantee.
Timed physics and complete-frame owner/all-thread managed bytes are zero. PNG
capture and final validation occur outside those brackets. Both PNGs were read
and visually checked; the mass scene is present.

Physical gates retain penetration/escape ≤ .601 scene units and basis agreement
≤ 1e-6. CPU penetration/escape is 0.297866821/0.299926758; GPU
headless is 0.089958191/0.022941589, and window is
0.116661072/0.077636719. All motion/energy/awake/publication
checks pass. CPU retains 86303 contacts, GPU headless/window
91591/91008; different lists/trajectories are allowed.

The immutable runtime manifest remained byte-identical throughout both processes.
Main advanced to 412222a7 during the window pair due to another chat's offscreen
rendering work; the repository-state guard then stopped the outer runner. Each
benchmark itself exited zero. These are valid frozen-7c37a291 results, not results
for the newer main. The source/revision drift and guard failure are retained;
network work subsequently reused only this same verified frozen runtime. Shared
desktop activity and the concurrent work limit performance attribution.

Evidence: bin/physics-goal-acceptance/2026-10-13/frozen-7c37a291 contains exact
commands/environment/outcomes, runtime manifests, source fingerprints, JSON and
PNGs. Runtime source SHA-256 is
`c54bd0322612deac7e4728928c390bc9130b307a7f9762e12384b66b069834d6`;
Electron2D.dll is `94b6b85ce0611455c65f944e9fb00e31c5d03f35878687d08aa008557185c6bf`;
test DLL is `71499d686dd220e445cc3d71f3fce97df2bac7f6e17929a363af6c68a5d80276`.
Native allocator totals, isolated renderer transfers/waits, other platforms and
complete inherited/registered numerical acceptance remain open.

## Completion-stage CPU control, 2026-10-13

Optional internal profiling now splits the existing completion phase into
pose/motion/resolved-field publication, contact capture/monitor tracking and
extension-contact projection/event queue preparation. The three intervals remain
inside phase 5; the benchmark checks their summed duration against that phase.
User event delivery remains in its existing later phase. Profiling adds three
interval timestamps when enabled, with no per-body timestamps or new device waits.
The public API and the default contact scheduling policy are unchanged.

A frozen Release build based on main baa00305 plus this profiling slice ran two
CPU-only processes. Both retained the complete 16,384-body scene above, four walls,
1600 warmup ticks and 256 measured ticks. Only
ELECTRON2D_PHYSICS_PERFORMANCE_SERIAL_CONTACTS changed from 0 to 1: the control
forces contact publication onto the owner while leaving the four-worker numerical
solver and all observation features intact. This is a host-stage control, not a
different physics implementation or a CPU/GPU comparison.

| CPU contact publication | Complete step p50 / p95 / p99 / max ms | Completion mean ms | Pose/fields mean ms | Contacts/monitor mean ms | Extension/events mean ms |
| --- | --- | ---: | ---: | ---: | ---: |
| Default parallel | 102.5065 / 113.9158 / 136.3221 / 144.4272 | 25.1609 | 5.8715 | 17.7884 | 1.4994 |
| Forced serial | 133.2271 / 147.3538 / 158.4564 / 164.7605 | 60.3021 | 5.6112 | 53.2847 | 1.4044 |

Both processes exited zero with 0/0 owner/all-thread managed bytes, 86,303 retained
contacts, 156,928 enter and 70,625 exit observations. Penetration/escape was
.297866821/.299926758 against the unchanged .601 limit; motion, energy, awake and
publication gates passed. Same-backend state/contact hashes also matched as a
diagnostic. These single sequential shared-desktop trials show that contact
publication can dominate host completion; they do not establish isolated scaling
or a GPU speedup. The earlier default-only run also passed with 0/0 bytes and a
91.5712 ms step median, illustrating between-run host variability.

The GPU currently collects completed scene contacts serially after immutable
readback. Its new subphase timing has not been exercised. Parallel host conversion
is a candidate to measure, preserving owner-thread callbacks and device-owned
numerics. Capacity-sized GPU reports are another measured cost: 80-byte rows at
eight slots for 16,384 receivers occupy 10,485,760 bytes before counts and state
publication. Compacting those destinations must account for any additional
selection/count wait; neither optimization is implemented by this slice.

These CPU runs explicitly denied graphics availability through the dummy video
driver, invalid GPU driver/missing Vulkan files and absent display/session paths.
The owner confirmed that the power cable involved in the latest device-loss
investigation remains unchecked. Long mass GPU and native-window trials are
deferred until its compatibility is verified; GPU performance acceptance stays
open. No GPU load was added by this control.

Main advanced to 49256885 during the serial trial. The saved runtime remained
byte-identical across both processes; the observed C# fingerprint reconstructs
exactly from baa00305 plus the four changed profiling files. These timings belong
to that frozen candidate, not the newer main. Evidence under
bin/physics-completion-profile/2026-10-13/cpu-paired retains the build, commands,
runtime manifest, source binding, candidate patch, outcomes and full reports.

| Evidence | SHA-256 |
| --- | --- |
| C# source under src and tests/Electron2D.Tests | `dc98ca3d96ff68d8c2774ff2aefc537cf71acef2ca536fb1b04abfd454b32895` |
| Frozen runtime DLL | `b37963fd16ada005907aca245b28f56037b5b8e48c693890da4d2befc609e82e` |
| Frozen test DLL | `c17bf34f244c05831080c4fc50b6de0f2e690fb4099d83902fe460a4d27c89a0` |
| parallel/CPU.json | `b4b480831bc9e9ec296949ab34eb77024196b7d6e03b040077465d9d6b42aa28` |
| serial/CPU.json | `5ce8de803fa89aa2de70a50af0cd80e560ebdc1b50a065beae5f4802dc19c752` |

Reproduce with the complete-scene command above using BACKEND=cpu and
SERIAL_CONTACTS=0 or 1. Complete-step distributions and three named completion means
are emitted in CPU.json. Native allocation totals, GPU subphases, current-revision
paired GPU/window measurements and final physics acceptance remain open.

The final slice was rebuilt and checked on main 49256885: CPU scene force/contact/
shape-event regressions, retained-worker scheduling and both 512-body profiling
modes passed with graphics denied. Each small profile used 256 warmup and 64 measured
ticks and retained 2,506 contacts with 0/0 managed bytes. Release runtime/test builds,
both formatting checks, compiled API coverage and wiki tests/generation/check
passed; source and saved runtime stayed unchanged throughout. This is CPU-only
verification of the profiling change, not a fresh full GPU/native/network gate.
Logs and manifests are retained in the sibling final-checks directory.

## CPU contact-pair capacity, 2026-10-15

The [owner-pipeline follow-up](physics-retirement-memory.md#functional-network-and-complete-step-checks)
exposed 4,120 managed bytes in CPU `Solve` with a 64-body monitored scene after
128 warmup frames. A fresh run on the unchanged `4fcd3a70` assembly reproduced
exactly 4,120/4,120 owner/all-thread bytes. Temporary allocation-site diagnostics
identified `B2SetItem[512]` in `b2GrowTable`, called by contact creation during
broad-phase pair updates. The diagnostic was removed byte-for-byte before the
accepted build; instrumented output is not ordinary timing/allocation evidence.

`PrepareSolverCapacity` already prepares four contacts per power-of-two body
capacity. It now grows the broad-phase pair hash table in that same owner phase,
with two slots per budgeted contact because insertion grows at half occupancy.
Existing keys are rehashed and preserved. It does not create or remove contacts,
change numerical algorithms, select another backend or extend warmup. Tables
reuse ordinary native-world lifetime/checkpoint paths. Larger topologies beyond
that budget can still grow cold capacity; this is not an all-pairs reservation
or a guarantee for arbitrary authored topology changes.

### Existing full-scene regression

The original `PhysicsPipelinePerformance` executable is the regression: its
physical, identity, scene/direct-state, event, interpolation and strict allocation
assertions remain unchanged. No duplicate scene, test-only preallocation or
allocation allowance is introduced. Fresh processes run 128 warm and 128 measured
complete `PhysicsFrame` calls at 144 Hz, four substeps and 16 iterations, with four
walls, awake bodies, materials, eight reported contacts and contact monitors.
CPU uses dummy video and an unavailable driver/ICD, without display or renderer;
GPU controls use the pinned lavapipe ICD with disabled loader layers and no real window.

| Bodies | Policy | Backend | p50 / p95 / p99 (ms) | Owner / all-thread managed bytes |
| ---: | --- | --- | --- | --- |
| 64 | Owner | CPU | 0.1367 / 0.1624 / 0.2294 | 0 / 0 |
| 64 | Separate | CPU | 0.1600 / 0.1940 / 0.2513 | 0 / 0 |
| 256 | Owner | CPU | 1.6089 / 1.7488 / 1.9636 | 0 / 0 |
| 256 | Separate | CPU | 1.6421 / 1.8082 / 1.8913 | 0 / 0 |
| 64 | Owner | GPU | 8.4178 / 11.4226 / 12.4407 | 0 / 0 |
| 64 | Separate | GPU | 8.4472 / 11.1123 / 11.7693 | 0 / 0 |

Both 64-body CPU profiles retain the original 272 entries, 245 exits and 27
reported final points. Final state/contact hashes also match the old serial
profile as auxiliary diagnostics; the public physical/invariant assertions are
the acceptance boundary. The unchanged software-GPU controls each upload 1,304
bytes and read 47,456 bytes per tick. These small-scene trials overlap unrelated
local checks and do not establish a before/after speed improvement, target GPU
advantage or real-window FPS. Complete percentile/wait/phase/traffic reports
retain the original schema.

### Storage cost and verification limits

The same raw public world/history fixture is measured before and after this
change through the calibrated CLRMD/process runner. Both source-matched runtime
assemblies execute the identical fixture binary. At 64 bodies/one history, the
live owner graph adds 4,096 bytes and the world-plus-history graph adds 8,192
bytes: one larger pair-key array in each owner. After history release only the
world's 4,096-byte increase remains; after source-world release the difference
is zero. The 1,024-body/four-history fixture already reached this pair-table capacity
during simulation, so all six selected-graph measurements remain unchanged. These
are controlled graph deltas, not RSS/native totals or all-scene memory estimates.

The five managed regressions cover backend ownership, private replay, separate
numerical execution, failed/ordinary world retirement and static pool cleanup.
The existing checkpoint transfer preserves the table's captured capacity and
keys. Strict Linux x64 NativeAOT passes the 32-world ordinary/failed lifetime
matrix and private CPU checkpoint contact/joint/sleep/sensor/query replay. Its
1,024-box/four-worker checkpoint test retains 0/0 managed bytes for 64 warm and
128 measured capture/restore pairs; it is not a native run of the evolving scene.
The first focused host build lacked its real `PhysicsDebugTests` helper; linking
the actual file corrected the test-host compilation without stubs or runtime changes.

Two complete mixed network checks run CPU servers with an unavailable GPU and
software-GPU primary clients in serial/separate policies through tick 900, with
late admission at 310. Existing impaired delivery, correction/replay, identity,
lifecycle/event and strict 0/0 warm step/replay assertions pass; their 30 memory
observations keep collected-owner checks. These memory hosts do not instrument
numerical thread identity. The selected-source/PDB audit covers both runtime
profiles, the 710-file managed test project, focused native host and actual raw/
network/probe builds. These scopes do not replace the evolving-scene checks above.

Reproduce the unchanged regression with the Release test executable:

```bash
ELECTRON2D_TEST_PHYSICS_PERFORMANCE=1 \
ELECTRON2D_PHYSICS_PERFORMANCE_COUNT=64 \
ELECTRON2D_PHYSICS_PERFORMANCE_WARMUP=128 \
ELECTRON2D_PHYSICS_PERFORMANCE_SAMPLES=128 \
ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND=cpu \
ELECTRON2D_PHYSICS_PERFORMANCE_THREADED=0 \
dotnet PATH_TO_RELEASE_TEST_DLL
```

Use `THREADED=1` for the separate lane and `COUNT=256` for the larger CPU profile.
The software-GPU controls use `BACKEND=gpu` and the explicit pinned ICD. Local
artifacts: `bin/physics-cpu-evolving/2026-10-15/` contains red/diagnostic logs,
`green-*`, five regression logs, matched `baseline-memory-*` and `memory-*`,
source/PDB manifests and final validation. Public API and coverage states are
unchanged. Residual GPU native retention, unrestricted topology budgets,
foreign-platform/hardware/window acceptance and the full physics objective remain
open; RTX loading stays deferred while its power cable is unverified.

The first before/after memory comparison used source-matched fixture builds with
different revision metadata. The accepted comparison reruns one byte-identical
current fixture against isolated copies of the old and corrected runtime; the
provisional reports remain separate. The final 24 checks pass, including 54
matched heap/process observations and all seven source/PDB sets with no missing
or mismatched Compile inputs. Release, formatting, API coverage and wiki checks
pass; generated output remains ignored.

- Corrected matched runtime SHA-256: `89f4503aa9ba4fcfb0b2ef8c8cdea6c56ce664a55f611aa877afae9190713117`.
- Identical raw fixture SHA-256: `175060a92bcf474476faab13ffd7b6978c2bcc57ce5328624d6a1468eda1cd5b`.
- Baseline runtime SHA-256: `b2ed5fd923de2056cfff69387815e9b049e9b0b8cb8eed9b54859db3247b3794`.
