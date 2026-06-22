# Electron2D.Tooling service boundary

Статус: реализованная внутренняя основа.
Задача: `T-0115`.
Обновлено: 2026-06-22.

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
- `Runtime`.

`ProjectToolingHost` не создаёт workspace lease сам. Открытие Editor/headless workspace остаётся ответственностью слоя выше.

`ProjectToolingHost.SupportedCommandNames` публикует machine-readable список Tooling command identifiers для `Editor Capability Manifest`. Это не отдельный dispatcher: список нужен verifier-у, чтобы capability не ссылалась на несуществующий Tooling endpoint. Production semantics по-прежнему находятся в `ProjectService`, `TaskService` и job services.

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

`Build`, `Tests`, `Export`, `Import` и `Runtime` создают `WorkspaceJob` через `WorkspaceJobStore`. Текущая реализация не запускает реальные toolchains; она фиксирует job-backed contract:

- создаётся `WorkspaceSnapshot`;
- `WorkspaceJobInputIdentity` сохраняет snapshot id, workspace revision, content revision, document revisions и build configuration hash;
- job ставится в `Queued`;
- result возвращает `JobKind`, `JobState`, input identity, diagnostics и artifacts.

Такой result нужен будущим CLI JSONL, MCP events и Editor panels без дублирования job lifecycle.

## Текущие ограничения

Текущий `T-0115` закрывает service boundary и базовые wrappers. Он не реализует:

- CLI commands и flags;
- MCP tools/resources;
- Editor UI updates сверх событий, уже публикуемых `ProjectWorkspace`;
- полноценные scene/resource/script convenience commands;
- реальный import/build/test/export/run toolchain запуск;
- реальный named pipe/Unix domain socket protocol server.

Локальный registry/gateway contract для обнаружения active Editor-сессии реализован отдельно: [Editor session discovery и Editor-hosted Agent Gateway](editor-session-discovery.md).

`Editor Capability Manifest` реализован отдельно: [Editor Capability Manifest](editor-capability-manifest.md). Он использует текущий Tooling catalog, чтобы проверять AI-паритет между Editor, Tooling, MCP и CLI.

Остальные возможности остаются в следующих задачах и должны использовать текущий Tooling contract.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ToolingServiceBoundaryTests
```

Проверка покрывает transaction wrappers, result shape, revision mismatch, unsafe path, TaskService acceptance guard, task links через transaction semantics и job-backed long operations.
