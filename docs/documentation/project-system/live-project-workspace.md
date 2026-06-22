# Live ProjectWorkspace

Статус: реализованная внутренняя основа.
Задача: `T-0139`.
Обновлено: 2026-06-22.

## Назначение

`ProjectWorkspace` реализован в `Electron2D.ProjectSystem` как internal core, то есть внутренняя модель проекта для будущих Editor, Tooling, CLI, MCP, CI и тестов. Он не является публичным runtime API для игр.

Первая реализация хранит состояние in-memory. Она не пишет файлы на диск, не запускает IPC, не синхронизирует внешние изменения и не содержит UI bindings. Эти возможности подключаются отдельными задачами поверх текущего core-контракта.

## Компоненты

Текущий `ProjectWorkspace` создаёт обязательные сервисы:

- `DocumentStore` — открытые документы, normalized path, text content, canonical snapshot, persisted/in-memory revisions и dirty state;
- `CommandBus` — typed internal commands для открытия, изменения и отметки persisted text document;
- `ChangeEventStream` — in-memory publisher/subscriber для workspace events;
- `RevisionStore` — `WorkspaceRevision`, `ContentRevision`, `DocumentRevisions`, `DirtyDocuments` и `PersistenceState`;
- `OperationJournal` — завершённые operations, unfinished transaction marker и recovery snapshot dirty-документов;
- `UndoRedo` — минимальный in-memory список undo groups для следующих задач;
- `ImportState` — минимальное хранилище import states по project-relative path;
- `BuildState` — минимальное in-memory состояние сборки;
- `DiagnosticsStore` — structured diagnostics из `Diagnostics.Core` по source key.
- `Transactions` — internal transaction engine для dry-run, revision checks, safe save/headless/external import modes, conflicts и grouped undo.

## Ownership

`ProjectWorkspaceLeaseRegistry` управляет ownership для одного project root:

- первый `OpenEditor(...)` создаёт `EditorPrimary`;
- второй `OpenEditor(...)` для того же root получает `EditorReadOnly` и message с текущим owner id;
- `OpenHeadless(...)` создаёт mutable headless workspace тем же core-кодом, не забирая Editor lease;
- primary lease имеет heartbeat через `OwnerLease.Touch(...)`;
- stale lease определяется по configured timeout и может быть заменён новым primary owner;
- `Dispose()` primary workspace освобождает lease, если registry всё ещё принадлежит этому owner.

`EditorReadOnly` workspace не выполняет mutating commands: `CommandBus.CanExecuteMutatingCommands` возвращает `false`, а mutating methods возвращают failed result.

## Document commands

`OpenTextDocument(...)`:

1. normalizes path;
2. parses text через `ProjectDocumentParser`;
3. создаёт clean document с одинаковыми persisted/in-memory revisions;
4. увеличивает `WorkspaceRevision`;
5. публикует `DocumentOpened`;
6. записывает completed operation journal entry.

`ReplaceTextDocument(...)`:

1. проверяет `expectedRevision`;
2. fail-closed, если revision не совпала;
3. parses новый text;
4. увеличивает `InMemoryRevision`;
5. оставляет `PersistedRevision` прежней;
6. помечает document dirty;
7. увеличивает `WorkspaceRevision` и `ContentRevision`;
8. публикует `DocumentChanged`;
9. записывает completed operation journal entry.

`MarkDocumentPersisted(...)`:

1. проверяет `expectedRevision`;
2. обновляет `PersistedRevision` до `InMemoryRevision`;
3. снимает dirty state;
4. увеличивает `WorkspaceRevision`;
5. публикует `DocumentPersisted`;
6. записывает completed operation journal entry.

## Diagnostics и recovery

`DiagnosticsStore.SetDiagnostics(...)` заменяет diagnostics для source key, увеличивает `WorkspaceRevision`, публикует `DiagnosticsUpdated` и записывает operation journal entry. `ContentRevision` при этом не меняется, потому что diagnostic update не меняет игровые/project source documents.

`OperationJournal.BeginTransaction(...)` создаёт unfinished transaction marker. `RecordRecoverySnapshot(...)` сохраняет in-memory snapshot dirty-документов с path, text, persisted revision и in-memory revision. `CompleteTransaction(...)` закрывает marker и проставляет completion timestamp.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectWorkspaceTests
```

Эта проверка покрывает ownership/read-only/headless режимы, stale lease replacement, document dirty/revision state, event stream, diagnostics store и recovery journal.
