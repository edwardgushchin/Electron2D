# Shape2D resources baseline

## Цель

`0.1.0 Preview` должен предоставить Electron2D resources для основных 2D collision shapes:

- `RectangleShape2D`;
- `CircleShape2D`;
- `CapsuleShape2D`;
- `SegmentShape2D`;
- `ConvexPolygonShape2D`;
- `ConcavePolygonShape2D`.

Эти resources нужны `CollisionShape2D`, сериализации сцен и будущему physics backend. Они не должны раскрывать Box2D.NET types и не должны требовать production solver, contacts или queries.

## Публичный API

Все classes наследуются от `Shape2D`, который наследуется от `Resource`.

`Shape2D`:

- `GetRid()` lazily создаёт shape RID через `PhysicsServer2D` и возвращает тот же RID до `Free()`;
- при `Free()` освобождает shape RID, если он был создан.

`RectangleShape2D`:

- `Size: Vector2`;
- значение должно быть положительным по обеим осям.

`CircleShape2D`:

- `Radius: float`;
- значение должно быть больше `0`.

`CapsuleShape2D`:

- `Radius: float`;
- `Height: float`;
- оба значения должны быть положительными;
- `Height` не может быть меньше `Radius * 2`.

`SegmentShape2D`:

- `A: Vector2`;
- `B: Vector2`;
- точки не должны совпадать.

`ConvexPolygonShape2D`:

- `Points: Vector2[]`;
- массив должен содержать минимум `3` точки;
- точки не должны повторяться;
- polygon должен быть convex и не collinear.

`ConcavePolygonShape2D`:

- `Segments: Vector2[]`;
- массив должен содержать чётное количество точек;
- массив должен описывать минимум один non-zero segment.

## Concave only static

`ConcavePolygonShape2D` разрешён только под `StaticBody2D`. Проверка выполняется в `CollisionShape2D`:

- при установке `Shape`;
- при входе `CollisionShape2D` в `SceneTree`.

Если shape находится под `RigidBody2D`, `Area2D` или другим non-static `CollisionObject2D`, операция должна завершаться понятным `InvalidOperationException`, в котором названы `ConcavePolygonShape2D` и `StaticBody2D`.

## Serialization

Все concrete shape resources должны быть зарегистрированы в AOT-safe resource metadata registry. Round-trip через `ResourceObjectSerializer`, `SerializedResourceTextSerializer` и `ResourceObjectSerializer.Instantiate()` должен восстанавливать тип и свойства shape без reflection fallback.

Сериализуемые properties:

- `RectangleShape2D.Size`;
- `CircleShape2D.Radius`;
- `CapsuleShape2D.Radius`, `CapsuleShape2D.Height`;
- `SegmentShape2D.A`, `SegmentShape2D.B`;
- `ConvexPolygonShape2D.Points`;
- `ConcavePolygonShape2D.Segments`.

## Не входит в задачу

- collision solver;
- contacts и overlap signals;
- raycast/point/shape queries;
- binding geometry в Box2D.NET production backend;
- editor gizmos для редактирования shapes;
- `CollisionPolygon2D`.

## Критерии приёмки

- Public API экспортирует все six concrete shape resource classes.
- `GetRid()` создаёт shape RID ожидаемого `PhysicsServer2D.ShapeType`.
- Invalid dimensions, duplicate/collinear/concave points и invalid segments дают понятные exception messages.
- `ConcavePolygonShape2D` разрешён под `StaticBody2D` и запрещён под non-static collision objects.
- AOT-safe resource serialization round-trip восстанавливает все shape types и properties.
- Reflection-тест подтверждает отсутствие публичных `Box2D` типов и public signatures.
- `docs/documentation/physics/shape2d-resources.md` описывает фактическую реализацию.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.
