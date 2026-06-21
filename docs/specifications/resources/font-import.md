# Импорт TTF/OTF в Font

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

Internal `FontImportResourceFactory` должен уметь создать `Font` instance из metadata. Это остаётся внутренним механизмом: публичный `Font` уже существует, а public concrete `FontFile` не добавляется без отдельной Godot-like API задачи.

## Критерии приёмки

- Integration tests проверяют TTF metadata, fallback paths и `Sdf` policy.
- Integration tests проверяют OTF metadata и default `Bitmap` policy.
- Integration tests проверяют fallback font file как dependency: изменение fallback вызывает `DependencyChanged` reimport.
- Golden-data test фиксирует exact JSON output `FontImportMetadata`.
- Public API compatibility не меняется.
