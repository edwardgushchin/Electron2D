# Local documentation pipeline

Обновлено: 2026-06-27.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0127` и проверок `T-0213`/`T-0214`.
Обновлено: 2026-06-27.

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
- C#-генераторы `eng\Electron2D.Build` для GitHub Wiki, API manifest и индекса локальной документации.

## Generated artifact

Canonical tracked artifact:

```text
data/documentation/electron2d-local-docs-index.json
```

Файл должен быть стабильным JSON: UTF-8 без BOM, переводы строк LF, детерминированный порядок свойств, отсортированные `entries`. Он пересоздаётся командой:

```powershell
dotnet run --project eng\Electron2D.Build -- update docs
```

Проверка синхронизации:

```powershell
dotnet run --project eng\Electron2D.Build -- update docs --check
```

`update docs --check` должен завершаться ошибкой, если отслеживаемый индекс отсутствует, отличается от ожидаемого результата C#-генерации, ссылается на отсутствующий исходный файл, не содержит обязательные `commands` или теряет обязательные метаданные `audiences`/`sources`.

## Schema shape

Local docs index должен содержать:

- `schemaVersion = 1`;
- `manifestVersion = 0.1.0-preview`;
- `generatedFrom` с путями и hash для API manifest, documentation files и examples file;
- `audiences` со значениями `human`, `ai`, `cli`, `ide`, `wiki`, `inspector`, `generator`;
- `commands` с entries для `docs search`, `docs type`, `docs member`, `docs example`;
- `sources` с категориями `apiManifest`, `documentation`, `examples`, `wiki`;
- `entries`, отсортированные по `id`.

`sources.wiki` должен быть объектом JSON с полями `generator` и `compatibilityPage`; строка, массив, `null` или другое значение считаются ошибкой схемы.

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

Внутренний C#-инструмент репозитория `eng/Electron2D.Build` должен иметь отдельную поверхность команд для пересоздаваемого индекса локальной документации:

```bash
dotnet run --project eng/Electron2D.Build -- verify docs
dotnet run --project eng/Electron2D.Build -- update docs --check
dotnet run --project eng/Electron2D.Build -- update docs
```

`update docs --check` строит ожидаемый индекс C#-логикой `eng\Electron2D.Build` и сравнивает его с `data/documentation/electron2d-local-docs-index.json`. `update docs` пересоздаёт этот файл тем же генератором. `verify docs` использует этот же C#-генератор, сверяет отслеживаемый индекс с ожидаемым результатом и затем валидирует JSON-схему, `generatedFrom`, `commands`, `sources`, `audiences`, ссылки `apiId` на API manifest, хеши исходных документов и примеров, обязательные записи и метаданные `sources.wiki.generator = "eng/Electron2D.Build update wiki"`.

Старые локальные скрипты автоматизации не являются целевым интерфейсом для `verify docs`, `update docs --check` или `update docs`. Они могут упоминаться только как историческое состояние до переноса, но текущий контракт задаёт C#-инструмент `eng\Electron2D.Build` и его структурированные JSON-диагностики. Проверка не должна делать необработанный поиск через `grep` по всему репозиторию: исторические и миграционные документы допускаются как источники индекса, а `verify docs` проверяет контракт пересоздаваемого индекса и существование перечисленных источников через структурированные правила.

CI должен запускать local documentation verifier после API manifest/Wiki checks или рядом с документационными verifier-ами. Если API manifest, local docs index, examples source или CLI output рассинхронизированы, CI должен падать.

## Критерии приёмки

- `e2d docs search/type/member/example` работает локально в text и JSON modes.
- Wiki, XML documentation и JSON API manifest остаются согласованными источниками: local docs index не дублирует public API вручную, а ссылается на manifest stable ids.
- Local docs index содержит audience/source metadata для AI, CLI, IDE, Wiki, Inspector и generators.
- Documentation pipeline описывает Editor co-development workflow, headless CI workflow, `ProjectWorkspace`, `ProjectTaskManager`, `TaskActivity`, MCP/IPC, Agent Workspace panel, external change synchronizer, conflict panel, grouped Undo/Redo и visible runtime control простыми русскими формулировками.
- Документация объясняет, что пользовательские проекты используют `ProjectTaskManager`, а не локальные рабочие файлы репозитория `TASKS.md`, `completed-tasks/` или `dev-diary/`.
- Documentation pipeline содержит проверяемые команды `eng\Electron2D.Build` для локального индекса, API manifest и GitHub Wiki.
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
dotnet run --project eng\Electron2D.Build -- update docs
```

Проверить синхронизацию index:

```powershell
dotnet run --project eng\Electron2D.Build -- update docs --check
```

Проверить весь локальный документационный контур и обязательные метаданные через C#-инструмент:

```powershell
dotnet run --project eng\Electron2D.Build -- verify docs
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

`eng\Electron2D.Build update wiki` остаётся источником generated Wiki pages. Local docs index ссылается на Wiki/API manifest pipeline, но не хранит Wiki pages внутри основного репозитория.

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

`MCP resources` — ресурсы локального MCP-сервера, через которые AI-клиент читает состояние открытого проекта, документацию, диагностику и artifacts. До реализации MCP adapter локальная документация фиксирует контракт в `agent-native-workflow.md`, а `verify docs` проверяет, что этот раздел остаётся в индексируемых документах.

Headless CI workflow описывается отдельно: если Editor закрыт, CLI/MCP создают headless `ProjectWorkspace`, выполняют build/test/run/export через snapshot и возвращают структурированные результаты без владения GUI-сессией.

Пользовательские проекты не должны использовать локальные workflow-файлы репозитория движка. `TASKS.md`, `completed-tasks/` и `dev-diary/` нужны только агентам, которые разрабатывают сам Electron2D. Игра или приложение на Electron2D хранит своё состояние задач через `ProjectTaskManager` и связанные проектные файлы.

## Что проверяет verifier

`dotnet run --project eng\Electron2D.Build -- verify docs` выполняет:

- C#-генерацию ожидаемого `data/documentation/electron2d-local-docs-index.json` по текущим `data/api`, `data/documentation` и `docs`;
- проверку, что отслеживаемый `data/documentation/electron2d-local-docs-index.json` существует, парсится как JSON и совпадает с ожидаемым результатом генерации;
- проверку `schemaVersion`, `manifestVersion`, `generatedFrom`, `audiences`, `commands`, `sources` и `entries`;
- проверку, что `sources.wiki` является объектом JSON и содержит `generator = "eng/Electron2D.Build update wiki"`;
- проверку обязательных команд `docs search`, `docs type`, `docs member`, `docs example` и форматов `text`/`json`;
- проверку, что `api-type` и `api-member` entries ссылаются на существующие stable identifiers из `data/api/electron2d-api-manifest.json`;
- проверку, что исходные пути из индекса существуют в репозитории;
- проверку, что хеши документов и `data/documentation/electron2d-doc-examples.json` актуальны.

Если C#-генерация, сверка или валидация индекса завершается ошибкой, команда возвращает ненулевой код и печатает структурированные JSON-диагностики на стандартный вывод.

CI запускает verifier как отдельный documentation gate. Если API manifest, Markdown documentation, examples source, generated index или CLI output расходятся, gate падает.
