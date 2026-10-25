# CanvasBatch

Last updated: 2026-10-25

- Declaration: `internal readonly record struct CanvasBatch`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A contiguous vertex range with one borrowed material state, command texture, filter, addressing mode and anisotropy limit. CanvasItem.AppendCanvas coalesces adjacent ranges only when those values match, preserving draw order. Instanced ranges additionally reference FirstInstance/InstanceCount in the owning viewport frame and do not merge with ordinary vertex ranges. Backend caches own native resources; this record owns none.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null, TextureFilter Filter = TextureFilter.Nearest, TextureRepeat Repeat = TextureRepeat.Disabled, int MaxAnisotropy = 1, BlendMode Blend = BlendMode.Mix, Rect2i? Clip = null, CanvasOperation Operation = CanvasOperation.Draw, Rect2i Region = default, bool MIPMaps = false, bool GroupShader = false, bool MaskShader = false, int FirstInstance = 0, int InstanceCount = 0, CanvasLightingItem? Lighting = null, CanvasItem? InstanceOwner = null, CanvasGlyph Glyph = default, Color GlyphModulation = default, bool LinearColors = false, Color? CanvasModulation = null, Rect2? VisibilityBounds = null, bool Procedural = false, Mesh.PrimitiveType Primitive = Mesh.PrimitiveType.Triangles, bool SnapGeneratedVertices = false, bool SnapNativeLines = false, MultiMeshGPUDraw GPU = default, ParticleGPUDraw? Particles = null, Texture? TextureSource = null, long ObjectCount = 1)` | [Construction and values](#construction-and-values) |
| `internal float RasterMargin { get; }` | Half-pixel native line coverage; one pixel with deferred endpoint snapping; zero for other topology. Used by group fitting and texture-redraw visibility. |
| `internal byte[]? ShaderCode { get; }` | [Shader code](#shader-code) |

## Member descriptions

### Construction and values

`CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null, TextureFilter Filter = TextureFilter.Nearest, TextureRepeat Repeat = TextureRepeat.Disabled, int MaxAnisotropy = 1, BlendMode Blend = BlendMode.Mix, Rect2i? Clip = null, CanvasOperation Operation = CanvasOperation.Draw, Rect2i Region = default, bool MIPMaps = false, bool GroupShader = false, bool MaskShader = false, int FirstInstance = 0, int InstanceCount = 0, CanvasLightingItem? Lighting = null, CanvasItem? InstanceOwner = null, CanvasGlyph Glyph = default, Color GlyphModulation = default, bool LinearColors = false, Color? CanvasModulation = null, Rect2? VisibilityBounds = null, bool Procedural = false, Mesh.PrimitiveType Primitive = Mesh.PrimitiveType.Triangles, bool SnapGeneratedVertices = false, bool SnapNativeLines = false, MultiMeshGPUDraw GPU = default, ParticleGPUDraw? Particles = null, Texture? TextureSource = null, long ObjectCount = 1)`

First and Count index the prepared vertex list. Primitive selects triangle, line or point topology; SnapNativeLines requests endpoint snapping only in native upload. Compatibility native untextured uniform-color lines snap endpoints; textured or interpolated-color ranges expand before corner snapping. Matching topology and snap policy are required for coalescing; separate line strips never merge. Material null selects the built-in program. Texture null means opaque white for the built-in command sampler. Filter, Repeat and MaxAnisotropy are resolved per item before batching; tiled commands force Enabled addressing. These values participate in batch equality. These arguments become record properties.

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

## Sampled descriptor array integration

ShaderCode exposes Material.Program.NativeCode to native submission. Original Shader.GetSPIRV exports remain source payloads. Physical texture slots include every fixed descriptor array element, with per-element override/default resolution and the existing dependency discovery and native binding flow. See [the component contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays).

## Particle integration

Particle batches additionally retain ParticleGPUDraw metadata for native state/geometry conversion and indirect draw selection; ordinary batches retain their existing path. See [GPU particles](../components/gpu-particles.md).

ObjectCount retains generated drawing-record cardinality before compatible batch merging. Merging adds the scalar counts; this is distinct from vertex/instance count. Both native backends consume it only when they issue canvas geometry, and completed frame publication is owned by [canvas counters](../components/canvas-counters.md).

NormalMapFlags retains rectangle flip/transpose state only for programs that consume an implicit canvas normal. It participates in both ordinary and mesh coalescing keys; this record still owns no native resources. See [the connected normal producer](../components/visual-shaders.md#fresnel-and-implicit-canvas-normals).
