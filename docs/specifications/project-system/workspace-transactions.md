# WorkspaceTransactionEngine и безопасные project operations

Статус: целевая спецификация для `T-0125`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Canonical document model, revision model и structural diff](canonical-document-model.md); [Stable project text formats, migrations и JSON Schema](project-text-formats.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceJob contract и event stream](workspace-jobs.md).

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
