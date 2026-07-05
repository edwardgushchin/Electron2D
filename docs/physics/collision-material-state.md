# Collision layers, material, gravity и sleeping baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1-preview` должен иметь Electron2D baseline для collision filters и material/body state, который можно передать будущему production physics backend без изменения публичного API.

Задача не реализует solver. Она фиксирует public resource/node properties, validation и внутреннюю синхронизацию состояния, чтобы следующие physics задачи могли подключить contacts, areas и queries к уже проверенному контракту.

## Публичный API

### `CollisionObject2D`

`CollisionObject2D` сохраняет существующие properties:

- `CollisionLayer: uint`;
- `CollisionMask: uint`.

Добавляются Electron2D helper methods:

- `SetCollisionLayerValue(int layerNumber, bool value)`;
- `GetCollisionLayerValue(int layerNumber)`;
- `SetCollisionMaskValue(int layerNumber, bool value)`;
- `GetCollisionMaskValue(int layerNumber)`.

`layerNumber` использует Electron2D numbering `1..32`. Значение `1` соответствует bit `0`, значение `32` соответствует bit `31`. Значения вне диапазона дают понятный `ArgumentOutOfRangeException`.

### `PhysicsMaterial`

Добавляется public `PhysicsMaterial : Resource`.

Properties:

- `Friction: float` - коэффициент трения, конечное значение `>= 0`;
- `Bounce: float` - Electron2D публичное имя для restitution, конечное значение `>= 0`;
- `Rough: bool` - marker для будущего правила combined friction;
- `Absorbent: bool` - marker для будущего правила combined bounce.

Публичное свойство не называется `Restitution`, потому Electron2D API использует `Bounce`.

`PhysicsMaterial` должен иметь AOT-safe metadata для `ResourceObjectSerializer`, чтобы round-trip не зависел от reflection fallback.

### `PhysicsBody2D`

Добавляется:

- `PhysicsMaterialOverride: PhysicsMaterial?`.

Это Electron2D material override для `StaticBody2D`, `RigidBody2D` и будущих body nodes.

### `RigidBody2D`

Существующие properties `GravityScale`, `Sleeping` и `CanSleep` остаются public Electron2D state. `GravityScale` должен принимать любое конечное число, включая отрицательное значение для инверсии будущей gravity.

## Внутренний backend state

При `SceneTree.PhysicsFrame()` runtime синхронизирует во внутренний physics backend:

- collision filter для любого `CollisionObject2D`: `CollisionLayer` и `CollisionMask`;
- body state для любого `PhysicsBody2D`: `PhysicsMaterialOverride`;
- rigid body state для `RigidBody2D`: `GravityScale`, `Sleeping`, `CanSleep`.

Текущий `ManagedPhysicsServer2DBackend` хранит эти snapshots для тестов и будущего production backend. Он всё ещё не рассчитывает столкновения.

## Не входит в задачу

- collision solver;
- contacts и overlap signals;
- area sensors;
- raycast, point query и shape query;
- friction/bounce combine в contact solver;
- Box2D.NET production binding;
- project settings для default gravity.

## Критерии приёмки

- Layer/mask helper methods используют numbering `1..32` и проверены тестами.
- `PhysicsMaterial` валидирует `Friction` и `Bounce`, документирован и сериализуется через AOT-safe metadata.
- `PhysicsBody2D.PhysicsMaterialOverride` доступен для `StaticBody2D` и `RigidBody2D`.
- `RigidBody2D.GravityScale`, `Sleeping`, `CanSleep` попадают во внутренний body snapshot во время physics frame.
- Внутренний backend получает collision filter snapshots во время physics frame.
- Public API guard, GitHub Wiki API source, документация реализации и release-facing документы синхронизированы.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.

## Фактическое состояние, ограничения и проверки

`CollisionObject2D`, `PhysicsBody2D`, `RigidBody2D` и `PhysicsMaterial` теперь закрывают начальный state baseline для будущего 2D solver.

## Что реализовано

- `CollisionObject2D.CollisionLayer` и `CollisionObject2D.CollisionMask` хранят 32-bit collision filter.
- `SetCollisionLayerValue()`, `GetCollisionLayerValue()`, `SetCollisionMaskValue()`, `GetCollisionMaskValue()` используют numbering `1..32`.
- Значение `1` соответствует младшему bit, значение `32` соответствует старшему bit `uint`.
- Номер layer/mask вне `1..32` даёт `ArgumentOutOfRangeException` с понятным диапазоном.
- `PhysicsMaterial` хранит `Friction`, `Bounce`, `Rough`, `Absorbent`.
- `Bounce` - публичное имя для restitution.
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
