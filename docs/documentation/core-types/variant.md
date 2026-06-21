# `Variant`

Статус: реализованный baseline.
Задача: `T-0020`.
Обновлено: 2026-06-21.

## Public API

Текущий runtime экспортирует:

- `Electron2D.Variant`;
- `Electron2D.Variant.Type`;
- `Electron2D.Collections.Array`;
- `Electron2D.Collections.Dictionary`.

`Variant` - Electron2D value carrier для динамических API. Это `readonly struct`; `default(Variant)` и `Variant.CreateFrom(null)` означают `Variant.Type.Nil`.

## Закрытый список `Variant.Type`

В `0.1.0 Preview` поддержаны только:

- `Nil`;
- `Bool`;
- `Int`;
- `Float`;
- `String`;
- `Vector2`;
- `Vector2I`;
- `Rect2`;
- `Rect2I`;
- `Transform2D`;
- `Color`;
- `StringName`;
- `NodePath`;
- `Rid`;
- `Object`;
- `Callable`;
- `Dictionary`;
- `Array`.

`Resource` и `Node` хранятся как `Object`. Enum значения хранятся как `Int`.

## Создание значений

Поддержаны implicit conversions для:

- `bool`;
- `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`;
- `float`, `double`;
- `string?`;
- `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color`;
- `StringName`, `NodePath`, `Rid`, `Callable`;
- `Object?` и наследники, включая `Resource` и `Node`;
- `Electron2D.Collections.Array?`;
- `Electron2D.Collections.Dictionary?`.

Enum значения создаются через `Variant.From<T>()` или `Variant.CreateFrom(object?)`, по правилам текущего C# API.

## Чтение значений

Доступны строгие методы:

- `As<T>()`;
- `AsBool()`;
- `AsInt64()`;
- `AsInt32()`;
- `AsDouble()`;
- `AsString()`;
- `AsVector2()`;
- `AsVector2I()`;
- `AsRect2()`;
- `AsRect2I()`;
- `AsTransform2D()`;
- `AsColor()`;
- `AsStringName()`;
- `AsNodePath()`;
- `AsRid()`;
- `AsObject()`;
- `AsCallable()`;
- `AsArray()`;
- `AsDictionary()`.

Если фактический `Variant.Type` не совпадает с ожидаемым типом, выбрасывается `InvalidCastException` с указанием фактического и ожидаемого типа. Unsupported CLR values, например `decimal`, `DateTime`, `Guid` или произвольный `System.Object`, дают `ArgumentException`. `ulong`, который не помещается в `long`, даёт `ArgumentOutOfRangeException`.

## Коллекции

`Electron2D.Collections.Array` хранит ordered list значений `Variant`.

Текущий API:

- `Count`;
- indexer `this[int index]`;
- `Add(Variant value)`;
- `Clear()`;
- `RemoveAt(int index)`;
- `ToArray()`;
- enumeration.

`Electron2D.Collections.Dictionary` хранит пары `Variant` -> `Variant`.

Текущий API:

- `Count`;
- indexer `this[Variant key]`;
- `Add(Variant key, Variant value)`;
- `Clear()`;
- `ContainsKey(Variant key)`;
- `Remove(Variant key)`;
- `TryGetValue(Variant key, out Variant value)`;
- enumeration.

Коллекции являются mutable reference-like объектами. Если один и тот же экземпляр `Array` или `Dictionary` положен в несколько `Variant`, эти `Variant` ссылаются на один контейнер.

## Ограничения

- `Signal` пока не входит в public API и не поддержан как `Variant.Type`.
- 3D-типы и packed arrays не входят в `0.1.0 Preview`.
- Stable text serialization round-trip реализован отдельной задачей `T-0021` как internal runtime contract.
- Signal/group/deferred/property APIs пока не мигрированы на `Variant`; они будут переводиться отдельными задачами.

## Проверки

- Red-state unit tests сначала падали на отсутствующем `Electron2D.Variant` и `Electron2D.Collections`.
- Green-state `dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore` проходит и покрывает nil, primitives, enum mapping, 2D math, identity handles, object/resource values, collections и ошибки unsupported/wrong cast.
