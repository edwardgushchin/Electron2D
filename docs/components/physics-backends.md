# Physics backend selection and shared worlds

Last updated: 2026-10-12

[Same-revision mass measurements](physics-current-performance.md) record complete
CPU/GPU headless steps and real-window frames at 65,536 awake bodies on
`7bff847e`. GPU step p50 is 38.7552/48.9548 ms headless/window versus CPU
276.6446/263.8018 ms; actual window FPS is 15.6201 versus 3.4699. All warmed
physics/frame samples allocate zero managed bytes. This workload passes physical
gates and confirms a host-specific advantage; 60 Hz, native/foreign acceptance
and complete registered extensions remain open. Earlier measurements below retain
their own source/workload scope.

## CPU collider ownership

CPUPhysicsWorldBackend now retains its concrete collider attachments by the shared
collider identity. Body/shape handles stay in that owner and the concrete CPU
attachment; CollisionObject, PhysicsBody, raw server colliders and the common
collider facade expose no CPU body/shape IDs or default-empty native aliases.
CPU fields, queries, Area monitoring, CCD, kinematic intervals, contacts, joints
and portable one-way mapping resolve native data through their actual owner.
Retirement removes only the matching attachment, so a delayed old detach cannot
remove a newer entry. World disposal clears retained attachment references.

Contact-policy preparation dispatches through the selected collider implementation.
Portable sleep capture/finalization dispatches through the world owner; CPU restores
native sleep after shared authoring, while GPU retains its existing resident motion
write that already commits sleep time/flags. No extra GPU transfer or CPU mirror
is introduced by that finalization route.

[Wrapped-collider checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.WrappedCollider.cs)
use a non-CPU implementation type delegating actual CPU state. The selected-view
fixture exercises real impulse/policy hooks, scene/raw contacts, Area monitoring,
ray/shape CCD, queries, retirement/reentry and portable capture/apply. Shared
physics APIs and warm allocation guards are retained. Public registration and the
complete external server operation bridge remain open under ADR 0103.

## GPU collider ownership

The resident GPU store retains concrete collider attachments by shared identity.
The common collider facade has no resident handles, shape lists, snapshot caches
or concrete GPU casts. The selected world creates an attachment with its actual
store; publication, queries, Areas, joints, portable capture/apply and checkpoint
cache restoration resolve that attachment from the store. Matching retirement
removes the entry even after failed creation or cleanup; a stale detach preserves
a replacement and store disposal clears retained references.

Object/canvas identity refresh and collision-priority edits now dispatch through
the selected collider implementation. GPU publishes them to its resident records;
CPU reads shared identity/priority in queries and contact policy. The ownership
lookup is a retained host identity table, not a copy of device physical state.
It introduces no GPU transfer, device wait or additional snapshot publication.

The [wrapped-collider fixture](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.WrappedCollider.cs)
now delegates real CPU and GPU attachments. It checks scene/raw policy/impulse
hooks, canvas/object query identity, contacts, Areas, ray/shape CCD, attachment
retirement, a real two-body pin, local checkpoint and portable restoration.
CPU/GPU/no-device ownership profiles pass. Both selected implementations retain
0/0 owner/all-thread managed bytes in 64 warmed scene callback/contact steps and
512-body full intervals. Exact checkpoint/portable pose restoration uses 0.001
scene unit to allow float/unit-conversion rounding; the live pin stays within
0.5 scene unit of its anchor after eight impulse-driven steps, allowing finite
solver/contact correction while remaining far below its three-unit body radius.
Public named registration and the complete external operation bridge remain open.

## Selected direct-state construction

The retained world owner now creates both cached direct-space and direct-body
views. Body factories run after interval initialization and before solver/worker
borrowing, so their guarded constructors can prepare real geometry and state.
The same selected view reaches scene integration, server force/sync callbacks,
explicit acquisition and completed contact snapshots. Space factories execute
outside the service registry lock; World and body-space access retain that cache.
Lazy World implementation creation also leaves its runtime lock before invoking
the factory and rejects concurrent or recursive creation and final-resource release.

Publication validates live context, body identity, immutable attachment generation
and disposal before replacing a cache. Null, foreign, stale, disposed and recursive
factory results reject. A rejected borrowed foreign view is not disposed. Factory
execution borrows world/body lifetime; recursive stepping, own attachment release
and scene hierarchy changes reject. A pre-solver factory exception publishes no
solved tick and finalizes the interval. A late callback-view exception preserves
the completed tick and is aggregated after later body callbacks are attempted.

CPU collider attachments retain their concrete creator rather than recovering it
by casting the selected world owner. This permits an owning implementation to
provide views while its concrete solver still owns native sleep/contact policy.

[Selected-view tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedViews.cs)
use a test-only owner delegating complete real CPU/GPU simulation, with an extension
that doubles a real impulse and returns real solved contacts. A SceneTree and raw
bodies exercise queries, callbacks, lifetime/failures and 64 warmed steps at zero
owner/all-thread managed allocation, also in CPU without a graphics device.
This implements the internal ownership route; public named registration, the
complete server implementation bridge, custom algorithms and platform/native
acceptance remain open under ADR 0103.

