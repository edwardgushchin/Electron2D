# Shared texture views

Last updated: 2026-11-12

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

Renderer uploads support RGBA8 and BC1/2/3/7 with the matching Vulkan or
Direct3D 12 adapter and device sampling support. Vulkan requires 1.1 or later.
Unsupported upload formats and backends reject a distinct sRGB request.

The first export of a completed SDR viewport prepares both physical images for
compatible views on the Vulkan or Direct3D 12 adapter. One GPU copy of each physical image
preserves the completed frame and the alternate buffer before publishing a
device RID. Later exports reuse that pair. The linear RID retains attachment and
available storage roles. The sRGB RID uses a sampled view of each physical image
with raw transfers. Both RIDs and views made with the shared factories select
the completed buffer when a command is recorded. Multisampled canvases export the
resolved image. Resize, target replacement and detachment expire the old RIDs.
Queued reads still complete from the image selected when they were recorded.
HDR viewport exports retain their linear half-float storage and reject a distinct
sRGB view.

Run `ELECTRON2D_TEST_DEVICE_CANVAS_SRGB=vulkan` with `SDL_GPU_DRIVER=vulkan` to
check ordinary, array and swizzled slice sampling in a rendered frame. The
[test](../../tests/Electron2D.Tests/DeviceCanvasSRGBTests.cs) also checks raw
reads and writes, mip/layer isolation, proxy identity, active-list refusal and
view retirement. The `direct3d12` profile also checks typed SRV and native
resource identity, sRGB attachment encoding, encoded copies and incompatible
format or usage refusal. Its mip slice uses a single-layer allocation. The
`direct3d12-preceding` profile verifies refusal on the preceding native bridge.

Run `ELECTRON2D_TEST_RENDERER_TEXTURE_SRGB=vulkan` with `SDL_GPU_DRIVER=vulkan`
to check the [renderer upload profile](../../tests/Electron2D.Tests/RendererTextureSRGBTests.cs).
It draws the upload before its first export, writes the linear allocation with a
compute shader before asking for sRGB, checks ordinary and array samples against
decoded values, and tests raw bytes, compatible updates, source retirement and
queued readback. When the device exposes BC1, it also verifies decoded BC1 pixels
and the unchanged encoded block. The `direct3d12` profile runs the same upload
checks. The `preceding` and `direct3d12-preceding` profiles verify that older
bridges preserve linear uploads when distinct views are unavailable.

Run `ELECTRON2D_TEST_VIEWPORT_SRGB=vulkan` with `SDL_GPU_DRIVER=vulkan` to check
[viewport exports](../../tests/Electron2D.Tests/ViewportSRGBTests.cs). The
fixture checks gamma-decoded canvas pixels, raw writes, buffer swaps, shared
views, queued reads, resize, MSAA resolve and HDR refusal. The `direct3d12`
profile runs the same checks. The `preceding` and `direct3d12-preceding` profiles
verify that older bridges preserve the linear export.

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

SDL exposes no mutable-image import/allocation hook. Alternate-format backing on
Vulkan uses a private mutable image with its exact device-local memory requirement,
wrapped in the same pinned SDL texture structures. Native texture/total memory
queries include these allocations once; ordinary views add no image payload.
See [render memory](render-memory.md). Unknown cores/backends or missing view
procedures reject explicitly; failed native setup releases views, pins, image and
memory before returning a failure.

