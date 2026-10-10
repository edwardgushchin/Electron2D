# Per-body continuous collision

Last updated: 2026-10-10

## Public contract

RigidBody.ContinuousCD and PhysicsServer.BodySetContinuousCollisionDetectionMode /
BodyGetContinuousCollisionDetectionMode share [CCDMode](../classes/CCDMode.md):
Disabled=0, CastRay=1, CastShape=2. The default is Disabled, stored by the scene
property descriptor. Scene and raw server bodies share the same runtime setting;
packing, reattachment, frozen/static/kinematic roles preserve it. Changes wake a
dynamic body; undefined values reject without changing configuration. Live body,
owner-thread and step-phase guards match other body settings.

Disabled no longer silently inherits the CPU world's global automatic sweep.
Ray mode uses the midpoint of the leading support face and its round radius,
ignoring its own rotation; a large compound contour supplies the complete logical
support face. Full-shape mode keeps all solver pieces and their rotational arcs.
Sensors do not block, reciprocal masks and explicit body/joint exceptions apply,
and one-way side policy remains consistent with the current contact episode.
A CCD-disabled neighbour keeps its setting while receiving ordinary collision
impulses from a protected body. Directed rays validate the full authored ray manifold at the impact pose, including when CastRay has reduced the sweep to a support point. Compound targets choose one outer entry; containment and ray-ray pairs do not become solid segments. SeparationRayDynamicsTests exercises a 6000 u/s ray against a 0.2-unit floor at 1/60 s in both modes, accepting one unit of endpoint error for the CPU solver slop and impact advancement.

## Solved motion and remaining time

[PhysicsSpace.ContinuousSolver.cs](../../src/Servers/Physics/PhysicsSpace.ContinuousSolver.cs)
uses the existing owner-thread finalization hook after constraint solving and
before worker pose publication. Trajectories therefore include motor/spring/contact
impulses and position correction, not just velocity before the solve. Four nominal
substeps retain the existing cadence. At a detected impact the hook shortens all
pending displacements to the common elapsed fraction, then the world solves the
remaining interval and regenerates contacts. Every body consumes the same elapsed
time. Near-contact motion uses bounded short intervals; failure to converge within
4096 continuations of one nominal substep fails the world explicitly.

Each nominal substep applies forces/gravity and external springs once. Impact
continuations skip already-applied forces, disable repeated warm starts, and
retain each joint's remaining linear/angular/motor impulse budget.
Only joints that actually solved spend a budget, sampled before sleep transfer; a
newly woken joint receives the remaining-time budget instead of the full nominal
interval. Force eligibility is likewise captured before sleeping-set transfer. Original
settings and hooks are restored before callbacks. Sleeping bodies that wake during
an interval retain their unapplied gravity eligibility. Pose publication and sleep
aging use actual elapsed time; solver impulse budgets use their nominal interval.
Complete contact impulses are accumulated through the existing frame collector,
including transient impacts. Public callbacks and object events still run once
in the outer step's normal order. One-way contact epochs advance with actual
solver intervals so retired contacts cannot keep a stale side decision.

CPU geometry/response stays in Box2D.NET. The trajectory pass reuses its swept
shape queries and dynamic tree, including center-aware translation, current
logical filters, and a disk envelope for intermediate rotation. Buffers retain
peak capacity. It snapshots engine body attachments and proposed sweeps already
available in the CPU solver; it does not add a CPU mirror to the independent GPU
store. The old GPU stage experiment also runs this CPU continuous coordinator;
its fused GPU finalizer is bypassed during these intervals because their
publication fraction is known only after the host trajectory check. This is a
stage experiment, not independent GPU CCD.

A CPU-only projection of completed poses was rejected: a following body could
cross a peer that had just stopped at a wall. Prediction before the constraint
solve was also replaced because motor-created motion was not available then.
The current hook reads solved displacements before publication and consumes the
rest of the tick through ordinary contact response.

## Geometry and numerical limits

