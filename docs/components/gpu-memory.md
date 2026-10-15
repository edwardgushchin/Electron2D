# Resident GPU buffer memory

Last updated: 2026-10-15

[Repeated public world retirement](physics-retirement-memory.md) collects the
selected store/device after normal and failed intervals, yet the default threaded
software process accumulates resident free glibc arena storage. Requested buffer
accounting does not close that open native-memory/performance issue.

## Measured boundary

`GPUPhysicsBodyStore.BufferMemory` reports the current requested bytes and live
resource counts for every SDL storage buffer and transfer buffer owned by that
store, including its local replay checkpoints. It also records cumulative
allocation/release counts and the maximum combined live byte count. The peak
includes old and replacement resources that coexist during growth; adding
separate subsystem estimates would miss this overlap.

Registration occurs immediately after a successful native allocation, before the
diagnostic initialization hook. Failed preparation releases registered temporary
resources. The same release path handles ordinary retirement, capacity rebinding,
checkpoint retirement and SafeHandle cleanup. Capacity rebinding preserves the
existing wrapper and device reference. Buffer metadata contains only native
resource identities, requested sizes and storage/transfer roles; it copies no
simulation state and adds no device readback, submission or wait.

The metadata dictionary grows only when live native capacity grows. Updates and
observations use its lock, including cleanup on a finalizer thread; ordinary
capacity-stable numerical work does not access it. Warm allocation measurements
must still include real steps and checkpoint operations, rather than only reads
of these counters.

These are **requested resource bytes**, not resident physical VRAM or complete
process memory. SDL/driver heap alignment, deferred retirement, pipelines, shader
storage, command/fence objects, allocator metadata, managed arrays, dictionaries,
thread stacks, CPU solver state and transport/history buffers are outside this
counter. Software Vulkan can back both resource classes with system memory; its
values cannot establish an RTX memory budget or hardware performance advantage.

## Executable checks

`GPUPhysicsMemoryTests` runs through the existing resident-body and complete GPU
profiles. Initial body-only allocation is compared with independently observed
storage-buffer sizes and the actual command/request/result transfer layout.
Buffer counts and layouts are diagnostic expectations for the current implementation;
update them with an internal storage redesign.
Growth from 64 to 128 slots checks the fixed status headers, all live counts and
the simultaneous old/new peak. A failure during replacement must release all
temporaries without losing old capacity. Nine initial preparation failures check
each storage-buffer prefix, successful retry and matched releases. Removing bodies
retains reusable capacity; repeated store disposal leaves zero live bytes/resources.

`GPUPhysicsCheckpointTests.Warm` additionally observes the entire 1024-body,
512-contact-pair, one-pin world and its checkpoint. The 128 warmed capture/restore
pairs must preserve all native byte/count/allocation/release observations while
retaining the existing strict zero owner/all-thread managed-byte requirement.
Checkpoint disposal must subtract exactly its declared device capacity without
changing world transfer capacity; store disposal releases every remaining buffer.

