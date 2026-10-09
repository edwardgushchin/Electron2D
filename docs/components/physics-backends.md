# Physics backend selection and shared worlds

Last updated: 2026-10-10

## Public selection

`PhysicsServer.SpaceCreate(PhysicsServer.Backend backend, bool allowCPUFallback = false)`
creates a caller-owned, initially inactive space. The parameterless overload still
selects CPU. `SpaceGetRequestedBackend`, `SpaceGetBackend` and
`SpaceGetBackendFallbackReason` report immutable selection metadata, including for
a failed world. An invalid/freed space RID rejects; metadata reads do not run a step.

`new World(PhysicsServer.Backend backend, bool allowCPUFallback = false)` records the
same policy for lazy physics initialization. `RequestedPhysicsBackend` does not
initialize it; `PhysicsBackend`, `PhysicsFallbackReason`, `Space` and direct physics
access do. Resource duplicates retain the same runtime, choice and identity. These
constructor options configure runtime ownership, not a serialized solver checkpoint.
Existing parameterless World creation selects CPU.

```csharp
using var world = new World(PhysicsServer.Backend.GPU, allowCPUFallback: true);
using var viewport = new SubViewport { World = world, Size = new(800, 600) };
// Add ordinary RigidBody, Area, CharacterBody and Joint nodes to the viewport.
Console.WriteLine($"{world.RequestedPhysicsBackend} -> {world.PhysicsBackend}");
Console.WriteLine(world.PhysicsFallbackReason);
```

Choice is independent of canvas rendering. CPU does not initialize graphics. GPU
uses the resident SDL compute store and creates no B2World. Allowed fallback handles
GPU startup capability/native-load failures during construction and retains their
message. It does not catch a started physics interval. Device failure invalidates
the store; subsequent queries/steps reject while release remains available. No CPU
replay or hidden backend change follows a failed GPU step.

## Runtime flow and ownership

PhysicsSpace retains common scene/server registration, activity, owner-thread guards,
attachment generations, callbacks and events. PhysicsColliderBackend selects CPU
fixtures/body state or resident body/shape handles. Shapes retain logical owner indices
and object/canvas identities. Mass, forces, damping, CCD and one-way settings reach the
selected implementation. PhysicsJointBackend adapts the same sampled frames and
pin/groove/spring policy to either solver. World-anchor pins need no hidden GPU body.

GPU preparation applies authored edits, fields and per-body integration policy. Device
simulation includes contacts, joints, integration, sleep and CCD. Changed-state publication
updates observable body caches before scene poses and live-state callbacks. Contact
reports are requested only for configured receivers, preserving capped frame impulses.
Scene/raw Area monitoring runs resident geometry queries and reuses existing pair/event
trackers. Directional masks, monitorability and object associations remain common.
Queries, motion tests and scene ray/shape caches use the independent kernels. Area audio
point containment also uses GPU geometry rather than empty CPU fixture lists.

The GPU field adapter assigns current common-world traversal rank so equal-priority
Area order survives later scene/server insertion and removal. Explicit collision
exceptions synchronize only when authored membership changes; joint exclusions remain
owned by the device joint store. Immediate public wake reads propagate queued changes
through the last solved device graph without advancing time. A later explicit sleep
flushes earlier wakes first, preserving call order. No CPU contact/island mirror is used.

## CPU caches, transfers and waits

Every attached adapter caches a 64-byte observable snapshot for scene transforms,
server pose getters and direct-state callbacks. This is publication data, not a second
solver. RID/slot registries retain identity and authoring metadata. A shared epoch
invalidates cached reads in constant time; changed-state publication refreshes them.
An immediate dirty body getter may issue a selected GPU read. Bulk frame publication
uses the [changed-state stream](gpu-body-publication.md); its device history/output
and host scratch remain separately accounted for.

