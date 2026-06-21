# AudioStreamPlayer и AudioStreamPlayer2D

Статус: реализовано в `T-0073`.
Дата: 2026-06-21.
Спецификация: `docs/specifications/audio/audio-stream-player-nodes.md`.

## Что реализовано

Добавлены два публичных узла сцены для playback:

- `AudioStreamPlayer` наследуется от `Node` и воспроизводит `AudioStream` без 2D-позиционирования;
- `AudioStreamPlayer2D` наследуется от `Node2D` и рассчитывает 2D attenuation и left/right panning перед запуском внутреннего voice.

Оба узла используют существующий `AudioServer` и не раскрывают внутренние voice handles в public API.

## Public API

Оба player-типа поддерживают:

- `Stream`;
- `Autoplay`;
- `Bus`;
- `MaxPolyphony`;
- `PitchScale`;
- `Playing`;
- `StreamPaused`;
- `VolumeDb`;
- `VolumeLinear`;
- `Play(float fromPosition = 0f)`;
- `Seek(float toPosition)`;
- `Stop()`;
- `GetPlaybackPosition()`;
- `HasStreamPlayback()`;
- signal `finished`.

`AudioStreamPlayer2D` дополнительно поддерживает:

- `AreaMask`;
- `Attenuation`;
- `MaxDistance`;
- `PanningStrength`.

## Playback behavior

`Play()` создаёт новый internal voice через `AudioServer.PlayStream()`, если `Stream` назначен. Если `Stream` равен `null`, player остаётся остановленным.

`Playing = true` вызывает `Play()`, а `Playing = false` вызывает `Stop()`.

`Stop()` останавливает все voices этого player и сбрасывает stored playback position в `0`.

`Seek()` меняет stored playback position. Если player активен и не paused, текущий voice перезапускается с новой позиции.

`StreamPaused = true` останавливает активные voices, но сохраняет `Playing = true` и текущую позицию. `StreamPaused = false` возобновляет playback с сохранённой позиции.

`MaxPolyphony` ограничивает количество simultaneous voices. Когда лимит достигнут, новый `Play()` останавливает самый старый voice этого player.

## Looping, volume и pitch

Loop flag берётся из internal metadata импортированного `AudioStream`. Пользовательские subclasses `AudioStream` без таких metadata считаются non-looping.

`VolumeDb` передаёт decibel volume offset. `VolumeLinear` конвертируется в `VolumeDb`; `0` maps to quiet floor.

`PitchScale` должен быть finite и больше `0`.

## 2D attenuation и panning

`AudioStreamPlayer2D` использует `GlobalPosition` и preview listener position `Vector2.Zero`.

Правила расчёта:

- `MaxDistance == 0` отключает distance attenuation и panning;
- при `distance >= MaxDistance` effective gain становится `0`;
- при `0 < distance < MaxDistance` gain равен `(1 - distance / MaxDistance) ^ Attenuation`;
- gain переводится в decibels и добавляется к `VolumeDb`;
- pan вычисляется по горизонтальному смещению и clamped в диапазон `[-1, 1]`.

Dedicated listener nodes, area routing, user buses, mute, solo и global volume остаются отдельными audio задачами.

## Проверки

Focused checks:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~AudioStreamPlayerPublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~AudioStreamPlayerPlaybackTests" --no-restore -m:1
```

Coverage:

- public surface, defaults и validation проверены unit tests;
- play/stop/pause/resume/seek, `Playing`, `MaxPolyphony`, loop metadata, volume, pitch и autoplay проверены integration tests;
- 2D attenuation и panning проверены через internal voice playback snapshot.
