# Deferred calls и безопасный traversal

Статус: реализованный baseline.
Задача: `T-0014`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime реализует Godot-like deferred-call subset:

- `Object.CallDeferred(string method, params object?[] args)`;
- `Callable.CallDeferred(params object?[] args)`.

`StringName` уже есть как базовый тип, но текущий deferred-call baseline всё ещё принимает method names как `string`, arguments как `object?[]`. `Object.CallDeferred()` возвращает `null`, как текущий compatibility-free baseline для Godot-like deferred вызова без результата. Перевод method names на `StringName` должен идти отдельной migration-задачей вместе с будущим слоем `Variant`.

## Очередь deferred calls

`CallDeferred()` не вызывает метод сразу. Вызов добавляется во внутреннюю очередь и выполняется после текущего lifecycle/process/physics/input обхода, когда `SceneTree` переходит к завершению прохода дерева.

Очередь выполняется в порядке постановки. Если deferred callback ставит новый deferred callback, новый вызов выполняется в той же idle-фазе, пока очередь не станет пустой.

Ошибочная сигнатура deferred callable не прерывает обход и не останавливает обработку следующих deferred calls. Если deferred callable бросает исключение из пользовательского кода, `SceneTree` сохраняет внутреннюю диагностику и продолжает разбирать очередь.

## Safe traversal

`SceneTree` обходит children по snapshot текущего pass. Это позволяет вызывать `RemoveChild()` и `AddChild()` из `_Process()` без collection mutation errors и без пропуска siblings.

Node, добавленный во время текущего traversal, получает `_EnterTree()` и `_Ready()` при добавлении, но не обязан получить текущий `_Process()`/`_PhysicsProcess()` callback. Он участвует в следующем frame pass.

## QueueFree

`QueueFree()` из `_Ready()`, `_Process()`, `_PhysicsProcess()` или deferred callback помечает node на удаление. Фактическое удаление выполняется после завершения текущего traversal и после drain deferred queue.

Удаление subtree вызывает `_ExitTree()` child before parent и не прерывает callbacks siblings, которые уже находятся в snapshot текущего pass.

## Ограничения текущего baseline

- `SetDeferred()` и deferred property assignment ещё не реализованы.
- Infinite deferred recursion guard будет частью будущей runtime diagnostics policy.
- Очередь пока является внутренним механизмом runtime; отдельного публичного API для idle-loop нет.
