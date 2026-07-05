# Resource file baseline, stable UID и ссылки ресурсов

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0035`.
Обновлено: 2026-06-21.
Связанные источники: [Godot ResourceUID](https://docs.godotengine.org/en/stable/classes/class_resourceuid.html), [Godot ResourceLoader](https://docs.godotengine.org/en/stable/classes/class_resourceloader.html), [Electron2D 0.1-preview](../releases/0.1-preview.md), [Stable `Variant` serialization](../core-types/variant-serialization.md).

## Назначение

`T-0035` вводит базу для файлов ресурсов Electron2D `0.1-preview`: стабильные `uid://` идентификаторы, внешние ссылки, внутренние подресурсы и текстовый формат, который удобно смотреть в diff. Это нужно для будущих сцен, импорта, инспектора, текстового backend и Agent-native cross-platform 2D game engine tooling.

Формат должен быть узким и проверяемым. Он не обязан сам реализовать весь `ResourceLoader`/`ResourceSaver`, threaded loading, импорт и editor file dock. Import cache реализуется отдельной спецификацией `resource-import-cache.md`, а текущая задача должна дать устойчивую модель данных, на которую он сможет опереться.

## Electron2D граница

Публичный API добавляет только `ResourceUid`, потому что это прямой Electron2D аналог `ResourceUID` в C#-стиле имени. Он отвечает за связь между числовым UID, строкой `uid://...` и `res://` путём ресурса.

Минимальный публичный контракт:

```csharp
public static class ResourceUid
{
    public const long InvalidId = -1;

    public static void AddId(long id, string path);
    public static long CreateId();
    public static long CreateIdForPath(string path);
    public static string EnsurePath(string pathOrUid);
    public static string GetIdPath(long id);
    public static bool HasId(long id);
    public static string IdToText(long id);
    public static string PathToUid(string path);
    public static void RemoveId(long id);
    public static void SetId(long id, string path);
    public static long TextToId(string textId);
    public static string UidToPath(string uid);
}
```

Отличие от Godot фиксируется явно: в `0.1-preview` это статический C# facade внутри Electron2D runtime, а не engine singleton object. Поведение намеренно соответствует Godot: UID хранит связь с путём, `uid://` переживает rename/move через `SetId()`, а `PathToUid()`/`UidToPath()` позволяют хранить ссылки по UID с path fallback в текстовом файле.

Нельзя добавлять публичные `ResourceFile`, `ResourceFileSerializer`, `ResourceFormatLoader` или похожие классы в этой задаче. Они не нужны пользовательскому API `0.1-preview` до отдельного контракта загрузки/сохранения.

## Текстовый формат ресурса

Внутренний формат Electron2D использует расширение `.e2res` для generic resource files. Формат хранится как UTF-8 JSON с отступами, фиксированным порядком top-level полей и отсортированными property names. Это не локальный сайт и не human-only описание, а machine-readable файл, который можно проверять golden-data тестами.

Top-level поля идут в таком порядке:

1. `format` - строка `Electron2D.ResourceFile`;
2. `version` - целое число версии формата, для этой задачи `1`;
3. `uid` - строковый UID главного ресурса;
4. `type` - полное имя типа ресурса;
5. `path` - fallback путь `res://...`;
6. `external` - массив внешних ссылок;
7. `internal` - массив внутренних подресурсов;
8. `properties` - свойства главного ресурса.

Внешняя ссылка описывает ресурс в другом файле:

```json
{
  "id": 1,
  "uid": "uid://example",
  "path": "res://textures/player.png",
  "type": "Electron2D.Texture2D"
}
```

`uid` является основным идентификатором. `path` остаётся fallback-значением для чтения и понятного review, если UID временно неизвестен на другой машине.

Внутренний подресурс описывает ресурс внутри того же файла:

```json
{
  "id": 1,
  "type": "Electron2D.Resource",
  "properties": {
    "resource_name": {
      "type": "String",
      "value": "Stats"
    }
  }
}
```

`id` уникален только внутри файла. Свойства хранятся через stable `Variant` serialization и поэтому поддерживают только сериализуемый набор `Variant` из `0.1-preview`.

## Инварианты

- UID главного ресурса должен быть валидным `uid://` и не равняться `uid://<invalid>`.
- `ResourceUid.SetId(id, newPath)` меняет путь без изменения UID. Это и есть baseline для rename/move.
- Сериализация одного и того же документа должна возвращать одинаковый текст.
- Property names сортируются ordinal-сравнением, чтобы порядок dictionary/hash table не менял файл.
- External references сортируются по `id`.
- Internal resources сортируются по `id`.
- Unknown, missing или malformed поля должны давать понятную `FormatException`, а не молча терять данные.
- Runtime-only значения `Variant`, такие как `Rid`, `Object` и `Callable`, не допускаются в `properties`.

