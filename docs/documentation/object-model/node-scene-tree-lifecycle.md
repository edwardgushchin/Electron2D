# `Node` и `SceneTree`: lifecycle baseline

Статус: реализованный baseline.
Задача: `T-0009`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime добавляет:

- `Electron2D.Node`
- `Electron2D.SceneTree`
- `Electron2D.InputEvent`

## `Node`

`Node` наследуется от `Object` и предоставляет:

- `Name`;
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

Lifecycle callbacks являются `virtual` и называются только в Godot-like форме с подчёркиванием.

## `SceneTree`

`SceneTree` наследуется от `Object` и имеет `Root`. На этом этапе `Root` является обычным `Node`, потому что `Viewport` ещё не реализован.

Для тестового/runtime host есть internal traversal:

- `ProcessFrame(double delta)`;
- `PhysicsFrame(double delta)`;
- `DispatchInput(InputEvent inputEvent)`.

## Порядок callbacks

- `_EnterTree()` вызывается parent before child.
- `_Ready()` вызывается child before parent и только один раз на node.
- `_Process()`, `_PhysicsProcess()` и `_Input()` вызываются parent before child.
- `_ExitTree()` вызывается child before parent.

## Ошибки пользовательского кода

Если lifecycle callback бросает исключение, `SceneTree` сохраняет internal diagnostic и продолжает обход остальных node в текущем pass. Публичная diagnostics API будет выделена отдельной задачей; текущий internal baseline нужен для редактора/test host и для того, чтобы loop не обрывался молча.

## Отличия от Godot в текущем baseline

- Node paths реализуются следующими задачами.
- `SceneTree.Root` пока не `Viewport`.
- Pause/process modes пока не реализованы.
- `SceneTree.Root` пока не `Viewport`.
- Diagnostics API пока internal.
