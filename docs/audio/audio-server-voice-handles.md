# AudioServer и внутренние voice handles

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `T-0072`.
Дата: 2026-06-21.
Связанные документы: `docs/releases/0.1-preview.md`, `docs/resources/audio-stream-import.md`.

## Контекст

`T-0039` добавила импорт WAV/OGG в `AudioStream`, но не добавила playback. `T-0072` вводит границу audio server, через которую будущие `AudioStreamPlayer` и `AudioStreamPlayer2D` смогут запускать несколько независимых voice instances.

Voice instance здесь означает внутреннюю запись воспроизведения одного `AudioStream`: у неё есть стабильный handle внутри runtime, ссылка на backend handle, состояние воспроизведения и параметры запуска. Backend handle не является public API и не должен попадать в пользовательский код.

## Цель

Добавить `AudioServer` как публичную process-wide границу аудио-подсистемы и внутренний voice lifecycle:

- public API предоставляет только стабильные query/lock методы audio server;
- public API не раскрывает concrete backend, backend tracks, backend handles или voice handles;
- internal API создаёт несколько voices, останавливает voice по handle и очищает voices, которые backend уже завершил;
- default backend пригоден для автоматических тестов без физического audio device.

## Public API

`AudioServer` должен быть public static type в namespace `Electron2D`.

Минимальный публичный срез:

- `GetMixRate()` возвращает частоту микширования в герцах;
- `GetOutputLatency()` возвращает оценку задержки вывода в секундах;
- `GetSpeakerMode()` возвращает speaker layout;
- `GetBusCount()` возвращает количество доступных buses;
- `GetBusName(int busIdx)` возвращает имя bus;
- `GetBusIndex(string busName)` возвращает индекс bus или `-1`, если bus не найден;
- `Lock()` и `Unlock()` сериализуют критические audio изменения через backend.

`AudioServer.SpeakerMode` должен содержать `Stereo`, `Surround31`, `Surround51` и `Surround71`.

В `T-0072` существует только default `Master` bus. Пользовательские buses, mute, solo, volume routing и global volume были вынесены в `T-0074`.

## Internal API

Внутренний API доступен только runtime, tests и будущим editor/tooling слоям:

- `AudioServer.PlayStream(AudioStream stream, AudioVoicePlayback playback)` создаёт voice и возвращает `AudioVoiceHandle`;
- `AudioServer.StopVoice(AudioVoiceHandle voice)` останавливает voice и освобождает backend resources;
- `AudioServer.IsVoiceActive(AudioVoiceHandle voice)` возвращает, жив ли voice;
- `AudioServer.GetActiveVoiceCount()` возвращает количество живых voices;
- `AudioServer.CleanupFinishedVoices()` освобождает voices, которые backend уже считает завершёнными;
- `AudioServer.SetBackend(IAudioServerBackend backend)` заменяет backend для startup/tests.

`AudioVoiceHandle`, backend interface и backend handle остаются internal.

## Validation

- `PlayStream` должен отклонять `null`, freed streams, unknown length errors from the stream и невалидные playback параметры.
- `volumeDb` должен быть finite.
- `pitchScale` должен быть finite и больше `0`.
- Операции над неизвестным или уже освобождённым voice handle должны выбрасывать `ArgumentException`.
- `GetBusName` должен проверять диапазон индекса.
- `GetBusIndex` должен отклонять пустое имя.

## Tests

Acceptance tests:

- public API test фиксирует минимальный exported surface `AudioServer` и default query values;
- integration test создаёт несколько voices, останавливает одну, очищает backend-finished voices и проверяет, что handles больше не активны;
- integration test проверяет, что exported public API не содержит backend, track или voice types;
- XML documentation verifier должен проходить для всех новых public members.

## Out of scope

- `AudioStreamPlayer` и `AudioStreamPlayer2D`;
- 2D attenuation, panning, positional audio и doppler;
- пользовательские buses, mute, solo и volume routing, вынесенные в `T-0074`;
- audio effects;
- runtime decoding API в public surface.

## Фактическое состояние, ограничения и проверки

Статус: реализовано в `T-0072`.
Дата: 2026-06-21.
Спецификация: `docs/audio/audio-server-voice-handles.md`.

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
