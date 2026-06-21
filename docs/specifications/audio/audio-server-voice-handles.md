# AudioServer и внутренние voice handles

Статус: целевая спецификация `T-0072`.
Дата: 2026-06-21.
Связанные документы: `docs/specifications/releases/0.1.0-preview.md`, `docs/specifications/resources/audio-stream-import.md`.

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
