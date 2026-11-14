# MeshPrimitiveType

Last updated: 2026-11-14

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.MeshPrimitiveType` · **Source:** [MeshPrimitiveType.cs](../../src/Scene/Resources/MeshPrimitiveType.cs).

Shared topology selector for mesh resources and RenderingServer.

## Description

Selects how sequential vertices or indices form canvas geometry. The first five values are selectable. Max is the exclusive upper bound; authoring and packed import reject it, as well as values outside the enum. Archives continue to store topology as a 32-bit integer.

## Example

```csharp
var value = MeshPrimitiveType.LineStrip;
```

## Values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.MeshPrimitiveType LineStrip = 2` | Connected one-framebuffer-pixel line segments. |
| `public const Electron2D.MeshPrimitiveType Lines = 1` | Independent pairs of one-framebuffer-pixel line vertices. |
| `public const Electron2D.MeshPrimitiveType Max = 5` | Exclusive upper bound; cannot be selected for a surface. |
| `public const Electron2D.MeshPrimitiveType Points = 0` | Independent points, one framebuffer pixel unless an authored vertex output changes their diameter. |
| `public const Electron2D.MeshPrimitiveType TriangleStrip = 4` | Connected triangles with alternating winding. |
| `public const Electron2D.MeshPrimitiveType Triangles = 3` | Independent triangles. |

## Values descriptions

### LineStrip

`public const Electron2D.MeshPrimitiveType LineStrip = 2`

Summary: Connected one-framebuffer-pixel line segments.


### Lines

`public const Electron2D.MeshPrimitiveType Lines = 1`

Summary: Independent pairs of one-framebuffer-pixel line vertices.


### Max

`public const Electron2D.MeshPrimitiveType Max = 5`

Summary: Exclusive upper bound; cannot be selected for a surface.

### Points

`public const Electron2D.MeshPrimitiveType Points = 0`

Summary: Independent points, one framebuffer pixel unless an authored vertex output changes their diameter.


### TriangleStrip

`public const Electron2D.MeshPrimitiveType TriangleStrip = 4`

Summary: Connected triangles with alternating winding.


### Triangles

`public const Electron2D.MeshPrimitiveType Triangles = 3`

Summary: Independent triangles.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) owns typed arrays, pinned exercised byte packing and deferred deformation/channel consumers. [Mesh component](../components/meshes.md) records current scope and executable verification. [MeshTests](../../tests/Electron2D.Tests/MeshTests.cs) checks copied state, updates/rollback, topology, callbacks, lifetime, duplication/scene storage and warmed replay. [MeshRenderingTests](../../tests/Electron2D.Tests/MeshRenderingTests.cs) checks real rendered pixels, primitive profiles, owned server lifetime and active/idle frames on GPU and compatibility. Native allocator totals, other platforms and owner acceptance remain unverified.
