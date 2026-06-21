# Control layout core

Статус: целевая спецификация для `T-0067`.
Дата: 2026-06-22.

## Цель

`Control` должен стать базой для UI-дерева игр и будущего редактора: хранить прямоугольник через anchors/offsets, ограничивать размер minimum size, управлять направлением роста при принудительном увеличении, применять clipping к hit-test потомков и давать проверяемую навигацию фокуса.

Эта задача не реализует контейнеры, готовые виджеты, theme system, tooltips или high-DPI scaling. Она должна дать нижний уровень, на котором следующие UI-задачи смогут строить layout и controls.

## Контракт geometry

- `AnchorLeft`, `AnchorTop`, `AnchorRight`, `AnchorBottom` хранят доли родительского прямоугольника по соответствующим сторонам.
- `OffsetLeft`, `OffsetTop`, `OffsetRight`, `OffsetBottom` хранят пиксельные смещения от anchor-позиций.
- Если родитель - другой `Control`, anchors считаются от `Size` родителя.
- Если прямой или ближайший layout-root является `Viewport`, anchors считаются от `Viewport.Size`.
- `Position` и `Size` остаются публичными удобными свойствами, но отражают вычисленный прямоугольник из anchors/offsets.
- `GetRect()` возвращает локальный прямоугольник `Control`.
- `GetGlobalRect()` возвращает прямоугольник в координатах root viewport.

## Minimum size и grow direction

- `CustomMinimumSize` задаёт пользовательский минимум.
- `_GetMinimumSize()` остаётся override hook для derived controls.
- `GetMinimumSize()` возвращает максимум между `CustomMinimumSize` и `_GetMinimumSize()`.
- `GetCombinedMinimumSize()` возвращает тот же minimum baseline до появления контейнерных theme/stylebox contributions.
- `SetSize()` и `ResetSize()` не должны оставлять control меньше combined minimum size.
- `GrowHorizontal` и `GrowVertical` определяют, в какую сторону расширяется прямоугольник, если requested size меньше minimum size:
  - `Begin` двигает начало назад и сохраняет конец;
  - `End` сохраняет начало и двигает конец;
  - `Both` распределяет недостающий размер между началом и концом.

## Clipping

`ClipContents` ограничивает mouse/touch hit-test потомков прямоугольником текущего control. Рендерный scissor остаётся частью будущего renderer/backend work, но public flag и input clipping должны работать уже в этой задаче.

## Focus navigation

- `FocusNext` и `FocusPrevious` задают явные paths для перехода фокуса.
- `FocusNeighborLeft`, `FocusNeighborTop`, `FocusNeighborRight`, `FocusNeighborBottom` задают directional focus paths.
- `FindNextValidFocus()` и `FindPrevValidFocus()` возвращают следующий focusable `Control` в текущем `Viewport`.
- Если явный path пустой, не найден или указывает на control без доступного focus, используется fallback по порядку обхода UI-дерева.
- Только видимые controls внутри текущего tree и с `FocusMode != None` участвуют в navigation.

## Критерии приёмки

- Есть tests для anchors/offsets относительно `Viewport` и parent `Control`.
- Есть tests для `CustomMinimumSize`, `_GetMinimumSize()`, `GrowDirection` и `ResetSize()`.
- Есть tests, где `ClipContents` не даёт дочернему control получить mouse GUI input за пределами родителя.
- Есть tests для explicit `FocusNext`/`FocusPrevious` и fallback focus order.
- Публичный API задокументирован XML comments и отражён в GitHub Wiki.
