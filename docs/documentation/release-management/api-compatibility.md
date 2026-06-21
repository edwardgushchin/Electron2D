# Таблица совместимости Godot-like API

Статус: реализованная проверка compatibility baseline.
Задача: `T-0004`.
Обновлено: 2026-06-21.

## Где находится таблица

Compatibility table хранится как GitHub Wiki source:

```text
.github/wiki/API-Compatibility.md
```

Это не локальный сайт и не generated documentation portal. Файл предназначен для публикации в GitHub Wiki проекта.

## Текущий baseline

Новый runtime assembly `Electron2D` экспортирует текущий Godot-like baseline объектной модели, 2D math, RNG, identity, Variant value carrier, texture/canvas/camera и immediate drawing surface:

- `Electron2D.AtlasTexture`
- `Electron2D.Callable`
- `Electron2D.Camera2D`
- `Electron2D.CanvasItem`
- `Electron2D.CanvasLayer`
- `Electron2D.Collections.Array`
- `Electron2D.Collections.Dictionary`
- `Electron2D.Color`
- `Electron2D.ConnectFlags`
- `Electron2D.Error`
- `Electron2D.Font`
- `Electron2D.HorizontalAlignment`
- `Electron2D.InputEvent`
- `Electron2D.Mathf`
- `Electron2D.Node`
- `Electron2D.Node2D`
- `Electron2D.NodePath`
- `Electron2D.Object`
- `Electron2D.PackedScene`
- `Electron2D.RandomNumberGenerator`
- `Electron2D.Rect2`
- `Electron2D.Rect2I`
- `Electron2D.RefCounted`
- `Electron2D.RenderingServer`
- `Electron2D.RenderingServer+RenderingFeature`
- `Electron2D.RenderingServer+RenderingProfile`
- `Electron2D.Resource`
- `Electron2D.Rid`
- `Electron2D.SceneTree`
- `Electron2D.Sprite2D`
- `Electron2D.StringName`
- `Electron2D.Texture2D`
- `Electron2D.Transform2D`
- `Electron2D.Variant`
- `Electron2D.Variant+Type`
- `Electron2D.Vector2`
- `Electron2D.Vector2I`
- `Electron2D.Viewport`

Это осознанный минимальный baseline после удаления старого `src/Electron2D/`: каждый новый публичный тип должен добавляться только через задачу и только в Godot-like форме.

Wiki source содержит:

- легенду статусов `Supported`, `Partial`, `Experimental`, `Planned`, `Not planned`;
- planned Godot-like 2D surface;
- явно исключённый legacy/component API.

## Локальная проверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ApiCompatibility.ps1
```

Verifier собирает `src/Electron2D/Electron2D.csproj`, читает exported public types из `Electron2D.dll`, сверяет их с `.github/wiki/API-Compatibility.md` и запрещает возврат legacy/component типов.
