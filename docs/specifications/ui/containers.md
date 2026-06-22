# UI containers

Статус: целевая спецификация для `T-0068`.
Дата: 2026-06-22.

## Цель

Electron2D `0.1.0 Preview` должен иметь базовый набор UI-контейнеров, достаточный для вложенного layout: общий `Container`, линейные `HBoxContainer`/`VBoxContainer`, табличный `GridContainer`, контейнер внешних отступов `MarginContainer`, центрирующий `CenterContainer` и прокручиваемый `ScrollContainer`.

Контейнеры работают только с прямыми дочерними `Control`. Остальные дочерние `Node` остаются в дереве, но не участвуют в layout. Готовые widgets, scrollbar nodes, tooltips, theme resources и editor-only panels остаются отдельными задачами.

## Общий контракт

- `Container` наследуется от `Control`.
- `Container.FitChildInRect(Control child, Rect2 rect)` размещает прямой дочерний `Control` в локальном прямоугольнике контейнера.
- `Container.QueueSort()` помечает контейнер как требующий пересчёта layout; во время runtime frame контейнер пересчитывает дочерние controls.
- Контейнеры не меняют parent/owner ребёнка и не добавляют compatibility API.
- Контейнеры учитывают только `Control.GetCombinedMinimumSize()` и публичные size flags дочернего control.

## Size flags и theme constants

- `Control.SizeFlagsHorizontal` и `Control.SizeFlagsVertical` задают, как direct parent container распределяет доступное место.
- `SizeFlags.ShrinkBegin` оставляет control в начале выделенного прямоугольника и не растягивает его.
- `SizeFlags.Fill` разрешает растянуть control внутри выделенного прямоугольника.
- `SizeFlags.Expand` участвует в распределении дополнительного места по основной оси контейнера.
- `SizeFlags.ExpandFill` объединяет `Expand` и `Fill`.
- `SizeFlags.ShrinkCenter` и `SizeFlags.ShrinkEnd` размещают control по центру или в конце выделенного прямоугольника, когда `Fill` не задан.
- `SizeFlagsStretchRatio` задаёт вес распределения дополнительного места между expand-children и должен быть положительным finite value.
- `AddThemeConstantOverride()` и `GetThemeConstant()` дают минимальный публичный путь для `separation`, `h_separation`, `v_separation` и `margin_*` значений без полноценной theme system.

## Линейные контейнеры

- `BoxContainer` раскладывает direct `Control` children по одной оси.
- `HBoxContainer` использует горизонтальную ось.
- `VBoxContainer` использует вертикальную ось.
- `BoxContainer.Alignment` двигает всю группу в начало, центр или конец, если свободное место не распределено через `Expand`.
- `BoxContainer.AddSpacer(bool begin)` добавляет растягиваемый `Control` в начало или конец списка детей.
- Theme constant `separation` задаёт расстояние между детьми; default value равен `4`.

## GridContainer

- `GridContainer.Columns` задаёт количество колонок и должен быть больше нуля.
- Дети раскладываются row-major order.
- Ширина каждой колонки равна максимальной minimum width среди детей этой колонки.
- Высота каждой строки равна максимальной minimum height среди детей этой строки.
- Theme constants `h_separation` и `v_separation` задают расстояние между колонками и строками; default value равен `4`.

## MarginContainer

- `MarginContainer` размещает direct `Control` children внутри прямоугольника, уменьшенного на theme constants `margin_left`, `margin_top`, `margin_right`, `margin_bottom`.
- Minimum size контейнера равен максимуму minimum size детей плюс соответствующие отступы.

## CenterContainer

- `CenterContainer` размещает direct `Control` children по центру собственного `Size`.
- `UseTopLeft = true` сохраняет размер ребёнка, но делает top-left corner ребёнка якорем центрирования.

## ScrollContainer

- `ScrollContainer` наследуется от `Container` и по умолчанию включает `ClipContents`.
- `ScrollHorizontal` и `ScrollVertical` задают offset контента и clamp-ятся к доступному диапазону.
- `HorizontalScrollMode` и `VerticalScrollMode` фиксируют preview-level политику: `Disabled` запрещает смещение по оси, остальные значения оставляют ось доступной для программного scroll.
- `EnsureControlVisible(Control control)` принимает descendant control и сдвигает scroll offset так, чтобы его rectangle оказался внутри viewport area.
- Public scrollbar nodes в этой задаче не добавляются, потому что они относятся к задаче готовых controls.

## Критерии приёмки

- Есть tests для `HBoxContainer` и `VBoxContainer`, включая resize, minimum size, separation и expand/fill flags.
- Есть tests для `GridContainer`, `MarginContainer` и `CenterContainer`.
- Есть tests для `ScrollContainer`, clamp scroll offsets и `EnsureControlVisible()`.
- Публичный API имеет XML documentation и отражён в GitHub Wiki.
