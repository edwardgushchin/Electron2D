# Canvas node submission baseline

## Назначение

`T-0026` вводит первый Godot-like слой видимых 2D nodes поверх уже созданных `Node`, `Texture2D` и internal canvas item render queue.

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

`Position` по умолчанию `Vector2.Zero`, `Rotation` - `0`, `Scale` - `Vector2.One`. `Transform` строится из position/rotation/scale. Setter `Transform` поддерживает decomposition без skew; skew остаётся вне `0.1.0 Preview` subset.

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

## Ограничения `T-0026`

- Реальное SDL_GPU drawing, shader/material API и clipping не реализуются здесь.
- `CanvasItem.QueueRedraw()` и immediate draw methods остаются `T-0028`.
- `Sprite2D` sprite sheet frames (`frame`, `hframes`, `vframes`) остаются будущим animation/resource step.
- Public texture filter/repeat policy может быть добавлена отдельным CanvasItem sampling step, если она нужна до настоящего GPU drawing.

## Критерии приёмки

- Public API exports include `CanvasItem`, `Node2D`, `Sprite2D` and `CanvasLayer`, and API compatibility Wiki marks them as implemented partial surface.
- Unit tests cover default values, show/hide, `IsVisibleInTree()`, transform/global transform, reparent with `keepGlobalTransform`, sprite rect and pixel opacity.
- Integration tests cover submission layer, z-index, tree order, inherited modulate/self-modulate, hidden ancestors and texture batching key.
- New source files include the MIT License header.
- Documentation under `docs/documentation/rendering/` describes the implemented behavior and limitations.
