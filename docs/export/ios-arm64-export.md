# iOS arm64 export

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0092`.
Обновлено: 2026-06-23.

## Назначение

Electron2D `0.1.0 Preview` должен экспортировать один проект в iOS runtime package без переписывания игровой логики. iOS является платформой запуска и экспорта, но не платформой редактирования проекта.

`T-0092` считается закрытой только после macOS/Xcode проверки с iOS simulator или подключённым iOS device. Частичные шаги, которые можно проверить на Windows, не являются готовым release path, но должны быть полезны для финального gate:

- deterministic Xcode project plan без запуска внешних процессов;
- signing plan только со ссылками на внешние credentials;
- mobile runtime policy для touch, safe area, lifecycle, audio, resources, filesystem и precompiled rendering artifacts;
- structured smoke artifact, который может честно вернуть `blocked`, если simulator/device недоступен.

App Store Connect automation, внешний upload, cloud signing и публикация не входят в `0.1.0 Preview`.

## Export target

Preset для iOS export использует:

- `target`: `IosArm64`;
- `runtimeIdentifier`: `ios-arm64`;
- `configuration`: `Debug` или `Release`;
- `selfContained`: `true`;
- `rendererProfile`: `Automatic`, `Compatibility` или `Standard`;
- `outputDirectory`: корневая папка iOS export artifacts;
- `includeDebugSymbols`: разрешён для debug workflow;
- `signing.required`: `true` для device/release workflow; simulator debug может быть unsigned только как локальный blocked/planning mode.

Planner должен fail closed для target, отличного от `IosArm64`, runtime identifier, отличного от `ios-arm64`, framework-dependent deployment, отсутствующего project file path, отсутствующих project settings и required signing без identity.

## Toolchain

Минимальный iOS toolchain для финальной проверки:

- macOS host;
- Xcode и `xcodebuild`;
- simulator tooling или подключённый iOS device;
- .NET SDK `10.0.x` с iOS workload;
- user-provided signing identity и provisioning profile references.

Validator и planner не запускают `xcodebuild`, signing, deploy, publication и не читают секреты. Они получают обнаруженные факты окружения и возвращают deterministic diagnostics. При отсутствии macOS/Xcode финальный smoke должен сохранять blocked artifact и не объявлять export успешным.

Stable diagnostics:

- `E2D-EXPORT-IOS-0001` - iOS preset использует target, отличный от `IosArm64`;
- `E2D-EXPORT-IOS-0002` - iOS preset использует runtime identifier, отличный от `ios-arm64`;
- `E2D-EXPORT-IOS-0003` - iOS export не self-contained;
- `E2D-EXPORT-IOS-0004` - project file path отсутствует;
- `E2D-EXPORT-IOS-0005` - project settings отсутствуют;
- `E2D-EXPORT-IOS-0006` - project settings некорректны;
- `E2D-EXPORT-IOS-0007` - required signing identity отсутствует;
- `E2D-EXPORT-IOS-0008` - Xcode project staging не удалось записать;
- `E2D-EXPORT-IOS-0009` - обязательный staged file отсутствует или не читается;
- `E2D-EXPORT-IOS-0010` - путь ресурса выходит за пределы project root;
- `E2D-EXPORT-IOS-0011` - iOS simulator или device недоступен;
- `E2D-EXPORT-IOS-0012` - smoke criterion не прошёл.
- `E2D-EXPORT-IOS-0013` - Xcode недоступен для iOS toolchain validation.

Сообщения diagnostics не должны раскрывать private keys, certificate bodies, provisioning profile contents, passwords, tokens или secret values.

## Xcode project plan

Внутренний planner должен строить deterministic plan без side effects:

- path к исходному `.csproj`;
- `dotnet publish` arguments для `net10.0-ios` и `ios-arm64`;
- output directory;
- staging directory;
- generated Xcode project directory и `.xcodeproj` path;
- app bundle path;
- `Info.plist` path;
- entitlements path;
- export metadata path;
- project assets directory;
- app name, executable name, bundle identifier;
- renderer profile;
- graphics backend label `metal`;
- mobile policies;
- smoke criteria;
- required staged files;
- signing-required state, identity и credential reference.

Planner не должен записывать файлы и не должен читать secret material.

## Staging layout

Xcode project builder должен создавать transient staging layout внутри output directory:

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
    debug/
    release/
  smoke/
```