Completion publishes changes synchronously. Preparation does so only when pending
forces or body-snapshot consumers need pre-step activity; see the conditional-publication
contract below. Contact receivers
download their requested capped records; Area monitoring issues queries per local shape.
Those costs are part of the complete SpaceStep and must not be omitted from CPU/GPU
comparisons. Immediate connected-wake publication adds a status read/fence when a previous
device graph exists. Native allocation, CPU cache capacity and large-scene throughput
still need the full goal's comparative acceptance; resident-kernel timings alone do not
establish it.

## Verification and remaining work

`ELECTRON2D_TEST_GPU_SPACE=1` selects the same public server/scene scenarios on CPU and
GPU: impulses, mass, falling/resting contacts, object identity, Area events, callbacks,
point/ray/shape/rest/contact/motion queries, reentry, pin/motor, groove and spring.
The no-device child process checks CPU execution, explicit GPU startup failure and
observable allowed fallback. A separate GPU failure scenario checks no post-step CPU
replay. Shared tests use physical bounds in scene units; they do not require identical
backend traversal or solver bits. RayCast/ShapeCast attached disposal also exposed and
fixed the shared internal processing-stop guard during Node teardown.

This is the first connected selectable backend implementation. Full conformance across
all existing physics suites, targeted performance advantage, long-running rendered FPS,
other device/platform profiles, backend extensions, remaining picking/debug/tile API
and authoritative CPU-server/GPU-client networking are still required by ADR 0054.
## Verification record (2026-10-09)

Linux x64, .NET 10.0.1, NVIDIA GeForce RTX 3090 Ti/Vulkan. Both the complete
CPU collider suite and complete existing GPU suite passed. The latter now also
advances public CPU and independent GPU worlds in native windows under each
renderer (`gpu` and `compatibility`), and checks retained compute access after
renderer teardown. This is a short native lifetime check, not visual or long-run
FPS acceptance. The public GPU-space suite passed startup fallback, terminal
step failure, audio containment, live exceptions and frozen rotation-lock behavior.

A first native fixture tried reading child poses after Engine.Run had disposed the
window tree; it failed with ObjectDisposedException. Sampling before Quit fixed
the test. A first 4,096-body benchmark with only 192 warmup ticks found 663,576 B
of CPU contact-capacity growth. Both backends now get the same 768-tick warmup;
zero owner/all-thread managed allocation remains a strict assertion.

The recorded workload creates 1,024 or 4,096 awake circles (radius 2) and one static
floor, gravity 980, default materials/iterations, 4 substeps, dt 1/60 s and 128
measured complete public SpaceStep calls. It includes preparation, device waits,
readback and host state publication. Final population/finite-state checks occur
outside timing. It excludes rendering, input and object creation. Native driver
allocation and full-family stability are not established by GC counters.

The first completed 768-tick run measured CPU/GPU medians of 2.5383/4.0051 ms at
1,024 and 7.2050/17.9219 ms at 4,096; both had zero managed bytes. Concurrent local
build activity overlapped this exploratory run; use the isolated-candidate record
below for the final checked revision. These measurements do not establish a GPU
advantage or a rendered FPS target.


### Isolated physics-candidate result

The candidate contains this physics change over `56d8760e`, excluding concurrent
renderer/WebGPU and example edits. Release runtime/tests, compiled API coverage
(11,440 declarations, zero unmapped), wiki test/generation/check and public GPU-space
checks passed from that snapshot. Native shader generation also passed in the
working tree. No benchmark overlapped another build/test launched by this task;
the desktop and other host activity were not disabled.

| Active circles | Backend | p50 / p95 / p99, ms | Managed bytes, owner / all threads | Upload / readback / uniforms, B per step | Mean device wait, ms |
| --- | --- | --- | --- | --- | --- |
| 1,024 | CPU | 2.5829 / 3.0465 / 3.2601 | 0 / 0 | 0 / 0 / 0 | 0 |
| 1,024 | GPU | 4.5917 / 6.9685 / 7.3442 | 0 / 0 | 184 / 82,104 / 17,820 | 3.1533 |
| 4,096 | CPU | 6.2899 / 7.3669 / 7.6947 | 0 / 0 | 0 / 0 / 0 | 0 |
| 4,096 | GPU | 5.0907 / 6.2756 / 6.6660 | 0 / 0 | 184 / 327,864 / 18,184 | 2.8254 |

