# Resident GPU joints

Last updated: 2026-10-09

## Executing boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) owns independent device pin,
finite-groove and damped-spring connections. Their data, warm impulses, collision
veto lookup and solving stay on the device. No Box2D world, joint handles, CPU
joint solver or evolving joint-state mirror participates. This extends the
[resident contact solver](gpu-contact-solver.md) under ADR 0054; it is still an
internal backend component, not a selectable PhysicsServer implementation.

Per-joint bias, pin-anchor softness and general impulse/correction caps now execute
in this internal component. Their CPU implementation and scene/server adapters,
full queries/events, portable checkpoints and network replay remain open. Existing
public CPU joints and their settings are unchanged; no public coverage row is
closed by this internal solver work.

## Ownership and authoring

AddJoint returns a store/slot/generation-qualified handle. JointDefinition contains
authored engine frames and scalar settings, including collision policy. Coordinates
are body-local scene units; a pin alone may use a default second handle for the
fixed world, in which case FrameB is in world coordinates. No hidden body is needed.
For a groove, FrameA.X is the guide direction. Constraint moment arms use the [resolved local mass center](gpu-resident-mass.md),
while sampled frames remain relative to the body origin. Frames are validated as finite unit-scale transforms, with local-anchor radius and
guide/rest extents at most ten million scene units. Enabled pin angle limits stay
inside ±0.99π. MotorMaxTorque uses N·m, converted once to kg·scene-unit²/s² (×10,000);
angular speed is rad/s. Spring stiffness is kg/s² and axial damping kg/s.

SetJoint replaces the complete authored configuration after validation. Identical
writes do nothing; actual edits invalidate only that joint's warm record. Roles or
endpoints can change on the same internal handle. GetJointDefinition returns its
authored value. RemoveJoint unlinks both endpoint lists and invalidates identity.
Body removal visits its incident joints and removes them before destroying the
body. CPU metadata contains authored frames/settings and attachment links, never
solved pose, velocity or accumulated joint impulse. Disposal and owner-thread/error
rules are the body store's existing rules.

Pending changes coalesce by slot. A device scatter updates configuration and resets
changed warm records. Device-to-device growth preserves other configuration and
history. A GPU hash table stores canonical collision-disabled body pairs; cold
changes rebuild it from live device joints. Several joints may veto the same pair,
and removing/toggling one cannot remove another's contribution. Broad phase checks
this table for physical body pairs; sensor monitoring retains its separate policy.
Unchanged simulation does not upload authored joint records or rebuild the table.

## Constraint integration

Pin contributes two bilateral anchor rows, optional lower/upper angular rows and a
bounded motor row. Groove contributes a perpendicular row and two axial endpoint
rows; zero and negative intervals are valid, and relative rotation stays free.
A rotating guide uses the derivative of its rotating axis, including the first
body's anchor-to-anchor lever arm. The same projected-Jacobi update and body gather
iterate joint and contact rows together. Pin/guide static anchors remain fixed;
virtual static-surface velocity belongs to contacts and does not drive these rows.

The solver reserves five 64-byte coefficient / 32-byte impulse rows per retained
joint slot. Inactive and unused rows are explicitly cleared and do not enter body
incident lists. When the final joint is removed, subsequent contact-only solves
exclude the entire joint row range. Joint rows use signed or one-sided impulse
bounds; motor bounds are torque×substep duration. Positional corrections remain
separate from physical velocity. Motor rows do not constrain correction velocity.

A spring evaluates the existing Hooke-plus-exponential-axial-drag rule on GPU before
contact response is prepared. It includes both anchors' rotational effective mass,
preserves tangential velocity, and has no force direction below the existing
scene-unit normalization epsilon (about 1.19e-5). All spring impulses are prepared
before a body gather applies them. A preparation error prevents that gather from
applying the batch; a later execution failure invalidates the whole store without
CPU replay. Spring-only prepasses are skipped when no authored spring exists.

