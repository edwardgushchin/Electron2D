# Mesh.PrimitiveType

Last updated: 2026-10-02

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.Mesh.PrimitiveType` · **Source:** [Mesh.cs](../../src/Scene/Resources/Mesh.cs).

Typed enum or managed payload.

## Description

Retains the implemented numeric topology/channel policy identities; unsupported values reject or remain absent at exact coverage dependencies.

## Example

```csharp
var value = Mesh.PrimitiveType.LineStrip;
```

## Values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.Mesh.PrimitiveType LineStrip = 2` | Connected one-framebuffer-pixel line segments. |
| `public const Electron2D.Mesh.PrimitiveType Lines = 1` | Independent pairs of one-framebuffer-pixel line vertices. |
| `public const Electron2D.Mesh.PrimitiveType Points = 0` | Independent one-framebuffer-pixel points. |
| `public const Electron2D.Mesh.PrimitiveType TriangleStrip = 4` | Connected triangles with alternating winding. |
| `public const Electron2D.Mesh.PrimitiveType Triangles = 3` | Independent triangles. |

## Values descriptions

### LineStrip

`public const Electron2D.Mesh.PrimitiveType LineStrip = 2`

Summary: Connected one-framebuffer-pixel line segments.


### Lines

`public const Electron2D.Mesh.PrimitiveType Lines = 1`

Summary: Independent pairs of one-framebuffer-pixel line vertices.


### Points

`public const Electron2D.Mesh.PrimitiveType Points = 0`

Summary: Independent one-framebuffer-pixel points.


### TriangleStrip

`public const Electron2D.Mesh.PrimitiveType TriangleStrip = 4`

Summary: Connected triangles with alternating winding.


### Triangles

`public const Electron2D.Mesh.PrimitiveType Triangles = 3`

Summary: Independent triangles.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.
