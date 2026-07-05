# WorkspaceTransactionEngine и безопасные project operations

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0125`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1-preview](../releases/0.1-preview.md); [Canonical document model, revision model и structural diff](canonical-document-model.md); [Stable project text formats, migrations и JSON Schema](project-text-formats.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceJob contract и event stream](workspace-jobs.md).

## Назначение

`WorkspaceTransactionEngine` задаёт общий внутренний механизм изменяющих операций проекта. Он работает поверх `ProjectWorkspace`, canonical document model и stable text formats, чтобы операции человека, AI-агента, CLI и внешнего файла не затирали друг друга и возвращали один проверяемый результат.

Этот слой не является Editor UI. Он только создаёт transaction result, diagnostics, undo group, changed files/objects и conflict records. Панель конфликтов редактора, MCP adapter и команды конкретных доменов подключаются поверх этого контракта в отдельных задачах.

## Режимы применения

Обязательные режимы:

- `WorkspaceOnly` — применяет изменения к live workspace, создаёт Undo-группу, помечает документы dirty, но не пишет на диск;
- `SaveAffectedDocuments` — сохраняет уже dirty-документы через temporary files и atomic replace, затем обновляет `PersistedRevision`;
- `HeadlessCommit` — применяет изменения и сохраняет их как одну headless-транзакцию, когда Editor не является интерактивным владельцем;
- `ExternalImport` — принимает текст, уже пришедший с диска, сравнивает его с persisted baseline и пытается объединить с dirty in-memory состоянием.

Каждый режим должен поддерживать `DryRun`. Dry-run выполняет validation, revision checks, path checks и structural diff, возвращает `ChangedFiles`, `ChangedObjects`, `Conflicts` и diagnostics, но не меняет workspace и не пишет файлы.

## Transaction request

Transaction request должен содержать:

- `OperationId`;
- `ActorKind`;
- `OperationKind`;
- `Mode`;
- `DryRun`;
- optional `UndoGroupId`;
- список document edits: project-relative path, `ExpectedRevision`, новый text или external text.

`ExpectedRevision` обязателен для открытого документа. Если текущая `InMemoryRevision` не совпадает с expected revision, transaction должен fail-closed без изменения workspace и файлов.

`OperationId` и `UndoGroupId` нужны для provenance, то есть происхождения операции: кто инициировал изменение, какой transaction создал dirty state и какую группу можно отменить одним будущим Undo.

## Transaction result

Результат должен возвращать:

- `Succeeded`;
- `Mode`;
- `DryRun`;
- `OperationId`;
- `UndoGroupId`;
- `WorkspaceRevision`;
- `ContentRevision`;
- `DocumentRevisions`;
- `PersistedRevisions`;
- `DirtyDocuments`;
- `PersistenceState`;
- `ChangedFiles`;
- `ChangedObjects`;
- `CreatedObjects`;
- `Conflicts`;
- `Diagnostics`;
- `BackupFiles`.

`PersistedRevisions` хранит persisted revision по affected document. Если операция затрагивает один документ, consumer может читать единственное значение из этого словаря как `persistedRevision` для совместимости с более простыми adapter schemas.

## Structural merge и conflicts

`ExternalImport` использует три состояния:

1. persisted baseline — последний текст, который workspace успешно прочитал или сохранил;
2. current in-memory state — dirty state человека или AI;
3. incoming external state — новый текст с диска.

Непересекающиеся property changes одного документа должны объединяться автоматически. Две property-правки считаются непересекающимися, если они относятся к разным `ObjectUid` или разным property paths.

Conflict record создаётся, если:

- current и incoming изменили один `ObjectUid` и один property path по-разному;
- incoming удалил объект, который current изменил;
- current удалил объект, который incoming изменил;
- incoming change не может быть безопасно применён без потери unknown properties.

Conflict record должен содержать document path, conflict kind, object UID, optional property path и человекочитаемое объяснение. UI conflict panel строится позже поверх этих records; transaction engine сам не показывает UI.

## Запись файлов и rollback

`SaveAffectedDocuments` и `HeadlessCommit` должны:

- проверять, что target path остаётся внутри project root;
- запрещать запись generated/cache paths, включая `.electron2d/import-cache/`, `.electron2d/workspaces/`, `bin/`, `obj/` и paths с `/generated/`;
- писать temporary file в той же директории, что и target file;
- заменять существующий target через atomic replace;
- удалять temporary file после успеха или ошибки;
- сохранять backup existing target в `.electron2d/backups/<operation-id>/`;
- не обновлять `PersistedRevision`, если validation или запись не завершилась успешно.

Если validation fails, disk write не должен начинаться. Если запись fails, transaction result должен содержать structured diagnostic, а workspace persisted state не должен помечаться saved.

## Unknown properties и generated/cache protection

Transaction engine не должен молча удалять unknown source fields. Если merge меняет известное property, он должен применять change к текущему JSON tree и сохранять неизвестные top-level и sibling fields.

Scene/resource/project APIs не должны редактировать import cache или generated workspace artifacts. Любая попытка изменить generated/cache path возвращает failed result со structured diagnostic.

## Критерии приёмки

- Есть focused tests на `WorkspaceOnly`: expected revision, dirty state, `documentRevisions`, `persistedRevisions`, changed objects, provenance и grouped undo.
- Есть focused tests на `SaveAffectedDocuments`: dry-run возвращает `ChangedFiles` без записи, actual save пишет через temporary file/replace, обновляет `PersistedRevision`, очищает dirty state и остаётся внутри project root.
- Есть focused tests на `HeadlessCommit`: dry-run не меняет workspace/disk, actual commit применяет и сохраняет изменения, validation error не пишет файл и оставляет workspace state неизменным.
- Есть focused tests на `ExternalImport`: непересекающиеся changes объединяются, unknown properties сохраняются, конфликт одного property и deletion-vs-change возвращает conflict records без молчаливой потери данных.
- Есть focused tests на generated/cache path protection и path escaping.
- Result contract содержит revisions, persistence state, changed files/objects, diagnostics, conflicts, backups и undo group.
- Implementation documentation описывает фактическое поведение, текущие ограничения и focused test command.

## Фактическое состояние, ограничения и проверки

Статус: реализованная внутренняя основа.
Задача: `T-0125`.
Обновлено: 2026-06-22.

## Назначение

`WorkspaceTransactionEngine` реализован в `Electron2D.ProjectSystem` как internal core, то есть внутренняя модель изменяющих операций проекта для будущих Tooling, CLI, MCP, Editor adapters и тестов. Он не является публичным runtime API для игр и не показывает UI конфликтов сам.

Текущий `ProjectWorkspace` создаёт transaction engine в свойстве `Transactions`. Engine работает поверх открытых `ProjectWorkspaceDocument`, revision store, operation journal, undo store и canonical structural diff.

## Request и result

`WorkspaceTransactionRequest` содержит:

- `OperationId`;
- `ActorKind`;
- `OperationKind`;
- `Mode`;
- `DryRun`;
- optional `UndoGroupId`;
- document edits с project-relative path, `ExpectedRevision` и новым text.

`WorkspaceTransactionResult` возвращает:

- `Succeeded`;
- `Mode`;
- `DryRun`;
- `OperationId`;
- `UndoGroupId`;
- `WorkspaceRevision`;
- `ContentRevision`;
- `DocumentRevisions`;
- `PersistedRevisions`;
- `DirtyDocuments`;
- `PersistenceState`;
- `ChangedFiles`;
- `ChangedObjects`;
- `CreatedObjects`;
- `Conflicts`;
- `Diagnostics`;
- `BackupFiles`.

Для отказов используется structured diagnostic `E2D-TOOLING-0002`. Plain string error не является transaction result.

## Режимы

`WorkspaceOnly` проверяет `ExpectedRevision`, parses target text, считает structural diff, применяет изменение к live workspace через existing command bus, добавляет reversible `UndoGroupId` в `WorkspaceUndoRedoStore` и не пишет на диск. Документ остаётся dirty, `PersistedRevision` не меняется.

`SaveAffectedDocuments` сохраняет текущие dirty-документы. Dry-run возвращает `ChangedFiles`, но не пишет файлы и не очищает dirty state. Actual save пишет source file через temporary file и replace, создаёт backup existing file в `.electron2d/backups/<operation-id>/`, затем вызывает `MarkDocumentPersisted(...)`.

`HeadlessCommit` сначала validates edits и parses target text. Dry-run возвращает `ChangedFiles` без изменения workspace/disk. Actual commit пишет target files через тот же atomic writer, затем применяет изменения к workspace и сразу mark persisted, поэтому результат становится clean.

`ExternalImport` принимает один changed document text, который уже пришёл с диска. Engine сравнивает persisted baseline, current in-memory state и incoming external state. Непересекающиеся property changes объединяются в текущий JSON tree, unknown top-level и sibling fields сохраняются. Если incoming change редактирует то же property или удаляет объект, уже изменённый в workspace, result содержит `WorkspaceTransactionConflict` и workspace не меняется. Успешный external import с `UndoGroupId` сохраняет before/after snapshots и может быть отменён через `UndoLast(...)`.

## Undo/Redo snapshots

Transaction engine записывает reversible groups только после успешного non-dry-run изменения. Group содержит before/after state каждого affected open text document. Повторная запись того же `UndoGroupId` в хвост store объединяет document states в один group, что позволяет отменить batch внешних изменений одним действием.

`SaveAffectedDocuments` и job-backed операции не добавляют project Undo group. Они меняют persistence или запускают долгую работу, но не являются редактированием project source model.

## Path safety и atomic write

Все paths нормализуются как project-relative paths. Transaction engine отказывает paths с `..`, выходом за project root и generated/cache prefixes:

- `.electron2d/import-cache/`;
- `.electron2d/workspaces/`;
- `.electron2d/context/`;
- `.electron2d/session/`;
- `bin/`;
- `obj/`;
- paths с `/generated/`.

Atomic writer создаёт temporary file рядом с target file. Для существующего target используется replace с backup file внутри `.electron2d/backups/<operation-id>/`. Temporary file удаляется после успеха или ошибки.

## Текущие ограничения

- Engine работает только с уже открытыми text documents.
- Structural merge первой реализации применяет safe property changes для scene node properties, root JSON/settings properties и main resource properties.
- Dirty C# buffers сейчас защищены fail-closed conflict-ом, если текущий text snapshot не может доказать safe merge.
- Unsupported incoming changes возвращают conflict record, а не пытаются угадать merge.
- Event stream остаётся `ProjectWorkspace.Events`; отдельного transaction event stream пока нет.
- Backups создаются для existing target files при save/headless commit; отдельная migration backup policy будет расширена задачами migrations/tooling.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~WorkspaceTransactionTests
```

Тесты покрывают `WorkspaceOnly`, `SaveAffectedDocuments`, `HeadlessCommit`, `ExternalImport`, dry-run, expected revision, real grouped undo/redo, changed files/objects, backups, validation rollback, conflict records, C# dirty buffer conflict, unknown property preservation и generated/cache path protection.
