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

Broad-phase AABB traversal now runs on GPU before manifold generation. The owner
packs the current static/kinematic/dynamic trees as 32-byte threaded nodes and
48-byte moved-proxy queries. Escape links preserve the existing child2-first
visitation order without a shader stack limit. Dynamic queries visit kinematic,
static and dynamic trees; other proxies visit only the dynamic tree. Fat bounds
and zero-category pruning match the CPU search. The GPU emits real candidate
proxy keys; it does not receive CPU-generated overlap pairs.

Each query retains a power-of-two candidate capacity. A normal batch uses one
submission/readback; overflow reports the full count and repeats the unchanged
GPU query after growing storage, before invoking any user filters or creating
contacts. There is no density cap or truncated-pair fallback. The GPU also applies
self/moved-pair deduplication, existing-contact lookup, same-body and sensor veto,
64-bit category/mask and signed group filtering, and joint collision veto. The
shared pair hash uses split 32-bit arithmetic, so this does not require shaderInt64.
Joint filtering walks the smaller body adjacency list. Only user filters and
ordered contact creation remain on the owner after readback; tree maintenance
and the shared contact lifecycle remain managed. The GPU maintains its own
resident lookup table for those contacts. Buffers and per-shape capacity hints
retain their peak size; new topology/capacity can allocate outside warmed checks.

Filter inputs use 48 bytes per shape and 32 bytes per joint. Shape records are
packed in slot order; moved flags come directly from the move array rather than
one hash lookup per shape. Tree, shape and joint snapshots are currently uploaded
on each moving batch. The pair table itself is never uploaded: 4-byte GPU slots
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
It excludes the later managed contact creation loop. Cumulative upload/readback
bytes and candidate totals make transfer volume visible without per-frame logs.

Contact geometry is generated on GPU for all nine registered pair families
among circles, capsules, two-sided segments and convex polygons (up to eight
vertices, including rounded polygons). SAT, edge clipping and vertex contacts
preserve feature IDs and the speculative distance used for warm starting.
Chain segments are explicitly unsupported by this development entry. Geometry
records are shared by shape ID rather than duplicated per contact (144 bytes);
each pair occupies 48 bytes and its returned manifold 80 bytes. The owner waits
for the collision fence and validates the batch before publication. Material
mixing, pre-solve filtering and contact transitions still use the common managed
world path. Feature-ID matching and reuse of normal/tangent/rolling impulses now
execute in the collision shader. It reads the previous completed GPU solver
buffer directly when that contact's source is current; cold or older sources
upload a compact 32-byte history record. Empty histories need no upload.
The matched 32-byte result remains on GPU for preparation and is also read back
for the existing managed contact snapshot. Shared contact processing shifts
anchors and applies pre-solve veto without repeating feature matching. A veto
clears rolling state, preventing a later contact from reviving stale impulses.

The generated manifold buffer now remains available to constraint preparation.
Each contact carries a source version and slot through graph copies: positive
versions address generated geometry, negative versions address a completed solve.
World identity and the latest submission prevent using an overwritten solver buffer;
the owner/step marker prevents cross-world or old-step reuse. Feature IDs resolve
point reordering or pruning directly on GPU. Center-of-mass offsets are captured at the
collision pose. The solver uploads a 128-byte input (body indices/masses,
materials, retained impulses, source, offsets and surface velocities) instead of retransmitting the
working contact geometry. Missing provenance or an explicit internal geometry replacement uses a separate
80-byte override; this includes sleeping contacts awakened after collision
collection. Reset, another collision batch and consumption invalidate reuse.
The input/manifold buffers bind read-only during solving. The first manifold
readback and managed event processing still remain; this is partial
residency, not elimination of the collision synchronization fence.

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

The tree, solver and manifold callbacks run on the world owner. Tree maintenance,
user filtering/contact creation, sleep/CCD finalization and queries remain in the managed
backend; large worlds retain CPU contact-update workers. There is no production
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

Remaining work: GPU tree maintenance and contact creation, chain manifolds, GPU contact transitions without full manifold/history readback, spring
force setup, sleep/CCD finalization, complete query/event/state contracts, independent backend selection
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
Tree/shape/joint uploads remain and are the next residency boundary.

Artifacts: ignored `bin/physics-sandbox/profile-Release-{gpu,cpu}-resident-pairs-{a,b}.json`.
Current shader SHA-256 values:
`PhysicsPairTable.comp.spv` = `1a561f7e51416d87a41fbfc4993d041bac9db4f2df9da2d7ab1d54a73e7cb598`;
`PhysicsBroadPhase.comp.spv` = `47a2914804786b51f7ff7f7d30e441af0b3ab958c6e640c8025c35184f813cdb`.
