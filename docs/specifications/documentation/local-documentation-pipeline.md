# Local documentation pipeline

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
- `docs/documentation/README.md` и Markdown-файлы под `docs/documentation/` - текущая человекочитаемая implementation documentation;
- `docs/specifications/architecture/agent-native-workflow.md` - architecture contract для Editor co-development и headless workflow;
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