## Shared joint inputs and native ownership

Selected joint Attach and ApplySolverPolicy receive an immutable
[PhysicsJointConfiguration](../classes/PhysicsJointConfiguration.md) value. It
contains complete engine-unit frames, collision policy, pin/groove settings,
bias/force/correction caps and effective spring rest/stiffness/damping. Scene zero
rest fallback is resolved by the common runtime before compilation. Concrete
implementations receive no mutable PhysicsJointRuntime record.

CPUPhysicsWorldBackend retains native joint attachments by shared joint identity.
Its spring prepare/validate/apply passes resolve that actual owner even when the
selected implementation is a delegating wrapper. The common joint facade exposes
no native joint ID or CPU spring cast. Retirement removes only the matching
attachment; failed creation/release and world disposal clear ownership. Resident
GPU spring execution stays on device, with no new mirror, transfer or wait.
Detached frame sampling still uses the existing CPU numeric convention.

[Selected-view checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedViews.cs)
wrap real CPU/GPU pin, groove and spring implementations, verify refreshed policy
values, real constrained motion and native clear/replacement retirement. Shared
public joint suites retain their numerical, lifecycle and zero-allocation checks.
This internal input family does not close public registration or custom solver
operation coverage under ADR 0103.

## Shared collider shape inputs

The selected collider now receives one borrowed ReadOnlySpan of PhysicsColliderShape
values rather than separate scene/server slot types. Each value retains the exact
resource, local transform, logical index, enabled policy and body-local one-way
metadata. Disabled slots remain in the sequence; sensor inputs omit body-only
one-way response. CPU/GPU compilation consumes the same complete input family.

The common identity facade projects authored scene/raw slots into reusable scratch,
preserving each source's preflight order before native fixtures are removed.
Recursive preparation of that collider rejects; all exits clear scratch references
and release the guard. The array contains temporary authoring inputs, not physical
state, and adds no device transfer or host simulation mirror. Native owners retain
their own geometry validation and concrete shape storage.

[Input checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.ShapeInputs.cs)
exercise real selected scene/raw bodies and Areas, three logical slots with a
middle disabled slot, exact public query indices, rotated one-way metadata and
recursive geometry-hook failure/retry on CPU/GPU/no-device. The rotation check uses
0.00001 vector-distance tolerance for float sine/cosine rounding; logical IDs,
enabled state, resources and margins compare exactly. Complete public registration,
external operation dispatch and custom algorithm geometry remain open.

## Selected lifecycle and activity

The retained owner now receives Initialize after its immutable space RID and
selected implementation are published internally, before activation/attachment.
Local active state belongs to that owner. ApplyActive runs before commit; unchanged
writes skip implementation work and a throwing hook preserves the previous policy.
Initialization, activation, selected query flush and end-sync borrow the world
context, rejecting recursive steps, own release and scene hierarchy changes.
Cleanup borrowing uses lifetime/thread guards even for a failed world.

SynchronizeResults brackets actual selected result publication; solved body
callbacks run before query/event flushing. FlushQueryEvents invokes the selected
hook, then always drains common contact/sleep/Area queues. Hook and observer errors
are retained together. Shared callbacks keep their existing mutation rules outside
the implementation-hook borrow. Nested detach/object-membership flushes restore
the previous flushing flag. EndSynchronization attempts the selected EndSync after
all deliveries, passes the first prior interval/publication failure and retires
its phase state even when finalization throws. Existing Dispose owns native finish.

Empty active positive intervals now execute the same selected step/lifecycle,
including tick/statistics publication. Inactive/global-suspended and zero-time
calls retain their skip policy. Borrowed recursive calls reject before that skip
so a zero-time call cannot evade the hook lifetime guard.

The [lifecycle fixture](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.Lifecycle.cs)
drives a real CPU/GPU delegated owner, raw bodies and a monitoring Area. It checks
activation rollback, nested removal, body/event ordering, three distinct flush/
observer/end-sync errors, solved-tick retention, failed initialization cleanup and
empty-world dispatch. CPU/GPU/no-device lifecycle fixtures retain 0/0 owner/all-thread
managed bytes over 64 warmed complete intervals after blocking GC fixture
preparation; this setup is confined to tests. Activity and public GPU checkpoint
profiles also pass. Named public registration and the complete external server
family remain open under ADR 0103.

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

[Consumer-driven GPU publication](gpu-demand-publication.md) now avoids mandatory raw-body state downloads. Scene/view/contact/kinematic consumers and explicit getters retain current results; callbacks preserve shared ordered publication. The current 4096-body fixture downloads 752 bytes per step rather than a full moving-body stream, while GPU speed advantage remains open.

