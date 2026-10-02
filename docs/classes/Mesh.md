# Mesh

Last updated: 2026-10-02

**Namespace:** `Electron2D` · **Declaration:** `public abstract class Electron2D.Mesh` · **Source:** [Mesh.cs](../../src/Scene/Resources/Mesh.cs).

**Inherits:** [Resource](Resource.md).

## Description

Abstract typed two-dimensional surface contract. Count/array/format/topology/material callbacks project applicable mesh reads. SurfaceGetArrays returns independent typed channel copies; vertex/index counts and policies preserve the current resource state. Mesh.GetRID lazily creates a weak stable resource-owned identity, independent of Engine.Run, until disposal; it is borrowed and cannot be mutated/freed through RenderingServer ownership. Implementations publish Changed after geometry/material edits. Runtime arrays and node/canvas geometry are strictly 2D. GetFaces exposes copied local 2D triangle faces; strips expand with alternating winding, and point/line surfaces contribute nothing. Further source channels/deformation/LOD/geometry are exact coverage dependencies, not nominal callbacks.

## Example

```csharp
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{ Vertices = [new(0, 0), new(20, 0), new(0, 20)] });
int count = mesh.GetSurfaceCount();
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `protected Mesh()` | Initializes the reusable mesh resource contract. |

## Constructors descriptions

### .ctor

`protected Mesh()`

Summary: Initializes the reusable mesh resource contract.


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited contract; see Description. |
| `public Electron2D.Vector2[] GetFaces()` | Gets copied local vertices for every triangle face. |
| `public override Electron2D.RID GetRID()` | Returns the stable borrowed identity of this mesh. |
| `public System.Int32 GetSurfaceCount()` | Gets the number of currently authored surfaces. |
| `protected abstract System.Int32 OnGetSurfaceCount()` | Supplies the authored surface count. |
| `protected abstract System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` | Supplies the explicit index count. |
| `protected abstract System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` | Supplies a surface's vertex count. |
| `protected abstract Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` | Supplies copied typed arrays for one surface. |
| `protected abstract Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` | Supplies a surface format mask. |
| `protected abstract Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` | Supplies a surface's borrowed material. |
| `protected abstract Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` | Supplies the primitive ordering policy. |
| `protected abstract System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` | Commits a borrowed surface material and reports its change. |
| `public System.Int32 SurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` | Gets one surface's explicit index count. |
| `public System.Int32 SurfaceGetArrayLen(System.Int32 surfaceIndex)` | Gets one surface's vertex count. |
| `public Electron2D.MeshSurfaceData SurfaceGetArrays(System.Int32 surfaceIndex)` | Gets independent typed arrays for one surface. |
| `public Electron2D.Mesh.ArrayFormat SurfaceGetFormat(System.Int32 surfaceIndex)` | Gets one surface's typed channel and policy mask. |
| `public Electron2D.Material SurfaceGetMaterial(System.Int32 surfaceIndex)` | Gets a surface's borrowed material. |
| `public Electron2D.Mesh.PrimitiveType SurfaceGetPrimitiveType(System.Int32 surfaceIndex)` | Gets one surface's primitive topology. |
| `public System.Void SurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` | Assigns a borrowed material to one surface. |

## Methods and protected extension points descriptions

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### GetFaces

`public Electron2D.Vector2[] GetFaces()`

Summary: Gets copied local vertices for every triangle face.

Returns: Three vertices per face; point and line surfaces contribute no vertices.

Remarks: Indexed and sequential triangles and triangle strips are expanded in surface order. Strip winding alternates to preserve face orientation. The result is caller-owned cold query storage.

System.ObjectDisposedException: The mesh is disposed.

System.ArgumentException: Custom surface arrays are malformed.


### GetRID

`public override Electron2D.RID GetRID()`

Summary: Returns the stable borrowed identity of this mesh.

Returns: A renderer-independent logical identity valid until resource disposal.

System.ObjectDisposedException: The mesh is disposed.


### GetSurfaceCount

`public System.Int32 GetSurfaceCount()`

Summary: Gets the number of currently authored surfaces.

Returns: The nonnegative surface count.

System.ObjectDisposedException: The mesh is disposed.


### OnGetSurfaceCount

`protected abstract System.Int32 OnGetSurfaceCount()`

Summary: Supplies the authored surface count.

Returns: A nonnegative count.


### OnSurfaceGetArrayIndexLen

`protected abstract System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

