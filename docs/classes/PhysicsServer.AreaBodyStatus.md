# PhysicsServer.AreaBodyStatus

Last updated: 2026-09-26

- **Declared in:** [PhysicsServer](PhysicsServer.md)
- **Source:** [PhysicsServer.Areas.cs](../../src/Servers/Physics/PhysicsServer.Areas.cs)
- **Declaration:** `public enum PhysicsServer.AreaBodyStatus`

## Description

Reports the logical pair transition delivered by an Area body/Area monitor callback. It owns no resource.

| Value | Integer | Contract |
| --- | ---: | --- |
| `Added` | 0 | A logical other/local shape pair entered the receiver. |
| `Removed` | 1 | A retained pair departed. |

Callbacks also carry other RID, stable scene InstanceID or zero, other shape index and local shape index. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) checks values/payloads; [ADR 0077](../decisions/physics-monitoring.md#adr-0077) owns current monitoring semantics.
