# Native wide-integer consumer

This executable consumes only the public Electron2D API. It imports GLSL/HLSL through the existing device shader target and uses the ordinary native consumer assets.

Build and run `dotnet run --project tests/DeviceInt64.Consumer -c Release -- managed`. Select `compute`, `graphics` or `canvas` in a Vulkan display session with enabled shaderInt64. Compute also checks a mixed Int64/FP64 HLSL conversion and requires shaderFloat64 for that case.

Compute checks exact signed/unsigned arithmetic above 32 bits, SSBO/UBO data, independent copied specialization variants and the original 128-byte push layout. A raw SPIR-V variant uses 64-bit array extents and access indices, while the pinned GLSL frontend requires 32-bit access indices. Graphics checks flat wide vec4/vec2 stage transfer, location footprints, actual float-target pixels and invalid interface overlap. Canvas consumes the renderer-owned result through TextureRD. Prepared work/readback intervals check owner-thread managed allocations.

The pinned DXC rejects direct 64-bit specialization declarations. HLSL specializes 32-bit values and widens them before arithmetic; GLSL/SPIR-V checks direct long/ulong specialization. The mixed kernel verifies the native UInt32-to-double path with its Int64 intermediate.

`unavailable-int64`, `unavailable-fp64` and `unavailable-both` require the development-only tools/render-native/test_int64_mask.c SDL3_DYNAMIC_API shim. Set ELECTRON2D_TEST_MASK_PRECISION to int64, fp64 or both respectively. The remaining supported precision must still execute; the ordinary float32 kernel executes in all cases. Never ship this shim with the runtime.

Publish with `dotnet publish tests/DeviceInt64.Consumer -c Release -r linux-x64 -p:PublishAot=true -p:PublishTrimmed=true -p:TrimMode=full -p:IlcTreatWarningsAsErrors=true -p:ILLinkTreatWarningsAsErrors=true -o tests/DeviceInt64.Consumer/bin/aot` and run the same profiles from the published host. Native allocation totals, throughput, other devices/platforms and human acceptance require separate verification.
