# iOS arm64 export

## Status

`IosArm64` with runtime identifier `ios-arm64` is still a blocked mobile release target for Electron2D `0.1.0 Preview`. It is not a ready release path in the current repository state because the required macOS/Xcode simulator or device smoke has not been run.

The current repository can validate iOS preset inputs fail-closed, create a deterministic Xcode project staging plan, write a transient iOS staging project, and write a structured smoke artifact that reports `blocked` when no simulator or device is available. It does not run `xcodebuild`, does not sign an app, does not deploy to a simulator or device, and does not pass a real iOS smoke check yet.

## SDK and toolchain

iOS export completion still requires:

- macOS host with a supported Xcode installation;
- .NET SDK `10.0.x` with the iOS workload installed;
- `xcodebuild` and simulator tooling available on `PATH`;
- configured Apple signing identity and provisioning profile references;
- simulator or device smoke coverage for lifecycle, input, audio, resources, filesystem, orientation, and safe-area behavior.

The current validator returns diagnostics when Xcode is unavailable. The planner and staging builder are deterministic and do not silently mark iOS export as complete.

## Current planner

`Electron2DIosExportPlanner.CreatePlan(...)` builds an internal plan for:

- target `IosArm64`;
- runtime identifier `ios-arm64`;
- target framework `net10.0-ios`;
- architecture `arm64`;
- Metal rendering backend label;
- transient `ios/` staging directory;
- `Electron2D.iOS.csproj`;
- `AppDelegate.cs`;
- `Info.plist`;
- `Entitlements.plist`;
- `ExportMetadata.json`;
- `Electron2D.iOS.xcodeproj/project.pbxproj`;
- `Assets/electron2d/**` project resource area;
- app bundle path under `artifacts/<configuration>/`;
- signing identity and credential reference as non-secret strings;
- mobile policies for touch, foreground/background lifecycle, safe area, orientation, audio, resource loading, filesystem sandbox and precompiled rendering artifacts;
- smoke criteria for build, install, launch, render, input, lifecycle, orientation, safe area, audio, resources, filesystem, precompiled artifacts and shutdown.

The planner fail-closes for wrong target, wrong runtime identifier, framework-dependent deployment, missing project path, missing project settings and missing signing identity when signing is required.

## Current staging builder

`Electron2DIosXcodeProjectBuilder.Build(...)` writes a transient staging layout under the preset output directory:

```text
<outputDirectory>/
  ios/
    Electron2D.iOS.csproj
    AppDelegate.cs
    Info.plist
    Entitlements.plist
    ExportMetadata.json
    Electron2D.iOS.xcodeproj/
      project.pbxproj
    Assets/
      electron2d/
        project.e2d.json
        <main scene path>
        assets/
  artifacts/
    <configuration>/
  smoke/
```

The staging builder copies `project.e2d.json`, the main scene and `assets/**`. It does not copy `.electron2d/tasks/**`, `TASKS.md`, `dev-diary/` or `completed-tasks/`.

The generated host files include smoke markers for launch, render, touch, safe area, foreground/background lifecycle, audio, resources, filesystem, precompiled rendering artifacts and shutdown. These markers are only staging evidence until they are observed on a real iOS simulator or device.

## Current CLI routes

The current CLI exposes the planner and staging builder without claiming that iOS is release-ready:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-ios --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-ios --project <project-root> --output exports/ios/debug --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-ios --project <project-root> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json
```

`plan-ios` returns the deterministic plan. `build-ios --skip-publish true` writes the transient staging project. `run-ios` writes `Electron2D.IosDeviceSmokeArtifact`; on hosts without simulator/device evidence it exits as a failure with `data.result.status = "smoke-blocked"` and diagnostic `E2D-EXPORT-IOS-0011`.

## Current smoke artifact

`Electron2DIosDeviceSmokeRunner.Run(...)` writes a JSON artifact with format `Electron2D.IosDeviceSmokeArtifact`.

When simulator/device evidence is missing, the artifact status is `blocked`, diagnostic `E2D-EXPORT-IOS-0011` is emitted and all required criteria remain failed. This is useful for release gate transparency, but it does not close `T-0092`.

## Signing and credentials

Release iOS export will require signing. Repository files may store only non-secret references:

- signing identity label;
- provisioning profile name;
- bundle identifier;
- CI secret name;
- environment variable name used by future tooling.

Repository files must not contain private keys, certificates, provisioning profile contents, passwords, access tokens, or copied secret payloads.

## Known limitations

- `.ipa` packaging is not implemented.
- Signing execution is not implemented.
- Simulator/device install and launch are not implemented.
- Pause/resume, orientation, safe area, touch, virtual keyboard, audio, resources, and filesystem smoke checks are not implemented.
- The CI workflow reports mobile export as a status gap instead of running iOS packaging.

## Verification

Focused local tests for the current planner/staging/smoke artifact:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~IosExportTests"
```

There is still no iOS release-package verifier that runs on a simulator or device. Final `T-0092` acceptance requires a macOS host with Xcode and a real simulator/device smoke run.