GPU reduces median complete-step time by about 19% on this 4,096-body workload;
CPU remains faster at 1,024. These are one-run measurements, not a guarantee across
scenes, platforms or the 65,536-fragment target. Publication includes 80-byte
change records for all moving bodies. User callbacks, steady sensors/contact
receivers, persistent native allocation and actual FPS need separate performance
acceptance. Local logs: `/tmp/e2d-gpu-binding-candidate-focused.log`,
`/tmp/e2d-gpu-binding-cpu.log`, `/tmp/e2d-gpu-binding-fullgpu2.log`, and
`/tmp/e2d-gpu-binding-candidate-coverage.log`.


## Public scene motion conformance

`ELECTRON2D_TEST_GPU_MOTION_SCENES=1` invokes the same existing assertions with
explicit CPU and GPU World/SpaceCreate selection. It is also included in the full
GPU suite. CPU-only stage controls stay separate. These tests exercise the public
scene/server path, not only internal resident kernels; no fallback is enabled.
The common scenarios passed on Linux/.NET 10.0.1/Vulkan on 2026-10-09 without a
solver change or weaker numerical tolerances.

| Shared suite | Executed contract |
| --- | --- |
| CharacterBodyTests | Grounded/floating motion, floors/walls/ceilings, slope/ceiling options, slide limits, floor snap, platform layer masks/departure/carry, Area gravity, same-frame queries, fixed-lane poses, packing, failures and reentry. |
| AnimatableBodyTests | Resting/moving riders, deferred/immediate presentation, exact targets, zero time, rotation, listener failure, invalid scale/skew, threading, packing and reentry. |
| RigidFreezeModeTests | Frozen static/kinematic roles, retained mass/lock, target/idle velocity, moderate/fast moving contacts, force duration and one callback per outer tick, disable override, packing and failure guards. Backend-neutral role checks are diagnostic alongside physical behavior. |
| WorldTests | Duplicate identity, shared once-per-tick stepping, queries/Areas/joints, CPU-default to explicit GPU world and back, stale views/membership, listener/geometry failure recovery and retained caller-world lifetime. Default scene-owned worlds remain CPU; this does not claim all GPU-to-GPU viewport combinations. |
| PhysicsSurfaceVelocityTests | Stationary linear/angular surfaces, friction and normal response, wakeup, point/contact velocities, animated target plus surface channel, character carry, raw body mode/reattachment and lifetime. The injected CPU solver-lock flag check remains CPU-only; public owner/pose-phase guards run in the shared scene suites. |
| SeparationRayShapeTests / SeparationRayDynamicsTests | Directed and reverse queries against current finite shape families, containment, slope policies, recovery and character snap, Area sensing, material response, momentum/impulses, explicit inertia, live edits, sleep, masks/exceptions/one-way, CastRay/CastShape and scene events. |

Existing tolerances are preserved. Character position bands distinguish safe
obstacle response from tunnelling and verify classification/normal separately;
platform carry accepts 8–12 units for a prescribed 10-unit move. Kinematic target
checks use 0.02 scene units, with 0.01 rad presentation and 0.1–0.15 rad/s angular
velocity allowance for the CPU angle decoder. Direct ray contacts use 0.01-unit
point tolerance; cast intervals bracket analytic fractions 0.5 and 0.425 within
0.01, covering the existing eight-refinement resolution. Coupled ray momentum
allows 0.02 of 600 kg*u/s, equal/opposite impulses allow 0.02 kg*u/s and the
integrated 360 kg*u/s transfer allows 2 kg*u/s. These assert physical invariants
and public outcomes rather than backend traversal or bitwise identity.

