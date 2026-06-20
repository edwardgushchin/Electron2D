# Deferred calls и безопасное изменение дерева во время обхода

Статус: целевая спецификация.
Задача: `T-0014`.
Обновлено: 2026-06-20.

## Цель

Реализовать Godot-like baseline для deferred calls и безопасного изменения `SceneTree` во время lifecycle/process/physics traversal.

## Public API

Новый public surface:

- `Object.CallDeferred(string method, params object?[] args)`;
- `Callable.CallDeferred(params object?[] args)`.

До появления `StringName` и `Variant` method names представлены `string`, arguments представлены `object?[]`, а `Object.CallDeferred()` возвращает `null`.

## Deferred queue

- Deferred calls выполняются после текущего lifecycle/process/physics/input traversal.
- Порядок выполнения соответствует порядку постановки в очередь.
- Очередь drain'ится до пустого состояния: deferred call, который добавляет новый deferred call, выполняет новый call в той же idle-фазе.
- Ошибочные deferred call signatures не ломают traversal и не останавливают очередь. User-code exception из deferred callable должен попадать в internal diagnostics.

## Safe tree changes

- `QueueFree()` из `_Ready()` удаляет node после завершения lifecycle pass.
- `QueueFree()` из `_PhysicsProcess()` удаляет node после завершения physics traversal и не прерывает sibling callbacks.
- `RemoveChild()`/`AddChild()` во время process traversal не повреждают обход и не вызывают collection mutation exceptions.
- Nodes, добавленные во время текущего traversal, не обязаны получать callback в том же pass; они должны участвовать в следующем pass.

## Ограничения текущего baseline

- Global idle loop ещё не выделен как отдельный runtime subsystem; очередь drain'ится через текущие `SceneTree` lifecycle/process/physics/input host methods.
- `SetDeferred()` и property assignment через `Variant` остаются будущими задачами.
- Infinite deferred recursion guard пока не реализован и будет частью будущей runtime policy.

## Acceptance tests

- `Object.CallDeferred()` и `Callable.CallDeferred()` выполняются после текущего process traversal и в порядке постановки.
- Deferred calls, поставленные из deferred calls, выполняются в той же idle-фазе.
- `QueueFree()` из `_Ready()` удаляет node после lifecycle pass.
- `QueueFree()` из `_PhysicsProcess()` не прерывает sibling callbacks и удаляет subtree после physics traversal.
- Изменение children во время process traversal не повреждает обход; новый child начинает process со следующего frame.
