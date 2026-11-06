# Multisample texture copies

Last updated: 2026-11-06

`RenderingDevice.TextureCopy` preserves every native sample when source and destination have the same sample count. This extends the existing rectangular transfer operation, without a new public type or an implicit resolve. [ADR 0028](../decisions/rendering.md#adr-0028) owns the rendering contract.

## Regions, formats and roles

Both images require their respective CanCopyFrom/CanCopyTo roles. Sample counts must match, including single-sample pairs. Use TextureResolveMultisample to reduce color or [depth/stencil](depth-resolve.md) to one sample. CPU MS sample upload/readback retains its separate native prerequisites.

Color copies preserve encoded texel/block values. Matching footprints may be reinterpreted on the executing Vulkan profile, without numeric conversion; other native drivers retain their compatible-format adapter gate. Depth/stencil copies require the same exact format and preserve both aspects. A color image cannot substitute for a depth image with the same byte footprint. The complete format vocabulary remains a capability-query domain, rather than a promise that every native format/sample tuple is available.

Ordinary images, independent array layers and shared/sliced views use the same coordinate, mip, role, owner and active-list validation. Coordinates truncate to integral texels after bounds validation. MS allocations retain one mip and an exact supported count; invalid mips, layers, regions and count mismatches reject before recording. No resampling occurs, and texels outside the destination rectangle remain unchanged.

Shared views resolve to the original native image and selected physical layer. Alias overlap is checked at that physical range. Overlapping same-subresource rectangles reject; disjoint same-subresource copies use one retained scratch image with the original format, extent, mip count and **sample count**. Different layers of one image copy directly. Sampled component maps do not transform raw copied bytes.

## Native timeline and lifetime

Color uses the existing SDL texture copy command. The pinned Vulkan implementation already records an equal-count image copy and pins both resources and their pending transfer allocations. Depth/stencil uses the existing private aspects.c bridge, with separate depth/stencil copy regions and ordinary-layout restoration. It requires the verified native aspect adapter and format transfer features; MS depth allocation with copy roles rejects when that adapter is absent. Empty depth rendering without copy roles remains independent.

The aspect bridge validates source/destination counts independently of the managed boundary. Buffer/image transfer commands still reject MS images. Native command references and allocator pins retain original images through submission; managed scratch and view retirement follow the existing device queue. Copying neither submits nor delivers readback callbacks. Later draws, shader sampling, storage operations, copies and resolves consume the recorded result in order. Source retirement completes already recorded work.

The Vulkan command follows [the image-copy requirements](https://docs.vulkan.org/refpages/latest/refpages/source/vkCmdCopyImage.html), including equal counts and independent depth/stencil aspects. No vendored source is modified and no native export is added.

## Executable evidence and limits

[MultisampleCopy.Consumer](../../tests/MultisampleCopy.Consumer/README.md) uses only public API. Source authoring and compiler-free binary replay exercise 2X, 4X and 8X for R8G8B8A8UNORM, R32SFLOAT, R32SINT, R32UINT, D16UNORM, X8D24UNORMPACK32, D32SFLOAT, D24UNORMS8UINT and D32SFLOATS8UINT on the checked host. A native raster producer writes independently expected pixel/sample values; GLSL and HLSL compute readers check every sample, avoiding resolve-based assertions. Separate per-sample stencil references are checked through actual native stencil comparisons.

A storage-only MS compute producer feeds a copy/read chain, and a copied shared color view feeds the existing native resolve. Checks cover full/rectangular/self/cross-layer copies, untouched texels/layers, shared slices, compatible color reinterpretation, source retirement, overlap, count/format/role errors, bounds, active compute/draw lists and non-owner access. Each format/count pair warms sixteen cycles and measures thirty-two self-copy, view-copy and native sample-read cycles with zero owner-thread managed bytes. The C check independently rejects unequal-count depth copies, direct same-subresource native copies and raw MS buffer transfers. Existing ordinary/compressed transfers, depth uploads, MS sampling/storage, graphics and resolve checks remain relevant regressions.

Receipts under ignored `bin/multisample-copies/2026-10-29` retain exact source, native, package and executable identities. The exercised host is Linux/Wayland, Vulkan, NVIDIA RTX 3090 Ti. Managed allocation measurements cover the creating thread; native heap/driver allocations, other threads, validation layers, foreign platforms/backends and human acceptance require their own evidence. An Implemented declaration is audited executable semantics, not completion of the product platform matrix.
