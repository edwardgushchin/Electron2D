# PhysicsPointQueryParameters

Last updated: 2026-10-09

**Inherits:** ElectronObject · **Source:** [PhysicsQueryParameters.cs](../../src/Servers/Physics/PhysicsQueryParameters.cs)

Configures one [PhysicsDirectSpaceState.IntersectPoint](PhysicsDirectSpaceState.md) call. The global Position is finite scene units. `Exclude` reads and writes copy the RID array. `CanvasInstanceID` selects exactly one canvas association: zero for the default canvas, or a CanvasLayer instance ID. It does not transform Position. Scene entry/exit updates collider associations; server callers can assign them explicitly.

## Example

Partial snippet with an attached `player` body:

```csharp
using var query = new PhysicsPointQueryParameters { Position = new Vector2(10, 20) };
var hits = player.GetWorld()?.DirectSpaceState.IntersectPoint(query);
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PhysicsPointQueryParameters()` | — | Point at the world origin. |
| `public Vector2 Position { get; set; }` | (0, 0) | Finite global query point. |
| `public ulong CanvasInstanceID { get; set; }` | 0 | Exact canvas association, including zero. |
| `public uint CollisionMask { get; set; }` | all bits | Eligible collider layers. |
| `public RID[] Exclude { get; set; }` | empty | Copied collider RIDs to skip. |
| `public bool CollideWithAreas { get; set; }` | false | Include Area sensors. |
| `public bool CollideWithBodies { get; set; }` | true | Include physics bodies. |

Null `Exclude` and nonfinite Position reject before mutation. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks defaults, errors, copied arrays, layer/Area filters, exclusions and result caps. See [ADR 0063](../decisions/physics.md#adr-0063).
