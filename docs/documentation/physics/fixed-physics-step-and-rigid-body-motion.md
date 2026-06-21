# Fixed physics timestep, basic CCD и one-way platform baseline

`0.1.0 Preview` теперь выполняет физику фиксированными шагами по `1/60` секунды и имеет начальное движение `RigidBody2D` по `LinearVelocity`.

## Что реализовано

- `SceneTree.PhysicsFrame(double delta)` накапливает время и запускает столько fixed ticks по `1/60`, сколько помещается в накопителе.
- `_PhysicsProcess(double delta)` получает `1/60` на каждом fixed tick.
- Остаток времени меньше `1/60` сохраняется до следующего `PhysicsFrame()`.
- `RigidBody2D` двигается по `LinearVelocity * fixedDelta`.
- `RigidBody2D` применяет `AngularVelocity * fixedDelta`, если `LockRotation == false`.
- `Freeze == true` и `Sleeping == true` останавливают движение тела.
- Быстрое движение `RigidBody2D` проверяется через AABB sweep against `StaticBody2D`, поэтому тело останавливается перед статическим препятствием, если пересекло его за один fixed tick.
- При столкновении компонент `LinearVelocity` по нормали столкновения обнуляется.
- `CollisionShape2D.OneWayCollision` блокирует движение сверху вниз и пропускает движение снизу вверх.
- Узел, вызвавший `QueueFree()` во время fixed tick, остаётся живым до конца текущего обхода дерева, но следующие fixed ticks внутри того же `PhysicsFrame()` его уже пропускают.

## Правила collision checks

Baseline использует AABB-границы активных `CollisionShape2D`:

- `Shape != null`;
- `Disabled == false`;
- target body находится внутри `SceneTree`;
- target body не поставлен в очередь удаления;
- moving `RigidBody2D.CollisionMask` пересекается с target `CollisionLayer`;
- self-collision исключён.

Проверка выбирает ближайшее столкновение по пройденной доле движения. Текущий baseline проверяет `RigidBody2D` только against `StaticBody2D`.

## One-way collision

`CollisionShape2D.OneWayCollision` участвует в baseline только для static shapes. Блокировка происходит, если:

- moving body движется вниз, то есть `motion.Y > 0`;
- collision normal направлена вверх;
- нижняя грань moving body до шага находится не ниже верхней грани platform shape с учётом `OneWayCollisionMargin`.

Движение снизу вверх и боковое движение не блокируются one-way shape.

## Ограничения

Это не полноценный physics solver:

- нет impulses, mass solve, restitution и friction combine;
- нет rigid-rigid collision;
- нет точного narrow-phase по реальной форме shape;
- нет contact manifold и shape-level contacts;
- нет gravity integration;
- production kinematic solver narrow-phase остаётся отдельной задачей;
- production Box2D.NET backend пока не выбран.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests"
```

Связанный physics/lifecycle набор:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~NodeSceneTreeLifecycleTests|FullyQualifiedName~DeferredCallTests|FullyQualifiedName~CSharpScriptModelTests|FullyQualifiedName~PhysicsNodeLifecycleTests|FullyQualifiedName~PhysicsMaterialStateTests|FullyQualifiedName~Area2DOverlapSignalTests|FullyQualifiedName~PhysicsDirectSpaceState2DTests|FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```
