# NativeLibraries

Last updated: 2026-10-29

Native binaries come from the explicitly selected `Electron2D.{Platform}` package and its platform-only dependencies. The managed `Electron2D` package contains no native payload or implicit platform references. [Native delivery](../native-packaging.md) records the package/version/RID contract and separates local validation from publication and target execution.

- Declaration: `internal static class NativeLibraries`
- Source: [NativeLibraries.cs](../../src/Properties/NativeLibraries.cs)
- Components: [Shader materials](../components/shader-materials.md), [Text](../components/text.md), [Audio playback](../components/audio-playback.md).
- Visibility: internal; unavailable to engine consumers.

## Description

Render, mesh, text, networking/compression and codec bridge imports share EngineLibrary: Electron2D for dynamic platforms, libElectron2D for the Web archive and __Internal for statically linked Apple applications. The platform package supplies one libElectron2D.so, libElectron2D.dylib, libElectron2D.dll or libElectron2D.a. The Windows name stays distinct from managed Electron2D.dll. SDL, SDL_image, SDL_shadercross, SPIR-V Cross FAudio, FreeType, HarfBuzz and OpenSSL retain their existing external identities.

The resolver retains one process-lifetime engine handle and one SDL core handle. The engine module does not load SDL for cold allocation or host-memory queries. Before the first SDL service, native allocation setup completes, the selected SDL core loads and a private dispatch table binds that exact process-lifetime handle once. Rendering uses those generated pinned dispatch entries. This preserves lazy SDL loading and avoids duplicate cores or recursive initialization. Linux font payloads retain immediate local/deep binding against host font symbols; the common bridge uses normal loading. TLS retains its separate platform loader and .NET provider contract. There is no public setup or unload API.

Desktop paths use runtimes/<RID>/native beneath AppContext.BaseDirectory and a portable OS/process-architecture RID. Android uses the APK native-library search path; Apple/Web use their application link and import-table rules. Native package targets preserve those paths for project and package consumers. [Native delivery](../native-packaging.md) separates the new source/package contract from prior component-layout receipts.

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

Runs automatically once as a module initializer. EngineLibrary returns the retained common native handle. SDL imports return the selected shared core; image/shadercross/audio load that core before their own files. FreeType, HarfBuzz and TLS imports retain their separate platform resolution paths. Other names return zero untouched. Missing native libraries propagate at first relevant use. Calling Initialize manually a second time is unsupported because .NET allows one resolver per assembly.

## Verification and limits

The unified module changes native packaging without changing the public managed API. Its Linux verification and remaining target gates are recorded in [native delivery](../native-packaging.md). Earlier component-bundle runs do not qualify this module. The owned SDL source profile remains a separate allocation-accounting qualification.
