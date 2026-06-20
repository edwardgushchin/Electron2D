# CanvasItem render queue baseline

## Назначение

`T-0024` вводит internal baseline для сортировки и batching canvas item команд. Это подготовка к public `CanvasItem`, `Node2D`, `Sprite2D` и `CanvasLayer`, которые остаются задачей `T-0026`.

Задача не добавляет публичные node-типы и не добавляет новый public API. Все новые runtime-типы остаются internal и доступны только runtime/tests.

## Источники поведения

- [Godot `CanvasItem`](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html): canvas items рисуются в tree order, `visible` скрывает item и потомков, `modulate`/`self_modulate` меняют цвет, `z_index` и `y_sort_enabled` влияют на draw order.
- [Godot `RenderingServer`](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html): в 2D все видимые объекты являются canvas items, а backend API для них расположен в `canvas_*` методах.

## Internal API

Минимальная internal surface:

```csharp
internal sealed class CanvasItemRenderQueue
{
    int Count { get; }

    void Add(CanvasItemRenderCommand command);
    void Clear();
    CanvasItemRenderPlan BuildPlan();
}
```

Команда:

```csharp
internal readonly struct CanvasItemRenderCommand
{
    Rid CanvasItem { get; }
    CanvasItemBatchKey BatchKey { get; }
    int Layer { get; }
    int ZIndex { get; }
    bool YSortEnabled { get; }
    float YSortPosition { get; }
    long TreeOrder { get; }
    bool Visible { get; }
    Color Modulate { get; }
    Color SelfModulate { get; }
    Color EffectiveModulate { get; }
}
```

`EffectiveModulate` равен `Modulate * SelfModulate`. Future public nodes будут заранее передавать в команду уже вычисленный inherited `Modulate`, потому что T-0024 ещё не строит scene-level `CanvasItem` hierarchy.

Batch key:

```csharp
internal readonly struct CanvasItemBatchKey
{
    Rid Texture { get; }
    Rid Material { get; }
    Rid Clip { get; }
    CanvasItemBlendMode BlendMode { get; }
}
```

`CanvasItemBlendMode` пока internal и нужен только для batching key. Public material/blend API не вводится.

## Сортировка

`CanvasItemRenderQueue.BuildPlan()` должен:

1. отфильтровать команды с `Visible == false`;
2. отсортировать оставшиеся команды по:
   - `Layer`;
   - `ZIndex`;
   - `YSortPosition`, если `YSortEnabled == true`;
   - `TreeOrder`;
   - insertion order как стабильный tie-breaker;
3. сохранить исходные command values без мутации.

Y-sort применяется только внутри одного `ZIndex`: команды с меньшим `ZIndex` всегда рисуются раньше, даже если их `YSortPosition` больше.

## Batching

После сортировки очередь строит contiguous batches. Команды объединяются в один batch только если они идут подряд в итоговом порядке и имеют одинаковый `CanvasItemBatchKey`.

Batching не имеет права менять порядок отрисовки. Если две команды с одинаковым key разделены другой командой, они остаются в разных batches.

`CanvasItemRenderPlan.DrawCallCount` равен количеству batches. Это измеримый критерий T-0024: набор совместимых adjacent commands должен давать меньше draw calls, чем command count.

## Ограничения `T-0024`

- Public `CanvasItem`, `Node2D`, `Sprite2D`, `CanvasLayer` и draw API остаются задачей `T-0026`.
- Texture lifetime/upload остаются задачей `T-0025`.
- Реальное SDL_GPU drawing и shader/material submission не выполняются в T-0024.
- Visibility inheritance через реальные node ancestors не реализуется здесь; future public nodes должны передавать уже вычисленное значение `Visible`.