Warm allocation checks now use precise process-wide managed allocation counters
for both backends: character slides/reusable snapshots, idle/moving platforms,
frozen motion, world lookups, reverse ray casts/rest/recovery, directed Areas,
active surface contacts and directed solver steps. The checked 64/128-frame
windows allocate zero bytes across managed threads. Caller-created copied
results, initial capacity growth, native drivers and cross-platform allocation
are outside these checks. This closes these concrete public-path verification
gaps; the remaining contract, networking and broad performance goal stays open.


Verification receipt: `/tmp/e2d-gpu-motion-final.log` contains the final shared
matrix, including stale direct-state rejection across successful and failed-listener
world transfers. `/tmp/e2d-gpu-motion-fullgpu.log` contains the full GPU suite,
including both native renderer lifetime hosts. Release/API/wiki checks passed in
an isolated snapshot of this physics change; the main checkout contained concurrent
unrelated example/rendering work, including a temporarily absent WaterSurface.GPU.cs.
No production physics change was needed for the checked behavior.

The diagnostic two-body workload (one awake supported ray, one floor, eight
configured contact slots per body, 4 substeps/16 iterations, dt 1/60 s, final 128
warmup/128 samples) measured complete SpaceStep p50/p95/p99 of
0.0175/0.0177/0.0206 ms on CPU and 4.4904/10.2662/11.1360 ms on GPU. Both allocated
zero all-thread managed bytes. GPU upload/readback/uniforms were
256/1,512/17,332 bytes per tick, with 4.1068 ms mean device wait. This includes
public state/contact publication; it excludes construction, rendering and native
allocation. These tiny-world diagnostics ran on the shared desktop and do not
replace the controlled massive-scene comparison or establish rendered FPS.

## Conditional body publication

The GPU adapter no longer downloads authored intermediate poses for every ordinary
step. Pre-step activity is needed to preserve pending-force eligibility, omission
and retention while sleeping/static, and dispatch of receivers active before the
solver. If any body has pending force/torque or satisfies the existing callback/
contact-snapshot predicate, the original full pre-step publication and activity
capture remain. The complete receiver order is retained, allowing an earlier
callback to enable a later receiver within the same step. With no such consumer,
activity capture is skipped; constant forces and field integration stay on device.
Queued connected wakes are still flushed before simulation. This optimization
removes a redundant observation, not a solver phase or physical feature.

Rotation-lock preparation reads the existing authored role/integration metadata
through the validated resident handle. It does not read a GPU pose snapshot just
to learn an already-known policy bit. Actual pose/query/joint consumers continue
to use fresh selected reads when necessary. Completion still publishes changed
states for scene poses, fields, contacts, getters and callbacks.

Each attachment distinguishes a cache compatible with the last change publication
from a selected intermediate read. A world retains its last publication's authored
state epoch. If an explicit getter observes a temporary pose which is then restored,
a later unchanged change-stream entry cannot falsely validate that temporary cache.
The attachment refreshes it on its next real read. A completed publication or a
selected read at the current publication epoch restores compatibility; it does not
force permanent per-body reads after one getter. No second 64-byte state copy,
new request list or device buffer is introduced. Reattachment resets the qualifier.

`PhysicsGPUPublicationTests` runs public CPU/GPU checks for unchanged/intermediate
reads, fresh solved poses, late callback registration, live direct views, custom
integration attach/detach, retained forces across sleep/static roles, force omission,
one-tick torque and connected joint wake. The counter `ChangePublicationCount`
records successful resident change publications, allowing an exact one-versus-two
consumer check without depending on incidental solver fences. Select with
`ELECTRON2D_TEST_GPU_PUBLICATION=1`; the managed and full GPU runners include it.
The first candidate exposed both a hidden rotation-policy read and stale cache
revalidation; those candidates were rejected before the final implementation.

### Large public-world measurements

