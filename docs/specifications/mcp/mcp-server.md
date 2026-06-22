# Локальный MCP-сервер поверх active Editor session и Tooling

Статус: целевая спецификация для `T-0119`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md); [Editor session discovery и Editor-hosted Agent Gateway](../tooling/editor-session-discovery.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [ProjectTaskManager, TaskActivity и task storage](../project-system/project-task-manager.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [Diagnostics adapters: JSON, stream и SARIF](../diagnostics/diagnostics-adapters.md); [Runtime debug bridge и scene inspection](../runtime/runtime-debug-bridge.md).

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
