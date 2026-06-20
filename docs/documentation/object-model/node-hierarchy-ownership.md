# Иерархия `Node`, ownership и безопасное удаление

Статус: реализованный baseline.
Задача: `T-0010`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime реализует Godot-like hierarchy subset:

- `Node.Owner`;
- `Node.GetParent()`;
- `Node.GetChild(int index)`;
- `Node.GetChildCount()`;
- `Node.GetIndex()`;
- `Node.AddChild(Node child)`;
- `Node.RemoveChild(Node child)`;
- `Node.MoveChild(Node childNode, int toIndex)`;
- `Node.Reparent(Node newParent, bool keepGlobalTransform = true)`;
- `Node.IsAncestorOf(Node node)`;
- `Node.QueueFree()`;
- `Object.IsQueuedForDeletion()`.

Публичного `Parent` property нет. Parent читается через `GetParent()`, а `Owner` хранится отдельно и используется для будущих editor/serialization сценариев.

## Иерархия

`AddChild()` добавляет node в direct children текущего node. Node не может иметь больше одного parent, не может быть добавлен в себя и не может быть добавлен в свой descendant.

Sibling names поддерживаются уникальными на уровне direct parent. Если добавляемый child конфликтует по имени, runtime назначает уникальный suffix.

`GetChild(int index)` поддерживает отрицательный индекс от конца списка и возвращает `null`, если index вне диапазона. `GetIndex()` возвращает sibling index или `-1`, если parent отсутствует.

`MoveChild()` меняет порядок child среди siblings без изменения parent и без lifecycle callbacks.

## Ownership

`Owner` может быть `null` или ancestor текущего node. Попытка назначить owner, который не является ancestor, завершается `InvalidOperationException`.

`RemoveChild()` отсоединяет subtree и вызывает `_ExitTree()` при необходимости, но не удаляет node. После detach owner сбрасывается только у тех node, для которых прежний owner больше не является ancestor. Если owner остаётся ancestor внутри отсоединённого subtree, он сохраняется.

## Reparent

`Reparent()` переносит node к новому parent. Node должен уже иметь parent, а новый parent не может быть текущим node или descendant текущего node.

Параметр `keepGlobalTransform` принят для Godot-like сигнатуры. В текущем baseline он не влияет на поведение, потому что `Node2D` и transform API ещё не реализованы.

Если reparent переносит node между tree/orphan состояниями, применяются обычные lifecycle правила `_ExitTree()` и `_EnterTree()`. `_Ready()` повторно не вызывается без отдельного будущего `RequestReady()` API.

## Safe deletion

`QueueFree()` помечает node на удаление. Повторные вызовы безопасны. Если node находится в `SceneTree`, фактическое удаление выполняется в конце текущего runtime pass после завершения traversal.

При удалении node удаляются все descendants. Удалённые node становятся invalid для `Object.IsInstanceValid()`.

`RemoveChild()` не является удалением: отсоединённый node остаётся valid и может быть добавлен в дерево повторно.

## Ограничения текущего baseline

- `SetDeferred()` и property assignment через deferred queue ещё не реализованы.
- `GetPath()` и `GetPathTo()` ещё не реализованы.
- `PackedScene` уже использует `Owner` для in-memory owned subtree snapshot; file-level serialization остаётся будущей задачей resource pipeline.
