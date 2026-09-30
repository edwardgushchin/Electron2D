# Scene physics joints component

Last updated: 2026-09-30

## Scope and owned types

[Joint](../classes/Joint.md) is the spatial base that stores two body paths, resolves them against one SceneTree world, and owns a backend constraint lifetime. [PinJoint](../classes/PinJoint.md) makes a revolute connection with collision suppression, angular limits and a finite-torque motor. [GrooveJoint](../classes/GrooveJoint.md) keeps a second-body anchor inside a finite guide on the first body while allowing free rotation. All inherit Entity's transform and Node's scene lifecycle; none replaces a PhysicsBody or owns shape resources.

## Runtime flow and invariants

The scene world registers each joint on tree entry. After all body siblings enter, the pin converts the joint origin to both bodies' local anchor frames; the groove also stores its local axis and samples body B's anchor at InitialOffset. The fixed step prepares bodies and joints after user physics callbacks and before four solver substeps. Path edits, body departure, or a changed collision policy remove and rebuild the internal joint as needed. Moving the joint node alone does not retune already attached body-local anchors. Body exit destroys the joint before its native body. Missing, duplicate or world-mismatched endpoints leave the node configured but inactive, with `GetConfigurationWarnings()` explaining the issue. A later body entry can connect on a subsequent fixed step.

One scene unit is 0.01 backend meters. Pin angle and motor speed use radians and radians per second. The motor has a caller-tunable torque cap in newton-meters; enabled angle limits must be ordered within ±0.99π. Groove Length and InitialOffset are signed scene-unit distances along its local Y axis, bounded to ten million scene units by the backend joint extent; the solver limits its anchor to the two endpoints while retaining free rotation. A live Length edit changes limits without reanchoring, while InitialOffset resamples the body-B anchor on the next step. Invalid scaled/skewed or unrepresentable geometry fails the frame and can be corrected. A connected collision-policy change refreshes both endpoints' fixtures so an existing overlap starts or stops producing contacts without moving the bodies.

## Current implementation and limits

The Joint base, PinJoint and GrooveJoint provide executable scene constraints. The public joint RID, positional bias and linear pin-anchor softness remain blocked by explicit server identity and solver mappings. DampedSpringJoint still requires a verified stiffness/damping conversion. Joint debug drawing awaits a scene debug-canvas flag and draw pass. See [Joint2D](../coverage/classes/Joint2D.md), [PinJoint2D](../coverage/classes/PinJoint2D.md), [DampedSpringJoint2D](../coverage/classes/DampedSpringJoint2D.md) and [GrooveJoint2D](../coverage/classes/GrooveJoint2D.md) coverage.

[PinJointTests](../../tests/Electron2D.Tests/PinJointTests.cs) and [GrooveJointTests](../../tests/Electron2D.Tests/GrooveJointTests.cs) check actual solver motion, live settings, lifecycle, packing, invalid-state recovery and warmed zero managed allocation on Linux/.NET 10. Both static-state allocation fixes are tracked in [Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102). Native allocation, other platforms and owner acceptance remain unverified. [ADR 0084](../decisions/physics.md#adr-0084) and [ADR 0085](../decisions/physics.md#adr-0085) define the two concrete slices.
