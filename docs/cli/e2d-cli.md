# `e2d` CLI для headless, CI и active Editor routing

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

Статус: реализованная внутренняя основа.
Задачи: `T-0116`, `T-0148`.
Обновлено: 2026-06-23.

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
- `validate`;
- `docs`;
- `api`;
- `mcp`;
- `tasks`;
- `context`;
- `doctor`.

Группы, которые ещё реализуются отдельными задачами, возвращают structured diagnostic `E2D-CLI-0001`, а не молча выполняют частичный или небезопасный путь.

## Common flags

Group help показывает common flags:

- `--project <path>`;
- `--format text|json|jsonl|sarif`;
- `--quiet`;
- `--verbose`.

Для mutating/job groups дополнительно показываются:

- `--dry-run`;
- `--headless`.

`--project` по умолчанию равен текущей директории. `--headless` запрещает active Editor route для команды и создаёт headless `ProjectWorkspace`.

Команды `docs search/type/member/example` остаются отдельной справочной веткой и принимают только `--format text|json`; `jsonl` и `sarif` для них не поддерживаются.

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

### `api compare-godot`

`api compare-godot` проверяет один публичный тип по canonical API manifest:

```powershell
e2d api compare-godot Control --format json
```

Команда читает `data/api/electron2d-api-manifest.json`, не открывает `ProjectWorkspace` и возвращает общий JSON envelope с `route = none`. В `data` находятся `mode = api.compareGodot`, `sourcePath`, краткое описание `type`, `result.status` и `strictParity`.

Для типа внутри утверждённого `0.1.0` 2D-профиля `result.status = parity_verified`, `succeeded = true`, `exitCode = 0`, а счётчики `missingTypes`, `missingMembers`, `signatureMismatches`, `inheritanceMismatches`, `defaultMismatches` и `unexpectedChanges` равны `0`.

Для типа вне утверждённого профиля команда завершается fail-closed: `succeeded = false`, `exitCode = 1`, `data.result.status = out_of_profile`, diagnostics содержит `E2D-CLI-0002`. Output не предлагает alternative API или workaround, чтобы агент не обходил границы строгого profile contract.

### `project validate`

`project validate` создаёт stable CLI envelope. Текущий `T-0116` не запускает полный project validator; команда фиксирует parser/output contract для будущего validation layer.

### `project create`

`project create` создаёт AI-ready проект из `data/templates/electron2d-empty/` через тот же `ProjectTemplateCreator`, который использует Project Manager:

```powershell
e2d project create MyGame --output .\projects --renderer-profile Compatibility --format json
```

Команда создаёт `.csproj`, `project.e2d.json`, main scene, `AGENTS.md`, `.gitignore`, starter skills в `.codex/skills/`, начальную доску `.electron2d/tasks/board.e2tasks` и стартовую задачу `.electron2d/tasks/welcome.e2task`. Затем она пытается выполнить `git init`.

JSON envelope возвращает `command = "project create"`, `route = "headless"` и `data` с `projectName`, `projectPath`, `projectSettingsPath`, `mainScenePath`, `rendererProfile`, `gitInitialized`, `taskBoardPath`, `starterSkillCount` и `agentInstructionsPath`.

Если `git` недоступен, команда остаётся успешной для созданных файлов, но добавляет warning diagnostic `E2D-PROJECT-0003`; `gitInitialized` будет `false`.

### `validate`

Короткая команда validation route:

```powershell
e2d validate --project <path> --format sarif
```

Команда использует тот же preview validation каркас, что и `project validate`, но пишет SARIF 2.1.0 через diagnostics adapter. Сейчас route проверяет CLI parsing и output contract; полноценный project/compiler/shader validator подключается отдельными задачами, поэтому успешный SARIF output может не содержать diagnostics.

### `doctor`

`e2d doctor` создаёт read-only diagnostic report для reproducibility baseline и локального окружения:

```powershell
e2d doctor --project <path> --format json
```

Команда возвращает общий CLI envelope с `command = "doctor"`, `route = "none"`, пустыми `changedFiles` и `dirtyDocuments`, а также `data.mode = "doctor.environment"`. `route = "none"` означает, что команда не открывала `ProjectWorkspace`, не подключалась к active Editor и не становилась вторым владельцем проекта.

