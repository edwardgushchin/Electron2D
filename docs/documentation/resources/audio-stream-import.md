# Импорт WAV/OGG в AudioStream

Статус: реализованный internal baseline.
Задача: `T-0039`.
Обновлено: 2026-06-21.

## Что реализовано

Добавлен импорт WAV и OGG source assets в `AudioStream` metadata cache. Внутренний importer означает, что код находится внутри runtime assembly и доступен тестам, будущему редактору и будущим инструментам, но не добавляет пользовательские importer-классы в public API.

Текущие типы находятся в:

- `src/Electron2D/Audio/AudioStream.cs` - public base resource для audio metadata queries;
- `src/Electron2D/Audio/ImportedAudioStream.cs` - internal stream instance, созданный из import metadata;
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

`Static` imports возвращают `true` из `CanBeSampled()`. `Streaming` imports возвращают `false`. Playback, buses, voices, attenuation and device lifecycle остаются отдельными задачами audio runtime.

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
