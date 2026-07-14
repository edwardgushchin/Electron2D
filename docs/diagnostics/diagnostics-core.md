# Diagnostics.Core

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0146`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Runtime diagnostics пользовательского кода](../object-model/runtime-diagnostics.md).

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
| `E2D-CLI-0001` | `Error` | `Tooling` | command group или subcommand не реализован в текущем Preview scope |
| `E2D-CLI-0002` | `Error` | `Tooling` | CLI arguments неполные или некорректные |
| `E2D-CLI-0003` | `Error` | `Tooling` | route selection или project root не позволяют безопасно выполнить команду |
| `E2D-CAPABILITY-0001` | `Error` | `Tooling` | Editor capability parity нарушен между Editor, Tooling и MCP |
| `E2D-CAPABILITY-0002` | `Error` | `Tooling` | CLI binding policy для capability некорректна |
| `E2D-CAPABILITY-0003` | `Error` | `Tooling` | Editor capability manifest имеет некорректную форму, ссылку или покрытие категорий |
| `E2D-CAPABILITY-0004` | `Error` | `Tooling` | capability ссылается на неопубликованный Tooling command или MCP tool/resource |
| `E2D-DIAG-0001` | `Error` | `Diagnostics` | сама diagnostic запись неполная или противоречит registry |
| `E2D-MCP-0001` | `Error` | `Tooling` | MCP tool опубликован в manifest, но его узкая production semantics ещё не реализована в текущем Preview scope |
| `E2D-PROJECT-0001` | `Error` | `Project` | project document malformed или не может быть parsed |
| `E2D-PROJECT-0002` | `Error` | `Project` | project document version новее поддерживаемой |
| `E2D-PROJECT-0003` | `Warning` | `Project` | project document содержит безопасно применимый suggested fix |
| `E2D-RUNTIME-0001` | `Error` | `Runtime` | runtime debug bridge отклонил unsafe, production-mode или invalid runtime inspection request |
| `E2D-TASK-0002` | `Error` | `Project` | task operation rejected из-за acceptance guard, privileged field guard или недопустимого перехода |
| `E2D-TASK-0003` | `Warning` | `Project` | dependency graph требует внимания: cycle, unfinished dependency или cancelled dependency |
| `E2D-TASK-0004` | `Error` | `Project` | writer lock не получен до deadline; операция может быть повторена |
| `E2D-TASK-0005` | `Error` | `Project` | ограниченное ожидание writer lock отменено; операция может быть повторена |
| `E2D-TASK-0006` | `Error` | `Project` | optimistic task/board revision не совпала; mutation не записана и не повторяется автоматически |
| `E2D-TASK-0007` | `Error` | `Project` | operation ID уже связан с другим fingerprint или повреждённым receipt |
| `E2D-TEST-0001` | `Error` | `Tooling` | scene test assertion не нашёл ожидаемый node, type или property value |
| `E2D-TEST-0002` | `Error` | `Tooling` | scene visual comparison не нашёл reference image или превысил tolerance |
| `E2D-TOOLING-0001` | `Info` | `Tooling` | job cancellation state changed или cancel request был отклонён без изменения job state |
| `E2D-TOOLING-0002` | `Error` | `Tooling` | workspace transaction rejected из-за revision mismatch, unsafe path, validation error или conflict |
| `E2D-TOOLING-0003` | `Warning` | `Tooling` | active Editor session не найдена, устарела, освобождена или descriptor относится к другому project root |
| `E2D-TOOLING-0004` | `Error` | `Tooling` | Editor session endpoint небезопасен или не является поддержанным локальным endpoint |

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

## Фактическое состояние, ограничения и проверки

Статус: реализованная внутренняя основа.
Задача: `T-0146`.
Обновлено: 2026-06-22.

## Назначение

`Diagnostics.Core` реализован в `Electron2D.ProjectSystem` как internal contract, то есть внутренний формат, доступный тестам и будущим слоям Editor/Tooling, но не публичный runtime API для игр.

Слой задаёт общий формат structured diagnostics: сообщений об ошибках, предупреждениях, информационных состояниях и подсказках. Он не зависит от CLI, MCP, Editor UI или SARIF. Эти внешние представления должны подключаться отдельными adapters поверх core-модели.

Текущий adapter слой для CLI, JSONL stream events и SARIF описан в [Diagnostics adapters: JSON, JSONL stream и SARIF](diagnostics-adapters.md). Core остаётся источником registry, validation rules и безопасных suggested fixes; adapters отвечают только за внешнее представление того же payload.

## Registry кодов

`DiagnosticCodeRegistry` хранит immutable список `DiagnosticCodeDefinition`. Каждый definition фиксирует:

- `Code`;
- `Severity`;
- `Category`;
- `Title`;
- `DocumentationUri`.

Коды отсортированы по `Code`, уникальны и используют формат `E2D-<DOMAIN>-NNNN`.

Текущий начальный registry:

| Code | Severity | Category | Назначение |
| --- | --- | --- | --- |
| `E2D-AGENT-0001` | `Error` | `Tooling` | agent MCP handshake отклонён: неизвестная сессия, неверный token или невозможность подключиться к active Editor route |
| `E2D-AGENT-0002` | `Error` | `Tooling` | agent MCP token истёк до handshake |
| `E2D-AGENT-0003` | `Error` | `Tooling` | agent process bootstrap или запуск процесса отклонён до handshake |
| `E2D-CLI-0001` | `Error` | `Tooling` | command group или subcommand не реализован в текущем Preview scope |
| `E2D-CLI-0002` | `Error` | `Tooling` | CLI arguments неполные или некорректные |
| `E2D-CLI-0003` | `Error` | `Tooling` | route selection или project root не позволяют безопасно выполнить команду |
| `E2D-CAPABILITY-0001` | `Error` | `Tooling` | Editor capability parity нарушен между Editor, Tooling и MCP |
| `E2D-CAPABILITY-0002` | `Error` | `Tooling` | CLI binding policy для capability некорректна |
| `E2D-CAPABILITY-0003` | `Error` | `Tooling` | Editor capability manifest имеет некорректную форму, ссылку или покрытие категорий |
| `E2D-CAPABILITY-0004` | `Error` | `Tooling` | capability ссылается на неопубликованный Tooling command или MCP tool/resource |
| `E2D-DIAG-0001` | `Error` | `Diagnostics` | diagnostic запись неполная или противоречит registry |
| `E2D-DOCTOR-0001` | `Error` | `Project` | reproducibility baseline отсутствует, повреждён или противоречит `global.json`/`.csproj` |
| `E2D-MCP-0001` | `Error` | `Tooling` | MCP tool опубликован в manifest, но его узкая production semantics ещё не реализована в текущем Preview scope |
| `E2D-PROJECT-0001` | `Error` | `Project` | project document malformed или не может быть parsed |
| `E2D-PROJECT-0002` | `Error` | `Project` | project document version новее поддерживаемой |
| `E2D-PROJECT-0003` | `Warning` | `Project` | project document содержит безопасно применимый suggested fix |
| `E2D-RUNTIME-0001` | `Error` | `Runtime` | runtime debug bridge отклонил unsafe, production-mode или invalid runtime inspection request |
| `E2D-TASK-0002` | `Error` | `Project` | task operation rejected из-за acceptance guard, privileged field guard или недопустимого перехода |
| `E2D-TASK-0003` | `Warning` | `Project` | dependency graph требует внимания: cycle, unfinished dependency или cancelled dependency |
| `E2D-TASK-0004` | `Error` | `Project` | writer lock не получен до deadline; операция может быть повторена |
| `E2D-TASK-0005` | `Error` | `Project` | ограниченное ожидание writer lock отменено; операция может быть повторена |
| `E2D-TASK-0006` | `Error` | `Project` | optimistic task/board revision не совпала; mutation не записана и не повторяется автоматически |
| `E2D-TASK-0007` | `Error` | `Project` | operation ID уже связан с другим fingerprint или повреждённым receipt |
| `E2D-TEST-0001` | `Error` | `Tooling` | scene test assertion не нашёл ожидаемый node, type или property value |
| `E2D-TEST-0002` | `Error` | `Tooling` | scene visual comparison не нашёл reference image или превысил tolerance |
| `E2D-TOOLING-0001` | `Info` | `Tooling` | job cancellation state changed или cancel request был отклонён без изменения job state |
| `E2D-TOOLING-0002` | `Error` | `Tooling` | workspace transaction rejected из-за revision mismatch, unsafe path, validation error или conflict |
| `E2D-TOOLING-0003` | `Warning` | `Tooling` | active Editor session не найдена, устарела, освобождена или descriptor относится к другому project root |
| `E2D-TOOLING-0004` | `Error` | `Tooling` | Editor session endpoint небезопасен или не является поддержанным локальным endpoint |

`StructuredDiagnostic.Create(...)` создаёт запись только по registered code. Если severity или category не совпадают с registry, factory завершает создание ошибкой. Это нужно, чтобы будущие CLI/MCP/Editor adapters не расходились в трактовке одного и того же кода.

## Diagnostic payload

`StructuredDiagnostic` хранит:

- `Code`;
- `Severity`;
- `Category`;
- `Message`;
- optional `DiagnosticLocation`;
- `RelatedLocations`;
- `SuggestedFixes`;
- `DocumentationUri` из registry.

`DiagnosticLocation` поддерживает project-relative file, 1-based line/column, scene UID, node path и resource UID. `line` и `column` остаются пустыми, если подсистема не знает точную позицию.

## Suggested fixes

`DiagnosticSuggestedFix` содержит title и один или несколько `DiagnosticFixAction`.

Разрешённые actions:

- `ReplaceText`;
- `CreateFile`;
- `UpdateJsonProperty`;
- `DeleteJsonProperty`.

Action path нормализуется как project-relative path. Absolute paths, `..`, generated/cache paths и URI вне `res://` отклоняются. JSON actions требуют JSON Pointer, начинающийся с `/`; text replacements требуют 1-based start/end line/column и `newText`.

Core-слой только описывает fix. Он не применяет изменения к файлам и не запускает команды.

## JSON serialization

`DiagnosticJsonSerializer` пишет deterministic JSON:

- поля идут в стабильном порядке;
- enum значения пишутся строками;
- line endings нормализуются в LF;
- round-trip сохраняет code, severity, category, message, location, related locations, suggested fixes и documentation URI.

Deserializer проверяет, что `documentationUri` совпадает с registry definition для `code`.

Методы `DiagnosticJsonSerializer.ToJson(...)` и `DiagnosticJsonSerializer.ToJsonArray(...)` используются adapters для сохранения полного diagnostic payload в CLI JSON, JSONL events и SARIF properties.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~DiagnosticsCoreTests
```

Эта проверка покрывает registry stability, required fields, suggested fix validation и deterministic serialization.
