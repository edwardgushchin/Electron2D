# PhysicsDirectSpaceState2D raycast, point query и shape query baseline

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
