# Stencil-only textures

Last updated: 2026-11-06

The existing RenderingDevice texture API now executes DataFormat.S8UINT on the pinned Vulkan backend under [ADR 0028](../decisions/rendering.md#adr-0028). Storage is an actual eight-bit stencil image, not a color allocation. No public type or selector is added.

## Public behavior

S8UINT requires DepthStencilAttachment, queried DepthResolveAttachment or InputAttachment. Sampling, CanUpdate, CanCopyFrom, CanCopyTo and CPURead are independent roles. Public metadata retains S8UINT. ColorAttachment, writable Storage and color reinterpretation are rejected. Input-only images retain private initialization targets without gaining public rendering/readback permissions. Unsupported native formats return false and reject allocation before publishing a RID.

Single-sample data is one unsigned byte per texel, tightly packed by mip within each layer. Empty data initializes stencil zero. Upload/update, rectangular copies, synchronous span/allocated readback and both queued forms use the existing device timeline. Native transfer planes remain four-byte aligned independently of public packing. Shared views retain original-owner layer/mip coordinates, raw bytes and retirement. Disjoint self-copies use a matching stencil scratch allocation; overlapping regions reject.

Framebuffer depth/stencil slots accept stencil-only images. Clear/load/discard/store operate on the stencil aspect. All eight stencil operations, comparisons and independently copied face masks/references execute. Depth test, depth write and depth bounds reject because the attachment has no depth aspect. Ordinary color attachments can accompany stencil. Existing pipeline variants retain the private stencil role.

Sampling returns unsigned integer values through uint GLSL/HLSL textures; combined depth/stencil formats continue to expose depth independently. Ordered unsigned input attachments read stencil at the fragment pixel and chosen sample. Arrays, Sampling combinations and shared selections use retained per-view GPU snapshots; physical input/output alias feedback rejects. Later passes and views consume preceding writes in order.

Exact 1X/2X/4X/8X allocations query the native tuple. Equal-count copies preserve each sample. Explicit and inferred framebuffer resolves select stencil sample zero and require the independently enabled stencil resolve bit; a depth resolve bit is not required. Raw CPU multisample transfers still require their separate adapter. TextureRD/TextureArrayRD readback exposes raw unsigned values as float32 RGBA; retained canvas samples native integer storage with nearest filtering. RenderingServer roundtrips preserve S8 metadata.

## Native integration and failures

The private image/view adapter allocates VkFormat S8_UINT with stencil-only aspects. An internal unsigned descriptor proxy plus a retained property identifies this exact allocation; public identities and metadata never change format. The reserved unsigned depth-slot render-pass key is disjoint from valid ordinary color/depth keys. Pipeline and draw-pass adapters replace that depth attachment format while retaining the pinned core's caches, ownership and teardown. Shader bytecode remains unchanged. Vendored source is unchanged.

The stencil capability export gates allocation. Image/view creation uses its private ABI3; aspect query ABI3/ABI4 distinguishes stencil transfers/graphics inputs, preserving preceding depth ABIs. Missing exports or physical format support refuse explicitly. Failed native initialization, transient pipeline passes and draw-pass creation do not publish usable state and permit retry. Ordinary unsigned color and depth transfers continue after refusal. D16UNORMS8UINT retains its separate format adapter prerequisite: the exercised device also reports that exact physical format unsupported, so no substitute is claimed.

## Evidence and limits

[StencilTextures.Consumer](../../tests/StencilTextures.Consumer/README.md) exercises source GLSL/HLSL and compiler-free bytecode, odd dimensions/mips/layers, independent faces, saturated/wrapped pass/fail operations, exact per-sample values, copies, explicit/automatic resolve, ordered/multiview inputs, queued snapshots, retirement and retained canvas. Prepared input/draw/span-readback brackets require zero owner-thread managed bytes. Native ABI guards and test-only format/render-pass refusal controls cover the private boundary and retries; the preceding complete module is the missing-export control.

Receipts under `C:/Users/eduar/.codex/artifacts/Electron2D/stencil-textures-20261106` retain native/source/package identities, original SPIR-V validation and managed-package/strict full-trim NativeAOT results. The exercised host is Windows x64/Vulkan, NVIDIA RTX 3090 Ti. Other backends, the complete release RID matrix, validation layers, native/driver heaps, sanitizers and physical owner acceptance remain unverified under ADR 0021. Declaration coverage does not qualify those targets. The existing enum was already counted; this capability does not inflate Implemented totals.
