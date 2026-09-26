# PhysicsRayQueryParameters2D

Last updated: 2026-09-26

**Inherits:** ElectronObject · **Source:** [PhysicsQueryParameters2D.cs](../../src/Servers/Physics/PhysicsQueryParameters2D.cs)

Configures one [PhysicsDirectSpaceState.IntersectRay](PhysicsDirectSpaceState.md) call. It is caller-owned and mutable; reads and writes of `Exclude` copy the RID array. Finite scene-unit endpoints are global coordinates.

## Example

Partial snippet with an attached `player` body:

```csharp
using var query = PhysicsRayQueryParameters2D.Create(new(0, 0), new(0, 100));
query.Exclude = [player.GetRID()];
var hit = player.GetWorld2D()?.DirectSpaceState.IntersectRay(query);
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PhysicsRayQueryParameters2D()` | — | Zero-length origin ray. |
| `public static PhysicsRayQueryParameters2D Create(Vector2 from, Vector2 to, uint collisionMask = uint.MaxValue, RID[]? exclude = null)` | — | Caller-owned preconfigured parameters. |
| `public Vector2 From { get; set; }` | (0, 0) | Finite global ray origin. |
| `public Vector2 To { get; set; }` | (0, 0) | Finite global ray endpoint. |
| `public uint CollisionMask { get; set; }` | all bits | Eligible collider layers. |
| `public RID[] Exclude { get; set; }` | empty | Copied collider RIDs to skip. |
| `public bool CollideWithAreas { get; set; }` | false | Include Area sensors. |
| `public bool CollideWithBodies { get; set; }` | true | Include physics bodies. |
| `public bool HitFromInside { get; set; }` | false | Report a containing filled shape at the origin with zero normal. |

## Errors and verification

Null `Exclude` throws `ArgumentNullException`; nonfinite From/To throws `ArgumentOutOfRangeException` before mutation. An overflowing endpoint span rejects when queried. A zero-length ray returns no hit. Query masks use the collider's layer independently of its own mask. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) covers defaults, copies, invalid rollback, Area/body flags, masks, exclusions and inside hits. See [ADR 0063](../decisions/physics.md#adr-0063).
