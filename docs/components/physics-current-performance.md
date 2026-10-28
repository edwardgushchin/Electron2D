# Same-revision dense physics measurements

The [2026-10-21 packed contact publication pair](gpu-resident-reports.md#frozen-monitored-scene-pair) measures the complete 16,384-body CPU/GPU scene on its pinned runtime, with unchanged contact/event population and lower selected-report traffic. Its results do not refresh the older revisions below.

Last updated: 2026-10-28

The [2026-10-27 containment correction](gpu-contact-colors.md#dense-retention-witness-2026-10-27)
closes the raw benchmark with four walls and adds CPU/GPU open-wall controls. The
former open-top scene could legitimately eject particles above its short side
walls, contradicting its own bounded-field assertion. Schema 3 marks the changed
workload; historical three-wall numbers and exploratory validation do not qualify
it. The quiet-host admission requirement and declaration states are unchanged.

## Current monitored-scene diagnostic

The [2026-10-28 six-process monitored-scene series](physics-scene-performance.md#monitored-scene-diagnostic-at-d54b90f03-2026-10-28)
records complete CPU/GPU steps, real-window FPS, managed allocation scopes and
host activity on a frozen d54b90f03 source cohort. All physical checks pass;
contention, varying tails and observed Enabled VSync keep performance acceptance
open. It does not qualify the separate raw-server-body pile below.

## Comparison admission at 6c47d64b1 (2026-10-26)

The new comparison at `6c47d64b18ae3b9746d553a93e461b1bceca2141` also stopped
before its first physical case. Across 601.523 seconds, all 275 observations
exceeded both original quiet limits: background CPU was .396–10.166 cores
(median .566), and total GPU utilization was 6–46% (median 26%). Zero of the
required 30 consecutive two-second quiet samples accumulated. A separate
reference-runtime GPU context appeared in 46 observations. No user application
was stopped, threshold changed or case retried.

This attempt reused the complete 16,384-body workload and eight-case order below.
Source closure verified all 6,115 manifest inputs against the selected commit;
only noncompiled coverage metadata differs from the accepted build source. The
runtime retains revision 0513b15cd metadata plus the publisher implementation;
the same immutable cohort passed the complete ordinary suite and 19 focused
profiles. Runtime/root SHA-256 are
`2b9384670460384a6e94cc8ce1f0ba0293143e6cfcde3fc7208abfc2ff53c478` and
`aa999e8e0ffaaba7c9ca281980ab708e67a0197bc090de8327b1c0d269942789`.
Eighty existing files were hardlinked with no new binary storage for admission.
The redundant links are retired after the terminal admission failure; the
qualified origin remains under `bin/event-publisher-membership/2026-10-26/`.

Evidence is under `bin/physics-full-comparison/2026-10-26-6c47d64b1/`:
`current-method.json`, `committed-source-proof.json`, `admission-summary.json`
and the complete isolation history. History SHA-256:
`fc3e6e2580e38e68dd0435571b416ea794308470117da4654e4b0e3abddce2e8`.
There are zero started/completed physical cases and no new latency, speed ratio,
window FPS or numerical results. Declaration coverage remains 1,335/1,369
(97.52%), with 34 open; complete performance acceptance remains open.

### Earlier admission at a8df21d9

The complete comparison prepared for `a8df21d9033ec655e7f65425fc629e27c154b613`
did not start a physical case. Its initial quiet-window admission expired after
601.389 seconds: all 275 observations exceeded the .25-core background CPU limit,
so zero of the required 30 consecutive two-second quiet samples accumulated.
Background CPU load ranged from .453 to 14.620 cores, with median .627; total GPU
utilization ranged from 2% to 92%, with median 20%. No foreign compute context was
observed. This is an admission failure; it supplies no current step latency,
CPU/GPU speed ratio, window FPS, numerical result or driver-failure evidence.

The unchanged workload was 16,384 awake monitored scene bodies, four walls,
144 Hz, four substeps and sixteen solver sweeps, contact handlers and eight-contact
caps, with interpolation enabled and sleep disabled. Each fresh process required
1600 warmup and 256 measured frames. The planned order was GPU/CPU/CPU/GPU
headless, followed by the same order in real Wayland windows. Both paths use the
same frozen runtime and complete physical features. Admission retains the .25-core
CPU and 5% total GPU limits with no foreign compute context; later cases require
three quiet observations. During each case, other-process average CPU activity
must remain within one core. No case was retried, no threshold was relaxed and no
user application was stopped.

Evidence is retained under `bin/physics-full-comparison/2026-10-25-a8df21d90/`:
`current-method.json`, `current-controller.json`,
`current-isolation-history.jsonl` and `admission-summary.json`. The 79-file frozen
payload and runner hashes still match their manifests. Its 3806 unique authored
sources were checked against the committed source; the accepted assembly retains
its precommit revision metadata, so source closure establishes the association.
Runtime/root SHA-256 are
`9a45903c85c1db55a5cd2890860290e247783033a27a2952a26b03ad81596f5a` and
`fba3efebd718de3f749a92d3cee38101bde8b0c33cd631ddc3770d0ca6723369`.
The frozen payload uses existing qualified hardlinks with zero new binary storage.
The admission history SHA-256 is
`405b95b5d4e6fadd50a28ff945a224f5f096e7e2e97851c5dc3a89f74d702055`.

Current complete headless/window comparison remains open until a host interval
passes the original admission and case-isolation criteria. A new attempt needs a
new evidence directory and a source-qualified cohort for its selected revision;
the terminal controller is not restarted automatically. Earlier measurements
below retain their own revisions and do not qualify these later completion-sharing
changes. Functional zero-allocation checks and reduced submission counts remain
separate evidence, as do full backend, platform and owner acceptance.

The [2026-10-20 RTX baseline](physics-rtx-current.md) repeats complete raw-server
and monitored-scene paths, real-window FPS and native allocation request traffic.
Its pinned runtime and fixed internal render size qualify the new measurements;
the older trials below retain their own revisions.

The [16,384-body monitored scene refresh](physics-scene-performance.md#16384-body-refresh-at-f3983555-2026-10-12)
uses an independent runtime copy at f3983555 for both headless and native-window
pairs. Its complete scene costs and provenance remain separate from the raw-server
workloads below; all historical numbers retain their measured revisions.

## Window checks at 567be307

The 16,384-body public server workload ran CPU then GPU in one process on source revision `567be307b407af89c65240e801b8c631526e7c21`, with 240 warmup and 240 measured ticks, fixed 1/60-second steps, four substeps, sixteen iterations and sleeping disabled. Both paths use the same 1152 x 800 GPU renderer and draw the entire population. MaxFPS is zero; Disabled presentation was requested and the actual mode was Enabled. The one-tick-per-frame catch-up policy is unchanged. All values below are measured desktop trials rather than cross-device acceptance.

| Backend | Full step mean / p50 / p95 / p99, ms | Frame mean / p50 / p95 / p99, ms | Actual FPS and ticks/s | Physics/frame owner and all-thread bytes |
| --- | --- | --- | ---: | --- |
| CPU | 57.0265 / 56.0603 / 63.1626 / 72.0673 | 63.1183 / 62.0856 / 69.4368 / 78.7018 | 15.8432 | 0 / 0 |
| GPU | 14.9283 / 14.8014 / 16.3052 / 16.7271 | 19.8826 / 19.6790 / 21.7174 / 22.3513 | 50.2952 | 0 / 0 |

GPU median complete physics is 3.79 times faster and measured window throughput 3.17 times higher in this workload. Neither path sustains the requested 60 physics ticks/s. This is a server-body instance-rendering workload; it must not be generalized to the monitored scene path. A separate 4096-body complete scene trial on the same source gave CPU/GPU medians 19.1687/18.8702 ms; that small difference is not a repeatable scene advantage.

The GPU physics interval uploaded/read 55,680/183,504 bytes over 240 ticks; explicit window pose demand raised total physics-store traffic to 62,972,160/63,099,984 bytes. The summed wait was 2,005.74 ms. The renderer's instance buffer carried 188,743,680 bytes separately. Publishing every visible body's pose is required by this host renderer; none of these figures is network payload. CPU/GPU maximum penetration was .227389/.224485 against .601 scene units, escape was zero and final energy .951503/.947617 of initial energy. Captures were inspected and show the complete contained pile.

Additional CPU-then-GPU sequences with newly allocated GPU buffers filled with NaN and finite float-one words both passed the same physical and memory gates. GPU penetration was .223490/.263825 and escape zero. The final fixture also reports `BufferInitialization` and `RequestedVSync`; its finite-pattern confirmation uses 64 measured ticks after the same 240 warmup and again passes, with CPU/GPU 16.6907/50.7532 FPS and .227389/.264062 penetration. The resident first-step regression now runs separate 16,384-body NaN and finite-pattern worlds. Diagnostic filling occurs during setup, outside warmed measurements.

Evidence is under `bin/physics-mass/2026-10-11/567be307-window-16384`, `567be307-window-poison-16384`, `finite-window-16384` and `finite-final-window-16384`; the scene trial is under `bin/physics-scene-pipeline/2026-10-11/567be307-4096`. These passes do not explain the earlier sporadic uninstrumented window escape, establish native allocation totals, cover other platforms or close full numerical-extension and final network/window acceptance.

## Earlier 65,536-body revision and workload

The existing [mass fixture](../../tests/Electron2D.Tests/PhysicsMassPerformance.cs)
ran the complete public CPU and independent GPU paths on local main at
`7bff847e8eb1cd46b48c613ed6f9079db705228c`. CPU then GPU execute sequentially in
each process. Both headless and native-window runs use the same compiled assemblies;
HEAD, tracked runtime/test source and assembly digests were checked before and after
the measurements. Runtime/test source matched that commit; only this task's
documentation was dirty. Fresh runtime/test builds reported that same commit in
their informational versions. That earlier report changed documentation only; the newer initialization checks above also extend the test fixture.

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
supersede it. Native allocation accounting, other devices/platforms,
human visual acceptance and the complete registered
backend/custom-algorithm contract remain open under
[ADR 0054](../decisions/physics-backends.md#adr-0054) and
[ADR 0103](../decisions/physics-extensions.md#adr-0103).
The goal requires measured GPU advantage and separately reported real-window FPS;
it does not impose a sustained 60 Hz threshold at this population.

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

The [complete scene measurements](physics-scene-performance.md) cover a separate CPU/GPU RigidBody/monitor/interpolation workload with zero warmed managed bytes. The earlier 1536-body headless GPU cost is higher and the original 4096-body medians are close. The [native monitored-scene window](physics-scene-performance.md#full-monitored-scene-in-a-native-window) now measures the complete scene, pose publication, idle processing and actual rendering separately; its fixed host and reported presentation/VSync settings qualify that evidence. The raw-body/window advantage above must not be generalized to this scene workload.
