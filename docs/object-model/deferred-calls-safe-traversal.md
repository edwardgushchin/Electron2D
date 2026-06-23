# Deferred calls и безопасное изменение дерева во время обхода

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0014`.
Обновлено: 2026-06-20.

## Цель

Реализовать Electron2D baseline для deferred calls и безопасного изменения `SceneTree` во время lifecycle/process/physics traversal.

## Public API

Новый public surface:

- `Object.CallDeferred(string method, params object?[] args)`;
- `Callable.CallDeferred(params object?[] args)`.

`StringName` и `Variant` уже есть как базовые типы, но текущий deferred-call baseline всё ещё принимает method names как `string`, arguments как `object?[]`, а `Object.CallDeferred()` возвращает `null`. Перевод на `StringName`/`Variant` должен идти отдельной migration-задачей.

## Deferred queue

- Deferred calls выполняются после текущего lifecycle/process/physics/input traversal.
- Порядок выполнения соответствует порядку постановки в очередь.
- Очередь drain'ится до пустого состояния: deferred call, который добавляет новый deferred call, выполняет новый call в той же idle-фазе.
- Ошибочные deferred call signatures не ломают обход и не останавливают очередь. Исключение из пользовательского кода внутри deferred callable должно попадать во внутреннюю диагностику.

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0014`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime реализует Electron2D deferred-call subset:

- `Object.CallDeferred(string method, params object?[] args)`;
- `Callable.CallDeferred(params object?[] args)`.

`StringName` и `Variant` уже есть как базовые типы, но текущий deferred-call baseline всё ещё принимает method names как `string`, arguments как `object?[]`. `Object.CallDeferred()` возвращает `null`, как текущий compatibility-free baseline для Electron2D deferred вызова без результата. Перевод method names и arguments на `StringName`/`Variant` должен идти отдельной migration-задачей.

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