Owned implementation: `RenderingDevice.TextureViews.cs`, `GPUTextureViews.cs`,
`tools/render-native/views.c` and the existing texture/transfer/descriptor/renderer
lifetime paths. [ADR 0028](../decisions/rendering.md#adr-0028) owns this boundary.

## Direct3D 12 color views

On the pinned Windows GPU backend, views share a single-sample sampled color
allocation. Declared RGBA8, BGRA8 and BC1/2/3/7 linear/sRGB pairs use one typeless
resource with typed descriptors. Each declared format must support the requested
roles. Renderer uploads and completed SDR viewport images use a sampling-only sRGB
view with raw transfers, and their linear owners retain the original roles.
Copies within these pairs preserve encoded bytes. The full view preserves the
owner's shape, including arrays. A single-layer allocation supports ordinary
views over a selected mip interval. A one-mip view can serve as a color attachment. A view of an ordinary
storage image can receive a compute write at its selected base mip. Direct3D 12
allows disjoint sampled and writable mip ranges in one dispatch and rejects
overlapping ranges and duplicate writable bindings before entering the native pass.

The wrapper owns SDL descriptor slots and a COM reference to the original
resource. Color and storage descriptors and barriers address physical
subresources. Copies and readbacks resolve to the same original coordinates.
The original RID retires all sibling views after pending work. No persistent
image allocation is added for a view. The native setup briefly allocates a
staging texture to acquire SDL's descriptors.

A texture array created without Sampling, Storage or StorageAtomic can expose
an ordinary view of any layer and mip interval through
`TextureCreateSharedFromSlice`. Transfers resolve the view to the original
subresources. For a one-mip ColorAttachment view the native RTV selects that
layer and mip. Disjoint layer views may be attached to one framebuffer.
Declared linear/sRGB formats use the corresponding typed RTV. The view shares
the original allocation and retains its transfer permissions.
[Direct3D 12 array RTV fields](https://learn.microsoft.com/en-us/windows/win32/api/d3d12/ns-d3d12-d3d12_tex2d_array_rtv)

Sampled and storage slices of multi-layer allocations require shader lowering
to one-layer array SRVs and UAVs. Imported arrays require Sampling, so this
layer target path applies to device-owned allocations. Depth/stencil,
multisample and input attachments retain their native prerequisites.
[Direct3D 12 compute array UAVs](graphics-storage-images.md#direct3d-12-compute-arrays)
use a cached base-mip view and transition every writable layer.

## Verification and remaining limits

On Windows x64, build the [Direct3D 12 producer fixture](native-texture-handles.md#verification)
and run the Release root runner with `SDL_GPU_DRIVER=direct3d12` and
`ELECTRON2D_TEST_NATIVE_TEXTURE_IMPORT=gpu-direct3d12-layer-targets`. The profile compares the native
resource identity and encoded bytes of each layer/mip before and after a
two-attachment render pass, a disjoint sibling copy, an interval update and a
typed sRGB clear. It checks invalid intervals, duplicate output and copy
overlap refusal, sampling refusal and original-owner retirement. Set
`ELECTRON2D_D3D12_IMPORT_DEBUG=1` to check the device's native debug queue. Use
`gpu-direct3d12-layer-targets-preceding` with the previous native adapter to
verify refusal and preservation of transfer behavior.
The Windows x64 check used a source-built render bridge and the pinned SDL
core with the existing local engine module. Release package qualification
remains open.

[DeviceGraphicsStorage.Consumer](../../tests/DeviceGraphicsStorage.Consumer/README.md)
provides `views`, `views-source`, `views-canvas`, `views-unavailable` and
`views-failure`. Checks cover distinct layer/mip bytes, initial format overrides,
normalized/integer/float/compressed classes, physical alias writes/atomics,
GLSL/HLSL sampling/query extents, graphics pass/scissor restarts, readonly input
snapshots, MSAA resolve, depth rejection, raw snapshots, borrowed-source expiration,
owner-family retirement, refusal rollback and warmed owner-thread allocations.
`Electron2DRenderCheck` additionally checks defragmentation pins, direct native
nested views, deferred unsubmitted commands and mutable allocation accounting.

Linux/Wayland Vulkan verifies the full Vulkan view profile. On Windows x64
with an RTX 3090 Ti, the three sRGB fixtures pass on Direct3D 12 and Vulkan.
All three Direct3D 12 refusal profiles pass with the preceding native module.
The Direct3D 12 device/canvas sRGB fixture also passes with the debug layer.
Enable it with `ELECTRON2D_D3D12_IMPORT_DEBUG=1` and set
`ELECTRON2D_D3D12_IMPORT_FIXTURE` to the [native producer DLL](native-texture-handles.md#verification).

The Windows x64
`gpu-direct3d12-local` and `gpu-direct3d12` import profiles exercise ordinary
mip views and exact array views with the Direct3D 12 debug layer, including
physical overlap, framebuffer and compute writes, canvas pixels, and pending
readback at owner retirement. Native heap churn, Metal/browser behavior,
release-package qualification and owner visual acceptance retain separate gates. Native
HLSL input attachments may declare an unspecified depth role; the lowering accepts
that valid role while retaining explicit depth-input rejection, following
[SPIR-V OpTypeImage](https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html#OpTypeImage).

[Depth/stencil transfers](depth-texture-transfers.md) now map shared-view regions to their original physical mip/layer before native aspect copies. View readback exposes its own tight extent; updating a selected view preserves every neighboring original subresource. The original owner and native transfer allocation remain pinned through pending commands.
