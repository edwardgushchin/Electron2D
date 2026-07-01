# Windows x64 export

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0088`, обновлена для reference game export contract.
Обновлено: 2026-06-23.

## Назначение

Windows x64 export должен превратить локальный Electron2D project в self-contained Windows package, который можно запустить без установленного .NET runtime. Пользователь запускает результат экспорта обычным project-specific executable, а не `dotnet run`, не `.csproj` и не debug build directory. Первая реализация не публикует GitHub Release, не подписывает пакет и не выполняет deploy: она строит проверяемый package plan, создаёт локальный package output и даёт verifier для publish/run.

## Target contract

Поддерживаемый target:

- `target`: `WindowsX64`;
- `runtimeIdentifier`: `win-x64`;
- `selfContained`: `true`;
- `configuration`: `Debug` или `Release`;
- `rendererProfile`: `Standard`, `Compatibility` или `Automatic`;
- display mode берётся из project settings: windowed или fullscreen;
- output layout обязан содержать project-specific executable entry point, resource pack manifest и несколько runtime resource packages;
- рядом с executable запрещены source-layout файлы и каталоги проекта: `project.e2d.json`, `export_presets.e2export.json`, `scenes/`, `assets/`, `resources/`, `.electron2d/tasks/**`;
- `project.e2d.json`, scenes, assets и resources должны попадать в пакеты `packs/**/*.e2dpkg`, а manifest `electron2d.pack.json` должен описывать, какие package files нужны проекту и отдельным сценам.

Pack layout должен быть расширяемым:

- `electron2d.pack.json` - manifest пакетов, читаемый player-бинарником;
- `packs/project.e2dpkg` - project settings и минимальные project-level metadata без editor/task workflow files;
- `packs/scenes/<scene-name>.e2dpkg` - отдельный пакет для каждой scene file, чтобы runtime мог загружать сцену без чтения всех сцен сразу;
- `packs/assets/<group>.e2dpkg` - package для top-level asset group, например `assets/platformer/**`;
- `packs/resources.e2dpkg` - package для импортируемых runtime resources.

Debug export должен сохранять debug symbols как часть package plan. Release export по умолчанию не требует debug symbols.

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
- window mode и window size/fullscreen state;
- required runtime files, которые должны присутствовать в published output;
- путь к resource pack manifest;
- список package files и package entries;
- forbidden loose files/directories, которые не должны появляться рядом с executable.

Planner не читает secrets, не запускает signing, не создаёт release artifacts на GitHub и не меняет файлы проекта.

## Fail-closed validation

Planner должен возвращать diagnostics вместо исключений для ожидаемых ошибок:

- target не `WindowsX64`;
- runtime identifier не `win-x64`;
- export не self-contained;
- пустой project file path;
- пустой output directory;
- отсутствующий project settings или display settings;
- неподдерживаемая configuration или renderer profile.

Diagnostics не должны содержать секреты или credential reference.

## Local verifier

`dotnet run --project eng/Electron2D.Build -- package --rid win-x64` должен проверять релизный архив для Windows без PowerShell как активной зависимости и фиксировать:

1. пакет библиотеки среды выполнения `Electron2D`;
2. выходные файлы `dotnet publish` для `Electron2D.Editor`;
3. выходные файлы `dotnet publish` для инструмента разработчика `e2d`;
4. root `README.md`, `LICENSE` и `release-manifest.json`;
5. Windows archive `electron2d-0.1.0-preview-win-x64.zip`;
6. SHA-256 файл рядом с архивом;
7. отсутствие `.ps1`, рабочего журнала задач, дневников, доказательств внешнего аудита и репозиторных служебных каталогов внутри архива.

Verifier не публикует GitHub Release, не подписывает пакет и не выполняет deploy.

## Критерии приёмки

- Package plan покрыт integration tests до implementation.
- Planner строит корректный Debug/windowed и Release/fullscreen plan.
- Planner fail-closed для неподдержанного target/runtime/self-contained contract.
- `e2d export build-windows --project <project-root> --output <dir>` создаёт project-specific `.exe`, `electron2d.pack.json`, несколько `packs/**/*.e2dpkg` и не оставляет loose project-source files в output folder.
- Exported reference game executable запускается как обычный бинарник и может в проверочном режиме принять `--play-script ... --screenshot <path>`.
- Local verifier документирует и проверяет реальный Windows publish/run.
- CI запускает verifier только на Windows runner.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний Windows export planner и локальный verifier для `win-x64` publish/run. Внутренний planner означает механизм, доступный коду движка, будущим tools и тестам через assembly internals; это не публичный runtime API для игровых проектов.

## Target

- Export target: `WindowsX64`.
- Runtime identifier: `win-x64`.
- Форма архива: релизный архив с пакетом библиотеки среды выполнения, `Electron2D.Editor`, `tools/e2d`, `README.md`, `LICENSE` и `release-manifest.json`.
- Проверка: `dotnet run --project eng/Electron2D.Build -- package --rid win-x64` и `dotnet run --project eng/Electron2D.Build -- release verify`.

## Host requirements

Локальная команда `package --rid win-x64` не проверяет ОС-хоста и может выполняться там, где доступен .NET SDK `10.0.x` и восстановлены зависимости репозитория. Если CI позже закрепит эту проверку за `windows-latest`, это будет отдельное ограничение сценария CI, а не правило команды сборки.

## SDK and toolchain

Команда сборки использует .NET SDK `10.0.x`, `dotnet pack` для пакета библиотеки среды выполнения и `dotnet publish` для `Electron2D.Editor` и `e2d` с runtime identifier `win-x64`. Ей не нужен внешний генератор установщика, инструмент магазина приложений или провайдер подписи.

## Signing and credentials

The current Windows verifier does not sign artifacts. If a future release preset requires signing, repository files may contain only a signing identity label or credential reference. Passwords, tokens, private keys, certificate contents, and copied secret payloads must stay outside the repository.

## Known limitations

- x64 is the only Windows runtime identifier verified in `0.1.0 Preview`.
- Installer generation, code signing, store packaging, auto-update metadata, and GitHub Release publication are outside this verifier.
- The verifier covers the empty project template, not final reference games.

## Planner

`WindowsExportPlanner.CreatePlan(...)` принимает:

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
- required output files: project-specific `.exe`, `electron2d.pack.json` and `packs/**/*.e2dpkg`.
- resource pack manifest: `electron2d.pack.json`;
- package files: `packs/project.e2dpkg`, per-scene `packs/scenes/*.e2dpkg`, asset group packages and `packs/resources.e2dpkg`;
- forbidden loose files next to executable: `project.e2d.json`, `export_presets.e2export.json`, `assets/`, `resources/`, `scenes/`, `.electron2d/tasks/**`.

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

```bash
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- release verify
```

Порядок сборки:

1. собирает локальный package `Electron2D`;
2. публикует `Electron2D.Editor` под `win-x64`;
3. публикует `e2d` под `win-x64`;
4. создаёт `release-manifest.json`;
5. создаёт `.zip`-архив и SHA-256;
6. проверяет политику запрещённых файлов.

Verifier не подписывает artifacts, не выполняет deploy и не публикует GitHub Release.

## CI

GitHub Actions запускает `dotnet run --project eng/Electron2D.Build -- package --rid win-x64` только в `windows-latest` job. Linux и macOS jobs продолжают запускать общий test runner и не пытаются публиковать `win-x64`.
