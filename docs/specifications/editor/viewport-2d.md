# 2D Viewport редактора

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
