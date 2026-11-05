# Same-revision dense physics measurements

The [2026-10-21 packed contact publication pair](gpu-resident-reports.md#frozen-monitored-scene-pair) measures the complete 16,384-body CPU/GPU scene on its pinned runtime, with unchanged contact/event population and lower selected-report traffic. Its results do not refresh the older revisions below.

Last updated: 2026-11-05

## Public API comparison after driver recovery (2026-11-05)

All physics timing in this series comes from the ordinary [public consumer](../../tests/PhysicsQueryPerformance.Consumer/README.md). It references only Electron2D, has no runtime friend access and uses no private reflection, backend objects, internal profiling controls or private counters. Full frames time `SceneTree.PhysicsFrame`; individual queries call the public `PhysicsDirectSpaceState` overloads. This supersedes internal-stage diagnostics as the method for the requested public-API comparison. The other physical method families remain unmeasured.

### Hardware and installation

Windows originally bound the installed PCI device to Microsoft `display.inf` / `BasicDisplay`. NVIDIA 621.14 WHQL was downloaded from its official server and both package and setup signatures verified. The package matches device `VEN_10DE&DEV_2203`. The first elevation request returned cancellation; after user confirmation the second ran. The active device is now NVIDIA GeForce RTX 3090 Ti, Windows driver `32.0.16.2114`, device problem code zero. `nvidia-smi` and direct SDL Direct3D 12/Vulkan probes identify that card. The GPU callback check passes 128 warm/128 measured frames at 0/0 managed bytes; its full process finishes in 22.074 seconds instead of the earlier software-device deadline.

The installer nevertheless exits `0x80070005` in `CDisplayDriverExtSite::AfterInstall` / `Registry.cpp:1602`. This late registry failure remains unresolved; it is not reported as a clean installer exit. The driver is active and the public physical checks below run without reboot. No registry ACLs, clocks, power limits or user applications were changed.

### Complete public scene frames

Both paths use the same frozen self-contained .NET 10.0.14 host, identical four-wall circle scenes, sixteen solver iterations, awake bodies, eight-contact caps, contact events and interpolation. Each process warms 1600 and measures 256 fixed 1/144-second frames. Both explicit backend checks, completed ticks, published/direct state, containment, penetration, energy and contact checks pass with zero owner/process managed bytes. No real window is rendered.

| Bodies | CPU mean / p95 / p99, ms | GPU mean / p95 / p99, ms | CPU/GPU mean ratio |
| ---: | --- | --- | ---: |
| 1536 | 10.040 / 11.849 / 14.130 | 13.580 / 15.937 / 16.607 | 0.739 |
| 16384 | 125.587 / 132.743 / 137.992 | 97.611 / 118.781 / 128.902 | 1.287 |

A ratio above one favors GPU. These are **single-process desktop diagnostics**, not isolated maximum-throughput or performance acceptance. The series ran GPU/CPU at 1536 and CPU/GPU at 16384; the host retained other applications. External observations cover each whole process, including setup, warmup and validation. Their `background_cores` field is system busy time minus child process CPU time and includes driver/system work; it does not isolate foreign application load. No private numerical-stage timing is part of this comparison.

### Individual public queries

Each unchanged static fixture runs one-collider hit and distant-miss controls for all nine declared overloads. The tables contain unadjusted mean microseconds/call. Percentiles in raw JSON describe batch means, not individual call tails; loop/delegate baselines are separate and not subtracted. GPU query returns include necessary synchronous completion and transfers. These cached, prepared queries do not measure cold or edited geometry.

#### 16 static bodies

| Public method | Hit CPU / GPU, us | Hit CPU/GPU | Miss CPU / GPU, us | Miss CPU/GPU |
| --- | --- | ---: | --- | ---: |
| IntersectRay | 2.150 / 110.693 | 0.019 | 2.181 / 113.883 | 0.019 |
| IntersectPoint (array) | 1.448 / 121.168 | 0.012 | 1.483 / 130.835 | 0.011 |
| IntersectPoint (span) | 1.475 / 135.048 | 0.011 | 1.431 / 129.500 | 0.011 |
| IntersectShape (array) | 2.781 / 141.664 | 0.020 | 2.727 / 133.472 | 0.020 |
| IntersectShape (span) | 2.775 / 138.226 | 0.020 | 3.051 / 140.853 | 0.022 |
| CastMotion | 3.924 / 130.303 | 0.030 | 4.346 / 132.120 | 0.033 |
| CollideShape (array) | 3.148 / 128.656 | 0.024 | 3.084 / 134.367 | 0.023 |
| CollideShape (span) | 3.100 / 125.410 | 0.025 | 3.012 / 125.653 | 0.024 |
| GetRestInfo | 3.623 / 136.420 | 0.027 | 2.722 / 135.705 | 0.020 |

#### 256 static bodies

| Public method | Hit CPU / GPU, us | Hit CPU/GPU | Miss CPU / GPU, us | Miss CPU/GPU |
| --- | --- | ---: | --- | ---: |
| IntersectRay | 44.130 / 115.246 | 0.383 | 35.984 / 117.926 | 0.305 |
| IntersectPoint (array) | 46.900 / 133.035 | 0.353 | 25.156 / 129.561 | 0.194 |
| IntersectPoint (span) | 54.178 / 142.160 | 0.381 | 24.502 / 134.214 | 0.183 |
| IntersectShape (array) | 51.378 / 148.604 | 0.346 | 39.009 / 130.898 | 0.298 |
| IntersectShape (span) | 47.344 / 142.549 | 0.332 | 41.853 / 134.413 | 0.311 |
| CastMotion | 65.748 / 137.982 | 0.476 | 56.294 / 135.730 | 0.415 |
| CollideShape (array) | 50.119 / 133.174 | 0.376 | 42.898 / 147.979 | 0.290 |
| CollideShape (span) | 43.913 / 131.425 | 0.334 | 43.015 / 139.121 | 0.309 |
| GetRestInfo | 45.186 / 138.625 | 0.326 | 42.133 / 143.706 | 0.293 |

#### 4096 static bodies

| Public method | Hit CPU / GPU, us | Hit CPU/GPU | Miss CPU / GPU, us | Miss CPU/GPU |
| --- | --- | ---: | --- | ---: |
| IntersectRay | 749.032 / 203.012 | 3.690 | 716.834 / 203.174 | 3.528 |
| IntersectPoint (array) | 556.475 / 239.320 | 2.325 | 547.394 / 210.958 | 2.595 |
| IntersectPoint (span) | 551.511 / 237.906 | 2.318 | 544.181 / 220.047 | 2.473 |
| IntersectShape (array) | 858.925 / 256.271 | 3.352 | 848.160 / 223.788 | 3.790 |
| IntersectShape (span) | 857.577 / 245.357 | 3.495 | 865.549 / 224.072 | 3.863 |
| CastMotion | 1155.617 / 241.124 | 4.793 | 1145.718 / 220.170 | 5.204 |
| CollideShape (array) | 983.351 / 219.920 | 4.471 | 959.637 / 227.208 | 4.224 |
| CollideShape (span) | 938.865 / 232.880 | 4.032 | 916.854 / 224.312 | 4.087 |
| GetRestInfo | 905.065 / 224.622 | 4.029 | 881.253 / 222.354 | 3.963 |

The comparison currently covers nine query overloads and the full public scene-frame call. It does not claim timing coverage of every physical API method, failure/lifecycle case, raw-server workload, network workload or window FPS. The preliminary method inventory is not a completion metric. Source/run identities, per-frame samples, both allocation scopes, external host observations and the 54 paired query rows are retained under `bin/physics-public-comparison/2026-11-05/`; driver evidence is under `bin/physics-driver/2026-11-05/`. The older CPU-only figures and private phase breakdown below retain their historical diagnostic scope.

## Current physical task gate (2026-11-05)

The [task-boundary correction](physics-contract-audit.md#physics-task-boundary-2026-11-05)
keeps whole-engine Object/script/memory rows in their owning audits. Their census
states do not replace physical acceptance. A headless Windows CLR 10.0.14 callback
check passes CPU owner/separate modes with strict 0/0 warmed managed bytes after
separating the unrelated GUI prerequisite. This is bounded functional evidence.

The initial GPU callback preflight emits an HLSL compilation warning but does not
finish within 90 seconds. Its child is terminated and no performance sample is
accepted. The subsequent [Windows probe](#windows-cpu-diagnostics-2026-11-05)
identifies a software device. Hardware GPU availability and startup/step progress
remain physical qualification prerequisites. Full-library memory accounting is
not the next step. Preflight receipts are in `bin/physics-task-boundary/2026-11-05/`.

## Windows CPU diagnostics (2026-11-05)

This earlier section predates the public-only measurement requirement. Its query fixture is public-only, but its complete-scene phase diagnostics use internal test controls and are not the accepted public comparison above. The complete per-method task remains open. At that point, a direct device probe using the same SDL binary selects `direct3d12`, `Microsoft Basic Render Driver`, driver `10.0.26100.9711`; it does not supply an RTX hardware result. Explicit Vulkan creation also fails with `SDL_HINT_GPU_DRIVER vulkan unsupported!`. No hardware GPU ratio or window FPS is available.

Both current measurement hosts are self-contained Windows x64 / .NET 10.0.14 at runtime source `8e5fa256a44e750e4efd11d9e2cef8d77157ceac`. Their runtime DLL SHA-256 is `90c96a13a18758a2be98427616ea2aef5e16cd65d91fe50b9bf297a1ea29d1fd`. Initial framework-dependent .NET 10.0.1 controls are retained separately and are not combined with this series. These are single-run diagnostics without quiet-host admission or process/owner CPU clocks on this platform; they do not establish performance acceptance.

### Complete monitored scene

Every case uses the existing complete scene fixture: four walls, 144 Hz, four substeps, sixteen solver sweeps, awake bodies, eight-contact caps, events and interpolation. Each fresh CPU process warms 1600 and measures 256 frames. Physical, publication and zero-managed-allocation checks pass. There is no rendered window in these cases.

| Bodies | CPU full step mean / p50 / p95 / p99 / max, ms | Owner / all-thread managed bytes | Hardware GPU / ratio |
| ---: | --- | ---: | --- |
| 1536 | 7.4851 / 7.2557 / 9.0300 / 9.5508 / 9.7950 | 0 / 0 | Unmeasured |
| 16384 | 115.2630 / 114.6777 / 121.2520 / 128.2468 / 133.7064 | 0 / 0 | Unmeasured |

The existing eight nonoverlapping profile intervals are below. The residual is full-frame work outside those intervals and timer boundary overhead; it is not silently assigned to the solver. Nested completion subphases remain in the raw JSON.

| Interval | CPU 1536 bodies, mean ms | CPU 16384 bodies, mean ms |
| --- | ---: | ---: |
| Authoring | 0.199240 | 6.127093 |
| World policy / fields | 0.000241 | 0.000793 |
| Body motion / callbacks | 0.459965 | 8.943962 |
| Solver | 4.921432 | 54.626888 |
| Result synchronization | 0.000353 | 0.001366 |
| Pose / contacts | 1.254339 | 30.469077 |
| Areas / views / statistics | 0.000651 | 0.001918 |
| Callbacks / events | 0.000818 | 0.001497 |
| Outside instrumented intervals | 0.648110 | 15.090452 |

### Direct-space query overloads

The [public-only query consumer](../../tests/PhysicsQueryPerformance.Consumer/README.md) measures all nine declared query overloads and rejects a missing or duplicate case against the compiled class. Each unchanged static-circle fixture executes a one-collider hit and a distant miss: 64 warm and 128 measured batches, 64 calls each. The table contains unadjusted mean microseconds per call, including necessary synchronous query completion and loop/delegate overhead. Raw percentiles describe batch means, not individual-call tails. The empty-loop baseline is reported separately and is not subtracted.

| Method | Scenario | CPU 16 bodies, us | CPU 256 bodies, us | CPU 4096 bodies, us |
| --- | --- | ---: | ---: | ---: |
| IntersectRay | Hit | 2.097 | 37.704 | 684.354 |
| IntersectPoint (array) | Hit | 1.377 | 28.441 | 529.930 |
| IntersectPoint (span) | Hit | 1.349 | 23.825 | 549.309 |
| IntersectShape (array) | Hit | 2.626 | 38.748 | 882.660 |
| IntersectShape (span) | Hit | 2.562 | 42.748 | 859.662 |
| CastMotion | Hit | 3.698 | 56.045 | 1058.567 |
| CollideShape (array) | Hit | 2.599 | 39.948 | 831.049 |
| CollideShape (span) | Hit | 2.515 | 40.030 | 852.938 |
| GetRestInfo | Hit | 2.547 | 40.639 | 875.124 |
| IntersectRay | Miss | 1.976 | 33.384 | 680.814 |
| IntersectPoint (array) | Miss | 1.354 | 23.500 | 531.508 |
| IntersectPoint (span) | Miss | 1.317 | 23.430 | 529.216 |
| IntersectShape (array) | Miss | 2.603 | 38.317 | 878.839 |
| IntersectShape (span) | Miss | 2.530 | 38.302 | 844.485 |
| CastMotion | Miss | 3.342 | 55.587 | 1139.435 |
| CollideShape (array) | Miss | 2.501 | 40.291 | 842.172 |
| CollideShape (span) | Miss | 2.490 | 39.613 | 835.375 |
| GetRestInfo | Miss | 2.438 | 40.436 | 865.195 |

Hit array overloads allocate 64 bytes/call for point/shape results and 40 bytes/call for collision points; the exercised span overloads allocate zero managed bytes. The counts are measured owner/process allocation traffic, not native or retained memory. No hardware GPU query case is accepted. Other physical API families, cold/edited-world queries, detailed numerical kernels, networking and real-window comparison still require measurements.

Reproduction, exact commands, source/binary identities and raw results are under `bin/physics-speed/2026-11-05/`: `run-cpu.py`, `series-net10.0.14.json`, `source-identity.json`, `device.json`, `queries-cpu-*-net10.0.14.json` and `scene-*-cpu-net10.0.14/CPU.json`. Hardware driver/host availability must be resolved before the paired GPU runs. Historical Linux/RTX measurements below keep their own revisions and cannot fill the missing Windows GPU cells.

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

## Comparison admission at 18d95cf44 (2026-10-29)

The frozen eight-case CPU/GPU headless/window comparison stopped at its initial
quiet-host gate after 600.451445 seconds and 277 observations. Background CPU
was .046510–14.487720 cores (median .870220); the longest stable sequence was
14 of the required 30 consecutive two-second samples. Foreign non-desktop GPU
contexts appeared in 32 observations; total GPU utilization reached 8%. The
original .25-core/5% limits and context exclusion were unchanged. No physical
case started, so this attempt supplies no new complete-step latency, GPU
advantage, window FPS, numerical or allocation result.

All 6,406 source inputs match `18d95cf44221cbf48c33b410a2da915567b70b20`;
the manifest records 96 frozen output files. Runtime/root SHA-256 are
`3b3f03f66203f97a7f3cfad716f528cad79bd01335ba36d2b9954cc0f803ab8a` and
`ea25d00868edb86bde29762a6e4f6f82b3a840b416b72042763cf51a641306aa`.
Method, source proof, terminal controller state and admission summary are under
`bin/physics-full-comparison/2026-10-29-18d95cf44/`. Later source changes and
[late-callback transfer checks](gpu-resident-reports.md#late-callback-demand-2026-10-29)
do not inherit performance qualification from this unstarted comparison.

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
