# Polygon

Last updated: 2026-10-07

- Declaration: `public partial class Polygon : Entity`
- Source: [Polygon.cs](../../src/Scene/2D/Polygon.cs), [skin](../../src/Scene/2D/Polygon.Skin.cs)
- Inherits: [Entity](Entity.md)
- Inherited by: no production type currently
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Skeletal deformation

The [skeletal component](../components/skeletal-animation.md) adds Skeleton, a relative borrowed rig path, and copied bone path/weight authoring. Paths resolve relative to the rig. Records with mismatched source counts or missing bones do not contribute; four strongest positive influences normalize per point and zero sums preserve input. The retained skin uses live/interpolated poses without retriangulating; inversion ignores it. Ordinary forced redraws reuse point, UV, color and contour scratch arrays. Versioned stored bone records restore atomically. Both current native backends execute skin positions while keeping original color/UV/materials and triangulation.

## Properties

| Signature | Contract |
| --- | --- |
| `public System.String Skeleton { get; set; }` | [Gets or sets the scene path of the borrowed skeleton.](#skeleton) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `public System.Void AddBone(System.String path, System.Single[] weights)` | [Appends a bone path and copied per-source-vertex weights.](#addbone) |
| `public System.Void ClearBones()` | [Inherited lifecycle/schema override.](#clearbones) |
| `public System.Void EraseBone(System.Int32 index)` | [Removes an indexed record and compacts later records.](#erasebone) |
| `public System.Int32 GetBoneCount()` | [Inherited lifecycle/schema override.](#getbonecount) |
| `public System.String GetBonePath(System.Int32 index)` | [Returns a record's path relative to the skeleton.](#getbonepath) |
| `public System.Single[] GetBoneWeights(System.Int32 index)` | [Returns an independent copy of a record's weights.](#getboneweights) |
| `public System.Void SetBonePath(System.Int32 index, System.String path)` | [Replaces a record path.](#setbonepath) |
| `public System.Void SetBoneWeights(System.Int32 index, System.Single[] weights)` | [Replaces a record's copied weights.](#setboneweights) |

## Member descriptions

### AddBone

`public System.Void AddBone(System.String path, System.Single[] weights)`

Appends a bone path and copied per-source-vertex weights.

Weights must match the selected source point count to participate; mismatched records remain authored. Entries are bounded to 4096.

- `path`: Path relative to the selected skeleton.
- `weights`: Finite weights; nonpositive values do not contribute. At most four strongest positive influences survive per vertex.

### ClearBones

`public System.Void ClearBones()`

Inherited lifecycle/schema override; see the linked base type.

### EraseBone

`public System.Void EraseBone(System.Int32 index)`

Removes an indexed record and compacts later records.

- `index`: Valid record index.

### GetBoneCount

`public System.Int32 GetBoneCount()`

Inherited lifecycle/schema override; see the linked base type.

### GetBonePath

`public System.String GetBonePath(System.Int32 index)`

Returns a record's path relative to the skeleton.

The authored path.

- `index`: Valid record index.

### GetBoneWeights

`public System.Single[] GetBoneWeights(System.Int32 index)`

Returns an independent copy of a record's weights.

Copied finite weights.

- `index`: Valid record index.

### SetBonePath

`public System.Void SetBonePath(System.Int32 index, System.String path)`

Replaces a record path.

- `index`: Valid record index.
- `path`: Scene path relative to the skeleton.

### SetBoneWeights

`public System.Void SetBoneWeights(System.Int32 index, System.Single[] weights)`

Replaces a record's copied weights.

- `index`: Valid record index.
- `weights`: Finite values; mismatched counts do not contribute.

### Skeleton

`public System.String Skeleton { get; set; }`

Gets or sets the scene path of the borrowed skeleton.

Empty initially. Paths resolve relative to this polygon; missing or detached skeletons leave its geometry undeformed.

Inverted polygons ignore skinning. Bone paths are relative to the selected Skeleton.

## Description

The three concrete [IK solvers](../components/skeletal-animation.md#executable-ik-solvers) now produce live bone transforms consumed by this retained skin path. Native tests display one weighted Polygon per solver on GPU and compatibility; no new public Polygon API or triangle/shader contract is introduced.

Polygon is a spatial scene node that fills a closed local contour. It inherits the tree lifecycle from Node, canvas visibility, material and sampling from CanvasItem, and position, rotation and scale from Entity. `Vertices` is the copied vertex list; the name avoids a C# member/type collision. With no `Polygons` entries, the node draws vertices in order and closes the contour. With entries, each `int[]` selects one independently filled contour by vertex index. Separate contours do not create holes. `InvertEnabled` fills the area between the contour and its bounds expanded by `InvertBorder`, ignoring explicit `Polygons` while enabled.

The node borrows `Texture`, subscribes to its change signal, and retains draw commands until geometry, color, UV, texture or resource content changes. Assigning arrays copies them; getters and `SceneState` reads return independent arrays, including each nested contour. Mutations on an attached node follow the scene owner thread. Disposing Polygon removes its texture subscription and does not dispose the texture.

## Example

```csharp
var shape = new Polygon
{
    Name = "Ground",
    Vertices = [new(0, 0), new(64, 0), new(64, 32), new(0, 32)],
    Color = Colors.Green,
};
window.AddChild(shape);
```

## Properties

| Signature | Default | Drawing effect |
| --- | --- | --- |
| `Vector2[] Vertices { get; set; }` | Empty | Copied finite local vertices. |
| `int InternalVertexCount { get; set; }` | 0 | Omits this many trailing vertices from the default contour; explicit contours can use them. |
| `int[][] Polygons { get; set; }` | Empty | Copied index contours; each with fewer than three indices is skipped. |
| `Color Color { get; set; }` | White | Uniform fill when no complete per-vertex set exists. |
| `Color[] VertexColors { get; set; }` | Empty | Interpolated colors only when the array matches the drawn vertex count. |
| `Texture? Texture { get; set; }` | Null | Borrowed sampled texture. |
| `Vector2[] UV { get; set; }` | Empty | Pixel-space texture coordinates when the array matches the drawn vertex count; otherwise offset local positions are used. |
| `Vector2 Offset { get; set; }` | Zero | Local displacement before drawing and default UV generation. |
| `Vector2 TextureOffset { get; set; }` | Zero | Translation in pixel-space texture coordinates. |
| `float TextureRotation { get; set; }` | 0 | Clockwise texture-coordinate rotation in radians. |
| `Vector2 TextureScale { get; set; }` | One | Per-axis texture-coordinate scale before rotation. |
| `bool InvertEnabled { get; set; }` | False | Fills outside the contour up to its padded bounds. |
| `float InvertBorder { get; set; }` | 100 | Finite padding, in local units, around the contour bounds during inversion. |

## Drawing and errors

At least three selected vertices are required. Positions are offset, then texture UVs are scaled, rotated, translated and divided by positive texture dimensions for normalized sampling. A complete `VertexColors` array replaces uniform `Color`; an incomplete one uses `Color` for every drawn vertex. `Polygons` references the complete vertex list, including internal vertices. Inversion excludes trailing internal vertices and appends a bridge around the expanded bounding rectangle. It ignores explicit index contours and uses the uniform color or UVs generated from its new positions when original per-vertex arrays no longer match. The existing canvas renderer triangulates every contour; self-intersections and non-inverted holes are unsupported. Invalid contour indices or untriangulable contours fail on redraw. Nonfinite input vectors/colors, negative internal counts and disposed textures are rejected when assigned. A nonpositive inversion border can make triangulation fail. A texture with zero or invalid dimensions fails on redraw. Inherited materials and backend capability errors follow CanvasItem.

All thirteen properties participate in typed `PackedScene` capture. Vector, color and nested index arrays are copied at capture, state read and instantiation; borrowed texture resources follow the ordinary scene resource policy. Geometry and texture edits request redraw even when the node is hidden; the next visible frame records current values.

## Missing capabilities

The pinned source stores an antialias flag without reading it in its draw path; Electron2D does not expose an inert flag. Skeleton, bone paths and weights require the absent 2D skeleton and mesh-deformation path. Their exact triggers remain in [coverage](../coverage/classes/Polygon2D.md). Degenerate and self-intersecting inverted contours and cross-platform native rendering beyond the current Linux checks remain unverified.

## Verification

[PolygonTests](../../tests/Electron2D.Tests/PolygonTests.cs) checks geometry, attributes, copied arrays, inverted square and concave contours in both windings, scene storage and errors. [PolygonRenderingTests](../../tests/Electron2D.Tests/PolygonRenderingTests.cs) reads pixels for filled, indexed, textured and inverted shapes across color and inversion-border changes on Linux Wayland compatibility/GPU with HLSL/GLSL materials, and dummy/software compatibility. This is executable pixel evidence, not owner visual acceptance.
