# Animation, AnimationLibrary и AnimationPlayer baseline

## Текущее состояние

`0.1.0 Preview` теперь поддерживает ресурсные animation tracks:

- `Animation` хранит value tracks и method call tracks.
- `AnimationLibrary` группирует animations по именам и отдаёт стабильный список имён.
- `AnimationPlayer` монтирует libraries, запускает animations, применяет tracks во время `SceneTree.ProcessFrame()` и испускает сигнал `animation_finished`.

Это runtime baseline. Editor timeline, blend tree, state machine, audio tracks, 3D transform tracks, skeletal animation, секции и capture mode не реализованы в этой задаче.

## Animation

`Animation` наследуется от `Resource` и содержит:

- `Length` в секундах;
- `LoopMode` со значениями `None` и `Linear`;
- tracks типов `Value` и `Method`;
- включение/выключение tracks;
- `NodePath` для каждого track;
- sorted key storage с заменой key при совпадающем времени.

Value tracks добавляются через `TrackInsertKey()`. `ValueTrackInterpolate()` возвращает:

- первый key, если время раньше первого key;
- последний key, если время позже последнего key;
- последний key слева для `InterpolationTypeEnum.Nearest`;
- линейно интерполированное значение для `float`, `double`, `int`, `long`, `Vector2` и `Color`;
- последнее значение слева для остальных Variant types.

Method tracks добавляются через `MethodTrackInsertKey()`. Method key содержит method name и массив `Variant` arguments.

## AnimationLibrary

`AnimationLibrary` хранит `Animation` resources по `StringName`:

- `AddAnimation()` возвращает `Error.Ok`, `Error.InvalidParameter` или `Error.AlreadyExists`;
- `RemoveAnimation()` удаляет animation и ничего не делает для missing name;
- `RenameAnimation()` сохраняет тот же `Animation` instance под новым именем;
- `GetAnimationList()` возвращает имена в alphabetical order.

Library регистрирует user signals `animation_added`, `animation_removed` и `animation_renamed`.

## AnimationPlayer

`AnimationPlayer` наследуется от `Node` и реализует внутренний process hook. `SceneTree.ProcessFrame(delta)` вызывает пользовательский `_Process(delta)`, затем internal lifecycle handler, поэтому player продвигает playback даже без override `_Process()`.

Player поддерживает:

- default library с пустым именем;
- named libraries через `library/animation`;
- `RootNode`, по умолчанию `..`;
- `Autoplay` в `_Ready()`;
- `AssignedAnimation`, `CurrentAnimation`, `CurrentAnimationPosition`, `CurrentAnimationLength`;
- `SpeedScale`;
- `Play()`, `Queue()`, `GetQueue()`, `ClearQueue()`, `Pause()`, `Stop()` и `Advance()`.

Bare animation name ищется в default library. Имя вида `library/animation` ищется в named library.

## Track target resolution

`AnimationPlayer` сначала разрешает `RootNode` относительно себя. Track path затем разрешается относительно найденного root.

Value track path должен содержать property subname:

- `Target:Position` задаёт целое property `Position`;
- `Target:Position:X` задаёт вложенное property `X` внутри `Position`.

Property lookup ищет public instance property или field сначала точным ordinal name, затем ordinal ignore-case fallback. Если node, property или conversion не найдены, track пропускается, а остальные tracks продолжают применяться.

Method track path указывает node. Method key вызывает public method через `Callable`. Arguments передаются как boxed Variant values.

## Playback

`Play()` сразу применяет value tracks на стартовой позиции. Method tracks при этом не вызываются.

`Advance(delta)` и `SceneTree.ProcessFrame(delta)` двигают позицию на:

```text
delta * SpeedScale * customSpeed
```

Если множитель скорости равен `0`, `IsPlaying()` остаётся `true`, но позиция не меняется.

Для `LoopModeEnum.None` player останавливается на `Length`, применяет финальные value tracks, испускает `animation_finished` и запускает следующую queued animation, если очередь не пуста.

Для `LoopModeEnum.Linear` player переходит к началу animation и продолжает playback без `animation_finished`.

Method keys вызываются один раз, когда forward playback пересекает их время: key вызывается при условии `previous < keyTime <= current`.

## Проверки

- `AnimationPlayerTracksTests.AnimationStoresValueTracksAndInterpolatesDiscreteAndLinearValues`
- `AnimationPlayerTracksTests.AnimationLibraryManagesAnimationsWithStableNames`
- `AnimationPlayerTracksTests.AnimationPlayerAppliesInterpolatedAndDiscreteValueTracksDuringSceneProcess`
- `AnimationPlayerTracksTests.AnimationPlayerCallsMethodTracksOnceQueuesPlaybackAndEmitsCompletionSignal`
