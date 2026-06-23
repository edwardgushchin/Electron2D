# Импорт PNG/JPEG в Texture2D и AtlasTexture

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0037`.
Обновлено: 2026-06-21.
Связанные документы: [Import cache ресурсов](resource-import-cache.md), [Texture2D resource baseline](../rendering/texture-resource-baseline.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0037` добавляет первый image importer поверх import cache. Он должен принимать PNG/JPEG source assets, извлекать переносимые metadata, учитывать настройки импорта и создавать cache artifact, из которого будущий loader сможет создать `Texture2D` и связанные `AtlasTexture`.

В `0.1.0 Preview` задача не добавляет public `ImageTexture`, public `ResourceLoader`, GPU upload и runtime decoding pixels. Результат является внутренним механизмом: он доступен тестам, будущему editor import dock и будущему loader, но не расширяет пользовательский public API.

## Source assets и sidecar

Importer поддерживает:

- `.png`;
- `.jpg`;
- `.jpeg`.

Настройки импорта хранятся в optional sidecar рядом с image source:

```text
res://textures/player.png.e2import.json
```

Sidecar является source data, то есть редактируемым файлом проекта. Он не находится в import cache. Если sidecar меняется, image asset должен переимпортироваться через dependency tracking.

Минимальный sidecar формат:

```json
{
  "filter": "Nearest",
  "repeat": "Mirror",
  "mipmaps": true,
  "atlas": [
    {
      "name": "idle",
      "region": { "x": 2, "y": 4, "width": 16, "height": 8 },
      "margin": { "x": 1, "y": 1, "width": 2, "height": 2 },
      "filterClip": true
    }
  ],
  "platforms": [
    { "name": "desktop", "format": "rgba8", "quality": 100 },
    { "name": "android", "format": "etc2", "quality": 80 }
  ]
}
```

Defaults:

- `filter`: `Linear`;
- `repeat`: `Disabled`;
- `mipmaps`: `false`;
- `atlas`: empty array;
- `platforms`: empty array;
- atlas `margin`: zero rect;
- atlas `filterClip`: `false`;
- platform `quality`: `100`.

## Metadata artifact

Importer пишет один cache artifact:

```text
resources/<uid>/texture.e2tex.json
```

Artifact format:

- `format`: `Electron2D.TextureImportMetadata`;
- `version`: `1`;
- `source`: исходный `res://...`;
- `uid`: stable `uid://...`, созданный из source path;
- `imageFormat`: `Png` или `Jpeg`;
- `width`, `height`;
- `hasAlpha`;
- `hasMipmaps`, `mipmapCount`;
- `sampling.filter`, `sampling.repeat`;
- `atlas`;
- `platforms`.

JSON output должен быть stable: atlas regions сортируются по `name`, platform variants сортируются по `name`.

## Image metadata

PNG importer читает `IHDR` и определяет alpha:

- color type `4` или `6` означает alpha;
- chunk `tRNS` также означает alpha.

JPEG importer читает start-of-frame segment и получает width/height. JPEG alpha не поддерживается и всегда записывается как `false`.

Importer не обязан декодировать pixels в этой задаче. GPU upload и pixel storage остаются следующими задачами.

## Создание runtime resources

Internal `TextureImportResourceFactory` должен уметь:

- создать `Texture2D` instance с width/height/alpha/mipmap metadata;
- создать `AtlasTexture` instances из atlas regions и привязать их к импортированной atlas texture.

Это остаётся внутренним механизмом, чтобы не добавлять public классы, не закреплённые отдельной Electron2D API задачей.

## Критерии приёмки

- Integration tests проверяют PNG metadata, alpha, filter/repeat, mipmaps, atlas region, platform variants и создание `Texture2D`/`AtlasTexture` из metadata.
- Integration tests проверяют JPEG width/height и отсутствие alpha.
- Integration tests проверяют sidecar как dependency: изменение sidecar приводит к `DependencyChanged` reimport.
- Golden-data test фиксирует exact JSON output `TextureImportMetadata`.
- Public API compatibility не меняется.

## Фактическое состояние, ограничения и проверки

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
