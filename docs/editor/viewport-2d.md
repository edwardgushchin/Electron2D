# 2D Viewport редактора

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0081`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен получить проверяемую модель 2D Viewport для работы со сценой: навигация по виду, выбор объектов и базовые transform-инструменты. Модель должна работать поверх публичных runtime-узлов `Viewport`, `Node2D`, `Camera2D` и текущего снимка collision shapes без добавления новых публичных типов в runtime assembly.

## Контракт поведения

Viewport редактора должен поддерживать:

- pan: смещение вида в screen coordinates;
- zoom: масштаб вида вокруг выбранной screen point без скачка world point под курсором;
- select: выбор одного объекта по точке;
- multi-select: добавление к выбору по точке и выбор рамкой;
- move: перенос всех выбранных `Node2D`;
- rotate: поворот выбранных узлов вокруг общего pivot;
- scale: масштаб выбранных узлов вокруг общего pivot;
- snapping: привязка move/rotate/scale к заданным шагам;
- pivot: вычисление pivot по центру bounds выбранных объектов или принятие явного pivot;
- bounds: объединённые world bounds выбранных объектов;
- collision shapes: снимок debug collision shapes для overlay;
- camera preview: world rectangle активной `Camera2D` для отрисовки preview рамки.

Все операции должны быть детерминированы и пригодны для automated smoke-check. Viewport модель не должна сохранять scene file напрямую; сохранение transform-изменений в проектный формат выполняют последующие задачи Inspector/scene persistence.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--viewport-2d-smoke <work-root>
```

Smoke-режим должен:

- создать runtime scene с root `Viewport`, несколькими `Node2D`, collision shape и `Camera2D`;
- выполнить pan и zoom;
- выбрать объект по screen point;
- добавить второй объект в multi-selection;
- переместить, повернуть и масштабировать выбранные узлы с включённым snapping;
- вычислить selection bounds;
- получить collision-shape overlay;
- получить camera preview rectangle;
- вывести machine-readable строки: `Pan`, `Zoom`, `Selected`, `PlayerPosition`, `EnemyPosition`, `PlayerRotation`, `EnemyRotation`, `PlayerScale`, `EnemyScale`, `SelectionBounds`, `CollisionOverlays`, `CameraPreview`, `WorldUnderCursorStable`;
- вернуть exit code `0`, если все инварианты выполнены.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --viewport-2d-smoke ...`.
- Тест подтверждает pan/zoom и стабильность world point под курсором при zoom.
- Тест подтверждает single select и multi-select.
- Тест подтверждает move/rotate/scale со snapping для двух выбранных узлов.
- Тест подтверждает selection bounds после трансформаций.
- Тест подтверждает наличие collision-shape overlay.
- Тест подтверждает camera preview rectangle.
- Документация реализации описывает smoke workflow и ограничения.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.

## Фактическое состояние, ограничения и проверки

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