`data.checks[]` содержит `dotnetSdk`, `electron2d`, `nativeRuntime`, `androidSdk`, `androidNdk`, `xcode`, `exportTemplates`, `graphicsCapabilities` и `signing`. Optional mobile toolchains могут иметь status `missing` без failed exit code. Missing или malformed reproducibility files дают summary `blocked` и diagnostic `E2D-DOCTOR-0001`.

Signing check читает только references из `export_presets.e2export.json`. Значения environment variables, certificate files, keystore files и private paths не читаются и не выводятся. `env:NAME` может появиться в output как форма ссылки, но значение `NAME` не раскрывается.

### `tasks export`

`tasks export` создаёт read-only Markdown-отчёт по встроенным задачам пользовательского проекта:

```powershell
e2d tasks export --project <path> --status done --format markdown
```

Если `--format` не указан, команда пишет тот же Markdown в stdout. Команда читает `.electron2d/tasks/*.e2task`, может использовать `.electron2d/tasks/board.e2tasks` только для порядка, но не меняет task storage, не открывает `ProjectWorkspace` на запись, не создаёт Undo group и не пишет отчёт в проектные файлы.

Поддержаны фильтры `--status`, `--milestone`, `--version`, `--epic`, `--assignee` и `--agent-session`. `status` сравнивается со статусом задачи. `milestone`, `version`, `epic` и `agent-session` сейчас читаются из labels вида `milestone:<value>`, `version:<value>`, `epic:<value>`, `agent-session:<value>`; agent session также может быть найден в activity payload с `AgentSessionId=<id>` или `agentSession=<id>`.

Markdown output содержит явное предупреждение, что это report only. Он не заменяет `.electron2d/tasks/*.e2task` и `.electron2d/tasks/board.e2tasks`, не создаёт `TASKS.md`, `completed-tasks/` или `dev-diary/` в пользовательском проекте.

### `context build`

`context build` создаёт компактный static context pack в `.electron2d/context/`:

```powershell
e2d context build --project <path> --format json
```

Команда возвращает общий CLI envelope с `command = "context build"`, `route = "none"` и `data.mode = "context.build"`. `route = "none"` означает, что команда не подключалась к active Editor, не открывала headless `ProjectWorkspace`, не создавала undo group и не меняла исходные игровые документы.

Output layout:

- `context-manifest.json`;
- `project-summary.json`;
- `api-surface.json`;
- `godot-differences.json`;
- `scene-index.json`;
- `resource-graph.json`;
- `diagnostics.json`;
- `conventions.md`.

Context pack является snapshot: он может устареть после любого изменения project settings, scene, resource, script или task documents. Для active Editor workflow живые MCP resources остаются более актуальным источником состояния.

Поддержаны `--format text|json`; `jsonl`, `sarif` и `markdown` для этой команды не поддерживаются. Фактический состав и security policy описаны в [Статический context pack проекта](../project-system/static-context-pack.md).

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

### `export plan-web`, `export build-web`, `export run-web`

WebAssembly browser export имеет отдельные CLI routes и не queue generic `WorkspaceJob`.

```powershell
e2d export plan-web --project <path> --format json
e2d export build-web --project <path> --output exports/web --skip-publish true --format json
e2d export run-web --project <path> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json
```

