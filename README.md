# Electron2D

`0.1.0 Preview` сейчас находится в clean rewrite baseline: старый runtime удалён, а новый Godot-like 2D API собирается заново по `TASKS.md`. Текущий public API содержит базовые `Object`, `RefCounted`, `Resource`, `Node`, `SceneTree` и `InputEvent`, включая lifecycle, hierarchy, `Owner`, `Reparent()` и `QueueFree()` baseline.

Текущая проверка:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

Compatibility table готовится как GitHub Wiki source: `.github/wiki/API-Compatibility.md`.
