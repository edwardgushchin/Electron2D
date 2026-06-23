# `Variant`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`Variant` должен стать Electron2D контейнером значения для динамических runtime API Electron2D `0.1.0 Preview`: сигналов, deferred calls, будущей базы свойств, scene/resource serialization и Agent-native cross-platform 2D game engine tooling. Публичный C# API `Variant`, входящий в утверждённый 2D-профиль, должен совпадать с Godot `4.7-stable` .NET/C# API. Значения и типы, относящиеся к API вне 2D-профиля, в `0.1.0 Preview` не входят, но их отсутствие должно быть явно отражено в profile manifest и проверках совместимости.

## Источники поведения

- [Godot C# Variant](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_variant.html);
- [Godot Variant](https://docs.godotengine.org/en/stable/classes/class_variant.html);
- [Godot `variant.h`](https://github.com/godotengine/godot/blob/master/core/variant/variant.h).

Godot C# использует `Variant` как `struct`, где `default`/пустой constructor означают null-like `Nil`, а совместимые C# значения преобразуются в `Variant` через implicit conversions или `Variant.From<T>()`.

## Закрытый список типов `0.1.0 Preview`

`Variant.Type` в Electron2D `0.1.0 Preview` поддерживает только эти значения:

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

`Resource` и `Node` хранятся как `Object`, потому что они наследуются от `Electron2D.Object`. Перечисления хранятся как `Int`, как в Godot C#.

В `0.1.0 Preview` намеренно не входят:

- 3D-типы (`Vector3`, `Transform3D`, `Basis`, `Projection` и связанные типы);
- `Signal`, пока в публичном API нет отдельного Electron2D `Signal`;
- packed arrays;
- editor-only значения;
- произвольные CLR-объекты, не наследующиеся от `Electron2D.Object`.

## C# API

Минимальный публичный API:

```csharp
namespace Electron2D;

public readonly struct Variant : IEquatable<Variant>
{
    public enum Type;

    public Type VariantType { get; }
    public object? Obj { get; }

    public bool IsNil();

    public static Variant CreateFrom(object? value);
    public static Variant From<T>(T value);

    public T As<T>();
    public bool AsBool();
    public long AsInt64();
    public int AsInt32();
    public double AsDouble();
    public string AsString();
    public Vector2 AsVector2();
    public Vector2I AsVector2I();
    public Rect2 AsRect2();
    public Rect2I AsRect2I();
    public Transform2D AsTransform2D();
    public Color AsColor();
    public StringName AsStringName();
    public NodePath AsNodePath();
    public Rid AsRid();
    public Object? AsObject();
    public Callable AsCallable();
    public Electron2D.Collections.Array AsArray();
    public Electron2D.Collections.Dictionary AsDictionary();
}
```

`Variant` должен иметь implicit conversions из поддерживаемых не-enum C# типов. Enum значения должны проходить через `Variant.From<T>()` или `Variant.CreateFrom(object?)`, чтобы не создавать неявных enum overloads.

## Коллекции

Для соответствия Godot C# коллекции живут не как `System.Array`/`System.Collections.Generic.Dictionary`, а как отдельный Electron2D namespace:

```csharp
namespace Electron2D.Collections;

public sealed class Array;
public sealed class Dictionary;
```

`Array` хранит `Variant` элементы в порядке добавления. `Dictionary` хранит пары `Variant` -> `Variant`. Эти контейнеры являются mutable reference-like значениями; `Variant` хранит ссылку на контейнер.

## Правила преобразования

- `default(Variant)` и `Variant.CreateFrom(null)` возвращают `Variant.Type.Nil`.
- `bool` возвращает `Bool`.
- `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long` возвращают `Int`.
- `ulong` возвращает `Int` только если значение помещается в `long`; иначе должна быть понятная ошибка переполнения.
- `float` и `double` возвращают `Float`; внутри `Variant` хранится `double`.
- `decimal`, `DateTime`, `Guid`, arbitrary CLR objects и остальные неподдержанные типы должны давать понятную ошибку `ArgumentException`.
- `As<T>()` и typed `As...()` методы должны быть строгими: если фактический `Variant.Type` не соответствует целевому типу, выбрасывается `InvalidCastException` с указанием фактического и ожидаемого типа.
- `As<T>()` для enum читает `Int` и возвращает enum через его underlying value.

## Критерии приёмки

- Unit-тесты покрывают `Nil`, primitive values, numeric normalization, enum mapping, 2D math values, identity values, `Object`/`Resource`, `Array`, `Dictionary` и ошибки неподдержанных типов.
- Public API baseline содержит только Electron2D новые типы `Electron2D.Variant`, `Electron2D.Variant.Type`, `Electron2D.Collections.Array` и `Electron2D.Collections.Dictionary`.
- Документация текущего поведения описывает закрытый список и ограничения `0.1.0 Preview`.
- `Signal`, 3D-типы и stable serialization не реализуются в этой задаче.

## Фактическое состояние, ограничения и проверки

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
