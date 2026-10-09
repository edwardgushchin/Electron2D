# Resident GPU physics stages

Last updated: 2026-10-09

## Implemented boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) is the first independent
device-state component under [ADR 0054](../decisions/physics.md#adr-0054). It creates
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
It is not a complete physics backend: independent GPU public joint adapters,
scene/server selection/publication and networking remain open. [Resident CCD](gpu-resident-ccd.md) now executes internally. Automatic/custom [mass profiles](gpu-resident-mass.md) now use shared authoring
geometry and center-aware device motion/constraint preparation. [Connected sleep/wake](gpu-resident-sleep.md) now executes on GPU. Step
remains an integration-only control; FindContacts computes contact points.
Its partial-pipeline timings cannot be compared with full CPU physics or reported
as window FPS. These missing consumers must be connected to resident state before
the independent GPU objective is satisfied.

## Storage, transfers and waits

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
