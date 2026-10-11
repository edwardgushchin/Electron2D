# Same-revision dense physics measurements

Last updated: 2026-10-11

## Revision and workload

The existing [mass fixture](../../tests/Electron2D.Tests/PhysicsMassPerformance.cs)
ran the complete public CPU and independent GPU paths on local main at
`7bff847e8eb1cd46b48c613ed6f9079db705228c`. CPU then GPU execute sequentially in
each process. Both headless and native-window runs use the same compiled assemblies;
HEAD, tracked runtime/test source and assembly digests were checked before and after
the measurements. Runtime/test source matched that commit; only this task's
documentation was dirty. Fresh runtime/test builds reported that same commit in
their informational versions. This report changes documentation only.

| Input | Value |
| --- | --- |
| Host | Linux x64, AMD Ryzen 7 5700X, 16 logical processors |
| GPU | NVIDIA GeForce RTX 3090 Ti, Vulkan, driver 615.71.09 |
| SDK | .NET 10.0.101, Release |
| Population | 65,536 awake dynamic circles and three static walls |
| Geometry/material | Radius 4, mass 1, friction .3, bounce 0 |
| Motion | Gravity 980, linear/angular damping .05, sleeping disabled |
| Initial layout | Deterministic staggered dense pile, x/y spacing 8/7 |
| Step | Fixed 1/60 s, four substeps, 16 solver iterations |
| Sampling | 240 warmup ticks, 64 measured ticks, profiling enabled |
| Window | Real 1152 x 800 GPU-rendered window, all instances drawn |
| Frame policy | MaxFPS 0, one physics tick maximum per rendered frame |
| VSync | Disabled requested; actual Enabled on this desktop |

The tracked-source digest covers sorted tracked paths under `src/`,
`tests/Electron2D.Tests/` and `Electron2D.csproj`, hashing each path, a NUL, its
contents and a NUL. Recorded SHA-256 values are:

| Artifact | SHA-256 |
| --- | --- |
| Tracked runtime/test source | `3b385ad86fde565f4cfb0094c432b0b9594fde0025ec16a791a8eee565de3bf1` |
| Runtime DLL, including test-output copy | `cec6616449d8370512bf52b1c83c6adc166192fe9405c7167b1ab9b982751711` |
| Test executable DLL | `4f9cd9b03c96a5fd7d51734e4f89155ce2292ffcab5d67ef05cc47e3f81b0e51` |

## Complete physics and real frames

All times below are milliseconds. Headless measures complete `SpaceStep` without
render pose demand. Window physics measures that same complete step; pose and
instance publication then calls the public body-transform batch for all bodies.
Frame time spans actual FramePostDraw boundaries, including publication and
rendering. Actual FPS comes from rendered frame count over wall time, separately
from the physics latency distribution.

| Run | Backend | Step mean | Step p50 / p95 / p99 / max | Managed owner / all-thread bytes |
| --- | --- | ---: | --- | --- |
| Headless | CPU | 304.0303 | 276.6446 / 520.7795 / 651.7187 / 678.1936 | 0 / 0 |
| Headless | GPU | 38.7700 | 38.7552 / 40.1891 / 40.9807 / 42.3828 | 0 / 0 |
| Window | CPU | 265.4084 | 263.8018 / 277.1560 / 285.7795 / 289.1661 | 0 / 0 |
| Window | GPU | 49.0958 | 48.9548 / 50.7209 / 51.4831 / 51.7989 | 0 / 0 |

| Backend | Actual FPS / physics ticks per second | Frame mean | Frame p50 / p95 / p99 / max | Pose + instance mean | Pose + instance p50 / p95 / p99 / max | Frame managed owner / all-thread bytes |
| --- | ---: | ---: | --- | ---: | --- | --- |
| CPU | 3.4699 / 3.4699 | 288.1895 | 284.2957 / 298.0780 / 309.0654 / 431.8330 | 14.9558 | 14.8195 / 15.7003 / 16.0737 / 17.0529 | 0 / 0 |
| GPU | 15.6201 / 15.6201 | 64.0199 | 63.8871 / 65.5559 / 66.5239 / 66.7131 | 9.2925 | 9.2677 / 9.7290 / 10.1261 / 10.2218 | 0 / 0 |

GPU complete-step p50 is 7.14 times faster headless and 5.39 times faster with
the window. Actual window FPS is 4.50 times higher. Neither path sustains 60
physics ticks per second at this population. Fixed tick duration remains 1/60 s;
the configured catch-up budget allows simulation time to advance more slowly than
wall time. Each mode has one freshly built paired trial, not a statistical claim
across runs.
The headless CPU p99/max and window CPU frame maximum remain recorded as measured;
this trial does not isolate a cause for their larger tails.

Schema 2 records the following whole-step mean phases. GPU preparation subcounters
are separate nested scopes and must not be added to these phase totals.

| Phase | CPU headless | GPU headless | CPU window | GPU window |
| --- | ---: | ---: | ---: | ---: |
| Authoring | 2.4236 | 1.1754 | 2.6734 | 1.4934 |
| World policy/fields | 0.3458 | 2.9045 | 0.3829 | 3.1785 |
| Body motion/callback preparation | 16.1642 | 4.2227 | 15.0572 | 4.1403 |
| Solver | 282.8016 | 26.9999 | 245.1751 | 37.3659 |
| Result synchronization | 0.0014 | 1.4116 | 0.0013 | 1.1775 |
| Pose/contacts | 1.7337 | 1.6674 | 1.5918 | 1.3902 |
| Areas/views/statistics | 0.5582 | 0.3868 | 0.5246 | 0.3479 |
| Callbacks/events | 0.0009 | 0.0008 | 0.0007 | 0.0007 |

