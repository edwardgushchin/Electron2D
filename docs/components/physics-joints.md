# Physics joints component

Last updated: 2026-10-15

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

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) now exercises all three roles, failed internal creation followed by retirement, body transfer that suspends/reconnects constraints, exact stored frame/policy metadata, clear/reuse and CPU/GPU world-anchor ownership. The selected implementation never retargets another world. Public registration and extension construction remain open under ADR 0103.

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
family changes the first physical response, preventing an inert row from passing.

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
