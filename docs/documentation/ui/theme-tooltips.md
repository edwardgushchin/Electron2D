# Документация реализации: темы, DPI scale и tooltips UI

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
