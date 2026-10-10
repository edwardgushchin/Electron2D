# Joint solver policies

Last updated: 2026-10-10

## Public boundary

Joint bias, correction-speed limits, force limits and pin linear softness now run
through shared scene/server state and the explicitly selected CPU or independent
GPU world. The [resident GPU solver](gpu-resident-joints.md#joint-solver-policies)
owns its joint constraints and spring evaluation. The old CPU-hosted GPU stage
remains a separate diagnostic path. The shared family checks below exercise the
public independent backends; complete physics and platform acceptance stay open.

| Public surface | Default and behavior |
| --- | --- |
| `Joint.Bias`, `PhysicsServer.JointGetBias(RID)`, `JointSetBias(RID, float)` | Finite [0,1] fraction of positional error requested for correction per scheduled substep. Zero inherits the space default. |
| `Joint.MaxBias`, `PhysicsServer.JointGetMaxBias(RID)`, `JointSetMaxBias(RID, float)` | Finite nonnegative correction-speed cap, default float.MaxValue. Linear correction uses vector scene-unit/s, angular stops use rad/s; the current world guard also caps each at 200. Zero disables position recovery without disabling velocity constraints. |
| `Joint.MaxForce`, `PhysicsServer.JointGetMaxForce(RID)`, `JointSetMaxForce(RID, float)` | Finite nonnegative per-second impulse budget; float.MaxValue is unlimited. Linear channels use kg·scene-unit/s² and separate pure-angular channels use kg·scene-unit²/s². Each substep allows the value times its duration in each channel. Linear axes share one vector cap. |
| `PinJoint.Softness`, `PhysicsServer.PinJointGetSoftness(RID)`, `PinJointSetSoftness(RID, float)` | Finite nonnegative inverse-kilogram anchor compliance, default zero. Adds to the two linear inverse-mass diagonals and supplies accumulated-impulse feedback. It does not tune an angular spring. |
| `ProjectSettings.Physics2DDefaultConstraintBias` | Typed physics/2d/solver/default_constraint_bias, default 0.2, finite [0,1], with feature overrides sampled when a space is created. |
| `PhysicsServer.SpaceGetConstraintDefaultBias(RID)`, `SpaceSetConstraintDefaultBias(RID, float)` | Read/replace the captured space default. Live changes update and wake zero-bias joints; explicit nonzero joint bias is preserved. Contact-separation bias is a separate contact policy. |

MaxBias and MaxForce on Joint are serializable scene projections of the same
server settings. All four scene properties survive PackedScene. Raw joint
clear/replacement preserves general settings; concrete replacement resets pin
softness with the other role-specific parameters. Body departure/reattachment
preserves authored settings and sampled frames. The RID remains stable.
Invalid values, stale/wrong-kind RIDs, off-owner access and access during a solver
step reject before changing configuration. The server guard includes a scene
joint's attachment space even while the joint has no connected bodies.

Pin/groove policies affect anchor and endpoint recovery. Springs have no
position-correction rows: Bias/MaxBias do not replace their Hooke/drag coefficients.
MaxForce caps the spring's combined elastic and damping impulse. The pin's existing
MotorMaxTorque remains an additional motor-only cap in N·m. General angular caps
use scene units, so they convert to backend torque by 0.0001 rather than the
linear-force factor 0.01.

## CPU execution

PhysicsJointRuntime retains the common authored values. PhysicsJointBackend
converts units and attaches them to the live joint simulation record. A changed
profile wakes bodies, reacquires the simulation record after a possible solver-set
transfer, and clears old warm impulses without resampling anchors. The native
record's clear/copy paths include all policy fields, preserving sleep/wake and
body/world membership transitions.

Engine joints now use the explicit default 0.2 correction fraction, replacing the
previous implicit frequency-derived profile. A joint preparation computes
biasRate=fraction/h for the scheduled substep. Native raw joints without an engine
policy retain the original frequency profile. Pin uses its two-axis mass matrix;
nonzero softness adds an inverse-mass diagonal and accumulated-impulse term.
The softened CPU 2×2 solve evaluates the augmented matrix in double precision to
avoid overflowing solely from a large finite softness, then returns float impulses.

Pin's linear accumulated impulse is clamped by vector length. Its pure-angular
sum combines spring/motor/limit rows and is capped separately; component history
is rescaled with that sum. Groove combines its perpendicular and axial rows
before clamping. Both warm-start application and completed solve apply the limits,
so cached impulses cannot bypass a newly smaller duration or force cap. Correction
speed is similarly limited as a vector, rather than independently per axis.
The CPU solver's combined bias/physical impulse remains inside that interval's
budget; its relaxation pass does not create an additional force allowance.

Each existing kinematic path interval supplies its own duration to four native
substeps, and the spring preflight uses that interval's duration. Splitting motion
therefore preserves the total force-times-time bound. The independent GPU path
keeps a separate physical/correction budget and carries the unspent original
substep allowance through CCD impact intervals. These are different numerical
methods under the same documented upper bounds, not bitwise-equivalent solvers.

## GPU stage host

The current stage host packs two additional vec4 records per joint: correction
fraction/linear cap/angular cap/softness and force-channel caps. Its joint packet is
now 224 bytes instead of 192. The existing submission transfers these values;
there is no extra policy-specific fence. Preparation, warm clamping, softened
pin solving and post-solve vector/angular projection execute in its shader.
Its spring preflight remains on CPU, as before. This host still depends on Box2D
storage and is not the independent resident backend.

## Verification boundary

`ELECTRON2D_TEST_JOINT_POLICIES=1` selects the focused public suite plus existing
pin, groove, spring and server-joint checks. Add
`ELECTRON2D_SANDBOX_GPU_SOLVER=1` to exercise the same policy assertions on the
stage GPU path. The normal CPU collider aggregate and full GPU suite include the
new public checks as well.

PhysicsJointPolicyTests covers defaults, scene/server bidirectionality, packing,
invalid rollback, wrong roles, project capture, inherited/explicit bias, vector
and angular correction caps, unequal-mass softness, linear/angular force units,
zero caps, spring force, kinematic subdivision budgets, waking, reattachment,
clear/replacement and owner rejection. PhysicsServerJointTests adds in-step policy
mutation rejection, including a scene joint with no endpoints. Existing suites
retain pendulum, motor/limits, off-center force, world replacement and physical
bone coverage.

Analytic checks use a 0.04-s frame with four substeps. Positional bias expectations
are initialError×(1-bias)^4. The softness case uses masses 1 and 3, softness 2 and
zero correction cap: relative velocity is multiplied by
(2/(1+1/3+2))^4. Force-cap checks use a 0.1-s frame and cap 10, hence linear impulse
1, or angular velocity change 0.5 for inertia 2. Kinematic motion travels 20 units
through one-unit colliders, exercising subdivisions without multiplying that
allowance. Float/rotation/iterative tolerances are 0.002 scene units or rad/s,
0.003 for the vector-force and subdivided-motion checks. Configuration and identity
assertions are exact.

Both CPU and stage GPU execute 128 warmup then 128 frames alternating actual
MaxForce and body-velocity edits, with zero owner-thread managed allocations.
This is not a native allocator measurement or full CPU/GPU performance comparison.
Public independent selection, portable snapshots and the network example now
execute. Full-contract, workload and foreign-device acceptance remain open.

The complete adaptation and units are defined in [ADR 0087](../decisions/physics-joints.md#adr-0087).

## Recorded checks, 2026-10-09

The final Release source-native build passed with zero warnings/errors. The CPU
collider aggregate and full GPU runner passed, including the new public policy
checks, the independent resident suites and compute-device lifetime with both
renderers. Runtime formatting (excluding Vendor), formatting of the touched tests,
compiled API coverage, wiki tests/generation/check and runtime shader delivery
checks passed. Coverage now accounts for 11,290 public/protected declarations with
zero unmapped; wiki generation reports 660 public types and 678 files. General
space-parameter coverage remains Partial for the remaining solver settings.

The first coverage regression assertions still expected bias/softness to be
Blocked. They now require those implemented rows while continuing to require the
open CCD and remaining space-parameter rows. A kinematic-budget fixture initially
used contact surface velocity and then the first placement transform; neither
requests motion. The final fixture places the kinematic body first and then queues
its target transform, exercising the actual subdivided path. Production motion
semantics were preserved.

Evidence: `/tmp/electron2d-joint-public-final-cpu.log`,
`/tmp/electron2d-joint-public-final-gpu.log`,
`/tmp/electron2d-joint-public-release-final.log`,
`/tmp/electron2d-joint-public-coverage-final4.log` and
`/tmp/electron2d-joint-public-wiki-final-check.log`. Generated stage solver SPIR-V
SHA-256: `bd798f1d6a3afc23c1973bceeaf428f6eefc6e6cb324abc7d2e8eb864cd03f83`.
Temporary logs and generated shader/wiki files are not shipped. The timing loops
inside the regression suites ran on a shared desktop and do not establish a
controlled throughput comparison, window FPS or network acceptance.

## Public CPU/GPU conformance

The 2026-10-10 joint family run selects a public `World(backend)` or
`PhysicsServer.SpaceCreate(backend)` for every simulated case. PinJointTests,
GrooveJointTests, DampedSpringJointTests, PhysicsServerJointTests and
PhysicsJointPolicyTests execute on both backends. Supplementary CPU handle checks
remain CPU diagnostics; common acceptance checks poses, velocities, identities,
contacts, queries, callbacks and lifecycle. The stage-host switches are retained
for their separate regression coverage.

```sh
ELECTRON2D_TEST_GPU_JOINT_CONTRACT=1 dotnet run --project tests/Electron2D.Tests -c Release
```

The shared cases cover pendulum motion, motor/limits/torque, finite/reversed/zero
length guides, live reanchoring, bias/softness/caps, force and axial damping,
transformed/off-center anchors, momentum/equilibrium, sleeping/frozen/custom
integration, raw/scene settings and packing, pending/replaced worlds, reentry,
wrong-thread/phase writes and callback failures.

Two implementation defects were exposed and corrected:

- Resident body-motion queries now include incident collision-disabled joints
  alongside explicit pair exceptions. Both endpoint directions, supplied-pose
  sweeps, overlap recovery, multiple joints and independently owned explicit
  exceptions retain their contributions. Fixed-world pins exclude no unrelated
  body. Previously the internal query deliberately omitted joint vetoes, which
  contradicted the common contact/motion contract in ADR 0087.
- The GPU spring kernel applies a finite MaxForce cap before rejecting an
  unbounded intermediate elastic impulse. MaxForce=0 supplies no impulse. Decay
  is multiplied before extreme stiffness so complete decay removes the elastic
  term. NaN still rejects; an unrepresentable unlimited impulse still fails the
  world. Tests use float.MaxValue stiffness, both force directions, complete
  axial decay, MaxForce 10 and zero over .1 s. The finite case changes unit-mass
  speed by at most one scene unit/s, within .002 roundoff allowance.

No physical stage, body or solver iteration is removed. Query metadata adds one
8-byte peer token per applicable incident joint, using retained request storage.
There is no extra GPU fence, state mirror or body readback. Sixty-four warmed
joint-filtered motion queries check **zero owner/all-thread managed bytes**;
existing 64/128-frame active joint and live-policy checks retain zero owner-thread
managed allocation. These are bounded checks, not native allocator measurements.

### Numerical and error boundaries

Hooke tests now compare against the exact initial oscillator velocity rather than
requiring the CPU interval sampler's exact value from a different integrator.
For stiffness k=20, initial extension x=50, inverse mass sum K and dt=1/60 s,
body mass m gives `v=-x*sqrt(k*K)*sin(sqrt(k*K)*dt)/(m*K)`.
The allowance is `k*k*x*K*dt^3/(6*m) + .0002` scene-unit/s: the constant initial
force sample's leading integration-error bound plus float roundoff. This covers
the CPU interval sample and GPU substep samples without relaxing mass/sign or
momentum checks. Pair momentum remains within .001; long-term relaxed distance
within .3 scene units. Analytic pin/groove correction, softness and impulse budgets
retain their .001–.003 velocity/position and .002 rad tolerances.

Pure axial damping with a fixed direction retains the .001 scene-unit/s
exponential check over .05 s. A moving tangent rotates the line between anchors;
its central drag preserves angular momentum within .01 for initial magnitude
3000 (unit mass), rather than preserving a fixed world-axis velocity component.
Off-center damping/torque retains .002 scene-unit/s and rad/s at .05 s. The existing
rotated pin/world replacement permits one unit of anchor error and .05 rad beyond
the configured angular limit over 120 steps at 1/120 s.

Malformed authored geometry rejects before starting a solve and remains repairable
on either backend. CPU spring numeric preflight can reject a batch before mutation
and resume after correction. An unrepresentable begun GPU solve instead leaves a
failed GPU world under ADR 0054: no partial scene velocity is published, further
physics/state reads reject, and ordinary disposal releases that world. Tests create
fresh worlds for separate overflow cases and preserve this explicit distinction.
Other physics families, cross-platform numerics, native allocations and full-game
performance remain governed by the physics audit.

### Recorded checks, 2026-10-10

The final matrix passes the public joint family, retained stage control, resident
joints and motion queries, shared public motion scenes, GPU checkpoints, portable
snapshots, separate CPU-server/GPU-client network correction and the default suite.
A real-window regression with 4,096 awake circles, four substeps, 16 iterations,
240 warmup/64 measured ticks passes CPU and GPU with zero owner/all-thread managed
allocation in physics and complete rendered frames. The actual GPU capture was
inspected. This is a workload regression check; the 65,536-body 60 Hz/FPS target
and general game/platform acceptance remain open.

Local ignored evidence is retained in
`bin/physics-joint-validation/2026-10-10/final/`: `checks.json`, suite logs, separate
network-process logs and `mass/` CPU/GPU JSON/PNG. Earlier failing query/spring-cap
runs and the initial integration-comparison failures are stored beside it.
