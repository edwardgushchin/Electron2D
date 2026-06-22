# WorkspaceTransactionEngine и безопасные project operations

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

`WorkspaceOnly` проверяет `ExpectedRevision`, parses target text, считает structural diff, применяет изменение к live workspace через existing command bus, добавляет `UndoGroupId` в `WorkspaceUndoRedoStore` и не пишет на диск. Документ остаётся dirty, `PersistedRevision` не меняется.

`SaveAffectedDocuments` сохраняет текущие dirty-документы. Dry-run возвращает `ChangedFiles`, но не пишет файлы и не очищает dirty state. Actual save пишет source file через temporary file и replace, создаёт backup existing file в `.electron2d/backups/<operation-id>/`, затем вызывает `MarkDocumentPersisted(...)`.

`HeadlessCommit` сначала validates edits и parses target text. Dry-run возвращает `ChangedFiles` без изменения workspace/disk. Actual commit пишет target files через тот же atomic writer, затем применяет изменения к workspace и сразу mark persisted, поэтому результат становится clean.

`ExternalImport` принимает один changed document text, который уже пришёл с диска. Engine сравнивает persisted baseline, current in-memory state и incoming external state. Непересекающиеся property changes объединяются в текущий JSON tree, unknown top-level и sibling fields сохраняются. Если incoming change редактирует то же property или удаляет объект, уже изменённый в workspace, result содержит `WorkspaceTransactionConflict` и workspace не меняется.

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
- Unsupported incoming changes возвращают conflict record, а не пытаются угадать merge.
- Event stream остаётся `ProjectWorkspace.Events`; отдельного transaction event stream пока нет.
- Backups создаются для existing target files при save/headless commit; отдельная migration backup policy будет расширена задачами migrations/tooling.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~WorkspaceTransactionTests
```

Тесты покрывают `WorkspaceOnly`, `SaveAffectedDocuments`, `HeadlessCommit`, `ExternalImport`, dry-run, expected revision, grouped undo, changed files/objects, backups, validation rollback, conflict records, unknown property preservation и generated/cache path protection.
