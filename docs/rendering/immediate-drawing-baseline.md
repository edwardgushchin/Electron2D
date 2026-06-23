# Immediate drawing baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`T-0028` вводит Electron2D custom drawing baseline для `CanvasItem` в `0.1.0 Preview`.

Задача закрывает:

- public `_Draw()` callback на `CanvasItem`;
- public `QueueRedraw()`;
- public immediate drawing methods `DrawLine()`, `DrawRect()`, `DrawCircle()`, `DrawPolygon()`, `DrawTexture()` и `DrawString()`;
- internal cached draw command list, который попадает в `CanvasSubmissionContext`;
- redraw invalidation: `_Draw()` вызывается не чаще одного раза за frame, даже если `QueueRedraw()` вызван несколько раз;
- минимальный public `Font` и `HorizontalAlignment`, необходимые для Electron2D `DrawString()` signature.

## Источники поведения

- [Godot `CanvasItem`](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html): `_draw()` вызывается после redraw request; draw methods работают в local space; `queue_redraw()` coalesces redraw once per frame.
- [Godot custom drawing in 2D](https://docs.godotengine.org/en/stable/tutorials/2d/custom_drawing_in_2d.html): draw commands are cached and `_draw()` is called again only after `queue_redraw()`.
- [Godot `Font`](https://docs.godotengine.org/en/stable/classes/class_font.html): string drawing uses `Font` and baseline positioning; actual glyph layout/rendering remains a text backend responsibility.

## Public API

`CanvasItem` получает:

```csharp
public virtual void _Draw();

public void QueueRedraw();
public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false);
public void DrawRect(Rect2 rect, Color color, bool filled = true, float width = -1f, bool antialiased = false);
public void DrawCircle(Vector2 position, float radius, Color color, bool filled = true, float width = -1f, bool antialiased = false);
public void DrawPolygon(Vector2[] points, Color[] colors, Vector2[]? uvs = null, Texture2D? texture = null);
public void DrawTexture(Texture2D texture, Vector2 position, Color? modulate = null);
public void DrawString(
    Font font,
    Vector2 position,
    string text,
    HorizontalAlignment alignment = HorizontalAlignment.Left,
    float width = -1f,
    int fontSize = 16,
    Color? modulate = null);
```

`Font`:

```csharp
public abstract class Font : Resource
{
}
```

`HorizontalAlignment`:

```csharp
public enum HorizontalAlignment
{
    Left = 0,
    Center = 1,
    Right = 2,
    Fill = 3
}
```

All new public types and members must have XML documentation in SDL-like C# style: `summary`, `remarks` when needed, `param`, `returns`, `threadsafety`, `since` and `seealso` for related API.

## Redraw behavior

Each `CanvasItem` starts with redraw queued. During `SceneTree.ProcessFrame()` the tree processes `_Process()` first, then redraws queued visible `CanvasItem` nodes in tree order.

`QueueRedraw()` marks the item dirty. Multiple calls before the next frame still result in a single `_Draw()` callback.

When `_Draw()` runs:

- previous cached commands for that item are cleared;
- draw methods append commands in call order;
- after `_Draw()` returns, cached commands stay active until the next redraw.

If a visible item is not queued for redraw, its cached commands are reused.

Draw methods are valid only while the item is executing `_Draw()`. Calling them outside `_Draw()` throws `InvalidOperationException`.

## Submission behavior

`CanvasSubmissionContext` submits cached drawing commands together with existing `Sprite2D` commands.

Each command keeps:

- command kind;
- owning canvas item RID;
- layer, z-index, y-sort state, tree order and visibility;
- inherited `Modulate`, `SelfModulate`, effective transform and debug name;
- local-space geometry converted by the same viewport/camera/layer/node transform path used by sprites;
- primitive parameters such as line width, filled flag, antialias flag, polygon points/colors/uvs, texture, font, text, alignment, text width and font size.

Texture-backed commands receive a stable internal texture RID in the batch key. Untyped shape/string commands do not expose backend resources and remain internal.

## Ограничения `T-0028`

- Реальный SDL_GPU primitive renderer, shader/material handling and golden image raster output remain future renderer work.
- `DrawString()` text layout, fallback and cache belong to `T-0029`; real raster/GPU text drawing remains future renderer work.
- `DrawTextureRect()`, `DrawTextureRectRegion()`, `DrawPolyline()`, `DrawMultiline()` and transform stack APIs are intentionally outside this baseline.
- Public `PackedVector2Array`/`PackedColorArray` types are not introduced in this task; C# arrays are used for this preview signature.

## Критерии приёмки

- Public API exports include `Font` and `HorizontalAlignment`, and API compatibility Wiki marks them as implemented partial surface.
- Unit tests cover `_Draw()`, `QueueRedraw()` coalescing, cached commands and invalid draw calls outside `_Draw()`.
- Integration tests cover line, rect, circle, polygon, texture and string command submission through `CanvasSubmissionContext`.
- New source files include the MIT License header.
- Documentation under `docs/rendering/` describes the implemented behavior and limitations.

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задача: `T-0028`.
Обновлено: 2026-06-21.

## Public API

`CanvasItem` теперь поддерживает Electron2D custom drawing:

- `_Draw()`;
- `QueueRedraw()`;
- `DrawLine()`;
- `DrawRect()`;
- `DrawCircle()`;
- `DrawPolygon()`;
- `DrawTexture()`;
- `DrawString()`.

Для `DrawString()` добавлены public Electron2D типы:

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
