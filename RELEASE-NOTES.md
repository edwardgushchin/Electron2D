# Electron2D 0.1.0 Preview

Дата baseline: 2026-06-20.

`0.1.0 Preview` перезапущен как clean rewrite. Старая реализация `src/Electron2D/` удалена, а предыдущая работа сохранена в локальной ветке `private/pre-rewrite-backup-2026-06-20`.

## Что есть сейчас

- Новый пустой runtime-проект `Electron2D`.
- Начальный Godot-like public API: `Object`, `RefCounted`, `Resource`.
- Начальный lifecycle API: `Node`, `SceneTree`, `InputEvent`.
- Начальная иерархия `Node`: `Owner`, `GetParent()`, `GetChild()`, `MoveChild()`, `Reparent()` и `QueueFree()`.
- Начальное разрешение путей: `NodePath`, `GetNode()` и `GetNodeOrNull()`.
- Начальные группы: `AddToGroup()`, `RemoveFromGroup()`, `GetGroups()` и `SceneTree.CallGroup()`.
- Начальные сигналы: `Callable`, `Connect()`, `Disconnect()`, `EmitSignal()` и error-return semantics.
- Начальные deferred calls: `Object.CallDeferred()`, `Callable.CallDeferred()` и безопасный drain deferred queue после traversal.
- Начальные scene resources: `PackedScene`, in-memory `Pack()`/`Instantiate()` и `SceneTree.ChangeSceneToPacked()`.
- Начальная внутренняя runtime-диагностика: исключения из пользовательского кода сохраняют node context, callback, kind, message и stack trace без остановки обхода дерева, очереди или signal emission.
- Начальный 2D math API: `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- Начальный RNG API: `RandomNumberGenerator` с документированными `Seed`, `State` и воспроизводимой PCG32 sequence policy.
- Начальный identity API: `StringName` и `Rid` для будущего `Variant` и server-backed resources.
- Тестовая инфраструктура и desktop CI matrix.
- GitHub Wiki source для API compatibility.
- Package metadata `0.1.0-preview`.
- MIT License: корневой `LICENSE`, package metadata и source-file headers согласованы и проверяются CI.

## Чего пока нет

- `Variant` integration для math-типов, file-level scene serialization, full `ConnectFlags` semantics, `CallGroupFlags`, `GetPath()`, `GetPathTo()`, `SetDeferred()`, pause/process modes и `Viewport` root ещё реализуются следующими задачами.
- Экспорт Android/iOS пока отмечен как явный release gap, а не как active CI gate.

## Правило API

В новый runtime не переносится Unity-like/component history. Публичный API должен появляться только как согласованный Godot-like 2D-поднабор.

## Breaking changes policy для 0.x

До стабильной версии `1.0` публичный API может меняться между preview-сборками. Breaking changes допустимы только при явной записи в `CHANGELOG.md`, `RELEASE-NOTES.md` и compatibility table; compatibility layer ради старого API не добавляется.
