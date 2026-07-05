# SpriteFrames и AnimatedSprite2D baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1-preview` должен предоставить runtime baseline для покадровой 2D-анимации: ресурс `SpriteFrames` хранит именованные анимации, а узел `AnimatedSprite2D` воспроизводит выбранную анимацию в `SceneTree.ProcessFrame()` и отправляет текущую texture frame в существующий canvas submission pipeline.

Задача не реализует editor UI для редактирования `SpriteFrames`, `AnimationPlayer`, `Tween`, timeline, import slicing или сложные animation tracks. Эти части остаются отдельными задачами анимационного и editor-доменов.

## Публичный API

### SpriteFrames

- `SpriteFrames` наследуется от `Resource`.
- Ресурс содержит анимации по имени `StringName`.
- При создании есть пустая анимация `default`.
- `AddAnimation(StringName animation)` добавляет пустую анимацию.
- `HasAnimation(StringName animation)` проверяет наличие анимации.
- `RemoveAnimation(StringName animation)` удаляет анимацию; если удалена последняя, создаётся пустая `default`.
- `RenameAnimation(StringName animation, StringName newName)` переименовывает анимацию без потери frames.
- `DuplicateAnimation(StringName animationFrom, StringName animationTo)` копирует frames, speed и loop mode.
- `Clear(StringName animation)` удаляет все frames внутри одной анимации.
- `ClearAll()` удаляет все анимации и создаёт пустую `default`.
- `GetAnimationNames()` возвращает имена в стабильном alphabetical order.
- `SetAnimationSpeed(StringName animation, float fps)` и `GetAnimationSpeed(StringName animation)` управляют speed в frames per second.
- `SetAnimationLoop(StringName animation, bool loop)` и `GetAnimationLoop(StringName animation)` остаются compatibility helpers для linear loop.
- `SetAnimationLoopMode(StringName animation, SpriteFrames.LoopModeEnum loopMode)` и `GetAnimationLoopMode(StringName animation)` управляют loop mode.
- `AddFrame(StringName animation, Texture2D texture, float duration = 1.0f, int atPosition = -1)` добавляет frame в конец или в заданную позицию.
- `SetFrame(StringName animation, int index, Texture2D texture, float duration = 1.0f)` заменяет frame.
- `RemoveFrame(StringName animation, int index)` удаляет frame.
- `GetFrameCount(StringName animation)`, `GetFrameTexture(StringName animation, int index)` и `GetFrameDuration(StringName animation, int index)` читают frames.

### AnimatedSprite2D

- `AnimatedSprite2D` наследуется от `Node2D`, а не от `Sprite2D`, чтобы public API не получал свойства `Texture`/`Region*`, которых нет в контракте узла.
- Узел содержит `SpriteFrames? SpriteFrames`, `StringName Animation`, `string Autoplay`, `int Frame`, `float FrameProgress`, `float SpeedScale`, `bool Centered`, `Vector2 Offset`, `bool FlipH`, `bool FlipV`.
- `Play(StringName name = default, float customSpeed = 1.0f, bool fromEnd = false)` начинает или возобновляет animation playback.
- `PlayBackwards(StringName name = default)` вызывает обратное воспроизведение текущей или указанной анимации.
- `Pause()` оставляет текущие `Frame` и `FrameProgress`.
- `Stop()` останавливает playback и сбрасывает `Frame`, `FrameProgress` и custom speed.
- `IsPlaying()` возвращает `true`, если playback активен, даже при `SpeedScale == 0` или custom speed `0`.
- `GetPlayingSpeed()` возвращает `SpeedScale * customSpeed`, если playback активен; иначе `0`.
- `SetFrameAndProgress(int frame, float progress)` задаёт frame и progress без дополнительного сброса progress.
- `GetRect()` возвращает local destination rect текущего frame.
- `IsPixelOpaque(Vector2 position)` проверяет pixel opacity текущего frame texture.

## Playback behavior

- `Autoplay` запускается в `_Ready()`, если `SpriteFrames` содержит указанную анимацию.
- Изменение `Animation` сбрасывает `Frame` и `FrameProgress`.
- Изменение `Frame` сбрасывает `FrameProgress`.
- `FrameProgress` хранится в диапазоне `0..1`.
- Frame duration считается как `SpriteFrames.GetFrameDuration(animation, frame) / (SpriteFrames.GetAnimationSpeed(animation) * Abs(GetPlayingSpeed()))`.
- `SpeedScale == 0` или custom speed `0` сохраняют playing state, но не двигают кадры.
- `LoopModeEnum.None` останавливает playback после последнего frame при движении вперёд или после первого frame при движении назад.
- `LoopModeEnum.Linear` переходит с конца к началу или с начала к концу.
- `LoopModeEnum.Pingpong` меняет направление на концах. Первый и последний frame не должны проигрываться дважды при смене направления.
- Если текущий frame texture заменён внутри `SpriteFrames`, следующий canvas submission использует новую texture без пересоздания `AnimatedSprite2D`.

## Rendering behavior

