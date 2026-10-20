# Retained physics world and history storage

Last updated: 2026-10-20

[CPU contact-pair preparation](physics-scene-performance.md#cpu-contact-pair-capacity-2026-10-15)
now adds 4,096 selected-graph bytes to the controlled 64-body world and each
history, with zero additional difference after source release. The 1,024-body
fixture is unchanged because its table already reached the same reserved capacity.
The original tables below retain their stated revisions and are not silently
relabelled as measurements of this correction.

## Scope and method

[The development probe](../../tools/physics-memory/README.md) now measures a
controlled raw server world through the public API on Linux x64/CoreCLR 10.0.1.
The CPU process has no usable Vulkan ICD; the GPU process selects the full resident
solver on pinned lavapipe, without fallback, display or renderer. Both use the
same engine DLL, shared radius-2 circle resource, isolated initially overlapping
pairs, one origin pin, no user handlers, zero gravity and sleeping disabled.
The 1024/4096 original bodies remain active and publish 512/2048 constrained islands.
CPU publishes 8136/33672 broad-phase candidates; GPU publishes 512/2048. These are
actual backend candidate counts, not reduced object populations or contact counts.

The parent suspends its own child after two blocking collections/finalizer drains.
CLRMD walks strong managed references, counts each object once by reported Size,
and partitions the forward union into world, map, packet, common checkpoints and
selected solver checkpoints. Both common and resident checkpoint registries form
boundaries. Borrowed world/backend references resolve to the already counted world.
The partition must equal an unrestricted forward union of all those roots.
Every phase checks a shared 65536-byte array and a separately live weak target:
shared storage is counted once and the weak target is absent. Per-type counts and
bytes remain in the raw JSON. A small shared empty array/comparer can be attributed
to the first owner that reaches it; individual checkpoint totals therefore differ
slightly while the deduplicated union remains exact for this convention.

These are **CLR-reported managed object bytes in this fixture graph**, not GC
allocation deltas, dominator retained sizes, RSS, native heaps, allocator padding
or a complete process budget. Fixture anchors/RID arrays, static service tables,
static pool shells outside the released world's graph, thread stacks, runtime
bookkeeping, pipelines and driver memory are excluded. There is no scene tree or
network example history. Arbitrary user handlers and dependent handles need a
separate ownership audit. CoreCLR sizes establish no NativeAOT size guarantee.
Requested SDL storage/transfer bytes below are separate reservations, not physical
VRAM; lavapipe may place them in host memory. No timing or FPS acceptance follows.
The later [static CPU world-pool measurement](#static-cpu-world-pool) adds one
identified static root separately; it does not change the historical tables below.

## Measured managed object bytes

Each history phase warms eight full steps with private capture/restore and portable
capture/apply. Shrink removes three quarters of the bodies and the joint, unbinds
the retired objects and recaptures all histories. Released-history retains the
caller's disposed checkpoint/map wrappers and portable packet. Released-world
also frees the remaining bodies, shared shape and space; the probe requires no
remaining PhysicsSpace object. The packet intentionally remains held.

| Original bodies / local checkpoints | Phase | CPU object bytes | GPU object bytes |
| --- | --- | ---: | ---: |
| 1024 / 1 | world | 36,327,602 | 2,754,504 |
| 1024 / 1 | history | 43,353,634 | 5,985,756 |
| 1024 / 1 | shrink | 39,761,694 | 2,960,332 |
| 1024 / 1 | released-history | 34,807,258 | 1,916,664 |
| 1024 / 1 | released-world | 384,464 | 384,464 |
| 1024 / 4 | world | 36,327,602 | 2,754,504 |
| 1024 / 4 | history | 59,692,570 | 10,695,432 |
| 1024 / 4 | shrink | 53,749,122 | 5,218,552 |
| 1024 / 4 | released-history | 34,859,482 | 1,971,912 |
| 1024 / 4 | released-world | 436,688 | 436,688 |
| 4096 / 4 | world | 151,040,550 | 10,979,976 |
| 4096 / 4 | history | 248,600,942 | 42,894,504 |
| 4096 / 4 | shrink | 224,835,006 | 20,988,568 |
| 4096 / 4 | released-history | 145,200,758 | 7,863,496 |
| 4096 / 4 | released-world | 1,770,768 | 1,770,768 |

The history phase for 1024 bodies and four points partitions as follows. Selected
solver rows include actual private managed copies; their device storage is in the
separate SDL totals below.

| Owner | CPU object bytes | GPU object bytes |
| --- | ---: | ---: |
| world | 36,327,690 | 2,840,288 |
| map | 1,337,744 | 1,337,744 |
| snapshot | 241,864 | 241,864 |
| checkpoint-0 | 1,106,944 | 1,106,944 |
| checkpoint-0-solver | 4,339,392 | 461,940 |
| checkpoint-1 | 1,106,944 | 1,106,944 |
| checkpoint-1-solver | 4,339,368 | 461,940 |
| checkpoint-2 | 1,106,944 | 1,106,944 |
| checkpoint-2-solver | 4,339,368 | 461,940 |
| checkpoint-3 | 1,106,944 | 1,106,944 |
| checkpoint-3-solver | 4,339,368 | 461,940 |

For 4096 bodies each common point owns 4,424,704 B. The first selected CPU solver
copy owns 18,376,808 B and later copies 18,376,784 B; each selected GPU managed copy
owns 1,882,460 B. This separates replay-entry cost from the selected numeric history.

CPU pools dominate the original 1024-body world: 79,873 B2ContactSim shells account
for 23,003,424 B of its 36,327,602 B graph. They are prepared capacity, not live
contacts. Retiring payload references does not remove these reusable class shells.
Shrinking a world or recapturing history therefore does not imply proportional
capacity reclamation. The selected CPU copy can grow free-ID storage during shrink;
its measured bytes increase despite fewer live bodies. These measurements motivate
a capacity-policy review; they do not authorize removing required physical state.

The 1024-body packet initially encodes 241,800 B. After shrink its payload is
60,464 B but the byte-array capacity remains 241,800 B (241,864 B with snapshot
wrapper/array object sizes). At 4096 bodies the corresponding lengths are
966,792 B and 241,712 B, with capacity 966,792 B. Disposed wrappers still retain
empty reusable arrays: the four-point 1024 profile keeps 436,688 B after the world
has gone, versus 384,464 B for one point. That residual is caller-held history and
packet storage, not a retained live space or selected solver.

## Requested SDL bytes on the same fixture

| Original bodies | Phase | Device buffer bytes | Transfer buffer bytes | Combined peak bytes |
| --- | --- | ---: | ---: | ---: |
| 1024 | world | 1,125,568 | 393,520 | 1,519,600 |
| 1024 | history | 2,862,600 | 524,600 | 3,387,200 |
| 1024 | shrink | 2,862,600 | 524,600 | 3,387,200 |
| 1024 | released-history | 1,289,416 | 524,600 | 3,387,200 |
| 4096 | world | 4,369,600 | 1,573,168 | 5,943,280 |
| 4096 | history | 11,316,744 | 2,097,464 | 13,414,208 |
| 4096 | shrink | 11,316,744 | 2,097,464 | 13,414,208 |
| 4096 | released-history | 5,024,968 | 2,097,464 | 13,414,208 |

These totals include all private device checkpoint buffers and capacity overlap
already covered by [buffer accounting](gpu-memory.md). Shrink preserves their peak
capacities. Releasing the four points removes 1,573,184/6,291,776 device bytes for
1024/4096 bodies. The original world uses 54 device and 7 transfer buffers; portable
publication and four points use 117 and 9. After point disposal it keeps 57 and 9.
After world disposal no GPUPhysicsBodyStore remains in the collected heap; this
is not an external driver-heap or native-leak measurement.

## World-anchor capacity

The first CPU fixture exposed an out-of-range write in CaptureBodyMotions at a
power-of-two collider population. A single-body pin lazily creates one shape-free
static endpoint; that endpoint had not prepared the additional native body slot.
GetJointWorldBody now prepares solver/contact-motion capacity immediately after
creating it. The internal anchor still has ordinary world lifetime and identity.

The public-only `--anchor-capacity` regression first steps exactly 8, 16, 64 or
256 raw bodies, then creates an origin pin and steps again. A hard pin must reduce
10 units/s to below .01 units/s in 1/120 s; this bound allows solver float error
while requiring a real constraint response. CPU/software-GPU serial/separate
profiles run in CoreCLR and NativeAOT. Every profile requires 64 warmed active
steps at 0/0 owner/all-thread managed bytes. The frozen prior runtime fails the
same public regression before the allocation check; the corrected runtime passes.
The test uses the existing two-pass finalizer-draining helper; an earlier fixture
omitted its second wait and observed 1008 all-thread bytes after collection. That
candidate is retained as a failed measurement, not counted as a pass.

## Evidence and remaining work

Raw local artifacts are under `bin/physics-retained-memory/2026-10-15/`:
`accepted-1024-one/`, `accepted-1024-four/` and `accepted-4096-four/` each contain
CPU/GPU JSON and build logs; `environment.json` records the software driver hash.
All six reports use runtime SHA-256
`0617f3d2942575217c3c7afeae8459dd5cf9f276b52fe327191c25b28c019797`.
The local source/PDB audit checks actual Compile inputs, not only filenames.
Raw report checksums:

| Report | SHA-256 |
| --- | --- |
| accepted-1024-four/cpu.json | `603d6988abfa73efbb511d1c4904b0d7fe60646878506aefe30628f498af7aaf` |
| accepted-1024-four/gpu.json | `f01550e16145795d5057e005beba3081f07ae1ddf6545e065853008e8537f054` |
| accepted-1024-one/cpu.json | `292a92ab756db9511b44fbcc896340b9d54ba101c07fe696b5261326089cad2f` |
| accepted-1024-one/gpu.json | `96f5b6bbcd59668760981474639a075dfb17ebc32b38b5c1f471db77fb516e86` |
| accepted-4096-four/cpu.json | `4a38354eb36b0fe8558fb5336f4e93cf8f41396e7b242de74cde667d5a500eb2` |
| accepted-4096-four/gpu.json | `80496d6a9f4156e36fe470444f8d21fd172692054cd1593607e4df0533604621` |

The complete retained/native/device/history budget remains open. Subsequent
[scene and network measurements](physics-network-memory.md) cover the controlled
PhysicsNetwork observer/map/input/event/presentation/transport graph and additional
local scene checkpoints. The static CPU world-slot pool is now measured below;
other static/global roots and native/driver storage remain open.
Process peaks, larger mixed geometry/CCD/sleep/Area loads, platform/AOT memory,
full-step timing, real-window FPS and target-hardware GPU benefit remain separate
acceptance work. Real RTX load was deferred for these profiles. The later
authorized [hardware baseline](physics-rtx-current.md) measures separate workloads
and does not complete retained-memory acceptance.
No applicable API row is promoted by these bounded memory measurements.

## Static CPU world pool

The live owner graph did not expose static-slot references after a world was
destroyed. A focused regression found two retained source arrays per world:
reusable color segments still pointed at arena solver blocks, and the slot still
held its source graph-color array. World reset now clears both references. The
test creates four simultaneous 32-body contact worlds for each combination of
ordinary/separate numerical lanes, with/without held independent checkpoints,
and normal/injected-failed intervals. The old candidate retains all 64 weak array
targets across 32 worlds; the fixed CoreCLR and focused Linux NativeAOT candidates
retain zero. Active steps still reuse the same block storage. This is a lifetime
regression, not a new public capability or a numerical-tolerance change.

The probe now reads the actual `B2Worlds.b2_worlds` static field without running
target code or initializing that field. It traverses the array and idle slots,
cuts active slots before their callbacks can borrow game objects, then counts
only each active slot's object shell. Shared bytes already in the selected owner
union are reported separately. Add only `AdditionalManagedObjectBytes` to that
union; the two unadjusted totals overlap. Idle slots retaining solver-block or
graph-color arrays reject the observation. This is one static-root audit, not
complete global ownership, dominator retention, native memory or RSS.

On the fixed Linux x64/CoreCLR candidate, both the 64-body/one-checkpoint CPU
profile and the 1024-body/four-checkpoint CPU profile leave the same fully idle
pool: **8,961 objects, 631,832 bytes, 128 slots, zero active slots**.

| Remaining static storage | Objects | Object bytes |
| --- | ---: | ---: |
| worker contexts | 8,192 | 327,680 |
| world shells | 128 | 117,760 |
| worker reference arrays | 128 | 68,608 |
| cleared color-segment arrays | 128 | 52,224 |
| reset step contexts | 128 | 50,176 |
| contact-end event descriptor arrays | 128 | 7,168 |
| sensor-end event descriptor arrays | 128 | 7,168 |
| world reference array | 1 | 1,048 |

With one live CPU world the reported pool is 627,824 bytes, including its
920-byte shell already counted in the owner graph; the additional amount is
626,904 bytes. After world release the held 1024-body map/packet/history graph
is 436,688 bytes, so that graph plus this static pool is 1,068,520 bytes. This
combined subtotal still excludes all other global and native/process storage.
The independent GPU-only 1024-body process never initializes this CPU pool and
reports zero slots/bytes.

Raw evidence is under `bin/physics-static-memory/2026-10-15/`: `red.log`,
`managed-cpu_world_lifetime.log`, `aot-world.log`, and `accepted-1024/`.
The source-native engine SHA-256 for the managed raw/network fixtures is
`f338f62e25da376de1dbaf8c12d03d174405c96a59a1f4c9d8a51f2809bdb2c8`.
`network-cpu/` and `network-gpu/` repeat the full 900-tick, late-310 three-process
checks with separate numerical scheduling selected, with 24 calibrated heap
observations.
All five CPU children leave that same fully idle pool; the GPU child never
initializes it. Existing gameplay, correction and zero warmed allocation gates
remain active. These public-API memory hosts do not enable internal backend
telemetry, so their reports do not verify numerical thread identity or device
transfer timing. Their pauses are not speed measurements.

One 64-body software-GPU probe stalled with its fixture in a ptrace stop and
timed out; it is not an accepted GPU result. A fresh `retry-64/` cohort completes
both CPU/GPU profiles on the same assemblies. The runner now starts each probe in
an owned process group and kills that group on timeout/interruption. A nested-child
timeout check verifies cleanup. The successful 1024-body and network profiles do
not prove general live-attachment reliability. Native/driver heaps, other globals,
whole-process peaks, NativeAOT sizes, wider workloads and final hardware/window
acceptance remain open.

The subsequent [process and allocator measurements](physics-process-memory.md)
add startup baselines, GC commitment, glibc statistics and current RSS/PSS plus
kernel-reported high water to the same controlled fixtures. These overlapping
process scopes do not replace per-world ownership or close the full budget.
