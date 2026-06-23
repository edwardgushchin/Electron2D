# Local documentation pipeline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0127`.
Обновлено: 2026-06-22.

## Назначение

Electron2D `0.1.0 Preview` должен поставлять локальную документацию вместе с версией движка, чтобы человек, CLI, IDE, AI-агент, Inspector, генераторы и GitHub Wiki verifier-ы ссылались на один согласованный контракт.

В этой спецификации local documentation pipeline означает проверяемую цепочку:

```text
XML documentation + GitHub Wiki compatibility
          ↓
data/api/electron2d-api-manifest.json
          ↓
data/documentation/electron2d-local-docs-index.json
          ↓
e2d docs search/type/member/example
```

Local docs index не является новым владельцем публичного API. Он хранит только поисковый индекс, source metadata и ссылки на stable identifiers из API manifest. Полное описание типов и members CLI должен брать из `data/api/electron2d-api-manifest.json`.

## Источники

Проверяемые источники:

- `data/api/electron2d-api-manifest.json` - canonical machine-readable описание public API, созданное из compiled assembly, XML documentation и GitHub Wiki compatibility table;
- `docs/README.md` и Markdown-файлы под `docs/` - текущая человекочитаемая implementation documentation;
- `docs/architecture/agent-native-workflow.md` - architecture contract для Editor co-development и headless workflow;
- `data/documentation/electron2d-doc-examples.json` - короткие локальные примеры, которые можно возвращать из CLI без чтения исходников runtime;
- GitHub Wiki generator и API manifest generator - существующие генераторы для Wiki/HTML-представления, XML-derived справки и JSON API manifest.

## Generated artifact

Canonical tracked artifact:

```text
data/documentation/electron2d-local-docs-index.json
```

Файл должен быть stable JSON: UTF-8 без BOM, LF line endings, deterministic property order, отсортированные entries. Он пересоздаётся командой:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-LocalDocumentationIndex.ps1
```

Проверка синхронизации:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Update-LocalDocumentationIndex.ps1 -Check
```

`-Check` должен падать, если tracked index отсутствует, отличается от expected output, ссылается на отсутствующий source file, не содержит обязательных commands или теряет обязательные audience/source metadata.

## Schema shape

Local docs index должен содержать:

- `schemaVersion = 1`;
- `manifestVersion = 0.1.0-preview`;
- `generatedFrom` с путями и hash для API manifest, documentation files и examples file;
- `audiences` со значениями `human`, `ai`, `cli`, `ide`, `wiki`, `inspector`, `generator`;
- `commands` с entries для `docs search`, `docs type`, `docs member`, `docs example`;
- `sources` с категориями `apiManifest`, `documentation`, `examples`, `wiki`;
- `entries`, отсортированные по `id`.

Entry должен содержать:

- stable `id`;
- `kind`: `api-type`, `api-member`, `documentation`, `example`;
- `title`;
- `summary`;
- `keywords`;
- `sourcePath`;
- `sourceId` или `apiId`;
- `audiences`.

Для `api-type` и `api-member` entry `apiId` должен ссылаться на stable identifier из API manifest. Для `example` entry источник должен быть `data/documentation/electron2d-doc-examples.json`.

## CLI contract

CLI project:

```text
src/Electron2D.Cli/Electron2D.Cli.csproj
```

Executable name:

```text
e2d
```

Минимальные команды `T-0127`:

```bash
e2d docs search "move and slide"
e2d docs type CharacterBody2D --format json
e2d docs member CharacterBody2D.MoveAndSlide --format json
e2d docs example "platformer movement" --format json
```

Общие правила:

- `--help` должен показывать группу `docs` и подкоманды;
- `--format text|json` поддерживается всеми четырьмя docs commands;
- при неизвестном type/member/example команда завершается с ненулевым exit code и понятным сообщением без stack trace;
- `docs search` ищет по local docs index и возвращает stable ids, kind, title, summary и source path;
- `docs type` возвращает API manifest type entry целиком или краткое text-представление;
- `docs member` принимает `Type.Member` или full type/member name и возвращает matching manifest member;
- `docs example` возвращает локальный пример с title, summary, code, source path и связанными API ids.

CLI должен fail-closed: если local docs index или API manifest отсутствуют, устарели или не парсятся как JSON, команда должна завершиться с ошибкой и указать verifier command.

## CI и verifier

`tools\Verify-LocalDocumentation.ps1` должен проверять:

- `Update-LocalDocumentationIndex.ps1 -Check`;
- наличие `Electron2D.Cli` в solution;
- успешное выполнение `e2d docs search/type/member/example` в JSON-режиме;
- что `docs type CharacterBody2D` и `docs member CharacterBody2D.MoveAndSlide` читают API manifest, а не отдельный ручной список;
- что `docs example "platformer movement"` возвращает локальный пример с source metadata;
- что documentation pipeline docs перечисляют локальные commands и verifier.

CI должен запускать local documentation verifier после API manifest/Wiki checks или рядом с документационными verifier-ами. Если API manifest, local docs index, examples source или CLI output рассинхронизированы, CI должен падать.

## Критерии приёмки

- `e2d docs search/type/member/example` работает локально в text и JSON modes.
- Wiki, XML documentation и JSON API manifest остаются согласованными источниками: local docs index не дублирует public API вручную, а ссылается на manifest stable ids.
- Local docs index содержит audience/source metadata для AI, CLI, IDE, Wiki, Inspector и generators.
- Documentation pipeline описывает Editor co-development workflow, headless CI workflow, `ProjectWorkspace`, `ProjectTaskManager`, `TaskActivity`, MCP/IPC, Agent Workspace panel, external change synchronizer, conflict panel, grouped Undo/Redo и visible runtime control простыми русскими формулировками.
- Документация объясняет, что пользовательские проекты используют `ProjectTaskManager`, а не локальные рабочие файлы репозитория `TASKS.md`, `completed-tasks/` или `dev-diary/`.
- Documentation pipeline содержит проверяемые команды для `Update-LocalDocumentationIndex.ps1`, `Verify-LocalDocumentation.ps1`, API manifest и GitHub Wiki verifier-ов.
- CI проверяет синхронизацию local documentation pipeline.

## Фактическое состояние, ограничения и проверки

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

Index создаётся из `data/api/electron2d-api-manifest.json`, implementation documentation under `docs/`, architecture spec `docs/architecture/agent-native-workflow.md` и examples source. Public API entries в index хранят stable `apiId`, но полный type/member payload CLI читает из API manifest.

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

`Editor Capability Manifest` — текущий машиночитаемый список семантически значимых возможностей редактора и их Tooling/MCP/CLI bindings. Canonical artifact хранится в `data/editor/electron2d-editor-capabilities.json`, а implementation documentation индексируется как обычная страница Tooling. MCP resource `electron2d://editor/capabilities` возвращает тот же manifest для AI-клиентов.

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