- `AnimatedSprite2D` отправляет texture command только когда текущая анимация существует и frame texture не `null`.
- Source rect равен полному размеру текущей frame texture.
- Destination rect учитывает `Centered` и `Offset`.
- Canvas submission должен сохранять `FlipH`, `FlipV`, inherited `Modulate`, `SelfModulate`, layer/z-index/y-sort ordering и transform rules как для других canvas items.

## Validation

- Empty animation names are invalid for `SpriteFrames` resource methods.
- Duplicate animation names are invalid.
- Missing animation names are invalid for mutating/read methods that require an existing animation.
- Frame index must be inside the animation frame range.
- Frame duration and animation speed must be finite and greater than `0`.
- `Texture2D` frame texture must not be `null` when adding or replacing a frame.

## Acceptance Criteria

- `SpriteFrames` API manages animations, sorted names, frame insert/replace/remove, speed and loop modes.
- `AnimatedSprite2D` supports autoplay, play, reverse playback, pause, stop, `Frame`, `FrameProgress`, `SpeedScale` and custom speed.
- Runtime frame advancement is deterministic through `SceneTree.ProcessFrame()`.
- Canvas submission uses the current frame texture and reacts to frame texture replacement inside `SpriteFrames`.
- Public API is documented with full XML documentation on every new public type/member.
- Integration tests cover resource API, playback timing, loop behavior, autoplay, rendering submission and resource update behavior.
- Implementation documentation is added under `docs/animation/`.

## Фактическое состояние, ограничения и проверки

## Текущее состояние

`0.1-preview` поддерживает простой runtime-путь для покадровой 2D-анимации:

- `SpriteFrames` хранит именованные animations, frame textures, relative frame durations, speed в frames per second и loop mode.
- `AnimatedSprite2D` читает `SpriteFrames`, обновляет playback state в `SceneTree.ProcessFrame()` и отправляет текущую texture frame в canvas submission.

`AnimatedSprite2D` наследуется от `Node2D`. Он не наследуется от `Sprite2D`, чтобы его public API не получал прямые свойства `Texture` и `Region*`; текущий frame берётся только из `SpriteFrames`.

## SpriteFrames

Новый `SpriteFrames` resource всегда содержит пустую animation `default`. Ресурс поддерживает:

- добавление, удаление, переименование и дублирование animations;
- `Clear()` для одной animation и `ClearAll()` для полного сброса с восстановлением пустой `default`;
- стабильный alphabetical order в `GetAnimationNames()`;
- `AddFrame()`, `SetFrame()`, `RemoveFrame()`, `GetFrameCount()`, `GetFrameTexture()` и `GetFrameDuration()`;
- `SetAnimationSpeed()` / `GetAnimationSpeed()`;
- `SetAnimationLoop()` / `GetAnimationLoop()` для linear loop compatibility;
- `SetAnimationLoopMode()` / `GetAnimationLoopMode()` для `None`, `Linear` и `Pingpong`.

Пустые animation names, duplicate names, missing animations, invalid frame indexes, non-finite speed/duration и duration/speed `<= 0` завершаются явными exceptions.

## AnimatedSprite2D

`AnimatedSprite2D` поддерживает:

- `SpriteFrames`, `Animation`, `Autoplay`, `Frame`, `FrameProgress`, `SpeedScale`;
- `Centered`, `Offset`, `FlipH`, `FlipV`;
- `Play()`, `PlayBackwards()`, `Pause()`, `Stop()`, `IsPlaying()`, `GetPlayingSpeed()`;
- `SetFrameAndProgress()`, `GetRect()` и `IsPixelOpaque()`.

`Autoplay` выполняется в `_Ready()`, если строка не пустая. `_Process(delta)` продвигает frame state перед draw traversal, поэтому canvas submission получает уже обновлённый frame. `GetPlayingSpeed()` возвращает только `SpeedScale * customSpeed` с учётом текущего pingpong direction; animation FPS применяется отдельно при расчёте продвижения кадра.

## Loop behavior

- `None` останавливает playback при достижении первого или последнего frame.
- `Linear` переходит на противоположный конец animation.
- `Pingpong` меняет direction на границе. При переходе direction первый и последний frame не повторяются дважды подряд.
- `SpeedScale == 0` или `customSpeed == 0` оставляют `IsPlaying() == true`, но не продвигают frame.

## Rendering

Canvas submission для `AnimatedSprite2D` использует текущую frame texture:

- source rect равен полному размеру текущей texture;
- destination rect учитывает `Centered` и `Offset`;
- inherited `Modulate`, `SelfModulate`, `ZIndex`, `YSortEnabled`, layer, transform и pixel snapping работают через существующий `CanvasSubmissionContext`;
- замена texture внутри `SpriteFrames.SetFrame()` видна в следующем submission без пересоздания `AnimatedSprite2D`.

## Проверки

- `SpriteFramesAnimatedSprite2DTests.SpriteFramesManagesAnimationsFramesSpeedAndLoopModes`
- `SpriteFramesAnimatedSprite2DTests.AnimatedSprite2DAutoplayAdvancesFramesAndLoops`
- `SpriteFramesAnimatedSprite2DTests.AnimatedSprite2DStopsAtNonLoopingEndAndCanPlayBackwards`
- `SpriteFramesAnimatedSprite2DTests.AnimatedSprite2DSubmissionUsesCurrentFrameAndUpdatedSpriteFramesResource`
