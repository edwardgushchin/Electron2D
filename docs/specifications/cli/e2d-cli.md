# `e2d` CLI для headless, CI и active Editor routing

Статус: целевая спецификация для `T-0116`.
Обновлено: 2026-06-22.
Связанные документы: [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md); [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md).

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
- `docs`;
- `api`;
- `mcp`;
- `context`;
- `doctor`.

В `T-0116` обязательный исполняемый scope:

- существующие `docs search/type/member/example`;
- `project validate`;
- `workspace transaction` как generic mutation path;
- job-backed stubs для `import`, `build`, `run`, `test`, `export`;
- help и stable unsupported-command diagnostics для групп, реализация которых закреплена отдельными задачами.

`project create`, детальные scene/resource команды, `api compare-godot`, `mcp serve`, `context build` и `doctor` расширяются отдельными задачами, но уже должны использовать общий parser, common flags и result envelope.

## Common flags

Каждая команда и группа должны принимать:

- `--help`;
- `--format text|json|jsonl`;
- `--quiet`;
- `--verbose`;
- `--project <path>`.

Изменяющие команды дополнительно принимают:

- `--dry-run`;
- `--headless`.

`--project` нормализуется в absolute project root. Если флаг отсутствует, используется текущая директория.

`--headless` означает: не использовать active Editor route даже если discovery нашёл Editor. Без `--headless` `workspace transaction` выбирает active Editor, если registry/gateway доступен.

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

Tooling diagnostics пробрасываются без потери `code`, `severity`, `category`, `message` и `documentationUri`.

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

`import`, `build`, `run`, `test`, `export` используют `WorkspaceJob` через `Electron2D.Tooling`.

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

Поскольку `T-0116` ещё не запускает реальные toolchains, progress/completion появляются позже. CLI уже должен сериализовать same identity fields, чтобы будущий real runner не менял schema.

## Documentation commands

`docs search/type/member/example` сохраняют существующее поведение. Они должны поддерживать common flags, но `--format jsonl` для docs недопустим и возвращает `E2D-CLI-0002`.

## Acceptance criteria

- Root help и group help показывают обязательные command groups и common flags.
- `project validate`, `workspace transaction`, `import`, `build`, `run`, `test`, `export`, `docs` принимают `--project`, `--format`, `--quiet`, `--verbose`; mutating/job commands принимают `--dry-run`.
- `workspace transaction --format json` возвращает stable result envelope с route, diagnostics, changed files, dirty documents и operation metadata.
- При active Editor registry command routed в active `ProjectWorkspace` и не создаёт independent writer.
- При отсутствии active Editor command использует explicit headless fallback.
- `--dry-run` не меняет файл и workspace.
- `import/build/run/test/export --format jsonl` возвращают stable JSONL job event с snapshot identity fields и stale marker.
- Unsupported groups/subcommands возвращают stable CLI diagnostic code и non-zero exit code.
- Существующие `docs` JSON commands продолжают работать.
- Есть focused integration tests для JSON/JSONL shape, common flags/help, active Editor routing, headless fallback и unsupported diagnostics.
- Implementation documentation описывает фактическое поведение, ограничения и focused test command.
