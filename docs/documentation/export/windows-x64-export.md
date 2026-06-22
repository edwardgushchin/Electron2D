# Windows x64 export

Текущая реализация добавляет внутренний Windows export planner и локальный verifier для `win-x64` publish/run. Внутренний planner означает механизм, доступный коду движка, будущим tools и тестам через assembly internals; это не публичный runtime API для игровых проектов.

## Target

- Export target: `WindowsX64`.
- Runtime identifier: `win-x64`.
- Package shape: self-contained desktop folder with `Electron2D.Empty.exe`.
- Verification status: checked by `tools\Verify-WindowsExport.ps1` on Windows.

## Host requirements

Windows export verification must run on a Windows host. CI runs it only in the `windows-latest` job.

## SDK and toolchain

The verifier uses .NET SDK `10.0.x`, `dotnet restore`, and `dotnet publish` for `win-x64`. It does not require an external installer builder, store packaging tool, or signing provider.

## Signing and credentials

The current Windows verifier does not sign artifacts. If a future release preset requires signing, repository files may contain only a signing identity label or credential reference. Passwords, tokens, private keys, certificate contents, and copied secret payloads must stay outside the repository.

## Known limitations

- x64 is the only Windows runtime identifier verified in `0.1.0 Preview`.
- Installer generation, code signing, store packaging, auto-update metadata, and GitHub Release publication are outside this verifier.
- The verifier covers the empty project template, not final reference games.

## Planner

`Electron2DWindowsExportPlanner.CreatePlan(...)` принимает:

- export preset;
- путь к `.csproj` проекта;
- project settings.

Planner не запускает внешние процессы. Он строит deterministic package plan:

- `dotnet publish` arguments;
- `configuration`: `Debug` или `Release`;
- `runtimeIdentifier`: `win-x64`;
- `selfContained`: `true`;
- output directory;
- путь к expected `.exe`;
- renderer profile и internal graphics backend label;
- window mode: `Windowed` или `Fullscreen`;
- required files: `project.e2d.json` и main scene.

## Validation

Planner fail-closed возвращает diagnostics и `Plan == null`, если:

- preset target не `WindowsX64`;
- runtime identifier не `win-x64`;
- export не self-contained;
- путь к project file пустой;
- project settings отсутствуют или не проходят validation.

Stable diagnostic codes для Windows layer:

- `E2D-EXPORT-WINDOWS-0001` - target не `WindowsX64`;
- `E2D-EXPORT-WINDOWS-0002` - runtime identifier не `win-x64`;
- `E2D-EXPORT-WINDOWS-0003` - export не self-contained;
- `E2D-EXPORT-WINDOWS-0004` - пустой project file path;
- `E2D-EXPORT-WINDOWS-0006` - отсутствуют project settings;
- `E2D-EXPORT-WINDOWS-0007` - project settings невалидны.

## Local verification

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-WindowsExport.ps1
```

Verifier работает только на Windows host. Он:

1. собирает локальный package `Electron2D`;
2. создаёт временный проект из `data/templates/electron2d-empty`;
3. восстанавливает зависимости из локального package source;
4. публикует `Debug` и `Release` под `win-x64` с `--self-contained true`;
5. запускает exported `.exe`;
6. проверяет output reference scene и C# script lifecycle.

Verifier не подписывает artifacts, не выполняет deploy и не публикует GitHub Release.

## CI

GitHub Actions запускает `tools/Verify-WindowsExport.ps1` только в `windows-latest` job. Linux и macOS jobs продолжают запускать общий test runner и не пытаются публиковать `win-x64`.
