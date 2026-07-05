# Human-AI concurrent editing, conflicts и grouped Undo

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `0.1-preview`.
Задача: `T-0143`.
Связанные документы: [Agent-native workflow](../architecture/agent-native-workflow.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceTransactionEngine](workspace-transactions.md); [External Change Synchronizer](external-change-synchronizer.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md).

Эта спецификация фиксирует минимальный проверяемый слой совместного редактирования человека и AI поверх `ProjectWorkspace`. `ProjectWorkspace` здесь означает внутреннюю живую модель проекта: открытые документы, ревизии, dirty state, журнал операций, import state, diagnostics и общий transaction engine. Слой не рисует Editor UI сам, но обязан возвращать enough state для будущих conflict panel, Agent Workspace и Tooling/MCP adapters.

## Document revisions

Каждый открытый текстовый project document имеет:

- stable `DocumentId`;
- `PersistedRevision`;
- `InMemoryRevision`;
- dirty state как отличие persisted/in-memory revision;
- canonical snapshot для structural diff.

Изменяющие операции над уже открытым документом обязаны передавать `expectedRevision`. Единственное исключение — external transaction, где incoming file state сначала сравнивается с persisted baseline и текущим dirty state.

## Merge policy

`WorkspaceTransactionEngine.ExternalImport` выполняет three-way сравнение:

1. persisted baseline;
2. текущий in-memory document;
3. incoming file state.

Для scene/resource/settings JSON безопасно объединяются только непересекающиеся property changes. Изменение одного свойства двумя участниками возвращает `WorkspaceTransactionConflictKind.PropertyConflict`. Удаление объекта, изменённого другим участником, возвращает `WorkspaceTransactionConflictKind.DeletedChangedObject`. Неподдержанные structural changes возвращают conflict record, а не применяются догадкой.

Для C# и обычных text buffers минимальный `0.1-preview` слой должен либо выполнить безопасный text merge, либо вернуть conflict. Молчаливая перезапись dirty buffer запрещена.

Generated/cache paths не редактируются и не участвуют в automatic merge. Binary assets не получают automatic content merge.

## Binary assets

Замена или удаление binary asset считается конфликтом, если asset уже известен синхронизатору и используется открытым document через project-relative path или `res://` reference. Такой конфликт:

- не удаляет known file state;
- помечает `ImportState[path] = pending-conflict`;
- возвращает structured diagnostic;
- не добавляет project Undo group.

Новый binary asset без usage conflict может перейти в import state `Importing`.

## Provenance

Каждая применённая project mutation записывает origin в `OperationJournal` через `ProjectWorkspaceActorKind`:

- `Human`;
- `Agent`;
- `Cli`;
- `ExternalFile`;
- `Test`.

Журнал операций отвечает за техническое происхождение изменения. Project task status, acceptance и смысловые заметки остаются в `ProjectTaskManager`.

## Grouped Undo/Redo

`ProjectWorkspaceUndoRedoStore` хранит реальные reversible workspace groups, а не только имена. Undo group содержит:

- `UndoGroupId`;
- `OperationId`;
- `ActorKind`;
- affected documents;
- before/after in-memory document states.

Одна Tooling/MCP transaction с общим `UndoGroupId` отменяется одним `UndoLast(...)` и повторяется одним `RedoLast(...)`, если все документы группы имеют сохранённые snapshots.

Если несколько external file changes применены в одном debounce drain, они получают один batch undo group. Повторное добавление того же `UndoGroupId` в конец undo stack объединяет document states в одну группу: первый `before` сохраняется, последний `after` обновляется.

`build`, `import`, `run`, `test` и `export` jobs не создают project Undo group, потому что они не являются редактированием project source documents.

Удаление и перезапись файлов за пределами in-memory document snapshots обратимы только при наличии backup/snapshot. Если snapshot отсутствует, операция должна быть fail-closed и не объявляться reversible.

## Acceptance criteria

- Открытые scene/resource/settings/code/text documents имеют `DocumentId`, persisted/in-memory revision и dirty state.
- Workspace/document mutations требуют `expectedRevision`; stale revision возвращает structured diagnostic.
- External import объединяет непересекающиеся property changes.
- External import возвращает conflict при изменении одного свойства двумя участниками.
- External import возвращает conflict при удалении объекта, изменённого другим участником.
- Dirty C# buffer не перезаписывается прямым file change без safe merge или conflict.
- Binary asset replace/delete возвращает pending conflict, если asset используется открытым document.
- Operation journal сохраняет provenance для Human/Agent/CLI/ExternalFile/Test.
- Agent/tooling transaction с одним `UndoGroupId` отменяется одним `UndoLast(...)` и повторяется одним `RedoLast(...)`.
- External debounce batch использует один grouped Undo для всех успешно применённых text imports в batch.
- Job-backed build/import/run/test/export operations не добавляют Undo group.
- Документация реализации описывает текущие ограничения и команды проверки.

## Фактическое состояние, ограничения и проверки

Статус: реализованная внутренняя основа для `T-0143`.
Обновлено: 2026-06-23.
Связанные документы: [спецификация concurrent editing](concurrent-editing-and-undo.md); [Live ProjectWorkspace](live-project-workspace.md); [WorkspaceTransactionEngine](workspace-transactions.md); [External Change Synchronizer](external-change-synchronizer.md); [Electron2D.Tooling service boundary](../tooling/tooling-service-boundary.md).

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
