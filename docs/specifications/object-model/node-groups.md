# Группы `Node` и group calls

Статус: целевая спецификация.
Задача: `T-0012`.
Обновлено: 2026-06-20.

## Цель

Реализовать Electron2D baseline для групп node: локальное членство node, persistent group metadata и базовые `SceneTree` group queries/calls.

## Public API

`Node`:

- `AddToGroup(string group, bool persistent = false)`;
- `RemoveFromGroup(string group)`;
- `IsInGroup(string group)`;
- `GetGroups()`.

`SceneTree`:

- `GetFirstNodeInGroup(string group)`;
- `GetNodeCountInGroup(string group)`;
- `GetNodesInGroup(string group)`;
- `HasGroup(string name)`;
- `CallGroup(string group, string method, params object?[] args)`.

`StringName` уже есть как базовый тип, но текущий group baseline всё ещё принимает group/method names как `string`, а `GetNodesInGroup()` возвращает массив `Node[]`. Перевод на `StringName`, `Variant` и Godot collections должен идти отдельной migration-задачей.

## Инварианты

- Group name не может быть пустым или whitespace.
- Повторный `AddToGroup()` не создаёт дубликаты.
- Если group была добавлена как persistent, повторный non-persistent add не сбрасывает persistent metadata.
- `RemoveFromGroup()` удаляет членство и persistent metadata.
- `GetGroups()` возвращает имена групп node без exposing mutable internal state.
- `SceneTree` group methods учитывают только nodes, которые сейчас находятся внутри tree.
- `GetNodesInGroup()` возвращает nodes в scene hierarchy order.
- `CallGroup()` вызывает метод на nodes в scene hierarchy order и игнорирует nodes, у которых нет подходящего метода.

## Persistent metadata

Persistent flag используется текущим `PackedScene` baseline для сохранения group membership и остаётся metadata для будущего file-level serialization pipeline.

## Ограничения текущего baseline

- `CallGroup()` использует reflection и `object?[]`; перевод на `StringName`, `Variant` и script binding остаётся будущей задачей.
- `CallGroupFlags`, deferred/reverse group calls, `NotifyGroup()` и `SetGroup()` остаются будущими задачами.
- Persistence сохраняется в in-memory `PackedScene`; file-level scene serialization ещё не реализована.

## Acceptance tests

- `AddToGroup()`, `RemoveFromGroup()`, `IsInGroup()` и `GetGroups()` работают без дубликатов.
- Persistent flag сохраняется и не сбрасывается повторным non-persistent add.
- `SceneTree` возвращает group nodes только для nodes inside tree и в hierarchy order.
- `RemoveChild()` исключает subtree из group queries, но не удаляет локальное membership.
- `CallGroup()` вызывает matching method в hierarchy order и игнорирует nodes без метода.
