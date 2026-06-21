# Сериализация сцен, ресурсов и переносимых property values

Статус: реализованный internal baseline.
Задача: `T-0041`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний file-level serializer baseline для сцен, ресурсов и переносимых property values. Внутренний означает, что код доступен тестам, будущему редактору, export/import tooling и будущим AI-friendly project operations, но не добавляет public `ResourceLoader`, public `ResourceSaver` или новые пользовательские классы.

Текущие типы находятся в `src/Electron2D/Core/Resources/Serialization/`:

- `SerializedPropertyValue` - переносимая модель значения свойства;
- `SerializedResourceDocument` - JSON document для одного resource file;
- `SceneFileDocument` - JSON document для scene file;
- `SerializedResourceTextSerializer` - stable JSON serializer для resource document;
- `SceneFileTextSerializer` - stable JSON serializer для scene document;
- `ResourceObjectSerializer` - internal reflection-based baseline для custom `Resource` round-trip.

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

`ResourceObjectSerializer.Capture()` создаёт resource document из public get/set properties custom `Resource`. `ResourceObjectSerializer.Instantiate()` создаёт новый instance через parameterless constructor и восстанавливает поддержанные свойства.

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
- AOT-safe metadata registry без reflection остаётся отдельной задачей `T-0043`.
- Unsupported CLR values дают ошибку вместо silent fallback.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SceneResourceSerializationTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~SerializedResourceGoldenTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
