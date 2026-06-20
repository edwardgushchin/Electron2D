# `Variant`

## Назначение

`Variant` должен стать Godot-like контейнером значения для динамических runtime API Electron2D `0.1.0 Preview`: сигналов, deferred calls, будущей базы свойств, scene/resource serialization и AI-friendly tooling. В этой версии `Variant` не обязан повторять полный набор Godot 4, но обязан иметь закрытый и проверяемый список поддерживаемых значений.

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
- `Signal`, пока в публичном API нет отдельного Godot-like `Signal`;
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

Для соответствия Godot C# коллекции живут не как `System.Array`/`System.Collections.Generic.Dictionary`, а как отдельный Godot-like namespace:

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
- Public API baseline содержит только Godot-like новые типы `Electron2D.Variant`, `Electron2D.Variant.Type`, `Electron2D.Collections.Array` и `Electron2D.Collections.Dictionary`.
- Документация текущего поведения описывает закрытый список и ограничения `0.1.0 Preview`.
- `Signal`, 3D-типы и stable serialization не реализуются в этой задаче.
