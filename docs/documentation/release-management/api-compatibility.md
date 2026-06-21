# Таблица совместимости API

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

Новый runtime assembly `Electron2D` экспортирует текущий baseline объектной модели, resource UID, 2D math, RNG, identity, Variant value carrier, C# scripting marker attributes, keyboard/mouse input events, texture/canvas/camera, shader material resource layer, immediate drawing surface, text/UI baseline, frame-based sprite animation, `PhysicsServer2D` RID-boundary, первые 2D physics nodes, concrete shape resources, physics material resource, `Area2D` overlap signals baseline, direct 2D physics query baseline, fixed physics tick, basic rigid body movement, `CharacterBody2D` kinematic movement baseline и debug collision shape hooks:

- `Electron2D.AnimatedSprite2D`
- `Electron2D.Area2D`
- `Electron2D.AtlasTexture`
- `Electron2D.Callable`
- `Electron2D.Camera2D`
- `Electron2D.CanvasItem`
- `Electron2D.CanvasLayer`
- `Electron2D.CapsuleShape2D`
- `Electron2D.CharacterBody2D`
- `Electron2D.CharacterBody2D+MotionModeEnum`
- `Electron2D.CharacterBody2D+PlatformOnLeaveEnum`
- `Electron2D.CircleShape2D`
- `Electron2D.Collections.Array`
- `Electron2D.Collections.Dictionary`
- `Electron2D.CollisionObject2D`
- `Electron2D.CollisionShape2D`
- `Electron2D.Color`
- `Electron2D.ConcavePolygonShape2D`
- `Electron2D.ConnectFlags`
- `Electron2D.Control`
- `Electron2D.ConvexPolygonShape2D`
- `Electron2D.Error`
- `Electron2D.ExportAttribute`
- `Electron2D.Font`
- `Electron2D.HorizontalAlignment`
- `Electron2D.InputEvent`
- `Electron2D.InputEventFromWindow`
- `Electron2D.InputEventKey`
- `Electron2D.InputEventMouse`
- `Electron2D.InputEventMouseButton`
- `Electron2D.InputEventMouseMotion`
- `Electron2D.InputEventWithModifiers`
- `Electron2D.Key`
- `Electron2D.KeyLocation`
- `Electron2D.KinematicCollision2D`
- `Electron2D.Label`
- `Electron2D.Material`
- `Electron2D.Mathf`
- `Electron2D.MouseButton`
- `Electron2D.MouseButtonMask`
- `Electron2D.Node`
- `Electron2D.Node2D`
- `Electron2D.NodePath`
- `Electron2D.Object`
- `Electron2D.PackedScene`
- `Electron2D.PhysicsBody2D`
- `Electron2D.PhysicsDirectSpaceState2D`
- `Electron2D.PhysicsMaterial`
- `Electron2D.PhysicsPointQueryParameters2D`
- `Electron2D.PhysicsRayQueryParameters2D`
- `Electron2D.PhysicsServer2D`
- `Electron2D.PhysicsServer2D+ProcessInfo`
- `Electron2D.PhysicsServer2D+ShapeType`
- `Electron2D.PhysicsServer2D+SpaceParameter`
- `Electron2D.PhysicsShapeQueryParameters2D`
- `Electron2D.RandomNumberGenerator`
- `Electron2D.RayCast2D`
- `Electron2D.Rect2`
- `Electron2D.Rect2I`
- `Electron2D.RectangleShape2D`
- `Electron2D.RefCounted`
- `Electron2D.RenderingServer`
- `Electron2D.RenderingServer+RenderingFeature`
- `Electron2D.RenderingServer+RenderingProfile`
- `Electron2D.Resource`
- `Electron2D.ResourceUid`
- `Electron2D.Rid`
- `Electron2D.RigidBody2D`
- `Electron2D.RigidBody2D+CenterOfMassModeEnum`
- `Electron2D.RigidBody2D+FreezeModeEnum`
- `Electron2D.SceneTree`
- `Electron2D.SegmentShape2D`
- `Electron2D.Shader`
- `Electron2D.Shader+Mode`
- `Electron2D.ShaderMaterial`
- `Electron2D.Shape2D`
- `Electron2D.SignalAttribute`
- `Electron2D.Sprite2D`
- `Electron2D.SpriteFrames`
- `Electron2D.SpriteFrames+LoopModeEnum`
- `Electron2D.StaticBody2D`
- `Electron2D.StringName`
- `Electron2D.Texture2D`
- `Electron2D.ToolAttribute`
- `Electron2D.Transform2D`
- `Electron2D.Variant`
- `Electron2D.Variant+Type`
- `Electron2D.Vector2`
- `Electron2D.Vector2I`
- `Electron2D.VerticalAlignment`
- `Electron2D.Viewport`
- `Electron2D.ViewportTexture`
- `Electron2D.World2D`

Это осознанный минимальный baseline после удаления старого `src/Electron2D/`: каждый новый публичный тип должен добавляться только через задачу и только в согласованной форме публичного API.

Wiki source содержит:

- легенду статусов `Supported`, `Partial`, `Experimental`, `Planned`, `Not planned`;
- planned 2D surface;
- явно исключённый legacy/component API.

## Локальная проверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ApiCompatibility.ps1
```

Verifier собирает `src/Electron2D/Electron2D.csproj`, читает exported public types из `Electron2D.dll`, сверяет их с `.github/wiki/API-Compatibility.md` и запрещает возврат legacy/component типов.
