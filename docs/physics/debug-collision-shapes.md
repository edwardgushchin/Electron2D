# Debug collision shapes baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1-preview` должен дать будущему редактору и тестовому окружению проверяемый способ включить отображение collision shapes и получить данные для debug visualization без доступа к внутренним объектам physics backend.

Публичный API ограничивается уже согласованными runtime-именами:

- `SceneTree.DebugCollisionsHint: bool`;
- `CollisionShape2D.DebugColor: Color`.

Фактический список debug shapes остаётся внутренним механизмом движка. Под ним понимается снимок управляемых .NET-объектов, который нужен редактору, тестам и будущему debug bridge, но не становится public API игры.

## Поведение

- `SceneTree.DebugCollisionsHint == false` отключает сбор debug shapes и возвращает пустой внутренний снимок.
- `SceneTree.DebugCollisionsHint == true` разрешает внутренний снимок collision shapes для текущего `SceneTree`.
- `CollisionShape2D.DebugColor` хранит override color для debug visualization.
- `Color.Transparent` означает отсутствие override color; внутренний снимок использует default debug color.
- Snapshot включает `CollisionShape2D` с `Shape != null`, даже если `Disabled == true`, чтобы editor diagnostics мог показать, что shape есть, но выключен.
- Snapshot не включает `CollisionShape2D` без `Shape`.
- Snapshot сортируется в порядке обхода scene tree и shape index внутри owner.
- Snapshot содержит только managed scene objects и geometry data: owner node, shape node, shape resource, shape index, world-space AABB bounds, debug color и disabled flag.
- Snapshot не содержит native pointers, identifiers внутреннего physics backend или изменяемые backend objects.

## Ограничения

- Debug drawing commands в renderer пока не создаются автоматически.
- Editor UI toggle пока не реализуется, только runtime hook и internal snapshot.
- Snapshot использует те же managed AABB bounds, что и query baseline; точная геометрия отрисовки shape остаётся будущей задачей.
- Project settings для default debug color пока не реализуются.

## Критерии приёмки

- `SceneTree.DebugCollisionsHint` документирован и управляет доступностью internal snapshot.
- `CollisionShape2D.DebugColor` документирован и попадает в snapshot.
- Transparent debug color заменяется default debug color в snapshot.
- Disabled collision shape попадает в snapshot с `Disabled == true`.
- Collision shape без `Shape` не попадает в snapshot.
- Snapshot не раскрывает backend handles.
- Добавлены integration tests.
- Обновлена implementation documentation.

## Фактическое состояние, ограничения и проверки

## Текущее состояние

`0.1-preview` предоставляет минимальный runtime hook для будущего отображения collision shapes в редакторе и для автоматических diagnostics checks. Внешний API состоит из двух публичных свойств:

- `SceneTree.DebugCollisionsHint` - включает или отключает внутренний снимок collision shapes для дерева сцены.
- `CollisionShape2D.DebugColor` - задаёт цвет override для debug visualization конкретной формы.

Список форм для отрисовки остаётся внутренним механизмом. Под "внутренним снимком" здесь понимается набор управляемых .NET-объектов, доступный тестам и будущему editor viewport: владелец collision object, узел формы, ресурс формы, индекс формы, bounds в мировых координатах, цвет и признак `Disabled`.

## Правила снимка

- Если `SceneTree.DebugCollisionsHint == false`, дерево возвращает пустой внутренний снимок.
- Если `SceneTree.DebugCollisionsHint == true`, дерево собирает формы при обходе текущего `Root`.
- `CollisionShape2D` без `Shape` пропускается.
- `CollisionShape2D.Disabled == true` не исключает форму из снимка: редактор должен иметь возможность показать, что форма существует, но выключена для physics checks.
- `CollisionShape2D.DebugColor == Color.Transparent` заменяется на default debug color.
- Порядок форм соответствует обходу scene tree и индексу формы внутри владельца collision object.
- Снимок не содержит native pointers, identifiers внутреннего physics backend или изменяемые объекты backend.

## Ограничения

- Renderer не создаёт draw commands автоматически.
- Editor UI toggle будет реализован отдельной задачей viewport.
- Геометрия снимка пока ограничена AABB bounds, которые уже используются текущими physics query checks.
- Project settings для default debug color пока не реализованы.

## Проверки

- `PhysicsDebugCollisionShapesTests.DebugCollisionsHintControlsCollisionShapeSnapshot`
- `PhysicsDebugCollisionShapesTests.DebugCollisionSnapshotIncludesDisabledShapesAndDefaultColor`
- `PhysicsDebugCollisionShapesTests.DebugCollisionSnapshotSkipsEmptyShapesAndDoesNotExposeBackendHandles`
