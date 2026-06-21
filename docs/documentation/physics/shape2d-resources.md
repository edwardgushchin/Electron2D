# Shape2D resources baseline

`Shape2D` resources - текущий baseline форм столкновений для `CollisionShape2D` в `0.1.0 Preview`.

## Что реализовано

- `Shape2D.GetRid()` лениво создаёт shape `Rid` через `PhysicsServer2D` и возвращает тот же handle до `Free()`.
- `Shape2D.Free()` освобождает созданный shape `Rid` через `PhysicsServer2D.FreeRid()`.
- `RectangleShape2D` хранит `Size` и запрещает неположительный или нечисловой размер.
- `CircleShape2D` хранит `Radius` и запрещает неположительный или нечисловой радиус.
- `CapsuleShape2D` хранит `Radius` и `Height`; высота должна быть больше диаметра.
- `SegmentShape2D` хранит endpoints `A` и `B`; точки не должны совпадать.
- `ConvexPolygonShape2D` хранит копию массива `Points`; массив должен содержать минимум три конечные точки, без дублей и с convex-геометрией.
- `ConcavePolygonShape2D` хранит копию массива `Segments`; массив должен содержать пары конечных точек, каждая пара описывает ненулевой segment.
- `CollisionShape2D` разрешает `ConcavePolygonShape2D` только под `StaticBody2D`. Под `RigidBody2D`, `Area2D` или другим non-static `CollisionObject2D` выбрасывается понятный `InvalidOperationException`.
- Все concrete shape resources зарегистрированы во внутреннем metadata registry, который недоступен игре как public API и нужен для AOT-safe serialization без поиска свойств через reflection.

## RID и backend

Concrete shape resource создаёт только shape `Rid` ожидаемого `PhysicsServer2D.ShapeType`:

- `RectangleShape2D` -> `Rectangle`;
- `CircleShape2D` -> `Circle`;
- `CapsuleShape2D` -> `Capsule`;
- `SegmentShape2D` -> `Segment`;
- `ConvexPolygonShape2D` -> `ConvexPolygon`;
- `ConcavePolygonShape2D` -> `ConcavePolygon`.

Текущий `ManagedPhysicsServer2DBackend` хранит тип shape и lifetime RID. Он ещё не передаёт geometry в production solver, потому что solver и production Box2D.NET backend остаются отдельными задачами.

## Serialization

`ResourceObjectSerializer` round-trip восстанавливает тип и свойства shape через заранее зарегистрированную metadata:

- `size`;
- `radius`;
- `height`;
- `a`;
- `b`;
- `points`;
- `segments`.

Массивы `Points` и `Segments` копируются на чтении и записи, чтобы внешний код не мог изменить geometry в обход validation.

## Что ещё не реализовано

- запись shape geometry в production physics backend;
- collision solver, contacts и overlap signals;
- collision layers/masks в расчёте столкновений;
- raycast, point query и shape query;
- `CollisionPolygon2D`;
- editor gizmos для редактирования shapes.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~Shape2DResourceTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```

License header verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
```
