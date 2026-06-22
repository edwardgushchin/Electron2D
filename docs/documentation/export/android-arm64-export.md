# Android arm64 export

## Status

`AndroidArm64` with runtime identifier `android-arm64` is documented as a blocked mobile target for Electron2D `0.1.0 Preview`. It is not a ready release path in the current repository state.

The current repository can validate Android preset inputs fail-closed, but it does not build an APK, does not build an AAB, does not sign a package, does not deploy to a device, and does not run a real-device smoke check.

## SDK and toolchain

Future Android export requires:

- .NET SDK `10.0.x` with the Android workload installed;
- Android SDK path from `ANDROID_HOME`, `ANDROID_SDK_ROOT`, or future editor settings;
- Android NDK path compatible with the selected workload;
- platform tools with `adb` available to the future verifier;
- a connected device or managed emulator for lifecycle, input, audio, resource, filesystem, orientation, and safe-area smoke checks.

The current validator returns diagnostics when the Android SDK or NDK path is missing. It must not silently continue with a partial export plan.

## Signing and credentials

Release Android export will require signing. Repository files may store only non-secret references:

- signing identity label;
- keystore alias;
- CI secret name;
- environment variable name used by future tooling.

Repository files must not contain keystore contents, key passwords, store passwords, private keys, certificates, access tokens, or copied secret payloads.

## Known limitations

- APK debug export is not implemented.
- AAB release export is not implemented.
- Signing execution is not implemented.
- Device/emulator install and launch are not implemented.
- Pause/resume, orientation, safe area, touch, virtual keyboard, audio, resources, and filesystem smoke checks are not implemented.
- The CI workflow reports mobile export as a status gap instead of running Android packaging.

## Verification

There is no Android export verifier to run for a release package yet. Current coverage is limited to preset validation tests that fail closed when SDK, NDK, or signing references are absent.
