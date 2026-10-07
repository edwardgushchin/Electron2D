# NavigationObstacle

Last updated: 2026-10-07

- Source: [NavigationObstacle.cs](../../src/Navigation/2D/NavigationObstacle.cs)
- Inherits: [Entity](Entity.md)
- Component: [Navigation avoidance](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance)

## Description

Node-owned moving disc plus oriented static contour in the same ORCA map. Radius predicts motion with Velocity; contour prediction remains static. Public local vertices are copied. Source transforms publish world translation separately from basis-transformed offsets and scale radius with the largest absolute axis clamped to .001. Two contour points form a two-sided wall; winding controls the constrained side of larger convex/concave outlines. Finite simple contours reject duplicates, zero area and intersections before publication. Empty/single-point contours have no static segments.

Map source selection, World replacement, immediate pause detachment/resume restoration, tree exit/reentry and disposal follow the same server obstacle storage. Direct server edits do not rewrite cached source descriptors. Five own source properties persist through fresh-process scene archives; RIDs and map overrides remain runtime state. The RID is borrowed and cannot be freed independently. Bake-affect/carve properties retain their exact geometry parsing/carving producer dependencies in coverage; avoidance is fully executable.

## Example

Requires an attached parent scene with avoidance-enabled agents.

```csharp
var obstacle = new NavigationObstacle
{
    Radius = 8,
    Position = new(64, 48),
    AvoidanceLayers = 1
};
parent.AddChild(obstacle);
// Optional local static wall:
obstacle.Vertices = [new(0, -20), new(0, 20)];
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationObstacle()`](#constructor) | Creates an enabled detached obstacle with zero radius and empty contour. |

## Properties

| Member | Contract |
| --- | --- |
| [`public bool AvoidanceEnabled { get; set; }`](#avoidanceenabled) | Gets or changes participation in avoidance. |
| [`public uint AvoidanceLayers { get; set; }`](#avoidancelayers) | Gets or changes the 32-bit layers visible to agent masks. |
| [`public float Radius { get; set; }`](#radius) | Gets or changes the finite nonnegative source disc radius. |
| [`public Vector2 Velocity { get; set; }`](#velocity) | Gets or changes finite moving-disc velocity; static contour prediction ignores it. |
| [`public Vector2[] Vertices { get; set; }`](#vertices) | Gets or changes a copied finite simple oriented local contour. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Typed inherited lifecycle/discovery hook. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Typed inherited lifecycle/discovery hook. |
| [`public bool GetAvoidanceLayerValue(int layerNumber)`](#getavoidancelayervalue) | Returns an avoidance layer bit using one-based indices. |
| [`public override string[] GetConfigurationWarnings()`](#getconfigurationwarnings) | Typed inherited lifecycle/discovery hook. |
| [`public RID GetNavigationMap()`](#getnavigationmap) | Returns explicit map selection, attached World map, or empty when detached. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Typed inherited lifecycle/discovery hook. |
| [`public RID GetRID()`](#getrid) | Returns the stable node-owned obstacle identity. |
| [`protected override void OnEnterTree()`](#onentertree) | Typed inherited lifecycle/discovery hook. |
| [`protected override void OnExitTree()`](#onexittree) | Typed inherited lifecycle/discovery hook. |
| [`protected override void OnNotification(int what)`](#onnotification) | Typed inherited lifecycle/discovery hook. |
| [`public void SetAvoidanceLayerValue(int layerNumber, bool value)`](#setavoidancelayervalue) | Changes an avoidance layer bit using one-based indices. |
| [`public void SetNavigationMap(RID map)`](#setnavigationmap) | Changes a live override; empty restores the attached World map. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationObstacle()`

Creates an enabled detached obstacle with zero radius and empty contour.

<a id="avoidanceenabled"></a>
### `public bool AvoidanceEnabled { get; set; }`

Gets or changes participation in avoidance. True initially.

`ObjectDisposedException`: The node is disposed.

<a id="avoidancelayers"></a>
### `public uint AvoidanceLayers { get; set; }`

Gets or changes the 32-bit layers visible to agent masks. One initially; independent of direct server edits.

`ObjectDisposedException`: The node is disposed.

<a id="radius"></a>
### `public float Radius { get; set; }`

Gets or changes the finite nonnegative source disc radius. Zero initially; world radius uses the largest absolute scale axis, clamped to .001.

`ArgumentOutOfRangeException`: Radius or transformed radius is negative/nonfinite.

<a id="velocity"></a>
### `public Vector2 Velocity { get; set; }`

Gets or changes finite moving-disc velocity; static contour prediction ignores it. Zero initially; retained source velocity.

`ArgumentException`: Velocity is nonfinite.

<a id="vertices"></a>
### `public Vector2[] Vertices { get; set; }`

Gets or changes a copied finite simple oriented local contour. Empty initially. Two points form a two-sided wall; larger contours preserve their winding.

`ArgumentNullException`: Assigned array is null.

`ArgumentException`: Points are nonfinite, duplicate or form a degenerate/intersecting contour.

<a id="createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Typed inherited lifecycle/discovery hook.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Typed inherited lifecycle/discovery hook.

<a id="getavoidancelayervalue"></a>
### `public bool GetAvoidanceLayerValue(int layerNumber)`

Returns an avoidance layer bit using one-based indices. Whether the bit is set.

`layerNumber`: Index one through thirty-two.

`ArgumentOutOfRangeException`: Index is outside the accepted interval.

<a id="getconfigurationwarnings"></a>
### `public override string[] GetConfigurationWarnings()`

Typed inherited lifecycle/discovery hook.

<a id="getnavigationmap"></a>
### `public RID GetNavigationMap()`

Returns explicit map selection, attached World map, or empty when detached. Source assignment independent of direct server edits.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Typed inherited lifecycle/discovery hook.

<a id="getrid"></a>
### `public RID GetRID()`

Returns the stable node-owned obstacle identity. Borrowed RID valid until disposal; FreeRID rejects it.

<a id="onentertree"></a>
### `protected override void OnEnterTree()`

Typed inherited lifecycle/discovery hook.

<a id="onexittree"></a>
### `protected override void OnExitTree()`

Typed inherited lifecycle/discovery hook.

<a id="onnotification"></a>
### `protected override void OnNotification(int what)`

Typed inherited lifecycle/discovery hook.

<a id="setavoidancelayervalue"></a>
### `public void SetAvoidanceLayerValue(int layerNumber, bool value)`

Changes an avoidance layer bit using one-based indices.

`layerNumber`: Index one through thirty-two.

`value`: New bit state.

`ArgumentOutOfRangeException`: Index is outside the accepted interval.

<a id="setnavigationmap"></a>
### `public void SetNavigationMap(RID map)`

Changes a live override; empty restores the attached World map.

`map`: Live map RID or empty.

`ArgumentException`: Map RID is invalid.

`InvalidOperationException`: The map belongs to another scene tree or mutation is off-owner.

## Verification

[NavigationAvoidanceTests](../../tests/Electron2D.Tests/NavigationAvoidanceTests.cs) verifies masks, pause, transforms, source arrays, native movement and fresh-process reconstruction; the component records allocation and platform limits.
