# GPU physics implementation status

Last updated: 2026-10-08

[ADR 0054](../decisions/physics.md#adr-0054) selects a full GPU world alongside the
managed CPU compatibility backend. Implementation is in progress. The existing
public physics API still selects CPU; no production GPU/fallback selector has
been exposed yet.

The required selector lets a game developer deliberately choose CPU/Box2D.NET
or GPU for the application's goals. CPU is a first-class backend even on a
GPU-capable device. Automatic startup fallback is a separate configurable
policy; requested and actual selection must be observable. Physics backend
choice is independent of the renderer. Live migration of an existing world
is not yet implemented or claimed.

`GPUPhysicsWorld` executes velocity/delta-pose integration and the colored and
overflow contact solver: warm start, speculative/soft bias, one/two contact
points, friction, tangent speed, rolling resistance and restitution. Revolute
and wheel constraints execute on GPU, including springs, motors and limits.
These implement the current PinJoint and GrooveJoint backend constraint roles.
The spring-joint force preflight/application still belongs to CPU world setup.

Broad-phase search now uses an independent resident GPU hierarchy. The GPU orders
proxy centers by a two-dimensional Morton key, bitonic-sorts key/shape-index pairs,
and builds a complete binary heap by bottom-up AABB/type-mask reduction. Empty
slots remain invisible; duplicate Morton keys use shape indices as a stable tie.
Traversal uses implicit parent/child indices without a shader stack. The
[Morton-order construction principle](https://developer.nvidia.com/blog/thinking-parallel-part-iii-tree-construction-gpu/)
is a reference; this implementation uses a balanced heap, not that article's
radix-tree topology or performance results.

Only initial binding/capacity growth uploads all 32-byte proxies. Creation,
destruction, movement, enlargement and category changes mark unique shape slots;
subsequent batches upload their final records. Hooks live at the dynamic-tree
proxy boundary, including the direct bullet-enlargement lane. GPU refitting updates
leaf/internal bounds. Topology edits or accumulated updates reaching half the leaf
capacity trigger a spatial rebuild entirely from resident proxies. Unchanged
proxies need neither upload nor refit. Bitonic sorting is O(n log² n); its rebuild
cost remains a measured optimization boundary.

CPU trees remain as query/CCD mirrors and supply leaf publication ranks. The GPU
tree receives no CPU topology or internal bounds. Before user filters and contact
creation, validated candidate shape IDs are sorted by those ranks with a cached
comparison over a Span. This preserves exact CPU pair/callback order despite GPU
spatial rebuilds. Node/proxy records use 32 bytes, query records 48 bytes and
returned candidate IDs 4 bytes. Tree snapshot/rebuild/refit/update/upload counters
expose the work performed. Refitting is counted per batch, including batches that
also rebuild. The CPU mirror/rank walk is not removed or claimed to be GPU work.

Each query retains a power-of-two candidate capacity. A normal batch uses one
submission/readback; overflow reports the full count and repeats the unchanged
GPU query after growing storage, before invoking any user filters or creating
contacts. There is no density cap or truncated-pair fallback. The GPU also applies
self/moved-pair deduplication, existing-contact lookup, same-body and sensor veto,
64-bit category/mask and signed group filtering, and joint collision veto. The
shared pair hash uses split 32-bit arithmetic, so this does not require shaderInt64.
Joint filtering walks the smaller body adjacency list. User filters remain on the
owner after readback, followed by GPU contact identity allocation and initialization.
Body/contact adjacency is built and retained on GPU; disjoint-contact unlink and ID
release also execute there. CPU query/CCD trees, mirror/event publication, external
authoring edits and their constraint coloring remain managed; contact-driven island merging/unlinking
and disconnected-island splitting execute on GPU. The GPU maintains its own
resident lookup table for those contacts. Buffers and per-shape capacity hints
retain their peak size; new topology/capacity can allocate outside warmed checks.

Filter inputs use 48 bytes per shape and 32 bytes per joint and stay resident.
Owner-side shape creation/destruction/filter edits and joint creation/destruction/
collision-policy edits journal unique slot IDs. Removing a joint also journals its
predecessors whose next links change and the attached bodies' shape records, which
cache adjacency head/count. Packing samples final values, so intermediate edits and
ID reuse cannot leave stale records. Disabled shapes are included. Binding changes,
lost observers, buffer growth and moved-epoch wrap require a full snapshot; warm
batches upload only changed records. GPU scatter applies these records before a
separate pass marks moved shapes from the existing query buffer with the current
epoch. Pure movement does not upload stable filters or joints, and the retry uses
the same epoch. `FilterSnapshotCount`, `FilterUpdatedShapes`, `FilterUpdatedJoints`
and `FilterUploadBytes` report this boundary. This retains broad-phase metadata;
resident manifold geometry and per-step pair poses/solver-joint inputs use separate buffers.

The pair table itself is never uploaded: 4-byte GPU slots
reference retained 8-byte contact keys. The initial world binding and capacity
growth upload keys once; later batches upload only 16-byte changed-contact records.

Contact creation/destruction marks unique dirty contact IDs on the owner. The
journal samples their final identities immediately before submission, so repeated
reuse or create/destroy between queries cannot leave intermediate pairs behind.
A GPU pass removes old slot identities and writes final keys; a separate pass
inserts live keys using 32-bit atomic compare/exchange. Tombstones preserve probe
chains. At most half the table is live; accumulated changes reaching a quarter of
table capacity trigger a GPU clear/reinsert from resident keys. This avoids a
full CPU snapshot and bounds tombstone accumulation. No 64-bit atomics or spinning
entry locks are used. A 4-byte status readback validates bounded probe operations
before any candidate publication. Failed submissions do not commit cache identity
or clear the journal; the ordinary world failure path prohibits CPU replay.

World/broad-phase identity, observer ownership and buffer capacity gate residency.
Switching worlds or replacing the observer forces a fresh snapshot. Disposal
unhooks the observer, and world reset clears it. CPU worlds have no observer.
`PairTableSnapshotCount`, `PairTableRebuildCount`, `PairTableUpdatedSlots` and
`PairTableUploadBytes` expose cumulative internal accounting. The internal
cumulative `BroadPhaseProfileMS` measures host packing, command recording/uploads,
submit/fence/readback, and result validation/user filtering/list publication.
It excludes the later contact creation submission and CPU mirror publication. Cumulative upload/readback
bytes and candidate totals make transfer volume visible without per-frame logs.

`CreateContacts` consumes the ordered pair list after user filtering. Its GPU
prefix scan skips unsupported shape pairs without disturbing order, reserves IDs
from a resident LIFO free stack or its next-ID counter, increments each slot's
unsigned 32-bit generation, and initializes canonical shape/body identities,
event flags, initial awake/disabled set and built-in mixed materials. The owner
validates the complete batch before importing these identities into the CPU pool
and publishing the common contact/body links. Custom material callbacks retain
their original owner-thread position and order. CPU worlds keep native creation.

The pool observes every external allocation/free in order; unlike the pair-key
journal, these events cannot be coalesced. Ordinary frees append in parallel on
GPU; rare mixed CPU allocations/frees replay serially. Initial binding, capacity
growth or observer replacement snapshots the native pool and slot generations.
Slots occupy 32 bytes, request/result records 64/80 bytes, mutation records 48
bytes and pool state 32 bytes. `CreatedContactCount`, `ContactPoolSnapshotCount`
and `ContactPoolUploadBytes` are cumulative; the last counts pool snapshots and
mutation uploads, excluding creation request/result transfers. World reset and
host disposal detach only owned callbacks. A failed creation step poisons the
world through the existing failure path without CPU replay.

New contact adjacency is constructed on GPU in the same creation submission.
Endpoints are bitonic-sorted by body ID and original pair ordinal, preserving
exact insertion order independently of GPU scheduling. Workgroups sort 64-entry
tiles in shared memory and finish each global merge locally, reducing separate
compute passes. Each endpoint resolves
its predecessor/successor and insertion-time count; the final head/count is
committed in a separate pass so readers cannot race the update. The scan buffer
is reused as endpoint-sort scratch after ID allocation. Bitonic sorting is
O(n log² n); binary search of each body segment adds O(log n) work per endpoint.

Resident contact links and body head/count records occupy 16 bytes each. The
owner imports GPU next links/counts in original pair order, so custom material
callbacks see the same intermediate adjacency as the CPU path. The complete
result is checked against ordered scratch heads/counts before live publication.
An optional contact-link observer journals external CPU creation/removal and
both neighboring edges; repeated changes coalesce by contact/body identity.
Warm batches upload only final 32-byte dirty records, with no full adjacency
snapshot. Binding/observer/capacity changes restore the snapshot. This retains
GPU adjacency while external authoring removal and graph/island mutation remain shared.
`ContactLinkUploadBytes` counts snapshots and delta payloads separately from the
ID-pool counter. `ValidateContactLinks` is an internal diagnostic that synchronizes
pending changes and reads the complete GPU lists back for exact mirror comparison;
it is exercised by tests, not the normal frame path.

Disjoint contacts are removed in the existing collision command buffer, after
manifold generation and before the same readback fence. The GPU marks selected
slots, compacts them in ascending contact-ID order, and reuses endpoint sorting
to group removals by body. Bodies run in parallel; each body's removals execute
in ID order, preserving every intermediate previous/next/head/count state. This
serial lane per body is an explicit high-degree-body performance limit. The GPU
updates resident adjacency, invalidates slots without changing their generations
and appends freed IDs in the same order as the CPU pool.

Each removed contact returns a 48-byte identity/two-endpoint record. The owner
validates batch membership, generations, ordering and bounds before publication,
then imports each record at its original position among contact state changes.
The shared publication path checks the exact current prefix and retains event,
island/graph and wake behavior. A completion callback verifies that the entire
batch was consumed and pool counts agree. GPU-origin free/link notifications do
not reupload their already-resident results. External body/shape/filter/joint edits
continue through the CPU authoring path and its delta journals. The pure numeric
manifold entry does not remove contacts. `RemovedContactCount` and
`ContactRemovalReadbackBytes` expose cumulative accounting. Failure rejects replay;
publication failure still permits disposal of the CPU mirror and GPU resources.

Contact geometry is generated on GPU for all nine registered pair families
among circles, capsules, two-sided segments and convex polygons (up to eight
vertices, including rounded polygons). SAT, edge clipping and vertex contacts
preserve feature IDs and the speculative distance used for warm starting.
Chain segments are explicitly unsupported by this development entry. Geometry
records stay resident by shape ID rather than being duplicated per contact (160
bytes, including surface material). Shape creation/destruction, primitive,
material and per-shape/per-body hit-event edits
invalidate that slot. Geometry is packed lazily on the first overlapping pair
that references an invalid slot; unused/sleeping geometry needs no upload.
A scatter pass in the existing collision pipeline installs only changed records
before the manifold pass, within one submission/fence. Repeated references and
intermediate edits coalesce to one final record. World/observer changes and
buffer growth invalidate all cached slots; failed batches do not mark pending
records resident. Movement and collision filters do not change these inputs. `ResidentGeometryCount`, `UploadedGeometryCount` and `GeometryUploadBytes`
report each batch, while `GeometryCacheResetCount` is cumulative. Pair poses still
upload each batch: each pair occupies 64 bytes (including center offsets), its
returned manifold 80 bytes and the mixed material 16 bytes. The owner waits for
the collision fence and validates the batch before publication. Integrated pairs
reference resident contact slots by integer ID and generation; stale generations
are rejected before geometry access. The isolated numeric entry can still pass
shape IDs directly. Shape/pair identity headers now use integer fields without
float conversion. The integrated
GPU entry mixes native/default and Electron2D rough/absorbent materials, classifies
contact transitions/hit flags, prunes optional speculative points and shifts
anchors to centers of mass. Custom material and pre-solve callbacks remain on the
owner, preserving per-contact callback order and veto behavior. Contacts requiring
a pre-solve callback retain the original deepest point; optional pruning follows
the hook. Retained workers copy body metadata and GPU results into the CPU mirror
and per-worker contact bitsets; their ordered union is unchanged. Hooked contacts
publish on the owner after workers join, and custom material callbacks keep all
publication on the owner. The step-scoped completion marker skips the CPU collision-update task;
authoring constraint coloring, authoring island mutation and event publication still use the managed path. Feature-ID matching and reuse of normal/tangent/rolling impulses now
execute in the collision shader. It reads the previous completed GPU solver
buffer directly when that contact's source is current; cold or older sources
upload a compact 32-byte history record. Empty histories need no upload.
The matched 32-byte result remains on GPU for preparation and is also read back
for the existing managed contact snapshot. Integrated GPU contacts already have
mass-relative anchors; the pure numeric entry retains raw anchors for CPU update
comparisons. Owner-side pre-solve veto does not repeat feature matching. A veto
clears rolling state, preventing a later contact from reviving stale impulses.

The generated manifold buffer now remains available to constraint preparation.
Each contact carries a source version and slot through graph copies: positive
versions address generated geometry, negative versions address a completed solve.
World identity and the latest submission prevent using an overwritten solver buffer;
the owner/step marker prevents cross-world or old-step reuse. Feature IDs resolve
point reordering or pruning directly on GPU. Center-of-mass offsets are captured
at the collision pose; complete GPU updates pass zero offsets to preparation
because the collision kernel already applied them. The solver uploads a 128-byte input (body indices/masses,
materials, retained impulses, source, offsets and surface velocities) instead of retransmitting the
working contact geometry. Missing provenance or an explicit internal geometry replacement uses a separate
80-byte override; this includes sleeping contacts awakened after collision
collection. Reset, another collision batch and consumption invalidate reuse.
The input/manifold buffers bind read-only during solving. The first manifold
readback and managed event processing still remain; this is partial
residency, not elimination of the collision synchronization fence.

Dirty awake islands now split through GPU connectivity discovery and list
construction. The host packs their ordered raw adjacency, including non-touching
contacts and disabled joints. GPU label propagation monotonically lowers each
body's component seed, with pointer shortening between passes. Static endpoints
belong to constraints but do not connect dynamic groups. Eligibility matches the
shared contact/joint rules. A convergence check retries immutable input when
necessary, bounded by the number of bodies; there is no CPU discovery fallback.
Prefix sums reserve separate stack regions and components build their original
DFS body/contact/joint ordering independently. Ordering remains serial within
one component, an explicit performance limit for a large connected group.

All output lists are checked for membership, cycles, bounds, reciprocal order,
counts and complete coverage before any live mutation. The owner reserves mirror
capacity, allocates new island IDs in the original seed order, imports all lists
and frees the base ID last. Sleeping/clean islands do no GPU work. The solver
routes GPU split callbacks through the owner; CPU worlds retain their worker task.
Explicit body sleep uses the same split callback. Input/output storage and ID maps
are retained; first use and topology growth remain outside warmed allocation claims.
The split stage does not move graph coloring, sleeping decisions or set
transfers to GPU. Contact-driven merges now use the batch stage described below;
authoring merges remain CPU work. `SplitIslandCount`, `SplitComponentCount` and
`SplitConvergenceBatches` expose the work actually exercised.

The internal `PhysicsSpace.EnableGPUSolver` development entry submits all four
substeps as one GPU command buffer. Body/contact/joint state remains resident
between stages and is published once after its fence. Packed records are 80,
208 and 192 bytes; solver uniforms occupy 64 bytes. GPU/transfer buffers retain
capacity. Colored groups execute in parallel without shared dynamic-body writes;
overflow preserves serial joint/contact order. The earlier
`EnableGPUIntegration` entry remains a numeric development check.

Contact and joint preparation also runs on GPU before the substeps: effective
masses, relative restitution velocity, static/contact softening, warm-start
reset, local anchor frames, and pin/wheel spring and motor coefficients. Filter
joint base tuning is preserved; its spring-force application still runs in the
managed preflight. Solved contact impulses publish directly to their manifolds,
without a CPU SIMD preparation/store pass. Joint frames and coefficients publish
with impulses for subsequent queries and finalization.

The tree, contact creation, solver and manifold callbacks run on the world owner. CPU tree mirrors,
user callbacks, contact/body links, sleep/CCD finalization and queries remain in the managed
backend; GPU contact kernels run through the owner, with retained workers publishing the CPU mirror; CPU worlds retain their contact-update workers. There is no production
backend selector yet. A GPU failure drains pending CPU tasks, releases scratch
ownership and rejects replay while permitting world disposal. This hybrid stage
is not the completed GPU backend.

The shared `World` runtime also permits last-resource disposal after a GPU
failure; rebinding continues to reject that failed world. This is checked by
an injected owned-world solver failure followed by space-identity expiration.

When a GPU renderer exists, the physics world retains its SDL device handle;
otherwise it creates a compute device without a window. Its video-subsystem
reference and safe native resource handles have independent lifetimes. A CPU
world does not create compute resources. Native checks exercise both a retained
GPU-render device and a separate compute device beside the compatibility
renderer, then dispatch after renderer teardown. Complete startup fallback
still needs integration verification.

`ELECTRON2D_TEST_GPU_PHYSICS=1` selects the native compute checks. These exercise
gravity, forces/torque, damping, locks, speed caps and rotation at dispatch sizes
1, 63, 64, 65, 4,097 and 65,536. GPU/CPU floating point results use a relative
tolerance; this does not establish bit-identical cross-device simulation or
complete world parity. The development world check compares 120 falling/contact
ticks with CPU, reads contacts and direct queries, invokes post-solver callbacks
and verifies faulted-world disposal without replay. Eight prepared dispatches
at each tested size allocate zero managed bytes in the checked Linux/Vulkan run.
The solver conformance check compares 64 raw four-substep batches, including
colored/serial contacts, static/kinematic bodies, zero hertz/inertia, disabled
warm starting, contact softening and motor/limit/spring joint equations. Extra
batches cross 64-invocation preparation boundaries and contain up to 1,025
colored contacts/joints plus overflow constraints; eight prepared
solver submissions also allocate zero managed bytes. Refined divide/root
operations correct small GPU arithmetic errors before stiff constraints amplify
them. A kinematic wheel regression failed the existing 2e-5 tolerance before
refinement and passes without changing that tolerance. This does not establish
bit-identical cross-device execution.
Resident-path checks compare body states and contact impulses against CPU
solving at 3, 63, 64, 65, 66, 67 and 1,027 contacts. They exercise nonzero mass
centers, graph copies, feature reordering/pruning, explicit overrides and older batches,
pre-solve veto, overwritten/empty/consumed batches and step reset. Eight warmed
collision/update/solve cycles allocate zero all-thread managed bytes on the
checked Linux/Vulkan path. Transfer counters assert exactly 128 bytes per
resident contact plus 80 bytes per geometry override.
History checks compare GPU feature matching with the actual CPU contact updater
across all nine pair families, including reordered, unmatched and duplicate old
features, rolling impulses and empty old manifolds. Resident checks exercise six
collision/solve cycles, explicit invalidation, capacity growth, an unprocessed
contact, foreign-world snapshots, cleared history and a failed preparation after
solver-buffer growth. Starting a new solve invalidates its predecessor before
a buffer can be replaced or overwritten. Eight warmed complete
cycles allocate zero all-thread managed bytes on the tested Linux/Vulkan device.
`ResidentHistoryCount`, `UploadedHistoryCount` and `HistoryUploadBytes` expose
internal submission accounting; sleeping contacts whose source buffer was reused
seed their current CPU snapshot rather than reading an obsolete GPU slot.
Manifold checks compare 4,290 pairs across nine supported shape combinations,
including rotated/offset and rounded geometry, exact contact feature IDs,
one/two/empty contacts and an unused arena tail. Eight warmed submissions at
each dispatch size allocate zero all-thread managed bytes. World checks also
inject a manifold-stage failure and verify disposal without CPU replay.
The first maximum-Smash GPU-solver profile (65,537 bodies, 32 warmup/64
measured headless ticks) averaged 132.88 ms, p95 185.80 ms, with zero
all-thread managed bytes after storage reservation followed the prepared world
capacity. A phase-instrumented repeat averaged 140.36 ms: packing 5.15 ms,
command recording/upload 2.45 ms, submission/fence wait 22.73 ms, publication
3.91 ms. The wait includes transfers and execution; these are host timings,
not GPU timestamp queries. This hybrid path does not establish an application
speedup, sustained frame rate or complete GPU-world acceptance. An earlier
contended run grew body/contact buffers during wakeup and allocated 32,245,152
bytes across 64 ticks; that growth was corrected before the clean profiles.
After GPU manifold integration, the same 32-warmup/64-sample headless workload
averaged 152.15 ms, p95 219.32 ms, with zero owner/all-thread managed bytes.
There were 65,537 total bodies; awake bodies increased from 6,435 to 41,097
during impact propagation, so this is not an all-awake steady-state benchmark.
The collision stage averaged 63.85 ms, including geometry packing, another
submission/readback and managed contact updates. The hybrid transfer path
remains expensive; this result does not establish an application speedup.
With GPU constraint preparation and refined arithmetic, a subsequent isolated
32-warmup/64-sample run averaged 168.27 ms, p95 228.22 ms, again with zero
owner/all-thread managed bytes. Solver time was 50.42 ms and collision time
78.74 ms; awake bodies increased from 6,425 to 41,122 among 65,537 total bodies.
This remains a hybrid throughput result, not a sustained FPS or speedup claim.
Resident-geometry transfer was compared with forced raw geometry upload in two
consecutive runs of the same final binary (32 warmup/64 measured ticks, headless,
65,537 bodies). Resident/raw averaged 184.58/188.83 ms, p95 231.87/271.46 ms;
both allocated zero owner/all-thread managed bytes. Both ended with the same
state hash `13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`
and 6,425→41,122 awake bodies during the measured interval. Across 4,969,124
solver contacts, residency uploaded 477,035,904 bytes instead of 874,565,824
(45.45% less); all contacts reused geometry. Host packing/recording averaged
3.97/1.92 ms with residency and 4.84/2.62 ms with forced uploads. This short
pair establishes lower transfer volume, not an end-to-end speedup. Collision
processing still averaged 91.84/92.53 ms. The earlier full-geometry CPU comparison
candidate cost 9.07 ms to pack and was replaced by batch provenance carried with
the contact. The diagnostic profile flag
`ELECTRON2D_SANDBOX_PROFILE_UPLOAD_MANIFOLDS=1` disables reuse only in the test
host; no production backend setting or CPU solver fallback is added.
Artifacts: `bin/physics-sandbox/profile-Release-gpu-resident-final.json` and
`profile-Release-gpu-resident-final-upload.json` (ignored local evidence).
With GPU feature matching and resident solved history, the same headless
32-warmup/64-sample maximum-Smash run averaged 196.38 ms, p95 266.21 ms,
with zero owner/all-thread managed bytes. It reused 4,883,353 history records
without any history upload; empty new histories required no input record.
There were 4,969,124 solver contacts. The state hash and awake-body progression
matched the preceding profile exactly. Collision/solver phases averaged
100.69/53.16 ms. The 32-byte matched-history readback still serves the managed
contact mirror; this stage does not demonstrate an application speedup.
Artifact: `bin/physics-sandbox/profile-Release-gpu-warm-matching.json`.
The real native window run (32 warmup/64 measured frames, 65,537 bodies)
reached 5.32 FPS. Its zero-allocation gate failed: three frames allocated 4,992
managed bytes each outside the measured physics phases and renderer. The same
rare allocation signature was already observed in the CPU-host profile; its
source remains unresolved. This run is retained as failed evidence and does
not support a whole-frame zero-allocation claim.
Native allocation accounting, other devices/platforms and visual acceptance
remain unverified.

Remaining work: remove CPU tree mirrors/rank dependency and adjacency mirror dependency,
move authoring constraint coloring and island changes to GPU, remove full CPU graph-validation scans and the separate split transfers, complete external edit handling, implement chain manifolds and GPU contact transitions without full manifold/history readback, spring
force setup, island sleep/set transfer and CCD/shape finalization, complete query/event/state contracts, independent backend selection
and startup fallback, native end-to-end scene checks and performance profiling.

Constant linear/angular surface velocity is shared with the CPU constraint path.
Its endpoint data adds 32 bytes to contact inputs/working records (128/208 bytes
now); it affects normal/friction/rolling/restitution response without pose motion.
PhysicsSurfaceVelocityTests exercises surface response, queries, wakeup, character
carry and kinematic additivity on CPU/GPU. The standalone multi-world test first
stalled during SDL/GTK video reinitialization on this Wayland host; its harness now
retains SDL for the full run, as the general GPU runner does. This does not establish
unrestricted native video teardown/reinitialization support. Historical profile
sizes/timings above describe their recorded binaries.

Typed PhysicsServer transform/velocity/sleep state and axis operations share the
CPU/GPU body runtime. PhysicsServerStateTests verifies raw kinematic target travel,
scene/server/direct-state consistency, detach/reentry and owner/phase guards on the
Vulkan solver path. Its 64 warmed state/read/step cycles allocate zero all-thread
managed bytes; GPU completeness and foreign-device/native allocation remain open.

Normal and signed tangent impulses now cover all solver substeps, including warm
starting. The two reserved endpoint-vector w components hold tangent totals without
changing 128-byte input or 208-byte working layouts; resident history feature IDs
are unchanged. Publication tags the native solve epoch. The shared reporting path
aggregates multiple kinematic intervals, retains short-lived contacts and selects
deepest capped contacts before callbacks. PhysicsContactImpulseTests verifies CPU/GPU momentum, paired signs,
sleep reset, cap bounds/ties and zero warmed all-thread managed allocation. One full
GPU run passed all physics checks and then aborted inside GTK/libdecor while opening
a renderer-lifetime test window; a fresh identical run passed completely. This
intermittent native-window failure was not fixed by the contact change.

The broad-phase oracle compares final pairs and custom-filter callback order against
CPU tree queries at 0, 1, 63, 64, 65, 257 and 4,097 bodies. It includes all body
modes, compound shapes, sensors, 64-bit masks, positive/negative groups, joint veto,
existing pairs, single/both moved proxies, destroyed/reused slots, refiltering,
teleport and tree rebuild. A fully overlapping 257-body case forces GPU output
and the shared pair arena beyond their original estimates. Forty warmup passes
precede sixteen measured unchanged-topology passes with zero all-thread managed
bytes. Pair-stage failure after a completed GPU query poisons the world, rejects
replay and still permits disposal. These checks are also available through
`ELECTRON2D_TEST_GPU_BROAD_PHASE=1`.

Four sequential maximum-Smash runs of the same final binary compared GPU tree
traversal with the existing CPU worker traversal (GPU solver/manifolds in both).
Each used 65,537 bodies, 32 warmup and 64 measured headless ticks. Run order was
GPU A, CPU A, CPU B, GPU B; no build, formatter or other test ran concurrently.

| Pair traversal | Whole step mean | Step p95 | Pair stage mean | All-thread managed bytes |
| --- | ---: | ---: | ---: | ---: |
| GPU A | 176.47 ms | 274.19 ms | 20.29 ms | 0 |
| CPU A | 165.50 ms | 255.79 ms | 11.00 ms | 0 |
| CPU B | 161.18 ms | 245.59 ms | 10.71 ms | 0 |
| GPU B | 172.90 ms | 263.16 ms | 19.29 ms | 0 |

All four final state hashes were
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`,
with the same 6,425→41,122 awake-body progression. This hybrid GPU stage is
slower than CPU pair traversal on this host. Full-tree packing/transfer,
readback and serial managed filtering/contact creation remain on the path;
these timings do not isolate GPU execution or prove which individual cost
accounts for the difference. The initial exact-count/two-submission version
was replaced by retained per-query capacities to avoid a mandatory second
fence; that change alone does not establish a speedup. The unmodified baseline
at `266ecc05` averaged 188.80 ms (pairs 13.18 ms), illustrating why the final
same-binary repeated comparison is the relevant result.

The test-only `ELECTRON2D_SANDBOX_PROFILE_CPU_PAIRS=1` selects CPU traversal for
this comparison; it does not add public backend selection or recovery fallback.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-pairs-final-{a,b}.json`;
initial evidence is `profile-Release-gpu-pairs-before.json` and
`profile-Release-gpu-pairs-initial.json`. Shader binary SHA-256:
`265c455c256b04207dc1b9655b90997eb483f72c9d24ae2ef524bc65a85a9855`.

With built-in GPU pair filtering, the dense oracle verifies exactly 32,894 initial
pairs for 257 overlapping bodies (two joint vetoes), and no existing-contact
candidates on later unchanged passes. It also toggles collide-connected and destroys
a joint while retaining CPU pair and custom-filter order. The expanded focused
runner passes with the same warmed zero-managed-allocation budget.

The first filter implementation packed shapes during tree traversal and performed
one moved-set hash lookup per shape. Its packing phase averaged 12.31 ms; switching
to sequential shape slots and marking moved flags from the move array removed that
work. Four subsequent same-binary profiles used the same 65,537-body,
32-warmup/64-measurement headless workload, sequentially GPU A/CPU A/CPU B/GPU B.
No build, formatter or other test ran concurrently.

| Pair traversal/filtering | Whole step mean | Step p95 | Pair stage mean | All-thread managed bytes |
| --- | ---: | ---: | ---: | ---: |
| GPU A | 166.78 ms | 259.57 ms | 14.07 ms | 0 |
| CPU A | 165.34 ms | 259.33 ms | 10.94 ms | 0 |
| CPU B | 166.01 ms | 259.00 ms | 10.92 ms | 0 |
| GPU B | 164.06 ms | 250.94 ms | 13.32 ms | 0 |

All hashes and awake-body progressions match the preceding profiles. GPU host
packing averaged 5.46/5.08 ms; command recording/uploads 1.35/1.29 ms;
submit/fence/readback 3.71/3.47 ms; result processing 0.94/0.88 ms. The later
managed contact creation loop is outside those four host phases but inside the
pair-stage total. These are host timings, not GPU timestamp measurements.
Each GPU run transferred 1,072,996,000 input bytes and 156,052,512 readback bytes
across 64 measured steps and returned 496,638 filtered candidates. Tree and source
pair-table residency remain unfinished. The pair stage is still slower than CPU;
the whole-step spread does not establish a sustained end-to-end speedup or 60 FPS.

Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-filters-final-{a,b}.json`;
the initial packing evidence is `profile-Release-gpu-filters-a.json`. Current
`PhysicsBroadPhase.comp.spv` SHA-256:
`3fda139685616a94bdc828ee29022949877a74f62f0ce8d96faa1df36d3266d4`.

Resident-pair tests include late attachment to an already populated CPU world,
contact ID swaps with repeated intermediate reuse, bulk removal/recreation and
GPU-only tombstone rebuild without another snapshot. Sixteen warmed unchanged
query passes upload zero pair-table bytes. Sixty-four active two-slot churn passes
after thirty-two warmups upload exactly 2,048 bytes (two 16-byte final updates per
pass), with zero all-thread managed allocations. The full pair/custom-filter
order still matches the CPU oracle; the existing GPU failure test verifies world
poisoning and disposal after a completed pair query.

Maximum-Smash residency profiles use the same sequential GPU A/CPU A/CPU B/GPU B
comparison, 65,537 bodies, 32 warmup and 64 measured headless ticks. No builds,
formatters or other tests ran concurrently.

| Pair traversal/lookup | Whole step mean | Step p95 | Pair stage mean | All-thread managed bytes |
| --- | ---: | ---: | ---: | ---: |
| Resident GPU A | 161.13 ms | 245.10 ms | 12.30 ms | 0 |
| CPU A | 165.62 ms | 257.18 ms | 10.71 ms | 0 |
| CPU B | 165.22 ms | 258.76 ms | 11.07 ms | 0 |
| Resident GPU B | 163.07 ms | 257.46 ms | 12.09 ms | 0 |

All four state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`,
with the same awake-body progression. Both GPU runs applied 938,070 final dirty
slots using 15,009,120 upload bytes, zero full snapshots and four GPU table
rebuilds. The old per-step table snapshots would upload 536,870,912 bytes for
this workload; total broad-phase input fell from 1,072,996,000 to 551,134,208 bytes
(48.64% less). Readback is 156,052,844 bytes including status, with the same
496,638 candidates. GPU host phase means A/B were packing 5.05/4.69 ms,
recording/uploads 0.74/0.63 ms, submit/fence/readback 2.96/3.23 ms, and result
processing 0.87/0.78 ms. Managed contact creation follows those four phases.
The pair stage remains slower than CPU; these short profiles do not establish
sustained application FPS, foreign-device speed or native allocation totals.
At that stage, tree/shape/joint uploads remained the next residency boundary.

Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-resident-pairs-{a,b}.json`.
Current shader SHA-256 values:
`PhysicsPairTable.comp.spv` = `1a561f7e51416d87a41fbfc4993d041bac9db4f2df9da2d7ab1d54a73e7cb598`;
`PhysicsBroadPhase.comp.spv` = `47a2914804786b51f7ff7f7d30e441af0b3ab958c6e640c8025c35184f813cdb`.

Tree checks retain the existing CPU pair/custom-filter oracle through independent
GPU builds/refits. They include empty, one/two-visible-proxy, sparse/dense and
coincident-center layouts, shape-slot reuse, type masks, category/filter edits,
teleport and CPU-only tree rebuild. Unchanged queries perform no tree upload or
refit. A high-key dynamic proxy moves between disjoint regions while all user
filters veto contact creation, keeping spatial candidates observable every tick;
32 warmups precede 64 measured movement/refit steps. Those steps upload exactly
2,048 bytes (64 changed 32-byte proxies), include a GPU spatial rebuild, take no
full snapshot and allocate zero all-thread managed bytes. Direct tree enlargement
also exercises the notification used by bullets. The first custom-IComparer sort
allocated 2,048 bytes in sixteen warmed oracle passes; cached Comparison/Span sorting
removed those allocations without weakening the budget or pair-order comparison.

Maximum-Smash tree profiles keep the same sequential GPU A/CPU A/CPU B/GPU B
comparison, 65,537 bodies, 32 warmup and 64 measured headless ticks. No builds,
formatters or other tests ran concurrently.

| Pair search hierarchy | Whole step mean | Step p95 | Pair stage mean | All-thread managed bytes |
| --- | ---: | ---: | ---: | ---: |
| Resident GPU tree A | 163.88 ms | 225.68 ms | 12.96 ms | 0 |
| CPU A | 162.68 ms | 254.43 ms | 10.58 ms | 0 |
| CPU B | 169.27 ms | 252.87 ms | 10.79 ms | 0 |
| Resident GPU tree B | 166.30 ms | 258.60 ms | 12.91 ms | 0 |

All four hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`,
with the same awake-body progression. Each GPU run performed 16 spatial rebuilds
and 64 refit batches from 1,204,915 changed proxies, uploading 38,557,280 tree
bytes with zero full snapshots. Total broad-phase input fell from 551,134,208
to 321,239,648 bytes; readback stayed 156,052,844 bytes and filtered candidates
496,638. Pair-table residency is unchanged. Host phases A/B were packing/ranking
5.46/5.43 ms, recording/uploads 0.54/0.53 ms, submit/fence/readback 3.50/3.56 ms,
and result validation/order/user filtering 0.75/0.73 ms. Managed contact creation
follows those phases. The independent GPU hierarchy removes full tree uploads;
it does not establish a speedup over CPU search or sustained application FPS.
At that stage, CPU mirrors/ranking and full shape/joint metadata packing remained overhead.

Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-resident-tree-{a,b}.json`.
Current shader SHA-256 values:
`PhysicsTree.comp.spv` = `4dc874c03b9237e988f49a53ffe613e6ae83201066eb3746b1a9c3d24090a2ac`;
`PhysicsBroadPhase.comp.spv` = `3f2d02c640faa6bf2b2806199bbcbfb6dcddd7f23332f1b1ab926d5bb3e446e6`.

The native category setter's Debug guard previously tested child fields that alias
leaf userData, rejecting valid nonzero shape IDs. The focused
`ELECTRON2D_TEST_GPU_TREE_CATEGORY=1` Debug check reproduced that assertion and now
passes with allocated-leaf validation; it also verifies observer detachment on
tree destruction. The Release GPU oracle directly disables/restores a leaf's
category after enlargement and compares complete pairs/callback order with CPU.


## Resident broad-phase filters (2026-10-08)

The focused GPU/CPU exact-order oracle covers incremental mask/group edits without
proxy recreation, sensor/body slot reuse, disabled edits and reenable, body type
changes, joint creation, middle-link deletion, recycled joint IDs, live collision
policy toggles, body destruction, capacity growth, lost observers and moved-epoch
wrap. Existing sparse/dense/high-bit filters, mixed body types and overflow retries
remain covered. Unchanged and moving batches upload zero filter bytes. After 32
warm ticks, 64 refilter ticks upload exactly 64 final 48-byte shapes (3,072 bytes);
64 joint policy edits upload one final 32-byte joint and only endpoint shape
metadata per tick. Both intervals allocate zero managed bytes across all threads.
World destruction clears both observers. The full GPU suite and injected failed
broad-phase interval preserve world failure/teardown behavior.

`PhysicsFilters.comp.spv` SHA-256:
`bd2eedb21a4340463b560a9aee35f5ff0bff1e328afc49f66b6fc20c8e194f12`.

Sequential Linux/Vulkan headless runs used the same 65,537-body Smash fixture,
32 warm and 64 measured steps, with no builds/tests overlapping measurement.
Only pair traversal changes between modes; manifolds and solving remain GPU:

| Mode | Whole step mean | Step p95 | Pair stage mean | All-thread managed bytes |
| --- | --- | --- | --- | --- |
| GPU pairs A | 160.63 ms | 242.38 ms | 10.39 ms | 0 |
| CPU pairs A | 164.90 ms | 256.78 ms | 10.80 ms | 0 |
| CPU pairs B | 164.58 ms | 257.02 ms | 10.82 ms | 0 |
| GPU pairs B | 160.74 ms | 251.80 ms | 10.17 ms | 0 |

All four runs retain state SHA-256
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
The awake population grows from 6,425 to 41,122 during impact propagation; this is
not a steady all-awake interval. Both GPU runs have zero filter snapshots, changed
records and filter upload bytes. Total broad-phase input falls from the preceding
resident-tree profile's 321,239,648 to 119,897,696 bytes (62.68% less); readback
remains 156,052,844 bytes and candidates 496,638. Pair/tree delta counts are unchanged.
Host packing/ranking averages 3.54/3.51 ms, versus the preceding 5.46/5.43 ms.
This removes the full filter scan/upload; the complete step remains about 161 ms
and does not meet 60 FPS. CPU contact transitions, pair/constraint input packing,
readback, queries/CCD and publication ranking still remain in the developing backend.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-resident-filters-{a,b}.json`.


## Resident shape geometry (2026-10-08)

`ELECTRON2D_TEST_GPU_GEOMETRY=1` checks three contacts sharing two geometry slots,
all four editable primitive types (including type changes), rejected degenerate
capsules, movement/rotation, material/filter changes, repeated edits, disabled
edits, empty/separated batches, shape ID reuse, buffer growth, lost observers,
failed-batch retry and disposal without detaching another host's observer.
The existing nine-family CPU manifold oracle also requires zero geometry upload
on repeated batches. After 32 warm edits, 64 measured edit batches upload exactly
one final 144-byte shape each and allocate zero managed bytes across all threads.
The test-only `ELECTRON2D_SANDBOX_PROFILE_UPLOAD_GEOMETRY=1` control invalidates the
cache before each batch, forcing upload of every referenced shape for comparison
within the same binary. This differs from the former full shape-slot upload,
which also transferred unused geometry.

`PhysicsCollide.comp.spv` SHA-256:
`ccd5cd9ffad33c3fc2f75c730bf088c79785071e36d2ff830fda3a85d5e2aa79`.

Four sequential Linux/Vulkan headless profiles used 65,537 Smash bodies with 32
warmup and 64 measured steps; no builds/tests overlapped measurement. `resident`
retains geometry and `upload` forces each referenced shape to upload every batch:

| Mode | Whole step mean | Step p95 | Collision stage mean | Geometry input bytes |
| --- | --- | --- | --- | --- |
| resident A | 161.23 ms | 249.26 ms | 75.31 ms | 5,153,472 |
| upload A | 159.65 ms | 246.02 ms | 74.46 ms | 214,963,056 |
| upload B | 162.55 ms | 253.59 ms | 76.13 ms | 214,963,056 |
| resident B | 160.39 ms | 253.69 ms | 74.10 ms | 5,153,472 |

Both resident runs upload 35,788 first-use/invalid slots (5,153,472 bytes), reuse
1,457,011 referenced slots and reset no caches during measurement. Both forced
runs upload 1,492,799 records (214,963,056 bytes) and reset 64 times. Geometry input
falls by 97.60%; both modes still upload current pair poses and read back manifolds
for managed contact processing. All four intervals allocate zero owner/all-thread
managed bytes, retain awake counts 6,425 through 41,122 and state SHA-256
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
The intervals cover impact propagation, not steady all-awake or rendered frames.
Run-to-run timing overlaps: this verifies reduced transfers and geometry residency,
not a demonstrated whole-step speedup or 60 FPS. Contact lifecycle, pose/constraint
packing, readback, query/CCD mirrors and the remaining complete-backend obligations
are unchanged. Artifacts: ignored
`bin/physics-sandbox/profile-Release-gpu-geometry-{resident,upload}-{a,b}.json`.


## GPU contact updates (2026-10-08)

The complete collision callback now computes ordinary contact updates on GPU and
publishes their CPU query/graph mirror through retained workers. User material and pre-solve
callbacks remain CPU code. Existing event bitset order, island linking, graph
coloring and solver-set changes remain shared; this stage does not move the entire
contact lifecycle to GPU or remove its readback. `GenerateManifolds` remains the
pure numeric entry and the test-only CPU contact-update control.

`ELECTRON2D_TEST_GPU_CONTACT_UPDATES=1` compares 129 contacts directly with
`b2CollideTask` across default/rough-absorbent/custom materials, speculative modes,
pre-solve allow/veto, hit flags and previous touching states. It compares flags,
change bits, geometry, persistence/impulses, body caches, materials and callback
identity/order/deepest-point inputs. Live friction, restitution, full material,
user material and shape/body hit-event edits are checked. After 32 warm edits,
64 measured batches allocate zero managed bytes across all four worker threads
and the owner, including per-worker change-bit union and owner-only callbacks.
The expanded matrix caught an unmasked polygon-type check after adding material
flags to the shape header; all geometry type reads now mask those flags.
The CPU test-only speculative pruning also had a duplicated first-point condition;
it now tests the second separation, with a direct regression and GPU parity checks.

`PhysicsCollide.comp.spv` SHA-256 for this stage:
`d87c2e694f57320b0acab6a6c6887c02ce76adc729ccf0da6070c884d69bc617`.

The first candidate published every GPU result serially on the owner. Maximum
Smash profiles (`{gpu,cpu}-contact-updates-{a,b}`) exposed a regression: GPU whole
steps averaged 184.01/184.81 ms versus CPU-contact control 165.13/162.25 ms; the
collision stage was 93.60/93.58 ms versus 76.97/76.02 ms. Those results rejected
serial publication for ordinary contacts. The final path restores the existing
worker scheduler for copies/bitsets while keeping contact arithmetic on GPU and
custom callbacks on the owner. The initial parallel-publication check
`gpu-contact-publication-a` returned 160.14 ms/step, 74.55 ms collision, zero managed
bytes and the same state hash; the complete final comparison follows below.

The final sequential comparison uses the same 65,537-body Smash impact interval,
32 warm and 64 measured steps on Linux/Vulkan, with no concurrent builds/tests.
`ELECTRON2D_SANDBOX_PROFILE_CPU_CONTACTS=1` keeps GPU geometry/solving but switches
contact updates to the original CPU task within the same binary:

| Mode | Whole step mean | Step p95 | Collision stage mean | All-thread managed bytes |
| --- | --- | --- | --- | --- |
| GPU updates A | 161.79 ms | 234.18 ms | 74.60 ms | 0 |
| CPU updates A | 164.33 ms | 234.19 ms | 77.12 ms | 0 |
| CPU updates B | 160.24 ms | 235.54 ms | 75.62 ms | 0 |
| GPU updates B | 159.12 ms | 233.80 ms | 74.51 ms | 0 |

Each GPU run updates 12,078,146 contacts on GPU; the CPU control reports zero.
All four runs retain state SHA-256
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`,
awake counts 6,425 through 41,122 and zero owner/all-thread managed allocation.
Geometry/material input is 5,726,080 bytes (35,788 records at the new 160-byte
stride). A 64-byte pair now carries center offsets, and complete GPU updates
read back another 16-byte material vector per contact. Normal-word spare bits
carry the six contact state flags alongside two persistence bits without growing
the manifold result. This is executable GPU contact arithmetic with a CPU mirror,
not elimination of the synchronization/readback boundary. The remaining roughly
160 ms whole step does not meet 60 FPS; graph/island mutation, contact creation,
queries/CCD, sleep and other full-backend requirements still remain.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-contact-final-{a,b}.json`.

## GPU contact identities and creation (2026-10-08)

`ELECTRON2D_TEST_GPU_CONTACT_CREATION=1` compares empty and populated worlds with
mixed static/kinematic/dynamic bodies and all registered shape creation roles.
It checks contact IDs, unsigned generation wrap, initial flags/set/material,
body adjacency and exact custom-material callback order on the owner. Late
attachment, repeated destroy/recreate, external CPU allocation/free sequences,
observer loss, buffer growth and replacing/disposing GPU hosts are exercised.
Sixteen measured churn cycles after 32 warmups allocate zero all-thread managed
bytes and take no new pool snapshot. A stale generation is rejected by the GPU
collision slot lookup; graph copies preserve simulation identity. Creation tests
include chain identities without claiming chain manifold support. The full GPU
suite injects failure after a completed creation batch and verifies failed-world
query/replay rejection and disposal; ordinary CPU tests retain native creation.

The test-only `ELECTRON2D_SANDBOX_PROFILE_CPU_CREATION=1` detaches creation and
pool observers together. That control uses native creation and direct shape-ID
collision input; all other GPU stages remain enabled. An earlier exploratory
control only disabled creation, leaving serial import of native mutations into
the GPU pool (`cpu-contact-create-a`); it is not the native-stage baseline.

The first identity-packing candidate fetched every contact generation through
the native contact-object table. A maximum-Smash probe cost 173.30 ms per step,
including 87.51 ms collision, while the detached native-creation control cost
157.97/74.16 ms. Simulations now carry their generation through graph copies,
removing that additional random lookup without weakening slot-generation checks.
The follow-up probe (`gpu-contact-identity-b`) returned 158.44 ms per step.

Final sequential Linux/Vulkan maximum-Smash profiles use 65,537 bodies, 32
warmup and 64 measured headless diagnostic steps, in GPU A/CPU A/CPU B/GPU B
order. No build, formatter or other test runs concurrently:

| Creation/identity mode | Whole-step mean | p95 | Pair stage mean | Collision mean | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: | ---: |
| GPU A | 158.04 ms | 233.39 ms | 11.48 ms | 73.28 ms | 0 / 0 |
| CPU A | 156.96 ms | 248.44 ms | 10.95 ms | 72.06 ms | 0 / 0 |
| CPU B | 161.35 ms | 251.91 ms | 10.99 ms | 75.01 ms | 0 / 0 |
| GPU B | 158.51 ms | 249.11 ms | 11.33 ms | 72.49 ms | 0 / 0 |

Each GPU interval creates 496,638 contacts, takes zero pool snapshots and uploads
21,196,176 pool-mutation bytes, plus 31,784,832 request bytes and the same number
of result bytes (64 bytes per candidate each way). Pool status readbacks are
additional. The CPU control reports zero pool/creation work. All four state hashes
match `13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`, with
the same 6,425→41,122 awake-body progression. These profiles do not demonstrate
a sustained whole-step speedup or 60 FPS. CPU adjacency/graph/islands, query/CCD
mirrors, per-step packing and readback remain. The interval captures an impact
propagating through a sleeping wall, not a steady all-awake or native-window run.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-contact-create-final-{a,b}.json`.

## GPU contact adjacency (2026-10-08)

The creation oracle now reads back every retained GPU contact link and body
head/count after each topology edit. Custom material callbacks hash the complete
intermediate body/contact adjacency, verifying the ordered prefix they observe.
Body deletion/ID reuse, disable/enable/type changes and loss of the link observer
are included alongside the existing contact-ID churn and lifetime cases. Unchanged
queries upload zero link bytes; 16 warmed churn/diagnostic cycles allocate zero
all-thread managed bytes. Full CPU/GPU stepping, queries, callback order and
failed-interval disposal remain required and separate from this direct oracle.

The initial implementation submitted every bitonic stage separately. Sequential
maximum-Smash probes (`{gpu,cpu}-contact-links-{a,b}`) measured GPU pair stages at
14.41/14.71 ms versus the native-creation control at 10.72/11.14 ms. GPU whole steps
were 161.59/165.36 ms versus 159.83/158.86 ms. This version was replaced by 64-entry
shared-memory tile sorts and local merge tails; global cross-tile comparisons
retain separate barriers and exact ordering. Both versions use the existing
scan storage as scratch rather than adding a separate sort buffer.

Final sequential Linux/Vulkan profiles use the same 65,537-body scene, 32 warmup
and 64 measured headless diagnostic steps, with no concurrent build/test/formatter.
The CPU-creation control disables contact allocation/adjacency and their observers;
all other GPU stages remain enabled:

| Creation/adjacency mode | Whole-step mean | p95 | Pair stage mean | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: |
| GPU A | 163.94 ms | 260.42 ms | 13.91 ms | 0 / 0 |
| CPU A | 158.60 ms | 253.84 ms | 10.97 ms | 0 / 0 |
| CPU B | 158.10 ms | 253.21 ms | 10.71 ms | 0 / 0 |
| GPU B | 163.27 ms | 256.06 ms | 13.69 ms | 0 / 0 |

Each GPU interval creates 496,638 contacts and uploads 73,049,984 bytes of dirty
adjacency records without a full pool/adjacency snapshot. Request records remain
64 bytes; creation results now use 80 bytes including two next links and two
insertion-time counts. Both controls report zero adjacency upload. All four
state hashes remain `13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`,
with 6,425→41,122 awake bodies and zero managed allocation. The GPU creation/adjacency
stage still costs more than CPU creation in this hybrid world. Smaller dispatch
batches do not establish a whole-step speedup or 60 FPS. CPU-driven deletion,
graph/islands, query/CCD mirrors, packing and readback remain; this is neither a
steady all-awake workload nor native-window FPS acceptance.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-contact-links-final-{a,b}.json`.

## GPU disjoint-contact removal (2026-10-08)

`ELECTRON2D_TEST_GPU_CONTACT_REMOVAL=1` compares worlds with 0/1/2/63/64/65/129/257
dynamic bodies plus a static collider. Dense clusters and a high-degree static
surface repeatedly separate and reconnect, exercising sparse/recycled contact
IDs and adjacent removals in both directions. The oracle checks all body links,
pool free-ID order/generations, graph slots/island membership, begin/end event
order, pre-solve callback-visible topology and the complete retained GPU adjacency
readback. Sixteen separation/reconnection cycles after 32 warmups allocate zero
all-thread managed bytes and perform zero pool or adjacency uploads/snapshots.
External CPU body destruction remains covered through its journal path.

The full GPU suite additionally injects failure after the first removal is
published to the CPU mirror. Replay is rejected and disposal succeeds. Running
this oracle before fresh CPU/integration worlds exposed a pre-existing reset
bug: raw world Clear left integration, solver and manifold callbacks attached.
They now reset alongside every other GPU hook and the retained step context,
which also releases body arrays and the GPU-owner reference; explicit sibling-hook lifetime
checks and the complete suite verify world-slot reuse without a disposed host.

The test-only `ELECTRON2D_SANDBOX_PROFILE_CPU_REMOVAL=1` keeps GPU creation,
adjacency, manifolds/contact updates and solving while selecting the original
CPU disjoint-removal path and its mirror journals. This isolates the new removal
stage; it is not a public backend or failure-fallback selector.

Final sequential Linux/Vulkan maximum-Smash runs use 65,537 bodies, 32 warmup
and 64 measured headless diagnostic steps. No build, formatter or other test
runs concurrently; order is GPU A/CPU A/CPU B/GPU B:

| Removal mode | Whole-step mean | p95 | Collision mean | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: |
| GPU A | 163.27 ms | 258.41 ms | 74.85 ms | 0 / 0 |
| CPU A | 167.39 ms | 262.63 ms | 76.50 ms | 0 / 0 |
| CPU B | 163.03 ms | 257.63 ms | 74.05 ms | 0 / 0 |
| GPU B | 165.07 ms | 268.28 ms | 76.80 ms | 0 / 0 |

Each GPU interval removes 443,964 contacts and reads back 21,310,272 removal bytes.
Pool and adjacency delta uploads are both zero, with no snapshot. The CPU control
reports zero GPU removals and uploads 21,196,176 pool bytes plus 73,049,984 adjacency
bytes. CPU journals upload on the following collision, so transfer intervals do
not count exactly the same boundary contacts as current-step removal counters.
All four state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`, with the same
6,425→41,122 awake-body progression. Lower transfer volume does not establish a
consistent whole-step speedup or 60 FPS. CPU graph/island work, query/CCD mirrors,
packing and other readbacks remain; the fixture is an impact through a sleeping
wall, not a steady all-awake or native-window FPS measurement.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-contact-remove-{a,b}.json`.

## GPU disconnected-island splitting (2026-10-08)

`ELECTRON2D_TEST_GPU_ISLANDS=1` compares CPU and GPU splits for 1/2/63/64/65/257/1,025
bodies. Fixtures mix touching/non-touching static contacts, a kinematic member,
disabled joints, revolute/wheel/filter joints, cycles and reordered seed lists.
Breaking bridges produces up to 205 components; a separate 1,025-body case stays
connected after a redundant constraint is removed. The oracle checks exact native
island IDs/free-stack order, body/contact/joint lists, graph slots and solver sets,
including explicit sleep/wake and a four-worker world's real scheduled split.
Rebinding/disposal/world reset preserve hook ownership. The full GPU suite injects
failure after island publication, then verifies replay rejection and disposal.

After 24 warmup break/split/reconnect cycles, 16 measured cycles allocate zero
managed bytes across all threads in every fixture. All splits converge in one
submission. `ELECTRON2D_TEST_GPU_ISLANDS_PROFILE=1` records split-only wall time,
including packing, device work, readback, validation and mirror publication:

| Bodies | Resulting components | CPU split mean | GPU split mean | Managed bytes, all threads |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 0.00012 ms | 0.09689 ms | 0 |
| 65 | 13 | 0.00183 ms | 0.21534 ms | 0 |
| 257 | 52 | 0.00743 ms | 0.27101 ms | 0 |
| 1,025 | 205 | 0.03873 ms | 0.40951 ms | 0 |
| 1,025 | 1 | 0.04763 ms | 1.88037 ms | 0 |

This stage establishes GPU connectivity/list construction, not a speedup. The
current host round trip dominates small islands and serial per-component DFS
ordering limits large connected islands. Graph inputs are packed each split;
the island graph is not yet resident. Artifact: ignored
`bin/physics-sandbox/island-split-profile.json`.

Sequential maximum-Smash GPU/CPU-split-control probes use 65,537 bodies, 32 warmup
and 64 measured headless diagnostic steps. The control selects the original split
with `ELECTRON2D_SANDBOX_PROFILE_CPU_SPLITS=1`; other GPU stages remain enabled.
Whole-step means are 160.13/161.49 ms, with zero owner/all-thread managed allocation
and the same state hash
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
The measured impact interval executes **zero splits**, so these numbers only check
the existing path and cannot establish the new stage's performance. Artifacts:
ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-island-split-a.json`.
CPU island merging, graph coloring, sleeping/set transfer, CCD/query mirrors and
public backend selection remain unfinished; this is not full-backend or FPS acceptance.


## GPU contact-driven island graph (2026-10-08)

The internal GPU world now batches all contact-driven island membership changes
after custom material/pre-solve callbacks and before contact-state publication.
A GPU weighted union preserves the larger-island winner, endpoint-A ties, exact
list concatenation and ordered contact insertion/removal. It records only merge
parents during the ordered pass; a parallel pass remaps body/contact/joint IDs.
This eliminates per-merge traversal of every smaller-island member. The initial
form used two compute passes in one submission; resident maintenance and compact
output add the passes described below without a wait per contact.

The host checks the full returned graph before live changes. The original contact
loop still wakes sleeping sets and frees each removed island ID at its original
point, preserving solver-set swap indices and ID-pool order. The final lists are
imported before solver validation. User callbacks see only completed state;
internal graph publication remains owner-thread-only. CPU worlds and authoring
joint/body changes retain their existing immediate graph path. There is no GPU
failure fallback or replay. Hook ownership survives rebind/dispose/world reset.

A partial merge failure can leave native membership pointing at a freed island.
Failed-world release now invalidates managed views and detaches body/area/joint
owners without traversing raw graph links or capturing partially advanced motion.
The space releases the remaining backend storage in bulk. Release guards preserve
owner/stepping restrictions while queries, mutation and replay remain rejected.

`ELECTRON2D_TEST_GPU_ISLAND_GRAPH=1` extends the existing contact churn oracle to
check every island descriptor, list, member identity, free-ID order, graph slot and
solver-set index. Repeated removal/split/reconnection forces actual merges after
warmup. Separate fixtures check unequal joint islands, sleeping-set wake chains,
stopped-touching contacts that remain alive, callback ownership and partial-merge
teardown. The full GPU suite also injects failure after completed graph publication.

The initial graph implementation crossed the host boundary as a complete snapshot
per nonempty batch. It is superseded by the resident stage below. Initial sequential
Linux/Vulkan maximum-Smash runs use 65,537 bodies, 32 warmup and 64 measured headless
diagnostic steps with no concurrent build/test/formatter. The test-only control
`ELECTRON2D_SANDBOX_PROFILE_CPU_ISLAND_GRAPH=1` retains other GPU stages:

| Island graph | Whole-step mean | p95 | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: |
| GPU | 179.16 ms | 249.15 ms | 0 / 0 |
| CPU control | 161.82 ms | 220.33 ms | 0 / 0 |

The GPU interval performs 405,412 membership changes and 35,134 merges, transferring
1,432,956,032 graph bytes across both directions. Both state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
That snapshot implementation was slower than the CPU graph control; the next stage
below replaces its complete graph transfers. Parallel ordering remains unfinished. Artifacts:
ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-island-graph-a.json`.
These checks establish this executing stage, not full GPU backend completion,
steady all-awake frame rate, other devices or native-window acceptance.


## Resident island graph and compact publication (2026-10-08)

The graph now persists on GPU. Serialized native graph mutations journal island,
body, contact and joint slots, coalescing repeated writes to the final record.
The hooks cover creation/destruction, type/enable changes, authoring list edits,
merges and both CPU and GPU split publication. Adjacent list nodes are included.
Prepared GPU merges do not reupload their own island changes. Contact lifecycle
records still synchronize newly created, freed and reused slots. Binding/pool
changes, a lost observer or graph-buffer growth require a snapshot; warm fixed
capacity batches upload only the journal and their ordered contact operations.

The GPU tracks changed slots across union, remapping and retirement. Compaction
returns keyed 68-byte island and 20-byte member records instead of the complete
graph. The owner applies these to retained mirrors, validates the complete graph
and publishes only changed records. A growing output batch reports its required
word count; the host grows scratch and gathers the already completed graph again.
Union/list mutations are never repeated. Warm capacity uses one submission/fence.
Readback accounting includes the retained output capacity, not just useful words.

The focused graph oracle now requires no snapshots or output retries after warmup,
while preserving zero all-thread managed allocation and exact CPU lists/ID order.
It forces initial compact-output overflow, recycled body/island IDs, authoring
joint merge/removal, static/kinematic/dynamic changes, disable/enable, CPU and GPU
split publication, observer loss, capacity growth and ownership/reset boundaries.
The complete GPU failure/callback/lifecycle suite covers the same integrated path.

The ordered union pass is still serial. The host still scans its retained graph
for validation, publishes native mirrors and manages wake/set-transfer/coloring.
Split inputs/results remain separate and feed the journal. This stage does not
complete the independent backend or establish native-window FPS or other devices.

Final sequential Linux/Vulkan maximum-Smash runs use the same 65,537-body scene,
32 warmup and 64 measured headless diagnostic steps. Order is GPU A/CPU A/CPU B/GPU B;
no build/test/formatter ran concurrently. The CPU graph control disables all three
batch callbacks and the new graph journal observer; other GPU stages remain active.

| Island graph | Whole-step mean | p95 | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: |
| Resident GPU A | 177.50 ms | 246.13 ms | 0 / 0 |
| CPU A | 160.88 ms | 256.78 ms | 0 / 0 |
| CPU B | 163.40 ms | 234.11 ms | 0 / 0 |
| Resident GPU B | 179.73 ms | 255.26 ms | 0 / 0 |

Each GPU interval performs 405,412 membership changes and 35,134 merges, uploads
52,704,224 graph bytes and reads back 46,528,640 bytes. Total transfer is 99,232,864
bytes, down 93.07% from the prior snapshot stage's 1,432,956,032 bytes. There are
zero snapshots and zero readback-capacity retries in the measured interval. All
four state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
The transfer reduction does not establish a whole-step speedup: the GPU graph is
still slower than the CPU graph control. Ordered union and full host validation
remain targets for separate timing and parallelization. Artifacts: ignored
`bin/physics-sandbox/profile-Release-{gpu,cpu}-island-resident-final-{a,b}.json`.
An initial atomic-dirty-mark candidate measured 175.47 ms with the same transfer
counts (`profile-Release-gpu-island-resident-a.json`); final slot-owned writes remove
those unnecessary atomics without a demonstrated timing gain.


## Parallel ordered contact lists (2026-10-08)

Phase timing first measured graph preparation at 3.09 ms, command recording at
0.15 ms, GPU submission/fence wait at 11.93 ms, readback decoding at 0.35 ms,
validation at 5.66 ms and final publication at 0.19 ms per maximum-Smash step.
The whole-step mean was 179.15 ms. Artifact: ignored
`bin/physics-sandbox/profile-Release-gpu-island-phases-baseline.json`.

Weighted union now records contact-list concatenation trees instead of serially
splicing every contact. An initial island list is a leaf; a started-contact node
prepends that contact to the old winner/loser subtrees. Actual winner and freed-ID
order stay unchanged. Removal of other members commutes with this construction,
so removals and their final-root counters execute in parallel after union.

Bounded ping-pong pointer jumping resolves subtree followers and runs of removed
contacts. Separate passes write next links, heads, inverse previous links and tails,
then clear removed members. Every valid destination has one writer per pass,
except explicitly atomic removal counters. Scratch extends the existing dirty
buffer, retaining the same resource bindings. GPU error reads occur once per
workgroup with a shared barrier, avoiding a contended atomic read per invocation.
Compact output, validation before live publication and failure/no-replay handling
remain in place. Empty-contact groups, static endpoints, joints and sleeping-set
wake chains retain their original native lists and identities.

The contact oracle adds complete removal of a dense 64-body group's contact list,
alongside mixed removals and long prepend chains. Malformed internal island metadata
must raise a GPU error before mirror publication, exercising the shared error gate.
The complete GPU suite checks callbacks, failures, disposal, owner affinity and both
rendering-device lifetimes. `IslandGraphProfileMS` reports cumulative host phases;
submit/fence wait includes queue/transfer costs and is not a per-kernel GPU timestamp.

The first ordered-tree candidate still read the shared error atomically in every
invocation: 176.82 ms whole-step mean and 10.40 ms submit/wait, with the same state
hash and zero managed bytes. Artifact: ignored
`bin/physics-sandbox/profile-Release-gpu-island-rope-a.json`. The final version uses
one atomic error read per workgroup. This candidate measured 179.27/176.46 ms
whole steps and 10.15/10.14 ms submit/wait in two paired runs; CPU graph controls
were 161.59/162.08 ms. Artifacts: ignored
`profile-Release-{gpu,cpu}-island-parallel-final-{a,b}.json` under `bin/physics-sandbox/`.

The final union loop additionally retains the current component root, contact head
and count in registers across consecutive insertions. It flushes before any merge
or component change, preserving intermediate winner/free-ID results. A root lookup
can stop at that known live winner. The first cached-head probe measured 177.60 ms
whole-step and 9.58 ms submit/wait (`profile-Release-gpu-island-cached-head-a.json`).
Final paired results follow below.


Final sequential Linux/Vulkan runs use 65,537 bodies, 32 warmup and 64 measured
headless diagnostic steps. Order is GPU A/CPU A/CPU B/GPU B; no other build, test
or formatter ran concurrently. The CPU graph control leaves other GPU stages on.

| Graph mode | Whole-step mean | p95 | Graph submit/wait | Graph validation | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: | ---: |
| Parallel/cached GPU A | 178.51 ms | 246.25 ms | 9.32 ms | 5.77 ms | 0 / 0 |
| CPU A | 164.03 ms | 264.81 ms | — | — | 0 / 0 |
| CPU B | 162.62 ms | 233.18 ms | — | — | 0 / 0 |
| Parallel/cached GPU B | 177.78 ms | 285.63 ms | 9.58 ms | 5.99 ms | 0 / 0 |

GPU preparation remains 3.10/3.14 ms, recording 0.28/0.27 ms, decoding 0.32/0.32 ms
and publication 0.18/0.17 ms. Compared with the earlier 11.93 ms submit/wait probe,
this graph portion is lower, but the whole step still does not establish an
application speedup or reach the CPU graph control. Further work remains in
weighted union, full CPU validation/packing, coloring and the other backend stages.

The GPU intervals still perform 405,412 membership changes and 35,134 merges with
99,232,864 transfer bytes, zero snapshots/retries and zero warmed managed bytes.
All four state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-island-cached-final-{a,b}.json`.
These are impact-through-sleeping-wall measurements, not sustained all-awake,
native-window FPS, cross-device or native-allocation acceptance.

## Packed host graph expectations (2026-10-08)

Graph preparation and validation no longer dereference every backend contact/body/
joint object. One byte per journal key retains independent CPU alive/linked flags.
The initial snapshot and later authoring journal refresh them, ordered CPU contact
operations change expected membership, and successful backend island release clears
its alive flag. Readback cannot change these expectations. The retained dense graph
still receives complete bounds, list-order, cycle, count and membership validation
before publication; dead records claiming liveness are now explicitly rejected.
No shader, transfer format or public API changes are involved.

The contact-churn oracle continues to cover recycled IDs, authoring joint/body
changes, CPU/GPU split publication, observer loss and capacity growth. Added fault
injection corrupts dead body/joint/island records and live unlinked static-body
records in the returned scratch mirror. Validation rejects them before live
publication. The full GPU suite and default managed runner pass; warmed churn
retains zero managed bytes and no graph snapshots/readback retries.

Sequential Linux/Vulkan runs repeat the preceding 65,537-body headless workload
with 32 warmup and 64 measured steps, in GPU A/CPU A/CPU B/GPU B order. The CPU
control replaces only island-graph processing. Builds, tests and formatting do
not run concurrently with these measurements.

| Graph mode | Whole-step mean | p95 | Preparation | Validation | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: | ---: |
| Packed flags GPU A | 174.12 ms | 270.13 ms | 1.82 ms | 4.21 ms | 0 / 0 |
| CPU A | 160.23 ms | 249.84 ms | — | — | 0 / 0 |
| CPU B | 160.36 ms | 242.30 ms | — | — | 0 / 0 |
| Packed flags GPU B | 172.65 ms | 256.06 ms | 1.80 ms | 4.15 ms | 0 / 0 |

Preparation plus validation is about 6.0 ms versus 8.9–9.1 ms in the preceding
cached-list runs. Whole steps are lower too, but the CPU controls also improved;
this is not evidence of an overall GPU advantage. GPU graph submit/wait remains
9.01/9.15 ms and its complete host list traversal still costs CPU time.

Both GPU intervals retain 405,412 membership changes, 35,134 merges and 99,232,864
graph transfer bytes (52,704,224 upload, 46,528,640 readback), with zero snapshots
or retries. All four state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-island-dense-flags-{a,b}.json`.
The preceding sleeping-wall, native-window, cross-device and allocation boundaries
continue to apply. Full GPU world completion remains a separate open requirement.

## Collision-batch GPU constraint coloring (2026-10-08)

The GPU entry now selects constraint colors for the ordered collision-state batch.
Preparation anticipates sleeping-set wakes, adding their touching contacts and
joints in original set order before each triggering contact. The normal insertion/
removal paths consume generation-checked results, keep existing simulation-array
order and update the CPU occupancy mirror. Immediate authoring coloring, sleep
decisions and set transfer remain CPU operations. Ordinary CPU worlds use the
unchanged greedy policy through one shared contact/joint helper.

The shader first transposes the uploaded per-color body bitsets into one occupied-
color mask per body in parallel. A serial ordered pass reads endpoint masks and
selects the first free low/high bit according to the existing dynamic/static
priority, or uses overflow. Scratch shares the same buffer; no extra binding or
transfer is needed. The current 24-color layout fits in one 32-bit mask. A future
larger layout fails explicitly. Parallel assignment remains open work.

Each nonempty batch performs one submission with 32-byte input operations and
four-byte returned colors plus one error word. Body bitsets currently upload every
batch. CPU validation checks bounds, color policy, independent occupancy transitions
and final publication identity/order; the finish hook checks complete consumption
and final bitsets. Empty mutation batches skip compute. Capacity persists for all
current constraints, avoiding growth merely because a larger subset changes.

The churn oracle now compares every contact/joint slot and complete color bitsets,
including static/kinematic endpoints, authoring changes, ID reuse, dense overflow
and allocation intervals. A sleeping star wakes 30 joints plus existing touching
contacts and reaches overflow; unchanged contacts then require no extra submission.
Malformed shader input and invalid returned colors fail before graph publication.
A mid-publication failure checks poisoned-world no-replay and cleanup. Replacement
callback ownership, rebind/dispose and world reset are covered by the same suite.

The first shader searched each color's body bitset serially. It measured 7.69/7.49 ms
for dispatch/readback and 175.25/177.06 ms whole steps versus 171.21/171.95 ms for
CPU-color controls. This candidate is superseded by the mask-transpose shader.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-constraint-colors-{a,b}.json`.

Final sequential Linux/Vulkan runs use the same 65,537-body headless Smash workload,
32 warmup and 64 measured steps, ordered GPU A/CPU A/CPU B/GPU B. No build, test or
formatter ran concurrently. The CPU control disables only coloring hooks; other
GPU stages stay enabled (`ELECTRON2D_SANDBOX_PROFILE_CPU_COLORS=1`).

| Coloring | Whole-step mean | p95 | Preparation | Dispatch/readback | Validation | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| GPU masks A | 177.21 ms | 246.97 ms | 1.59 ms | 3.73 ms | 0.060 ms | 0 / 0 |
| CPU A | 174.72 ms | 248.06 ms | — | — | — | 0 / 0 |
| CPU B | 172.92 ms | 274.27 ms | — | — | — | 0 / 0 |
| GPU masks B | 176.92 ms | 267.48 ms | 1.66 ms | 3.62 ms | 0.060 ms | 0 / 0 |

GPU dispatch/readback cost is lower than the serial-search candidate, but complete
steps remain slower than CPU coloring. These data establish executable GPU work
and exact state equivalence, not an application speedup. The measured phases are
host wall times, not GPU kernel timestamps; final mirror updates/bitset comparison
are outside the three coloring phase timers.

Each final GPU interval colors/removes 405,412 constraints in 64 submissions,
transferring 26,659,600 bytes. All eight candidate/final state hashes remain
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`;
owner/all-thread managed allocation is zero. Final artifacts: ignored
`bin/physics-sandbox/profile-Release-{gpu,cpu}-constraint-color-masks-{a,b}.json`.
The final SPIR-V rebuild matches SHA-256
`3e7aa26d09a41a77b196002df95b3d7026e1315f9d8f0111f4dcd4d74ddfb348`.
Sleeping-wall/native-window/cross-platform/native-allocation limits still apply;
public backend selection and full GPU-world completion remain open.

## Fused numeric body finalization (2026-10-08)

GPU solve now includes a parallel body-finalization pass after restitution in the
same command submission. It reads resident solved velocities/deltas, enforces axis
locks, normalizes absolute rotation, computes center/origin, updates sleep time
using velocity and weighted position correction, and emits fast/awake/split flags.
Each body adds a 64-byte input and a 64-byte result. There is no extra solver fence
or solved-body upload. Input/result storage follows prepared body capacity.

All records are validated before solved-state publication. The owner then lends
the array to the ordinary finalization workers for one step. Workers import numeric
results and retain move-event ordering, delta/force clearing, CCD continuation,
shape bounds and island/set publication. A `finally` after worker completion and
step reset clear the borrowed array. Rebind/dispose invalidate pending ownership;
failed intervals do not replay on CPU. CPU worlds retain the same numeric rules.
Island sleep reduction/transfer, actual CCD and shape-AABB computation remain CPU.

The new numeric oracle runs the existing CPU finalizer on identical solved states
for 0/1/63/64/65/257/4,097 bodies. It checks exact pose equality, locks, offset centers,
rotation, sleep timers/flags and position-correction wakeups. Thirty-two warmup and
sixteen measured sleep/wake steps allocate zero managed bytes across all threads.
Separate live worlds compare fast bodies, bullets, wall impacts and ordered move
events. Invalid generation, nonfinite output, status and rotation fail before pose
publication. Stale/reset/duplicate consumption, callback ownership and an injected
failure before worker publication are checked as well.

The first candidate passed tolerance-based checks but failed maximum-Smash state
equivalence: GPU hash
`3886202ECE045868C5FF4A5A994E3603C6386D3D9C0858828DE18631781F2A49`
versus the existing CPU-control hash below. GPU means were 177.40/178.22 ms and CPU
controls 169.12/171.93 ms. Those runs are retained as failed equivalence evidence
under ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-body-finalization-{a,b}.json`.
Strengthening the oracle located an adjacent-float rounding discrepancy in near-unit
rotation normalization. Newton correction alone can choose the wrong root neighbor
at a midpoint. Finalization now compares the FMA residual with adjacent-root
midpoint-square boundaries and corrects reciprocal rounding too. This correction
uses float/uint operations locally; other solver shaders are unchanged.

Final sequential Linux/Vulkan runs repeat the 65,537-body headless impact workload
with 32 warmup and 64 measured steps in GPU A/CPU A/CPU B/GPU B order. The control
sets `ELECTRON2D_SANDBOX_PROFILE_CPU_FINALIZATION=1`; all other GPU stages stay on.
No build, test or formatter runs concurrently with these measurements.

| Numeric finalization | Whole-step mean | p95 | Remaining CPU transform phase | Solver submissions | Managed bytes, owner/all threads |
| --- | ---: | ---: | ---: | ---: | ---: |
| GPU A | 175.77 ms | 252.68 ms | 1.661 ms | 64 | 0 / 0 |
| CPU A | 177.30 ms | 245.03 ms | 1.660 ms | 64 | 0 / 0 |
| CPU B | 174.61 ms | 241.82 ms | 1.669 ms | 64 | 0 / 0 |
| GPU B | 179.13 ms | 254.57 ms | 1.644 ms | 64 | 0 / 0 |

Both GPU intervals finalize 1,459,456 bodies in 64 existing solver submissions,
adding 186,810,368 input/result transfer bytes. Solver packing averages 4.38/4.36 ms
versus 3.51/3.52 ms in controls, and readback/publication 5.18/5.14 versus 4.84/4.79 ms.
All four final hashes match
`13E529560ADFA82C42498E411407CE134B211859CFE79B706A0EC98322B09F90`.
Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-body-finalization-rounded-{a,b}.json`.
The final SPIR-V rebuild matches SHA-256
`8d621f206159d24c48fad8d3640e8f103152e807d5eed0db126c67b0171ef432`.

The numerical transfer is verified; these timings do not establish an application
speedup. Shape bounds, CCD and publication still dominate the remaining CPU
transform phase. Native allocation accounting, sustained all-awake/native-window
FPS, foreign devices and full GPU-world completion remain separate requirements.
