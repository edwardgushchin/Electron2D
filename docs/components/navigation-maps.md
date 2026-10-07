# Authored navigation maps and regions

Last updated: 2026-10-07

The initial NavigationServer backend consumes authored convex polygon geometry and publishes immutable planar map iterations. Maps/regions are real RID owners; NavigationRegion : Entity and World.NavigationMap use the same storage. Gameplay can query a route around missing walkable areas and follow it using existing scene/physics APIs. No renderer-specific navigation mesh or native backend identity enters public APIs.

## Publication and ownership

NavigationServer is a permanent retained ElectronObject with static operations and Engine named-service lookup. User-created MapCreate/RegionCreate/LinkCreate identities require FreeRID; map deletion detaches staged region memberships. World lazily creates an active borrowed map whose lifetime matches its canvas/physics runtime. Scene region RIDs are stable before attachment and remain until node disposal; exit removes map membership. FreeRID rejects world maps and scene region ownership.

Configuration/geometry edits stage under the service gate. Getters for those properties report staged values, while path/projection/bounds read the latest committed iteration. Synchronize first builds every dirty region/map; a failed build preserves all previous iterations for retry. It then publishes dirty snapshots, increments nonzero iteration IDs and then delivers MapChanged outside the gate. The scene physics lane calls the same synchronization before user callbacks. Edits during callbacks affect later publications; recursive synchronization rejects. Callback failures aggregate after committed iterations. A batch host uses Synchronize directly; deprecated force-update APIs are excluded.

Geometry arrays are copied and published as immutable resource versions. Scene nodes and server regions borrow a NavigationPolygon and observe Changed/disposal; editing the resource stages a new version, while published paths remain stable until sync. Empty/disabled/inactive topology produces empty paths and zero/empty projection identities. Layer-zero queries traverse no geometry. Source scene properties and server configuration remain separate; source setters republish their matching setting, and world/transform changes stage current map/geometry placement.

## Geometry and routes

NavigationPolygon supplies finite local vertices and consistently wound convex polygon index arrays. At least three distinct indices and nonzero area are required. Public getters return copies; replacement validates retained polygons before commit. Resource copies share immutable versions safely. Polygon replacement and edits deliver NavigationPolygonChanged after geometry staging. Detached/disabled region bounds and projections retain their own committed geometry independently of active map membership. The typed archive stores a bounded versioned byte payload; load validates all lengths, geometry and trailing data before publication. NavigationPolygon/NavigationRegion factories are registered for fresh-process loading. Region settings and borrowed resource graphs are stored; server RIDs/iterations are live runtime identity.

Shared exact edges connect within and between regions independently of optional margin connections. Enabled map/region edge settings and finite nonnegative EdgeConnectionMargin permit projected overlapping near-edge portals between regions on free edges only. Edges with more than two exact owners reject before any publication. Committed cells retain region masks and entry/travel costs. Endpoint projection chooses the closest applicable cell; corridor search uses closest portal entry positions and weighted cost/heuristic ordering. Unreachable destinations return the closest reached boundary. The unoptimized result retains portal midpoints; the optimized result uses a funnel and obstacle-adjacent corners. Paths are independent arrays and search scratch allocates per query. Projection, overlap ratios and funnel orientation use double intermediates so finite long edges do not overflow single-precision squared lengths.

The current profile does not claim the complete pinned topology/routing backend. Quantized cell rasterization, quantized nonmanifold/overlapping edge ownership and full cost/tie equivalence remain Partial dependencies of path/projection/edge connection operations. Cell-size/raster controls are absent until that consumer executes. Cold edge construction currently compares edges quadratically; large-map performance requires a measured spatial edge index. No broad performance claim follows from small fixtures.

Baking/source parsing, outlines/clearance, terrain source adapters, async workers, path-query metadata objects, random-area sampling, agent/obstacle avoidance, debug/editor producers and a public planar navmesh data adapter retain exact separate coverage triggers. The deprecated make_polygons_from_outlines, map_force_update and get_region_rid are excluded with pinned metadata; no aliases or inert switches are shipped.

