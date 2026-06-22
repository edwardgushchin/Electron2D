# Scene Tree dock редактора

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
