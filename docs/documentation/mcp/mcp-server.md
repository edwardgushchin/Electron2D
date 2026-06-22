# Локальный MCP adapter для Editor-сессии и Tooling

Статус: реализованный внутренний контракт для `T-0119`.
Обновлено: 2026-06-22.
Связанные документы: [Локальный MCP-сервер поверх active Editor session и Tooling](../../specifications/mcp/mcp-server.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md); [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [ProjectTaskManager](../project-system/project-task-manager.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [Diagnostics adapters: JSON, JSONL stream и SARIF](../diagnostics/diagnostics-adapters.md); [Runtime debug bridge и scene inspection](../runtime/runtime-debug-bridge.md); [Editor-attached runtime control](../runtime/editor-attached-runtime-control.md).

## Назначение

`Electron2D.Mcp` реализует локальный MCP-like adapter: тонкий внутренний слой, который принимает уже разобранные запросы к resources/tools и вызывает `Electron2D.Tooling`. В текущем Preview это in-process contract, то есть проверяемый C# API без сетевого transport. Transport поверх pipe/socket можно добавить позже без изменения имён resources и tools.

Adapter не зависит от облачного AI-провайдера, не содержит ключи моделей, не запускает произвольный shell и не читает signing secrets. Команда `e2d mcp serve` сейчас выводит manifest resources/tools и route information в общем CLI JSON envelope; она не запускает долгоживущий серверный процесс.

`T-0149` добавляет Editor-side bootstrap для локальных агентских процессов. Bootstrapper создаёт temporary MCP configuration outside project root и передаёт агенту только путь к нему через `ELECTRON2D_MCP_CONFIG`; token остаётся внутри config file и проверяется до подключения к active Editor route. Сам `McpServerSession` по-прежнему является in-process contract без cloud provider и без shell execution.

## Route selection

`McpServerSession.Open(projectRoot, registry, nowUtc)` нормализует project root и выбирает route:

- `activeEditor` - если `EditorSessionRegistry` нашёл живой primary Editor для этого проекта; mutating tools работают через тот же `ProjectWorkspace`, поэтому видят unsaved documents и dirty state;
- `headless` - если registry не передан, Editor закрыт или active session устарела; adapter создаёт headless `ProjectWorkspace`, открывает project documents с диска и выполняет операции через `Tooling`;
- `blocked` зарезервирован для будущих rejected descriptor/permission scenarios.

Headless-сессия открывает `*.scene.json` и `.electron2d/tasks/*.e2task`, чтобы job snapshot identity и task tools работали с теми же document revisions, что и active Editor route.

## Resources

Manifest публикует обязательные Preview resources:

- `electron2d://project/summary`;
- `electron2d://project/settings`;
- `electron2d://project/scenes`;
- `electron2d://project/resources`;
- `electron2d://project/diagnostics`;
- `electron2d://workspace/open-documents`;
- `electron2d://workspace/selection`;
- `electron2d://workspace/import-state`;
- `electron2d://workspace/build-state`;
- `electron2d://scene/{uid}`;
- `electron2d://resource/{uid}`;
- `electron2d://api/type/{name}`;
- `electron2d://api/godot-compatibility/{name}`;
- `electron2d://editor/capabilities`;
- `electron2d://runtime/capabilities`;
- `electron2d://runtime/session`;
- `electron2d://docs/topic/{name}`.

Полностью исполняемая часть возвращает live state для `project/summary`, `workspace/open-documents`, `project/diagnostics`, `workspace/import-state`, `workspace/build-state` и `runtime/session`. Остальные resources имеют стабильные URI и безопасный placeholder payload, пока узкие доменные подсистемы реализуются отдельными задачами.

`project/diagnostics` должен сохранять тот же полный diagnostic payload, что и CLI/JSONL adapters: file/node/resource location, related locations и safe suggested fixes не сокращаются до human-only строки.

`electron2d://editor/capabilities` возвращает canonical `Editor Capability Manifest` из `data/editor/electron2d-editor-capabilities.json`. Этот resource показывает каждую capability, её category, support status, Tooling binding, MCP binding и CLI policy. Если строка имеет `partial`, это означает видимый контракт будущего workflow, а не готовый production путь.

`electron2d://runtime/session` возвращает active Editor-attached runtime session: session kind, state, snapshot identity, metrics, input actions, highlighted node path and diagnostics. Если attached session отсутствует, resource возвращает `active = false`.

## Tools

Manifest публикует все обязательные tool names из MCP specification: project, scene, resource, workspace, runtime и task tools. Наличие имени означает стабильный route и безопасный failure mode.

Исполняемый минимум `T-0119`:

- `workspace_get_state` возвращает open documents, dirty documents, workspace/content revisions и per-document revisions;
- `workspace_apply_transaction` применяет text transaction через `ProjectToolingHost.Project.ApplyTextEdit`; в active Editor route используется `WorkspaceOnly`, в headless route используется `HeadlessCommit`;
- `project_build`, `project_run`, `project_test`, `project_export` и `resource_import` создают `WorkspaceJob` через соответствующий Tooling service;
- `runtime_start`, `runtime_stop`, `runtime_pause`, `runtime_resume`, `runtime_step`, `runtime_inject_input`, `runtime_capture_frame`, `runtime_get_scene_tree`, `runtime_get_diagnostics`, `runtime_highlight_node` и `runtime_report_crash` управляют active Editor-attached runtime session через `ToolingRuntimeService`;
- `task_list`, `task_get`, `task_append_activity`, `task_submit_for_acceptance`, `task_accept`, `task_request_changes` и `task_cancel` идут через `TaskService`.

Узкие scene/resource tools, которые ещё не имеют production semantics, возвращают structured diagnostic `E2D-MCP-0001` и не пишут project files напрямую.

Runtime tools не создают отдельную приватную модель. Они читают и меняют `ProjectWorkspace.Runtime.ActiveSession`, поэтому Agent Workspace и MCP видят один и тот же state.

## Task guard

MCP task tools используют agent `OperationContext`. Этот контекст имеет capability для записи задач и отправки на приёмку, но не имеет trusted human capability. Поэтому:

- `task_submit_for_acceptance` может перевести задачу из `Review` в `AwaitingAcceptance`;
- `task_accept` возвращает failed result с `E2D-TASK-0002`;
- `task_request_changes` возвращает failed result с `E2D-TASK-0002`, пока нет будущего доверенного подтверждения из Editor;
- MCP payload не принимает `ActorKind`, `AcceptedBy`, `AcceptedAt` или privileged activity fields.

## Job events

Job tools возвращают queued MCP event с именем `operation.queued`. Event сохраняет данные из core job contract:

- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `InputBuildConfigurationHash`;
- `Stale`.

`operation.started`, `operation.progress`, `operation.diagnostic`, `operation.artifactProduced` и `operation.completed` уже закреплены specification как stream shape, но реальные toolchain runners подключаются отдельными задачами.

Diagnostics lists внутри job events и будущие `operation.diagnostic` events используют общий diagnostics stream contract из `Diagnostics adapters`. MCP adapter не создаёт отдельный payload для diagnostic сообщений.

## CLI manifest

Команда:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- mcp serve --project <project-root> --format json
```

возвращает общий CLI envelope с `command = "mcp serve"`, `route`, `data.resources`, `data.tools`, `data.cloudProviderRequired = false` и `data.editorCapabilityManifest`.

`data.editorCapabilityManifest` содержит `path`, количество capabilities, количество `releaseRequired` строк, `succeeded` и diagnostics verifier-а. Полный manifest читается через resource `electron2d://editor/capabilities`.

## Agent bootstrap config

Agent process bootstrap использует тот же route vocabulary, что и `McpServerSession`: successful handshake из открытого Editor должен получить `activeEditor`. Config file имеет format `Electron2D.AgentMcpBootstrap`, содержит `agentSessionId`, `projectRoot`, local endpoint descriptor, `expiresAtUtc` и ephemeral token. Этот файл временный, хранится вне project root и не является canonical project storage.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~Electron2DMcpServerTests
```

Она покрывает manifest resources/tools, `electron2d://editor/capabilities`, `electron2d://runtime/session`, live dirty state active Editor route, `workspace_apply_transaction`, headless job event snapshot identity, runtime control tools, task acceptance guard и CLI manifest без облачного AI-провайдера.
