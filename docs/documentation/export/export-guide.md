# Export guide

<!-- export-doc:overview -->
## Overview

Electron2D `0.1.0 Preview` has a verified desktop export baseline, a WebAssembly browser package/smoke baseline, and an Android arm64 debug APK baseline with emulator smoke evidence. This page is the user-facing entry point for export documentation: it explains which targets are currently verified, which toolchains are required, how signing references are represented, and which commands prove the current repository state.

The export layer is intentionally fail-closed. A missing SDK, unsupported runtime identifier, missing signing identity, or missing project setting must produce diagnostics instead of a partial package.

<!-- export-doc:target-matrix -->
## Target matrix

| Target | Runtime identifier | Status | Verification |
| --- | --- | --- | --- |
| `WindowsX64` | `win-x64` | Verified desktop baseline | `tools\Verify-WindowsExport.ps1` on Windows |
| `LinuxX64` | `linux-x64` | Verified desktop baseline | `tools\Verify-LinuxExport.ps1` on Linux or WSL |
| `MacOSArm64` | `osx-arm64` | Verified desktop baseline | `tools\Verify-MacOSExport.ps1` on macOS arm64 |
| `AndroidArm64` | `android-arm64` | Debug APK baseline with emulator smoke; release AAB signing plan | [Android arm64 export](android-arm64-export.md) |
| `IosArm64` | `ios-arm64` | Planner/staging baseline; blocked simulator/device smoke | [iOS arm64 export](ios-arm64-export.md) |
| `WebAssemblyBrowser` | `browser-wasm` | Package and smoke baseline | [WebAssembly browser export](webassembly-browser-export.md) |

Desktop export means the repository can create, publish, and run the empty project package on the matching host. The WebAssembly browser baseline can plan the static package layout, create host/loader/manifest files, copy project resources, and write a structured smoke artifact. Android can now plan/stage debug APK and release AAB workflows, build a debug APK when the local Android toolchain is available, install/run it through `adb`, and produce a structured smoke artifact. iOS can now plan and stage a transient Xcode project and write a blocked smoke artifact, but remains blocked as a release path until a macOS/Xcode simulator or device smoke run passes.

<!-- export-doc:preset-file -->
## Preset file

Export presets live in `export_presets.e2export.json`. The current model is documented in [Export preset model](export-preset-model.md).

Each preset names a target, configuration, runtime identifier, output directory, renderer profile, debug symbol policy, and signing references. The file must not contain secret values. Use references such as a CI secret name, environment variable name, certificate alias, or signing identity label instead of passwords, private keys, keystore contents, certificates, provisioning profile contents, or tokens.

<!-- export-doc:desktop-verification -->
## Desktop verification

Run the verifier that matches the host operating system:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-WindowsExport.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-LinuxExport.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-MacOSExport.ps1
```

Each verifier builds the local package, creates a temporary project from `data/templates/electron2d-empty`, restores that project from the local package source, publishes a self-contained build, runs the exported executable, and checks the reference scene output.

The macOS verifier must run on macOS arm64. The Linux verifier runs on Linux directly or through WSL on Windows. The Windows verifier must run on Windows.

<!-- export-doc:mobile-status -->
## Mobile status

`AndroidArm64` has planner, staging, debug APK build and device/emulator smoke commands:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-android --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --output exports/android/debug --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-android --project <project-root> --output exports/android/debug --smoke-output .electron2d/export-smoke/android-smoke.json --adb-path <path-to-adb> --adb-serial <serial> --format json
```

`run-android` is intentionally fail-closed until a connected authorized device or emulator is available. Use `--adb-serial` when more than one Android target is connected. For `x86_64` emulators, the command builds a temporary `android-x64` smoke package; this does not replace the `android-arm64` production preset.

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-ios --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-ios --project <project-root> --output exports/ios/debug --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-ios --project <project-root> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json
```

`IosArm64` currently has a CLI planner, transient Xcode project staging builder and blocked smoke artifact writer. It does not run `xcodebuild`, signing, install, launch or simulator/device smoke from this repository state. Mobile export remains a release gate until each platform task provides device/simulator smoke checks, reference-game evidence and CI/reporting evidence.

<!-- export-doc:web-status -->
## WebAssembly browser status

`WebAssemblyBrowser` with runtime identifier `browser-wasm` has planner, package and smoke commands:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-web --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-web --project <project-root> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json
```

`plan-web` returns a JSON plan for `wwwroot`, `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, `_framework`, `assets`, `project.e2d.json`, the main scene path, browser policies and smoke criteria. `build-web --skip-publish true` creates the static files without invoking external publish; normal `build-web` first validates matching WebAssembly build tools and then runs `dotnet publish`. `run-web` checks the generated package and writes a structured smoke artifact with launch instructions. These commands do not queue workspace jobs, deploy to hosting, or publish remote artifacts.

<!-- export-doc:signing-credentials -->
## Signing and credentials

Signing data is split into two categories:

- public or non-secret labels: signing identity name, certificate alias, profile name, bundle identifier, package id;
- secret-bearing material: passwords, tokens, private keys, keystore contents, certificate contents, provisioning profile contents.

Repository files may contain only the first category. Secret-bearing material must stay outside the repository and be referenced through `credentialReference` or the future editor/CI secret configuration.

Verifier scripts do not read real secrets and do not publish artifacts.

<!-- export-doc:known-limitations -->
## Known limitations

- Desktop export is verified against the empty project template, not finished reference games.
- Windows x64, Linux x64 glibc, and macOS arm64 are the only verified desktop targets.
- Linux musl, Linux ARM, Windows ARM, macOS x64, Android release signing with real credentials, and iOS simulator/device smoke are outside the current verified native export baseline.
- Automated browser launch, remote hosting deploy, PWA installation and service worker caching are outside the current WebAssembly baseline.
- Android lifecycle, orientation, safe area, touch, package staging, debug APK build and emulator deployment/runtime smoke have a repository baseline, but release signing publish and long reference-game soak are not complete release paths.
- GitHub Release publication is intentionally outside these verifier commands and requires a separate user command.

## Documentation check

After editing export documentation, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ExportDocumentation.ps1
```
