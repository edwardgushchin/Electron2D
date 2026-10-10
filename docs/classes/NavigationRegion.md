# NavigationRegion

Last updated: 2026-10-10

- Source: [NavigationRegion.cs](../../src/Navigation/2D/NavigationRegion.cs)
- Inherits: [Entity](Entity.md)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Scene geometry placement in a World map, stable borrowed RID, layer/cost/enable/edge properties and stored polygon graphs.

The [navigation contract](../components/navigation-maps.md) defines staged versus committed state, world coordinates and ownership. Static server operations serialize configuration; scene node mutations follow the scene owner thread. Resource arrays and returned paths are copied. The current topology/query profile has explicit coverage limits.

## Example

Partial consumer snippet; requires the existing scene/resource variables shown in comments.

```csharp
// Inside an existing SceneTree; polygon contains authored convex geometry.
var region = new NavigationRegion { NavigationPolygon = polygon };
root.AddChild(region);
// The next physics boundary commits map topology.
RID map = region.GetNavigationMap();
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationRegion()`](#constructor) | Creates an enabled detached region with stable borrowed RID identity. |

## Properties

| Member | Contract |
| --- | --- |
| [`public bool Enabled { get; set; }`](#enabled) | Gets or changes whether authored region geometry participates in navigation. |
| [`public System.Single EnterCost { get; set; }`](#entercost) | Gets or changes finite nonnegative cost paid on entry from another region. |
| [`public uint NavigationLayers { get; set; }`](#navigationlayers) | Gets or changes the region's navigation layer bits, initially one. |
| [`public NavigationPolygon? NavigationPolygon { get; set; }`](#navigationpolygon) | Gets or sets the borrowed authored navigation polygon resource. |
| [`public System.Single TravelCost { get; set; }`](#travelcost) | Gets or changes finite nonnegative travel cost per unit of world distance. |
| [`public bool UseEdgeConnections { get; set; }`](#useedgeconnections) | Gets or changes automatic edge connections to other regions. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override System.Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Supplies a fresh typed node factory for scene reconstruction. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Releases scene-owned region RID and subscriptions before inherited disposal. |
| [`public Rect2 GetBounds()`](#getbounds) | Returns committed world-space bounds of this region's current topology. |
| [`public bool GetNavigationLayerValue(int layerNumber)`](#getnavigationlayervalue) | Returns one navigation-layer bit using one-based indices 1 through 32. |
| [`public RID GetNavigationMap()`](#getnavigationmap) | Returns the explicit map override or the selected World's map while attached. |
| [`protected override System.Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds typed stored navigation properties or geometry payload to inherited resource/scene schema. |
| [`public RID GetRID()`](#getrid) | Returns this node's stable borrowed region identity. |
| [`protected override void OnEnterTree()`](#onentertree) | Stages this node in the selected World map. |
| [`protected override void OnExitTree()`](#onexittree) | Detaches map membership while preserving the node RID. |
| [`protected override void OnNotification(int what)`](#onnotification) | World and global transform notifications stage current placement. |
| [`public void SetNavigationLayerValue(int layerNumber, bool value)`](#setnavigationlayervalue) | Changes one navigation-layer bit using one-based indices 1 through 32. |
| [`public void SetNavigationMap(RID map)`](#setnavigationmap) | Sets a validated explicit navigation map, or empty to select the scene World's map. |

## Events

| Member | Contract |
| --- | --- |
| [`public event System.Action? NavigationPolygonChanged`](#navigationpolygonchanged) | Occurs after polygon replacement or an authored resource edit stages server geometry. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationRegion()`

Creates an enabled detached region with stable borrowed RID identity.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="enabled"></a>
### `public bool Enabled { get; set; }`

Gets or changes whether authored region geometry participates in navigation.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="entercost"></a>
### `public System.Single EnterCost { get; set; }`

Gets or changes finite nonnegative cost paid on entry from another region.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="navigationlayers"></a>
### `public uint NavigationLayers { get; set; }`

Gets or changes the region's navigation layer bits, initially one.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="navigationpolygon"></a>
### `public NavigationPolygon? NavigationPolygon { get; set; }`

Gets or sets the borrowed authored navigation polygon resource. Assignment stages a complete geometry version before notifying; null clears it. Resource edits stage later versions without replacing this source property.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="travelcost"></a>
### `public System.Single TravelCost { get; set; }`

Gets or changes finite nonnegative travel cost per unit of world distance.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="useedgeconnections"></a>
### `public bool UseEdgeConnections { get; set; }`

Gets or changes automatic edge connections to other regions.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="createsceneinstancefactory"></a>
### `protected override System.Func<Node> CreateSceneInstanceFactory()`

Supplies a fresh typed node factory for scene reconstruction.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Releases scene-owned region RID and subscriptions before inherited disposal.

`disposing`: Inherited typed lifecycle argument.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="getbounds"></a>
### `public Rect2 GetBounds()`

Returns committed world-space bounds of this region's current topology.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="getnavigationlayervalue"></a>
### `public bool GetNavigationLayerValue(int layerNumber)`

Returns one navigation-layer bit using one-based indices 1 through 32.

`layerNumber`: One-based navigation layer number, from 1 through 32.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="getnavigationmap"></a>
### `public RID GetNavigationMap()`

Returns the explicit map override or the selected World's map while attached.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="getpropertydescriptors"></a>
### `protected override System.Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Adds typed stored navigation properties or geometry payload to inherited resource/scene schema.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="getrid"></a>
### `public RID GetRID()`

Returns this node's stable borrowed region identity.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="onentertree"></a>
### `protected override void OnEnterTree()`

Stages this node in the selected World map.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="onexittree"></a>
### `protected override void OnExitTree()`

Detaches map membership while preserving the node RID.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="onnotification"></a>
### `protected override void OnNotification(int what)`

World and global transform notifications stage current placement.

`what`: Inherited typed lifecycle argument.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="setnavigationlayervalue"></a>
### `public void SetNavigationLayerValue(int layerNumber, bool value)`

Changes one navigation-layer bit using one-based indices 1 through 32.

`layerNumber`: One-based navigation layer number, from 1 through 32. `value`: Whether the selected navigation layer bit is enabled.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="setnavigationmap"></a>
### `public void SetNavigationMap(RID map)`

Sets a validated explicit navigation map, or empty to select the scene World's map.

`map`: Live navigation map RID; empty is accepted only when detaching a region.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

<a id="navigationpolygonchanged"></a>
### `public event System.Action? NavigationPolygonChanged`

Occurs after polygon replacement or an authored resource edit stages server geometry.

The node caches source properties; direct server edits do not mutate those properties. Node mutation follows owner-thread rules, and disposed access throws. The RID belongs to the node; geometry resources are borrowed. Tree entry/world changes stage membership, exit detaches it, and physics synchronization publishes query topology.

## Verification and limits

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) checks typed resource/scene storage and fresh loading, deferred publication and rollback, geometry/cost/layer routes, lifetime and allocation boundaries. The rendered host follows a real World-map corridor on both current hardware backends. [ADR 0097](../decisions/navigation.md#adr-0097) and the [component contract](../components/navigation-maps.md) state remaining baking/source parsing, async, debug/global activation and comprehensive weighted query acceptance prerequisites.

Worker edits of the borrowed polygon stage server geometry immediately, while NavigationPolygonChanged delivery for an attached node returns to the scene owner queue. Queued delivery skips disposed or reparented-to-another-tree nodes.
