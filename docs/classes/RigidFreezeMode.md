# RigidFreezeMode

Last updated: 2026-10-18

## Role publication errors

A partial selected error during a frozen role change retains the requested FreezeMode. Numerical preparation or repeated assignment retries that role before use; editing the mode on an unfrozen body without a direct server profile causes no unnecessary motion readback. See [scene role publication](../components/physics-registered-adapter.md#failed-scene-role-publication).

- **Source:** [RigidBody.Freeze.cs](../../src/Scene/2D/RigidBody.Freeze.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum RigidFreezeMode`
- **Used by:** [RigidBody.FreezeMode](RigidBody.md#freezemode)

## Description

Chooses the body's role while Freeze is true. It owns no resource and has no effect on unfrozen dynamics.

| Value | Integer | Contract |
| --- | ---: | --- |
| `Static` | 0 | Default; manual transforms teleport without contact velocity. |
| `Kinematic` | 1 | Manual targets derive fixed-step velocity and participate in path contacts. |

```csharp
using var body = new RigidBody { FreezeMode = RigidFreezeMode.Kinematic, Freeze = true };
```

Gravity and force response stay disabled in both roles. Undefined values reject before mutation; PackedScene stores the selected value. [RigidFreezeModeTests](../../tests/Electron2D.Tests/RigidFreezeModeTests.cs) checks values, lifecycle, physical contacts and storage; [ADR 0075](../decisions/physics.md#adr-0075) defines the typed mapping and verification limits.
