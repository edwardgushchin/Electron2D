# Импорт WAV/OGG в AudioStream

Статус: целевая спецификация для `T-0039`.
Обновлено: 2026-06-21.
Связанные документы: [Import cache ресурсов](resource-import-cache.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md).

## Назначение

`T-0039` добавляет resource importer для WAV и OGG source assets. Importer должен создавать stable cache artifact с metadata, из которого future audio runtime сможет создать `AudioStream` без чтения исходного проекта как generated state.

В `0.1.0 Preview` задача не добавляет `AudioServer`, playback nodes, mixer graph или device playback. Результат закрывает asset pipeline baseline: source file остаётся в проекте, generated metadata лежит в import cache, UID стабилен относительно `res://` path, а loop/static/streaming/platform packaging decisions сохраняются в diff-friendly JSON.

## Source assets и sidecar

Importer поддерживает:

- `.wav`;
- `.ogg`.

Настройки импорта хранятся в optional sidecar рядом с audio source:

```text
res://audio/theme.ogg.e2import.json
```

Sidecar является source data и не переносится в import cache. Если sidecar меняется, import cache должен переимпортировать audio asset через dependency tracking.

Минимальный sidecar формат:

```json
{
  "mode": "Streaming",
  "loop": {
    "enabled": true,
    "begin": 0.5,
    "end": 8.25
  },
  "platforms": [
    { "name": "desktop", "packaging": "copy" },
    { "name": "android", "packaging": "streaming_asset" }
  ]
}
```

Defaults:

- `mode`: `Static`;
- `loop.enabled`: `false`;
- `loop.begin`: `0`;
- `loop.end`: audio length in seconds;
- `platforms`: empty array.

`Static` означает, что future runtime may decode the sound into memory before playback. `Streaming` means future runtime should keep the asset suitable for progressive reading from packaged game data. Эта задача сохраняет выбор режима в metadata, но не выполняет playback.

## Metadata artifact

Importer пишет один cache artifact:

```text
resources/<uid>/audio.e2audio.json
```

Artifact format:

- `format`: `Electron2D.AudioImportMetadata`;
- `version`: `1`;
- `source`: исходный `res://...`;
- `uid`: stable `uid://...`, созданный из source path;
- `audioFormat`: `Wav` или `OggVorbis`;
- `mode`: `Static` или `Streaming`;
- `sampleRate`;
- `channelCount`;
- `bitsPerSample`;
- `sampleCount`;
- `length`;
- `loop`;
- `platforms`.

JSON output должен быть stable: platform packaging entries сортируются по `name`.

## Audio metadata

WAV importer читает RIFF/WAVE chunks:

- `fmt ` chunk для channel count, sample rate, block align и bits per sample;
- `data` chunk для sample count и length;
- только PCM и IEEE float payloads являются допустимыми для первой версии.

OGG importer читает Ogg Vorbis identification packet для channel count и sample rate. Length берётся из final granule position, если он доступен; иначе length и sample count равны `0`.

Loop metadata приходит из sidecar. Source-embedded loop tags можно добавить отдельной задачей, если они понадобятся для editor import UX.

## Создание runtime resources

Internal `AudioImportResourceFactory` должен уметь создать `AudioStream` instance из metadata. Этот instance exposes только public `AudioStream` contract: length, monophonic state, meta-stream state и sample eligibility. Import metadata, file format, packaging decisions и loop boundaries остаются internal data для будущих audio runtime задач.

## Критерии приёмки

- Integration tests проверяют WAV metadata, length, channel count, loop metadata, static mode и platform packaging entries.
- Integration tests проверяют OGG Vorbis sample rate, channel count, streaming mode и length из granule position.
- Integration tests проверяют sidecar как dependency: изменение sidecar приводит к `DependencyChanged` reimport.
- Golden-data test фиксирует exact JSON output `AudioImportMetadata`.
- Public API guard включает `AudioStream`, но не экспортирует importer/cache implementation types.
- Public `AudioStream` members имеют полную XML documentation.
