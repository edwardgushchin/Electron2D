# Scene physics joints component

Last updated: 2026-09-30

## Scope and owned types

[Joint](../classes/Joint.md) is the spatial base that stores two body paths, resolves them against one SceneTree world, and owns a backend constraint lifetime. [PinJoint](../classes/PinJoint.md) makes a real revolute connection with collision suppression, angular limits and a finite-torque motor. Both inherit Entity's transform and Node's scene lifecycle; neither replaces a PhysicsBody or owns shape resources.

## Runtime flow and invariants

The scene world registers each joint on tree entry. After all body siblings enter, the pin converts the joint origin to both bodies' local anchor frames. The fixed step prepares bodies and joints after user physics callbacks and before four solver substeps. Path edits, body departure, or a changed collision policy remove and rebuild the internal joint as needed. Moving the joint node alone does not retune the already attached body-local anchors. Body exit destroys the joint before its native body. Missing, duplicate or world-mismatched endpoints leave the node configured but inactive, with `GetConfigurationWarnings()` explaining the issue. A later body entry can connect on a subsequent fixed step.

One scene unit is 0.01 backend meters. Pin angle and motor speed use radians and radians per second. The motor has a caller-tunable torque cap in newton-meters; enabled angle limits must be ordered within ±0.99π. Invalid scaled/skewed geometry fails the frame and can be corrected. A connected collision-policy change refreshes both endpoints' fixtures so an existing overlap starts or stops producing contacts without moving the bodies.

## Current implementation and limits

The Joint base and PinJoint provide executable scene constraints. The public joint RID, positional bias and linear pin-anchor softness remain blocked by explicit server identity and solver mappings. DampedSpringJoint requires a verified stiffness/damping conversion; GrooveJoint requires a solver path that permits sliding along a finite segment while both bodies can rotate. See [Joint2D](../coverage/classes/Joint2D.md), [PinJoint2D](../coverage/classes/PinJoint2D.md), [DampedSpringJoint2D](../coverage/classes/DampedSpringJoint2D.md) and [GrooveJoint2D](../coverage/classes/GrooveJoint2D.md) coverage.

[PinJointTests](../../tests/Electron2D.Tests/PinJointTests.cs) checks actual solver motion, live collision filtering, lifecycle, packing, invalid-state recovery and warmed zero managed allocation on Linux/.NET 10. The revolute static-state allocation fix is tracked in [Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102). Native allocation, other platforms and owner acceptance remain unverified. [ADR 0084](../decisions/physics.md#adr-0084) defines this slice.
