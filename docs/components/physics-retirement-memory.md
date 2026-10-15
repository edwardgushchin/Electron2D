# Repeated physics world retirement

Last updated: 2026-10-15

## Exercised lifetime

The [memory runner](../../tools/physics-memory/README.md#repeated-world-retirement)
now repeats complete raw-world creation, simulation, history and disposal in one
process through the public API. Linux x64/CoreCLR uses a denied Vulkan ICD for CPU
and the pinned lavapipe device for GPU, without fallback, renderer, window or
NVIDIA mapping. The initial cohorts left the runtime unchanged; the owner-side
pipeline correction and its follow-up measurements are recorded below.

Each world contains 64 or 1024 awake radius-2 circles in isolated overlapping pairs,
one origin pin, zero ordinary gravity and one or four independent checkpoints.
After initial solving, eight full steps also capture/restore every local point
and capture/apply the portable packet. The live state is measured before disposal.
Even GPU cycles enable explicit gravity on an unpinned body and enter an
unrepresentable-duration interval. The aggregate failure, preserved selected
backend and subsequent step/state-read rejection are exact requirements.
CPU cycles are ordinary; separate existing CPU injection tests cover its failed
interval lifetime, not process-memory behavior in this profile.

The source space is freed while histories are held. All caller-owned bodies,
joint, Shape, histories, map and packet are then released. A non-inlined helper
returns before collection so dead fixture locals cannot imitate engine retention.
The probe rejects any remaining space, selected backend/store/device, Shape,
public checkpoint/map or packet at retirement and after a final 250 ms idle pause.
The selected owner graph must be zero; the separately measured static CPU pool
still retains its fixed 631,832 bytes. Live owner partitions and calibration keep
the earlier [graph rules](physics-retained-memory.md).

The [process collector](physics-process-memory.md) also reads full smaps and groups
RSS by mapping name. File pages, `[heap]` and unnamed anonymous pages remain
distinct. Shared library RSS includes shared pages; mapping names do not identify
every native allocator or the stack that allocated a block. Rollup and per-mapping
reads happen sequentially before attachment, so their sums need not be identical.

## Initial native-growth finding

The initial default separate-lane software-GPU processes showed large growth in resident free
glibc arena storage across retired worlds. The selected owners disappear and
process threads return to seven; reported used arena bytes fall rather than grow
with the large RSS increase. Serial CPU profiles settle after initial warmup.
This exposed a process-retention/performance issue, not evidence that a
particular engine wrapper or physical state mirror remains alive.

A fresh matched 64-body separate-lane control changes only the child process's
`MALLOC_ARENA_MAX` environment value to `1`. That control greatly reduces repeated
RSS/arena growth while preserving the lifetime/error assertions. The runner
records the inherited value and does not impose it. The [glibc arena tunable](https://sourceware.org/glibc/manual/2.35/html_node/Malloc-Tunable-Parameters.html)
supports interpreting this as allocator-arena retention. These initial measurements
did not identify allocation sites or verify a remedy. The control shipped no global
allocator tuning or runtime ownership change, and established no speed benefit.

The initial fault candidate used a huge duration with zero velocities/gravity and
did not fail. Explicit gravity supplies the required unrepresentable numerical
input; the first candidate is excluded. The successful fault checks prove the
public terminal boundary and subsequent retirement, not hardware device loss.

## Remaining acceptance

Owner-side pipeline preparation below reduces the large default threaded GPU
growth. Residual growth and a complete native-memory ceiling stay open. Investigate
remaining driver/compiler/device lifetimes with native allocation stacks and
same-workload timing before claiming complete retention/performance acceptance.
The one-arena control is diagnostic evidence, not the accepted application policy.
Runs beyond 32 cycles, mixed scene/network retirement, registered backends, native ownership
stacks, continuous peaks, NativeAOT/foreign memory and target-hardware/window
acceptance remain open. Real RTX loading remains deferred while its cable is
unverified. No public API row or overall physics acceptance is promoted.

## Observed retired-process bytes

Each row compares retirement after cycle 1 and cycle 8 in one fresh process.
Used/free are glibc `Uordblks`/`Fordblks`; all numbers are bytes. Eight cycles
provide bounded evidence and do not establish an eventual peak or memory ceiling.

| Bodies / histories / policy | Backend | RSS 1 | RSS 8 | Used 8 | Free 8 |
| --- | --- | ---: | ---: | ---: | ---: |
| 64 / 1 / owner | CPU | 75,182,080 | 75,689,984 | 11,346,336 | 941,664 |
| 64 / 1 / owner | GPU | 225,087,488 | 254,128,128 | 1,943,616 | 155,719,616 |
| 64 / 1 / separate | CPU | 75,218,944 | 75,722,752 | 11,113,056 | 1,125,792 |
| 64 / 1 / separate | GPU | 222,109,696 | 650,395,648 | 1,918,320 | 556,743,312 |
| 1024 / 4 / owner | CPU | 118,919,168 | 119,300,096 | 1,393,328 | 11,296,080 |
| 1024 / 4 / owner | GPU | 234,741,760 | 267,243,520 | 1,940,000 | 156,177,888 |
| 1024 / 4 / separate | CPU | 119,263,232 | 119,664,640 | 1,404,192 | 11,502,304 |
| 1024 / 4 / separate | GPU | 235,118,592 | 713,113,600 | 1,919,936 | 607,900,736 |
| 64 / 1 / separate / matched default | CPU | 75,190,272 | 75,694,080 | 11,108,224 | 1,126,528 |
| 64 / 1 / separate / matched default | GPU | 225,759,232 | 631,279,616 | 1,924,704 | 537,465,248 |
| 64 / 1 / separate / arena1 | CPU | 75,137,024 | 75,640,832 | 11,224,240 | 203,600 |
| 64 / 1 / separate / arena1 | GPU | 223,764,480 | 243,486,720 | 2,024,384 | 144,366,656 |

The matched default 64-body GPU process grows from 225,759,232 to 631,279,616
RSS bytes. With the single arena control it grows from 223,764,480 to 243,486,720
bytes, with most of that change already present after cycle 2. Reported used
bytes at cycle 8 are 1,924,704 and 2,024,384 respectively. This strongly supports
free allocator storage, rather than a growing selected managed graph, as the main
measured difference. It does not select an engine remedy.

For the original separate-lane 64-body GPU cohort, unnamed anonymous mapping RSS
grows from 103,170,048 to 515,444,736 bytes while `[heap]` grows from 25,509,888
to 41,074,688. Total glibc arena bytes grow from 146,599,936 to 558,661,632.
This locates the residency in mapping categories; it does not name native
allocation stacks or prove that all anonymous pages belong to glibc.

## Verification and evidence

The four default cohorts cover both sizes/history depths and both numerical
policies. Together with the two matched controls they execute 96 world lifetimes
in twelve application children, including 24 terminal GPU intervals, and produce
216 calibrated heap/process observations. Every retirement has a zero selected
graph and no forbidden owner objects. The static CPU pool remains 631,832 bytes
after initialization. The final idle observation does not remove the native
retention. Cold setup and the eight per-world steps are not warmed-allocation
or throughput brackets.

A normal six-phase 64-body CPU/GPU smoke run and a full 900-tick, late-310
three-process CPU/separate network check verify the existing tool modes. Their
12 and 15 observations are separate from the retirement cohorts. The network
check preserves its original physical/identity/event/convergence and zero warmed
owner/all-thread step/replay assertions.

Run the following for the matching
CPU/software-GPU retirement cohort:

```bash
python3 -B tools/physics-memory/run.py NEW_OUTPUT --bodies 64 --history 1 \
  --cycles 8 --threaded --icd /absolute/path/to/lavapipe.json
```

Omit `--threaded` for owner-lane solving.
The control prepends `MALLOC_ARENA_MAX=1` to that command in its own process
environment only. CPU-only execution omits the ICD.

Local artifacts: `bin/physics-retirement-memory/2026-10-15/accepted-{64,1024}-{serial,threaded}/`,
`control-64-threaded-{default,arena1}/`, `normal-smoke/`, and `network-smoke/`.
Per-phase mappings, owner/type totals, calibration and environment identities
remain in JSON; every completed phase is also noted in the error-stream log.

- Fixture/Electron2D.dll SHA-256: `b7ee4937601f8b6f68f13a8e756d46810a14991b5ea4df61de484e31f95c221f`.
- Fixture/Fixture.dll SHA-256: `89233c24f3d3918729e9560893a6d00ca0677dbb0ecc5a7da47a739776678e05`.
- Probe/Probe.dll SHA-256: `068a659f5054fa32f750850d569982a8a137667f98ec86d6574bc39e07be8152`.

## Owner pipeline preparation

A scoped pre-allocation probe on `GPUPhysicsDevice.CreatePipeline` reproduced
13 of 14 initial world kernels being created on the separate numerical thread;
the serial control created all on owner. The world now calls
`PrepareSimulationPipelines` at the end of owner preparation, after authored
motion/joint/exception/report/wake work and before solver borrowing. Authored
feature counts prepare possible numerical handles without simulation, buffer
resizing, readback, submissions or waits. The existing per-store handles and
release paths remain unchanged. Cold preparation time belongs to
`GPUPrepareBodiesMS` and the complete step; warm calls reuse handles. No allocator
tunable, shared device/compiler cache or solver-thread pool is introduced.

`GPUPhysicsPipelineTests` executes empty and active selected GPU worlds, an
origin pin, first kinematic/CCD transitions, late field/report/debug enablement,
and a cold solver-compilation failure. Preparation failure leaves tick zero and
the same GPU world usable; retry succeeds without CPU replay. Started numerical
failures remain terminal in the retirement fixture. Both scheduling policies
create all 17 normal feature kernels on owner; including failed/retried setup,
each observes 26 creation attempts. Two 128-warm/128-measured full-step brackets
require exactly 0/0 owner/all-thread managed bytes and no new pipeline. The
managed and strict Linux x64 NativeAOT hosts pass the same tests.

### Matched eight-cycle follow-up

The unchanged public retirement workloads now run on the corrected runtime,
with default glibc policy and the same pinned software driver. All 64 complete
world lifetimes, including 16 terminal GPU intervals, pass the original heap
owner/error assertions. The 144 observations retain zero selected graph after
retirement; the idle CPU pool stays 631,832 bytes. Every retired GPU process has
seven threads. This is separate from warmed throughput/allocation evidence.

| Bodies / histories / policy | Backend | RSS 1 | RSS 8 | Used 8 | Free 8 |
| --- | --- | ---: | ---: | ---: | ---: |
| 64 / 1 / owner | CPU | 75,079,680 | 75,587,584 | 11,220,832 | 821,408 |
| 64 / 1 / owner | GPU | 234,475,520 | 263,356,416 | 1,929,296 | 165,220,272 |
| 64 / 1 / separate | CPU | 75,296,768 | 75,800,576 | 11,112,576 | 1,126,272 |
| 64 / 1 / separate | GPU | 236,118,016 | 271,499,264 | 1,931,472 | 172,676,912 |
| 1024 / 4 / owner | CPU | 119,480,320 | 119,345,152 | 1,395,184 | 11,302,416 |
| 1024 / 4 / owner | GPU | 244,441,088 | 277,102,592 | 1,930,688 | 165,739,072 |
| 1024 / 4 / separate | CPU | 119,181,312 | 119,566,336 | 1,392,128 | 11,489,792 |
| 1024 / 4 / separate | GPU | 246,231,040 | 284,495,872 | 1,938,032 | 172,694,928 |

Compared with the initial default cohorts, the large separate-lane growth is
substantially reduced. Retired RSS/free arenas still increase; selected wrapper
collection does not establish complete native release or an eventual ceiling.
The creation-thread evidence and matched change support cold compilation on
fresh numerical threads as a major contributor, without naming every native
allocation stack.

### Thirty-two-cycle process-only follow-up

A first 32-cycle heap-attached CPU run stalled in CLRMD attachment after cycle 18;
it was interrupted and its whole owned process group was reaped. It is excluded
from accepted measurements. Four fresh processes then ran the same 64-body/one-
history fixture for 32 cycles without debugger attachment. They preserve the
fixture's physical/error/disposal assertions and sample full-GC, glibc and procfs
at every pause, but make no heap-graph/owner-collection claim. This separates
diagnostic attachment behavior from application process retention.

| Policy | Backend | RSS 1 | RSS 8 | RSS 16 | RSS 32 | Arena 32 | Used 32 |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Owner | CPU | 70,197,248 | 70,582,272 | 70,578,176 | 70,578,176 | 12,054,528 | 11,229,376 |
| Owner | GPU | 229,765,120 | 258,465,792 | 261,693,440 | 261,914,624 | 169,893,888 | 1,953,952 |
| Separate | CPU | 70,418,432 | 70,795,264 | 70,795,264 | 70,795,264 | 12,242,944 | 11,127,120 |
| Separate | GPU | 231,550,976 | 271,581,184 | 271,757,312 | 272,797,696 | 180,142,080 | 1,966,576 |

These 128 lifetimes, 32 terminal GPU intervals and 264 process observations show
much smaller later growth in this bounded software profile. All retired thread
counts are seven. Residual allocator retention, heap-attached long-run
reliability, native allocation stacks, foreign/NativeAOT memory, real GPU/window
performance and the overall physics objective remain open.

Local evidence: `bin/physics-owner-pipelines/2026-10-15/`, including `red.log`,
`pipeline.log`, `native.log`, `accepted8-{64,1024}-{serial,threaded}/`,
`long-{cpu,gpu}-{serial,threaded}.json` and the rejected attachment log under
`accepted-64-serial/`. Source/PDB and validation reports identify the actual
compiled runtime and preserve distinct graph/process scopes.

### Functional, network and complete-step checks

Seven managed hosts pass pipeline ownership, both scheduling policies of the
shared CPU/GPU CCD/boundary/directed-ray suite, shared GPU shape/body/joint/world
checks, backend ownership, Area behavior and report-only contacts. The source-
matched NativeAOT ownership host also passes. Two complete three-process network
runs use a denied-GPU CPU server and a software-GPU primary client, in owner and
separate modes, through tick 900 with late join at 310. Original impairment,
correction/replay, identity/lifecycle/event and 0/0 warm step/replay assertions
remain unchanged. Their 30 heap/process observations are additional to the
retirement cohorts. These memory hosts do not instrument numerical thread identity.

A first 64-body monitored scene with only 128 warmup frames failed the CPU zero-
allocation gate at 4,120 owner/all-thread bytes in Solve. It is retained under
`timing-serial/` and is not a passing benchmark. The documented canonical 1,600-
frame warmup then runs 128 measured complete PhysicsFrames on the same runtime,
with all 64 awake bodies, four walls, contact monitoring, 16 solver iterations,
four substeps, interpolation and 144 Hz fixed input. All four physical and 0/0
managed-byte gates pass. This verifies that warmup profile; the shorter evolving
scene's allocation remains unaccepted, and no allowance or subtraction was added.

The subsequent [CPU pair-table preparation](physics-scene-performance.md#cpu-contact-pair-capacity-2026-10-15)
identifies and fixes that 4,120-byte allocation. The original rejected result and
the older 1,600-warmup table below remain historical evidence, not results from
the corrected runtime.

| Policy | Backend | Whole step p50 / p95 / p99 (ms) | Included wait p50 (ms) | Upload / readback / uniforms per tick (B) |
| --- | --- | --- | ---: | --- |
| Owner | CPU | 0.3619 / 0.5429 / 0.6093 | 0.0000 | 0 / 0 / 0 |
| Owner | GPU | 14.1081 / 15.4117 / 16.4305 | 13.4025 | 1304 / 47456 / 31216.25 |
| Separate | CPU | 0.3781 / 0.4430 / 0.4635 | 0.0000 | 0 / 0 / 0 |
| Separate | GPU | 13.6929 / 15.0062 / 15.3443 | 12.9807 | 1304 / 47456 / 31247.75 |

These are software-device measurements of a small scene, not target-hardware
advantage, real-window FPS or a before/after speed claim. Cold setup and owner
compilation remain inside complete setup/step accounting, not warmed samples.
No RTX device is loaded while its power cable remains unverified.

Release runtime build, both format checks, compiled API coverage, wiki generator
checks and production identity search pass. Seven source/PDB sets match all
Compile inputs: 2,221 runtime files in both profiles, 710 test files, two focused
AOT-host files, four raw-fixture files, 16 network files and four probe files,
with zero missing or mismatched inputs. The current source/validation manifests
remain ignored evidence rather than shipped engine artifacts. Coverage stays
Partial for separate numerical execution and the overall objective stays open.

- Matched runtime SHA-256: `b2ed5fd923de2056cfff69387815e9b049e9b0b8cb8eed9b54859db3247b3794`.
- Raw fixture SHA-256: `46fa3d0b4790353fbc8aa5e8ac67b4f05f43a993249945c3094a5c92dde29408`.
- Network fixture SHA-256: `71fbe4e9b60c3ab38d277d1bb75f2caa4d3296d3ef9036ff50f1d4a20dfd5396`.
- Probe SHA-256: `9d86a2937f3ac376b4b35cadd7eb15d6fb60319910f63d0a7be9a96d35deb787`.
