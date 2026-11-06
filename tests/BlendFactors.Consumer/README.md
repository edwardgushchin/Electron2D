# Blend factor consumer

Uses only public Electron2D API. Run `dotnet run --project tests/BlendFactors.Consumer -c Release` with a matching native module, `SDL_GPU_DRIVER=vulkan` and the published `ELECTRON2D_SHADER_IMPORT_TOOL`. Set `ELECTRON2D_BLEND_OUTPUT` to a new artifact directory to retain binary capsules and original stage SPIR-V. Pass that directory as the single argument for compiler-free replay.

Every factor is compared with an independent floating-point pixel equation in all four factor fields. Further cases cover dynamic constants, logical operations, invalid values/indices, grouped decorations, missing primary output, copied state, sparse slot three, component maps, two views/passes, exact 1X/4X resolve, depth/two-sided stencil and retirement. HLSL sparse fixtures relocate valid location-zero SPIR-V because the pinned frontend rejects nonzero dual-source declarations. See [the contract](../../docs/components/blend-factors.md).

`ELECTRON2D_BLEND_REFUSAL=feature` uses the test-only `tools/render-native/test_blend_mask.c` via `SDL3_DYNAMIC_API` to refuse the real optional feature request; ordinary rendering and constant alpha must survive. `bridge` uses the preceding full native module and rejects the new adapter while retaining ordinary graphics. Both refusal modes accept the same binary directory. The root runner exposes `ELECTRON2D_TEST_BLEND_FACTORS=1|DIRECTORY`.

Use `Electron2DUsePackage=true` with a fresh managed package/cache, then publish with `PublishAot=true`, `PublishTrimmed=true`, `TrimMode=full` and execute the binary replay again. The native render checker and package/export tests cover the private adapter boundary; the Windows receipt does not qualify other backend/platform cohorts or native allocations.
