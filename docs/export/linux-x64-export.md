# Linux x64 glibc export

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

`dotnet run --project eng\Electron2D.Build -- package --rid linux-x64` должен проверять релизный архив для Linux без PowerShell как активной зависимости:

- создавать пакет библиотеки среды выполнения `Electron2D`;
- публиковать `Electron2D.Editor` и инструмент разработчика `e2d` под `linux-x64`;
- создавать `electron2d-0.1.0-preview-linux-x64.tar.gz`;
- писать SHA-256 файл рядом с архивом;
- включать `README.md`, `LICENSE`, `release-manifest.json`, `library/`, `editor/` и `tools/e2d/`;
- запрещать `.ps1`, рабочий журнал задач, дневники, доказательства внешнего аудита и репозиторные служебные каталоги внутри архива.

Verifier не публикует GitHub Release, не подписывает artifacts и не выполняет deploy.

## Критерии приёмки

- Package plan покрыт integration tests до implementation.
- Planner строит корректный Debug и Release `linux-x64` glibc plan.
- Planner fail-closed для unsupported target, musl/ARM runtime identifiers и framework-dependent deployment.
- Local verifier запускает exported app на Linux host или через WSL.
- CI запускает verifier только на Linux runner.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний Linux export planner и локальный verifier для `linux-x64` publish/run. Внутренний planner означает механизм, доступный коду движка, будущим tools и тестам через assembly internals; это не публичный runtime API для игровых проектов.

## Target

- Export target: `LinuxX64`.
- Runtime identifier: `linux-x64`.
- Форма архива: релизный архив с пакетом библиотеки среды выполнения, `Electron2D.Editor`, `tools/e2d`, `README.md`, `LICENSE` и `release-manifest.json`.
- Проверка: `dotnet run --project eng\Electron2D.Build -- package --rid linux-x64` и `dotnet run --project eng\Electron2D.Build -- release verify`.

## Host requirements

Локальная команда `package --rid linux-x64` не требует Linux-хоста или WSL и может выполняться там, где доступен .NET SDK `10.0.x` и восстановлены зависимости репозитория. Если CI позже закрепит эту проверку за `ubuntu-latest`, это будет отдельное ограничение сценария CI, а не правило команды сборки.

## SDK and toolchain

Команда сборки использует .NET SDK `10.0.x`, `dotnet pack` для пакета библиотеки среды выполнения и `dotnet publish` для `Electron2D.Editor` и `e2d` с runtime identifier `linux-x64`. Текущая целевая система - Linux на glibc. Runtime identifiers для musl и ARM Linux намеренно отклоняются.

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

```bash
dotnet run --project eng/Electron2D.Build -- package --rid linux-x64
dotnet run --project eng/Electron2D.Build -- release verify
```

Порядок сборки:

1. собирает локальный package `Electron2D`;
2. публикует `Electron2D.Editor` под `linux-x64`;
3. публикует `e2d` под `linux-x64`;
4. создаёт `release-manifest.json`;
5. создаёт `.tar.gz`-архив и SHA-256;
6. проверяет политику запрещённых файлов.

Verifier не подписывает artifacts, не выполняет deploy и не публикует GitHub Release.

## CI

GitHub Actions запускает `tools/Verify-LinuxExport.ps1` только в `ubuntu-latest` job. Windows и macOS jobs продолжают запускать общий test runner и не пытаются публиковать `linux-x64`.
