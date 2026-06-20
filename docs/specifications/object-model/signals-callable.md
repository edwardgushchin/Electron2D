# Сигналы, `Callable` и emission semantics

Статус: целевая спецификация.
Задача: `T-0013`.
Обновлено: 2026-06-20.

## Цель

Реализовать Godot-like baseline для сигналов `Object`, `Callable`, connect/disconnect и synchronous emission semantics без legacy event bus и без compatibility layer.

## Public API

Новый public surface:

- `Electron2D.Callable`;
- `Electron2D.Error`;
- `Electron2D.ConnectFlags`;
- `Object.AddUserSignal(string signal)`;
- `Object.HasSignal(string signal)`;
- `Object.Connect(string signal, Callable callable, ConnectFlags flags = ConnectFlags.None)`;
- `Object.Disconnect(string signal, Callable callable)`;
- `Object.IsConnected(string signal, Callable callable)`;
- `Object.EmitSignal(string signal, params object?[] args)`.

`Callable` должен поддерживать:

- callable из target object + method name;
- callable из C# `Action`;
- callable из C# typed `Action<T>`;
- equality для disconnect/is-connected.

До появления `StringName`, `Variant` и typed signal declaration signal/method names представлены `string`, а signal arguments представлены `object?[]`.

## Инварианты

- Сигнал должен быть объявлен через `AddUserSignal()` до подключения или emission.
- `Connect()` возвращает `Error.Ok` при успехе.
- `Connect()` возвращает `Error.Unavailable`, если signal неизвестен.
- Повторное подключение того же callable без `ConnectFlags.ReferenceCounted` возвращает `Error.AlreadyExists`.
- `Disconnect()` удаляет connection, если она есть; missing connection не должен ломать runtime.
- `EmitSignal()` вызывает connections в порядке подключения.
- `EmitSignal()` использует snapshot connections на начало emission: disconnect/connect во время emission не повреждает текущий проход.
- Ошибка в одном callback не прерывает остальные callbacks; итоговый `EmitSignal()` возвращает `Error.Failed`, а user-code exception попадает в internal diagnostics, если известен `SceneTree` context.
- Callable с несовместимыми аргументами не вызывается, а emission возвращает `Error.Failed`.

## Ограничения текущего baseline

- `ConnectFlags.Deferred`, `Persist`, `OneShot` и `AppendSourceObject` объявлены для Godot-like API, но runtime semantics будут реализованы в следующих задачах.
- `Callable.Bind()`, `Unbind()` и `Variant` return values остаются будущими задачами.
- Built-in signals для lifecycle ещё не объявляются автоматически; они будут добавлены отдельной задачей, когда появится полный signal registry.

## Acceptance tests

- Multiple subscribers вызываются в порядке подключения.
- Typed и untyped `Callable` получают аргументы корректно.
- `Disconnect()` во время emission не повреждает текущий snapshot, но влияет на следующий emission.
- Unknown signal, duplicate connection и callback exception возвращают проверяемый `Error`.
- Public API остаётся Godot-like и не добавляет .NET-only event surface.
