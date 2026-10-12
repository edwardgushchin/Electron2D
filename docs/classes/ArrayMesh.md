# ArrayMesh

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.ArrayMesh` · **Source:** [ArrayMesh.cs](../../src/Scene/Resources/ArrayMesh.cs).

**Inherits:** [Mesh](Mesh.md).

## Description

Concrete copied surface storage. AddSurfaceFromArrays accepts typed Vertices/Colors/UVs/UV2s/Indices, optional empty deformation/LOD inputs and supported flags. Every channel is copied and validated before adding; vertex/color/primary-UV/secondary-UV counts and index bounds must match topology. All five topologies execute. Empty or malformed vertex payloads reject. Colors clamp/truncate to RGBA8 UNORM on import; source getters expose decoded quantized colors, preserving the exercised pinned storage precision. Position/texture coordinates remain float pairs. The new index is the previous GetSurfaceCount. Surfaces default to an empty name and null borrowed material; duplicates names are allowed and search returns the first exact ordinal match. Remove shifts later indices, Clear removes all; borrowed materials remain live. Every edit commits before Changed, including equal name/material writes; listener failure does not undo valid data.

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
| `public System.Void SurfaceUpdateAttributeRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in a surface's packed color and texture-coordinate buffer. |
| `public System.Void SurfaceUpdateVertexRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in packed two-float positions in the surface vertex buffer. |

## Methods and protected extension points descriptions

### AddSurfaceFromArrays

`public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)`

Summary: Adds one copied two-dimensional surface.

primitive: Vertex topology, including points, lines and strips.

arrays: Borrowed typed vertex/color/primary-UV/secondary-UV/index/skin arrays copied before mutation.

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

Summary: Updates bytes in a surface's packed color and texture-coordinate buffer.

surfaceIndex: Existing zero-based surface index.

offset: Byte offset within the packed attribute buffer.

data: Present RGBA8 UNORM bytes followed by present primary and secondary little-endian float UV pairs.

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

Region updates accept byte offsets including partial records. Affected complete X/Y or color/texture-coordinate records are reconstructed and finite-validated in a first pass, then committed in a second pass without heap allocation. Vertex stride is eight bytes; attribute stride is four RGBA8 bytes plus eight bytes per present primary and secondary UV channel, in that order. Names/materials/data getters validate indices and disposed resource state. Nonempty blendShapes/lods and unknown flags throw NotSupportedException before mutation; matching explicit existing channel bits are supported. Advanced channels remain exact Blocked/Partial coverage; no stubs are shipped.

Inherited SurfaceGetArrayLen, SurfaceGetArrayIndexLen, SurfaceGetFormat and SurfaceGetPrimitiveType query the same concrete storage; GetFaces expands its triangle surfaces into independent local Vector2 faces. Vertex and attribute updates execute with or without UseDynamicUpdate, which is a storage-policy hint.

Inherited Mesh.GetAABB returns local Rect2 visibility bounds for every stored vertex, including unreferenced vertices and point/line topologies. Prepared bounds/replay do not copy live ArrayMesh data. MeshInstance resource-change listeners use worker-safe invalidation; attached node setters retain owner/capture guards.

## Mesh skin API

| Complete signature | Contract |
| --- | --- |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Extends inherited typed schema with validated surface bytes and a separate material resource graph. |
| `public System.Void SurfaceUpdateSkinRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates copied bytes in the packed skin buffer, including partial records. |

## Mesh skin member descriptions

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Extends inherited typed schema with validated surface bytes and a separate material resource graph.

### SurfaceUpdateSkinRegion

`public System.Void SurfaceUpdateSkinRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Updates copied bytes in the packed skin buffer, including partial records.

Remarks: Retained mesh draws observe edits without rerecording; configured palette bounds are validated at draw time.

`surfaceIndex`: Existing surface with bone/weight channels.

`offset`: Byte offset into 16-byte four-slot or 32-byte eight-slot records.

`data`: Little-endian uint16 indices followed by UNORM16 weights for each vertex.

Throws `System.InvalidOperationException`: No skin buffer exists.

Throws `System.ArgumentOutOfRangeException`: The surface or byte region is invalid.

The [mesh component](../components/meshes.md#server-palettes-and-foureight-skin-records) owns the actual storage, coordinate, lifetime, callback, archive and verification contract. New palette API uses the existing native-service availability/owner gate. Headless retained geometry and native rendered/backend acceptance remain separately recorded.

## Surface file state

Exact ArrayMesh files store bounded versioned geometry/topology/flags/name/skin bytes separately from typed Material arrays, preserving aliases. Complete validation precedes replacement. New storage supports 4096 surfaces, 65536 name code units and 64 MiB; import/query/duplicate/file preparation may allocate. Palette attachments and logical identities are transient.

## Secondary mesh UV channel

[MeshSurfaceData.UV2s](MeshSurfaceData.md#uv2s) supplies copied finite float pairs independent of primary UV and instance data. [The mesh component](../components/meshes.md#secondary-uv-storage-and-shader-consumer) records packing, archives, shader locations and executable verification.

[SurfaceTool.Commit](SurfaceTool.md) uses shared internal surface validation and publishes copied geometry plus its borrowed material atomically before one Changed. Public AddSurfaceFromArrays retains its existing null-material initial state.

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public ArrayMesh()` | Creates an empty mesh. |
| `public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)` | Adds one copied two-dimensional surface. |
| `public System.Void ClearSurfaces()` | Removes every surface without disposing borrowed materials. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` |  |
| `protected override Electron2D.Resource CreateDuplicateInstance()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `protected override System.Int32 OnGetSurfaceCount()` |  |
| `protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` |  |
| `protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` |  |
| `protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` |  |
| `public System.Void RegenNormalMaps()` | Regenerates each surface's tangent channel from its existing normals and primary UVs. |
| `public System.Int32 SurfaceFindByName(System.String name)` | Finds the first exact ordinal surface name. |
| `public System.String SurfaceGetName(System.Int32 surfaceIndex)` | Gets an authored surface name. |
| `public System.Void SurfaceRemove(System.Int32 surfaceIndex)` | Removes one surface and shifts subsequent indices down. |
| `public System.Void SurfaceSetName(System.Int32 surfaceIndex, System.String name)` | Assigns a literal name and emits Changed. |
| `public System.Void SurfaceUpdateAttributeRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in a surface's packed color and primary/secondary UV buffer. |
| `public System.Void SurfaceUpdateSkinRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates copied bytes in the packed skin buffer, including partial records. |
| `public System.Void SurfaceUpdateVertexRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in packed positions and octahedral normal/tangent records. |

## Member descriptions

### ArrayMesh

`public ArrayMesh()`

summary: Creates an empty mesh.

### AddSurfaceFromArrays

`public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)`

summary: Adds one copied two-dimensional surface.

primitive: Vertex topology, including points, lines and strips.

arrays: Borrowed typed vertex/normal/tangent/color/primary and secondary UV/index/skin arrays copied before mutation.

blendShapes: Null or empty until the typed deformation consumer is integrated.

lods: Null or empty until scale-selected index sets are integrated.

flags: UseDynamicUpdate, Use2DVertices, Use8BoneWeights and matching channel bits. Unsupported bits reject before mutation.

remarks: Channel bits are inferred; explicitly supplied channel bits must match the arrays. The new surface index is the old GetSurfaceCount result.

System.ArgumentException: Channel counts, primitives or finite values are invalid.

System.ArgumentNullException: The data or a channel array is null.

System.ArgumentOutOfRangeException: An index or primitive is invalid.

System.NotSupportedException: Flags require an unimplemented storage/attribute layer.

System.ObjectDisposedException: The mesh is disposed.

### ClearSurfaces

`public System.Void ClearSurfaces()`

summary: Removes every surface without disposing borrowed materials.

System.ObjectDisposedException: The mesh is disposed.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

### OnGetSurfaceCount

`protected override System.Int32 OnGetSurfaceCount()`

### OnSurfaceGetArrayIndexLen

`protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

