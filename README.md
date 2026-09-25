# Electron2D

Electron2D is a typed C# 2D game-engine runtime. The runtime is built as `Electron2D.dll`; applications supply their own platform host and native SDL libraries. Current runtime targets and **verified renderer behavior** are separate below.

## Platforms and renderers

| Platform | Runtime ID / ABI | SDL_Renderer canvas | SDL_GPU canvas and ShaderMaterial | Other programmable GPU path |
| --- | --- | --- | --- | --- |
| Linux Wayland and XWayland | `linux-x64` | Verified native pixels | Verified Vulkan and HLSL/GLSL shader pixels | — |
| Linux ARM64 | `linux-arm64` | Native package mapped; device execution pending | Native package mapped; device execution pending | — |
| Windows | `win-x86`, `win-x64`, `win-arm64` | Native packages mapped; Windows host check pending | Native packages mapped; Windows host check pending | — |
| macOS | `osx-x64`, `osx-arm64` | Native packages mapped; macOS host check pending | Native packages mapped; macOS host check pending | — |
| Android phone, tested SM-A256E | `android-arm64` | Verified OpenGL ES 2 pixels | Verified Vulkan and HLSL/SPIR-V shader pixels with reduced optional Vulkan features | — |
| Android TV, tested MiTV-MSSP3 | `android-arm` (`armeabi-v7a`) | Verified OpenGL ES 2 pixels; automatic fallback works | SDL_GPU unavailable on this device; its userspace does not advertise Vulkan | Standalone [GLES2 shader probe](tests/Electron2D.AndroidProbe/README.md) and red pixel verified; Electron2D shader backend pending |
| Other Android ABIs | `android-x86`, `android-x64`; `android-arm` on other devices | Packages mapped; device checks pending | Packages mapped; device checks pending | — |
| iOS and tvOS | iOS/tvOS device and simulator RIDs | Native packages mapped; [macOS CI](.github/workflows/apple-library.yml) prepared, run pending | Native packages mapped; macOS CI run pending | — |
| Web browser | `browser-wasm` | Red canvas verified in an isolated SDL/Emscripten probe; product host pending | SDL 3.4.16 Web build exposes no SDL_GPU driver | Standalone [Chrome WebGPU shader probe](tests/Electron2D.WebGpuProbe/README.md) verified; Electron2D backend pending |

“Verified” means the engine rendered on the named device or display backend. “Standalone” means a graphics API probe ran outside the Electron2D renderer. A native package or successful compilation alone is not runtime support. Android TV uses Android packages and its actual 32-bit ABI. Physics currently runs through Box2D.NET on the CPU; its contact simulation passed on Linux, the tested Android phone and TV, and an isolated WebAssembly probe. GPU physics is outside the current runtime.

See the [verification matrix](docs/platform-verification.md) for commands, evidence and limits, [ADR 0021](docs/decisions/product.md#adr-0021) for target scope, and [ADR 0028](docs/decisions/rendering.md#adr-0028) for renderer policy.

## Build and check

```bash
dotnet build Electron2D.csproj -c Release
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

The [Android device probe](tests/Electron2D.AndroidProbe/README.md) publishes a test-only APK for separate SDL_Renderer, SDL_GPU, shader and CPU-physics checks. A game application needs its own host, lifecycle and packaging. The [MIT license](licence/Electron2D-LICENSE.txt) and [third-party notices](licence/THIRD_PARTY_NOTICES.md) are kept in `licence/`.
