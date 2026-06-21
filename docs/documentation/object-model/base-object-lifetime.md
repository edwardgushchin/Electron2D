# Базовые типы `Object`, `RefCounted`, `Resource`

Статус: реализованный baseline.
Задача: `T-0008`.
Обновлено: 2026-06-21.

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

Это минимальный Godot-like baseline. `ResourceUid` и внутренний `.e2res` формат уже добавлены в ресурсном домене, но публичные `ResourceLoader`/`ResourceSaver`, импорт и сохранение файлов будут добавлены отдельными задачами.

## Отличия от Godot в текущем baseline

- Реализован только минимальный subset, нужный для старта object model.
- `Object` пока не содержит metadata API: `StringName` и `Variant` уже есть, но сами metadata методы будут вводиться отдельной задачей с тестами.
- Базовый `Resource` пока не возвращает `Rid`: `Rid` уже есть, но конкретные server-backed ресурсы появятся вместе с rendering/physics/audio/text servers.
- Public API не реализует `IDisposable`, чтобы не добавлять .NET-specific метод `Dispose()` в Godot-like surface.
