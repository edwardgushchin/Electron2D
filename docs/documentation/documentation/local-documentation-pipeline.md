# Local documentation pipeline

Текущая реализация поставляет локальную документацию через generated index и CLI-группу `e2d docs`. Generated index — это пересоздаваемый файл, который собирает поисковые записи и ссылки на исходные документы. Он не заменяет API manifest и не становится вторым списком public API.

## Артефакты

Canonical local docs index:

```text
data/documentation/electron2d-local-docs-index.json
```

Источник коротких локальных примеров:

```text
data/documentation/electron2d-doc-examples.json
```

CLI project:

```text
src/Electron2D.Cli/Electron2D.Cli.csproj
```

Executable assembly name:

```text
e2d
```

Index создаётся из `data/api/electron2d-api-manifest.json`, implementation documentation under `docs/documentation/`, architecture spec `docs/specifications/architecture/agent-native-workflow.md` и examples source. Public API entries в index хранят stable `apiId`, но полный type/member payload CLI читает из API manifest.

## Команды

Пересоздать local docs index:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-LocalDocumentationIndex.ps1
```

Проверить синхронизацию index:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-LocalDocumentationIndex.ps1 -Check
```

Проверить весь локальный документационный контур:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```

Локальные команды CLI:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- docs search "move and slide"
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- docs type CharacterBody2D --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- docs member CharacterBody2D.MoveAndSlide --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- docs example "platformer movement" --format json
```

Все четыре команды поддерживают `--format text|json`. JSON mode предназначен для AI-агентов, IDE, Inspector и генераторов; text mode — для человека в терминале.

## Источники правды

`data/api/electron2d-api-manifest.json` остаётся источником public API metadata. Он уже создаётся из compiled assembly, XML documentation и GitHub Wiki compatibility table.

`tools\Update-ApiWiki.ps1` остаётся источником generated Wiki pages. Local docs index ссылается на Wiki/API manifest pipeline, но не хранит Wiki pages внутри основного репозитория.

`data/documentation/electron2d-doc-examples.json` хранит только короткие примеры для локального поиска. Если пример ссылается на API, он указывает stable `electron2d://api/...` identifiers.

## Editor co-development workflow

Документация Agent-native cross-platform 2D game engine workflow доступна через index как обычный documentation entry. Она простыми словами описывает:

- `ProjectWorkspace` — внутреннюю живую модель открытого проекта, где редактор, CLI, MCP и будущие IDE-интеграции видят одни и те же документы, ревизии и диагностику;
- `ProjectTaskManager` — проектную систему задач пользователя внутри Electron2D, а не локальный task tracker этого репозитория;
- `TaskActivity` — журнал действий по пользовательской задаче внутри проекта;
- `MCP/IPC` — локальное межпроцессное соединение, через которое агент обращается к открытому редактору без эмуляции кликов;
- `Agent Workspace panel` — будущую панель редактора с текущей задачей, транзакциями, jobs, diagnostics и artifacts;
- `external change synchronizer` — механизм, который замечает изменения файлов на диске и объединяет их с открытым состоянием;
- `conflict panel` — экран разбора конфликтов, когда человек и AI меняют одно и то же место;
- `grouped Undo/Redo` — правило, что AI-транзакция попадает в историю отмены одной группой;
- `visible runtime control` — запуск, пауза, step frame, input injection и screenshot в наблюдаемом пользователем runtime.

`Editor Capability Manifest` — будущий машиночитаемый список семантически значимых возможностей редактора и их Tooling/MCP/CLI bindings. В текущем pipeline он доступен человеку и AI-агенту через indexed architecture documentation; будущие generated manifests должны добавлять отдельные `documentation` или `example` entries с тем же source metadata.

`MCP resources` — ресурсы локального MCP-сервера, через которые AI-клиент читает состояние открытого проекта, документацию, диагностику и artifacts. До реализации MCP adapter локальная документация фиксирует контракт в `agent-native-workflow.md`, а `Verify-LocalDocumentation.ps1` проверяет, что этот раздел не выпал из documentation pipeline.

Headless CI workflow описывается отдельно: если Editor закрыт, CLI/MCP создают headless `ProjectWorkspace`, выполняют build/test/run/export через snapshot и возвращают структурированные результаты без владения GUI-сессией.

Пользовательские проекты не должны использовать локальные workflow-файлы репозитория движка. `TASKS.md`, `completed-tasks/` и `dev-diary/` нужны только агентам, которые разрабатывают сам Electron2D. Игра или приложение на Electron2D хранит своё состояние задач через `ProjectTaskManager` и связанные проектные файлы.

## Что проверяет verifier

`tools\Verify-LocalDocumentation.ps1` выполняет:

- `tools\Update-LocalDocumentationIndex.ps1 -Check`;
- проверку, что `Electron2D.Cli` входит в `src/Electron2D.sln`;
- сборку `src\Electron2D.Cli\Electron2D.Cli.csproj`;
- JSON-запросы `docs search`, `docs type`, `docs member`, `docs example`;
- проверку, что `CharacterBody2D` и `MoveAndSlide` приходят из API manifest;
- проверку, что example `platformer movement` приходит из examples source;
- проверку этой страницы на обязательные объяснения Editor co-development и headless workflow.

CI запускает verifier как отдельный documentation gate. Если API manifest, Markdown documentation, examples source, generated index или CLI output расходятся, gate падает.
