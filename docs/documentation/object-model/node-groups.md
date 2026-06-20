# Группы `Node` и group calls

Статус: реализованный baseline.
Задача: `T-0012`.
Обновлено: 2026-06-20.

## Public API

`Node` предоставляет:

- `AddToGroup(string group, bool persistent = false)`;
- `RemoveFromGroup(string group)`;
- `IsInGroup(string group)`;
- `GetGroups()`.

`SceneTree` предоставляет:

- `GetFirstNodeInGroup(string group)`;
- `GetNodeCountInGroup(string group)`;
- `GetNodesInGroup(string group)`;
- `HasGroup(string name)`;
- `CallGroup(string group, string method, params object?[] args)`.

До появления `StringName`, `Variant` и Godot collections group names и method names представлены `string`, а `GetNodesInGroup()` возвращает `Node[]`.

## Membership

`AddToGroup()` добавляет node в локальный список групп. Повторное добавление не создаёт дубликаты. Если group была добавлена как persistent, повторное добавление без persistent не сбрасывает metadata.

`RemoveFromGroup()` удаляет membership и persistent metadata. `GetGroups()` возвращает копию имён групп, поэтому вызывающий код не может изменить внутреннее состояние node.

Group name не может быть `null`, пустым или whitespace.

## SceneTree queries

`SceneTree` group queries учитывают только nodes, которые сейчас находятся внутри tree. Если subtree отсоединён через `RemoveChild()`, его локальное membership сохраняется, но group queries перестают возвращать эти nodes до повторного добавления в tree.

`GetNodesInGroup()` возвращает nodes в hierarchy order.

## CallGroup

`CallGroup()` вызывает public instance method на каждом node группы в hierarchy order. Nodes без подходящего метода игнорируются.

Пользовательские исключения из вызываемого метода проходят через внутренний механизм диагностики `SceneTree` с типом ошибки `GroupCall`, как и lifecycle callbacks.

## Ограничения текущего baseline

- `CallGroupFlags`, deferred/reverse group calls, `NotifyGroup()` и `SetGroup()` ещё не реализованы.
- Persistent groups сохраняются текущим in-memory `PackedScene` baseline; file-level scene serialization ещё не реализована.
- Нет `StringName`/`Variant` binding; это будет заменено в будущих задачах базовых типов.
