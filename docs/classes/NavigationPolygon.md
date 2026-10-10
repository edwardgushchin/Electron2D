# NavigationPolygon

Last updated: 2026-10-10

- Source: [NavigationPolygon.cs](../../src/Navigation/2D/NavigationPolygon.cs)
- Inherits: [Resource](Resource.md)
- Component: [Navigation baking](../components/navigation-baking.md)

## Description

Owns copied outlines/configuration and immutable authored or baked planar geometry. GetVertices/GetPolygon/GetOutline export independent arrays. Cold edits and parsing/baking may allocate; settings/array snapshots serialize state and Changed follows commitment. Configuration and private geometry have explicit portable schemas, including read support for earlier geometry payloads. Generated CellSize metadata participates in map compatibility; manual topology retains its established raster behavior.

## Example

```csharp
using var polygon = new NavigationPolygon { AgentRadius = 2 };
polygon.AddOutline([new(0, 0), new(100, 0), new(100, 100), new(0, 100)]);
using var source = new NavigationMeshSourceGeometryData();
source.AddObstructionOutline([new(40, 40), new(60, 40), new(60, 60), new(40, 60)]);
NavigationServer.BakeFromSourceGeometryData(polygon, source);
```

## API

| Member | Contract |
| --- | --- |
| [`public NavigationPolygon()`](#member-0) | Creates empty authored navigation geometry. |
| [`public void AddOutline(ReadOnlySpan<Vector2> outline)`](#member-1) | Appends a copied finite outline. |
| [`public void AddOutlineAtIndex(ReadOnlySpan<Vector2> outline, int index)`](#member-2) | Inserts a copied outline. |
| [`public void AddPolygon(ReadOnlySpan<int> polygon)`](#member-3) | Adds one copied convex polygon expressed as local vertex indices. |
| [`public void Clear()`](#member-4) | Clears authored vertices and polygons. |
| [`public void ClearOutlines()`](#member-5) | Clears outlines without altering the current authored or baked geometry. |
| [`public void ClearPolygons()`](#member-6) | Clears polygons while retaining vertices. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#member-7) | Uses the inherited Resource copy/lifecycle contract. |
| [`protected override Resource CreateDuplicateInstance()`](#member-8) | Uses the inherited Resource copy/lifecycle contract. |
| [`public Vector2[] GetOutline(int index)`](#member-9) | Returns an independent outline. |
| [`public int GetOutlineCount()`](#member-10) | Returns the authored outline count. |
| [`public bool GetParsedCollisionMaskValue(int layerNumber)`](#member-11) | Reads one of the 32 parsed collision-layer bits. |
| [`public int[] GetPolygon(int index)`](#member-12) | Returns an independent copy of a polygon's indices. |
| [`public int GetPolygonCount()`](#member-13) | Returns the number of authored polygons. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#member-14) | Uses the inherited Resource copy/lifecycle contract. |
| [`public Vector2[] GetVertices()`](#member-15) | Returns an independent copy of the current local-space vertices. |
| [`public void RemoveOutline(int index)`](#member-16) | Removes an existing outline. |
| [`public void SetOutline(int index, ReadOnlySpan<Vector2> outline)`](#member-17) | Replaces one copied outline. |
| [`public void SetParsedCollisionMaskValue(int layerNumber, bool value)`](#member-18) | Changes one parsed collision-layer bit. |
| [`public void SetVertices(ReadOnlySpan<Vector2> vertices)`](#member-19) | Replaces vertices transactionally while retaining valid existing polygon indices. |
| [`public float AgentRadius { get; set; }`](#member-20) | Gets or changes the clearance erosion distance around traversable boundaries and ordinary obstructions. |
| [`public Rect2 BakingRect { get; set; }`](#member-21) | Gets or changes the optional positive-area rectangle restricting the bake. |
| [`public Vector2 BakingRectOffset { get; set; }`](#member-22) | Gets or changes the offset applied to the baking rectangle. |
| [`public float BorderSize { get; set; }`](#member-23) | Gets or changes the nonwalkable border inside an enabled baking rectangle. |
| [`public float CellSize { get; set; }`](#member-24) | Gets or changes the positive cell size required by maps consuming generated geometry. |
| [`public uint ParsedCollisionMask { get; set; }`](#member-25) | Gets or changes the 32-bit static collision-layer selection mask. |
| [`public NavigationPolygon.ParsedGeometryType ParsedGeometry { get; set; }`](#member-26) | Gets or changes the scene geometry family to collect. |
| [`public NavigationPolygon.SamplePartitionType SamplePartition { get; set; }`](#member-27) | Gets or changes the convex polygon or triangle partitioning of the walkable surface. |
| [`public NavigationPolygon.SourceGeometryMode SourceGeometry { get; set; }`](#member-28) | Gets or changes the root or group-based scene traversal mode. |
| [`public string SourceGeometryGroupName { get; set; }`](#member-29) | Gets or changes the group name used by group traversal modes. |

## Member descriptions

<a id="member-0"></a>
### `public NavigationPolygon()`

Creates empty authored navigation geometry.

<a id="member-1"></a>
### `public void AddOutline(ReadOnlySpan<Vector2> outline)`

Appends a copied finite outline.

**Parameter outline:** Local-space contour, including empty authoring state.

<a id="member-2"></a>
### `public void AddOutlineAtIndex(ReadOnlySpan<Vector2> outline, int index)`

Inserts a copied outline.

**Parameter outline:** Finite local-space contour.

**Parameter index:** Insertion index from zero through count.

<a id="member-3"></a>
### `public void AddPolygon(ReadOnlySpan<int> polygon)`

Adds one copied convex polygon expressed as local vertex indices.

**Parameter polygon:** At least three distinct valid indices, in either consistent winding.

**Exception System.ArgumentException:** Indices, area or convexity are invalid.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-4"></a>
### `public void Clear()`

Clears authored vertices and polygons.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-5"></a>
### `public void ClearOutlines()`

Clears outlines without altering the current authored or baked geometry.

<a id="member-6"></a>
### `public void ClearPolygons()`

Clears polygons while retaining vertices.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-7"></a>
### `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`



<a id="member-8"></a>
### `protected override Resource CreateDuplicateInstance()`



<a id="member-9"></a>
### `public Vector2[] GetOutline(int index)`

Returns an independent outline.

**Parameter index:** Existing outline index.

**Returns:** Copied finite points.

<a id="member-10"></a>
### `public int GetOutlineCount()`

Returns the authored outline count.

**Returns:** Number of outlines.

<a id="member-11"></a>
### `public bool GetParsedCollisionMaskValue(int layerNumber)`

Reads one of the 32 parsed collision-layer bits.

**Parameter layerNumber:** One-based layer from 1 to 32.

**Returns:** The bit state.

<a id="member-12"></a>
### `public int[] GetPolygon(int index)`

Returns an independent copy of a polygon's indices.

**Parameter index:** Zero-based authored polygon index.

**Returns:** Returns an independent copy of a polygon's indices.

**Exception System.ArgumentOutOfRangeException:** The index is invalid.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-13"></a>
### `public int GetPolygonCount()`

Returns the number of authored polygons.

**Returns:** Returns the number of authored polygons.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-14"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`



<a id="member-15"></a>
### `public Vector2[] GetVertices()`

Returns an independent copy of the current local-space vertices.

**Returns:** Returns an independent copy of the current local-space vertices.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-16"></a>
### `public void RemoveOutline(int index)`

Removes an existing outline.

**Parameter index:** Existing outline index.

<a id="member-17"></a>
### `public void SetOutline(int index, ReadOnlySpan<Vector2> outline)`

Replaces one copied outline.

**Parameter index:** Existing outline index.

**Parameter outline:** Finite local-space contour.

<a id="member-18"></a>
### `public void SetParsedCollisionMaskValue(int layerNumber, bool value)`

Changes one parsed collision-layer bit.

**Parameter layerNumber:** One-based layer from 1 to 32.

**Parameter value:** Enabled state.

<a id="member-19"></a>
### `public void SetVertices(ReadOnlySpan<Vector2> vertices)`

Replaces vertices transactionally while retaining valid existing polygon indices.

**Parameter vertices:** Finite local-space points copied by this resource.

**Exception System.ArgumentException:** Points or retained polygon geometry are invalid.

**Exception System.ObjectDisposedException:** This instance is disposed.

<a id="member-20"></a>
### `public float AgentRadius { get; set; }`

Gets or changes the clearance erosion distance around traversable boundaries and ordinary obstructions.

**Value:** Initially 10f.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-21"></a>
### `public Rect2 BakingRect { get; set; }`

Gets or changes the optional positive-area rectangle restricting the bake.

**Value:** Initially default.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-22"></a>
### `public Vector2 BakingRectOffset { get; set; }`

Gets or changes the offset applied to the baking rectangle.

**Value:** Initially default.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-23"></a>
### `public float BorderSize { get; set; }`

Gets or changes the nonwalkable border inside an enabled baking rectangle.

**Value:** Initially 0f.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-24"></a>
### `public float CellSize { get; set; }`

Gets or changes the positive cell size required by maps consuming generated geometry.

**Value:** Initially 1f.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-25"></a>
### `public uint ParsedCollisionMask { get; set; }`

Gets or changes the 32-bit static collision-layer selection mask.

**Value:** Initially uint.MaxValue.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-26"></a>
### `public NavigationPolygon.ParsedGeometryType ParsedGeometry { get; set; }`

Gets or changes the scene geometry family to collect.

**Value:** Initially ParsedGeometryType.Both.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-27"></a>
### `public NavigationPolygon.SamplePartitionType SamplePartition { get; set; }`

Gets or changes the convex polygon or triangle partitioning of the walkable surface.

**Value:** Initially SamplePartitionType.ConvexPartition.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-28"></a>
### `public NavigationPolygon.SourceGeometryMode SourceGeometry { get; set; }`

Gets or changes the root or group-based scene traversal mode.

**Value:** Initially SourceGeometryMode.RootNodeChildren.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.

<a id="member-29"></a>
### `public string SourceGeometryGroupName { get; set; }`

Gets or changes the group name used by group traversal modes.

**Value:** Initially navigation_polygon_source_geometry_group.

**Exception System.ArgumentOutOfRangeException:** The value is outside its finite supported domain.


## Lifecycle, verification and limits

The [baking component](../components/navigation-baking.md) defines ownership, ordering, finite/Clipper limits, explicit work allocation and actual NavigationBakingTests/BakedRoutes evidence. Background input is immutable; scene traversal/publication follow the owner, and failures release active state. Native/foreign/AOT/browser/editor/human gates remain separate. Inherited public/protected Resource behavior applies. The [ADR 0097 contract](../decisions/navigation.md#adr-0097) governs the typed surface.

Nested enum identities are [ParsedGeometryType](NavigationPolygon.ParsedGeometryType.md), [SamplePartitionType](NavigationPolygon.SamplePartitionType.md) and [SourceGeometryMode](NavigationPolygon.SourceGeometryMode.md). The cross-3D mesh bridge and deprecated outline generation stay in coverage as excluded counterparts.
