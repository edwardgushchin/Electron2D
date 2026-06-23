# `e2d` CLI для headless, CI и active Editor routing

Статус: целевая спецификация для `T-0116`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md); [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [Diagnostics adapters: JSON, stream и SARIF](../diagnostics/diagnostics-adapters.md).

## Назначение

`e2d` — first-class CLI для headless-режима, CI, пакетных операций и локальной автоматизации. Он не заменяет основной workflow в открытом Editor: если active Editor-сессия найдена для project root, изменяющая команда должна работать через `ProjectToolingHost` этой сессии либо требовать явный headless mode.

CLI adapter не реализует собственную модель проекта. Он разбирает аргументы, выбирает route, вызывает `Electron2D.Tooling` и сериализует стабильный result.

## Command groups

Root help должен показывать группы:

- `project`;
- `scene`;
- `resource`;
- `workspace`;
- `import`;
- `build`;
- `run`;
- `test`;
- `export`;
- `validate`;
- `docs`;
- `api`;
- `mcp`;
- `tasks`;
- `context`;
- `doctor`.

В `T-0116` обязательный исполняемый scope:

- существующие `docs search/type/member/example`;
- `project validate`;
- `validate` как короткий diagnostics/validation route;
- `workspace transaction` как generic mutation path;
- job-backed stubs для `import`, `build`, `run`, `test`, `export`;
- help и stable unsupported-command diagnostics для групп, реализация которых закреплена отдельными задачами.

`project create`, детальные scene/resource команды, `api compare-godot`, `mcp serve`, `context build` и `doctor` расширяются отдельными задачами, но уже должны использовать общий parser, common flags и result envelope.
`tasks export` является read-only report route для встроенного `ProjectTaskManager`.
`context build` является read-only route без открытия `ProjectWorkspace`: команда создаёт `.electron2d/context/` как компактный snapshot проекта и возвращает `route = "none"`.

## Common flags

Каждая команда и группа должны принимать:

- `--help`;
- `--format text|json|jsonl|sarif`;
- `--quiet`;
- `--verbose`;
- `--project <path>`.

Изменяющие команды дополнительно принимают:

- `--dry-run`;
- `--headless`.

`--project` нормализуется в absolute project root. Если флаг отсутствует, используется текущая директория.

`--headless` означает: не использовать active Editor route даже если discovery нашёл Editor. Без `--headless` `workspace transaction` выбирает active Editor, если registry/gateway доступен.

`sarif` является форматом diagnostics/validation output. Documentation commands `docs search/type/member/example` остаются отдельной веткой и принимают только `--format text|json`.

`tasks export` принимает `--format markdown`; если format не указан, команда пишет тот же Markdown в stdout. `jsonl` и `sarif` для этого отчёта не поддерживаются.

`context build` принимает `--format text|json`; если format не указан, команда пишет короткую text-сводку. `jsonl`, `sarif` и `markdown` для context pack не поддерживаются.

## Result envelope

JSON output должен быть stable:

```json
{
  "schemaVersion": 1,
  "command": "workspace transaction",
  "succeeded": true,
  "exitCode": 0,
  "projectRoot": "C:/Project",
  "route": "activeEditor",
  "message": "Workspace transaction applied.",
  "diagnostics": [],
  "changedFiles": [],
  "dirtyDocuments": ["scenes/main.scene.json"],
  "operation": {
    "operationId": "op-cli-...",
    "operationKind": "workspace.transaction",
    "workspaceRevision": 2,
    "contentRevision": 1,
    "documentRevisions": {
      "scenes/main.scene.json": 2
    },
    "persistenceState": "Dirty",
    "undoGroupId": "undo-cli-..."
  },
  "job": null,
  "data": {}
}
```

`route` values:

- `none` — команда не открывает workspace;
- `activeEditor` — команда routed в active Editor workspace;
- `headless` — команда использовала headless workspace;
- `blocked` — команда отказана до Tooling вызова.

Ошибки CLI adapter-а возвращают stable code:

- `E2D-CLI-0001` — command group или subcommand не реализован в текущем Preview scope;
- `E2D-CLI-0002` — аргументы неполные или некорректные;
- `E2D-CLI-0003` — route selection или project root не позволяют безопасно выполнить команду.

Tooling diagnostics пробрасываются без потери полного `StructuredDiagnostic` payload: `code`, `severity`, `category`, `message`, `location`, `relatedLocations`, `suggestedFixes` и `documentationUri`.

## `workspace transaction`

Generic mutation path:

```powershell
e2d workspace transaction --project <path> --path <relative-file> --expected-revision <n> --text <text> --format json
```

Правила:

- `--path` обязателен и должен быть project-relative;
- `--expected-revision` обязателен;
- `--text` обязателен для `ReplaceText` операции;
- при active Editor route применяется `WorkspaceOnly`, не пишет файл на диск и возвращает dirty document;
- при headless route применяется `HeadlessCommit`, пишет файл атомарно через transaction engine;
- `--dry-run` не меняет workspace и файл, но возвращает предполагаемые affected files и diagnostics;
- result содержит `changedFiles`, `dirtyDocuments`, revisions, diagnostics и undo group.

## Job commands и JSONL

`import`, `build`, `run`, `test`, `export` используют `WorkspaceJob` через `Electron2D.Tooling`, кроме явно реализованных specialized routes вроде WebAssembly browser `export plan-web`, `export build-web` и `export run-web`.

`--format jsonl` пишет по одной JSON object строке. Минимальный `T-0116` stream обязан содержать queued event с:

- `schemaVersion`;
- `command`;
- `event`;
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

## WebAssembly browser export routes

`export plan-web`, `export build-web` и `export run-web` являются specialized routes для `WebAssemblyBrowser` и возвращают `route = none`, потому что не изменяют active Editor workspace и не ставят generic job в очередь.

- `export plan-web --format json` возвращает `data.mode = "export.web.plan"`, `browser-wasm`, publish arguments, package layout, browser policies и smoke criteria.
- `export build-web --format json` возвращает `data.mode = "export.web.build"` и создаёт static package files. Без `--skip-publish true` команда сначала проверяет WebAssembly build tools и запускает внешний `dotnet publish`.
- `export run-web --format json` возвращает `data.mode = "export.web.run"` и пишет `Electron2D.WebAssemblySmokeArtifact` с launch URL, runtime policies, diagnostics и criteria results.

Поскольку `T-0116` ещё не запускает реальные toolchains, progress/completion появляются позже. CLI уже должен сериализовать same identity fields, чтобы будущий real runner не менял schema.

`run` имеет отдельный headless runtime mode, когда указаны runtime-флаги `--scene`, `--frames`, `--fixed-delta`, `--input`, `--capture-frame` или `--output`. Этот mode описан в [Headless runtime automation](../runtime/headless-runtime-automation.md) и должен сосуществовать с generic job JSONL mode: `e2d run --format jsonl` без runtime-флагов продолжает возвращать queued job event.

`run debug` имеет отдельный runtime debug bridge mode. Он описан в [Runtime debug bridge и scene inspection](../runtime/runtime-debug-bridge.md) и должен сосуществовать с generic job JSONL mode и headless runtime mode: `e2d run debug --format json` возвращает CLI envelope с `data.mode = "runtime.debugBridge"`, session, scene tree, metrics, diagnostics и optional screenshot metadata.

`test` имеет отдельный scene/visual mode для `--format json`, когда найден `tests/electron2d.scene-tests.json` или явно указан `--manifest <path>`. Этот mode описан в [Scene tests и visual regression tests](../testing/scene-visual-testing.md) и должен сосуществовать с generic job JSONL mode: `e2d test --format jsonl` без scene-test manifest продолжает возвращать queued job event.

## Validation и SARIF

`e2d validate --project <path> --format sarif` должен писать SARIF 2.1.0 через общий diagnostics adapter. `e2d project validate --format sarif` может использовать тот же writer.

Правила:

- SARIF root содержит `$schema = https://json.schemastore.org/sarif-2.1.0.json` и `version = 2.1.0`;
- `runs[0].tool.driver.name = Electron2D`;
- diagnostic codes попадают в `runs[0].tool.driver.rules`;
- каждый result сохраняет `ruleId`, `level`, `message`, location при наличии файла и полный Electron2D payload в `result.properties.electron2dDiagnostic`;
- suggested fixes сохраняются как structured actions в `result.properties.electron2dSuggestedFixes`;
- validation route может временно возвращать пустой diagnostics set, пока полноценный project/compiler/shader validator реализуется отдельными задачами.

## Documentation commands

`docs search/type/member/example` сохраняют существующее поведение. Они должны поддерживать `--project`, `--quiet`, `--verbose` и `--format text|json`, но `jsonl` и `sarif` для docs недопустимы и возвращают ошибку формата.

## Project Tasks report command

`tasks export` читает `.electron2d/tasks/*.e2task` и пишет Markdown-отчёт:

```powershell
e2d tasks export --project <path> --status done --format markdown
```

Команда не открывает workspace на запись, не создаёт Undo group и не пишет отчёт в проектные файлы. `TASKS.md`, `completed-tasks/` и `dev-diary/` не создаются и не обновляются. Markdown является внешним отчётом, а не canonical task storage.

Фильтры:

- `--status`;
- `--milestone`;
- `--version`;
- `--epic`;
- `--assignee`;
- `--agent-session`.

`status` сравнивается с `ProjectTask.Status`. `milestone`, `version`, `epic` и `agent-session` читаются из task labels вида `milestone:<value>`, `version:<value>`, `epic:<value>`, `agent-session:<value>`; agent session также может быть найден в activity payload с `AgentSessionId=<id>` или `agentSession=<id>`.

## Acceptance criteria

- Root help и group help показывают обязательные command groups и common flags.
- `project validate`, `validate`, `workspace transaction`, `import`, `build`, `run`, `test`, `export` принимают `--project`, `--format text|json|jsonl|sarif`, `--quiet`, `--verbose`; mutating/job commands принимают `--dry-run`.
- `docs` принимает `--project`, `--format text|json`, `--quiet`, `--verbose`, но отклоняет `jsonl` и `sarif`.
- `tasks export` принимает `--project`, `--format markdown`, `--quiet`, `--verbose` и read-only filters, а Markdown output покрыт exact golden test.
- `workspace transaction --format json` возвращает stable result envelope с route, diagnostics, changed files, dirty documents и operation metadata.
- При active Editor registry command routed в active `ProjectWorkspace` и не создаёт independent writer.
- При отсутствии active Editor command использует explicit headless fallback.
- `--dry-run` не меняет файл и workspace.
- `import/build/run/test/export --format jsonl` возвращают stable JSONL job event с snapshot identity fields и stale marker.
- JSON и JSONL diagnostics сохраняют полный payload с location, related locations и suggested fixes.
- `e2d validate --format sarif` возвращает SARIF 2.1.0 с rules, results, locations и сохранённым Electron2D payload.
- Unsupported groups/subcommands возвращают stable CLI diagnostic code и non-zero exit code.
- Существующие `docs` JSON commands продолжают работать.
- Есть focused integration tests для JSON/JSONL shape, common flags/help, active Editor routing, headless fallback и unsupported diagnostics.
- Implementation documentation описывает фактическое поведение, ограничения и focused test command.
