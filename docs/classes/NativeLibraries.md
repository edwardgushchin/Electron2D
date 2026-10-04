# NativeLibraries

Last updated: 2026-10-04

- Declaration: `internal static class NativeLibraries`
- Source: [NativeLibraries.cs](../../src/Properties/NativeLibraries.cs)
- Components: [Shader materials](../components/shader-materials.md), [Text](../components/text.md), [Audio playback](../components/audio-playback.md).
- Visibility: internal; unavailable to engine consumers.

## Description

Installs Electron2D.dll's native import resolver on Linux before binding calls. The pinned packages contain aliases that can otherwise load two SDL core copies with inconsistent object ownership. A lazy process-lifetime core handle uses the canonical libSDL3.so.0 SONAME. There is intentionally no public setup or unload API.

The private `libElectron2DTextBreak.so` and `libFAudio.so.0` live under `runtimes/<RID>/native` beside the engine assembly. Resolution uses the executing runtime identifier and the assembly directory, independently of the working directory. Ordinary .NET resolution remains available when NuGet has flattened RID-specific native assets. Build and package ownership follow [ADR 0012](../decisions/product.md#adr-0012).

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static void Initialize()` | [Module initialization](#module-initialization) |

## Member descriptions

### Module initialization

`internal static void Initialize()`

Runs automatically once as a module initializer. HarfBuzz imports resolve their packaged native identity on every platform. On Linux, private text imports resolve the runtime-directory library; SDL3 imports return the retained core handle; FAudio first loads that same SDL core and then resolves its runtime-directory library. SDL3_image/SDL3_shadercross force the shared core to load before returning zero for ordinary dependency resolution. Other names return zero untouched. Missing native libraries propagate at first relevant use. Calling Initialize manually a second time is unsupported because .NET allows one resolver per assembly.

## Verification and limits

Self-contained Linux x64 tests loaded images and GPU shaders with LD_LIBRARY_PATH unset; loader diagnostics found one published libSDL3.so.0. Other platforms use their ordinary loader and have not been natively accepted by this integration.

[NativeLibraryTests](../../tests/Electron2D.Tests/NativeLibraryTests.cs) checks project-reference runtime-directory files and rejects root copies. Generic and Linux x64 RID engine builds, a fresh self-contained HostExample publish and local NuGet package entries preserve the selected private native layout. The executable suite passes; focused font/audio checks and both Wayland audio hosts load the libraries with `LD_LIBRARY_PATH` unset from an external working directory. A local NuGet consumer verifies public text/audio calls in both its ordinary build and SDK-flattened Linux x64 RID publish. These checks do not establish foreign native execution or physical listening.
