# CanvasVertex

Last updated: 2026-10-14

- Declaration: `internal readonly record struct CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default)`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The sequential 164-byte vertex layout shared by canvas geometry and both native backends. Position is in framebuffer pixels, Color contains the multiplied drawing/modulation color, and UV contains normalized sampling coordinates. GPU attributes use float2 at offset 0/location 0, float4 at offset 8/location 1, and float2 at offset 24/location 2. Compatibility copies the same values into SDL vertices. This value owns no resources.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default)` | [Construction and values](#construction-and-values) |

## Member descriptions

### Construction and values

`CanvasVertex(Vector2 Position, Color Color, Vector2 UV = default)`

The positional constructor sets the record properties; UV defaults to zero for untextured geometry. Upstream recording/tessellation validates finite values. Value copies retain identical coordinates and color; no native pointers are stored.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.

The complete constructor also carries InstanceCustom, UV2, Normal, Tangent, Custom0 through Custom3 and Glyph. Earlier attributes retain their offsets; Glyph is float4 at byte 148/input location 14, delivered flat at fragment location 11. Its private mode/range/outline controls engine LCD/MSDF decoding. None leaves ordinary geometry unchanged. Compatibility consumes the initial position/color/UV fields and rejects non-None glyph batches. See [glyph contract](../components/canvas-rendering.md#lcd-and-msdf-texture-regions).
