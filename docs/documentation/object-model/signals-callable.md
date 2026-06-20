# Сигналы, `Callable` и emission semantics

Статус: реализованный baseline.
Задача: `T-0013`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime добавляет:

- `Electron2D.Callable`;
- `Electron2D.Error`;
- `Electron2D.ConnectFlags`;
- `Object.AddUserSignal(string signal)`;
- `Object.HasSignal(string signal)`;
- `Object.Connect(string signal, Callable callable, ConnectFlags flags = ConnectFlags.None)`;
- `Object.Disconnect(string signal, Callable callable)`;
- `Object.IsConnected(string signal, Callable callable)`;
- `Object.EmitSignal(string signal, params object?[] args)`.

`Callable` поддерживает:

- target object + method name через `new Callable(Object target, string method)`;
- untyped `Action` через `Callable.From(Action)`;
- typed `Action<T>` через `Callable.From<T>(Action<T>)`;
- equality для `Disconnect()` и `IsConnected()`;
- прямой вызов через `Call(params object?[] args)`;
- deferred вызов через `CallDeferred(params object?[] args)`.

## Сигналы

Сигнал должен быть объявлен через `AddUserSignal()` до подключения или emission. `Connect()` и `EmitSignal()` для неизвестного signal возвращают `Error.Unavailable`.

`Connect()` возвращает:

- `Error.Ok` при успешном подключении;
- `Error.Unavailable`, если signal неизвестен;
- `Error.InvalidParameter`, если `Callable` пустой;
- `Error.AlreadyExists`, если тот же callable уже подключён без `ConnectFlags.ReferenceCounted`.

`Disconnect()` безопасен для missing connection и не ломает runtime.

## Emission semantics

`EmitSignal()` вызывает callbacks в порядке подключения. Перед обходом создаётся snapshot connections, поэтому `Disconnect()` во время emission не повреждает текущий проход и влияет только на следующие emissions.

Если callback бросает исключение или аргументы не подходят сигнатуре callable, emission продолжает обход остальных callbacks, но возвращает `Error.Failed`. User-code exception из signal callback дополнительно записывается в internal `SceneTree` diagnostics, если для target или emitter известен tree context.

## Ограничения текущего baseline

- `StringName`, `Variant` и typed signal declarations ещё не реализованы; signal/method names представлены `string`, arguments представлены `object?[]`.
- `ConnectFlags.Deferred`, `Persist`, `OneShot` и `AppendSourceObject` пока объявлены, но их runtime semantics будут реализованы отдельными задачами.
- `Callable.Bind()`, `Unbind()` и `Variant` return values ещё не реализованы.
- Built-in lifecycle signals пока не объявляются автоматически.
