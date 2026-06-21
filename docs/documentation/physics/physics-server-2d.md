# PhysicsServer2D boundary

`PhysicsServer2D` - текущая публичная Godot-like граница низкоуровневой 2D-физики. В `0.1.0 Preview` она создаёт и освобождает physics resources через `Rid`, но ещё не выполняет реальную симуляцию столкновений.

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
- raycast, point query и shape query;
- `CharacterBody2D` и kinematic solver;
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
