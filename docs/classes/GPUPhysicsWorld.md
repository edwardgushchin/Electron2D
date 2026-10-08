# GPUPhysicsWorld

Last updated: 2026-10-08

**Declaration:** `internal sealed unsafe partial class GPUPhysicsWorld : IDisposable`

**Source:** [GPUPhysicsWorld.cs](../../src/Servers/Physics/GPUPhysicsWorld.cs), [GPUPhysicsWorld.Solver.cs](../../src/Servers/Physics/GPUPhysicsWorld.Solver.cs), [GPUPhysicsWorld.Collision.cs](../../src/Servers/Physics/GPUPhysicsWorld.Collision.cs), [GPUPhysicsWorld.ContactUpdate.cs](../../src/Servers/Physics/GPUPhysicsWorld.ContactUpdate.cs), [GPUPhysicsWorld.ContactCreation.cs](../../src/Servers/Physics/GPUPhysicsWorld.ContactCreation.cs), [GPUPhysicsWorld.ContactRemoval.cs](../../src/Servers/Physics/GPUPhysicsWorld.ContactRemoval.cs), [GPUPhysicsWorld.Islands.cs](../../src/Servers/Physics/GPUPhysicsWorld.Islands.cs), [GPUPhysicsWorld.IslandGraph.cs](../../src/Servers/Physics/GPUPhysicsWorld.IslandGraph.cs), [GPUPhysicsWorld.IslandResidency.cs](../../src/Servers/Physics/GPUPhysicsWorld.IslandResidency.cs), [GPUPhysicsWorld.BroadPhase.cs](../../src/Servers/Physics/GPUPhysicsWorld.BroadPhase.cs), [GPUPhysicsWorld.Tree.cs](../../src/Servers/Physics/GPUPhysicsWorld.Tree.cs), [GPUPhysicsWorld.PairTable.cs](../../src/Servers/Physics/GPUPhysicsWorld.PairTable.cs), [GPUPhysicsWorld.Filters.cs](../../src/Servers/Physics/GPUPhysicsWorld.Filters.cs), [GPUPhysicsWorld.Storage.cs](../../src/Servers/Physics/GPUPhysicsWorld.Storage.cs) · **Component:** [GPU physics](../components/gpu-physics.md)

## Internal flow

The developing GPU-world host currently executes velocity and delta-pose
integration, GPU hierarchy construction/refit/traversal/built-in filters, contact identity allocation/initialization and adjacency construction/disjoint-contact removal, contact-driven island merging/unlinking and disconnected-island splitting, circle/capsule/segment/polygon manifolds, contacts and revolute/wheel constraints. It retains the rendering device when available or creates a
windowless SDL compute device, with its own video-subsystem reference. Packed
80-byte body records and 32-byte integration/64-byte solver uniforms have matching compute layouts.
Contact/joint working records occupy 208/192 bytes. Contact uploads use 128-byte
inputs and optional 80-byte geometry overrides. GPU/transfer buffers grow together
before use and retain their capacity.

