# Спецификация: темы, DPI scale и tooltips UI

## Статус
- Задача: `T-0070`
- Статус: утверждено для реализации
- Последнее обновление: `2026-06-22T04:03:00+03:00`

## Контекст
Перед началом редактора UI public API должен быть закрыт как runtime-система, а не как набор частичных controls. `T-0070` добавляет ресурс темы, lookup-цепочку для controls, локальные overrides, базовое DPI scaling поведение и tooltip API.

Эта задача не начинает редактор. Результат должен работать для игровых UI, будущих editor panels и generated GitHub Wiki, где строки UI API после реализации не должны оставаться в статусе `Partial`.

## Область реализации
Нужно добавить публичные runtime-типы:

- `Theme`
- `StyleBox`
- `StyleBoxFlat`

Нужно расширить публичный `Control`:

- `Theme`
- `ThemeTypeVariation`
- `TooltipText`
- `_GetTooltip(Vector2)`
- `_MakeCustomTooltip(string)`
- `GetTooltip(Vector2)`
- theme lookup methods для color, constant, font, font size, icon и style box
- local theme override methods для color, constant, font, font size, icon и style box

Нужно расширить публичный `Viewport`:

- `GuiGetHoveredControl()`

## Публичное поведение

### Theme
- `Theme` наследуется от `Resource`.
- `Theme.DefaultBaseScale` задаёт базовый scale темы. Значение `0` означает, что у темы нет собственного scale и runtime использует fallback `1`.
- `Theme.DefaultFont` и `Theme.DefaultFontSize` используются как fallback для font/font size lookup.
- Theme items хранятся по паре `name` + `themeType`.
- Поддерживаются item kinds: color, constant, font, font size, icon, style box.
- `Set*`, `Get*`, `Has*`, `Clear*` методы должны работать детерминированно и валидировать имена.
- `Theme.Clear()` удаляет все items и default-значения.
- `StyleBoxFlat` может храниться в theme style box items и рисовать прямоугольный фон с рамками.

### Control theme lookup
- Локальный override текущего `Control` имеет самый высокий приоритет.
- Если override отсутствует, lookup ищет ближайший `Theme` в текущем control или в цепочке parent controls.
- Если `Control.ThemeTypeVariation` не пустой, lookup сначала ищет item в этом theme type, затем в фактическом типе control и его UI-base типах.
- Если явный `themeType` передан в `GetTheme*`, lookup использует его перед type chain.
- Missing color возвращает белый цвет.
- Missing constant возвращает `0`.
- Missing font возвращает `null`, если тема не содержит default font.
- Missing font size возвращает `16`, если тема не содержит valid default font size.
- Missing icon и style box возвращают `null`.
- `GetThemeConstant()` и `GetThemeFontSize()` применяют resolved base scale темы. Scale округляется до ближайшего integer, но font size не может стать меньше `1`.

### Tooltip
- `Control.TooltipText` хранит обычный tooltip text.
- `Control._GetTooltip(Vector2)` по умолчанию возвращает `TooltipText`.
- `Control.GetTooltip(Vector2)` вызывает `_GetTooltip(Vector2)` и нормализует `null` в пустую строку.
- `Control._MakeCustomTooltip(string)` может вернуть `Control` для custom tooltip. Базовая реализация возвращает `null`.
- `Viewport.GuiGetHoveredControl()` возвращает последний visible control под mouse/touch pointer после GUI dispatch или `null`, если pointer вне controls.

### Rendering usage
- `Panel` использует theme style box `panel`, если он найден, иначе theme color `panel`, если он задан, иначе текущий default fill.
- `Button` использует state style boxes `normal`, `pressed`, `disabled`, `focus`, если они найдены, иначе state colors `normal_color`, `pressed_color`, `disabled_color`, `focus_color`, если они заданы, иначе текущие default colors.
- Существующие controls, использующие `font` и `font_size`, должны получать значения через общую lookup-цепочку.

## Сериализация
- `Theme` регистрируется в AOT-safe resource metadata.
- Сериализуются `DefaultBaseScale`, `DefaultFontSize`, color items, constant items, font size items и embedded `StyleBoxFlat` items.
- Runtime-ссылки `DefaultFont`, font items и icon items сохраняются в памяти и участвуют в lookup. Их external resource graph будет оформляться общим loader/import контрактом, а не отдельным обходным форматом темы.

## Приемочные критерии
- Добавлены unit-тесты публичного API темы, style boxes, theme overrides и tooltip API.
- Добавлены integration-тесты lookup fallback, DPI scaling, hover tracking и theme serialization.
- `dotnet test src/Electron2D.sln` проходит.
- `dotnet build src/Electron2D.sln -c Release` проходит.
- `tools/Update-ApiWiki.ps1` обновляет GitHub Wiki без локального сайта.
- В `.github/wiki/API-Compatibility.md` строки для API из этой задачи имеют статус `Supported`.
- Активная задача перенесена в `completed-tasks/2026/06 Июнь.md` только после успешных проверок.
