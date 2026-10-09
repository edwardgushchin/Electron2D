# Physics backend selection and shared worlds

Last updated: 2026-10-09

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

Preparation and completion currently publish changes synchronously. Contact receivers
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
