# Physics nodes lifecycle baseline

## Цель

`0.1.0 Preview` должен предоставить Godot-like узлы верхнего уровня для 2D-физики:

- `StaticBody2D`;
- `RigidBody2D`;
- `Area2D`;
- `CollisionShape2D`;
- `RayCast2D`.

Эти классы нужны сценам и будущему редактору как стабильная публичная форма физического дерева. Они не должны раскрывать Box2D.NET handles и не должны обещать завершённую симуляцию столкновений раньше задач про shapes, areas, collision layers, queries и fixed timestep.

## Область задачи

В этой задаче вводится lifecycle baseline:

- `CollisionObject2D` как abstract Godot-like база для `Area2D` и `PhysicsBody2D`;
- `PhysicsBody2D` как abstract Godot-like база для `StaticBody2D` и `RigidBody2D`;
- `StaticBody2D`, `RigidBody2D` и `Area2D` создают RID через `PhysicsServer2D` при входе в `SceneTree`;
- эти RID освобождаются при выходе из дерева или при `Free()`/`QueueFree()`;
- повторный вход в дерево создаёт новый живой RID;
- `CollisionObject2D.GetRid()` возвращает текущий RID объекта или пустой `Rid`, если узел ещё не внутри дерева;
- `CollisionObject2D` синхронизирует `GlobalTransform` в physics backend при `_PhysicsProcess()`;
- `CollisionShape2D` и `RayCast2D` наследуются от `Node2D` и имеют Godot-like public properties, но не создают physics shape/query RID в этой задаче;
- `QueueFree()` во время `_PhysicsProcess()` должен быть безопасным: узел остаётся живым до конца обхода, а RID освобождается при flush delete queue.

## Минимальный публичный API

`CollisionObject2D`:

- `GetRid()`;
- `CollisionLayer`;
- `CollisionMask`.

`PhysicsBody2D` не добавляет публичные members сверх `CollisionObject2D`.

`StaticBody2D`:

- `ConstantLinearVelocity`;
- `ConstantAngularVelocity`.

`RigidBody2D`:

- enum `FreezeModeEnum`;
- enum `CenterOfMassModeEnum`;
- `Mass`;
- `Inertia`;
- `CenterOfMass`;
- `CenterOfMassMode`;
- `GravityScale`;
- `LinearVelocity`;
- `AngularVelocity`;
- `Freeze`;
- `FreezeMode`;
- `Sleeping`;
- `CanSleep`;
- `LockRotation`.

`Area2D`:

- `Monitoring`;
- `Monitorable`;
- `Priority`.

`CollisionShape2D`:

- `Shape`;
- `Disabled`;
- `OneWayCollision`;
- `OneWayCollisionMargin`.

`RayCast2D`:

- `Enabled`;
- `TargetPosition`;
- `ExcludeParent`;
- `HitFromInside`;
- `CollideWithAreas`;
- `CollideWithBodies`;
- `CollisionMask`;
- `ForceRaycastUpdate()`;
- `IsColliding()`;
- `GetCollider()`;
- `GetColliderRid()`;
- `GetColliderShape()`;
- `GetCollisionPoint()`;
- `GetCollisionNormal()`.

`Shape2D`:

- abstract `Resource` base для будущих concrete shape resources;
- `GetRid()` возвращает shape RID или пустой `Rid`, если concrete shape ещё не реализует RID.

Все members выше должны быть Godot-like: если поведение ещё не реализовано, метод возвращает безопасное пустое состояние, а не Electron2D-only compatibility API.

## Backend state

Внутренний `IPhysicsServer2DBackend` должен хранить для body/area RID последний transform, переданный узлом. Это внутренний механизм для тестов и будущей реализации physics backend; он не является публичным API игры.

Для `StaticBody2D`, `RigidBody2D` и `Area2D` backend должен различать body kind:

- static body;
- rigid body;
- area.

Этот kind нужен, чтобы последующие задачи могли подключать реальную физику без миграции публичных классов.

## Не входит в задачу

- concrete `Shape2D` resources: `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D`, `ConcavePolygonShape2D`;
- запись geometry в `PhysicsServer2D`;
- collision layers/masks в solver;
- contacts и shape-level overlap signals;
- raycast query implementation;
- `CharacterBody2D` и kinematic solver;
- выбор Box2D.NET как production backend.

## Критерии приёмки

- Public API экспортирует `CollisionObject2D`, `PhysicsBody2D`, `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D` и `Shape2D`.
- `StaticBody2D`, `RigidBody2D` и `Area2D` создают RID при входе в дерево, освобождают его при выходе и создают новый RID при повторном входе.
- `CollisionObject2D.GetRid()` не возвращает Box2D.NET handle.
- `CollisionObject2D` передаёт `GlobalTransform` в backend во время physics frame.
- `QueueFree()` во время `_PhysicsProcess()` безопасно освобождает RID после завершения обхода текущего physics frame.
- `CollisionShape2D` хранит `Shape`, `Disabled`, `OneWayCollision`, `OneWayCollisionMargin`.
- `RayCast2D` хранит query properties; выполнение query и result state закреплены отдельной спецификацией direct space state.
- Reflection-тест подтверждает отсутствие публичных `Box2D` типов и public signatures.
- `docs/documentation/physics/physics-nodes-lifecycle.md` описывает фактическую реализацию.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.
