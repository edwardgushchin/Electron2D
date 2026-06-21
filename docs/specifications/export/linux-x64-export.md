# Linux x64 glibc export

Статус: целевая спецификация для `T-0089`.
Обновлено: 2026-06-21.

## Назначение

Linux x64 export должен превратить локальный Electron2D project в self-contained Linux package для glibc-based desktop systems. Первая реализация не закрывает musl, ARM Linux или mobile Linux: эти targets должны явно fail-closed.

## Target contract

Поддерживаемый target:

- `target`: `LinuxX64`;
- `runtimeIdentifier`: `linux-x64`;
- `libc`: `glibc`;
- `selfContained`: `true`;
- `configuration`: `Debug` или `Release`;
- `rendererProfile`: `Standard`, `Compatibility` или `Automatic`;
- desktop runtime: Wayland/X11 environment;
- output layout обязан сохранять `project.e2d.json`, `scenes/**` и executable entry point без `.exe` suffix.

Out of scope для этой задачи:

- `linux-musl-x64`;
- `linux-arm64`;
- `linux-musl-arm64`;
- any ARM Linux runtime identifier.

## Package plan

Внутренний planner должен строить deterministic plan без запуска внешних процессов:

- путь к project file;
- publish arguments для `dotnet publish`;
- runtime identifier;
- configuration;
- self-contained flag;
- output directory;
- executable path;
- renderer profile;
- graphics backend label;
- supported desktop display protocols: `wayland`, `x11`;
- libc family;
- excluded runtime identifiers;
- required runtime files.

Planner не читает secrets, не запускает signing, не создаёт release artifacts на GitHub и не меняет файлы проекта.

## Fail-closed validation

Planner должен возвращать diagnostics вместо исключений для ожидаемых ошибок:

- target не `LinuxX64`;
- runtime identifier не `linux-x64`;
- export не self-contained;
- runtime identifier указывает на musl или ARM;
- пустой project file path;
- пустой output directory;
- отсутствующий project settings или display settings;
- неподдерживаемая configuration или renderer profile.

## Local verifier

`tools/Verify-LinuxExport.ps1` должен:

- на Linux host запускать проверку напрямую;
- на Windows host запускать проверку через WSL, если WSL доступен;
- публиковать `Debug` и `Release` с `linux-x64` и `--self-contained true`;
- запускать exported executable;
- проверять output reference scene и C# script lifecycle.

Verifier не публикует GitHub Release, не подписывает artifacts и не выполняет deploy.

## Критерии приёмки

- Package plan покрыт integration tests до implementation.
- Planner строит корректный Debug и Release `linux-x64` glibc plan.
- Planner fail-closed для unsupported target, musl/ARM runtime identifiers и framework-dependent deployment.
- Local verifier запускает exported app на Linux host или через WSL.
- CI запускает verifier только на Linux runner.