## Links across separated surfaces

NavigationLink : Entity and caller-owned server link RIDs connect nearest enabled convex polygon surfaces through the same map iteration. Default connection radius is four world units. Each endpoint independently chooses the nearest polygon strictly inside that radius, in existing region/cell order; exact boundary distance and radius zero do not attach. Layer filtering applies afterward to both attached regions and the link. Links do not become walkable surfaces, closest-point owners or region bounds.

Synthetic link cells carry enablement, direction, layers and entry/travel costs. Forward portals connect the projected start and end; bidirectional links also permit reverse travel. The same corridor search and raw/funnel output execute across them. Collinear optimized paths may omit redundant portal points; raw paths retain point portals. A route supplies geometric travel; game code decides jump/teleport/other actions, and typed per-segment metadata remains a later query resource slice.

Source endpoints are local. Detached global getter/setter methods preserve that local role; attachment and Entity transforms publish finite world endpoints. Source properties cache independently of direct server edits. World replacement preserves the node RID and transfers membership; empty overrides restore the current World map. Configuration warnings use the existing Node tooling API for coincident endpoints. Typed source properties and factories load in a fresh process; live RID/map overrides are not serialized.

A link counter remains zero before attached synchronization, increments for changed link settings after successful map publication and wraps from uint.MaxValue to one. Map-only radius/topology rebuilds do not increment unchanged link counters. Failed map builds preserve every prior link version and map snapshot. Scene links are held weakly and swept at the same boundary; node/world disposal or caller FreeRID supplies normal deterministic cleanup. Two thousand warmed idle identity/unchanged setting/synchronization cycles and two thousand changing scene endpoint staging/global lookup cycles allocate no managed bytes; the latter excludes topology builds. changed topology and returned paths allocate by their explicit profile.

## Public use

```csharp
var map = NavigationServer.MapCreate();
var region = NavigationServer.RegionCreate();
using var polygon = new NavigationPolygon();
polygon.SetVertices([new(0, 0), new(64, 0), new(64, 64), new(0, 64)]);
polygon.AddPolygon([0, 1, 2, 3]);
NavigationServer.MapSetActive(map, true);
NavigationServer.RegionSetMap(region, map);
NavigationServer.RegionSetNavigationPolygon(region, polygon);
NavigationServer.Synchronize();
Vector2[] path = NavigationServer.MapGetPath(map, new(4, 4), new(60, 60), true);
NavigationServer.FreeRID(region);
NavigationServer.FreeRID(map);
```

## Verification

NavigationTests checks copied geometry, validation rollback, deferred publication, exact funnel corners, weighted alternative corridors, partial edge overlap, layer/enable filtering, unreachable projection, region ownership, callback staging/reentry, all-map rollback, reversed edge overlap, self-intersecting polygon rejection, weak scene lifetime, World/scene membership and transform updates. Two thousand prepared closest-point queries and unchanged synchronization boundaries each allocate zero managed bytes; returned paths and topology changes allocate. Packed region/polygon files load in a fresh process and supply real query topology.

The native host selected by ELECTRON2D_TEST_NAVIGATION_HOST=1 creates scene regions, queries the World map and moves a public Entity along the optimized path. Fallback is disabled and a hardware renderer is asserted. Pixel readback checks the actor at the goal after following the corridor around the missing area. Optional ELECTRON2D_NAVIGATION_SNAPSHOT saves real rendered pixels. GPU and hardware compatibility Linux evidence is distinct from authoring/headless simulation, foreign-platform verification and human acceptance. See ADR 0097 and ADR 0021.

NavigationLinkTests adds linked routes, strict radius boundaries, source/global endpoints, warning and fresh archive checks. Its native host verifies disabled-to-enabled deferred publication, directed traversal and real actor goal pixels across the missing surface.
