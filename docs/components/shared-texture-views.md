# Shared texture views

Last updated: 2026-10-28

`RenderingDevice.TextureCreateShared` exposes the original allocation through a copied
`RDTextureView`. `TextureCreateSharedFromSlice` selects one original layer and a
positive mip interval as an ordinary image. The only applicable `TextureSliceType`
is `Texture`. Both factories resolve an existing shared view to its original owner:
indices and default full extent are relative to that owner, not to the source view.
No pixels are copied or periodically synchronized.

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
Direct writable image-array descriptors retain their native array write binding
and SPIR-V lowering dependency; a per-layer ordinary view already executes.
Physical range overlap guards cover framebuffer outputs, draw feedback, blits and
self copies. Disjoint layers/mips remain usable. Sampled siblings of writable
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
