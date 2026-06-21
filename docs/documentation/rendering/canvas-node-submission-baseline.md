# Canvas node submission baseline

Статус: реализовано.
Задача: `T-0026`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлены Godot-like public nodes:

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

## Ограничения

- Реальный SDL_GPU draw submission ещё не реализован.
- `Camera2D`, viewport scaling, pixel snapping и presentation modes остаются `T-0027`.
- `CanvasItem.QueueRedraw()` и immediate drawing API остаются `T-0028`.
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
