# Mesh

Last updated: 2026-10-18

**Namespace:** `Electron2D` · **Declaration:** `public abstract class Electron2D.Mesh` · **Source:** [Mesh.cs](../../src/Scene/Resources/Mesh.cs).

**Inherits:** [Resource](Resource.md).

**Inherited By:** [ArrayMesh](ArrayMesh.md), [ImmediateMesh](ImmediateMesh.md).

## Description

Abstract typed two-dimensional surface contract. Count/array/format/topology/material callbacks project applicable mesh reads. SurfaceGetArrays returns independent typed channel copies; vertex/index counts and policies preserve the current resource state. Mesh.GetRID lazily creates a weak stable resource-owned identity, independent of Engine.Run, until disposal; it is borrowed and cannot be mutated/freed through RenderingServer ownership. Implementations publish Changed after geometry/material edits. Runtime arrays and node/canvas geometry are strictly 2D. GetFaces exposes copied local 2D triangle faces; strips expand with alternating winding, and point/line/UseEmptyVertexArray surfaces contribute nothing. Named target deformation and authored LOD selection execute; compression, automatic simplification and remaining geometry operations retain exact coverage dependencies.

## Example

```csharp
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{ Vertices = [new(0, 0), new(20, 0), new(0, 20)] });
int count = mesh.GetSurfaceCount();
```


## Complete API

| Complete signature | Contract |
| --- | --- |
| `protected Mesh()` | Initializes the reusable mesh resource contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `public Electron2D.PlaceholderMesh CreatePlaceholder()` | Creates a caller-owned surface-free mesh with copied local visibility bounds. |
| `public Electron2D.Rect2 GetAABB()` | Returns the axis-aligned visibility bounds of this two-dimensional mesh. |
| `public System.Int32 GetBlendShapeCount()` | Gets the number of named blend shapes. |
| `public System.String GetBlendShapeName(System.Int32 index)` | Gets one shape's authored name. |
| `public Electron2D.Vector2[] GetFaces()` | Gets copied local vertices for every triangle face. |
| `public override Electron2D.RID GetRID()` | Returns the stable borrowed identity of this mesh. |
| `public System.Int32 GetSurfaceCount()` | Gets the number of currently authored surfaces. |
| `protected virtual Electron2D.Rect2 OnGetAABB()` | Supplies the local two-dimensional visibility rectangle. |
| `protected virtual System.Int32 OnGetBlendShapeCount()` | Supplies the number of named shapes; unshaped meshes return zero. |
| `protected virtual System.String OnGetBlendShapeName(System.Int32 index)` | Supplies a name for an existing shape. |
| `protected abstract System.Int32 OnGetSurfaceCount()` | Supplies the authored surface count. |
| `protected virtual System.Void OnSetBlendShapeName(System.Int32 index, System.String name)` | Renames an existing shape and publishes its resource change. |
| `protected abstract System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` | Supplies the explicit index count. |
| `protected abstract System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` | Supplies a surface's vertex count. |
| `protected abstract Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` | Supplies copied typed arrays for one surface. |
| `protected virtual Electron2D.MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(System.Int32 surfaceIndex)` | Supplies copied shape payloads in the mesh's shape-index order. |
| `protected abstract Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` | Supplies a surface format mask. |
| `protected virtual System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> OnSurfaceGetLODs(System.Int32 surfaceIndex)` | Supplies copied alternative index sets for one surface. |
| `protected abstract Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` | Supplies a surface's borrowed material. |
| `protected abstract Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` | Supplies the primitive ordering policy. |
| `protected abstract System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` | Commits a borrowed surface material and reports its change. |
| `public System.Void SetBlendShapeName(System.Int32 index, System.String name)` | Renames one shape without changing its index or payload. |
| `public System.Int32 SurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` | Gets one surface's explicit index count. |
| `public System.Int32 SurfaceGetArrayLen(System.Int32 surfaceIndex)` | Gets one surface's vertex count. |
| `public Electron2D.MeshSurfaceData SurfaceGetArrays(System.Int32 surfaceIndex)` | Gets independent typed arrays for one surface. |
| `public Electron2D.MeshSurfaceData[] SurfaceGetBlendShapeArrays(System.Int32 surfaceIndex)` | Gets copied position/normal/tangent payloads for all named shapes of one surface. |
| `public Electron2D.Mesh.ArrayFormat SurfaceGetFormat(System.Int32 surfaceIndex)` | Gets one surface's typed channel and policy mask. |
| `public System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> SurfaceGetLODs(System.Int32 surfaceIndex)` | Returns copied local edge-error thresholds and alternative vertex indices. |
| `public Electron2D.Material SurfaceGetMaterial(System.Int32 surfaceIndex)` | Gets a surface's borrowed material. |
| `public Electron2D.Mesh.PrimitiveType SurfaceGetPrimitiveType(System.Int32 surfaceIndex)` | Gets one surface's primitive topology. |
| `public System.Void SurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` | Assigns a borrowed material to one surface. |

