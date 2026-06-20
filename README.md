# Electron2D

`0.1.0 Preview` сейчас находится в clean rewrite baseline: старый runtime удалён, а новый Godot-like 2D API собирается заново по `TASKS.md`. Текущий public API содержит базовые `Object`, `RefCounted`, `Resource`, `Node`, `NodePath`, `SceneTree`, `InputEvent`, `Callable`, `Error` и `ConnectFlags`, включая lifecycle, hierarchy, groups, signals, deferred calls, `Owner`, `Reparent()`, `QueueFree()` и `GetNode()` baseline.

Текущая проверка:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

Compatibility table готовится как GitHub Wiki source: `.github/wiki/API-Compatibility.md`.
