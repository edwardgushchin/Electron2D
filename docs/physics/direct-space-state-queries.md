# PhysicsDirectSpaceState2D raycast, point query и shape query baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1.0 Preview` должен предоставить Electron2D query surface для простых physics checks:

- `RayCast2D` должен выполнять запрос и хранить последний result;
- `PhysicsDirectSpaceState2D.IntersectRay()` должен возвращать ближайший hit;
- `PhysicsDirectSpaceState2D.IntersectPoint()` должен возвращать объекты под точкой;
- `PhysicsDirectSpaceState2D.IntersectShape()` должен возвращать объекты, пересекающие shape.

Задача не выбирает production physics backend и не раскрывает Box2D.NET handles. Текущий result строится по уже проверенному AABB baseline активных `CollisionShape2D`.

## Публичный API

Добавляются Electron2D public types:

- `World2D`;
- `PhysicsDirectSpaceState2D`;
- `PhysicsRayQueryParameters2D`;
- `PhysicsPointQueryParameters2D`;
- `PhysicsShapeQueryParameters2D`.

`CanvasItem.GetWorld2D()` возвращает `World2D` для текущей `SceneTree`. `World2D.DirectSpaceState` возвращает query object.

`PhysicsDirectSpaceState2D` methods:

- `IntersectRay(PhysicsRayQueryParameters2D parameters): Electron2D.Collections.Dictionary`;
- `IntersectPoint(PhysicsPointQueryParameters2D parameters, int maxResults = 32): Electron2D.Collections.Array`;
- `IntersectShape(PhysicsShapeQueryParameters2D parameters, int maxResults = 32): Electron2D.Collections.Array`.

`RayCast2D.ForceRaycastUpdate()` должен использовать тот же direct state baseline и обновлять:

- `IsColliding()`;
- `GetCollider()`;
- `GetColliderRid()`;
- `GetColliderShape()`;
- `GetCollisionPoint()`;
- `GetCollisionNormal()`.

## Query parameters

`PhysicsRayQueryParameters2D`:

- `From: Vector2`;
- `To: Vector2`;
- `CollisionMask: uint`, default `uint.MaxValue`;
- `CollideWithBodies: bool`, default `true`;
- `CollideWithAreas: bool`, default `false`;
- `HitFromInside: bool`;
- `Exclude: Rid[]`.

`PhysicsPointQueryParameters2D`:

- `Position: Vector2`;
- `CollisionMask: uint`, default `uint.MaxValue`;
- `CollideWithBodies: bool`, default `true`;
- `CollideWithAreas: bool`, default `false`;
- `Exclude: Rid[]`.

`PhysicsShapeQueryParameters2D`:

- `Shape: Shape2D?`;
- `ShapeRid: Rid`;
- `Transform: Transform2D`, default `Transform2D.Identity`;
- `Motion: Vector2`;
- `Margin: float`;
- `CollisionMask: uint`, default `uint.MaxValue`;
- `CollideWithBodies: bool`, default `true`;
- `CollideWithAreas: bool`, default `false`;
- `Exclude: Rid[]`.

`ShapeRid` сохраняется для Electron2D API формы, но текущий managed baseline умеет вычислять geometry только из `Shape`. Если `Shape == null`, `IntersectShape()` возвращает пустой результат.

## Result dictionaries

`IntersectRay()` возвращает пустой `Dictionary`, если hit отсутствует. При hit result содержит keys:

- `position: Vector2`;
- `normal: Vector2`;
- `collider: Object`;
- `collider_id: long`;
- `rid: Rid`;
- `shape: long`.

`IntersectPoint()` и `IntersectShape()` возвращают `Array` of `Dictionary`. Каждый item содержит:

- `collider: Object`;
- `collider_id: long`;
- `rid: Rid`;
- `shape: long`.

## Query rules

