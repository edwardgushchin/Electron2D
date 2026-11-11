# Shared texture views

Last updated: 2026-11-11

`RenderingDevice.TextureCreateShared` exposes the original allocation through a copied
`RDTextureView`. `TextureCreateSharedFromSlice` selects one original layer and a
positive mip interval as an ordinary image. The only applicable `TextureSliceType`
is `Texture`. Both factories resolve an existing shared view to its original owner:
indices and default full extent are relative to that owner, not to the source view.
No pixels are copied or periodically synchronized.

`RenderingDevice.TextureIsShared` reports whether a live RID represents one of
these views or an sRGB export. Allocation owners, renderer borrow wrappers and
invalid, expired or foreign RIDs return false.

## sRGB canvas texture exports

`RenderingServer.TextureGetRDTexture(texture, SRGB: true)` returns already-sRGB
storage directly. For an ordinary or array device allocation that declares a
compatible sRGB format, it returns a cached native view of the same bytes. It
preserves the source view's sampled component map and exact physical layer/mip
range, including when a proxy resolves to a device slice. Sampling decodes RGB;
raw readback and updates use the encoded bytes. Creating a new view requires an
idle device. Freeing the export allows a fresh export from the same source.
Freeing its source RID, including a slice, expires the cached export.

The first device export of a compatible renderer upload prepares mutable backing
for the ordinary image or array. This includes first requests for a linear RID.
Any subsequent sRGB export samples the same GPU bytes with an sRGB image view.
The linear RID keeps its native storage-image roles, and the sRGB RID supports
sampling and encoded transfers. Compatible authored updates preserve both RIDs
and overwrite device writes. Source disposal or allocation replacement expires
both. If the canvas already drew the upload, preparation at first export can
allocate new backing and upload the authored bytes again. Images used only by the
canvas keep ordinary SDL allocation.

Renderer uploads support RGBA8 and BC1/2/3/7 when the selected Vulkan device
supports the sampled format and negotiates Vulkan 1.1 or later with the matching
native bridge. Other upload formats and backends reject a distinct sRGB request
when they lack a compatible view. Viewport exports need views for each physical
ping-pong image and must select the completed image at command recording.

Run `ELECTRON2D_TEST_DEVICE_CANVAS_SRGB=vulkan` with `SDL_GPU_DRIVER=vulkan` to
check ordinary, array and swizzled slice sampling in a rendered frame. The
[test](../../tests/Electron2D.Tests/DeviceCanvasSRGBTests.cs) also checks raw
reads and writes, mip/layer isolation, proxy identity, active-list refusal and
view retirement. The `direct3d12` profile checks already-sRGB export and refusal
to create mutable format backing on that adapter. Both profiles pass on the
tested Windows desktop.

Run `ELECTRON2D_TEST_RENDERER_TEXTURE_SRGB=vulkan` with `SDL_GPU_DRIVER=vulkan`
to check the [renderer upload profile](../../tests/Electron2D.Tests/RendererTextureSRGBTests.cs).
It draws the upload before its first export, writes the linear allocation with a
compute shader before asking for sRGB, checks ordinary and array samples against
decoded values, and tests raw bytes, compatible updates, source retirement and
queued readback. When the device exposes BC1, it also verifies decoded BC1 pixels
and the unchanged encoded block. The `direct3d12` and `preceding` profiles check distinct-view
refusal and working linear uploads on Direct3D 12 and the preceding Vulkan bridge.
All three profiles pass on the tested Windows desktop.

## Formats and consumers

A nonempty `RDTextureFormat` shareable list must include the exact allocation
`DataFormat`. Additional formats must have compatible native format classes and
support all requested usage/sample roles. Uncompressed equal-bit classes and
compressed linear/sRGB pairs use actual native image views; depth/stencil keeps
its exact native encoding. A queryable format is not a promise that every device
supports its combination of roles. Unsupported combinations reject before RID
publication. Initial `TextureCreate` format overrides use the same native storage.
`Max` selects the original stored format, including when the source already has
an alternate view.

`TextureGetFormat` copies the stored allocation format and shareable list. For a
slice it reports selected width/height, one layer, the selected mip count and
ordinary-image type. Shader scalar validation, framebuffer encoding and canvas
conversion use the effective view format. Sampled component maps compose with
native format conversion; raw uploads, clears, copies and readbacks retain encoded
storage. Storage images and attachments require identity channel maps.

