# Canvas node submission baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`T-0026` вводит первый Electron2D слой видимых 2D nodes поверх уже созданных `Node`, `Texture2D` и internal canvas item render queue.

Задача закрывает:

- public `CanvasItem` для видимости, модуляции и 2D draw order;
- public `Node2D` для local/global transform, position, rotation и scale;
- public `Sprite2D` для отправки texture-backed sprite commands;
- public `CanvasLayer` для layer-based 2D draw order;
- internal submission-модель, которая обходит subtree и строит `CanvasItemRenderPlan` без раскрытия renderer internals в public API.

## Источники поведения

- [Godot `CanvasItem`](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html): canvas items рисуются в tree order, `visible` скрывает item и потомков, `modulate` наследуется потомками, `self_modulate` влияет только на сам item, а `z_index` меняет порядок внутри canvas layer.
- [Godot `Node2D`](https://docs.godotengine.org/en/stable/classes/class_node2d.html): 2D node имеет position, rotation, scale, local/global transform и методы `rotate`, `translate`, `to_global`, `to_local`.
- [Godot `Sprite2D`](https://docs.godotengine.org/en/stable/classes/class_sprite2d.html): sprite displays a `Texture2D`, supports `centered`, `offset`, `flip_h`, `flip_v`, `region_enabled`, `region_rect`, `get_rect()` and pixel opacity query.
- [Godot `CanvasLayer`](https://docs.godotengine.org/en/stable/classes/class_canvaslayer.html): descendants of a layer are drawn in that layer; layer order has priority over `CanvasItem.z_index`.

## Public API

`CanvasItem`:

```csharp
public class CanvasItem : Node
{
    public bool Visible { get; set; }
    public Color Modulate { get; set; }
    public Color SelfModulate { get; set; }
    public int ZIndex { get; set; }
    public bool YSortEnabled { get; set; }

    public void Show();
    public void Hide();
    public bool IsVisibleInTree();
}
```

`Visible` по умолчанию равен `true`. `Modulate` и `SelfModulate` по умолчанию равны `Color.White`. `ZIndex` по умолчанию равен `0`.

`Node2D`:

```csharp
public class Node2D : CanvasItem
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public float RotationDegrees { get; set; }
    public Vector2 Scale { get; set; }
    public Transform2D Transform { get; set; }
    public Vector2 GlobalPosition { get; set; }
    public float GlobalRotation { get; set; }
    public float GlobalRotationDegrees { get; set; }
    public Vector2 GlobalScale { get; set; }
    public Transform2D GlobalTransform { get; set; }

    public void ApplyScale(Vector2 ratio);
    public void GlobalTranslate(Vector2 offset);
    public void Rotate(float radians);
    public Vector2 ToGlobal(Vector2 localPoint);
    public Vector2 ToLocal(Vector2 globalPoint);
    public void Translate(Vector2 offset);
}
```

`Position` по умолчанию `Vector2.Zero`, `Rotation` - `0`, `Scale` - `Vector2.One`. `Transform` строится из position/rotation/scale. Setter `Transform` поддерживает decomposition без skew; skew остаётся вне `0.1-preview` subset.

`Sprite2D`:

```csharp
public class Sprite2D : Node2D
{
    public bool Centered { get; set; }
    public bool FlipH { get; set; }
    public bool FlipV { get; set; }
    public Vector2 Offset { get; set; }
    public bool RegionEnabled { get; set; }
    public bool RegionFilterClipEnabled { get; set; }
    public Rect2 RegionRect { get; set; }
    public Texture2D? Texture { get; set; }

    public Rect2 GetRect();
    public bool IsPixelOpaque(Vector2 pos);
}
```

`Centered` по умолчанию равен `true`; остальные flags по умолчанию `false`. Если texture отсутствует, sprite не отправляет draw command.

`CanvasLayer`:

```csharp
public class CanvasLayer : Node
{
    public int Layer { get; set; }
    public Vector2 Offset { get; set; }
    public float Rotation { get; set; }
    public float RotationDegrees { get; set; }
    public Vector2 Scale { get; set; }
    public Transform2D Transform { get; set; }
    public bool Visible { get; set; }

    public Transform2D GetFinalTransform();
    public void Hide();
    public void Show();
}
```

`Layer` по умолчанию равен `1`, как в Godot. Обычные `CanvasItem` вне `CanvasLayer` отправляются в layer `0`.

Все новые public types и members должны иметь XML documentation в SDL-like C# стиле: `summary`, `remarks` когда нужно, `param`, `returns`, `threadsafety`, `since` и `seealso` для связанных API.

## Visibility and modulation inheritance

`CanvasItem.IsVisibleInTree()` возвращает `false`, если сам item скрыт или его direct/indirect `CanvasItem` ancestor скрыт. Если между двумя canvas items стоит обычный `Node`, propagation chain прерывается: нижний canvas item начинает независимую visibility/modulate chain.

Для draw command:

- `Modulate` содержит inherited color chain: parent `Modulate` values умножаются на current `Modulate`;
- `SelfModulate` содержит только current `SelfModulate`;
- `EffectiveModulate` остаётся `Modulate * SelfModulate`;
- children наследуют `Modulate`, но не наследуют `SelfModulate`.

## Transform inheritance

`Node2D.GlobalTransform` равен local `Transform`, если direct parent не является `Node2D`. Если direct parent является `Node2D`, global transform равен `parent.GlobalTransform * Transform`.

`Node.Reparent(newParent, keepGlobalTransform: true)` должен сохранять `GlobalTransform` для `Node2D`. Для non-`Node2D` поведение остаётся прежним: меняется только parent-child связь.

## Internal submission model

Минимальная internal surface:

```csharp
internal sealed class CanvasSubmissionContext
{
    CanvasItemRenderPlan BuildPlan(Node root);
}
```

`BuildPlan()` должен:

1. обойти subtree в tree order;
2. учитывать текущий `CanvasLayer.Layer`, `CanvasLayer.Visible` и `CanvasLayer.Transform`;
3. отправлять команду только для `Sprite2D` с non-null `Texture`;
4. фильтровать скрытые items через уже существующий `CanvasItemRenderQueue`;
5. передавать в `CanvasItemRenderCommand` layer, z-index, tree order, effective modulate chain, node transform, source rect, destination rect and flip flags;
6. создавать стабильный internal texture RID на texture resource reference для batching key.

Submission-модель остаётся internal. Public API не должен раскрывать `CanvasItemRenderQueue`, `CanvasItemRenderCommand`, `CanvasSubmissionContext`, backend interfaces или texture handles.

## Связанный Camera/Viewport baseline

`T-0027` добавляет к `CanvasSubmissionContext` учёт `Viewport.CanvasTransform`, текущей `Camera2D`, transform snapping и vertex snapping. Целевой контракт описан в [Camera2D, Viewport and presentation baseline](camera-viewport-presentation-baseline.md).

## Связанный Immediate Drawing baseline

`T-0028` добавляет к `CanvasSubmissionContext` cached draw commands из `CanvasItem._Draw()`: line, rect, circle, polygon, texture и string. Целевой контракт описан в [Immediate drawing baseline](immediate-drawing-baseline.md).

## Ограничения `T-0026`

- Реальное SDL_GPU drawing, shader/material API и clipping не реализуются здесь.
- `Sprite2D` sprite sheet frames (`frame`, `hframes`, `vframes`) остаются будущим animation/resource step.
- Public texture filter/repeat policy может быть добавлена отдельным CanvasItem sampling step, если она нужна до настоящего GPU drawing.

## Критерии приёмки

- Public API exports include `CanvasItem`, `Node2D`, `Sprite2D` and `CanvasLayer`, and API compatibility Wiki marks them as implemented partial surface.
- Unit tests cover default values, show/hide, `IsVisibleInTree()`, transform/global transform, reparent with `keepGlobalTransform`, sprite rect and pixel opacity.
- Integration tests cover submission layer, z-index, tree order, inherited modulate/self-modulate, hidden ancestors and texture batching key.
- New source files include the MIT License header.
- Documentation under `docs/rendering/` describes the implemented behavior and limitations.

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задача: `T-0026`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлены Electron2D public nodes:

- `CanvasItem`;
- `Node2D`;
- `Sprite2D`;
- `CanvasLayer`.

`CanvasItem` наследуется от `Node` и добавляет:

- `Visible`;
- `Modulate`;
- `SelfModulate`;
- `ZIndex`;
- `YSortEnabled`;
- `Show()`;
- `Hide()`;
- `IsVisibleInTree()`.

`Node2D` наследуется от `CanvasItem` и добавляет local/global transform API:

- `Position`;
- `Rotation`;
- `RotationDegrees`;
- `Scale`;
- `Transform`;
- `GlobalPosition`;
- `GlobalRotation`;
- `GlobalRotationDegrees`;
- `GlobalScale`;
- `GlobalTransform`;
- `ApplyScale()`;
- `GlobalTranslate()`;
- `Rotate()`;
- `ToGlobal()`;
- `ToLocal()`;
- `Translate()`.

`Sprite2D` наследуется от `Node2D` и добавляет:

- `Texture`;
- `Centered`;
- `Offset`;
- `FlipH`;
- `FlipV`;
- `RegionEnabled`;
- `RegionFilterClipEnabled`;
- `RegionRect`;
- `GetRect()`;
- `IsPixelOpaque()`.

`CanvasLayer` наследуется от `Node` и добавляет:

- `Layer`;
- `Offset`;
- `Rotation`;
- `RotationDegrees`;
- `Scale`;
- `Transform`;
- `Visible`;
- `GetFinalTransform()`;
- `Hide()`;
- `Show()`.

## Visibility And Modulate

`CanvasItem.Visible` по умолчанию равен `true`. `Hide()` и `Show()` меняют это свойство.

`CanvasItem.IsVisibleInTree()` учитывает только прямую цепочку `CanvasItem` ancestors. Если между двумя canvas nodes находится обычный `Node`, inherited visibility chain прерывается, и нижний `CanvasItem` становится независимым от верхнего.

`Modulate` наследуется direct canvas descendants. `SelfModulate` применяется только к текущему item и не наследуется детьми. Во время submission команда получает:

- `Modulate`: уже умноженная inherited chain;
- `SelfModulate`: значение текущего item;
- `EffectiveModulate`: `Modulate * SelfModulate`.

## Transform

`Node2D.Transform` строится из `Position`, `Rotation` и `Scale`. Setter `Transform` раскладывает transform обратно на эти три значения для subset без skew.

`Node2D.GlobalTransform` учитывает только direct `Node2D` parent. Обычный `Node` между двумя `Node2D` прерывает transform chain. `Node.Reparent(newParent, keepGlobalTransform: true)` сохраняет `GlobalTransform` для `Node2D`.

`CanvasLayer.Transform` строится из `Offset`, `Rotation` и `Scale`. В текущем baseline `GetFinalTransform()` возвращает этот transform без viewport/camera поправок.

## Sprite Submission

Internal `CanvasSubmissionContext` обходит subtree и строит `CanvasItemRenderPlan` через существующий `CanvasItemRenderQueue`.

Submission отправляет command только для `Sprite2D` с non-null `Texture`. В command попадают:

- canvas item RID;
- stable internal texture RID для batching key;
- layer;
- z-index;
- y-sort flag и y-position;
- tree order;
- visibility;
- inherited modulate и self-modulate;
- node transform;
- source rect;
- destination rect;
- flip flags;
- debug name из `Node.Name`.

Обычные canvas items вне `CanvasLayer` попадают в layer `0`. Descendants `CanvasLayer` попадают в layer этого `CanvasLayer`; layer order имеет приоритет над `ZIndex`.

## Связанный Camera/Viewport baseline

Начиная с `T-0027`, `CanvasSubmissionContext` учитывает `Viewport.CanvasTransform`, текущую `Camera2D`, `Viewport.Snap2DTransformsToPixel` и `Viewport.Snap2DVerticesToPixel`. Подробности описаны в [Camera2D, Viewport and presentation baseline](camera-viewport-presentation-baseline.md).

## Связанный Immediate Drawing baseline

Начиная с `T-0028`, `CanvasSubmissionContext` также отправляет cached draw commands из `CanvasItem._Draw()`: line, rect, circle, polygon, texture и string. Подробности описаны в [Immediate drawing baseline](immediate-drawing-baseline.md).

## Связанный Compatibility backend baseline

Начиная с `T-0033`, `CompatibilityRenderingBackend` принимает `CanvasItemRenderPlan` и строит internal `SdlRendererFramePlan`. Этот plan покрывает `Sprite2D`, UI text через `Label`, immediate primitives, `DrawTexture()` и tile-like texture copies как SDL_Renderer-compatible command stream. Подробности описаны в [SDL_Renderer Compatibility backend baseline](sdl-renderer-compatibility-backend.md).

## Ограничения

- Реальный SDL_GPU draw submission ещё не реализован.
- Compatibility profile уже строит SDL_Renderer-compatible command plan, но real-window SDL_Renderer presentation ещё не реализован.
- Sprite sheet frame API (`frame`, `hframes`, `vframes`) пока не реализован.
- Public texture filter/repeat ещё не вынесены в `CanvasItem`; текущие GPU sampling descriptors остаются internal до настоящего drawing pipeline.

## Проверки

Целевые наборы:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
```

Unit tests покрывают defaults, show/hide, visibility inheritance, transforms, global transforms, `Reparent(..., keepGlobalTransform: true)`, sprite rect, region и pixel opacity.

Integration tests покрывают layer/z/tree order, hidden canvas items/layers, inherited modulate/self-modulate, transform/source/destination rects, flip flags и texture batching key.
