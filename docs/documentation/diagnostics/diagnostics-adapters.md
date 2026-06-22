# Diagnostics adapters: JSON, JSONL stream и SARIF

Статус: реализованная внутренняя основа.
Задача: `T-0123`.
Обновлено: 2026-06-22.
Связанные документы: [Diagnostics adapters: JSON, stream и SARIF](../../specifications/diagnostics/diagnostics-adapters.md); [Diagnostics.Core](diagnostics-core.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [Локальный MCP adapter для Editor-сессии и Tooling](../mcp/mcp-server.md).

## Назначение

Diagnostics adapters - это слой преобразования внутренней модели `StructuredDiagnostic` во внешние форматы для CLI, JSONL-событий, будущих MCP resources, Agent Workspace panel и CI-отчётов. Он не вводит отдельную модель ошибок: каждый adapter берёт записи из общего registry `Diagnostics.Core` и сохраняет тот же payload.

Слой реализован в `Electron2D.ProjectSystem`, потому что диагностика нужна CLI, Tooling, MCP и будущему редактору без зависимости от UI.

## Full JSON diagnostic payload

`DiagnosticJsonSerializer.ToJson(...)` и `DiagnosticJsonSerializer.ToJsonArray(...)` пишут полный diagnostic object:

- `code`;
- `severity`;
- `category`;
- `message`;
- `location`;
- `relatedLocations`;
- `suggestedFixes`;
- `documentationUri`.

CLI adapter `Electron2DCommandLine.WriteDiagnostics(...)` использует этот полный payload для JSON output и JSONL job events. Поэтому потребители больше не получают урезанную запись только с `code`, `severity`, `category`, `message` и `documentationUri`.

Published schema:

```text
schemas/diagnostics/electron2d-diagnostic.schema.json
```

## JSONL diagnostics stream

`DiagnosticStreamEventJsonSerializer.WriteEvent(...)` создаёт один machine-readable event для JSONL streams:

- `schemaVersion = 1`;
- `event`;
- `producer`;
- `timestampUtc`;
- `diagnostics`.

Поле `diagnostics` содержит тот же полный JSON payload. Текущий код не запускает долгоживущий stream сам по себе; он фиксирует формат события, который могут использовать будущие MCP, Agent Workspace и subsystem runners без зависимости от готового Editor UI.

Published schema:

```text
schemas/diagnostics/diagnostic-stream-event.schema.json
```

## SARIF output

`DiagnosticSarifSerializer.WriteRun(...)` создаёт SARIF 2.1.0 объект для CI и статических отчётов. CLI route `e2d validate --format sarif` использует этот serializer и возвращает:

- `$schema = https://json.schemastore.org/sarif-2.1.0.json`;
- `version = 2.1.0`;
- `runs[0].tool.driver.name = Electron2D`;
- `runs[0].tool.driver.rules` по diagnostic codes;
- `runs[0].results` по diagnostic entries.

Severity mapping:

| `DiagnosticSeverity` | SARIF `level` |
| --- | --- |
| `Error` | `error` |
| `Warning` | `warning` |
| `Info` | `note` |
| `Hint` | `note` |

Если diagnostic содержит `location.file`, adapter пишет SARIF `physicalLocation.artifactLocation.uri` и, когда доступны, `startLine`/`startColumn`. Если location отсутствует, result остаётся валидным и просто не содержит `locations`.

Полный Electron2D payload сохраняется в `result.properties.electron2dDiagnostic`. Safe suggested fixes сохраняются отдельно в `result.properties.electron2dSuggestedFixes` как структурированные actions; adapter не превращает их в shell-команды.

## CLI validate

Короткая команда:

```powershell
e2d validate --project <path> --format sarif
```

и route `e2d project validate --format sarif` используют общий SARIF writer. В текущем Preview validation route является стабильным каркасом: он проверяет CLI parsing, route и output format, но ещё не запускает полноценный project/compiler/shader validator. Поэтому успешный вызов может вернуть SARIF run без diagnostics.

## Текущие ограничения

- Полный project validator добавляется отдельными задачами.
- Compiler, shader import и build diagnostics должны подключаться к этому же payload, но их production checks реализуются в соответствующих доменных задачах.
- JSON Schema files публикуются в репозитории и проверяются тестами на наличие и базовую структуру; отдельная внешняя SARIF schema validation пока не запускается.
- Suggested fixes остаются описанием безопасных file/JSON actions. Применение fixes должно идти через transaction layer, а не напрямую из SARIF consumer.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~DiagnosticsAdapterTests
```

Эта проверка покрывает полный JSON diagnostic payload, JSONL stream event для fake consumer, SARIF envelope, SARIF rules/results/locations, сохранение Electron2D payload в SARIF properties и наличие published schemas.
