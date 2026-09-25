# Android platform probe

This test-only SDLActivity exercises Electron2D through five independent scenarios selected by the Android intent extra `scenario`. It is not a production application host.

| Scenario | Check |
| --- | --- |
| `compatibility` (default) | Force SDL_Renderer, draw a red rectangle, read back pixel `(4, 4)`, and quit. |
| `gpu` | Disable fallback, enumerate SDL GPU drivers, attempt a GPU canvas and read back the same pixel. |
| `auto` | Request the GPU with fallback enabled; verify which renderer actually produced the pixel. |
| `shader` | Load the pinned HLSL SPIR-V fixture, disable fallback, draw through `ShaderMaterial`, and read back the pixel. |
| `physics` | Step a dynamic Box2D body onto a static floor for 120 frames; require `75 < Y < 85` and nearly zero velocity. |

Publish separately for each device ABI from the repository root:

```bash
dotnet publish tests/Electron2D.AndroidProbe/Electron2D.AndroidProbe.csproj -c Release -r android-arm64 --self-contained true
# For 32-bit Android TV use -r android-arm.
```

Install the signed APK under `tests/Electron2D.AndroidProbe/bin/Release/net10.0-android/<RID>/publish/` with `adb -s SERIAL install -r`. Resolve its launcher component using `adb -s SERIAL shell cmd package resolve-activity --brief -a android.intent.action.MAIN -c android.intent.category.LAUNCHER org.electron2d.probe`. Run each scenario with `adb -s SERIAL shell am start -S -n COMPONENT --es scenario SCENARIO`, then inspect `adb -s SERIAL logcat -d -s Electron2DProbe:I`. A successful stage ends with `DONE SCENARIO`; `FAIL SCENARIO` records the exception. The GPU/shader stages must not be counted as passed when the device reports no suitable SDL GPU device.

On 2026-09-25 the Samsung SM-A256E (`android-arm64`, API 36) and MiTV-MSSP3 Android TV (`android-arm`, API 30) passed compatibility pixel and physics checks. With the relaxed Vulkan feature set, the phone passed GPU and shader pixels. The TV rejected SDL GPU creation even with those optional features disabled; its `auto` scenario uses SDL_Renderer. The tested TV advertises OpenGL ES 2 but no Vulkan feature. See the [platform verification matrix](../../docs/platform-verification.md). An APK build may emit XA4301 for a duplicate `libSDL3.so` path; the tested APK installed and ran.
