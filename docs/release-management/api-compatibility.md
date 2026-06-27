# Таблица совместимости Electron2D API

Обновлено: 2026-06-27.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0004`.
Обновлено: 2026-06-27.

## Цель

Для `0.1.0 Preview` нужно поддерживать публичный API только в рамках согласованного Electron2D 2D-поднабора. Все публичные типы runtime assembly должны быть отражены в compatibility table с одним из статусов:

- `Supported`
- `Partial`
- `Experimental`
- `Planned`

## GitHub Wiki

Compatibility table должна храниться в GitHub Wiki repository проекта. Репозиторий не должен добавлять локальный сайт, static site generator или отдельный local docs portal ради этой таблицы. Каталог `.github/wiki/` допустим только как игнорируемый локальный клон `Electron2D.wiki.git`.

Canonical location для текущей задачи:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
API-Compatibility.md
```

Этот файл предназначен для публикации в GitHub Wiki проекта.

## Clean baseline

После clean reset runtime assembly может временно не экспортировать публичных типов. Это допустимый baseline, если:

- verifier подтверждает `0` exported public types;
- legacy/component API не существует в public surface;
- planned Electron2D типы перечислены как `Planned`.

## UI gate before Editor

`Electron2D.Editor` нельзя начинать до отдельного UI public API gate. Этот gate считается закрытым только когда все UI-related public API строки в GitHub Wiki `API-Compatibility.md` переведены в `Supported` на основании фактической реализации, тестов, XML documentation, generated Wiki pages, спецификаций и документации реализации.

Запрещено переводить UI rows из `Partial` в `Supported` только ради разблокировки редактора. Если для редактора, Project Manager, Inspector, dock UI, встроенного редактора кода или Agent Workspace panel не хватает публичного UI API, соответствующая задача должна оставаться заблокированной до реализации этого API в runtime.

Список UI/Text rows берётся из generated GitHub Wiki page `API-UI-and-Text.md`. Текущая целевая поверхность `T-0214` для таблицы совместимости — C#-команда `verify api-compatibility --wiki-path .github/wiki`; расширение этой проверки отдельными UI/Text-правилами остаётся C#-миграционным долгом, если проверка должна стать самостоятельной.

## Запрещённый API

Следующие имена не должны появляться в public surface новой реализации:

- `IComponent`
- `SpriteRenderer`
- `SpriteAnimator`
- `AudioSource`
- `Rigidbody`
- `Collider`
- `BoxCollider`
- `CircleCollider`
- `PolygonCollider`
- `PhysicsBodyType`

## Верификация

```bash
dotnet run --project eng\Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
```

Verifier должен сверить tracked API manifest, который пересоздаётся из compiled runtime, XML documentation и compatibility table, с GitHub Wiki clone и убедиться, что каждый публичный тип отражён в `API-Compatibility.md` с допустимым статусом. Legacy/component API должен запрещаться по public surface, но не публиковаться отдельным списком в Wiki. Для `T-0214` целевая поверхность проверки - C#-команда `verify api-compatibility --wiki-path .github/wiki`.

## Фактическое состояние, ограничения и проверки

Статус: реализованная проверка compatibility baseline.
Задача: `T-0004`.
Обновлено: 2026-06-27.

## Где находится таблица

Compatibility table хранится в GitHub Wiki repository:

```text
https://github.com/edwardgushchin/Electron2D.wiki.git
API-Compatibility.md
```

Это не локальный сайт и не generated documentation portal. Основной репозиторий использует `.github/wiki/` только как игнорируемый локальный клон; опубликованный файл находится в GitHub Wiki проекта.

## Текущий baseline

Новый runtime assembly `Electron2D` экспортирует текущий baseline объектной модели, resource UID, 2D math, RNG, identity, Variant value carrier, C# scripting marker attributes, keyboard/mouse input events, texture/canvas/camera, tile set and tile map layer runtime baseline, shader material resource layer, immediate drawing surface, text/UI baseline, frame-based sprite animation, resource animation tracks, `AnimationPlayer`, `Tween`, `PhysicsServer2D` RID-boundary, первые 2D physics nodes, concrete shape resources, physics material resource, `Area2D` overlap signals baseline, direct 2D physics query baseline, fixed physics tick, basic rigid body movement, `CharacterBody2D` kinematic movement baseline и debug collision shape hooks:

- `Electron2D.AnimatedSprite2D`
- `Electron2D.Animation`
- `Electron2D.Animation+InterpolationTypeEnum`
- `Electron2D.Animation+LoopModeEnum`
- `Electron2D.Animation+TrackTypeEnum`
- `Electron2D.AnimationLibrary`
- `Electron2D.AnimationPlayer`
- `Electron2D.Area2D`
- `Electron2D.AtlasTexture`
- `Electron2D.Callable`
- `Electron2D.CallbackTweener`
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
- `Electron2D.IntervalTweener`
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
- `Electron2D.PropertyTweener`
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
- `Electron2D.TileData`
- `Electron2D.TileMapLayer`
- `Electron2D.TileSet`
- `Electron2D.TileSetAtlasSource`
- `Electron2D.TileSetSource`
- `Electron2D.ToolAttribute`
- `Electron2D.Transform2D`
- `Electron2D.Tween`
- `Electron2D.Tween+EaseType`
- `Electron2D.Tween+TransitionType`
- `Electron2D.Tweener`
- `Electron2D.Variant`
- `Electron2D.Variant+Type`
- `Electron2D.Vector2`
- `Electron2D.Vector2I`
- `Electron2D.VerticalAlignment`
- `Electron2D.Viewport`
- `Electron2D.ViewportTexture`
- `Electron2D.World2D`

Это осознанный минимальный baseline после удаления старого `src/Electron2D/`: каждый новый публичный тип должен добавляться только через задачу и только в согласованной форме публичного API.

GitHub Wiki содержит:

- легенду статусов `Supported`, `Partial`, `Experimental`, `Planned`;
- planned 2D surface;
- текущий public runtime surface.

## UI gate before Editor

UI public API gate закрывается отдельной проверкой поверх GitHub Wiki: все строки из generated category page `API-UI-and-Text.md` должны соответствовать фактическому runtime API, иметь тесты, XML documentation, generated Wiki pages, спецификацию, документацию реализации и статус `Supported`, а не `Partial`.

Если будущая editor-задача требует public UI type, property, method или event, которого ещё нет в runtime, такая editor-задача остаётся заблокированной. Нельзя разблокировать редактор простой заменой статуса в таблице совместимости без реализации и проверок.

## Локальная проверка

```bash
dotnet run --project eng\Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
```

Verifier читает `data/api/electron2d-api-manifest.json`, проверяет его форму, сверяет public type entries с `API-Compatibility.md` в клоне `Electron2D.wiki.git` и запрещает возврат legacy/component типов без публикации отдельного legacy-блока в Wiki. Сам manifest пересоздаётся отдельной командой `update api-manifest --wiki-path .github/wiki --check`, которая строит проверяемый снимок из compiled runtime и XML documentation.

UI gate остаётся правилом совместимости: generated Wiki category `API-UI-and-Text.md` должна соответствовать `API-Compatibility.md`, а UI/Text public types должны получить статус `Supported` только после фактической реализации и проверок. Отдельная командная проверка этого правила должна быть перенесена в C#-инструмент перед тем, как её объявлять текущим gate.
