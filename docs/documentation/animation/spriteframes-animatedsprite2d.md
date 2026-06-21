# SpriteFrames и AnimatedSprite2D baseline

## Текущее состояние

`0.1.0 Preview` поддерживает простой runtime-путь для покадровой 2D-анимации:

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
