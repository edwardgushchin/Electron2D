# ArrayMesh

Last updated: 2026-10-02

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.ArrayMesh` · **Source:** [ArrayMesh.cs](../../src/Scene/Resources/ArrayMesh.cs).

**Inherits:** [Mesh](Mesh.md).

## Description

Concrete copied surface storage. AddSurfaceFromArrays accepts typed Vertices/Colors/UVs/Indices, optional empty deformation/LOD inputs and supported flags. Every channel is copied and validated before adding; vertex/color/UV counts and index bounds must match topology. All five topologies execute. Empty or malformed vertex payloads reject. Colors clamp/truncate to RGBA8 UNORM on import; source getters expose decoded quantized colors, preserving the exercised pinned storage precision. Position/UV remain float pairs. The new index is the previous GetSurfaceCount. Surfaces default to an empty name and null borrowed material; duplicates names are allowed and search returns the first exact ordinal match. Remove shifts later indices, Clear removes all; borrowed materials remain live. Every edit commits before Changed, including equal name/material writes; listener failure does not undo valid data.

## Example

```csharp
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{ Vertices = [new(0, 0), new(20, 0), new(0, 20)] }, flags: Mesh.ArrayFormat.UseDynamicUpdate);
var node = new MeshInstance { Mesh = mesh };
// Attach node to a Window; retain mesh until the scene is disposed.
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public ArrayMesh()` | Creates an empty mesh. |

## Constructors descriptions

### .ctor

`public ArrayMesh()`

Summary: Creates an empty mesh.


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)` | Adds one copied two-dimensional surface. |
| `public System.Void ClearSurfaces()` | Removes every surface without disposing borrowed materials. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Concrete inherited contract; see Description. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Concrete inherited contract; see Description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited contract; see Description. |
| `protected override System.Int32 OnGetSurfaceCount()` | Concrete inherited contract; see Description. |
| `protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` | Concrete inherited contract; see Description. |
| `protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` | Concrete inherited contract; see Description. |
| `protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` | Concrete inherited contract; see Description. |
| `protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` | Concrete inherited contract; see Description. |
| `protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` | Concrete inherited contract; see Description. |
| `protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` | Concrete inherited contract; see Description. |
| `protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` | Concrete inherited contract; see Description. |
| `public System.Int32 SurfaceFindByName(System.String name)` | Finds the first exact ordinal surface name. |
| `public System.String SurfaceGetName(System.Int32 surfaceIndex)` | Gets an authored surface name. |
| `public System.Void SurfaceRemove(System.Int32 surfaceIndex)` | Removes one surface and shifts subsequent indices down. |
| `public System.Void SurfaceSetName(System.Int32 surfaceIndex, System.String name)` | Assigns a literal name and emits Changed. |
| `public System.Void SurfaceUpdateAttributeRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in a surface's packed color and UV buffer. |
| `public System.Void SurfaceUpdateVertexRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in packed two-float positions in the surface vertex buffer. |

## Methods and protected extension points descriptions

### AddSurfaceFromArrays

`public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)`

Summary: Adds one copied two-dimensional surface.

primitive: Vertex topology, including points, lines and strips.

arrays: Borrowed typed vertex/color/UV/index arrays copied before mutation.

blendShapes: Null or empty until the typed deformation consumer is integrated.

lods: Null or empty until scale-selected index sets are integrated.

flags: UseDynamicUpdate or Use2DVertices. Unsupported bits reject before mutation.

Remarks: Channel bits are inferred. The new surface index is the old GetSurfaceCount result.

System.ArgumentException: Channel counts, primitives or finite values are invalid.

System.ArgumentNullException: The data or a channel array is null.

System.ArgumentOutOfRangeException: An index or primitive is invalid.

System.NotSupportedException: Flags require an unimplemented storage/attribute layer.

System.ObjectDisposedException: The mesh is disposed.


### ClearSurfaces

`public System.Void ClearSurfaces()`

Summary: Removes every surface without disposing borrowed materials.

System.ObjectDisposedException: The mesh is disposed.


### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnGetSurfaceCount

`protected override System.Int32 OnGetSurfaceCount()`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceGetArrayIndexLen

`protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceGetArrayLen

`protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceGetArrays

`protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceGetFormat

`protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceGetMaterial

`protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceGetPrimitiveType

`protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### OnSurfaceSetMaterial

`protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

Concrete inherited resource/node hook; ownership, errors and effects are described above.


### SurfaceFindByName

`public System.Int32 SurfaceFindByName(System.String name)`

Summary: Finds the first exact ordinal surface name.

name: Literal non-null name.

Returns: The first index, or minus one.

System.ArgumentNullException: The name is null.


### SurfaceGetName

`public System.String SurfaceGetName(System.Int32 surfaceIndex)`

Summary: Gets an authored surface name.

surfaceIndex: Existing zero-based index.

Returns: The literal name, empty initially.


### SurfaceRemove

`public System.Void SurfaceRemove(System.Int32 surfaceIndex)`

Summary: Removes one surface and shifts subsequent indices down.

surfaceIndex: Existing zero-based index.

System.ArgumentOutOfRangeException: The index is invalid.

System.ObjectDisposedException: The mesh is disposed.


### SurfaceSetName

`public System.Void SurfaceSetName(System.Int32 surfaceIndex, System.String name)`

Summary: Assigns a literal name and emits Changed.

surfaceIndex: Existing zero-based index.

name: Non-null name; duplicates and empty names are allowed.

System.ArgumentNullException: The name is null.


### SurfaceUpdateAttributeRegion

`public System.Void SurfaceUpdateAttributeRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Summary: Updates bytes in a surface's packed color and UV buffer.

surfaceIndex: Existing zero-based surface index.

offset: Byte offset within the packed attribute buffer.

data: Present RGBA8 UNORM bytes followed by present little-endian float UV pairs.

Remarks: Color uses four bytes per vertex, UV uses eight; partial records retain untouched bytes. Validation of the complete affected UV records precedes any write.

System.ArgumentException: An affected UV becomes nonfinite.

System.ArgumentOutOfRangeException: The region exceeds the buffer.


### SurfaceUpdateVertexRegion

`public System.Void SurfaceUpdateVertexRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Summary: Updates bytes in packed two-float positions in the surface vertex buffer.

surfaceIndex: Existing zero-based index.

offset: Byte offset within the two-float position buffer.

data: Little-endian float X/Y storage, including partial byte records.

Remarks: Retained draws observe the update without rerecording.

System.ArgumentException: An affected position becomes nonfinite.

System.ArgumentOutOfRangeException: The region exceeds the vertex buffer.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.

Region updates accept byte offsets including partial records. Affected complete X/Y or color/UV records are reconstructed and finite-validated in a first pass, then committed in a second pass without heap allocation. Vertex stride is eight bytes; attribute stride is four RGBA8 bytes plus eight UV bytes when present. Names/materials/data getters validate indices and disposed resource state. Nonempty blendShapes/lods and unknown flags throw NotSupportedException before mutation. Advanced channels remain exact Blocked/Partial coverage; no stubs are shipped.

Inherited SurfaceGetArrayLen, SurfaceGetArrayIndexLen, SurfaceGetFormat and SurfaceGetPrimitiveType query the same concrete storage; GetFaces expands its triangle surfaces into independent local Vector2 faces. Vertex and attribute updates execute with or without UseDynamicUpdate, which is a storage-policy hint.
