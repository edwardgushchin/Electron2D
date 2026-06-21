# CanvasItem render queue baseline

Статус: реализовано.
Задача: `T-0024`.
Обновлено: 2026-06-21.

## Назначение

В `0.1.0 Preview` появился internal render queue для `CanvasItem` submissions. Public `CanvasItem`, `Node2D`, `Sprite2D` и `CanvasLayer` реализованы отдельным baseline и используют эту очередь через internal submission context.

Текущий baseline решает две задачи renderer pipeline:

- детерминированно сортирует canvas item команды;
- строит contiguous batches, чтобы совместимые соседние команды превращались в меньшее число draw calls.

## Internal model

`CanvasItemRenderQueue` принимает `CanvasItemRenderCommand`. Команда содержит:

- `Rid` canvas item;
- batch key: texture, material, clip и blend mode;
- layer;
- z-index;
- y-sort flag и y-position;
- tree order;
- visible flag;
- modulate и self-modulate.
- transform;
- source rect;
- destination rect;
- flip flags;
- debug name для диагностических тестов.

`EffectiveModulate` вычисляется как `Modulate * SelfModulate`. Public nodes передают в команду уже вычисленную inherited visibility/modulate chain.

## Sort order

`BuildPlan()` фильтрует invisible commands и сортирует оставшиеся по:

1. layer;
2. z-index;
3. y-position, если y-sort включён;
4. tree order;
5. insertion order как stable tie-breaker.

Y-sort не пробивает z-index: item с меньшим z-index рисуется раньше, даже если его y-position больше.

## Batching

Batch создаётся только для соседних команд с одинаковым `CanvasItemBatchKey`. Очередь не переупорядочивает команды ради batching, поэтому draw order остаётся первичным правилом.

`CanvasItemRenderPlan.DrawCallCount` равен количеству batches. Тесты фиксируют, что adjacent compatible commands дают меньше draw calls, а одинаковые commands через ordering barrier остаются отдельными batches.

## Ограничения

- Реальный SDL_GPU draw submission ещё не реализован.
- Public drawing methods (`DrawLine`, `DrawRect`, `DrawTexture` и другие) остаются будущим `CanvasItem` API.
- Material/shader resource management остаётся отдельной задачей.

## Проверки

Целевой набор:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
```

Он проверяет empty/clear behavior, invalid RID validation, stable order, y-sort within z-index, visibility filtering, effective modulate и batching draw-call count.
