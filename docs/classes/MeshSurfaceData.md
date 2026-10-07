# MeshSurfaceData

Last updated: 2026-10-07

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.MeshSurfaceData` · **Source:** [MeshSurfaceData.cs](../../src/Scene/Resources/MeshSurfaceData.cs).

Typed enum or managed payload.

## Description

Typed replacement for the heterogeneous mesh array-of-arrays. Named fields hold two-dimensional positions, optional matching vertex colors/primary UV optional vertex ordering indices and flattened four/eight bone indices and weights. Setter arrays remain caller-owned until passed to AddSurfaceFromArrays, which copies all channels. Query results own independent arrays. This payload adds no native handle, generic object dispatcher or data-only claim for unsupported channels.

## Example

```csharp
var data = new MeshSurfaceData
{
    Vertices = [new(0, 0), new(20, 0), new(0, 20)],
    Colors = [Colors.Red, Colors.Green, Colors.Blue],
    Indices = [0, 1, 2]
};
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public MeshSurfaceData()` | Creates an empty set of typed surface arrays. |

## Constructors descriptions

### .ctor

`public MeshSurfaceData()`

Summary: Creates an empty set of typed surface arrays.


## Properties

| Complete signature | Contract |
| --- | --- |
| `public Electron2D.Color[] Colors { get; set; }` | Gets or sets optional vertex colors. |
| `public System.Int32[] Indices { get; set; }` | Gets or sets optional indices defining primitive vertex order. |
| `public Electron2D.Vector2[] UVs { get; set; }` | Gets or sets optional primary texture coordinates. |
| `public Electron2D.Vector2[] Vertices { get; set; }` | Gets or sets local two-dimensional vertex positions. |

## Properties descriptions

### Colors

`public Electron2D.Color[] Colors { get; set; }`

Summary: Gets or sets optional vertex colors.

Value: Empty or one finite color per vertex.


### Indices

`public System.Int32[] Indices { get; set; }`

Summary: Gets or sets optional indices defining primitive vertex order.

Value: Empty initially; each index must address Vertices when consumed.


### UVs

`public Electron2D.Vector2[] UVs { get; set; }`

Summary: Gets or sets optional primary texture coordinates.

Value: Empty or one finite normalized-space coordinate per vertex; values outside zero through one retain sampler behavior.


### Vertices

`public Electron2D.Vector2[] Vertices { get; set; }`

Summary: Gets or sets local two-dimensional vertex positions.

Value: Empty initially; vertices must be finite when consumed.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.

## Mesh skin API

| Complete signature | Contract |
| --- | --- |
| `public System.Int32[] Bones { get; set; }` | Gets or sets optional flattened four- or eight-slot bone indices per vertex. |
| `public System.Single[] Weights { get; set; }` | Gets or sets flattened skin weights matching Bones. |

## Mesh skin member descriptions

### Bones

`public System.Int32[] Bones { get; set; }`

Gets or sets optional flattened four- or eight-slot bone indices per vertex.

Value: Empty without skin; every stored index is zero through 65535 and must resolve when drawn with a palette.

### Weights

`public System.Single[] Weights { get; set; }`

Gets or sets flattened skin weights matching Bones.

Value: Finite values; imports clamp and truncate to unsigned normalized 16-bit weights without normalizing the sum.

The [mesh component](../components/meshes.md#server-palettes-and-foureight-skin-records) owns the actual storage, coordinate, lifetime, callback, archive and verification contract. New palette API uses the existing native-service availability/owner gate. Headless retained geometry and native rendered/backend acceptance remain separately recorded.
