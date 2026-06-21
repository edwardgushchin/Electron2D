# CharacterBody2D kinematic movement baseline

`CharacterBody2D` в `0.1.0 Preview` даёт управляемому персонажу начальный kinematic solver поверх managed AABB-проверок. Тело не двигается силами: игровой код задаёт `Velocity`, вызывает `MoveAndSlide()` внутри `_PhysicsProcess()`, а runtime двигает узел, останавливает его перед статической геометрией и обновляет состояние пола, стены и потолка.

## Что реализовано

- `CharacterBody2D` наследуется от `PhysicsBody2D` и создаёт собственный body RID с внутренним kind `Character`.
- `PhysicsBody2D.MoveAndCollide(Vector2 motion, bool testOnly = false, float safeMargin = 0.08f, bool recoveryAsCollision = false)` выполняет AABB sweep against `StaticBody2D`.
- `MoveAndCollide()` возвращает `KinematicCollision2D` с collider, collider RID, shape index, normal, travel, remainder, collider velocity и local shape.
- `testOnly == true` сообщает столкновение без изменения `Position`.
- `CharacterBody2D.MoveAndSlide()` применяет `Velocity * 1/60`, выполняет до `MaxSlides` slide-итераций и обновляет `Velocity` после столкновений.
- `IsOnFloor()`, `IsOnWall()`, `IsOnCeiling()` и варианты `Only` отражают состояние последнего `MoveAndSlide()`.
- `GetFloorNormal()`, `GetWallNormal()`, `GetFloorAngle()`, `GetLastMotion()`, `GetPositionDelta()`, `GetRealVelocity()`, `GetSlideCollisionCount()`, `GetSlideCollision()` и `GetLastSlideCollision()` возвращают данные последнего движения.
- `FloorSnapLength` выполняет дополнительное движение против `UpDirection`, если тело не движется вверх и после основного шага не стоит на полу.
- `PlatformFloorLayers` и `StaticBody2D.ConstantLinearVelocity` попадают в `GetPlatformVelocity()` для floor collisions.
- `SegmentShape2D` использует нормаль сегмента для базовой slope-классификации; остальные shape types используют AABB normal.

## Baseline solver

Текущая реализация намеренно остаётся простым managed baseline:

- участвуют только active `CollisionShape2D` с `Shape != null` и `Disabled == false`;
- moving body проверяется только against `StaticBody2D`;
- self-collision исключён;
- `CollisionMask` moving body должен пересекаться с `CollisionLayer` target body;
- `safeMargin` расширяет target bounds перед sweep;
- `CollisionShape2D.OneWayCollision` блокирует движение сверху вниз и пропускает движение снизу вверх;
- collision выбирается по ближайшей пройденной доле движения.

## Ограничения

- нет production narrow-phase по реальной геометрии shape;
- нет character-character и character-rigid push;
- нет material combine, friction/restitution response и gravity integration;
- `FloorStopOnSlope`, `FloorConstantSpeed`, `FloorBlockOnWall`, `WallMinSlideAngle` и `PlatformOnLeave` сейчас фиксируют public API, но их сложное поведение ещё не реализовано;
- `recoveryAsCollision` сохраняет форму API, но отдельной recovery-фазы пока нет.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~CharacterBody2DKinematicSolverTests"
```

Связанный physics/lifecycle набор:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~PhysicsNodeLifecycleTests|FullyQualifiedName~PhysicsMaterialStateTests|FullyQualifiedName~Area2DOverlapSignalTests|FullyQualifiedName~PhysicsDirectSpaceState2DTests|FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests|FullyQualifiedName~CharacterBody2DKinematicSolverTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```
