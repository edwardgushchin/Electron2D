# NavigationServer

Last updated: 2026-10-07

- Source: [NavigationServer.cs](../../src/Servers/Navigation/NavigationServer.cs), [NavigationServer.API.cs](../../src/Servers/Navigation/NavigationServer.API.cs), [NavigationServer.Links.cs](../../src/Servers/Navigation/NavigationServer.Links.cs), [NavigationServer.Query.cs](../../src/Servers/Navigation/NavigationServer.Query.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Permanent retained navigation service with static map/region/link lifecycle, staged settings, committed queries and MapChanged publication.

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
| [`public static void FreeRID(RID rid)`](#freerid) | Releases a caller-owned map or region; scene/world identities retain their owning lifetime. |
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

Releases a caller-owned map or region; scene/world identities retain their owning lifetime.

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
