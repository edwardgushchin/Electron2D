# GPUPhysicsWorld

Last updated: 2026-10-08

**Declaration:** `internal sealed unsafe partial class GPUPhysicsWorld : IDisposable`

**Source:** [GPUPhysicsWorld.cs](../../src/Servers/Physics/GPUPhysicsWorld.cs), [GPUPhysicsWorld.Solver.cs](../../src/Servers/Physics/GPUPhysicsWorld.Solver.cs), [GPUPhysicsWorld.Collision.cs](../../src/Servers/Physics/GPUPhysicsWorld.Collision.cs), [GPUPhysicsWorld.ContactUpdate.cs](../../src/Servers/Physics/GPUPhysicsWorld.ContactUpdate.cs), [GPUPhysicsWorld.ContactCreation.cs](../../src/Servers/Physics/GPUPhysicsWorld.ContactCreation.cs), [GPUPhysicsWorld.ContactRemoval.cs](../../src/Servers/Physics/GPUPhysicsWorld.ContactRemoval.cs), [GPUPhysicsWorld.Islands.cs](../../src/Servers/Physics/GPUPhysicsWorld.Islands.cs), [GPUPhysicsWorld.BroadPhase.cs](../../src/Servers/Physics/GPUPhysicsWorld.BroadPhase.cs), [GPUPhysicsWorld.Tree.cs](../../src/Servers/Physics/GPUPhysicsWorld.Tree.cs), [GPUPhysicsWorld.PairTable.cs](../../src/Servers/Physics/GPUPhysicsWorld.PairTable.cs), [GPUPhysicsWorld.Filters.cs](../../src/Servers/Physics/GPUPhysicsWorld.Filters.cs), [GPUPhysicsWorld.Storage.cs](../../src/Servers/Physics/GPUPhysicsWorld.Storage.cs) · **Component:** [GPU physics](../components/gpu-physics.md)

## Internal flow

The developing GPU-world host currently executes velocity and delta-pose
integration, GPU hierarchy construction/refit/traversal/built-in filters, contact identity allocation/initialization and adjacency construction/disjoint-contact removal, disconnected-island splitting, circle/capsule/segment/polygon manifolds, contacts and revolute/wheel constraints. It retains the rendering device when available or creates a
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
count records against the CPU mirror. External authoring removal and graph/island changes still run
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
`UpdatedContactCount` accumulates complete contact updates. Contact graph/island
mutation, event publication and the first manifold readback still remain managed. Chain
segments are rejected explicitly; CPU tree mirrors, user filters, contact/body links
and sensor queries still belong to the CPU path.

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
or sleeping islands keep their existing no-op behavior. Island merges, constraint
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
