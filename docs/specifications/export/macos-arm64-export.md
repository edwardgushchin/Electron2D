# macOS arm64 export

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

`tools/Verify-MacOSExport.ps1` должен:

- запускаться только на macOS host;
- требовать `arm64` host для полного publish/bundle/run;
- публиковать `Debug` и `Release` с `osx-arm64` и `--self-contained true`;
- создавать `.app` bundle с `Info.plist`;
- запускать `Contents/MacOS/<ExecutableName>`;
- проверять output reference scene и C# script lifecycle.

Verifier не публикует GitHub Release, не выполняет deploy и не использует реальные signing credentials.

## Критерии приёмки

- Package plan покрыт integration tests до implementation.
- Planner строит корректный Debug и Release `osx-arm64` app bundle plan.
- Planner fail-closed для unsupported target, `osx-x64`, framework-dependent deployment и missing signing identity.
- Local verifier документирует macOS arm64 publish/bundle/run и запускается только на macOS arm64 host.
- CI запускает verifier только на macOS runner.
