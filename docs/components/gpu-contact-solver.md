# Resident GPU contact response

Last updated: 2026-10-09

## Executing boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) now combines resident force
integration, broad/narrow phase, contact impulses, warm history and pose integration
through Simulate. It creates no CPU solver world or CPU contact/adjacency/history
mirror. [The earlier stage report](gpu-resident-bodies.md) retains body/broad/narrow
measurements; those timings exclude the response workload measured here.

This is an internal response pipeline, not a selectable public GPU backend.
[Resident joints](gpu-resident-joints.md) now share its iteration loop;
independent GPU public joint integration,
scene/server and direct-state
publication, complete frame impulse/event reports, one-way/body exceptions,
world setting integration and networking remain open. The store now resolves [mass profiles](gpu-resident-mass.md) from shared authored
geometry and uses center-relative moment arms; public-world integration still needs
a backend adapter. No full CPU-vs-GPU or window-FPS acceptance is claimed.

## Solve and history

Each substep integrates physical velocities, computes current contact points, solves
them and advances poses. Contact threads update normal/Coulomb-friction impulses
independently. A device-built incident list lets one invocation per dynamic body
gather signed impulse deltas and apply its inverse mass/inertia without
floating-point atomics or CPU graph coloring.
Degree-damped projected Jacobi bounds simultaneous updates. Related numerical
background on parallel scheduling/convergence is available in
[the authors' discussion of parallel rigid-body solvers](https://www.richardtonge.com/).
Current convergence and performance claims come from the checks below.

Static and kinematic bodies retain zero inverse dynamics. Their linear/angular
velocities contribute actual contact-point surface motion. RigidLinear suppresses
angular impulse response. Supplied kilograms and scene-unit-squared inertia govern
translation and torque. Signed shape friction/bounce use the same existing material
mixing helper as the stage host, including rough precedence and absorbent subtraction.
SetShapeMaterial journals only actual authored changes; its values are finite,
scene-unit backend inputs, not new public resource ownership.

Physical normal impulses handle nonpenetration, speculative approach and thresholded
restitution. Separate correction velocities repair penetration during pose advancement;
they never enter physical velocity or saved warm impulses. Without this split, the
initial warm-start candidate visibly jittered in the eight-box test (11.67 units/s).
The corrected test settles below its 0.5-unit/s bound. Directed separation-ray points
receive real normal/friction/restitution response in both slope modes; public CPU
ray response and full reporting remain separate open requirements.

An 80-byte device history record retains shape generations, point features, body
pose/velocity edit epochs, shape and geometry revisions, physical impulses and the
previous normal. Lookup compares full keys, not hash equality alone; a normal dot
product below 0.99 rejects the match. Reused impulses scale by substep duration.
Geometry/material/local-placement/filter edits invalidate affected records; explicit
body pose/velocity changes invalidate that body's records. Ordinary force integration
and unrelated body creation retain history. Integer epoch fields avoid encoding
metadata as arithmetic float values. Changed contour/shape identity cannot reuse
old features. GPU-to-GPU history growth and device hash rebuilding preserve existing
records; an empty contact batch clears the prior history count. Correction impulses are
scratch only. These local records are not portable network snapshots.

The contact kernel also refines separated polygon corners into a Euclidean speculative
point when face clipping has no interval. Analytic regression and the 1,620-case
family test check this path and unique point feature keys used by history.

## Storage, transfers and failure

| Device payload | Purpose |
| --- | --- |
| 64 bytes / shape slot | Authored placement, body/geometry identities, masks/sensor policy, signed material and edit revision. Scatter edits are 80 bytes. |
| 32 bytes / shared geometry | Existing descriptor, now including an integer resource revision. |
| 64 bytes / contact coefficients | Body adjacency links, normal/tangent angular Jacobians, friction, effective masses and target speeds; unchanged throughout iterations. |
| 32 bytes / contact impulses | Accumulated/delta physical and positional impulses, separate from coefficients. |
| 8 bytes / body head | Device-built incident-list head and contact degree. |
| 16 bytes / body correction | Temporary positional/angular correction velocity, reset per solve. |
| 80 bytes / history record | Full identity/revision key, physical impulses and normal. |
| 4 bytes / hash slot | Device record index; retained power-of-two table stays at most half full. |

Preparation projects each world-space lever arm onto the normal/tangent once.
The iterative kernels use these scalar angular Jacobians for contact velocity and
torque, avoiding repeated cross products and storing neither redundant inverse
masses nor world anchors. Coefficients and hot impulse data now occupy 96 rather
than 112 bytes per point (14.3% less logical storage; retained capacity rounds up).
The device-linked incident lists remain complete, including high-degree bodies.

Impulse updates and body gathers have separate small shaders. Update binds four
read-only inputs and two outputs; gather binds three read-only inputs and three
outputs. Coefficients are read-only throughout both. History, shape and geometry
buffers participate only in preparation/publication, rather than every iteration.
The shared constraint include keeps the two kernels and preparation on one layout.
Physical velocity and correction updates still use the same degree-damped solve,
substep/iteration counts and material policy; floating-point expression association
and unordered device contact publication can change trajectories within the tested
physical bounds. Bitwise equivalence is not claimed.

Warm four-substep ticks transfer 128 buffer bytes each way: force status 4, broad
summary 8, contact summary 8, solver status/match count 8 and pose status 4 per
substep. Uniform payload depends on tree sorting and iteration count. Every stage
currently waits before error/count publication: twenty waits per four-substep tick.
The opt-in ProfileSolverPasses diagnostic adds a fence after each solver dispatch
and accumulates seven SolverPassMS counters (clear, prepare, update, gather, save,
hash clear, hash insert). These measurements include submission/fence overhead and
perturb batching; they are not device timestamp queries or normal-step timings.
The flag is false by default and the ordinary submission count is unchanged.
SolverMS records total solver submission/map/dispatch/wait time; SolverWaitMS isolates
its fence wait. They exclude broad/narrow and velocity/pose passes. Driver overhead
and native allocations are not measured as heap totals; owned buffer capacity is
retained and no managed allocation occurs in the sampled warm windows.

Input validation precedes simulation. Once an executing Simulate/SolveConstraints path
fails, the store rejects future reads/mutations until disposal; no CPU replay occurs.
Finite authored inputs that generate nonrepresentable contact impulses are tested.
An independent velocity-only SolveConstraints call keeps poses fixed; Simulate consumes
the separately prepared correction velocities when advancing poses.

## Checks and numerical scope

Run `ELECTRON2D_TEST_GPU_RESIDENT_SOLVER=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after a Release source-native build. The full GPU runner includes these checks.
Add `ELECTRON2D_PROFILE_RESIDENT_PASSES=1` to enable fenced diagnostics only after
the population warmup; the test labels these results separately.
Built-in shader binaries are generated/validated from source during the runtime
build under the [shader build contract](../../tools/shaders/README.md#built-in-runtime-shaders).

Analytic unequal-mass impacts verify momentum, elastic energy and absorbent bounce;
offset contacts verify torque/inertia and rotation locks. A free radius-one disc
(mass two, inertia one) hits a fixed floor at (10,10) units/s; friction yields
20/3 units/s translation and rad/s spin, preserving angular momentum 20 about
the contact within 0.001 mass·scene-unit²/s. Signed friction, stationary
linear/angular surface velocity, a gravity-driven conveyor, both directed-ray slope
modes, sensors, input rejection, history revisions/identity/growth/timestep changes
and failed-state disposal execute. A locked plank with 64 then 257 incident
contacts checks complete impulse gathering and capacity growth. Turning every
support into a sensor checks that an empty physical incident list cannot apply
stale warm or correction impulses. One damped iteration must stop its 10-unit/s
normal velocity within 0.002 units/s (summation error across 257 contributions). Velocity/angular tolerances are 0.001; accumulated
face-friction velocity allows 0.002. Elastic energy tolerance 0.02 is about 1.3e-6
relative to the analytic 15,000-unit result. Conveyor speed allows 0.05 after two
seconds at 60 Hz, four substeps and sixteen iterations.

The stack contains eight freely rotating 20×20 boxes, unit mass, inertia 800/12,
friction one, a static floor and a one-unit top-box horizontal offset. It runs ten
seconds at 60 Hz, four substeps, thirty-two iterations. All boxes remain within five
horizontal units of the column, speed below 0.5 and vertical displacement below six
units. The positional bound includes accumulated 0.5-unit per-contact penetration
allowance across eight levels. This is a finite-duration stability check, not a
proof for arbitrary stack height or iteration count.

The population runs contain 4,096 / 65,536 radius-one circles with mass one, inertia
0.5, friction 0.3 and zero bounce, plus three static pit walls. They start on a
64×64 / 256×256 two-unit grid. Gravity is 980, timestep 1/120 s, four substeps and
sixteen iterations; contact margin is 0.1 and penetration allowance 0.01. Sleeping
is not implemented in this store, so the whole population stays active. Warmup is
384 ticks and 256 ticks are sampled. Every body is then read explicitly outside the
window to reject nonfinite state, wall/floor escapes and unbounded total mechanical
energy. The energy allowance is one tenth scene unit of gravitational potential
per body for stabilization/float error; the observed change is negative. Large piles
are still rearranging at the end; no whole-pile rest claim is made.

The initial cold-history candidate failed the 65,536-body containment check: maximum
Y reached 12,450 below a floor at 511. It is rejected, not counted as a faster valid
result. Warm history plus separate positional correction keeps the full population
inside. Retained evidence: `/tmp/electron2d-resident-solver-cold-history-failure.log`,
`/tmp/electron2d-resident-solver-warm-bias-failure.log` and subsequent solver logs.

## Initial response baseline (before iteration storage changes)

Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti, 2026-10-08; same parameters
and warmup above. Solver columns are arithmetic means per tick; latency columns
are distributions of the whole Simulate call.

| Run / dynamic bodies | Tick p50 | Tick p95 | Tick p99 | Mean total wait | Mean solver | Mean solver wait |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Dedicated / 4,096 | 3.3889 ms | 4.6525 ms | 5.2430 ms | 2.3864 ms | — | — |
| Dedicated / 65,536 | 23.5486 ms | 25.1313 ms | 26.3953 ms | 20.5752 ms | — | — |
| Full GPU suite / 4,096 | 3.4500 ms | 4.7026 ms | 5.3656 ms | 2.4136 ms | 1.4861 ms | 0.7468 ms |
| Full GPU suite / 65,536 | 23.6484 ms | 25.2589 ms | 25.9224 ms | 20.6302 ms | 15.4125 ms | 14.5663 ms |

The dedicated run predates the per-stage timers; it uses the same solve equations
and partial-buffer writes. Both runs report zero warmed owner-thread managed bytes
and 128 upload / 128 readback bytes per tick. Mean uniforms are 12,712 / 13,488 bytes.
The final full-suite populations retain 11,765 / 193,531 contact points, with
11,699 / 193,065 warm matches in the last substep. Maximum center Y is 126.0435 / 510.2666
against floor tops 127 / 511. Maximum linear speeds are 0.5983 / 59.3352 units/s;
the larger pile is still rearranging. Total energy change is about −42.90 million /
−5.046 billion mass·scene-unit²/s², consistent with settling and dissipative contacts.

The eight-box full-suite check ends with maximum speed 0.29106, maximum vertical
shift 2.65820 and sixteen warm matches. The solver is the largest measured cost at
65,536 bodies; the 23.65-ms step is not a 60-FPS result and includes neither a renderer
nor the remaining joint/sleep/CCD/publication work. Further iteration/data-access
and submission work, then a complete same-revision CPU/GPU comparison, remain required.

Logs: `/tmp/electron2d-resident-solver-stores.log` (dedicated),
`/tmp/electron2d-resident-solver-gpu-suite.log` (complete GPU suite), and
`/tmp/electron2d-resident-solver-contacts.log` (contact kernel regression). The full
suite additionally rechecks the original stage-hosted kernels and both renderer
lifetimes. These are Linux device checks; other platforms and native allocation
accounting are not inferred from them.


Generated SPIR-V SHA-256 for the initial baseline (intermediate outputs):

- PhysicsResidentBodies: `9fe5093c311b6ebd2992b791ceea081b324ab16f3a8c217ec875cea4c8c8ff50`.
- PhysicsResidentShapes: `a373d0845b4bf100cc7d7c7c5f0c6eb4a4bd2a39b2d64eda895a2e046f89b2cd`.
- PhysicsResidentContacts: `e45c89f31fe2313f3f7496fd96f4e0d0b760aa13eb05f240d62fa1c13121adc7`.
- PhysicsResidentSolve: `81ac20b2eb852ae403cd95a9102b17f1363247e5e8067e64546bafa44ff7c8e0`.


## Compact iteration layout (2026-10-08)

The population, gravity, four substeps, sixteen iterations, warmup/sample counts,
material values and numerical acceptance bounds above are unchanged. Rebuilt
before/after runs use the same host and test harness; only the solver implementation
changes. These are independent executions of unordered device work, not identical
per-step trajectories, and desktop load/clocks are not pinned.

| Sequential comparison / bodies | Tick p50 | Tick p95 | Tick p99 | Mean solver | Mean solver wait |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original / 4,096 | 3.6073 ms | 4.3815 ms | 5.7790 ms | 1.5145 ms | 0.7419 ms |
| Compact / 4,096 | 3.4231 ms | 4.4521 ms | 5.2984 ms | 1.3482 ms | 0.7608 ms |
| Original / 65,536 | 23.7931 ms | 25.5981 ms | 26.3623 ms | 15.7806 ms | 14.8417 ms |
| Compact / 65,536 | 21.3267 ms | 25.3955 ms | 26.8665 ms | 14.2354 ms | 13.5012 ms |

The large-case median falls by 10.4% in this pair, but its p99 does not improve.
Other dedicated runs gave original 21.0186 ms and compact 16.4406 ms medians; this
spread precludes a stable 60-FPS claim. Every measured warm interval still has zero
owner-thread managed bytes, 128 buffer bytes each direction, and mean uniforms
12,712 / 13,488 bytes per tick. The compact comparison keeps all bodies in the pit
(maximum Y 126.0436 / 510.2568), with 11,828 / 193,436 contact points and negative
mechanical-energy change. The eight-box case ends at speed 0.44192 and vertical
shift 2.63335, within the same 0.5 and six-unit bounds.

Fenced diagnostics explain the remaining limit, but must not be substituted for
normal-step latency. For 65,536 bodies, measured mean ms/tick by pass group:

| Group | Original | Compact |
| --- | ---: | ---: |
| Clear body scratch | 0.3119 | 0.2759 |
| Prepare coefficients / warm lookup | 0.9346 | 0.9817 |
| Update contact impulses (64 dispatches) | 7.8822 | 6.6741 |
| Gather body impulses (68 dispatches including warm start) | 8.5235 | 8.5148 |
| Save history | 0.5501 | 0.5464 |
| Clear history table | 0.3811 | 0.3221 |
| Insert history table | 0.3531 | 0.3360 |

Repeated body gathers remain expensive. A candidate that compacted only incident
indices into contiguous per-body ranges required another dispatch/buffer and gave
23.8956 ms median without a clear gain; it was removed. Separate kernels alone gave
18.6135 ms, and separate impulse storage without projected Jacobians gave 21.5661 ms
in exploratory runs. Only the combined compact-Jacobian implementation is retained;
these exploratory timings are not controlled single-variable attribution.

Evidence: `/tmp/electron2d-resident-solver-ab-baseline.log`,
`/tmp/electron2d-resident-solver-ab-final.log`,
`/tmp/electron2d-resident-solver-pass-baseline.log`,
`/tmp/electron2d-resident-solver-pass-final.log`. The new kernels retain analytic
momentum/energy/inertia/material tests, failure rejection, history boundaries,
257-point gathering, stack stability and complete-population containment checks.
The full `ELECTRON2D_TEST_GPU_PHYSICS=1` runner also passes with the new kernels,
including legacy stage checks and GPU-device lifetime under both renderers; its log
is `/tmp/electron2d-resident-solver-layout-gpu-suite.log`. Release builds, compiled
shader source-generation checks and scoped formatting execute separately.
Public CPU/GPU comparison, complete joint settings, sleep/CCD, scene publication, network replay and
real-window FPS remain outside this internal optimization.

Iteration-layout SPIR-V SHA-256 before mass-profile integration (untracked outputs):

- PhysicsResidentSolve: `b571e825415b248ec27130b4708a646673c31e58efa575607a4a7427cd99362e`.
- PhysicsResidentUpdate: `3284e297de5fe6785c2860bbd6e8da533d24fc373c5b5ebf58176e6df4bf0b85`.
- PhysicsResidentGather: `49a15a674bcda51d4d1e0a6000ef44c228bdfe553a11238312c0ad05974ce58d`.

[Resident sleep](gpu-resident-sleep.md) now supplies dynamic component sleep/wake. Historical timings/traffic above predate its graph passes and 8-byte body status. Current active benchmark bodies explicitly disable automatic sleep; no smaller awake population is substituted.

[Resident continuous collision](gpu-resident-ccd.md) now reuses this solver at impact intervals. Initial force/spring/motor duration is not replayed; CCD restitution uses a bounded contact skin, while ordinary non-CCD restitution keeps its original boundary. Public transient contact/event accumulation remains an adapter obligation.
