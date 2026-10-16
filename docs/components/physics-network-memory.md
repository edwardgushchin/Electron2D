# Scene, network and local history memory

Last updated: 2026-10-16

The [prepared handoff check](physics-network-example.md#prepared-future-input-at-control-handoff-2026-10-16) adds one tick marker and three integer counters per session, plus three report counters. They add bounded metadata without changing packet/history capacities or application wire messages. Earlier measured cohort totals retain their recorded source revision; modified session/report object sizes have not been remeasured by those cohorts.

The following original tables retain their 2026-10-15 source/candidate scope.
[The current slow-client update](#current-slow-client-queue-and-history-budget-2026-10-16)
remeasures the application after the ordering and sleep/wake verification changes.

## Exercised contract and scope

[The memory runner](../../tools/physics-memory/README.md#scene-and-network-histories)
now links the unchanged [PhysicsNetwork](physics-network-example.md) sources into
a test-only host. Four full checks use CPU authority, an early CPU or lavapipe GPU
client, and a late CPU client, in serial and separate numerical modes. Each runs
900 authority ticks, late admission at 310, all 28 final objects, six joint recipes,
five nontrivial geometry families and actual ENet loopback with application loss,
delay, duplication and reordering. The original process check retains its complete
identity/lifecycle/contact/sleep/ownership/event/interpolation/smoothing/divergence,
final convergence and zero warmed owner/all-thread step/replay allocation assertions.
Every CPU child verifies GPU startup is unavailable and creates no graphics service.
GPU children use the explicit software ICD, without fallback, window or NVIDIA mapping.

A test-only report partial pauses each finished session for four observations:

1. `completed`: the real example after its final state/event acknowledgements.
2. `checkpointed`: four additional public local scene checkpoints, with the first
   restored; ordinary PhysicsNetwork does not retain these points.
3. `disposed-held`: the session/world have been disposed while the session and
   public checkpoint wrappers remain held.
4. `released`: all session/history references released and collected.

The hooks run after gameplay allocation measurements. Both collection passes drain
finalizers before the ready file is published atomically. The CLRMD parent verifies
that every PID descends from its launched check before suspending it. Ready/go files
release the process after inspection; the check still validates all final reports.
There are 12 actual network children and 48 heap observations. The updated common
walker also passes the raw CPU/GPU 64-body five-phase smoke profiles.

Object bytes are an explicit deduplicated forward graph partition of the session
plus the four public checkpoint roots. Boundaries separate physical storage, world
resource, tree, all owned Nodes, map/payload, default/session multiplayer, ENet peer,
impairment queue, input/events/presentation, wire authoring/timing arrays and each
common/selected checkpoint. Borrowed callback/owner references stop at the other
group. Shared referents belong to the first group that reaches them. The partition
must equal the unrestricted union; known shared-array/weak-target calibration runs
at every pause. Calibration objects, the test-only caller handle array, static/global
pools, dependent handles, native heaps, thread stacks, allocator padding beyond
CLR-reported Size and driver/runtime bookkeeping are outside the graph. This
convention is specific to the example, including its real callbacks; it is not a
retained-size/dominator analysis or a complete process memory budget.

## Managed object bytes

| Primary / numerical lane | Process | Completed | With four local points | Disposed, held |
| --- | --- | ---: | ---: | ---: |
| cpu / serial | server | 6,652,312 | 7,007,096 | 5,496,300 |
| cpu / serial | cpu | 6,619,448 | 6,967,312 | 5,496,252 |
| cpu / serial | late | 6,622,794 | 6,970,498 | 5,499,476 |
| cpu / threaded | server | 6,652,648 | 7,007,432 | 5,496,636 |
| cpu / threaded | cpu | 6,619,784 | 6,967,536 | 5,496,588 |
| cpu / threaded | late | 6,623,370 | 6,971,362 | 5,499,812 |
| gpu / serial | server | 6,652,312 | 7,013,032 | 5,496,300 |
| gpu / serial | gpu | 5,450,912 | 5,841,328 | 5,108,552 |
| gpu / serial | late | 6,623,154 | 6,976,794 | 5,499,796 |
| gpu / threaded | server | 6,652,648 | 7,013,368 | 5,496,636 |
| gpu / threaded | gpu | 5,451,248 | 5,841,664 | 5,108,888 |
| gpu / threaded | late | 6,623,490 | 6,977,130 | 5,500,132 |

The GPU/separate profile's completed graph partitions as follows. These categories
include their directly owned wrappers and backing arrays; they exclude objects
already attributed to an earlier category.

| Owner | CPU authority | GPU client | Late CPU client |
| --- | ---: | ---: | ---: |
| physics | 1,381,112 | 215,360 | 1,381,684 |
| world-resource | 616 | 616 | 616 |
| scene-tree | 6,672 | 6,648 | 6,672 |
| scene-nodes | 206,642 | 205,370 | 206,592 |
| snapshot-map | 68,684 | 67,436 | 68,404 |
| snapshot | 14,256 | 14,304 | 18,008 |
| scene-default-multiplayer | 1,478 | 1,478 | 1,478 |
| network-routing | 100,264 | 100,264 | 100,264 |
| enet-peer | 199,628 | 166,572 | 166,572 |
| delayed-packets | 4,197,096 | 4,197,096 | 4,197,096 |
| input-history | 262,168 | 262,168 | 262,168 |
| event-history | 36,936 | 36,936 | 36,936 |
| presentation | 15,104 | 15,104 | 15,104 |
| wire-authoring | 91,728 | 91,728 | 91,728 |
| timing-samples | 65,584 | 65,584 | 65,584 |
| simulation-residual | 3,480 | 3,480 | 3,480 |
| session-residual | 1,200 | 1,104 | 1,104 |

The fixed impairment queue alone owns 4,197,096 B per process: its 128 slots reserve
4,194,304 packet bytes plus array/object metadata and due/size/peer/mode tables.
This is the configured worst-case storage capacity, not observed in-flight traffic.
Input commands reserve 256 ticks × 32 slots: 262,144 payload bytes, 262,168 B with
array metadata. Three 256-entry event arrays total 36,936 B. Presentation storage
is 15,104 B; the two 4096-entry timing arrays total 65,584 B. Wire/authoring arrays
retain 91,728 B independently of current packet sizes. These are measured managed
capacities; native ENet packet/channel queues are not included.

Final encoded physics payloads are 8,720 B in the all-CPU cohorts and 8,836 B in
the GPU-client cohorts. Their peak snapshot byte-array capacities are larger:
14,192 B on the authority, 14,240 B on primary clients and 17,624/17,944 B on late
clients. Snapshot wrapper plus array metadata add 64 B. Different packet sizes
reflect the recorded physical histories, not fewer objects or a changed wire schema.

For the GPU/separate profile, four common scene points own 188,752 B total for
each process. The four selected solver copies add:

| Process | Selected managed solver bytes | Requested device history bytes |
| --- | ---: | ---: |
| server | 171,912 | 0 |
| gpu | 198,576 | 66,720 |
| late | 164,832 | 0 |

The local points include actual scene observer/contact/configuration state and
private selected numeric storage. The CPU private copy is smaller than the GPU
managed copy in this small scene; the raw large-world results retain their separate
population/capacity scope. This sample is not a universal backend memory ranking.

## Disposal and requested GPU resources

There are 52 owned Nodes while live, including shape children. Holding disposed
session/actor wrappers retains 29 disposed Nodes, empty reusable map/checkpoint
arrays, the impairment queue and command/event/presentation buffers. A disposed
PhysicsSpace remains reachable through those held wrappers, with its disposed
flag checked; native/device release does not imply collection of caller-held C#
objects. The managed bytes above deliberately preserve this distinction.

In both GPU profiles the store requests 159,576 device bytes and 40,760 transfer
bytes at completion, with a historical combined peak of 208,400 B. Adding the four
points raises device bytes to 226,296 B and the combined peak to 267,056 B; transfer
capacity stays 40,760 B. Held disposal requires both current counters to be zero.
These SDL reservations are not physical VRAM or native driver heaps; lavapipe may
back them in host RAM.

Final collection checks zero remaining NetworkSession/NetworkWorld, Nodes, Shapes,
SceneTree, World, PhysicsSpace, public checkpoint/map, CPU world backend, GPU store,
SceneMultiplayer, ENetMultiplayerPeer and ENetConnection objects in each child.
All 12 children pass. The selected session graph is gone; this does not claim zero
process memory or deletion of reusable static vendor/runtime pools.

## Evidence and remaining work

Artifacts are under `bin/physics-scene-network-memory/2026-10-15/`.
`accepted-{cpu,gpu}-{serial,threaded}/network/` holds all phase JSON and original
`sessions/` reports; the process check log contains its full-contract PASS. Per-type
counts, array capacities, calibration, driver observations and child PIDs remain
in JSON. `checks.json`, `compiled-source-audit.json` and `validation.json` record
checks and source/artifact identity; candidate compiler/phase experiments remain
separate. Runtime and shipped example sources/public API are unchanged.

All four cohorts use the same three assemblies (SHA-256):

- Probe/Probe.dll: `d1814cee57e4745d9eebb6b98cb19219569d9d46533dd53a4fba33c8420a55d5`.
- NetworkFixture/NetworkFixture.dll: `700d2874b12f295fdd7accd13963fa1fbd203c46c5179ff0d7f1b548f466728b`.
- NetworkFixture/Electron2D.dll: `d86ee55e99e6385cfad87601713d1ee49cf879177b801f6392e025624a4437a4`.

The original 1024/4096-body [raw budget](physics-retained-memory.md) remains scoped
to its recorded candidate. Current development-tool refactoring shares calibration
and traversal; it does not change that engine implementation or rewrite old reports.
The historical 16-slot network command budget is distinguished from today's 32 slots.

The subsequent [static CPU world-pool audit](physics-retained-memory.md#static-cpu-world-pool)
repeats CPU/GPU cohorts with separate scheduling selected and 24 observations on the fixed retirement
candidate and measures the idle pool independently. The historical tables and
assembly hashes above retain their own candidate scope.

Next work remains other static/global roots and native/driver allocations, complete
process peaks and transport high-water traffic. NativeAOT sizes, wider workloads,
foreign platforms, full physics/API acceptance, hardware GPU advantage and real
window FPS remain open. Collection pauses perturb elapsed session time, so this
profile is not a new speed/FPS benchmark. Real RTX loading remains deferred while
the power cable is unverified. No declaration state or architectural decision is
promoted by these bounded measurements.

[Process-wide memory observations](physics-process-memory.md) subsequently add a
pre-startup baseline to each child and record GC, glibc and procfs alongside the
owned graph. Each new cohort has fifteen observations. Existing tables above
retain their four-phase/candidate scope; process counters are separate overlapping
measurements rather than additional disjoint owners.

## Current slow-client queue and history budget, 2026-10-16

Four frozen-source ordinary CPU/GPU cohorts now use both numerical policies,
900 authority ticks, late admission after tick 310 and 250 ms first-client poll
delay, without check-progress traffic or authority phase/input waits. All retain
28 final objects, physical/contact/joint/geometry, actual reordered-state,
sleep/wake restoration, input trust, event identity and .002 convergence gates.
Every measured warm physical ordinary/replay interval is 0/0 owner/all-thread
managed bytes. Collection pauses perturb the session clock and packet schedule;
these measurements do not replace the unpaused performance record.

The collector takes five phases per selected process, including baseline and
release: 12 children and 60 heap observations. Calibration, exact owner-union
partitioning, own-descendant attachment, disposed-space/zero-requested-GPU checks
all pass. Every released observation has zero selected scene/network/history
owners; the known static CPU pool is reported separately and remains excluded
from this table.

| Primary / lane | Process | Completed graph B | With four local points B | Disposed, held graph B |
| --- | --- | ---: | ---: | ---: |
| cpu-serial | server | 6,656,096 | 7,025,680 | 5,051,492 |
| cpu-serial | cpu | 6,625,616 | 6,986,440 | 5,053,628 |
| cpu-serial | late | 6,627,162 | 6,988,194 | 5,054,972 |
| cpu-threaded | server | 6,656,672 | 7,026,080 | 5,051,828 |
| cpu-threaded | cpu | 6,625,912 | 6,986,656 | 5,053,964 |
| cpu-threaded | late | 6,627,498 | 6,988,354 | 5,055,308 |
| gpu-serial | server | 6,656,096 | 7,025,680 | 5,051,492 |
| gpu-serial | gpu | 5,455,088 | 5,845,504 | 5,112,360 |
| gpu-serial | late | 6,627,162 | 6,988,194 | 5,054,972 |
| gpu-threaded | server | 6,656,432 | 7,025,840 | 5,051,828 |
| gpu-threaded | gpu | 5,455,424 | 5,845,840 | 5,112,696 |
| gpu-threaded | late | 6,627,258 | 6,988,114 | 5,055,308 |

The queue owner is 4,198,184 CLR-reported bytes in every live/held process: a
152-byte wrapper plus 4,198,032 bytes for six arrays. The byte pool contains
4,194,304 payload bytes (4,194,328 object bytes), `_due` and `_order` each contain
128 values (1,048 object bytes each), and `_sizes`, `_peers` and `_modes` each
contain 128 values (536 object bytes each). The historical queue owner was
4,197,096 bytes; the measured 1,088-byte difference is the new 1,048-byte order
array and 40 more wrapper bytes, including the new queue accounting fields.
Other graph differences also include application/engine state and are not
attributed to those fields alone.

The current lifetime payload maxima are 6 packets / 64,828 B for every authority,
2 / 1,080 B for primary CPU clients, 2 / 888 B for GPU clients and 5 / 2,700 B
for late CPU clients. Packet-count and byte maxima are independent observations.
The CLRMD pause independently reads `_sizes`, verifies current occupancy against
counters and compares all four queue values with the original completed report.
Duplication contributes each copy; flush and peer retirement release current
occupancy without resetting the lifetime maxima. Startup controls cover drain,
drop, repeated peer retirement and preservation of another peer's payload.
These are application bytes before framing, not ENet/native/kernel/network peaks.

In the GPU/separate cohort, four common checkpoint roots own 188,752 managed B
for each process. Their selected solver partitions own 180,600 B on the CPU
server, 198,576 B on the GPU client and 172,048 B on the late CPU client. These
partitions are already in the checkpointed totals. Total graph growth may also
include scratch retained by capture/restore. Requested GPU device/transfer storage
is 159,576/40,760 B at completion and 225,656/40,760 B with four points; recorded
peak is 266,416 B. Held disposal reports zero requested device/transfer bytes.
These are reservations and managed objects, not physical VRAM or native ownership.

The same current report/queue controls also pass one public NativeAOT GPU/separate
paced cohort. Its four observed processes execute the same native ELF without
CoreCLR. This establishes native report/control behavior; the heap tables above
are CoreCLR sizes and do not claim NativeAOT layout equivalence.

Artifacts, all 60 phase reports, per-type partitions, queue inventories, calibrated
process observations and original session assertions remain under
bin/physics-network-memory-current/2026-10-16. `validation.json` records the exact
source/binary identities, four cohorts and native smoke. Source/PDB matches cover
2,223 runtime, 16 measurement-host, five probe and 713 executable-test inputs.
Release/API/wiki checks pass; the engine snapshot and coverage states are unchanged.
Complete static/global/native/driver ownership, peaks beyond these scoped counters,
NativeAOT object sizes, arbitrary workloads, foreign platforms, target-device
advantage and window FPS remain open. The RTX power-cable prerequisite remains.

## Native allocation-stack cohort, 2026-10-16

The [runner](../../tools/physics-memory/README.md#network-native-allocation-stacks)
now supports an explicit Heaptrack preload in network mode. Its implementation
lives in the measurement host's child-start partial; the corresponding hook is
absent from the public consumer's compiled method definitions/references.
Only the authority and two clients are interposed. The collector/check launcher
are checked unprofiled at the first gate, and all fifteen child observations
check their expected profiler and software/denied-driver mappings.

Four new cohorts cover CPU/GPU primary clients and owner/separate numerical lanes,
with ordinary unpaced authority, 250 ms primary polling, late admission requested
at tick 310 and 900 authority ticks. All twelve children pass the unchanged
physical, geometry, joint, support, portable identity, actual packet disorder,
ownership, sleep/wake correction, exact confirmed-event and final .002 convergence
checks. Warm authority steps and each client's predicted/replayed steps retain
0/0 owner/all-thread managed allocation. All 60 full-GC/glibc/procfs phases complete. This
mode intentionally skips CLRMD: `Measurement=null` and `HeapAttached=false`
make selected managed graphs unmeasured rather than empty.

Each trace runs from process startup through shutdown, including the four
additional public scene checkpoints and first restore after the session report.
The figures below are simultaneous requested-byte peaks across traced hooked
allocations in each process, followed by still-live requests at trace shutdown.
They include host/runtime and native libraries, exclude untraced/direct-mapping
and profiler-internal allocations, and do not prove an engine leak. They cannot
be summed with GC, RSS or requested SDL buffers, or subtracted from ordinary
non-interposed samples to infer exclusive owners. Maxima from different
processes need not occur simultaneously.

| Cohort | Role | Traced process peak B | Traced end B | ENet module leaf maximum B |
| --- | --- | ---: | ---: | ---: |
| CPU / owner | authority | 24,253,256 | 18,660,831 | 36,834 |
| CPU / owner | CPU client | 24,556,745 | 18,977,262 | 158,234 |
| CPU / owner | late CPU | 25,279,921 | 18,979,722 | 100,602 |
| CPU / separate | authority | 24,114,300 | 18,516,249 | 36,834 |
| CPU / separate | CPU client | 24,545,669 | 18,959,688 | 162,998 |
| CPU / separate | late CPU | 25,289,541 | 18,982,900 | 100,994 |
| GPU / owner | authority | 24,250,208 | 18,657,783 | 37,202 |
| GPU / owner | GPU client | 482,986,667 | 19,072,472 | 322,540 |
| GPU / owner | late CPU | 25,129,401 | 18,980,882 | 89,617 |
| GPU / separate | authority | 24,229,028 | 18,630,977 | 37,202 |
| GPU / separate | GPU client | 483,040,735 | 19,053,498 | 322,544 |
| GPU / separate | late CPU | 25,267,933 | 18,961,292 | 100,746 |

The ENet column is the individual lifetime maximum whose immediate allocation
caller is `libElectron2DENet.so`, not its value at the global process peak and
not an extra amount to add to that peak. All twelve direct ENet module leaf
groups return to zero bytes at trace shutdown. For example, the GPU/separate
primary allocates 2,014,434 cumulative bytes in 843 calls from that module;
its authority allocates 4,450,253 B in 4,673 calls. Cumulative allocator traffic
is not live storage or application/network wire volume. Inclusive ENet stack
groups also include allocations reached in CoreCLR through native callbacks;
their nonzero end values are not direct ENet module ownership.

At the GPU/separate global peak, 467,250,415 B have an immediate lavapipe caller;
467,445,835 B contain lavapipe anywhere in the stack. These are overlapping
allocation-path views. Large paths traverse `VULKAN_CreateComputePipeline`.
The exact split between ordinary gameplay and the added checkpoint work is
not established by phase pauses; this is not a claimed 483 MB gameplay budget.
Both GPU clients finish with the previously isolated 256 B direct-driver
[topology residue](physics-native-phases.md#driver-only-lifetime-attribution-2026-10-16).
CPU processes have no mapped software/physical GPU and no driver leaf group.

A same-source non-preloaded GPU/separate control passes all fifteen original
CLRMD observations, including disposed held buffers and zero final selected
owners. A strict public-consumer NativeAOT publication and ordinary GPU/separate
900-tick process check also pass. Its four observed processes run the same ELF
without CoreCLR, with one software GPU client and no NVIDIA mappings. These
controls verify collection-mode separation and the public consumer; the native
allocation tables remain instrumented CoreCLR evidence.

Artifacts are `bin/physics-network-native/2026-10-16/`: four instrumented cohort
directories, `default-gpu-threaded/`, `aot-public-gpu-threaded/`, calibrated
event summaries, unmerged reports/flamegraphs, `hook-erasure.json`,
`compiled-source-audit.json` and `validation.json`. Trace identities, native
module and exercised source/binary identities are retained there. Runtime API
and coverage states are unchanged. Per-phase native peaks, complete ownership
and unhooked/direct-mapping totals, NativeAOT native allocation budgets, arbitrary
workloads, foreign platforms, target-device advantage and real window FPS remain
open. The unverified RTX power cable remains a prerequisite for its load tests.

## Event-delimited native phases, 2026-10-16

The current network/preload mode now adds seven small native allocation/free
markers to each child trace. These delimit the event stream before the original
full-GC pauses and at session-ready/first-warm-step boundaries. The new
[`summarize_native.py`](../../tools/physics-memory/summarize_native.py) validates
the pinned Heaptrack 1.5/v3 stream and marker order, excludes their 140 cumulative
requested bytes, and preserves live storage across intervals. It reports gross
and non-marker peaks, entry/exit balances, allocator traffic and the allocation-
leaf module partition at each interval's peak. This counts event-visible native
requests, not every allocator, managed objects, padding, RSS or physical VRAM.

A separate C calibration creates/frees 4,096/8,192 B around the markers. Its
gross/non-marker peaks are 86,034/86,016 B; the startup interval allocates 8,192 B,
the initial-simulation interval frees 8,192 B, and the post-warm interval frees
4,096 B. Cross-boundary live storage is retained, rather than resetting to zero
at each marker. The remaining 4,096 B at trace shutdown are its standard-output
buffer, not a required zero-allocation claim. Synthetic tests also check carried
peaks and rejection of bad versions, missing/duplicate/unordered markers,
unbalanced frees, negative indices and backward timestamps. Independent
Heaptrack flamegraphs check every accepted gross peak/end total.

The accepted matrix repeats all four ordinary CPU/GPU and owner/separate
cohorts with the original 900 ticks, 28 objects, packet impairments, ownership,
geometry/contact/joint/support, event, correction and .002 final-convergence
conditions. Warm managed authority and client predicted/replayed intervals
retain 0/0 owner/all-thread bytes. All 60 process observations and 84 markers
complete. The non-preloaded control still passes all fifteen CLRMD graph phases.
The observer/launcher remain unmarked and unprofiled. The public consumer has
none of the three test hook definitions/references and passes strict NativeAOT
publication and its ordinary mixed-backend process check.

The first warm marker uses the existing step predicate, before that step's
timer/allocation counters. Authorities mark upcoming tick 300 after 299 steps.
CPU primaries mark tick 300 after 206/220 steps; GPU primaries mark 553/556 after
128 steps. Late clients mark 427–439 after 128 steps. These are real local
prediction/replay histories, not a substituted global tick-only warmup. The
post-warm interval still includes network polling, correction/replay, feature
changes and report preparation before the completion hook; it is not a
physics-step-only bracket. Session construction also precedes client admission,
so remaining lazy setup/compilation belongs to initial simulation.

| Cohort / role | Construction peak B | Initial simulation peak B | After first warm gate peak B | Extra-checkpoint interval peak B |
| --- | ---: | ---: | ---: | ---: |
| CPU owner / authority | 14,872,395 | 15,083,210 | 15,022,084 | 14,956,340 |
| CPU owner / CPU | 14,979,488 | 15,436,761 | 15,369,835 | 15,303,555 |
| CPU owner / late | 14,970,648 | 15,371,243 | 15,322,091 | 15,276,651 |
| CPU separate / authority | 14,724,023 | 15,121,882 | 15,063,260 | 14,997,580 |
| CPU separate / CPU | 14,980,356 | 15,285,917 | 15,225,312 | 15,159,032 |
| CPU separate / late | 14,975,164 | 15,414,163 | 15,364,419 | 15,318,979 |
| GPU owner / authority | 14,730,251 | 14,963,890 | 14,902,684 | 14,836,940 |
| GPU owner / GPU | 45,729,311 | 482,969,577 | 200,395,319 | 199,917,007 |
| GPU owner / late | 14,970,480 | 15,371,699 | 15,321,915 | 15,276,387 |
| GPU separate / authority | 14,873,295 | 15,091,690 | 15,033,068 | 14,967,388 |
| GPU separate / GPU | 45,721,187 | 483,044,977 | 200,470,231 | 199,991,906 |
| GPU separate / late | 14,979,876 | 15,264,307 | 15,214,595 | 15,169,067 |

Thus the large software-GPU peak is in initial simulation, before its first
actual warm gate; it is not caused by the four added checkpoint captures.
Post-warm and checkpoint peaks include inherited live driver/runtime storage,
not just requests born in those intervals. Their difference is not an exclusive
checkpoint budget. CPU full-lifetime peaks of 24–25 MB occur later in the
release/report interval, while its active-session peaks are about 15 MB.
Disposal, release, bootstrap and shutdown values and peak-module partitions
remain in the complete eight-interval reports, not omitted from accounting.

Markers are actual bounded requests from the diagnostic module, not fabricated
allocation records. They add no GPU idle/step fence. Native concurrency is
partitioned in profiler event order; timestamps have the profiler's original
granularity. Marker loading/managed receipts and existing observation overhead
remain instrumentation. These are not uninstrumented gameplay totals, native
allocations per physical step or a hardware performance comparison.

Accepted artifacts are `bin/physics-network-phases/2026-10-16/accepted/`, with
four marked matrices and the default control; the parent directory retains
source/binary audits, calibration, hook-erasure, public AOT and validation
receipts. The initial pre-format pilot stays separate. API/coverage and engine
runtime behavior remain unchanged. Direct mappings/unhooked/foreign ownership,
strict NativeAOT allocation budgets, arbitrary/massive scenes, target GPU gain,
rendered output and real window FPS remain open. RTX loading is still deferred
until its power cable is checked.

## Live native call-path inventory, 2026-10-16

The analyzer now inventories live requests at all seven markers and process end,
using the same accepted traces above. It partitions every non-marker byte by
allocation-leaf module and the first different module/function in the recorded
parent stack. This keeps C++ allocation entry points separate from their JIT or
driver callers. Unknown frames remain explicit; a call-path group is not an
object-owner graph. The twelve largest size/trace sites include live allocation
counts and complete recorded address/module/function stacks, with explicit
omitted-site bytes/counts. Their totals and each full route partition equal the
observed boundary balance.

Offline replay of the original sixteen interpreted traces produces 128 balanced
inventories: 96 for the twelve session processes and 32 for four calibrations.
Every earlier numerical field, marker and interval is unchanged. No new network,
GPU or performance run is substituted for those original checks. Synthetic tests
cover shared allocator modules, nested same-module frames, multiple live requests
per allocation-info, reuse, top-site truncation and malformed addresses.

The GPU boundaries show where the previously observed approximately 200 MB live
balance comes from. The pipeline group below is specifically a lavapipe leaf
whose first external recorded frame is SDL `VULKAN_CreateComputePipeline`.
The loader group has a lavapipe leaf and the Vulkan loader as its first external
module. The JIT group has a C++ allocation leaf and `libclrjit.so` as that caller.
These disjoint groups are subsets of the total, not an exhaustive owner census.

| GPU policy / boundary | Total live B | Pipeline call path B | Loader call paths B | C++ via JIT B |
| --- | ---: | ---: | ---: | ---: |
| Owner / first warm | 199,908,226 | 154,232,740 | 28,297,327 | 13,044,328 |
| Owner / completed | 199,915,391 | 154,232,740 | 28,297,327 | 13,044,328 |
| Owner / checkpointed | 190,211,155 | 154,232,740 | 28,297,327 | 3,279,464 |
| Owner / disposed-held | 6,416,385 | 0 | 0 | 3,934,824 |
| Owner / released | 19,310,337 | 0 | 0 | 16,779,880 |
| Owner / process end | 19,057,956 | 0 | 0 | 16,779,880 |
| Separate / first warm | 199,981,802 | 154,232,740 | 28,303,679 | 13,044,328 |
| Separate / completed | 199,990,290 | 154,232,740 | 28,303,679 | 13,044,328 |
| Separate / checkpointed | 190,286,142 | 154,232,740 | 28,303,679 | 3,279,464 |
| Separate / disposed-held | 6,424,257 | 0 | 0 | 3,934,824 |
| Separate / released | 19,318,769 | 0 | 0 | 16,779,880 |
| Separate / process end | 19,060,130 | 0 | 0 | 16,779,880 |

The pipeline group has the same live-byte balance at warm/completed/checkpointed
boundaries and no live requests after disposal; matching balances do not prove
unchanged individual allocations. The later JIT-call-path increase lies in the
report/release interval and is not residual pipeline memory. Direct lavapipe leaf
end totals remain 256 B, separate from the two zeroed groups. This narrows the
native ownership investigation without calling all driver-leaf or process-end
bytes engine leaks. No global driver cleanup, runtime cache, allocator tuning or
reduced scene is introduced.

Reanalysis receipts and complete inventories are under
`bin/physics-network-sites/2026-10-16/`, referencing the original accepted inputs
by SHA-256. The interpretation aggregates allocation-info by size/trace, so it
does not establish pointer identity or lifetime pairing across boundaries.
Addresses are process-local, symbol availability is bounded by the captured
binaries, and inline source frames are not expanded. Unhooked/direct-mapped
storage, exact owner graphs, NativeAOT native budgets, hardware gain and window
acceptance remain open. RTX load still requires the power-cable check.

## NativeAOT native lifetime cohort, 2026-10-16

The runner now supports `--network --network-aot`. Its host links all thirteen
public example source files plus the same test-only child-launch hook used by
the CoreCLR collector. Publication enables the source-native build path,
NativeAOT and strict linker warnings. Session/world logic, report preparation
and all original checks stay in the example sources. The host uses the public
runtime API; it has no private backend probe or added checkpoint/GC phase gates.

Four profiled cohorts cover CPU/GPU primary clients and owner/separate numerical
policies. All retain 900 authority ticks, 28 objects, late-admission request 310,
250 ms primary poll delay, ordinary unpaced prediction/replay and the complete
original geometry/contact/joint/support, packet-impairment, lifecycle/ownership,
confirmed-event and .002 final-convergence checks. Every warmed physical owner/
all-thread allocation counter remains 0/0. CPU processes exercise denied GPU
startup before their working worlds. Each native launcher and its three children
use the same ELF SHA-256 across all five AOT cohorts:
`e1ee3cd5df8d384fc0a21932bbed7fb0d751bb6ceb04ae2913397a352e0bd62c`.
All 55 delivered native dependency files also match across those five AOT
cohorts. The supplementary CoreCLR regression has separate ENet, FAudio and
TextBreak binary identities, recorded independently. Its native code/readonly
sections differ as well; these cohorts are not a normalized AOT/CoreCLR
performance comparison.

The collector and launcher stay unprofiled. Twelve actual session processes
produce twelve lifetime traces; sampled mappings verify the native executable,
absence of CoreCLR/phase-marker libraries, expected preload and only the GPU
role's pinned software driver. No heap attachment is performed; selected object
sizes are explicitly unmeasured. A fifth AOT GPU/separate cohort exercises the
same host without preload, and the ordinary public NativeAOT consumer passes its
full mixed-backend check separately. Its test hooks remain erased. A fresh
marked CoreCLR GPU/separate regression retains all fifteen observations and
twenty-one markers after extraction of the shared launch hook.

The analyzer's explicit `--lifetime` mode accepts unmarked data and rejects phase
markers; its default still requires all seven. Independent unsuppressed/unmerged
Heaptrack peak/end flamegraphs agree exactly for all fifteen new native/CoreCLR
traces. Generated flamegraphs are retained losslessly compressed. These are
per-process hooked native requested bytes over the complete lifetime:

| NativeAOT cohort / role | Simultaneous peak B | Process-end B |
| --- | ---: | ---: |
| CPU owner / authority | 1,316,782 | 1,207,996 |
| CPU owner / CPU | 1,660,485 | 1,520,855 |
| CPU owner / late | 1,616,389 | 1,520,855 |
| CPU separate / authority | 1,317,422 | 1,208,636 |
| CPU separate / CPU | 1,661,125 | 1,521,535 |
| CPU separate / late | 1,606,152 | 1,521,535 |
| GPU owner / authority | 1,316,738 | 1,207,996 |
| GPU owner / GPU | 469,341,513 | 1,639,613 |
| GPU owner / late | 1,616,389 | 1,520,855 |
| GPU separate / authority | 1,317,632 | 1,208,522 |
| GPU separate / GPU | 469,417,495 | 1,639,739 |
| GPU separate / late | 1,606,174 | 1,521,421 |

The GPU end inventories have 256 B with direct lavapipe allocation leaves,
131,184 B with C++ allocation leaves, and zero direct ENet leaves. The executable
itself accounts for 1,110,185/1,110,273 B of end allocation leaves. NativeAOT folds
runtime and engine code into that executable; its leaf identity is not an exact
owner classification. CoreCLR/JIT paths from the earlier cohort are absent;
these results neither transfer its phase boundaries to AOT nor identify every
remaining allocation. Complete unhooked/direct-mapped storage and managed heap
reservations remain outside this requested-byte scope. No process peak is added
to another process, RSS, selected managed graph or requested device-buffer total.

Controller-failure testing also reproduced an owned stopped child surviving a
nonzero collector exit. The shared runner now routes that failure through its
existing process-group cleanup; the test changes from RED to GREEN alongside the
existing timeout/interruption cleanup. No successful cohort depends on retrying
an allocation threshold or weakening the physical checks.

Artifacts are under `bin/physics-network-aot-native/2026-10-16/`; the completed
CPU/separate pilot is retained as that accepted cohort. They include strict
publication, process/trace identities, unprofiled controls, source/PDB and hook
audits, independent analyzer totals and verification receipts. API/coverage and
runtime behavior remain unchanged. NativeAOT phase attribution, exact native
owners, arbitrary/massive workloads, target GPU advantage and real window FPS
remain open. RTX loading still requires its power-cable check.
