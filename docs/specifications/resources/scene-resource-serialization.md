# Сериализация сцен, ресурсов и переносимых property values

Статус: целевая спецификация для `T-0041`.
Обновлено: 2026-06-21.
Связанные документы: [Stable `Variant` serialization](../core-types/variant-serialization.md), [Resource file baseline](resource-file-baseline.md), [`PackedScene` и смена активной сцены](../object-model/packed-scene.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0041` добавляет внутренний file-level serializer baseline для сцен и ресурсов. Он должен сохранять переносимые значения, ссылки на ресурсы и структуру сцены в стабильном JSON, чтобы `load -> modify -> save -> load` не терял values и references.

В `0.1.0 Preview` задача не добавляет public `ResourceLoader`, public `ResourceSaver`, editor FileSystem dock или полный metadata database. Результат является внутренним механизмом для тестов, будущего редактора, import/export tooling и AI-friendly project operations.

## Property value model

Формат свойств должен поддерживать:

- `Variant` values из стабильного `VariantTextSerializer`;
- `Array` как рекурсивный список property values;
- `Dictionary` как список key/value entries, где key и value тоже property values;
- `Enum` с именем CLR-типа, именем значения и числовым значением;
- `Nullable<T>` с именем underlying CLR-типа и optional inner value;
- ссылки на внешние ресурсы через local reference id;
- ссылки на внутренние ресурсы через local reference id.

`Variant.Object`, `Variant.Rid` и `Variant.Callable` не должны сериализоваться как portable property values. Ссылки на ресурсы пишутся отдельным reference slot, а не прямым object value.

## Resource document

Resource document хранит:

1. `format` - строка `Electron2D.SerializedResource`;
2. `version` - версия формата, для `T-0041` равна `1`;
3. `uid` - stable UID главного ресурса;
4. `type` - CLR type name ресурса;
5. `path` - fallback `res://...` путь;
6. `external` - внешние resource references;
7. `internal` - внутренние subresources;
8. `properties` - свойства главного ресурса.

Внутренние ресурсы используют тот же property value model. Сериализация должна быть stable: references сортируются по `id`, properties по ordinal имени, dictionary entries по serialized key.

## Scene document

Scene document хранит:

1. `format` - строка `Electron2D.SceneFile`;
2. `version` - версия формата, для `T-0041` равна `1`;
3. `external` - внешние resource references;
4. `internal` - внутренние subresources;
5. `nodes` - сохранённые nodes.

Node entry хранит:

- `id` - локальный положительный id node;
- `type` - CLR type name node;
- `name` - `Node.Name`;
- `parent` - parent node id или `null` для root;
- `owner` - owner node id или `null`;
- `groups` - persistent groups;
- `properties` - переносимые свойства node.

Формат `SceneFileDocument` не обязан сразу заменять in-memory `PackedScene`; он является file-level representation для будущих loader/saver и editor/tooling задач.

## Custom Resource round-trip

Для пользовательских `Resource` internal serializer должен уметь создать document из зарегистрированной metadata, поддержанной property value model, и восстановить новый instance через typed factory delegate. Reflection fallback, automatic public property discovery и dynamic type lookup не входят в этот контракт. Подробный metadata contract описан в [`AOT-safe metadata для Inspector и serialization`](aot-safe-metadata.md).

## Ошибки

Serializer должен fail closed:

- unknown `format` или `version` дают `FormatException`;
- malformed property value даёт `FormatException`;
- unsupported CLR value при capture даёт `InvalidOperationException`;
- resource reference с невалидным id или отсутствующим `uid` даёт `FormatException`;
- отсутствующий CLR type при instantiate custom resource даёт `InvalidOperationException`.

## Критерии приёмки

- Integration tests проверяют `load -> modify -> save -> load` для scene document: values, arrays, dictionaries, enum, nullable и resource references сохраняются.
- Integration tests проверяют custom `Resource` round-trip через object serializer и registered metadata.
- Golden-data test фиксирует exact JSON output resource document с enum, nullable, array, dictionary и resource reference.
- Public API compatibility не меняется.
- Source license verifier проходит для новых C# files.
