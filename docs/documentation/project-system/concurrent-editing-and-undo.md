# Human-AI concurrent editing и grouped Undo

Статус: реализованная внутренняя основа для `T-0143`.
Обновлено: 2026-06-23.
Связанные документы: [спецификация concurrent editing](../../specifications/project-system/concurrent-editing-and-undo.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceTransactionEngine](workspace-transactions.md); [External Change Synchronizer](external-change-synchronizer.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md).

Этот слой работает внутри `Electron2D.ProjectSystem`. Он не рисует Editor conflict panel сам, но возвращает данные, которые будущий Editor, Agent Workspace, Tooling и MCP смогут показать без повторной проверки project files.

## Что реализовано

Открытые project documents имеют `DocumentId`, `PersistedRevision`, `InMemoryRevision`, dirty state и canonical snapshot. `WorkspaceTransactionEngine` требует `ExpectedRevision` для обычных workspace/headless edits и использует `ExternalImport` для прямых изменений файлов.

`ExternalImport` выполняет three-way сравнение persisted baseline, текущего dirty document и incoming file state:

- непересекающиеся property changes в scene/resource/settings JSON объединяются;
- изменение одного property двумя участниками возвращает `PropertyConflict`;
- удаление объекта, изменённого другим участником, возвращает `DeletedChangedObject`;
- unsupported structural changes возвращают conflict record;
- dirty C# buffer не перезаписывается direct file change: текущий минимальный слой возвращает conflict, если safe text merge не может быть доказан.

`OperationJournal` записывает provenance через `ProjectWorkspaceActorKind`: `Human`, `Agent`, `Cli`, `ExternalFile` или `Test`.

## Grouped Undo/Redo

`ProjectWorkspaceUndoRedoStore` хранит reversible groups с before/after snapshots открытых text documents. `UndoLast(...)` применяет before snapshots всей группы, а `RedoLast(...)` повторяет after snapshots.

Повторная запись того же `UndoGroupId` в хвост undo stack объединяется в один group. Это используется для external debounce batch: несколько file changes, применённых в одном `Drain(...)`, получают общий `undo-external-batch-*` group и отменяются одним `UndoLast(...)`.

Undo/redo публикует workspace events, обновляет revisions/dirty state и пишет отдельную operation journal entry для самого undo/redo действия. Снимки сейчас in-memory; удаление или перезапись файлов вне сохранённого snapshot не объявляются обратимыми.

Job-backed операции `build`, `import`, `run`, `test` и `export` создают `WorkspaceJob`, но не добавляют project Undo group.

## Binary assets

Новый binary asset может перейти в `Importing`. Замена или удаление уже известного binary asset возвращает pending conflict, если asset используется открытым document через project-relative path или `res://` reference.

В этом случае:

- `ImportState[path] = pending-conflict`;
- result содержит diagnostic `E2D-TOOLING-0002`;
- known file state не удаляется;
- project Undo group не создаётся.

Это минимальный usage graph: текущая реализация ищет ссылки в открытых documents. Полный resource graph будет расширяться вместе с Editor resource workflow.

## Ограничения

- Visual conflict panel ещё не реализована; текущий слой отдаёт `WorkspaceTransactionConflict`, diagnostics и import state.
- Text merge для C# buffers пока fail-closed через conflict, если текущий text snapshot не может доказать safe merge.
- Undo/redo работает для открытых text documents, где transaction сохранила before/after snapshots.
- Binary content не merge-ится автоматически.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~WorkspaceTransactionTests|FullyQualifiedName~ExternalChangeSynchronizerTests"
```

Тесты покрывают revision guard, structural merge, same-property conflict, deleted-changed-object conflict, dirty C# buffer conflict, provenance, real grouped undo/redo, shared external batch undo group и binary replace/delete pending conflicts.
