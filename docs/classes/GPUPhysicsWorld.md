# GPUPhysicsWorld

Last updated: 2026-10-08

**Declaration:** `internal sealed unsafe partial class GPUPhysicsWorld : IDisposable`

**Source:** [GPUPhysicsWorld.cs](../../src/Servers/Physics/GPUPhysicsWorld.cs), [GPUPhysicsWorld.Solver.cs](../../src/Servers/Physics/GPUPhysicsWorld.Solver.cs), [GPUPhysicsWorld.Collision.cs](../../src/Servers/Physics/GPUPhysicsWorld.Collision.cs), [GPUPhysicsWorld.BroadPhase.cs](../../src/Servers/Physics/GPUPhysicsWorld.BroadPhase.cs), [GPUPhysicsWorld.PairTable.cs](../../src/Servers/Physics/GPUPhysicsWorld.PairTable.cs), [GPUPhysicsWorld.Storage.cs](../../src/Servers/Physics/GPUPhysicsWorld.Storage.cs) · **Component:** [GPU physics](../components/gpu-physics.md)

## Internal flow

The developing GPU-world host currently executes velocity and delta-pose
integration, broad-phase tree traversal/built-in filters, circle/capsule/segment/polygon manifolds, contacts and revolute/wheel constraints. It retains the rendering device when available or creates a
windowless SDL compute device, with its own video-subsystem reference. Packed
80-byte body records and 32-byte integration/64-byte solver uniforms have matching compute layouts.
Contact/joint working records occupy 208/192 bytes. Contact uploads use 128-byte
inputs and optional 80-byte geometry overrides. GPU/transfer buffers grow together
before use and retain their capacity.

`FindBroadPhasePairs` packs the three current trees into threaded pre-order and
executes moved-proxy fat-AABB queries on GPU. Node/query records occupy
32/48 bytes; returned candidates are 4-byte proxy keys. Per-query retained capacities
permit a single warmed submission. Overflow returns its full count and grows/retries
the immutable query before publishing candidates to the owner-side user filter.
GPU built-in checks include self/moved/existing-pair deduplication, same-body/sensor
veto, 64-bit masks, signed groups and the smaller joint adjacency list. Pair-table
hashing uses split 32-bit arithmetic. Shape/joint records use 48/32 bytes; resident table slots and contact keys use 4/8 bytes.
CPU pair and custom-filter order is preserved, including deleted/reused proxy slots.
`BroadPhaseCandidateCount` and `BroadPhaseRetryCount` describe the latest query batch;
`BroadPhaseCandidateTotal`, upload/readback byte totals and `BroadPhaseProfileMS`
accumulate host timing and transfer accounting. No moved proxies means no submission.
Tree maintenance, user callbacks and contact creation remain CPU. The GPU lookup
table is updated from unique dirty contact IDs. Owner-side lifecycle notifications
are coalesced to final key values, then GPU removal/key replacement precedes atomic
parallel insertion. A quarter-table change budget triggers GPU tombstone rehash;
full key snapshots are limited to binding/capacity changes. Four status bytes are
validated after the fence before publishing candidates. Observer/source identity
and disposal prevent stale-world reuse. `PairTableSnapshotCount`,
`PairTableRebuildCount`, `PairTableUpdatedSlots` and `PairTableUploadBytes` report
this residency. Ordinary CPU worlds do not register the change observer.

`GenerateManifolds` packs geometry once per referenced shape, current pair
transforms and fat-proxy overlap, then generates all contact points in one
compute submission. Its packed shape/pair/result records occupy 144/48/80
bytes, plus a 32-byte history result. Feature matching and normal/tangent/rolling
warm-start reuse execute on GPU. Current contacts read the retained previous
solver buffer; cold/stale contacts upload 32-byte histories. Empty histories
need no upload. The complete result is validated before reaching material,
pre-solve and contact-transition processing in the managed world. Chain
segments are rejected explicitly; tree maintenance, user filters/contact creation
and sensor queries still belong to the CPU path.

`Integrate` requires the live world owner. It packs awake states, submits the
integration kernel, waits for the submission fence, verifies every returned
pose/velocity is finite, then publishes the result into the managed query mirror.
Per-contact signed source versions distinguish geometry from completed solver
state; graph copies preserve the source and world identity rejects foreign data.
`Solve` resolves generated manifolds by per-contact batch version and slot, then
selects retained feature IDs on GPU. Captured center offsets
preserve mass-relative anchors. Missing provenance or an internal geometry replacement has an explicit
upload; no CPU solve is substituted. Step reset, collision replacement and solve
consumption prevent reuse of stale buffers. Inputs/manifolds are read-only during
solving. `ResidentContactCount`, `UploadedManifoldCount` and `ContactUploadBytes`
record the most recent solve's transfer accounting. The managed collision
readback remains for material/event processing and the CPU state mirror.
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
