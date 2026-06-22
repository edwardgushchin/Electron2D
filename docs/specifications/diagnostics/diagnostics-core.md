# Diagnostics.Core

Статус: целевая спецификация.
Задача: `T-0146`.
Обновлено: 2026-06-22.
Связанные документы: [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md); [Runtime diagnostics пользовательского кода](../object-model/runtime-diagnostics.md).

## Цель

`Diagnostics.Core` задаёт единый внутренний формат ошибок, предупреждений, информационных сообщений и подсказок до появления `ProjectWorkspace`, Tooling, CLI, MCP и Editor adapters.

Этот слой не является публичным runtime API. Он нужен как стабильный контракт между внутренними подсистемами: import, build, validation, scripting, export, future Tooling и Editor output. Внешние представления, включая SARIF 2.1.0, JSONL-streams, MCP resources и визуальные панели редактора, строятся отдельными adapter-задачами поверх этой модели.

## Обязательная модель диагностики

Каждая structured diagnostic запись должна хранить:

- `code` - стабильный идентификатор из registry;
- `severity` - `Error`, `Warning`, `Info` или `Hint`;
- `category` - домен источника сообщения: `Project`, `Scene`, `Resource`, `Import`, `Build`, `Runtime`, `Script`, `Export`, `Tooling` или `Diagnostics`;
- `message` - человекочитаемое объяснение без секретов;
- `location` - необязательный primary location с `file`, `line`, `column`, scene UID, node path и resource UID;
- `relatedLocations` - дополнительные места, связанные с причиной сообщения;
- `suggestedFixes` - безопасные структурированные действия;
- `documentationUri` - ссылка на локальную документацию или стабильный fragment документации версии.

`line` и `column` являются 1-based значениями. Если источник не знает позицию, он оставляет поле пустым, а не записывает `0`.

## Stable error code registry

Registry должен быть immutable во время работы процесса и обязан:

- хранить уникальные коды в формате `E2D-<DOMAIN>-NNNN`;
- возвращать definition по коду и полный отсортированный список definitions;
- фиксировать canonical severity, category, title и documentation URI для каждого кода;
- запрещать diagnostics с unknown code, если запись создаётся через core factory;
- быть пригодным для будущих exports в документацию, CLI и SARIF adapters.

Минимальный initial registry для `T-0146`:

| Code | Severity | Category | Назначение |
| --- | --- | --- | --- |
| `E2D-DIAG-0001` | `Error` | `Diagnostics` | сама diagnostic запись неполная или противоречит registry |
| `E2D-PROJECT-0001` | `Error` | `Project` | project document malformed или не может быть parsed |
| `E2D-PROJECT-0002` | `Error` | `Project` | project document version новее поддерживаемой |
| `E2D-PROJECT-0003` | `Warning` | `Project` | project document содержит безопасно применимый suggested fix |
| `E2D-TOOLING-0001` | `Info` | `Tooling` | job cancellation state changed или cancel request был отклонён без изменения job state |

## Suggested fixes

Suggested fix — это предложение, которое можно показать человеку или передать future Tooling-команде. Оно не выполняется автоматически самим `Diagnostics.Core`.

Разрешённые action kinds:

- `ReplaceText` - замена известного диапазона текста в project-relative файле;
- `CreateFile` - создание project-relative файла, если он ещё не существует;
- `UpdateJsonProperty` - замена значения по JSON Pointer в project-relative JSON-файле;
- `DeleteJsonProperty` - удаление значения по JSON Pointer в project-relative JSON-файле.

Каждый action должен содержать project-relative `path`. Absolute paths, `..`, generated/cache paths, shell commands, process arguments, network URI и secret values запрещены. Для text replacement обязательны start/end line/column и `newText`; optional `expectedText` нужен будущим tooling-командам для fail-closed применения.

## Serialization

Core JSON serialization должна быть deterministic:

- поля diagnostic object пишутся в стабильном порядке;
- enum значения пишутся строками;
- collections сортируются там, где порядок не задаёт пользовательский смысл;
- line endings в serialized JSON нормализуются в LF;
- round-trip сохраняет code, severity, category, message, location, related locations, suggested fixes и documentation URI.

Serialization не обязана быть финальным CLI schema. CLI JSON, SARIF и live stream adapters могут иметь собственные wrapper-поля, но обязаны сохранять core payload без потери смысла.

## Acceptance criteria

- Есть immutable registry со стабильными уникальными кодами, canonical severity/category и documentation URI.
- Core factory не создаёт diagnostic с unknown code, пустым message или severity/category, противоречащими registry.
- Diagnostic location поддерживает file/line/column, scene UID, node path, resource UID и related locations.
- Suggested fixes представлены только безопасными structured actions; absolute paths, `..`, generated/cache paths и command-like payload запрещены.
- JSON serialization deterministic и round-trip сохраняет все обязательные поля.
- Focused tests покрывают required fields, registry stability, suggested fix validation и serialization.
- Implementation documentation описывает фактическое поведение и команду проверки.
