# PhysicsServer.ProcessInfo

Last updated: 2026-10-09

**Declaration:** `public enum PhysicsServer.ProcessInfo` ·
**Source:** [PhysicsServer.Statistics.cs](../../src/Servers/Physics/PhysicsServer.Statistics.cs).

| Value | Number | Meaning |
| --- | ---: | --- |
| ActiveObjects | 0 | Awake nonstatic bodies, including kinematics. |
| CollisionPairs | 1 | Backend collision candidates, including sensors. |
| IslandCount | 2 | Active dynamic constraint groups with contacts or joints. |

[GetProcessInfo](PhysicsServer.md#statistics) sums the latest completed steps of
active registered worlds. See [Physics statistics](../components/physics-statistics.md)
for activation, sleep, failed-step and CPU/GPU semantics.
