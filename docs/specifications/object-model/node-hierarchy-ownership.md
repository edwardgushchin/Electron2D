# Иерархия `Node`, ownership и безопасное удаление

Статус: целевая спецификация.
Задача: `T-0010`.
Обновлено: 2026-06-20.

## Цель

Довести базовую иерархию `Node` до Godot-like поведения для `0.1.0 Preview`: parent-child дерево, `Owner`, reparent/move операции и безопасное отложенное удаление через `QueueFree()`.

## Public API

`Node` должен сохранить Godot-like public API и не возвращать legacy/component surface:

- `Owner`;
- `GetParent()`;
- `GetChild(int index)`;
- `GetChildCount()`;
- `GetIndex()`;
- `AddChild(Node child)`;
- `RemoveChild(Node child)`;
- `MoveChild(Node childNode, int toIndex)`;
- `Reparent(Node newParent, bool keepGlobalTransform = true)`;
- `IsAncestorOf(Node node)`;
- `QueueFree()`.

`Object` должен иметь Godot-like `IsQueuedForDeletion()`, потому что именно через него проверяется состояние, заданное `QueueFree()`.

Публичного свойства `Parent` быть не должно: parent читается через `GetParent()`. `Owner` и parent не являются одним и тем же состоянием.

## Parent-child инварианты

- У node может быть не более одного parent.
- Node нельзя добавить в себя или в своего descendant.
- Direct children одного parent должны иметь уникальные имена.
- `GetChild(int index)` возвращает child по порядку, отрицательный индекс считается с конца.
- `GetIndex()` возвращает текущий индекс node среди siblings или `-1`, если parent отсутствует.
- `MoveChild()` меняет sibling order без изменения parent/tree.
- `RemoveChild()` отсоединяет child subtree от parent/tree, вызывает `_ExitTree()` при необходимости, но не удаляет node.

## Ownership

`Owner` хранит scene ownership для editor/serialization сценариев и не равен parent.

- `Owner` может быть `null`.
- Непустой `Owner` должен быть ancestor текущего node.
- Попытка установить owner, который не является ancestor, должна завершаться ошибкой.
- При `RemoveChild()` owner удалённого node или его descendants сбрасывается в `null`, если прежний owner больше не является ancestor.
- При `Reparent()` owner сохраняется только если после операции он остаётся ancestor; иначе owner сбрасывается.

## Reparent

`Reparent(Node newParent, bool keepGlobalTransform = true)` переносит node к новому parent.

- Node должен уже иметь parent.
- `newParent` не может быть текущим node или descendant текущего node.
- `keepGlobalTransform` принимается для соответствия Godot-like API, но в T-0010 не меняет поведение, потому что `Node2D` ещё не реализован.
- Если перенос меняет присутствие в `SceneTree`, lifecycle callbacks должны следовать уже реализованным правилам `_ExitTree()`/`_EnterTree()`/`_Ready()`.

## Safe deletion

`QueueFree()` помечает node на удаление и делает повторные вызовы безопасными.

- До безопасной точки node остаётся доступным и может завершить текущий lifecycle traversal.
- Удаление выполняется в конце runtime pass `SceneTree`.
- При удалении node удаляются все descendants.
- Удалённые node становятся invalid для `Object.IsInstanceValid()`.
- `RemoveChild()` не удаляет node; повторное добавление отсоединённого node должно оставаться возможным.

## Acceptance tests

- public API не содержит `Parent`, но содержит `GetParent()` и `Owner`;
- `GetChild()`, `GetIndex()` и `MoveChild()` сохраняют корректный sibling order;
- нельзя создать parent cycle через `AddChild()` или `Reparent()`;
- `Owner` может ссылаться только на ancestor и сбрасывается при detach/reparent, если ancestor больше недоступен;
- `Reparent()` переносит node и сохраняет owner, когда owner остаётся ancestor;
- `QueueFree()` безопасен при повторных вызовах и удаляет node subtree после текущего traversal, не прерывая sibling callbacks;
- `RemoveChild()` отсоединяет subtree без удаления и позволяет добавить его снова.
