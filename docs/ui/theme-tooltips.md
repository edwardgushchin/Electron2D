# Спецификация: темы, DPI scale и tooltips UI

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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
- Активная задача перенесена в `data/completed-tasks/2026/06 Июнь.md` только после успешных проверок.

## Фактическое состояние, ограничения и проверки

## Назначение
Темы Electron2D задают переиспользуемое оформление runtime UI: цвета, числовые constants, шрифты, размеры шрифтов, иконки и style boxes. Controls используют тему через локальные overrides и ближайший `Theme` resource в ветке `Control`.

Эта реализация закрывает runtime API для `T-0070` и остаётся частью UI gate перед редактором.

## Theme resource
`Theme` является `Resource`. Theme items адресуются по имени item и theme type. Например, кнопка может запрашивать `normal` style box для type `Button`, а контейнер - `separation` constant для type `HBoxContainer`.

Поддержанные item kinds:
- color
- constant
- font
- font size
- icon
- style box

`DefaultBaseScale`, `DefaultFont` и `DefaultFontSize` используются как fallback, когда конкретный item отсутствует.

## Lookup порядок
`Control` ищет theme values в таком порядке:

1. Локальный override текущего control.
2. Ближайший `Theme`, назначенный на текущий control или parent controls.
3. Default values темы.
4. Runtime fallback values.

Если `ThemeTypeVariation` задан, он проверяется перед фактическим type chain control. Явный `themeType` в `GetTheme*` используется первым.

## DPI scale
`Theme.DefaultBaseScale` масштабирует `GetThemeConstant()` и `GetThemeFontSize()`. Значение `0` означает отсутствие scale у конкретной темы, поэтому runtime использует fallback `1`.

Font size округляется до ближайшего integer и не может стать меньше `1`. Constants округляются до ближайшего integer и не становятся отрицательными.

## Style boxes
`StyleBox` задаёт content margins и публичный `Draw(CanvasItem, Rect2)` hook. `StyleBoxFlat` рисует прямоугольный фон и рамки без текстуры.

`Panel` использует style box `panel`, если он есть в теме. `Button` использует state style boxes `normal`, `pressed`, `disabled`, `focus`.

## Tooltips
`Control.TooltipText` хранит простой tooltip text. `GetTooltip(Vector2)` вызывает virtual `_GetTooltip(Vector2)`, поэтому пользовательский control может вернуть текст в зависимости от локальной позиции pointer.

`_MakeCustomTooltip(string)` возвращает custom `Control` или `null`. Базовая реализация возвращает `null`, а создание, размещение и скрытие визуального tooltip node остаётся задачей более высокого UI layer.

`Viewport.GuiGetHoveredControl()` хранит последний control под pointer после mouse/touch dispatch. Это позволяет runtime и будущим инструментам получать текущий tooltip source без повторного обхода дерева.

## Сериализация
`Theme` регистрируется в AOT-safe metadata registry. Сохраняются:
- `DefaultBaseScale`
- `DefaultFontSize`
- color items
- constant items
- font size items
- embedded `StyleBoxFlat` items

Runtime-ссылки на `Font` и `Texture2D` участвуют в lookup. Их файловое сохранение должно идти через общий resource reference graph, когда он будет подключён к loader/import workflow.

## Проверки
Для задачи должны проходить:

```powershell
dotnet test src/Electron2D.sln
dotnet build src/Electron2D.sln -c Release
pwsh tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues
pwsh tools/Update-ApiWiki.ps1 -OutputPath .github/wiki
pwsh tools/Verify-ApiCompatibility.ps1
```
