# External Change Synchronizer

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0140`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceTransactionEngine и безопасные project operations](workspace-transactions.md); [ProjectTaskManager, TaskActivity и task storage](project-task-manager.md); [FileSystem dock редактора](../editor/file-system-dock.md); [Resource file baseline, stable UID и ссылки ресурсов](../resources/resource-file-baseline.md); [Import cache ресурсов](../resources/resource-import-cache.md).

## Назначение

`ExternalChangeSynchronizer` отвечает за файлы проекта, которые изменились на диске вне Editor: их мог создать coding agent, CLI, IDE, file sync или другой локальный процесс. Synchronizer должен превратить такое изменение в проверяемое изменение `ProjectWorkspace`, чтобы Editor, FileSystem dock, открытые документы, task board и будущие MCP/IPC adapters видели один согласованный результат.

Synchronizer не является новым источником истины и не обходит доменные правила. Для текстовых документов он использует `WorkspaceTransactionEngine` в режиме `ExternalImport`. Для `.e2task` и `.e2tasks` он использует `ProjectTaskManager` и его проверку переходов/полномочий. Для assets он обновляет live import state, чтобы FileSystem dock мог сразу показать статус `Importing`, `Compiling` или `Error` до завершения полноценного import job.

## Наблюдение и склейка событий

Watcher — это внутренний наблюдатель файловой системы. В рабочем Editor он должен быть рекурсивным: изменения в подпапках project root попадают в одну очередь synchronizer. События не применяются сразу. Они проходят через debounce, то есть короткую задержку после последней записи файла, и coalescing, то есть объединение нескольких событий одного пути в одну операцию. Это нужно, чтобы частичная запись файла не попадала в parser.

Для локальной Tier 1 desktop файловой системы целевой debounce не должен превышать `250 ms`. Acceptance измеряется после стабилизации записи: если файл перестал меняться, text-file change должен попасть в `ProjectWorkspace` не позднее этого окна как P95, то есть в 95% локальных наблюдений.

## Фильтры и подавление собственных записей

Synchronizer обязан игнорировать:

- `.git/`;
- `.electron2d/import-cache/`;
- `.electron2d/workspaces/`;
- `.electron2d/context/`;
- `.electron2d/session/`;
- `bin/`;
- `obj/`;
- временные файлы с суффиксами `.tmp`, `.swp`, `.bak`;
- paths с `/generated/`;
- скрытые temporary-файлы atomic write вида `.name.<id>.tmp`.

Editor и transaction engine могут сами писать файлы через save/headless path. Такие записи регистрируются как self-write suppression: synchronizer получает path и короткое окно подавления, затем игнорирует совпадающее событие, чтобы не импортировать собственный save обратно как внешний конфликт.

## Поддерживаемые изменения

Synchronizer должен поддерживать create, change, move/rename и delete.

Для text documents:

- create открывает новый document в `ProjectWorkspace`, если путь не игнорируется;
- change читает новый текст с диска и применяет `ExternalImport`;
- move/rename сохраняет `DocumentId`, если документ имеет stable UID или reconciliation по hash однозначно связывает старый и новый путь;
- delete закрывает или помечает document как externally deleted, если документ открыт и clean; dirty document не удаляется из памяти и получает conflict diagnostic.

Для scene/resource text files structural diff остаётся ответственностью `WorkspaceTransactionEngine`: безопасные непересекающиеся изменения merge-ятся, конфликт открытого dirty-документа возвращает structured diagnostic и не затирает память.

Для binary assets synchronizer не читает содержимое как text. Он классифицирует путь как asset change, публикует import state `Importing` и возвращает changed file result для FileSystem dock и будущего import job.

## Overflow, ambiguous rename и resume

Если watcher сообщает overflow, synchronizer не запускает полный project rescan. Он сканирует только затронутый каталог, сравнивает known path snapshot с текущим состоянием и создаёт события для добавленных, изменённых и удалённых файлов.

Если rename/move неоднозначен, synchronizer пытается reconciliation: сначала stable UID из text formats, затем content hash для файлов без UID. Если соответствие всё ещё неоднозначно, операция остаётся pending-import/conflict и возвращает diagnostic вместо молчаливого удаления/создания.

После возвращения Editor к работе после паузы synchronizer выполняет лёгкую consistency check: сверяет известные opened/known paths и affected directories, но не обходит весь project root без причины.

## Task documents

`.e2task` и `.e2tasks` являются first-class Editor metadata. External task import всегда использует непривилегированный context:

```text
PrincipalKind = ExternalFile
Capabilities  = Task.EditUnprivilegedFields
Origin        = ExternalImport
```

Внешний task-import должен идти через `TaskTransitionValidator` и `TaskAcceptanceService`, включая future migrations, CLI import и crash recovery. Direct file edit не может принять задачу за человека, менять audit fields или выполнять privileged transitions:

- `AwaitingAcceptance -> Done`;
- `Done -> Ready`;
- `Cancelled -> Backlog`.

Попытка такого изменения возвращает structured diagnostic, переводит путь в `pending-conflict` или pending-import state и не затирает dirty task document.

## Результат операции

Каждый drain очереди возвращает machine-readable result:

- обработанные paths;
- ignored paths;
- changed files;
- changed objects;
- created objects;
- deleted paths;
- moved paths;
- diagnostics;
- conflicts;
- количество directory scans;
- признак, что full project rescan не выполнялся;
- максимальную задержку применённого события.

Editor adapters используют этот result для обновления FileSystem dock, conflict panel, task board и Agent Workspace.

## Критерии приёмки

- Есть отдельный synchronizer contract, который работает поверх `ProjectWorkspace`, `WorkspaceTransactionEngine` и `ProjectTaskManager`, а не вводит параллельный mutable state.
- Есть focused tests на recursive watcher options, debounce и coalescing.
- Есть focused tests на create/change/move/delete text documents через workspace events и revisions.
- Есть focused tests на UID или hash reconciliation при move/rename.
- Есть focused tests на watcher overflow с directory-only scan и на resume consistency check без full project rescan.
- Есть focused tests на self-write suppression.
- Есть focused tests на ignore rules для `.git`, `.electron2d/import-cache`, `bin`, `obj`, temporary files и generated artifacts.
- Есть focused tests, что external text-file change применяется в окне не больше `250 ms` после стабилизации записи.
- Есть focused tests, что новый asset получает live import state `Importing`, `Compiling` или `Error` и видим для FileSystem dock.
- Есть focused tests на `.e2task`/`.e2tasks` create/change/move/delete.
- External task changes импортируются через `ProjectTaskManager` и `WorkspaceTransactionEngine`.
- External task import использует `PrincipalKind.ExternalFile`, capability `Task.EditUnprivilegedFields` и origin `ExternalImport`.
- Privileged task fields/transitions возвращают structured diagnostic и pending-conflict state.
- Dirty task document не затирается external change.
- Implementation documentation описывает фактическое поведение, ограничения и focused test command.

## Фактическое состояние, ограничения и проверки

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
