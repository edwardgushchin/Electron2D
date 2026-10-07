# NavigationPolygon

Last updated: 2026-10-07

- Source: [NavigationPolygon.cs](../../src/Navigation/2D/NavigationPolygon.cs)
- Inherits: [Resource](Resource.md)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Copied planar vertices and authored convex polygon indices, immutable versions, resource duplication and bounded typed storage.

The [navigation contract](../components/navigation-maps.md) defines staged versus committed state, world coordinates and ownership. Static server operations serialize configuration; scene node mutations follow the scene owner thread. Resource arrays and returned paths are copied. The current topology/query profile has explicit coverage limits.

## Example

```csharp
using var polygon = new NavigationPolygon();
polygon.SetVertices([new(0, 0), new(32, 0), new(32, 32), new(0, 32)]);
polygon.AddPolygon([0, 1, 2, 3]);
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationPolygon()`](#constructor) | Creates empty authored navigation geometry. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public void AddPolygon(System.ReadOnlySpan<int> polygon)`](#addpolygon) | Adds one copied convex polygon expressed as local vertex indices. |
| [`public void Clear()`](#clear) | Clears authored vertices and polygons. |
| [`public void ClearPolygons()`](#clearpolygons) | Clears polygons while retaining vertices. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, System.Func<Resource?, Resource?> duplicateSubresource, System.Func<Resource?, Resource?> forceDuplicateSubresource)`](#copycustomstateto) | Shares the immutable geometry version safely with the typed duplicate. |
| [`protected override Resource CreateDuplicateInstance()`](#createduplicateinstance) | Creates an empty typed resource for inherited duplication. |
| [`public int[] GetPolygon(int index)`](#getpolygon) | Returns an independent copy of a polygon's indices. |
| [`public int GetPolygonCount()`](#getpolygoncount) | Returns the number of authored polygons. |
| [`protected override System.Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds typed stored navigation properties or geometry payload to inherited resource/scene schema. |
| [`public Vector2[] GetVertices()`](#getvertices) | Returns an independent copy of the current local-space vertices. |
| [`public void SetVertices(System.ReadOnlySpan<Vector2> vertices)`](#setvertices) | Replaces vertices transactionally while retaining valid existing polygon indices. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationPolygon()`

Creates empty authored navigation geometry.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="addpolygon"></a>
### `public void AddPolygon(System.ReadOnlySpan<int> polygon)`

Adds one copied convex polygon expressed as local vertex indices.

`polygon`: Borrowed authored resource, or null to clear; indexed data is copied by the polygon resource.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="clear"></a>
### `public void Clear()`

Clears authored vertices and polygons.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="clearpolygons"></a>
### `public void ClearPolygons()`

Clears polygons while retaining vertices.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="copycustomstateto"></a>
### `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, System.Func<Resource?, Resource?> duplicateSubresource, System.Func<Resource?, Resource?> forceDuplicateSubresource)`

Shares the immutable geometry version safely with the typed duplicate.

`target`: Inherited typed lifecycle argument. `deep`: Inherited typed lifecycle argument. `subresourceMode`: Inherited typed lifecycle argument. `duplicateSubresource`: Inherited typed lifecycle argument. `forceDuplicateSubresource`: Inherited typed lifecycle argument.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="createduplicateinstance"></a>
### `protected override Resource CreateDuplicateInstance()`

Creates an empty typed resource for inherited duplication.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="getpolygon"></a>
### `public int[] GetPolygon(int index)`

Returns an independent copy of a polygon's indices.

`index`: Zero-based authored polygon index.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="getpolygoncount"></a>
### `public int GetPolygonCount()`

Returns the number of authored polygons.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="getpropertydescriptors"></a>
### `protected override System.Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Adds typed stored navigation properties or geometry payload to inherited resource/scene schema.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="getvertices"></a>
### `public Vector2[] GetVertices()`

Returns an independent copy of the current local-space vertices.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

<a id="setvertices"></a>
### `public void SetVertices(System.ReadOnlySpan<Vector2> vertices)`

Replaces vertices transactionally while retaining valid existing polygon indices.

`vertices`: Finite copied local-space points; retained polygon indices must remain valid.

Local coordinates and indices describe authored convex geometry. Mutations validate before replacing the immutable version and then deliver Changed. Public arrays are independent copies. Disposed access rejects; invalid geometry leaves the previous data intact.

## Verification and limits

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) checks typed resource/scene storage and fresh loading, deferred publication and rollback, geometry/cost/layer routes, lifetime and allocation boundaries. The rendered host follows a real World-map corridor on both current hardware backends. [ADR 0097](../decisions/navigation.md#adr-0097) and the [component contract](../components/navigation-maps.md) state remaining raster, baking, async, link, query and avoidance prerequisites.
