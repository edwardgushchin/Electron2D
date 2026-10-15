# Repeated physics world retirement

Last updated: 2026-10-15

## Exercised lifetime

The [memory runner](../../tools/physics-memory/README.md#repeated-world-retirement)
now repeats complete raw-world creation, simulation, history and disposal in one
process through the public API. Linux x64/CoreCLR uses a denied Vulkan ICD for CPU
and the pinned lavapipe device for GPU, without fallback, renderer, window or
NVIDIA mapping. The runtime implementation is unchanged.

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

## Open native-growth finding

Default separate-lane software-GPU processes show large growth in resident free
glibc arena storage across retired worlds. The selected owners disappear and
process threads return to seven; reported used arena bytes fall rather than grow
with the large RSS increase. Serial CPU profiles settle after initial warmup.
This is an unresolved process-retention/performance issue, not evidence that a
particular engine wrapper or physical state mirror remains alive.

A fresh matched 64-body separate-lane control changes only the child process's
`MALLOC_ARENA_MAX` environment value to `1`. That control greatly reduces repeated
RSS/arena growth while preserving the lifetime/error assertions. The runner
records the inherited value and does not impose it. The [glibc arena tunable](https://sourceware.org/glibc/manual/2.35/html_node/Malloc-Tunable-Parameters.html)
supports interpreting this as allocator-arena retention; the exact allocation
sites and a production remedy remain unverified. No global allocator tuning or
runtime ownership change is shipped, and no speed benefit is inferred.

The initial fault candidate used a huge duration with zero velocities/gravity and
did not fail. Explicit gravity supplies the required unrepresentable numerical
input; the first candidate is excluded. The successful fault checks prove the
public terminal boundary and subsequent retirement, not hardware device loss.

## Remaining acceptance

The default threaded GPU growth stays open. Investigate cold pipeline/driver work
and fresh numerical/device thread lifetimes before choosing a remedy; verify any
change with repeated worlds, real public behavior and same-workload timing.
The one-arena control is diagnostic evidence, not the accepted application policy.
Longer runs, mixed scene/network retirement, registered backends, native ownership
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
