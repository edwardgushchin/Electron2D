# Immediate drawing baseline

Статус: реализовано.
Задача: `T-0028`.
Обновлено: 2026-06-21.

## Public API

`CanvasItem` теперь поддерживает Godot-like custom drawing:

- `_Draw()`;
- `QueueRedraw()`;
- `DrawLine()`;
- `DrawRect()`;
- `DrawCircle()`;
- `DrawPolygon()`;
- `DrawTexture()`;
- `DrawString()`.

Для `DrawString()` добавлены public Godot-like типы:

- `Font`;
- `HorizontalAlignment`.

`Font` является abstract `Resource`. После `T-0029` `DrawString()` не ограничивается строковой заглушкой: он создаёт internal text layout с glyph records, fallback font resolution и cache. Реальный raster/GPU draw call остаётся будущей renderer-задачей.

## Redraw

Каждый `CanvasItem` создаётся с queued redraw. Во время `SceneTree.ProcessFrame()` дерево сначала вызывает `_Process()`, затем выполняет draw pass для visible `CanvasItem` nodes.

`QueueRedraw()` помечает item dirty. Несколько вызовов до следующего frame дают один `_Draw()` callback.

Когда `_Draw()` выполняется:

- старые cached draw commands текущего item очищаются;
- вызовы `Draw*` добавляют новые commands в порядке вызова;
- после выхода из `_Draw()` команды остаются активными до следующего redraw.

Вызов `Draw*` вне `_Draw()` бросает `InvalidOperationException`.

## Commands

Internal command stream поддерживает:

- `Line`;
- `Rect`;
- `Circle`;
- `Polygon`;
- `Texture`;
- `String`.

`CanvasSubmissionContext` добавляет cached draw commands в тот же `CanvasItemRenderPlan`, что и `Sprite2D`. Команды получают layer, z-index, y-sort state, tree order, inherited modulate, self-modulate, command color/modulate, transform, debug name и geometry/text/texture fields.

`DrawTexture()` создаёт texture-backed command с source rect по размеру `Texture2D` и destination rect по указанной позиции.

`DrawString()` создаёт text command с `Font`, text, baseline position, alignment, width, font size и internal `TextLayout`. Layout содержит glyph positions, выбранные fallback fonts, basic RTL direction и measured destination rect.

## Golden Data

`tests\Electron2D.Tests.GoldenData\CanvasImmediateDrawingGoldenTests.cs` фиксирует stable text representation для primitive command stream: line, rect и circle. Начиная с `T-0033`, `SdlRendererCompatibilityGoldenTests` дополнительно проверяет reference scene как SDL_Renderer-compatible command stream. Это ещё не golden image rendering; pixel/screenshot output появится после реального renderer presentation.

## Ограничения

- Реальный SDL_GPU primitive renderer ещё не реализован.
- Compatibility profile уже создаёт internal SDL_Renderer-compatible command plan для primitive commands, но не вызывает SDL3-CS renderer functions.
- `DrawString()` создаёт layout и попадает в compatibility text command, но real raster/GPU text draw call ещё не реализован.
- `DrawTextureRect()`, `DrawTextureRectRegion()`, polyline/multiline draw methods и draw transform stack пока не реализованы.
- Public packed arrays не введены; preview signature использует C# arrays для `DrawPolygon()`.

## Проверки

Целевые наборы:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --no-restore
```

Unit tests покрывают public guard вне `_Draw()` и public `Font`/`HorizontalAlignment`.

Integration tests покрывают redraw coalescing, cached commands, draw diagnostics и submission для line/rect/circle/polygon/texture/string. Text-specific integration tests находятся в `TextLayoutSubmissionTests`.

Golden-data tests покрывают стабильный primitive command stream.
