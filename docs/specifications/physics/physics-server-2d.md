# PhysicsServer2D boundary

## Цель

`PhysicsServer2D` в `0.1.0 Preview` должен стать первой публичной Godot-like границей физики. Пользовательский код работает только с `Electron2D.Rid`, `Vector2`, `Transform2D`, `Variant`, `Callable` и другими типами Electron2D. Типы, handles и структуры backend-кандидата Box2D.NET не должны появляться в public API.

Эта спецификация не реализует физические тела, collision shapes, contacts, queries или kinematic solver полностью. Она фиксирует серверную границу, на которую будут опираться следующие задачи физики.

## Область `0.1.0 Preview`

Публичный baseline:

- static class `PhysicsServer2D`;
- enum `PhysicsServer2D.SpaceParameter`;
- enum `PhysicsServer2D.ShapeType`;
- enum `PhysicsServer2D.ProcessInfo`;
- `SetActive(bool active)`;
- `SpaceCreate()`;
- `SpaceSetActive(Rid space, bool active)`;
- `SpaceIsActive(Rid space)`;
- `SpaceSetParam(Rid space, SpaceParameter param, float value)`;
- `SpaceGetParam(Rid space, SpaceParameter param)`;
- `AreaCreate()`;
- `BodyCreate()`;
- `JointCreate()`;
- `WorldBoundaryShapeCreate()`;
- `SeparationRayShapeCreate()`;
- `SegmentShapeCreate()`;
- `CircleShapeCreate()`;
- `RectangleShapeCreate()`;
- `CapsuleShapeCreate()`;
- `ConvexPolygonShapeCreate()`;
- `ConcavePolygonShapeCreate()`;
- `ShapeGetType(Rid shape)`;
- `FreeRid(Rid rid)`;
- `GetProcessInfo(ProcessInfo processInfo)`.

Названия методов и enum соответствуют C#-стилю Godot: PascalCase вместо snake_case, но без Electron2D-only public aliases.

## Backend boundary

Внутренний backend должен быть заменяемым через internal API, доступный runtime и тестам. Public API не предоставляет способ выбрать backend вручную.

Default backend для этой задачи:

- выделяет уникальные `Rid` для spaces, areas, bodies, joints и shapes;
- хранит тип shape для `ShapeGetType()`;
- хранит active state пространства;
- хранит значения `SpaceParameter`;
- освобождает любой RID, созданный через `PhysicsServer2D`;
- возвращает `0` для `GetProcessInfo()` до появления реальной simulation step.

После `FreeRid()` дальнейшие операции с освобождённым RID должны завершаться `ArgumentException`, чтобы ошибка не превращалась в тихую потерю состояния.

## Запрет на утечку Box2D

Автоматическая проверка public API должна подтверждать:

- public типы и public сигнатуры `Electron2D` не содержат namespace или type name с `Box2D`;
- `PhysicsServer2D` использует `Rid` и Godot-like value types, а не backend handles;
- internal `IPhysicsServer2DBackend` и default backend не экспортируются из assembly.

## Не входит в задачу

- реальный physics step;
- `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`;
- данные shape geometry через `ShapeSetData()`;
- direct space state, raycast, point query и shape query;
- collision layers/masks, materials, sleeping, CCD, one-way platforms;
- `CharacterBody2D` и kinematic solver;
- выбор Box2D.NET как production backend.

## Критерии приёмки

- `PhysicsServer2D` public API создаёт и освобождает `Rid`-объекты без публичных backend handles.
- Space active state и `SpaceParameter` round-trip покрыты тестами.
- Shape creation возвращает RID, а `ShapeGetType()` возвращает ожидаемый `ShapeType`.
- Internal backend можно заменить в тесте без изменения public API.
- Reflection-тест подтверждает отсутствие публичных `Box2D` типов и public signatures.
- `docs/documentation/physics/physics-server-2d.md` описывает фактическую реализацию.
- `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и focused tests проходят.
