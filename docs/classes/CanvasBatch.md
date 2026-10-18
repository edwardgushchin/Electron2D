# CanvasBatch

Last updated: 2026-10-14

- Declaration: `internal readonly record struct CanvasBatch`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A contiguous vertex range with one borrowed material state, command texture, filter, addressing mode and anisotropy limit. CanvasItem.AppendCanvas coalesces adjacent ranges only when those values match, preserving draw order. Instanced ranges additionally reference FirstInstance/InstanceCount in the owning viewport frame and do not merge with ordinary vertex ranges. Backend caches own native resources; this record owns none.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null, TextureFilter Filter = TextureFilter.Nearest, TextureRepeat Repeat = TextureRepeat.Disabled, int MaxAnisotropy = 1, BlendMode Blend = BlendMode.Mix, Rect2i? Clip = null, CanvasOperation Operation = CanvasOperation.Draw, Rect2i Region = default, bool MIPMaps = false, bool GroupShader = false, bool MaskShader = false, int FirstInstance = 0, int InstanceCount = 0, CanvasLightingItem? Lighting = null, CanvasItem? InstanceOwner = null, CanvasGlyph Glyph = default, Color GlyphModulation = default, bool LinearColors = false, Color? CanvasModulation = null, Rect2? VisibilityBounds = null)` | [Construction and values](#construction-and-values) |
| `internal byte[]? ShaderCode { get; }` | [Shader code](#shader-code) |

## Member descriptions

### Construction and values

`CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null, TextureFilter Filter = TextureFilter.Nearest, TextureRepeat Repeat = TextureRepeat.Disabled, int MaxAnisotropy = 1, BlendMode Blend = BlendMode.Mix, Rect2i? Clip = null, CanvasOperation Operation = CanvasOperation.Draw, Rect2i Region = default, bool MIPMaps = false, bool GroupShader = false, bool MaskShader = false, int FirstInstance = 0, int InstanceCount = 0, CanvasLightingItem? Lighting = null, CanvasItem? InstanceOwner = null, CanvasGlyph Glyph = default, Color GlyphModulation = default, bool LinearColors = false, Color? CanvasModulation = null, Rect2? VisibilityBounds = null)`

First and Count index the prepared triangle list. Material null selects the built-in program. Texture null means opaque white for the built-in command sampler. Filter, Repeat and MaxAnisotropy are resolved per item before batching; tiled commands force Enabled addressing. These values participate in batch equality. These arguments become record properties.

### Shader code

`internal byte[]? ShaderCode { get; }`

Returns Material.Program.Code or null for the default program. The byte array is shared immutable runtime state, never returned directly through public Shader APIs.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.

The complete internal constructor also carries BlendMode Blend, nullable Clip, CanvasOperation Operation, Region, MIPMaps and GroupShader fields. Draw ranges can coalesce only with Draw ranges; Copy/GroupBegin/GroupEnd preserve render boundaries and borrowed material lifetime. Zero-count control operations are valid. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

MaskShader selects the built-in final alpha mask while preserving command texture/UV/color. MaskBegin/MaskEnd/MaskFinish carry capture/finalization boundaries and fitted screen regions. Materials are borrowed; the built-in AndDraw final pass deliberately clears its material state.

InstanceOwner carries the CanvasItem supplying per-item uniform buffers. Adjacent batches merge only when this owner also matches. Group/mask operations preserve it through batch rewrites. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

Glyph carries LCD/MSDF mode and converted range/outline policy; GlyphModulation carries the LCD blend constant. Both participate in ordinary batch equality. CanvasMesh rejects merging into glyph ranges. Built-in masks retain scalar glyph coverage; built-in transparent group rewrites clear glyph state after restoring LCD tint. See [glyph regions](../components/canvas-rendering.md#lcd-and-msdf-texture-regions).

Manual mesh visibility rectangles are carried in retained batches for group fitting; they replace the batch's CPU geometry bounds. Batches with different visibility policies remain distinct. See [manual mesh bounds](../components/meshes.md#manual-mesh-visibility-bounds).
