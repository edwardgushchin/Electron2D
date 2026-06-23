# `Node` и `SceneTree`: lifecycle baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0009`.
Обновлено: 2026-06-21.

## Цель

Реализовать первый Electron2D lifecycle baseline для `Node` и `SceneTree` без component API, compatibility layer и legacy Unity-like callbacks.

## Public API

Минимальный public surface задачи:

- `Electron2D.Node`
- `Electron2D.SceneTree`
- `Electron2D.InputEvent`
- `Electron2D.ProcessMode`

## `Node`

`Node` должен:

- наследоваться от `Object`;
- иметь `Name`;
- иметь `ProcessMode`;
- иметь Electron2D `GetParent()` для чтения parent;
- поддерживать `AddChild(Node child)` и `RemoveChild(Node child)`;
- поддерживать `GetChildCount()`;
- поддерживать `IsInsideTree()`;
- поддерживать `GetTree()`;
- иметь Electron2D lifecycle callbacks:
  - `_EnterTree()`;
  - `_Ready()`;
  - `_Process(double delta)`;
  - `_PhysicsProcess(double delta)`;
  - `_Input(InputEvent inputEvent)`;
  - `_ExitTree()`.

## `SceneTree`

`SceneTree` должен:

- наследоваться от `Object`;
- иметь `Root`;
- иметь `Paused`;
- вводить `Root` в дерево при создании;
- позволять тестам и внутреннему runtime-коду выполнять process, physics и input проходы через непубличный API.

## Порядок lifecycle

- `_EnterTree()` вызывается сверху вниз: parent before child.
- `_Ready()` вызывается снизу вверх: child before parent.
- `_Ready()` вызывается для каждого node не более одного раза.
- `_Process()`, `_PhysicsProcess()` и `_Input()` вызываются сверху вниз.
- `_ExitTree()` при удалении subtree вызывается снизу вверх: child before parent.

## Pause и process mode

`ProcessMode` задаёт, получает ли node idle, physics и input callbacks при паузе дерева:

- `Inherit` наследует effective mode ближайшего ancestor; для root fallback равен `Pausable`;
- `Pausable` работает только когда `SceneTree.Paused == false`;
- `WhenPaused` работает только когда `SceneTree.Paused == true`;
- `Always` работает при любом значении `Paused`;
- `Disabled` не получает callbacks.

`SceneTree.Paused` не должен останавливать `_Draw()`/cached drawing: paused frame всё ещё должен отрисовывать текущее состояние UI. Pause не отменяет deferred/delete queue flushing.

## Ошибки пользовательского кода

Если пользовательский lifecycle callback бросает исключение, `SceneTree` должен поймать его, сохранить диагностику для тестов и будущих инструментов редактора и продолжить обход остальных node в текущем lifecycle pass. Исключение не должно молча теряться и не должно обрывать весь loop.

Diagnostic API остаётся внутренним. Централизованное правило внутренней runtime-диагностики уточняется отдельной спецификацией `runtime-diagnostics.md`.

## Acceptance tests

- добавление node к `SceneTree.Root` вызывает `_EnterTree()` и `_Ready()`;
- subtree получает enter/ready в указанном порядке;
- process, physics и input callbacks получают delta/input и идут в tree order;
- `RemoveChild()` вызывает `_ExitTree()` и очищает `GetParent()`/`GetTree()`;
- exception в пользовательском callback сохраняется в diagnostic и не прерывает обход sibling node;
- `SceneTree.Paused` останавливает pausable `_Process()`, `_PhysicsProcess()` и `_Input()`, но оставляет `WhenPaused`/`Always` nodes активными;
- baseline smoke test на существование `Electron2D.Node` становится зелёным;
- compatibility table отражает новые public types.

## Отличия от Godot в `T-0009`

- Полная hierarchy/ownership/reparent/delete semantics остаётся в `T-0010`.
- Эта спецификация описывает исходный `T-0009` baseline. Начиная с `T-0027`, `SceneTree.Root` публично остаётся `Node`, но фактический root object является `Viewport`.
- Public `Window` root ещё не реализован.
- Полная Godot pause behavior для всех server subsystems остаётся будущим расширением; текущий baseline покрывает node callbacks, input dispatch и reference project pause workflow.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0009`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime добавляет:

- `Electron2D.Node`
- `Electron2D.SceneTree`
- `Electron2D.InputEvent`
- `Electron2D.ProcessMode`

## `Node`

`Node` наследуется от `Object` и предоставляет:

- `Name`;
- `ProcessMode`;
- `Owner`;
- `AddChild(Node child)`;
- `RemoveChild(Node child)`;
- `GetParent()`;
- `GetChild(int index)`;
- `GetChildCount()`;
- `GetIndex()`;
- `MoveChild(Node childNode, int toIndex)`;
- `Reparent(Node newParent, bool keepGlobalTransform = true)`;
- `IsAncestorOf(Node node)`;
- `QueueFree()`;
- `IsInsideTree()`;
- `GetTree()`;
- `_EnterTree()`;
- `_Ready()`;
- `_Process(double delta)`;
- `_PhysicsProcess(double delta)`;
- `_Input(InputEvent inputEvent)`;
- `_ExitTree()`.

Lifecycle callbacks являются `virtual` и называются только в Electron2D форме с подчёркиванием.

## `SceneTree`

`SceneTree` наследуется от `Object`, имеет `Root` и `Paused`. Публичное свойство `Root` остаётся типизированным как `Node`, но фактический root object теперь является `Viewport` с именем `root`.

Для тестов и внутреннего runtime-кода есть методы запуска обхода дерева:

- `ProcessFrame(double delta)`;
- `PhysicsFrame(double delta)`;
- `DispatchInput(InputEvent inputEvent)`.

## Порядок callbacks

- `_EnterTree()` вызывается parent before child.
- `_Ready()` вызывается child before parent и только один раз на node.
- `_Process()`, `_PhysicsProcess()` и `_Input()` вызываются parent before child.
- `_ExitTree()` вызывается child before parent.

## Pause And Process Mode

`Node.ProcessMode` управляет idle, physics и input callbacks во время паузы:

- `Inherit` наследует effective mode ancestor; root fallback равен `Pausable`;
- `Pausable` работает только при `SceneTree.Paused == false`;
- `WhenPaused` работает только при `SceneTree.Paused == true`;
- `Always` работает независимо от `Paused`;
- `Disabled` не получает эти callbacks.

Drawing pass не отключается паузой: `SceneTree.ProcessFrame()` всё равно выполняет draw traversal, чтобы paused UI мог отрисоваться.

## Ошибки пользовательского кода

Если lifecycle callback бросает исключение, `SceneTree` сохраняет internal diagnostic с node context, callback, failure kind, message и stack trace. Обход остальных node в текущем pass продолжается.

## Ограничения текущего baseline

- Node paths реализуются следующими задачами.
- Diagnostics API пока внутренний и описан в `runtime-diagnostics.md`.
