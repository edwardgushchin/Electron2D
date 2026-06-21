# Collision layers, material, gravity и sleeping baseline

`CollisionObject2D`, `PhysicsBody2D`, `RigidBody2D` и `PhysicsMaterial` теперь закрывают начальный Godot-like state baseline для будущего 2D solver.

## Что реализовано

- `CollisionObject2D.CollisionLayer` и `CollisionObject2D.CollisionMask` хранят 32-bit collision filter.
- `SetCollisionLayerValue()`, `GetCollisionLayerValue()`, `SetCollisionMaskValue()`, `GetCollisionMaskValue()` используют Godot-like numbering `1..32`.
- Значение `1` соответствует младшему bit, значение `32` соответствует старшему bit `uint`.
- Номер layer/mask вне `1..32` даёт `ArgumentOutOfRangeException` с понятным диапазоном.
- `PhysicsMaterial` хранит `Friction`, `Bounce`, `Rough`, `Absorbent`.
- `Bounce` - публичное Godot-like имя для restitution.
- `Friction` и `Bounce` должны быть конечными значениями `>= 0`.
- `PhysicsBody2D.PhysicsMaterialOverride` хранит material override для `StaticBody2D`, `RigidBody2D` и будущих body nodes.
- `RigidBody2D.GravityScale` принимает любое конечное число, включая отрицательное значение для будущей инверсии gravity.
- `RigidBody2D.Sleeping` и `RigidBody2D.CanSleep` попадают во внутренний body snapshot.

## Внутренняя синхронизация

Во время `SceneTree.PhysicsFrame()` runtime отправляет во внутренний backend:

- `PhysicsCollisionFilter` для любого `CollisionObject2D`;
- `PhysicsBody2DState` для любого `PhysicsBody2D`;
- `PhysicsRigidBody2DState` внутри body state для `RigidBody2D`.

Эти snapshot-типы являются внутренним механизмом движка, недоступным игре как public API. Они нужны тестам и будущему production backend, чтобы solver получил уже проверенный state contract.

Текущий `ManagedPhysicsServer2DBackend` сохраняет snapshots рядом с RID resource registry. Он всё ещё не рассчитывает столкновения, friction, bounce, gravity, sleep transitions или contacts.

## Serialization

`PhysicsMaterial` зарегистрирован во внутреннем AOT-safe metadata registry. `ResourceObjectSerializer` round-trip сохраняет:

- `friction`;
- `bounce`;
- `rough`;
- `absorbent`.

## Что ещё не реализовано

- collision solver;
- contacts и overlap signals;
- material combine rules внутри contacts;
- area sensors;
- raycast, point query и shape query;
- project settings для default gravity;
- Box2D.NET production binding.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~PhysicsMaterialStateTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```
