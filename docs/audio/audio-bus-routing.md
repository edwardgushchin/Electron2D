# Audio bus routing

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `T-0074`.
Дата: 2026-06-21.
Связанные документы: `docs/releases/0.1-preview.md`, `docs/audio/audio-server-voice-handles.md`, `docs/audio/audio-stream-player-nodes.md`.

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
- сложный DSP graph и effects editor не входят в `0.1-preview`.

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

## Фактическое состояние, ограничения и проверки

Статус: реализовано в `T-0074`.
Дата: 2026-06-21.
Спецификация: `docs/audio/audio-bus-routing.md`.

## Что реализовано

`AudioServer` теперь хранит process-wide bus graph:

- `Master` всегда существует с индексом `0`;
- пользовательские buses можно добавлять, удалять, перемещать и переименовывать;
- пользовательский bus отправляет сигнал в другой bus слева от себя;
- `AudioStreamPlayer.Bus` и `AudioStreamPlayer2D.Bus` выбирают target bus по имени;
- неизвестное имя bus при playback заменяется на `Master`;
- громкость рассчитывается по routing path от выбранного bus до `Master`;
- mute и solo применяются к voices, которые стартуют после изменения bus state.

Общая громкость проекта управляется через volume API на `Master`, то есть через `AudioServer.SetBusVolumeDb(0, value)` или `AudioServer.SetBusVolumeLinear(0, value)`.

## Public API

`AudioServer` поддерживает:

- `SetBusCount(int amount)`;
- `AddBus(int atPosition = -1)`;
- `RemoveBus(int index)`;
- `MoveBus(int index, int toIndex)`;
- `SetBusName(int busIdx, string name)`;
- `SetBusSend(int busIdx, StringName send)`;
- `GetBusSend(int busIdx)`;
- `SetBusVolumeDb(int busIdx, float volumeDb)`;
- `GetBusVolumeDb(int busIdx)`;
- `SetBusVolumeLinear(int busIdx, float volumeLinear)`;
- `GetBusVolumeLinear(int busIdx)`;
- `SetBusMute(int busIdx, bool enable)`;
- `IsBusMute(int busIdx)`;
- `SetBusSolo(int busIdx, bool enable)`;
- `IsBusSolo(int busIdx)`.

Существующие `GetBusCount()`, `GetBusName(int)` и `GetBusIndex(string)` работают поверх того же bus graph.

## Routing behavior

При старте voice `AudioServer` строит path:

```text
selected bus -> send bus -> ... -> Master
```

Итоговый `VolumeDb` равен volume voice плюс сумма `VolumeDb` каждого bus на path.

Если любой bus на path muted, итоговый `VolumeDb` становится `-80`. Если включён solo хотя бы на одном bus, voice остаётся слышимым только когда его path содержит solo bus; остальные voices получают `-80`.

`AudioVoicePlayback.Bus` во внутреннем snapshot содержит фактически выбранный bus. Это нужно тестам и будущим runtime-инструментам; публичных voice handles нет.

## Ограничения

В `0.1-preview` bus graph deliberately simple:

- audio effects и effect bypass не реализованы;
- editor UI для bus layout не реализован;
- сохранение bus layout в project settings относится к будущему settings/editor срезу;
- изменения bus state не пересчитывают уже запущенные voices, а применяются к следующим playback starts.

## Проверки

Focused checks:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~AudioServerPublicApiTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AudioServerBusRoutingTests" --no-restore -m:1
```

Coverage:

- public `AudioServer` surface зафиксирован unit test;
- добавление, удаление, перемещение, переименование и validation buses проверены integration tests;
- volume routing, unknown bus fallback, mute и solo проверены через internal voice playback snapshot.