## Member descriptions

### Mesh

`protected Mesh()`

summary: Initializes the reusable mesh resource contract.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GetAABB

`public Electron2D.Rect2 GetAABB()`

summary: Returns the axis-aligned visibility bounds of this two-dimensional mesh.

returns: A finite local rectangle, empty without vertices.

remarks: Default bounds include stored vertices from every topology, including unreferenced vertices. Prepared custom surface snapshots are reused; live ArrayMesh positions are read without copied queries.

System.ArgumentException: A custom bounds callback supplies an invalid rectangle.

### GetBlendShapeCount

`public System.Int32 GetBlendShapeCount()`

summary: Gets the number of named blend shapes.

returns: A nonnegative count, zero for an unshaped mesh.

### GetBlendShapeName

`public System.String GetBlendShapeName(System.Int32 index)`

summary: Gets one shape's authored name.

index: Existing zero-based shape index.

returns: A non-null name; empty names are permitted.

### GetFaces

`public Electron2D.Vector2[] GetFaces()`

summary: Gets copied local vertices for every triangle face.

returns: Three vertices per face; point, line and UseEmptyVertexArray surfaces contribute no vertices.

remarks: Indexed and sequential triangles and triangle strips are expanded in surface order. Strip winding alternates to preserve face orientation. The result is caller-owned cold query storage.

System.ObjectDisposedException: The mesh is disposed.

System.ArgumentException: Custom surface arrays are malformed.

### GetRID

`public override Electron2D.RID GetRID()`

summary: Returns the stable borrowed identity of this mesh.

returns: A renderer-independent logical identity valid until resource disposal.

System.ObjectDisposedException: The mesh is disposed.

### GetSurfaceCount

`public System.Int32 GetSurfaceCount()`

summary: Gets the number of currently authored surfaces.

returns: The nonnegative surface count.

System.ObjectDisposedException: The mesh is disposed.

### OnGetAABB

`protected virtual Electron2D.Rect2 OnGetAABB()`

summary: Supplies the local two-dimensional visibility rectangle.

returns: Finite nonnegative local bounds; the default derives them from prepared surface positions.

### OnGetBlendShapeCount

`protected virtual System.Int32 OnGetBlendShapeCount()`

summary: Supplies the number of named shapes; unshaped meshes return zero.

returns: A nonnegative count.

### OnGetBlendShapeName

`protected virtual System.String OnGetBlendShapeName(System.Int32 index)`

summary: Supplies a name for an existing shape.

index: Existing zero-based shape index.

returns: A non-null name.

### OnGetSurfaceCount

`protected abstract System.Int32 OnGetSurfaceCount()`

summary: Supplies the authored surface count.

returns: A nonnegative count.

### OnSetBlendShapeName

`protected virtual System.Void OnSetBlendShapeName(System.Int32 index, System.String name)`

summary: Renames an existing shape and publishes its resource change.

index: Existing zero-based shape index.

name: Non-null authored name.

### OnSurfaceGetArrayIndexLen

`protected abstract System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

summary: Supplies the explicit index count.

surfaceIndex: Existing zero-based index.

returns: The index count.

### OnSurfaceGetArrayLen

`protected abstract System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

summary: Supplies a surface's vertex count.

surfaceIndex: Existing zero-based index.

returns: The vertex count.

### OnSurfaceGetArrays

`protected abstract Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

summary: Supplies copied typed arrays for one surface.

surfaceIndex: Existing zero-based index.

returns: Independent channel arrays.

### OnSurfaceGetBlendShapeArrays

`protected virtual Electron2D.MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(System.Int32 surfaceIndex)`

summary: Supplies copied shape payloads in the mesh's shape-index order.

surfaceIndex: Existing zero-based surface index.

returns: Position/normal/tangent-only payloads, or empty when the mesh has no shapes.

### OnSurfaceGetFormat

`protected abstract Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

summary: Supplies a surface format mask.

surfaceIndex: Existing zero-based index.

returns: The format mask.

### OnSurfaceGetLODs

`protected virtual System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> OnSurfaceGetLODs(System.Int32 surfaceIndex)`

summary: Supplies copied alternative index sets for one surface.

surfaceIndex: Existing zero-based surface index.

returns: Positive finite local thresholds and independent index arrays; empty without LODs.

### OnSurfaceGetMaterial

