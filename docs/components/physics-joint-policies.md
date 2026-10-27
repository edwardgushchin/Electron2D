# Joint solver policies

Last updated: 2026-10-27

Collision-policy publication now retains the latest requested flag and complete
common pair contribution through selected errors. Equal setters and world
query/history/positive-active-step preparation retry before numerical use, while
zero/inactive intervals and configured input reads do not repair. Retirement drops
the pending request and exactly one contribution. See [complete exception operations](physics-registered-adapter.md#complete-exception-operations)
and the updated [ADR 0087](../decisions/physics-joints.md#adr-0087).


## Typed parameter publication

A selected publication error retains the requested typed parameter channel.
Equal setters, live world queries, local/portable capture and active positive
steps retry before numerical use. Configured reads and zero/inactive intervals
do not repair. Solver policy, pin limits, each motor channel and groove limits
retire their debt independently; a later channel failure does not replay an
already completed channel. Publication reentry rejects live preparation.
Detach/clear/free discards the old attachment's debt; reentry applies retained
configuration to a fresh attachment. Existing per-interval refresh remains separate.
See [complete joint parameter operations](physics-registered-adapter.md#complete-joint-parameter-operations)
for actual scope, response checks and remaining acceptance limits.

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
dispatches to CPUPhysicsJointImplementation, which converts units and attaches them to the live joint simulation record. A changed
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

<a id="public-cpu-gpu-conformance"></a>
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
  explicitly skips the elastic product when it reaches zero. This preserves
  complete axial decay when backend compilation reassociates multiplication of
  extreme stiffness and displacement. NaN still rejects; an unrepresentable unlimited impulse still fails the
  world. Tests use float.MaxValue stiffness, both force directions, complete
  axial decay, MaxForce 10 and zero over .1 s. The finite case changes unit-mass
  speed by at most one scene unit/s, within .002 roundoff allowance.

CI run `39869572938` on macOS ARM64 passed the earlier query/compound/CCD checks
but failed this complete-decay case at `PhysicsJointPolicyTests.cs:145` with an
invalid-constraint status. The guarded product addresses that arithmetic path;
the corrected Metal execution still requires its full target suite.

The guarded kernel passes the existing complete shared CCD/joint-policy profile
on local NVIDIA Vulkan and the isolated CI Mesa 26.0.8 library. Both execute the
extreme-stiffness, complete-decay and zero-force cases, then 128 warmup/128 active
policy frames with zero owner/all-thread managed bytes. Native translation
retains the explicit zero-decay branch in generated MSL; this is compiler evidence,
not Metal execution.

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

The ordinary full runner executes the complete PhysicsJointPolicyTests on CPU and
GPU; `ELECTRON2D_TEST_CONTINUOUS_PHYSICS=cpu|gpu` selects the same cases. After 128
warmup frames, 128 live-policy/body-velocity edit frames require 0/0 owner/all-thread
managed bytes. Analytic bounds and error/lifetime assertions remain unchanged;
native allocations, foreign platforms and final performance acceptance stay open.

[Off-center spring anchors](physics-registered-adapter.md#off-center-spring-anchors-and-portable-joints)
now execute an independent consumer's actual anchor torque, angular-velocity axial
damping, effective mass and shared force cap on scene/raw spring identities in both
endpoint orders. CPU supplies the same analytic checks; serial/separate modes pass.
[Portable correction](physics-snapshots.md#portable-joint-continuation) preserves
sampled frames for scene/raw pin/groove/spring and verifies continued constraints,
private replay and strict zero warm managed allocation. These bounded circle fixtures
retain broader numerical/graph/current GPU/native/network/performance limits.


## Joint force after reversing CCD impact

The public-only `--joint-impact-budget` consumer checks a capped pin or zero-length
groove joining two free, unit-radius, mass-2 linear rigid bodies. One starts at
3000 scene units/s and rebounds elastically from a static unit circle; the other
starts at rest 1000 units away in Y and receives only the joint impulse. Both have
zero gravity/damping/friction and no positional joint correction. The 0.02-second
step uses MaxForce 0 or 50, so each body can receive at most 1 impulse unit, or
.5 units/s total velocity variation. Rotation is locked to isolate that channel.

A final velocity alone is insufficient: repeated opposite joint impulses can
cancel while the contact solver retains the same rebound speed. For the free
witness, starting at rest with displacement `x`, final velocity `v` and duration
`dt`, total velocity variation must be at least `abs(2*x/dt-v)`. Each instantaneous
impulse contributes its velocity change times a coefficient between -1 and 1 to
that expression, independently of a solver's subdivision schedule. The test uses
this necessary bound with .003 units/s float allowance. It also checks rebound
speed within .03, transverse velocity/rotation within .001, complete contact
identity/shape indices and full contact impulse against both bodies' momentum
within .03 impulse units. Authored force remains exact, with one outer tick and
state callback. Targets at signed X=8,23,38,53 place impacts in all four nominal
quarters; disabled controls cross between discrete samples without contact.

The 128 configurations combine both CCD modes, both body ownership choices,
scene/raw pin and groove resources, both travel signs and all four impact phases.
Force-zero and force-50 calls provide 256 continuous cases; there are another
128 disabled controls and five private checkpoint replay cases (.001 position,
.002 velocity, .01 full contact impulse). After 256 warmups, 256 complete moving
frames, policy edits, state publication and assertions must allocate strict
0/0 owner/all-thread managed bytes.

The independent public provider previously retained a signed accumulated joint
impulse across CCD passes. Reversing it made spent force available again. At
configuration 64 its witness reached x=.0020019915 and final v=-.5, requiring
.7001991 units/s against the .5 allowance. CPU passed the same test. The provider
now resets iterative accumulators for each interval and retains spent magnitudes
for the whole frame, independently for linear, angular and motor channels.
Remaining caps clamp at zero. Iterations share a cap; a new CCD pass consumes its
unspent remainder. Spring evaluation remains once per frame with its existing
combined cap. Transient budget fields reset before each new full solve, including
post-checkpoint replay; no public snapshot/schema changes are needed.

This correction is confined to the independently implemented public consumer;
the shipping CPU/GPU solvers and API are unchanged. The whole extension class
remains Partial until its remaining numerical/inherited/platform requirements are
accepted. Arbitrary joint/contact/CCD networks, native allocations, routed
networking and final performance acceptance remain separate.


The 2026-10-20 Linux/.NET 10 source-built verification passes 12 new impact
profiles (CPU/registered/GPU, owner/separate and CLR/strict NativeAOT), plus
12 CLR registered companions for joint response, continuous response, loaded
joint/contact sleep, four/64-body closed graphs and portable joints in both
thread modes. Each impact profile measures 256 moving complete frames at 0/0
owner/all-thread managed bytes (3,072 measured frames total). Across both build
variants, maximum rebound-speed error is .000244 units/s on CPU and zero at the
printed six decimals on registered/GPU; maximum free-witness budget excess is
zero and inferred net joint impulse is at most 1 on all three. Companions retain
their own physical/replay and allocation assertions.

GPU processes observe the selected NVIDIA RTX 3090 Ti hardware driver without a
software Vulkan driver; CPU/provider profiles run with Vulkan unavailable and do
not load NVIDIA. No new Xid is observed over the final profiles. Source proofs
match the complete 2,422 runtime and 262 consumer PDB documents for both ordinary
and AOT managed-input builds; strict NativeAOT has no trim/AOT warnings. Logs,
recovered exact RED source inputs, payload hashes and validation are under
`bin/physics-joint-impact-budget/2026-10-20/`. Functional elapsed times overlap
compilation/quality work and are not performance measurements. The API census
remains 1304/1369 Implemented (95.25%), 65 open, 92 Excluded.


## Retained hard-row guesses

The CPU native preparation halves retained pin anchor/angular-stop and groove
transverse/endpoint impulse guesses once per native interval for engine-owned
joint policies. The complete subsequent solve still uses the requested bias,
compliance, force/correction caps and iteration count. Motor and angular-spring
guesses keep their existing behavior; raw native joints without an engine policy
keep the original preparation. The independent resident GPU solver is unchanged.
This reduces feedback from approximate cached hard-row reactions in long chains;
it adds no authored damping, compliance, public setting or topology limit.

The original public 65-body pin chain retained momentum after a unit axial impulse
but its kinetic energy grew from a supplied 0.5 to a peak 318.221957 over the first
128 frames. A 129-body chain grew still further. These values are
kg*scene-unit²/s², not SI joules (divide by 10,000 for SI at the current scale).
The earlier momentum-only assertion could not detect this defect. Clearing old
pin impulse guesses after teleports removed the prior retained-partition failure,
but an ordinary impulse without teleporting reproduced the wider feedback.
An ignored 0.75 carry control still grew energy; 0.5 and zero carry controls
remained bounded over the diagnostic window. These controls are causal probes,
not general stability or performance proofs.

`PhysicsJointEnergyTests`, shared with the public-only consumer as
`--joint-energy`, tests pin and groove chains with 65/129/257 bodies and a
129-body rotated pin chain with alternating 1/8 kg masses. After 128 active warm
frames, a unit impulse on the first 1 kg body is followed for 512 complete steps.
Every step checks kinetic energy including authored angular inertia, complete
linear momentum and zero owner/all-thread managed allocation. The passive energy
bound is 0.5051 scene energy units: 1% above the supplied 0.5 plus 0.0001 for float
anchor noise. Momentum error is below 0.002 kg*scene-unit/s; final joined anchors
stay within 0.05 scene units. Bodies remain active and retain every constraint;
there is no sleeping or population reduction in this measurement.

The dormant-capacity fixture again preserves existing joints while changing
partitions and resetting body poses/velocities. Its former group-65 case retains
the original 128-frame active warmup, 16-frame natural-sleep window, pose/momentum
bounds and zero-byte assertions. Broader coupled/contact/CCD/network, native
allocation, foreign-platform and hardware performance gates remain separate.
All eight CPU/GPU owner/separate JIT/strict NativeAOT energy profiles pass with
strict 0/0 managed bytes across 28,672 measured response frames. Eight retained
partition/sleep/checkpoint profiles and ten adjacent root/family/checkpoint/coupled
profiles also pass (26 successful profiles). The diagnostic stage profile remains
separate from the independent public GPU world. CPU processes disable GPU discovery;
GPU mappings confirm NVIDIA without a software driver. PDB proofs match 2436
runtime, 930 root-test and 265 consumer authored sources, with matched AOT inputs.

With the identical final public consumer DLL/PDB, the former CPU runtime fails
the 65-body energy bound on frame 47 at 0.5290228178546386 scene energy units on
both owner/separate lanes, and still fails the retained group-65 sleep scenario.
Those three expected RED receipts are preserved. No public declaration state is
promoted by these numerical checks; the 64-byte full-suite allocation and massive
GPU Xid/device-loss gates remain open.
Receipts and frozen payloads are under `bin/physics-joint-recovery/2026-10-21/`.


## Common pin reaction point (2026-10-27)

For a pair of movable bodies, the resident GPU pin solver now applies its equal
and opposite linear impulses at the midpoint of their current anchors. The two
angular Jacobian arms reach that same world point from each body's center of
mass. Previously the arms reached the two separated anchors,
so their positional error could create an artificial net pair torque. The
authored body-local anchors and their positional correction remain unchanged.
If either endpoint has zero inverse mass, both authored torque arms retain
their preceding behavior. Such an endpoint supplies an external reaction rather
than conserving the movable pair's angular momentum. No extra row, pass,
readback, buffer or public setting is added; softness,
force/correction budgets, angular limits, motors and checkpoint layout retain
their existing contract. CPU solving is unchanged.

The unchanged 8x8 impact at 60 Hz reproduces the preceding boundary with the old
embedded shader. The baseline control and final owner-lane suite record these
maxima over the same 512 complete response steps:

| GPU shader | Requested iterations | Angular-momentum error | Maximum anchor gap, scene units |
| --- | ---: | ---: | ---: |
| Previous | 16 | 0.0381906629 | 0.0046849134 |
| Common point | 16 | 0.0012289248 | 0.0046823370 |
| Previous | 64 | 0.0081304703 | 0.0011312350 |
| Common point | 64 | 0.0007891841 | 0.0011312350 |

An initial candidate also moved the reaction point for a fixed endpoint. The
complete policy suite rejected it: the analytic default-bias correction differed
by 0.0021410598 at the unchanged 0.002 tolerance. The final implementation retains
the original fixed-endpoint arms; that failed candidate remains archived.

This removes a source of angular error while retaining the measured anchor
accuracy. Contacts, position recovery and finite precision still contribute
error; this is not an exact conservation promise for arbitrary graphs. The
permanent accuracy pair now requires the original 0.03 angular bound at **16**
iterations as well as the existing stricter convergence comparison at 64.
The old shader fails the new default-iteration assertion. No population, impulse,
energy budget, gap bound or allocation budget is reduced. The test-only
`ELECTRON2D_JOINT_ENERGY_IMPACT_ONLY=1` selector reproduces the accuracy pair;
an ordinary invocation retains all sixteen chain/lattice cases.

The final frozen CLR cohort passes all sixteen cases on CPU/GPU and owner/separate
numerical lanes: 32,768 measured response steps at strict 0/0 managed bytes.
The ten CPU/GPU policy, pin, groove, spring and server suites also pass. The final
strict NativeAOT consumer checks the two accuracy cases on all four lanes,
another 4,096 response steps at 0/0; this is a focused native qualification,
not the full sixteen-case native matrix. GPU process maps confirm NVIDIA without
software Vulkan, CPU profiles forbid GPU discovery, and NativeAOT maps have no
CoreCLR. PDB proofs match 2,684 runtime, 1,014 root-test and 293 public-consumer
source documents. Separate resource extraction matches the final shader in CLR
and NativeAOT input assemblies; the latter inputs remain unchanged during native
compilation. The receipts and aggregate `validation.json` are in the evidence
directory below. No new kernel Xid/device-loss entry was observed in the interval.

The first strict NativeAOT cohort's CPU owner profile aborted in the 257-body pin
chain with 184/184 owner/all-thread bytes after four zero-byte cases. Its CPU
solver was unchanged. This interval remains unaccepted and unattributed; the
separate-lane full suite and focused owner accuracy pair passed, but do not
explain or erase that failure. The frozen executable and receipt remain under
`native-final/` and `native-full-cpu/` in the evidence directory below.

Source snapshots, embedded SPIR-V hashes, the intentional regression failure and
immutable runtime cohorts are retained under ignored
`bin/physics-pin-momentum/2026-10-27/`. These are numerical functional controls,
not hardware throughput measurements. Physical declaration coverage remains
**1358/1369 (99.20%)**, with **11 open** and 92 Excluded. Scene/rendering,
separate-process network, native heap, foreign-platform and complete-goal
acceptance remain separate. The following October 22 evidence retains its
original source and numerical boundary.

## Closed pin lattices and impact accuracy (2026-10-22)

PhysicsJointEnergyTests, shared with the public --joint-energy consumer, now includes connected 4x4, 8x8 and 16x16 pin grids: 16/64/256 grid bodies and 24/112/480 horizontal/vertical joints. A rotated 8x8 profile alternates 1/8 kg masses. Every body remains awake, every edge remains present, gravity and authored damping are zero, and angular inertia is one scene inertia unit. A bare unit impulse supplies about 0.5 scene energy units. The contact variant adds a unit-mass free witness with an eight-unit impulse, about 32 scene energy units, real layer/mask participation and four reported contacts. It checks reported collider/shape identities and the complete contact impulse against the witness velocity change; the witness has no joint.

Each case warms 128 complete raw-space steps. Contact cases also warm an actual impact for 256 steps and restore a local checkpoint. Restoration is compared with the captured warmed pose, rotation and velocities, not ideal authored zeros: finite solver preparation can leave small values before capture. The measured bracket includes the impulse, 512 full 1/60 s steps, every body's state and every joint-anchor gap. It requires 0/0 owner/all-thread managed bytes, exact tick/backend/iteration selection, finite awake state, peak energy within 1% of the actual initial state plus impulse work and 0.0001 scene energy units, and complete linear-momentum error below 0.002 kg*scene-unit/s. Maximum anchor separation is below 0.05 scene units. A contact case must observe nonzero physical impulse; a missing joint implementation cannot pass the anchor bound.

### Angular convergence boundary

An initial diagnostic applied one 0.03 scene-angular-momentum bound to all profiles. The 60 Hz/16-iteration GPU 8x8 impact exceeded it at frame 47 with 0.0381907 while energy remained below the supplied 32 and linear momentum remained conserved. The 16x16 and mixed profiles also exceeded that provisional bound. These failures remain archived; no production solver was changed or certified by discarding them.

Full-trajectory controls over the same 8.5333 simulated seconds show the following maxima. Timings overlap compilation and other functional work and are not performance evidence.

| Backend | Fixed Hz / requested iterations | Angular-momentum error | Maximum anchor gap, scene units |
| --- | --- | ---: | ---: |
| CPU | 60 / 16 | 0.0185584 | 0.000487331 |
| CPU | 120 / 16 | 0.00951182 | 0.000243902 |
| CPU | 60 / 64 | 0.00957085 | 0.0000524608 |
| GPU | 60 / 16 | 0.0381928 | 0.00468491 |
| GPU | 120 / 16 | 0.0149422 | 0.00232034 |
| GPU | 240 / 16 | 0.00952733 | 0.00117074 |
| GPU | 60 / 64 | 0.00813132 | 0.00113124 |

This establishes convergence for this coupled impact, not an exact angular-conservation guarantee at every time step or topology. The permanent accuracy pair keeps the same 8x8 impact at 60 Hz and explicitly requests 16 then 64 iterations. Both angular and anchor errors must decrease by at least 25%, allowing only 0.0001 scene angular-momentum units and 0.00001 scene units of comparison noise; the 64-iteration angular error must also stay below the original 0.03 bound. That bound is less than 0.1% of the witness impulse times the four-unit cell pitch. It applies to this explicit accuracy profile, not a universal default-iteration promise. The same physical population, materials, contacts, impulse/energy checks and zero-byte budgets remain on both sides.

The seven previous chain profiles are retained. Nine new lattice profiles bring each executable invocation to sixteen 512-step response cases. All eight CLR/strict NativeAOT profiles pass on CPU/GPU and owner/separate numerical lanes: 65,536 measured response steps at strict 0/0 managed bytes, including 36,864 new lattice steps. CPU processes disable GPU discovery; GPU process maps confirm NVIDIA without a software driver, and NativeAOT profiles have no CoreCLR mapping. Stable payload hashes and PDB proofs bind 2,501 runtime and 268 consumer source documents; the rebuilt root fixture separately matches 943 documents. A stale root-test binary found during proof verification is excluded from acceptance; these profiles exercise the current public consumer.

Source/PDB proofs, original diagnostic failures, trajectory controls and the accepted cohort are retained under ignored bin/physics-joint-lattice/2026-10-22/, with suite-validation.json recording the final checks. These raw-space numerical checks do not establish scene/rendered output, arbitrary custom-extension coupling, separate-process network acceptance, native allocation totals, foreign platforms or hardware throughput. No API declaration is promoted; physics remains 1306/1369 (95.40%), with 63 open declarations.
