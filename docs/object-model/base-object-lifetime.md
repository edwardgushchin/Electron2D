# Базовые типы `ElectronObject`, `RefCounted`, `Resource`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0008`.
Обновлено: 2026-06-21.

## Цель

Новая реализация Electron2D должна начать runtime API с Electron2D базовых типов, без compatibility layer и без Unity-like component history.

## Public API

Минимальный public surface задачи:

- `Electron2D.ElectronObject`
- `Electron2D.RefCounted`
- `Electron2D.Resource`

## `ElectronObject`

`ElectronObject` должен:

- выдавать стабильный ненулевой instance id через `GetInstanceId()`;
- иметь idempotent `Free()`;
- поддерживать `ElectronObject.IsInstanceValid(ElectronObject? instance)`;
- запрещать работу с освобождённым объектом внутри наследников через protected проверку;
- не реализовывать `IComponent`, `IDisposable` или Unity-like component API.

## `RefCounted`

`RefCounted` должен:

- наследоваться от `ElectronObject`;
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
- `Electron2D.wiki.git/API-Compatibility.md` отражает новые public types.

## Отличия от Godot в `T-0008`

- Metadata API откладывается до отдельной задачи: базовые `StringName` и `Variant` уже есть, но сами методы metadata должны появиться только с отдельной спецификацией и тестами.
- Базовый `Rid` реализуется задачей `T-0019`, но `Resource` получит server-backed `Rid` только вместе с конкретными rendering/physics/audio/text resource types.
- `IDisposable` не входит в public API.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0008`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime экспортирует:

- `Electron2D.ElectronObject`
- `Electron2D.RefCounted`
- `Electron2D.Resource`

## `ElectronObject`

`ElectronObject` предоставляет:

- `GetInstanceId()`;
- `Free()`;
- `IsQueuedForDeletion()`;
- `ElectronObject.IsInstanceValid(ElectronObject? instance)`.

`Free()` idempotent. После `Free()` объект считается invalid, но `GetInstanceId()` остаётся стабильным.

`IsQueuedForDeletion()` возвращает флаг отложенного удаления, который сейчас устанавливается `Node.QueueFree()`.

## `RefCounted`

`RefCounted` наследуется от `ElectronObject`, стартует с reference count `1` и предоставляет:

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

Это минимальный Electron2D baseline. `ResourceUid` и внутренний `.e2res` формат уже добавлены в ресурсном домене, но публичные `ResourceLoader`/`ResourceSaver`, импорт и сохранение файлов будут добавлены отдельными задачами.

## Ограничения текущего baseline

- Реализован только минимальный subset, нужный для старта object model.
- `ElectronObject` пока не содержит metadata API: `StringName` и `Variant` уже есть, но сами metadata методы будут вводиться отдельной задачей с тестами.
- Базовый `Resource` пока не возвращает `Rid`: `Rid` уже есть, но конкретные server-backed ресурсы появятся вместе с rendering/physics/audio/text servers.
- Public API не реализует `IDisposable`, чтобы не добавлять .NET-specific метод `Dispose()` в Electron2D surface.
