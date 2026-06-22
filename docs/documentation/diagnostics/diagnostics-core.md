# Diagnostics.Core

Статус: реализованная внутренняя основа.
Задача: `T-0146`.
Обновлено: 2026-06-22.

## Назначение

`Diagnostics.Core` реализован в `Electron2D.ProjectSystem` как internal contract, то есть внутренний формат, доступный тестам и будущим слоям Editor/Tooling, но не публичный runtime API для игр.

Слой задаёт общий формат structured diagnostics: сообщений об ошибках, предупреждениях, информационных состояниях и подсказках. Он не зависит от CLI, MCP, Editor UI или SARIF. Эти внешние представления должны подключаться отдельными adapters поверх core-модели.

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
| `E2D-DIAG-0001` | `Error` | `Diagnostics` | diagnostic запись неполная или противоречит registry |
| `E2D-PROJECT-0001` | `Error` | `Project` | project document malformed или не может быть parsed |
| `E2D-PROJECT-0002` | `Error` | `Project` | project document version новее поддерживаемой |
| `E2D-PROJECT-0003` | `Warning` | `Project` | project document содержит безопасно применимый suggested fix |
| `E2D-TOOLING-0001` | `Info` | `Tooling` | job cancellation state changed или cancel request был отклонён без изменения job state |

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

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~DiagnosticsCoreTests
```

Эта проверка покрывает registry stability, required fields, suggested fix validation и deterministic serialization.