Views execute through ordinary/array sampling, selected-layer writable image
loads/stores/R32 atomics, color/depth/stencil attachments, multisample color resolve,
readonly input snapshots and retained `TextureRD`/`TextureArrayRD` consumers.
Direct [array storage descriptors](graphics-storage-images.md#compute-and-array-consumers)
now execute native full-layer/single-mip loads, writes and signed/unsigned R32
atomics in compute and independently enabled graphics stages.
Physical range overlap guards cover framebuffer outputs, draw feedback, blits and
self copies. [Equal-count MS copies](multisample-copies.md) preserve each sample in the original selected layer and use matching-count scratch for disjoint same-subresource copies. Disjoint layers/mips remain usable. Sampled siblings of writable
views share the actual image's GENERAL descriptor layout.

`TextureRD.GetImage` preserves raw channels before sampled component maps. An
`Image` requires a complete mip chain; a bounded view with an incomplete chain
returns its exact base level without fabricating lower levels. The GPU resource
and `TextureRD.HasMIPMaps` retain the actual selected mip interval.

## Ownership and native integration

Every shared RID depends directly on the original texture Entry. Freeing an
intermediate view preserves siblings. Freeing the original owner automatically
frees its views and invalidates descriptor sets, framebuffers and canvas resources
that borrow them. Pending transfers finish before removal and callbacks run after
the whole resource family is removed. Renderer-source expiration immediately
invalidates views; the renderer retires their native wrappers at its safe frame
boundary. Authored source updates are prepared through the original borrow.

The private source/hash/version/revision-bound SDL 3.4.18 Vulkan bridge owns real
image views and pinned SDL wrappers. It pins both the source texture and allocator
region against defragmentation, disables native cycling of that source container,
and resolves transfer locations to actual original coordinates. Native references
retain unsubmitted commands; collection waits only when retirement is pending.
The last physical device release drains this cache before destroying the device.
No private native pointer is public and no vendored source is modified.

SDL exposes no mutable-image import/allocation hook. Alternate-format backing uses
a private mutable Vulkan image with its exact device-local memory requirement,
wrapped in the same pinned SDL texture structures. Native texture/total memory
queries include these allocations once; ordinary views add no image payload.
See [render memory](render-memory.md). Unknown cores/backends or missing view
procedures reject explicitly; failed native setup releases views, pins, image and
memory before returning a failure.

Owned implementation: `RenderingDevice.TextureViews.cs`, `GPUTextureViews.cs`,
`tools/render-native/views.c` and the existing texture/transfer/descriptor/renderer
lifetime paths. [ADR 0028](../decisions/rendering.md#adr-0028) owns this boundary.

## Verification and remaining limits

[DeviceGraphicsStorage.Consumer](../../tests/DeviceGraphicsStorage.Consumer/README.md)
provides `views`, `views-source`, `views-canvas`, `views-unavailable` and
`views-failure`. Checks cover distinct layer/mip bytes, initial format overrides,
normalized/integer/float/compressed classes, physical alias writes/atomics,
GLSL/HLSL sampling/query extents, graphics pass/scissor restarts, readonly input
snapshots, MSAA resolve, depth rejection, raw snapshots, borrowed-source expiration,
owner-family retirement, refusal rollback and warmed owner-thread allocations.
`Electron2DRenderCheck` additionally checks defragmentation pins, direct native
nested views, deferred unsubmitted commands and mutable allocation accounting.

Linux/Wayland Vulkan is the current exercised platform gate. Native heap churn,
Metal/D3D12/browser behavior and owner visual acceptance are separate. Native
HLSL input attachments may declare an unspecified depth role; the lowering accepts
that valid role while retaining explicit depth-input rejection, following
[SPIR-V OpTypeImage](https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html#OpTypeImage).

[Depth/stencil transfers](depth-texture-transfers.md) now map shared-view regions to their original physical mip/layer before native aspect copies. View readback exposes its own tight extent; updating a selected view preserves every neighboring original subresource. The original owner and native transfer allocation remain pinned through pending commands.
