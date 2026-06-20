# `PackedScene` и смена активной сцены

Статус: целевая спецификация.
Задача: `T-0015`.
Обновлено: 2026-06-20.

## Цель

Реализовать минимальный Godot-like baseline для `PackedScene`: упаковка node tree в ресурс сцены, instancing сохранённого дерева и смена активной сцены через `SceneTree`.

## Public API

Новый public surface:

- `PackedScene : Resource`;
- `Error PackedScene.Pack(Node path)`;
- `bool PackedScene.CanInstantiate()`;
- `Node? PackedScene.Instantiate()`;
- `Node? SceneTree.CurrentScene`;
- `Error SceneTree.ChangeSceneToPacked(PackedScene packedScene)`.

Editor-only `GenEditState`, inherited scenes и file-level `ResourceSaver`/`ResourceLoader` не входят в baseline `T-0015`.

## Packing semantics

- `Pack(Node path)` сохраняет root node и descendants, которые принадлежат сохраняемой сцене через `Owner`.
- Root node сохраняется всегда.
- Descendant сохраняется только если его parent уже сохранён и его `Owner` указывает на сохранённого ancestor.
- Runtime-only descendants с `Owner == null` не попадают в packed scene.
- `Name`, persistent group metadata и owner relationships сохраняются.
- Previous packed data заменяется новым результатом `Pack()`.

## Instancing semantics

- `CanInstantiate()` возвращает `true`, если packed scene содержит root node.
- `Instantiate()` возвращает новый detached node tree.
- Новый instance не переиспользует исходные node instances.
- Owner relationships внутри instance должны указывать на cloned ancestors, а не на исходную сцену.
- Если stored node type нельзя создать через parameterless constructor, `Instantiate()` возвращает `null`.

## Scene change semantics

- `SceneTree.CurrentScene` хранит текущую активную main scene или `null`.
- `ChangeSceneToPacked()` принимает только packed scene, которую можно instantiate.
- Текущая сцена удаляется из `Root` сразу и освобождается через queued deletion в конце текущего pass.
- Новый instance добавляется в `Root` и становится `CurrentScene` в конце текущего pass.
- Если смена сцены запрошена вне обхода, она применяется при следующем проходе `SceneTree`.

## Ограничения текущего baseline

- Сериализация в файл и загрузка по path остаются частью будущего resource pipeline.
- Custom script state и constructor arguments не сериализуются.
- Node references/properties за пределами текущего minimal `Node` surface не сериализуются до появления `Variant` и property database.

## Acceptance tests

- `Pack()` сохраняет root и owned descendants, исключая runtime-only descendants без `Owner`.
- `Instantiate()` создаёт новый tree, сохраняет `Name`, persistent groups и owner relationships.
- `CanInstantiate()` меняется после успешного `Pack()`.
- `ChangeSceneToPacked()` удаляет текущую сцену, добавляет новый instance и обновляет `CurrentScene`.
- Invalid packed scene не меняет текущую сцену.
