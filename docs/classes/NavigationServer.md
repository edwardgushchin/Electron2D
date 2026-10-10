# NavigationServer

Last updated: 2026-10-10

- Source: [NavigationServer.cs](../../src/Servers/Navigation/NavigationServer.cs), [NavigationServer.API.cs](../../src/Servers/Navigation/NavigationServer.API.cs), [NavigationServer.Links.cs](../../src/Servers/Navigation/NavigationServer.Links.cs), [NavigationServer.Query.cs](../../src/Servers/Navigation/NavigationServer.Query.cs), [NavigationServer.Agents.cs](../../src/Servers/Navigation/NavigationServer.Agents.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Permanent retained navigation service with static map/region/link/agent lifecycle, staged settings, committed queries and MapChanged publication.

The [navigation contract](../components/navigation-maps.md) defines staged versus committed state, world coordinates and ownership. Static server operations serialize configuration; scene node mutations follow the scene owner thread. Resource arrays and returned paths are copied. The current topology/query profile has explicit coverage limits.

## Example

Partial consumer snippet; requires the existing scene/resource variables shown in comments.

```csharp
// polygon contains authored convex geometry.
RID map = NavigationServer.MapCreate();
RID region = NavigationServer.RegionCreate();
NavigationServer.MapSetActive(map, true);
NavigationServer.RegionSetMap(region, map);
NavigationServer.RegionSetNavigationPolygon(region, polygon);
NavigationServer.Synchronize();
Vector2[] path = NavigationServer.MapGetPath(map, new(1, 1), new(20, 20), true);
NavigationServer.FreeRID(region);
NavigationServer.FreeRID(map);
```

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public static void FreeRID(RID rid)`](#freerid) | Releases a caller-owned map, region, link or agent; scene/world identities retain their owning lifetime. |
| [`public static RID[] GetMaps()`](#getmaps) | Returns copied live map identities. |
| [`public static RID MapCreate()`](#mapcreate) | Creates a caller-owned, initially inactive navigation map. |
| [`public static Vector2 MapGetClosestPoint(RID map, Vector2 point)`](#mapgetclosestpoint) | Projects a finite world point onto the committed map, or returns zero for empty topology. |
| [`public static RID MapGetClosestPointOwner(RID map, Vector2 point)`](#mapgetclosestpointowner) | Returns the closest committed region identity, or empty for empty topology. |
| [`public static System.Single MapGetEdgeConnectionMargin(RID map)`](#mapgetedgeconnectionmargin) | Returns the staged region-edge connection margin. |
| [`public static ulong MapGetIterationID(RID map)`](#mapgetiterationid) | Returns the latest complete published map iteration, initially zero. |
| [`public static Vector2[] MapGetPath(RID map, Vector2 origin, Vector2 destination, bool optimize, uint navigationLayers = 1)`](#mapgetpath) | Returns a copied committed polygon path, projecting endpoints and returning the closest reached destination when disconnected. |
| [`public static RID[] MapGetRegions(RID map)`](#mapgetregions) | Returns copied staged region memberships of a map. |
| [`public static bool MapGetUseEdgeConnections(RID map)`](#mapgetuseedgeconnections) | Returns the staged edge-connection setting. |
| [`public static bool MapIsActive(RID map)`](#mapisactive) | Returns the map's staged active setting. |
| [`public static void MapSetActive(RID map, bool active)`](#mapsetactive) | Stages map activity; inactive committed maps have no query topology. |
| [`public static void MapSetEdgeConnectionMargin(RID map, System.Single margin)`](#mapsetedgeconnectionmargin) | Stages the finite nonnegative overlap matching margin for region edges. |
| [`public static void MapSetUseEdgeConnections(RID map, bool enabled)`](#mapsetuseedgeconnections) | Stages automatic margin connections between free region edges; exact shared edges remain connected. |
| [`public static RID RegionCreate()`](#regioncreate) | Creates a caller-owned enabled navigation region with no map or geometry. |
| [`public static Rect2 RegionGetBounds(RID region)`](#regiongetbounds) | Returns the region's committed transformed geometry bounds, independently of map activity. |
| [`public static Vector2 RegionGetClosestPoint(RID region, Vector2 point)`](#regiongetclosestpoint) | Projects a finite point onto this region's committed geometry independently of map activity, or returns zero for no geometry. |
| [`public static bool RegionGetEnabled(RID region)`](#regiongetenabled) | Returns the staged region enabled flag. |
| [`public static System.Single RegionGetEnterCost(RID region)`](#regiongetentercost) | Returns staged region entry cost. |
| [`public static RID RegionGetMap(RID region)`](#regiongetmap) | Returns a region's staged map identity. |
| [`public static uint RegionGetNavigationLayers(RID region)`](#regiongetnavigationlayers) | Returns staged navigation-layer bits. |
| [`public static ulong RegionGetOwnerID(RID region)`](#regiongetownerid) | Returns the region's staged logical owner identity. |
| [`public static Transform RegionGetTransform(RID region)`](#regiongettransform) | Returns the staged region transform. |
| [`public static System.Single RegionGetTravelCost(RID region)`](#regiongettravelcost) | Returns staged region travel cost. |
| [`public static bool RegionGetUseEdgeConnections(RID region)`](#regiongetuseedgeconnections) | Returns this region's staged edge-connection flag. |
| [`public static bool RegionOwnsPoint(RID region, Vector2 point)`](#regionownspoint) | Reports whether this region owns the closest committed point, preserving map tie ownership. |
| [`public static void RegionSetEnabled(RID region, bool enabled)`](#regionsetenabled) | Stages a region's enabled flag. |
| [`public static void RegionSetEnterCost(RID region, System.Single cost)`](#regionsetentercost) | Stages finite nonnegative entry cost. |
| [`public static void RegionSetMap(RID region, RID map)`](#regionsetmap) | Stages a region's map membership; empty detaches it. |
| [`public static void RegionSetNavigationLayers(RID region, uint layers)`](#regionsetnavigationlayers) | Stages navigation-layer bits. |
| [`public static void RegionSetNavigationPolygon(RID region, NavigationPolygon? polygon)`](#regionsetnavigationpolygon) | Stages an authored polygon resource version, borrowing the resource and observing later changes. |
| [`public static void RegionSetOwnerID(RID region, ulong ownerID)`](#regionsetownerid) | Stages the logical owner instance identity used to associate a region with a scene object. |
| [`public static void RegionSetTransform(RID region, Transform transform)`](#regionsettransform) | Stages a region's finite world transform. |
| [`public static void RegionSetTravelCost(RID region, System.Single cost)`](#regionsettravelcost) | Stages finite nonnegative travel cost per world distance. |
| [`public static void RegionSetUseEdgeConnections(RID region, bool enabled)`](#regionsetuseedgeconnections) | Stages automatic edge connections for this region. |
| [`public static void Synchronize()`](#synchronize) | Commits staged navigation topology through the same kernel used at a physics boundary. |
| [`protected override void ValidateDisposal()`](#validatedisposal) | Rejects consumer disposal of the permanent process service. |

## Events

| Member | Contract |
| --- | --- |
| [`public static event System.Action<RID> MapChanged`](#mapchanged) | Occurs after a complete dirty map iteration is published. |

## Member descriptions

<a id="freerid"></a>
### `public static void FreeRID(RID rid)`

Releases a caller-owned map, region, link or agent; scene/world identities retain their owning lifetime.

`rid`: Live caller-owned navigation map or region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="getmaps"></a>
### `public static RID[] GetMaps()`

Returns copied live map identities.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapcreate"></a>
### `public static RID MapCreate()`

Creates a caller-owned, initially inactive navigation map.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetclosestpoint"></a>
### `public static Vector2 MapGetClosestPoint(RID map, Vector2 point)`

Projects a finite world point onto the committed map, or returns zero for empty topology.

`map`: Live navigation map RID; empty is accepted only when detaching a region. `point`: Finite world-space query position.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetclosestpointowner"></a>
### `public static RID MapGetClosestPointOwner(RID map, Vector2 point)`

Returns the closest committed region identity, or empty for empty topology.

`map`: Live navigation map RID; empty is accepted only when detaching a region. `point`: Finite world-space query position.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetedgeconnectionmargin"></a>
### `public static System.Single MapGetEdgeConnectionMargin(RID map)`

Returns the staged region-edge connection margin.

`map`: Live navigation map RID; empty is accepted only when detaching a region.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetiterationid"></a>
### `public static ulong MapGetIterationID(RID map)`

Returns the latest complete published map iteration, initially zero.

`map`: Live navigation map RID; empty is accepted only when detaching a region.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetpath"></a>
### `public static Vector2[] MapGetPath(RID map, Vector2 origin, Vector2 destination, bool optimize, uint navigationLayers = 1)`

Returns a copied committed polygon path, projecting endpoints and returning the closest reached destination when disconnected.

`map`: Live navigation map RID; empty is accepted only when detaching a region. `origin`: Finite world-space route origin. `destination`: Finite world-space requested destination. `optimize`: True selects portal funnel corners; false keeps portal midpoints. `navigationLayers`: Applicable region layer bits, one by default; zero selects no cells.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetregions"></a>
### `public static RID[] MapGetRegions(RID map)`

Returns copied staged region memberships of a map.

`map`: Live navigation map RID; empty is accepted only when detaching a region.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapgetuseedgeconnections"></a>
### `public static bool MapGetUseEdgeConnections(RID map)`

Returns the staged edge-connection setting.

`map`: Live navigation map RID; empty is accepted only when detaching a region.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapisactive"></a>
### `public static bool MapIsActive(RID map)`

Returns the map's staged active setting.

`map`: Live navigation map RID; empty is accepted only when detaching a region.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapsetactive"></a>
### `public static void MapSetActive(RID map, bool active)`

Stages map activity; inactive committed maps have no query topology.

`map`: Live navigation map RID; empty is accepted only when detaching a region. `active`: True enables map participation; the published change follows synchronization.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapsetedgeconnectionmargin"></a>
### `public static void MapSetEdgeConnectionMargin(RID map, System.Single margin)`

Stages the finite nonnegative overlap matching margin for region edges.

`map`: Live navigation map RID; empty is accepted only when detaching a region. `margin`: Finite nonnegative world-space edge overlap distance.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapsetuseedgeconnections"></a>
### `public static void MapSetUseEdgeConnections(RID map, bool enabled)`

Stages automatic margin connections between free region edges; exact shared edges remain connected.

`map`: Live navigation map RID; empty is accepted only when detaching a region. `enabled`: Staged participation flag; exact shared edges remain independent of margin flags.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regioncreate"></a>
### `public static RID RegionCreate()`

Creates a caller-owned enabled navigation region with no map or geometry.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetbounds"></a>
### `public static Rect2 RegionGetBounds(RID region)`

Returns the region's committed transformed geometry bounds, independently of map activity.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetclosestpoint"></a>
### `public static Vector2 RegionGetClosestPoint(RID region, Vector2 point)`

Projects a finite point onto this region's committed geometry independently of map activity, or returns zero for no geometry.

`region`: Live navigation region RID. `point`: Finite world-space query position.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetenabled"></a>
### `public static bool RegionGetEnabled(RID region)`

Returns the staged region enabled flag.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetentercost"></a>
### `public static System.Single RegionGetEnterCost(RID region)`

Returns staged region entry cost.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetmap"></a>
### `public static RID RegionGetMap(RID region)`

Returns a region's staged map identity.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetnavigationlayers"></a>
### `public static uint RegionGetNavigationLayers(RID region)`

Returns staged navigation-layer bits.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetownerid"></a>
### `public static ulong RegionGetOwnerID(RID region)`

Returns the region's staged logical owner identity.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongettransform"></a>
### `public static Transform RegionGetTransform(RID region)`

Returns the staged region transform.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongettravelcost"></a>
### `public static System.Single RegionGetTravelCost(RID region)`

Returns staged region travel cost.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regiongetuseedgeconnections"></a>
### `public static bool RegionGetUseEdgeConnections(RID region)`

Returns this region's staged edge-connection flag.

`region`: Live navigation region RID.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionownspoint"></a>
### `public static bool RegionOwnsPoint(RID region, Vector2 point)`

Reports whether this region owns the closest committed point, preserving map tie ownership.

`region`: Live navigation region RID. `point`: Finite world-space query position.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetenabled"></a>
### `public static void RegionSetEnabled(RID region, bool enabled)`

Stages a region's enabled flag.

`region`: Live navigation region RID. `enabled`: Staged participation flag; exact shared edges remain independent of margin flags.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetentercost"></a>
### `public static void RegionSetEnterCost(RID region, System.Single cost)`

Stages finite nonnegative entry cost.

`region`: Live navigation region RID. `cost`: Finite nonnegative entry cost or travel distance multiplier.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetmap"></a>
### `public static void RegionSetMap(RID region, RID map)`

Stages a region's map membership; empty detaches it.

`region`: Live navigation region RID. `map`: Live navigation map RID; empty is accepted only when detaching a region.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetnavigationlayers"></a>
### `public static void RegionSetNavigationLayers(RID region, uint layers)`

Stages navigation-layer bits.

`region`: Live navigation region RID. `layers`: Unsigned 32-bit navigation-layer mask.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetnavigationpolygon"></a>
### `public static void RegionSetNavigationPolygon(RID region, NavigationPolygon? polygon)`

Stages an authored polygon resource version, borrowing the resource and observing later changes.

`region`: Live navigation region RID. `polygon`: Borrowed authored resource, or null to clear; indexed data is copied by the polygon resource.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetownerid"></a>
### `public static void RegionSetOwnerID(RID region, ulong ownerID)`

Stages the logical owner instance identity used to associate a region with a scene object.

`region`: Live navigation region RID. `ownerID`: Logical scene owner instance identity; zero means no associated owner.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsettransform"></a>
### `public static void RegionSetTransform(RID region, Transform transform)`

Stages a region's finite world transform.

`region`: Live navigation region RID. `transform`: Finite invertible local-to-world transform.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsettravelcost"></a>
### `public static void RegionSetTravelCost(RID region, System.Single cost)`

Stages finite nonnegative travel cost per world distance.

`region`: Live navigation region RID. `cost`: Finite nonnegative entry cost or travel distance multiplier.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="regionsetuseedgeconnections"></a>
### `public static void RegionSetUseEdgeConnections(RID region, bool enabled)`

Stages automatic edge connections for this region.

`region`: Live navigation region RID. `enabled`: Staged participation flag; exact shared edges remain independent of margin flags.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="synchronize"></a>
### `public static void Synchronize()`

Commits staged navigation topology through the same kernel used at a physics boundary. Useful for typed batch hosts without a SceneTree. Deprecated force-update operations are not exposed.

<a id="validatedisposal"></a>
### `protected override void ValidateDisposal()`

Rejects consumer disposal of the permanent process service.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

<a id="mapchanged"></a>
### `public static event System.Action<RID> MapChanged`

Occurs after a complete dirty map iteration is published. Callbacks run on the synchronizing thread, outside the service gate. Recursive synchronization rejects; callback edits stage the next iteration. Subscriber failures are aggregated after all deliveries.

Configuration setters stage the next iteration; configuration getters report staged values. Geometry queries read immutable committed data. Required identities must resolve to the owning kind; absent/stale/wrong-kind RIDs throw `ArgumentException`. Caller-owned identities require `FreeRID`; borrowed World/node identities reject it.

## Link operations

Owned map link memberships, strict connection radius and directed/bidirectional link settings use the same staged topology and lifetime kernel. Default radius is four world units; endpoints attach strictly inside it to the nearest enabled polygon, before query masks. Link counters change only when changed attached links synchronize and wrap at uint.MaxValue. Geometry links are excluded from map surface projections.

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public static RID LinkCreate()`](#linkcreate) | Creates a caller-owned enabled, bidirectional link with no map and zero endpoints. |
| [`public static bool LinkGetEnabled(RID link)`](#linkgetenabled) | Returns whether the link participates in committed routes from staged link settings. |
| [`public static Vector2 LinkGetEndPosition(RID link)`](#linkgetendposition) | Returns the finite world-space end endpoint from staged link settings. |
| [`public static float LinkGetEnterCost(RID link)`](#linkgetentercost) | Returns finite nonnegative cost paid when entering the link from staged link settings. |
| [`public static ulong LinkGetIterationID(RID link)`](#linkgetiterationid) | Returns the link's committed version counter, initially zero. |
| [`public static RID LinkGetMap(RID link)`](#linkgetmap) | Returns the link's staged map assignment. |
| [`public static uint LinkGetNavigationLayers(RID link)`](#linkgetnavigationlayers) | Returns the unsigned 32-bit navigation-layer mask from staged link settings. |
| [`public static ulong LinkGetOwnerID(RID link)`](#linkgetownerid) | Returns the logical scene owner instance identity from staged link settings. |
| [`public static Vector2 LinkGetStartPosition(RID link)`](#linkgetstartposition) | Returns the finite world-space start endpoint from staged link settings. |
| [`public static float LinkGetTravelCost(RID link)`](#linkgettravelcost) | Returns finite nonnegative distance multiplier inside the link from staged link settings. |
| [`public static bool LinkIsBidirectional(RID link)`](#linkisbidirectional) | Returns whether traversal also permits end-to-start travel from staged link settings. |
| [`public static void LinkSetBidirectional(RID link, bool bidirectional)`](#linksetbidirectional) | Stages whether traversal also permits end-to-start travel; initially true. |
| [`public static void LinkSetEnabled(RID link, bool enabled)`](#linksetenabled) | Stages whether the link participates in committed routes; initially true. |
| [`public static void LinkSetEndPosition(RID link, Vector2 position)`](#linksetendposition) | Stages the finite world-space end endpoint; initially Vector2.Zero. |
| [`public static void LinkSetEnterCost(RID link, float cost)`](#linksetentercost) | Stages finite nonnegative cost paid when entering the link; initially 0. |
| [`public static void LinkSetMap(RID link, RID map)`](#linksetmap) | Stages a link map assignment; empty detaches it. |
| [`public static void LinkSetNavigationLayers(RID link, uint layers)`](#linksetnavigationlayers) | Stages the unsigned 32-bit navigation-layer mask; initially 1. |
| [`public static void LinkSetOwnerID(RID link, ulong ownerID)`](#linksetownerid) | Stages the logical scene owner instance identity; initially 0. |
| [`public static void LinkSetStartPosition(RID link, Vector2 position)`](#linksetstartposition) | Stages the finite world-space start endpoint; initially Vector2.Zero. |
| [`public static void LinkSetTravelCost(RID link, float cost)`](#linksettravelcost) | Stages finite nonnegative distance multiplier inside the link; initially 1. |
| [`public static float MapGetLinkConnectionRadius(RID map)`](#mapgetlinkconnectionradius) | Returns the map's staged endpoint attachment radius. |
| [`public static RID[] MapGetLinks(RID map)`](#mapgetlinks) | Returns copied staged link memberships of the live map. |
| [`public static void MapSetLinkConnectionRadius(RID map, float radius)`](#mapsetlinkconnectionradius) | Stages the finite nonnegative endpoint attachment radius, initially four world units. |

## Member descriptions

<a id="linkcreate"></a>
### `public static RID LinkCreate()`

Creates a caller-owned enabled, bidirectional link with no map and zero endpoints. A new link RID requiring FreeRID.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetenabled"></a>
### `public static bool LinkGetEnabled(RID link)`

Returns whether the link participates in committed routes from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetendposition"></a>
### `public static Vector2 LinkGetEndPosition(RID link)`

Returns the finite world-space end endpoint from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetentercost"></a>
### `public static float LinkGetEnterCost(RID link)`

Returns finite nonnegative cost paid when entering the link from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetiterationid"></a>
### `public static ulong LinkGetIterationID(RID link)`

Returns the link's committed version counter, initially zero. Counter increments on changed attached link synchronization; wraps from uint.MaxValue to one.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetmap"></a>
### `public static RID LinkGetMap(RID link)`

Returns the link's staged map assignment. Assigned map, or empty when detached.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetnavigationlayers"></a>
### `public static uint LinkGetNavigationLayers(RID link)`

Returns the unsigned 32-bit navigation-layer mask from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetownerid"></a>
### `public static ulong LinkGetOwnerID(RID link)`

Returns the logical scene owner instance identity from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgetstartposition"></a>
### `public static Vector2 LinkGetStartPosition(RID link)`

Returns the finite world-space start endpoint from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkgettravelcost"></a>
### `public static float LinkGetTravelCost(RID link)`

Returns finite nonnegative distance multiplier inside the link from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linkisbidirectional"></a>
### `public static bool LinkIsBidirectional(RID link)`

Returns whether traversal also permits end-to-start travel from staged link settings. Current staged value.

`link`: Live navigation link RID.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetbidirectional"></a>
### `public static void LinkSetBidirectional(RID link, bool bidirectional)`

Stages whether traversal also permits end-to-start travel; initially true.

`link`: Live navigation link RID.

`bidirectional`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetenabled"></a>
### `public static void LinkSetEnabled(RID link, bool enabled)`

Stages whether the link participates in committed routes; initially true.

`link`: Live navigation link RID.

`enabled`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetendposition"></a>
### `public static void LinkSetEndPosition(RID link, Vector2 position)`

Stages the finite world-space end endpoint; initially Vector2.Zero.

`link`: Live navigation link RID.

`position`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind; endpoint is nonfinite.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetentercost"></a>
### `public static void LinkSetEnterCost(RID link, float cost)`

Stages finite nonnegative cost paid when entering the link; initially 0.

`link`: Live navigation link RID.

`cost`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind.

`ArgumentOutOfRangeException`: Cost is nonfinite or negative.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetmap"></a>
### `public static void LinkSetMap(RID link, RID map)`

Stages a link map assignment; empty detaches it.

`link`: Live navigation link RID.

`map`: Live map RID, or empty to detach.

`ArgumentException`: A required RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetnavigationlayers"></a>
### `public static void LinkSetNavigationLayers(RID link, uint layers)`

Stages the unsigned 32-bit navigation-layer mask; initially 1.

`link`: Live navigation link RID.

`layers`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetownerid"></a>
### `public static void LinkSetOwnerID(RID link, ulong ownerID)`

Stages the logical scene owner instance identity; initially 0.

`link`: Live navigation link RID.

`ownerID`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksetstartposition"></a>
### `public static void LinkSetStartPosition(RID link, Vector2 position)`

Stages the finite world-space start endpoint; initially Vector2.Zero.

`link`: Live navigation link RID.

`position`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind; endpoint is nonfinite.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="linksettravelcost"></a>
### `public static void LinkSetTravelCost(RID link, float cost)`

Stages finite nonnegative distance multiplier inside the link; initially 1.

`link`: Live navigation link RID.

`cost`: New staged value.

`ArgumentException`: The link RID is absent, stale or of another kind.

`ArgumentOutOfRangeException`: Cost is nonfinite or negative.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="mapgetlinkconnectionradius"></a>
### `public static float MapGetLinkConnectionRadius(RID map)`

Returns the map's staged endpoint attachment radius. Finite nonnegative world-space distance.

`map`: Live navigation map RID.

`ArgumentException`: The map RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="mapgetlinks"></a>
### `public static RID[] MapGetLinks(RID map)`

Returns copied staged link memberships of the live map. Independent link identity array, including disabled links.

`map`: Live navigation map RID.

`ArgumentException`: The map RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

<a id="mapsetlinkconnectionradius"></a>
### `public static void MapSetLinkConnectionRadius(RID map, float radius)`

Stages the finite nonnegative endpoint attachment radius, initially four world units.

`map`: Live navigation map RID.

`radius`: World distance; attachment is strictly inside this radius. Zero prevents attachment.

`ArgumentOutOfRangeException`: Radius is nonfinite or negative.

`ArgumentException`: The map RID is absent, stale or of another kind.

Configuration reads report staged values; path/projection read immutable published topology. Caller-created link RIDs require FreeRID; node-owned identities reject consumer free. A failed map build leaves all link counters and previous queries unchanged.

## Typed query operations

Queries capture settings and one map iteration, then publish selected arrays and length before the optional callback. Missing map/object lifetime errors preserve the previous result; completion failure occurs after publication. Read the [query contract](../components/navigation-maps.md#typed-query-objects-and-transition-metadata).

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public static void QueryPath(NavigationPathQueryParameters parameters, NavigationPathQueryResult result, Action? callback = null)`](#querypath) | Queries one committed map using a captured parameter snapshot and atomically replaces the supplied result. |
| [`public static Vector2[] SimplifyPath(ReadOnlySpan<Vector2> path, float epsilon)`](#simplifypath) | Returns a copied path with points removed by iterative Ramer-Douglas-Peucker simplification. |

## Member descriptions

<a id="querypath"></a>
### `public static void QueryPath(NavigationPathQueryParameters parameters, NavigationPathQueryResult result, Action? callback = null)`

Queries one committed map using a captured parameter snapshot and atomically replaces the supplied result. Callbacks may mutate settings/results and run nested queries. Callback exceptions propagate after publication. Required map identity resolves at query time; invalid input or disposed objects preserve the previous result.

`parameters`: Caller-owned typed settings; copied filter arrays remain stable during the query.

`result`: Caller-owned result receiving selected arrays and length together.

`callback`: Optional zero-argument completion invoked on this calling thread after publication, outside gates.

`ArgumentNullException`: A required object is null.

`ArgumentException`: The captured map is absent, stale or of another kind.

`ObjectDisposedException`: A parameter/result object is disposed.

<a id="simplifypath"></a>
### `public static Vector2[] SimplifyPath(ReadOnlySpan<Vector2> path, float epsilon)`

Returns a copied path with points removed by iterative Ramer-Douglas-Peucker simplification. Independent ordered points retaining endpoints, including empty and singleton paths.

`path`: Finite world-space points; never mutated.

`epsilon`: Finite world-distance tolerance; negative values clamp to zero.

`ArgumentOutOfRangeException`: An input point or epsilon is nonfinite.

## Verification and limits

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) checks typed resource/scene storage and fresh loading, deferred publication and rollback, geometry/cost/layer routes, lifetime and allocation boundaries. The rendered host follows a real World-map corridor on both current hardware backends. [ADR 0097](../decisions/navigation.md#adr-0097) and the [component contract](../components/navigation-maps.md) state remaining raster/search equivalence, baking, async and avoidance prerequisites.

## Agent identities and map membership

Stable caller/scene agent identities support real map registration and version consumption; they feed the same avoidance kernel.

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public static RID AgentCreate()`](#agentcreate) | Creates a caller-owned navigation agent identity detached from every map. |
| [`public static RID AgentGetMap(RID agent)`](#agentgetmap) | Returns the agent's current map membership. |
| [`public static bool AgentIsMapChanged(RID agent)`](#agentismapchanged) | Returns and consumes whether the assigned map's committed version differs from the last observed version. |
| [`public static void AgentSetMap(RID agent, RID map)`](#agentsetmap) | Assigns an agent to a live map; an empty RID detaches it. |
| [`public static RID[] MapGetAgents(RID map)`](#mapgetagents) | Returns copied staged agent memberships, including scene-owned agents. |

## Member descriptions

<a id="agentcreate"></a>
### `public static RID AgentCreate()`

Creates a caller-owned navigation agent identity detached from every map. Live agent RID requiring FreeRID.

<a id="agentgetmap"></a>
### `public static RID AgentGetMap(RID agent)`

Returns the agent's current map membership. Assigned map or empty.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentismapchanged"></a>
### `public static bool AgentIsMapChanged(RID agent)`

Returns and consumes whether the assigned map's committed version differs from the last observed version. False for a detached agent; otherwise whether the iteration changed.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentsetmap"></a>
### `public static void AgentSetMap(RID agent, RID map)`

Assigns an agent to a live map; an empty RID detaches it.

`agent`: Live agent RID.

`map`: Live map RID or empty.

`ArgumentException`: A required RID is absent, stale or of another kind.

<a id="mapgetagents"></a>
### `public static RID[] MapGetAgents(RID map)`

Returns copied staged agent memberships, including scene-owned agents. Independent agent RID array.

`map`: Live map RID.

`ArgumentException`: The map RID is absent, stale or of another kind.

## Reciprocal avoidance operations

The [shared kernel](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance) supplies real desired/forced/current velocity and obstacle state. Step is the typed caller-map batch projection of the same scene physics boundary. Registered callbacks run on the stepping thread after simultaneous publication; bound scene maps cannot be advanced by a second owner.

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public static bool AgentGetAvoidanceEnabled(RID agent)`](#agentgetavoidanceenabled) | Returns whether reciprocal avoidance participates for the agent. |
| [`public static uint AgentGetAvoidanceLayers(RID agent)`](#agentgetavoidancelayers) | Returns 32-bit layers visible to other participant masks for the agent. |
| [`public static uint AgentGetAvoidanceMask(RID agent)`](#agentgetavoidancemask) | Returns 32-bit mask selecting other participants for the agent. |
| [`public static float AgentGetAvoidancePriority(RID agent)`](#agentgetavoidancepriority) | Returns priority from zero through one; higher priority ignores lower for the agent. |
| [`public static int AgentGetMaxNeighbors(RID agent)`](#agentgetmaxneighbors) | Returns maximum selected neighbors; nonpositive disables agent neighbors for the agent. |
| [`public static float AgentGetMaxSpeed(RID agent)`](#agentgetmaxspeed) | Returns finite nonnegative output speed cap for the agent. |
| [`public static float AgentGetNeighborDistance(RID agent)`](#agentgetneighbordistance) | Returns finite nonnegative neighbor search radius for the agent. |
| [`public static bool AgentGetPaused(RID agent)`](#agentgetpaused) | Returns whether simulation and callback delivery are paused for the agent. |
| [`public static Vector2 AgentGetPosition(RID agent)`](#agentgetposition) | Returns finite world-space source position for the agent. |
| [`public static float AgentGetRadius(RID agent)`](#agentgetradius) | Returns finite nonnegative avoidance disc radius for the agent. |
| [`public static float AgentGetTimeHorizonAgents(RID agent)`](#agentgettimehorizonagents) | Returns finite nonnegative agent prediction horizon for the agent. |
| [`public static float AgentGetTimeHorizonObstacles(RID agent)`](#agentgettimehorizonobstacles) | Returns finite nonnegative contour prediction horizon for the agent. |
| [`public static Vector2 AgentGetVelocity(RID agent)`](#agentgetvelocity) | Returns finite desired velocity before stepping or computed velocity after stepping for the agent. |
| [`public static bool AgentHasAvoidanceCallback(RID agent)`](#agenthasavoidancecallback) | Returns whether the agent has a registered avoidance callback. |
| [`public static void AgentSetAvoidanceCallback(RID agent, Action<Vector2> callback)`](#agentsetavoidancecallback) | Sets or clears typed velocity delivery after committed avoidance publication. |
| [`public static void AgentSetAvoidanceEnabled(RID agent, bool value)`](#agentsetavoidanceenabled) | Sets whether reciprocal avoidance participates for the agent. |
| [`public static void AgentSetAvoidanceLayers(RID agent, uint value)`](#agentsetavoidancelayers) | Sets 32-bit layers visible to other participant masks for the agent. |
| [`public static void AgentSetAvoidanceMask(RID agent, uint value)`](#agentsetavoidancemask) | Sets 32-bit mask selecting other participants for the agent. |
| [`public static void AgentSetAvoidancePriority(RID agent, float value)`](#agentsetavoidancepriority) | Sets priority from zero through one; higher priority ignores lower for the agent. |
| [`public static void AgentSetMaxNeighbors(RID agent, int value)`](#agentsetmaxneighbors) | Sets maximum selected neighbors; nonpositive disables agent neighbors for the agent. |
| [`public static void AgentSetMaxSpeed(RID agent, float value)`](#agentsetmaxspeed) | Sets finite nonnegative output speed cap for the agent. |
| [`public static void AgentSetNeighborDistance(RID agent, float value)`](#agentsetneighbordistance) | Sets finite nonnegative neighbor search radius for the agent. |
| [`public static void AgentSetPaused(RID agent, bool value)`](#agentsetpaused) | Sets whether simulation and callback delivery are paused for the agent. |
| [`public static void AgentSetPosition(RID agent, Vector2 value)`](#agentsetposition) | Sets finite world-space source position for the agent. |
| [`public static void AgentSetRadius(RID agent, float value)`](#agentsetradius) | Sets finite nonnegative avoidance disc radius for the agent. |
| [`public static void AgentSetTimeHorizonAgents(RID agent, float value)`](#agentsettimehorizonagents) | Sets finite nonnegative agent prediction horizon for the agent. |
| [`public static void AgentSetTimeHorizonObstacles(RID agent, float value)`](#agentsettimehorizonobstacles) | Sets finite nonnegative contour prediction horizon for the agent. |
| [`public static void AgentSetVelocity(RID agent, Vector2 value)`](#agentsetvelocity) | Sets finite desired velocity before stepping or computed velocity after stepping for the agent. |
| [`public static void AgentSetVelocityForced(RID agent, Vector2 velocity)`](#agentsetvelocityforced) | Replaces the internal simulation velocity after teleporting without changing the desired velocity. |
| [`public static RID ObstacleCreate()`](#obstaclecreate) | Creates a caller-owned enabled obstacle with zero radius and empty contour. |
| [`public static bool ObstacleGetAvoidanceEnabled(RID obstacle)`](#obstaclegetavoidanceenabled) | Returns participation in avoidance. |
| [`public static uint ObstacleGetAvoidanceLayers(RID obstacle)`](#obstaclegetavoidancelayers) | Returns 32-bit layers visible to agent masks. |
| [`public static RID ObstacleGetMap(RID obstacle)`](#obstaclegetmap) | Returns current obstacle map membership. |
| [`public static bool ObstacleGetPaused(RID obstacle)`](#obstaclegetpaused) | Returns whether participation is paused. |
| [`public static Vector2 ObstacleGetPosition(RID obstacle)`](#obstaclegetposition) | Returns finite world-space translation. |
| [`public static float ObstacleGetRadius(RID obstacle)`](#obstaclegetradius) | Returns finite nonnegative moving-disc radius. |
| [`public static Vector2 ObstacleGetVelocity(RID obstacle)`](#obstaclegetvelocity) | Returns finite moving-disc velocity; static contours remain stationary predictions. |
| [`public static Vector2[] ObstacleGetVertices(RID obstacle)`](#obstaclegetvertices) | Returns a copied oriented local obstacle contour. |
| [`public static void ObstacleSetAvoidanceEnabled(RID obstacle, bool value)`](#obstaclesetavoidanceenabled) | Sets participation in avoidance. |
| [`public static void ObstacleSetAvoidanceLayers(RID obstacle, uint value)`](#obstaclesetavoidancelayers) | Sets 32-bit layers visible to agent masks. |
| [`public static void ObstacleSetMap(RID obstacle, RID map)`](#obstaclesetmap) | Assigns a live map; empty detaches the obstacle. |
| [`public static void ObstacleSetPaused(RID obstacle, bool value)`](#obstaclesetpaused) | Sets whether participation is paused. |
| [`public static void ObstacleSetPosition(RID obstacle, Vector2 value)`](#obstaclesetposition) | Sets finite world-space translation. |
| [`public static void ObstacleSetRadius(RID obstacle, float value)`](#obstaclesetradius) | Sets finite nonnegative moving-disc radius. |
| [`public static void ObstacleSetVelocity(RID obstacle, Vector2 value)`](#obstaclesetvelocity) | Sets finite moving-disc velocity; static contours remain stationary predictions. |
| [`public static void ObstacleSetVertices(RID obstacle, System.ReadOnlySpan<Vector2> vertices)`](#obstaclesetvertices) | Sets a copied simple oriented local contour; empty or one point creates no static segments. |
| [`public static void Step(System.Double delta)`](#step) | Synchronizes topology and advances avoidance on caller maps unbound to a SceneTree. |

## Member descriptions

<a id="agentgetavoidanceenabled"></a>
### `public static bool AgentGetAvoidanceEnabled(RID agent)`

Returns whether reciprocal avoidance participates for the agent. Current value, initially false.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetavoidancelayers"></a>
### `public static uint AgentGetAvoidanceLayers(RID agent)`

Returns 32-bit layers visible to other participant masks for the agent. Current value, initially 1u.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetavoidancemask"></a>
### `public static uint AgentGetAvoidanceMask(RID agent)`

Returns 32-bit mask selecting other participants for the agent. Current value, initially 1u.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetavoidancepriority"></a>
### `public static float AgentGetAvoidancePriority(RID agent)`

Returns priority from zero through one; higher priority ignores lower for the agent. Current value, initially 1f.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetmaxneighbors"></a>
### `public static int AgentGetMaxNeighbors(RID agent)`

Returns maximum selected neighbors; nonpositive disables agent neighbors for the agent. Current value, initially 10.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetmaxspeed"></a>
### `public static float AgentGetMaxSpeed(RID agent)`

Returns finite nonnegative output speed cap for the agent. Current value, initially 100f.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetneighbordistance"></a>
### `public static float AgentGetNeighborDistance(RID agent)`

Returns finite nonnegative neighbor search radius for the agent. Current value, initially 500f.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetpaused"></a>
### `public static bool AgentGetPaused(RID agent)`

Returns whether simulation and callback delivery are paused for the agent. Current value, initially false.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetposition"></a>
### `public static Vector2 AgentGetPosition(RID agent)`

Returns finite world-space source position for the agent. Current value, initially Vector2.Zero.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetradius"></a>
### `public static float AgentGetRadius(RID agent)`

Returns finite nonnegative avoidance disc radius for the agent. Current value, initially 10f.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgettimehorizonagents"></a>
### `public static float AgentGetTimeHorizonAgents(RID agent)`

Returns finite nonnegative agent prediction horizon for the agent. Current value, initially 1f.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgettimehorizonobstacles"></a>
### `public static float AgentGetTimeHorizonObstacles(RID agent)`

Returns finite nonnegative contour prediction horizon for the agent. Current value, initially 0f.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agentgetvelocity"></a>
### `public static Vector2 AgentGetVelocity(RID agent)`

Returns finite desired velocity before stepping or computed velocity after stepping for the agent. Current value, initially Vector2.Zero.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is absent, stale or of another kind.

<a id="agenthasavoidancecallback"></a>
### `public static bool AgentHasAvoidanceCallback(RID agent)`

Returns whether the agent has a registered avoidance callback. True when a typed handler is assigned.

`agent`: Live agent RID.

`ArgumentException`: The agent RID is invalid.

<a id="agentsetavoidancecallback"></a>
### `public static void AgentSetAvoidanceCallback(RID agent, Action<Vector2> callback)`

Sets or clears typed velocity delivery after committed avoidance publication.

`agent`: Live agent RID.

`callback`: Calling-thread handler or null to clear.

`ArgumentException`: The agent RID is invalid.

<a id="agentsetavoidanceenabled"></a>
### `public static void AgentSetAvoidanceEnabled(RID agent, bool value)`

Sets whether reciprocal avoidance participates for the agent.

`agent`: Live agent RID.

`value`: New setting; default false.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetavoidancelayers"></a>
### `public static void AgentSetAvoidanceLayers(RID agent, uint value)`

Sets 32-bit layers visible to other participant masks for the agent.

`agent`: Live agent RID.

`value`: New setting; default 1u.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetavoidancemask"></a>
### `public static void AgentSetAvoidanceMask(RID agent, uint value)`

Sets 32-bit mask selecting other participants for the agent.

`agent`: Live agent RID.

`value`: New setting; default 1u.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetavoidancepriority"></a>
### `public static void AgentSetAvoidancePriority(RID agent, float value)`

Sets priority from zero through one; higher priority ignores lower for the agent.

`agent`: Live agent RID.

`value`: New setting; default 1f.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

`ArgumentOutOfRangeException`: The setting is outside its finite accepted range.

<a id="agentsetmaxneighbors"></a>
### `public static void AgentSetMaxNeighbors(RID agent, int value)`

Sets maximum selected neighbors; nonpositive disables agent neighbors for the agent.

`agent`: Live agent RID.

`value`: New setting; default 10.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetmaxspeed"></a>
### `public static void AgentSetMaxSpeed(RID agent, float value)`

Sets finite nonnegative output speed cap for the agent.

`agent`: Live agent RID.

`value`: New setting; default 100f.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

`ArgumentOutOfRangeException`: The setting is outside its finite accepted range.

<a id="agentsetneighbordistance"></a>
### `public static void AgentSetNeighborDistance(RID agent, float value)`

Sets finite nonnegative neighbor search radius for the agent.

`agent`: Live agent RID.

`value`: New setting; default 500f.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

`ArgumentOutOfRangeException`: The setting is outside its finite accepted range.

<a id="agentsetpaused"></a>
### `public static void AgentSetPaused(RID agent, bool value)`

Sets whether simulation and callback delivery are paused for the agent.

`agent`: Live agent RID.

`value`: New setting; default false.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetposition"></a>
### `public static void AgentSetPosition(RID agent, Vector2 value)`

Sets finite world-space source position for the agent.

`agent`: Live agent RID.

`value`: New setting; default Vector2.Zero.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetradius"></a>
### `public static void AgentSetRadius(RID agent, float value)`

Sets finite nonnegative avoidance disc radius for the agent.

`agent`: Live agent RID.

`value`: New setting; default 10f.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

`ArgumentOutOfRangeException`: The setting is outside its finite accepted range.

<a id="agentsettimehorizonagents"></a>
### `public static void AgentSetTimeHorizonAgents(RID agent, float value)`

Sets finite nonnegative agent prediction horizon for the agent.

`agent`: Live agent RID.

`value`: New setting; default 1f.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

`ArgumentOutOfRangeException`: The setting is outside its finite accepted range.

<a id="agentsettimehorizonobstacles"></a>
### `public static void AgentSetTimeHorizonObstacles(RID agent, float value)`

Sets finite nonnegative contour prediction horizon for the agent.

`agent`: Live agent RID.

`value`: New setting; default 0f.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

`ArgumentOutOfRangeException`: The setting is outside its finite accepted range.

<a id="agentsetvelocity"></a>
### `public static void AgentSetVelocity(RID agent, Vector2 value)`

Sets finite desired velocity before stepping or computed velocity after stepping for the agent.

`agent`: Live agent RID.

`value`: New setting; default Vector2.Zero.

`ArgumentException`: The agent RID is invalid or a vector is nonfinite.

<a id="agentsetvelocityforced"></a>
### `public static void AgentSetVelocityForced(RID agent, Vector2 velocity)`

Replaces the internal simulation velocity after teleporting without changing the desired velocity.

`agent`: Live agent RID.

`velocity`: Finite forced simulation velocity.

`ArgumentException`: The RID or vector is invalid.

<a id="obstaclecreate"></a>
### `public static RID ObstacleCreate()`

Creates a caller-owned enabled obstacle with zero radius and empty contour. Live obstacle RID requiring FreeRID.

<a id="obstaclegetavoidanceenabled"></a>
### `public static bool ObstacleGetAvoidanceEnabled(RID obstacle)`

Returns participation in avoidance. Current setting, initially true.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetavoidancelayers"></a>
### `public static uint ObstacleGetAvoidanceLayers(RID obstacle)`

Returns 32-bit layers visible to agent masks. Current setting, initially 1u.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetmap"></a>
### `public static RID ObstacleGetMap(RID obstacle)`

Returns current obstacle map membership. Map or empty.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetpaused"></a>
### `public static bool ObstacleGetPaused(RID obstacle)`

Returns whether participation is paused. Current setting, initially false.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetposition"></a>
### `public static Vector2 ObstacleGetPosition(RID obstacle)`

Returns finite world-space translation. Current setting, initially Vector2.Zero.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetradius"></a>
### `public static float ObstacleGetRadius(RID obstacle)`

Returns finite nonnegative moving-disc radius. Current setting, initially 0f.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetvelocity"></a>
### `public static Vector2 ObstacleGetVelocity(RID obstacle)`

Returns finite moving-disc velocity; static contours remain stationary predictions. Current setting, initially Vector2.Zero.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclegetvertices"></a>
### `public static Vector2[] ObstacleGetVertices(RID obstacle)`

Returns a copied oriented local obstacle contour. Independent offset array.

`obstacle`: Live obstacle RID.

`ArgumentException`: The RID is invalid.

<a id="obstaclesetavoidanceenabled"></a>
### `public static void ObstacleSetAvoidanceEnabled(RID obstacle, bool value)`

Sets participation in avoidance.

`obstacle`: Live obstacle RID.

`value`: New setting.

`ArgumentException`: The RID or vector is invalid.

<a id="obstaclesetavoidancelayers"></a>
### `public static void ObstacleSetAvoidanceLayers(RID obstacle, uint value)`

Sets 32-bit layers visible to agent masks.

`obstacle`: Live obstacle RID.

`value`: New setting.

`ArgumentException`: The RID or vector is invalid.

<a id="obstaclesetmap"></a>
### `public static void ObstacleSetMap(RID obstacle, RID map)`

Assigns a live map; empty detaches the obstacle.

`obstacle`: Live obstacle RID.

`map`: Live map RID or empty.

`ArgumentException`: A required RID is invalid.

`InvalidOperationException`: A scene map belongs to another tree.

<a id="obstaclesetpaused"></a>
### `public static void ObstacleSetPaused(RID obstacle, bool value)`

Sets whether participation is paused.

`obstacle`: Live obstacle RID.

`value`: New setting.

`ArgumentException`: The RID or vector is invalid.

<a id="obstaclesetposition"></a>
### `public static void ObstacleSetPosition(RID obstacle, Vector2 value)`

Sets finite world-space translation.

`obstacle`: Live obstacle RID.

`value`: New setting.

`ArgumentException`: The RID or vector is invalid.

<a id="obstaclesetradius"></a>
### `public static void ObstacleSetRadius(RID obstacle, float value)`

Sets finite nonnegative moving-disc radius.

`obstacle`: Live obstacle RID.

`value`: New setting.

`ArgumentException`: The RID or vector is invalid.

`ArgumentOutOfRangeException`: Value is negative or nonfinite.

<a id="obstaclesetvelocity"></a>
### `public static void ObstacleSetVelocity(RID obstacle, Vector2 value)`

Sets finite moving-disc velocity; static contours remain stationary predictions.

`obstacle`: Live obstacle RID.

`value`: New setting.

`ArgumentException`: The RID or vector is invalid.

<a id="obstaclesetvertices"></a>
### `public static void ObstacleSetVertices(RID obstacle, System.ReadOnlySpan<Vector2> vertices)`

Sets a copied simple oriented local contour; empty or one point creates no static segments.

`obstacle`: Live obstacle RID.

`vertices`: Finite offsets from obstacle Position. Two points create a two-sided wall; winding controls larger contours.

`ArgumentException`: The RID or contour is invalid, duplicated or self-intersecting.

<a id="step"></a>
### `public static void Step(System.Double delta)`

Synchronizes topology and advances avoidance on caller maps unbound to a SceneTree. Bound scene maps advance at their own physics boundary. All outputs publish before callbacks; callbacks may stage future state but recursive stepping or synchronization rejects.

`delta`: Finite nonnegative float-representable duration in seconds. Zero only synchronizes; no callbacks occur.

`ArgumentOutOfRangeException`: Duration is negative, nonfinite or outside the float timestep range.

`InvalidOperationException`: Stepping or synchronization is reentered.

`AggregateException`: Observers fail after completed publication.

## Raster topology, profiling and surface sampling

The [committed topology contract](../components/navigation-maps.md#geometry-and-routes) owns these operations and [ProcessInfo](NavigationServer.ProcessInfo.md). Raster getters report staged settings; counters/pathways/region versions/samples read complete published snapshots.

| Complete declaration | Contract |
| --- | --- |
| `public static System.Int32 GetProcessInfo(Electron2D.NavigationServer.ProcessInfo processInfo)` | Returns a counter from the last complete synchronization; staged changes do not affect this snapshot. |
| `public static System.Single MapGetCellSize(Electron2D.RID map)` | Returns the staged raster cell size. |
| `public static System.Single MapGetMergeRasterizerCellScale(Electron2D.RID map)` | Returns the staged raster scale. |
| `public static Electron2D.Vector2 MapGetRandomPoint(Electron2D.RID map, System.UInt32 navigationLayers, System.Boolean uniformly)` | Samples an enabled nonempty committed map surface matching the supplied navigation layers. |
| `public static System.Void MapSetCellSize(Electron2D.RID map, System.Single cellSize)` | Stages the raster cell size, clamping finite values to at least 0.0001 world units. |
| `public static System.Void MapSetMergeRasterizerCellScale(Electron2D.RID map, System.Single scale)` | Stages the raster scale, clamping finite values to 0.0001 through 0.1. |
| `public static Electron2D.Vector2 RegionGetConnectionPathwayEnd(Electron2D.RID region, System.Int32 connection)` | Returns the second endpoint of one published margin pathway. |
| `public static Electron2D.Vector2 RegionGetConnectionPathwayStart(Electron2D.RID region, System.Int32 connection)` | Returns the first endpoint of one published margin pathway. |
| `public static System.Int32 RegionGetConnectionsCount(Electron2D.RID region)` | Returns the region's committed directed free-edge margin pathway count. |
| `public static System.UInt64 RegionGetIterationID(Electron2D.RID region)` | Returns the region's last published nonzero iteration identity, or zero before its first publication. |
| `public static Electron2D.Vector2 RegionGetRandomPoint(Electron2D.RID region, System.UInt32 navigationLayers, System.Boolean uniformly)` | Samples an enabled committed region surface matching the supplied navigation layers. |

<a id="member-37e2932cdebf"></a>
### `GetProcessInfo(Electron2D.NavigationServer.ProcessInfo)`

Kind: `method`

```csharp
public static System.Int32 GetProcessInfo(Electron2D.NavigationServer.ProcessInfo processInfo)
```

#### Summary

Returns a counter from the last complete synchronization; staged changes do not affect this snapshot.

#### Returns

The committed count, initially zero.

#### Parameters

- `processInfo`: A defined counter identity.

#### Exceptions

- `T:System.ArgumentOutOfRangeException`: The counter identity is unknown.

<a id="member-d0306740ffd2"></a>
### `MapGetCellSize(Electron2D.RID)`

Kind: `method`

```csharp
public static System.Single MapGetCellSize(Electron2D.RID map)
```

#### Summary

Returns the staged raster cell size.

#### Returns

One initially.

#### Parameters

- `map`: A live map RID.

<a id="member-3dbdf32e95a7"></a>
### `MapGetMergeRasterizerCellScale(Electron2D.RID)`

Kind: `method`

```csharp
public static System.Single MapGetMergeRasterizerCellScale(Electron2D.RID map)
```

#### Summary

Returns the staged raster scale.

#### Returns

0.1 initially.

#### Parameters

- `map`: A live map RID.

<a id="member-f13614a2ec03"></a>
### `MapGetRandomPoint(Electron2D.RID, System.UInt32, System.Boolean)`

Kind: `method`

```csharp
public static Electron2D.Vector2 MapGetRandomPoint(Electron2D.RID map, System.UInt32 navigationLayers, System.Boolean uniformly)
```

#### Summary

Samples an enabled nonempty committed map surface matching the supplied navigation layers.

#### Remarks

Prepared surface groups are reused. Random sequences are not guaranteed across runtime versions.

#### Returns

A world point inside a surface, or zero when no eligible surface exists.

#### Parameters

- `map`: A live map RID.
- `navigationLayers`: Eligible layer mask.
- `uniformly`: True weights every surface level by area; false chooses regions, polygons and triangles uniformly.

<a id="member-597b3e23eca7"></a>
### `MapSetCellSize(Electron2D.RID, System.Single)`

Kind: `method`

```csharp
public static System.Void MapSetCellSize(Electron2D.RID map, System.Single cellSize)
```

#### Summary

Stages the raster cell size, clamping finite values to at least 0.0001 world units.

#### Parameters

- `map`: A live map RID.
- `cellSize`: Finite cell size; one initially.

#### Exceptions

- `T:System.ArgumentException`: The RID is invalid or the value is nonfinite.

<a id="member-8c3d8413f862"></a>
### `MapSetMergeRasterizerCellScale(Electron2D.RID, System.Single)`

Kind: `method`

```csharp
public static System.Void MapSetMergeRasterizerCellScale(Electron2D.RID map, System.Single scale)
```

#### Summary

Stages the raster scale, clamping finite values to 0.0001 through 0.1.

#### Remarks

The effective square cell dimension is CellSize multiplied by this scale.

#### Parameters

- `map`: A live map RID.
- `scale`: Finite raster scale; 0.1 initially.

#### Exceptions

- `T:System.ArgumentException`: The RID is invalid or the value is nonfinite.

<a id="member-10bf2b2a706a"></a>
### `RegionGetConnectionPathwayEnd(Electron2D.RID, System.Int32)`

Kind: `method`

```csharp
public static Electron2D.Vector2 RegionGetConnectionPathwayEnd(Electron2D.RID region, System.Int32 connection)
```

#### Summary

Returns the second endpoint of one published margin pathway.

#### Returns

A world-space endpoint.

#### Parameters

- `region`: A live region RID.
- `connection`: A valid zero-based pathway index.

#### Exceptions

- `T:System.ArgumentOutOfRangeException`: The index does not identify a published pathway.

<a id="member-d910c8246e55"></a>
### `RegionGetConnectionPathwayStart(Electron2D.RID, System.Int32)`

Kind: `method`

```csharp
public static Electron2D.Vector2 RegionGetConnectionPathwayStart(Electron2D.RID region, System.Int32 connection)
```

#### Summary

Returns the first endpoint of one published margin pathway.

#### Returns

A world-space endpoint.

#### Parameters

- `region`: A live region RID.
- `connection`: A valid zero-based pathway index.

#### Exceptions

- `T:System.ArgumentOutOfRangeException`: The index does not identify a published pathway.

<a id="member-b8a5e0281bde"></a>
### `RegionGetConnectionsCount(Electron2D.RID)`

Kind: `method`

```csharp
public static System.Int32 RegionGetConnectionsCount(Electron2D.RID region)
```

#### Summary

Returns the region's committed directed free-edge margin pathway count.

#### Returns

Zero for detached regions or absent pathways; raster pairs and links are excluded.

#### Parameters

- `region`: A live region RID.

<a id="member-19d1ad262fa0"></a>
### `RegionGetIterationID(Electron2D.RID)`

Kind: `method`

```csharp
public static System.UInt64 RegionGetIterationID(Electron2D.RID region)
```

#### Summary

Returns the region's last published nonzero iteration identity, or zero before its first publication.

#### Returns

A 32-bit wrapping identity projected to ulong.

#### Parameters

- `region`: A live region RID.

<a id="member-af260024b43b"></a>
### `RegionGetRandomPoint(Electron2D.RID, System.UInt32, System.Boolean)`

Kind: `method`

```csharp
public static Electron2D.Vector2 RegionGetRandomPoint(Electron2D.RID region, System.UInt32 navigationLayers, System.Boolean uniformly)
```

#### Summary

Samples an enabled committed region surface matching the supplied navigation layers.

#### Remarks

Detached regions retain geometry for this query. Prepared surface groups are reused.

#### Returns

A world point inside the region, or zero when no eligible surface exists.

#### Parameters

- `region`: A live region RID.
- `navigationLayers`: Eligible layer mask.
- `uniformly`: True weights polygons and triangles by area; false chooses both uniformly.