`FindBroadPhasePairs` maintains an independent GPU hierarchy from dirty proxy
records. Morton keys, bitonic sorting and bottom-up bounds/type-mask reduction build
and refit a complete binary heap. Stackless traversal executes fat-AABB queries.
The CPU mirror supplies publication ranks, and cached Comparison/Span sorting
preserves its pair order before user callbacks. No CPU topology/internal bounds
are uploaded. Binding/capacity growth uploads all proxies; later updates are
32-byte final proxy records. Topology edits or half-capacity accumulated updates
trigger GPU spatial sorting. `TreeSnapshotCount`, `TreeRebuildCount`,
`TreeRefitCount`, `TreeUpdatedProxies` and `TreeUploadBytes` report cumulative work. Node/query records occupy
32/48 bytes; returned candidates are 4-byte shape IDs. Per-query retained capacities
permit a single warmed submission. Overflow returns its full count and grows/retries
the immutable query before publishing candidates to the owner-side user filter.
GPU built-in checks include self/moved/existing-pair deduplication, same-body/sensor
veto, 64-bit masks, signed groups and the smaller joint adjacency list. Pair-table
hashing uses split 32-bit arithmetic. Shape/joint records use 48/32 bytes; resident table slots and contact keys use 4/8 bytes.
Shape/joint metadata stays resident. Shape and joint lifecycle/filter observers
coalesce final dirty records, including predecessor links and endpoint shape
adjacency after joint deletion. A GPU scatter pass updates those records, then a
separate pass marks moved shapes from existing queries with the current epoch.
Movement alone uploads no stable filter metadata. Binding/capacity/observer changes
or epoch wrap require a snapshot; retries retain the same epoch.
`FilterSnapshotCount`, `FilterUpdatedShapes`, `FilterUpdatedJoints` and
`FilterUploadBytes` expose that work. These filter records are separate from
resident local geometry. Pair-pose and solver-joint inputs still upload per step.
CPU pair and custom-filter order is preserved, including deleted/reused proxy slots.
`BroadPhaseCandidateCount` and `BroadPhaseRetryCount` describe the latest query batch;
`BroadPhaseCandidateTotal`, upload/readback byte totals and `BroadPhaseProfileMS`
accumulate host timing and transfer accounting. No moved proxies means no submission.
CPU query/CCD tree mirrors, rank collection, user callbacks and contact/body adjacency publication remain CPU. The GPU lookup
table is updated from unique dirty contact IDs. Owner-side lifecycle notifications
are coalesced to final key values, then GPU removal/key replacement precedes atomic
parallel insertion. A quarter-table change budget triggers GPU tombstone rehash;
full key snapshots are limited to binding/capacity changes. Four status bytes are
validated after the fence before publishing candidates. Observer/source identity
and disposal prevent stale-world reuse. `PairTableSnapshotCount`,
`PairTableRebuildCount`, `PairTableUpdatedSlots` and `PairTableUploadBytes` report
this residency. Ordinary CPU worlds do not register the change observer.

`EnableContactCreation` installs an owner-thread creation callback and an ordered
ID-pool mutation observer. `CreateContacts` uploads post-filter ordered pairs;
GPU normalization, prefix sums and LIFO allocation produce contact IDs, unsigned
generations, shape/body identities, initial set/event flags and built-in material
values. The owner validates every result before claiming those IDs in the CPU
mirror and publishing common adjacency/pair links. Custom material callbacks
retain their original position and order. Full pool snapshots occur only after
binding/observer changes or growth; ordinary frees append in parallel, while
mixed external CPU allocation/free events replay serially. Slots/request/result/
mutation/state records occupy 32/64/80/48/32 bytes. `CreatedContactCount` and
`ContactPoolSnapshotCount` are cumulative; `ContactPoolUploadBytes` counts only
pool snapshots/mutations, excluding request and result transfers. Disposal
preserves another host's callbacks. CPU worlds retain native contact creation.

Creation also sorts endpoints by body ID/pair ordinal on GPU and computes next/
previous links, insertion-time counts and final heads. A separate pass commits
body heads/counts. The owner validates and publishes the ordered prefix, preserving
callback-visible adjacency. Contact links and body records use 16 bytes each;
CPU-origin link changes coalesce to 32-byte contact/body updates, including both
neighbors on deletion. Binding/observer/capacity changes snapshot both stores.
`ContactLinkUploadBytes` counts those uploads. The internal diagnostic
`ValidateContactLinks` synchronizes pending changes and compares all GPU link/head/
count records against the CPU mirror. External authoring removal and its graph/island changes still run
on CPU; normal frames do not perform that complete diagnostic readback.