Staging project не становится canonical game project и не записывается в исходный project root. `Assets/electron2d/**` содержит project settings, main scene и игровые resources. `EditorMetadata`, включая `.electron2d/tasks/**`, local-only `TASKS.md`, `dev-diary/` и `completed-tasks/`, не входит в staging.

Generated host должен фиксировать:

- touch path;
- safe area snapshot;
- foreground/background lifecycle markers;
- audio route marker;
- resource loading marker;
- filesystem save-data marker;
- rendering readiness marker;
- precompiled rendering artifact policy for iOS.

## Build workflow

Plan для будущего build использует:

```text
dotnet publish <ios csproj> --configuration <Debug|Release> --framework net10.0-ios --runtime ios-arm64 --self-contained true --output <artifacts>
xcodebuild -project <xcodeproj> -scheme Electron2D.iOS -configuration <Debug|Release> -destination <simulator-or-device>
```

Planner может сохранить deterministic command arguments, но не выполняет команды. Реальный `xcodebuild`, signing и deploy допустимы только в verifier или CLI-команде, запущенной на macOS после явного выбора simulator/device/signing environment.

CLI должен открыть тот же безопасный путь, что и внутренний planner:

```text
e2d export plan-ios --project <project-root> --format json
e2d export build-ios --project <project-root> --output exports/ios/debug --skip-publish true --format json
e2d export run-ios --project <project-root> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json
```

`plan-ios` возвращает deterministic JSON plan без записи файлов. `build-ios --skip-publish true` создаёт transient Xcode staging project без запуска `dotnet publish`, `xcodebuild`, signing или deploy. `run-ios` обязан записать structured smoke artifact; если simulator/device evidence не передан и не обнаружен, команда возвращает failure со статусом `smoke-blocked` и diagnostic `E2D-EXPORT-IOS-0011`.

## Simulator/device smoke

Smoke artifact должен проверять:

1. build;
2. install;
3. launch;
4. rendering readiness;
5. touch input;
6. foreground/background lifecycle;
7. orientation;
8. safe area;
9. audio;
10. resources;
11. filesystem save data;
12. precompiled rendering artifacts;
13. clean shutdown.

Если macOS/Xcode/simulator/device недоступны, smoke runner обязан сохранить artifact со статусом `blocked`, diagnostic `E2D-EXPORT-IOS-0011` и всеми criteria как not passed. Blocked artifact не закрывает `T-0092`, но нужен для прозрачного release gate.

## Критерии приёмки

- Export preset model round-trip поддерживает `IosArm64`.
- Toolchain validator fail closed, если Xcode или signing references отсутствуют для preset-а, которому они нужны.
- iOS planner создаёт deterministic `ios-arm64` Xcode project plan, Metal graphics backend, signing plan, mobile policies и smoke criteria.
- iOS planner fail closed для wrong target, wrong runtime identifier, framework-dependent deployment, missing project file path, missing project settings и missing signing identity.
- iOS Xcode project builder создаёт staging project, host files, metadata, project settings, main scene и `assets/**`, но не копирует `.electron2d/tasks/**`.
- iOS smoke runner создаёт structured artifact; при отсутствии simulator/device возвращает blocked/failure с `E2D-EXPORT-IOS-0011`.
- CLI routes `e2d export plan-ios`, `e2d export build-ios` и `e2d export run-ios` возвращают stable JSON envelope и не queue generic export job.
- На macOS host с Xcode выполнен simulator или device smoke для lifecycle, render, input, audio, resources, filesystem, safe area и clean shutdown.
- Implementation documentation описывает фактический target, layout, diagnostics, limitations и команды проверки.
- Focused export tests, source license/header checks и documentation verifiers проходят.

## Фактическое состояние, ограничения и проверки

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
