# Live ProjectWorkspace

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0139`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1-preview](../releases/0.1-preview.md); [Canonical document model, revision model и structural diff](canonical-document-model.md); [Stable project text formats, migrations и JSON Schema](project-text-formats.md); [Diagnostics.Core](../diagnostics/diagnostics-core.md).

## Цель

`ProjectWorkspace` — это единая внутренняя живая модель проекта. Её использует открытый Editor, будущий Tooling, CLI, MCP, CI и тесты. Если Editor открыт, он владеет primary workspace session; если Editor закрыт, тот же core-код создаёт headless workspace для пакетных сценариев.

Этот слой не является публичным runtime API для игр. Он нужен, чтобы человек, AI-агент и headless tools не открывали параллельные несогласованные состояния одного проекта и не теряли unsaved changes.

## Компоненты

Минимальный workspace содержит:

- `DocumentStore` — открытые project documents, их canonical snapshots, dirty state и text content;
- `CommandBus` — единый internal вход для typed workspace commands;
- `ChangeEventStream` — in-memory поток событий для будущих Scene Tree, Inspector, FileSystem dock, viewport, diagnostics panel и Agent Workspace adapters;
- `RevisionStore` — `WorkspaceRevision`, `ContentRevision`, `documentRevisions`, `persistedRevision`, `dirtyDocuments` и `persistenceState`;
- `OperationJournal` — журнал происхождения операций и crash recovery state;
- `UndoRedo` — минимальный стек undo groups для будущего grouped undo/redo;
- `ImportState` — in-memory состояние импорта resources;
- `BuildState` — in-memory состояние сборки/reload;
- `DiagnosticsStore` — актуальные structured diagnostics из `Diagnostics.Core`.
- `Transactions` — внутренний transaction engine для dry-run, revision checks, safe save/headless/external import modes, conflicts и grouped undo.

Первая реализация может использовать in-memory storage и typed methods без disk save. Atomic writes, external watcher, IPC, SARIF, MCP и Editor UI bindings выполняются отдельными задачами.

## Ownership и режимы открытия

Для одного canonical project root допускается один primary Editor owner. Ownership registry должен поддерживать:

- `EditorPrimary` — владелец живой mutable workspace session;
- `EditorReadOnly` — второй Editor для того же project root получает read-only workspace с diagnostic explanation;
- `Headless` — workspace без Editor ownership, создаваемый тем же core-кодом для тестов/CI/CLI;
- lease heartbeat — timestamp последнего heartbeat primary owner;
- release — явное освобождение ownership;
- stale lease detection — устаревший lease можно заменить новым primary owner.

`EditorReadOnly` не должен применять mutating commands. Headless workspace не должен забирать primary lease открытого Editor.

## Document lifecycle

Открытие text document:

1. normalizes project-relative path;
2. parses canonical snapshot через `ProjectDocumentParser`;
3. создаёт `ProjectWorkspaceDocument` с `PersistedRevision`, `InMemoryRevision`, `Dirty = false`;
4. публикует событие `document.opened`;
5. обновляет `documentRevisions`.

Изменение text document:

1. требует `expectedRevision`;
2. fail closed, если expected revision не совпала с текущей `InMemoryRevision`;
3. parses новый text в canonical snapshot;
4. увеличивает `InMemoryRevision`;
5. оставляет `PersistedRevision` прежней;
6. помечает document dirty;
7. увеличивает `WorkspaceRevision` и `ContentRevision`;
8. публикует событие `document.changed`;
9. записывает operation journal entry.

Mark persisted:

1. обновляет `PersistedRevision` до текущей `InMemoryRevision`;
2. снимает dirty state;
3. публикует событие `document.persisted`;
4. обновляет `persistenceState`.

## Events

`ChangeEventStream` первой реализации является in-memory publisher/subscriber. Он должен:

- сохранять порядок публикации;
- доставлять события fake consumers в tests;
- не зависеть от Editor UI;
- содержать `eventKind`, `workspaceRevision`, `documentId`, `documentPath`, `operationId` и diagnostics, когда они есть.

UI bindings и external adapters подключаются поверх этого stream в отдельных задачах.

## Operation journal и crash recovery

`OperationJournal` должен хранить:

- operation id;
- actor kind: `Human`, `Agent`, `Cli`, `ExternalFile`, `Test`;
- operation kind;
- affected documents;
- started/completed timestamps;
- unfinished transaction state;
- recovery snapshot dirty-документов, если он создан safe in-memory способом;
- recovery message для следующего запуска.

Первая реализация не обязана писать recovery files на диск. Она должна иметь проверяемый in-memory contract: можно начать transaction, записать dirty snapshot, увидеть unfinished transaction, завершить или отбросить её, и получить понятное recovery message.

## Diagnostics

`DiagnosticsStore` хранит `StructuredDiagnostic` без привязки к CLI, MCP или Editor UI. Установка diagnostics публикует `diagnostics.updated` event и обновляет workspace revision, если набор diagnostics изменился.

## Acceptance criteria

- Specification описывает `DocumentStore`, `CommandBus`, `ChangeEventStream`, `RevisionStore`, `OperationJournal`, `UndoRedo`, `ImportState`, `BuildState` и `DiagnosticsStore`.
- Один project root может получить только один `EditorPrimary`; второй Editor получает `EditorReadOnly` или clear refusal.
- Lease heartbeat обновляется и stale lease может быть заменён новым primary owner.
- Headless workspace создаётся тем же core-кодом без primary Editor lease.
- Открытые scenes/resources/settings/code documents имеют `PersistedRevision`, `InMemoryRevision`, dirty state, `documentRevisions`, `dirtyDocuments` и `persistenceState`.
- Mutating command с неверным `expectedRevision` fail-closed и не меняет документ.
- In-memory subscribers получают `document.opened`, `document.changed`, `document.persisted` и `diagnostics.updated`.
- Operation journal поддерживает started/completed entries, unfinished transaction marker, recovery dirty snapshot и recovery message.
- Diagnostics store принимает `StructuredDiagnostic` и публикует change event.
- Focused tests покрывают open project, document mutation, revision/dirty state, event stream, ownership/read-only/headless и recovery journal.
- Implementation documentation описывает фактическое поведение и команду проверки.

## Фактическое состояние, ограничения и проверки

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
- `UndoRedo` — in-memory store reversible undo/redo groups с before/after snapshots открытых text documents;
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

## Undo/redo

`ProjectWorkspaceUndoRedoStore` хранит reversible groups. Группа содержит `UndoGroupId`, provenance исходной операции и before/after snapshots affected documents. `UndoLast(...)` возвращает все documents группы к before snapshots, `RedoLast(...)` применяет after snapshots.

Если несколько операций подряд добавляют тот же `UndoGroupId`, store объединяет их в одну группу: первый before snapshot остаётся началом группы, последний after snapshot становится итогом. Это используется external synchronizer-ом для debounce batch.

## Diagnostics и recovery

`DiagnosticsStore.SetDiagnostics(...)` заменяет diagnostics для source key, увеличивает `WorkspaceRevision`, публикует `DiagnosticsUpdated` и записывает operation journal entry. `ContentRevision` при этом не меняется, потому что diagnostic update не меняет игровые/project source documents.

`OperationJournal.BeginTransaction(...)` создаёт unfinished transaction marker. `RecordRecoverySnapshot(...)` сохраняет in-memory snapshot dirty-документов с path, text, persisted revision и in-memory revision. `CompleteTransaction(...)` закрывает marker и проставляет completion timestamp.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectWorkspaceTests
```

Эта проверка покрывает ownership/read-only/headless режимы, stale lease replacement, document dirty/revision state, event stream, diagnostics store и recovery journal.