The complete collision entry also removes disjoint contacts on GPU within its
existing submission/fence. Ascending-ID compaction and body endpoint sorting
preserve the CPU deletion order. Each body unlinks its selected edges in sequence;
independent bodies run in parallel. Slot generations survive free-list append.
Forty-eight-byte removal records retain every intermediate link/head/count prefix.
The owner validates the batch and publishes each removal among the original
state changes; the common mirror path preserves events and graph/island updates.
A completion hook verifies all records and pool counts. Own free/link notifications
are suppressed because the GPU already holds their results. External CPU authoring
edits still use journals. The numeric manifold-only entry remains non-destructive.
`RemovedContactCount` and `ContactRemovalReadbackBytes` are cumulative. A partial
publication failure poisons the world without replay and leaves disposal available.

`GenerateManifolds` keeps local shape geometry resident. Creation/destruction and
primitive/material/hit-event edits invalidate a slot, and the first overlapping pair referencing it
packs the final record. A GPU scatter pass installs changed geometry before the
manifold pass in the same submission. Unused geometry waits for a real reference;
movement and filter edits do not upload it. World/observer changes or buffer
growth clear residency, and only a validated completed batch commits pending
slots. Disposal detaches only its own observer. `ResidentGeometryCount`,
`UploadedGeometryCount` and `GeometryUploadBytes` describe the latest batch;
`GeometryCacheResetCount` is cumulative. Current pair transforms and fat-proxy
overlap are still packed on CPU before generating all points in one submission. Its packed shape/pair/result records occupy 160/64/80
bytes, plus a 32-byte history result. Shape records include surface material and
hit-event inputs; the pair carries center-of-mass offsets. Integer identity headers avoid float ID conversion. Simulations carry their generation through graph copies to avoid a separate native-contact lookup during packing. Integrated pairs address resident contact slots by ID/generation, with stale generations rejected before geometry reads; isolated numeric probes retain direct shape-ID input. Feature matching and normal/tangent/rolling
warm-start reuse execute on GPU. Current contacts read the retained previous
solver buffer; cold/stale contacts upload 32-byte histories. Empty histories
need no upload. The pure `GenerateManifolds` entry retains raw shape-relative
anchors for numeric checks and the CPU-update control.

`UpdateContacts` is the GPU world's integrated entry. The collision kernel mixes
built-in materials (including rough/absorbent rules), classifies disjoint/touching/
started/stopped/hit flags, prunes optional speculative points and shifts anchors
to each center of mass. It returns another 16-byte material vector; the six compact
state bits share the manifold's persistence word. Retained CPU workers publish body indices/mass/surface caches and per-worker
change bitsets; the existing ordered union feeds graph/event publication. A
step-scoped completion marker bypasses the CPU collision-update task. Custom material and
pre-solve callbacks execute on the owner, with their original per-contact order.
Built-in contacts without pre-solve hooks publish in parallel; hooked contacts
publish on the owner after workers join. Worlds using custom material callbacks
keep publication on the owner to preserve callback order. A veto clears touching/hit/start state and rolling history; optional pruning for
hooked contacts follows the callback so its deepest-point input is unchanged.
`UpdatedContactCount` accumulates complete contact updates. Constraint coloring, authoring island changes, event publication and the first manifold readback still remain managed. Chain
segments are rejected explicitly; CPU tree mirrors, user filters, contact/body links
and sensor queries still belong to the CPU path.

`EnableIslandChanges` batches the ordered contact-state changes after material/pre-solve
callbacks finish. GPU weighted union keeps the original larger-island winner and
endpoint-A tie break, concatenates body/contact/joint lists and applies contact
insertions/removals in contact-ID order. A second pass resolves every member's final
root in parallel. Merges do not walk the smaller island's entire member lists.
The owner validates identities, list bounds/order/counts and membership before
publication. During the original contact loop, sleeping sets wake at the original
points and removed island IDs are freed in the original order. Completed lists
publish before solver validation; no game callback observes the intermediate graph.
Authoring changes outside collision retain their immediate CPU path.

