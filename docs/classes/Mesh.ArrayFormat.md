# Mesh.ArrayFormat

Last updated: 2026-10-18

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.Mesh.ArrayFormat` · **Source:** [Mesh.cs](../../src/Scene/Resources/Mesh.cs).

Typed enum or managed payload.

## Description

Retains the implemented numeric topology/channel policy identities; unsupported values reject or remain absent at exact coverage dependencies.

## Example

```csharp
var value = Mesh.ArrayFormat.Color;
```


## Complete API

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.Mesh.ArrayFormat BlendShapeMask = 7` | Position, normal and tangent channels permitted in a blend-shape payload. |
| `public const Electron2D.Mesh.ArrayFormat Bones = 1024` | Flattened unsigned 16-bit bone indices. |
| `public const Electron2D.Mesh.ArrayFormat CompressFlagsBase = 25` | First policy flag bit. |
| `public const Electron2D.Mesh.ArrayFormat CompressAttributes = 536870912` | Packed UV and joint numerical frame storage; positions retain float XY. |
| `public const Electron2D.Mesh.ArrayFormat Color = 8` | Vertex colors. |
| `public const Electron2D.Mesh.ArrayFormat Custom0 = 64` | Packed numerical custom channel. |
| `public const Electron2D.Mesh.ArrayFormat Custom0Shift = 13` | First bit position of the three-bit Custom0 format selector. |
| `public const Electron2D.Mesh.ArrayFormat Custom1 = 128` | Packed numerical custom channel. |
| `public const Electron2D.Mesh.ArrayFormat Custom1Shift = 16` | First bit position of the three-bit Custom1 format selector. |
| `public const Electron2D.Mesh.ArrayFormat Custom2 = 256` | Packed numerical custom channel. |
| `public const Electron2D.Mesh.ArrayFormat Custom2Shift = 19` | First bit position of the three-bit Custom2 format selector. |
| `public const Electron2D.Mesh.ArrayFormat Custom3 = 512` | Packed numerical custom channel. |
| `public const Electron2D.Mesh.ArrayFormat Custom3Shift = 22` | First bit position of the three-bit Custom3 format selector. |
| `public const Electron2D.Mesh.ArrayFormat CustomBase = 13` | First custom format selector bit position. |
| `public const Electron2D.Mesh.ArrayFormat CustomBits = 3` | Number of bits per custom format selector. |
| `public const Electron2D.Mesh.ArrayFormat CustomMask = 7` | Unshifted three-bit custom format value mask. |
| `public const Electron2D.Mesh.ArrayFormat Index = 4096` | Vertex ordering indices. |
| `public const Electron2D.Mesh.ArrayFormat None = 0` | No channels or policies. |
| `public const Electron2D.Mesh.ArrayFormat Normal = 2` | Octahedrally packed mesh-local normal directions. |
| `public const Electron2D.Mesh.ArrayFormat Tangent = 4` | Octahedrally packed tangent directions and orientation. |
| `public const Electron2D.Mesh.ArrayFormat TexUV = 16` | Primary texture coordinates. |
| `public const Electron2D.Mesh.ArrayFormat TexUV2 = 32` | Secondary texture coordinates. |
| `public const Electron2D.Mesh.ArrayFormat Use2DVertices = 33554432` | Vertex positions use two floating components. |
| `public const Electron2D.Mesh.ArrayFormat Use8BoneWeights = 134217728` | Skin records contain eight indices and eight weights instead of four. |
| `public const Electron2D.Mesh.ArrayFormat UseDynamicUpdate = 67108864` | Surface buffers accept explicit region updates. |
| `public const Electron2D.Mesh.ArrayFormat UseEmptyVertexArray = 268435456` | Permits absent position storage and generated shader geometry. |
| `public const Electron2D.Mesh.ArrayFormat Vertex = 1` | Vertex positions. |
| `public const Electron2D.Mesh.ArrayFormat Weights = 2048` | Flattened unsigned normalized 16-bit skin weights. |

## Member descriptions

### BlendShapeMask

`public const Electron2D.Mesh.ArrayFormat BlendShapeMask = 7`

summary: Position, normal and tangent channels permitted in a blend-shape payload.

### Bones

`public const Electron2D.Mesh.ArrayFormat Bones = 1024`

summary: Flattened unsigned 16-bit bone indices.

### Color

`public const Electron2D.Mesh.ArrayFormat Color = 8`

summary: Vertex colors.

### Custom0

`public const Electron2D.Mesh.ArrayFormat Custom0 = 64`

summary: Packed numerical custom channel.

### Custom0Shift

`public const Electron2D.Mesh.ArrayFormat Custom0Shift = 13`

summary: First bit position of the three-bit Custom0 format selector.

### Custom1

`public const Electron2D.Mesh.ArrayFormat Custom1 = 128`

summary: Packed numerical custom channel.

### Custom1Shift

`public const Electron2D.Mesh.ArrayFormat Custom1Shift = 16`

summary: First bit position of the three-bit Custom1 format selector.

### Custom2

`public const Electron2D.Mesh.ArrayFormat Custom2 = 256`

summary: Packed numerical custom channel.

### Custom2Shift

`public const Electron2D.Mesh.ArrayFormat Custom2Shift = 19`

summary: First bit position of the three-bit Custom2 format selector.

### Custom3

`public const Electron2D.Mesh.ArrayFormat Custom3 = 512`

summary: Packed numerical custom channel.

### Custom3Shift

`public const Electron2D.Mesh.ArrayFormat Custom3Shift = 22`

summary: First bit position of the three-bit Custom3 format selector.

### CustomBase

`public const Electron2D.Mesh.ArrayFormat CustomBase = 13`

summary: First custom format selector bit position.

### CustomBits

`public const Electron2D.Mesh.ArrayFormat CustomBits = 3`

summary: Number of bits per custom format selector.

### CustomMask

`public const Electron2D.Mesh.ArrayFormat CustomMask = 7`

summary: Unshifted three-bit custom format value mask.

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


### CompressFlagsBase

`public const Electron2D.Mesh.ArrayFormat CompressFlagsBase = 25`

First bit of the shared storage-policy flags; this is a bit-position value, not a standalone policy mask.

### CompressAttributes

`public const Electron2D.Mesh.ArrayFormat CompressAttributes = 536870912`

Selects four-byte UV UNORM16 pairs and six-byte joint normal/tangent axis/angle records. Position pairs remain float XY; custom/color/skin/index packing is unchanged. See [the compressed attribute contract](../components/meshes.md#compressed-attributes).

### UseEmptyVertexArray

`public const Electron2D.Mesh.ArrayFormat UseEmptyVertexArray = 268435456`

Permits absent positions with explicit channel counts or zero-count uint16 index-only invocations. Retained drawing requires an authored vertex stage and author-owned visibility bounds. CPU face extraction skips flagged surfaces even when positions are supplied. See [procedural surfaces](../components/meshes.md#procedural-surfaces-without-positions).
