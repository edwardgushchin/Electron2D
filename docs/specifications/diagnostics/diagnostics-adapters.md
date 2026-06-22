# Diagnostics adapters: JSON, stream и SARIF

Статус: целевая спецификация для `T-0123`.
Обновлено: 2026-06-22.
Связанные документы: [Diagnostics.Core](diagnostics-core.md), [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [`e2d` CLI](../cli/e2d-cli.md), [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md).

## Назначение

`Diagnostics.Core` задаёт внутреннюю модель diagnostic: код, severity, category, message, location, related locations, suggested fixes и documentation URI. Эта спецификация фиксирует adapter layer, то есть слой сериализации той же модели во внешние форматы для CLI, будущих Tooling streams, MCP, Editor panels и CI.

Задача T-0123 не реализует полноценный compiler, shader или project validator. Она обязана зафиксировать формат, чтобы последующие validation/build/script/import tasks не придумывали разные diagnostic payloads и чтобы AI-агент не парсил human-only строки.

## Full JSON diagnostic payload

Каждый JSON diagnostic object обязан сохранять полный payload `StructuredDiagnostic`:

```json
{
  "code": "E2D-PROJECT-0003",
  "severity": "Warning",
  "category": "Project",
  "message": "Project file can be migrated safely.",
  "location": {
    "file": "project.e2project.json",
    "line": 2,
    "column": 3,
    "sceneUid": null,
    "nodePath": null,
    "resourceUid": "uid://project-settings"
  },
  "relatedLocations": [
    {
      "location": {
        "file": "scenes/main.scene.json",
        "line": 7,
        "column": 9,
        "sceneUid": "scene://main",
        "nodePath": "/Root",
        "resourceUid": null
      },
      "message": "Scene references project settings."
    }
  ],
  "suggestedFixes": [
    {
      "title": "Write current schema version.",
      "actions": [
        {
          "kind": "UpdateJsonProperty",
          "path": "project.e2project.json",
          "jsonPointer": "/version",
          "expectedValue": null,
          "newValue": "1"
        }
      ]
    }
  ],
  "documentationUri": "docs/documentation/diagnostics/diagnostics-core.md#e2d-project-0003"
}
```

CLI JSON, CLI JSONL job events, scene/visual test artifacts and future MCP resources must use this full object in `diagnostics`. Адаптер не имеет права терять `location`, `relatedLocations` или `suggestedFixes`.

Published schema:

```text
schemas/diagnostics/electron2d-diagnostic.schema.json
```

## Stream event contract

Live diagnostics streams use JSONL, one event per line. Первый event type:

```json
{
  "schemaVersion": 1,
  "event": "diagnostics.updated",
  "producer": "cli.validate",
  "timestampUtc": "2026-06-22T19:00:00.0000000+00:00",
  "diagnostics": []
}
```

Rules:

- `event` is stable and machine-readable;
- `producer` identifies the adapter or subsystem;
- `timestampUtc` is ISO 8601 UTC;
- `diagnostics` contains full JSON diagnostic payloads;
- consumers must be able to parse events without Editor UI or MCP server.

Published schema:

```text
schemas/diagnostics/diagnostic-stream-event.schema.json
```

## SARIF 2.1.0

`e2d validate --format sarif` must emit a SARIF 2.1.0 object:

```json
{
  "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
  "version": "2.1.0",
  "runs": []
}
```

Rules:

- `runs[0].tool.driver.name` is `Electron2D`;
- every diagnostic code in the run appears in `runs[0].tool.driver.rules`;
- `result.ruleId` equals diagnostic `code`;
- `result.level` maps `Error` to `error`, `Warning` to `warning`, `Info` to `note`, `Hint` to `note`;
- primary `location.file`, `line` and `column` map to SARIF `physicalLocation`;
- `documentationUri` maps to rule `helpUri`;
- full Electron2D diagnostic payload is preserved under `result.properties.electron2dDiagnostic`;
- suggested fixes are preserved as structured actions under `result.properties.electron2dSuggestedFixes`;
- when a diagnostic has no location, SARIF result remains valid and simply omits `locations`.

`e2d project validate --format sarif` may share the same output path. `e2d validate --format sarif` is the short command required for CI and agents.

## Acceptance criteria

- CLI and Tooling JSON diagnostics use the full `StructuredDiagnostic` payload, including location, related locations and suggested fixes.
- JSONL job events and diagnostics stream events use the same full payload.
- `e2d validate --format sarif` emits SARIF 2.1.0 with `Electron2D` tool driver, rules, results, locations and preserved Electron2D payload.
- Suggested fixes remain structured and safe; adapters do not convert them to shell commands.
- Published JSON schemas cover full diagnostic payload and stream event.
- Tests cover JSON adapter shape, stream event fake consumer parsing, SARIF shape and schema file presence.
- Implementation documentation in `docs/documentation/diagnostics/` describes current behavior and verification commands.
