# PhysicsDirectSpaceState2D

Last updated: 2026-09-25

**Inherits:** ElectronObject · **Source:** [PhysicsDirectSpaceState2D.cs](../../src/Servers/Physics/PhysicsDirectSpaceState2D.cs)

## Description

A live view of one existing Box2D space, shared by [World2D](World2D.md) and [PhysicsServer2D.SpaceGetDirectState](PhysicsServer2D.md). It prepares pending scene fixture and transform edits before querying, including queries before the first physics frame. It does not advance simulation or own a second world. Attached queries require the space's owner thread and reject execution while its solver is stepping. A freed space makes a retained view unusable.

## Example

Partial snippet with an attached `player` body:

```csharp
var direct = player.GetWorld2D()?.DirectSpaceState;
using var ray = PhysicsRayQueryParameters2D.Create(new(0, 0), new(0, 100));
PhysicsRayResult2D? nearest = direct?.IntersectRay(ray);
```

## API summary

| Member | Contract |
| --- | --- |
| `public PhysicsRayResult2D? IntersectRay(PhysicsRayQueryParameters2D parameters)` | Nearest eligible ray hit, or null. |
| `public PhysicsPointResult2D[] IntersectPoint(PhysicsPointQueryParameters2D parameters, int maxResults = 32)` | Caller-owned, RID/index ordered and deduplicated filled-shape hits, capped by a nonnegative maximum. |

## Query behavior

Both methods honor all 32 layer bits, RID exclusions and independent body/Area flags. They test collider layers even if that collider's own collision mask is zero. Ray hits carry a global position and outward normal; an enabled origin-inside hit uses the ray origin and zero normal. A zero-length ray or a query with no eligible colliders returns null/empty. Point queries test filled circle, capsule and polygon interiors; hollow edges have no point interior. Several backend fixtures belonging to one direct CollisionShape or CollisionPolygon owner produce one point result. Ray ties choose the lowest collider RID and shape-owner index; point results sort by those keys before limiting `maxResults`. Negative maximum throws `ArgumentOutOfRangeException`.

The current implementation scans registered scene and server fixture lists directly so a collider with mask zero remains queryable by layer. This is linear in fixture count; a later measured large-world cost can justify a dedicated broad-phase path. Typed shape sweeps, casts, rest information and canvas-instance filtering remain [coverage gaps](../coverage/classes/PhysicsDirectSpaceState2D.md) with their own dependency triggers. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies scene/server shared space, nearest/inside rays, copied query state, Area and layer filtering, point ordering/caps, errors and teardown. Native allocation, other platforms and large-world performance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).
