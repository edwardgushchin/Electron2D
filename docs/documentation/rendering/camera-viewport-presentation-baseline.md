# Camera2D, Viewport and presentation baseline

Статус: реализовано.
Задача: `T-0027`, обновлено в `T-0030`.
Обновлено: 2026-06-21.

## Public API

В runtime добавлены Godot-like public nodes:

- `Camera2D`;
- `Viewport`;
- `ViewportTexture` через `Viewport.GetTexture()`.

`Camera2D` наследуется от `Node2D` и добавляет:

- `Enabled`;
- `IgnoreRotation`;
- `Offset`;
- `Zoom`;
- `Align()`;
- `ClearCurrent(bool enableNext = true)`;
- `ForceUpdateScroll()`;
- `GetScreenCenterPosition()`;
- `GetScreenRotation()`;
- `GetTargetPosition()`;
- `IsCurrent()`;
- `MakeCurrent()`;
- `ResetSmoothing()`.

`Viewport` наследуется от `Node` и добавляет:

- `Size`;
- `CanvasTransform`;
- `Snap2DTransformsToPixel`;
- `Snap2DVerticesToPixel`;
- `GetCamera2D()`;
- `GetVisibleRect()`;
- `GetTexture()`.

`SceneTree.Root` остаётся публично типизированным как `Node`, но фактический объект root теперь является `Viewport` с именем `root`.

## Camera2D

`Camera2D.Enabled` по умолчанию равен `true`. Если enabled camera входит в subtree `Viewport`, а текущей камеры ещё нет, viewport выбирает её текущей камерой.

`MakeCurrent()` делает камеру текущей на ближайшем ancestor `Viewport`. Если камера не находится под `Viewport`, метод бросает `InvalidOperationException`.

`ClearCurrent(enableNext: true)` очищает текущую камеру и выбирает первую enabled camera в tree order. `ClearCurrent(enableNext: false)` оставляет viewport без текущей камеры.

В текущем baseline:

- `IgnoreRotation` по умолчанию равен `true`;
- `Offset` по умолчанию равен `Vector2.Zero`;
- `Zoom` по умолчанию равен `Vector2.One`;
- `Zoom` принимает только finite и non-zero компоненты;
- `GetTargetPosition()` возвращает `GlobalPosition + Offset`;
- `GetScreenCenterPosition()` возвращает target position;
- `GetScreenRotation()` возвращает `0`, когда `IgnoreRotation == true`, иначе `GlobalRotation`;
- `Align()`, `ForceUpdateScroll()` и `ResetSmoothing()` являются документированными no-op, потому что smoothing, limits и drag margins не входят в `0.1.0 Preview` baseline.

## Viewport

`Viewport.GetVisibleRect()` возвращает `Rect2`, начинающийся в `(0, 0)` и имеющий размер `Viewport.Size`.

`Viewport.GetTexture()` возвращает связанный `ViewportTexture`. Одна и та же texture instance возвращается повторно для одного viewport. `ViewportTexture` наследуется от `Texture2D`, помечается как scene-local resource и отражает текущий `Viewport.Size`.

`Viewport.CanvasTransform` применяется к canvas submission перед camera transform.

Финальный internal canvas transform вычисляется как:

```text
CanvasTransform * CameraTransform
```

`CameraTransform` переводит world coordinates в viewport coordinates:

```text
screen = center + rotate(-camera_rotation) * zoom * (world - target)
```

Если текущей камеры нет, camera transform равен `Transform2D.Identity`.

## Pixel snapping

`Snap2DTransformsToPixel` округляет origin итогового transform в internal render command. Это происходит после viewport, camera, canvas layer и node transform.

`Snap2DVerticesToPixel` округляет position и end point destination rectangle в internal render command.

Оба режима не меняют свойства nodes. Они влияют только на команды, которые строит `CanvasSubmissionContext`.

## Presentation plan

Internal `ViewportPresentationSettings` строит `ViewportPresentationPlan` для resize/high-DPI baseline. Модель закрывает:

- logical window size;
- framebuffer size с учётом `DpiScale`;
- render target size;
- viewport rectangle внутри framebuffer;
- canvas scale;
- canvas offset;
- final canvas transform.

Internal enums используют Godot multiple-resolution terminology:

- `ViewportStretchMode.Disabled`;
- `ViewportStretchMode.CanvasItems`;
- `ViewportStretchMode.Viewport`;
- `ViewportStretchAspect.Ignore`;
- `ViewportStretchAspect.Keep`;
- `ViewportStretchAspect.KeepWidth`;
- `ViewportStretchAspect.KeepHeight`;
- `ViewportStretchAspect.Expand`;
- `ViewportStretchScaleMode.Fractional`;
- `ViewportStretchScaleMode.Integer`.

`CanvasItems` mode масштабирует canvas относительно base size. `Viewport` mode рассчитывает lower-resolution render target при scale больше `1`. Integer scale mode округляет scale вниз и не опускает его ниже `1`.

## Ограничения

- Реальные SDL window resize events и swapchain recreation остаются задачами будущего renderer/window integration.
- Camera smoothing, drag margins и limits не реализованы.
- Public `Window` API не вводится.
- Editor 2D viewport interactions остаются в `T-0081`.

## Проверки

Целевые наборы:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
```

Unit tests покрывают defaults, screen queries, current-camera lifecycle, `SceneTree.Root` как `Viewport`, visible rect, `ViewportTexture` metadata и invalid zoom.

Integration tests покрывают camera transform в sprite submission, transform/vertex pixel snapping без mutation nodes, fractional canvas-items presentation, integer high-DPI viewport presentation и внутреннее восстановление render target resources после пересоздания device.
