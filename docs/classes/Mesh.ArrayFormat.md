# Mesh.ArrayFormat

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.Mesh.ArrayFormat` · **Source:** [Mesh.cs](../../src/Scene/Resources/Mesh.cs).

Typed enum or managed payload.

## Description

Retains the implemented numeric topology/channel policy identities; unsupported values reject or remain absent at exact coverage dependencies.

## Example

```csharp
var value = Mesh.ArrayFormat.Color;
```

## Values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.Mesh.ArrayFormat Color = 8` | Vertex colors. |
| `public const Electron2D.Mesh.ArrayFormat Index = 4096` | Vertex ordering indices. |
| `public const Electron2D.Mesh.ArrayFormat None = 0` | No channels or policies. |
| `public const Electron2D.Mesh.ArrayFormat TexUV = 16` | Primary texture coordinates. |
| `public const Electron2D.Mesh.ArrayFormat TexUV2 = 32` | Secondary texture coordinates. |
| `public const Electron2D.Mesh.ArrayFormat Use2DVertices = 33554432` | Vertex positions use two floating components. |
| `public const Electron2D.Mesh.ArrayFormat UseDynamicUpdate = 67108864` | Surface buffers accept explicit region updates. |
| `public const Electron2D.Mesh.ArrayFormat Vertex = 1` | Vertex positions. |

## Values descriptions

### Color

`public const Electron2D.Mesh.ArrayFormat Color = 8`

Summary: Vertex colors.


### Index

`public const Electron2D.Mesh.ArrayFormat Index = 4096`

Summary: Vertex ordering indices.


### None

`public const Electron2D.Mesh.ArrayFormat None = 0`

Summary: No channels or policies.


### TexUV

`public const Electron2D.Mesh.ArrayFormat TexUV = 16`

Summary: Primary texture coordinates.


### Use2DVertices

`public const Electron2D.Mesh.ArrayFormat Use2DVertices = 33554432`

Summary: Vertex positions use two floating components.


### UseDynamicUpdate

`public const Electron2D.Mesh.ArrayFormat UseDynamicUpdate = 67108864`

Summary: Surface buffers accept explicit region updates.


### Vertex

`public const Electron2D.Mesh.ArrayFormat Vertex = 1`

Summary: Vertex positions.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.

## Mesh skin API

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.Mesh.ArrayFormat Bones = 1024` | Flattened unsigned 16-bit bone indices. |
| `public const Electron2D.Mesh.ArrayFormat Use8BoneWeights = 134217728` | Skin records contain eight indices and eight weights instead of four. |
| `public const Electron2D.Mesh.ArrayFormat Weights = 2048` | Flattened unsigned normalized 16-bit skin weights. |

## Mesh skin member descriptions

### Bones

`public const Electron2D.Mesh.ArrayFormat Bones = 1024`

Flattened unsigned 16-bit bone indices.

### Use8BoneWeights

`public const Electron2D.Mesh.ArrayFormat Use8BoneWeights = 134217728`

Skin records contain eight indices and eight weights instead of four.

### Weights

`public const Electron2D.Mesh.ArrayFormat Weights = 2048`

Flattened unsigned normalized 16-bit skin weights.

The [mesh component](../components/meshes.md#server-palettes-and-foureight-skin-records) owns the actual storage, coordinate, lifetime, callback, archive and verification contract. New palette API uses the existing native-service availability/owner gate. Headless retained geometry and native rendered/backend acceptance remain separately recorded.

## Secondary mesh UV channel

[MeshSurfaceData.UV2s](MeshSurfaceData.md#uv2s) supplies copied finite float pairs independent of primary UV and instance data. [The mesh component](../components/meshes.md#secondary-uv-storage-and-shader-consumer) records packing, archives, shader locations and executable verification.

### TexUV2

`public const Electron2D.Mesh.ArrayFormat TexUV2 = 32`

Secondary texture coordinates. Inferred when UV2s is present; explicit flags require matching data.

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.Mesh.ArrayFormat Bones = 1024` | Flattened unsigned 16-bit bone indices. |
| `public const Electron2D.Mesh.ArrayFormat Color = 8` | Vertex colors. |
| `public const Electron2D.Mesh.ArrayFormat Index = 4096` | Vertex ordering indices. |
| `public const Electron2D.Mesh.ArrayFormat None = 0` | No channels or policies. |
| `public const Electron2D.Mesh.ArrayFormat Normal = 2` | Octahedrally packed mesh-local normal directions. |
| `public const Electron2D.Mesh.ArrayFormat Tangent = 4` | Octahedrally packed tangent directions and orientation. |
| `public const Electron2D.Mesh.ArrayFormat TexUV = 16` | Primary texture coordinates. |
| `public const Electron2D.Mesh.ArrayFormat TexUV2 = 32` | Secondary texture coordinates. |
| `public const Electron2D.Mesh.ArrayFormat Use2DVertices = 33554432` | Vertex positions use two floating components. |
| `public const Electron2D.Mesh.ArrayFormat Use8BoneWeights = 134217728` | Skin records contain eight indices and eight weights instead of four. |
| `public const Electron2D.Mesh.ArrayFormat UseDynamicUpdate = 67108864` | Surface buffers accept explicit region updates. |
| `public const Electron2D.Mesh.ArrayFormat Vertex = 1` | Vertex positions. |
| `public const Electron2D.Mesh.ArrayFormat Weights = 2048` | Flattened unsigned normalized 16-bit skin weights. |

## Member descriptions

### Bones

`public const Electron2D.Mesh.ArrayFormat Bones = 1024`

summary: Flattened unsigned 16-bit bone indices.

### Color

`public const Electron2D.Mesh.ArrayFormat Color = 8`

summary: Vertex colors.

### Index

`public const Electron2D.Mesh.ArrayFormat Index = 4096`

summary: Vertex ordering indices.

### None

`public const Electron2D.Mesh.ArrayFormat None = 0`

summary: No channels or policies.

### Normal

`public const Electron2D.Mesh.ArrayFormat Normal = 2`

summary: Octahedrally packed mesh-local normal directions.

### Tangent

`public const Electron2D.Mesh.ArrayFormat Tangent = 4`

summary: Octahedrally packed tangent directions and orientation.

### TexUV

`public const Electron2D.Mesh.ArrayFormat TexUV = 16`

summary: Primary texture coordinates.

### TexUV2

`public const Electron2D.Mesh.ArrayFormat TexUV2 = 32`

summary: Secondary texture coordinates.

### Use2DVertices

`public const Electron2D.Mesh.ArrayFormat Use2DVertices = 33554432`

summary: Vertex positions use two floating components.

### Use8BoneWeights

`public const Electron2D.Mesh.ArrayFormat Use8BoneWeights = 134217728`

summary: Skin records contain eight indices and eight weights instead of four.

### UseDynamicUpdate

`public const Electron2D.Mesh.ArrayFormat UseDynamicUpdate = 67108864`

summary: Surface buffers accept explicit region updates.

### Vertex

`public const Electron2D.Mesh.ArrayFormat Vertex = 1`

summary: Vertex positions.

### Weights

`public const Electron2D.Mesh.ArrayFormat Weights = 2048`

summary: Flattened unsigned normalized 16-bit skin weights.
