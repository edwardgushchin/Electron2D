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

`Font` является abstract `Resource`. В текущем baseline он нужен для Godot-like signature и command capture; реальный SDL_ttf backend, glyph layout, fallback и cache остаются задачей `T-0029`.

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

`DrawString()` создаёт text command с `Font`, text, baseline position, alignment, width и font size, но не выполняет glyph layout.

## Golden Data

`tests\Electron2D.Tests.GoldenData\CanvasImmediateDrawingGoldenTests.cs` фиксирует stable text representation для primitive command stream: line, rect и circle. Это не golden image rendering; raster/GPU golden output появится после реального primitive renderer.

## Ограничения

- Реальный SDL_GPU primitive renderer ещё не реализован.
- `DrawString()` не раскладывает glyphs и не обращается к SDL_ttf.
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

Integration tests покрывают redraw coalescing, cached commands, draw diagnostics и submission для line/rect/circle/polygon/texture/string.

Golden-data tests покрывают стабильный primitive command stream.