After spring forces, pin/groove warm impulses and contacts share the ordinary
iteration loop. A 64-byte per-joint history record retains five scalar impulses,
prior substep duration, endpoint edit/generation epochs and the current substep's
impulse budget. Warm impulses scale by
duration and respect current bounds. Pose/velocity edits invalidate affected
endpoint history; joint changes reset the record. These are local device records,
not portable server snapshots. Joint warm state requires no readback. The seven optional contact-pass timers
include pending joint dispatches in the next fenced pass; they do not isolate
joint preparation time. SolverMS and WaitMS retain their whole-submission scopes.

## Storage and transfer boundary

| Payload | Size / purpose |
| --- | --- |
| Authored device joint | 128 bytes: generation/type/endpoints, endpoint generations and flags, local frames, limits, motor/spring values and solver policy. |
| Scatter edit | 144 bytes per changed slot, plus an 8-byte batch status reset/readback. |
| Warm state and budget | 64 bytes per retained joint slot, preserved during device growth. |
| Collision veto table | 4 bytes per hash slot; power-of-two capacity at least twice joint high-water count. |
| Solver rows | Up to five × (64 + 32) bytes per joint high-water slot, sharing contact buffers and body adjacency. |
| Unchanged no-shape joint tick | Four substeps, each transferring body-force status 8, solver status 8 and pose status 8 bytes in each direction: 96 bytes per tick. Twelve publication waits. |

Explicit body snapshots are read only for requested consumers; the tests read the
complete population after their timing/allocation window. Worlds with colliders
retain the broad/narrow summary traffic documented in the contact report. Uniform
bytes and waits are counted separately. Spatial uniforms are now 64 bytes,
including joint filter bounds; prior component timings used the older layout.
Native driver allocations and asynchronous publication are not inferred from these
payload counts.

## Joint solver policies

The internal JointDefinition carries these values. All reject nonfinite or
negative input before replacing the old definition; Bias also rejects values
above one. A real change wakes both endpoint components and clears only that
joint's history and budget. Reused slots start with the new definition.

| Value | Default | Executing meaning |
| --- | --- | --- |
| Bias | 0 | Zero uses Simulate/SolveConstraints correctionFactor. Otherwise the value is the fraction of anchor/limit error requested for correction in one scheduled substep. |
| MaxBias | float.MaxValue | Maximum positional correction speed, also bounded by the shared maxCorrectionSpeed guard. Linear anchor/groove correction uses vector length in scene units/s; angular stops use rad/s. Zero suppresses positional recovery while retaining physical velocity constraints. |
| MaxForce | float.MaxValue | Unlimited sentinel, or a per-second budget: linear kg·scene-unit/s² and a separate pure-angular kg·scene-unit²/s² channel use the same authored scalar. The budget for a scheduled substep is MaxForce times its duration. MotorMaxTorque remains the additional N·m motor-only cap. |
| Softness | 0 | Pin linear-anchor compliance in inverse-kilogram units. Each effective inverse mass gains softness, and the iteration subtracts softness times accumulated impulse. Zero retains the rigid anchor. It does not create an angular spring or affect groove/spring roles. |

Pin and groove keep physical velocity and position correction separate. The sum
of the lengths of their accumulated linear physical and positional impulses must
fit one linear budget; the sum of the absolute accumulated pure-angular physical
and positional impulses fits the separate angular budget. This is a conservative
bound, so two correction channels cannot independently spend the full allowance.
Groove projects its perpendicular/endpoint rows together; pin projects both anchor
axes together. Limits are not per-axis, per-iteration or per-row force allowances.

Projection runs on GPU after warm-start preparation and each Jacobi update, before
body gather. It rescales accumulated values and adjusts the delta by the same
change, so replacing an old warm impulse does not add an extra impulse. The pass
is absent when no authored pin/groove has a finite limit. Spring Hooke and axial drag
share one clamped impulse in their existing prepass. Springs have no positional
recovery rows: Bias and MaxBias do not alter the force-law coefficients.

Extra CCD impact solves use the unspent budget from the original scheduled
substep. Completed solves accumulate spent magnitudes on the device; new scheduled
substeps reset the allowance using their own duration. This prevents impact count
or solver iteration count from multiplying force. A standalone SolveConstraints
call starts its own interval. No budget state is read back or mirrored on CPU.

