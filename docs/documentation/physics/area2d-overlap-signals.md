# Area2D sensors и overlap signals baseline

`Area2D` теперь имеет начальный sensor baseline для `0.1.0 Preview`: область на physics frame находит пересечения с телами и другими областями, хранит текущие overlap snapshots и эмитит стандартные signal names через `Object.Connect()`.

## Что реализовано

- Built-in signals регистрируются при создании `Area2D`:
  - `body_entered`;
  - `body_exited`;
  - `area_entered`;
  - `area_exited`.
- `GetOverlappingBodies()` возвращает текущие valid body nodes как `Node2D[]`.
- `GetOverlappingAreas()` возвращает текущие valid area nodes как `Area2D[]`.
- `HasOverlappingBodies()` и `HasOverlappingAreas()` проверяют наличие текущих пересечений.
- `OverlapsBody(Node2D body)` и `OverlapsArea(Area2D area)` проверяют конкретный объект.
- `Monitoring == false` очищает текущий snapshot и отключает collection/signals этой области.
- `Monitorable == false` не даёт другим areas обнаружить эту область.
- `CollisionMask` monitoring area должен пересекаться с `CollisionLayer` target object.

## Overlap model

Текущий managed baseline использует AABB-пересечение active `CollisionShape2D`:

- `CollisionShape2D.Disabled == true` исключает shape из проверки;
- `CollisionShape2D.Shape == null` не участвует в overlap;
- rectangle, circle, capsule, segment, convex polygon и concave segments сводятся к world-space `Rect2`;
- `CollisionShape2D.GlobalTransform` учитывает transform shape node и родителей `Node2D`;
- collision object без active shapes не участвует в overlap.

Это не production narrow-phase solver. Baseline нужен, чтобы gameplay-код, editor previews и будущий backend уже использовали стабильный публичный API.

## Порядок сигналов

На каждом `SceneTree.PhysicsFrame()` изменения эмитятся в стабильном порядке:

1. `body_entered`;
2. `body_exited`;
3. `area_entered`;
4. `area_exited`.

Внутри каждой группы nodes сортируются по `GetInstanceId()`. Это даёт повторяемые integration tests и предсказуемое поведение для простых сцен.

## Deferred removal

Если callback overlap-сигнала вызывает `QueueFree()` для body/area, удаление остаётся deferred через `SceneTree`. Объект валиден внутри callback, удаляется после текущего traversal и не ломает следующий physics frame.

Freed nodes не возвращаются из overlap helper methods и не получают exit-сигнал после фактического освобождения, чтобы signal payload не ссылался на уже освобождённый объект.

## Что ещё не реализовано

- точная narrow-phase collision detection;
- contact manifolds;
- shape-level signals `body_shape_entered`, `body_shape_exited`, `area_shape_entered`, `area_shape_exited`;
- точные shape-level query narrow-phase results;
- production continuous collision detection;
- production Box2D.NET backend.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~Area2DOverlapSignalTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```
