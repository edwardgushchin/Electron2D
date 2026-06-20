# Stable `Variant` serialization

Статус: реализованный internal baseline.
Задача: `T-0021`.
Обновлено: 2026-06-21.

## Назначение

`VariantTextSerializer` фиксирует стабильный canonical JSON round-trip для сериализуемого подмножества `Variant` в `0.1.0 Preview`. Это внутренний runtime contract для будущих scene/resource files, golden-data проверок и tooling. Он не является public API Electron2D.

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

`Deserialize()` даёт `FormatException`, если JSON malformed, не является object, не содержит `type`, содержит неизвестный `type`, содержит runtime-only `type` или если `value` не соответствует ожидаемой форме.

## Проверки

Integration tests фиксируют exact JSON для scalar values, 2D math, `StringName`, `NodePath`, nested `Array`, sorted `Dictionary`, runtime-only serialization errors и malformed/unsupported input errors.
