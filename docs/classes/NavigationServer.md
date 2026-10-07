# NavigationServer

Last updated: 2026-10-07

- Source: [NavigationServer.cs](../../src/Servers/Navigation/NavigationServer.cs), [NavigationServer.API.cs](../../src/Servers/Navigation/NavigationServer.API.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Permanent retained navigation service with static map/region lifecycle, staged settings, committed queries and MapChanged publication.

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

## Verification and limits

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) checks typed resource/scene storage and fresh loading, deferred publication and rollback, geometry/cost/layer routes, lifetime and allocation boundaries. The rendered host follows a real World-map corridor on both current hardware backends. [ADR 0097](../decisions/navigation.md#adr-0097) and the [component contract](../components/navigation-maps.md) state remaining raster, baking, async, link, query and avoidance prerequisites.
