# Electron2D

`0.1.0 Preview` сейчас находится в clean rewrite baseline: старый runtime удалён, а новый Godot-like 2D API собирается заново по `TASKS.md`. Текущий public API содержит базовые `Object`, `RefCounted`, `Resource`, `ResourceUid`, `PackedScene`, `Node`, `NodePath`, `SceneTree`, `InputEvent`, `Callable`, `Error`, `ConnectFlags`, 2D math-типы, `RandomNumberGenerator`, `StringName`, `Rid`, `Variant`, `Texture2D`, `AtlasTexture`, `ViewportTexture`, `Shader`, `Material`, `ShaderMaterial`, `CanvasItem`, `Node2D`, `Sprite2D`, `CanvasLayer`, `Camera2D`, `Viewport`, `Font`, `HorizontalAlignment`, `VerticalAlignment`, `Control`, `Label`, Godot-like коллекции `Electron2D.Collections.Array`/`Dictionary` и `RenderingServer`, включая lifecycle, hierarchy, groups, signals, deferred calls, scene instancing, `Owner`, `Reparent()`, `QueueFree()`, `GetNode()`, deterministic RNG baseline, identity baseline, resource UID baseline, internal `.e2res` resource file baseline, closed-list Variant baseline, internal stable Variant serialization, renderer profile/feature baseline, internal SDL_GPU lifecycle baseline, internal Android mobile GPU smoke/fallback policy, internal SDL_Renderer compatibility frame plan, internal CanvasItem render queue baseline, texture lifetime baseline, internal sprite submission baseline, camera transform, pixel snapping, internal viewport presentation plan, offscreen render target recovery baseline, shader import diagnostics baseline, shader material parameters baseline, immediate drawing command capture и text layout/fallback/cache baseline.

Текущая проверка:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

Compatibility table готовится как GitHub Wiki source: `.github/wiki/API-Compatibility.md`.

## License

Electron2D распространяется по MIT License. Каждый вручную написанный C# и PowerShell source-файл содержит MIT license header, а проверка выполняется командой:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-SourceLicenseHeaders.ps1
```
