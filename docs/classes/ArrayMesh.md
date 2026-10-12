# ArrayMesh

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.ArrayMesh` · **Source:** [ArrayMesh.cs](../../src/Scene/Resources/ArrayMesh.cs).

**Inherits:** [Mesh](Mesh.md).

## Description

Concrete copied surface storage. AddSurfaceFromArrays accepts typed Vertices/Normals/Tangents/Colors/UVs/UV2s/Custom0–Custom3/Indices, copied named morph targets and copied authored LOD indices and supported flags. Every channel is copied and validated before adding; vertex/normal/tangent/color/primary-UV/secondary-UV/custom-record counts and index bounds must match topology. All five topologies execute. Empty or malformed vertex payloads reject. Colors clamp/truncate to RGBA8 UNORM on import; source getters expose decoded quantized colors, preserving the exercised pinned storage precision. Positions remain float pairs; UVs use float pairs unless CompressAttributes is selected. The new index is the previous GetSurfaceCount. Surfaces default to an empty name and null borrowed material; duplicates names are allowed and search returns the first exact ordinal match. Remove shifts later indices, Clear removes all; borrowed materials remain live. Every edit commits before Changed, including equal name/material writes; listener failure does not undo valid data.

## Example

```csharp
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{ Vertices = [new(0, 0), new(20, 0), new(0, 20)] }, flags: Mesh.ArrayFormat.UseDynamicUpdate);
var node = new MeshInstance { Mesh = mesh };
// Attach node to a Window; retain mesh until the scene is disposed.
```


Shape count/name accessors are inherited from [Mesh](Mesh.md). Named targets, two stored modes and independent [MeshInstance](MeshInstance.md) weights execute through [the shared deformation contract](../components/meshes.md#named-targets-and-instance-deformation).

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public ArrayMesh()` | Creates an empty mesh. |
| `public System.Void AddBlendShape(System.String name)` | Adds a named shape definition before any surfaces exist. |
| `public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)` | Adds one copied two-dimensional surface. |
| `public System.Void ClearBlendShapes()` | Clears shape definitions before any surfaces exist. |
| `public System.Void ClearSurfaces()` | Removes every surface without disposing borrowed materials. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` |  |
| `protected override Electron2D.Resource CreateDuplicateInstance()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `protected override System.Int32 OnGetBlendShapeCount()` |  |
| `protected override System.String OnGetBlendShapeName(System.Int32 index)` |  |
| `protected override System.Int32 OnGetSurfaceCount()` |  |
| `protected override System.Void OnSetBlendShapeName(System.Int32 index, System.String name)` |  |
| `protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` |  |
| `protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` |  |
| `protected override System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> OnSurfaceGetLODs(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` |  |
| `protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` |  |
| `protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` |  |
| `public System.Void RegenNormalMaps()` | Regenerates each surface's tangent channel from its existing normals and primary UVs. |
| `public System.Int32 SurfaceFindByName(System.String name)` | Finds the first exact ordinal surface name. |
| `public System.String SurfaceGetName(System.Int32 surfaceIndex)` | Gets an authored surface name. |
| `public System.Void SurfaceRemove(System.Int32 surfaceIndex)` | Removes one surface and shifts subsequent indices down. |
| `public System.Void SurfaceSetName(System.Int32 surfaceIndex, System.String name)` | Assigns a literal name and emits Changed. |
| `public System.Void SurfaceUpdateAttributeRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in a surface's packed color, primary/secondary UV and custom buffer. |
| `public System.Void SurfaceUpdateSkinRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates copied bytes in the packed skin buffer, including partial records. |
| `public System.Void SurfaceUpdateVertexRegion(System.Int32 surfaceIndex, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates bytes in packed positions and octahedral normal/tangent records. |
| `public Electron2D.BlendShapeMode BlendShapeMode { get; set; }` | Gets or selects normalized absolute targets or additive relative offsets. |

## Member descriptions

### ArrayMesh

`public ArrayMesh()`

summary: Creates an empty mesh.

### AddBlendShape

`public System.Void AddBlendShape(System.String name)`

summary: Adds a named shape definition before any surfaces exist.

name: Non-null name; collisions gain the first free ordinal suffix starting at two.

remarks: All subsequently added surfaces require one shape payload per definition.

### AddSurfaceFromArrays

`public System.Void AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null, Electron2D.Mesh.ArrayFormat flags = None)`

summary: Adds one copied two-dimensional surface.

primitive: Vertex topology, including points, lines and strips.

arrays: Borrowed typed vertex/normal/tangent/color/primary and secondary UV/custom/index/skin arrays copied before mutation.

blendShapes: One copied position/normal/tangent-only target per named definition; null or empty for unshaped meshes.

lods: Copied positive finite local error thresholds mapped to shorter index sets; null or empty uses base geometry.

flags: UseDynamicUpdate, Use2DVertices, Use8BoneWeights, CompressAttributes and matching channel bits. Unsupported bits reject before mutation.

remarks: Channel bits are inferred; explicitly supplied channel bits must match the arrays. The new surface index is the old GetSurfaceCount result.

System.ArgumentException: Channel counts, primitives or finite values are invalid.

System.ArgumentNullException: The data or a channel array is null.

System.ArgumentOutOfRangeException: An index or primitive is invalid.

System.NotSupportedException: Flags require an unimplemented storage/attribute layer.

System.ObjectDisposedException: The mesh is disposed.

### ClearBlendShapes

`public System.Void ClearBlendShapes()`

summary: Clears shape definitions before any surfaces exist.

remarks: ClearSurfaces removes geometry while retaining these definitions.

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

### OnGetBlendShapeCount

`protected override System.Int32 OnGetBlendShapeCount()`

### OnGetBlendShapeName

`protected override System.String OnGetBlendShapeName(System.Int32 index)`

### OnGetSurfaceCount

`protected override System.Int32 OnGetSurfaceCount()`

### OnSetBlendShapeName

`protected override System.Void OnSetBlendShapeName(System.Int32 index, System.String name)`

### OnSurfaceGetArrayIndexLen

`protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

