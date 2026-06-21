# Resource file baseline, stable UID и ссылки ресурсов

Статус: целевая спецификация для `T-0035`.
Обновлено: 2026-06-21.
Связанные источники: [Godot ResourceUID](https://docs.godotengine.org/en/stable/classes/class_resourceuid.html), [Godot ResourceLoader](https://docs.godotengine.org/en/stable/classes/class_resourceloader.html), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Stable `Variant` serialization](../core-types/variant-serialization.md).

## Назначение

`T-0035` вводит базу для файлов ресурсов Electron2D `0.1.0 Preview`: стабильные `uid://` идентификаторы, внешние ссылки, внутренние подресурсы и текстовый формат, который удобно смотреть в diff. Это нужно для будущих сцен, импорта, инспектора, текстового backend и AI-friendly tooling.

Формат должен быть узким и проверяемым. Он не обязан сам реализовать весь `ResourceLoader`/`ResourceSaver`, threaded loading, импорт и editor file dock. Import cache реализуется отдельной спецификацией `resource-import-cache.md`, а текущая задача должна дать устойчивую модель данных, на которую он сможет опереться.

## Godot-like граница

Публичный API добавляет только `ResourceUid`, потому что это прямой Godot-like аналог `ResourceUID` в C#-стиле имени. Он отвечает за связь между числовым UID, строкой `uid://...` и `res://` путём ресурса.

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

Отличие от Godot фиксируется явно: в `0.1.0 Preview` это статический C# facade внутри Electron2D runtime, а не engine singleton object. Поведение намеренно соответствует Godot: UID хранит связь с путём, `uid://` переживает rename/move через `SetId()`, а `PathToUid()`/`UidToPath()` позволяют хранить ссылки по UID с path fallback в текстовом файле.

Нельзя добавлять публичные `ResourceFile`, `ResourceFileSerializer`, `ResourceFormatLoader` или похожие классы в этой задаче. Они не нужны пользовательскому API `0.1.0 Preview` до отдельного контракта загрузки/сохранения.

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

`id` уникален только внутри файла. Свойства хранятся через stable `Variant` serialization и поэтому поддерживают только сериализуемый набор `Variant` из `0.1.0 Preview`.

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
