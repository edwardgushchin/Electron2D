# Text backend baseline

Статус: целевая спецификация для `T-0029`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Immediate drawing baseline](immediate-drawing-baseline.md), [Canvas node submission baseline](canvas-node-submission-baseline.md).

## Цель

Electron2D `0.1.0 Preview` должен иметь минимальный текстовый baseline для рендеринга UI и пользовательского immediate drawing:

- `Font` умеет измерять строку, высоту, ascent, descent и наличие Unicode codepoint;
- `CanvasItem.DrawString()` создаёт render command с layout glyphs, fallback font resolution и cache;
- `Label` рисует простой текст через `Control` и тот же `DrawString()` pipeline;
- `Compatibility` и `Standard` профили объявляют поддержку текста через `RenderingServer.RenderingFeature.Text`;
- internal text backend boundary остаётся скрытой от публичного API и готовит будущий real raster/GPU draw path без раскрытия native handles.

## Граница preview baseline

Эта задача реализует не полный text server и не полноценный shaping engine. Она фиксирует минимальный проверяемый baseline, на который позже можно добавить импорт шрифтов, сложные fallback chains, bidi shaping и реальные backend draw calls. TTF/OTF import metadata реализуется отдельной спецификацией `resources/font-import.md`.

## Публичный API

Новый публичный API должен оставаться Electron2D:

- `Font : Resource`
  - `GetStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16)`
  - `GetHeight(int fontSize = 16)`
  - `GetAscent(int fontSize = 16)`
  - `GetDescent(int fontSize = 16)`
  - `HasChar(int charCode)`
- `VerticalAlignment`
  - `Top = 0`
  - `Center = 1`
  - `Bottom = 2`
  - `Fill = 3`
- `Control : CanvasItem`
  - `Position`
  - `Size`
  - `AddThemeFontOverride(string name, Font font)`
  - `GetThemeFont(string name)`
  - `AddThemeFontSizeOverride(string name, int fontSize)`
  - `GetThemeFontSize(string name)`
- `Label : Control`
  - `Text`
  - `HorizontalAlignment`
  - `VerticalAlignment`
  - `Uppercase`

Не допускается добавлять публичные native text handles, публичный `SpriteRenderer`, `IComponent`, Unity-like wrapper или compatibility layer. Внутренние типы layout/cache/backend могут существовать только как implementation detail.

## Layout contract

Text layout выполняется для UTF-8/Unicode текста через `System.Text.Rune`.

Для каждой строки layout хранит:

- исходный текст;
- направление `LeftToRight` или `RightToLeft`;
- итоговый размер;
- горизонтальный alignment offset;
- список glyph records: codepoint, выбранный `Font`, local glyph position, advance и признак найденного glyph.

Минимальные правила:

- `fontSize <= 0` отклоняется исключением.
- `null` text/font/theme name отклоняются исключением.
- `HorizontalAlignment.Left` не смещает glyphs.
- `HorizontalAlignment.Center` и `Right` смещают glyphs внутри `width`, когда `width >= 0`.
- `HorizontalAlignment.Fill` в 0.1 ведёт себя как left alignment без распределения пробелов; это должно быть задокументировано.
- Базовый RTL определяется по Hebrew/Arabic Unicode ranges. Для RTL glyphs в layout идут в визуальном порядке справа налево и получают позиции от правого края строки.
- Если primary font не содержит glyph, layout выбирает первый fallback font, который его содержит.
- Если glyph не найден ни в primary, ни в fallback fonts, команда всё равно сохраняет glyph record с primary font и `GlyphAvailable = false`.

## Cache contract

`Font` владеет внутренним cache layout results. Cache key включает:

- identity `Font`;
- generation font data;
- `text`;
- `HorizontalAlignment`;
- `width`;
- `fontSize`;
- generation fallback fonts.

Повторный `DrawString()` или `Font.GetStringSize()` с тем же ключом должен переиспользовать layout. Cache является внутренним механизмом, доступным тестам через internal API, но не частью public surface.

## Internal text backend boundary

Внутренняя граница text backend должна уметь:

- открыть шрифт из файла;
- закрыть font handle;
- проверить glyph codepoint;
- измерить строку;
- добавить fallback font;
- создать text engine для Standard profile, когда graphics device доступен;
- создать backend text object.

Эта граница не обязана выполнять реальный GPU draw в T-0029. Render commands должны содержать достаточно данных, чтобы следующий backend step мог отрисовать текст без изменения public API.

## Label contract

`Label` использует theme overrides:

- font name: `font`;
- font size name: `font_size`;
- default font size: `16`.

Если theme font не задан, `Label._Draw()` не создаёт draw command. Если font задан:

- `Uppercase = true` рисует `Text.ToUpperInvariant()`;
- `HorizontalAlignment` передаётся в `DrawString()`;
- `VerticalAlignment` выбирает baseline внутри `Size.Y`;
- `Size.X > 0` используется как alignment width, иначе ширина считается неограниченной.

## Проверки

Минимальный acceptance набор:

- unit tests публичного Electron2D API `Font`, `Control`, `Label`, `VerticalAlignment`;
- integration tests render command layout для `DrawString()`;
- integration tests `Label` submission в `Compatibility` и `Standard` профилях;
- Unicode и базовый RTL tests;
- fallback font resolution test;
- cache reuse test;
- golden-data test для стабильного layout summary;
- API compatibility verifier отражает новые public types в GitHub Wiki source;
- source license verifier проходит для новых C# files.
