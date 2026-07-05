# Stable `Variant` serialization

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`T-0021` должен зафиксировать стабильный текстовый round-trip формат для сериализуемого подмножества `Variant` в Electron2D `0.1-preview`. Этот формат нужен будущим scene/resource files, golden-data тестам и Agent-native cross-platform 2D game engine tooling. Он не добавляет новый публичный API: в `0.1-preview` serializer остаётся internal runtime сервисом.

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

Стабильный формат `0.1-preview` поддерживает:

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

Следующие значения являются допустимыми `Variant`, но не являются стабильными переносимыми данными в `0.1-preview`:

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0021`.
Обновлено: 2026-06-21.

## Назначение

`VariantTextSerializer` фиксирует стабильный canonical JSON round-trip для сериализуемого подмножества `Variant` в `0.1-preview`. Это внутренний runtime contract для будущих scene/resource files, golden-data проверок и tooling. Он не является public API Electron2D.

## Internal API

Текущий internal API:

- `VariantTextSerializer.Serialize(Variant variant)`;
- `VariantTextSerializer.Deserialize(string text)`.

Доступ к нему имеют runtime assembly и integration tests через `InternalsVisibleTo`.

## Canonical JSON

Каждое значение пишется как object с фиксированным порядком полей:

```json
{"type":"Int","value":42}
```

`type` содержит строковое имя `Variant.Type`. `value` содержит данные значения. Повторная сериализация результата `Deserialize()` возвращает тот же canonical JSON.

## Поддержанные значения

Сериализуются:

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
- `Array`;
- `Dictionary`.

`Dictionary` хранится как array entries `{"key":Variant,"value":Variant}`. Entries сортируются по canonical JSON ключа через ordinal comparison, чтобы результат не зависел от порядка hash table.

## Ошибки

`Rid`, `Object` и `Callable` являются допустимыми `Variant`, но не сериализуются этим форматом: они завязаны на текущую runtime-сессию или callback binding. Попытка `Serialize()` для таких значений даёт `InvalidOperationException` с указанием типа.

Ссылки на ресурсы в `.e2res` и scene/resource documents не записываются как `Variant.Object`: для них используется отдельная internal resource/reference model с `uid://`, fallback path и local reference id, описанная в [resource file baseline](../resources/resource-file-baseline.md) и [сериализации сцен/ресурсов](../resources/scene-resource-serialization.md).

`Deserialize()` даёт `FormatException`, если JSON malformed, не является object, не содержит `type`, содержит неизвестный `type`, содержит runtime-only `type` или если `value` не соответствует ожидаемой форме.

## Проверки

Integration tests фиксируют exact JSON для scalar values, 2D math, `StringName`, `NodePath`, nested `Array`, sorted `Dictionary`, runtime-only serialization errors и malformed/unsupported input errors.
