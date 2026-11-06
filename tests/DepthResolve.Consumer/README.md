# Depth/stencil resolve consumer

This public-API executable checks native sample-zero depth/stencil resolve for all five supported encodings at 2X/4X/8X, sampled GPU results, layered/shared views, automatic framebuffer resolves, snapshots, lifetime and warmed owner managed allocations. See [the contract and limits](../../docs/components/depth-resolve.md).

Run with the matching rebuilt unified native module:

```powershell
dotnet run --project tests/DepthResolve.Consumer -c Release
```

Source mode requires the published ShaderImport tool and its pinned toolchain. Set ELECTRON2D_DEPTH_RESOLVE_OUTPUT to an absolute fixture directory to retain original SPIR-V and engine bytecode. Pass that directory as the sole argument for compiler-free replay. Set ELECTRON2D_SHADER_IMPORT_TOOL to an absent executable during replay to verify the compiler boundary.

The project honors Electron2DUsePackage, Electron2DVersion, Electron2DNativeLibrary and the shared native-consumer overrides. Strict NativeAOT qualification uses PublishAot=true, TrimMode=full and warnings as errors with the complete matching native dependency closure.

For the private Windows refusal control, build tools/render-native/test_depth_resolve_mask.c against the pinned SDL include and src/dynapi directories, select it with SDL3_DYNAMIC_API and set ELECTRON2D_DEPTH_RESOLVE_REFUSAL to all, stencil or api. The stencil profile keeps depth-only resolve; api refuses optional Vulkan 1.2 creation. All profiles verify ordinary depth rendering and native color resolve. The legacy control replaces only the unified module with the preceding full build and uses profile legacy without the shim; restore the current module afterward. These fixtures are never shipped.
