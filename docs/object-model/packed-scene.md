# `PackedScene` и смена активной сцены

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0015`.
Обновлено: 2026-06-20.

## Цель

Реализовать минимальный Electron2D baseline для `PackedScene`: упаковка node tree в ресурс сцены, instancing сохранённого дерева и смена активной сцены через `SceneTree`.

## Public API

Новый public surface:

- `PackedScene : Resource`;
- `Error PackedScene.Pack(Node path)`;
- `bool PackedScene.CanInstantiate()`;
- `Node? PackedScene.Instantiate()`;
- `Node? SceneTree.CurrentScene`;
- `Error SceneTree.ChangeSceneToPacked(PackedScene packedScene)`.

Editor-only `GenEditState`, inherited scenes и file-level `ResourceSaver`/`ResourceLoader` не входят в baseline `T-0015`. Внутренний file-level scene document реализуется отдельной ресурсной задачей `T-0041`.

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

- Public сериализация в файл и загрузка по path остаются частью будущего resource pipeline.
- Internal `SceneFileDocument` для будущего loader/saver baseline задаётся в ресурсной спецификации `scene-resource-serialization.md`.
- Custom script state и constructor arguments не сериализуются.
- Node references/properties за пределами текущего minimal `Node` surface не сериализуются до появления `Variant` и property database.

## Acceptance tests

- `Pack()` сохраняет root и owned descendants, исключая runtime-only descendants без `Owner`.
- `Instantiate()` создаёт новый tree, сохраняет `Name`, persistent groups и owner relationships.
- `CanInstantiate()` меняется после успешного `Pack()`.
- `ChangeSceneToPacked()` удаляет текущую сцену, добавляет новый instance и обновляет `CurrentScene`.
- Invalid packed scene не меняет текущую сцену.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0015`.
Обновлено: 2026-06-20.

## Public API

Текущий runtime реализует Electron2D scene resource subset:

- `PackedScene : Resource`;
- `PackedScene.Pack(Node path)`;
- `PackedScene.CanInstantiate()`;
- `PackedScene.Instantiate()`;
- `SceneTree.CurrentScene`;
- `SceneTree.ChangeSceneToPacked(PackedScene packedScene)`.

`PackedScene` наследуется от `Resource`, как Electron2D ресурс сцены. File-level `ResourceSaver`/`ResourceLoader` ещё не реализованы, поэтому текущий public baseline работает как in-memory scene resource.

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
