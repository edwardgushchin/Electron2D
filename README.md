# Electron2D

`0.1.0 Preview` сейчас находится в clean rewrite baseline: старый runtime удалён, а новый Godot-like 2D API собирается заново по `TASKS.md`. Текущий public API содержит базовые `Object`, `RefCounted`, `Resource`, `PackedScene`, `Node`, `NodePath`, `SceneTree`, `InputEvent`, `Callable`, `Error`, `ConnectFlags`, 2D math-типы, `RandomNumberGenerator`, `StringName`, `Rid`, `Variant` и Godot-like коллекции `Electron2D.Collections.Array`/`Dictionary`, включая lifecycle, hierarchy, groups, signals, deferred calls, scene instancing, `Owner`, `Reparent()`, `QueueFree()`, `GetNode()`, deterministic RNG baseline, identity baseline, closed-list Variant baseline и internal stable Variant serialization.

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
