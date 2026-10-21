# Physics joints component

Last updated: 2026-10-21

## Lifecycle request ownership

After clear passes its access preflight, cleanup always commits Empty and forgets
endpoints, including selected detach or contribution-invalidation errors. The RID
and common settings survive. A server clear retains scene override ownership even
on failure, so endpoint reentry cannot restore the retired scene connection.
Validated server make requests commit role, sampled frames, endpoints and scene
ownership before selected factory/attachment/contribution effects. A failed
publication retains that request for explicit retry or endpoint reentry; explicit
scene authoring can still reclaim it. Rejected validation/borrowing preserves the
prior request. Failure detaching an old connection precedes the new request commit.
Selected providers release their resources before reporting Detach errors; the
engine retires that attachment and preserves the original cleanup errors.
See [complete joint lifecycle operations](../components/physics-registered-adapter.md#complete-joint-lifecycle-operations)
for executed scope and limits.

## Physical skeletal integration

PhysicalBone borrows its first authored direct Joint child and optionally configures physical parent/body endpoints and its origin. Skeleton-parent endpoints remain authored. Existing ADR 0084 anchors are sampled when a connection is built; no joints are generated.

## Scope and owned types

[Joint](../classes/Joint.md) is the spatial base that stores two body paths, resolves them against one SceneTree world, and owns a backend constraint lifetime. [PinJoint](../classes/PinJoint.md) makes a revolute connection with collision suppression, angular limits and a finite-torque motor. [GrooveJoint](../classes/GrooveJoint.md) keeps a second-body anchor inside a finite guide on the first body while allowing free rotation. [DampedSpringJoint](../classes/DampedSpringJoint.md) supplies an elastic force and axial damping between sampled anchors. All inherit Entity's transform and Node's scene lifecycle; none replaces a PhysicsBody or owns shape resources.

## Runtime flow and invariants

The selected PhysicsWorldBackend now creates a fresh PhysicsJointImplementation for each constraint attachment. PhysicsJointBackend retains only that current implementation; RID, authored values, local frames, membership and pair-exception accounting stay in PhysicsJointRuntime. CPUPhysicsJointImplementation owns native endpoint/joint IDs, exact compiled local frames and transient spring evaluation. GPUPhysicsJointImplementation owns its resident handle and retained definition, using the device solver for spring/constraint execution. Generic live setters, solver policy, portable frames and release all dispatch through the selected attachment. Current-body local-frame sampling also dispatches through the selected collider implementation; detached sampling preserves the existing CPU numeric convention. CPUPhysicsWorldBackend owns its lazy shape-free world anchor and prepares solver/contact-motion capacity when its body slot is added; GPU pins retain their virtual world endpoint with no CPU body. [Capacity boundaries](physics-retained-memory.md#world-anchor-capacity) cover first attachment after a full collider capacity.

PhysicsJointRuntime retains shared RID identity, scene-unit local frames and guide
bounds, scalar settings, world membership and pair-exception accounting.
[PhysicsJointBackend](../classes/PhysicsJointBackend.md) routes creation, live updates and release to the selected concrete attachment. Attach and policy updates receive immutable [PhysicsJointConfiguration](../classes/PhysicsJointConfiguration.md) values. CPU native spring scheduling resolves attachments through the CPU world ownership table, including selected wrappers; device spring execution stays resident. Scene joint classes and the
runtime no longer depend on vendor types. Stored frame bases preserve the sampled
rotation through detach/reentry; a membership change does not resample anchors.

The scene world registers each joint on tree entry. After all body siblings enter, the pin converts the joint origin to both bodies' local anchor frames; the groove also stores its local axis and samples body B's anchor at InitialOffset. The fixed step prepares bodies and joints after user physics callbacks and before four solver substeps. Path edits rebuild scene geometry; body departure releases the native handle, and collision-policy edits preserve anchors. Moving the joint node alone does not retune already attached body-local anchors. Body exit destroys the joint before its native body. Missing, duplicate or world-mismatched endpoints leave the node configured but inactive, with `GetConfigurationWarnings()` explaining the issue. A later body entry can connect on a subsequent fixed step.

The CPU adapter maps one scene unit to 0.01 backend meters; the independent GPU solver uses scene units directly. Pin angle and motor speed use radians and radians per second. The motor has a caller-tunable torque cap in newton-meters; enabled angle limits must be ordered within ±0.99π. Groove Length and InitialOffset are signed scene-unit distances along its local Y axis, bounded to ten million scene units by the backend joint extent; the solver limits its anchor to the two endpoints while retaining free rotation. A live Length edit changes limits without reanchoring, while InitialOffset resamples the body-B anchor on the next step. Invalid scaled/skewed or unrepresentable geometry fails the frame and can be corrected. A connected collision-policy change refreshes both endpoints' fixtures so an existing overlap starts or stops producing contacts without moving the bodies.

## Current implementation and limits

The spring adapter samples local anchors through backend transforms and uses a native filter joint for island/collision ownership. Before each native interval it evaluates Hooke impulse and effective-mass axial exponential drag, then applies equal opposite impulses at the anchor points. CPU spring responses preflight before any are applied in that interval. The independent GPU solver evaluates springs on device substeps and retains the defined failed-world state after a begun execution error. Pure damping with zero stiffness is supported; Length does not cap stretch and zero RestLength uses its magnitude. Kinematic subdivision durations prevent multiplying the spring force by the number of native calls. Static/frozen bodies remain immovable, relaxed sleepers stay asleep, and nonzero impulses wake dynamic endpoints. The initial spring slice used existing native body APIs; the current [solver-policy integration](physics-joint-policies.md) also updates native pin/groove kernels and caps the combined spring impulse.

The Joint base, PinJoint, GrooveJoint and DampedSpringJoint provide executable scene connections. Stable joint RIDs and raw server creation now execute through the shared runtime. Positional bias, maximum correction speed/force and linear pin-anchor softness now execute through the [shared public policy](physics-joint-policies.md). Authored joint debug drawing executes through the retained canvas under ADR 0100. See [Joint2D](../coverage/classes/Joint2D.md), [PinJoint2D](../coverage/classes/PinJoint2D.md), [DampedSpringJoint2D](../coverage/classes/DampedSpringJoint2D.md) and [GrooveJoint2D](../coverage/classes/GrooveJoint2D.md) coverage.

[PinJointTests](../../tests/Electron2D.Tests/PinJointTests.cs), [GrooveJointTests](../../tests/Electron2D.Tests/GrooveJointTests.cs) and [DampedSpringJointTests](../../tests/Electron2D.Tests/DampedSpringJointTests.cs) check actual solver motion, live settings, lifecycle, packing, invalid-state recovery and warmed zero managed allocation on Linux/.NET 10. Both static-state allocation fixes are tracked in [Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102). Native allocation, other platforms and owner acceptance remain unverified. [ADR 0084](../decisions/physics-joints.md#adr-0084) and [ADR 0085](../decisions/physics-joints.md#adr-0085) define the constraint slices; [ADR 0086](../decisions/physics-joints.md#adr-0086) defines spring forces.

## Shared server integration

[PhysicsServer](../classes/PhysicsServer.md#joints) creates caller-owned empty/replaceable identities and exposes typed scalar settings. [PhysicsJointRuntime](../classes/PhysicsJointRuntime.md) shares the actual scene pin/groove/spring kernels and world list; [PhysicsServer.JointType](../classes/PhysicsServer.JointType.md) reports the configured role even while pending. Scene GetRID is stable and borrowed. Raw server configuration accepts scene/server body RIDs; a one-body pin uses a hidden shape-free world anchor. Two-body connections suspend on detach/foreign membership and reconnect with preserved local frames and fresh native IDs; endpoint free clears the role, and world free retains caller configuration. Scene raw geometry overrides persist until scene edits/reentry reclaim them.

Active disabled-joint pair contributions are independent of each other and explicit exceptions, affect contacts and motion tests, and deduplicate in snapshots. Dependent body membership/disposal validates related world ownership/phases before mutation. [PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) checks native server response, scene sharing, lifecycle, failure recovery, pair accounting and 64 warmed active typed-setting/spring frames without managed allocation under [ADR 0087](../decisions/physics-joints.md#adr-0087). The initial RID integration made no vendor changes; the current policy slice extends native joint kernels. native allocations, other platforms and owner acceptance remain unverified.

The backend extraction passes the 31-suite collider CPU group and full current GPU
stage-host suite. PhysicsServerJointTests runs the same rotated/off-center pin and
world-replacement assertions on both paths, including motor/limits and rejection
of invalid replacement. Each world runs 120 steps at 1/120 s; one scene unit of
anchor error and 0.05 rad beyond the configured angle limit allow the existing
solver tolerances. Independent GPU joint ownership, spring evaluation, public-world integration and portable replay now execute; full physics/network acceptance remains open.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) now exercises all three roles, failed internal creation followed by retirement, body transfer that suspends/reconnects constraints, exact stored frame/policy metadata, clear/reuse and CPU/GPU world-anchor ownership. The selected implementation never retargets another world. Public registration and extension construction execute under ADR 0103 through the [registered adapter](physics-registered-adapter.md); wider numerical acceptance remains open.

## Independent GPU foundation

[GPUPhysicsBodyStore](gpu-resident-joints.md) now retains device joint configuration
and warm history, solves pins/grooves together with contacts and applies Hooke/axial
spring impulses on GPU. Joint collision vetoes are device-resident. Public scene/server GPU worlds now create the resident attachment through the selected owner. Public bias/compliance/caps, sleep/wake, motion vetoes, failure release and portable/network replay execute through both selected built-ins; complete platform and performance acceptance remain open.

The [shared public CPU/GPU family run](physics-joint-policies.md#public-cpu-gpu-conformance)
now covers all five joint suites, including motion vetoes from multiple joints,
finite caps with extreme stiffness, failure/disposal and numerical error bounds.


CPU sleep/wake may recolor a joint after moving its contacts. An empty prepared
joint-color buffer is now transferred to a previously unused color before growing
storage. Populated colors retain their buffers. The common public server-joint suite
checks a pin/contact component's first recoloring, zero owner/all-thread managed
allocation and its subsequent anchor response on CPU and GPU. See the
[network regression record](physics-network-example.md#joint-wake-allocation-regression-2026-10-10).


The shared runtime rejects scene/raw parameter and local path/geometry writes inside initial/live numerical hooks before mutation. Typed authoring survives endpoint reentry and ordinary callbacks retain writes. [Registered joint parameters](physics-registered-adapter.md#joint-parameter-authoring) records complete profile propagation, bounded spring response and allocation checks.


Clear and Configure share the numerical authoring guard. Public make preflights both incoming body owners before preparing either, including an Empty resource and a detached first endpoint. [Registered connection lifetime](physics-registered-adapter.md#joint-connection-lifetime) checks pending/clear/replacement/reentry, errors, callbacks and warmed active frames on CPU/GPU/registered worlds.


Dependent retirement now continues after a joint detach error and unregisters its old world membership. Body free clears every dependent role before dropping the body identity; caller-owned joint free retires its RID after validated cleanup begins. Authoring clear/make retains its separate retry policy.


## Closed coupled graph (2026-10-14)

The public-only --coupled-joints fixture runs four dynamic bodies (two scene, two
raw) with two pins, one finite groove and one off-center damped spring in a closed
loop. Both scene-owned and raw joint resources execute the same mechanism on
explicit CPU/GPU worlds without startup fallback. Gravity and external damping
are zero; collision layers are zero to isolate joint response. An off-center
20-unit impulse starts real translation and rotation. Disabling each constraint
family changes the 32-frame physical response, preventing an inert row from passing.

Every complete step checks finite derived state, both pin anchor gaps and groove
line/range, total linear/angular momentum, center travel and bounded kinetic plus
spring energy. Limits are .5 scene units for anchor/guide error (1.25 percent of the
40-unit mechanism), .03 impulse units against the initial 20-unit momentum,
2 angular units against the initial 360, and .05 units for center drift. The energy
ceiling 600 exceeds twice the initial kinetic/elastic energy, allowing finite-step
constraint correction while rejecting unbounded growth; it is a stability bound,
not exact energy conservation with damping. Local 32-step checkpoint replay uses
.005 units/velocity and .002 radians/angular velocity. The same bounds apply to
both implementations; post-correction cross-backend trajectories need not match.

An independent CPU world supplies eight portable body/joint IDs and encoded
snapshot packets to another CPU or GPU world. Deliberate pose/velocity divergence
is corrected silently with stable distinct local RIDs, all four pose/rotation/linear/angular values within .002, exact sleep/role flags
and the authoritative tick. Incompatible coupled policy rejects before any
encoded world or event mutation. Each of 257 corrections is followed
by actual constrained motion. Separate 128-warm/128-measured gates require strict
0/0 owner/all-thread managed bytes for full local frames and complete capture,
encoding/decoding, application and both worlds' numerical frames. This is local
portable transport; real socket/network acceptance retains its separate example.

Fresh serial/separate CPU/GPU processes observe no hardware driver; GPU executes
CPU-only Vulkan. Separate profiles observe two live numerical lanes when both
worlds run; GPU profiles observe those lanes with the software driver present.
The bounded ring establishes this coupled path. Contact-rich graphs, larger or
extreme-scale workloads, full inherited/extension contracts, native/foreign
execution, routed networking and hardware/window performance remain open.


## Joint and contact support (2026-10-14)

The public-only --joint-contacts fixture combines a pin-connected pair of unit-mass
linear rigid bodies, a finite vertical groove, a compressed spring (rest 50,
stiffness 20, damping 4) and a real rectangle floor. Gravity is 980. The floor has
friction .6 and virtual horizontal surface velocity 30 while its pose stays fixed.
Both scene/raw contact receivers and scene/raw joint ownership execute on explicit
CPU/GPU worlds. Negative controls remove floor collision or elastic stiffness;
the unchanged support/impulse assertions reject both substitutions.

After 300 settling frames the upper/lower centers remain near y 23/27, guide x stays
near zero, and the pin separation remains 4. Tolerances are .15 units for the
column position, .02 for separation and .1 units/s for resting speed. Contact
snapshots verify the actual scene floor RID/object/instance identity, both logical
shape slots, upward normal and surface point velocity 30 within .001. The scene
receiver starts one monitored floor episode;32-step checkpoint replay keeps that
episode and solved support, with .005 pose/velocity repeat tolerances. Clearing all
three joints releases the body to travel more than 20 units in 60 frames while floor
contact remains real.

Complete contact impulse is compared with both weights plus spring load and the
measured change in total vertical momentum. The undamped target is
`(1960 + 20*(50-y_load))/60 - delta_momentum`. The accepted finite-interval spring
law in ADR 0086 multiplies elastic impulse by exponential decay and damps axial
speed; a continuous Hooke equilibrium is therefore not an exact frame oracle.
The common permitted deficit is bounded by
`(elastic/60 + abs(load_speed_before) + 980/60)*(1-exp(-4/60))`, plus .15 impulse
units for position/rounding. This preserves nonzero damping on both backends while
allowing their different native/device interval subdivisions. A missing spring,
missing weight or final-substep-only impulse fails that bound. Tangential impulse
must be positive and its ratio to normal impulse is .6 within .08.

Each of four role/ownership variants runs 128 warm and 128 measured complete frames
with full contact/public-state observation at strict 0/0 owner/all-thread managed
bytes. CPU needs no display/GPU; GPU uses CPU-only Vulkan. Serial/separate fresh
processes exercise the same contract and observe the numerical lane only when
requested. This bounded contact-rich path does not certify larger/extreme graphs,
complete extension/inherited behavior, native/foreign execution, real networking
or hardware/window performance.

## Coupled support, continuous motion and sleep

Steady spring tension can balance contact and joint reactions without preventing automatic sleep. CPU spring preparation skips an island whose endpoints are asleep; the resident GPU graph uses actual motion, quiet clocks and authored changes rather than treating every relaxed-length error as an unconditional wake request. Rest-length edits and external impulses still wake connected bodies.

The public joint/contact consumer now runs CPU, GPU and an independent registered implementation with scene/server bodies and scene/server joints. It checks three simultaneous joint families against a frictional floor, analytic support and complete contact identity, negative controls, silent private replay and release of constraint rows. A separate fixture checks stable automatic sleep, authored spring wake, CastRay/CastShape high-speed impacts after lifting both bodies ten units, and sleeping-history replay. Every measured bracket retains full frames and strict zero owner/all-thread managed bytes; native allocator totals and target-device performance remain separate.

The independent consumer solves joint/contact constraints before advancing poses and bounds coupled motion intervals to a quarter tick while retaining full-step force/impulse budgets. Its closed four-body pin/groove/spring loop retains the existing anchor, momentum and energy tolerances, private replay and encoded correction from a distinct CPU authority. This in-process transport is separate from multi-process network acceptance. Evidence: bin/physics-coupled-extension/2026-10-15/validation.json.

Changed spring rest length, stiffness and damping now dispatch the live solver policy immediately, while equal-value writes preserve quiet state. CPU pose edits wake linked native constraints, including shapeless static anchors; internal pose restoration keeps its silent path. The shared profile checks both coefficient and static-anchor wake, return to sleep, unchanged-value writes and private impact replay.


The public [network example](physics-network-example.md#coupled-support-across-network-correction-2026-10-15)
now also exercises a spring-loaded pin/groove column with real floor support across
authoritative correction and replay in separate processes. It checks contact
identity, awake impulse balance, natural sleep and impulse wake independently of
final authoritative convergence. This is one bounded loaded graph; arbitrary/dense
coupling and complete hardware/platform/performance acceptance remain open.

Scene departure follows the [shared node transition order](scene-hierarchy.md#entry-readiness-and-departure). Physical bindings remain live through OnExitTree/TreeExiting and retire in engine membership cleanup before NodeRemoved/TreeExited, including callback failures. The typed hook is inherited from Node; backend retirement cannot depend on a user calling its base implementation.


## Distributed closed joint network

`--coupled-joints --large-graph` extends the existing public-only consumer to one
closed ring of 64 dynamic bodies and 64 joints: 22 pins, 21 finite grooves and
21 off-center damped springs. Neighbor centers are 20 scene units apart, the
circle radius is `10/sin(pi/64)`, and each edge connects the next body modulo 64.
This is one connected mechanism. Half the bodies are scene-owned and half raw;
both scene-owned and raw joint variants execute. Explicit CPU, independent
resident GPU and independently registered implementations retain their selected
identity without fallback, in owner and separate numerical thread modes.

Each body has mass 2 and inertia 5. An initial impulse
`(3*cos(5*theta), 2*sin(7*theta))`, with another `(20,8)` on body zero, acts at
local offset `(2,3)`. Thus every part of the ring starts with numerical load.
Gravity, external damping, sleep and collision masks are disabled to isolate the
constraint network. Pins share edge midpoints; grooves span 8 units across the
edge midpoint; spring anchors are one quarter edge length from their centers,
with sampled relaxed length 10, stiffness 30 and axial damping 5. Disabling all
joints of each family in a separate 32-frame control must change public velocity
by more than `1e-5` summed squared scene velocity units.

At 1/60 s each full step checks all anchor/finite-guide errors below .5 scene
units, total linear momentum error below .03 impulse units, angular momentum
error below 2 angular units and mass-center drift below .05 units. Initial
momentum and angular momentum are derived from every authored impulse and lever,
including spin. Kinetic energy plus every spring's potential energy must stay
below 1.1 times the initial kinetic energy (about 833.2). The 20-unit spacing gives
a 2.5-percent anchor envelope. A spring's effective inverse axial mass is at most
`1 + 2*5*5/5 = 11`; four substeps give
`h*sqrt(30*11) < .076`. The 10-percent energy allowance permits this finite-step
oscillation and correction while rejecting unstable growth; damped trajectories
need not conserve energy or match another solver bit for bit.

The original four-body fixture remains a control. Both sizes exercise 32-frame
local checkpoint replay with .005 scene position/velocity and .002 angular
bounds, and 257 CPU-authoritative encoded snapshot corrections. The large map
binds all 128 body/joint identities. Every correction preserves distinct local
RIDs, exact sleep/mode/tick values, silent observer history and pose/motion within
.002; incompatible joint policy rejects before any encoded state or event change.
Each size and ownership variant measures 128 moving full frames and 128 complete
capture/write/read/apply/two-world-step cycles after 128 warmups, with strict zero
owner/all-thread managed allocation. Rendering and real socket transport have
separate acceptance gates.


The 2026-10-20 source-built Linux/.NET 10 run passes 24 fresh-process profiles:
four/64 bodies, CPU/registered/GPU, owner/separate numerical mode and CLR/strict
NativeAOT. Each profile runs both joint ownership variants. All 48 moving-frame
and 48 complete-transport allocation brackets report 0/0 owner/all-thread bytes:
6,144 measured local frames and 6,144 measured two-world correction cycles,
18,432 measured physical frames in total. There are 72 discriminating disabled
family controls. Maximum large-ring errors across both executable variants are:

| Backend | Pin gap | Finite groove error | Linear momentum error | Angular momentum error | Peak kinetic plus elastic energy |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU | .005012 | .002804 | .000317 | .019165 | 396.356232 |
| Registered | .010444 | .053564 | .000313 | .029907 | 395.675018 |
| GPU | .007190 | .036189 | .000317 | .125122 | 394.505493 |

GPU processes load the NVIDIA hardware driver, observe the selected RTX 3090 Ti,
and load no software Vulkan driver; CPU/provider processes load no NVIDIA driver
with Vulkan unavailable. No new Xid is observed over these profiles. Portable
PDB source sets/checksums match all 2,422 runtime and 261 consumer source documents
for each build variant. Strict NativeAOT publishes without trim/AOT warnings.
Evidence, per-process payload hashes, device observations and source proofs are
under `bin/physics-joint-network/2026-10-20/`; the earlier single-impact preliminary
cohort is superseded by the distributed-load final cohorts. Functional profiles
overlap build/verification work, so their elapsed times are not performance data.

The API census stays 1304/1369 Implemented (95.25%), with 65 open declarations and
92 Excluded. Runtime/API behavior is unchanged. The bounded isolated network does
not establish contact-rich/CCD networks, arbitrary scales, routed multiplayer,
native allocation totals, other platforms, window FPS or final same-revision
mass-performance acceptance; the complete extension class remains Partial.

## Mixed compound support

The public-only `--joint-contacts` entry now retains its circle/rectangle control
and adds mixed capsule/convex compound bodies on a two-segment concave floor.
Each dynamic body has a disabled leading slot, two active slots, and an offset
second shape. Scene and raw bodies reverse capsule/polygon order. Both active
local slots must publish floor contacts, preserving floor slot 1 and ignoring
disabled slot 0. Point queries at the offset shape center require its actual
body RID and logical slot 2. Sixteen report slots prevent truncating the combined
manifolds; the full summed impulse remains subject to the existing force balance.

Every scene/raw receiver and scene/raw joint-owner combination runs the original
pin/groove/spring load, stationary floor with virtual surface velocity, normal and
tangential impulse bounds, monitored checkpoint continuation and motion after
clearing all three joints. The same mixed bodies also run automatic coupled sleep,
equal/changed spring policy, moved-anchor wake, and both CastRay/CastShape impacts
after private restoration of the sleeping island. No numerical tolerance, mass,
gravity, damping, warmup or zero-byte assertion is reduced.

The first mixed CPU/GPU attempts rejected an overly strict new normal assertion.
CPU diagnostics show a unit horizontal normal at a compound polygon corner with
exactly zero impulse. Such additional segment-endpoint contacts carry no support;
they remain in the published snapshot. Every contact must have a finite unit
normal, while every nonzero support impulse retains the upward-normal assertion.
The original circle control still requires every normal to point upward. No
runtime or report filtering changes are made for this fixture correction.

Each complete profile has eight 128-frame support brackets and sixteen
128-frame restored CCD brackets: 3,072 measured complete frames, with matching
warmup and strict 0/0 owner/all-thread managed bytes. Missing-floor and
missing-elastic controls run for both geometry families. These bounded composed
checks do not establish arbitrary geometry/coupling, real network transport,
native allocator totals, foreign platforms, window FPS or performance advantage.
The earlier unattributed full-suite 64-byte failure remains open separately.

### Active joint-batch convergence

The composed GPU case exposed a real convergence defect after the normal-assertion
correction. At frame 403, previously quiet load/peer bodies acquired horizontal
velocities .466555/1.4772923 while the physical floor and all constraints remained
present. An isolated copy reproduces the failure at the default 16 solver sweeps;
64 sweeps are a successful diagnostic control, not the production correction.

Contact colors apply their rows sequentially, then joint rows update together.
The latter still divided its correction by a degree containing those already
completed contact rows. Changing contact features therefore under-relaxed joints
according to unrelated row population. The update now counts only the joint
prefix of each retained incidence list for a joint-only batch. Traversal validates
indices/membership and is bounded by the existing list count. Full Jacobi batches
retain their full degree. No extra sweeps, buffers, host state mirror or readback
are added. The isolated case now passes at the unchanged 16 sweeps.

The final source-built cohort passes all twelve JIT/strict NativeAOT profiles:
CPU with unavailable GPU, NVIDIA resident GPU, and the independent registered
implementation, each on owner/separate numerical hosts. They retain 36,864
measured complete frames and 48 negative controls in total, with strict 0/0
owner/all-thread managed bytes. The paired public five-suite CPU/GPU joint
contract and resident joint policy/store suite also pass, including 4,096-pin
limited/unlimited checks. PDB proofs match 2,432 runtime, 924 root-test and 262
public-consumer authored sources; AOT has its separately matched build cohort.

Receipts, unchanged payload hashes, NVIDIA/no-software mappings, telemetry,
source proofs, failed candidates and the default-16/diagnostic-64 controls remain
under `bin/physics-mixed-support/2026-10-21/`. Its `validate.py` checks all fourteen
final receipts and the retained failing/converged controls. Builds and functional
profiles overlap, so elapsed times are not performance comparisons. Per-row
incidence counting and dense-joint latency remain unmeasured costs; native/foreign,
network, full-suite allocation and whole-goal acceptance remain open.
