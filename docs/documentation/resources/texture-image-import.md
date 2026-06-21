# Импорт PNG/JPEG в Texture2D и AtlasTexture

Статус: реализованный internal baseline.
Задача: `T-0037`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен внутренний image importer для import cache. Внутренний означает, что код находится внутри runtime assembly и доступен тестам, будущему редактору и будущим инструментам, но не добавляет новые пользовательские public классы.

Текущие типы находятся в `src/Electron2D/Assets/Resources/Importing/`:

- `TextureImageImporter` - importer для `.png`, `.jpg`, `.jpeg`;
- `TextureImageMetadataReader` - читает PNG/JPEG dimensions и alpha metadata без pixel decoding;
- `TextureImportMetadata` - stable cache metadata model;
- `TextureImportMetadataTextSerializer` - stable JSON serializer;
- `TextureImportResourceFactory` - создаёт internal `Texture2D` и `AtlasTexture` instances из metadata.

Default `ResourceImportOptions.CreateDefault()` теперь регистрирует `ResourceFileImporter` и `TextureImageImporter`.

## Sidecar настройки

Optional sidecar хранится рядом с image source:

```text
res://textures/player.png.e2import.json
```

Sidecar является source data. Если он меняется, import cache видит это как изменение dependency и переимпортирует texture.

Поддерживаемые поля:

- `filter`: `Nearest`, `Linear`, `NearestWithMipmaps`, `LinearWithMipmaps`;
- `repeat`: `Disabled`, `Enabled`, `Mirror`;
- `mipmaps`: `true`/`false`;
- `atlas`: список regions с `name`, `region`, optional `margin`, optional `filterClip`;
- `platforms`: список variants с `name`, `format`, optional `quality`.

## Cache artifact

Importer пишет:

```text
<cacheRoot>/resources/<uid>/texture.e2tex.json
```

Artifact содержит:

- source path;
- stable UID;
- image format `Png`/`Jpeg`;
- width/height;
- alpha flag;
- mipmap policy и count;
- sampling filter/repeat;
- atlas regions;
- platform variants.

PNG alpha определяется по color type `4`/`6` и chunk `tRNS`. JPEG alpha всегда `false`.

## Runtime resource factory

`TextureImportResourceFactory.CreateTexture()` создаёт internal `Texture2D` instance с metadata width/height/alpha/mipmaps.

`TextureImportResourceFactory.CreateAtlasTextures()` создаёт `AtlasTexture` instances из atlas regions, сохраняет `Region`, `Margin`, `FilterClip` и привязывает их к atlas texture.

## Текущие ограничения

- Pixel decoding и GPU upload ещё не реализованы.
- Public `ImageTexture` не добавлен.
- Public `ResourceLoader`/`ResourceSaver` ещё не реализованы.
- Platform variants пока фиксируются как metadata, а не компилируются в real platform texture compression artifacts.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~TextureImageImportTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~TextureImportMetadataGoldenTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
