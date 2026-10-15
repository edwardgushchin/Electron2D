# PhysicsDirectSpaceState

[Unrounded polygon containment](../components/physics-point-containment.md) now excludes an exterior CPU point through fixture half-planes and applies the same decision to ordinary/directed ray origins. Logical seams, inside-start policy, filters and result identity retain their existing contract; float boundary rounding remains bounded separately.

Last updated: 2026-10-15

**Inherits:** ElectronObject · **Inherited By:** [PhysicsDirectSpaceStateExtension](PhysicsDirectSpaceStateExtension.md) · **Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs)

## Description

Jiggle uses the existing internal scalar ray overload for a candidate dynamic point in the attached scene world, bodies-only with its 32-bit collision mask. Query preparation and owner/stepping guards are unchanged. Tests exercise real scene fixture hits before a physics step and warmed native render/query intervals; whole-bone collision is not supplied by this ray.

A live view of one existing CPU or GPU space, shared by [World](World.md) and [PhysicsServer.SpaceGetDirectState](PhysicsServer.md). It prepares pending scene fixture and transform edits before querying, including queries before the first physics frame. It does not advance simulation or own a second world. Attached queries require the space's owner thread and reject execution while its solver is stepping. A freed space makes a retained view unusable.

All six operations use the selected per-world implementation after shared access guards and authoring preparation. Array/span overloads preserve ordering, caps, identity and untouched storage tails. Disposing this view retires only the view; a replacement still queries the same world. See [query dispatch](../components/physics-queries.md#runtime-flow) for ownership and current extension limits.

Acquisition, as well as query execution, requires the owning thread outside
solver execution or terminal failure. The selected world owner creates cached
views outside the shared service registry lock. Null/foreign/disposed factory
results and recursive acquisition reject before cache publication. The internal
[factory route](../components/physics-backends.md#selected-direct-state-construction)
executes extensions; public named registration now executes through the same selected owner. Complete custom numerical/geometry acceptance remains open.

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

Both methods honor all 32 layer bits, RID exclusions and independent body/Area flags. They test collider layers even if that collider's own collision mask is zero. Ray hits carry a global position and outward normal; an enabled origin-inside hit uses the ray origin and zero normal. Containment is tested over every piece of a logical compound slot before casting, so an inside start cannot hit an internal polygon seam. Skipping that slot leaves other slots of the same collider eligible. A zero-length ray or a query with no eligible colliders returns null/empty. Point queries test filled circle, capsule and polygon interiors; hollow edges have no point interior. Several backend fixtures belonging to one direct CollisionShape or CollisionPolygon owner produce one point result. Ray ties choose the lowest collider RID and shape-owner index; point results sort by those keys before limiting `maxResults`. Negative maximum throws `ArgumentOutOfRangeException`.

Shape operations share [PhysicsShapeQueryParameters](PhysicsShapeQueryParameters.md). `IntersectShape` includes geometry crossed by `Motion`, sorts by collider RID and direct shape-owner index, deduplicates compound pieces and then applies `maxResults`. `CastMotion` ignores an entire logical collider slot already intersecting any query piece at its origin and returns eight-refinement safe/unsafe fractions around the earliest new collision. `CollideShape` returns query point then collider point for each contact, applies the pair cap after RID/index ordering, and returns an empty array on a miss. `GetRestInfo` returns null on a miss and reports the deepest contact with collider point, normal directed toward the query and velocity at that point. Query shapes and server fixtures are live; geometry edits are prepared before querying. Invalid or stale shape RIDs reject the query. Negative result caps throw `ArgumentOutOfRangeException`; a zero cap returns an empty array. All shape operations reject off-owner or in-step access.

Compound polygon seams never count as new hits after an initial overlap. Directed shape queries also reject a ray origin contained by any piece of a logical compound polygon, including a compound query's swept region in the reverse case. Other slots remain independently eligible. PhysicsShapeQueryTests covers these CPU regressions. [Resident GPU shape queries](../components/gpu-resident-shape-queries.md) execute internally, but this public view remains CPU-backed.

The span overloads execute the same queries and ordering as the array overloads. They return the written hit or pair count, truncate excess results, leave unused elements unchanged, and accept empty destinations. An odd contact destination leaves its final element unchanged. Query scratch storage is reused after preparation. Array overloads remain independent copied exports. Ray and point queries borrow their already-copied parameter exclusions internally without cloning on each call. `PhysicsShapeQueryTests` checks compound owner identity, arrays versus spans, truncation, odd/empty capacity, exclusions, errors and zero managed allocation across warmed active/miss queries.

The implementation scans registered scene and server fixture lists directly so a collider with mask zero remains queryable by layer. This is linear in fixture count; a measured large-world cost can justify a dedicated broad-phase path. Point queries filter by exact CanvasInstanceID before ordering/deduplication/caps; zero selects the default canvas. Rays and shape/motion queries do not apply this filter. Object-instance binding, tile ownership and public GPU integration retain their [coverage gaps](../coverage/classes/PhysicsDirectSpaceState2D.md). [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies ray/point behavior; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) verifies resource/RID shape selection, swept overlap, fractions, contact pairs, manifold families, compound and hollow geometry, scene/server results, filters, errors and warmed unchanged casts/rest queries without managed allocation. Native allocation, other platforms and large-world performance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

SeparationRayShape queries use a front-facing directed surface crossing. Containment and ray-ray pairs do not contact; IntersectRay and IntersectPoint cannot intersect a separation ray. Its Margin and positive axial Motion extend the endpoint. An ordinary moving convex shape against a stationary ray uses its swept primitive union. SlideOnSlope selects the surface normal and its synthetic separation contact point; false separates opposite the axis. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) checks the directed and reverse operations under [ADR 0068](../decisions/physics.md#adr-0068).


Assigned object identity is sampled with each query result, including raw server objects.
Rebinding does not retarget earlier results; disposal/collection makes object resolution null
without erasing the sampled ID. See [object associations](../components/physics-object-bindings.md).

## Viewport picking consumer

The internal picking stage consumes the same complete point-hit collector as both
public IntersectPoint overloads. It applies scene eligibility before its own cap;
public query masks, ordering and result limits retain their existing contracts.
See [Physics picking](../components/physics-picking.md) for CPU/GPU ownership and
current GPU metadata transfer cost.

## Caller-created extension views

[PhysicsDirectSpaceStateExtension](PhysicsDirectSpaceStateExtension.md) binds a borrowed live space RID and implements all six queries through typed protected hooks. Every inherited public array/span/scalar overload reaches those hooks after common preparation and guards. Hook results are validated before output changes; object associations are sampled from current RID/shape ownership. Nested invocations restore exclusions and use independent retained scratch. The cached server-created view retains its selected built-in solver dispatch; registered-backend integration remains open under ADR 0103.


[Registered factory-selected views](../components/physics-registered-adapter.md#factory-selected-direct-views) now execute through a separate public-only consumer: cache identity, an initially empty contact snapshot, synchronized immutable publication before callbacks, result/lifetime validation, recreation and zero warm managed allocations. Independent caller-created body views keep direct hook dispatch; wider numerical-extension acceptance stays open.

The public GPU adapter retains one rebindable query-geometry lease. Alternating live resources reuses its wrapper and retires the previous reference; past resources are not accumulated. ConvexPolygonShapeTests exercises 128 warmed overlap/contact/rest/motion cycles between shared and standalone geometry on CPU/GPU with zero owner/all-thread bytes, preserving current contour results and initial-overlap fractions. GPUPhysicsShapeQueryTests checks successful replacement, null/disposed/thread rejection with preserved previous geometry, and retired lease failure. Geometry uploads and query waits retain their existing separate costs.

TileMapLayerTests checks generated tile owner/RID/logical-slot projection, exact nondefault/default canvas filtering, reparenting and sampled weak owner lifetime on public CPU/GPU worlds. Sixty-four warmed full frames plus span point queries allocate zero owner/all-thread managed bytes on Linux/.NET 10.

The complete PhysicsMotionTests, PhysicsQueryTests and PhysicsShapeQueryTests now use explicitly selected public CPU/GPU worlds. The common suites retain scene/raw identities, view replacement and last-resource retirement, filters/exclusions, compound origin-inside and initial-overlap rules, hollow/segment/convex contact families, one-way recovery, motion fractions/travel and scene shape-owner payloads. Existing numerical bounds are unchanged. Warm gates include both owner/all-thread managed counters: 64 scene-motion, compound recovery/ray/cast/rest queries and 128 alternating span-query cycles require 0/0 bytes. Caller-owned World aliases may outlive the SceneTree; its colliders retire immediately and the view expires after the last World resource is released. Native allocations, foreign platforms, visual and final performance acceptance remain open.

## Geometric query brackets

Built-in shape casts now target geometric boundaries in CPU conservative advancement and independent resident GPU query/motion sweeps, rather than a penetration-reduced solver target. Disk queries and linear-disk CCD share the CPU analytic fraction kernel; other represented proxies use geometric distance with 1e-5 scene-unit convergence tolerance. The internal CPU cast explicitly selects this query policy while native solver callers retain their existing policy. Sharp zero-radius contact retains the preceding finite normal. CastMotion keeps eight refinements: analytic first touch must lie between safe/unsafe with maximum width 1/256. The rejected disk 10/12 case returned .87109375/.875; the rejected rectangle .4 case returned .421875/.42578125. Ten circle/segment query combinations against rectangle, capsule, segment, convex and concave targets now verify rounded and sharp boundaries; each runs 128 warm queries at strict 0/0 managed bytes. Bit equality across backends is not required.

[Compound slot checks](../components/physics-registered-adapter.md#compound-slot-numerics) exercise the physical first-touch invariant and retained logical identities.


[Query-shape borrowing](../components/physics-space-extensions.md#query-shape-borrowing-2026-10-14) retains resource/owned RID geometry through preparation, hooks and validated publication, with nested/error continuation and zero warmed full-frame allocations. Existing declarations and coverage states remain unchanged; full numerical/foreign/hardware acceptance stays open.
