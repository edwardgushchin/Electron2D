# Scene Tree dock редактора

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
