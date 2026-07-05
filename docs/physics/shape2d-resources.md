# Shape2D resources baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1-preview` должен предоставить Electron2D resources для основных 2D collision shapes:

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
- `docs/physics/shape2d-resources.md` описывает фактическую реализацию.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.

## Фактическое состояние, ограничения и проверки

`Shape2D` resources - текущий baseline форм столкновений для `CollisionShape2D` в `0.1-preview`.

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
