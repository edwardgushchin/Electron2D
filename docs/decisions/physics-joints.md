# Electron2D physics joint decisions

Last updated: 2026-10-10

This bounded document owns scene/server joint identity, geometry, constraint lifetime and force integration. [The decision index](index.md) routes other domains.

<a id="adr-0084"></a>
## ADR 0084: Scene-owned pin constraints

Last updated: 2026-10-10

- Status: Accepted
- Scope: Joint and PinJoint scene-node ownership, collision policy, angular limits and motor
- Depends on: [0008](scene.md#adr-0008), [0054](physics-backends.md#adr-0054), [0061](physics.md#adr-0061), [0063](physics.md#adr-0063)

### Decision

- Map Joint2D and PinJoint2D to `Joint : Entity` and `PinJoint : Joint`. NodeA/NodeB are stored string paths under the accepted Node path contract. After both distinct PhysicsBody endpoints enter one SceneTree world, a pin fixes their local anchors at the joint's global origin. The backend handle belongs to the scene world, is rebuilt on changed paths or tree reentry, and is destroyed before either body leaves. Moving the joint node alone does not retune existing anchors. Detached or unresolved paths leave the public configuration intact; a later body entry can connect on the next fixed step. Invalid scaled/skewed joint geometry fails that step and can be corrected.
- DisableCollision defaults true. Changing the policy preserves anchors and refreshes endpoint fixtures so an already-overlapping pair begins or ends contact on the next step. Pin angular limits and motor speed update an active revolute solver without recreating the joint. The angle starts at zero relative to the two body poses at attachment. Validate finite ordered limits inside the backend's ±0.99π range before enabling or changing an active limit. Motor torque is a separate stored `MotorMaxTorque` property, default 10 N·m, because this backend requires a finite torque cap for simultaneous stable limits and motor response; callers can tune it for inertia.
- `Joint.GetRID` exposes the stable scene-owned server identity under [ADR 0087](#adr-0087). Positional `Bias`, common caps and `PinJoint.Softness` now execute under ADR 0087. Softness changes linear anchor compliance, not angular spring coefficients. Authored joint diagnostics execute under ADR 0100; shared inherited API retains its owning coverage. The concrete finite groove and force-based damped spring roles are defined by ADRs 0085 and 0086 in this document.
- A warmed static/dynamic pin step reuses the solver's read-only identity state; the pinned backend patch is proposed upstream in [Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102). The checked Linux/.NET 10 path allocates zero managed bytes over 64 active frames; no native or cross-platform allocation claim is made.

### Consequences

PinJointTests covers a real pendulum, angular limit and motor response, collision changes on an already-overlapping pair, live edits, path and body exit/reentry, invalid-input recovery, PackedScene, owner-thread rejection and warmed allocation. Native allocator accounting, other platforms and owner acceptance remain unverified.

<a id="adr-0085"></a>
## ADR 0085: Finite groove constraint with free rotation

Last updated: 2026-10-10

- Status: Accepted
- Scope: GrooveJoint scene geometry and solver integration
- Depends on: [0084](#adr-0084), [0054](physics-backends.md#adr-0054), [0008](scene.md#adr-0008)

### Decision

- Map GrooveJoint2D to `GrooveJoint : Joint`. Its local-Y segment runs from zero to signed Length=50 by default; InitialOffset=25 samples body B's local anchor from the transformed point. The Box2D wheel constraint permits free relative rotation and translation along body A's local guide axis. Disable the wheel spring and rotational motor, enable lower/upper limits at the segment endpoints, and convert public scene units by 0.01. Zero length is a point limit; negative length reverses the interval; an offset outside it is pulled toward an endpoint.
- A live Length edit updates native limits without reanchoring either body. A live InitialOffset edit rebuilds the joint before the next fixed step. Bound both signed distances to the backend's 100,000-meter joint extent (ten million scene units); validate finite values, transformed endpoints and local anchors before native mutation. A rejected geometry step preserves the previous joint. Retain inherited path, collision and lifecycle behavior. Authored joint diagnostics execute under ADR 0100; inherited solver policies execute under ADR 0087; its two own public properties execute.
- Reuse the read-only identity body state in wheel warm-start and solve static branches to remove 576 managed bytes per four-substep active frame. This vendor hot-path change is included in [Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102).

### Consequences

GrooveJointTests covers sliding endpoints, free rotation, transformed/reversed/zero-length guides, out-of-range anchors, live edits, PackedScene, body exit/reentry, numeric/thread rollback and 64 warmed active frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified.

<a id="adr-0086"></a>
## ADR 0086: Anchor spring force and axial damping

Last updated: 2026-10-10

- Status: Accepted
- Scope: DampedSpringJoint geometry, coefficients and native-body force integration
- Depends on: [0084](#adr-0084), [0054](physics-backends.md#adr-0054), [0074](physics-forces.md#adr-0074), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014)

### Context

The pinned DampedSpringJoint2D [scene type](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/physics/joints/damped_spring_joint_2d.cpp) samples anchors at its origin and transformed local `(0, length)`. Its [solver](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_joints_2d.cpp) applies Hooke force proportional to relaxed-length error and exponential axial drag using the effective inverse mass at those anchors. The reference XML's maximum-length and damping-ratio prose do not describe that implementation. Box2D's frequency/damping-ratio distance joint normalizes stiffness by effective mass and cannot express a pure damper with zero stiffness through that parameter pair.

### Decision

- Map the type to `DampedSpringJoint : Joint`. Store signed Length=50, RestLength=0, Stiffness=20 and Damping=1. Sample both body-local anchors through the actual backend transforms; a Length edit resamples the second anchor before the next fixed step. Zero RestLength consistently uses `abs(Length)` on entry and live edits. Length is an anchor-placement distance, not a maximum stretch. Geometry uses the existing ten-million-scene-unit extent bound; relaxed length and coefficients are finite nonnegative values. Negative coefficients/relaxed length reject as an explicit typed physical-parameter adaptation.
- Use a native filter joint for scene lifetime, island linkage and the inherited collision policy. Box2D retains body integration and contact solving. At each native world interval, compute Hooke impulse `J_s=(L_rest-distance)*stiffness*dt` and axial effective inverse mass including each anchor's rotational lever arm. Drag coefficient has kg/s units and decay `exp(-damping*dt*inverse_mass)`. The total axial impulse is `J_s*decay-v_relative*(1-decay)/inverse_mass`; apply equal opposite impulses at both anchors through the existing native body API. This implements zero stiffness with nonzero damping and leaves tangential motion undamped. Near-coincident anchors follow the backend normalization epsilon and supply no force direction.
- On CPU, prepare every spring impulse and resulting native velocity for finite representability before applying any spring in that interval. An invalid CPU spring therefore cannot partially apply earlier spring impulses. Independent GPU worlds evaluate spring impulses per device substep; a begun execution failure follows the terminal world contract in ADR 0054, without publishing partial scene state or replaying on CPU. Kinematic path subdivisions evaluate the same rule with each interval's duration and current backend anchor geometry; they retain the existing one outer callback/event cycle. A nonzero impulse wakes eligible dynamic bodies, while static/frozen bodies remain immovable and relaxed sleeping bodies stay asleep. Custom body force integration does not omit these external joint impulses.
- Retain live RestLength/Stiffness/Damping edits, PackedScene, path/reentry and collision behavior. Authored joint diagnostics execute under ADR 0100; inherited policies execute where the spring force-law role applies under ADR 0087; all four own properties execute. No vendored source is changed by this slice. Explicit spring-force evaluation before each native interval is the backend integration profile; it is not a claim of bit-identical constraint/contact iteration results across different solvers.

### Consequences and verification

DampedSpringJointTests checks analytic Hooke/mass response, pure axial exponential damping, off-center torque, pair momentum/equilibrium, stretching beyond Length, rotated/reversed/coincident anchors, live configuration, frozen/custom integration, sleep/wake, collision policy, PackedScene, lifecycle, multi-spring numeric preflight and kinematic subdivision duration. Sixty-four warmed active spring frames allocate zero managed bytes on Linux/.NET 10. Native allocations, broad-scene stability/performance, other platforms and owner visual acceptance remain unverified.

<a id="adr-0087"></a>
## ADR 0087: Shared scene and server joint resources

Last updated: 2026-10-10

- Status: Accepted
- Scope: PhysicsServer joint resources, typed concrete settings, scene RID projection and dependent lifetime
- Depends on: [0084](#adr-0084), [0085](#adr-0085), [0086](#adr-0086), [0063](physics.md#adr-0063), [0001](product.md#adr-0001), [0014](resources.md#adr-0014)

### Decision

- One internal PhysicsJointRuntime owns the settings, body-local frames, configured role and current native handle for either a caller-owned server RID or a scene-owned Joint RID. JointCreate returns Empty; JointClear removes the connection while preserving identity and collision policy. JointMakePin/Groove/DampedSpring replace the concrete connection and reset its specific parameters. JointType retains Pin=0, Groove=1 and DampedSpring=2; the reference's MAX=3 return for an empty joint becomes the meaningful Empty=3 value. Scene Joint.GetRID is stable from construction until disposal; FreeRID rejects scene-owned identities. The owning node releases them.
- Global scene-unit anchors sample local frames once, using native transforms for attached bodies. Bound every local anchor radius to 100,000 backend meters (ten million scene units) before replacing the old connection; finite extreme anchors can otherwise overflow the solver lever-arm matrix. This inclusive numeric boundary applies to all three roles. Detached server endpoints can connect later when they share an active world. Pin permits an empty second RID and binds the first body to a hidden shape-free static world body; groove and spring require two distinct live body RIDs. Pending connections suspend on departure or temporary cross-world membership and reconnect with fresh native IDs while retaining local frames. Freeing an endpoint clears dependent joints before unregistering that RID. Freeing a world destroys native handles but retains caller-owned joint/body configuration. Membership changes scan registered connections only on the cold lifecycle path.
- Scene paths normally own geometry. A raw server make/clear on a scene RID persists until a scene path/geometry/name change or reentry reclaims it; the concrete node role and scene world cannot be replaced. Typed scalar settings and collision policy remain bidirectional. Pin's limits and motor default disabled/zero with torque cap 10 N·m. Raw spring construction samples anchor separation as relaxed length, stiffness 20 and damping 1.5 kg/s, matching the pinned backend factory. Scene spring defaults remain RestLength=0, stiffness 20 and damping 1: scene zero resolves to abs(Length), whereas a raw server relaxed-length zero is literal. Its getter reports the effective current relaxed length.
- Replace numeric parameter/flag selectors with concrete typed getter/setter pairs, as for existing body/Area profiles. Spring provides all three pairs; pin provides two flag pairs and lower/upper/target velocity pairs plus the accepted torque cap. Selector container/sentinel rows are excluded as representation only. Pin softness and all three general Bias/MaxBias/MaxForce typed pairs now execute through the common runtime and CPU/stage-GPU solvers. The numeric JointParam container/selectors are excluded as representation only. Joint.MaxBias/MaxForce are accepted serializable scene projections of the existing shared server capabilities; Joint.Bias and PinJoint.Softness retain their direct scene roles. Joint debug drawing remains dependent on the scene flag/draw pass.
- CPU, stage GPU and the independent internal GPU solver implement per-joint correction/impulse policies. Public scene/server properties and typed operations share PhysicsJointRuntime; the independent GPU public-world adapter now executes and is covered by the common scene/server joint suites. Bias is finite in [0, 1], with zero inheriting the world correction fraction. MaxBias is a nonnegative correction-speed cap, bounded by the world stability guard: vector scene-unit/s for linear anchors/grooves and rad/s for angular stops. Pin Softness is nonnegative inverse-mass compliance on the two linear anchor axes; it does not mean angular spring frequency. Each new space samples the typed Physics2DDefaultConstraintBias project setting (0.2) with feature overrides. SpaceGetConstraintDefaultBias/SpaceSetConstraintDefaultBias expose the captured/live space value and update zero-bias joints. Engine CPU/stage-GPU joints now use bias/h, replacing the earlier implicit frequency-derived profile; raw native joints without an engine policy retain that profile.
- MaxForce uses a finite nonnegative scalar, with float.MaxValue as the unlimited sentinel. Each scheduled substep permits scalar times duration in the linear impulse channel (kg·scene-unit/s) and separately in the pure-angular channel (kg·scene-unit²/s); the existing N·m motor torque cap is additional. Coupled linear rows share a vector cap. If position correction uses separate impulses, the sum of physical/correction magnitudes shares that cap. Additional CCD solves consume the original interval's unspent allowance; neither iterations nor impacts create extra force. Spring force plus axial damping shares one capped axial impulse; spring roles have no positional recovery rows, so Bias/MaxBias do not replace their force-law coefficients. These rules deliberately make the requested general force cap effective for pin translation and springs as well as grooves/angular rows: the pinned [joint source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_joints_2d.cpp) does not apply its generic cap consistently to every role. The public adaptation retains these units, defaults, lifetime and limits. Backend warm-start and final-solve clamping prevent cached impulses from bypassing a changed duration or cap; native copy/clear paths preserve/reset the authored policy. Springs cap their complete preflight impulse before collective velocity validation.
- Each active collision-disabled joint contributes independently to both endpoints' body-contact and motion-test suppression. Explicit body exceptions are separate. Snapshots deduplicate both sources; toggling, clearing or freeing one joint cannot remove another joint's or an explicit exception's contribution. Native contact flags and fixture refresh preserve existing anchors and apply to already-overlapping pairs.
- Every resource operation and dependent membership/disposal checks all related active world threads/phases before native mutation or beginning object disposal, including a detached endpoint of a pending connection. The scene world owns native solver handles; the registry retains weak scene owners. PhysicsSpace evaluates shared pin/groove kernels and spring force preflight. The native kernels carry correction/compliance/cap data under the recorded vendor patch; the stage GPU packet implements the same policies. The server guard includes the attachment space of a scene joint even without endpoints.

### Verification and limits

PhysicsServerJointTests checks all three native responses through one replaceable RID, single-body world pin, bidirectional scene settings, automatic/literal rest, clear/reclaim, detached/foreign/replacement-world membership, wrong/stale roles, callback failure, body/joint disposal rollback, related-world owner rejection, multiple-joint/explicit exception accounting and 64 warmed active typed-setting/spring frames with zero managed allocation on Linux/.NET 10. Existing scene pin/groove/spring checks still pass. Native allocations, broad-scene performance/stability, other platforms and owner visual acceptance remain unverified.

PhysicsJointPolicyTests verifies defaults, bidirectional scene/server values and packing, analytic positional/linear/angular behavior, captured/overridden world defaults, kinematic subdivision budgets, invalid rollback, wakes, reattachment and 128 warmed live-edit frames without owner-thread managed allocation on CPU and stage GPU. The shared policy and scene/server family suites now explicitly select public CPU and independent GPU worlds. Native allocations, foreign platforms, broad workloads and full physics/network acceptance remain open. See [joint conformance and numerical bounds](../components/physics-joint-policies.md#public-cpu-gpu-conformance).
