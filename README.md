# Electron2D

`0.1.0 Preview` сейчас находится в clean rewrite baseline: старый runtime удалён, а новый Godot-like 2D API собирается заново по локальному task tracker maintainer-а. Текущий public API содержит базовые `Object`, `RefCounted`, `Resource`, `ResourceUid`, `PackedScene`, `Node`, `NodePath`, `SceneTree`, `InputEvent*` keyboard/mouse baseline, `Key`, `KeyLocation`, `MouseButton`, `MouseButtonMask`, `Callable`, `Error`, `ConnectFlags`, `[Export]`, `[Signal]`, `[Tool]`, 2D math-типы, `RandomNumberGenerator`, `StringName`, `Rid`, `Variant`, `Texture2D`, `AtlasTexture`, `ViewportTexture`, `Shader`, `Material`, `ShaderMaterial`, `CanvasItem`, `Node2D`, `Sprite2D`, `CanvasLayer`, `Camera2D`, `Viewport`, `Font`, `HorizontalAlignment`, `VerticalAlignment`, `Control`, `Label`, Godot-like коллекции `Electron2D.Collections.Array`/`Dictionary`, `RenderingServer`, `PhysicsServer2D`, `CollisionObject2D`, `PhysicsBody2D`, `PhysicsMaterial`, `World2D`, `PhysicsDirectSpaceState2D`, `PhysicsRayQueryParameters2D`, `PhysicsPointQueryParameters2D`, `PhysicsShapeQueryParameters2D`, `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`, `Shape2D`, `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D` и `ConcavePolygonShape2D`, включая lifecycle, hierarchy, groups, signals, deferred calls, scene instancing, `Owner`, `Reparent()`, `QueueFree()`, `GetNode()`, C# script class baseline, internal script metadata bridge для export properties/signals/tool-state, internal SDL input event mapping для keyboard/mouse/wheel/text input, deterministic RNG baseline, identity baseline, resource UID baseline, internal `.e2res` resource file baseline, internal import cache baseline, internal PNG/JPEG texture import metadata baseline, internal TTF/OTF font import metadata baseline, internal shader source artifact import baseline, internal scene/resource serialization baseline, internal AOT-safe metadata registry, internal data stability stress gate, closed-list Variant baseline, internal stable Variant serialization, renderer profile/feature baseline, internal `PhysicsServer2D` RID-boundary, physics node RID lifecycle/transform sync baseline, shape resource validation/AOT-safe serialization baseline, collision filter/material/body-state baseline, `Area2D` overlap signals/helper methods baseline, AABB direct physics query baseline, fixed physics tick `1/60`, basic `RigidBody2D` motion, AABB sweep для быстрых тел и one-way platform baseline, Box2D.NET candidate validation gate для desktop JIT/NativeAOT и allocations per tick, internal SDL_GPU lifecycle baseline, internal Android mobile GPU smoke/fallback policy, internal SDL_Renderer compatibility frame plan, internal CanvasItem render queue baseline, texture lifetime baseline, internal sprite submission baseline, camera transform, pixel snapping, internal viewport presentation plan, offscreen render target recovery baseline, shader import diagnostics baseline, shader material parameters baseline, immediate drawing command capture и text layout/fallback/cache baseline.

Текущая проверка:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

AOT metadata smoke:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-AotMetadataSafety.ps1 -NativeAot
```

Compatibility table готовится как GitHub Wiki source: `.github/wiki/API-Compatibility.md`.

## License

Electron2D распространяется по MIT License. Каждый вручную написанный C# и PowerShell source-файл содержит MIT license header, а проверка выполняется командой:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-SourceLicenseHeaders.ps1
```