Full CPU and managed retained-object budgets, native allocator live sets,
application-bounded multi-tick history, physical-device heaps and real-window FPS
remain open under [ADR 0054](../decisions/physics-backends.md#adr-0054).

## Linux software Vulkan observation (2026-10-15)

The source-native Release tests use .NET 10.0.1, tiered compilation disabled,
SDL offscreen and the pinned lavapipe driver (llvmpipe LLVM 23.1.1). Process maps
must contain software Vulkan and no NVIDIA driver. The body-only fixture keeps
65,536 bodies and 384 warmup/256 measured integration steps; this is a stage
control, not a complete physical-scene performance claim. The checkpoint fixture
retains all 1024 bodies, 512 collision pairs and one world pin, with 64 warmup
cycles and 128 measured capture/restore pairs.

| Fixture | World device bytes | Checkpoint device bytes | Transfer bytes | Combined peak bytes |
| --- | ---: | ---: | ---: | ---: |
| 64 body slots, before growth | 26,128 | 0 | 16,408 | 42,536 |
| 128 body slots, after growth | 52,240 | 0 | 32,792 | 127,568 |
| 65,536 integration-only bodies | 26,738,704 | 0 | 16,777,240 | 65,273,936 |
| 1024-body contact/joint checkpoint | 1,105,072 | 393,296 | 393,448 | 1,891,816 |

The checkpoint interval records zero new storage/transfer allocations or releases
and 0/0 owner/all-thread managed bytes. Its 786,592 device-copy bytes per pair are
separate from capacity. All buffer requests have matching releases at disposal.
Exact commands, source/binary fingerprints, process observations and full logs
are under ignored `bin/physics-buffer-memory/2026-10-15/`. These observations do
not close the broader memory and hardware gates listed above.

## Remaining memory owners

[Scene/network graph measurements](physics-network-memory.md) extend the raw-world
budget to the complete example's managed scene, histories and transport capacities,
including an additional four-point local scene history. Native/driver/static-global
storage and traffic peaks are still outside the measured partition.

[Retained managed graph measurements](physics-retained-memory.md) now cover controlled raw CPU/software-GPU worlds, common/selected checkpoints, maps and peak portable payloads at 1024/4096 bodies. These fixture partitions do not close the complete total below.

[CPU pool payload retirement](cpu-checkpoints.md#retired-pool-payloads) releases
borrowed user data, names, manifold delegates and replaced move-event references
from retired records and checkpoint copies. The pool shells and backing array
capacities remain reusable and still belong in the retained byte budget.

[Retained common histories](physics-snapshots.md#temporary-reference-lifetime)
release removed shape resources and retired contact identities from reusable
array tails after replacement. Live-prefix payload and allocated capacity remain
owned by their map/checkpoint; a collection check does not measure their byte size.

Requested GPU resources are one part of the required total. The complete retained
budget must cover these owners beyond the controlled raw fixture without counting
shared objects twice:

- `GPUPhysicsBodyStore` authored slots, sparse commands, geometry, readback scratch
  and the resource-size dictionary, including what remains after store retirement.
- `CPUPhysicsWorldBackend` and the vendored world/pool/worker storage;
  `CPUPhysicsCheckpoint` owns a separate managed solver copy.
- `PhysicsSpace.Checkpoint` retains common replay entries, poses, joint
  configuration and diagnostics in addition to each backend's private copy.
- `PhysicsSnapshot` keeps its peak byte-array capacity, which can exceed the
  current packet length. `PhysicsSnapshotMap` additionally owns identity tables,
  object/joint bindings, replay entries, one-way history and capture scratch.
- `examples/PhysicsNetwork` owns bounded input/event/presentation histories and
  transport queues. Its encoded packet length alone cannot describe this memory.

The controlled graph measurements do not promote these broader owners to accepted totals.

[Local checkpoint disposal](physics-space-checkpoints.md#released-history-references)
now releases the selected history reference even after a cleanup exception.
[Portable scratch](physics-snapshots.md#temporary-reference-lifetime) clears borrowed
object references after successful and rejected operations, including partial
attachment preparation. CPU/GPU weak-reference tests distinguish these fixed
retention paths from the reusable array capacities that still require a byte budget.
The shared spatial/joint status buffer now reserves its full four-word sleep/status
layout (16 B rather than 8 B). Earlier measurements above describe their recorded
candidate; this correction adds 8 B to worlds that own that buffer.

## Query investigation found during validation

The extended resident-query profile fails its ray-normal comparison because the
existing fixture authors a .37-rad CPU pose but converts it through backend-specific
approximate trigonometry before creating the GPU body (.37121636 rad). The prior
frozen runtime fails identically. An isolated equal-authored-angle probe passes
that comparison; an independent rectangle-basis oracle also passes without
changing the existing .004-position and .001-normal comparison bounds.

Correcting the polygon edge oracle to the same authored basis then exposes a
separate CPU point-containment case: clockwise 12-vertex polygon, random seed 313,
family 9/sample 29, point `(1790.1703, 73.80258)`. Its local point
`(-11.405591, -2.2234466)` is outside by .0013154149 scene units; GPU excludes it,
while the CPU GJK point test includes it. The actual CPU body basis is
`(0.93232733, 0.36161548)`, matching .37 rad. The memory change left this numerical-query investigation open. It is now
corrected by the [unrounded point-containment slice](physics-point-containment.md),
which replaces CPU zero-radius polygon distance membership and fixes the stale
fixture input without widening the existing bounds. The original memory validation
remains recorded with its failure; the later query verification has separate evidence.

The original failure, previous-runtime comparison and causal diagnostic sources/logs
are retained under `query-diagnostic/` and `queries-prior-runtime.*` alongside the
memory evidence. One early diagnostic build lacked a retained oracle variable;
its subsequent stale-DLL execution is excluded from evidence. The separately
rebuilt equal-angle-v2 and point-native-pose probes identify the actual cases above.

## Revision scope

The initial managed profiles, including the full executable suite, use the frozen
`f73d6f9e` baseline plus this memory change. While validation ran, the independent
OGV integration `02d7a48a` entered main without changing physics source/tests.
A fresh source-native managed build on that main repeats resident-body memory,
checkpoint memory and no-device CPU checks with the same byte totals. NativeAOT
source checks also include that OGV integration. The complete managed suite was
not repeated after this unrelated integration. Separate source/binary manifests,
Compile/PDB document audits and the foreign commit delta preserve both scopes;
no old assembly is relabelled as a full current-main run.