The checked CPU path covers circles, rectangles, capsules, segments, full convex
resources and paired concave segments within their existing geometry tolerances.
Ray mode is deliberately approximate. SeparationRayDynamicsTests now verifies
directed dynamic response and axial ray/full-shape CCD. WorldBoundaryTests additionally verifies analytic infinite planes with ray/full-shape
translation and rotating-plane CCD; conservative angular bounds use finite-body
distance from the plane owner rather than a nonexistent finite plane radius. The independent resident GPU store now uses the same
public enum and retains its own device CCD implementation. Explicit public CPU/GPU
worlds now run the common CCD, boundary and directed-ray suites described below.

CPU TOI uses the backend's existing 0.5-scene-unit linear slop and speculative
contact profile. CPU restitution is resolved in the ordinary constraint solver;
its within-substep trajectory is not bitwise identical to resident GPU impact
integration. In the 3000-unit/s, 0.02-second elastic test, both return -3000-unit/s
velocity and -6000 kg-unit/s impulse. Position assertions allow the CPU's contact
skin and one nominal substep of response timing, rather than asserting identical
CPU/GPU contact features or hit coordinates. Numerical failures are explicit;
there is no replay on another backend or silent loss of the remaining interval.

## Verification

PhysicsCCDTests exercises public scene/server configuration and packing, off-owner
and invalid writes, thin obstacles, off-ray corners, compound/hollow geometry,
relative motion, rotational impact, contact events, full bounce impulse, a motor
starting from rest, constant force and finite motor budgets across split impacts,
one-way surfaces, reciprocal masks, collision exceptions and a multi-frame impact
chain. The ordinary CPU collider group and full GPU suite include this runner;
independent resident CCD tests remain separate.

Build the Release test project with `-p:Electron2DBuildNativeFromSource=true`.
Run `ELECTRON2D_TEST_CCD=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-build`.
Adding `ELECTRON2D_SANDBOX_GPU_SOLVER=1` exercises the CPU host with experimental
GPU stages. `SDL_VIDEODRIVER=dummy` exercises the CPU runner without video output.
The warmed microbenchmark measures 128 reset/active public steps after 64 warmup
steps, with one projectile and one wall; it includes authored reset calls and
checks all-thread managed allocation. The late-woken motor regression derives first
touch time from authored geometry and verifies remaining-duration angular impulse,
without reading backend contact callbacks. It is not a mass-world, native-allocation
or window-FPS comparison. Foreign platforms and full physics acceptance remain open;
authoritative multiplayer is verified separately by the PhysicsNetwork workflow.

The earlier Linux/.NET 10 dummy-video run measured **p50/p95/p99
0.0220/0.0222/0.0256 ms** per reset/active step and **0 all-thread managed bytes**
(`/tmp/electron2d-cpu-ccd-headless-final3.log`). The CPU host with experimental
GPU stages measured **1.3750/1.9555/2.9777 ms** on Vulkan/RTX 3090 Ti with the same
small workload and zero managed bytes. This stage overhead is not the independent
resident GPU backend's performance.

The earlier complete 38-group CPU collider runner and full GPU suite passed
(`/tmp/electron2d-cpu-ccd-collider-final3.log`,
`/tmp/electron2d-cpu-ccd-gpu-final3.log`). The GPU suite includes the public CCD
checks through its experimental host and the separate independent resident CCD
suite; no public independent-GPU or multiplayer acceptance is implied.

Restitution contact points with incoming closing speed also bound the initial impact interval, even when positional correction is already separating. This lets outgoing velocity consume the remainder. PhysicsCCDTests checks ray/full-shape bounce at bias 0, 0.8 and 1 with full impulse accounting.


## Public CPU/GPU conformance, 2026-10-10

`ELECTRON2D_TEST_GPU_CCD_CONTRACT=1` runs PhysicsCCDTests, WorldBoundaryTests and
SeparationRayDynamicsTests against explicit `PhysicsServer.Backend.CPU` and `GPU`
spaces and scene Worlds. The older CPU-hosted GPU stage switch remains a separate
control. Shared tests now include configuration/packing/lifetime, all built-in
geometry, relative and rotational sweeps, filtering, frame impulses, infinite
boundaries, directed-ray materials/mass/sleep and scene callbacks.

