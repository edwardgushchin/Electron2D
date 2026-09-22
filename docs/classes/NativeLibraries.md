# NativeLibraries

Last updated: 2026-09-22

- Declaration: `internal static class NativeLibraries`
- Source: [NativeLibraries.cs](../../src/Properties/NativeLibraries.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Installs Electron2D.dll's native import resolver on Linux before binding calls. The pinned packages contain aliases that can otherwise load two SDL core copies with inconsistent object ownership. A lazy process-lifetime core handle uses the canonical libSDL3.so.0 SONAME. There is intentionally no public setup or unload API.

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static void Initialize()` | [Module initialization](#module-initialization) |

## Member descriptions

### Module initialization

`internal static void Initialize()`

Runs automatically once as a module initializer. On non-Linux it returns. On Linux it registers the assembly resolver: SDL3 imports return the retained core handle; SDL3_image/SDL3_shadercross first force that same core to load and then return zero to use ordinary dependency resolution. Other names return zero untouched. Missing native libraries propagate at first relevant use. Calling Initialize manually a second time is unsupported because .NET allows one resolver per assembly.

## Verification and limits

Self-contained Linux x64 tests loaded images and GPU shaders with LD_LIBRARY_PATH unset; loader diagnostics found one published libSDL3.so.0. Other platforms use their ordinary loader and have not been natively accepted by this integration.
