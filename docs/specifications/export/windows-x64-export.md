# Windows x64 export

Статус: целевая спецификация для `T-0088`.
Обновлено: 2026-06-21.

## Назначение

Windows x64 export должен превратить локальный Electron2D project в self-contained Windows package, который можно запустить без установленного .NET runtime. Первая реализация не публикует GitHub Release, не подписывает пакет и не выполняет deploy: она строит проверяемый package plan и локальный verifier для publish/run.

## Target contract

Поддерживаемый target:

- `target`: `WindowsX64`;
- `runtimeIdentifier`: `win-x64`;
- `selfContained`: `true`;
- `configuration`: `Debug` или `Release`;
- `rendererProfile`: `Standard`, `Compatibility` или `Automatic`;
- display mode берётся из project settings: windowed или fullscreen;
- output layout обязан сохранять `project.e2d.json`, `scenes/**` и executable entry point.

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
- required runtime files, которые должны присутствовать в published output.

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

`tools/Verify-WindowsExport.ps1` должен выполняться только на Windows host и проверять:

1. локальную упаковку `Electron2D`;
2. создание временного проекта из `templates/electron2d-empty`;
3. restore из локального package source;
4. `dotnet publish` для Debug и Release с `win-x64` и `--self-contained true`;
5. запуск exported executable;
6. наличие expected output для reference scene и C# script lifecycle.

Verifier не публикует GitHub Release, не подписывает пакет и не выполняет deploy.

## Критерии приёмки

- Package plan покрыт integration tests до implementation.
- Planner строит корректный Debug/windowed и Release/fullscreen plan.
- Planner fail-closed для неподдержанного target/runtime/self-contained contract.
- Local verifier документирует и проверяет реальный Windows publish/run.
- CI запускает verifier только на Windows runner.
