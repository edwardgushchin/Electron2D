# Stable `Variant` serialization

## Назначение

`T-0021` должен зафиксировать стабильный текстовый round-trip формат для сериализуемого подмножества `Variant` в Electron2D `0.1.0 Preview`. Этот формат нужен будущим scene/resource files, golden-data тестам и Agent-native cross-platform 2D game engine tooling. Он не добавляет новый публичный API: в `0.1.0 Preview` serializer остаётся internal runtime сервисом.

## Формат

Формат - canonical JSON object без лишних пробелов:

```json
{"type":"Int","value":42}
```

Каждое значение записывается как object с двумя полями в фиксированном порядке:

1. `type` - строковое имя `Variant.Type`;
2. `value` - данные значения.

JSON property names внутри value objects пишутся в lower camel case и фиксированном порядке. Все числа пишутся invariant culture через `System.Text.Json`.

## Сериализуемые типы

Стабильный формат `0.1.0 Preview` поддерживает:

- `Nil` -> `{"type":"Nil","value":null}`;
- `Bool` -> JSON boolean;
- `Int` -> JSON number в диапазоне signed 64-bit;
- `Float` -> JSON number для `double`;
- `String` -> JSON string;
- `Vector2` -> `{"x":float,"y":float}`;
- `Vector2I` -> `{"x":int,"y":int}`;
- `Rect2` -> `{"position":Vector2,"size":Vector2}`;
- `Rect2I` -> `{"position":Vector2I,"size":Vector2I}`;
- `Transform2D` -> `{"x":Vector2,"y":Vector2,"origin":Vector2}`;
- `Color` -> `{"r":float,"g":float,"b":float,"a":float}`;
- `StringName` -> JSON string;
- `NodePath` -> JSON string;
- `Array` -> JSON array of serialized Variant objects;
- `Dictionary` -> JSON array of entries: `{"key":Variant,"value":Variant}`.

Dictionary entries сортируются по canonical JSON ключа через ordinal string comparison. Это делает output стабильным независимо от внутреннего порядка hash table.

## Несериализуемые значения

Следующие значения являются допустимыми `Variant`, но не являются стабильными переносимыми данными в `0.1.0 Preview`:

- `Rid`, потому что он имеет смысл только внутри текущей runtime-сессии;
- `Object`, включая `Resource` и `Node`, потому что file-level object/resource references должны описываться отдельной моделью ссылок, а не переносимым `Variant` значением;
- `Callable`, потому что callback binding не является переносимыми данными.

Serializer должен выбрасывать `InvalidOperationException` с понятным сообщением при попытке записать эти значения.

Начиная с `T-0035`, file-level resource references должны обрабатываться не через `Variant.Object`, а через отдельный resource file baseline с `uid://` и fallback path. `T-0041` расширяет это правило до scene/resource documents через local reference slots. Этот документ сохраняет запрет на прямую сериализацию `Object` внутри `Variant`.

Deserializer должен выбрасывать `FormatException`, если:

- JSON не является object;
- отсутствует поле `type`;
- `type` неизвестен;
- `type` известен как `Variant.Type`, но не поддержан stable serialization форматом;
- `value` не соответствует ожидаемой форме.

## Internal API

Минимальный internal contract:

```csharp
namespace Electron2D;

internal static class VariantTextSerializer
{
    internal static string Serialize(Variant variant);
    internal static Variant Deserialize(string text);
}
```

Этот API предназначен для runtime pipeline и integration tests. Он не является public API Electron2D.

## Критерии приёмки

- Round-trip tests фиксируют exact JSON для `Nil`, primitives, 2D math, `StringName`, `NodePath`, nested `Array` и sorted `Dictionary`.
- Повторная сериализация результата `Deserialize()` возвращает тот же canonical JSON.
- `Object`, `Callable`, `Rid`, неизвестные `type` и malformed JSON дают понятные ошибки.
- Public API compatibility count не меняется относительно `T-0020`.
