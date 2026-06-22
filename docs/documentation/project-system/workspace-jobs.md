# WorkspaceJob contract и event stream

Статус: реализованная внутренняя основа.
Задача: `T-0147`.
Обновлено: 2026-06-22.

## Назначение

`WorkspaceJob` реализован в `Electron2D.ProjectSystem` как internal core, то есть внутренняя модель долгих операций для будущих Tooling, CLI, MCP, CI и Editor adapters. Он не является публичным runtime API для игр и не запускает реальные import/build/test/export/run toolchains.

Первая реализация хранит job state in-memory внутри `ProjectWorkspace.Jobs`. Она нужна, чтобы все будущие адаптеры читали один lifecycle contract: operation id, snapshot input identity, state, progress, cancel, diagnostics, artifacts, timestamps и stale markers.

## Job store и lifecycle

`ProjectWorkspace` создаёт `WorkspaceJobStore` в свойстве `Jobs`. `WorkspaceJobStore.Enqueue(...)` создаёт `WorkspaceJob` в состоянии `Queued` с progress `0`, заданным `OperationId`, `Kind`, `WorkspaceJobInputIdentity` и флагом `CanCancel`.

Поддержанные `WorkspaceJobKind`:

- `Import`;
- `Build`;
- `Test`;
- `Export`;
- `Run`.

Поддержанные `WorkspaceJobState`:

- `Queued`;
- `Running`;
- `Succeeded`;
- `Failed`;
- `Cancelled`.

Lifecycle methods:

- `Start(...)` переводит job из `Queued` в `Running`, заполняет `StartedAt` и публикует `operation.started`.
- `ReportProgress(...)` принимает значения `0..1`, работает только для `Running` и не позволяет progress уменьшаться.
- `CompleteSucceeded(...)` переводит job в `Succeeded`, выставляет progress `1`, сбрасывает `CanCancel`, заполняет `CompletedAt` и публикует `operation.completed`.
- `CompleteFailed(...)` добавляет structured diagnostic, сохраняет последний progress, переводит job в `Failed`, заполняет `CompletedAt` и публикует `operation.diagnostic`, затем `operation.completed`.
- `Cancel(...)` для cancellable `Queued` или `Running` job добавляет structured diagnostic `E2D-TOOLING-0001`, переводит job в `Cancelled`, заполняет `CompletedAt` и публикует `operation.diagnostic`, затем `operation.completed`.

Terminal states (`Succeeded`, `Failed`, `Cancelled`) не принимают новые progress updates, diagnostics, artifacts или повторный cancel.

## Event stream

`WorkspaceJobEventStream` является in-memory publisher/subscriber. Он сохраняет порядок публикации и отдаёт fake consumers в tests immutable event payload:

- `EventName`;
- `OperationId`;
- `Kind`;
- `State`;
- `Progress`;
- `CanCancel`;
- `InputIdentity`;
- `StartedAt`;
- `CompletedAt`;
- `Stale`;
- optional `Diagnostic`;
- optional `Artifact`.

Поддержанные event names:

- `operation.started`;
- `operation.progress`;
- `operation.diagnostic`;
- `operation.artifactProduced`;
- `operation.completed`.

CLI JSONL, MCP events, Editor panels и Agent Workspace должны подключаться поверх этого stream в отдельных задачах. Текущий core не содержит JSONL serializer, IPC или UI binding.

## Diagnostics и cancellation

Job diagnostics используют `StructuredDiagnostic` из `Diagnostics.Core`. Plain string error не является job result.

Для cancel/refused cancel добавлен registry code:

| Code | Severity | Category | Назначение |
| --- | --- | --- | --- |
| `E2D-TOOLING-0001` | `Info` | `Tooling` | job cancellation state changed или cancel request был отклонён без изменения job state |

Если cancel невозможен из-за `CanCancel = false` или terminal state, `Cancel(...)` возвращает failed `WorkspaceJobCancelResult` со structured diagnostic `E2D-TOOLING-0001`, но не меняет job state, не добавляет diagnostic в job и не публикует event. Это fail-closed поведение защищает consumers от ложного изменения lifecycle.

## Artifacts и stale markers

`WorkspaceJobArtifact` хранит:

- `ArtifactKind`;
- `InputIdentity`;
- `Stale`.

`WorkspaceJob.RefreshStale(...)` использует `WorkspaceSnapshotStalenessEvaluator` и текущий build/run/export configuration hash. Если изменился input document, `ContentRevision` или configuration hash, job и все уже добавленные artifacts получают `Stale = true`.

Metadata-only изменения workspace, например diagnostics update для editor selection, меняют `WorkspaceRevision`, но не `ContentRevision`; такие изменения не делают job или artifact stale при неизменных input documents и configuration hash.

## Ограничения первой реализации

- Job store не сохраняется на диск.
- Реальные import/build/test/export/run workers ещё не подключены.
- Нет CLI JSONL/MCP/Editor adapter.
- Нет event replay для поздних subscribers; consumer получает только события после подписки.
- `OperationId` uniqueness проверяется только внутри текущего in-memory `WorkspaceJobStore`.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~WorkspaceJobTests
```

Эта проверка покрывает lifecycle, ordered event stream, progress, failed completion diagnostics, cancellation, artifacts и stale marking по snapshot inputs.
