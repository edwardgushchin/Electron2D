# SpriteFrames и AnimatedSprite2D baseline

## Цель

`0.1.0 Preview` должен предоставить runtime baseline для покадровой 2D-анимации: ресурс `SpriteFrames` хранит именованные анимации, а узел `AnimatedSprite2D` воспроизводит выбранную анимацию в `SceneTree.ProcessFrame()` и отправляет текущую texture frame в существующий canvas submission pipeline.

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
- Implementation documentation is added under `docs/documentation/animation/`.
