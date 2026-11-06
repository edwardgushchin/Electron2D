# Canvas shadow atlas

Last updated: 2026-11-06

## Scope and API

RenderingServer.CanvasSetShadowTextureSize(int size) controls actual point and directional shadow row texels for all canvases in its active session. Values at or below one select one; other requests round upward to a power of two. The supported axis bound is 16384; larger requests fail atomically. The owner/capture/submission rules apply even to equal values. Startup reads the effective registered nonbasic restart setting ProjectSettings.CanvasShadowAtlasSize (`rendering/2d/shadow_atlas/size`, default 2048, valid 1..16384). Runtime changes preserve stored project configuration and reset with the session.

Required context: an Engine.Run session, on its owner outside drawing callbacks:

```csharp
RenderingServer.CanvasSetShadowTextureSize(1025); // 2048 actual shadow texels
```

## Production flow and lifetime

CanvasLightingFrame captures the same transformed finite scene/server/tile contours and source records as ordinary lights and SDF consumers. Mask and inclusive layer selection, directed-edge culling and current interpolated transforms determine depth. Each shadow-enabled source receives one row below the point-mask images in the existing sampleable RGBA32Float atlas. A GPU fragment pass intersects rays with segments and stores nearest depth before receivers execute. No CPU depth rasterization, extra sampler, native export, vendor change or authored resource is introduced.

The atlas belongs to its CanvasRenderTarget. Different viewport sizes/source counts retain independent storage instead of recreating one global texture on every switch. Size changes replace storage at preparation; target retirement/shutdown releases it through the ordinary device ownership. Failed allocation does not discard the old atlas. Stable draws reuse geometry/upload buffers, programs and textures. The shared canvas pipeline cache uses a private record key: nested eight-element tuple hashing allocated 24 managed bytes per lookup on the exercised runtime.

## Projection and filtering

Point rows use periodic polar rays around the transformed source position. This is an explicit backend projection adaptation of the pinned four perspective sectors; texels cover equal angles. Directional rows cover the viewport diagonal orthographically. Their upstream plane is the viewport's minimum projected depth minus max(0, MaxDistance); the downstream plane covers the viewport. This corrects the former finite receiver-ray interpretation of MaxDistance. Contours in range cast shadows over all downstream receivers, independent of light position. Camera/canvas transforms are already in framebuffer coordinates; the distance extension remains in framebuffer pixels.

None/PCF5/PCF13 compare one/five/thirteen nearest depth texels before averaging visibility. ShadowFilterSmooth adds to the unit texel spacing. Point offsets wrap periodically; out-of-range directional samples are unshadowed. Reduce periodic spacing modulo row width and cap directional spacing at that width before multiplying offsets, so even finite float extrema cannot overflow shader coordinates. Integer texel fetches avoid interpolation with adjacent depths, masks or rows. Empty rays store the largest finite float32 depth, preserving visibility even for a finite-extreme ShadowVertex; the preceding 1e30 sentinel failed that public pixel control. A small relative depth tolerance avoids self-shadowing at equal depth. Point masks retain linear texel-center clamping and cannot interpolate into a shadow row.

Fixed float/signed/unsigned surface channels and automatic/authored Light material composition share the same row/atlas metadata. Final fragment ShadowVertex controls lookup position. Directional-only shadow materials retain the atlas descriptor even with no point mask. The existing sixteen-sampler capacity and four user uniform slots remain enforced. Compatibility accepts configuration and unlit rendering, and retains its explicit rejection of actual lighting passes.

## Verification and limits

CanvasShadowAtlasChecks and the public-only CanvasShadowAtlas.Consumer compare 105 complete 128x96 images with a double-precision analytic ray/depth oracle. They cover startup rounding; nonpositive, one, odd, 16384 and rejected sizes; point/directional projection; PCF5/13; fractional and finite extreme smoothing; both cull directions, masks, disabled shadows and fixed/programmed receivers, plus an imported authored Light function and constant ShadowVertex surface override. Separate scene/server rows execute beside an independent SubViewport, including resizing, owner/capture refusal, resource retirement and borrowed masks. Windows Vulkan source profiles pass ordinary, 4X MSAA and HDR rendering; each verifies 64 active and 64 idle prepared draws at zero managed owner bytes, plus 64 alternating multi-viewport draws at zero bytes. Compatibility unlit configuration passes separately. Source, fresh managed-package and strict full-trim NativeAOT each pass 126 public checks on all three GPU profiles and seven compatibility checks. Independent validation accepts 257 automatic-light modules and 33 authored/surface channel/mask modules. The existing native lighting/MSAA/SDF/interpolation/integer/material suites pass with zero prepared owner managed bytes.

The controlled draws call ForceDraw(false) inside readiness. They execute real GPU output without advancing simulation or native input processing; the allocation bracket includes mutation and the complete public draw call, excluding pixel readback/oracle work. Automatic-frame and native-heap allocation qualification, broad performance, owner visual acceptance and the complete ADR 0021 platform/backend matrix remain separate. The initial Windows D3D12 attempt failed creating the existing base canvas pipeline before shadow rendering; these receipts qualify Vulkan only.

Pinned internal source: [resolution control](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/renderer_canvas_render_rd.cpp#L1966), [directional projection](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/renderer_canvas_render_rd.cpp#L1039), [public size contract](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderingServer.xml#L1034).

Source/package/native identities and logs are retained in `C:/Users/eduar/.codex/artifacts/Electron2D/canvas-shadow-atlas-20261106/qualification.json`. The two actual PCF13 captures were inspected; pixel assertions provide the acceptance oracle. These local receipts are not release-package or full native-matrix qualification.
