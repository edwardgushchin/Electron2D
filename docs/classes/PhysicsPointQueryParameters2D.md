# PhysicsPointQueryParameters2D

Last updated: 2026-09-26

**Inherits:** ElectronObject · **Source:** [PhysicsQueryParameters2D.cs](../../src/Servers/Physics/PhysicsQueryParameters2D.cs)

Configures one [PhysicsDirectSpaceState.IntersectPoint](PhysicsDirectSpaceState.md) call. The global Position is finite scene units. `Exclude` reads and writes copy the RID array. This node-independent query does not yet carry a canvas instance ID; independent viewport canvas identity remains [Blocked](../coverage/classes/PhysicsPointQueryParameters2D.md).

## Example

Partial snippet with an attached `player` body:

```csharp
using var query = new PhysicsPointQueryParameters2D { Position = new Vector2(10, 20) };
var hits = player.GetWorld2D()?.DirectSpaceState.IntersectPoint(query);
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PhysicsPointQueryParameters2D()` | — | Point at the world origin. |
| `public Vector2 Position { get; set; }` | (0, 0) | Finite global query point. |
| `public uint CollisionMask { get; set; }` | all bits | Eligible collider layers. |
| `public RID[] Exclude { get; set; }` | empty | Copied collider RIDs to skip. |
| `public bool CollideWithAreas { get; set; }` | false | Include Area sensors. |
| `public bool CollideWithBodies { get; set; }` | true | Include physics bodies. |

Null `Exclude` and nonfinite Position reject before mutation. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks defaults, errors, copied arrays, layer/Area filters, exclusions and result caps. See [ADR 0063](../decisions/physics.md#adr-0063).
