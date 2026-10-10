# GPUCanvasBackend

Last updated: 2026-10-10

- Declaration: `internal sealed unsafe class GPUCanvasBackend : CanvasBackend`
- Source: [GPUCanvasBackend.cs](../../src/Servers/Rendering/GPUCanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Compressed/integer formats and foreign/native-allocation acceptance retain exact dependencies.

## Description

The SDL GPU implementation of CanvasBackend. Construction retains the display window, creates a device, claims the window, and builds the default pipeline. Construction failure unwinds the completed steps. The five built-in fragment/blend pipelines are retained after first use until backend disposal, so temporarily absent blend modes do not recreate pipelines on their next warmed frame. Unused custom shader pipelines retain the existing eviction policy. The backend owns RGBA8 target/vertex/upload buffers, shader pipelines, samplers and per-Texture GPU caches. All calls require the renderer owner thread. Eligible MultiMesh triangles bind a second per-instance input stream from the same retained upload/vertex buffer, with a distinct built-in vertex shader and pipeline key. Ordinary geometry, compositing and skinning retain their existing vertex layout.

GPU sampler cache keys include texel filter, repeat, effective anisotropy and mip interpolation. Non-mipmap canvas modes clamp LOD to zero; mip/anisotropic modes use all uploaded levels. Mirror uses native mirrored repeat. Named material samplers use linear filtering, clamp-to-edge coordinates and LOD zero, independently of canvas policies. Cached sampler handles release on backend disposal.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
using CanvasBackend backend = new GPUCanvasBackend(nativeWindow);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal GPUCanvasBackend(SafeHandle window)` | [Construction](#construction) |
| `internal override string Method { get; }` | [Method](#method) |
| `internal override string Driver { get; }` | [Driver](#driver) |
| `internal override Vector2i GetPixelSize()` | [Pixel size](#pixel-size) |
| `internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)` | [Draw](#draw) |
| `internal override Image Readback()` | [Readback](#readback) |
| `public override void Dispose()` | [Disposal](#disposal) |

## Member descriptions

### Construction

`internal GPUCanvasBackend(SafeHandle window)`

Creates the device for formats reported by ShaderCompiler, claims window, loads the built-in vertex shader and default fragment pipeline. Native zero/false results fail explicitly.

### Method

`internal override string Method { get; }`

Always gpu.

### Driver

`internal override string Driver { get; }`

SDL device driver name captured after creation; unknown if SDL supplies no name.

### Pixel size

`internal override Vector2i GetPixelSize()`

Uses the window physical pixel dimensions.

### Draw

`internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)`

Prepares program/texture resources before encoding, prunes unused caches, resizes target/buffers, uploads changed pixels and vertices, and emits ordered render batches. Uniforms serialize with material updates. Before each upload, optional reserved TIME receives the current float32 render time at its reflected offset/binding. A shader requiring TIME rejects a clock outside finite float32 range during resource preflight, before encoding or drawing. Programs without TIME use their existing buffers unchanged. TEXTURE binds the command texture or owned white pixel. Submission commits texture upload versions only on success. An unsubmitted command is cancelled unless a swapchain was acquired, in which case it must be submitted for release. Runtime backend switching/device recovery are absent.

### Readback

`internal override Image Readback()`

Copies the last target through a download transfer buffer and fence, waits for completion and returns a caller-owned tightly packed RGBA8 Image. Native staging rows are padded to 64 pixels. Temporary handles are released on failure.

### Disposal

`public override void Dispose()`

Idempotently waits for device idle, releases caches, samplers, target/buffers/shaders, releases the window claim and disposes the device. An idle-wait failure is reported after cleanup. Inherited native-context queries reject GL identities.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

Named AtlasTexture bindings resolve to the terminal source before texture-cache lookup. Views therefore share the native allocation with direct source use, sample full source storage, observe updates, and reject empty/disposed chains before submission. Region/margin remapping belongs to virtual canvas drawing, not named material binding.

On the current Linux Wayland Vulkan profile, remapping a previously presented root surface rejects with NotSupportedException before native/managed visibility changes. Wayland protocol traces show buffer state retained at xdg_surface recreation even after GPU idle, swapchain release and SDL.SyncWindow; trigger: an SDL-owned native presentation-completion/unmap acknowledgement bridge or verified backend/compositor correction. Hiding succeeds, offscreen targets continue, compatibility remapping executes. This is an explicit platform capability gap, not a completed Show path.

Native group/copy boundaries end a render pass before switching attachments, copying a region or generating mipmaps. Screen sampling never reads the active writable group attachment. Default group output uses premultiplied storage/tint/blending; a cached overwrite pipeline clears the group region. Sampler roles resolve SCREEN_TEXTURE separately from ordinary texture resources. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

ClipChildren MaskBegin copies main background into BackBuffer, then captures children. MaskEnd restores main drawing. The built-in Clip shader separately samples command alpha and screen RGB using physical inverse dimensions; AndDraw reuses uploaded owner vertices for its early pass. Nested/writable-storage hazards reject before Draw. See [alpha masks](../components/canvas-rendering.md#canvas-alpha-masks).
