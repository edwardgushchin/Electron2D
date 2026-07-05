# Сериализация сцен, ресурсов и переносимых property values

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0041`.
Обновлено: 2026-06-21.
Связанные документы: [Stable `Variant` serialization](../core-types/variant-serialization.md), [Resource file baseline](resource-file-baseline.md), [`PackedScene` и смена активной сцены](../object-model/packed-scene.md), [Electron2D 0.1-preview](../releases/0.1-preview.md).

## Назначение

`T-0041` добавляет внутренний file-level serializer baseline для сцен и ресурсов. Он должен сохранять переносимые значения, ссылки на ресурсы и структуру сцены в стабильном JSON, чтобы `load -> modify -> save -> load` не терял values и references.

В `0.1-preview` задача не добавляет public `ResourceLoader`, public `ResourceSaver`, editor FileSystem dock или полный metadata database. Результат является внутренним механизмом для тестов, будущего редактора, import/export tooling и Agent-native cross-platform 2D game engine project operations.

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0041`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний file-level serializer baseline для сцен, ресурсов и переносимых property values. Внутренний означает, что код доступен тестам, будущему редактору, export/import tooling и будущим Agent-native cross-platform 2D game engine project operations, но не добавляет public `ResourceLoader`, public `ResourceSaver` или новые пользовательские классы.

Текущие типы находятся в `src/Electron2D/Assets/Resources/Serialization/`:

- `SerializedPropertyValue` - переносимая модель значения свойства;
- `SerializedResourceDocument` - JSON document для одного resource file;
- `SceneFileDocument` - JSON document для scene file;
- `SerializedResourceTextSerializer` - stable JSON serializer для resource document;
- `SceneFileTextSerializer` - stable JSON serializer для scene document;
- `ResourceObjectSerializer` - internal serializer для custom `Resource` round-trip поверх registered AOT-safe metadata.

## Property value model

Поддерживаются:

- `Variant` values, которые уже умеет писать `VariantTextSerializer`;
- `Array` как список nested property values;
- `Dictionary` как список key/value entries;
- `Enum` с CLR type name, enum name и числовым значением;
- `Nullable<T>` как typed slot с `null` или nested value;
- resource reference slots: `External` и `Internal`.

Ссылки на ресурсы не пишутся как `Variant.Object`. Для них используется отдельный `Resource` reference slot с local `id`, а сами referenced resources находятся в `external` или `internal` секциях документа.

## Resource document

`SerializedResourceTextSerializer` пишет формат:

```text
Electron2D.SerializedResource
```

Top-level поля:

- `format`;
- `version`;
- `uid`;
- `type`;
- `path`;
- `external`;
- `internal`;
- `properties`.

Properties сортируются ordinal-сравнением. External и internal references сортируются по `id`. Dictionary entries сортируются по serialized key, чтобы JSON оставался стабильным.

`ResourceObjectSerializer.Capture()` создаёт resource document только для custom `Resource`, чей тип зарегистрирован в `ResourceObjectMetadataRegistry`. `ResourceObjectSerializer.Instantiate()` берёт typed factory и property descriptors из той же metadata. Имена свойств в JSON берутся из metadata, а не из CLR property names.

## Scene document

`SceneFileTextSerializer` пишет формат:

```text
Electron2D.SceneFile
```

Scene document содержит external resource references, internal subresources и список nodes. Node entry хранит:

- локальный `id`;
- `type`;
- `name`;
- `parent`;
- `owner`;
- persistent `groups`;
- `properties`.

`SceneFileDocument` пока не заменяет public `PackedScene`. Это file-level representation для будущих loader/saver, editor и tooling задач.

## Текущие ограничения

- Public `ResourceLoader`/`ResourceSaver` ещё не реализованы.
- In-memory `PackedScene` ещё не читает и не пишет `SceneFileDocument` напрямую.
- Resource references сохраняются как slots, но automatic reference resolution при `ResourceObjectSerializer.Instantiate()` не реализован.
- Source generator для metadata ещё не реализован; текущая регистрация metadata выполняется вручную или тестовым кодом.
- Unsupported CLR values дают ошибку вместо silent fallback.

## Stress gate

100-cycle stability, rename/move resources, import cache rebuild и corruption diagnostics проверяются отдельным release gate [Stress data stability для scene/resource pipeline](data-stability-stress.md).

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SceneResourceSerializationTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~SerializedResourceGoldenTests" --no-restore -m:1
powershell -ExecutionPolicy Bypass -File tools\Verify-AotMetadataSafety.ps1 -NativeAot
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
