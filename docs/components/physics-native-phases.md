# Native physics compiler and device phases

Last updated: 2026-10-15

## Scope and executable path

The [memory runner](../../tools/physics-memory/README.md#native-compiler-and-device-phases)
now runs five cold controls and the existing complete public GPU retirement
fixture in both numerical policies. The runtime implementation is unchanged.
The explicit test entry lives in `GPUPhysicsNativeMemoryTests`; `NativePhaseProbe`
collects nearby full-GC/glibc/procfs observations without CLRMD attachment. This
keeps compiler/reflection, device lifetime and complete numerical worlds distinct.
Test and public-fixture runtime DLL hashes must match before any profile starts.

Each profile starts in a fresh Linux x64/CoreCLR process, without display,
renderer or window, with the pinned lavapipe ICD and disabled Vulkan layers.
The controls reject missing/non-software ICD configuration before creating a
device, then check the actual diagnostic device identity. The collector rejects
NVIDIA mappings, requires software Vulkan during live worlds/retained-device
observations, and requires its absence in formats/reflection controls. The normal
process ABI/allocation and procfs-parser calibrations precede the matrix.

Formats initializes/enumerates/quits the compiler library once per cycle.
Reflection does the same for each of fourteen real embedded physics kernels,
freeing every returned metadata allocation, without a device. Device creates
and destroys the independent compute owner without pipelines. Pipelines retains
one device across repeated fourteen-kernel creation/release sets, then releases
the device. Device-pipelines recreates both each cycle. Pipeline controls wait
for GPU idle after release to permit deferred native retirement; this diagnostic
wait is neither a runtime change nor an accepted extra simulation fence.

The public profiles retain all 64 awake circles, one pin, one checkpoint, eight
full steps plus capture/restore and portable capture/apply per world. Even cycles
enter the original terminal numerical-error path, preserve selected GPU identity
and reject subsequent state reads/steps before disposal. These are complete
world intervals, unlike the five controls. They make no visual or speed claim.

## Thirty-two-cycle matrix

There are 303 process observations: 34 for each ordinary control, 35 for the
retained-device control and 66 for each full-world policy. All 64 public world
lifetimes and 32 terminal intervals pass their original assertions. Counts and
all byte scopes remain distinct from the earlier selected-graph measurements.
Control hosts and the public fixture include different host/JIT baselines;
subtracting their absolute totals would not identify exclusive native owners.

| Profile | RSS after cycle 1 | RSS after cycle 16 | RSS after cycle 32 | Arena after 32 | Used arena after 32 |
| --- | ---: | ---: | ---: | ---: | ---: |
| formats | 102,973,440 | 103,223,296 | 103,280,640 | 38,682,624 | 17,831,312 |
| reflection | 104,783,872 | 105,037,824 | 105,037,824 | 38,682,624 | 17,902,848 |
| device | 120,664,064 | 137,846,784 | 138,407,936 | 48,893,952 | 18,087,760 |
| pipelines | 249,430,016 | 253,857,792 | 253,857,792 | 155,684,864 | 30,552,560 |
| device-pipelines | 238,792,704 | 249,225,216 | 249,257,984 | 155,701,248 | 1,990,672 |
| world | 229,756,928 | 266,735,616 | 266,842,112 | 174,682,112 | 2,087,888 |
| world-threaded | 231,608,320 | 268,316,672 | 273,354,752 | 180,760,576 | 2,088,672 |

The retained-device rows still include that live device and seventeen threads;
its final release returns to seven threads and 242,782,208 RSS bytes. Each
retired device-pipelines/public-world sample has seven threads.
Device-only samples briefly show eight threads at cycles 2 and 5, then return
to seven at the next pause and final release; the extra thread is not attributed
to a native owner. Reflection and format enumeration do not reproduce the large
rise. Repeated pipeline sets on one retained device show much less growth than repeatedly creating the whole
device lifetime. This moves the next allocation-stack investigation toward
native device/driver setup and allocator reuse; it does not assign every byte
or establish a particular native leak.

SDL allocation counting is disabled in this native build: every control reports
`OutstandingSDLAllocations=-1`. It therefore supplies no live-allocation proof.
The counter's general meaning is a count, not bytes, and excludes allocations
made outside SDL. No compiler/device cache, retained process-wide GPU service,
allocator setting or physics algorithm is introduced on the strength of these
controls. Arena free pages can remain resident after resources are released.

## Longer public-world follow-up

A separate source-matched small host repeats the unchanged public retirement
routine four times in one process, 32 worlds per batch, for each numerical policy.
It preserves the routine's baseline and 250 ms settled pause at every batch;
worlds are still freshly created and disposed. The two processes execute 256
lifetimes and 128 terminal intervals, producing 528 process-only observations.
This extension is retained as a local experimental harness, separate from the
shipped mode's 32-cycle limit and without heap attachment or SDL live counts.

| Policy | RSS 32 | RSS 64 | RSS 128 | Arena 32 | Arena 128 | Used arena 128 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Owner | 266,481,664 | 267,706,368 | 267,776,000 | 174,559,232 | 174,559,232 | 2,007,360 |
| Separate | 274,300,928 | 275,800,064 | 275,947,520 | 181,383,168 | 181,567,488 | 2,011,744 |

All retired observations return to seven threads. From cycle 32 to 128, RSS
adds 1,294,336 bytes on owner and 1,646,592 bytes on the separate lane; owner
arena capacity stays fixed, separate arena capacity adds 184,320 bytes. Reported
used arena storage stays near two megabytes while most arena capacity is free.
The initial large threaded accumulation corrected by owner-side compilation is
not reproduced here. Later residency is much smaller in this bounded workload.
This supports retained allocator capacity as a major measured contributor, not
an unconditional memory ceiling, native ownership-stack attribution or a proof
that all small native allocations stop growing.

## Reproduction and remaining gates

```bash
python3 -B tools/physics-memory/run.py NEW_OUTPUT --native-phases \
  --bodies 64 --history 1 --cycles 32 --icd /absolute/path/to/lavapipe.json
```

`native-phases.json` preserves samples and assembly identities;
`native-phases.err` records completed phases. `environment.json` and calibration
reports pin the host and software driver. Current accepted artifacts are under
`bin/physics-native-phases/2026-10-15/accepted-final/`; the 128-world source/host
and reports are `host/`, `long-managed/`, `long-world.json` and
`long-threaded.json`. Earlier small-host prototypes remain separate. The first
integrated build's ambiguous path type was corrected before accepted execution.

The ordinary CPU/GPU heap fixtures, eight-cycle threaded retirement and
three-process CPU-server/software-GPU-client network regression also pass,
with 63 separate heap/process observations. Release runtime build, test/probe
format checks, compiled API coverage, wiki tests, generation and generation
check pass. Seven portable-PDB source sets match the exercised sources; runtime
production source and public API remain unchanged. Local `validation.json`
keeps these checks and source/binary identities separate from the phase totals.

These are cold/setup and retirement observations, not warmed allocation or
throughput brackets, continuous peaks, native GPU heap/VRAM totals, rendered
batch output or owner acceptance. No selected managed graph is measured in this
mode. The earlier heap-attached long-run stall remains a separate diagnostic
reliability issue; avoiding attachment does not claim to fix it. Ordinary heap/
retirement/network modes and their existing assertions remain required regressions.
Residual native allocation stacks, larger/different workloads, NativeAOT/foreign
memory, full physics/extension/inherited contracts and target GPU/window speed
remain open. RTX loading stays deferred while its power cable is unverified.
