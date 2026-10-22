# MaterialState

Last updated: 2026-10-22

- Declaration: `internal sealed class MaterialState`
- Source: [Material.cs](../../src/Scene/Resources/Material.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.

## Description

The material-owned value storage for a single ShaderProgram version. Its Program and Shader references are stable; uniform byte arrays and borrowed texture overrides change under the ShaderMaterial gate. Rendering batches retain this state for the frame. This class owns neither shader/texture resources nor native handles.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
var state = material.GetCanvasState();
state?.PushUniforms(commandBuffer, (float)renderTime, inverseTargetSize);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal MaterialState(object gate, ShaderProgram program, MaterialState? previous, Shader? shader = null)` | [Construction and migration](#construction-and-migration) |
| `internal readonly ShaderProgram Program` | [Program](#program) |
| `internal readonly byte[][] Buffers` | [Buffers](#buffers) |
| `internal readonly Resource?[] Textures` | [Textures](#textures) |
| `internal readonly Shader? Shader` | [Shader](#shader) |
| `internal void CopyTextures(Span<Texture?> target)` | [Texture capture](#texture-capture) |
| `internal void PushUniforms(nint command, float time, Vector2 screenPixelSize)` | [Uniform upload](#uniform-upload) |

## Member descriptions

### Construction and migration

`internal MaterialState(object gate, ShaderProgram program, MaterialState? previous, Shader? shader = null)`

Allocates buffers and texture slots, then initializes each material and Instance value/array element from validated compiled defaults (implicit numeric zero or matrix identity). Reserved TIME starts at zero and is never copied as a user parameter. Migrates explicitly owned prior uniforms by name plus shader element type (including integer signedness) and array length, copying stored components to new offsets/strides. Float2x2 values pass through Transform basis components so changed matrix stride and row/column order do not change the represented value. RGB copies exactly twelve bytes without touching the following scalar; migrates texture overrides by name. The caller holds the old state gate while copying it.

Logical boolean width is part of migration compatibility. Bool/mask defaults come from canonical zero/one component metadata, with false/zero when absent. Matching boolean buffers preserve normalized components; numeric/bool or width changes reset the member.

### Program

`internal readonly ShaderProgram Program`

The matching immutable code/layout version.

### Buffers

`internal readonly byte[][] Buffers`

Owned padded uniform bytes, accessed under the shared material gate.

### Textures

`internal readonly Resource?[] Textures`

Borrowed ordinary or layered texture overrides, indexed by reflected binding. Null resolves the Shader default; sampled-shape migration, disposal and type validation preserve the reflected role.

### Shader

`internal readonly Shader? Shader`

Borrowed resource used to resolve current default textures; a disposed shader fails when its defaults are read.

### Texture capture

`internal void CopyTextures(Span<Texture?> target)`

Requires room for all slots, writes current overrides/defaults under the gate and uses null for reserved TEXTURE. An ordinary missing binding raises InvalidOperationException. Backend preparation clears its scratch array in finally and validates pixel availability/disposal.

### Uniform upload

`internal void PushUniforms(nint command, float time, Vector2 screenPixelSize)`

While holding the same gate used by setters, writes the supplied finite float32 render time to Program.TimeUniform when present, then pushes each complete padded buffer to the command buffer fragment binding. The backend supplies a valid live command. This method does not allocate new value storage.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

CopyTextures leaves renderer-owned command/screen sampler slots null for native binding. PushUniforms additionally writes inverse target dimensions into SCREEN_PIXEL_SIZE under the material lock; TIME remains independent. The state owns no native backbuffer. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

PushUniforms additionally accepts the owning CanvasItem. It selects reusable item bytes for Instance blocks and shared bytes for other blocks immediately before drawing; reserved renderer values remain shared engine bindings. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

## Stage upload

SynchronizeSharedUniforms(byte[]?[]? instanceBuffers = null) copies canonical logical values to their other-stage layouts after renderer builtin and per-item preparation. Arrays preserve element strides; matrices round-trip through Transform basis to honor independent row/column layouts. Effective per-item storage is used when present. PushUniforms uploads the fragment buffer prefix to fragment bindings, then remaining buffers to vertex bindings starting at one; vertex binding zero remains renderer-owned. Prepared propagation and native submission allocate no managed storage.

## Sampled descriptor array integration

Textures holds one borrowed override per physical descriptor element. Migration matches Name, Element and the complete sampled type/descriptor length. CopyTextures resolves each missing override against Shader.DefaultTexture(name, element), raising for a missing effective element before submission. Shared stage slots reuse matching logical elements and retain independent native binding maps. See [the component contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays).

## Particle integration

The retained particle compute path pushes typed user/global/TIME/SDF uniforms to compute slots one through three while the renderer owns frame slot zero. Canvas fragment/vertex pushing keeps its existing separate stage mapping. See [GPU particles](../components/gpu-particles.md).

SamplerConfiguration?[] Samplers aligns with the immutable program image slots. CaptureSamplerStates refreshes copied Shader metadata during image preparation, preserving an old material program only for currently compatible image types. It allocates no new per-frame table. Drawable blits capture the same metadata before submission. See [explicit image sampler state](../components/shader-materials.md#explicit-image-sampler-state).

## Compiled defaults

`UniformOverrides` retains exact names written through numeric/matrix setters. A compatible program transition copies only those values; unset members are initialized from the new immutable ShaderUniform default. Reset writes the current default and removes its ownership flag. Fallback Instance buffers carry the same defaults when no per-item owner is supplied. See [authored uniform defaults](../components/shader-materials.md#authored-uniform-defaults).

CopyTextures preserves authored/default/global absence for validated graph image policies, leaving render-only fallback selection to the GPU backend. Legacy unassigned images without such policy continue to reject. Renderer-owned channel/screen images do not acquire material ownership.

Image value migration requires material ownership in both program versions. A screen-owned replacement drops the prior material resource instead of retaining a hidden override for a later reload. Caller-owned resources remain live.
