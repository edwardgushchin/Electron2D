# Text backend baseline через SDL_ttf

Статус: реализовано.
Задача: `T-0029`, обновлено в `T-0038`.
Обновлено: 2026-06-21.

## Public API

Текущий Electron2D text baseline включает:

- `Font : Resource`;
- `VerticalAlignment`;
- `Control : CanvasItem`;
- `Label : Control`;
- `CanvasItem.DrawString()`.

`Font` поддерживает:

- `GetStringSize()`;
- `GetHeight()`;
- `GetAscent()`;
- `GetDescent()`;
- `HasChar()`.

`Control` содержит локальные `Position` и `Size`, а также минимальные theme overrides для текста:

- `AddThemeFontOverride("font", font)`;
- `GetThemeFont("font")`;
- `AddThemeFontSizeOverride("font_size", size)`;
- `GetThemeFontSize("font_size")`.

`Label` рисует одну строку plain text через `DrawString()` и поддерживает `Text`, `HorizontalAlignment`, `VerticalAlignment` и `Uppercase`.

## Layout

`DrawString()` теперь создаёт internal `TextLayout` при записи draw command. Layout хранит:

- исходный text;
- `TextLayoutDirection` (`LeftToRight` или `RightToLeft`);
- итоговый размер;
- horizontal alignment offset;
- glyph records: Unicode codepoint, выбранный `Font`, glyph position, advance и признак найденного glyph.

Unicode обрабатывается через `System.Text.Rune`, поэтому surrogate pairs считаются одним codepoint. Базовый RTL определяется по Hebrew/Arabic Unicode ranges. Для RTL glyphs в layout идут в визуальном порядке справа налево.

`HorizontalAlignment.Center` и `Right` смещают glyphs внутри заданной ширины. `Fill` в `0.1.0 Preview` пока ведёт себя как left alignment: распределение пробелов и complex shaping не реализованы.

## Fallback и cache

`Font` имеет internal fallback hooks. Если primary font не содержит glyph, layout выбирает первый fallback font, который его содержит. Если glyph не найден нигде, record остаётся в layout с primary font и `GlyphAvailable = false`.

Каждый `Font` владеет internal layout cache. Повторный layout с тем же text/alignment/width/font size и тем же generation возвращает тот же `TextLayout` instance. Cache доступен тестам через internal API, но не является public surface.

## SDL_ttf boundary

Добавлена внутренняя граница `ISdlTtfApi`/`SdlTtfApi` поверх SDL3-CS:

- open/close font;
- glyph availability;
- string size;
- fallback font;
- GPU text engine;
- text object creation.

Публичный API Electron2D не раскрывает SDL_ttf handles. Начиная с `T-0033`, Compatibility backend преобразует text commands в `SDL_ttf_RenderText`-совместимые internal frame commands. Реальные SDL_ttf/SDL_Renderer draw calls остаются следующей integration task; текущий результат готовит command data для этого backend path.

## Профили рендеринга

`Compatibility`, `Standard` и internal `SDL_GPU` profiles продолжают объявлять `RenderingServer.RenderingFeature.Text`. Тесты проверяют, что `Label` submit работает в `Compatibility` и `Standard`.

## Ограничения

- TTF/OTF import metadata реализован в `T-0038` как internal import cache importer. Нет публичного `FontFile`, public `ResourceLoader` или real glyph rasterization pipeline.
- Нет многострочного layout, wrapping, clipping, overrun policy и bidi shaping.
- Нет реального raster/GPU draw call для text; Compatibility backend пока получает готовый layout в command stream и сериализует его для golden-data проверки.
- `Label` поддерживает только одну строку plain text и theme overrides `font`/`font_size`.

## Проверки

Фокусные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~TextPublicApiTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~TextLayoutSubmissionTests"
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~TextLayoutGoldenTests"
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