The graph stays resident between batches. Body/contact/joint creation, deletion,
authoring merges/unlinks and CPU/GPU splits journal unique record keys. Packing
reads the final value after each authoring operation, including adjacent list
nodes. A lost observer, a different native island pool or graph-buffer growth
requires one complete snapshot; ordinary batches scatter only changed records.
Prepared GPU merges suppress redundant native island journal writes, while contact
lifecycle changes still invalidate their final slots. Sleep/set-index changes do
not change the graph records. Journal writes follow the backend's serialized graph
mutation phases, including the single CPU split worker when that control is used.

Resident islands/members use 64/16 bytes, ordered operations 32 bytes, journal
updates 80 bytes, status/uniforms 16/32 bytes. GPU passes mark changed slots and
compact output into keyed 68-byte island or 20-byte member records. Cleared merge
parents cannot survive slot reuse. The owner patches retained mirrors, validates
the complete graph and publishes only returned changes. Output capacity persists;
an overflow grows it and reruns only output gathering from the completed GPU graph,
never union, contact insertion or removal. No extra physics work is replayed.

`IslandGraphSnapshotCount`, `IslandGraphUploadBytes`, `IslandGraphReadbackBytes`
and `IslandGraphReadbackRetries` expose residency and actual scheduled transfers,
including spare compact-output capacity and retries. `IslandChangeCount`,
`MergedIslandCount` and `IslandGraphTransferBytes` retain their cumulative meanings.
No membership changes means no submission; pending journals wait for the next real
batch. Weighted union and body/joint list splicing remain serial on GPU; contact
list construction/removal now uses the parallel ordered-tree path described below.
The CPU still validates its full retained mirror. CPU/GPU split currently uses a separate input/output path
and journals its publication into this resident graph.

Validation uses independent packed CPU liveness/membership flags keyed by the
same record IDs. Snapshots and authoring journals refresh those flags; ordered
CPU contact operations change expected membership and successful backend island
release clears liveness. GPU output never supplies the expectations used to
validate itself. Preparation no longer scans all backend contact objects, and
member validation reads dense flags/records instead of dereferencing every
backend body, contact and joint. The complete list traversal, bounds, previous-link,
cycle, count and membership checks remain. Dead records claiming to be alive and
live unlinked members claiming an island are rejected before publication.

Partial publication failure poisons the world; failed-world teardown detaches
managed resources and releases raw storage in bulk without traversing incomplete
lists or capturing uncommitted motion. Rebinding/disposal resets only this host's
callbacks, and world reset clears the journal observer.

Contact lists preserve order through a temporary concatenation tree. Each initial
island list is a leaf; a started contact becomes the first element of a node whose
children are the prior winner and loser lists. Weighted union still selects exactly
the original winners and frees the same IDs. Consecutive insertions into the same
component retain its root/head/count in registers, flushing before a real merge or
component change. The root shortcut is valid only for that current live winner. Removing other contacts commutes with
this list construction, so removal counters/marks run after union in parallel.
Removal results carry the final surviving root; only link results require the
intermediate merge winner for native publication.

Ping-pong pointer jumping resolves subtree followers, then disjoint invocations
connect original-list tails and inserted contacts. Another bounded pointer-jump
sequence skips runs of removed contacts. Independent passes install surviving
next links, group heads, inverse previous links and tails before clearing removed
members. Head/previous/tail writes have one writer per destination; shared removal
counters use atomics. Bounds are determined by actual batch links/removals, not a
fixed iteration cap. Scratch reuses extra space in the retained dirty buffer;
this adds no storage binding or per-frame managed allocation. The shared error
flag is read atomically once per workgroup and broadcast through a barrier.

`IslandGraphProfileMS` accumulates six host wall-time phases when internal physics
profiling is enabled: preparation, command recording/upload, submit/fence wait,
readback decoding, validation and final mirror publication. GPU wait includes queue
and transfer time; it is not a hardware timestamp for one shader. Consuming native
merge records in the contact-state loop is outside these six phases.

