# RigidCenterOfMassMode

Last updated: 2026-09-26

- **Source:** [RigidBody.Mass.cs](../../src/Scene/2D/RigidBody.Mass.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum RigidCenterOfMassMode`
- **Used by:** [RigidBody.CenterOfMassMode](RigidBody.md#centerofmassmode)

## Description

Chooses the center used by a rigid body's mass profile. It owns no resources.

| Value | Integer | Contract |
| --- | ---: | --- |
| `Auto` | 0 | Default; compute center from active geometry. The stored CenterOfMass remains zero. |
| `Custom` | 1 | Use the stored local CenterOfMass relative to body origin. |

```csharp
using var body = new RigidBody();
body.CenterOfMassMode = RigidCenterOfMassMode.Custom;
body.CenterOfMass = new Vector2(5, 0);
```

Returning to Auto clears the stored vector and preserves an explicit Inertia override. Undefined values reject before mutation. Changed mode reports PropertyListChanged after committing the entire profile. [PhysicsMassProfileTests](../../tests/Electron2D.Tests/PhysicsMassProfileTests.cs) verifies values, transitions, failure ordering and storage; [ADR 0073](../decisions/physics-mass.md#adr-0073) defines the typed projection.