Three regressions were reproduced before their resident shader fixes:

- An unchanged motor woke an explicitly sleeping body on later substeps. Driving
  wake now requires an already awake dynamic endpoint; edits and impacts still wake
  the component normally.
- After preserving sleep, an impact-awakened motor missed the remainder of its
  impact interval. A device marker in the existing 64-byte joint state now starts
  the first active solve with the remaining-time motor/general-force allowance.
  It does not repeat torque for joints that already solved in that substep.
- Directed ray contacts used the speculative search extension as physical length.
  The manifold now retains the authored tip and signed separation. A stationary
  ray with a 0.5-unit gap must remain unchanged for both slope policies; CCD at
  6000 u/s stops at Y=19.897 on GPU and Y=19.900 on CPU, within the stated one-unit
  contact-slop tolerance. The scene resting-height check is now bounded on both sides.

The motor oracle uses four first-touch times across a 0.02-s tick, immediate impact
or two idle ticks after explicit sleep, and unlimited or finite general force:
16 cases per backend. With inertia 1 and torque 0.002 N·m, expected angular velocity
is `-min(20, MaxForce) * remainingSeconds`. The 0.005-rad/s tolerance covers the CPU's
0.5-unit contact slop at 3000 u/s (0.00334 rad/s) plus floating-point error. Its
sleep/identity assertions remain exact. The separate linear-force budget fixture
sets MaxBias=0 so positional correction cannot consume the allowance being measured.

Linux/.NET 10, Vulkan/RTX 3090 Ti; one active body and an obstacle, complete public
steps. CCD includes authored pose/velocity resets, 64 warmup and 128 samples;
boundary/ray use 128 warmup and 128 samples. CCD uses 0.02 s; the others use 1/60 s.
All measured loops report zero all-thread managed bytes:

| Probe | CPU p50 / p95 / p99, ms | GPU p50 / p95 / p99, ms |
| --- | --- | --- |
| CCD reset + step | 0.0276 / 0.0278 / 0.0306 | 3.2160 / 4.1311 / 4.7219 |
| Infinite boundary | 0.0169 / 0.0171 / 0.0197 | 2.5933 / 3.3602 / 4.0999 |
| Directed ray | 0.0179 / 0.0227 / 0.0268 | 2.6544 / 3.3505 / 4.1564 |

These probes measure small-world overhead and contract behavior. They do not prove
large-world speedup, 60 FPS, native allocator behavior or foreign-platform support.
The public test log is retained in the ignored local evidence directory
`bin/physics-ccd-validation/2026-10-10/public-ccd.log`.


Regression networking is a separate gate: one separate-process check observed
856 managed bytes during warmed replay on the late CPU client, while GPU prediction/
replay and the CPU authority remained at zero. The next run and a rebuilt baseline
`84b9c443` run passed with zero. The subsequent [joint wake regression](physics-network-example.md#joint-wake-allocation-regression-2026-10-10)
located an unused joint color allocating storage instead of reusing an empty
prepared color. The captured replay and reduced public regression now pass with
zero after that fix. Both failing and passing reports remain in the local evidence;
this does not establish unconditional zero allocation for arbitrary new workloads.


The final native-window regression used 4,096 awake circles, four substeps,
16 iterations, fixed 60-Hz physics, 240 warmup and 240 sampled ticks, GPU canvas
rendering and enabled VSync. CPU/GPU physics p50 was 12.1028 / 10.3091 ms; observed
window rates were 124.8 / 144.0 FPS, with physics remaining 60.0 ticks/s. Both
physics and whole rendered-frame loops reported zero managed bytes on owner/all
threads. Neither world lost a body; both captures were saved under
`bin/physics-ccd-validation/2026-10-10/native-window/`. This is a 4,096-body
regression result, not the 65,536-fragment target or a before/after speedup claim.