`ELECTRON2D_TEST_GPU_PUBLICATION_BENCHMARK=1` selects
`PhysicsGPUPublicationPerformance`. Optional `ELECTRON2D_PUBLICATION_COUNTS` is a
comma-separated even body-count list, default `512,4096,16384,65536`.
Each world has half static and half dynamic circles, radius 4, arranged as separated
colliding pairs with one unit of initial penetration. Every iteration resets all
dynamic poses/linear velocities and runs the complete public SpaceStep at 1/60 s,
4 substeps and 16 solver iterations. Automatic sleep is disabled for dynamics;
no callbacks, sensors, contact-report receivers or debug capture are requested.
64 warmups and 64 measured iterations use the same workload on each backend.
Actual population and finite separating response are verified after timing. This
is a mass collision workload with independent pairs, not a dense destruction pile.

Baseline is `d37b6f09` with only the publication counter and benchmark harness.
Both CPU/GPU after columns use the same candidate. Linux x64, .NET 10.0.1 Release,
Vulkan/RTX 3090 Ti. Entries are whole reset-plus-step p50/p95/p99 milliseconds:

| Bodies | CPU after | GPU before | GPU after | GPU readback B/tick before / after |
| ---: | --- | --- | --- | ---: |
| 512 | 0.4299 / 0.5396 / 0.8614 | 2.7680 / 3.7033 / 4.4021 | 3.2413 / 6.9550 / 7.2580 | 41160 / 192 |
| 4096 | 4.2516 / 9.2730 / 12.4016 | 4.2071 / 5.3594 / 6.2812 | 4.1154 / 5.1992 / 6.2678 | 327880 / 192 |
| 16384 | 15.4845 / 17.6731 / 25.2686 | 10.6502 / 13.0604 / 14.3852 | 11.2745 / 14.3228 / 16.5346 | 1310920 / 192 |
| 65536 | 63.0945 / 66.6107 / 67.8118 | 51.0120 / 54.5431 / 54.9735 | 50.7523 / 139.6676 / 226.3529 | 5243080 / 192 |

All intervals measured **0 owner-thread and 0 all-thread managed bytes**. GPU
submissions fell from 26 to 23 and body publications from two to one per tick.
After upload is 45232 / 360624 / 1441968 / 5767344 bytes for these populations;
baseline uploads eight more bytes. The deterministic reset fixture returns to the
same final poses every tick, so no changed bodies need republishing after warmup.
The 192 bytes are necessary status/count traffic, not a universal body-state budget;
a world with changed final poses still downloads their change records. This does
not reduce the number of physical bodies, contacts or solver iterations.

Logs: `/tmp/e2d-publication-baseline-perf.log` and
`/tmp/e2d-publication-after-perf.log`. Timing tails were variable, and the first
matrix does not establish a consistent latency improvement from this optimization.
Desktop activity and dynamic device clocks were not controlled; no unproven cause
is assigned to that variation. The byte/publication reductions are exact counters.
A separate small debug fixture run also showed a 17 ms median; that observation
is retained in `/tmp/e2d-publication-small2.log`, not silently replaced by a faster run.

A follow-up 65,536-body run with `ELECTRON2D_PUBLICATION_PROFILE=1` and a local
runtime-reference reuse in the consumer scan measured CPU 67.9761/72.5464/93.7380 ms
and GPU 39.2548/43.7043/45.0439 ms. GPU reset/step p50 was 10.4212/29.0945 ms,
mean fence wait 8.9723 ms, with the same 192-byte readback and zero managed bytes.
The optional existing phase profiler now covers GPU steps too (means, milliseconds):

| GPU step phase | Mean ms |
| --- | ---: |
| Attachment preparation | 1.1014 |
| Consumer scan / pre-publication / authored fields | 1.9269 |
| Body/joint preparation and wake publication | 11.7488 |
| Resident simulation and optional contact diagnostics | 9.5978 |
| Post-step publication and contact reports | 1.1806 |
| Scene/server completion | 2.7981 |
| Contacts / Areas / direct-view capture | .4190 |
| Callbacks / events | .0008 |

The profiler uses eight existing internal timing/allocation slots with GPU-specific
phase meanings and is disabled by default. It adds no phase-specific GPU fences.
The 65,536-body result is a bounded GPU advantage on this workload, not a 60 Hz or
cross-platform acceptance result. Reducing authored-command/preparation cost remains
a measured priority. Profile log: `/tmp/e2d-publication-large-profile.log`.

