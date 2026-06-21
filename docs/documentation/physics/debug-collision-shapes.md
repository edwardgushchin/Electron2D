# Debug collision shapes baseline

## Текущее состояние

`0.1.0 Preview` предоставляет минимальный runtime hook для будущего отображения collision shapes в редакторе и для автоматических diagnostics checks. Внешний API состоит из двух публичных свойств:

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
