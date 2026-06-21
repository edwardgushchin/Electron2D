# Audio bus routing

Статус: целевая спецификация `T-0074`.
Дата: 2026-06-21.
Связанные документы: `docs/specifications/releases/0.1.0-preview.md`, `docs/specifications/audio/audio-server-voice-handles.md`, `docs/specifications/audio/audio-stream-player-nodes.md`.

## Контекст

`T-0072` добавила `AudioServer` и внутренний lifecycle voice, а `T-0073` добавила `AudioStreamPlayer` и `AudioStreamPlayer2D`. `T-0074` закрывает минимальный bus graph: пользовательские audio buses, routing, mute, solo и общую громкость через `Master`.

Bus graph здесь означает список именованных audio buses внутри `AudioServer`. Каждый voice выбирает bus по имени, а `AudioServer` рассчитывает итоговую громкость перед отправкой voice во внутренний backend.

## Цель

Добавить public API для управления buses:

- `Master` всегда существует с индексом `0`;
- пользовательские buses можно добавлять, удалять, перемещать и переименовывать;
- каждый пользовательский bus отправляет сигнал в другой bus слева от себя;
- громкость bus накапливается по routing path до `Master`;
- mute глушит весь routing path, если включён на любом bus path;
- solo пропускает только voices, чей routing path содержит solo bus, когда хотя бы один bus находится в solo;
- общая громкость проекта задаётся громкостью `Master` через тот же volume API;
- сложный DSP graph и effects editor не входят в `0.1.0 Preview`.

## Public API

`AudioServer` расширяется следующими public methods:

- `SetBusCount(int amount)`;
- `AddBus(int atPosition = -1)`;
- `RemoveBus(int index)`;
- `MoveBus(int index, int toIndex)`;
- `SetBusName(int busIdx, string name)`;
- `SetBusSend(int busIdx, StringName send)`;
- `StringName GetBusSend(int busIdx)`;
- `SetBusVolumeDb(int busIdx, float volumeDb)`;
- `float GetBusVolumeDb(int busIdx)`;
- `SetBusVolumeLinear(int busIdx, float volumeLinear)`;
- `float GetBusVolumeLinear(int busIdx)`;
- `SetBusMute(int busIdx, bool enable)`;
- `bool IsBusMute(int busIdx)`;
- `SetBusSolo(int busIdx, bool enable)`;
- `bool IsBusSolo(int busIdx)`.

Существующие `GetBusCount()`, `GetBusName(int)` и `GetBusIndex(string)` продолжают работать поверх общего списка buses.

## Validation

- `SetBusCount` не принимает значения меньше `1`.
- `Master` нельзя удалить, переместить или переименовать.
- `AddBus` с `-1` добавляет bus в конец списка; явная позиция должна быть в диапазоне от `1` до текущего `GetBusCount()`.
- `RemoveBus` и `MoveBus` принимают только пользовательские buses.
- Имя bus не может быть пустым, whitespace-only или дублировать уже существующий bus.
- `SetBusSend` для пользовательского bus принимает только существующий bus левее текущего; это исключает routing cycles.
- `SetBusSend` для `Master` принимает только пустое значение.
- Volume values должны быть finite; linear volume не может быть отрицательным.

## Routing rules

При `AudioServer.PlayStream()`:

- неизвестное имя bus заменяется на `Master`;
- routing path строится от выбранного bus до `Master`;
- итоговый `VolumeDb` равен voice volume плюс сумма `VolumeDb` каждого bus на path;
- если любой bus на path muted, итоговый `VolumeDb` становится quiet floor;
- если есть хотя бы один solo bus и path не содержит solo bus, итоговый `VolumeDb` становится quiet floor;
- `AudioVoicePlayback.Bus` сохраняет фактически использованное имя bus после fallback.

Quiet floor для preview runtime равен `-80 dB`. Это значение используется для deterministic tests и не обещает физическую тишину конкретного audio device.

## Tests

Acceptance tests:

- public API test фиксирует новый `AudioServer` surface;
- unit test проверяет добавление, удаление, перемещение, переименование buses и validation;
- integration test проверяет volume routing от player bus через user bus в `Master`;
- integration test проверяет mute и solo через internal voice playback snapshot;
- documentation check должен описывать отсутствие сложного DSP graph и effects editor.

## Out of scope

- audio effects, effect chain и effect bypass;
- editor UI для bus layout;
- сохранение bus layout в project settings;
- dedicated listener nodes, area routing и physical audio device lifecycle.