- Участвуют только valid collision objects внутри `SceneTree`.
- Участвуют только active `CollisionShape2D`: `Shape != null` и `Disabled == false`.
- Query `CollisionMask` должен пересекаться с `CollisionLayer` target object.
- `CollideWithBodies` и `CollideWithAreas` выбирают body/area candidates.
- `Exclude` исключает target `Rid`.
- Results сортируются по расстоянию для raycast и по `GetInstanceId()` для point/shape lists.
- `maxResults < 0` даёт `ArgumentOutOfRangeException`.
- `maxResults == 0` возвращает пустой `Array`.

## Не входит в задачу

- точный narrow-phase solver;
- motion cast fraction;
- `CollideShape()`, `CastMotion()`, `GetRestInfo()`;
- shape-level contacts;
- fixed timestep;
- `CharacterBody2D`;
- production Box2D.NET backend.

## Критерии приёмки

- `RayCast2D` возвращает ближайший body hit, `Rid`, shape index, point и normal.
- Raycast respects `CollisionMask`, `CollideWithBodies`, `CollideWithAreas`, `ExcludeParent`, `HitFromInside` и no-hit cases.
- `PhysicsDirectSpaceState2D.IntersectPoint()` и `IntersectShape()` проверены на filters, masks, max results и no-hit.
- Public API guard, GitHub Wiki API source, документация реализации и release-facing документы синхронизированы.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.

## Фактическое состояние, ограничения и проверки

`0.1.0 Preview` теперь имеет начальный query surface для 2D-физики. Он работает поверх managed AABB baseline активных `CollisionShape2D` и не раскрывает Box2D.NET или другие backend handles.

## Что реализовано

- `CanvasItem.GetWorld2D()` возвращает `World2D`.
- `World2D.DirectSpaceState` возвращает `PhysicsDirectSpaceState2D`.
- `PhysicsDirectSpaceState2D.IntersectRay()` возвращает ближайший hit как `Electron2D.Collections.Dictionary`.
- `PhysicsDirectSpaceState2D.IntersectPoint()` возвращает `Electron2D.Collections.Array` of hit dictionaries.
- `PhysicsDirectSpaceState2D.IntersectShape()` возвращает `Electron2D.Collections.Array` of hit dictionaries.
- `RayCast2D.ForceRaycastUpdate()` использует тот же direct space state и обновляет cached result.
- `RayCast2D` также обновляет result во время physics frame, если `Enabled == true`.

## Query parameters

Добавлены public parameter resources:

- `PhysicsRayQueryParameters2D`;
- `PhysicsPointQueryParameters2D`;
- `PhysicsShapeQueryParameters2D`.

Общие правила:

- `CollisionMask` выбирает target `CollisionLayer`;
- `CollideWithBodies` и `CollideWithAreas` выбирают типы объектов;
- `Exclude` исключает target `Rid`;
- `maxResults < 0` даёт `ArgumentOutOfRangeException`;
- `maxResults == 0` возвращает пустой `Array`.

`PhysicsShapeQueryParameters2D.ShapeRid` есть как public API slot для будущего production backend. Текущий managed baseline вычисляет geometry только из `Shape`; если `Shape == null`, `IntersectShape()` возвращает пустой результат.

## Result dictionaries

Ray result содержит:

- `position`;
- `normal`;
- `collider`;
- `collider_id`;
- `rid`;
- `shape`.

Point и shape query items содержат:

- `collider`;
- `collider_id`;
- `rid`;
- `shape`.

Значения хранятся как `Variant`, поэтому результат остаётся совместимым с текущими `Dictionary`/`Array` контейнерами.

## Ограничения

Это AABB baseline, а не точный solver:

- rectangle, circle, capsule, segment, convex polygon и concave segments сводятся к bounds;
- contact manifolds не создаются;
- `CollideShape()`, `CastMotion()` и `GetRestInfo()` ещё не реализованы;
- production continuous collision detection остаётся следующей задачей.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~PhysicsDirectSpaceState2DTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```
