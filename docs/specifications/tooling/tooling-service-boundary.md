# Electron2D.Tooling service boundary

Статус: целевая спецификация для `T-0115`.
Обновлено: 2026-06-22.
Связанные документы: [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Live ProjectWorkspace](../project-system/live-project-workspace.md); [WorkspaceTransactionEngine и безопасные project operations](../project-system/workspace-transactions.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [ProjectTaskManager, TaskActivity и task storage](../project-system/project-task-manager.md).

## Назначение

`Electron2D.Tooling` — общий слой семантических операций над `ProjectWorkspace`. Его будут использовать Editor, CLI, MCP, CI и тесты, чтобы операция «изменить проект», «изменить сцену», «изменить задачу», «запустить build/test/export/run» или «получить документацию» имела один contract и не дублировалась в каждом adapter-е.

Этот слой не является Editor UI, MCP protocol server или CLI parser. Он принимает уже разобранные команды, применяет их к `ProjectWorkspace` и возвращает structured result. Визуальная доска задач, локальный IPC, CLI flags и MCP schema реализуются отдельными задачами поверх этого contract.

## Service boundary

Минимальный набор сервисов:

- `ProjectService` — общие workspace transactions, save, headless commit и external import;
- `TaskService` — операции `ProjectTaskManager`;
- `SceneService` — scene document operations поверх transaction engine;
- `ResourceService` — resource document operations поверх transaction engine;
- `ScriptService` — script document operations и future language-service boundary;
- `ImportService` — job-based import operations;
- `BuildService` — job-based build operations;
- `TestService` — job-based test operations;
- `ExportService` — job-based export operations;
- `RuntimeService` — job-based run/runtime-control boundary;
- `DocumentationService` — доступ к локальному документационному индексу и API manifest.

В `T-0115` достаточно реализовать общий host, result contract, project transaction wrappers, task wrappers и job-backed stubs для долгих операций. Узкие scene/resource/script/runtime semantics закрываются отдельными задачами, но уже должны использовать один result shape.

## Operation result

Каждая изменяющая операция возвращает `ToolingOperationResult`:

- `Succeeded`;
- `OperationId`;
- `OperationKind`;
- `WorkspaceRevision`;
- `DocumentRevisions`;
- `PersistedRevision`;
- `DirtyDocuments`;
- `PersistenceState`;
- `ChangedFiles`;
- `ChangedObjects`;
- `CreatedObjects`;
- `Diagnostics`;
- `UndoGroupId`;
- optional `TaskId`;
- optional `JobId`.

`PersistedRevision` является удобным единственным значением для adapter-ов, когда операция затронула один документ. Полный map остаётся в `DocumentRevisions`.

## Transaction rules

Tooling не имеет собственной реализации конкурентного редактирования. Все изменяющие document operations должны вызывать один из режимов `WorkspaceTransactionEngine`:

- `WorkspaceOnly`;
- `SaveAffectedDocuments`;
- `HeadlessCommit`;
- `ExternalImport`.

Операции, меняющие открытый document, обязаны принимать `expectedRevision`. Revision mismatch возвращает failed result со structured diagnostic и не меняет workspace. `UndoGroupId` передаётся в transaction engine, чтобы будущий Editor мог отменить агентскую операцию одной группой.

Tooling не должен писать generated/cache paths и не должен писать за пределами project root. Эти проверки выполняет transaction engine; Tooling обязан пробрасывать diagnostics, а не перехватывать их строковыми ошибками.

## TaskService

`TaskService` предоставляет команды:

- `task_list`;
- `task_get`;
- `task_create`;
- `task_update`;
- `task_claim`;
- `task_set_status`;
- `task_add_subtask`;
- `task_add_dependency`;
- `task_append_activity`;
- `task_link_transaction`;
- `task_link_job`;
- `task_link_artifact`;
- `task_submit_for_acceptance`;
- `task_accept`;
- `task_request_changes`;
- `task_cancel`.

Task commands должны использовать `ProjectTaskManager`, `TaskDependencyGraph`, `OperationContext` и task document serializer. `task_accept` и `task_request_changes` требуют доверенный human `OperationContext` с нужной capability; agent-originated payload не может подменить `PrincipalKind` или audit fields.

`task_cancel` переводит задачу в `Cancelled` как больше не нужную. Это не отказ от результата на приёмке: для возврата результата используется `task_request_changes`.

`task_create` создаёт canonical `.electron2d/tasks/<taskId>.e2task` document и возвращает тот же operation result shape. Project template, начальная доска и внешний report export находятся вне `T-0115`.

## Long-running operations

Import/build/test/export/run являются долгими операциями. Tooling service для них создаёт `WorkspaceJob` через job contract и возвращает `ToolingJobResult`:

- `Succeeded`;
- `OperationId`;
- `JobId`;
- `JobKind`;
- `JobState`;
- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `Diagnostics`;
- `Artifacts`.

`T-0115` не запускает реальные toolchains. Он обязан закрепить, что долгие операции используют job contract, а не синхронный boolean result.

## Workspace events

Tooling operations не публикуют отдельную параллельную event model. Они используют события `ProjectWorkspace` и events, которые создают transactions, diagnostics, tasks и jobs. Editor UI, MCP resources и Agent Workspace adapters должны потреблять эти события позже.

## Критерии приёмки

- Есть focused tests, что `ProjectService` применяет `WorkspaceOnly`, `SaveAffectedDocuments`, `HeadlessCommit` и `ExternalImport` через `WorkspaceTransactionEngine`, возвращая единый `ToolingOperationResult`.
- Есть focused tests, что result содержит operation id/kind, workspace/document revisions, persisted revision, dirty state, changed files/objects, diagnostics и undo group.
- Есть focused tests, что revision mismatch и unsafe path возвращают failed result без изменения workspace.
- Есть focused tests, что `TaskService` предоставляет task commands и использует `ProjectTaskManager`/`OperationContext` для submit, accept, request changes, cancel и activity.
- Есть focused tests, что agent context не может выполнить `task_accept`, а human context с capability может.
- Есть focused tests, что task links на transactions/jobs/artifacts записываются в task document через transaction semantics.
- Есть focused tests, что build/test/export/run/import service stubs создают `WorkspaceJob` и возвращают job event/state data, а не boolean.
- `Electron2D.Tooling` вынесен в отдельный project ниже Editor/CLI/MCP adapters и не зависит от Editor UI.
- Implementation documentation описывает фактическое поведение, текущие ограничения и focused test command.
