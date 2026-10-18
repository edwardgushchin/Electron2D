# MeshSurfaceData

Last updated: 2026-10-18

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.MeshSurfaceData` · **Source:** [MeshSurfaceData.cs](../../src/Scene/Resources/MeshSurfaceData.cs).

Typed enum or managed payload.

## Description

Typed replacement for the heterogeneous mesh array-of-arrays. Named fields hold two-dimensional positions, optional matching numerical normals/tangents, vertex colors/primary and secondary UV, four optional packed custom payloads, optional vertex ordering indices and flattened four/eight bone indices and weights. Setter arrays remain caller-owned until passed to AddSurfaceFromArrays, which copies all channels. Query results own independent arrays. This payload adds no native handle, generic object dispatcher or data-only claim for unsupported channels.

## Example

```csharp
var data = new MeshSurfaceData
{
    Vertices = [new(0, 0), new(20, 0), new(0, 20)],
    Colors = [Colors.Red, Colors.Green, Colors.Blue],
    Indices = [0, 1, 2]
};
```

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public MeshSurfaceData()` | Creates an empty set of typed surface arrays. |
| `public System.Int32[] Bones { get; set; }` | Gets or sets optional flattened four- or eight-slot bone indices per vertex. |
| `public Electron2D.Color[] Colors { get; set; }` | Gets or sets optional vertex colors. |
| `public Electron2D.MeshCustomData Custom0 { get; set; }` | Gets or sets the optional first packed custom channel. |
| `public Electron2D.MeshCustomData Custom1 { get; set; }` | Gets or sets the optional second packed custom channel. |
| `public Electron2D.MeshCustomData Custom2 { get; set; }` | Gets or sets the optional third packed custom channel. |
| `public Electron2D.MeshCustomData Custom3 { get; set; }` | Gets or sets the optional fourth packed custom channel. |
| `public System.Int32[] Indices { get; set; }` | Gets or sets optional indices defining primitive vertex order. |
| `public Electron2D.Vector3[] Normals { get; set; }` | Gets or sets optional mesh-local numerical normal vectors. |
| `public Electron2D.Vector4[] Tangents { get; set; }` | Gets or sets optional mesh-local tangent vectors and handedness. |
| `public Electron2D.Vector2[] UV2s { get; set; }` | Gets or sets optional secondary texture coordinates. |
| `public Electron2D.Vector2[] UVs { get; set; }` | Gets or sets optional primary texture coordinates. |
| `public System.Int32 VertexCount { get; set; }` | Gets or sets the logical count independently of position storage. |
| `public Electron2D.Vector2[] Vertices { get; set; }` | Gets or sets local two-dimensional vertex positions. |
| `public System.Single[] Weights { get; set; }` | Gets or sets flattened skin weights matching Bones. |

## Member descriptions

### MeshSurfaceData

`public MeshSurfaceData()`

summary: Creates an empty set of typed surface arrays.

### Bones

`public System.Int32[] Bones { get; set; }`

summary: Gets or sets optional flattened four- or eight-slot bone indices per vertex.

value: Empty without skin; every stored index is zero through 65535 and must resolve when drawn with a palette.

### Colors

`public Electron2D.Color[] Colors { get; set; }`

summary: Gets or sets optional vertex colors.

value: Empty or one finite color per vertex.

### Custom0

`public Electron2D.MeshCustomData Custom0 { get; set; }`

summary: Gets or sets the optional first packed custom channel.

value: Null without the channel; one finite packed record per vertex when present.

### Custom1

`public Electron2D.MeshCustomData Custom1 { get; set; }`

summary: Gets or sets the optional second packed custom channel.

value: Null without the channel; independent of Custom0.

### Custom2

`public Electron2D.MeshCustomData Custom2 { get; set; }`

summary: Gets or sets the optional third packed custom channel.

value: Null without the channel; independent of other custom channels.

### Custom3

`public Electron2D.MeshCustomData Custom3 { get; set; }`

summary: Gets or sets the optional fourth packed custom channel.

value: Null without the channel; independent of instance shader data.

### Indices

`public System.Int32[] Indices { get; set; }`

summary: Gets or sets optional indices defining primitive vertex order.

value: Empty for sequential invocations; each index addresses VertexCount, or zero through 65535 for a zero-count index-only procedural surface.

### Normals

`public Electron2D.Vector3[] Normals { get; set; }`

summary: Gets or sets optional mesh-local numerical normal vectors.

value: Empty or one finite Vector3 per vertex; consumed surfaces pack directions to octahedral UNORM16 or a compressed joint axis/angle frame.

### Tangents

`public Electron2D.Vector4[] Tangents { get; set; }`

summary: Gets or sets optional mesh-local tangent vectors and handedness.

value: Empty or one finite Vector4 per vertex; XYZ is the direction and W selects negative or positive orientation.

### UV2s

`public Electron2D.Vector2[] UV2s { get; set; }`

summary: Gets or sets optional secondary texture coordinates.

value: Empty or one finite coordinate per vertex; independent of primary UVs and instance shader data.

### UVs

`public Electron2D.Vector2[] UVs { get; set; }`

summary: Gets or sets optional primary texture coordinates.

value: Empty or one finite normalized-space coordinate per vertex; values outside zero through one retain sampler behavior.

### Vertices

`public Electron2D.Vector2[] Vertices { get; set; }`

summary: Gets or sets local two-dimensional vertex positions.

value: Empty initially; vertices must be finite when consumed.

### Weights

`public System.Single[] Weights { get; set; }`

summary: Gets or sets flattened skin weights matching Bones.

value: Finite values; imports clamp and truncate to unsigned normalized 16-bit weights without normalizing the sum.


## Compressed attribute storage

`Mesh.ArrayFormat.CompressAttributes` retains float XY positions, four-byte UNORM16 primary/secondary UVs and a six-byte joint normal/tangent axis/angle record. UVs outside [0,1] use copied per-component scale metadata; zero axes remain zero and overflowing scale rejects before mutation. Normal-only imports synthesize a tangent; tangent-only compressed input rejects. Frame packing normalizes/projects numerical directions, so it does not retain arbitrary magnitude/nonorthogonality. Partial updates preserve untouched canonical bytes. Tangent regeneration retains exact UV bytes/scale and reencodes the coupled frame. Copies, saved scenes and morph output retain the format and exact base records. AMS8 persists metadata and canonical bytes and reads AMS1–7. See [compressed attributes](../components/meshes.md#compressed-attributes) for layout, precision and verification limits.

### VertexCount

`public System.Int32 VertexCount { get; set; }`

Zero infers `Vertices.Length`. A positive value must match supplied positions; without positions it sizes the optional channels. Index-only procedural surfaces may retain zero and unsigned 16-bit indices. Validation happens when consumed. [Procedural surfaces](../components/meshes.md#procedural-surfaces-without-positions) require `UseEmptyVertexArray` and an authored vertex stage for drawing.

```csharp
using var generated = new ArrayMesh { CustomAABB = new(0, 0, 64, 64) };
generated.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,
    new MeshSurfaceData { VertexCount = 3 }, flags: Mesh.ArrayFormat.UseEmptyVertexArray);
// Assign an authored vertex ShaderMaterial before drawing this mesh.
```
