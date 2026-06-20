# `StringName` и `Rid`

Статус: реализовано для `T-0019`.
Обновлено: 2026-06-21.

## Публичные типы

В `Electron2D` добавлены:

- `StringName`;
- `Rid`.

`StringName` - Godot-like immutable interned name. Он нужен для будущего перевода signal, group, method, property и resource APIs с обычных `string` на Godot-like name handles.

`Rid` - Godot-like opaque resource identifier, то есть непрозрачный handle низкоуровневого ресурса. Это не .NET Runtime Identifier для платформ сборки.

## `StringName`

Реализовано:

- constructor `StringName(string? value)`;
- implicit conversion из `string?`;
- `IsEmpty()`;
- `ToString()`;
- equality operators для `StringName`/`StringName` и `StringName`/`string?`;
- ordinal equality и hashing;
- корректное поведение `default(StringName)`, `null` и empty string.

`StringName` можно использовать как key в `Dictionary<StringName, TValue>`. Два значения с одинаковым текстом равны и дают одинаковый hash code.

Полный набор string methods не продублирован. Для строковых операций нужно вызвать `ToString()` и использовать обычные `System.String` API.

## `Rid`

Реализовано:

- `GetId()`;
- `IsValid()`;
- `ToString()`;
- equality operators;
- comparison operators `<`, `<=`, `>`, `>=`;
- equality, ordering и hashing.

`default(Rid)` имеет ID `0` и считается invalid. Valid `Rid` значения в `0.1.0 Preview` выдаёт только внутренний allocator будущих серверов; публичного конструктора из integer ID нет.

## Internal allocator

Внутренний `RidAllocator` доступен только runtime/tests и будущим серверным abstractions. Он не является public API.

Реализовано:

- `Allocate()` выдаёт новый valid `Rid`;
- `Owns(Rid rid)` проверяет, принадлежит ли `Rid` allocator текущей runtime-сессии;
- `Free(Rid rid)` освобождает владение;
- `ActiveCount` показывает количество активных handles.

После `Free()` само значение `Rid` остаётся opaque ID, но allocator больше не считает его живым. `Rid`, выданный одним allocator, не принадлежит другому allocator.

## Ограничения

- `Object` metadata API ещё не переведён на `StringName`/`Variant`; сами metadata методы должны появиться отдельной задачей.
- `NodePath.GetName()` и `GetSubname()` пока возвращают `string`; перевод на `StringName` должен быть отдельной задачей с обновлением тестов.
- Базовый `Resource` пока не возвращает `Rid`; конкретные server-backed ресурсы получат `Rid` вместе с rendering/physics/audio/text servers.
