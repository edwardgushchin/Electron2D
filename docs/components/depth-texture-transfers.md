# Depth/stencil texture transfers

Last updated: 2026-11-06

The existing RenderingDevice texture family executes copied single-sample depth/stencil data on the pinned Vulkan backend: TextureCreate, TextureUpdate, TextureCopy, TextureGetData and both TextureGetDataAsync forms. No public type, native handle or configuration switch is added. [ADR 0028](../decisions/rendering.md#adr-0028) owns the rendering boundary.

## Byte layout and validation

One input/output buffer contains one layer's tightly packed mip chain, largest mip first. Width/height halve down to one. There is no public row or mip padding. Every multi-layer input supplies exactly one chain per layer. Source bytes are copied before return.

| DataFormat | Encoded bytes per texel |
| --- | --- |
| D16UNORM | Two-byte little-endian normalized depth. |
| X8D24UNORMPACK32 | Little-endian word: depth in low 24 bits; high eight bits read as zero. |
| D32SFLOAT | Four-byte little-endian IEEE single depth. |
| D24UNORMS8UINT | Little-endian word: depth in low 24 bits and stencil in high eight bits. |
| D32SFLOATS8UINT | Four-byte little-endian single depth, one stencil byte, three zero padding bytes. |

Unused input bits are ignored; output is canonical. D32 values must be finite and in the inclusive unit range. Validate every initial chain before allocation and each update before command recording; invalid bytes do not change existing pixels. Empty data initializes every depth/stencil layer/mip to zero through existing native passes. The existing 256-layer depth-target bound remains. D16/S8 has no selected SDL representation and queries false.

DepthStencilAttachment, queried DepthResolveAttachment or [depth input](depth-inputs.md) roles are required. Sampling, CanUpdate, CanCopyFrom, CanCopyTo and CPURead are independent roles. Exact native format/usage support and the aspect adapter gate allocation; format capability is cached for the device lifetime. TextureIsFormatSupportedForUsage has no sample-count argument: a successful ordinary-format query does not permit raw multisample data. [Equal-count MS copies](multisample-copies.md) preserve native samples; CPU MS upload/readback retains its separate prerequisite; [depth/stencil resolve](depth-resolve.md) now executes through its optional native profile.

TextureCopy requires matching depth/stencil formats and preserves both aspects. Color/depth reinterpretation is rejected even when byte footprints match. Existing region, usage, overlap and list guards apply. Disjoint same-subresource copies use a retained depth scratch image. Shared views map to the original physical layer/mip; uploads and reads expose the selected view's own tight chain. Neighboring mips/layers remain unchanged. TextureClear remains a color operation; draw passes own depth/stencil clears.

## Native commands and ownership

The private aspects.c bridge uses the checked SDL 3.4.18 Vulkan ABI. Depth and stencil use separate aligned native buffer planes, matching the [Vulkan aspect-copy formats](https://docs.vulkan.org/spec/latest/chapters/copies.html#copies-buffers-images-depth-stencil). Public packing is independent of device memory layout. Both aspects transition together from the ordinary sampled/depth-target layout to transfer usage and back; each copied mip/layer has its own range. No separate-depth-layout feature or alternate graphics backend is introduced.

Commands retain original SDL texture and transfer-buffer references. SDL allocator regions participating in texture transfers receive the same pending-transfer pins as ordinary native copies; the checked command prefix includes that list. The bridge follows SDL's nullable defrag-lock convention. Recording changes no global Vulkan procedure pointers. Failure to retain resources rejects before transfer commands. Existing SDL submission, fence completion, command-cache reuse and deferred destruction retire all pins. Per-mip CPU packing uses spans and existing retained transfer buffers; no per-frame byte mirror or GPU readback is inserted for copies.

Synchronous reads preserve the device's ordinary completion/callback delivery. Queued reads capture request-time contents in independent reusable transfer storage, retain FIFO, deliver later callbacks after an earlier callback fails, and support caller-owned Memory. Pending reads finish before explicit RID/view retirement; device disposal cancels callbacks. The existing owner-thread, list, reentrancy and callback rules apply.

A missing adapter or unsupported format returns false for transfer queries and rejects depth data before publication. Empty ordinary depth rendering/sampling remains available. Raw native/managed allocation totals, validation-layer acceptance and other platforms require their own qualification.

## Executable evidence

[DepthTransfers.Consumer](../../tests/DepthTransfers.Consumer/README.md) uses only public API. The fixture checks all five host-supported formats, odd dimensions and native alignment, canonical padding, layer/mip independence, shared-slice updates, disjoint self copies, rectangular copies, raw/caller-owned/queued readback, request ordering around updates, callback failures, pending retirement and disposal cancellation. Native compute sampling consumes uploaded selected-view depths; real color targets test uploaded depth and stencil through native comparisons. Invalid D32 values, byte lengths, missing roles, wrong aspect/format pairs, overlap and active-list failures reject.

After sixteen warmups, each format runs thirty-two upload/copy/span-read/Memory-read/submit/completion cycles with zero owner-thread managed bytes. The native C check covers format capability and ABI guards. Source authoring and compiler-free binary replay are separate profiles; package and strict NativeAOT receipts retain their actual code/native identities under ignored bin/depth-aspect-transfers/2026-10-29.

The exercised host is Linux/Wayland, Vulkan, NVIDIA RTX 3090 Ti. Native heap traffic, validation layers, foreign backend/platform behavior and physical/owner acceptance are unverified. Existing color transfers, shared views, MSAA sampling/storage, depth graphics, source authoring and ordinary readback regressions remain relevant checks.

[Depth/stencil resolve](depth-resolve.md) adds matching single-sample destinations, including the DepthResolveAttachment role. This role retains a private native depth/stencil target and uses the same portable aspect readback; raw CPU multisample transfers remain separate.

[Stencil-only S8UINT](stencil-textures.md) adds tight one-byte chains, native stencil aspects and matching scratch/copy/view behavior to this transfer family. Combined format packing is unchanged.
