# Physics nodes lifecycle baseline

`StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D` и `Shape2D` - текущий Godot-like baseline физических узлов для `0.1.0 Preview`.

## Что реализовано

- `CollisionObject2D` наследуется от `Node2D` и является общей базой для `Area2D` и `PhysicsBody2D`.
- `PhysicsBody2D` является общей базой для `StaticBody2D` и `RigidBody2D`.
- `StaticBody2D`, `RigidBody2D` и `Area2D` создают physics RID при входе в `SceneTree`.
- При выходе из дерева, `Free()` или `QueueFree()` RID освобождается через `PhysicsServer2D.FreeRid()`.
- Повторное добавление узла в дерево создаёт новый RID.
- `CollisionObject2D.GetRid()` возвращает текущий RID или пустой `Rid`, если узел не находится в дереве.
- Во время `SceneTree.PhysicsFrame()` `CollisionObject2D` передаёт свой `GlobalTransform` во внутренний physics backend.
- `QueueFree()` внутри `_PhysicsProcess()` безопасен: узел удаляется после завершения текущего обхода дерева, а RID освобождается во время flush delete queue.
- `CollisionLayer` и `CollisionMask` хранятся на `CollisionObject2D`, но ещё не участвуют в solver.
- `CollisionObject2D` синхронизирует collision filter во внутренний physics backend во время physics frame.
- `StaticBody2D` хранит `ConstantLinearVelocity` и `ConstantAngularVelocity`.
- `RigidBody2D` хранит базовые Godot-like свойства: `Mass`, `Inertia`, `CenterOfMass`, `CenterOfMassMode`, `GravityScale`, `LinearVelocity`, `AngularVelocity`, `Freeze`, `FreezeMode`, `Sleeping`, `CanSleep`, `LockRotation`.
- `PhysicsBody2D` хранит `PhysicsMaterialOverride`, а `RigidBody2D` синхронизирует `GravityScale`, `Sleeping` и `CanSleep` во внутренний body-state snapshot.
- `Area2D` хранит `Monitoring`, `Monitorable` и `Priority`, регистрирует built-in overlap signals и поддерживает body/area overlap helper methods.
- `CollisionShape2D` хранит `Shape`, `Disabled`, `OneWayCollision` и `OneWayCollisionMargin`.
- `CollisionShape2D` проверяет, что `ConcavePolygonShape2D` используется только под `StaticBody2D`.
- `RayCast2D` хранит настройки query, выполняет AABB query через `PhysicsDirectSpaceState2D` и возвращает cached result через `IsColliding()`, `GetCollider()`, `GetColliderRid()`, `GetColliderShape()`, `GetCollisionPoint()`, `GetCollisionNormal()`.
- `Shape2D` является abstract `Resource` base для concrete shape resources и лениво создаёт shape `Rid` через `PhysicsServer2D`.

## Внутренний lifecycle hook

`Node` вызывает внутренний `ISceneTreeLifecycleHandler` до пользовательского `_EnterTree()`, после пользовательского `_PhysicsProcess()` и после пользовательского `_ExitTree()`. Это внутренний механизм движка, недоступный игре как public API. Он нужен, чтобы engine-owned RID создавался и освобождался независимо от того, переопределяет ли пользователь lifecycle callbacks.

## Что ещё не реализовано

- запись geometry concrete shapes в production physics backend;
- collision solver, contacts, shape-level overlap signals и точная narrow-phase проверка;
- точный raycast/point/shape narrow-phase и production backend queries;
- `CharacterBody2D` и kinematic solver;
- production backend на Box2D.NET.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~PhysicsNodeLifecycleTests|FullyQualifiedName~PhysicsServer2DTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```
