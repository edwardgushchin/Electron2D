# Writable graphics storage consumer

Public-API native checks for ordinary vertex/fragment storage-buffer stores and 32-bit atomics. Build `DeviceGraphicsStorage.Consumer.csproj` in Release with the repository native overrides and shader toolchain. Run the resulting executable with one of these profiles:

- `graphics`: imported GLSL/HLSL, shared stage descriptor, typed specialization, copied push/scissor, preserved color pixels across restarts, draw → compute → readonly graphics → CPU and 32 prepared zero-owner-byte cycles.
- `source`: the same checks using explicit source compilation; set `ELECTRON2D_SHADER_IMPORT_TOOL` to the built ShaderImport executable.
- `edges`: two views, two subpasses, depth retention, GPU-produced indirect commands, two-command writable batches, zero count, same-pass readonly consumption, expired borrowed sets and 32 prepared zero-owner-byte cycles.
- `aliases`: the ordinary chain with AsStorage vertex and index allocations.
- `precision`: original mixed 8/16/64-bit integer and FP64 buffer layouts on a device supporting those independent features.
- `canvas`: create/retire the renderer twice and verify retained canvas pixels from the native storage chain.

Use `DOTNET_TieredCompilation=0` for allocation measurements. Publish with `PublishAot=true`, `PublishTrimmed=true`, `TrimMode=full`, `IlcTreatWarningsAsErrors=true` and `ILLinkTreatWarningsAsErrors=true`, then repeat the profiles. These checks establish Linux/Vulkan execution; native allocations, performance and other platforms require their own evidence.

On Linux, compile `mask-features.c` as a test-only shared library against the pinned SDL/Vulkan headers with `cc -shared -fPIC ... -ldl`. Set `LD_PRELOAD` to that library and `ELECTRON2D_TEST_GRAPHICS_STORAGE_MASK` to `vertex`, `fragment` or `all`; run `unavailable-vertex`, `unavailable-fragment` or `unavailable-all` respectively. The shim removes the selected physical feature from the bridge query. Each profile checks the available sibling, readonly modules and rejection before publishing shader/binary identities, including specialized templates. Never package the shim.

Each draw owns race freedom among its invocations. Pass boundaries order successive draws; a vertex write and fragment read in the same draw require shader-appropriate synchronization. Writable images/formatted texel buffers and 64-bit graphics atomics keep their separate adapters.

Formatted producer profiles: texels (imported chain), texel-source (explicit GLSL/HLSL source), texel-formats (21 independent encoded vectors per language, odd capacities and adjacent packed fields), texel-atomics (signed/unsigned prior values, descriptor arrays/selection, specialization, failure recovery and borrowed lifetime), texel-nonuniform (different invocation choices and actual alternating pixels) and texel-canvas. Repeat with strict NativeAOT. texel-core, texel-core-formats and texel-core-atomics and texel-core-nonuniform use test_shader8_mask.c via SDL3_DYNAMIC_API with ELECTRON2D_TEST_MASK_SHADER8=api to refuse Vulkan 1.2 requests; texel-unavailable-vertex/fragment/all use mask-features.c as above. ELECTRON2D_GRAPHICS_TEXEL_OUTPUT exports valid external fixture modules for spirv-val. Prepared cycles require zero owner managed bytes.

The pinned HLSL frontend does not compile specialization constants as resource-array lengths. Its atomic source declares two fixed arrays; the consumer creates a separate UInt32 SPIR-V specialization length for those arrays, leaving unrelated literals intact. GLSL source specialization and HLSL-derived external SPIR-V both exercise the engine pipeline contract. Formatless store fixtures use Unknown-format SPIR-V with its declared write capability; the engine removes that image-only requirement after buffer conversion. These transformations are test inputs through public bytecode APIs, not runtime compiler dependencies.
