# MeshCustomFormat

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.MeshCustomFormat` · **Source:** [MeshCustomData.cs](../../src/Scene/Resources/MeshCustomData.cs).

## Description

One shared packed-layout contract for the four independent numerical mesh custom channels. Max denotes an unselected authoring channel and is not a stored layout.

## Example

```csharp
var format = MeshCustomFormat.RGBA8UNORM;
```

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.MeshCustomFormat Max = 8` | No selected authoring channel; not a storage format. |
| `public const Electron2D.MeshCustomFormat RFloat = 4` | One little-endian float32 component. |
| `public const Electron2D.MeshCustomFormat RGBA8SNORM = 1` | Four signed normalized eight-bit components. |
| `public const Electron2D.MeshCustomFormat RGBA8UNORM = 0` | Four unsigned normalized eight-bit components. |
| `public const Electron2D.MeshCustomFormat RGBAFloat = 7` | Four little-endian float32 components. |
| `public const Electron2D.MeshCustomFormat RGBAHalf = 3` | Four little-endian binary16 components. |
| `public const Electron2D.MeshCustomFormat RGBFloat = 6` | Three little-endian float32 components. |
| `public const Electron2D.MeshCustomFormat RGFloat = 5` | Two little-endian float32 components. |
| `public const Electron2D.MeshCustomFormat RGHalf = 2` | Two little-endian binary16 components. |

## Member descriptions

### Max

`public const Electron2D.MeshCustomFormat Max = 8`

summary: No selected authoring channel; not a storage format.

### RFloat

`public const Electron2D.MeshCustomFormat RFloat = 4`

summary: One little-endian float32 component.

### RGBA8SNORM

`public const Electron2D.MeshCustomFormat RGBA8SNORM = 1`

summary: Four signed normalized eight-bit components.

### RGBA8UNORM

`public const Electron2D.MeshCustomFormat RGBA8UNORM = 0`

summary: Four unsigned normalized eight-bit components.

### RGBAFloat

`public const Electron2D.MeshCustomFormat RGBAFloat = 7`

summary: Four little-endian float32 components.

### RGBAHalf

`public const Electron2D.MeshCustomFormat RGBAHalf = 3`

summary: Four little-endian binary16 components.

### RGBFloat

`public const Electron2D.MeshCustomFormat RGBFloat = 6`

summary: Three little-endian float32 components.

### RGFloat

`public const Electron2D.MeshCustomFormat RGFloat = 5`

summary: Two little-endian float32 components.

### RGHalf

`public const Electron2D.MeshCustomFormat RGHalf = 2`

summary: Two little-endian binary16 components.
