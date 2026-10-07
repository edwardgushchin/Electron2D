# NavigationPathQueryParameters

Last updated: 2026-10-07

- Source: [NavigationPathQueryParameters.cs](../../src/Navigation/2D/NavigationPathQueryParameters.cs)
- Declaration: public sealed NavigationPathQueryParameters : ElectronObject
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Navigation maps and queries](../components/navigation-maps.md)

## Description

Reusable caller-owned typed settings: finite world endpoints, map identity, layer/region filters, A-star and output/metadata modes, search/return limits and simplification. Per-object gates serialize scalar writes, copied arrays and settings snapshots/result publication. QueryPath reads one committed map; later map/settings changes do not mutate the in-flight versions. Caller coordinates the lifetime of live RID owners. These ElectronObject types are not Resource archive assets and do not expose manual reference counting.

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
| [PathMetadataFlags](NavigationPathQueryParameters.PathMetadataFlags.md) | Optional parallel point metadata arrays. |
| [PathPostProcessing](NavigationPathQueryParameters.PathPostProcessing.md) | Path corridor output processing modes. |
| [PathfindingAlgorithm](NavigationPathQueryParameters.PathfindingAlgorithm.md) | Supported path search algorithms. |

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationPathQueryParameters()`](#constructor) | Creates default A-star/funnel parameters with all metadata and a 4096 polygon search limit. |

## Properties

| Member | Contract |
| --- | --- |
| [`public NavigationPathQueryParameters.PathfindingAlgorithm Algorithm { get; set; }`](#algorithm) | Gets or changes the supported weighted corridor search algorithm. |
| [`public RID[] ExcludedRegions { get; set; }`](#excludedregions) | Gets or changes the copied excludedregions RID filter array. |
| [`public RID[] IncludedRegions { get; set; }`](#includedregions) | Gets or changes the copied includedregions RID filter array. |
| [`public RID Map { get; set; }`](#map) | Gets or changes the Map RID resolved at query time; empty/unresolved required identities reject the query. |
| [`public NavigationPathQueryParameters.PathMetadataFlags MetadataFlags { get; set; }`](#metadataflags) | Gets or changes the parallel result point metadata selection. |
| [`public uint NavigationLayers { get; set; }`](#navigationlayers) | Gets or changes the 32-bit applicable navigation-layer mask. |
| [`public float PathReturnMaxLength { get; set; }`](#pathreturnmaxlength) | Gets or changes the finite maximum returned world-space path length, clamped to zero; zero disables clipping. |
| [`public float PathReturnMaxRadius { get; set; }`](#pathreturnmaxradius) | Gets or changes the finite returned-path circle radius about its start, clamped to zero; zero disables clipping. |
| [`public float PathSearchMaxDistance { get; set; }`](#pathsearchmaxdistance) | Gets or changes the finite processed-entry distance from projected start, clamped to zero; zero is unlimited. |
| [`public int PathSearchMaxPolygons { get; set; }`](#pathsearchmaxpolygons) | Gets or changes the maximum processed polygon count; zero or negative is unlimited. |
| [`public NavigationPathQueryParameters.PathPostProcessing PostProcessing { get; set; }`](#postprocessing) | Gets or changes the corridor output mode. |
| [`public float SimplifyEpsilon { get; set; }`](#simplifyepsilon) | Gets or changes the finite simplification distance in world units, clamped to zero. |
| [`public bool SimplifyPath { get; set; }`](#simplifypath) | Gets or changes the whether output point decimation is applied. |
| [`public Vector2 StartPosition { get; set; }`](#startposition) | Gets or changes the finite world-space start point. |
| [`public Vector2 TargetPosition { get; set; }`](#targetposition) | Gets or changes the finite world-space requested destination. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override void Dispose(bool disposing)`](#dispose) | Releases retained arrays before inherited deterministic disposal. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds complete typed own properties to inherited object discovery. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationPathQueryParameters()`

Creates default A-star/funnel parameters with all metadata and a 4096 polygon search limit.

<a id="algorithm"></a>
### `public NavigationPathQueryParameters.PathfindingAlgorithm Algorithm { get; set; }`

Gets or changes the supported weighted corridor search algorithm. Default PathfindingAlgorithm.AStar; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="excludedregions"></a>
### `public RID[] ExcludedRegions { get; set; }`

Gets or changes the copied excludedregions RID filter array. Excluded regions override included regions, including either endpoint region of a link. Empty by default; returned arrays are independent copies.

`ArgumentNullException`: Assigned array is null.

`ObjectDisposedException`: This object is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="includedregions"></a>
### `public RID[] IncludedRegions { get; set; }`

Gets or changes the copied includedregions RID filter array. Empty includes every applicable region; otherwise both link endpoint regions must be included. Empty by default; returned arrays are independent copies.

`ArgumentNullException`: Assigned array is null.

`ObjectDisposedException`: This object is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="map"></a>
### `public RID Map { get; set; }`

Gets or changes the Map RID resolved at query time; empty/unresolved required identities reject the query. Default default; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="metadataflags"></a>
### `public NavigationPathQueryParameters.PathMetadataFlags MetadataFlags { get; set; }`

Gets or changes the parallel result point metadata selection. Default PathMetadataFlags.IncludeAll; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="navigationlayers"></a>
### `public uint NavigationLayers { get; set; }`

Gets or changes the 32-bit applicable navigation-layer mask. Default 1u; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathreturnmaxlength"></a>
### `public float PathReturnMaxLength { get; set; }`

Gets or changes the finite maximum returned world-space path length, clamped to zero; zero disables clipping. Default 0f; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathreturnmaxradius"></a>
### `public float PathReturnMaxRadius { get; set; }`

Gets or changes the finite returned-path circle radius about its start, clamped to zero; zero disables clipping. Default 0f; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathsearchmaxdistance"></a>
### `public float PathSearchMaxDistance { get; set; }`

Gets or changes the finite processed-entry distance from projected start, clamped to zero; zero is unlimited. Default 0f; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="pathsearchmaxpolygons"></a>
### `public int PathSearchMaxPolygons { get; set; }`

Gets or changes the maximum processed polygon count; zero or negative is unlimited. Default 4096; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="postprocessing"></a>
### `public NavigationPathQueryParameters.PathPostProcessing PostProcessing { get; set; }`

Gets or changes the corridor output mode. Default PathPostProcessing.CorridorFunnel; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="simplifyepsilon"></a>
### `public float SimplifyEpsilon { get; set; }`

Gets or changes the finite simplification distance in world units, clamped to zero. Default 0f; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="simplifypath"></a>
### `public bool SimplifyPath { get; set; }`

Gets or changes the whether output point decimation is applied. Default false; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="startposition"></a>
### `public Vector2 StartPosition { get; set; }`

Gets or changes the finite world-space start point. Default Vector2.Zero; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="targetposition"></a>
### `public Vector2 TargetPosition { get; set; }`

Gets or changes the finite world-space requested destination. Default Vector2.Zero; captured as one immutable query setting snapshot.

`ObjectDisposedException`: This object is disposed.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Queries capture one settings/map snapshot and publish one complete result before callback. Public arrays are copied; user result property setters are independent. Disposed access rejects. Query geometry uses world coordinates.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Releases retained arrays before inherited deterministic disposal.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Adds complete typed own properties to inherited object discovery.

## Verification and limits

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) checks every query control, copied arrays, enum/flag validation, result transaction/reset, completion failure/nesting and zero-allocation scalar/reset cycles. Its rendered host uses result link/owner metadata to execute one teleport and reach real goal pixels. Arrays, path scratch and topology changes allocate; no whole-frame/native allocation or large-map performance claim follows. [ADR 0097](../decisions/navigation.md#adr-0097) owns typed adaptations and the exact remaining raster/search equivalence on query coverage.
