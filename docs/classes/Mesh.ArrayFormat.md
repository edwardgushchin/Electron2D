# Mesh.ArrayFormat

Last updated: 2026-10-02

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
