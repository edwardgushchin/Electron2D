# Android display and canvas probe

This test-only SDLActivity runs the public Electron2D `Engine.Run` path. It draws one red rectangle for 900 process frames, then quits with code zero. It is an executable device smoke, not a production application host.

From the repository root, publish for the device ABI:

```bash
dotnet publish tests/Electron2D.AndroidProbe/Electron2D.AndroidProbe.csproj -c Release -r android-arm64 --self-contained true
# Use -r android-arm for a 32-bit Android TV.
```

Install `tests/Electron2D.AndroidProbe/bin/Release/net10.0-android/<RID>/publish/org.electron2d.probe-Signed.apk` with `adb -s SERIAL install -r`, launch `org.electron2d.probe` with `adb -s SERIAL shell monkey -p org.electron2d.probe 1`, and inspect `adb -s SERIAL logcat -d -s Electron2DProbe:I` for `RUN`, `DRAW`, and `DONE 0`. Capture a device screenshot while it runs to verify the red rectangle. The TV used for the initial check had 32-bit `armeabi-v7a` userspace, so a 64-bit APK would not exercise it.

When switching between `android-arm` and `android-arm64` in one checkout, remove the previous RID directory under `tests/Electron2D.AndroidProbe/obj/Release/net10.0-android/` if the Android Java compiler reports stale generated sources. The APK may emit Android warning `XA4301` about a duplicate `libSDL3.so` path; the tested APK installed and ran. This probe does not cover suspend/resume, controller input, audio, storage, store packaging, or release signing.
