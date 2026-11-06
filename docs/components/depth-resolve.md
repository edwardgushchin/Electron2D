# Depth/stencil multisample resolve

Last updated: 2026-11-06

[ADR 0028](../decisions/rendering.md#adr-0028) extends the existing texture and framebuffer API with a native Vulkan 1.2 resolve. Depth and stencil each select **sample zero**; the source keeps all of its samples. D16UNORM, X8D24UNORMPACK32, D32SFLOAT, D24UNORMS8UINT and D32SFLOATS8UINT follow their exact stored formats and the existing [portable readback encoding](depth-texture-transfers.md). Color resolve remains unchanged.

## Public workflows

`TextureResolveMultisample(source, destination)` requires a multisample source, a matching single-sample destination, one mip, identical dimensions/type/layers and the respective CanCopyFrom/CanCopyTo roles. Mark a depth destination with IsResolveBuffer or DepthResolveAttachment. Every layer resolves on the existing GPU timeline. Original-owner shared factories and selected physical layers retain their documented semantics; sampled swizzles do not transform the raw resolved values. Invalid arguments, expired/foreign identities, active lists and non-owner access reject through the existing device guards.

`FramebufferCreate` and `FramebufferFormatCreate` infer an optional unique DepthResolveAttachment alongside a matching multisample DepthStencilAttachment. This destination has one sample and the same depth/stencil format and extent. Both images supply at least viewCount layers. Each native draw-pass closure resolves the selected view, including depth-only passes, skipped views, storage/snapshot restarts and later draws that reload the preserved source. Extra layers remain untouched. Automatic framebuffer resolve requires no public copy permissions. Combining DepthStencilAttachment with DepthResolveAttachment retains the depth-render role precedence; use the resolve role alone to infer a destination. A destination without a source, multiple inferred depth destinations or mismatched counts/formats reject.

The public RDFramebufferPass description has only per-color resolve slots. Explicit multipass layouts do not infer a depth destination or introduce a new property. Resolve depth explicitly after ending such a draw list. IsResolveBuffer alone marks an explicit depth destination and does not select the inferred framebuffer role.

DepthResolveAttachment is a queried capability and allocates a native depth/stencil target. The optional profile requires Vulkan 1.2 and SAMPLE_ZERO support for each stored aspect; combined depth/stencil requires both bits. Device recreation records only the successfully enabled profile. Refusal or an older native module preserves ordinary device creation and rejects the resolve allocation instead of advertising it. Raw CPU multisample sample transfer remains a separate adapter.

## Native ownership and ordering

The private aspect adapter prepares one retained render pass/framebuffer per source/destination layer pair. Preparation validates complete selected extents and exact formats/counts before commands are recorded. Shared locations translate to the original native image and physical mip/layer. Persistent SDL texture references and allocator pins keep cached attachment views fixed; command references retain recorded images through submission. Freeing either image releases the cache after GPU completion, before destroying its views. Device cancellation/teardown releases the same owners safely.

Recording transitions both aspects to attachment layouts, performs the native resolve and restores their ordinary layouts. Following sampling, copies, draw passes and synchronous/queued readback consume that result in order. A queued readback retains its request-time snapshot across a later resolve. Resolve neither submits nor delivers callbacks. Warm explicit resolve plus span readback allocates zero managed bytes on the owner thread; this does not measure native driver allocations.

## Executable evidence and limits

[DepthResolve.Consumer](../../tests/DepthResolve.Consumer/README.md) uses only public API. Source/SPIR-V and compiler-free binary replay cover five formats at 2X/4X/8X with two layers. Actual raster producers write different depth values and stencil references per sample; every pixel is checked against independently expected sample zero. A compute consumer samples resolved depth through the restored native layout. Additional checks cover shared/sliced owner selection, preserved repeated sources, marked destinations, automatic depth-only two-view framebuffers, snapshot ordering, retirement, role/list/count errors and 32 warmed zero-owner-managed-byte cycles for every format/count pair.

Ignored receipts at `C:/Users/eduar/.codex/artifacts/Electron2D/depth-resolve-20261106` retain source/binary runs, the native checker, independent depth/stencil and optional API refusal controls, the preceding full-module missing-export control, package/AOT checks and exact identities. The exercised host is Windows x64, Vulkan, NVIDIA GeForce RTX 3090 Ti. The native checker also guards the private ABI/null arguments and existing views, allocator retirement and aspect commands.

These receipts are bounded platform progress. Other operating systems, non-Vulkan backends, validation layers, native heap/driver allocation accounting, performance and human acceptance retain their separate checks. They do not qualify the complete release RID matrix under ADR 0021.

[Stencil-only S8UINT](stencil-textures.md) resolves sample zero using only the enabled stencil bit. It retains the same explicit/inferred framebuffer ownership and ordering.
