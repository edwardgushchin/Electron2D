# `NodePath` и разрешение node paths

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0011`.
Обновлено: 2026-06-20.

## Цель

Реализовать Electron2D baseline для `NodePath`, `Node.GetNode()` и `Node.GetNodeOrNull()` без локального compatibility layer и без legacy shortcuts.

## Public API

Новый public surface:

- `Electron2D.NodePath`;
- `NodePath.NodePath()`;
- `NodePath.NodePath(string path)`;
- `NodePath.GetName(int index)`;
- `NodePath.GetNameCount()`;
- `NodePath.GetSubname(int index)`;
- `NodePath.GetSubnameCount()`;
- `NodePath.IsAbsolute()`;
- `NodePath.IsEmpty()`;
- `NodePath.ToString()`;
- `Node.GetNode(NodePath path)`;
- `Node.GetNodeOrNull(NodePath path)`.

Для C# удобства допускается implicit conversion из `string` в `NodePath`, чтобы Electron2D вызовы `GetNode("Child")` компилировались.

## Формат пути

- Имена node разделяются `/`.
- `.` означает текущий node.
- `..` означает parent node.
- Путь с начальным `/` считается absolute и разрешается от `SceneTree.Root`.
- Absolute путь `/root` должен указывать на `SceneTree.Root`.
- Subnames после `:` парсятся в `NodePath`, но `GetNode()` и `GetNodeOrNull()` используют только node-name часть пути.

## Разрешение

- Relative path разрешается от node, на котором вызван `GetNode()` или `GetNodeOrNull()`.
- Absolute path разрешается только если вызывающий node находится внутри `SceneTree`.
- `GetNodeOrNull()` возвращает `null`, если путь не найден.
- `GetNode()` возвращает node или бросает `InvalidOperationException`, если путь не найден.
- После rename node старый path больше не должен разрешаться, новый path должен разрешаться.

## Ограничения текущего baseline

- `StringName` реализуется отдельной задачей базовых типов, но `NodePath.GetName()` и `GetSubname()` временно возвращают `string` до отдельной migration-задачи.
- Property/resource resolution для subnames относится к будущим задачам `Variant`/resource pipeline.
- `GetPath()` и `GetPathTo()` могут быть реализованы отдельной задачей, если они понадобятся release scope.

## Acceptance tests

- `NodePath` парсит absolute/relative имена и subnames.
- Relative `GetNode()` разрешает direct child, nested child, `.`, `..` и sibling path через parent.
- Absolute `GetNode()` разрешает `/root` и descendant path от `SceneTree.Root`.
- Missing path возвращает `null` через `GetNodeOrNull()` и ошибку через `GetNode()`.
- После rename старый path не разрешается, новый path разрешается.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0011`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime добавляет:

- `Electron2D.NodePath`;
- `NodePath.NodePath()`;
- `NodePath.NodePath(string path)`;
- `NodePath.GetName(int index)`;
- `NodePath.GetNameCount()`;
- `NodePath.GetSubname(int index)`;
- `NodePath.GetSubnameCount()`;
- `NodePath.IsAbsolute()`;
- `NodePath.IsEmpty()`;
- `NodePath.ToString()`;
- `Node.GetNode(NodePath path)`;
- `Node.GetNodeOrNull(NodePath path)`.

`NodePath` поддерживает implicit conversion из `string`, поэтому C# вызовы вида `GetNode("Child")` компилируются как Electron2D shorthand.

## Формат пути

Node names разделяются `/`. `.` означает текущий node, `..` означает parent. Путь с начальным `/` считается absolute и разрешается от `SceneTree.Root`.

`SceneTree.Root` сейчас имеет имя `root`, поэтому absolute path к корню выглядит как `/root`.

Subnames после `:` парсятся в `NodePath`, но `GetNode()` и `GetNodeOrNull()` используют только node часть пути. Property/resource resolution будет добавлен позже.

## Разрешение

`GetNodeOrNull()` возвращает найденный node или `null`, если путь не найден. Absolute path возвращает `null`, если вызывающий node не находится внутри `SceneTree`.

`GetNode()` возвращает найденный node или бросает `InvalidOperationException`, если path не найден.

После изменения `Node.Name` старый path больше не разрешается, а новый path разрешается сразу, потому что lookup идёт по текущим sibling names.

## Ограничения текущего baseline

- `StringName` реализован как базовый тип, но `NodePath.GetName()` и `GetSubname()` пока возвращают `string`; перевод этих методов должен идти отдельной задачей с обновлением тестов.
- `GetPath()` и `GetPathTo()` ещё не реализованы.
- Property/resource lookup по subnames ещё не реализован.
