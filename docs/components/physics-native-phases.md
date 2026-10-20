# Native physics compiler and device phases

Last updated: 2026-10-20

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

## Allocation-stack follow-up, 2026-10-16

The runner's optional `--heaptrack-preload` now records native malloc-family
events in the same seven children, without attaching a managed heap reader.
The preload is explicit, restricted to native phases and hashed; raw traces,
their sizes/hashes and child stderr accompany the ordinary phase assertions.
Builds, calibration and the collector itself do not receive the interposer.
This is an instrumented cohort, separate from the ordinary residency tables.

Heaptrack 1.5.0 at `b54e92e88b0895f910a5b107f499d1155a086b80` was built
locally, without installation or an allocator replacement. Its CMake libunwind
probes needed `buf` instead of `&buf` for current GCC; configuration also used
`CMAKE_POLICY_VERSION_MINIMUM=3.5`. The retained source patch/build logs identify
these tooling changes. A native calibration keeps 1,048,576 B and frees
2,097,152 B. Both named call sites appear; the event stream reports three
allocations/two frees, exactly 3,219,456 B peak including the 73,728 B library
startup request, and 1,048,576 B still live at exit.

Four- and thirty-two-cycle cohorts pass all seven profiles: 354 nearby process
observations, 72 complete public-world lifetimes and 36 terminal intervals in
total. A further one-cycle default-mode regression passes 24 observations with
no preload/trace. Runtime DLLs match across the controls, worlds and both stack
cohorts. All GPU observations use the same pinned lavapipe library; no NVIDIA
mapping, renderer or window is involved.

These byte counts come from individual allocation/free events, independently
checked against Heaptrack's unrounded peak/end flamegraph sums. The peak is
simultaneous requested bytes across hooked allocations in the whole process.
"End" is still-live requests at trace shutdown, including host/runtime and
process-lifetime storage; it is not an engine leak verdict. Allocation-leaf
module means the recorded immediate allocation caller, not exclusive ownership.

| Profile | Whole-process hooked peak, 32 cycles | Whole-process hooked end, 32 cycles | lavapipe allocation-leaf end, 4 / 32 cycles |
| --- | ---: | ---: | ---: |
| formats | 40,028,156 | 18,038,143 | 0 / 0 |
| reflection | 40,028,174 | 18,034,669 | 0 / 0 |
| device | 47,288,360 | 18,208,638 | 1,024 / 8,192 |
| pipelines | 147,817,673 | 1,554,980 | 256 / 256 |
| device-pipelines | 147,813,971 | 1,439,610 | 1,024 / 8,192 |
| world | 153,956,792 | 1,606,978 | 1,024 / 8,192 |
| world-threaded | 153,969,718 | 1,612,784 | 1,024 / 8,192 |

At the owner-world global peak, 139,577,543 B have lavapipe somewhere in their
stack, including large paths through `VULKAN_CreateComputePipeline`. This is an
inclusive allocation-path group, not additional memory to add to the total.
The corresponding end group is 10,688 B; its direct lavapipe leaves contribute
8,192 B. Two distinct 128 B call paths, each with 32 outstanding requests,
contain `pthread_once` and the stripped driver frames. The four-cycle cohorts
have four requests per path; the single retained-device profile has one per
path despite creating 32 pipeline sets. Device-only control reproduces the
same scaling without pipeline/physics execution. The driver-only follow-up below
now identifies a driver static root and reproduces the residue independently.
Foreign-driver cleanup remains open. No process-wide device cache or global
shutdown/allocator change is introduced to hide the residual.

Current artifacts are `bin/physics-native-stacks/2026-10-16/`: `instrumented4/`,
`instrumented32/`, `calibration-analysis/`, `default-regression.json`,
`compiled-source-audit.json` and `validation.json`. Raw traces total 2,385,357,306 B
in the 32-cycle cohort; the matching interpreted gzip files, unmerged reports,
flamegraphs and local analysis sources remain available. Six portable-PDB source
sets match the exercised inputs, and Release/coverage/wiki and runner checks pass.

Traces change traffic, residency, threads and timings. They do not inventory
managed objects, arbitrary direct mappings, unhooked allocators, all driver
ownership or physical GPU memory, and do not measure an ordinary network client.
Phase pauses are not exact trace interval markers. Summed per-location maximums
are not a simultaneous process peak; the event-stream/global peak is used here.
The broader native/foreign-memory and target-hardware/window gates remain open.

