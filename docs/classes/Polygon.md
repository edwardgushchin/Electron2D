# Polygon

Last updated: 2026-11-15

- Declaration: `public partial class Polygon : Entity`
- Source: [Polygon.cs](../../src/Scene/2D/Polygon.cs), [skin](../../src/Scene/2D/Polygon.Skin.cs)
- Inherits: [Entity](Entity.md)
- Inherited by: no production type currently
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Physical skeletal integration

The existing weighted palette now consumes actual PhysicsBody-driven bone transforms through PhysicalBone and SkeletonModificationPhysicalBones. No separate skin render pass is introduced.

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
shape.Antialiased = true;
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
| `Vector2 TextureOffset { get; set; }` | Zero | Pixel-space translation after rotation and before scaling. |
| `float TextureRotation { get; set; }` | 0 | Clockwise texture-coordinate rotation in radians. |
| `Vector2 TextureScale { get; set; }` | One | Per-axis scale of the rotated and translated coordinates. |
| `bool Antialiased { get; set; }` | False | One-framebuffer-pixel transparent boundary fringe. |
| `bool InvertEnabled { get; set; }` | False | Fills outside the contour up to its padded bounds. |
| `float InvertBorder { get; set; }` | 100 | Finite padding, in local units, around the contour bounds during inversion. |


The `Antialiased` property is described [below](#antialiased).

## Drawing and errors

Fewer than three selected vertices produce an empty drawing. `Polygons` indexes the complete vertex list, including internal vertices. A complete `VertexColors` array replaces uniform `Color`; any other count uses `Color` for every drawn vertex. A complete `UV` array supplies pixel coordinates. Any other count uses the drawn local positions, including `Offset`. The UV transform rotates the source coordinates, adds `TextureOffset`, scales both axes by `TextureScale`, then divides by the texture dimensions. An `AtlasTexture` supplies the logical dimensions and the underlying sampled image. UVs address that full image. Sampling multiplies the result by the selected fill colors.

Inversion excludes trailing internal vertices and uses one contour, with a seven-point bridge to its expanded bounding rectangle. Explicit indices and skeletal deformation are ignored. The color and UV count checks include the seven inserted vertices. Ordinary and inverted redraws reuse the prepared vertex buffers. Skinning rebuilds its weight selection when the source count changes, including when an explicit contour selects the same indices from a longer source array.

Invalid contour indices and failed triangulation raise exceptions on redraw. Inverted drawing also requires a contour with nonzero signed area and padding large enough to expand each bound. Self-intersections and ordinary contour holes are unsupported. Nonfinite coordinates and colors, negative indices or internal counts, and disposed textures are rejected when assigned. A texture with invalid dimensions or a disposed assigned texture fails on recording. Materials, atlas sampling and backend capability errors follow CanvasItem.

All fourteen geometry properties participate in typed `PackedScene` capture. Vector, color and nested index arrays are copied at capture, state read and instantiation; borrowed texture resources follow the ordinary scene resource policy. Geometry and texture edits request redraw even when the node is hidden; the next visible frame records current values.

## Antialiased

```csharp
public bool Antialiased { get; set; }
```

False initially. True adds a one-framebuffer-pixel alpha fringe after ordinary skinning, transforms and vertex snapping. Interior fill remains unchanged. Shared edges in explicit contours cancel; reversed winding and reflected transforms preserve the outside. Inverted polygons smooth their inner hole and outer padded rectangle, excluding the numerical triangulation slit. Joins are limited to four framebuffer pixels. Texture coordinates and RGB extend from boundary vertices; texture and vertex alpha still multiply coverage.

The typed property stores through scene capture and resource files, requests redraw, and rejects disposed/off-owner mutation. Both current canvas backends consume the same triangles. HDR/MSAA use their existing capability gates. Authored vertex shaders run after this geometry and may alter its width; custom fragments must retain color alpha for smoothing. PremultAlpha fades RGB together with alpha; multiply blending explicitly reject submission because they ignore coverage alpha. This is an outward coverage fringe, not exact multisample area integration. Self-intersecting/deformed crossing contours retain their unsupported topology boundary.

## Verification

[PolygonTests](../../tests/Electron2D.Tests/PolygonTests.cs) covers defaults, array copies, the UV transform and fallback, square and concave inversion in both windings, errors and recovery, typed scene archives and prepared inverted redraw allocation. [PolygonRenderingTests](../../tests/Electron2D.Tests/PolygonRenderingTests.cs) uses public properties and frame readback to verify changes to the fill, inversion bounds and UV transform, live image updates and resizing, atlas sampling and texture removal, and the skin source count. It also measures prepared inversion redraw and skin presentation on the render owner thread.

On Windows x64, the node scenario passes on Compatibility/D3D11, GPU/D3D12 with the default and authored HLSL and GLSL shaders, and Compatibility/software. Each profile verifies zero managed owner-thread bytes over 128 prepared redraws. Platform qualification for the changed behavior follows ADR 0021.

[PolygonAntialiasTests](../../tests/Electron2D.Tests/PolygonAntialiasTests.cs) exercises boundary cancellation, inversion, transformed width, persistence, native coverage and prepared managed allocation. Physical GPU, native allocator, foreign platforms and human visual acceptance remain separate.
