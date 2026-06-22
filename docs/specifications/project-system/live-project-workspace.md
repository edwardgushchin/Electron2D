# Live ProjectWorkspace

Статус: целевая спецификация.
Задача: `T-0139`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Canonical document model, revision model и structural diff](canonical-document-model.md); [Stable project text formats, migrations и JSON Schema](project-text-formats.md); [Diagnostics.Core](../diagnostics/diagnostics-core.md).

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
