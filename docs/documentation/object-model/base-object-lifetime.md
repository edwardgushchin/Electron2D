# Базовые типы `Object`, `RefCounted`, `Resource`

Статус: реализованный baseline.
Задача: `T-0008`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime экспортирует:

- `Electron2D.Object`
- `Electron2D.RefCounted`
- `Electron2D.Resource`

## `Object`

`Object` предоставляет:

- `GetInstanceId()`;
- `Free()`;
- `IsQueuedForDeletion()`;
- `Object.IsInstanceValid(Object? instance)`.

`Free()` idempotent. После `Free()` объект считается invalid, но `GetInstanceId()` остаётся стабильным.

`IsQueuedForDeletion()` возвращает флаг отложенного удаления, который сейчас устанавливается `Node.QueueFree()`.

## `RefCounted`

`RefCounted` наследуется от `Object`, стартует с reference count `1` и предоставляет:

- `Reference()`;
- `Unreference()`;
- `GetReferenceCount()`.

Когда `Unreference()` переводит счётчик в `0`, объект освобождается через `Free()`.

## `Resource`

`Resource` наследуется от `RefCounted` и предоставляет:

- `ResourceName`;
- `ResourcePath`;
- `ResourceLocalToScene`;
- `ResourceSceneUniqueId`;
- `TakeOverPath(string path)`.

Это минимальный Godot-like baseline. Resource loading/import/saving будет добавлен отдельными задачами.

## Отличия от Godot в текущем baseline

- Реализован только минимальный subset, нужный для старта object model.
- `Object` пока не содержит metadata API, потому что `StringName` и `Variant` ещё не реализованы.
- `Resource` пока не возвращает `Rid`, потому что `Rid` относится к отдельной задаче базовых Variant/math типов.
- Public API не реализует `IDisposable`, чтобы не добавлять .NET-specific метод `Dispose()` в Godot-like surface.
