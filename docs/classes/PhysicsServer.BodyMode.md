# PhysicsServer.BodyMode

Last updated: 2026-09-26

**Owner:** [PhysicsServer](PhysicsServer.md) · **Source:** [PhysicsServer.cs](../../src/Servers/Physics/PhysicsServer.cs)

Selects a server-created body's backend motion policy.

| Value | Integer | Behavior |
| --- | ---: | --- |
| `Static` | 0 | Stationary collision body. |
| `Kinematic` | 1 | Manually moved body. |
| `Rigid` | 2 | Dynamic body, default from `BodyCreate`. |
| `RigidLinear` | 3 | Dynamic body with rotation locked. |

`BodySetMode` rejects undefined values before changing state. A dynamic-to-static change retains the solved pose; detaching a dynamic body also captures pose and velocity for later space assignment. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies default/static/dynamic transitions and explicit stepping. See [ADR 0063](../decisions/physics.md#adr-0063).
