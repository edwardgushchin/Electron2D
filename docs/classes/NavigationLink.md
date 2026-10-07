# NavigationLink

Last updated: 2026-10-07

- Source: [NavigationLink.cs](../../src/Navigation/2D/NavigationLink.cs)
- Declaration: public sealed NavigationLink : Entity
- Inherits: [Entity](Entity.md)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

A directed or bidirectional connection between two authored polygon surfaces. The node owns a stable borrowed server RID and caches seven typed source properties. Tree entry selects the explicit override or World map, exit detaches membership, and disposal releases identity. World/global transform changes stage current placement before the shared physics synchronization boundary. Units are world distance and nonnegative cost multipliers; scene mutation follows the tree owner thread. No automatic jump/teleport game action is supplied by a route.

## Example

Partial scene consumer; existing left/right regions and root are required.

```csharp
var crossing = new NavigationLink
{
    StartPosition = new(18, 12),
    EndPosition = new(38, 12),
    Bidirectional = false
};
root.AddChild(crossing);
// Query after a physics synchronization boundary.
Vector2[] path = NavigationServer.MapGetPath(crossing.GetNavigationMap(),
    new(12, 44), new(44, 44), true);
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationLink()`](#constructor) | Creates an enabled bidirectional detached link with stable node-owned identity. |

## Properties

| Member | Contract |
| --- | --- |
| [`public bool Bidirectional { get; set; }`](#bidirectional) | Gets or stages whether travel is allowed in both endpoint directions. |
| [`public bool Enabled { get; set; }`](#enabled) | Gets or stages whether this link participates in path queries. |
| [`public Vector2 EndPosition { get; set; }`](#endposition) | Gets or changes the local-space end endpoint. |
| [`public float EnterCost { get; set; }`](#entercost) | Gets or stages finite nonnegative link entry cost. |
| [`public uint NavigationLayers { get; set; }`](#navigationlayers) | Gets or stages the 32-bit navigation layer mask. |
| [`public Vector2 StartPosition { get; set; }`](#startposition) | Gets or changes the local-space start endpoint. |
| [`public float TravelCost { get; set; }`](#travelcost) | Gets or stages finite nonnegative distance multiplier along the link. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Creates fresh typed link nodes for scene reconstruction. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Releases node-owned link RID before inherited disposal. |
| [`public override string[] GetConfigurationWarnings()`](#getconfigurationwarnings) | Includes inherited warnings and warns when local endpoints are approximately coincident. |
| [`public Vector2 GetGlobalEndPosition()`](#getglobalendposition) | Returns the end endpoint in world coordinates while attached and local coordinates while detached. |
| [`public Vector2 GetGlobalStartPosition()`](#getglobalstartposition) | Returns the start endpoint in world coordinates while attached and local coordinates while detached. |
| [`public bool GetNavigationLayerValue(int layerNumber)`](#getnavigationlayervalue) | Returns a navigation layer bit using one-based indices one through thirty-two. |
| [`public RID GetNavigationMap()`](#getnavigationmap) | Returns the explicit map override, selected World's map while attached, or empty when detached. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds seven typed stored link source properties to inherited scene schema. |
| [`public RID GetRID()`](#getrid) | Returns the stable borrowed link RID. |
| [`protected override void OnEnterTree()`](#onentertree) | Stages membership and attached global endpoints in the selected map. |
| [`protected override void OnExitTree()`](#onexittree) | Detaches membership while retaining node-owned identity. |
| [`protected override void OnNotification(int what)`](#onnotification) | World/transform notifications stage the selected map and both transformed endpoints. |
| [`public void SetGlobalEndPosition(Vector2 position)`](#setglobalendposition) | Changes the local end endpoint using an attached world coordinate or detached local coordinate. |
| [`public void SetGlobalStartPosition(Vector2 position)`](#setglobalstartposition) | Changes the local start endpoint using an attached world coordinate or detached local coordinate. |
| [`public void SetNavigationLayerValue(int layerNumber, bool value)`](#setnavigationlayervalue) | Changes a navigation layer bit using one-based indices one through thirty-two. |
| [`public void SetNavigationMap(RID map)`](#setnavigationmap) | Stages an explicit map override; empty restores the selected World map. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationLink()`

Creates an enabled bidirectional detached link with stable node-owned identity.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="bidirectional"></a>
### `public bool Bidirectional { get; set; }`

Gets or stages whether travel is allowed in both endpoint directions. Source value, initially true; direct server edits do not change it.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="enabled"></a>
### `public bool Enabled { get; set; }`

Gets or stages whether this link participates in path queries. Source value, initially true; direct server edits do not change it.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="endposition"></a>
### `public Vector2 EndPosition { get; set; }`

Gets or changes the local-space end endpoint. Finite local coordinate, initially zero. Detached edits remain source state until attachment.

`ArgumentException`: The point or its attached world transformation is nonfinite.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="entercost"></a>
### `public float EnterCost { get; set; }`

Gets or stages finite nonnegative link entry cost. Source value, initially 0f; direct server edits do not change it.

`ObjectDisposedException`: The node is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or negative.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="navigationlayers"></a>
### `public uint NavigationLayers { get; set; }`

Gets or stages the 32-bit navigation layer mask. Source value, initially 1u; direct server edits do not change it.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="startposition"></a>
### `public Vector2 StartPosition { get; set; }`

Gets or changes the local-space start endpoint. Finite local coordinate, initially zero. Detached edits remain source state until attachment.

`ArgumentException`: The point or its attached world transformation is nonfinite.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="travelcost"></a>
### `public float TravelCost { get; set; }`

Gets or stages finite nonnegative distance multiplier along the link. Source value, initially 1f; direct server edits do not change it.

`ObjectDisposedException`: The node is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or negative.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Creates fresh typed link nodes for scene reconstruction.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Releases node-owned link RID before inherited disposal.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getconfigurationwarnings"></a>
### `public override string[] GetConfigurationWarnings()`

Includes inherited warnings and warns when local endpoints are approximately coincident.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getglobalendposition"></a>
### `public Vector2 GetGlobalEndPosition()`

Returns the end endpoint in world coordinates while attached and local coordinates while detached. Transformed attached endpoint or detached source coordinate.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getglobalstartposition"></a>
### `public Vector2 GetGlobalStartPosition()`

Returns the start endpoint in world coordinates while attached and local coordinates while detached. Transformed attached endpoint or detached source coordinate.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getnavigationlayervalue"></a>
### `public bool GetNavigationLayerValue(int layerNumber)`

Returns a navigation layer bit using one-based indices one through thirty-two. Whether the source mask contains the bit.

`layerNumber`: Navigation layer index from one through thirty-two.

`ArgumentOutOfRangeException`: The index is outside the accepted interval.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getnavigationmap"></a>
### `public RID GetNavigationMap()`

Returns the explicit map override, selected World's map while attached, or empty when detached. Live source assignment; runtime map RIDs are not serialized.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Adds seven typed stored link source properties to inherited scene schema.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="getrid"></a>
### `public RID GetRID()`

Returns the stable borrowed link RID. Identity valid until node disposal; consumer FreeRID rejects it.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="onentertree"></a>
### `protected override void OnEnterTree()`

Stages membership and attached global endpoints in the selected map.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="onexittree"></a>
### `protected override void OnExitTree()`

Detaches membership while retaining node-owned identity.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="onnotification"></a>
### `protected override void OnNotification(int what)`

World/transform notifications stage the selected map and both transformed endpoints.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="setglobalendposition"></a>
### `public void SetGlobalEndPosition(Vector2 position)`

Changes the local end endpoint using an attached world coordinate or detached local coordinate.

`position`: Finite point in the current global endpoint role.

`InvalidOperationException`: An attached inverse transform is singular or the caller is off the tree owner.

`ArgumentException`: The point is nonfinite.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="setglobalstartposition"></a>
### `public void SetGlobalStartPosition(Vector2 position)`

Changes the local start endpoint using an attached world coordinate or detached local coordinate.

`position`: Finite point in the current global endpoint role.

`InvalidOperationException`: An attached inverse transform is singular or the caller is off the tree owner.

`ArgumentException`: The point is nonfinite.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="setnavigationlayervalue"></a>
### `public void SetNavigationLayerValue(int layerNumber, bool value)`

Changes a navigation layer bit using one-based indices one through thirty-two.

`layerNumber`: Navigation layer index from one through thirty-two.

`value`: New bit state.

`ArgumentOutOfRangeException`: The index is outside the accepted interval.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

<a id="setnavigationmap"></a>
### `public void SetNavigationMap(RID map)`

Stages an explicit map override; empty restores the selected World map.

`map`: Live map RID or empty.

`ArgumentException`: A nonempty RID is absent, stale or of another kind.

`ObjectDisposedException`: The node is disposed.

Detached global endpoint methods use local source coordinates; attached endpoint methods use the Entity global transform. Source values cache independently of direct server writes. Mutations follow scene owner-thread/lifetime checks, and changed settings stage map topology for synchronization. Node-owned RID rejects caller FreeRID.

## Verification and limits

[NavigationLinkTests](../../tests/Electron2D.Tests/NavigationLinkTests.cs) checks direction, layers, strict radius, costs, lifecycle, transform roles, warnings, schema/fresh loading, counter rollback/wrap and allocation boundaries. Its real rendered host follows a linked corridor across separated regions on current hardware backends. Full map raster/search equivalence and typed query metadata remain the [map/query profile](../components/navigation-maps.md) dependencies under [ADR 0097](../decisions/navigation.md#adr-0097); native editor debug producers remain separate tooling work.
