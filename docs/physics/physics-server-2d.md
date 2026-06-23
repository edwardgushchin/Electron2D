# PhysicsServer2D boundary

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`PhysicsServer2D` в `0.1.0 Preview` должен стать первой публичной Electron2D границей физики. Пользовательский код работает только с `Electron2D.Rid`, `Vector2`, `Transform2D`, `Variant`, `Callable` и другими типами Electron2D. Типы, handles и структуры backend-кандидата Box2D.NET не должны появляться в public API.

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
- `PhysicsServer2D` использует `Rid` и Electron2D value types, а не backend handles;
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
- `docs/physics/physics-server-2d.md` описывает фактическую реализацию.
- `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и focused tests проходят.

## Фактическое состояние, ограничения и проверки

`PhysicsServer2D` - текущая публичная граница низкоуровневой 2D-физики. В `0.1.0 Preview` она создаёт и освобождает physics resources через `Rid`, но ещё не выполняет реальную симуляцию столкновений.

## Что реализовано

- `PhysicsServer2D.SpaceCreate()` создаёт space RID.
- `SpaceSetActive()` и `SpaceIsActive()` хранят active state space.
- `SpaceSetParam()` и `SpaceGetParam()` выполняют round-trip значений `PhysicsServer2D.SpaceParameter`.
- `AreaCreate()`, `BodyCreate()` и `JointCreate()` создают opaque RID для будущих area/body/joint операций.
- `WorldBoundaryShapeCreate()`, `SeparationRayShapeCreate()`, `SegmentShapeCreate()`, `CircleShapeCreate()`, `RectangleShapeCreate()`, `CapsuleShapeCreate()`, `ConvexPolygonShapeCreate()` и `ConcavePolygonShapeCreate()` создают shape RID.
- `ShapeGetType()` возвращает `PhysicsServer2D.ShapeType` для shape RID.
- `FreeRid()` освобождает RID, созданный текущим physics backend.
- `GetProcessInfo(ActiveObjects)` возвращает количество живых area/body RID; остальные значения пока возвращают `0`, потому что real simulation step ещё не реализован.

## Internal backend boundary

Публичный API не содержит Box2D.NET handles и не даёт пользователю выбирать backend. Внутри runtime есть `IPhysicsServer2DBackend`: это внутренний контракт между public facade и будущей реализацией физики. Он нужен тестам и runtime-коду, но не является API для игры.

Текущий default backend `ManagedPhysicsServer2DBackend` хранит resource registry: какие RID живы, какой у них kind, какой `ShapeType` у shape, последний transform для area/body RID, collision filter и body-state snapshot. Он намеренно не рассчитывает контакты, velocity, gravity и sleeping transitions.

## Ошибки RID

Операции с RID, который не был создан текущим backend или уже освобождён через `FreeRid()`, завершаются `ArgumentException`. Это защищает `0.1.0 Preview` от тихой потери состояния до появления полноценной диагностики physics server.

## Что ещё не реализовано

- запись geometry из `Shape2D` resources в production backend;
- physics step, contacts, material combine, collision response, sleeping transitions и CCD;
- production narrow-phase raycast, point query и shape query;
- production kinematic solver narrow-phase;
- production backend на Box2D.NET.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~PhysicsServer2DTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```
