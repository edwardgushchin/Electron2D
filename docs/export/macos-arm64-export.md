# macOS arm64 export

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0090`.
Обновлено: 2026-06-21.

## Назначение

macOS arm64 export должен превратить локальный Electron2D project в self-contained `.app` bundle для Apple Silicon desktop systems. Первая реализация не закрывает macOS x64, universal binaries, notarization, App Store packaging или deploy: эти capabilities должны быть отдельными задачами и явно fail-closed.

## Target contract

Поддерживаемый target:

- `target`: `MacOSArm64`;
- `runtimeIdentifier`: `osx-arm64`;
- `architecture`: `arm64`;
- `selfContained`: `true`;
- `configuration`: `Debug` или `Release`;
- `rendererProfile`: `Standard`, `Compatibility` или `Automatic`;
- graphics backend: Metal-backed desktop backend;
- output layout обязан создавать `<AppName>.app/Contents/Info.plist`;
- executable и runtime files размещаются в `<AppName>.app/Contents/MacOS/`, чтобы текущий template мог читать `project.e2d.json` через `AppContext.BaseDirectory`;
- `project.e2d.json`, `scenes/**` и C# script assemblies должны попадать в bundle рядом с executable.

Out of scope для этой задачи:

- `osx-x64`;
- universal `arm64+x64` bundles;
- notarization;
- App Store packaging;
- automatic signing with real credentials;
- deploy или GitHub Release publication.

## Package plan

Внутренний planner должен строить deterministic plan без запуска внешних процессов:

- путь к project file;
- publish arguments для `dotnet publish`;
- publish output directory;
- runtime identifier;
- architecture;
- configuration;
- self-contained flag;
- output directory;
- `.app` bundle path;
- `Contents`, `Contents/MacOS` и `Contents/Resources` directories;
- executable path внутри bundle;
- `Info.plist` path;
- bundle name, executable name и generated bundle identifier;
- renderer profile;
- graphics backend label;
- required bundle files;
- x64 policy;
- signing-required state и signing command arguments, если signing включён.

Planner не читает secrets, не запускает signing, не создаёт release artifacts на GitHub и не меняет файлы проекта.

## Codesigning policy

`Electron2DExportSigningSettings.Required == true` означает, что user-provided signing identity должна быть указана в preset. Planner должен включить deterministic `codesign` arguments, но не должен проверять Keychain, читать certificates, читать credentials или выполнять `codesign`.

`credentialReference` можно переносить в plan как opaque reference для будущего tooling. Diagnostic messages не должны раскрывать секреты.

Unsigned local Debug/Release bundle допустим для verifier, если signing не требуется preset-ом. User-provided signing и notarization будут выполняться отдельным release/export tooling после явной команды пользователя.

## Fail-closed validation

Planner должен возвращать diagnostics вместо исключений для ожидаемых ошибок:

- target не `MacOSArm64`;
- runtime identifier не `osx-arm64`;
- export не self-contained;
- runtime identifier указывает на `osx-x64`;
- пустой project file path;
- пустой output directory;
- отсутствующий project settings или display settings;
- неподдерживаемая configuration или renderer profile;
- signing required, но signing identity пустая.

## Local verifier

`dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64` должен проверять релизный архив для macOS arm64 без PowerShell как активной зависимости:

- создавать пакет библиотеки среды выполнения `Electron2D`;
- публиковать `Electron2D.Editor` и инструмент разработчика `e2d` под `osx-arm64`;
- создавать `electron2d-0.1-preview-osx-arm64.tar.gz`;
- писать SHA-256 файл рядом с архивом;
- включать `README.md`, `LICENSE`, `release-manifest.json`, `library/`, `editor/` и `tools/e2d/`;
- запрещать `.ps1`, рабочий журнал задач, дневники, доказательства внешнего аудита и репозиторные служебные каталоги внутри архива.

Verifier не публикует GitHub Release, не выполняет deploy и не использует реальные signing credentials.

## Критерии приёмки

- Package plan покрыт integration tests до implementation.
- Planner строит корректный Debug и Release `osx-arm64` app bundle plan.
- Planner fail-closed для unsupported target, `osx-x64`, framework-dependent deployment и missing signing identity.
- Local verifier документирует macOS arm64 publish/bundle/run и запускается только на macOS arm64 host.
- CI запускает verifier только на macOS runner.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний macOS export planner и macOS-only verifier для `osx-arm64` `.app` bundle. Внутренний planner означает механизм, доступный коду движка, будущим tools и тестам через assembly internals; это не публичный runtime API для игровых проектов.

## Target

- Export target: `MacOSArm64`.
- Runtime identifier: `osx-arm64`.
- Форма архива: релизный архив с пакетом библиотеки среды выполнения, `Electron2D.Editor`, `tools/e2d`, `README.md`, `LICENSE` и `release-manifest.json`.
- Проверка: `dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64` и `dotnet run --project eng/Electron2D.Build -- release verify`.

## Host requirements

Локальная команда `package --rid osx-arm64` не проверяет macOS arm64 как ОС-хост и может выполняться там, где доступен .NET SDK `10.0.x` и восстановлены зависимости репозитория. Если CI позже закрепит эту проверку за `macos-latest`, это будет отдельное ограничение сценария CI, а не правило команды сборки.

## SDK and toolchain

Команда сборки использует .NET SDK `10.0.x`, `dotnet pack` для пакета библиотеки среды выполнения и `dotnet publish` для `Electron2D.Editor` и `e2d` с runtime identifier `osx-arm64`. Экспорт macOS x64 намеренно находится вне текущего preview-объёма.

## Signing and credentials

The current planner can include deterministic `codesign` arguments when signing is required, but the verifier does not read secrets and does not require real signing credentials. Repository files may contain only a signing identity label or credential reference. Passwords, tokens, private keys, certificate contents, provisioning profile contents, and copied secret payloads must stay outside the repository.

## Known limitations

- arm64 is the only macOS runtime identifier verified in `0.1-preview`.
- Notarization, `.dmg` packaging, App Store packaging, real certificate lookup, auto-update metadata, and GitHub Release publication are outside this verifier.
- The verifier covers the empty project template, not final reference games.

## Planner

`Electron2DMacOSExportPlanner.CreatePlan(...)` принимает:

- export preset;
- путь к `.csproj` проекта;
- project settings.

Planner не запускает внешние процессы. Он строит deterministic package plan:

- `dotnet publish` arguments;
- `configuration`: `Debug` или `Release`;
- `runtimeIdentifier`: `osx-arm64`;
- `architecture`: `arm64`;
- `selfContained`: `true`;
- publish output directory;
- `.app` bundle path;
- `Contents`, `Contents/MacOS` и `Contents/Resources` directories;
- executable path inside `Contents/MacOS`;
- `Info.plist` path;
- bundle name, executable name и generated bundle identifier;
- renderer profile и internal graphics backend label `metal`;
- x64 policy: `unsupported-in-0.1-preview`;
- required bundle files;
- optional deterministic `codesign` arguments when signing is required.

## Validation

Planner fail-closed возвращает diagnostics и `Plan == null`, если:

- preset target не `MacOSArm64`;
- runtime identifier не `osx-arm64`;
- export не self-contained;
- runtime identifier указывает на x64;
- путь к project file пустой;
- project settings отсутствуют или не проходят validation;
- signing required, но signing identity пустая.

Stable diagnostic codes для macOS layer:

- `E2D-EXPORT-MACOS-0001` - target не `MacOSArm64`;
- `E2D-EXPORT-MACOS-0002` - runtime identifier не `osx-arm64`;
- `E2D-EXPORT-MACOS-0003` - export не self-contained;
- `E2D-EXPORT-MACOS-0004` - x64 runtime identifier вне scope;
- `E2D-EXPORT-MACOS-0005` - пустой project file path;
- `E2D-EXPORT-MACOS-0006` - отсутствуют project settings;
- `E2D-EXPORT-MACOS-0007` - project settings невалидны;
- `E2D-EXPORT-MACOS-0008` - signing required без signing identity.

## Codesigning

Planner только формирует `codesign` arguments из user-provided signing identity. Он не читает Keychain, certificates, private keys, passwords, environment variables или secret values.

`CredentialReference` переносится как opaque string для будущего tooling и не раскрывается в diagnostics. Verifier не использует реальные signing credentials, не выполняет deploy и не публикует GitHub Release.

## Local verification

```bash
dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64
dotnet run --project eng/Electron2D.Build -- release verify
```

Порядок сборки:

1. собирает локальный package `Electron2D`;
2. публикует `Electron2D.Editor` под `osx-arm64`;
3. публикует `e2d` под `osx-arm64`;
4. создаёт `release-manifest.json`;
5. создаёт `.tar.gz`-архив и SHA-256;
6. проверяет политику запрещённых файлов.

## CI

GitHub Actions запускает `dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64` только в `macos-latest` job. Windows и Linux jobs продолжают запускать свои platform-specific export checks и не пытаются публиковать `osx-arm64`.
