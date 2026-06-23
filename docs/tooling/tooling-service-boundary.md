# Electron2D.Tooling service boundary

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0115`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Live ProjectWorkspace](../project-system/live-project-workspace.md); [WorkspaceTransactionEngine и безопасные project operations](../project-system/workspace-transactions.md); [WorkspaceJob contract и event stream](../project-system/workspace-jobs.md); [ProjectTaskManager, TaskActivity и task storage](../project-system/project-task-manager.md).

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

## Фактическое состояние, ограничения и проверки

Статус: реализованная внутренняя основа, расширенная Script/Debugger services.
Задачи: `T-0115`, `T-0161`.
Обновлено: 2026-06-23.

## Назначение

`src/Electron2D.Tooling` добавлен как отдельный internal project, то есть внутренний слой для Editor, CLI, MCP, CI и integration tests. Он не является публичным runtime API для игр и не содержит UI, CLI parser, IPC или MCP protocol server.

Текущий слой принимает уже разобранные операции, применяет их к `ProjectWorkspace` и возвращает один structured result. Это снижает риск, что будущий Editor, CLI и MCP начнут по-разному менять один и тот же проект.

## ProjectToolingHost

`ProjectToolingHost` создаётся поверх открытого `ProjectWorkspace` и предоставляет services:

- `Project`;
- `Tasks`;
- `Build`;
- `Tests`;
- `Export`;
- `Import`;
- `Runtime`;
- `Script`;
- `Debug`.

`ProjectToolingHost` не создаёт workspace lease сам. Открытие Editor/headless workspace остаётся ответственностью слоя выше.

`ProjectToolingHost.SupportedCommandNames` публикует machine-readable список Tooling command identifiers для `Editor Capability Manifest`. Это не отдельный dispatcher: список нужен verifier-у, чтобы capability не ссылалась на несуществующий Tooling endpoint. Production semantics по-прежнему находятся в `ProjectService`, `TaskService`, job services, `ToolingScriptService` и `ToolingDebugService`.

## ToolingOperationResult

Изменяющие операции возвращают `ToolingOperationResult`:

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

`PersistedRevision` выбирается как single-document compatibility value для adapter-ов, которым нужно одно число. Полная карта остаётся в `DocumentRevisions`.

## ProjectService

`ProjectService.ApplyTextEdit(...)` оборачивает `WorkspaceTransactionEngine` и поддерживает режимы:

- `WorkspaceOnly`;
- `HeadlessCommit`;
- `ExternalImport`.

`ProjectService.SaveAffectedDocuments(...)` вызывает `WorkspaceTransactionRequest.SaveAffectedDocuments(...)`.

Tooling не имеет собственной логики merge, atomic write или unsafe-path проверки. Revision mismatch, generated/cache path protection, conflict records, backups и dirty state приходят из `WorkspaceTransactionEngine` и пробрасываются в `ToolingOperationResult`.

## TaskService

`TaskService` объявляет поддержанные command names:

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

Реализованные wrappers используют `ProjectTaskManager` и `OperationContext`:

- submit for acceptance;
- accept;
- request changes;
- cancel;
- append activity;
- link transaction;
- link job;
- link artifact;
- list/get открытых task documents.

`task_accept` требует trusted human context с capability `TaskAccept`; agent context получает `E2D-TASK-0002` и не меняет task status.

## Job services

`Build`, `Tests`, `Export` и `Import` создают `WorkspaceJob` через `WorkspaceJobStore`. `Runtime.Queue(...)` сохраняет тот же job-backed contract для generic `project.run` route. Эти paths не запускают реальные toolchains; они фиксируют job lifecycle shape:

- создаётся `WorkspaceSnapshot`;
- `WorkspaceJobInputIdentity` сохраняет snapshot id, workspace revision, content revision, document revisions и build configuration hash;
- job ставится в `Queued`;
- result возвращает `JobKind`, `JobState`, input identity, diagnostics и artifacts.

Такой result нужен будущим CLI JSONL, MCP events и Editor panels без дублирования job lifecycle.

## Runtime service

`ProjectToolingHost.Runtime` дополнительно реализует Editor-attached runtime control:

- `StartEditorAttached(...)` создаёт `WorkspaceSnapshot`, materializes snapshot, ставит run job в очередь и создаёт active `ProjectWorkspace.Runtime` session с `SessionKind = EditorAttachedPreview`;
- `Pause()`, `Resume()`, `Step(...)`, `InjectInput(...)`, `CaptureFrame()`, `GetSceneTree()`, `GetDiagnostics()`, `HighlightNode(...)`, `ReportProcessCrash(...)` и `Stop()` управляют active session;
- отсутствующая active session или недопустимая команда возвращает structured diagnostic `E2D-RUNTIME-0001`;
- crash state остаётся в workspace runtime session, чтобы MCP и Agent Workspace могли показать diagnostics без падения Editor workspace.

Этот service не является managed debugger и не заменяет будущий renderer-backed frame capture.

## Script service

`ProjectToolingHost.Script` добавляет смысловой доступ к C# documents без эмуляции действий в UI:

- mutating commands `script_create`, `script_rename`, `script_delete`, `script_apply_text_edits`, `script_format`, `script_rename_symbol`, `script_apply_code_action` используют `expectedRevision`, `WorkspaceTransactionEngine`, grouped undo и structured diagnostics;
- read-only commands `script_read`, `script_search_text`, `script_get_diagnostics`, `script_get_completions`, `script_get_signature_help`, `script_get_hover`, `script_get_definition`, `script_get_document_symbols`, `script_find_references`, `script_get_code_actions` читают live document text, `DocumentRevision` и `SemanticVersion`;
- IDE-команды не создают `WorkspaceSnapshot`; snapshot остаётся входом для build/test/run/debug jobs;
- `script_save` отклоняет agent save, если после базовой revision агента в документе есть ручные unsaved changes.

`ToolingScriptIdeResult` возвращает DTO, принадлежащие Tooling: completion items, signature help, hover, diagnostic, locations, document symbols и code actions. Внутренние типы language-services не выходят за boundary Tooling/MCP/Editor.

## Debug service

`ProjectToolingHost.Debug` добавляет model-first managed debugger contract:

- breakpoints создаются, обновляются и удаляются по `BreakpointId`;
- `debug_start` и `debug_restart` создают `WorkspaceSnapshot` и enqueue run job с `WorkspaceJobInputIdentity`;
- `debug_attach` для agent context разрешён только к active Editor game process, если нет явного интерактивного подтверждения;
- `debug_get_stack()` возвращает stacks всех threads;
- `debug_get_locals(frameId)` и `debug_get_arguments(frameId)` требуют явный frame id;
- `debug_get_watches()` возвращает только определения watches, а `debug_evaluate_watches(frameId)` возвращает значения для указанного frame.

Текущая реализация использует smoke session из `Electron2D.ManagedDebugging` и не является постоянным desktop debugger event loop. Она фиксирует contract для MCP, Editor Script workspace и будущего acceptance benchmark.

## Текущие ограничения

Текущий `T-0115` закрывает service boundary и базовые wrappers, а `T-0161` добавляет script/debug Tooling parity. Эти задачи не реализуют:

- CLI commands и flags;
- MCP tools/resources;
- Editor UI updates сверх событий, уже публикуемых `ProjectWorkspace`;
- полноценные scene/resource convenience commands;
- реальный import/build/test/export/run toolchain запуск;
- реальный named pipe/Unix domain socket protocol server.
- renderer-backed screenshot для attached runtime session.

Локальный registry/gateway contract для обнаружения active Editor-сессии реализован отдельно: [Editor session discovery и Editor-hosted Agent Gateway](editor-session-discovery.md).

`Editor Capability Manifest` реализован отдельно: [Editor Capability Manifest](editor-capability-manifest.md). Он использует текущий Tooling catalog, чтобы проверять AI-паритет между Editor, Tooling, MCP и CLI.

Остальные возможности остаются в следующих задачах и должны использовать текущий Tooling contract.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ToolingServiceBoundaryTests
```

Проверка покрывает transaction wrappers, result shape, revision mismatch, unsafe path, TaskService acceptance guard, task links через transaction semantics, job-backed long operations и Editor-attached runtime control.

Script/debug focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ScriptDebugToolingParityTests" -m:1
```
