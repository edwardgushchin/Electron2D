# Локальный MCP-сервер поверх active Editor session и Tooling

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0119`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1-preview](../releases/0.1-preview.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md); [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [ProjectTaskManager, TaskActivity и task storage](../project-system/project-task-manager.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [Diagnostics adapters: JSON, stream и SARIF](../diagnostics/diagnostics-adapters.md); [Runtime debug bridge и scene inspection](../runtime/runtime-debug-bridge.md).

## Назначение

Electron2D MCP adapter предоставляет локальный, необлачный typed interface для AI-клиентов и будущих IDE-интеграций. Он не привязан к OpenAI, Anthropic, Gemini или другому поставщику модели, не запускает произвольный shell и не читает signing secrets. Adapter принимает MCP-like requests, выбирает active Editor или headless workspace и вызывает `Electron2D.Tooling`.

`T-0119` фиксирует in-process protocol contract и command surface, который можно тестировать без внешнего AI-провайдера и без настоящего network/pipe server. Реальная transport binding может быть добавлена поверх этого contract без изменения resources/tools.

## Route semantics

MCP session создаётся для project root:

- если active Editor найден, adapter подключается к `ProjectWorkspace` active session через `EditorSessionRegistry`;
- если Editor закрыт или session stale, adapter создаёт headless `ProjectWorkspace`;
- descriptor mismatch или небезопасный endpoint возвращает structured diagnostic;
- mutating tools не создают второй writer, когда active Editor доступен.

Result каждого call содержит:

- `SchemaVersion`;
- `ToolName`;
- `Succeeded`;
- `Route`;
- `Diagnostics`;
- optional `Operation`;
- optional `Job`;
- optional `Task`;
- `Content`.

`Route` values совпадают с CLI route vocabulary: `activeEditor`, `headless`, `blocked`.

## Resources

`T-0119` должен публиковать resources:

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

Resources должны читать live `ProjectWorkspace` state, когда session routed в active Editor. Например, `electron2d://workspace/open-documents` обязан видеть dirty documents, открытые только в памяти.

`electron2d://project/diagnostics` и будущие diagnostics events должны использовать полный payload из `Diagnostics adapters`: `location`, `relatedLocations` и `suggestedFixes` не должны теряться при переходе из `DiagnosticsStore` в MCP resource или stream.

## Tools

`T-0119` должен регистрировать tools:

- `project_validate`, `project_build`, `project_run`, `project_test`, `project_export`;
- `scene_create`, `scene_inspect`, `scene_add_node`, `scene_remove_node`, `scene_move_node`, `scene_set_property`, `scene_attach_script`, `scene_connect_signal`;
- `resource_inspect`, `resource_import`, `resource_find_references`;
- `workspace_get_state`, `workspace_apply_transaction`, `workspace_resolve_conflict`, `workspace_undo_transaction`;
- `runtime_start`, `runtime_stop`, `runtime_pause`, `runtime_step`, `runtime_inject_input`, `runtime_capture_frame`, `runtime_get_scene_tree`, `runtime_get_diagnostics`;
- `task_list`, `task_get`, `task_create`, `task_update`, `task_claim`, `task_set_status`, `task_add_subtask`, `task_add_dependency`, `task_append_activity`, `task_link_transaction`, `task_link_job`, `task_link_artifact`, `task_submit_for_acceptance`, `task_accept`, `task_request_changes`, `task_cancel`.

Наличие tool в manifest означает стабильное имя, schema и safe failure mode. Узкие scene/resource/runtime commands, которые ещё не имеют production semantics, могут возвращать `E2D-MCP-0001`, но они должны быть видимыми и не должны делать direct file writes.

Runtime tools `runtime_pause`, `runtime_step`, `runtime_inject_input`, `runtime_capture_frame`, `runtime_get_scene_tree` и `runtime_get_diagnostics` должны подключаться к shared runtime debug bridge contract, а не читать process state отдельным форматом.

Обязательный исполняемый минимум `T-0119`:

- `workspace_get_state`;
- `workspace_apply_transaction`;
- `project_build`, `project_run`, `project_test`, `project_export`, `resource_import`;
- `task_list`, `task_get`, `task_append_activity`, `task_submit_for_acceptance`, `task_accept`, `task_request_changes`, `task_cancel`.

## Task guard

Task tools используют `TaskService` из `Electron2D.Tooling`. MCP payload не принимает произвольный `ActorKind`, `PrincipalKind`, `AcceptedBy`, `AcceptedAt` или `AcceptanceState`.

Agent context получает capability `TaskSubmitForAcceptance` и `TaskWrite`, но не получает trusted human capability. Поэтому `task_accept` и `task_request_changes` должны возвращать failed result с `E2D-TASK-0002`, пока вызов не пришёл через будущий trusted Editor confirmation.

## Job events

Job tools используют `WorkspaceJob` через Tooling. MCP event stream должен сериализовать:

