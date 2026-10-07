# NavigationPathQueryResult

Last updated: 2026-10-07

- Source: [NavigationPathQueryResult.cs](../../src/Navigation/2D/NavigationPathQueryResult.cs)
- Declaration: public sealed NavigationPathQueryResult : ElectronObject
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Navigation maps and queries](../components/navigation-maps.md)

## Description

Reusable caller-owned atomic path/length plus selected parallel primitive type, borrowed region/link RID and logical instance ID arrays. Per-object gates serialize scalar writes, copied arrays and settings snapshots/result publication. QueryPath reads one committed map; later map/settings changes do not mutate the in-flight versions. Caller coordinates the lifetime of live RID owners. These ElectronObject types are not Resource archive assets and do not expose manual reference counting.

## Example

Partial consumer: map is a real synchronized map with region/link topology.

```csharp
using var parameters = new NavigationPathQueryParameters
{
    Map = map,
    StartPosition = new(12, 44),
    TargetPosition = new(44, 44),
    PostProcessing = NavigationPathQueryParameters.PathPostProcessing.None
};
using var result = new NavigationPathQueryResult();
NavigationServer.QueryPath(parameters, result);
Vector2[] points = result.Path;
NavigationPathQueryResult.PathSegmentType[] types = result.PathTypes;
// A Link point marks transition entry; the next point is its destination.
```

## Enumerations

| Type | Role |
| --- | --- |
| [PathSegmentType](NavigationPathQueryResult.PathSegmentType.md) | Primitive owning a returned path point. |

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationPathQueryResult()`](#constructor) | Creates an empty result with zero path length. |

## Properties

| Member | Contract |
| --- | --- |
| [`public Vector2[] Path { get; set; }`](#path) | Gets or replaces a copied array of finite world-space path positions. |
| [`public float PathLength { get; set; }`](#pathlength) | Gets or replaces the independently stored world-space path length. |
| [`public ulong[] PathOwnerIDs { get; set; }`](#pathownerids) | Gets or replaces a copied array of logical instance identities associated with path point owners. |
| [`public RID[] PathRIDs { get; set; }`](#pathrids) | Gets or replaces a copied array of region/link RID owners of path points. |
| [`public NavigationPathQueryResult.PathSegmentType[] PathTypes { get; set; }`](#pathtypes) | Gets or replaces a copied array of typed region/link owners of path points. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override void Dispose(bool disposing)`](#dispose) | Releases retained arrays before inherited deterministic disposal. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds complete typed own properties to inherited object discovery. |
| [`public void Reset()`](#reset) | Resets all arrays and length to their initial values. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationPathQueryResult()`

Creates an empty result with zero path length.

<a id="path"></a>
### `public Vector2[] Path { get; set; }`

Gets or replaces a copied array of finite world-space path positions. Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.

`ArgumentNullException`: Assigned array is null.

`ObjectDisposedException`: This result is disposed.

`ArgumentOutOfRangeException`: Positions are nonfinite or point types are unsupported.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathlength"></a>
### `public float PathLength { get; set; }`

Gets or replaces the independently stored world-space path length. Zero initially and after reset; queries compute it after simplification and clipping.

`ArgumentOutOfRangeException`: Value is nonfinite or negative.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathownerids"></a>
### `public ulong[] PathOwnerIDs { get; set; }`

Gets or replaces a copied array of logical instance identities associated with path point owners. Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.

`ArgumentNullException`: Assigned array is null.

`ObjectDisposedException`: This result is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathrids"></a>
### `public RID[] PathRIDs { get; set; }`

Gets or replaces a copied array of region/link RID owners of path points. Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.

`ArgumentNullException`: Assigned array is null.

`ObjectDisposedException`: This result is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathtypes"></a>
### `public NavigationPathQueryResult.PathSegmentType[] PathTypes { get; set; }`

Gets or replaces a copied array of typed region/link owners of path points. Empty initially; metadata arrays are empty when their query flags are disabled. Setters are independent.

`ArgumentNullException`: Assigned array is null.

`ObjectDisposedException`: This result is disposed.

`ArgumentOutOfRangeException`: Positions are nonfinite or point types are unsupported.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Releases retained arrays before inherited deterministic disposal.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Adds complete typed own properties to inherited object discovery.

<a id="reset"></a>
### `public void Reset()`

Resets all arrays and length to their initial values.

`ObjectDisposedException`: This result is disposed.

## Verification and limits

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) checks every query control, copied arrays, enum/flag validation, result transaction/reset, completion failure/nesting and zero-allocation scalar/reset cycles. Its rendered host uses result link/owner metadata to execute one teleport and reach real goal pixels. Arrays, path scratch and topology changes allocate; no whole-frame/native allocation or large-map performance claim follows. [ADR 0097](../decisions/navigation.md#adr-0097) owns typed adaptations and the exact remaining raster/search equivalence on query coverage.

NavigationAgent retains a borrowed result of the same type. Consumer disposal rejects while its agent is alive. Internal immutable snapshots support allocation-free following; public copied setters remain usable and externally replaced paths safely trigger a fresh agent query.

## Borrowed result lifetime extension

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override void ValidateDisposal()`](#validatedisposal) | Typed inherited lifecycle/discovery hook. |

## Member descriptions

<a id="validatedisposal"></a>
### `protected override void ValidateDisposal()`

Typed inherited lifecycle/discovery hook.
