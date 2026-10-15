# Scene, network and local history memory

Last updated: 2026-10-16

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
