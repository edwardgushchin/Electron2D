# `StringName` и `Rid`

Статус: целевая спецификация.
Задача: `T-0019`.
Обновлено: 2026-06-21.

## Цель

Ввести два базовых Godot-like identity-типа для `0.1.0 Preview`:

- `StringName` - immutable interned name для имён сигналов, групп, свойств, методов и будущего `Variant`;
- `Rid` - opaque handle низкоуровневого ресурса для будущих rendering/physics/audio/text servers.

В этом документе `Rid` означает Godot-style resource identifier, а не .NET Runtime Identifier. Это разные понятия: .NET RID описывает платформу сборки, а `Electron2D.Rid` описывает handle ресурса текущей runtime-сессии.

Источники Godot-like поведения:

- [Godot StringName](https://docs.godotengine.org/en/stable/classes/class_stringname.html);
- [Godot RID](https://docs.godotengine.org/en/stable/classes/class_rid.html).

## `StringName`

`StringName` должен быть value type в namespace `Electron2D`.

Публичный API:

```csharp
public readonly struct StringName : IEquatable<StringName>
{
    public StringName(string? value);

    public bool IsEmpty();
    public override string ToString();
    public override bool Equals(object? obj);
    public bool Equals(StringName other);
    public override int GetHashCode();

    public static bool operator ==(StringName left, StringName right);
    public static bool operator !=(StringName left, StringName right);
    public static bool operator ==(StringName left, string? right);
    public static bool operator !=(StringName left, string? right);
    public static bool operator ==(string? left, StringName right);
    public static bool operator !=(string? left, StringName right);
    public static implicit operator StringName(string? value);
}
```

Контракт:

- `default(StringName)` равен пустому `StringName`.
- `new StringName(null)` равен пустому `StringName`.
- Сравнение регистрозависимое и использует ordinal semantics.
- Два `StringName` с одинаковым текстом равны и дают одинаковый hash code.
- `ToString()` возвращает исходный текст.
- `StringName` не обязан экспортировать весь набор методов `String`: в Godot C# для строковых методов требуется `ToString()`.

## `Rid`

`Rid` должен быть value type в namespace `Electron2D`.

Публичный API:

```csharp
public readonly struct Rid : IEquatable<Rid>, IComparable<Rid>
{
    public long GetId();
    public bool IsValid();
    public override string ToString();
    public override bool Equals(object? obj);
    public bool Equals(Rid other);
    public int CompareTo(Rid other);
    public override int GetHashCode();

    public static bool operator ==(Rid left, Rid right);
    public static bool operator !=(Rid left, Rid right);
    public static bool operator <(Rid left, Rid right);
    public static bool operator <=(Rid left, Rid right);
    public static bool operator >(Rid left, Rid right);
    public static bool operator >=(Rid left, Rid right);
}
```

Контракт:

- `default(Rid)` имеет ID `0` и считается invalid.
- `IsValid()` возвращает `true`, если ID не равен `0`.
- `Rid` не раскрывает ресурс и не даёт доступ к нему напрямую.
- `Rid` имеет смысл только внутри текущей runtime-сессии; его нельзя считать стабильным сериализуемым ID.
- Значение `Rid` можно сравнивать, использовать как dictionary key и логировать через `ToString()`.
- Публичного конструктора из integer ID нет: valid IDs выдаёт internal allocator будущих серверов.

## Internal allocator

Для проверки lifetime и будущих серверных abstractions вводится internal allocator. Он не является public API.

Минимальный internal contract:

- allocator выдаёт уникальные valid `Rid` значения;
- allocator знает, какие `Rid` принадлежат его текущей runtime-сессии;
- `Free(rid)` удаляет владение allocator над `Rid`;
- `default(Rid)` не принадлежит allocator;
- `Rid`, выданный одним allocator, не принадлежит другому allocator.

## Acceptance tests

- `StringName` покрыт equality, hashing, implicit string conversion, empty/default/null semantics и dictionary key behavior.
- `Rid` покрыт invalid/default semantics, equality, ordering и hashing.
- Internal allocator покрывает lifetime: allocate, ownership, free и session boundary.
- Public API compatibility table и runtime baseline test обновлены под новые типы.