### OnSurfaceGetArrayLen

`protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

### OnSurfaceGetArrays

`protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

### OnSurfaceGetFormat

`protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

### OnSurfaceGetMaterial

`protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

### OnSurfaceGetPrimitiveType

`protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

### OnSurfaceSetMaterial

`protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

### RegenNormalMaps

`public System.Void RegenNormalMaps()`

summary: Regenerates each surface's tangent channel from its existing normals and primary UVs.

remarks: Uses the same MikkTSpace path as SurfaceTool. Empty meshes do nothing. Every surface is prepared before publication; errors preserve all prior surfaces. Positions, indices, names, materials, policy flags and exact packed normal words remain unchanged. Success publishes one Changed event. Structural regeneration may allocate.

System.InvalidOperationException: A surface lacks the required normal/UV data or complete face.

System.ObjectDisposedException: The mesh or a borrowed material is disposed.

### SurfaceFindByName

`public System.Int32 SurfaceFindByName(System.String name)`

summary: Finds the first exact ordinal surface name.

name: Literal non-null name.

returns: The first index, or minus one.

System.ArgumentNullException: The name is null.

### SurfaceGetName

`public System.String SurfaceGetName(System.Int32 surfaceIndex)`

summary: Gets an authored surface name.

surfaceIndex: Existing zero-based index.

returns: The literal name, empty initially.

### SurfaceRemove

`public System.Void SurfaceRemove(System.Int32 surfaceIndex)`

summary: Removes one surface and shifts subsequent indices down.

surfaceIndex: Existing zero-based index.

System.ArgumentOutOfRangeException: The index is invalid.

System.ObjectDisposedException: The mesh is disposed.

### SurfaceSetName

`public System.Void SurfaceSetName(System.Int32 surfaceIndex, System.String name)`

summary: Assigns a literal name and emits Changed.

surfaceIndex: Existing zero-based index.

name: Non-null name; duplicates and empty names are allowed.

System.ArgumentNullException: The name is null.

### SurfaceUpdateAttributeRegion

`public System.Void SurfaceUpdateAttributeRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

summary: Updates bytes in a surface's packed color and primary/secondary UV buffer.

surfaceIndex: Existing zero-based surface index.

offset: Byte offset within the packed attribute buffer.

data: Present RGBA8 UNORM bytes followed by present little-endian primary and secondary float UV pairs.

remarks: Color uses four bytes per vertex; each UV channel uses eight. Partial records retain untouched bytes. Validation of every affected coordinate in both channels precedes any write.

System.ArgumentException: An affected coordinate becomes nonfinite.

System.ArgumentOutOfRangeException: The region exceeds the buffer.

### SurfaceUpdateSkinRegion

`public System.Void SurfaceUpdateSkinRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

summary: Updates copied bytes in the packed skin buffer, including partial records.

surfaceIndex: Existing surface with bone/weight channels.

offset: Byte offset into 16-byte four-slot or 32-byte eight-slot records.

data: Little-endian uint16 indices followed by UNORM16 weights for each vertex.

remarks: Retained mesh draws observe edits without rerecording; configured palette bounds are validated at draw time.

System.InvalidOperationException: No skin buffer exists.

System.ArgumentOutOfRangeException: The surface or byte region is invalid.

### SurfaceUpdateVertexRegion

`public System.Void SurfaceUpdateVertexRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

summary: Updates bytes in packed positions and octahedral normal/tangent records.

surfaceIndex: Existing zero-based index.

offset: Byte offset in the position block followed by interleaved normal/tangent words.

data: Little-endian float X/Y positions followed by per-vertex octahedral UNORM16 normal/tangent words, including partial byte records.

remarks: Retained draws observe the update without rerecording.

System.ArgumentException: An affected position becomes nonfinite.

System.ArgumentOutOfRangeException: The region exceeds the vertex buffer.
