# Resident GPU physics stages

[Complete buffer accounting](gpu-memory.md) now sums requested storage/transfer resources and temporary growth overlap, including checkpoint copies and failed preparation. Driver heaps and CPU/managed retained memory remain separate open measurements.

Last updated: 2026-10-24

## Implemented boundary

The public [caller-owned pose batch](gpu-demand-publication.md#explicit-pose-batches)
now selects 16-byte position/basis records without downloading full body state.
Its gather shares validated handles, pending-command/mass preparation and terminal
failure policy with the existing 64-byte snapshot read. Both formats use integer
result storage to preserve flag/mode bits; requested duplicates can exceed resident
body population without adding physical objects.

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) is the first independent
device-state component under [ADR 0054](../decisions/physics-backends.md#adr-0054). It creates
no CPU solver world. Body slots, generation changes, sparse edits, force/mass/gravity/
damping integration, geometry edits, transformed bounds, broad-phase tree maintenance,
complete candidate pairs and narrow-phase contact points execute through offline GPU pipelines. Authored values and read results use scene units. The original
[GPUPhysicsWorld stage host](../classes/GPUPhysicsWorld.md) remains separate.

The [contact response pipeline](gpu-contact-solver.md) now adds material/impulse
solving, warm history and separate positional correction through Simulate. This
component now also has [resident pin/groove/spring joints](gpu-resident-joints.md)
and [directed collision exceptions](gpu-resident-exceptions.md), plus
[latched one-way contact episodes](gpu-resident-one-way.md) and
[live role/integration policy changes](gpu-resident-parameters.md) and
[single-tick transient forces](gpu-resident-forces.md), plus
[kinematic targets and virtual surfaces](gpu-resident-kinematic.md) and
[device Area field reduction](gpu-resident-fields.md).
Public [scene/server selection, body publication and joint adapters](physics-backends.md)
now consume the resident world. Full API conformance, extensions, virtual tile owners,
networking and massive-scene performance acceptance remain open. [Resident CCD](gpu-resident-ccd.md) now executes internally. Automatic/custom [mass profiles](gpu-resident-mass.md) now use shared authoring
geometry and center-aware device motion/constraint preparation. [Connected sleep/wake](gpu-resident-sleep.md) now executes on GPU. Step
remains an integration-only control; FindContacts computes contact points.
Its partial-pipeline timings cannot be compared with full CPU physics or reported
as window FPS. Full observable conformance and whole-world performance must be established before
the independent GPU objective is satisfied.

## Scalar body edit flags

The Linux diagnostic found the same resident command bytes on a passing local
host and a failing EPYC 9V45: the mover header is `(1, 2, 1313924, 256)`, but its
body flags lose CCD bit 256 and reporting bit 128 before the CCD shader. Disabling
managed SIMD does not correct that result.

A scalar header-policy/current-flags candidate passed local NVIDIA and CI-version
Lavapipe compound, resident, parameter, kinematic/state and sleep controls. Native
run `40043371390` at `30e956813` still reproduces the identical CastRay failure and
`90124` flags on EPYC 9V74 (artifact `12541025364`). The candidate is removed;
changing the edit expression alone does not close this native defect.

`GPUPhysicsBodyStoreTests.VerifyPolicyEdits` checks actual device flag readback
before and after integration for all three CCD modes, opposing reporting/sleep
policies, mixed sparse commands, unrelated rotation locks and untouched lanes.
It retains 128 warm and 128 measured cycles with strict zero owner/all-thread
bytes and unchanged buffer accounting. The compound selector starts with its existing complete public physical workload
in a cold process, then checks these flags; both must pass. Resident-store warmup
must not precede the cold public contract while investigating first-use failures.

The failure-only temporary trace now appends body/active-command readbacks after
each body dispatch to its existing command buffer and maps them after its existing
fence. Each small CCD world retains at most 24 stage snapshots. This separates
edit application from later motion stages without changing production shaders,
command layout or numerical work. Extra diagnostic copy passes and file writes
are not performance or untraced acceptance evidence. Source receipts restore the
exact original body store and CCD source and remove the helper before controls.
Cold public-first run `40046406798` at `527ea9826` on AVX-512 Xeon 8573C retains
ten staged snapshots. The first, immediately after edit stage 0, has mover flags
`90156` with command `(1, 2, 1313924, 256)`; stage 3 has `90124`. Thus CCD policy is
already absent after edit application, rather than first disappearing in the
subsequent sweep. The staged trace still reproduces the original failure.
The next candidate reads the scalar policy word directly from the command buffer
before materializing the aggregate command. Reporting, CCD and sleep edits consume
that same word with their existing masks and order; destination-flag updates stay
unchanged. The policy regression adds the observed two-active-command/five-body
shape while retaining existing reporting/sleep flags and untouched tail state.
Command/body strides, bindings, buffers, submissions and readbacks do not change.
The manual diagnostic uploads CPU capabilities and driver versions before runner
publish, so AVX-512 coverage can be checked without waiting for the final logs.
This is distinct from the rejected scalar destination-flag candidate above.
Corrected execution on the failing native hosts remains required.

## Storage, transfers and waits

Field eligibility is retained separately from the current solver mode for scene
RigidBody, CharacterBody and direct scene server-mode profiles. Temporary
MakeStatic and frozen roles still reduce directed Area/default fields on device
and publish GetGravity/direct-state totals, without integrating static motion.
Ordinary static scenery and Areas remain non-receivers. The extra policy bit
uses the existing device flag word and sparse integration edit; it adds no buffer,
state download or fence. CollisionDisableModeTests exercises the same public
field, lifecycle and packed-scene checks on explicit CPU/GPU worlds; its 64
warmed mixed frames measure zero owner/all-thread managed bytes on Linux/.NET 10.

| Storage/transfer | Purpose and current cost |
| --- | --- |
| Device bodies | 96 bytes per retained slot; authoritative pose, actual/surface velocity, force, mass/damping and generation/role data. |
| Device local centers | 8 bytes per retained slot; read during integration and constraint preparation, copied on growth. |
| Device targets | 16 bytes per retained slot; pending kinematic world pose with no CPU pose mirror. |
| Device resolved fields | 16 bytes per retained slot; last scaled gravity and independent damping, copied on growth. |
| CPU slot metadata | 112-byte payload per slot for generation, free-list, pending-command routing, shape/joint/exception identity, authored surface/role/integration/damping-mode/mass profiles and resolved geometry; no live poses or velocities. |
| Device transient forces | 16 bytes per retained slot; pending linear force, torque and eligibility, with no host mirror. |
| Pending edit staging | 176 bytes per reserved command. At most one coalesced command per slot; entries are cleared after successful publication. Allocation follows capacity growth, not every frame. |
| Growth | Copy prior body slots GPU-to-GPU; no body download/upload reconstruction. Record copied bytes and wait for resource replacement. |
| Integration-only unchanged tick | 8-byte status reset upload, 32-byte compute uniform and 8-byte error/activity-result download. No body-state traffic. |
| Explicit selected read | 16-byte generation/store-qualified request and 64-byte pose/velocity/sleep/field result per requested body, plus status and dispatch uniforms. |
| Optional changed publication | Device comparison/history; eight reset/status bytes each direction, 16 uniform bytes and 80 bytes downloaded per changed body, with a second wait only for nonempty output. See [generation semantics and measured costs](gpu-body-publication.md). |
| Per-tick fence wait | Required by this synchronous stage's finite-result/error publication contract. Its measured time is recorded separately from total submission/map/dispatch work. |

At 65,536 slots, body/center/transient-force/target/field payload is 9.5 MiB and retained CPU metadata/command payload is
18 MiB; consumed command storage contains no live-state mirror. GPU command/request/
result scratch payload totals 16 MiB plus the eight-byte status. Upload/download transfer
capacity totals 16 MiB plus sixteen bytes. These are payload capacities, excluding
object/driver overhead. Further asynchronous publication requires an explicit
error/freshness contract, rather than silently removing the wait.

The SDL compute API supports device work without a window; the actual buffer,
command and fence pattern follows [SDL's GPU workflow](https://wiki.libsdl.org/SDL3/CategoryGPU).
The current Vulkan loader still needs SDL video initialization. Shared
GPUPhysicsDevice ownership retains that reference and applies the same platform
environment policy as DisplayServer.

## Shared pair/contact completion (2026-10-22)

Fresh `FindContacts` work with at least two shapes now records resident bounds,
tree/pair discovery and contact generation in one command. The contact kernel
reads the device pair count, clamped to retained pair capacity; it never reads an
unwritten pair. Pair and contact error/count records remain independent. An
additional eight-byte device status buffer prevents contact reset from destroying
the pair result once contact storage is prepared. A cold two-shape world without
pairs now also prepares contact/one-way scratch: the measured device payload is
63024 versus 51744 bytes for separate discovery (11280 extra bytes and seven
buffers), with the same 16984-byte transfer capacity. Empty/real/empty transitions
and complete release execute in both modes. These are requested payload capacities,
not driver allocator totals. One completion validates both records before publishing counts
and one-way history; optional velocity and one-way statuses retain separate
checked slots in the same readback.

Capacity recovery first grows an overflowing pair output, then a complete contact
or one-way output, with one recovery per family. It reruns immutable discovery,
without repeating pending velocity integration. Ordinary pair-only and spatial
query work retains its existing path. The internal `SeparateContactSubmissions`
control executes the same kernels with separate completion waits. Shared dispatch
can launch unused lanes up to retained pair capacity; full-scene timings must
account for that cost rather than assuming a throughput improvement.

On Linux x64/.NET 10.0.1 and NVIDIA Vulkan, the paired contact control observes
one versus two completion submissions, with the same 16 bytes uploaded and 16
bytes downloaded. This control flushes authored pose edits with `Step(0)` before
querying contacts; it is not a positive-duration scene simulation. Each mode
passes 128 contact-refresh cycles at strict zero owner/all-thread managed bytes.
Existing contact checks also pass 1620
rotated geometry cases, concave output growth, generation/lifetime edits,
terminal nonfinite failure and 4096/65536-body moving grids. The grid's existing
allocation gate measures owner bytes only. Its integration/broad/narrow timings
remain a partial-pipeline diagnostic. Evidence is retained in local ignored
`bin/physics-pair-contact-batch/2026-10-22/`.

The frozen CLR cohort passes fourteen adjacent solver, one-way, CCD, sleep,
spatial/shape/motion-query, pipeline, common-world checkpoint and full-scene
profiles. Eight strict full-trim NativeAOT public-consumer profiles pass contact
frames and portable contacts on CPU/GPU, owner/separate numerical lanes. CPU
profiles run with dummy video and unavailable Vulkan drivers. The final source,
payload, native binary and embedded contact-program hashes are checked by
`bin/physics-pair-contact-batch/2026-10-22/validate.py`.

The full scene retains 16384 awake bodies, four walls, 144 Hz input, four substeps,
16 solver iterations, eight reported contacts, monitoring and interpolation.
After 1600 warm frames, each path measures 256 complete physical frames at strict
zero owner/all-thread managed bytes and passes the existing physical gates.

| Full path | Mean ms | p50 ms | p95 ms | p99 ms | Mean submissions/tick |
| --- | ---: | ---: | ---: | ---: | ---: |
| GPU shared pair/contact completion | 93.573 | 98.822 | 109.575 | 120.390 | 15.188 |
| GPU separate completion control | 95.450 | 102.528 | 115.154 | 116.830 | 19.238 |
| CPU without a graphics device | 108.201 | 107.644 | 116.050 | 121.286 | 0 |

Both GPU runs upload 196888 bytes/tick; observed mean readback is 8740159 and
8767279.438 bytes/tick respectively. These remain complete report-driven reads.
GPU fence-wait p50/p95/p99 is 33.640/38.512/56.871 ms for shared completion and
38.200/42.521/42.943 ms for the control. CPU worker joins are included in its total
step time; the GPU fence counter does not measure CPU waiting. Raw reports retain
all transfer/uniform counts, completion phases, numerical diagnostics and maxima.

These times were collected concurrently with an older aggregate and are
diagnostic observations, not accepted speed ratios. The isolated control proves
the removed intermediate completion; full-scene throughput, real-window FPS,
foreign-platform and whole-goal acceptance remain open.

The later [current-main ordinary aggregate](physics-allocation-measurement.md#current-main-ordinary-aggregate-2026-10-22)
passes the complete default CLR suite at c17f173af in 4391.535 seconds, with this
pair/contact implementation, unchanged payload and all six point-field gates
at 0/0 bytes across 768 measured full frames. The repaired native cohort includes
KTX through its supported build property; the earlier missing-library failure
remains a rejected result. Exact source substitutions and the preserved extra
checkpoint test are recorded in that report. The following [network regression](physics-network-example.md#network-regression-after-shared-contact-completion-2026-10-22)
passes 28 selected NativeAOT profiles and fourteen real process/window sessions
on the same frozen runtime. Broader conformance and massive-scene performance
remain separate gates.

A [later default aggregate at d90d89c1c](physics-allocation-measurement.md#default-clr-aggregate-at-d90d89c1c-2026-10-22)
also completes in 4740.632 seconds with unchanged payload, no new Xid and all six
point-field allocation gates passing 768 measured frames at 0/0 bytes. Its pinned
runtime precedes the later cached-color command reduction; the result retains
that source boundary and establishes no massive-scene speed ratio.

The earlier isolated eight-case timing plan at 7b6db6079 stops after its first CPU case:
physical and 0/0-byte checks pass, but foreign workload reaches 2.107 mean cores
in an observed interval against the predeclared 1.0 limit. Its 1/8 result is
rejected, with no automatic retry or changed threshold. It establishes no GPU
speed ratio or real-window FPS result; see local `isolated-analysis.json`.

## Verification and measurement method

Run `ELECTRON2D_TEST_GPU_RESIDENT_BODY=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after the Release test build with source native dependencies. The full GPU suite
also runs these tests. The kernel is compiled with the repository's pinned glslang
and validated with its spirv-val against Vulkan 1.0.

Functional checks cover semi-implicit constant force/gravity and torque at 120 Hz,
static/kinematic/rotation-locked roles, positive/negative damping, sparse edits,
setter/impulse order, actual evolved state surviving growth, generation reuse,
foreign-store identity with matching numeric slots/generations, owner guards and
failed-state rejection after nonfinite device integration.

The measured workload contains 65,536 moving rigid body records, no collisions,
zero gravity and velocity (0.25, 0.5) scene units/s. Warmup is 384 ticks; 256 ticks
at 1/120 s are sampled. Managed allocation is measured on the owner thread.
All 65,536 positions are then checked through a diagnostic full read outside the
timing/allocation window; its cost is reported separately. The position tolerance
is 0.02 scene units after 640 float-precision additions near coordinates 0–255.
Smaller force/torque checks use 0.002 scene units and 0.001 rad after 120 ticks.
This is one platform's body-stage evidence, not full-backend, networking, native
allocation or cross-platform acceptance.

Historical body-only baseline at `fa155264bbb2246e2f2a41b9047349b1cc76297a`,
Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti, 2026-10-08:

| Run | Tick p50 | Tick p95 | Mean fence wait | Diagnostic full read |
| --- | ---: | ---: | ---: | ---: |
| Dedicated resident test | 0.0489 ms | 0.0606 ms | 0.0419 ms | 1.7368 ms |
| Resident test inside full GPU suite | 0.0448 ms | 0.0688 ms | 0.0437 ms | 0.9892 ms |

Both runs report zero warmed managed bytes per tick and the unchanged-tick payload
listed above. The full read requests 1 MiB of handle data and downloads 2 MiB of
state, plus status/uniform payload. Logs are
`/tmp/electron2d-resident-bodies-final.log` and
`/tmp/electron2d-resident-bodies-gpu-suite.log`. These two runs use the same source
and shader; no CPU physics speedup or window-FPS conclusion follows from them.
The SPIR-V SHA-256 is
`266ebba6cffa52ea3e70118813982b2985716bfa23f455c8d8d7c73585e3972c`.

## Resident geometry and broad phase

Shape resources provide their borrowed scene-unit geometry. The device retains one
32-byte descriptor per distinct resource, eight bytes per contour vertex and a
80-byte attachment per shape slot, including material, one-way direction/margin and integer edit revision. Circle/capsule radius, contour winding and separation-ray metadata
are retained; rectangles use four corners, convex polygons retain the whole contour,
and concave shapes retain all segment endpoints. No CPU fixture partition, dynamic
tree, body pose array or publication rank is imported. CPU attachments retain authored
local placement/filter/identity; evolving world bounds exist only on the device.

Resources are borrowed, not disposed by the store. Revision/epoch checks detect
changes even when a Changed observer throws. Only changed resources/attachments
upload; several borrowers share one geometry upload. Disposal deactivates geometry.
Body removal walks only its own shape list; constant-time attachment unlink avoids
quadratic full-world scans during mass retirement. Generations prevent reuse from
inheriting old attachments. A reusable/coalescing vertex arena handles cold geometry resizing;
GPU buffers grow by device copy. Owner-thread use and unchanged resources during
dispatch remain required.

FindPairs composes local shape placement with resident body pose and writes AABBs
on GPU. The existing balanced Morton tree kernel accepts an extra sensor subtree
bit; older stage-host proxies keep that bit zero. The tree refits every changed
query, sorting after topology edits or 32 refits. Bitonic sorting costs O(n log² n)
and is an explicit optimization boundary. Each query traverses without a shader
stack, emits only canonical a<b pairs and rejects same-body pairs. Ordinary body
pairs require a dynamic member and reciprocal masks; sensor candidates use the
sensor's directional mask, including static/kinematic and zero-layer sensors.
Touching bounds count as candidates. Exact geometry is handled by the narrow phase below; joint vetoes, body exceptions
and contact/overlap event publication remain later stages.

Pairs remain in a GPU buffer (16 bytes per pair: slot IDs and generations). A global
atomic count can serialize dense worlds; per-query counts/prefix scan are a candidate
only if that cost dominates measured workloads. Capacity overflow reports the full
count, grows storage and reruns just the immutable traversal. It neither truncates
pairs nor advances bodies twice. Addressability/device memory limits fail explicitly.

| Spatial operation | Traffic / synchronization |
| --- | --- |
| Warm changed broad phase | 8-byte reset upload and 8-byte error/count readback; one fence wait, plus 32-byte tree settings or 64-byte geometry/pair settings per dispatched pass. No shape, vertex, body pose, proxy or pair transfer. |
| Shape edit | 96-byte scatter command per changed attachment. |
| Geometry edit | 48-byte descriptor command plus 16 bytes per changed vertex. |
| Growth | Device-to-device copies of retained geometry/shape storage; counters include waits and copied bytes. |
| Explicit pair inspection | 16 downloaded bytes per pair, expanded into caller-owned store-qualified handles; separate fence wait. |
| Explicit bound inspection | One 32-byte proxy read; separate fence wait. |

Integration and broad phase use separate synchronous submissions. The first
wait enforces body-step error publication; the second publishes exact pair count and
failure/capacity status. No complete state validation readback occurs on a warm tick.
Combining submissions or delaying publication needs the connected solver's error and
freshness contract; these waits are recorded rather than hidden.

Run `ELECTRON2D_TEST_GPU_RESIDENT_SHAPES=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after the Release source-native test build. The full GPU suite also runs it.
Functional tests cover seven geometry families, 12-vertex convex contours, hollow
concave bounds, composed rotations/local edits, GPU motion, shared-resource edits
with throwing observers, disposal, stale/foreign/owner guards, sparse filters, body
and shape reuse, device growth and invalid GPU bounds. Full grid retirement verifies attachment-list removal.
A dense 96-body case checks
all 4,560 unique pairs and capacity retry. A brute-force authored AABB oracle checks
36 randomized 96-body layouts with 32-bit masks, four body modes and sensors across
tree refits/resorting. Bounds tolerance is 0.0001 scene units; pair sets are exact
and unordered. There is no bitwise comparison to another solver's internal state.

The performance grids use 64×64 and 256×256 moving rigid rectangles of size 2.2×2.2,
spacing 2, zero gravity and velocity (0.25, 0.5) units/s. Eight-neighbor candidates
give 16,002 and 260,610 pairs respectively. There are 384 warmup ticks and 256 sampled
ticks at 1/120 s, including periodic spatial sorts. Every unique neighbor pair is
verified after explicit full pair read, outside timing and allocation samples.
Only the owner thread's managed allocations are measured. Full physics response,
window FPS, CPU comparisons, networking, native allocations and other platforms
remain unverified by this workload.

Broad-phase baseline at `4cf94f3c6ba733f498864dbbdc81dd8d9fca99e5`,
Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti, 2026-10-08:

| Run / body count | Tick p50 | Tick p95 | Mean fence wait | Diagnostic full pair read |
| --- | ---: | ---: | ---: | ---: |
| Dedicated / 4,096 | 0.1944 ms | 0.4922 ms | 0.1475 ms | 0.4640 ms |
| Dedicated / 65,536 | 0.3038 ms | 0.8870 ms | 0.2740 ms | 2.8132 ms |
| Full GPU suite / 4,096 | 0.1836 ms | 0.5128 ms | 0.1522 ms | 0.4785 ms |
| Full GPU suite / 65,536 | 0.2941 ms | 0.7064 ms | 0.2550 ms | 1.7435 ms |

All four runs report zero warmed managed bytes per tick. Integration plus broad
phase uploads/downloads 12 status/count bytes each way, with no geometry/shape/pair
traffic. Mean uniform payload is 604 bytes/tick for 4,096 and 794 for 65,536, including
periodic sorts. Diagnostic full pair reads download 256,032 and 4,169,760 bytes
respectively, outside samples. Logs are `/tmp/electron2d-resident-shapes-final.log`
and `/tmp/electron2d-resident-shapes-gpu-suite.log`. The full suite also rechecks
legacy tree/filter/contact/joint kernels and both renderer lifetimes.

Broad-phase baseline SPIR-V SHA-256 digests (before margin uniforms/narrow phase):

- PhysicsResidentBodies: `962fb89abc612ef58e50e0acba2def47832f93f87f0ccfa5b7927ec79025da0c`.
- PhysicsResidentShapes: `29bb408276278fbd39ace2544f77e2a6d0a35a31253bb06c98a20d29ed7b6845`.
- PhysicsTree: `ee4fd8bfad41c0ca3c8291da56c5bd5384c0ec4ad872bfe6686e8e0b8c473c50`.

## Resident narrow phase

FindContacts consumes resident candidate pairs and computes ordinary convex contacts,
hollow concave-piece contacts and directed ray contacts on the GPU. No CPU shape-pair
packet or pose upload is needed. A shared geometry include keeps shape descriptors
consistent, and the original stage-hosted collision kernel shares its existing math
and segment clipping through PhysicsCollisionMath.inc.glsl. Its fixed-eight-vertex
geometry path is not imposed on the independent backend.

The independent kernel scans separating face and rounded-corner axes over the full
contour, clips incident edges, and emits up to two points per ordinary convex piece
pair. Contour size is bounded by device storage, not a fixed local shader array.
Clockwise/counterclockwise contours retain their authored vertex identities. The
accepted 0.5-unit short-segment/capsule-center fallback becomes a point/circle.
Concave geometry remains hollow: all contributing piece points survive output
capacity growth. Two concave shapes and two separation rays do not collide.
Directed rays reject containment/back-facing hits, choose the nearest concave
crossing and retain the accepted SlideOnSlope virtual-anchor/depth rule. The linked solver now executes directed impulse/material response; public ray
integration, sleep and full reports remain open.

Contact normals point from canonical shape A toward B. Signed separation is the
projection of the B-minus-A anchor difference on that normal; penetration is
negative. Local anchors are relative to each body's origin/orientation. A 64-byte
point holds shape slots/generations, 32-bit vertex/edge features and piece indices,
normal/separation/sensor role and two local anchors. These identities are local to
the store. Portable snapshots are not implemented by these records. The solver now keeps
separate versioned device impulse history, described in its linked report.

Physical speculative margin is a nonnegative scene-unit distance. Broad phase
expands each bound by half this amount; narrow phase retains separated points only
within it. Directed rays extend along their axis. Sensors always require exact
intersection, irrespective of the physical margin. Cached pairs/contacts invalidate
on body, attachment, source geometry and margin changes. Nonfinite bounds or contact
intermediates reject the store with no CPU replay. The nonfinite regression caught
an initially silent invalid intermediate; explicit shader guards now reject it.

The axis scan has quadratic cost in the current pair’s contour vertex counts and a global output counter may
serialize very dense contact batches. Those remain measured optimization boundaries.
Contact arrays retain peak power-of-two capacity. Overflow reports the complete
point count and retries only the immutable narrow phase, never body integration.
There is no sixteen-point reporting cap inside this solver-stage storage.

| Contact operation | Traffic / synchronization |
| --- | --- |
| Warm narrow phase | 8-byte reset upload, 32-byte settings and 8-byte count/error readback; one fence wait. Contact points stay on GPU. |
| Integration + broad + narrow | Three publication waits; 20 status/count bytes uploaded and downloaded per tick, plus pass uniforms. No pose, geometry, pair or contact traffic. |
| Explicit contact inspection | 64 bytes downloaded per point into caller-owned storage; separate allocation/copy/fence cost, outside normal ticks. |

Run `ELECTRON2D_TEST_GPU_RESIDENT_CONTACTS=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after a Release source-native test build. The full GPU suite includes this target.
Analytic circles/box faces verify signed depth and boundary anchors within 0.00001
scene units; directed anchors use 0.0001. A seeded set of 1,620 rotated cases covers
all seven families, both polygon windings and both ray modes, comparing contact
presence to standalone resource collision regions. Random-case reconstructed world
anchors allow 0.001 units for float rotation/translation near coordinates −12–3,264;
normal-length tolerance is 0.0001. A 200-piece concave test forces capacity recovery
and verifies all piece identities. Edits, retirement, disposal, sensor/speculative
rules, stable translated features and actual nonfinite-device failure also execute.

The same 64×64 / 256×256 grids above generate 32,004 / 521,220 contact points.
Warmup is 384 ticks, followed by 256 sampled ticks at 1/120 s. Every point's depth
is checked analytically against actual device body poses within 0.0001 units after
the timed window. The initial fixed-0.2-depth assumption failed because accumulated
float integration changes neighbor spacing across exponent boundaries; observed
depths are 0.199542–0.200916 and 0.192676–0.209766. The corrected check preserves
contact accuracy rather than expanding its tolerance. Contact/pose diagnostic reads
are separately timed twice: first use (including transfer-buffer growth/page first
touch where applicable), then reuse of the same buffers and destination arrays. Both
are single diagnostic timings, not latency distributions. These reads are excluded
from managed-allocation/tick samples.

Measured on Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti, 2026-10-08:

| Run / bodies | Tick p50 | Tick p95 | Tick p99 | Mean wait | Contact read first / warm | Pose read first / warm |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Dedicated / 4,096 | 0.2551 ms | 0.5643 ms | 0.7997 ms | 0.2049 ms | 0.9632 / 0.6093 ms | 0.7537 / 0.1113 ms |
| Dedicated / 65,536 | 0.5305 ms | 1.0969 ms | 1.3174 ms | 0.4839 ms | 18.2608 / 10.6991 ms | 0.9885 / 0.8236 ms |
| Full GPU suite / 4,096 | 0.2488 ms | 0.5234 ms | 0.8124 ms | 0.1995 ms | 1.1332 / 0.7031 ms | 0.1934 / 0.0935 ms |
| Full GPU suite / 65,536 | 0.5253 ms | 1.1082 ms | 1.3110 ms | 0.4782 ms | 19.2539 / 10.4956 ms | 1.2394 / 0.9306 ms |

Each run reports zero warmed owner-thread managed bytes per tick. Uniform payload
averages 668 / 858 bytes per tick, including periodic tree sorts. Explicit contact
reads download 2,048,256 / 33,358,080 bytes; diagnostic pose reads additionally upload
16 bytes and download 32 bytes per body plus status/uniforms. These full reads are
test-only consumers, not a proposed per-tick API requirement or CPU mirror. The
large measured readback cost is why the next constraint stage must consume the
resident contact buffer directly. Logs: `/tmp/electron2d-resident-contacts-test.log`
and `/tmp/electron2d-resident-contacts-gpu-suite.log`. This is still partial-pipeline
evidence: no whole CPU-vs-GPU physics, window FPS, networking, native allocations or
cross-platform acceptance follows from it.

Contact-generation baseline at `87b7f9ce85ed5e817f149837108dc434bacc80af` SPIR-V SHA-256:

- PhysicsCollide: `0301c25ded7990fc77a2a7b2c4e40a302992865bd73af78ba49033b26dd01ebd`.
- PhysicsResidentShapes: `e95f0603dd16b117adc89370fb2d17bd7076e910415d45ea6bd8ad98ea93f53e`.
- PhysicsResidentContacts: `e05711ef8609708cc869e6d2668ba0ffd8964d2c0fe8247f8faa9b7d9d5cb9db`.

## Native lifecycle observation

The first compute-only run passed motion/edits and failure checks, then hung during
repeated SDL video initialization with GdkDisplayManager registration errors. The
process inherited GDK_BACKEND=x11 in a Wayland session. GPU initialization had
bypassed the environment preparation already used by DisplayServer. Both paths
now call that shared helper; the same resident run and full GPU suite then finish.
Local logs retain the failed run and follow-ups under
`/tmp/electron2d-resident-bodies-{glib-failure,final,gpu-suite}.log`.
Nonfatal gtk_disable_setlocale warnings can still appear on repeated initialization.
The earlier distinct GLib impossible-allocation observation in GPU status remains
open; no stack evidence establishes that it had the same cause.

## Component velocity writes

The scene/server adapter queues linear and angular assignments independently through
`SetSolverLinearVelocity` / `SetSolverAngularVelocity`. Preserving the untouched
component happens on device. A pose followed by either setter no longer flushes
wakes and reads a one-body snapshot just to reconstruct a complete velocity value.
The shared snapshot epoch is still invalidated; a real getter, axis projection,
query, callback or subsequent step observes the ordered changes through existing
publication. No live velocity mirror or new public API is introduced.

Two unused bits in the existing command mask select XY or Z. Body storage remains
96 bytes, commands remain 176 bytes, and CPU slots retain only authored metadata.
A partial assignment replaces earlier impulses on its own component and preserves
the other component's impulses. Later impulses still apply. Full assignments replace
both components and clear both pending partial masks. Sleep clears both velocities;
a following partial write wakes the body without reviving its other old component.
Rotation-lock/role changes and subsequent explicit writes retain their order.
Virtual surface motion stays separate from solver motion. Shader history versions
compare only the affected velocity components; private sleep-clock changes alone
do not turn a partial assignment into a whole-velocity edit.

`ELECTRON2D_TEST_PHYSICS_VELOCITY_EDITS=1` exercises the public CPU/GPU scene/server
contract, direct-state setters and custom integration callbacks, impulses, sleep,
locks, axis projection and invalid/foreign/owner guards. It checks that a prepared
pose/linear/angular write sequence causes **zero GPU submissions/readback** before
an explicit observation. Resident-command checks also cover writes before creation
publication, coalesced impulses, full/partial replacement and stationary surfaces.
Nonintegrated motion comparisons use .0001 scene units/s and rad/s tolerance for
CPU unit conversion; identity/lifetime/traffic assertions are exact.

### Complete-path measurements

Linux x64, .NET 10.0.1 Release, Vulkan/RTX 3090 Ti. The unchanged
`ELECTRON2D_TEST_PHYSICS_CONTACT_DEBUG=1` fixture contains 256 independent
static/dynamic circle pairs, 512 real bodies, 96 warmups and 64 measured iterations.
Each iteration resets every dynamic pose/linear velocity and runs a full 1/60 s
step. Enabled diagnostics use cap 128; no rendering is included in this table.
Before is `1dedab39`; after is the component-write implementation on that base.
CPU/GPU controls run on the same after source and identical fixture.

| Path | Whole p50 / p95 / p99 ms | Reset / step p50 ms | Reset submissions | GPU upload / readback B per iteration | GPU mean wait ms |
| --- | --- | --- | ---: | --- | ---: |
| CPU after, contacts off | .3676 / .4206 / .4346 | .0707 / .2965 | 0 | 0 / 0 | 0 |
| CPU after, contacts on | .3712 / .4335 / .5215 | .0689 / .3000 | 0 | 0 / 0 | 0 |
| GPU before, contacts off | 54.0535 / 64.8426 / 69.4052 | 51.1207 / 3.2462 | 768 | 98496 / 63696 | 35.3224 |
| GPU after, contacts off | 2.9684 / 3.9388 / 5.1509 | .0667 / 2.9018 | 0 | 45248 / 41168 | 1.6692 |
| GPU before, contacts on | 58.3084 / 71.0705 / 133.0743 | 54.7341 / 3.4701 | 768 | 98496 / 64728 | 38.4161 |
| GPU after, contacts on | 2.9579 / 4.3234 / 4.7088 | .0667 / 2.8920 | 0 | 45248 / 42200 | 1.7825 |

All these warmed intervals allocated **0 owner-thread and 0 all-thread managed
bytes**. Logs: `/tmp/e2d-velocity-baseline-perf.log` and
`/tmp/e2d-velocity-perf1.log`. Removing setter readback improves this GPU whole path
about 18 times with contacts off, but **CPU is still faster** on this small scene.
This is an adapter improvement, not evidence of solver or massive-scene superiority.
The count and features are unchanged. First capacity growth and native/driver
allocations are not measured. Impulse validation and explicit getters still have
real read consumers; these commands do not remove their existing waits.

### Real-window measurement

`ELECTRON2D_TEST_PHYSICS_VELOCITY_EDITS_NATIVE=1` runs the same 256 circle pairs as
scene bodies in an actual 640-by-520 GPU-rendered window. Every fixed tick writes
pose, linear and angular velocity for all dynamics, then renders their real scene
poses. It warms 96 physics ticks and measures four seconds, excluding image capture
and shutdown. Fixed-step target is 60 Hz; `Engine.MaxFPS=0`. The request to disable
VSync fell back to **Enabled**, so the measured presentation rate is capped near
144 Hz. This check establishes sustained output under that cap, not uncapped FPS.

After optimization, CPU measured 143.83 FPS / 59.93 physics ticks/s and GPU measured
143.56 FPS / 59.92 ticks/s. Frame p50/p95/p99: CPU 6.9520/7.0153/7.0685 ms; GPU
6.9492/8.7416/10.1794 ms. Both measured zero owner-thread managed bytes over their
four-second windows. GPU's 240 ticks submitted 6480 batches, uploaded 10,859,520 B,
read 9,880,320 B and spent 633.012 ms in measured waits. Captures
`/tmp/e2d-velocity-window-after-{CPU,GPU}.png` verify actual body pixels; log
`/tmp/e2d-velocity-native1.log`. All-thread/native allocation, other renderers/platforms,
65,536-body sustained performance and authoritative networking remain separate gates.

The identical native harness built against the unmodified `1dedab39` runtime
measured CPU 143.31 FPS / 60.17 ticks/s. Baseline GPU produced only **1.46 FPS and
11.66 ticks/s**: six rendered frames and 48 physics ticks in 4.117 s. Its frame
p50/p95/p99 were 691.9386/711.3140/711.3140 ms; with only six frames these are a
short-stall observation, not a long-run latency distribution. The host performed
its capped catch-up ticks between renders. Those 48 ticks submitted 75,024 batches,
uploaded 7,283,712 B, read 4,138,752 B and waited 2820.160 ms; owner managed bytes
were zero. Per-tick submissions therefore fell from **1563 to 27** while the new
version sustained the requested 60 Hz. The baseline/after source has the same
population, edits, renderer, warmup and four-second measurement policy; actual work
completed differs because the old version cannot keep up. Log:
`/tmp/e2d-velocity-window-baseline.log`; captures
`/tmp/e2d-velocity-window-before-{CPU,GPU}.png`.

## Late capacity growth

Resident storage/transfer growth now reuses private managed RenderHandle wrappers. It still creates native allocations, preserves data where required, waits for the copy, and retires the previous native allocation. The wrapper keeps its original device reference and release callback, so a larger contact batch after warmup no longer creates a new delegate and SafeHandle for each grown buffer. Published graphics resources and scene/body RID identities are unchanged. The 64-body full-scene control exercises twelve such late buffer increases with zero owner/all-thread managed bytes after the fix; this does not claim zero native allocation or eliminate growth waits.

The existing PhysicsPipelinePerformance runner now compares CPU/GPU ordinary RigidBody scenes with contact monitoring and interpolation. Whole PhysicsFrame timing includes scene publication and observer work; solver phases, GPU transfers/waits and memory are separate report fields. All sampled bodies remain awake; physical/identity gates use public API rather than native CPU handles. This extends the raw-body performance evidence and keeps the complete extension/native/platform/window gates open.


For resident CCD against a rotating plane, resting-contact advancement now bounds
the actual plane-support acceleration and its third derivative, with a bounded
fourth-derivative remainder over the remaining interval. A convex quadratic bound
uses its endpoint maximum across all finite support vertices. The earlier
radius-only curvature overestimate exhausted 128 intervals for a radius-one circle
near a 20-rad/s dynamic plane despite representable contact motion. Interval and
shader iteration budgets, .001 CCD tolerance, failure behavior and device-resident
execution remain unchanged. The same new scenario and existing continuous suites
verify the correction; no CPU numerical fallback or mass-state readback is added.


The offset-center directed arc also exposed the resident circle-entry quadratic:
its float discriminant cancelled large products and retained an exit/tangent
report without the required entry impulse. The shared resident ray-circle kernel
now projects onto a normalized ray basis, computes the half chord from perpendicular
distance and forms the entry normal in that basis. It retains the same facing
threshold, geometry, budgets and device-float portability. All users of the shared
kernel, including capsule caps, contacts, queries, motion and CCD, receive the
same correction. No FP64 requirement, CPU geometry fallback or extra readback is
introduced. Full continuous/query regressions remain separate from provider
acceptance and hardware performance.


The complete offset-center angular scenario still required a CCD correction after
the stable circle-root change: a near separated proxy without a directed entry
was incorrectly treated as no hit for the entire remaining interval. The resident
CCD kernel now retains the containment exclusion and continues projected-velocity/
curvature refinement until a real directed entry, keeping its existing 256-iteration
and 128-interval budgets. The first speed-only continuation exhausted refinement
and was rejected. A focused --directed-angular case uses only the public API and
retains contact/energy assertions; its successful GPU probe records a real nonzero
entry impulse. Earlier cohorts before this correction are not final acceptance.
