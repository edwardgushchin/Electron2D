# Electron2D.Tooling service boundary

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
