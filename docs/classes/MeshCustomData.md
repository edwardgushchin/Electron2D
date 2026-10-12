# MeshCustomData

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.MeshCustomData` · **Source:** [MeshCustomData.cs](../../src/Scene/Resources/MeshCustomData.cs).

## Description

A caller-owned format and packed byte array. Mesh import and surface queries copy the object and bytes. Records use little-endian normalized, half or float components and must match the vertex count. Every decoded value must be finite. This value owns no native handle.

## Example

```csharp
var data = new MeshCustomData
{
    Format = MeshCustomFormat.RGBA8UNORM,
    Data = [255, 0, 0, 255]
};
```

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public MeshCustomData()` | Creates an empty unsigned normalized channel payload. |
| `public System.Byte[] Data { get; set; }` | Gets or sets contiguous little-endian packed records. |
| `public Electron2D.MeshCustomFormat Format { get; set; }` | Gets or sets the packed layout. |

## Member descriptions

### MeshCustomData

`public MeshCustomData()`

summary: Creates an empty unsigned normalized channel payload.

### Data

`public System.Byte[] Data { get; set; }`

summary: Gets or sets contiguous little-endian packed records.

value: Empty initially; consumed data must contain one complete finite record per vertex.

### Format

`public Electron2D.MeshCustomFormat Format { get; set; }`

summary: Gets or sets the packed layout.

value: RGBA8UNORM initially; Max is not accepted as stored geometry.
