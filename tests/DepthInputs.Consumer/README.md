# Depth framebuffer input consumer

This public-API executable exercises depth producers and ordered depth inputs for five formats at 1X/2X/4X/8X in GLSL/HLSL, including arrays/shared views, input-only data, copies, snapshots, retirement and prepared owner managed allocations. See [the executing contract and limits](../../docs/components/depth-inputs.md).

```powershell
dotnet run --project tests/DepthInputs.Consumer -c Release
```

Source mode requires the published ShaderImport executable and pinned toolchain. ELECTRON2D_DEPTH_INPUTS_OUTPUT selects an absolute directory for original SPIR-V and engine bytecode fixtures. Pass that directory as the sole argument for compiler-free replay; set ELECTRON2D_SHADER_IMPORT_TOOL to an absent executable to verify this boundary.

The project honors Electron2DUsePackage, Electron2DVersion and the shared native-consumer overrides. Qualify with the matching full native module and dependency closure. Strict NativeAOT uses PublishAot=true, TrimMode=full and warnings as errors. For the actual preceding-module ABI1 control, replace only the unified module, set ELECTRON2D_DEPTH_INPUTS_REFUSAL=1, run in a fresh process and restore the current module. This rejects all depth input encodings while executing ordinary color rendering and ABI1 depth transfers. ELECTRON2D_DEPTH_INPUTS_FOCUS=feedback runs the focused physical-alias guard.
