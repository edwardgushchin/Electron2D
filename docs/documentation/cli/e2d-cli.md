# `e2d` CLI для headless, CI и active Editor routing

Статус: реализованная внутренняя основа.
Задача: `T-0116`.
Обновлено: 2026-06-22.

## Назначение

`src/Electron2D.Cli` содержит executable assembly `e2d`. Текущий CLI остаётся headless-интерфейсом для CI и automation, но изменяющие операции уже умеют выбирать route: active Editor workspace или explicit headless workspace.

CLI не содержит собственной модели проекта. Он разбирает аргументы, выбирает route, вызывает `Electron2D.Tooling` и сериализует stable JSON/JSONL output.

## Command groups

Root help показывает группы:

- `project`;
- `scene`;
- `resource`;
- `workspace`;
- `import`;
- `build`;
- `run`;
- `test`;
- `export`;
- `docs`;
- `api`;
- `mcp`;
- `context`;
- `doctor`.

Группы, которые ещё реализуются отдельными задачами, возвращают structured diagnostic `E2D-CLI-0001`, а не молча выполняют частичный или небезопасный путь.

## Common flags

Group help показывает common flags:

- `--project <path>`;
- `--format text|json|jsonl`;
- `--quiet`;
- `--verbose`.

Для mutating/job groups дополнительно показываются:

- `--dry-run`;
- `--headless`.

`--project` по умолчанию равен текущей директории. `--headless` запрещает active Editor route для команды и создаёт headless `ProjectWorkspace`.

## Реализованные команды

### `docs`

Существующие команды сохранены:

```powershell
e2d docs search "move and slide" --format json
e2d docs type CharacterBody2D --format json
e2d docs member CharacterBody2D.MoveAndSlide --format json
e2d docs example "platformer movement" --format json
```

Они читают `data/api/electron2d-api-manifest.json` и `data/documentation/electron2d-local-docs-index.json`.

### `project validate`

`project validate` создаёт stable CLI envelope. Текущий `T-0116` не запускает полный project validator; команда фиксирует parser/output contract для будущего validation layer.

### `workspace transaction`

Generic mutation path:

```powershell
e2d workspace transaction `
  --project <path> `
  --path scenes/main.scene.json `
  --expected-revision 1 `
  --text "<scene json>" `
  --format json
```

Если active Editor registry доступен и `--headless` не указан, команда использует `EditorSessionRegistry.Connect(...)`, получает `ProjectToolingHost` active workspace и применяет `WorkspaceOnly` transaction. Файл на диске не меняется, а документ становится dirty в Editor workspace.

Если active Editor не найден или указан `--headless`, команда открывает headless `ProjectWorkspace`, загружает target document и применяет `HeadlessCommit`. `--dry-run` возвращает предполагаемые affected files, но не меняет workspace и файл.

### `import`, `build`, `run`, `test`, `export`

Эти команды создают `WorkspaceJob` через `Electron2D.Tooling` и могут писать JSONL:

```powershell
e2d build --project <path> --format jsonl --input-build-configuration-hash sha256:debug
```

Минимальный stream сейчас содержит queued event:

- `operationId`;
- `jobId`;
- `jobKind`;
- `jobState`;
- `inputSnapshotId`;
- `inputWorkspaceRevision`;
- `inputContentRevision`;
- `inputDocumentRevisions`;
- `inputBuildConfigurationHash`;
- `stale`;
- `diagnostics`;
- `artifacts`.

Реальные toolchain progress/completion events появятся в соответствующих import/build/run/test/export задачах. Schema уже фиксирует identity fields, чтобы будущий runner не менял CLI contract.

### `run` headless runtime mode

`e2d run` остаётся generic job command, если runtime-флаги не указаны. Headless runtime mode включается при `--scene`, `--frames`, `--fixed-delta`, `--input`, `--capture-frame` или `--output`.

Пример:

```powershell
e2d run `
  --project <path> `
  --scene scenes/main.scene.json `
  --frames 600 `
  --fixed-delta 0.0166667 `
  --input tests/input/start-game.json `
  --capture-frame 300 `
  --output artifacts/run-001 `
  --format json
```

В этом режиме команда создаёт output directory с `result.json`, `diagnostics.json`, `runtime.log.jsonl`, `scene-tree-final.json`, `performance.json` и `frame-XXXX.png`, если указан `--capture-frame`. JSON artifacts используют schemas из `schemas/runtime/` и сохраняют snapshot identity fields. Фактический формат описан в [Headless runtime automation](../runtime/headless-runtime-automation.md).

## JSON envelope

`--format json` возвращает stable object:

- `schemaVersion`;
- `command`;
- `succeeded`;
- `exitCode`;
- `projectRoot`;
- `route`;
- `dryRun`;
- `message`;
- `diagnostics`;
- `changedFiles`;
- `dirtyDocuments`;
- `operation`;
- `job`;
- `data`.

`route` принимает значения:

- `none`;
- `activeEditor`;
- `headless`;
- `blocked`.

## Diagnostics

CLI adapter добавляет к общему registry:

| Code | Severity | Category | Назначение |
| --- | --- | --- | --- |
| `E2D-CLI-0001` | `Error` | `Tooling` | command group или subcommand не реализован в текущем Preview scope |
| `E2D-CLI-0002` | `Error` | `Tooling` | CLI arguments неполные или некорректные |
| `E2D-CLI-0003` | `Error` | `Tooling` | route selection или project root не позволяют безопасно выполнить команду |

Tooling diagnostics пробрасываются в CLI JSON без потери stable code, severity, category, message и documentation URI.

## Текущие ограничения

`T-0116`/`T-0121` всё ещё не реализуют:

- `project create`;
- удобные scene/resource команды;
- `api compare-godot`;
- `context build`;
- полноценный `doctor`;
- запуск реальных import/build/test/export toolchains;
- запуск пользовательского C# runtime process внутри `run` headless mode.

Эти задачи должны использовать текущий parser, route selection, result envelope и `Electron2D.Tooling`, а не создавать второй project state.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DCliWorkflowTests
```

Проверка покрывает root/group help, common flags, `workspace transaction` JSON, dry-run headless fallback, active Editor routing, JSONL job identity и stable unsupported-command diagnostic.

Headless runtime проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DHeadlessRuntimeAutomationTests
```
