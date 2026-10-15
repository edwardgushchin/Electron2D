# Physics retained object measurement

Run from the repository root on Linux x64/glibc with .NET 10, a C compiler and the
normal source-native build prerequisites:

```bash
python3 -B tools/physics-memory/run.py bin/physics-memory-new
```

This builds a public-API fixture and a separate CLRMD probe. CPU runs with a missing
Vulkan ICD, no display and no renderer. To add the full resident GPU path on a
software device, supply an explicit lavapipe ICD:

```bash
python3 -B tools/physics-memory/run.py bin/physics-memory-new-software \
  --icd /absolute/path/to/lavapipe.json --bodies 1024 --history 4
```

The output directory must be new. Defaults are 1024 bodies and four checkpoints;
body counts must be multiples of four from 4 through 65536, histories 1 through 32.
The GPU path checks the selected implementation without startup fallback and
requires a mapped lavapipe library while the world is live; NVIDIA mappings reject.
No window is opened. Source-native build logs, per-type JSON reports, assembly
hashes and the software driver identity stay in the selected output directory.

The fixture creates raw circles in isolated pairs and one origin pin, with zero
gravity and sleeping disabled. It exercises full steps, private capture/restore
and portable capture/apply before each collection. Six phases cover a baseline
before world creation, the original world, added history/map/packet, a quarter-sized world after retirement and
recapture, disposed histories whose public wrappers remain held, and disposed
world. The portable packet deliberately survives to expose its peak capacity.

