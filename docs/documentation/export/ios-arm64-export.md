# iOS arm64 export

## Status

`IosArm64` with runtime identifier `ios-arm64` is documented as a blocked mobile target for Electron2D `0.1.0 Preview`. It is not a ready release path in the current repository state.

The current repository can validate iOS preset inputs fail-closed, but it does not generate an Xcode project, does not sign an app, does not deploy to a simulator or device, and does not run an iOS smoke check.

## SDK and toolchain

Future iOS export requires:

- macOS host with a supported Xcode installation;
- .NET SDK `10.0.x` with the iOS workload installed;
- `xcodebuild` and simulator tooling available on `PATH`;
- configured Apple signing identity and provisioning profile references;
- simulator or device smoke coverage for lifecycle, input, audio, resources, filesystem, orientation, and safe-area behavior.

The current validator returns diagnostics when Xcode is unavailable. It must not silently continue with a partial export plan.

## Signing and credentials

Release iOS export will require signing. Repository files may store only non-secret references:

- signing identity label;
- provisioning profile name;
- bundle identifier;
- CI secret name;
- environment variable name used by future tooling.

Repository files must not contain private keys, certificates, provisioning profile contents, passwords, access tokens, or copied secret payloads.

## Known limitations

- Xcode project generation is not implemented.
- `.ipa` packaging is not implemented.
- Signing execution is not implemented.
- Simulator/device install and launch are not implemented.
- Pause/resume, orientation, safe area, touch, virtual keyboard, audio, resources, and filesystem smoke checks are not implemented.
- The CI workflow reports mobile export as a status gap instead of running iOS packaging.

## Verification

There is no iOS export verifier to run for a release package yet. Current coverage is limited to preset validation tests that fail closed when Xcode or signing references are absent.