### Native window boundary

The existing 512-body component-edit window was rerun, along with all four
CPU/GPU × gpu/compatibility contact-pixel checks. The contact checks passed with
zero warmed render allocations. In the first window rerun, CPU measured 141.85 FPS /
59.69 ticks/s and GPU 90.36 FPS / 61.15 ticks/s, with actual VSync Enabled. GPU readback
was 49,000 B over 245 ticks (200 B/tick), instead of the earlier 41,168 B/tick.
That interval allocated zero GPU-window owner-thread managed bytes; the CPU-window
interval recorded 200,264 B. The whole-window allocation source has not been localized;
these figures must not be described as zero-allocation window acceptance. Headless
physical steps remained at zero on both backends. Log:
`/tmp/e2d-publication-native-window.log`; contact pixels:
`/tmp/e2d-publication-native-contacts.log`. Timing variation requires paired follow-up
before claiming a window-FPS improvement. Large rendered worlds, native allocations,
other platforms, extensions/tile owners and authoritative networking remain open.

A subsequent paired rerun used the unchanged 512-body window first on `d37b6f09`,
then on this implementation, with other test/build processes stopped. Baseline
CPU/GPU measured 143.78/143.63 FPS; after CPU/GPU measured 143.86/135.35 FPS.
All four intervals recorded zero owner-thread managed bytes, so the earlier CPU
allocation did not reproduce; its source remains unassigned. Both GPU intervals
completed 240 physics ticks in approximately four seconds. GPU readback fell from
9,880,320 to 48,000 bytes and submissions from 6480 to 5760; total measured waits
were 592.698/592.222 ms. After frame p50/p95/p99 was 6.9684/12.8642/14.2234 ms,
versus baseline 6.9512/8.3114/9.9863 ms. **No window-FPS improvement is claimed**:
this optimization removes traffic while host/driver timing remains variable.
Logs `/tmp/e2d-publication-window-paired-{before,after}.log` and captures
`/tmp/e2d-publication-window-paired-{before,after}-{CPU,GPU}.png` retain both runs.

## Contact receiver preparation

GPU report selection walks the space's attached scene and server bodies and reads
their retained `PhysicsBodyRuntime`. It no longer resolves every RID through the
process-wide registry each tick. Scene and server Areas are excluded; static and
kinematic body receivers still participate. Limits are read afresh before each
solve, so zero, increased and decreased caps do not require a new dirty flag or
receiver cache. Existing owner/lifetime validation and checked capacity sums remain.
No public API, solver settings, contact-selection policy or transfer format changes.

`PhysicsGPUPublicationTests` checks mixed scene/server receivers, scene static-body
reports, exclusion of both sensor kinds, live cap changes, detach/reattach and
permanently invalid old direct-state views on CPU and GPU. The internal preparation
profiler splits body/motion/joint work, report selection, command/wake publication
and its included fence wait. Timing calls run only when profiling is enabled.

On Linux/.NET 10, the same 65,536-body fixture above (32,768 independent colliding
pairs, 64 warmup and 64 measured ticks) was run in baseline/changed/changed/baseline
order. Baseline is `c5d3d319` with the same added timing instrumentation; changed
runs replace only receiver enumeration. Every dynamic pose and linear velocity is
reset each tick; solver population, geometry, sleep policy, substeps and iterations
are identical. Whole time includes these public edits and the complete step.

| Run | Report selection mean ms | Total preparation mean ms | Whole GPU p50 / p95 / p99 ms | Whole CPU p50 ms |
| --- | ---: | ---: | --- | ---: |
| Baseline first | 4.0590 | 11.7998 | 37.4349 / 42.0777 / 43.6200 | 66.4490 |
| Changed first | 1.4597 | 10.5504 | 38.3089 / 43.8692 / 46.0663 | 66.1238 |
| Changed repeat | 1.3028 | 9.2064 | 37.6139 / 39.9252 / 42.6025 | 67.1499 |
| Baseline repeat | 4.6024 | 13.0355 | 42.0410 / 47.9104 / 49.0096 | 67.6625 |

