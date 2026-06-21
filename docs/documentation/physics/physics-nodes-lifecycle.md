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
- `StaticBody2D` хранит `ConstantLinearVelocity` и `ConstantAngularVelocity`.
- `RigidBody2D` хранит базовые Godot-like свойства: `Mass`, `Inertia`, `CenterOfMass`, `CenterOfMassMode`, `GravityScale`, `LinearVelocity`, `AngularVelocity`, `Freeze`, `FreezeMode`, `Sleeping`, `CanSleep`, `LockRotation`.
- `Area2D` хранит `Monitoring`, `Monitorable` и `Priority`.
- `CollisionShape2D` хранит `Shape`, `Disabled`, `OneWayCollision` и `OneWayCollisionMargin`.
- `RayCast2D` хранит настройки query. До реализации raycast backend result methods возвращают пустое состояние: `IsColliding() == false`, `GetCollider() == null`, empty `Rid`, нулевой shape index и `Vector2.Zero` для point/normal.
- `Shape2D` является abstract `Resource` base для будущих concrete shape resources.

## Внутренний lifecycle hook

`Node` вызывает внутренний `ISceneTreeLifecycleHandler` до пользовательского `_EnterTree()`, после пользовательского `_PhysicsProcess()` и после пользовательского `_ExitTree()`. Это внутренний механизм движка, недоступный игре как public API. Он нужен, чтобы engine-owned RID создавался и освобождался независимо от того, переопределяет ли пользователь lifecycle callbacks.

## Что ещё не реализовано

- concrete shapes `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D`, `ConcavePolygonShape2D`;
- запись geometry shape в physics backend;
- collision solver, contacts, overlap signals, collision layers/masks в расчёте столкновений;
- raycast, point query и shape query;
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