- `operation.queued`;
- `operation.started`;
- `operation.progress`;
- `operation.diagnostic`;
- `operation.artifactProduced`;
- `operation.completed`.

Минимальный `T-0119` stream обязан вернуть queued event с:

- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `InputBuildConfigurationHash`;
- `Stale`.

Progress/completion для реальных toolchains добавляется позднее, но schema уже фиксирована.

События `operation.diagnostic` и diagnostics lists внутри job events должны использовать тот же diagnostics stream contract, что и CLI/Tooling adapters. MCP transport не вводит отдельный формат diagnostic payload.

## Security

MCP adapter обязан fail closed:

- не выполняет arbitrary shell;
- не читает signing secrets, keystore, certificates или production tokens;
- не принимает absolute paths и `..` для workspace writes;
- не пишет generated/import cache через scene/resource tools;
- не позволяет работать за пределами normalized project root;
- не печатает secret-like endpoint или payload values в diagnostics.

## Acceptance criteria

- Создан project `src/Electron2D.Mcp`, который зависит от `Electron2D.Tooling`, но не от Editor UI и не от cloud AI SDK.
- `McpServerSession` публикует все обязательные resources и tools.
- `workspace_get_state` читает live workspace state и возвращает dirty documents, revisions, diagnostics, import/build state.
- `workspace_apply_transaction` routed в active Editor workspace, если Editor открыт, и в headless workspace, если Editor закрыт.
- Mutating tools возвращают operation result shape из Tooling, а не пишут files напрямую.
- Task tools используют `TaskService`; agent `task_accept` не может обойти human acceptance guard.
- Job tools возвращают queued MCP event с snapshot identity fields и stale marker.
- Unsupported narrow tools возвращают stable diagnostic `E2D-MCP-0001`.
- MCP adapter не выполняет shell и не читает signing secrets.
- `e2d mcp serve --format json` возвращает manifest с resources/tools и route information без запуска cloud AI provider.
- Есть focused integration tests без облачных AI-провайдеров.
- Implementation documentation описывает фактическое поведение, ограничения и focused test command.

## Фактическое состояние, ограничения и проверки

Статус: реализованный внутренний контракт для `T-0119`, расширенный script/debug tools для `T-0161`.
Обновлено: 2026-06-23.
Связанные документы: [Локальный MCP-сервер поверх active Editor session и Tooling](mcp-server.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md); [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [ProjectTaskManager](../project-system/project-task-manager.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [Diagnostics adapters: JSON, JSONL stream и SARIF](../diagnostics/diagnostics-adapters.md); [Runtime debug bridge и scene inspection](../runtime/runtime-debug-bridge.md); [Editor-attached runtime control](../runtime/editor-attached-runtime-control.md).

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
- `script_create`, `script_open`, `script_read`, `script_rename`, `script_delete`, `script_search_text`, `script_apply_text_edits`, `script_save`, `script_format`, `script_get_diagnostics`, `script_get_completions`, `script_get_signature_help`, `script_get_hover`, `script_get_definition`, `script_get_document_symbols`, `script_find_references`, `script_rename_symbol`, `script_get_code_actions`, `script_apply_code_action` идут через `Tooling.Script`;
- `debug_set_breakpoint`, `debug_update_breakpoint`, `debug_remove_breakpoint`, `debug_start`, `debug_attach`, `debug_restart`, `debug_pause`, `debug_continue`, `debug_step_into`, `debug_step_over`, `debug_step_out`, `debug_get_threads`, `debug_get_stack`, `debug_get_locals`, `debug_get_arguments`, `debug_get_watches`, `debug_evaluate_watches`, `debug_add_watch`, `debug_update_watch`, `debug_remove_watch`, `debug_stop` идут через `Tooling.Debug`.

Узкие scene/resource tools, которые ещё не имеют production semantics, возвращают structured diagnostic `E2D-MCP-0001` и не пишут project files напрямую. Script/debug tools больше не относятся к этой placeholder-группе: если имя опубликовано в manifest, запрос маршрутизируется в `Tooling.Script` или `Tooling.Debug`.

Runtime tools не создают отдельную приватную модель. Они читают и меняют `ProjectWorkspace.Runtime.ActiveSession`, поэтому Agent Workspace и MCP видят один и тот же state.

Script tools возвращают document text, document revision, semantic version, diagnostics, completion/signature/hover/navigation payloads и code actions. Mutating script tools используют `expectedRevision` и workspace transaction. `script_save` отклоняет agent save при конфликте с ручными unsaved changes.

Debug tools возвращают breakpoint state, `WorkspaceSnapshot` identity для `debug_start`/`debug_restart`, stacks всех threads, variables выбранного frame и watches. `debug_get_watches()` не вычисляет expressions; значения появляются только в `debug_evaluate_watches(frameId)`.

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

Script/debug parity проверяется отдельно:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ScriptDebugToolingParityTests" -m:1
```
