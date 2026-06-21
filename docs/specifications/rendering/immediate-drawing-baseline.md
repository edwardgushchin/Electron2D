# Immediate drawing baseline

## Назначение

`T-0028` вводит Godot-like custom drawing baseline для `CanvasItem` в `0.1.0 Preview`.

Задача закрывает:

- public `_Draw()` callback на `CanvasItem`;
- public `QueueRedraw()`;
- public immediate drawing methods `DrawLine()`, `DrawRect()`, `DrawCircle()`, `DrawPolygon()`, `DrawTexture()` и `DrawString()`;
- internal cached draw command list, который попадает в `CanvasSubmissionContext`;
- redraw invalidation: `_Draw()` вызывается не чаще одного раза за frame, даже если `QueueRedraw()` вызван несколько раз;
- минимальный public `Font` и `HorizontalAlignment`, необходимые для Godot-like `DrawString()` signature.

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
- `DrawString()` captures text commands but does not perform glyph layout or SDL_ttf rendering; that belongs to `T-0029`.
- `DrawTextureRect()`, `DrawTextureRectRegion()`, `DrawPolyline()`, `DrawMultiline()` and transform stack APIs are intentionally outside this baseline.
- Public `PackedVector2Array`/`PackedColorArray` types are not introduced in this task; C# arrays are used for this preview signature.

## Критерии приёмки

- Public API exports include `Font` and `HorizontalAlignment`, and API compatibility Wiki marks them as implemented partial surface.
- Unit tests cover `_Draw()`, `QueueRedraw()` coalescing, cached commands and invalid draw calls outside `_Draw()`.
- Integration tests cover line, rect, circle, polygon, texture and string command submission through `CanvasSubmissionContext`.
- New source files include the MIT License header.
- Documentation under `docs/documentation/rendering/` describes the implemented behavior and limitations.
