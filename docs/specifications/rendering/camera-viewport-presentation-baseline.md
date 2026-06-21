# Camera2D, Viewport and presentation baseline

## Назначение

`T-0027` вводит baseline камеры и viewport presentation для `0.1.0 Preview`.

Задача закрывает:

- public `Camera2D` для текущей 2D camera, zoom, offset и screen center/rotation queries;
- public `Viewport` для root/subviewport baseline, active 2D camera, visible rect, canvas transform и pixel snapping flags;
- internal presentation plan для resize, stretch modes, integer scaling и high-DPI framebuffer scale;
- применение camera transform и pixel snapping к internal sprite submission.

## Источники поведения

- [Godot `Camera2D`](https://docs.godotengine.org/en/stable/classes/class_camera2d.html): camera has `enabled`, `ignore_rotation`, `offset`, `zoom`, `make_current()`, `clear_current()`, `is_current()`, `get_screen_center_position()`, `get_screen_rotation()` and `get_target_position()`.
- [Godot `Viewport`](https://docs.godotengine.org/en/stable/classes/class_viewport.html): viewport exposes active `get_camera_2d()`, `canvas_transform`, `get_visible_rect()`, `snap_2d_transforms_to_pixel` and `snap_2d_vertices_to_pixel`.
- [Godot multiple resolutions](https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html): stretch scale can be fractional or integer; `viewport` stretch mode renders to a lower-resolution target when scale is greater than 1; high-DPI displays require explicit scale handling.

## Public API

`Camera2D`:

```csharp
public class Camera2D : Node2D
{
    public bool Enabled { get; set; }
    public bool IgnoreRotation { get; set; }
    public Vector2 Offset { get; set; }
    public Vector2 Zoom { get; set; }

    public void Align();
    public void ClearCurrent(bool enableNext = true);
    public void ForceUpdateScroll();
    public Vector2 GetScreenCenterPosition();
    public float GetScreenRotation();
    public Vector2 GetTargetPosition();
    public bool IsCurrent();
    public void MakeCurrent();
    public void ResetSmoothing();
}
```

Defaults:

- `Enabled == true`;
- `IgnoreRotation == true`;
- `Offset == Vector2.Zero`;
- `Zoom == Vector2.One`.

Smoothing methods are no-op in `0.1.0 Preview`, because smoothing itself is outside this baseline. They exist only when they have observable camera state or documented no-op behavior covered by tests.

`Viewport`:

```csharp
public class Viewport : Node
{
    public Vector2I Size { get; set; }
    public Transform2D CanvasTransform { get; set; }
    public bool Snap2DTransformsToPixel { get; set; }
    public bool Snap2DVerticesToPixel { get; set; }

    public Camera2D? GetCamera2D();
    public Rect2 GetVisibleRect();
}
```

`SceneTree.Root` remains typed as `Node` for compatibility with the current object-model baseline, but the instance created by `SceneTree` becomes a `Viewport` named `root`.

All new public types and members must have XML documentation in SDL-like C# style: `summary`, `remarks` when needed, `param`, `returns`, `threadsafety`, `since` and `seealso` for related API.

## Camera behavior

`Camera2D.MakeCurrent()` makes the camera active on the nearest ancestor `Viewport`. If the camera is not under a `Viewport`, the method throws `InvalidOperationException`.

When an enabled camera enters a viewport and the viewport has no active camera, it becomes current. When the current camera exits the tree or is disabled, the viewport clears it.

For `0.1.0 Preview`:

- target position is `GlobalPosition + Offset`;
- screen center position equals target position;
- screen rotation is `0` when `IgnoreRotation == true`, otherwise `GlobalRotation`;
- zoom must be finite and non-zero on both axes.

## Viewport and camera transform

The internal final canvas transform for a viewport is:

```text
CanvasTransform * CameraTransform
```

`CameraTransform` maps world coordinates into viewport coordinates:

```text
screen = center + rotate(-camera_rotation) * zoom * (world - target)
```

where:

- `center = Size / 2`;
- `target = Camera2D.GetTargetPosition()`;
- `camera_rotation = Camera2D.GetScreenRotation()`.

If there is no active camera, `CameraTransform` is identity.

## Pixel snapping

`Viewport.Snap2DTransformsToPixel` snaps the submitted transform origin to full pixels after viewport/camera/layer/node transforms are combined.

`Viewport.Snap2DVerticesToPixel` snaps submitted destination rectangle position and end points to full pixels. This is an internal command-level baseline; real GPU vertex snapping remains part of the future draw backend.

Snapping does not mutate node properties. It affects only submission commands.

## Internal presentation model

Минимальная internal surface:

```csharp
internal sealed class ViewportPresentationSettings
{
    Vector2I BaseSize { get; set; }
    Vector2I WindowSize { get; set; }
    float DpiScale { get; set; }
    float StretchScale { get; set; }
    ViewportStretchMode StretchMode { get; set; }
    ViewportStretchAspect StretchAspect { get; set; }
    ViewportStretchScaleMode StretchScaleMode { get; set; }

    ViewportPresentationPlan BuildPlan();
}
```

Internal enums use Godot multiple-resolution terms:

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

`BuildPlan()` returns:

- logical window size;
- framebuffer size after high-DPI scale;
- render target size;
- viewport rectangle inside the framebuffer;
- canvas scale;
- canvas offset;
- final transform used by canvas submission.

## Ограничения `T-0027`

- Real SDL window resize events and swapchain recreation remain part of later renderer/window tasks.
- Camera smoothing, drag margins and limits are not implemented in this baseline.
- `Window` public API is not introduced; project/window settings will be added when display settings are implemented.
- Editor 2D viewport interactions remain `T-0081`.

## Критерии приёмки

- Public API exports include `Camera2D` and `Viewport`, and API compatibility Wiki marks them as implemented partial surface.
- Unit tests cover camera defaults, current-camera lifecycle, target position, screen rotation and viewport visible rect.
- Integration tests cover camera transform in sprite submission, transform/vertex pixel snapping, resize presentation plans, integer scaling and high-DPI framebuffer scale.
- `SceneTree.Root` is a `Viewport` instance while existing root node behavior remains compatible.
- New source files include the MIT License header.
- Documentation under `docs/documentation/rendering/` describes the implemented behavior and limitations.
