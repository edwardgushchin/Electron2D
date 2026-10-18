# Device-local GPU replay checkpoints

Last updated: 2026-10-18

## Executing scope

`GPUPhysicsBodyStore.CreateCheckpoint()` captures a reusable, store-bound
[`GPUPhysicsBodyStore.Checkpoint`](../classes/GPUPhysicsCheckpoint.md). Its `Capture`
replaces the saved state; `Restore` rewinds simulation data for the same authored
configuration. This is an internal foundation for local replay under ADR 0054 and
ADR 0094. It is **not** a public PhysicsSpace/SceneTree checkpoint, portable wire
snapshot, CPU checkpoint or complete authoritative replication implementation.
Those requirements remain open in the [physics audit](physics-contract-audit.md).

The store must retain its body/shape/joint identities, body roles/integration/mass
configuration, geometry/material/filter/one-way settings, Area field version and
world solver/sleep policy. Configuration mismatch rejects before any restore copy
or counter/state mutation. Birth/death or changed shape generations cannot be
silently applied to a different object. Resource geometry/disposal/policy changes
are checked against captured resources, rather than trusting the current GPU cache.
Joint/exception configuration edits still queued for upload also reject.

Motion inputs are rewindable: poses, velocities, impulses, persistent/transient
forces, pending kinematic targets, virtual surface velocities, can-sleep and
sleeping state. Capture flushes queued body edits without advancing time; dirty
geometry/joints are prepared when needed. Restore discards unsubmitted future body
commands. Caller-supplied simulation inputs/durations must be replayed by the host.

## Retained state and derived data

Checkpoints own GPU-to-GPU copies of body data, centers, transient forces, targets,
resolved fields, joint warm state, contact warm records, sleep graph/edges, pending
position correction, one-way episodes/hash table, completed contact-report records
and per-body heads, contact points and solver scratch. The last candidate-pair
buffer is copied with its diagnostic count. Host arrays contain only authored
configuration needed to reject incompatible restores; they are not live pose,
velocity, contact or island mirrors.

Each checkpoint owns separate buffers. Recapture retains high-water capacity;
several checkpoints can coexist without aliasing the running simulation. Later
contact-capacity growth is supported. The next solve rebuilds the contact-history
hash against the current allocation. A restored one-way table retains its captured
lookup size until the next episode publication, even if a later frame enlarged the
live allocation. Sleep clocks, connected-wake graph, idle-skip metadata and pending
force consumption are restored together.

Contact-color scheduling keys, colors and their device hash table are retained
when a valid multi-color cache exists. Record copies use 48 bytes per captured
contact row; the table uses four bytes per captured logical lookup slot. These
copies join the same capture/restore command and fence, without a CPU contact
mirror or physical readback. Lookup retains the captured table size even when a
later branch enlarged the live allocation. The next completed color schedule
publishes a table for the current capacity. Cumulative work counters do not rewind.
This is local numerical continuation data, not a portable network payload.

The first keyed-cache regression failed the existing eight-box 40-tick replay
bound: one body's position differed by about .023 against the unchanged .02 limit.
Retaining the scheduling cache addresses the discarded-future seed state. A new
focused growth fixture captures four rows, grows a discarded branch to 100 rows,
then requires restored cache reuse inside the larger live buffers. The focused profile passes in 9.790839 seconds, including this growth
case and the unchanged original contact/joint/CCD replay bounds. Warm capture and
restore retain strict 0 owner/all-thread managed bytes, no new native buffers and
the original two copy submissions; the disjoint single-color warm fixture has no
valid multi-color cache to copy. The final fourteen-profile focused/related
regression also passes on the checkpoint-retaining source payload, including the
complete resident solver and transient/dense contacts. The complete repository
suite and target hardware/window gates retain independent acceptance.

Spatial/query caches are invalidated and rebuilt from restored authoritative data.
The changed-body consumer is reset so its next read republishes all live bodies;
it cannot keep poses from the discarded future. Transient debug-contact output is
invalidated. Cumulative timing/traffic counters are diagnostics and are not rewound.
Completed gameplay contact reports remain readable immediately after restoration.

Capture/restore copy work uses one command submission and fence per operation when
there is data. It reads no physical payload back to CPU. Initial preparation,
capacity growth or queued inputs can add their existing work and allocations.
The byte counters expose actual device copies separately from upload/readback.

## Lifetime and errors

All operations require the store's owner thread. Checkpoints cannot be rebound to
another store. Source disposal releases every owned checkpoint before the compute
device; explicit checkpoint disposal releases buffers, authored arrays and its
source reference. Disposal during an executing copy rejects. Failed recapture
invalidates that checkpoint; after a restore begins replacing/copying live storage,
failure marks the store failed. A checkpoint cannot revive a failed store, and no
CPU replay is used. Validation rejection alone leaves the live world usable.

## Verification and limits

`ELECTRON2D_TEST_GPU_CHECKPOINT=1` runs `GPUPhysicsCheckpointTests` on the resident
Vulkan implementation; the complete GPU runner includes it too. Checks cover:

- Exact saved body bytes (including field values and sleep clocks) and completed
  contact records immediately after restoration, plus fresh changed-body publication.
- An eight-box contact trajectory replayed for 40 ticks within .02 scene units
  position/angle and .1 velocity units; tolerance permits floating-point ordering
  differences after rebuilding spatial data, not a different body population.
- Pin/motor warm state and a pending kinematic target replayed for 30 ticks within
  .005 position/velocity units; the target reaches its authored endpoint within .001.
- One-way episode restoration after a later branch grows contact/history/hash
  storage from one episode to more than 65, with replay within .001 units.
- A fast thin-wall CCD rebound replayed within .001 units, genuine sleep/idle skip,
  one-shot force consumption, pending future-input discard, multiple checkpoints,
  thread/configuration/resource/lifetime rejection and injected restore-copy failure.

The initial focused allocation run has 1,024 bodies in 512 colliding circle pairs
and one world pin, 64 warmup cycles and 128 measured capture+restore pairs. On
Linux/.NET 10/Vulkan/RTX 3090 Ti it measured mean .1759 ms, p50/p95/p99
.1660/.2165/.4097 ms, included waits .0946 ms per pair, zero owner/all-thread managed
bytes, 786,592 device-copy bytes, zero upload/readback bytes and two submissions.
The checkpoint reserves 393,296 device bytes and 265,216 authored-array bytes,
excluding object headers, driver/native overhead and the live world's own storage.
Log: `/tmp/e2d-checkpoint-final.log`. These are checkpoint-only timings, not a full
physics step, network round trip or rendered FPS.

This slice does not restore scene/server adapters, local RID lifetimes, authored
configuration changes or creation/destruction. It does not assign portable IDs,
serialize a world, separate predicted/confirmed events, capture CPU state, reconcile
across backends or provide the required multi-process network example. Public
capture/apply and lifecycle-aware local/wire histories must build on or replace
this internal boundary; none of those audit rows is closed by this component.

The internal [common-world layer](physics-space-checkpoints.md) now accompanies
resident history with scene/server backing state and observer histories. Portable authoritative
capture/apply and snapshots and networking remain open.

Game code can use the common layer through
[PhysicsCheckpoint](../classes/PhysicsCheckpoint.md), with the captured world tick.
This local storage remains device-owned and cannot be sent as a network snapshot.
