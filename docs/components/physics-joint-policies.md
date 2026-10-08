# Joint solver policies

Last updated: 2026-10-09

## Public boundary

Joint bias, correction-speed limits, force limits and pin linear softness now run
through shared scene/server state and the CPU solver. The existing internal GPU
stage host carries the same policies. The [independent resident GPU solver](gpu-resident-joints.md#joint-solver-policies)
also implements these policies, but its public-world adapter remains absent.
This closes the named public parameter capabilities; it does not establish a
complete selectable independent GPU world or networked physics.

| Public surface | Default and behavior |
| --- | --- |
| `Joint.Bias`, `PhysicsServer.JointGetBias(RID)`, `JointSetBias(RID, float)` | Finite [0,1] fraction of positional error requested for correction per scheduled substep. Zero inherits the space default. |
| `Joint.MaxBias`, `PhysicsServer.JointGetMaxBias(RID)`, `JointSetMaxBias(RID, float)` | Finite nonnegative correction-speed cap, default float.MaxValue. Linear correction uses vector scene-unit/s, angular stops use rad/s; the current world guard also caps each at 200. Zero disables position recovery without disabling velocity constraints. |
| `Joint.MaxForce`, `PhysicsServer.JointGetMaxForce(RID)`, `JointSetMaxForce(RID, float)` | Finite nonnegative per-second impulse budget; float.MaxValue is unlimited. Linear channels use kg·scene-unit/s² and separate pure-angular channels use kg·scene-unit²/s². Each substep allows the value times its duration in each channel. Linear axes share one vector cap. |
| `PinJoint.Softness`, `PhysicsServer.PinJointGetSoftness(RID)`, `PinJointSetSoftness(RID, float)` | Finite nonnegative inverse-kilogram anchor compliance, default zero. Adds to the two linear inverse-mass diagonals and supplies accumulated-impulse feedback. It does not tune an angular spring. |
| `ProjectSettings.Physics2DDefaultConstraintBias` | Typed physics/2d/solver/default_constraint_bias, default 0.2, finite [0,1], with feature overrides sampled when a space is created. |
| `PhysicsServer.SpaceGetConstraintDefaultBias(RID)`, `SpaceSetConstraintDefaultBias(RID, float)` | Read/replace the captured space default. Live changes update and wake zero-bias joints; explicit nonzero joint bias is preserved. Contact-separation bias is a separate unfinished capability. |

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
The remaining space parameters, public independent backend selection, portable
snapshots/replay, network processes and foreign-device acceptance stay open.

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