### OnSurfaceGetArrayLen

`protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

### OnSurfaceGetArrays

`protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

### OnSurfaceGetBlendShapeArrays

`protected override Electron2D.MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(System.Int32 surfaceIndex)`

### OnSurfaceGetFormat

`protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

### OnSurfaceGetLODs

`protected override System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> OnSurfaceGetLODs(System.Int32 surfaceIndex)`

### OnSurfaceGetMaterial

`protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

### OnSurfaceGetPrimitiveType

`protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

### OnSurfaceSetMaterial

`protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

### RegenNormalMaps

`public System.Void RegenNormalMaps()`

summary: Regenerates each surface's tangent channel from its existing normals and primary UVs.

remarks: Uses the same MikkTSpace path as SurfaceTool. Empty meshes do nothing. Every surface is prepared before publication; errors preserve all prior surfaces. Positions, indices, names, materials, policy flags, blend definitions/target payloads and exact packed UV bytes/scale remain unchanged. Ordinary normal words stay exact; compressed coupled frames reencode normals within numerical precision. Success publishes one Changed event. Structural regeneration may allocate.

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

summary: Updates bytes in a surface's packed color, primary/secondary UV and custom buffer.

surfaceIndex: Existing zero-based surface index.

offset: Byte offset within the packed attribute buffer.

data: Present RGBA8 UNORM, primary/secondary float UV pairs or compressed UNORM16 pairs, then Custom0 through Custom3 records in their selected layouts.

remarks: Color uses four bytes per vertex; each UV channel uses eight bytes, or four with CompressAttributes; custom formats use four, eight, twelve or sixteen. Partial records retain untouched bytes. Validation of every affected UV and custom component precedes any write.

System.ArgumentException: An affected UV or custom component becomes nonfinite.

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

data: Little-endian float X/Y positions followed by per-vertex octahedral words or six-byte compressed joint axis/angle frames; partial records are accepted.

remarks: Retained draws observe the update without rerecording.

System.ArgumentException: An affected position becomes nonfinite.

System.ArgumentOutOfRangeException: The region exceeds the vertex buffer.

### BlendShapeMode

`public Electron2D.BlendShapeMode BlendShapeMode { get; set; }`

summary: Gets or selects normalized absolute targets or additive relative offsets.

value: Relative initially; changing the mode preserves stored payloads and updates drawing.

## LOD behavior

Authored levels use copied positive local edge-error thresholds and shorter complete index sets over an indexed base. Drawing selects the largest threshold within one framebuffer pixel from the greatest singular value of the full transformed basis. Bounds, arrays and face/index counts preserve full geometry. Morph output and tangent regeneration retain levels. AMS7 persists LODs, targets and compressed attributes and reads AMS1–6. See [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).


## Compressed attribute storage

`Mesh.ArrayFormat.CompressAttributes` retains float XY positions, four-byte UNORM16 primary/secondary UVs and a six-byte joint normal/tangent axis/angle record. UVs outside [0,1] use copied per-component scale metadata; zero axes remain zero and overflowing scale rejects before mutation. Normal-only imports synthesize a tangent; tangent-only compressed input rejects. Frame packing normalizes/projects numerical directions, so it does not retain arbitrary magnitude/nonorthogonality. Partial updates preserve untouched canonical bytes. Tangent regeneration retains exact UV bytes/scale and reencodes the coupled frame. Copies, saved scenes and morph output retain the format and exact base records. AMS7 persists metadata and canonical bytes and reads AMS1–6. See [compressed attributes](../components/meshes.md#compressed-attributes) for layout, precision and verification limits.


ImporterMesh.GetMesh can atomically append a prevalidated compiled draft. Unshaped existing surfaces remain; target shape definitions must be empty and a shaped draft requires an empty target. One ResourceName/Changed publication follows complete geometry, names, materials and mode. A failing observer retains the committed mesh and producer cache. See [ImporterMesh](ImporterMesh.md).
