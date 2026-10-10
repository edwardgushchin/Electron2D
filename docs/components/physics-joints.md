# Physics joints component

Last updated: 2026-10-10

## Physical skeletal integration

PhysicalBone borrows its first authored direct Joint child and optionally configures physical parent/body endpoints and its origin. Skeleton-parent endpoints remain authored. Existing ADR 0084 anchors are sampled when a connection is built; no joints are generated.

## Scope and owned types

[Joint](../classes/Joint.md) is the spatial base that stores two body paths, resolves them against one SceneTree world, and owns a backend constraint lifetime. [PinJoint](../classes/PinJoint.md) makes a revolute connection with collision suppression, angular limits and a finite-torque motor. [GrooveJoint](../classes/GrooveJoint.md) keeps a second-body anchor inside a finite guide on the first body while allowing free rotation. [DampedSpringJoint](../classes/DampedSpringJoint.md) supplies an elastic force and axial damping between sampled anchors. All inherit Entity's transform and Node's scene lifecycle; none replaces a PhysicsBody or owns shape resources.

## Runtime flow and invariants

The selected PhysicsWorldBackend now creates a fresh PhysicsJointImplementation for each constraint attachment. PhysicsJointBackend retains only that current implementation; RID, authored values, local frames, membership and pair-exception accounting stay in PhysicsJointRuntime. CPUPhysicsJointImplementation owns native endpoint/joint IDs, exact compiled local frames and transient spring evaluation. GPUPhysicsJointImplementation owns its resident handle and retained definition, using the device solver for spring/constraint execution. Generic live setters, solver policy, portable frames and release all dispatch through the selected attachment. Current-body local-frame sampling also dispatches through the selected collider implementation; detached sampling preserves the existing CPU numeric convention. CPUPhysicsWorldBackend owns its lazy shape-free world anchor; GPU pins retain their virtual world endpoint with no CPU body.

PhysicsJointRuntime retains shared RID identity, scene-unit local frames and guide
bounds, scalar settings, world membership and pair-exception accounting.
[PhysicsJointBackend](../classes/PhysicsJointBackend.md) routes creation, live updates, release and spring evaluation to the selected concrete attachment. Scene joint classes and the
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