Receiver selection consistently dropped from 4.06–4.60 to 1.30–1.46 ms. Whole-step
ranges overlap: the first changed run was slower overall, and mean GPU fence waits
varied from 7.75 to 9.94 ms. These runs establish the local reduction, not a stable
whole-frame speedup or 60 Hz at this population. Changed runs still spend
5.95–7.14 ms preparing parameters/motion/joints and 1.95 ms publishing commands/wakes
(including 0.80–0.91 ms of fence waits). Those are separate remaining costs.

All eight CPU/GPU intervals measured zero owner-thread and all-thread managed bytes.
Each GPU tick retained 5,767,344 uploaded / 192 downloaded bytes, 23 submissions and
one body publication: this change removes host registry work without reducing real
contacts or device work. It neither measures native allocations nor adds new rendered
FPS or network acceptance. Logs are `/tmp/e2d-prepare-baseline.log`,
`/tmp/e2d-prepare-after.log`, `/tmp/e2d-prepare-after-repeat.log` and
`/tmp/e2d-prepare-baseline-repeat.log`.

The smaller-population table formerly reported here as the changed revision is
withdrawn. An IL audit found that restoring source with preserved timestamps let
an incremental build retain the baseline registry-based `PrepareGPUReports`.
`/tmp/e2d-prepare-small.log` and the final broad-suite run from that experiment
therefore do not verify the changed receiver path. The earlier two changed 65,536
runs and focused checks preceded that restoration; their measurements above remain
separate evidence. The stale method is recorded in `/tmp/e2d-parameters-stale-il.log`.
Subsequent comparisons force recompilation with `--no-incremental` and inspect the
consumer DLL, rather than relying on a successful incremental build.

A forced rebuild of `7e918def` confirms the attached-list receiver method in
`/tmp/e2d-parameters-baseline-il.log`. Its 65,536-body comparison measured CPU
p50/p95/p99 70.1175/76.8703/88.4601 ms and GPU 33.5278/39.9213/41.4280 ms, with
5.6378 ms for parameters/motion/joints, 1.2643 ms for report selection and 1.9765 ms
for command/wake publication. Both warmed intervals allocated zero owner/all-thread
managed bytes; GPU transfers and submission counts remain those reported above.
Log: `/tmp/e2d-parameters-baseline-forced.log`.

After the discrete restitution correction, the same forced-build 65,536-body fixture
measured CPU p50/p95/p99 67.1080/70.3527/72.1527 ms and GPU
35.0679/39.0912/41.0822 ms. Both intervals again allocated zero owner/all-thread managed
bytes; GPU upload/readback remained 5,767,344/192 bytes, 23 submissions and one
publication per tick. Mean preparation was 5.6995 ms for parameters/motion/joints,
1.3796 ms for report selection and 1.8649 ms for commands/wakes, with 7.9851 ms total
fence waits per tick. This preserves the full CPU/GPU comparison; the contact fix
is a correctness change and these samples do not establish an additional speedup.
Log: `/tmp/e2d-parameters-after.log`. The proposed owner-lookup micro-optimization
was deferred and is not part of this result. The forced baseline full GPU suite
passed in `/tmp/e2d-parameters-baseline-gpu.log`, replacing the stale broad-suite
verification described above.

The forced-build real-window check (`ELECTRON2D_TEST_PHYSICS_VELOCITY_EDITS_NATIVE=1`)
also completed: 640×520 GPU renderer, VSync enabled, MaxFPS=0, 512 circle bodies,
256 pose/linear/angular edits per physics tick, requested 60 Hz, 96 warmup ticks
and about four measured seconds per backend.

| Physics | Rendered FPS | Actual physics ticks/s | Frame p50 / p95 / p99 ms | Owner-thread managed bytes |
| --- | ---: | ---: | --- | ---: |
| CPU | 143.78 | 60.16 | 6.9545 / 6.9979 / 7.0433 | 0 |
| GPU | 143.60 | 59.69 | 6.9547 / 8.1649 / 9.2332 | 0 |

