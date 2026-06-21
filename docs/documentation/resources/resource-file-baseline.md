# Resource file baseline, stable UID и ссылки ресурсов

Статус: реализованный internal baseline.
Задача: `T-0035`.
Обновлено: 2026-06-21.

## Что реализовано

В `T-0035` добавлена базовая модель ресурсов для Electron2D `0.1.0 Preview`:

- public `ResourceUid` для связи числового UID, строки `uid://...` и `res://` пути;
- internal `.e2res` document model, то есть внутренняя модель файла ресурса, доступная тестам и будущим загрузчикам;
- internal `ResourceFileTextSerializer` для стабильного JSON round-trip;
- external references, то есть ссылки на ресурсы в других файлах;
- internal resources, то есть подресурсы внутри того же файла;
- golden-data проверка exact text output.

`internal` здесь означает не пользовательский public API, а код внутри runtime assembly. Доступ к нему имеют integration/golden tests через `InternalsVisibleTo`, чтобы формат можно было проверить без публикации лишних классов пользователю.

## `ResourceUid`

`ResourceUid` является публичной Godot-like поверхностью T-0035. Он хранит in-memory mapping между UID и путём ресурса:

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

T-0035 сам по себе не добавлял file-level `ResourceLoader`, `ResourceSaver`, импорт, файловое наблюдение и editor FileSystem dock. Import cache реализован отдельной задачей `T-0036` и описан в `resource-import-cache.md`. Текущий документ остаётся справкой по стабильному `.e2res` формату и UID-контракту, на который import cache опирается.

## Проверки

- `ResourceUidTests` проверяют stable path UID, `uid://` round-trip, rename/move через `SetId()`, `PathToUid()`, `UidToPath()` и invalid marker.
- `ResourceFileSerializationTests` проверяют round-trip external/internal references и ошибку при отсутствующем UID.
- `ResourceFileGoldenTests` фиксирует exact JSON output с LF-переносами.
