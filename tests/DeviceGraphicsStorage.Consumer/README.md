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
