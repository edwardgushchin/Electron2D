# Группы `Node` и group calls

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

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

`StringName` уже есть как базовый тип, но текущий group baseline всё ещё принимает group/method names как `string`, а `GetNodesInGroup()` возвращает `Node[]`. Перевод на `StringName`, `Variant` и Electron2D collections должен идти отдельной migration-задачей.

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
- Нет `StringName`/`Variant` binding для group API; это будет заменено в будущих migration-задачах.
