# Physics nodes lifecycle baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1.0 Preview` должен предоставить Electron2D узлы верхнего уровня для 2D-физики:

- `StaticBody2D`;
- `RigidBody2D`;
- `Area2D`;
- `CollisionShape2D`;
- `RayCast2D`.

Эти классы нужны сценам и будущему редактору как стабильная публичная форма физического дерева. Они не должны раскрывать Box2D.NET handles и не должны обещать завершённую симуляцию столкновений раньше задач про shapes, areas, collision layers, queries, fixed timestep и kinematic solver.

## Область задачи

В этой задаче вводится lifecycle baseline:

- `CollisionObject2D` как abstract Electron2D база для `Area2D` и `PhysicsBody2D`;
- `PhysicsBody2D` как abstract Electron2D база для `StaticBody2D` и `RigidBody2D`;
- `StaticBody2D`, `RigidBody2D` и `Area2D` создают RID через `PhysicsServer2D` при входе в `SceneTree`;
- эти RID освобождаются при выходе из дерева или при `Free()`/`QueueFree()`;
- повторный вход в дерево создаёт новый живой RID;
- `CollisionObject2D.GetRid()` возвращает текущий RID объекта или пустой `Rid`, если узел ещё не внутри дерева;
- `CollisionObject2D` синхронизирует `GlobalTransform` в physics backend при `_PhysicsProcess()`;
- fixed timestep и начальное движение `RigidBody2D` закреплены отдельной спецификацией `fixed-physics-step-and-rigid-body-motion.md`;
- `CollisionShape2D` и `RayCast2D` наследуются от `Node2D` и имеют Electron2D public properties, но не создают physics shape/query RID в этой задаче;
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

Все members выше должны быть Electron2D: если поведение ещё не реализовано, метод возвращает безопасное пустое состояние, а не Electron2D-only compatibility API.

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
- `QueueFree()` во время `_PhysicsProcess()` безопасно освобождает RID после завершения обхода текущего physics frame; поведение при нескольких fixed ticks описано отдельной спецификацией fixed timestep.
- `CollisionShape2D` хранит `Shape`, `Disabled`, `OneWayCollision`, `OneWayCollisionMargin`.
- `RayCast2D` хранит query properties; выполнение query и result state закреплены отдельной спецификацией direct space state.
- Reflection-тест подтверждает отсутствие публичных `Box2D` типов и public signatures.
- `docs/physics/physics-nodes-lifecycle.md` описывает фактическую реализацию.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.

## Фактическое состояние, ограничения и проверки

`StaticBody2D`, `RigidBody2D`, `CharacterBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D` и `Shape2D` - текущий baseline физических узлов для `0.1.0 Preview`.

## Что реализовано

- `CollisionObject2D` наследуется от `Node2D` и является общей базой для `Area2D` и `PhysicsBody2D`.
- `PhysicsBody2D` является общей базой для `StaticBody2D`, `RigidBody2D` и `CharacterBody2D`.
- `StaticBody2D`, `RigidBody2D`, `CharacterBody2D` и `Area2D` создают physics RID при входе в `SceneTree`.
- При выходе из дерева, `Free()` или `QueueFree()` RID освобождается через `PhysicsServer2D.FreeRid()`.
- Повторное добавление узла в дерево создаёт новый RID.
- `CollisionObject2D.GetRid()` возвращает текущий RID или пустой `Rid`, если узел не находится в дереве.
- Во время `SceneTree.PhysicsFrame()` `CollisionObject2D` передаёт свой `GlobalTransform` во внутренний physics backend.
- `SceneTree.PhysicsFrame()` запускает fixed ticks по `1/60` секунды; подробнее это описано в `fixed-physics-step-and-rigid-body-motion.md`.
- `QueueFree()` внутри `_PhysicsProcess()` безопасен: узел удаляется после завершения текущего обхода дерева, а RID освобождается во время flush delete queue. Если в одном `PhysicsFrame()` есть несколько fixed ticks, уже queued node пропускается на следующих ticks.
- `CollisionLayer` и `CollisionMask` хранятся на `CollisionObject2D` и участвуют в текущих AABB overlap/query/motion checks.
- `CollisionObject2D` синхронизирует collision filter во внутренний physics backend во время physics frame.
- `StaticBody2D` хранит `ConstantLinearVelocity` и `ConstantAngularVelocity`.
- `RigidBody2D` хранит базовые свойства: `Mass`, `Inertia`, `CenterOfMass`, `CenterOfMassMode`, `GravityScale`, `LinearVelocity`, `AngularVelocity`, `Freeze`, `FreezeMode`, `Sleeping`, `CanSleep`, `LockRotation`.
- `RigidBody2D` двигается по `LinearVelocity` на fixed tick и выполняет basic AABB sweep against `StaticBody2D`, включая one-way platform checks через `CollisionShape2D.OneWayCollision`.
- `CharacterBody2D` хранит `Velocity`, выполняет `MoveAndCollide()`/`MoveAndSlide()` через managed AABB sweep, обновляет floor/wall/ceiling state, floor snap, slide collisions и platform velocity.
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
- полноценный collision solver, contacts, rigid-rigid collision, shape-level overlap signals и точная narrow-phase проверка;
- точный raycast/point/shape narrow-phase и production backend queries;
- gravity integration для `RigidBody2D`;
- production kinematic solver narrow-phase;
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