`protected abstract Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

summary: Supplies a surface's borrowed material.

surfaceIndex: Existing zero-based index.

returns: The borrowed material or null.

### OnSurfaceGetPrimitiveType

`protected abstract Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

summary: Supplies the primitive ordering policy.

surfaceIndex: Existing zero-based index.

returns: The topology.

### OnSurfaceSetMaterial

`protected abstract System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

summary: Commits a borrowed surface material and reports its change.

surfaceIndex: Existing zero-based index.

material: Borrowed live material or null.

### SetBlendShapeName

`public System.Void SetBlendShapeName(System.Int32 index, System.String name)`

summary: Renames one shape without changing its index or payload.

index: Existing zero-based shape index.

name: Non-null authored name.

### SurfaceGetArrayIndexLen

`public System.Int32 SurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

summary: Gets one surface's explicit index count.

surfaceIndex: Existing zero-based surface index.

returns: Zero for sequential vertices; otherwise the index count.

### SurfaceGetArrayLen

`public System.Int32 SurfaceGetArrayLen(System.Int32 surfaceIndex)`

summary: Gets one surface's vertex count.

surfaceIndex: Existing zero-based surface index.

returns: The number of vertex positions.

### SurfaceGetArrays

`public Electron2D.MeshSurfaceData SurfaceGetArrays(System.Int32 surfaceIndex)`

summary: Gets independent typed arrays for one surface.

surfaceIndex: Existing zero-based surface index.

returns: Caller-owned copies of the surface channels.

System.ArgumentOutOfRangeException: The index is outside the mesh.

System.ObjectDisposedException: The mesh is disposed.

### SurfaceGetBlendShapeArrays

`public Electron2D.MeshSurfaceData[] SurfaceGetBlendShapeArrays(System.Int32 surfaceIndex)`

summary: Gets copied position/normal/tangent payloads for all named shapes of one surface.

surfaceIndex: Existing zero-based surface index.

returns: Independent payloads in shape-index order; empty for an unshaped mesh.

### SurfaceGetFormat

`public Electron2D.Mesh.ArrayFormat SurfaceGetFormat(System.Int32 surfaceIndex)`

summary: Gets one surface's typed channel and policy mask.

surfaceIndex: Existing zero-based surface index.

returns: The format mask.

### SurfaceGetLODs

`public System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> SurfaceGetLODs(System.Int32 surfaceIndex)`

summary: Returns copied local edge-error thresholds and alternative vertex indices.

surfaceIndex: Existing zero-based surface index.

returns: Ascending positive finite thresholds mapped to independent index arrays.

remarks: Canvas drawing chooses the largest threshold whose transformed error is at most one framebuffer pixel. Base arrays, bounds and face queries keep the full authored geometry.

System.ArgumentOutOfRangeException: The surface index is outside the mesh.

### SurfaceGetMaterial

`public Electron2D.Material SurfaceGetMaterial(System.Int32 surfaceIndex)`

summary: Gets a surface's borrowed material.

surfaceIndex: Existing zero-based surface index.

returns: The material or null for the drawing item's material.

### SurfaceGetPrimitiveType

`public Electron2D.Mesh.PrimitiveType SurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

summary: Gets one surface's primitive topology.

surfaceIndex: Existing zero-based surface index.

returns: The primitive ordering policy.

### SurfaceSetMaterial

`public System.Void SurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

summary: Assigns a borrowed material to one surface.

surfaceIndex: Existing zero-based surface index.

material: Borrowed live material or null.

System.ObjectDisposedException: The mesh or material is disposed.

## LOD behavior

Authored levels use copied positive local edge-error thresholds and shorter complete index sets over an indexed base. Drawing selects the largest threshold within one framebuffer pixel from the greatest singular value of the full transformed basis. Bounds, arrays and face/index counts preserve full geometry. Morph output and tangent regeneration retain levels. AMS6 persists LODs and targets and reads AMS1–5. See [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).

### CreatePlaceholder

`public Electron2D.PlaceholderMesh CreatePlaceholder()`

Creates a caller-owned surface-free mesh retaining the current visibility bounds. See [missing-resource recovery](../components/resource-files.md#missing-resource-recovery) for strict/tolerant policy, lifetime and actual verification limits.

## Packed surface integration

GetAABB retains geometry-derived bounds for ordinary resources and authored rectangles for packed server surfaces. Packed surface export uses copied buffers through [RenderingServer](RenderingServer.md#packed-mesh-surface-family).

## Procedural mesh integration

Face extraction skips UseEmptyVertexArray surfaces. Custom generated surfaces retain an independent logical count and may override OnGetAABB with authored visibility. Default procedural bounds are empty without recursive callbacks. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).
