# Базовые типы `Object`, `RefCounted`, `Resource`

Статус: целевая спецификация.
Задача: `T-0008`.
Обновлено: 2026-06-21.

## Цель

Новая реализация Electron2D должна начать runtime API с Godot-like базовых типов, без compatibility layer и без Unity-like component history.

## Public API

Минимальный public surface задачи:

- `Electron2D.Object`
- `Electron2D.RefCounted`
- `Electron2D.Resource`

## `Object`

`Object` должен:

- выдавать стабильный ненулевой instance id через `GetInstanceId()`;
- иметь idempotent `Free()`;
- поддерживать `Object.IsInstanceValid(Object? instance)`;
- запрещать работу с освобождённым объектом внутри наследников через protected проверку;
- не реализовывать `IComponent`, `IDisposable` или Unity-like component API.

## `RefCounted`

`RefCounted` должен:

- наследоваться от `Object`;
- начинаться с reference count `1`;
- поддерживать `Reference()`, `Unreference()` и `GetReferenceCount()`;
- освобождаться при переходе счётчика ссылок к `0`;
- не позволять увеличивать счётчик после `Free()`.

## `Resource`

`Resource` должен:

- наследоваться от `RefCounted`;
- иметь `ResourceName`;
- иметь readonly снаружи `ResourcePath`;
- иметь `ResourceLocalToScene`;
- иметь `ResourceSceneUniqueId`;
- поддерживать `TakeOverPath(string path)`.

## Acceptance tests

- instance id уникален и сохраняется после `Free()`;
- `Free()` idempotent;
- `RefCounted.Unreference()` освобождает объект при счётчике `0`;
- `Resource.TakeOverPath()` меняет `ResourcePath`;
- legacy component типы отсутствуют;
- `.github/wiki/API-Compatibility.md` отражает новые public types.

## Отличия от Godot в `T-0008`

- Metadata API откладывается до отдельной задачи: базовые `StringName` и `Variant` уже есть, но сами методы metadata должны появиться только с отдельной спецификацией и тестами.
- Базовый `Rid` реализуется задачей `T-0019`, но `Resource` получит server-backed `Rid` только вместе с конкретными rendering/physics/audio/text resource types.
- `IDisposable` не входит в public API.
