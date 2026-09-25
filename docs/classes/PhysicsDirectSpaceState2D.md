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
| `public PhysicsShapeResult2D[] IntersectShape(PhysicsShapeQueryParameters2D parameters, int maxResults = 32)` | Collider shape owners touched by the shape or its motion, RID/index ordered and deduplicated. |
| `public (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters2D parameters)` | First new collision bracket along a global displacement; `(1, 1)` for no hit. |
| `public Vector2[] CollideShape(PhysicsShapeQueryParameters2D parameters, int maxResults = 32)` | Even array of query/collider contact-point pairs, with a pair cap. |
| `public PhysicsRestInfo2D? GetRestInfo(PhysicsShapeQueryParameters2D parameters)` | Deepest eligible contact, with collider point, outward normal and point velocity. |

## Query behavior

Both methods honor all 32 layer bits, RID exclusions and independent body/Area flags. They test collider layers even if that collider's own collision mask is zero. Ray hits carry a global position and outward normal; an enabled origin-inside hit uses the ray origin and zero normal. A zero-length ray or a query with no eligible colliders returns null/empty. Point queries test filled circle, capsule and polygon interiors; hollow edges have no point interior. Several backend fixtures belonging to one direct CollisionShape or CollisionPolygon owner produce one point result. Ray ties choose the lowest collider RID and shape-owner index; point results sort by those keys before limiting `maxResults`. Negative maximum throws `ArgumentOutOfRangeException`.

Shape operations share [PhysicsShapeQueryParameters2D](PhysicsShapeQueryParameters2D.md). `IntersectShape` includes geometry crossed by `Motion`, sorts by collider RID and direct shape-owner index, deduplicates compound pieces and then applies `maxResults`. `CastMotion` ignores a collider already intersecting the query at its origin and returns eight-refinement safe/unsafe fractions around the earliest new collision. `CollideShape` returns query point then collider point for each contact, applies the pair cap after RID/index ordering, and returns an empty array on a miss. `GetRestInfo` returns null on a miss and reports the deepest contact with collider point, normal directed toward the query and velocity at that point. Query shapes and server fixtures are live; geometry edits are prepared before querying. Invalid or stale shape RIDs reject the query. Negative result caps throw `ArgumentOutOfRangeException`; a zero cap returns an empty array. All shape operations reject off-owner or in-step access.

The implementation scans registered scene and server fixture lists directly so a collider with mask zero remains queryable by layer. This is linear in fixture count; a measured large-world cost can justify a dedicated broad-phase path. Canvas-instance filtering and remaining direct-space methods retain [coverage gaps](../coverage/classes/PhysicsDirectSpaceState2D.md). [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies ray/point behavior; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) verifies resource/RID shape selection, swept overlap, fractions, contact pairs, manifold families, compound and hollow geometry, scene/server results, filters, errors and warmed unchanged casts/rest queries without managed allocation. Native allocation, other platforms and large-world performance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).
