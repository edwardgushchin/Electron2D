# Импорт TTF/OTF в Font

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0038`.
Обновлено: 2026-06-21.
Связанные документы: [Import cache ресурсов](resource-import-cache.md), [Text backend baseline](../rendering/text-backend-baseline.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0038` добавляет font importer поверх import cache. Он должен принимать TTF/OTF source assets, извлекать базовые font metadata, учитывать fallback fonts и SDF/bitmap policy из sidecar и писать stable cache artifact для будущего loader.

В `0.1.0 Preview` задача не добавляет public `FontFile`, public `ResourceLoader`, real glyph rasterization или SDL_ttf file handles в public API. Результат является внутренним механизмом для тестов, будущего редактора, будущего loader и backend-отрисовки.

## Source assets и sidecar

Importer поддерживает:

- `.ttf`;
- `.otf`.

Настройки импорта хранятся в optional sidecar рядом с font source:

```text
res://fonts/main.ttf.e2import.json
```

Sidecar является source data, то есть редактируемым файлом проекта. Он не находится в import cache. Если sidecar меняется, font asset должен переимпортироваться через dependency tracking.

Минимальный sidecar формат:

```json
{
  "fallbacks": [
    "res://fonts/fallback.ttf"
  ],
  "rasterization": {
    "mode": "Sdf",
    "baseSize": 48,
    "sdfSpread": 8
  }
}
```

Defaults:

- `fallbacks`: empty array;
- `rasterization.mode`: `Bitmap`;
- `rasterization.baseSize`: `16`;
- `rasterization.sdfSpread`: `0` для `Bitmap`, `8` для `Sdf`, если поле не задано.

Fallback paths являются dependencies. Если fallback font file меняется, основной font source должен переимпортироваться.

## Metadata artifact

Importer пишет один cache artifact:

```text
resources/<uid>/font.e2font.json
```

Artifact format:

- `format`: `Electron2D.FontImportMetadata`;
- `version`: `1`;
- `source`: исходный `res://...`;
- `uid`: stable `uid://...`, созданный из source path;
- `fontFormat`: `Ttf` или `Otf`;
- `familyName`;
- `styleName`;
- `fullName`;
- `postScriptName`;
- `fallbacks`;
- `rasterization`.

JSON output должен быть stable: fallback paths сортируются ordinal-сравнением.

## Font metadata

Importer читает sfnt header:

- TrueType signature `0x00010000` даёт `Ttf`;
- OpenType/CFF signature `OTTO` даёт `Otf`.

Из `name` table используются records:

- name ID `1` - family name;
- name ID `2` - style name;
- name ID `4` - full name;
- name ID `6` - PostScript name.

Если `name` table отсутствует или поле пустое, importer использует безопасный fallback на имя файла и `Regular`.

## Runtime resource factory

Internal `FontImportResourceFactory` должен уметь создать `Font` instance из metadata. Это остаётся внутренним механизмом: публичный `Font` уже существует, а public concrete `FontFile` не добавляется без отдельной Electron2D API задачи.

## Критерии приёмки

- Integration tests проверяют TTF metadata, fallback paths и `Sdf` policy.
- Integration tests проверяют OTF metadata и default `Bitmap` policy.
- Integration tests проверяют fallback font file как dependency: изменение fallback вызывает `DependencyChanged` reimport.
- Golden-data test фиксирует exact JSON output `FontImportMetadata`.
- Public API compatibility не меняется.

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0038`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний font importer для import cache. Внутренний означает, что код находится внутри runtime assembly и доступен тестам, будущему редактору и будущим инструментам, но не добавляет новые пользовательские public классы.

Текущие типы находятся в `src/Electron2D/Assets/Resources/Importing/`:

- `FontImporter` - importer для `.ttf` и `.otf`;
- `FontMetadataReader` - читает sfnt header и `name` table;
- `FontImportMetadata` - stable cache metadata model;
- `FontImportMetadataTextSerializer` - stable JSON serializer;
- `FontImportResourceFactory` - создаёт internal `Font` instance из metadata.

Default `ResourceImportOptions.CreateDefault()` теперь регистрирует `ResourceFileImporter`, `TextureImageImporter` и `FontImporter`.

## Sidecar настройки

Optional sidecar хранится рядом с font source:

```text
res://fonts/main.ttf.e2import.json
```

Sidecar является source data. Если он меняется, import cache видит это как изменение dependency и переимпортирует font.

Поддерживаемые поля:

- `fallbacks`: массив `res://...` путей к fallback font files;
- `rasterization.mode`: `Bitmap` или `Sdf`;
- `rasterization.baseSize`: базовый размер для будущего raster/cache build;
- `rasterization.sdfSpread`: spread для SDF policy.

Fallback font files тоже являются dependencies. Изменение fallback file hash вызывает reimport основного font source.

## Cache artifact

Importer пишет:

```text
<cacheRoot>/resources/<uid>/font.e2font.json
```

Artifact содержит:

- source path;
- stable UID;
- font format `Ttf`/`Otf`;
- family/style/full/PostScript names;
- fallback font paths;
- rasterization policy.

TTF определяется по sfnt signature `0x00010000`, OTF - по `OTTO`. `name` table используется для family/style/full/PostScript names. Если нужного name record нет, importer использует имя файла и `Regular`.

## SDF/bitmap policy

В `T-0038` SDF/bitmap policy является metadata-level контрактом. Это значит, что importer сохраняет намерение сборки будущего font cache, но не создаёт bitmap atlas и не выполняет SDF generation. Реальная rasterization через SDL_ttf и GPU upload остаётся следующими backend/import задачами.

## Текущие ограничения

- Public `FontFile` не добавлен.
- Public `ResourceLoader`/`ResourceSaver` ещё не реализованы.
- Glyph rasterization, shaping, atlas generation и SDL_ttf native file handles не входят в этот baseline.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~FontImportTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~FontImportMetadataGoldenTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
