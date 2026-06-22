# WorkspaceSnapshot, materialization и job input identity

Статус: реализованная внутренняя основа.
Задача: `T-0153`.
Обновлено: 2026-06-22.

## Назначение

`WorkspaceSnapshot` реализован в `Electron2D.ProjectSystem` как internal core для будущих build/test/run/export jobs. Он фиксирует входное состояние открытого `ProjectWorkspace`, чтобы долгие операции не читали случайное старое состояние с диска, когда Editor или headless workspace содержит dirty-документы.

Этот слой не запускает реальные build/test/run jobs и не является публичным runtime API для игр. Он предоставляет snapshot identity, materialization, staleness evaluation и export input policy, на которые будут опираться последующие Tooling, CLI, MCP и Editor adapters.

## Snapshot

`WorkspaceSnapshot.Create(...)` создаёт immutable копию текущего workspace state:

- `SnapshotId`;
- `WorkspaceRevision`;
- `ContentRevision`;
- `DocumentRevisions`;
- `DirtyDocuments`;
- `OpenCodeBuffers`;
- `CreatedAt`;
- копию text каждого открытого документа.

После создания snapshot не меняется. Если workspace получает новую правку, уже созданный snapshot сохраняет старые revisions и старый text.

`OpenCodeBuffers` сейчас собираются из открытых `.cs` documents. Это позволяет будущим build/test/run jobs видеть code buffers, которые ещё не совпадают с persisted files.

## Materialization

`WorkspaceSnapshotMaterializer.Materialize(...)` пишет snapshot в:

```text
.electron2d/workspaces/<session-id>/<snapshot-id>/
```

Materializer:

- проверяет, что `session-id` и `snapshot-id` являются безопасными path segments;
- пишет копии открытых документов только внутрь snapshot directory;
- пишет `workspace-snapshot.json` с `snapshotId`, `workspaceRevision`, `contentRevision`, `documentRevisions`, `dirtyDocuments` и `openCodeBuffers`;
- не меняет source files проекта;
- не меняет `PersistedRevision`;
- не очищает dirty state.

`WorkspaceSnapshotMaterialization.Cleanup()` удаляет только materialized snapshot directory и дополнительно проверяет, что удаляемый путь остаётся внутри `.electron2d/workspaces/`.

## Job input identity и stale rules

`WorkspaceJobInputIdentity.FromSnapshot(...)` создаёт общий identity payload для будущих job/artifact results:

- `InputSnapshotId`;
- `InputWorkspaceRevision`;
- `InputContentRevision`;
- `InputDocumentRevisions`;
- `InputBuildConfigurationHash`.

`WorkspaceSnapshotStalenessEvaluator.IsStale(...)` возвращает `true`, если изменился build configuration hash, `ContentRevision` или revision входного документа. Изменение только `WorkspaceRevision` не делает artifact stale, если `ContentRevision`, входные document revisions и build configuration остались прежними. Это важно для будущих task/activity/selection/editor metadata updates, которые не должны инвалидировать screenshot или runtime tree игрового запуска.

Текущий focused test использует diagnostics update как metadata-only изменение: оно меняет `WorkspaceRevision`, но не меняет `ContentRevision`.

## Export policy

`WorkspaceExportSnapshotPolicy.PlanCleanPersistedState(...)` — default export policy. Если workspace clean, plan успешен и snapshot не нужен. Если workspace dirty, plan возвращает отказ: clean export не должен неявно сохранять ручные dirty-документы.

`WorkspaceExportSnapshotPolicy.PlanDirtySnapshot(...)` — explicit dirty snapshot policy. Она требует уже созданный `WorkspaceSnapshot`, возвращает его `SnapshotId` и не меняет source files, `PersistedRevision` или dirty state.

Release artifact, построенный из dirty snapshot, должен быть помечен как dirty snapshot input последующими export tasks и не должен считаться clean release artifact без пользовательского решения.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~WorkspaceSnapshotTests
```

Тесты покрывают immutable snapshot identity, dirty documents, open code buffers, materialization, cleanup, stale evaluation и export policy.
