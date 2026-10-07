# PhysicsDirectSpaceState

Last updated: 2026-10-07

**Inherits:** ElectronObject · **Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs)

## Description

Jiggle uses the existing internal scalar ray overload for a candidate dynamic point in the attached scene world, bodies-only with its 32-bit collision mask. Query preparation and owner/stepping guards are unchanged. Tests exercise real scene fixture hits before a physics step and warmed native render/query intervals; whole-bone collision is not supplied by this ray.

A live view of one existing Box2D space, shared by [World](World.md) and [PhysicsServer.SpaceGetDirectState](PhysicsServer.md). It prepares pending scene fixture and transform edits before querying, including queries before the first physics frame. It does not advance simulation or own a second world. Attached queries require the space's owner thread and reject execution while its solver is stepping. A freed space makes a retained view unusable.

## Example

Partial snippet with an attached `player` body:

```csharp
var direct = player.GetWorld()?.DirectSpaceState;
using var ray = PhysicsRayQueryParameters.Create(new(0, 0), new(0, 100));
PhysicsRayResult? nearest = direct?.IntersectRay(ray);
```

## API summary

| Member | Contract |
| --- | --- |
| `public PhysicsRayResult? IntersectRay(PhysicsRayQueryParameters parameters)` | Nearest eligible ray hit, or null. |
| `public PhysicsPointResult[] IntersectPoint(PhysicsPointQueryParameters parameters, int maxResults = 32)` | Caller-owned, RID/index ordered and deduplicated filled-shape hits, capped by a nonnegative maximum. |
| `public PhysicsShapeResult[] IntersectShape(PhysicsShapeQueryParameters parameters, int maxResults = 32)` | Collider shape owners touched by the shape or its motion, RID/index ordered and deduplicated. |
| `public int IntersectPoint(PhysicsPointQueryParameters parameters, Span<PhysicsPointResult> results)` | Writes ordered unique point hits; destination length is the cap. |
| `public int IntersectShape(PhysicsShapeQueryParameters parameters, Span<PhysicsShapeResult> results)` | Writes ordered unique shape hits without creating an output array. |
| `public int CollideShape(PhysicsShapeQueryParameters parameters, Span<Vector2> results)` | Writes complete ordered point pairs; returns a pair count. |
| `public (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)` | First new collision bracket along a global displacement; `(1, 1)` for no hit. |
| `public Vector2[] CollideShape(PhysicsShapeQueryParameters parameters, int maxResults = 32)` | Even array of query/collider contact-point pairs, with a pair cap. |
| `public PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters)` | Deepest eligible contact, with collider point, outward normal and point velocity. |

## Query behavior

Both methods honor all 32 layer bits, RID exclusions and independent body/Area flags. They test collider layers even if that collider's own collision mask is zero. Ray hits carry a global position and outward normal; an enabled origin-inside hit uses the ray origin and zero normal. A zero-length ray or a query with no eligible colliders returns null/empty. Point queries test filled circle, capsule and polygon interiors; hollow edges have no point interior. Several backend fixtures belonging to one direct CollisionShape or CollisionPolygon owner produce one point result. Ray ties choose the lowest collider RID and shape-owner index; point results sort by those keys before limiting `maxResults`. Negative maximum throws `ArgumentOutOfRangeException`.

Shape operations share [PhysicsShapeQueryParameters](PhysicsShapeQueryParameters.md). `IntersectShape` includes geometry crossed by `Motion`, sorts by collider RID and direct shape-owner index, deduplicates compound pieces and then applies `maxResults`. `CastMotion` ignores a collider already intersecting the query at its origin and returns eight-refinement safe/unsafe fractions around the earliest new collision. `CollideShape` returns query point then collider point for each contact, applies the pair cap after RID/index ordering, and returns an empty array on a miss. `GetRestInfo` returns null on a miss and reports the deepest contact with collider point, normal directed toward the query and velocity at that point. Query shapes and server fixtures are live; geometry edits are prepared before querying. Invalid or stale shape RIDs reject the query. Negative result caps throw `ArgumentOutOfRangeException`; a zero cap returns an empty array. All shape operations reject off-owner or in-step access.

The span overloads execute the same queries and ordering as the array overloads. They return the written hit or pair count, truncate excess results, leave unused elements unchanged, and accept empty destinations. An odd contact destination leaves its final element unchanged. Query scratch storage is reused after preparation. Array overloads remain independent copied exports. Ray and point queries borrow their already-copied parameter exclusions internally without cloning on each call. `PhysicsShapeQueryTests` checks compound owner identity, arrays versus spans, truncation, odd/empty capacity, exclusions, errors and zero managed allocation across warmed active/miss queries.

The implementation scans registered scene and server fixture lists directly so a collider with mask zero remains queryable by layer. This is linear in fixture count; a measured large-world cost can justify a dedicated broad-phase path. Canvas-instance filtering and remaining direct-space methods retain [coverage gaps](../coverage/classes/PhysicsDirectSpaceState2D.md). [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies ray/point behavior; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) verifies resource/RID shape selection, swept overlap, fractions, contact pairs, manifold families, compound and hollow geometry, scene/server results, filters, errors and warmed unchanged casts/rest queries without managed allocation. Native allocation, other platforms and large-world performance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

SeparationRayShape queries use a front-facing directed surface crossing. Containment and ray-ray pairs do not contact; IntersectRay and IntersectPoint cannot intersect a separation ray. Its Margin and positive axial Motion extend the endpoint. An ordinary moving convex shape against a stationary ray uses its swept primitive union. SlideOnSlope selects the surface normal and its synthetic separation contact point; false separates opposite the axis. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) checks the directed and reverse operations under [ADR 0068](../decisions/physics.md#adr-0068).
