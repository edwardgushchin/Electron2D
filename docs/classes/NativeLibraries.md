# NativeLibraries

Last updated: 2026-10-05

Private native binaries come from versioned Linux/macOS packages in ordinary desktop builds. Only `Electron2DBuildNativeFromSource=true` invokes native compilers. [Native delivery](../native-packaging.md) separates audited packages, executable consumer checks and public publication; macOS runtime integration verification is pending.

- Declaration: `internal static class NativeLibraries`
- Source: [NativeLibraries.cs](../../src/Properties/NativeLibraries.cs)
- Components: [Shader materials](../components/shader-materials.md), [Text](../components/text.md), [Audio playback](../components/audio-playback.md).
- Visibility: internal; unavailable to engine consumers.

## Description

Installs Electron2D.dll's native import resolver before binding calls. On Linux and macOS, the pinned packages contain aliases that can otherwise load two SDL core copies with inconsistent object ownership. A lazy process-lifetime core handle uses canonical `libSDL3.so.0` or `libSDL3.0.dylib`. There is intentionally no public setup or unload API.

The private `libElectron2DTextBreak.so`, `libFAudio.so.0` and `libElectron2DENet.so` live under `runtimes/<RID>/native` beneath the application base directory. Resolution uses the executing runtime identifier and `AppContext.BaseDirectory`, independently of the working directory and managed assembly location. The native package targets preserve this directory for project and package consumers; ordinary .NET resolution remains available as a fallback. Build and package ownership follow [ADR 0012](../decisions/product.md#adr-0012).

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static void Initialize()` | [Module initialization](#module-initialization) |

## Member descriptions

### Module initialization

`internal static void Initialize()`

Runs automatically once as a module initializer. HarfBuzz imports resolve their packaged native identity on every platform. On Linux, private text imports resolve the runtime-directory library; SDL3 imports return the retained core handle; FAudio first loads that same SDL core and then resolves its runtime-directory library. SDL3_image/SDL3_shadercross force the shared core to load before returning zero for ordinary dependency resolution. Other names return zero untouched. Missing native libraries propagate at first relevant use. Calling Initialize manually a second time is unsupported because .NET allows one resolver per assembly.

## Verification and limits

Self-contained Linux x64 tests loaded images and GPU shaders with LD_LIBRARY_PATH unset; loader diagnostics found one published libSDL3.so.0. The macOS resolver additionally selects private text/FAudio/ENet and OpenSSL dylibs from the RID directory, loading the shared SDL core before FAudio. Matching native production passed; public consumer and full-suite execution remain pending. Windows and mobile/browser native integration retain their gates.

[NativeLibraryTests](../../tests/Electron2D.Tests/NativeLibraryTests.cs) checks project-reference runtime-directory files and rejects root copies. Generic and Linux x64 RID engine builds, a fresh self-contained HostExample publish and local NuGet package entries preserve the selected private native layout. The executable suite passes; focused font/audio checks and both Wayland audio hosts load the libraries with `LD_LIBRARY_PATH` unset from an external working directory. A local NuGet consumer verifies public text/audio calls in both its ordinary build and SDK-flattened Linux x64 RID publish. These checks do not establish foreign native execution or physical listening.
