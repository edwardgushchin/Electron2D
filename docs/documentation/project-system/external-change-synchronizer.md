# External Change Synchronizer

Статус: реализованная внутренняя основа.
Задача: `T-0140`.
Обновлено: 2026-06-22.

## Назначение

`ExternalChangeSynchronizer` реализован в `Electron2D.ProjectSystem` как внутренний слой между событиями файловой системы и живым `ProjectWorkspace`. Его задача — принять изменения файлов, сделанные вне Editor, и провести их через те же правила, которые уже используют workspace transactions, task storage и diagnostics.

Этот слой не является публичным runtime API. Его будут использовать Editor, будущий local IPC/MCP adapter и тесты, чтобы direct file edits от coding agent не расходились с состоянием открытого проекта.

## Текущее поведение

Synchronizer создаётся поверх открытого `ProjectWorkspace` и принимает события `Created`, `Changed`, `Deleted`, `Renamed`, `Overflow` и `Resume`.

Текущая реализация поддерживает:

- recursive watcher wrapper поверх project root с `IncludeSubdirectories = true`;
- debounce, то есть задержку применения события до стабилизации записи;
- coalescing, то есть объединение нескольких событий одного path в одну операцию;
- suppression для собственных записей Editor или transaction engine;
- ignore rules для `.git`, `.electron2d/import-cache`, `.electron2d/workspaces`, `.electron2d/context`, `.electron2d/session`, `bin`, `obj`, temporary files и generated paths;
- directory-only scan при watcher overflow;
- лёгкую resume consistency check по known/opened directories;
- create/change/delete text documents;
- rename/move text documents с сохранением `DocumentId`, если UID или hash позволяют связать старый и новый path;
- asset create/change через live import state `Importing`;
- binary asset replace/delete conflict для используемых ресурсов;
- grouped external Undo для text changes внутри одного debounce batch;
- `.e2task` и `.e2tasks` routing;
- structured diagnostics и pending-conflict state вместо перезаписи dirty documents.

## Routing

Обычные text documents:

- новый file открывается как clean document в `ProjectWorkspace`;
- изменение открытого document применяет `WorkspaceTransactionEngine` в режиме `ExternalImport`;
- conflict records и diagnostics возвращаются в `ExternalChangeSynchronizerResult`;
- dirty document не перезаписывается при конфликте.

Task documents:

- `.e2task` import идёт через `ProjectTaskManager.ImportExternalChange(...)`;
- `.e2tasks` board import идёт через `WorkspaceTransactionEngine.ExternalImport`;
- external task context создаётся synchronizer-ом, а не читается из файла:

```text
PrincipalKind = ExternalFile
Capabilities  = TaskEditUnprivilegedFields
Origin        = ExternalImport
```

Privileged task changes, включая попытку поставить `Done`, direct reopen или перезапись audit fields, возвращают `E2D-TASK-0002` и переводят path в `pending-conflict`.

Assets:

- binary assets не читаются как text;
- synchronizer записывает `workspace.ImportState[path] = "Importing"`;
- `ExternalChangeSynchronizerResult.ImportRecords` содержит `FileSystemDockStatus = "Importing"`;
- текущий FileSystem dock умеет принять live status provider и показать этот статус до завершения полноценного reimport.

Если binary asset уже известен synchronizer-у и используется открытым document через project-relative path или `res://` reference, replace/delete не применяется автоматически. Result получает diagnostic `E2D-TOOLING-0002`, `ImportState[path] = pending-conflict`, known fingerprint сохраняется, а project Undo group не создаётся.

## Grouped external Undo

Каждый `Drain(...)` создаёт batch operation id вида `op-external-batch-*` и matching `undo-external-batch-*`. Успешные text imports внутри одного debounce batch используют этот общий undo group. `ProjectWorkspaceUndoRedoStore` объединяет повторное добавление такого group id, поэтому пользователь может отменить весь batch одним `UndoLast(...)`.

## Result contract

`ExternalChangeSynchronizerResult` возвращает:

- `ProcessedPaths`;
- `IgnoredPaths`;
- `SuppressedPaths`;
- `ChangedFiles`;
- `DeletedPaths`;
- `MovedPaths`;
- `ChangedObjects`;
- `CreatedObjects`;
- `Diagnostics`;
- `Conflicts`;
- `DirectoryScanCount`;
- `ScannedDirectories`;
- `FullProjectRescan`;
- `MaxAppliedDelay`;
- `ImportRecords`.

Этот result предназначен для будущих UI adapters: FileSystem dock, task board, conflict panel и Agent Workspace смогут читать один результат вместо повторной проверки файловой системы.

## Текущие ограничения

- Core tests вызывают `Notify(...)` и `Drain(...)` напрямую; постоянный Editor loop, который вызывает drain по таймеру, будет подключаться в Editor integration tasks.
- FileSystem watcher wrapper уже настраивает recursive observation, но focused tests не зависят от реальных OS-events, чтобы не делать проверку нестабильной.
- Visual conflict panel пока не реализована. Текущий слой возвращает structured diagnostics, transaction conflicts и `pending-conflict` state.
- Binary asset import пока выставляет live status `Importing`; завершённый import job и `Compiling`/`Error` обновляются существующим resource import pipeline или будущим job adapter.
- Binary usage graph сейчас основан на ссылках в открытых documents. Полный resource graph будет расширяться вместе с Editor resource workflow.
- Root-level watcher overflow может потребовать root scan, если underlying platform не сообщает affected directory. Directory-specific overflow не запускает full project rescan.

## Проверка

Focused проверка synchronizer:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ExternalChangeSynchronizerTests
```

Focused проверка FileSystem dock live status:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~EditorFileSystemDockTests
```

Рекомендуемая regression-проверка для этого слоя:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ExternalChangeSynchronizerTests|FullyQualifiedName~EditorFileSystemDockTests|FullyQualifiedName~ProjectWorkspaceTests|FullyQualifiedName~WorkspaceTransactionTests|FullyQualifiedName~ProjectTaskManagerTests"
```

Тесты покрывают recursive watcher contract, debounce/coalescing, create/change/move/delete, ignore rules, self-write suppression, overflow/resume scans, task document guards, dirty conflict, grouped external undo, binary replace/delete pending conflict и visible import state.