## Transfers and waits

Counters below are per measured tick. Physics-store counters exclude renderer
buffer traffic. The window adds an explicit all-body pose consumer: one compact
request and one result of `8 + 16N` bytes each, with one additional submission.
The absence of this consumer in the headless interval avoids a host pose mirror.

| GPU scope | Upload / readback bytes | Uniform bytes | Submissions | Mean summed wait, ms |
| --- | --- | ---: | ---: | ---: |
| Headless physics | 232 / 752 | 68,566.5 | 17 | 21.8749 |
| Window physics | 232 / 752 | 68,631.5 | 17 | 32.2995 |
| Complete window physics-store traffic | 1,048,816 / 1,049,336 | 68,663.5 | 18 | 32.6125 |

Headless physics wait p50/p95/p99/max is
21.8905/22.9464/23.8456/24.1516 ms; window physics wait is
32.2612/33.8431/34.1837/34.4848 ms. Wait counters sum individual waits and are
included in complete-step timing; they are not an extra cost to add to that timing.
The larger window wait has no isolated causal experiment in this trial.

Both render paths write a 3,145,728-byte MultiMesh instance buffer per tick
(12 floats for each circle); this reports submitted visual buffer volume rather
than measured driver bus traffic. Physics-store CPU counters are zero. Final
whole-population validation and PNG capture occur after time/allocation counters
stop; their diagnostic state reads and allocations are outside measured frames.

## Physical checks and acceptance limits

All four runs retained 65,536 active dynamic bodies and finite pose/linear-velocity
state. Every body was checked for escape and energy; circle neighbors were checked
for penetration. The penetration bound is twice the .3 configured contact
allowance plus .001 floating-point tolerance, or .601 scene units. Energy must be
finite and below 102% of initial energy; escape must be less than the radius and
maximum speed below 1000. These gates detect the earlier dense-pile collapse;
they do not prove identical trajectories or full physical correctness.

| Run | Backend | Max penetration | Max escape | Max speed | Energy / initial energy |
| --- | --- | ---: | ---: | ---: | ---: |
| Headless | CPU | 0.270310 | 0 | 0.622049 | 0.967770 |
| Headless | GPU | 0.299482 | 0 | 30.082760 | 0.956898 |
| Window | CPU | 0.270310 | 0 | 0.622049 | 0.967770 |
| Window | GPU | 0.299675 | 0 | 5.113276 | 0.957444 |

Both actual 1152 x 800 PNG captures were visually inspected and show the full dense
pile. The window fixture verifies the actual renderer and successful Engine.Run
before writing its report. Headless and window process exit statuses are recorded
alongside report validation; both returned 0.

This confirms a measurable complete-step and real-window advantage on this host
for this raw-server circle workload. Joint, CCD, callback, scene synchronization,
network replay and small-world performance retain their own checks. The older
4096-body GPU disadvantage remains recorded in the
[publication report](gpu-demand-publication.md); this larger workload does not
supersede it. Native allocation accounting, other devices/platforms, sustained
60 Hz at this population, human visual acceptance and the complete registered
backend/custom-algorithm contract remain open under
[ADR 0054](../decisions/physics-backends.md#adr-0054) and
[ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Reproduction and local evidence

The existing Release test executable ran with these exact invocations from the
repository root. Use the source build and native dependencies required by the
repository before reproducing them; the measured assemblies are identified above.

```bash
ELECTRON2D_TEST_PHYSICS_MASS=headless ELECTRON2D_MASS_COUNT=65536 ELECTRON2D_MASS_WARMUP=240 ELECTRON2D_MASS_SAMPLES=64 ELECTRON2D_MASS_BACKEND=both ELECTRON2D_MASS_OUTPUT=bin/physics-current-performance/2026-10-11/fresh-build/headless dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll > bin/physics-current-performance/2026-10-11/fresh-build/headless.log 2>&1
ELECTRON2D_TEST_PHYSICS_MASS=window ELECTRON2D_MASS_COUNT=65536 ELECTRON2D_MASS_WARMUP=240 ELECTRON2D_MASS_SAMPLES=64 ELECTRON2D_MASS_BACKEND=both ELECTRON2D_MASS_OUTPUT=bin/physics-current-performance/2026-10-11/fresh-build/window dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll > bin/physics-current-performance/2026-10-11/fresh-build/window.log 2>&1
```

Ignored local evidence stays in `bin/physics-current-performance/2026-10-11/fresh-build/`:
schema 2 CPU/GPU reports in `headless/` and `window/`, both window PNGs,
headless/window logs, `revision-before.json`, `revision-after.json`,
`headless-outcome.json`, `window-outcome.json` and `report-validation.json`.
Host inventory is in `cpu-info.json` and `gpu-before.csv` in the parent directory.
The initial trial in that parent directory used retained DLLs whose informational
version still named the parent commit; it was replaced by the freshly built trial
above rather than used to certify current-source performance. Generated evidence
is excluded from the engine commit; this page retains the measured contract,
values, provenance and limits.

The [complete scene measurements](physics-scene-performance.md) now cover a separate CPU/GPU RigidBody/monitor/interpolation workload with zero warmed managed bytes. Its 1536-body GPU cost is higher and its 4096-body medians are close; the raw-body/window advantage above does not establish a scene-wide performance claim.
