# Depth framebuffer inputs

Last updated: 2026-11-06

[ADR 0028](../decisions/rendering.md#adr-0028) extends existing ordered framebuffer inputs to depth images. GLSL/HLSL `subpassLoad` reads the depth component at the current fragment pixel and selected sample. D16UNORM, X8D24UNORMPACK32, D32SFLOAT, D24UNORMS8UINT and D32SFLOATS8UINT retain their stored formats. Combined formats expose depth to floating inputs and preserve their stored stencil. A separate [stencil-only capability](stencil-textures.md) supplies unsigned inputs; combined formats never reinterpret stencil as floating depth.

## Public allocation and binding

InputAttachment accepts ordinary images and arrays with one mip and an exact supported sample count. TextureIsFormatSupportedForUsage queries the current native adapter; raw CPU multisample transfers remain unavailable. DepthStencilAttachment, Sampling, transfer and update roles are independent of the input role. An input-only depth allocation retains a private native depth target for zero initialization; GetFormat does not grant public render/copy permissions. It may receive ordinary encoded depth data and updates, or matching-count GPU copies when their public roles are present.

RDFramebufferPass.InputAttachments selects shader input indices, including unused holes and fixed descriptor arrays. RDUniform lists the corresponding image identities in element order. Bindings validate scalar and sample roles, current device/owner and exact selected attachment identity. A pass can read depth written by an earlier pass while rendering ordinary color output. Inputs and active color/depth/resolve outputs must address disjoint physical subresources, including aliases; disjoint layers of one allocation remain legal. Preserves, early end and list/retirement rules remain the [ordered-pass contract](local-compute.md#ordered-framebuffer-passes-and-inputs).

Ordinary input-only images bind directly. Combining Sampling with InputAttachment, using an array or selecting a shared view retains a readonly single-layer GPU snapshot. Each view captures its selected original image layer before the input pass; later writes refresh the next capture. Existing shared factories keep original-owner coordinates. Extra framebuffer layers remain untouched. Source retirement completes recorded work, invalidates borrowers and releases its private snapshots. Queued output readback preserves request-time contents across later input writes/draws.

## Native adapter

The aspect support query's ABI2 guarantees depth input format features and readonly graphics layout restoration, while ABI1 retains ordinary depth transfer semantics. A preceding complete native module refuses the new role before allocation and continues ordinary rendering/transfers. The pinned Vulkan core allocates readonly graphics images with sampled usage, although its format query requests writable storage. The engine queries the actual sampled role for input and shared-view capabilities, including sRGB color siblings; no vendored source is modified.

Depth inputs and their snapshots use the existing native image/view allocator. This preserves depth-only sampled views, depth/stencil target views and exact formats/sample counts without relying on the core's narrower depth validation. Snapshots reuse the existing aspect copy operation, translate shared locations to original physical layers, copy both stored aspects, retain command/allocator ownership and restore sampled/readonly graphics layouts. No CPU frame bridge, public native handle, new public type or native export is added. Original SPIR-V/bytecode remains unchanged; existing input lowering supplies readonly pixel-coordinate fetches.

## Verification and remaining limits

[DepthInputs.Consumer](../../tests/DepthInputs.Consumer/README.md) exercises real native depth producers and inputs in both source/SPIR-V and compiler-free binary profiles. Its basic matrix covers five formats, 1X/2X/4X/8X, GLSL/HLSL and direct/sampled snapshots with independently expected per-pixel/sample values. Additional scenarios cover layered views, shared/nested physical selection, descriptor arrays, input-only upload/update/MS copies, source/stencil preservation, request-time output snapshots, owner/list/role guards, retirement and prepared zero owner managed bytes. Linear/sRGB shared color inputs exercise the common readonly capability correction. A failing alias-feedback control guards the shared framebuffer validator.

Ignored receipts under `C:/Users/eduar/.codex/artifacts/Electron2D/depth-inputs-20261106` retain exact source/native/package/run identities, native ABI checks, preceding-module refusal and the package/NativeAOT qualification. The exercised host is Windows x64/Vulkan, NVIDIA RTX 3090 Ti. Managed measurements cover the creating thread; native heap/driver allocations, validation layers, performance and human acceptance remain separate. Full release RID/package delivery and foreign operating systems/backends retain the ADR 0021 completion gate. Declaration coverage does not establish that matrix.

[Stencil-only S8UINT](stencil-textures.md) now supplies unsigned inputs through its separate ABI4 capability and stencil-only views; combined formats continue to supply floating depth.
