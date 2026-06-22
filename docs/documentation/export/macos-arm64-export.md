# macOS arm64 export

Текущая реализация добавляет внутренний macOS export planner и macOS-only verifier для `osx-arm64` `.app` bundle. Внутренний planner означает механизм, доступный коду движка, будущим tools и тестам через assembly internals; это не публичный runtime API для игровых проектов.

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
- x64 policy: `unsupported-in-0.1.0-preview`;
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

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-MacOSExport.ps1
```

Verifier работает только на macOS arm64 host. Он:

1. восстанавливает и собирает локальный package `Electron2D`;
2. создаёт временный проект из `data/templates/electron2d-empty`;
3. восстанавливает зависимости из локального package source под `osx-arm64`;
4. публикует `Debug` и `Release` под `osx-arm64` с `--self-contained true`;
5. создаёт `.app` bundle с `Contents/Info.plist`;
6. копирует publish output в `Contents/MacOS`;
7. запускает `Contents/MacOS/Electron2D.Empty`;
8. проверяет output reference scene и C# script lifecycle.

## CI

GitHub Actions запускает `tools/Verify-MacOSExport.ps1` только в `macos-latest` job. Windows и Linux jobs продолжают запускать свои platform-specific export verifiers и не пытаются публиковать `osx-arm64`.
