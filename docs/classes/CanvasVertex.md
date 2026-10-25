# CanvasVertex

Last updated: 2026-10-25

- Declaration: `internal readonly record struct CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default, Color InstanceCustom = default, Vector2 UV2 = default, Vector3 Normal = default, Vector4 Tangent = default, Vector4 Custom0 = default, Vector4 Custom1 = default, Vector4 Custom2 = default, Vector4 Custom3 = default, Vector4 Glyph = default, uint VertexID = uint.MaxValue, uint InstanceID = uint.MaxValue)`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The sequential 172-byte vertex layout shared by canvas geometry and both native backends. Position is in framebuffer pixels, Color contains the multiplied drawing/modulation color, and UV contains normalized sampling coordinates. GPU attributes use float2 at offset 0/location 0, float4 at offset 8/location 1, and float2 at offset 24/location 2. Compatibility copies the same values into SDL vertices. This value owns no resources.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default, Color InstanceCustom = default, Vector2 UV2 = default, Vector3 Normal = default, Vector4 Tangent = default, Vector4 Custom0 = default, Vector4 Custom1 = default, Vector4 Custom2 = default, Vector4 Custom3 = default, Vector4 Glyph = default, uint VertexID = uint.MaxValue, uint InstanceID = uint.MaxValue)` | [Construction and values](#construction-and-values) |

## Member descriptions

### Construction and values

`CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default, Color InstanceCustom = default, Vector2 UV2 = default, Vector3 Normal = default, Vector4 Tangent = default, Vector4 Custom0 = default, Vector4 Custom1 = default, Vector4 Custom2 = default, Vector4 Custom3 = default, Vector4 Glyph = default, uint VertexID = uint.MaxValue, uint InstanceID = uint.MaxValue)`

The positional constructor sets the record properties; UV defaults to zero for untextured geometry. Upstream recording/tessellation validates finite values. Value copies retain identical coordinates and color; no native pointers are stored.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.

The complete constructor also carries InstanceCustom, UV2, Normal, Tangent, Custom0 through Custom3 and Glyph. Earlier attributes retain their offsets; Glyph is float4 at byte 148/input location 14, delivered flat at fragment location 11. Its private mode/range/outline controls engine LCD/MSDF decoding. None leaves ordinary geometry unchanged. Compatibility consumes the initial position/color/UV fields and rejects non-None glyph batches. See [glyph contract](../components/canvas-rendering.md#lcd-and-msdf-texture-regions).

Private input location 15 is uint2 at byte 164: original vertex ID followed by original instance ID at byte 168. An absent identity uses uint.MaxValue. CPU-expanded MultiMesh and GPU particle conversion publish source instance slots independently of custom/glyph data; authored vertex variants consume the pair before graph execution. See [mesh graph inputs](../components/visual-shaders.md#mesh-channels-and-logical-instance-inputs).
