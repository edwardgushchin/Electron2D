# Complete scene physics measurements

Last updated: 2026-10-11

## Workload and provenance

[PhysicsPipelinePerformance](../../tests/Electron2D.Tests/PhysicsPipelinePerformance.cs) now measures the same complete public scene path on explicit CPU and GPU World instances. Every dynamic object is a RigidBody with a CollisionShape, ContactMonitor, an eight-point contact cap, real entry/exit handlers and SceneTree physics interpolation. PhysicsFrame advances exactly one 1/144-second tick; both implementations retain four substeps and sixteen iterations. Sleeping is disabled. There is no rendered window in this fixture, so these are physics-frame costs rather than FPS.

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

Resident solver checks also passed the 16,384-body dirty-scratch initialization, momentum/energy/material/ray/joint checks, changing 64/257-support fallback graph and 4096/65,536-body contact workloads. This does not establish GPU advantage over CPU: the full monitored scene still costs more on GPU, and a rendered-window comparison was not run for this change. The wider numerical-extension/network/platform acceptance remains open.

Ignored evidence is in `bin/physics-scene-pipeline/2026-10-11/adaptive-color-rounds`: `control`, `paired`, `repeat`, source diff/hash and solver logs. A prior trial overlapping rendering integration is diagnostic only and is excluded from this comparison.
