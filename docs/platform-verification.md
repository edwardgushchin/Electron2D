# Platform verification matrix

Last updated: 2026-09-25

This matrix records execution on hosts and devices available during this audit. A successful build or packaged native library is not a runtime check. A blocked backend is a measured result for this build and device, not a claim that an entire platform can never support it.

| Runtime tested | SDL_Renderer canvas | SDL_GPU canvas | Shader pixels | Physics |
| --- | --- | --- | --- | --- |
| Linux Wayland, current host | Pass: compatibility pixel suite | Pass: GPU pixel suite | Pass: HLSL and GLSL material suite | Pass: managed Box2D suite and native interpolation pixels |
| Linux X11 through XWayland, current host | Pass: compatibility pixel suite | Pass: GPU pixel suite | Pass: HLSL and GLSL material suite | Pass: native interpolation pixels; same managed Box2D runtime |
| Samsung SM-A256E, Android API 36, `arm64-v8a` | Pass: `opengles2`, red readback `(1,0,0)` | Pass: Vulkan device with optional clip-distance, depth-clamping, indirect-first-instance and anisotropy disabled; red readback `(1,0,0)` | Pass: pinned HLSL SPIR-V material, red readback `(1,0,0)` | Pass: dynamic rectangle rests at `Y=80.009`, vertical velocity `0` after 120 steps |
| MiTV-MSSP3, Android TV API 30, `armeabi-v7a` | Pass: `opengles2`, red readback `(1,0,0)` | Blocked: OpenGL ES 2 is advertised, Vulkan is not; SDL reports no supported GPU device even after optional features are disabled. `auto` falls back to `opengles2` and preserves the red pixel | Blocked before material submission by GPU device creation | Pass: dynamic rectangle rests at `Y=80.009`, vertical velocity `0` after 120 steps |
| WebAssembly in the isolated SDL 3.4.16/Emscripten browser probe | Pass: direct SDL_Renderer red canvas in Chrome | Blocked: Chrome has WebGL2 and a WebGPU adapter/device, but this SDL build lists zero GPU drivers and `SDL_CreateGPUDevice(SPIRV)` returns null | Blocked by absent GPU backend; no shader pixel claim | Pass: managed Box2D contact at `Y=80.009` after 120 steps |
| iOS/tvOS | Not run: no macOS/Xcode host | Not run | Not run | Not run |
| Windows/macOS | Not run: no native host in this environment | Not run | Not run | Not run |

## Reproduce the checked paths

- Linux display, GPU, fallback, and shader pixels: `SDL_VIDEODRIVER=wayland ELECTRON2D_TEST_RENDER=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` and the same command with `SDL_VIDEODRIVER=x11`. Both runs ended with `Rendering runtime checks passed`; the X11 run used XWayland, not a standalone Xorg session.
- Linux scene physics with native canvas interpolation: `SDL_VIDEODRIVER=wayland ELECTRON2D_TEST_INTERPOLATION_NATIVE=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` and the same command with `SDL_VIDEODRIVER=x11`. Both ended with `Native physics-interpolation pixels passed: compatibility`. The ordinary managed test suite includes broader Box2D collision and lifecycle checks.
- Android device runs: build and use [Electron2D.AndroidProbe](../tests/Electron2D.AndroidProbe/README.md). Its five scenarios separate forced fallback, automatic fallback, GPU, shader, and physics. The engine project excludes nested generated Android Java/JAR/native files so switching ABI builds does not bind a prior test application's output.
- Web: the browser result came from an isolated `/tmp` source snapshot and WebAssembly host, with SDL 3.4.16 built through Emscripten 3.1.56 and a temporary browser-compatible projection of the SDL3-CS P/Invoke bindings. It is diagnostic evidence only. No browser host, transitive native package, nonblocking frame loop, input/storage lifecycle, or shader GPU backend has been merged into the runtime. A two-frame `Engine.Run` returned successfully but disposed its canvas before sustained browser presentation.

The Linux Wayland gate in [ADR 0021](decisions/product.md#adr-0021) remains the current release gate. The Android results cover these tested devices and paths; they do not establish lifecycle, controller input, audio, storage, signing, or store packaging. The Web result proves only the isolated fallback and managed physics probe. Chrome hardware acceleration is available; a shader-capable Electron2D bridge to WebGL/WebGPU is still absent. [SDL states that its GPU API currently has no Web backend](https://wiki.libsdl.org/SDL3/FAQDevelopment). Apple and desktop runtime claims require their own hosts and device checks.
