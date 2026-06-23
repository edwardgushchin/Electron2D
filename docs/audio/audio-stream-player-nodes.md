# AudioStreamPlayer и AudioStreamPlayer2D

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `T-0073`.
Дата: 2026-06-21.
Связанные документы: `docs/releases/0.1.0-preview.md`, `docs/audio/audio-server-voice-handles.md`, `docs/resources/audio-stream-import.md`.

## Контекст

`T-0039` добавила импорт аудио-ресурсов, а `T-0072` добавила `AudioServer` и внутренний voice lifecycle. `T-0073` должна дать пользователю scene nodes для запуска `AudioStream` без раскрытия внутренних handles.

Voice здесь означает один внутренний запуск аудио-потока внутри runtime. Пользователь управляет им через свойства и методы node, а не через handle.

## Цель

Добавить два публичных node-типа:

- `AudioStreamPlayer` для непозиционного звука;
- `AudioStreamPlayer2D` для звука с 2D attenuation и left/right panning.

Оба типа должны использовать существующий `AudioServer.PlayStream()` и не добавлять публичные backend handles, backend tracks или диагностические методы.

## Public API

`AudioStreamPlayer` наследуется от `Node`.

Минимальный публичный срез:

- `AudioStream? Stream`;
- `bool Autoplay`;
- `StringName Bus`;
- `int MaxPolyphony`;
- `float PitchScale`;
- `bool Playing`;
- `bool StreamPaused`;
- `float VolumeDb`;
- `float VolumeLinear`;
- `float GetPlaybackPosition()`;
- `bool HasStreamPlayback()`;
- `void Play(float fromPosition = 0f)`;
- `void Seek(float toPosition)`;
- `void Stop()`.

`AudioStreamPlayer2D` наследуется от `Node2D` и содержит тот же playback-срез, а также:

- `int AreaMask`;
- `float Attenuation`;
- `float MaxDistance`;
- `float PanningStrength`.

Оба типа регистрируют signal `finished`.

## Playback rules

- `Stream = null` допустим, но `Play()` без stream не создаёт voice.
- Установка `Stream` останавливает все текущие voices этого player.
- `Play(fromPosition)` создаёт новый voice, если `Stream` задан и player не paused.
- `Playing = true` эквивалентно `Play()`, `Playing = false` эквивалентно `Stop()`.
- `Stop()` останавливает все voices этого player и сбрасывает playback position в `0`.
- `Seek(toPosition)` меняет stored playback position; если player активен и не paused, текущие voices перезапускаются с новой позиции.
- `StreamPaused = true` останавливает активные voices, но сохраняет намерение продолжить playback. `Playing` остаётся `true`.
- `StreamPaused = false` возобновляет playback с сохранённой позиции, если player был в состоянии playback.
- `MaxPolyphony` не может быть меньше `1`. Если лимит достигнут, новый `Play()` останавливает самый старый voice этого player.
- `PitchScale` должен быть finite и больше `0`.
- `VolumeDb`, `VolumeLinear`, playback positions, `Attenuation`, `MaxDistance` и `PanningStrength` должны быть finite. `VolumeLinear`, `Attenuation`, `MaxDistance` и `PanningStrength` не могут быть отрицательными.

## Looping

Loop flag берётся из internal metadata импортированного `AudioStream`. Обычные пользовательские `AudioStream` subclasses считаются non-looping, пока конкретный stream type не добавит собственные loop metadata.

## 2D attenuation и panning

`AudioStreamPlayer2D` рассчитывает параметры voice перед запуском:

- listener position для preview-среза равен `Vector2.Zero`;
- distance берётся от `GlobalPosition` player до listener position;
- `MaxDistance == 0` означает отсутствие distance attenuation;
- при `distance >= MaxDistance` effective linear gain становится `0`;
- при `0 < distance < MaxDistance` effective gain равен `(1 - distance / MaxDistance) ^ Attenuation`;
- effective gain переводится в decibels и добавляется к `VolumeDb`;
- `Pan` clamped в диапазон `[-1, 1]` и считается как `(relative.X / MaxDistance) * PanningStrength`; при `MaxDistance == 0` pan равен `0`.

Этот расчёт является deterministic preview behavior. Отдельный listener node и area-based bus routing остаются будущими audio задачами. Пользовательский bus graph закрывается отдельной задачей `T-0074`.

## Tests

Acceptance tests:

- public API tests фиксируют exported surface, inheritance, defaults и validation;
- integration tests проверяют play/stop/pause/resume, `Playing` property, `Seek()`, `MaxPolyphony`, loop metadata, `VolumeDb`, `VolumeLinear`, `PitchScale`;
- integration tests проверяют `AudioStreamPlayer2D` attenuation и panning через internal voice playback snapshot;
- scene tree test проверяет `Autoplay` на enter-tree и stop на exit-tree;
- public baseline test добавляет оба новых exported types;
- XML documentation verifier должен проходить для всех новых public members.

## Out of scope

- пользовательские buses, mute, solo и global volume settings, вынесенные в `T-0074`;
- audio effects;
- публичные playback handle types;
- `AudioStreamPlayback` object API;
- physical audio device lifecycle;
- listener nodes, area routing, doppler и 3D positional audio.

## Фактическое состояние, ограничения и проверки

Статус: реализовано в `T-0073`.
Дата: 2026-06-21.
Спецификация: `docs/audio/audio-stream-player-nodes.md`.

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

`Bus` выбирает audio bus по имени. Если указанное имя отсутствует в `AudioServer`, playback маршрутизируется через `Master`.

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

Dedicated listener nodes и area routing остаются отдельными audio задачами. Пользовательские buses, mute, solo и общая громкость через `Master` реализованы в `T-0074`.

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
- bus selection, fallback в `Master`, mute, solo и volume routing проверены `AudioServerBusRoutingTests`.
