# Импорт WAV/OGG в AudioStream

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0039`.
Обновлено: 2026-06-21.
Связанные документы: [Import cache ресурсов](resource-import-cache.md), [Electron2D 0.1-preview](../releases/0.1-preview.md).

## Назначение

`T-0039` добавляет resource importer для WAV и OGG source assets. Importer должен создавать stable cache artifact с metadata, из которого future audio runtime сможет создать `AudioStream` без чтения исходного проекта как generated state.

В `0.1-preview` задача не добавляет `AudioServer`, playback nodes, mixer graph или device playback. Результат закрывает asset pipeline baseline: source file остаётся в проекте, generated metadata лежит в import cache, UID стабилен относительно `res://` path, а loop/static/streaming/platform packaging decisions сохраняются в diff-friendly JSON.

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal baseline.
Задача: `T-0039`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен импорт WAV и OGG source assets в `AudioStream` metadata cache. Внутренний importer означает, что код находится внутри runtime assembly и доступен тестам, будущему редактору и будущим инструментам, но не добавляет пользовательские importer-классы в public API.

Текущие типы находятся в:

- `src/Electron2D/Runtime/Audio/AudioStream.cs` - public base resource для audio metadata queries;
- `src/Electron2D/Runtime/Audio/ImportedAudioStream.cs` - internal stream instance, созданный из import metadata;
- `src/Electron2D/Assets/Resources/Importing/AudioStreamImporter.cs` - importer для `.wav` и `.ogg`;
- `src/Electron2D/Assets/Resources/Importing/AudioMetadataReader.cs` - читает WAV RIFF/WAVE и OGG Vorbis metadata без decoding;
- `src/Electron2D/Assets/Resources/Importing/AudioImportMetadata.cs` - stable cache metadata model;
- `src/Electron2D/Assets/Resources/Importing/AudioImportMetadataTextSerializer.cs` - stable JSON serializer;
- `src/Electron2D/Assets/Resources/Importing/AudioImportResourceFactory.cs` - создаёт `AudioStream` из metadata.

`ResourceImportOptions.CreateDefault()` регистрирует `AudioStreamImporter` вместе с resource, texture, font и shader importers.

## Sidecar настройки

Optional sidecar хранится рядом с audio source:

```text
res://audio/theme.ogg.e2import.json
```

Sidecar является source data. Если он меняется, import cache видит это как изменение dependency и переимпортирует audio asset.

Поддерживаемые поля:

- `mode`: `Static` или `Streaming`;
- `loop.enabled`: включает loop metadata;
- `loop.begin`: loop start в секундах;
- `loop.end`: loop end в секундах;
- `platforms`: список packaging metadata entries с `name` и `packaging`.

Defaults:

- `mode`: `Static`;
- `loop.enabled`: `false`;
- `loop.begin`: `0`;
- `loop.end`: audio length;
- `platforms`: empty array.

## Cache artifact

Importer пишет:

```text
<cacheRoot>/resources/<uid>/audio.e2audio.json
```

Artifact содержит:

- source path;
- stable UID;
- audio format `Wav` или `OggVorbis`;
- import mode `Static` или `Streaming`;
- sample rate;
- channel count;
- bits per sample;
- sample count;
- length in seconds;
- loop metadata;
- platform packaging metadata.

Platform entries сортируются по `name`, чтобы output оставался стабильным для version control и golden-data проверок.

## Runtime resource factory

`AudioImportResourceFactory.CreateAudioStream()` создаёт internal `ImportedAudioStream` и возвращает его как `AudioStream`.

`AudioStream` public API в этой задаче ограничен metadata queries:

- `GetLength()`;
- `IsMonophonic()`;
- `IsMetaStream()`;
- `CanBeSampled()`.

`Static` imports возвращают `true` из `CanBeSampled()`. `Streaming` imports возвращают `false`. Playback nodes используют эти metadata через `AudioStreamPlayer` и `AudioStreamPlayer2D`; пользовательские buses, advanced routing и physical device lifecycle остаются отдельными задачами audio runtime.

## Текущие ограничения

- Audio decoding и playback ещё не реализованы.
- Source-embedded loop tags не читаются; loop metadata задаётся sidecar-файлом.
- OGG reader ожидает Vorbis identification packet и использует final granule position для length.
- Platform packaging metadata пока фиксируется как import decision, но не создаёт platform-specific packaged files.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AudioStreamImportTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~AudioImportMetadataGoldenTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~AudioStreamPublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