`EnableIslandSplitting` installs an owner-thread split callback. Dirty awake
islands upload ordered body/contact/joint adjacency. GPU minimum-seed label
propagation with pointer shortening finds components, excluding static bodies
from connectivity and honoring contact/joint eligibility. Convergence is checked;
unconverged immutable input retries within a body-count bound without CPU fallback.
GPU prefix sums reserve disjoint stack regions, then each component builds its
exact DFS body/contact/joint list. Independent components run in parallel; ordering
within a single component is serial. This is an explicit large-component ceiling.

The owner validates complete, bounded lists and membership before reserving native
mirror capacity or changing live state. Existing island IDs are allocated in seed
order, all lists are imported, and the old base ID is freed last. CPU no longer
performs connectivity discovery for this path. Scheduled splits run on the owner
instead of a solver worker; explicit sleep requests use the same callback. Clean
or sleeping islands keep their existing no-op behavior. Authoring island merges, constraint
coloring, sleep decisions/transfer and query mirrors remain CPU work. Body/edge
inputs occupy 16/32 bytes; member/group/status results use 16/48/16 bytes and
uniforms 32 bytes. `SplitIslandCount`, `SplitComponentCount` and
`SplitConvergenceBatches` count executed splits, resulting components and submissions.
Buffers/maps retain capacity, but new larger topology can allocate during preparation.

`Integrate` requires the live world owner. It packs awake states, submits the
integration kernel, waits for the submission fence, verifies every returned
pose/velocity is finite, then publishes the result into the managed query mirror.
Per-contact signed source versions distinguish geometry from completed solver
state; graph copies preserve the source and world identity rejects foreign data.
`Solve` resolves generated manifolds by per-contact batch version and slot, then
selects retained feature IDs on GPU. Captured center offsets preserve mass-relative anchors for the pure entry;
complete GPU updates already shifted them and pass zero offsets to preparation. Missing provenance or an internal geometry replacement has an explicit
upload; no CPU solve is substituted. Step reset, collision replacement and solve
consumption prevent reuse of stale buffers. Inputs/manifolds are read-only during
solving. `ResidentContactCount`, `UploadedManifoldCount` and `ContactUploadBytes`
record the most recent solve's transfer accounting. The managed collision
readback remains for custom callbacks, graph/event publication and the CPU state mirror.
`ResidentHistoryCount`, `UploadedHistoryCount` and `HistoryUploadBytes` report the
most recent collision submission. Source slots are published only after successful
solve completion; older or explicitly invalidated snapshots use their CPU history.

`Solve` uploads raw constraint inputs and computes their effective masses, softness,
anchor frames and warm-start state on GPU. It retains states across all four substeps, preserves colored/overflow
ordering, then publishes poses, prepared joint frames and contact/joint impulses after one
submission fence. `Dispose` releases buffers, pipelines, device reference and video reference on
that owner; repeated disposal is inert. Partial native-resource creation releases
already created resources before throwing. No handles or backend types are public.

## Verification and limits

[GPUPhysicsTests](../../tests/Electron2D.Tests/GPUPhysicsTests.cs) selects real SDL
compute execution through `ELECTRON2D_TEST_GPU_PHYSICS=1`. CPU integration is the
numeric comparison source. Current tests do not establish a full GPU physics world,
cross-device determinism or sustained application frame rate. See the component
status for remaining world stages and backend selection/fallback work.

Contact inputs/working records include two virtual endpoint velocities. Preparation,
warm start, solving and restitution add those velocities without changing the
integrated body state. Surface conformance includes static endpoints in colored
and serial overflow constraints.

Working contact records remain 208 bytes. Endpoint-vector xyz still stores virtual
linear/angular velocity; their two reserved w components now accumulate signed
tangential impulse for points one and two. Feature IDs remain in Impulses1/2.W for
resident history matching. Publication includes the tangent totals and native solve
epoch; shader numeric validation checks these fields before exposing results.
PhysicsContactImpulseTests verifies world momentum and outer-frame sums.
