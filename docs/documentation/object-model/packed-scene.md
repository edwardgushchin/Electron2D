# `PackedScene` и смена активной сцены

Статус: реализованный baseline.
Задача: `T-0015`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime реализует Godot-like scene resource subset:

- `PackedScene : Resource`;
- `PackedScene.Pack(Node path)`;
- `PackedScene.CanInstantiate()`;
- `PackedScene.Instantiate()`;
- `SceneTree.CurrentScene`;
- `SceneTree.ChangeSceneToPacked(PackedScene packedScene)`.

`PackedScene` наследуется от `Resource`, как Godot-like ресурс сцены. File-level `ResourceSaver`/`ResourceLoader` ещё не реализованы, поэтому текущий public baseline работает как in-memory scene resource.

Внутренний scene file document для будущих loader/saver и editor задач уже реализован в ресурсном домене и описан в [Сериализация сцен, ресурсов и переносимых property values](../resources/scene-resource-serialization.md).

## Pack

`Pack(Node path)` сохраняет root node и owned descendants. Root сохраняется всегда. Descendant попадает в сцену только если его parent уже сохранён и его `Owner` указывает на сохранённого ancestor.

Runtime-only descendants без `Owner` не сохраняются. Это соответствует роли `Owner` как scene ownership metadata, а не как синонима parent.

Сохраняются:

- concrete node type, если его можно создать через parameterless constructor;
- `Name`;
- persistent groups;
- owner relationships внутри сохранённой сцены.

Previous packed data заменяется новым успешным `Pack()`.

## Instantiate

`CanInstantiate()` возвращает `true`, если `PackedScene` содержит root node.

`Instantiate()` возвращает новый detached node tree. Клоны не переиспользуют исходные node instances. Owner relationships переназначаются на cloned ancestors.

Если один из сохранённых node types нельзя создать через parameterless constructor, `Instantiate()` возвращает `null`.

## ChangeSceneToPacked

`SceneTree.CurrentScene` хранит текущую active main scene или `null`.

`ChangeSceneToPacked()` принимает только packed scene, которую можно instantiate. Invalid scene возвращает `Error.InvalidParameter` и не меняет текущую сцену.

При успешном вызове текущая сцена удаляется из `Root` сразу, но освобождается через queued deletion в конце следующего прохода дерева. Новый instance добавляется в `Root` и становится `CurrentScene` в конце этого же прохода. Если смена сцены запрошена вне обхода, она применяется при следующем `ProcessFrame()`, `PhysicsFrame()` или `DispatchInput()`.

## Ограничения текущего baseline

- Public загрузка/сохранение `PackedScene` по path ещё не реализована.
- Внутренний `SceneFileDocument` уже умеет stable JSON round-trip для nodes, persistent groups, owner/parent ids, properties и resource references, но пока не подключён к public `PackedScene`.
- Inherited scenes и editor-only instantiate modes не реализованы.
- Custom script state, exported properties и external node references ждут `Variant`/property database/resource pipeline.