Summary: Supplies the explicit index count.

surfaceIndex: Existing zero-based index.

Returns: The index count.


### OnSurfaceGetArrayLen

`protected abstract System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

Summary: Supplies a surface's vertex count.

surfaceIndex: Existing zero-based index.

Returns: The vertex count.


### OnSurfaceGetArrays

`protected abstract Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

Summary: Supplies copied typed arrays for one surface.

surfaceIndex: Existing zero-based index.

Returns: Independent channel arrays.


### OnSurfaceGetFormat

`protected abstract Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

Summary: Supplies a surface format mask.

surfaceIndex: Existing zero-based index.

Returns: The format mask.


### OnSurfaceGetMaterial

`protected abstract Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

Summary: Supplies a surface's borrowed material.

surfaceIndex: Existing zero-based index.

Returns: The borrowed material or null.


### OnSurfaceGetPrimitiveType

`protected abstract Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

Summary: Supplies the primitive ordering policy.

surfaceIndex: Existing zero-based index.

Returns: The topology.


### OnSurfaceSetMaterial

`protected abstract System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

Summary: Commits a borrowed surface material and reports its change.

surfaceIndex: Existing zero-based index.

material: Borrowed live material or null.


### SurfaceGetArrayIndexLen

`public System.Int32 SurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

Summary: Gets one surface's explicit index count.

surfaceIndex: Existing zero-based surface index.

Returns: Zero for sequential vertices; otherwise the index count.


### SurfaceGetArrayLen

`public System.Int32 SurfaceGetArrayLen(System.Int32 surfaceIndex)`

Summary: Gets one surface's vertex count.

surfaceIndex: Existing zero-based surface index.

Returns: The number of vertex positions.


### SurfaceGetArrays

`public Electron2D.MeshSurfaceData SurfaceGetArrays(System.Int32 surfaceIndex)`

Summary: Gets independent typed arrays for one surface.

surfaceIndex: Existing zero-based surface index.

Returns: Caller-owned copies of the surface channels.

System.ArgumentOutOfRangeException: The index is outside the mesh.

System.ObjectDisposedException: The mesh is disposed.


### SurfaceGetFormat

`public Electron2D.Mesh.ArrayFormat SurfaceGetFormat(System.Int32 surfaceIndex)`

Summary: Gets one surface's typed channel and policy mask.

surfaceIndex: Existing zero-based surface index.

Returns: The format mask.


### SurfaceGetMaterial

`public Electron2D.Material SurfaceGetMaterial(System.Int32 surfaceIndex)`

Summary: Gets a surface's borrowed material.

surfaceIndex: Existing zero-based surface index.

Returns: The material or null for the drawing item's material.


### SurfaceGetPrimitiveType

`public Electron2D.Mesh.PrimitiveType SurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

Summary: Gets one surface's primitive topology.

surfaceIndex: Existing zero-based surface index.

Returns: The primitive ordering policy.


### SurfaceSetMaterial

`public System.Void SurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

Summary: Assigns a borrowed material to one surface.

surfaceIndex: Existing zero-based surface index.

material: Borrowed live material or null.

System.ObjectDisposedException: The mesh or material is disposed.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.

Custom OnGetSurfaceCount must return a nonnegative count. Callback preparation rejects a changed revision or disposed resource, then retries on the next replay. Successful snapshots capture surface count and reuse it without callback dispatch on idle frames. The author must publish Changed after edits.
