# NavigationMeshSourceGeometryData

Last updated: 2026-10-10

- Source: [NavigationMeshSourceGeometryData.cs](../../src/Navigation/2D/NavigationMeshSourceGeometryData.cs)
- Inherits: [Resource](Resource.md)
- Component: [Navigation baking](../components/navigation-baking.md)

## Description

Owns copied traversable, obstruction and projected contour state. Getters export independent arrays; projected entries are immutable and export copied vertices. Internal coherent snapshots can be read on workers while later edits replace state. Mutations commit before Changed callbacks. Counts/points are bounded to 1048576; finite coordinates and valid storage are checked. Resource duplicates retain independent logical state and explicit compiled file registration reconstructs its payload. HasData intentionally reports only traversable contour presence; GetBounds includes every family.

## Example

```csharp
using var source = new NavigationMeshSourceGeometryData();
source.AddTraversableOutline([new(0, 0), new(80, 0), new(80, 80), new(0, 80)]);
source.AddProjectedObstruction([new(20, 20), new(30, 20), new(30, 30)], carve: true);
using var polygon = new NavigationPolygon { AgentRadius = 2 };
NavigationServer.BakeFromSourceGeometryData(polygon, source);
```

## API

| Member | Contract |
| --- | --- |
| [`public NavigationMeshSourceGeometryData()`](#member-0) | Creates empty source geometry. |
| [`public void AddObstructionOutline(ReadOnlySpan<Vector2> outline)`](#member-1) | Appends a copied obstruction contour when it has at least two points. |
| [`public void AddProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)`](#member-2) | Appends a projected obstruction. |
| [`public void AddTraversableOutline(ReadOnlySpan<Vector2> outline)`](#member-3) | Appends a copied traversable contour when it has at least two points. |
| [`public void AppendObstructionOutlines(Vector2[][] outlines)`](#member-4) | Appends copied obstruction contours. |
| [`public void AppendTraversableOutlines(Vector2[][] outlines)`](#member-5) | Appends copied traversable contours. |
| [`public void Clear()`](#member-6) | Clears all source contours and projected obstructions. |
| [`public void ClearProjectedObstructions()`](#member-7) | Clears only projected obstructions. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#member-8) | Uses the inherited Resource copy/lifecycle contract. |
| [`protected override Resource CreateDuplicateInstance()`](#member-9) | Uses the inherited Resource copy/lifecycle contract. |
| [`public Rect2 GetBounds()`](#member-10) | Returns the enclosing bounds of every source point. |
| [`public Vector2[][] GetObstructionOutlines()`](#member-11) | Returns copied obstruction contours. |
| [`public NavigationProjectedObstruction[] GetProjectedObstructions()`](#member-12) | Returns immutable projected entries with copied vertex exports. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#member-13) | Uses the inherited Resource copy/lifecycle contract. |
| [`public Vector2[][] GetTraversableOutlines()`](#member-14) | Returns copied traversable contours. |
| [`public bool HasData()`](#member-15) | Reports whether traversable contours are present. |
| [`public void Merge(NavigationMeshSourceGeometryData otherGeometry)`](#member-16) | Appends one coherent snapshot of another source, including self-merge. |
| [`public void SetObstructionOutlines(Vector2[][] outlines)`](#member-17) | Replaces obstruction contours after copying and validation. |
| [`public void SetProjectedObstructions(ReadOnlySpan<NavigationProjectedObstruction> obstructions)`](#member-18) | Replaces projected obstructions with immutable entries. |
| [`public void SetTraversableOutlines(Vector2[][] outlines)`](#member-19) | Replaces traversable contours after copying and validation. |

## Member descriptions

<a id="member-0"></a>
### `public NavigationMeshSourceGeometryData()`

Creates empty source geometry.

<a id="member-1"></a>
### `public void AddObstructionOutline(ReadOnlySpan<Vector2> outline)`

Appends a copied obstruction contour when it has at least two points.

**Parameter outline:** Finite root-local points.

<a id="member-2"></a>
### `public void AddProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)`

Appends a projected obstruction.

**Parameter vertices:** At least two finite points.

**Parameter carve:** Applies after clearance erosion when true.

<a id="member-3"></a>
### `public void AddTraversableOutline(ReadOnlySpan<Vector2> outline)`

Appends a copied traversable contour when it has at least two points.

**Parameter outline:** Finite root-local points.

<a id="member-4"></a>
### `public void AppendObstructionOutlines(Vector2[][] outlines)`

Appends copied obstruction contours.

**Parameter outlines:** Finite point arrays.

<a id="member-5"></a>
### `public void AppendTraversableOutlines(Vector2[][] outlines)`

Appends copied traversable contours.

**Parameter outlines:** Finite point arrays.

<a id="member-6"></a>
### `public void Clear()`

Clears all source contours and projected obstructions.

<a id="member-7"></a>
### `public void ClearProjectedObstructions()`

Clears only projected obstructions.

<a id="member-8"></a>
### `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`



<a id="member-9"></a>
### `protected override Resource CreateDuplicateInstance()`



<a id="member-10"></a>
### `public Rect2 GetBounds()`

Returns the enclosing bounds of every source point.

**Returns:** Zero for empty input.

**Exception System.OverflowException:** The finite coordinate span cannot be represented by Rect2.

<a id="member-11"></a>
### `public Vector2[][] GetObstructionOutlines()`

Returns copied obstruction contours.

**Returns:** Independent arrays.

<a id="member-12"></a>
### `public NavigationProjectedObstruction[] GetProjectedObstructions()`

Returns immutable projected entries with copied vertex exports.

**Returns:** Independent entry array.

<a id="member-13"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`



<a id="member-14"></a>
### `public Vector2[][] GetTraversableOutlines()`

Returns copied traversable contours.

**Returns:** Independent arrays.

<a id="member-15"></a>
### `public bool HasData()`

Reports whether traversable contours are present.

**Returns:** False for obstruction-only geometry.

<a id="member-16"></a>
### `public void Merge(NavigationMeshSourceGeometryData otherGeometry)`

Appends one coherent snapshot of another source, including self-merge.

**Parameter otherGeometry:** Live source resource.

<a id="member-17"></a>
### `public void SetObstructionOutlines(Vector2[][] outlines)`

Replaces obstruction contours after copying and validation.

**Parameter outlines:** Finite point arrays.

<a id="member-18"></a>
### `public void SetProjectedObstructions(ReadOnlySpan<NavigationProjectedObstruction> obstructions)`

Replaces projected obstructions with immutable entries.

**Parameter obstructions:** Entries with independent immutable vertex state.

<a id="member-19"></a>
### `public void SetTraversableOutlines(Vector2[][] outlines)`

Replaces traversable contours after copying and validation.

**Parameter outlines:** Finite point arrays.


## Lifecycle, verification and limits

The [baking component](../components/navigation-baking.md) defines ownership, ordering, finite/Clipper limits, explicit work allocation and actual NavigationBakingTests/BakedRoutes evidence. Background input is immutable; scene traversal/publication follow the owner, and failures release active state. Native/foreign/AOT/browser/editor/human gates remain separate. Inherited public/protected Resource behavior applies. The [ADR 0097 contract](../decisions/navigation.md#adr-0097) governs the typed surface.
