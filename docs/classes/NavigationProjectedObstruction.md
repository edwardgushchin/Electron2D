# NavigationProjectedObstruction

Last updated: 2026-10-10

- Source: [NavigationMeshSourceGeometryData.cs](../../src/Navigation/2D/NavigationMeshSourceGeometryData.cs)
- Component: [Navigation baking](../components/navigation-baking.md)

## Description

Immutable typed projected contour. Construction copies finite vertices; at least two are required. A two-point contour has no filled bake area. Carve bypasses agent-radius expansion by applying after erosion. The containing source owns an immutable entry reference; exports cannot mutate source state. This value has no native/resource disposal lifecycle.

## Example

```csharp
var projected = new NavigationProjectedObstruction(
    [new(10, 10), new(20, 10), new(20, 20)], carve: true);
using var source = new NavigationMeshSourceGeometryData();
source.SetProjectedObstructions([projected]);
```

## API

| Member | Contract |
| --- | --- |
| [`public NavigationProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)`](#member-0) | Copies at least two finite points. |
| [`public Vector2[] GetVertices()`](#member-1) | Returns an independent copy of the contour. |
| [`public bool Carve { get;  }`](#member-2) | Gets whether this contour bypasses agent-radius expansion. |

## Member descriptions

<a id="member-0"></a>
### `public NavigationProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)`

Copies at least two finite points.

**Parameter vertices:** Root-local contour.

**Parameter carve:** True excludes the contour after clearance erosion.

<a id="member-1"></a>
### `public Vector2[] GetVertices()`

Returns an independent copy of the contour.

**Returns:** Finite root-local vertices.

<a id="member-2"></a>
### `public bool Carve { get;  }`

Gets whether this contour bypasses agent-radius expansion.

**Value:** Immutable policy.


## Lifecycle, verification and limits

The [baking component](../components/navigation-baking.md) defines ownership, ordering, finite/Clipper limits, explicit work allocation and actual NavigationBakingTests/BakedRoutes evidence. Background input is immutable; scene traversal/publication follow the owner, and failures release active state. Native/foreign/AOT/browser/editor/human gates remain separate. The [ADR 0097 contract](../decisions/navigation.md#adr-0097) governs the typed surface.
