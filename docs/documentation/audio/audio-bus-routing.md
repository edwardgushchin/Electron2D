# Audio bus routing

Статус: реализовано в `T-0074`.
Дата: 2026-06-21.
Спецификация: `docs/specifications/audio/audio-bus-routing.md`.

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

В `0.1.0 Preview` bus graph deliberately simple:

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