The GPU interval had 239 ticks, 10,812,360 uploaded / 47,800 downloaded bytes,
5,736 submissions and 601.235 ms of accumulated waits. This is capped rendered output
for a small world, not 60 Hz at 65,536 bodies; all-thread/native window allocations
and a before/after rendered-speed claim are outside this check. Log:
`/tmp/e2d-parameters-native.log`; captures:
`/tmp/e2d-parameters-window-{CPU,GPU}.png`.

## Parameter owner resolution

`PrepareGPUParameters` reuses its already validated scene owner for omission and
persistent force/torque, and the current adapter for a non-rigid rotation lock.
It continues sampling authored parameters every step and retains GPU handle/access
checks, live edits and callback ordering. It adds no state cache or transfer change.
The source change is confined to this attached-only preparation path; detached
accessors and public validation keep their existing behavior.

Comparison uses `4c570ee6` and the two-line owner-reuse change, with both versions
forcibly rebuilt (`--no-incremental`). Consumer IL confirms the original owner
accessors versus direct reads after the initial owner resolution. The reverse
baseline run loads the saved forced-build baseline DLL with the same test harness.
No source timestamp restoration is used as a substitute for recompilation.

The 65,536-body fixture retains 32,768 independent colliding pairs, 64 warmup and
64 samples, no dynamic sleep, four substeps and sixteen iterations. Every dynamic
pose and linear velocity is reset each tick; whole time includes those public
edits and the complete physical step. Runs execute baseline/changed/changed/baseline.

| Run | Parameters/motion/joints mean ms | Whole GPU p50 / p95 / p99 ms | Whole CPU p50 / p95 / p99 ms |
| --- | ---: | --- | --- |
| Baseline first | 6.0157 | 38.5033 / 41.9345 / 60.0424 | 67.3968 / 70.8164 / 72.5025 |
| Changed first | 5.3763 | 35.7464 / 39.4099 / 40.7475 | 67.2458 / 71.8978 / 86.1696 |
| Changed repeat | 5.4309 | 34.6665 / 38.5808 / 39.8158 | 65.9394 / 68.7447 / 70.8995 |
| Baseline repeat | 6.2465 | 34.9506 / 42.3843 / 44.8025 | 71.1860 / 96.0241 / 187.1669 |

The preparation phase consistently decreases by about 0.6–0.9 ms in these runs.
Whole-time ranges overlap; the reverse baseline is faster than the first changed
run, and CPU tails also vary. This is evidence of reduced local preparation work,
not a stable whole-frame speedup or a new rendered-FPS claim.
All eight warm CPU/GPU intervals allocate zero owner/all-thread managed bytes.
Each GPU tick still uploads/downloads 5,767,344/192 bytes, makes 23 submissions and
one body publication; mean waits range 7.8772–9.6748 ms. Logs:
`/tmp/e2d-owner-{baseline,after,after-repeat,baseline-repeat}.log`.

The existing explicit CPU/GPU parameter, publication and public-world suites pass,
including live constant forces/torques, omission, locks, reentry, contact caps,
callbacks, failed-world teardown and CPU/no-device startup fallback in a separate
process. The public 4,096-active-circle-plus-floor scenario also reports zero
owner/all-thread managed bytes (GPU whole-step p50/p95/p99
4.5140/5.4787/6.0849 ms). Logs `/tmp/e2d-owner-parameters.log`,
`/tmp/e2d-owner-publication.log` and `/tmp/e2d-owner-space.log` retain those checks.
The prior real-window results remain separate: native allocations, new window
measurements, network restore/replay and general conformance are not established
by this optimization.

## Backend-local replay storage

The internal [CPU checkpoint](cpu-checkpoints.md) and [GPU checkpoint](gpu-checkpoints.md)
retain each solver's persistent replay state. They do not yet capture the common
attachment adapters, resource/scene lifetime or event-confirmation state. Shared
public world capture/apply and authoritative network correction remain open.
