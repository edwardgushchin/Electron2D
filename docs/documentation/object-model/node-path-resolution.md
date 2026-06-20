# `NodePath` и разрешение node paths

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

`NodePath` поддерживает implicit conversion из `string`, поэтому C# вызовы вида `GetNode("Child")` компилируются как Godot-like shorthand.

## Формат пути

Node names разделяются `/`. `.` означает текущий node, `..` означает parent. Путь с начальным `/` считается absolute и разрешается от `SceneTree.Root`.

`SceneTree.Root` сейчас имеет имя `root`, поэтому absolute path к корню выглядит как `/root`.

Subnames после `:` парсятся в `NodePath`, но `GetNode()` и `GetNodeOrNull()` используют только node часть пути. Property/resource resolution будет добавлен позже.

## Разрешение

`GetNodeOrNull()` возвращает найденный node или `null`, если путь не найден. Absolute path возвращает `null`, если вызывающий node не находится внутри `SceneTree`.

`GetNode()` возвращает найденный node или бросает `InvalidOperationException`, если path не найден.

После изменения `Node.Name` старый path больше не разрешается, а новый path разрешается сразу, потому что lookup идёт по текущим sibling names.

## Ограничения текущего baseline

- `StringName` ещё не реализован; `NodePath.GetName()` и `GetSubname()` временно возвращают `string`.
- `GetPath()` и `GetPathTo()` ещё не реализованы.
- Property/resource lookup по subnames ещё не реализован.
