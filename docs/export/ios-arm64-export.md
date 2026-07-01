# iOS arm64 export

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0092`.
Обновлено: 2026-06-24.

## Назначение

Electron2D `0.1.0 Preview` должен экспортировать один проект в iOS runtime package без переписывания игровой логики. iOS является платформой запуска и экспорта, но не платформой редактирования проекта.

iOS входит в `runtimeTargets` и в `releaseVerificationTargets` текущего preview-релиза. Blocked-environment artifact допустим, когда macOS/Xcode, simulator/device или iOS workload недоступны, но такой artifact не закрывает release gate: финальная проверка требует реального simulator или device smoke/soak либо отдельного изменения `docs/releases/0.1.0-preview.md`.

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
- На macOS с Xcode выполнена smoke-проверка на iOS-симуляторе или подключённом устройстве: жизненный цикл приложения, рендеринг, ввод, звук, загрузка ресурсов, файловая система, безопасная область экрана (`safe area`) и корректное завершение.
- Implementation documentation описывает фактический target, layout, diagnostics, limitations и команды проверки.
- Focused export tests, source license/header checks и documentation verifiers проходят.

## Фактическое состояние, ограничения и проверки

## Статус

`IosArm64` с runtime identifier `ios-arm64` остаётся заблокированным mobile release target для Electron2D `0.1.0 Preview`. В текущем состоянии репозитория это не готовый release path, потому что обязательный macOS/Xcode smoke на simulator или device ещё не выполнен.

Текущий репозиторий умеет fail closed проверять iOS preset inputs, создавать deterministic Xcode project staging plan, записывать transient iOS staging project и сохранять structured smoke artifact со статусом `blocked`, когда simulator или device недоступны. Он пока не запускает `xcodebuild`, не подписывает app, не устанавливает app на simulator или device и не проходит реальный iOS smoke.

## SDK и toolchain

Для завершения iOS export всё ещё нужны:

- macOS host с поддерживаемой установкой Xcode;
- .NET SDK `10.0.x` с установленным iOS workload;
- `xcodebuild` и simulator tooling, доступные через `PATH`;
- настроенные ссылки на Apple signing identity и provisioning profile;
- smoke-покрытие на simulator или device для lifecycle, input, audio, resources, filesystem, orientation и safe-area behavior.

Текущий validator возвращает diagnostics, когда Xcode недоступен. Planner и staging builder остаются deterministic и не помечают iOS export как завершённый молча.

## Текущий planner

`Electron2DIosExportPlanner.CreatePlan(...)` строит внутренний plan для:

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
- smoke criteria для build, install, launch, render, input, lifecycle, orientation, safe area, audio, resources, filesystem, precompiled artifacts и shutdown.

Planner завершает проверку fail closed для wrong target, wrong runtime identifier, framework-dependent deployment, missing project path, missing project settings и missing signing identity, когда signing обязателен.

## Текущий staging builder

`Electron2DIosXcodeProjectBuilder.Build(...)` записывает transient staging layout внутри preset output directory:

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

Staging builder копирует `project.e2d.json`, main scene и `assets/**`. Он не копирует `.electron2d/tasks/**`, `TASKS.md`, `dev-diary/` или `completed-tasks/`.

Сгенерированные host files содержат smoke markers для launch, render, touch, safe area, foreground/background lifecycle, audio, resources, filesystem, precompiled rendering artifacts и shutdown. Эти markers являются только staging evidence, пока они не будут подтверждены на реальном iOS simulator или device.

## Текущие CLI routes

Текущий CLI открывает planner и staging builder, но не утверждает, что iOS release-ready:

```bash
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-ios --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-ios --project <project-root> --output exports/ios/debug --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-ios --project <project-root> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json
```

`plan-ios` возвращает deterministic plan. `build-ios --skip-publish true` записывает transient staging project. `run-ios` записывает `Electron2D.IosDeviceSmokeArtifact`; на hosts без simulator/device evidence команда завершается failure с `data.result.status = "smoke-blocked"` и diagnostic `E2D-EXPORT-IOS-0011`.

## Текущий smoke artifact

`Electron2DIosDeviceSmokeRunner.Run(...)` записывает JSON artifact с format `Electron2D.IosDeviceSmokeArtifact`.

Когда simulator/device evidence отсутствует, artifact status равен `blocked`, diagnostic `E2D-EXPORT-IOS-0011` записывается, а все required criteria остаются failed. Это полезно для прозрачности release gate, но не закрывает `T-0092`.

## Signing и credentials

Release iOS export потребует signing. Файлы репозитория могут хранить только non-secret references:

- signing identity label;
- provisioning profile name;
- bundle identifier;
- CI secret name;
- имя переменной окружения, которую будет использовать будущий tooling.

Файлы репозитория не должны содержать private keys, certificates, provisioning profile contents, passwords, access tokens или скопированные secret payloads.

## Известные ограничения

- `.ipa` packaging не реализован.
- Выполнение signing не реализовано.
- Install и launch на simulator/device не реализованы.
- Smoke checks для pause/resume, orientation, safe area, touch, virtual keyboard, audio, resources и filesystem не реализованы.
- CI workflow показывает mobile export как status gap вместо запуска iOS packaging.

## Проверка

Focused local tests для текущего planner/staging/smoke artifact:

```bash
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~IosExportTests"
```

Verifier для iOS release package, который запускается на simulator или device, всё ещё отсутствует. Финальная приёмка `T-0092` требует macOS host с Xcode и реальный simulator/device smoke run.
