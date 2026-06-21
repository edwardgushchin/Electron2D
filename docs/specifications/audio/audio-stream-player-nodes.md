# AudioStreamPlayer и AudioStreamPlayer2D

Статус: целевая спецификация `T-0073`.
Дата: 2026-06-21.
Связанные документы: `docs/specifications/releases/0.1.0-preview.md`, `docs/specifications/audio/audio-server-voice-handles.md`, `docs/specifications/resources/audio-stream-import.md`.

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

Этот расчёт является deterministic preview behavior. Отдельный listener node и area-based bus routing остаются будущими audio задачами.

## Tests

Acceptance tests:

- public API tests фиксируют exported surface, inheritance, defaults и validation;
- integration tests проверяют play/stop/pause/resume, `Playing` property, `Seek()`, `MaxPolyphony`, loop metadata, `VolumeDb`, `VolumeLinear`, `PitchScale`;
- integration tests проверяют `AudioStreamPlayer2D` attenuation и panning через internal voice playback snapshot;
- scene tree test проверяет `Autoplay` на enter-tree и stop на exit-tree;
- public baseline test добавляет оба новых exported types;
- XML documentation verifier должен проходить для всех новых public members.

## Out of scope

- пользовательские buses, mute, solo, effects и global volume settings;
- публичные playback handle types;
- `AudioStreamPlayback` object API;
- physical audio device lifecycle;
- listener nodes, area routing, doppler и 3D positional audio.
