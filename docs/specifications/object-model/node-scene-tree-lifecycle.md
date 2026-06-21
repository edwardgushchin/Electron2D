# `Node` и `SceneTree`: lifecycle baseline

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

## `Node`

`Node` должен:

- наследоваться от `Object`;
- иметь `Name`;
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
- вводить `Root` в дерево при создании;
- позволять тестам и внутреннему runtime-коду выполнять process, physics и input проходы через непубличный API.

## Порядок lifecycle

- `_EnterTree()` вызывается сверху вниз: parent before child.
- `_Ready()` вызывается снизу вверх: child before parent.
- `_Ready()` вызывается для каждого node не более одного раза.
- `_Process()`, `_PhysicsProcess()` и `_Input()` вызываются сверху вниз.
- `_ExitTree()` при удалении subtree вызывается снизу вверх: child before parent.

## Ошибки пользовательского кода

Если пользовательский lifecycle callback бросает исключение, `SceneTree` должен поймать его, сохранить диагностику для тестов и будущих инструментов редактора и продолжить обход остальных node в текущем lifecycle pass. Исключение не должно молча теряться и не должно обрывать весь loop.

Diagnostic API остаётся внутренним. Централизованное правило внутренней runtime-диагностики уточняется отдельной спецификацией `runtime-diagnostics.md`.

## Acceptance tests

- добавление node к `SceneTree.Root` вызывает `_EnterTree()` и `_Ready()`;
- subtree получает enter/ready в указанном порядке;
- process, physics и input callbacks получают delta/input и идут в tree order;
- `RemoveChild()` вызывает `_ExitTree()` и очищает `GetParent()`/`GetTree()`;
- exception в пользовательском callback сохраняется в diagnostic и не прерывает обход sibling node;
- baseline smoke test на существование `Electron2D.Node` становится зелёным;
- compatibility table отражает новые public types.

## Отличия от Godot в `T-0009`

- Полная hierarchy/ownership/reparent/delete semantics остаётся в `T-0010`.
- Эта спецификация описывает исходный `T-0009` baseline. Начиная с `T-0027`, `SceneTree.Root` публично остаётся `Node`, но фактический root object является `Viewport`.
- Public `Window` root ещё не реализован.
- Pause mode и process mode остаются в следующих задачах.