[Caller-selected pose batches](gpu-demand-publication.md#explicit-pose-batches) preserve the scalar public contract on both implementations. Render consumers can read dynamic raw GPU poses with one compact gather and no full state mirror; scene presentation and static authored poses retain their existing path.

The dense 65536-body real-window fixture now measures full GPU physics p50
49.1659 ms versus CPU 250.9162 ms and actual FPS 15.0486 versus 3.6823, with
0/0 warmed physics/frame managed bytes. The 4096 case still favors CPU. These
[pose-batch measurements](gpu-demand-publication.md#real-window-pose-batch-measurements)
include publication, actual captures and physical invariant checks; broader
platform/native allocation and complete extension acceptance remain open.

The separate 65536-body independent-contact fixture now demonstrates full-step GPU p50 18.2922 ms versus CPU 55.1572 ms in an isolated repeat, with 0/0 warmed managed bytes and identical counts/substeps/iterations. [Complete measurements](gpu-demand-publication.md#massive-independent-contacts) include unfavorable smaller-world and tail results; this does not establish general performance or window FPS.

## Runtime flow and ownership

The selected PhysicsWorldBackend now creates a fresh PhysicsJointImplementation for each constraint attachment. PhysicsJointBackend retains only that current implementation; RID, authored values, local frames, membership and pair-exception accounting stay in PhysicsJointRuntime. CPUPhysicsJointImplementation owns native endpoint/joint IDs, exact compiled local frames and transient spring evaluation. GPUPhysicsJointImplementation owns its resident handle and retained definition, using the device solver for spring/constraint execution. Generic live setters, solver policy, portable frames and release all dispatch through the selected attachment. Current-body local-frame sampling also dispatches through the selected collider implementation; detached sampling preserves the existing CPU numeric convention. CPUPhysicsWorldBackend owns its lazy shape-free world anchor; GPU pins retain their virtual world endpoint with no CPU body.

The retained PhysicsColliderBackend now keeps common RID, weak object/canvas association, priority and attachment generation. PhysicsWorldBackend.CreateCollider creates a fresh PhysicsColliderImplementation for the actual selected solver. CPUPhysicsColliderImplementation owns native body/world references, fixture IDs, exact saved pose and mass/contact-policy scratch; GPUPhysicsColliderImplementation owns resident handles, shape/query leases, authored policy and qualified publication/replay caches. Pose, motion, sleep, locks, forces, mass, shape rebuild/filtering, surface velocity, contacts and portable operations dispatch to that same attachment. A world transfer retires the old implementation while retaining public identity and authored/observable state. Native CPU and resident GPU facets remain internal; public registration and complete extension contexts are still open under ADR 0103.

The internal [PhysicsWorldBackend](../classes/PhysicsWorldBackend.md) factory now
creates a fresh retained implementation for each space. [CPUPhysicsWorldBackend](../classes/CPUPhysicsWorldBackend.md)
owns the native world, workers and optional diagnostic GPU stages;
[GPUPhysicsWorldBackend](../classes/GPUPhysicsWorldBackend.md) owns the independent
resident store. PhysicsSpace gates an interval and dispatches its complete CPU/GPU
path through that selected object. CPU discrete and CCD native intervals also reach
the CPU implementation. Common callbacks, event order, authoring and attachment
identity remain in the space. All six direct-space query algorithms and prepared result scratch also dispatch through that owner; the public view retains validation and array/span projection. Complete body-motion dispatch, candidate/recovery/sweep algorithms and native shape access now belong to the CPU implementation. Common motion inputs contain only engine values and RIDs; GPU uses resident motion queries. Platform velocity uses indexed current RID/attachment membership and selected world-point collider motion, preserving native CPU conversion and GPU center-aware surface behavior. The GPU implementation uses the existing resident query driver. World release attempts all participants before
releasing the selected implementation and aggregating failures. Once release starts,
FreeRID unregisters the space even when cleanup throws. During any live physics
callback, release and recursive stepping reject without invalidating that context.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs)
checks fresh ownership, independent simulation, 512 bodies over 64 warmed full
steps at zero owner/all-thread managed allocation, and no-device selection/fallback. A worker cleanup fault must not skip native world destruction.
This is a built-in ownership boundary; public manager registration, the complete
server/direct-state extension family and custom geometry remain open under ADR 0103.

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
other platforms, extensions and complete authoritative networking acceptance remain open. Square-atlas tile owners now execute through TileMapLayerTests on both backends.

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

## Selected implementation ownership check (2026-10-10)

Fresh per-world CPU/GPU owners execute the existing full interval paths. The
512-body check measures 64 warmed full steps after 64 warmup steps; both paths
allocate 0/0 owner/all-thread managed bytes. Real server Area callbacks reject
recursive stepping and world release while keeping the registered implementation
live. An injected CPU worker-cleanup error still destroys the native world and
unregisters its RID. Strict unavailable GPU and explicit startup fallback pass
in a separate child without DISPLAY/WAYLAND_DISPLAY and with dummy video plus
an unavailable GPU driver.

The public-world fixture also ran both complete paths on this worktree, using
active circles plus a floor, four substeps, 768 warmup steps and 128 samples:

| Bodies | Backend | Full step p50/p95/p99, ms | Managed owner/all bytes | GPU upload/readback bytes per sample | Wait, ms |
| ---: | --- | --- | --- | --- | ---: |
| 1024 | CPU | 2.6073 / 2.8821 / 3.0792 | 0 / 0 | 0 / 0 | 0 |
| 1024 | GPU | 7.5826 / 8.4562 / 9.0034 | 0 / 0 | 240 / 82680 | 3.7628 |
| 4096 | CPU | 6.1230 / 7.0935 / 7.6039 | 0 / 0 | 0 / 0 | 0 |
| 4096 | GPU | 8.1307 / 9.1993 / 9.8419 | 0 / 0 | 240 / 328440 | 3.9382 |

Uniform uploads were 61916/64573 bytes per GPU sample at 1024/4096 bodies.
These measurements leave GPU advantage open; the ownership refactor does not
change readback policy or establish the target speed requirement. Logs and source
hashes are retained in `bin/physics-backend-ownership/2026-10-10/` and its evidence
manifest. Native allocation, foreign targets and real-window FPS are unverified.

CPU/GPU public checkpoints and portable snapshots passed. The existing network
checker also passed separate CPU-server/GPU-client/late-CPU-client processes through
tick 360 with delayed, dropped, duplicated and reordered packets, lifecycle and
correction; warmed step/replay all-thread counters were zero in their measured
intervals. This verifies those exercised scenarios on the current implementation,
not every network, platform or numerical acceptance condition. Public registered
backends, custom geometry and the complete extension dispatch families remain open.

## Owned world operations

World initialization, authored sleep/contact/iteration/default-joint application, native worker selection, solver/sensor scratch preparation and completed process statistics now execute through the selected implementation. Common PhysicsSpace retains validation, authored values and observer publication. CPU owns native world defaults, arena/dormant budgets and sensor-statistics traversal; GPU applies store policies, invalidates observation epochs and prepares resident-consumer scratch without CPU motion storage. Named registration and typed direct-state extensions now execute; complete numerical-extension acceptance remains open. [World policy borrowing](physics-registered-adapter.md#borrowed-world-policies) guards all nine typed parameters and local activation before authoring or nested selected dispatch, while ordinary post-solver edits remain available.

The dormant-capacity fast path also removes repeated whole-population scans when
no dynamic body has ever allowed sleeping. The existing public dense-pile fixture
now reports setup separately. At 16384 CPU circles, creation was 1053.5285 ms
before this fast path and 802.8116 ms after it; an isolated repeat measured
822.4178 ms. All runs retain four substeps, 16 iterations, 32 warmup and 32 measured
steps. Full-step p50/p95/p99 was 53.5809/56.8210/59.4889 ms before and
51.8929/54.1560/54.2829 ms in the repeat, with 0/0 warmed managed bytes and identical
.25791 maximum penetration. The first after-run overlapped formatting checks and
is retained separately; these individual runs are evidence of setup cost and
preserved behavior, not a broad throughput guarantee. Logs/reports are in
`bin/physics-world-operations/2026-10-10/capacity-before/`, `capacity-after/` and
`capacity-repeat/`. Sleeping worlds preserve their existing dormant budgets.

## Selected step phases

`PhysicsWorldBackend.Step` now enters one common `PhysicsSpace.ExecuteStep` boundary.
The selected owner executes BeginStep, world/field preparation, body/motion
preparation, Solve, SyncResults, body completion, contact collection, Area scanning
and EndStep. CPU owns its native interval/worker/one-way finalization flow; GPU owns
resident submissions, required state/report publication and the started-interval
failure marker. Concrete solver helpers remain internal implementation details.

Shared code retains topology preparation, scene contact/sleep queues, captured body
views, statistics publication and ordered user callback/event dispatch. The owner
advances the world tick only after actual solve; CPU results become dispatchable
then, while GPU waits for required publication to finish. EndStep is attempted on
failed preparation as well as solved intervals, and nested finally releases the
space step guard before user code. Invalid authored geometry publishes no solved
tick; a user callback exception preserves a completed usable world and later
callbacks are still attempted. A started GPU execution failure remains terminal.

Typed internal membership lists avoid boxed interface enumerators in warmed
owner phases. They are not public mutable collections; existing owner/topology
rules continue to protect them.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs)
checks solved tick/view publication, callback order, recursive-step and borrowed
release rejection, continued simulation after a user callback error, rejected
geometry and scene recovery on both implementations. Common physics, GPU demand,
checkpoint, portable and network checks retain their separate acceptance scope.

The mass fixture now emits schema 2 with named phase buckets: authoring,
world-policy-fields, body-motion-callbacks, solver, result-sync, pose-contacts,
areas-views-statistics and callbacks-events. Historical schema 1 phase values
remain historical; complete-step and full-frame measurements remain comparable
only with their recorded source/workload. Public registration and complete
server/direct-state/custom-geometry extensions remain open under ADR 0103.

The schema 2 step-dispatch worktree repeated the real 1152x800 window at 65536
awake dynamic circles plus three walls, four substeps, 16 iterations, 240 warmup
and 64 samples. CPU/GPU full physics p50/p95/p99 is
254.4662/268.1269/282.0360 and 50.3674/52.9640/54.4598 ms. Actual FPS is
3.6547/14.9030; complete-frame p50/p95/p99 is 272.6977/286.3639/300.7408 and
66.6083/69.7458/73.3597 ms. Pose/instance publication p50 is 12.9433/10.5441 ms.
Physics and rendered samples allocate 0/0 owner/all-thread managed bytes.
GPU-store frame upload/readback remains 1048816/1049336 bytes per measured tick,
18 submissions; final maximum penetration is .2703/.2933 against .601, energy is
within the configured bound and no particle escaped. The GPU PNG was visually
inspected. Reports/logs/captures are in
`bin/physics-step-dispatch/2026-10-10/window-65536/`. This is the same host/workload
profile as the pose-batch record, with actual VSync still Enabled; other devices,
native allocation and complete registered extensions remain open.

[Caller-created direct-space extensions](../components/physics-space-extensions.md) now execute all six typed query hooks through inherited public array/span/scalar operations. The library owns bound-space guards, nested exclusions, scratch lifetime and validated result publication. A separate public-only consumer computes real scene/raw/Area circle queries on CPU/GPU and checks lifecycle, failures and warmed allocation. Registered-backend factories, direct-body extensions and custom geometry remain open under ADR 0103.

[Caller-created body-state extensions](../components/physics-body-extensions.md) now execute the complete typed state/force/space/contact family. Body context is qualified by current attachment generation; nested hooks borrow body/world lifetime and preserve ordinary live mutation. Contact projection uses immutable engine-validated PhysicsBodyContact values. Registered server factories and factory-returned scene/server callback integration remain open under ADR 0103.

## Area implementation ownership

CPU/GPU owners now execute scene and raw Area geometry scans and audio point
containment. CPU owns stable field-priority scratch, exact native overlaps
(including world boundaries and directed separation rays), per-body gravity/damping
resolution and pre-step body/motion preparation. GPU owns resident overlap queries
and retained sensor/audio result buffers; the device field reducer remains unchanged.
PhysicsSpace keeps memberships, authored defaults, generation-qualified monitor
histories and ordered callback/event queues. Scanning commits observations through
common helpers; it does not invoke user code while the solver phase is borrowed.

The selected AreaContainsPoint operation serves spatial audio without a CPU/GPU
branch in shared code. CPU scans native fixtures by index; GPU uses current resident
sensor geometry, canvas/mask filtering and complete sensor capacity. Disabled slots,
transfers, reentry and free are read from current attachments, independently of
Monitoring/Monitorable for field and audio participation. Owner disposal releases
field/query scratch references. This is an executed internal ownership boundary,
not public registration or custom geometry.

[PhysicsAreaBackendTests](../../tests/Electron2D.Tests/PhysicsAreaBackendTests.cs)
uses the same CPU/GPU scene/raw fixture: mixed priority fields, independently
resolved damping, duplicate logical sensor slots, scene and raw notifications,
audio hit/miss masks, disabled slots and Area transfer/reentry/free. Numeric fields
and velocity use .002 scene-unit tolerance for finite float integration; identities,
counts, damping totals and allocation are exact. After 64 warm cycles, 64 full
steps plus two audio queries and a field read allocate 0 owner/all-thread bytes.
CPU also runs in the existing no-device ownership child. Native allocation,
foreign execution and new full-step/FPS performance measurements remain unverified.

## Selected observation and collision policy

[PhysicsBodyFields](../classes/PhysicsBodyFields.md) carries resolved engine-unit
gravity, signed linear/angular damping and initialization state. ReadResolvedFields
receives the current common values: CPU preserves its completed field reduction;
GPU samples its qualified resident observation cache. Scene/raw direct-state
access, callbacks and portable capture reach the selected collider. Common runtime
publication rejects nonfinite values before changing any field, rejects recursion
and borrows body/world lifetime. Internal completed-step publication requires an
owner-thread solved interval and retains the public query prohibition until common
observer dispatch begins.

Collision exception edits reach the selected collider and immutable world owner.
CPU validates native ownership and dirties authored fixtures for existing pre-solve
filter preparation. GPU owns its dirty flag, generation-qualified resident pair sets
and reusable RID input list in GPUPhysicsWorldBackend.Exceptions. The service copies
explicit RID pairs under its registry lock; implementation calls run afterward.
Joint vetoes retain their independent resident joint policy and contribution counts.
Query preparation and GPU interval/checkpoint preparation synchronize the same owner.

Portable preparation and ReadPortableBodies now dispatch through the selected
world. PhysicsSnapshotMap validates membership, supplies a borrowed common collider
span and clears its retained reference scratch in finally. CPU validates native
attachments already readable by current getters. GPU owns the reused handle/state
batch, gathers exactly the requested bound object set and publishes those results
to its concrete caches. This preserves the existing authoritative snapshot readback
and empty-apply flush; no additional state mirror, transfer or wait is introduced.
The wire/schema and ordinary unobserved GPU-step demand are unchanged. GPU restore
clears its own report ranges instead of common replay manipulating them.

[Selected-view tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedViews.cs)
use real wrapped CPU/GPU colliders to reject invalid fields/reentry/release, preserve
atomic state, suppress actual floor motion/contact response through exceptions,
restore it on removal and exercise scene/raw hooks. Portable preparation/read
failures release context and permit a fresh capture. Existing field, exception,
joint, checkpoint, mixed-backend snapshot and network suites retain their own
numerical and allocation boundaries. Public registration now executes under ADR 0103; arbitrary geometry and unexercised operation acceptance remain open.

## Selected checkpoint ownership

Common PhysicsSpace.Checkpoint now dispatches PrepareCheckpoint before creating
or recapturing the selected solver history. CPU retains its existing idle/native
preparation. GPU compiles pending body/Area/joint/exception policy in its owner,
then captures device history together with already published contact reports,
report ranges, publication epochs, wake state and exact collider observation caches
in [PhysicsSpace.GPUCheckpoint](../classes/PhysicsSpaceGPUCheckpoint.md).

Common scene/server replay records contain no GPU state, handles or report types.
[PhysicsAreaFieldConfiguration](../classes/PhysicsAreaFieldConfiguration.md) retains
complete authored fields for configuration checks and portable schema hashing.
Field order, values and the wire format are unchanged. GPU observation copies do
not request a new body-state readback or introduce a full physical CPU mirror.
Existing device checkpoint command flushing/copy waits retain their own cost.

Selected preparation, creation/capture, validation, restore, capacity reads and
release borrow world lifetime. Recursive checkpoint operations, stepping, world
release and membership transfer reject. A prepare/capture failure invalidates the
point while leaving the world usable; preflight rejection leaves motion unchanged.
A begun restore error retains the terminal checkpoint-world failure policy.
Release retires common storage in finally even when implementation cleanup throws.

[Selected checkpoint checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedCheckpoint.cs)
delegate actual CPU/GPU history, inject failures, verify borrow guards and real
motion rewind, then measure 64 warmed capture/restore cycles at zero owner/all-thread
allocation. Common scene/server contact/Area/force/target/joint/CCD replay and portable
snapshot suites retain their numerical and platform limits. This is an internal
executing boundary; public registered factory and complete extension-family
acceptance under ADR 0103 remain open.

## Native simulation and replay ownership

CPUPhysicsWorldBackend now owns native discrete/kinematic/CCD interval scheduling,
sweep-tree and force/joint budgets, one-way episodes, solved point-motion snapshots,
full outer-tick contact impulses and report-only geometry/statistics. Attached CPU
colliders read that owner's history; native pre-solve callbacks carry the same
implementation context. Material encoding/mixing and diagnostic CPU-stage callback
identity follow the owner. Shared space code retains scene membership, requested
selection, authoring, logical contact caps, user callbacks and event ordering.

Local replay dispatches through PhysicsBackendCheckpoint: CPU owns the native
checkpoint plus contact/motion/one-way/report-only histories; GPU wraps its device
checkpoint. Common replay still owns scene/raw authoring, poses, logical observer
histories; implementation-local GPU reports stay with the selected GPU checkpoint. Capture/validate/restore/dispose
run through the selected implementation, without a new public API or CPU mirror
for GPU history. CPU report-only pair/activity counters now rewind immediately;
previously they retained values from a later solved interval until another step.

Portable one-way capture and reconstruction also dispatch to the owner. CPU owns
native-to-network shape lookup and episode rebuilding; GPU owns reusable device
records and validates the shared membership index/generation. Common code keeps
network IDs, pair validation, tick metadata and silent observer correction.
Debug point enumeration uses the selected owner and scene-unit spans; the common
buffer remains capped and is captured with public checkpoints.

Continuous failure belongs to the CPU owner and remains terminal. PhysicsBackendOwnershipTests injects a native integration-stage exception after a real warm interval and verifies terminal access plus native world release; the maximum-duration probe alone did not produce a failure. Release drains
workers before destroying sweep scratch, clears history references and still
attempts diagnostic/device/native cleanup after an earlier cleanup error. GPU
started-interval failure retains its existing terminal policy without CPU replay.

[PhysicsReportOnlyTests](../../tests/Electron2D.Tests/PhysicsReportOnlyTests.cs)
now checks immediate selected statistics, contacts/identity and zero impulses
after a real separated interval and rewind on both backends. After warmup, 64
capture/restore/contact-read cycles allocate zero owner/all-thread bytes. Existing
CCD, force/joint budgets, frame impulses, directional/one-way geometry, parallel
contacts, local checkpoints, portable snapshots and network checks cover the
connected operations. Counts, IDs and allocation are exact; physical tolerances
remain those of each named fixture. Native allocation, foreign targets, broader
performance and complete public registration/custom geometry remain open.

## Selected body integration preparation

PhysicsBodyRuntime.ApplyBeforeStep retains common participation/contact-reporting
gates and captures pre-step activity, then dispatches default-force preparation to
the current PhysicsColliderImplementation. CPU owns omitted-force clearing, native
gravity policy and pending/constant force/torque application. GPU owns eligible
pending-force publication; common code owns successful command consumption; persistent constants, omission, gravity
and damping remain in its resident integration policy and kernels. This preserves
the existing timing, lock and sleep rules without a CPU cast in common integration.
Before an explicit GPU sleep command, pending authored integration/constant edits
are synchronized and their wake work flushed. This preserves call order: an earlier
policy edit cannot silently wake a body after a later explicit sleep assignment.

CPU field resolution and its finite preflight now belong to
CPUPhysicsColliderImplementation.Integration: current Area/default fields combine
with scene/raw gravity scale and damping modes, validate the complete result before
applying motion, then update the common observed totals. CPU-only transient-force
and field-motion helpers are private implementation operations. No equivalent
inert GPU field hook is exported; GPU keeps its actual resident reducer.

PhysicsBackendOwnershipTests.IntegrationOwnership drives one scene and one raw
body through pending plus persistent force/torque, next-tick persistence, omission,
sleep and force wakeup on both selected implementations. It checks actual velocities
within .002 scene units/radians per second, pending budgets once, and 64 warmed
force/step/read cycles at zero owner/all-thread managed allocation. The existing
Area fixture checks combined/replace fields, independent channels and disabled/
transferred geometry. CPU also runs without a graphics device. Public registration,
factory-provided views, custom geometry, native allocation and foreign-platform
acceptance remain open.

## Space construction transaction

Space RID registration now precedes selected-owner construction for both explicit and lazy scene worlds. A retained owner can initialize against a live context and execute guarded query extensions before publication, while step/activation/attachment/release remain blocked. Failure cleans registry and native ownership; a rejected foreign owner stays alive in its original world. Constructor recursion cannot create a second implementation on the same context. [Initialization ownership checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.Initialization.cs) cover these phases on CPU/GPU/no-device. Public registration now executes under ADR 0103; arbitrary geometry and unexercised operation acceptance remain open.

## Named implementation selection

The internal [PhysicsBackendRegistry](../classes/PhysicsBackendRegistry.md) now retains built-in and named factories, resolves a cold immutable selection and applies strictly greater default priorities. Existing CPU/GPU space creation uses its built-in entries. WorldRuntime can retain a selected registration before lazy construction; aliases share it and later default changes cannot replace it. Factories execute outside the registry lock, reject execution-context recursion and use the common registered-space initialization/cleanup transaction. PhysicsSpace retains internal requested/actual names, including startup fallback.

[Named factory checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.NamedFactories.cs) drive real scene and raw bodies through a selected wrapper that doubles impulses, selected body/space views, fresh inactive explicit owners and failed construction recovery. CPU/GPU assert 64 warmed full impulse/step/read intervals at zero owner/all-thread managed bytes; no-device CPU and fallback retain the same path. These internal checks do not close public manager/server-extension coverage: the complete executing operation family and public-only registered consumer remain required under ADR 0103.

## Selected shape-pair operations

[Resource and body-slot pairs](physics-shape-pairs.md) now execute through selected owners, preserve original resource geometry/disabled logical indices and compose authored slot poses with current body poses. Nested staging validates counts and finite output before caller mutation. GPU scalar pose demand now uses a sixteen-byte gather plus eight-byte status; no complete solved-state mirror is added. Borrowed body/Area release rejects before registry cleanup. Public server-extension declarations and custom algorithms remain open.

## Selected contact publication

[Factory contact hooks](physics-body-extensions.md#selected-contact-publication) now determine real scene monitor contacts and raw callback snapshots. CPU workers only capture native input; the owner projects complete validated immutable output. Cached contact getters preserve that output through later association/geometry edits, and user projection failures isolate the receiver without replaying or poisoning a completed GPU step. The full public registered server/algorithm family remains open.

## Value-only body integration inputs

Selected default-force preparation now receives [PhysicsBodyForceConfiguration](../classes/PhysicsBodyForceConfiguration.md), containing pending/persistent forces, gravity, omission/rotation and role eligibility. The numerical owner receives no PhysicsBodyRuntime or scene object through that operation. Common code samples actual scene/raw intent, then consumes pending commands after successful preparation. An implementation rejected before applying its input leaves the queue and tick unchanged; this does not make arbitrary partially executed backend mutations transactional. CPU applies transient/persistent values and omission policy; GPU applies eligible transient values while resident kernels retain persistent/field integration. GPU may skip empty command preparation when no observer/activity demand requires it.

[PhysicsBodyIntegrationConfiguration](../classes/PhysicsBodyIntegrationConfiguration.md) similarly carries complete authored gravity/damping/modes/lock/omission/constants/CCD into CPU field calculation and GPU parameter preparation. CPU returns complete validated fields for binding-layer assignment; GPU returns whether successful ordinary preparation can retire dirty flags. Step, checkpoint and explicit sleep use the same GPU binding logic. Unchanged warm policy is not reconstructed or uploaded; forced diagnostic refresh retains its earlier cache semantics. Numeric private methods no longer receive scene/runtime owners for these three paths. The redundant RigidBody.ApplyConstantForces helper is removed.

[Selected force-input tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.ForceInputs.cs) prove real doubled transient response on scene/raw bodies, exact sampled values, persistent/once-only/omitted/locked behavior, frozen reporting eligibility, stable sleeping, rejection before application, transfer and zero owner/all-thread managed bytes over 64 warmup/64 measured full frames. Sleeping is tested after deferred freeze-role geometry settles; geometry changes retain their separate wake behavior. No public manager/server-extension/custom algorithm acceptance is claimed.

## Selected motion-result publication

Selected motion dispatch now borrows world lifetime for the whole query and validates its completed output before caller replacement or scene movement. Finite travel/contact values, ordered fractions, the requested moving RID, live enabled logical slots, same-space bodies, moving-mask/target-layer filtering, symmetric body/joint exceptions and explicit RID/object exclusions are checked. Collider object identity is sampled by the engine; supplied identity cannot retarget a result. Misses clear collision payload and must report completed motion. Same-space recursive motion rejects because the selected solver owns reusable query scratch; query failures retire borrowing without advancing or poisoning the world. Public registration now executes under ADR 0103; arbitrary geometry and unexercised operation acceptance remain open.

[Motion publication checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.MotionPublication.cs) exercise altered scene/raw motion and CharacterBody execution, invalid results, stale/foreign/Area IDs, disabled slots, filters/exclusions, weak identity rebind, release/transfer/hierarchy/recursive rejection and retry after failure. The fixture measures 64 warmup and 64 full-frame/query cycles with zero owner/all-thread managed bytes on CPU/GPU. Supplied test vectors are checked exactly; built-in motion checks retain .0001 scene-unit conversion and .002 point-velocity tolerances. These checks establish only their exercised scope; complete public backend registration and independent custom solver acceptance remain open.

## Independent implementation bridge


[Registered numerical adapter](physics-registered-adapter.md) now connects an internal value/RID-only implementation to real scene/raw motion, fields/forces, joints, queries, contact/Area publication, private/portable history and terminal failure. Its independent circle/spring test solver owns its own state and does not wrap a built-in. Public factories/backend classes, arbitrary custom geometry and full external acceptance remain open.

The complete internal contract and its exercised boundaries are documented in [Registered numerical solver adapter](physics-registered-adapter.md). Backend.Extension is an observed implementation identity, not a built-in creation option. Existing CPU/GPU enum factories preserve their explicit startup rules.

Public [registered implementations](physics-registered-adapter.md) now execute through PhysicsServerManager, World(string) and PhysicsServer.SpaceCreate(string). Requested/actual names retain the captured registration and startup fallback identity. The scoped PhysicsServerBackend/Extension family and immutable inputs/results have a public-only circle/spring consumer; custom geometry and full extension/performance/platform acceptance remain open.

GPU publication now reuses a preceding complete observation only when the publication epoch and all live cache validity agree. Actual callback writes still force a fresh pre-step read. Dense change streams combine status and bounded data in one wait; sparse streams keep count-first exact-prefix transfer. This changes observation scheduling without changing simulation, contact limits or callback order; [publication evidence](gpu-body-publication.md) records its costs and limits.


[Area pose and monitor publication](physics-registered-adapter.md#area-pose-and-monitor-publication) now has common CPU/GPU/registered public-only conformance for creation, pose, both callbacks, monitorability, reentry and errors. Sixty-four active transition frames allocate zero managed bytes. Complete registered geometry and final acceptance remain open.


[Contact reporting depth](physics-registered-adapter.md#contact-reporting-depth) uses common reporting policy and existing resident GPU capped records. CPU metre depths convert to scene units; registered and projected results carry explicit immutable depth. This adds no GPU transfer or solved-state mirror.


[Registered lifecycle census](physics-registered-adapter.md#registered-lifecycle-census)
now verifies initialization, local/global activation, complete interval order,
completed statistics, failure/cleanup paths and 128 moving full frames per scene/raw
role at zero managed allocation through a public-only independent implementation.
Eight former unmapped rows have their actual typed projections; complete numerical
extension and platform/performance acceptance remain open.


[Registered diagnostic preparation](physics-registered-adapter.md#diagnostic-capacity-and-publication) now notifies the selected implementation before solving and on disable/detach, with a validated synchronized point prefix. This preserves the shared CPU/GPU scene diagnostic contract; wider numerical-extension and final performance acceptance remain open.


[Registered factory-selected views](physics-registered-adapter.md#factory-selected-direct-views) now have a public-only check of body/query factory dispatch, cache identity, initially empty contact snapshots, immutable publication before callbacks, failed factories and retry, attachment retirement and zero warm managed allocations. Wider numerical-extension acceptance stays open.
