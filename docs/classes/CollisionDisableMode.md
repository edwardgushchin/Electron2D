# CollisionDisableMode

Last updated: 2026-09-26

- **Source:** [CollisionDisableMode.cs](../../src/Scene/2D/CollisionDisableMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum CollisionDisableMode`
- **Used by:** [CollisionObject.DisableMode](CollisionObject.md#disablemode)

## Description

Selects physics participation while the collision object's effective inherited [ProcessMode](ProcessMode.md) is `Disabled`. This value owns no resource. Scene pause alone leaves the policy inactive.

## Values

| Member | Integer | Contract |
| --- | ---: | --- |
| `Remove` | 0 | Default. Detach from the physics world; enabling attaches again. |
| `MakeStatic` | 1 | Temporarily make bodies static; Area sensors and fields stay active. |
| `KeepActive` | 2 | Continue ordinary physics while Node process callbacks stay disabled. |

## Example

```csharp
using var body = new RigidBody();
body.DisableMode = CollisionDisableMode.MakeStatic;
body.ProcessMode = ProcessMode.Disabled;
```

When this body enters a SceneTree, its fixtures have static contact response. Undefined values reject before changing the stored policy. [CollisionDisableModeTests](../../tests/Electron2D.Tests/CollisionDisableModeTests.cs) checks enum values/defaults, transitions, all current siblings, storage and executable physics. [ADR 0072](../decisions/physics.md#adr-0072) records the enum naming and lifecycle contract.
