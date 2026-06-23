# Scene Tree dock редактора

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0080`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен получить первый Scene Tree dock: внутреннюю логику и UI-control backing для редактирования дерева сцены. Dock должен работать с существующим внутренним форматом `SceneFileDocument`, чтобы операции редактора сохранялись в том же scene JSON, который используют resource/scene serialization tests.

## Контракт данных

- Scene Tree dock открывает и сохраняет `SceneFileDocument` через `SceneFileTextSerializer`.
- Dock не вводит новый scene JSON.
- Root node является единственным node без `parent`.
- Root node нельзя удалить, переименовать в пустое имя или перенести под другой node.
- Для каждого non-root node `owner` должен ссылаться на ancestor node. Если операция делает прежний `owner` недействительным, dock назначает scene root owner.
- При сохранении не должно оставаться ссылок `parent`/`owner` на отсутствующие nodes.

## Операции dock

Dock должен поддерживать:

- add child node под выбранный parent;
- delete selected node вместе с descendants, кроме root;
- rename selected node;
- duplicate selected subtree с новыми local ids и стабильными owner-ссылками;
- reparent selected node;
- drag-and-drop move как редакторную операцию, которая вызывает reparent/move с режимами `into`, `before`, `after`;
- sync в `Tree`/`TreeItem`, чтобы UI-дерево отображало текущий document.

## Undo/redo hooks

Каждая mutating operation должна проходить через undo/redo слой редактора:

- операция сохраняет snapshot до изменения и snapshot после изменения;
- `Undo()` возвращает предыдущий snapshot и обновляет dock tree;
- `Redo()` повторяет snapshot после изменения и обновляет dock tree;
- smoke-проверка должна доказать, что undo восстанавливает удалённый node, а redo снова удаляет его.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--scene-tree-dock-smoke <work-root>
```

Smoke-режим должен:

- создать временный project-like folder в `<work-root>`;
- подготовить `scenes/main.scene.json` в формате `SceneFileDocument`;
- построить Scene Tree dock поверх `Tree`;
- выполнить add, rename, duplicate, drag-and-drop reparent и delete;
- выполнить undo и redo для последней операции;
- сохранить, заново загрузить scene file и проверить ссылки ownership;
- вернуть exit code `0`;
- вывести machine-readable строки: `ScenePath`, `NodeCount`, `InvalidOwnerCount`, `UndoAvailable`, `UndoRestored`, `RedoRemoved`, `TreeRootText`, `ScenePaths`.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --scene-tree-dock-smoke ...`.
- Тест подтверждает, что saved scene JSON остаётся валидным `SceneFileDocument`.
- Тест подтверждает, что итоговое дерево содержит добавленный, переименованный, продублированный и перенесённый subtree.
- Тест подтверждает, что удалённый node отсутствует после redo, а undo до redo его восстанавливал.
- Тест подтверждает, что все `owner` ссылки non-root nodes валидны.
- Документация реализации описывает smoke workflow и ограничения.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0080`.
Дата: 2026-06-22.

## Назначение

Scene Tree dock в `Electron2D.Editor` редактирует дерево сохранённой сцены: добавляет узлы, удаляет subtree, переименовывает узлы, дублирует subtree, переносит узлы и обновляет ownership. Текущий слой хранит данные в `SceneFileDocument` и синхронизирует их с runtime control `Tree`.

Это внутренняя логика редактора. Она не добавляет новые публичные типы в runtime assembly `Electron2D`.

## Текущее поведение

Dock работает с scene file:

```text
scenes/main.scene.json
```

Файл сохраняется через существующий `SceneFileTextSerializer`. После каждой операции dock держит дерево в валидном состоянии:

- root остаётся единственным node без parent;
- parent ссылки указывают только на существующие nodes;
- owner ссылки non-root nodes указывают на ancestor;
- перенос node не создаёт циклы;
- delete удаляет весь subtree.

## Undo/redo

Каждая операция записи проходит через внутренний undo/redo слой редактора. Он хранит снимок сцены до операции и после операции. `Undo()` возвращает предыдущее состояние, `Redo()` повторяет состояние после операции, а dock заново синхронизирует runtime `Tree`.

## Smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --scene-tree-dock-smoke .temp\editor-scene-tree-dock
```

Ожидаемый результат включает:

```text
Electron2D.Editor scene tree dock smoke passed
NodeCount=6
InvalidOwnerCount=0
UndoAvailable=True
UndoRestored=True
RedoRemoved=True
```

Smoke-команда создаёт временную сцену, выполняет add/rename/duplicate/drop/delete, проверяет undo/redo и сохраняет итоговый `SceneFileDocument`.

## Ограничения

- В этой задаче drag-and-drop реализован как редакторная команда переноса node. Полный pointer-driven drag UI добавляется в будущих interactive editor задачах.
- Dock не создаёт визуальный layout постоянного окна редактора; текущая задача добавляет logic и runtime `Tree` backing, чтобы следующие задачи могли встроить его в shell.
- Dock не редактирует свойства node; это область Inspector.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorSceneTreeDockTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
