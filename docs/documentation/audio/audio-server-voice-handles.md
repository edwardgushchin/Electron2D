# AudioServer и внутренние voice handles

Статус: реализовано в `T-0072`.
Дата: 2026-06-21.
Спецификация: `docs/specifications/audio/audio-server-voice-handles.md`.

## Что реализовано

`AudioServer` добавлен как публичная process-wide граница audio subsystem.

Публичный API:

- `AudioServer.GetMixRate()` возвращает текущую частоту микширования;
- `AudioServer.GetOutputLatency()` возвращает оценку задержки вывода;
- `AudioServer.GetSpeakerMode()` возвращает speaker layout;
- `AudioServer.GetBusCount()` возвращает количество buses;
- `AudioServer.GetBusName(int busIdx)` возвращает имя bus;
- `AudioServer.GetBusIndex(string busName)` возвращает индекс bus или `-1`;
- `AudioServer.SetBusCount(int amount)`, `AudioServer.AddBus()`, `AudioServer.RemoveBus()`, `AudioServer.MoveBus()` управляют пользовательскими buses;
- `AudioServer.SetBusName()`, `AudioServer.SetBusSend()`, `AudioServer.GetBusSend()` управляют именем и routing target;
- `AudioServer.SetBusVolumeDb()`, `AudioServer.SetBusVolumeLinear()`, `AudioServer.GetBusVolumeDb()`, `AudioServer.GetBusVolumeLinear()` управляют громкостью bus;
- `AudioServer.SetBusMute()`, `AudioServer.IsBusMute()`, `AudioServer.SetBusSolo()`, `AudioServer.IsBusSolo()` управляют mute и solo;
- `AudioServer.Lock()` и `AudioServer.Unlock()` группируют audio changes.

`Master` всегда доступен с индексом `0`. Пользовательские buses, mute, solo и volume routing реализованы в `T-0074`. Audio effects не входят в текущий baseline.

## Внутренний voice lifecycle

Внутренний voice lifecycle используется `AudioStreamPlayer` и `AudioStreamPlayer2D`.

Реализованы internal типы:

- `AudioVoiceHandle` - собственный runtime handle voice instance;
- `AudioBackendVoiceHandle` - handle concrete backend resource;
- `AudioVoicePlayback` - volume, pitch, loop, start position, panning и выбранный bus для запуска;
- `IAudioServerBackend` - internal backend boundary;
- `ManagedAudioServerBackend` - deterministic backend для тестов и headless runtime checks.

`AudioServer.PlayStream()` рассчитывает итоговый playback snapshot с учётом bus routing, создаёт собственный `AudioVoiceHandle`, сохраняет связь с backend handle и не раскрывает эту связь в public API. `AudioServer.StopVoice()` останавливает voice и освобождает backend resource. `AudioServer.CleanupFinishedVoices()` освобождает voices, которые backend уже считает завершёнными.

## Validation

`AudioServer.PlayStream()`:

- отклоняет `null`;
- отклоняет freed `AudioStream` instance;
- проверяет, что stream length finite и не отрицательный;
- проверяет, что volume finite;
- проверяет, что pitch finite и больше `0`.

Операции над неизвестным или уже освобождённым voice handle выбрасывают `ArgumentException`.

## Проверки

Focused checks:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~AudioServerPublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AudioServerVoiceTests" --no-restore -m:1
```

Coverage:

- public `AudioServer` surface зафиксирован unit test;
- multiple voices and cleanup проверены integration tests;
- exported public API не содержит internal backend, backend track или voice handle types.
- user bus routing, mute и solo дополнительно покрыты `AudioServerBusRoutingTests`.
