# PhysicsTestMotionParameters2D

Last updated: 2026-09-26

**Inherits:** ElectronObject · **Source:** [PhysicsTestMotion2D.cs](../../src/Servers/Physics/PhysicsTestMotion2D.cs) · **Component:** [Physics server and direct queries](../components/physics-queries.md)

## Description

Caller-owned mutable input to [PhysicsServer.BodyTestMotion](PhysicsServer.md). The supplied `From` pose and `Motion` are global scene coordinates. The body itself stays at its current pose. Exclusion arrays copy on both reads and writes.

## Example

Partial snippet with a registered `bodyRID`:

```csharp
using var parameters = new PhysicsTestMotionParameters2D
{
    From = Transform.Identity,
    Motion = new Vector2(0, 100)
};
bool blocked = PhysicsServer.Instance.BodyTestMotion(bodyRID, parameters);
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PhysicsTestMotionParameters2D()` | — | Mutable identity-pose query. |
| `public Transform From { get; set; }` | identity | Finite unit-scale, zero-skew global pose. |
| `public Vector2 Motion { get; set; }` | zero | Finite global displacement with finite length. |
| `public float Margin { get; set; }` | 0.08 | Finite nonnegative recovery margin in scene units. |
| `public bool RecoveryAsCollision { get; set; }` | false | Report initial depenetration as a contact. |
| `public RID[] ExcludeBodies { get; set; }` | empty | Copied body RID exclusions. |
| `public ulong[] ExcludeObjects { get; set; }` | empty | Copied managed instance-ID exclusions. |

## Property descriptions

`From`, `Motion` and `Margin` validate before mutation. The active physics profile rejects scaled or skewed body poses under [ADR 0054](../decisions/physics.md#adr-0054). `Motion` is displacement, not velocity. `RecoveryAsCollision=false` still moves a simulated test pose out of initial penetration, but reports only a collision caused by requested motion; true can report the recovery contact. `ExcludeBodies` applies to both scene and server body RIDs. `ExcludeObjects` uses unsigned managed `InstanceID` values and cannot name a server-only body. Null exclusion arrays reject. A disposed parameter object rejects access.

The upstream separation-ray option remains [Blocked](../coverage/classes/PhysicsTestMotionParameters2D.md) until a separation-ray shape has executable floor-snapping behavior. No inert option is exposed.

## Verification

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks defaults, invalid rollback, copied exclusions, server and scene bodies, recovery, one-way margins and no-hit reset. See [ADR 0063](../decisions/physics.md#adr-0063).
