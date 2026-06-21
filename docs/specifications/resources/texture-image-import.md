# Импорт PNG/JPEG в Texture2D и AtlasTexture

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

Это остаётся внутренним механизмом, чтобы не добавлять public классы, не закреплённые отдельной Godot-like API задачей.

## Критерии приёмки

- Integration tests проверяют PNG metadata, alpha, filter/repeat, mipmaps, atlas region, platform variants и создание `Texture2D`/`AtlasTexture` из metadata.
- Integration tests проверяют JPEG width/height и отсутствие alpha.
- Integration tests проверяют sidecar как dependency: изменение sidecar приводит к `DependencyChanged` reimport.
- Golden-data test фиксирует exact JSON output `TextureImportMetadata`.
- Public API compatibility не меняется.
