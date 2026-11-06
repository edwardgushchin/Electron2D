# Native blend factors

Last updated: 2026-11-06

RenderingDevice.BlendFactor contains all nineteen executable factors plus the exclusive Max sentinel. RenderPipelineCreate copies each active attachment's source/destination RGB and alpha factors independently. ConstantAlpha and OneMinusConstantAlpha replicate the constant alpha across RGB; ConstantColor retains its component values. Static BlendConstant and DrawListSetBlendConstants supply the same four native constants. Arithmetic operations, write masks and integer-target guards retain their existing semantics.

## Secondary fragment output

GLSL uses `layout(location=0,index=0) out vec4 primary` and a matching `layout(location=0,index=1) out vec4 secondary`. HLSL uses `[[vk::location(0)]][[vk::index(0)]]` and `[[vk::location(0)]][[vk::index(1)]]` on distinct output fields. Src1Color and its complement use secondary RGB or secondary alpha in the alpha equation; Src1Alpha and its complement use secondary alpha for every component. Each secondary output requires a primary with the same location and scalar type. Vector widths may differ; only declared components provide defined blend inputs. Index values outside zero/one, overlapping outputs and missing/mismatched primary scalar outputs reject before publication.

Only primary outputs count as framebuffer color slots. Both output indices retain the original logical location and map together to the compact native slot. Direct/grouped Location and Index decorations follow the same cold reflection normalization; the native module keeps the original index. SDL reflection omits Index, so its private reflection copy temporarily places secondary outputs in a separate location bank. The source/binary capsule format and high-level canvas interface remain unchanged.

The pinned HLSL frontend accepts dual-source declarations only at location zero. GLSL source and externally supplied SPIR-V support sparse logical locations. The public consumer relocates the HLSL fixture's compiled output to logical slot three to check the precompiled path separately; this does not claim HLSL sparse source syntax is implemented. The upgrade trigger is a frontend accepting nonzero dual-source locations.

## Native state and lifetime

Two private ABI1 exports query native blending and create a pipeline with a bounded copied table of four factors per active color target. The existing raster adapter patches only the native factors and retains multisampling, depth/stencil, logical operations, line/discard state and storage images. Existing private exports keep their signatures. The unified module and standalone render library export the same additions on their supported build targets; package export validation requires both.

Separate constant-alpha factors require the Vulkan raster adapter, with no extra physical feature. Secondary shader publication and second-source factors require an independently negotiated logical dualSrcBlend feature. Pipeline creation also checks the physical active-attachment limit. Optional feature refusal preserves ordinary graphics and constant-alpha factors; an older complete module missing the exports preserves ordinary graphics and rejects the new adapter-dependent roles. D3D12, Metal and WebGPU require their own blend/interface adapters. No public backend handle or inert compatibility value is introduced.

Descriptions remain caller-owned until pipeline creation and are then copied. Specialized, sparse-output, multiview, multipass and sampled component-map variants carry the same factors. Shader retirement invalidates borrowing pipelines. Logical operations preserve their existing precedence over arithmetic blending and floating/sRGB source-copy behavior. Existing native failure cleanup retains shader references and call-local state lifetime.

## Verification and limits

[BlendFactors.Consumer](../../tests/BlendFactors.Consumer/README.md) uses independent floating-point equations for every factor in every source/destination RGB/alpha field, both source languages and compiler-free binary replay. Sparse MRT targets check independent per-target equations and ignored hole/trailing descriptions. It checks dynamic alpha independently of constant RGB, logical-operation composition, copied descriptions, sparse primary/secondary remapping, both views, ordered passes, MSAA resolve, sampled component maps, independent stencil references and borrowed shader retirement. Prepared dynamic draw/span-readback cycles require exactly zero owner-thread managed bytes. Interface mutation checks cover grouped decorations, invalid indices, overlap and missing primary locations. Independent RGB secondary/RGBA primary pixels check that vector widths are not unnecessarily coupled.

The native render checker verifies the support ABI, physical limit, invalid count/factor/ABI guards and logical-feature refusal. A test-only Windows SDL dynamic-API shim refuses the dual-source request during real device negotiation. The preceding complete production module is the missing-export control. Source/project-reference, fresh managed-package and strict full-trim NativeAOT receipts are retained with native and package hashes outside the repository. These are bounded Windows/Vulkan checks on RTX 3090 Ti, not full ADR 0021 platform, native-heap, sanitizer, driver-performance or owner acceptance.
