# Иерархия `Node`, ownership и безопасное удаление

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0010`.
Обновлено: 2026-06-20.

## Цель

Довести базовую иерархию `Node` до Electron2D поведения для `0.1.0 Preview`: parent-child дерево, `Owner`, reparent/move операции и безопасное отложенное удаление через `QueueFree()`.

## Public API

`Node` должен сохранить Electron2D public API и не возвращать legacy/component surface:

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

`Object` должен иметь Electron2D `IsQueuedForDeletion()`, потому что именно через него проверяется состояние, заданное `QueueFree()`.

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
- `keepGlobalTransform` принимается для соответствия Electron2D API, но в T-0010 не меняет поведение, потому что `Node2D` ещё не реализован.
- Если перенос меняет присутствие в `SceneTree`, lifecycle callbacks должны следовать уже реализованным правилам `_ExitTree()`/`_EnterTree()`/`_Ready()`.

## Safe deletion

`QueueFree()` помечает node на удаление и делает повторные вызовы безопасными.

- До безопасной точки node остаётся доступным и может завершить текущий lifecycle traversal.
- Удаление выполняется в конце текущего прохода `SceneTree`.
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

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0010`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime реализует Electron2D hierarchy subset:

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

Параметр `keepGlobalTransform` принят для Electron2D сигнатуры. В текущем baseline он не влияет на поведение, потому что `Node2D` и transform API ещё не реализованы.

Если reparent переносит node между tree/orphan состояниями, применяются обычные lifecycle правила `_ExitTree()` и `_EnterTree()`. `_Ready()` повторно не вызывается без отдельного будущего `RequestReady()` API.

## Safe deletion

`QueueFree()` помечает node на удаление. Повторные вызовы безопасны. Если node находится в `SceneTree`, фактическое удаление выполняется в конце текущего прохода дерева после завершения обхода.

При удалении node удаляются все descendants. Удалённые node становятся invalid для `Object.IsInstanceValid()`.

`RemoveChild()` не является удалением: отсоединённый node остаётся valid и может быть добавлен в дерево повторно.

## Ограничения текущего baseline

- `SetDeferred()` и property assignment через deferred queue ещё не реализованы.
- `GetPath()` и `GetPathTo()` ещё не реализованы.
- `PackedScene` уже использует `Owner` для in-memory owned subtree snapshot; file-level serialization остаётся будущей задачей resource pipeline.