## Driver-only lifetime attribution, 2026-10-16

[`test_icd_lifetime.c`](../../tools/physics-memory/test_icd_lifetime.c) isolates the
remaining driver allocation from Electron2D, SDL, CoreCLR and the system Vulkan
loader. It opens the explicit software ICD directly through its negotiated
interface, creates an instance, enumerates the single CPU/llvmpipe physical
device and destroys the instance. It creates no logical device, pipeline or
compute work. Every final close must remove the library from both the dynamic
linker and procfs mappings; forbidden modules reject the control.

The three modes either load/close without enumeration, reopen around every
instance, or retain one library across all instances and then close it. Each
four-/thirty-two-cycle cohort passes both ordinary and Heaptrack runs: twelve
fresh processes, 144 enumeration lifetimes and 72 load-only intervals. Ten
invalid CLI/file cases and the missing-C++ prerequisite reject before successful
execution. The final helper is compiled with warnings as errors.

| Mode | lavapipe allocation-leaf end, 4 cycles | Same, 32 cycles |
| --- | ---: | ---: |
| load | 0 | 0 |
| reopen | 512 | 4,096 |
| retain | 128 | 128 |

Every outstanding driver request is 128 B. All normalize to the same leaf at
module offset `0x17dafe`, also present in both SDL call routes of the earlier
complete-world profiles. Thus one direct ICD load/enumeration leaves one block;
the earlier backend setup traverses two load/enumeration routes. An additional
owned-process GDB observation stops at the actual `realloc` instruction
`0x17daf9`: its old pointer is null and requested size is 128. After the driver
store at `0x17d965`, the static pointer at `0xe35e40` equals that returned
allocation, and the adjacent topology data reports one L3 cache. The program
then destroys the instance, unmaps the driver and exits normally. No private
driver state is modified or freed by the control.

The pinned CachyOS package is `3:26.2.4-1`, ELF build ID
`e43c9e6ec5cd20265af2c18512f76a2b1cba6693`, with its existing SHA-256 unchanged.
It was built with `!debug`; there is no local debug section, and the attempted
debuginfod query returned HTTP 400. This is instruction/data attribution, not
successful DWARF symbolization. The corresponding pinned
[Mesa source](https://gitlab.freedesktop.org/mesa/mesa/-/blob/96cb43121031992b85767f9c1be8f3f48e22b1d2/src/util/u_cpu_detect.c#L638)
allocates `L3_affinity_masks` with `realloc` in `get_cpu_topology`, stores it in
static CPU capability data and initializes it through `_util_cpu_detect_once`.
Its header defines 1,024 CPU bits, hence a 128 B affinity mask per L3 cache.
This source identification is an inference corroborated by the exact call,
static data, host topology and independent native reproduction; it does not
claim symbols from the stripped binary or a repaired third-party driver.

The first plain-C prototype could not unload its ICD: dynamic-linker diagnostics
showed a reverse dependency from `libstdc++` binding to driver exports. The final
control links the standard C++ runtime first and rejects its absence. This
permits real unload/reload and preserves the relevant host-library order; it is
not a runtime device cache or an alternate engine policy.

Artifacts are `bin/physics-native-symbols/2026-10-16/`: `control-manifest.json`,
`controls/`, `normalized-leaves.json`, `driver-root-observation.json`, the GDB
script/log, pinned source copies, disassembly/relocations and `validation.json`.
The new control establishes an independently reproducible software-driver
residue and its static root. Its cleanup repair, other native/global ownership,
ordinary network memory, NativeAOT/foreign allocators and hardware/window
acceptance remain open; no physics or platform gate is promoted.

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

The ordinary, non-preloaded samples are cold/setup and retirement observations, not warmed allocation or
throughput brackets, continuous peaks, native GPU heap/VRAM totals, rendered
batch output or owner acceptance. No selected managed graph is measured in this
mode. The earlier heap-attached long-run stall remains a separate diagnostic
reliability issue; avoiding attachment does not claim to fix it. Ordinary heap/
retirement/network modes and their existing assertions remain required regressions.
Residual native source-level ownership, larger/different workloads, NativeAOT/foreign
memory, full physics/extension/inherited contracts and target GPU/window speed
remain open. RTX loading was deferred for these phase profiles. The later
authorized [hardware baseline](physics-rtx-current.md) records separate workloads
and leaves native source-level ownership acceptance open.