The parent launches and suspends only its own child using
[CLRMD attachment](https://github.com/microsoft/clrmd/blob/main/doc/GettingStarted.md).
It disposes each attachment before advancing the fixture and terminates its child
on failure. This needs the ordinary parent-to-child ptrace permission; it changes
no system ptrace configuration and writes no process dump. The package lock
applies only to the probe, never the engine or game consumers.
The runner gives each probe an owned process group and kills the group on timeout
or interruption, including a fixture left suspended by a stuck attachment.
`python3 -B tools/physics-memory/test_run.py` checks this with a nested child.

Each managed object is counted once by its CLR-reported `Size`. The probe cuts
other owner roots, attributes the world first, then map, packet, and each common
checkpoint/selected solver. It cuts both common and resident checkpoint registries;
source-world and backend-owner references are borrowed. It verifies the resulting
partition equals the unrestricted forward union of all owner roots. A known shared
65536-byte array and a separately kept-alive weak target check duplicate and weak
reference handling at every phase. JSON includes every type's count and bytes.

This is an explicit graph partition for this controlled fixture, not a dominator
analysis or total process retained size. Shared empty arrays/comparers go to the
first owner that reaches them. The fixture has no user callbacks or scene tree;
do not apply this ownership convention to arbitrary game graphs. Weak targets and
conditional dependent handles are not traversed. The fixture anchors and RID array,
static service tables, other static pools after world disposal, native
heaps, thread stacks, CLR bookkeeping, GPU pipelines, driver heaps and allocator
padding beyond CLR-reported sizes are outside the managed result. Requested SDL
device/transfer buffer bytes are reported separately and include private history;
they do not measure physical VRAM. CoreCLR heap sizes do not establish NativeAOT
sizes, allocations per step, hardware timing or window FPS.

`CPUWorldPool` separately reads the actual static CPU world-slot array without
initializing it in the target. It walks idle slots and their strong references,
counts active slot object shells without traversing their borrowed callbacks,
and reports per-type bytes. `AdditionalManagedObjectBytes` excludes objects already
in the selected owner graph; only that additional value can be added to the main
managed total. Idle slots retaining graph-color or solver-block arrays reject the
measurement. A GPU-only process can report an uninitialized pool with zero slots.
This covers one identified static root, not all engine or runtime globals.

Recorded scope and measurements: [retained physics memory](../../docs/components/physics-retained-memory.md).

## Process and allocator observations

Every pause also reports the latest full blocking GC's heap, fragmentation and
committed bytes, and the host glibc `mallinfo2` fields. Before suspending its child,
the parent reads `/proc/PID/status` and `smaps_rollup`, converting kernel kB fields
to bytes. RSS/PSS, virtual size, kernel-reported RSS high water, shared/private
pages and thread counts stay separate from the managed owner graph and requested
SDL buffers. These scopes overlap; do not add them or infer native memory by
subtracting the selected graph from RSS.

The runner first checks the native structure size/offsets against a small C
program built with the host headers. A separate managed calibration process
allocates, touches and frees a known 8 MiB native block; its allocator counters
must observe both transitions. That block never enters a measured game's peak.
The procfs self-test verifies units and rejects missing, invalid, duplicate and
overflowing fields. Calibration files, host/kernel identity and hashes remain
with each cohort. No allocation interposer or allocator trimming is enabled.

The GC/glibc sample precedes JSON serialization; procfs follows its publication.
They are nearby quiescent observations, not an atomic transaction. Status RSS/high
water are kernel estimates; rollup is a current page-table observation, not a
continuous peak trace. Allocator caches, other native allocators and direct maps
prevent treating these counters as exact per-world native ownership. See the
[process memory measurements](../../docs/components/physics-process-memory.md).

## Scene and network histories

The same runner can link the unmodified PhysicsNetwork example into a test host:

```bash
python3 -B tools/physics-memory/run.py bin/physics-network-memory-new --network \
  --client-backend gpu --icd /absolute/path/to/lavapipe.json --threaded
```

Use `--client-backend cpu` without an ICD for an entirely GPU-denied cohort;
omit `--threaded` for the owner numerical lane. This mode uses the existing full
900-tick check, late admission at 310, all 28 objects, adverse real ENet delivery
and every existing physical/identity/event/convergence/allocation assertion.
The driver option still permits only the explicit software device.

Add `--unpaced --client-poll-delay-ms 250` to measure the ordinary slow-client
session without check-only authority barriers. Poll delay is 0–250 ms and both
options require `--network`; invalid combinations reject before creating output.
The environment receipt records both values. For example:

```bash
python3 -B tools/physics-memory/run.py bin/physics-memory-slow-new --network \
  --client-backend cpu --unpaced --client-poll-delay-ms 250 --threaded
```

Each live/held heap report includes `PacketQueue`: the six backing-array types,
lengths and CLR-reported object bytes, wrapper size, and current/lifetime-maximum
queued packet and application-payload counts. The probe independently sums the
actual `_sizes` array and checks the counters, bounds and equal slot capacities.
The wrapper/array total is already inside the `delayed-packets` owner partition.
Queue payload is occupancy within that reservation; do not add it to object bytes.
These counts include each queued duplicate and exclude multiplayer/ENet/UDP
framing, native transport buffers and physical link traffic.


The probe launches the check parent and inspects only descendants of that process.
A test-only partial pauses each of the authority, primary client and late CPU
client before startup, after completion, with four newly captured local scene checkpoints (the
first restored), after disposal while wrappers are held, and after releasing all
references. These extra local points measure an additional application choice;
ordinary PhysicsNetwork uses portable correction plus input replay without them.
The four final hooks run after step measurements; all hooks are absent from the shipped example.
Their caller handle array and calibration anchors are excluded from the graph.

Atomic ready/go files coordinate each attachment. The probe counts the union of
the session and public checkpoint roots, with boundaries for every owned Node,
world resource, SceneTree, default/session multiplayer, ENet peer, impairment link,
snapshot/map, input/events/presentation, wire authoring and timing arrays, and
common/selected checkpoint state. Other owner links are borrowed; each group can
traverse its own roots. The partition must equal the unrestricted union. The
known-array/weak calibration runs at each pause. This convention is specific to
this example's real callbacks and scene, not arbitrary user scripts.

Held disposal requires disposed physics spaces and zero requested GPU buffers.
Final collection requires no remaining session/world, Nodes, Shapes, SceneTree,
World, PhysicsSpace, checkpoint/map, selected CPU/GPU owner, SceneMultiplayer,
ENetMultiplayerPeer or ENetConnection objects. JSON contains per-type byte counts,
array capacities and requested GPU bytes. Existing process-check reports remain
under `network/sessions/`, fifteen memory observations under `network/`, and
`environment.json` records assembly hashes. Pauses/collection perturb wall time;
these observations are not a fresh speed benchmark, traffic-queue high-water
sample, native ENet/driver heap total, NativeAOT size or window/FPS evidence.

Recorded results: [scene and network memory](../../docs/components/physics-network-memory.md).

### Network native allocation stacks

Use the explicit Heaptrack preload with the full network workflow:

```bash
python3 -B tools/physics-memory/run.py NEW_OUTPUT --network \
  --client-backend gpu --icd /absolute/path/to/lavapipe.json --threaded \
  --unpaced --client-poll-delay-ms 250 \
  --heaptrack-preload /absolute/path/to/libheaptrack_preload.so
```

The CPU authority, primary client and late CPU client each receive their own
`network/allocation-traces/ROLE.heaptrack.raw`. The collector and check launcher
remain unprofiled; their mappings are checked at the first gate, and every child
gate verifies the expected profiler/driver mappings. The fifteen full-GC/glibc/
procfs observations remain, along with all original network/physics assertions.
This instrumented mode skips CLRMD attachment: phase reports say
`HeapAttached=false` and `Measurement=null`, which means unmeasured managed owners,
not zero managed bytes. `network/collection.json` records the collection mode,
preload identity and each trace's role/PID/size/SHA-256.

The child-start partial has an implementation only in `NetworkFixture`; its
declaration and call are erased from the ordinary public example. Omitting the
preload retains the original fifteen graph/calibration/retirement inspections.
CPU-only and owner-lane variants use the same options described above.

Traces cover complete process lifetimes, including cold startup, real ordinary
network simulation and the extra four local checkpoint captures/restoration
after the session report. Their peaks do not isolate ordinary gameplay, warmed
steps or checkpoint-only intervals. Use the matching Heaptrack interpreter and
unmerged reports as described below. Keep hooked requested-byte peaks/end values,
module allocation paths, wire payload, selected managed graphs and RSS distinct.
The profiler changes timing and memory. [Recorded network native scope](../../docs/components/physics-network-memory.md#native-allocation-stack-cohort-2026-10-16)
retains the remaining ownership, phase-attribution and hardware gates.

## Repeated world retirement

Use `--cycles 8` with the raw fixture to create and retire eight independent worlds
in one process. `--threaded` selects their separate numerical lane. Body/history
counts keep their existing meaning. Each world solves the paired-circle/origin-pin
scene, captures/restores every history and publishes/applies a portable packet.
Even GPU cycles then force an unrepresentable gravity interval and verify terminal
step/read rejection with the selected GPU backend still reported.

The fixture destroys the source while checkpoints are held, then releases every
caller-owned RID/history/packet. Every live and retired state is measured, followed
by a 250 ms idle observation. Retired observations require no PhysicsSpace, selected
backend/store/device, Shape, public checkpoint, snapshot map or packet object.
The selected graph must be empty; the separate static CPU pool can remain.
These are bounded lifetime checks, not native allocation or throughput acceptance.

This mode also groups `/proc/PID/smaps` RSS by mapping name, preserving anonymous,
heap, file and runtime mappings. Grouping does not identify individual allocation
owners or stacks. The parser checks repeated/missing RSS, grouping and filenames
with spaces. Procfs reads remain nearby observations rather than an atomic snapshot.
`host.mallocArenaMax` records an inherited `MALLOC_ARENA_MAX` diagnostic control;
the runner does not set that value. [Results and the open native-growth issue](../../docs/components/physics-retirement-memory.md)
distinguish default behavior from the one-arena experiment.

## Native compiler and device phases

```bash
python3 -B tools/physics-memory/run.py NEW_OUTPUT --native-phases \
  --bodies 64 --history 1 --cycles 32 --icd /absolute/path/to/lavapipe.json
```

This process-only mode separates five cold controls from the existing complete
public retirement fixture in both numerical policies. It requires one explicit
existing lavapipe ICD and includes both policies, so it rejects `--network` and
`--threaded`. Cycles default to 32; body/history counts affect only the complete
worlds. The test executable and public fixture must contain the same runtime DLL.

Each control uses the explicit test entry `--physics-native-memory MODE CYCLES`:

- `formats`: compiler-library initialization, format enumeration and shutdown.
- `reflection`: the same lifetime around reflection of fourteen physics kernels,
  with every metadata allocation freed; no GPU device.
- `device`: create and dispose an independent compute device, without pipelines.
- `pipelines`: retain one device, repeatedly create/release those fourteen kernels,
  then release the retained device after the final cycle.
- `device-pipelines`: recreate the device and pipeline set each cycle.

Pipeline controls wait for device idle after release so pending native retirement
can progress. This extra wait belongs only to the diagnostic control; it is not
a runtime optimization, measured physics step or hardware/window benchmark.
The complete worlds retain the original simulation, checkpoint/portable replay
and even-cycle terminal numerical-failure assertions.

`native-phases.json` records each full-GC/glibc/procfs pause with per-mapping RSS,
driver observations and binary identities. Unlike heap-attached `--cycles`, it
never attaches CLRMD and makes no selected-object/collected-owner claim. The
control metadata also preserves SDL's outstanding allocation count; `-1` means
that native build disabled counting. This count is neither bytes nor all driver
allocations. Both application baselines include their own host/JIT state; do not
subtract different control totals to invent exclusive native-owner budgets.
Nearby samples and scoped allocator statistics remain overlapping observations,
not continuous peaks. No global allocator or compiler/device lifetime is changed.

[Recorded phase results and limits](../../docs/components/physics-native-phases.md)
keep these controls separate from complete-physics and target-hardware acceptance.

### Native allocation stacks

Pass an existing [Heaptrack](https://github.com/KDE/heaptrack) preload library
explicitly to record malloc-family events and native stacks in each of the seven
native-phase children:

```bash
python3 -B tools/physics-memory/run.py NEW_OUTPUT --native-phases \
  --bodies 64 --history 1 --cycles 4 --icd /absolute/path/to/lavapipe.json \
  --heaptrack-preload /absolute/path/to/libheaptrack_preload.so
```

The runner rejects the option outside native phases/network mode and rejects a missing library
before creating output. Only the fixture children receive the preload; the
collector, build and calibration processes keep their ordinary allocator path.
Each `MODE.heaptrack.raw` covers startup through normal process shutdown. The
report preserves its size/SHA-256, the preload SHA-256 and child stderr. Existing
phase/error/driver checks still run. Traces are raw and can be large; start with
four cycles and retain enough disk space before extending the cohort.

Interpret and analyze with the matching Heaptrack tools, for example:

```bash
set -o pipefail
/absolute/path/to/heaptrack_interpret < NEW_OUTPUT/world.heaptrack.raw \
  | gzip -c > NEW_OUTPUT/world.heaptrack.interpreted.gz
heaptrack_print NEW_OUTPUT/world.heaptrack.interpreted.gz \
  --print-leaks --merge-backtraces=0 \
  --disable-builtin-suppressions --disable-embedded-suppressions
```

Keep these instrumented cohorts separate from ordinary GC/glibc/RSS results and
timing benchmarks. A traced requested-byte peak includes hooked native allocations
from the host/runtime and libraries, plus trace-visible allocations reached from
physics; it does not establish managed GC heap, arbitrary direct-mapping or
unhooked-allocator totals. A stack identifies an allocation path, not exclusive ownership. The
tool's end-of-process "leaked" label includes process-lifetime storage and does
not by itself prove an engine leak. Phase pauses do not delimit exact intervals
inside the allocation trace. No allocator tuning or runtime cache is introduced.

### Driver-only unload/reload control

On Linux with Vulkan headers, a C compiler and the standard C++ runtime:

```bash
mkdir NEW_NATIVE_OUTPUT
cc -std=c11 -g -O0 -Wall -Wextra -Werror \
  tools/physics-memory/test_icd_lifetime.c -ldl \
  -Wl,--no-as-needed -lstdc++ -Wl,--as-needed \
  -o NEW_NATIVE_OUTPUT/icd-lifetime
NEW_NATIVE_OUTPUT/icd-lifetime /absolute/path/to/libvulkan_lvp.so reopen 32
```

This standalone control accepts `load`, `reopen` or `retain` and 1 through 32
cycles. `load` only opens/closes the ICD. The other modes create/destroy instances
and enumerate the CPU/llvmpipe physical device, either reopening the library each
time or keeping it until the end. They create no logical device or GPU work.
Calls use the [driver interface](https://github.com/KhronosGroup/Vulkan-Loader/blob/main/docs/LoaderDriverInterface.md#loader-and-driver-interface-negotiation)
directly; no renderer, engine, SDL or system Vulkan loader is involved. Forbidden
mappings and a driver still loaded after its final close reject the control.

The explicit C++ link order matters: without an already loaded standard C++
runtime, reverse relocations may pin the ICD and invalidate the unload experiment.
The helper rejects that missing prerequisite. This is a diagnostic host, with
no change to runtime library/device retention.

To trace the same control with the matching installed/local Heaptrack tools:

```bash
heaptrack --record-only -o NEW_NATIVE_OUTPUT/reopen \
  NEW_NATIVE_OUTPUT/icd-lifetime /absolute/path/to/libvulkan_lvp.so reopen 32
```

Repeat with `load`/`retain` and separate output prefixes. Interpret/report the
trace with the matching Heaptrack version. Compare requested allocation paths,
not timing or RSS from different hosts. The [driver-only findings](../../docs/components/physics-native-phases.md#driver-only-lifetime-attribution-2026-10-16)
record the current source/binary attribution and remaining foreign-memory gates.