`plan-web` возвращает `data.mode = "export.web.plan"` и deterministic `wwwroot` layout. `build-web` создаёт `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, `project.e2d.json`, main scene и `assets/**`; без `--skip-publish true` команда сначала проверяет WebAssembly build tools и запускает внешний `dotnet publish`. `run-web` проверяет package contract и пишет `Electron2D.WebAssemblySmokeArtifact`.

### `export plan-ios`, `export build-ios`, `export run-ios`

iOS export имеет отдельные CLI routes и не queue generic `WorkspaceJob`.

```powershell
e2d export plan-ios --project <path> --format json
e2d export build-ios --project <path> --output exports/ios/debug --skip-publish true --format json
e2d export run-ios --project <path> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json
```

`plan-ios` возвращает `data.mode = "export.ios.plan"` и deterministic Xcode staging plan для `IosArm64`/`ios-arm64`. `build-ios --skip-publish true` создаёт transient Xcode project files, `Info.plist`, `Entitlements.plist`, `ExportMetadata.json`, project settings, main scene и `assets/**` без запуска `dotnet publish`, `xcodebuild`, signing или deploy. `run-ios` пишет `Electron2D.IosDeviceSmokeArtifact`; если simulator/device evidence отсутствует, команда возвращает failure с `data.result.status = "smoke-blocked"` и diagnostic `E2D-EXPORT-IOS-0011`.

### `run debug`

`e2d run debug` подключает preview runtime debug bridge. Он не запускает настоящий видимый game process; текущий route создаёт deterministic debug session по scene JSON и возвращает machine-readable runtime state.

Пример:

```powershell
e2d run debug `
  --project <path> `
  --scene scenes/main.scene.json `
  --session-kind editor `
  --step-frames 2 `
  --step-physics 1 `
  --fixed-delta 0.0166667 `
  --physics-delta 0.0166667 `
  --input-action jump=pressed `
  --inspect-node /Root/Player `
  --screenshot artifacts/debug/frame.png `
  --format json
```

Output использует общий CLI envelope с `command = "run debug"`, `data.mode = "runtime.debugBridge"`, `data.session`, `data.sceneTree`, `data.inspectedNode`, `data.metrics`, `data.diagnostics` и optional `data.screenshot`. Фактический contract описан в [Runtime debug bridge и scene inspection](../runtime/runtime-debug-bridge.md).

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

### `test` scene/visual mode

`e2d test` остаётся generic job command для `--format jsonl` и для проектов без scene-test manifest. Scene/visual mode включается только для `--format json`, когда найден `tests/electron2d.scene-tests.json` или явно указан `--manifest <path>`.

Пример:

```powershell
e2d test `
  --project <path> `
  --format json `
  --output artifacts/tests `
  --input-build-configuration-hash sha256:tests
```

В этом режиме команда запускает `Electron2D.Testing`, возвращает `route = headless`, `data.mode = test.scene` и создаёт `result.json`, `diagnostics.json`, `events.jsonl`, screenshots и pixel-diff artifacts. Фактический manifest и ограничения описаны в [Scene tests и visual regression tests](../testing/scene-visual-testing.md).

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
| `E2D-DOCTOR-0001` | `Error` | `Project` | reproducibility files отсутствуют, повреждены или противоречат `global.json`/`.csproj` |

Tooling diagnostics пробрасываются в CLI JSON без потери stable code, severity, category, message и documentation URI.

После `T-0123` CLI JSON и JSONL diagnostics используют полный payload `StructuredDiagnostic`: `location`, `relatedLocations` и `suggestedFixes` сохраняются вместе с code/severity/category/message/documentation URI. SARIF output сохраняет тот же Electron2D payload в `result.properties.electron2dDiagnostic`, а suggested fixes - в `result.properties.electron2dSuggestedFixes`.

## Текущие ограничения

`T-0116`/`T-0121`/`T-0148` всё ещё не реализуют:

- удобные scene/resource команды;
- `context build`;
- полноценный project/compiler/shader validator для `validate`;
- запуск реальных import/build/test/export toolchains;
- запуск пользовательского C# runtime process внутри `run` headless mode.
- настоящий visible game process внутри `run debug`; текущий bridge фиксирует shared inspection contract.

Эти задачи должны использовать текущий parser, route selection, result envelope и `Electron2D.Tooling`, а не создавать второй project state.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DCliWorkflowTests
```

Проверка покрывает root/group help, common flags, `project create`, `tasks export` Markdown golden output, `workspace transaction` JSON, dry-run headless fallback, active Editor routing, JSONL job identity и stable unsupported-command diagnostic.

Headless runtime проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DHeadlessRuntimeAutomationTests
```

Runtime debug bridge проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~RuntimeDebugBridgeTests
```

Scene/visual testing проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DSceneVisualTestingTests
```

Diagnostics adapters проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~DiagnosticsAdapterTests
```

Doctor проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ReproducibilityLockDoctorTests
```
