# GPUCanvasBackend

Last updated: 2026-10-27

Retained particle kernels and imported process programs own
[ComputePipeline](ComputePipeline.md) resources. Metal array-length queries bind
actual step/intake, process/outgoing and subemission source/target byte ranges
through the private size table before each dispatch. State stays on device;
pipeline retirement and renderer teardown release their tables together.

- Declaration: `internal sealed unsafe class GPUCanvasBackend : CanvasBackend`
- Source: [GPUCanvasBackend.cs](../../src/Servers/Rendering/GPUCanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.

## Description

Thin canvas strokes use existing native Lines/LineStrip pipelines. SnapNativeLines rounds original endpoints in the upload copy before the vertex program, without changing shared host geometry; HDR color conversion and canvas modulation keep their existing order.

The SDL GPU implementation of CanvasBackend. Construction retains the display window, acquires a process GPU device lease, claims the window, and builds the default pipeline. Compute consumers acquire separate leases that never retain this window; final device destruction follows the last dependent resource. See [device/window lifetime](../components/gpu-device-lifetime.md). Construction failure unwinds the completed steps. The five built-in fragment/blend pipelines are retained after first use until backend disposal, so temporarily absent blend modes do not recreate pipelines on their next warmed frame. Unused custom shader pipelines retain the existing eviction policy. The backend owns RGBA8 target/vertex/upload buffers, shader pipelines, samplers and per-Texture GPU caches. All calls require the renderer owner thread. Eligible MultiMesh triangles bind a second per-instance input stream from the same retained upload/vertex buffer, with a distinct built-in vertex shader and pipeline key. Ordinary geometry, compositing and skinning retain their existing vertex layout.

Per-MaterialState sampler-binding and swizzle scratch arrays use weak-key tables, preserving warmed storage across one-frame culling without retaining abandoned materials or native resources. Bindings are refilled before use, including particle compute. Native pipeline eviction remains unchanged; cold reconstruction may allocate. See [scratch lifetime and verification](../components/canvas-item-contract.md#material-scratch-lifetime).

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
| `internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, double time, ReadOnlySpan<CanvasInstance> instances, CanvasLightingFrame lighting)` | [Draw](#draw) |
| `internal override Image Readback(CanvasRenderTarget target, bool backBuffer = false)` | [Readback](#readback) |
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

`internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, double time, ReadOnlySpan<CanvasInstance> instances, CanvasLightingFrame lighting)`

Prepares program/texture resources before encoding, prunes unused caches, resizes target/buffers, uploads changed pixels and vertices, and emits ordered render batches. Uniforms serialize with material updates. Before each upload, optional reserved TIME receives the current float32 render time at its reflected offset/binding. A shader requiring TIME rejects a clock outside finite float32 range during resource preflight, before encoding or drawing. Programs without TIME use their existing buffers unchanged. TEXTURE binds the command texture or owned white pixel. Submission commits texture upload versions only on success. An unsubmitted command is cancelled unless a swapchain was acquired, in which case it must be submitted for release. Runtime backend switching/device recovery are absent.

### Readback

`internal override Image Readback(CanvasRenderTarget target, bool backBuffer = false)`

Copies the last target through a download transfer buffer and fence, waits for completion and returns a caller-owned tightly packed RGBA8 or linear RGBA half-float Image with its actual target format and four/eight-byte pixel stride. Native staging rows are padded to 64 pixels. Temporary handles are released on failure.

### Disposal

`public override void Dispose()`

Idempotently waits for device idle, releases caches, samplers, target/buffers/shaders, releases the window claim and disposes the device. An idle-wait failure is reported after cleanup. Inherited native-context queries reject GL identities.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

Named AtlasTexture bindings resolve to the terminal source before texture-cache lookup. Views therefore share the native allocation with direct source use, sample full source storage, observe updates, and reject empty/disposed chains before submission. Region/margin remapping belongs to virtual canvas drawing, not named material binding.

On the current Linux Wayland Vulkan profile, remapping a previously presented root surface rejects with NotSupportedException before native/managed visibility changes. Wayland protocol traces show buffer state retained at xdg_surface recreation even after GPU idle, swapchain release and SDL.SyncWindow; trigger: an SDL-owned native presentation-completion/unmap acknowledgement bridge or verified backend/compositor correction. Hiding succeeds, offscreen targets continue, compatibility remapping executes. This is an explicit platform capability gap, not a completed Show path.

Native group/copy boundaries end a render pass before switching attachments, copying a region or generating mipmaps. Screen sampling never reads the active writable group attachment. Default group output uses premultiplied storage/tint/blending; a cached overwrite pipeline clears the group region. Sampler roles resolve SCREEN_TEXTURE separately from ordinary texture resources. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

ClipChildren MaskBegin copies main background into BackBuffer, then captures children. MaskEnd restores main drawing. The built-in Clip shader separately samples command alpha and screen RGB using physical inverse dimensions; AndDraw reuses uploaded owner vertices for its early pass. Nested/writable-storage hazards reject before Draw. See [alpha masks](../components/canvas-rendering.md#canvas-alpha-masks).

[Automatic scene lighting](../components/shader-materials.md#automatic-scene-lighting) composes the computed fragment color before blend, preserving parameters, texture roles, discard, returns, reload and hardware instancing. Normal/specular and point-mask samplers share the native sixteen-binding stage capacity; four user uniform slots remain available. Current rendered evidence is Linux/Vulkan.

## Device export integration

[GPUCanvasBackend.DeviceTextures.cs](../../src/Servers/Rendering/GPUCanvasBackend.DeviceTextures.cs) creates/reuses the actual upload cache allocation and exports completed viewport target pairs. Borrowed ordinary/array exports retain cache residency until release or retirement. Allocation metadata and optional storage flags match native format/usage support. Device upload versions commit before canvas submission to avoid a second upload overwriting device writes. RenderingServer.TextureGetRDTexture exports actual renderer-managed ordinary uploads, independent arrays and completed viewport images to the retained device without CPU copying. Compatible authored updates keep the RID; replacing allocation, disposing the source, rebinding the viewport or retiring its target expires it. A viewport RID resolves the completed ping-pong image when each command is recorded. FreeRID releases only the borrowed wrapper; queued native operations retain the old allocation through completion. Device writes change GPU storage, while authored CPU snapshots remain unchanged; the next authored update overwrites device changes. Sampling, raw update/copy/read and supported storage roles reflect actual native allocation flags. Prepare changed uploads before starting a draw list. Empty storage, unrendered targets and the compatibility renderer return an empty RID. Distinct sRGB views still require native format-view ownership.

GPUCanvasBackend.Glyphs composes and caches encoded coverage programs on cold source/mode identities, binds an extra backing-image sampler for materials without TEXTURE and evicts unused compositions with native pipelines. LCD adds a component-blend pipeline key and per-batch blend constant; masks use ordinary scalar-alpha blend and transparent groups use their rewritten premultiplied capture. Integer sampled values reject this coverage profile. Glyph stages share the existing device, target and MSAA resolution path.

[HDR target/color helpers](../../src/Servers/Rendering/GPUCanvasBackend.HDR.cs) retain half-float derived source views, refresh native mip data before consumer filtering, and encode root presentation into a separate SDR target. Vertex frame constants retain per-batch canvas color independently of command/mesh colors; shared color/mask ranges always convert their original source. Readback and borrowed device identities report actual target precision. See [HDR canvas behavior and verification](../components/hdr-canvas.md).

`internal override void Present(ReadOnlySpan<CanvasPresentation> viewports)` composes completed native images at clipped client rectangles after drawing, replacing destination RGBA and clearing uncovered pixels black. The shared contract declares this operation abstract. An empty list clears the implicit zero-raster root presentation. ReadbackScreen returns the latest completed composition independently of source texture updates. See [screen attachment](../components/canvas-rendering.md#viewport-screen-attachment).

## Authored vertex stages

Native pipeline identity includes the selected authored vertex variant as well as fragment payload, blend, instancing, sample count, glyph mode and target format. Fragment and vertex samplers bind separately from the combined logical material resources; extra light/glyph resources use the fragment prefix count. Shader reload retires unused stage combinations through the existing frame lifetime. Renderer-owned mask/group passes keep their built-in vertices; authored group material draws use their own vertex stage. CanvasVertexProgramTests verifies pixel output and prepared owner-frame managed allocation on Linux X11/Wayland.

Programmable pipeline usage retains one previous frame. Alternating instance culling reuses its prepared pipeline; two consecutive unused frames retire it, and shutdown disposes all retained native pipelines.

## Procedural mesh integration

Procedural pipeline identity includes native topology and composed vertex code. Built-in mask/group passes preserve authored generated vertex execution and bind its vertex textures. Frame private metadata carries snapping/group UV policy without changing the public Frame schema. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).

## Writable instance consumer

Exported/indirect MultiMesh storage selects a retained GPU consumer. CanvasMesh captures source vertices/indices, palette deformation, LOD metadata and finite authored canvas state. CanvasMultiMesh retains manual/authored bounds and resource guards. GPUCanvasBackend owns conversion outputs, native index/LOD buffers and indirect commands; the private kernel interpolates raw device records and chooses each instance's LOD. CanvasVertexProgram snaps ordinary instance positions before the authored stage and procedural homogeneous output after it; built-in instance vertices support snapping and group screen UVs. Group color premultiplication, masks and original vertex IDs remain connected. Relaxed Android devices bind each LOD instance separately when nonzero indirect first-instance support is unavailable. See [ownership and Linux-native verification limits](../components/meshes.md#writable-instance-device-buffers).

## Retained particles

[GPUCanvasBackend.Particles.cs](../../src/Servers/Rendering/GPUCanvasBackend.Particles.cs) prepares the source graph, validates budgets and borrowed materials, records integration/custom compute/queue/history/sort work and commits CPU timing only after successful native submission. ParticleCheckpoint retains the rollback fields; ParticleQueueParameters carries planar queue conversion. A later draw-only cancellation also invalidates changed geometry uploads when simulation already committed in another viewport. Explicit detached requests reuse an owned one-pixel canvas target with an empty SDF field. Custom compute pipelines retain one previous frame before eviction.

[GPUCanvasBackend.ParticleVertices.cs](../../src/Servers/Rendering/GPUCanvasBackend.ParticleVertices.cs) owns expanded vertex and indirect-draw streams. ParticleVertexParameters carries canvas/color/layout/history/sheet inputs; ParticleStream retires its native vertex/command buffers. Conversion preserves the existing CanvasVertex layout, uses eight stored skin influences and omits inactive draws. Native history/order/state stay on the GPU. See [GPU particles](../components/gpu-particles.md) for the executing contract and exact acceptance limits.

The [independent native window path](../components/window-runtime.md#independent-native-windows) retains one scene renderer and display service. GPU surfaces share the authoritative device/targets; compatibility surfaces use the main driver and replay frozen submissions after cold history import. Native window IDs expire on child hide/detachment; viewport resources survive hiding.

[Loading-image presentation](../components/boot-images.md) reuses ordinary color targets and native presentation without mutating scene viewport targets. ReleaseTemporaryTexture removes a cold loading image from the native image cache immediately after submission, including failed attempts; no caller-owned image resource is disposed.

## Host platform integration

Startup captures support for every format in each ordinary native BC upload family after the selected device exists. HasOSFeature reads immutable s3tc/rgtc/bptc bits; it never creates a device, retains no resource beyond the existing backend and allocates no prepared managed state. Shutdown retires the published service snapshot.

A private ShaderSamplerCache prepares explicit per-image state before render/compute passes. Ordinary, global and layered images use the selected handle; TEXTURE/SCREEN_TEXTURE/SDF_TEXTURE preserve it while the renderer replaces the image. Canvas and particle execution share the configuration. BeginFrame advances cache retirement even after an earlier failed frame; teardown disposes retained samplers. See [explicit image sampler state](../components/shader-materials.md#explicit-image-sampler-state).

Glyph program tracking now retains the previous frame use set. Alternating plain/MSDF modes reuse transformed bytecode and native pipelines instead of repeatedly composing a dropped program. The public retained-mode consumer measures 64 active and 64 idle complete frames after warmup. See [retained distance-field mode](../components/canvas-rendering.md#retained-distance-field-mode).

Graph image policy resolves render-only ordinary/array fallback images and shader-domain sampling. Current canvas normal/specular channels use the existing producers, including missing-channel fallbacks. GPU color views refresh every used native array layer/mip before HDR sampling; prepared sampler/image caches share session retirement. Device clears and cold pixel checkpoints are verified separately from measured complete frames.

## Device screen presentation

Renderer-owned device screen images use the native swapchain color format and pixel dimensions, then copy on GPU into the same final presentation image. Main and child windows retain one presentation owner each. ReadbackScreen observes that exact submitted image; canvas targets keep their own pixels. Main hide retains the last known native format before releasing its claim, while the existing Wayland remap restriction remains explicit. See [native screen drawing](../components/local-compute.md#native-screen-drawing).

Canvas command texture size uses the renderer-owned float2 `TEXTURE_PIXEL_SIZE` contract in both canvas stages; it follows the resolved diffuse image logical metadata, independently of material samplers. See [command texture pixel size](../components/visual-shaders.md#command-texture-pixel-size) for authoring, stage/lifetime rules and verification limits.

[Mesh graph inputs](../components/visual-shaders.md#mesh-channels-and-logical-instance-inputs) retain decoded custom channels and exact logical instance IDs across hardware streams, CPU-expanded snapping and sorted GPU particles. The private CanvasVertex record is 172 bytes, with uint2 identities at byte 164/input location 15; glyph fields retain their existing offsets. Native and allocation verification is scoped by that contract.

Consuming fragment programs receive retained source regions or logical full-image pixel extents and the existing raw/HDR specular color/shininess factor through ordinary uniform buffers. See [canvas command metadata](../components/canvas-command-metadata.md) for semantics and verification.

## Canvas point integration

See [canvas point programs](../components/canvas-points.md) for native topology, the shared sized-point device capability and cold program validation. Authored sizes are preserved after the one-pixel default initialization; optional capabilities retain explicit rejection and device fallback.