An inactive endpoint or angular stop remains a speculative physical constraint,
but supplies no position-correction row. Previously both stops attempted to keep
zero correction velocity, so a distant, satisfied stop partly cancelled recovery
at the violated one. The analytic bias check caught that pre-existing defect; both
groove and pin angular stops now share the corrected rule.

These semantics follow the accepted policy boundary in [ADR 0087](../decisions/physics-joints.md#adr-0087).
The current CPU/native and public projections remain required work.

## Verification

Run `ELECTRON2D_TEST_GPU_RESIDENT_JOINTS=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after a Release source-native build. The full GPU runner includes the suite.

Checks cover unequal-mass pin momentum, fixed-world torque units/sign, motor/limits,
an unshaped off-center pendulum, changed timestep and device history growth;
finite/negative/zero groove intervals, free rotation and moving-guide linear/angular
momentum; analytic Hooke impulse, off-center torque, pure exponential axial damping,
tangential/coincident anchors; independent collision contributions, sensor policy,
foreign/stale endpoints, rejected replacement, wrong thread and endpoint cleanup.
A combined spring/groove/body-contact case settles on a floor, then removes every
joint and verifies free horizontal motion. Unrepresentable spring or bilateral pin impulses put
the world into its defined failed state and rejects subsequent reads.

Pin pair velocity tolerance is 0.001 scene units/s after 64 iterations. The isolated
0.01-N·m motor, inertia 100 kg·scene-unit² and one 1/60-s step allows 0.00002 rad/s.
Motor/angle stops allow 0.03 rad after five seconds at 60 Hz, four substeps and 32
iterations. The five-second pendulum allows 0.5 scene unit anchor error and final
mechanical-energy increase no greater than that error's gravitational potential
(490 for unit mass and gravity 980). Groove endpoint/normal error allows 0.1 unit;
the moving-guide pair checks linear/angular momentum to 0.001 in scene units.
Spring one-step velocity/torque assertions allow 0.0001, exponential axial speed
0.0002. Coupled spring/contact support allows 0.1 unit and 0.1 unit/s.

The population check uses 4,096 independent length-ten world pins, unit body mass,
inertia 20, a 64×64 origin grid spaced by 40, gravity 980, timestep 1/120 s, four
substeps and sixteen iterations. It warms 128 ticks and samples 128, including one
unchanged authored setting write per tick. All body anchors are checked afterward
within 0.3 scene unit. This checks persistent solving and allocation, not a full
CPU/GPU comparison, sleep/CCD performance, window FPS or arbitrary long-chain
convergence. The existing degree-damped solver retains that convergence limit.


## Historical measured run, 2026-10-08

Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti, 2026-10-08, the population
and timing window above: p50 **1.3928 ms**, p95 **1.7782 ms**, p99 **2.8448 ms**;
mean fence wait **0.8354 ms** per tick. Managed owner-thread allocation is **0 B**;
64 B upload, 64 B readback and 9,728 B uniforms per tick. JointUploadBytes is
unchanged throughout the samples. Maximum anchor error across all 4,096 bodies
is **0.00858** scene unit. Explicit snapshot reading is outside these figures.

The complete GPU runner also passes contact/broad/narrow/body and old stage-host
checks and compute-device lifetime with both renderers. Its contact-only 65,536-body
workload remains inside the pit with zero warmed allocation: p50 20.2813 ms,
p95 21.6094 ms, p99 22.4724 ms, 128 B each transfer direction and 13,616 B mean
uniforms. This is a regression measurement of the previously specified workload,
not a controlled CPU/GPU comparison or a 60-FPS window result.

Evidence: `/tmp/electron2d-resident-joints-bounds-gpu.log` (final suite) and
`/tmp/electron2d-resident-joints-profile.log` (earlier dedicated run). Generated SPIR-V SHA-256:

- JointEdits: `efee115886e42594b3faa90c091e7c10a77d0c797ac854017710a96b907481e5`.
- Joints: `599f68bba2748135f0e78bc1012463289425b9a9875a9d0d1c5cc2061ca77b36`.
- Shapes: `5e630174a874402efae9ed2e68511ebd2b75c2fe502b8f616d7d4961bba47195`.
- Update: `a233801b06b3853d3d738873fa6fbe857d13c72321e39a4936a9d4bcbedd466d`.
- Gather: `d3c05f9b222323df877283431e0d81c9d152643ee09adc1de8fc30a853132f37`.

Shader binaries are generated/validated during the build and remain untracked.
Other devices/platforms, native allocator totals and owner visual acceptance are
not established by this Linux compute run.

Resident joints now participate in [GPU sleep components](gpu-resident-sleep.md). The measurements above predate sleep; current regression populations explicitly set CanSleep=false, preserving the all-awake workload.

CCD impact intervals reuse pin/groove/contact solving but do not reapply the scheduled spring impulse or motor torque budget. Unrelated spring and motor analytic checks cover that cadence in GPUPhysicsCCDStoreTests.


## Policy verification, 2026-10-09

GPUPhysicsJointPolicyTests now runs inside the joint/full GPU suites. Analytic
one-substep checks cover inherited/explicit bias, diagonal vector speed limits,
zero correction, active versus inactive angular stops, unequal-mass soft pin
response with warm history, free rotation, 32/96-iteration force caps, zero force
after a live edit, spring and pure-damper caps, general versus motor torque caps,
shared physical/correction allowance and one/four-substep budgets. An unrelated
real 3000-unit/s CCD impact verifies that the pin retains only its original
substep allowance. Invalid configuration preserves its previous definition; slot
reuse resets all policies. A 128-warmup/128-sample loop alternates real joint policy
and body-velocity edits while checking zero owner-thread managed allocation.

For isolated bias/softness/force equations the error allowance is 0.0002 in the
asserted scene position, scene velocity or radian channel after 64 iterations;
this allows float rounding and residual Jacobi convergence. Finite vector-force
projection at 32/96 iterations allows 0.002 scene unit/s, combined impulse budget
0.001 kg·scene-unit/s, and the 0.02-s CCD budget check 0.0005 scene unit/s. Identity,
rollback and allocation assertions remain exact. Public API/event/network tests
are not replaced by these internal checks.

The mass-population check additionally repeats the same 4,096 moving pins with
Bias=0.3, MaxBias=100, MaxForce=5000 and Softness=0.001. It retains the identical
population, gravity, 128/128 warmup/sampling and final all-body read. The default
profile runs no limit-projection dispatch; the finite profile adds one before
warm gather and one after each of sixteen updates, per substep. Both retain
96 B upload and readback per tick each, with no authored joint upload in the
unchanged timing window. Uniform traffic is 12,288 versus 16,640 B/tick; this
extra work does not add a fence or state readback.

Final dedicated run on Vulkan / NVIDIA GeForce RTX 3090 Ti / .NET 10.0.1:

| Profile, 4,096 pins | p50 / p95 / p99, ms | Mean fence wait, ms/tick | Managed B/tick | Maximum anchor error, scene units |
| --- | --- | --- | --- | --- |
| Default policies | 1.8183 / 2.8654 / 3.6338 | 1.1023 | 0 | 0.00858 |
| Finite limits and softness | 2.3407 / 3.1062 / 4.4058 | 1.3873 | 0 | 0.00606 |

Desktop load was not controlled; repeated runs varied substantially. These are
recorded internal-path latency distributions, not a speedup claim, a full CPU/GPU
comparison or window FPS. Final explicit body reads remain outside the sample.
Native allocator totals and other devices are unverified.

Evidence is in `/tmp/electron2d-joint-policy-final-scoped.log`. The full GPU suite
also passed in `/tmp/electron2d-joint-policy-final-gpu.log`, including both renderer
lifetimes; the final scoped rerun followed a dispatch-only optimization that skips
unnecessary limit passes for spring-only worlds and added real policy-edit allocation
checks. No public CPU behavior changed and no new CPU/public joint policy claim is
made. Release/source-native builds, runtime/touched-test formatting, compiled API
coverage, shader delivery checks and wiki generation/check passed. The API remains
11,275 declarations with zero unmapped; the shader publish probe contains 26 generated
resources. Generated files remain untracked. Current generated SPIR-V SHA-256:

- Joints: `538ef810426fcaa28e109e974c2e01f55d0e5699c5a05335861e581c31d0fb0e`.
- Update: `fef2402bbd6595be715db3671feaaf3d19197c272046ef0cb6fc8a9927ed98b2`.
