# Linux x64 glibc export

Текущая реализация добавляет внутренний Linux export planner и локальный verifier для `linux-x64` publish/run. Внутренний planner означает механизм, доступный коду движка, будущим tools и тестам через assembly internals; это не публичный runtime API для игровых проектов.

## Target

- Export target: `LinuxX64`.
- Runtime identifier: `linux-x64`.
- Package shape: self-contained desktop folder with `Electron2D.Empty`.
- Verification status: checked by `tools\Verify-LinuxExport.ps1` on Linux or WSL.

## Host requirements

Linux export verification must run on a Linux host or on Windows through WSL. CI runs it only in the `ubuntu-latest` job.

## SDK and toolchain

The verifier uses .NET SDK `10.0.x`, `dotnet restore`, and `dotnet publish` for `linux-x64`. The current target is glibc-based Linux. musl and ARM Linux runtime identifiers are intentionally rejected.

## Signing and credentials

The current Linux verifier does not sign artifacts. If a future release preset requires signing, repository files may contain only a signing identity label or credential reference. Passwords, tokens, private keys, certificate contents, and copied secret payloads must stay outside the repository.

## Known limitations

- `linux-x64` with glibc is the only Linux runtime identifier verified in `0.1.0 Preview`.
- AppImage, Flatpak, Snap, distro packages, code signing, auto-update metadata, and GitHub Release publication are outside this verifier.
- The verifier covers the empty project template, not final reference games.

## Planner

`Electron2DLinuxExportPlanner.CreatePlan(...)` принимает:

- export preset;
- путь к `.csproj` проекта;
- project settings.

Planner не запускает внешние процессы. Он строит deterministic package plan:

- `dotnet publish` arguments;
- `configuration`: `Debug` или `Release`;
- `runtimeIdentifier`: `linux-x64`;
- `libcFamily`: `glibc`;
- `selfContained`: `true`;
- output directory;
- путь к executable без `.exe` suffix;
- renderer profile и internal graphics backend label;
- desktop display protocols: `wayland`, `x11`;
- excluded runtime identifiers для musl/ARM;
- required files: `project.e2d.json` и main scene.

## Validation

Planner fail-closed возвращает diagnostics и `Plan == null`, если:

- preset target не `LinuxX64`;
- runtime identifier не `linux-x64`;
- export не self-contained;
- runtime identifier указывает на musl или ARM;
- путь к project file пустой;
- project settings отсутствуют или не проходят validation.

Stable diagnostic codes для Linux layer:

- `E2D-EXPORT-LINUX-0001` - target не `LinuxX64`;
- `E2D-EXPORT-LINUX-0002` - runtime identifier не `linux-x64`;
- `E2D-EXPORT-LINUX-0003` - export не self-contained;
- `E2D-EXPORT-LINUX-0004` - musl/ARM runtime identifier вне scope;
- `E2D-EXPORT-LINUX-0005` - пустой project file path;
- `E2D-EXPORT-LINUX-0006` - отсутствуют project settings;
- `E2D-EXPORT-LINUX-0007` - project settings невалидны.

## Local verification

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LinuxExport.ps1
```

Verifier работает на Linux host напрямую или на Windows host через WSL. Он:

1. восстанавливает и собирает локальный package `Electron2D`;
2. создаёт временный проект из `data/templates/electron2d-empty`;
3. восстанавливает зависимости из локального package source под `linux-x64`;
4. публикует `Debug` и `Release` под `linux-x64` с `--self-contained true`;
5. запускает exported Linux executable;
6. проверяет output reference scene и C# script lifecycle.

Verifier не подписывает artifacts, не выполняет deploy и не публикует GitHub Release.

## CI

GitHub Actions запускает `tools/Verify-LinuxExport.ps1` только в `ubuntu-latest` job. Windows и macOS jobs продолжают запускать общий test runner и не пытаются публиковать `linux-x64`.
