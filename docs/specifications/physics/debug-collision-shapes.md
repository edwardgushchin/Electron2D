# Debug collision shapes baseline

## Цель

`0.1.0 Preview` должен дать будущему редактору и тестовому окружению проверяемый способ включить отображение collision shapes и получить данные для debug visualization без доступа к внутренним объектам physics backend.

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
