# NativeLibraries

Last updated: 2026-10-05

Private native binaries come from versioned Linux/macOS/Windows packages in ordinary desktop builds. [Native delivery](../native-packaging.md) separates source production, audited packages, executable checks and public publication; complete Windows/macOS target verification remains pending.

- Declaration: `internal static class NativeLibraries`
- Source: [NativeLibraries.cs](../../src/Properties/NativeLibraries.cs)
- Components: [Shader materials](../components/shader-materials.md), [Text](../components/text.md), [Audio playback](../components/audio-playback.md).
- Visibility: internal; unavailable to engine consumers.

## Description

iOS/tvOS compile the ten owned backend import names as `__Internal`, referring to symbols linked into the application executable. Module initialization does not register a dynamic resolver there. `Electron2D.Native.iOS`/`tvOS` supplies the selected RID's twelve static archives through transitive NativeReference metadata and CoreBluetooth; FreeType/SDL_image use one audited PNG/zlib copy. Apple SDK linking and simulator execution remain target gates, not outcomes of a Linux build.

Installs Electron2D.dll's native import resolver before binding calls. A lazy process-lifetime handle selects canonical `libSDL3.so.0`, Android `libSDL3.so`, `libSDL3.0.dylib` or `SDL3.dll` and shares that core with native dependents. There is intentionally no public setup or unload API.

The private `libElectron2DTextBreak.so`, `libFAudio.so.0` and `libElectron2DENet.so` live under `runtimes/<RID>/native` beneath the application base directory. macOS uses the corresponding dylibs plus private OpenSSL and WOFF2-capable `libElectron2DFreeType.dylib`. Resolution uses the executing runtime identifier and `AppContext.BaseDirectory`, independently of the working directory and managed assembly location. The native package targets preserve this directory for project and package consumers; ordinary .NET resolution remains available as a fallback. Build and package ownership follow [ADR 0012](../decisions/product.md#adr-0012).

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static void Initialize()` | [Module initialization](#module-initialization) |

## Member descriptions

### Module initialization

`internal static void Initialize()`

Runs automatically once as a module initializer. HarfBuzz imports resolve their packaged native identity on every platform. On Linux, private text imports resolve the runtime-directory library; SDL3 imports return the retained core handle; FAudio first loads that same SDL core and then resolves its runtime-directory library. SDL3_image/SDL3_shadercross force the shared core to load before returning zero for ordinary dependency resolution. Other names return zero untouched. Missing native libraries propagate at first relevant use. Calling Initialize manually a second time is unsupported because .NET allows one resolver per assembly.

## Verification and limits

Windows selects private text/audio/ENet/FreeType DLLs and loads private crypto before SSL. Its fresh ProjectReference/NuGet publish consumers passed on x86/x64/ARM64 in run 37319099153. The ordinary Windows library build now preserves all six private DLLs as well; complete full-suite reruns remain required.

Android resolves its seven private `.so` files through the SDK's APK native-library search path, not an application-relative desktop directory. FreeType/HarfBuzz use private identities; crypto loads before SSL and SDL loads before FAudio. The engine reference supplies `Electron2D.Native.Android` transitively. Package targets omit the redundant SDL RID copy because the AAR already provides the same JNI core. All four Android native apps passed in run 37335292748; updated CI and physical acceptance remain separate gates. Apple static application references are connected with target execution pending; Web application linking remains absent.

Self-contained Linux x64 tests loaded images and GPU shaders with LD_LIBRARY_PATH unset; loader diagnostics found one published libSDL3.so.0. The macOS resolver additionally selects private text/FAudio/ENet and OpenSSL dylibs from the RID directory, loading the shared SDL core before FAudio. Matching native production and public consumers passed; full-suite execution remains pending.

[NativeLibraryTests](../../tests/Electron2D.Tests/NativeLibraryTests.cs) checks project-reference runtime-directory files and rejects root copies. Generic and Linux x64 RID engine builds, a fresh self-contained HostExample publish and local NuGet package entries preserve the selected private native layout. The executable suite passes; focused font/audio checks and both Wayland audio hosts load the libraries with `LD_LIBRARY_PATH` unset from an external working directory. A local NuGet consumer verifies public text/audio calls in both its ordinary build and SDK-flattened Linux x64 RID publish. These checks do not establish foreign native execution or physical listening.
