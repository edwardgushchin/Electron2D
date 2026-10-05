# NativeLibraries

Last updated: 2026-10-06

Native binaries come from the explicitly selected `Electron2D.{Platform}` package and its platform-only dependencies. The managed `Electron2D` package contains no native payload or implicit platform references. [Native delivery](../native-packaging.md) records the package/version/RID contract and separates local validation from publication and target execution.

- Declaration: `internal static class NativeLibraries`
- Source: [NativeLibraries.cs](../../src/Properties/NativeLibraries.cs)
- Components: [Shader materials](../components/shader-materials.md), [Text](../components/text.md), [Audio playback](../components/audio-playback.md).
- Visibility: internal; unavailable to engine consumers.

## Description

Web uses matching archive module names in Mono's generated P/Invoke tables, not `__Internal` or a dynamic resolver. Its runtime-specific managed DLL is selected from `runtimes/browser-wasm/lib/net10.0/`; the generic compile asset keeps the same public API. `Electron2D.Web` supplies nineteen static archives through NativeFileReference and serves notices under `wwwroot/licence`. Local ProjectReference Chromium checks pass native font precision, ICU dictionaries/cache/allocation checks, images and FAudio PCM/lifecycle. A fresh package-only restore selects the exact runtime asset; CI also links and runs that independent consumer. Browser rendering and a production application host remain separate gates.

iOS/tvOS compile the ten owned backend import names as `__Internal`, referring to symbols linked into the application executable. Module initialization does not register a dynamic resolver there. `Electron2D.iOS`/`Electron2D.tvOS` supplies the selected RID's twelve static archives through transitive NativeReference metadata and CoreBluetooth; FreeType/SDL_image use one audited PNG/zlib copy. Apple SDK linking and simulator execution remain target gates, not outcomes of a Linux build.

Installs Electron2D.dll's native import resolver before binding calls. A lazy process-lifetime handle selects canonical `libSDL3.so.0`, Android `libSDL3.so`, `libSDL3.0.dylib` or `SDL3.dll` and shares that core with native dependents. There is intentionally no public setup or unload API.

The private `libElectron2DTextBreak.so`, `libFAudio.so.0` and `libElectron2DENet.so` live under `runtimes/<RID>/native` beneath the application base directory. macOS uses the corresponding dylibs plus private OpenSSL and WOFF2-capable `libElectron2DFreeType.dylib`. Resolution uses the portable OS/process-architecture RID and `AppContext.BaseDirectory`, independently of the working directory and managed assembly location. The native package targets preserve this directory for project and package consumers, including universal macOS HarfBuzz supplied with the neutral `osx` RID. A default desktop build/publish deploys that file into its SDK host's architecture-qualified directory. Ordinary .NET resolution remains available as a fallback. Build and package ownership follow [ADR 0012](../decisions/product.md#adr-0012).

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static string RuntimeRID { get; }` | Portable native asset RID, independent of distribution-specific .NET identity |
| `internal static void Initialize()` | [Module initialization](#module-initialization) |

## Member descriptions

### RuntimeRID

`internal static string RuntimeRID { get; }`

Desktop resolution uses `win`, `osx` or `linux` plus the executing process architecture. A distribution identity such as `arch-x64` therefore selects the portable `linux-x64` package directory. Other platforms retain the SDK runtime identifier and their native application loading rules.

### Module initialization

`internal static void Initialize()`

Runs automatically once as a module initializer. HarfBuzz imports resolve their packaged native identity on every platform. On Linux, private text imports resolve the runtime-directory library; SDL3 imports return the retained core handle; FAudio first loads that same SDL core and then resolves its runtime-directory library. SDL3_image/SDL3_shadercross load the shared core first, then resolve their selected runtime-directory files. Other names return zero untouched. Missing native libraries propagate at first relevant use. Calling Initialize manually a second time is unsupported because .NET allows one resolver per assembly.

## Verification and limits

Windows selects private text/audio/ENet/FreeType DLLs and loads private crypto before SSL. Its fresh ProjectReference/NuGet publish consumers passed on x86/x64/ARM64 in run 37319099153. The explicitly selected platform package preserves all six private DLLs; complete full-suite reruns remain required.

Android resolves its seven private `.so` files through the SDK's APK native-library search path, not an application-relative desktop directory. FreeType/HarfBuzz use private identities; crypto loads before SSL and SDL loads before FAudio. The executable explicitly selects `Electron2D.Android`; the managed engine supplies no implicit native package. Package targets omit the redundant SDL RID copy because the AAR already provides the same JNI core. All four Android native apps passed in run 37335292748; updated CI and physical acceptance remain separate gates. Apple static application references are connected with target execution pending; Web application linking remains absent.

Self-contained Linux x64 tests loaded images and GPU shaders with LD_LIBRARY_PATH unset; loader diagnostics found one published libSDL3.so.0. The macOS resolver additionally selects private text/FAudio/ENet and OpenSSL dylibs from the RID directory, loading the shared SDL core before FAudio. Run 37377030554 passed both explicit-RID ProjectReference/NuGet consumers, but its default no-RID consumer dropped the neutral HarfBuzz asset on both architectures. The transitive layout regression now retains and relocates that asset; corrected target consumers and full-suite execution remain pending.

[NativeLibraryTests](../../tests/Electron2D.Tests/NativeLibraryTests.cs) checks project-reference runtime-directory files and rejects root copies. The CharacterMovement Linux x64 publish and fresh ProjectReference/NuGet consumer checks preserve the entire selected native layout, including SDL and font libraries. The executable suite passes; focused font/audio checks and both Wayland audio hosts load the libraries with `LD_LIBRARY_PATH` unset from an external working directory. A local NuGet consumer verifies public text/audio calls in both its ordinary build and Linux x64 RID-directory publish. These checks do not establish foreign native execution or physical listening.

The default Debug apphost was also exercised against the system .NET runtime reporting `arch-x64`. Native resolution selects the packaged `linux-x64` directory by OS and process architecture; it does not require a custom DOTNET_ROOT or a distribution-specific native package.
