# Импорт TTF/OTF в Font

Статус: реализованный internal baseline.
Задача: `T-0038`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний font importer для import cache. Внутренний означает, что код находится внутри runtime assembly и доступен тестам, будущему редактору и будущим инструментам, но не добавляет новые пользовательские public классы.

Текущие типы находятся в `src/Electron2D/Core/Resources/Importing/`:

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
