# 2D Viewport редактора

Статус: документация реализации для `T-0081`.
Дата: 2026-06-22.

## Назначение

2D Viewport в `Electron2D.Editor` отвечает за навигацию по сцене и базовое редактирование transform-ов выбранных 2D узлов. Текущий слой реализует проверяемую модель поведения: pan, zoom, выбор, multi-select, move, rotate, scale, snapping, pivot, bounds, collision-shape overlays и camera preview.

Это внутренняя логика редактора. Она не добавляет новые публичные типы в runtime assembly `Electron2D`.

## Текущее поведение

Viewport model хранит смещение вида и zoom, переводит screen coordinates в world coordinates и обратно, а selectable objects берёт из зарегистрированных `Node2D` вместе с их локальными bounds.

Поддержанные операции:

- `Pan()` сдвигает view offset;
- `ZoomAt()` масштабирует вид вокруг screen point и сохраняет world point под курсором;
- `SelectAt()` выбирает объект по точке;
- `SelectByRect()` выбирает несколько объектов рамкой;
- `MoveSelected()` переносит выбранные узлы;
- `RotateSelected()` поворачивает выбранные узлы вокруг pivot;
- `ScaleSelected()` масштабирует выбранные узлы вокруг pivot;
- snapping применяется к move/rotate/scale, когда включены соответствующие шаги;
- `GetSelectionBounds()` возвращает объединённые world bounds выбранных objects;
- collision overlay берётся из текущего снимка physics debug shapes;
- camera preview вычисляется из `Camera2D` target, zoom и размера viewport.

## Smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --viewport-2d-smoke .temp\editor-viewport-2d
```

Ожидаемый результат включает:

```text
Electron2D.Editor 2D viewport smoke passed
Selected=Player|Enemy
CollisionOverlays=1
WorldUnderCursorStable=True
```

Smoke-команда создаёт runtime scene, выполняет pan/zoom, single select, multi-select, move, rotate, scale, проверяет bounds, collision overlay и camera preview.

## Ограничения

- Текущая задача не создаёт постоянный визуальный layout editor window; она добавляет поведение viewport model и scripted check.
- Текущая задача не сохраняет transform-изменения в scene file. Это должно быть соединено с Inspector и scene persistence в следующих задачах.
- Полноценные gizmo handles, hover-highlight и ручная проверка drag gestures будут добавляться в interactive editor задачах поверх этой модели.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorViewport2DTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
