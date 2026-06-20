# `NodePath` и разрешение node paths

Статус: целевая спецификация.
Задача: `T-0011`.
Обновлено: 2026-06-20.

## Цель

Реализовать Godot-like baseline для `NodePath`, `Node.GetNode()` и `Node.GetNodeOrNull()` без локального compatibility layer и без legacy shortcuts.

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

Для C# удобства допускается implicit conversion из `string` в `NodePath`, чтобы Godot-like вызовы `GetNode("Child")` компилировались.

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

- `StringName` ещё не реализован, поэтому `NodePath.GetName()` и `GetSubname()` временно возвращают `string`.
- Property/resource resolution для subnames относится к будущим задачам `Variant`/resource pipeline.
- `GetPath()` и `GetPathTo()` могут быть реализованы отдельной задачей, если они понадобятся release scope.

## Acceptance tests

- `NodePath` парсит absolute/relative имена и subnames.
- Relative `GetNode()` разрешает direct child, nested child, `.`, `..` и sibling path через parent.
- Absolute `GetNode()` разрешает `/root` и descendant path от `SceneTree.Root`.
- Missing path возвращает `null` через `GetNodeOrNull()` и ошибку через `GetNode()`.
- После rename старый path не разрешается, новый path разрешается.