## Критерии приёмки

- Unit tests проверяют `ResourceUid`: создание stable ID для path, `uid://` round-trip, регистрацию, rename/move через `SetId()`, `PathToUid()`, `UidToPath()` и `EnsurePath()`.
- Integration tests проверяют `.e2res` round-trip с external и internal references.
- Golden-data tests фиксируют exact text output resource file.
- Public API compatibility обновлена только на `Electron2D.ResourceUid`.
- Документация реализации описывает текущие ограничения: file loader/saver и import cache ещё не входят в T-0035.

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0035`.
Обновлено: 2026-06-21.

## Что реализовано

В `T-0035` добавлена базовая модель ресурсов для Electron2D `0.1-preview`:

- public `ResourceUid` для связи числового UID, строки `uid://...` и `res://` пути;
- internal `.e2res` document model, то есть внутренняя модель файла ресурса, доступная тестам и будущим загрузчикам;
- internal `ResourceFileTextSerializer` для стабильного JSON round-trip;
- external references, то есть ссылки на ресурсы в других файлах;
- internal resources, то есть подресурсы внутри того же файла;
- golden-data проверка exact text output.

`internal` здесь означает не пользовательский public API, а код внутри runtime assembly. Доступ к нему имеют integration/golden tests через `InternalsVisibleTo`, чтобы формат можно было проверить без публикации лишних классов пользователю.

## `ResourceUid`

`ResourceUid` является публичной Electron2D поверхностью T-0035. Он хранит in-memory mapping между UID и путём ресурса:

- `CreateId()` создаёт новый положительный UID, который ещё не зарегистрирован;
- `CreateIdForPath(path)` создаёт стабильный UID-кандидат для path;
- `AddId(id, path)` регистрирует новую связь;
- `SetId(id, path)` меняет путь существующего UID и поэтому сохраняет ссылки при rename/move;
- `PathToUid(path)` возвращает `uid://...`, если path зарегистрирован, иначе возвращает path без изменений;
- `UidToPath(uid)` возвращает зарегистрированный path или пустую строку;
- `EnsurePath(pathOrUid)` пропускает обычный path и преобразует известный `uid://...` в path;
- `IdToText(id)` и `TextToId(text)` конвертируют числовой UID и текстовую форму.

`uid://<invalid>` соответствует `ResourceUid.InvalidId`.

## Формат `.e2res`

Формат пишется как UTF-8 JSON с LF-переносами и фиксированным порядком полей:

```json
{
  "format": "Electron2D.ResourceFile",
  "version": 1,
  "uid": "uid://21i3v9",
  "type": "Electron2D.Resource",
  "path": "res://characters/player.e2res",
  "external": [],
  "internal": [],
  "properties": {}
}
```

Top-level поля:

- `format` - строка `Electron2D.ResourceFile`;
- `version` - версия формата, сейчас `1`;
- `uid` - UID главного ресурса;
- `type` - полное имя типа ресурса;
- `path` - fallback путь;
- `external` - внешние ссылки, отсортированные по `id`;
- `internal` - внутренние подресурсы, отсортированные по `id`;
- `properties` - свойства главного ресурса.

Свойства используют `VariantTextSerializer`, поэтому в `.e2res` допускаются только переносимые значения stable `Variant` serialization. Runtime-only значения, такие как `Rid`, `Object` и `Callable`, не записываются.

## Ограничения

T-0035 сам по себе не добавлял file-level `ResourceLoader`, `ResourceSaver`, импорт, файловое наблюдение и editor FileSystem dock. Import cache реализован отдельной задачей `T-0036` и описан в `resource-import-cache.md`. Более широкий internal serializer для сцен, ресурсов, arrays, dictionaries, enums, nullable и resource references реализован в `T-0041` и описан в `scene-resource-serialization.md`. Текущий документ остаётся справкой по стабильному `.e2res` формату и UID-контракту, на который import cache опирается.

## Проверки

- `ResourceUidTests` проверяют stable path UID, `uid://` round-trip, rename/move через `SetId()`, `PathToUid()`, `UidToPath()` и invalid marker.
- `ResourceFileSerializationTests` проверяют round-trip external/internal references и ошибку при отсутствующем UID.
- `ResourceFileGoldenTests` фиксирует exact JSON output с LF-переносами.
