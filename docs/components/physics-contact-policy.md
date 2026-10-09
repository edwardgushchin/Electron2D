# World and shape contact policy

Last updated: 2026-10-09

## Authored policy

[ADR 0098](../decisions/physics-contacts.md#adr-0098) defines Shape.CustomSolverBias
and typed PhysicsServer world getters/setters for default bias and allowed penetration.
[PhysicsContactSettings](../classes/PhysicsContactSettings.md) validates the shared
policy. New worlds sample ProjectSettings feature overrides: bias 0.8 and slack
0.3 scene units. Existing worlds retain their settings. Zero shape bias inherits
the world, one nonzero shape wins, and two nonzero biases use their arithmetic mean.
World zero disables correction. Slack suppresses correction of shallow penetration
without suppressing contact reporting, physical velocity constraints or materials.

Bias requests a fraction of excess penetration per outer tick. Each prepared interval
uses `1 - (1 - bias)^(interval / tick)`. Coupled constraints, correction-speed caps,
float precision and CCD clipping of already prepared velocities can delay convergence;
this is not a promise of exact geometric displacement for arbitrary contact networks.
Changing tick duration does not multiply the nominal fraction for an isolated contact.

World changes wake dynamics and reset quiet timers; equal writes preserve them.
Shape changes publish a separate policy epoch before Changed observers. Borrowers
observe a committed edit even if a callback throws. Geometry revisions, fixture/RID
identity, mass data and one-way contact history remain unchanged. CPU fixture tags
retain a weak authored resource reference, update only policy and wake touching
bodies. CollisionShape geometry uses the existing revision path instead of treating
every Changed event as a fixture rebuild. Copies publish geometry revisions; all
eight concrete shapes preserve inherited policy through duplicate, in-place copy
and built-in .e2dres storage.

## Solver execution

CPU scalar overflow and SIMD contacts cache the mixed bias at contact update and
prepare correction against the outer tick duration. Correction velocity is relaxed
out before physical momentum publication. Rotation locks mask solver inverse inertia
as well as final angular velocity; unlock restores authored inertia. This avoids
losing normal response into a forbidden rotational degree of freedom.

Virtual linear/angular surface velocity contributes contact-point displacement to
separation during position/relaxation stages. The surface pose remains fixed, but
relaxation no longer cancels the physical impulse transferred to a departing body.
CCD clips the initial restitution interval even when position correction separates
the bodies, so the outgoing bounce velocity advances during the remaining tick.

The older CPU-host/GPU-stage solver packs the computed contact correction rate/slack
into existing contact-input padding. Its step uniform is 80 bytes (formerly 64),
including elapsed surface displacement time. CPU remains the owner of that path.

Independent GPU shapes store bias in an existing padding word of the 80-byte shape
record; policy changes upload metadata without vertices or mass recompilation.
The device mixes shape/world biases and applies the interval exponent. The shared
resident solver uniform is 80 bytes (formerly 64); all four consuming kernels use
the same layout. Its correction channel also keeps inherited joint bias separate
from the contact default. Unspecified diagnostic arguments use the captured contact
policy and project constraint bias; an explicit correctionFactor retains the internal
diagnostic override for both. No host physical pose or contact calculation is added.
Public independent-GPU world binding remains open.

## Verification and limits

PhysicsContactPolicyTests covers seven finite/directed body geometries against an
analytic boundary, shape bias mixing, zero/full bias, slack, live edits and failed
observers, resource copies/storage, shared CPU scene/server state, phase/thread
validation and momentum/contact reporting. A 12-plane case exercises overflow and
dense incidence. Isolated penetration is checked within 0.003 scene units, dense
correction within 0.01; CCD-interval correction allows 0.004. One/four/eight resident
substeps and 0.01/0.04-second ticks check fractional policy. Surface and CCD regression
suites retain physical-response and frame-impulse assertions. GPU joint policy tests
verify that contact bias does not change inherited joint correction.

The allocation probe alternates one shared shape's bias before every active full
1/60-second step: 256 warmup, 128 measured samples. It measures policy propagation,
solver work and reporting storage, with no pose readback in the measured loop. It is
an overhead probe, not evidence of GPU speedup at large body counts. Native allocation,
other platforms/devices, window FPS, networking and public GPU selection remain
outside this acceptance. The CPU collider suite, full GPU suite and resource archive suite passed;
these checks do not establish owner visual acceptance.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti; one active contact:

| Execution path | p50 / p95 / p99, ms per edited step | All-thread managed bytes over 128 samples |
| --- | --- | --- |
| Public CPU, dummy video | 0.0048 / 0.0049 / 0.0052 | 0 |
| CPU host/GPU stages | 0.2230 / 0.2562 / 0.3785 | 0 |
| Independent resident GPU | 2.8037 / 3.4005 / 4.3771 | 0 |

The resident loop uploads 472 B of buffers and 27,604 B of uniforms and reads
200 B of status per step; included waits average 1.4645 ms. Logs:
`/tmp/electron2d-contact-policy-collider.log` and
`/tmp/electron2d-contact-policy-gpu-suite.log`. This deliberately tiny case exposes
submission overhead; it is not a large-world CPU/GPU performance comparison.

## Solver iterations

SpaceGetSolverIterations and SpaceSetSolverIterations use a positive integer, sampled
from Physics2DSolverIterations (default sixteen). Existing worlds retain their count
when project defaults change. Invalid/equal values preserve prior state; a changed
count wakes dynamics and resets quiet timers. An explicit sleep written afterward
wins. Owner/solver/lifetime validation applies to both accessors.

The count controls complete contact and joint sweeps within each time substep.
CPU repeats correction-enabled solving and relaxation separately; the independent
GPU repeats its coupled physical/position constraint passes. Integration, force
consumption, restitution and nominal joint/CCD budgets do not repeat per sweep.
Different algorithms need not converge equally fast at the same count.

CPU keeps one descriptor per stage/color and reuses it for all sweeps. Block claims
and cancellation use 64-bit atomics with a 48-bit phase ordinal, avoiding wrap at
65,536 phases without a policy clamp or per-iteration storage. Raw private backend
worlds keep one sweep for diagnostic controls; public world creation applies the
captured setting. Resident optional per-call iterations remain diagnostic overrides
and do not change the stored world value or its getter.

PhysicsSolverIterationTests runs the same eight-body contact and pin-chain cases on
CPU, stage GPU and resident GPU. Initial momentum is 60 kg*u/s and the converged
common speed is 7.5 u/s. After a 1/60-second tick, 128 sweeps must reduce RMS velocity
error below 40% of the one-sweep result and below 0.5 u/s; this latter bound is under
7% of the equilibrium speed. Momentum error is below 0.03 kg*u/s. A separate free
body moving at 40 u/s advances 0.8 u over 0.02 seconds within 0.00003 u at 1/16/128
sweeps, checking that iteration count does not multiply time. The same free-body
check accepts int.MaxValue sweeps: worlds without contacts/joints skip the empty
iteration loop entirely. The prior attempted
32-sweep common convergence budget was insufficient for the resident Jacobi chain
(1.601325 u/s contact RMS); the convergence test uses 128 on every path instead of
assuming CPU and GPU convergence rates match.

A CPU-only 256-body/128-pair check executes 8193 sweeps per each of four substeps,
with multiple graph work blocks and more than 65,535 phases. Storage prepared at
one sweep remains sufficient, and the measured high-count step allocates zero
managed bytes. Worker failure cancellation is covered separately by PhysicsParallelTests.

Small-world measurements use 256 active bodies in 32 eight-circle rows, shared
4 u/s velocity, no gravity/friction/correction, four substeps, 128 warmup and 128
samples per count. These are controlled iteration-cost probes with all bodies and
contacts retained, not large-world GPU speedup or window-FPS acceptance.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti, 256-body probe:

| Path | Sweeps | p50 / p95 / p99, ms | Managed bytes / 128 ticks |
| --- | --- | --- | --- |
| Public CPU, dummy video | 1 | 0.1136 / 0.1450 / 0.1550 | 0 |
| Public CPU, dummy video | 16 | 0.3670 / 0.4000 / 0.4055 | 0 |
| Public CPU, dummy video | 32 | 0.6443 / 0.6900 / 0.7388 | 0 |
| CPU host/GPU stages | 1 | 0.4677 / 0.7663 / 0.9880 | 0 |
| CPU host/GPU stages | 16 | 2.0894 / 2.4477 / 2.5976 | 0 |
| CPU host/GPU stages | 32 | 3.9832 / 4.3127 / 4.4319 | 0 |
| Independent resident GPU | 1 | 1.6085 / 2.2604 / 3.2013 | 0 |
| Independent resident GPU | 16 | 2.1984 / 2.8653 / 3.7316 | 0 |
| Independent resident GPU | 32 | 2.8541 / 3.5612 / 4.5762 | 0 |

Resident upload/readback remain 160/160 B per tick at all counts, with no pose reads
in the measurement loop. Uniform traffic is 7,480 / 17,080 / 27,320 B and included
mean wait is 1.0334 / 1.3143 / 1.6409 ms at 1/16/32 sweeps. CPU's 8193-sweep step
completed in 64.065 ms with zero managed bytes. Contact/pin RMS error at 128 sweeps:
CPU and stage GPU about 0.000002 u/s, resident GPU 0.071448 / 0.028477 u/s. These
figures establish convergence and expose small-workload dispatch cost; equal counts
do not promise equal numerical work or GPU acceleration. Logs:
`/tmp/e2d-iterations-final-cpu.log`, `/tmp/e2d-iterations-final-stages.log`,
`/tmp/e2d-iterations-final-resident.log`. CPU/GPU broad suites and parallel failure
checks passed; focused checks were repeated after the empty-iteration optimization. Native allocation,
other devices/platforms, real-window FPS, public GPU selection and network acceptance
remain unverified by this iteration-setting slice.

## Contact history limits

SpaceGet/SetContactRecycleRadius and SpaceGet/SetContactMaxSeparation complete the
nine typed world-parameter capabilities. New worlds capture project defaults of
1 and 1.5 scene units respectively. Both accept finite nonnegative distances whose
scene-unit square is finite; a positive distance must also retain a nonzero squared
backend distance. Invalid writes preserve the policy. Changed writes wake dynamics,
equal writes preserve sleep, and a later explicit sleep takes priority.

These settings bound reuse of cached normal/tangent impulses. Current collision
geometry is recomputed each interval, independent of the history limits. Both
body-local boundary anchors must move strictly less than the recycle radius.
Reprojecting the old anchors at current poses must leave normal separation and
tangential drift no greater than maximum separation. Zero radius disables reuse;
zero maximum separation still permits a stable penetrating contact. Neither value
enlarges query margins or delays collision-exit events.

CPU and GPU prefer a surviving feature, then the closest eligible old anchor pair.
Body/shape generations, geometry and participation guards remain; old/new normals
must have dot product at least 0.99. Each cached impulse can serve only one new point.
Resident GPU matching uses the pair/piece hash and an atomic claim word, retrying
when another point claims its candidate. Claims and history stay on device; this
introduces no CPU pose mirror, history computation or history readback.

CPU manifold points now retain two local boundary anchors (16 additional bytes per
point). Resident history records grow from 80 to 96 bytes; the existing 80-byte
solver uniform has room for both limits. The older CPU-host/GPU-stage path already
mirrors contact state: its uploaded/matched histories grow from 32 to 80 bytes,
solver inputs from 128 to 160, working contacts from 208 to 240 and collision
uniforms from 16 to 32. Those costs belong to that diagnostic path; they do not
introduce a host contact mirror in the independent backend.

PhysicsContactPersistenceTests checks all seven finite/directed body geometries
and an infinite boundary on CPU, stage GPU and independent GPU. A sliding body
retains contact support and its 400 u/s tangent speed as history is enabled,
disabled and rejected separately by radius or separation. Its reported per-frame
impulse remains (0, -9.8) kg*u/s within 0.03, balancing gravity over the 0.01-second
tick. A CPU/stage two-point fixture replaces feature IDs and verifies nearby-anchor
fallback without duplicating its one old impulse. A tilted resident box exercises
three competing-point intervals; reusable impulse count bounds the number of
warm-started points. Settings capture, invalid/thread/phase access, scene events,
sleep ordering and exact scalar boundary cases also execute.

The warmed allocation probe alternates radius 10/0 before each full tick of one
sliding body, with four substeps and sixteen sweeps. It measures 128 ticks after
256 warmup ticks without pose/report readback inside the measured loop. Measurements
and remaining acceptance limits are recorded below; this tiny case measures dispatch
overhead and does not establish GPU speedup at large body counts.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti:

| Path | p50 / p95 / p99, ms per edited tick | All-thread managed bytes / 128 ticks |
| --- | --- | --- |
| Public CPU, dummy video | 0.0172 / 0.0174 / 0.0195 | 0 |
| CPU host/GPU stages | 0.9778 / 1.5713 / 1.6707 | 0 |
| Independent resident GPU | 2.4119 / 3.6464 / 4.3695 | 0 |

Resident per-tick mean traffic is 374 B uploaded, 200 B of status read back and
17,299 B of uniforms; included waits average 1.4165 ms. The small workload remains
slower on GPU. Logs: `/tmp/e2d-history-final-cpu.log`,
`/tmp/e2d-history-final-stages.log`, `/tmp/e2d-history-final-resident.log`.
The CPU collider and full GPU regression suites passed; focused suites were repeated
after tightening rejection of squared backend-distance underflow. Native allocation,
other platforms/devices, rendered FPS, large-world throughput, public GPU binding
and network acceptance remain outside this contact-history slice.

## Collision priority

CollisionObject.CollisionPriority and PhysicsServer.BodyGet/SetCollisionPriority
retain one finite positive obstacle weight, default one. Scene/server access shares
that value even while detached, with owner-thread/step guards once attached. The
inherited descriptor participates in PackedScene. Area retains the property but
remains nonblocking. Priority edits do not rebuild fixtures, wake bodies or change
rigid mass/material/impulse response. They affect BodyTestMotion penetration recovery
and therefore PhysicsBody and CharacterBody movement through that shared path.

Each of four recovery attempts keeps up to 32 deepest accepted contact planes.
Sequential correction uses 40% of depth beyond 5% of the query margin, weighted by
obstacle priority normalized to mean one. A total below 0.00001 uses the raw weights,
retaining the near-zero policy. The moved body's own priority does not enter this
calculation. One-way direction/margin, disabled shapes, reciprocal masks, exceptions,
caller exclusions and logical contact identities retain their existing rules.
Candidate/tie order is an internal detail; public results preserve the observed
collision and motion contract rather than identical CPU/GPU point lists.

CPU recovery uses 512 bytes of stack scratch. Partitioned convex shapes contribute
the full contour once. Their motion manifold now clips an incident edge against
the selected reference face and retains up to two genuine boundary points; reducing
a face to one deepest point lost part of its weighted recovery. Single-point and
rounded-feature contacts keep the existing fallback. Directed rays choose one outer
entry across a convex contour's cached partitions; internal pieces do not gain
additional recovery weight. No physical body is moved by
a test-only query; normal scene movement applies the returned travel.

GPU recovery reserves 512 bytes per request in the device-only tail of the existing
output buffer; the 128-byte public result prefix is the only payload read back.
Each pair emits at most two points before recovery stores them, avoiding repeated
scratch scans inside the shared geometry callbacks. Surface.W stores
the authored weight in the existing 96-byte body record. Sparse edits use the
existing 176-byte command and a distinct mask bit. Surface/role edits preserve the
weight. A batch containing only priority edits leaves spatial versions, contact
history and sleep unchanged, so subsequent queries reuse the spatial tree.
There is no body/contact state readback beyond the existing requested query result.

The GPU normalizes weights through significands and exponent differences: direct
division by float.MaxValue can flush its reciprocal to zero on this Vulkan device.
The first candidate consequently returned a roughly 2.83e38-unit displacement in
the maximum-weight test; that candidate was rejected. Explicit non-unrolled loops
bound the recovery shader's repeated scratch scans. Measurements below distinguish
cold setup from warmed execution; this slice does not establish full GPU-world
selection, large simulation speedup, native allocations, window FPS or networking.

PhysicsCollisionPriorityTests checks an analytic circle penetrating two orthogonal
planes, priority ratios and common scaling, maximum finite weights, the near-zero
normalization threshold and subnormal values. The normal case allows 0.002 scene
units (0.02% of the circle diameter); tiny motion allows 0.0000001 units. Swapping
9:1 priorities must change the dominant recovery direction by more than one unit.
Forty-one contacts exercise the 32-plane cap, retaining the deeper perpendicular
plane. Scene tests cover test-only and applied movement, CharacterBody, nonblocking
Area, PackedScene, live scene/server projection, lifecycle/phase/thread rejection
and unchanged sleep/RID/fixtures. GPU tests additionally verify device sleep and
priority across role/surface edits, stale handles and spatial-tree reuse.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti, three bodies, one full
supplied-pose recovery query and one changed obstacle weight per sample:

| Path | Warmup / samples | p50 / p95 / p99, ms | All-thread managed bytes over samples |
| --- | --- | --- | --- |
| Public CPU, dummy video | 128 / 128 | 0.0019 / 0.0019 / 0.0020 | 0 |
| Independent resident GPU | 128 / 128 | 0.1587 / 0.3727 / 0.6072 | 0 |

Resident traffic is 280 B uploaded and 148 B read back per edited query, including
status, sparse authoring, the query and its single result; mean included wait is
0.1514 ms. Logs: `/tmp/e2d-priority-verified-cpu.log`, `/tmp/e2d-priority-verified-gpu.log`.
The full CPU collider and GPU suites passed, including both renderer lifetimes.
The GPU suite's unchanged 65,536-obstacle/256-query workload measured
1.2059/1.5792/2.1434 ms, with 22,536/33,800 B upload/readback per batch and zero
owner/all-thread managed allocation. This larger measurement covers queries, not
full simulation or window FPS; evidence is `/tmp/e2d-priority-final-gpu-suite.log`.

Cold pipeline preparation is a separate unresolved backend cost. The diagnostic
`ELECTRON2D_TEST_PHYSICS_PIPELINE=/absolute/file.spv` loads the offline program through
the normal GPUPhysicsDevice/ShaderCompiler path and releases the pipeline. Fresh
processes with `__GL_SHADER_DISK_CACHE=0` measured 134,340.63 ms for the motion shader
from parent c9193423 and 136,650.00 ms for this shader; device startup was
730.28/670.08 ms separately. Both sources used the repository's Vulkan 1.0 compiler
recipe. Logs: `/tmp/e2d-priority-pipeline-baseline.log` and
`/tmp/e2d-priority-pipeline-current.log`. This is one comparison on the stated
machine, not a cross-device guarantee. The long preparation already existed before
priority support; moving scratch storage and extracting per-pair collection did
not resolve it. It must be addressed before full independent-GPU readiness.
