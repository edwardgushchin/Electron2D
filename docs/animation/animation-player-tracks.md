# Animation, AnimationLibrary и AnimationPlayer baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1-preview` должен предоставить runtime baseline для ресурсных animation tracks. Ресурс `Animation` хранит value tracks и method call tracks, `AnimationLibrary` группирует именованные animations, а `AnimationPlayer` воспроизводит их во время `SceneTree.ProcessFrame()`.

Задача не реализует editor timeline, blend tree, animation state machine, audio tracks, 3D transform tracks, skeletal animation, секции и capture mode. Эти части остаются отдельными задачами после базового runtime-среза.

## Публичный API

### Animation

- `Animation` наследуется от `Resource`.
- `Length` задаёт длительность animation в секундах.
- `LoopMode` задаёт поведение на границе animation.
- `TrackTypeEnum` содержит поддержанные типы `Value` и `Method`.
- `InterpolationTypeEnum` содержит `Nearest` для дискретного значения и `Linear` для интерполяции между соседними ключами.
- `LoopModeEnum` содержит `None` и `Linear`.
- `AddTrack(Animation.TrackTypeEnum type, int atPosition = -1)` добавляет value или method track.
- `RemoveTrack(int trackIdx)`, `GetTrackCount()`, `TrackGetType(int trackIdx)`, `TrackSetEnabled(int trackIdx, bool enabled)` и `TrackIsEnabled(int trackIdx)` управляют tracks.
- `TrackSetPath(int trackIdx, NodePath path)` и `TrackGetPath(int trackIdx)` задают путь до node и property subname для value track или до node для method track.
- `TrackSetInterpolationType(int trackIdx, Animation.InterpolationTypeEnum interpolation)` и `TrackGetInterpolationType(int trackIdx)` управляют interpolation для value track.
- `TrackInsertKey(int trackIdx, double time, Variant value)` добавляет или заменяет value key.
- `MethodTrackInsertKey(int trackIdx, double time, StringName method, Variant[]? arguments = null)` добавляет или заменяет method call key.
- `TrackGetKeyCount(int trackIdx)`, `TrackGetKeyTime(int trackIdx, int keyIdx)` и `TrackGetKeyValue(int trackIdx, int keyIdx)` читают value key data.
- `ValueTrackInterpolate(int trackIdx, double time)` возвращает дискретное или интерполированное значение для value track.

### AnimationLibrary

- `AnimationLibrary` наследуется от `Resource`.
- `AddAnimation(StringName name, Animation animation)` добавляет animation и возвращает `Error`.
- `RemoveAnimation(StringName name)` удаляет animation.
- `RenameAnimation(StringName name, StringName newName)` переименовывает animation без пересоздания resource.
- `HasAnimation(StringName name)` проверяет наличие animation.
- `GetAnimation(StringName name)` возвращает animation или `null`.
- `GetAnimationList()` возвращает имена в стабильном alphabetical order.

### AnimationPlayer

- `AnimationPlayer` наследуется от `Node`.
- Конструктор регистрирует сигнал `animation_finished`.
- `RootNode` задаёт node, относительно которого разрешаются track paths. Значение по умолчанию - `..`.
- `Autoplay` содержит имя animation, которую нужно запустить в `_Ready()`.
- `AssignedAnimation` хранит последнюю выбранную animation.
- `CurrentAnimation`, `CurrentAnimationPosition` и `CurrentAnimationLength` описывают текущее воспроизведение.
- `SpeedScale` умножает скорость текущего playback.
- `AddAnimationLibrary(StringName name, AnimationLibrary library)`, `RemoveAnimationLibrary(StringName name)`, `HasAnimationLibrary(StringName name)`, `GetAnimationLibrary(StringName name)` и `GetAnimationLibraryList()` управляют libraries.
- `HasAnimation(StringName name)` проверяет animation с учётом default library и `library/animation` names.
- `Play(StringName name = default, double customBlend = -1.0, float customSpeed = 1.0f, bool fromEnd = false)` запускает animation.
- `Queue(StringName name)`, `GetQueue()` и `ClearQueue()` управляют FIFO-очередью.
- `Pause()` приостанавливает playback без сброса позиции.
- `Stop(bool keepState = false)` останавливает playback; при `keepState == false` позиция сбрасывается к началу.
- `IsPlaying()` сообщает, активен ли playback.
- `Advance(double delta)` продвигает playback на заданное время и используется тем же путём, что `SceneTree.ProcessFrame()`.

## Resolve и property application

- Bare animation name ищется в default library с пустым именем.
- Имя вида `library/animation` ищется в library с указанным именем.
- Value track path должен содержать node path и хотя бы один property subname, например `Target:Position`.
- Method track path указывает node, на котором вызывается method.
- `AnimationPlayer` сначала разрешает `RootNode` относительно себя, затем разрешает node path track относительно найденного root.
- Property lookup выполняется по public instance property или field. Имена сравниваются ordinal, с fallback на ordinal ignore-case.
- Value tracks поддерживают запись целого property, например `Target:Position`, и вложенного property, например `Target:Position:X`.
- Ошибка разрешения target или property не должна останавливать tree traversal. Такая track application пропускается, а остальные tracks продолжают применяться.

## Playback behavior

- `Play()` применяет value tracks на стартовой позиции сразу.
- `SceneTree.ProcessFrame(delta)` и `Advance(delta)` двигают `CurrentAnimationPosition` на `delta * SpeedScale * customSpeed`.
- При `SpeedScale == 0` или `customSpeed == 0` playback остаётся active, но позиция не меняется.
- Value tracks применяются после вычисления новой позиции.
- `InterpolationTypeEnum.Nearest` возвращает последний key со временем `<= time`.
- `InterpolationTypeEnum.Linear` интерполирует `float`, `double`, `int`, `long`, `Vector2` и `Color`; остальные Variant types ведут себя как `Nearest`.
- Method call keys вызываются один раз, когда playback пересекает их время вперёд. Method keys не вызываются при начальном `Play()` без продвижения времени.
- `LoopModeEnum.None` останавливает playback на `Length`, применяет финальные value tracks, испускает `animation_finished` и затем запускает следующий queued animation, если очередь не пуста.
- `LoopModeEnum.Linear` переходит к началу и продолжает playback без `animation_finished`.
- Queue сохраняет порядок добавления. Завершение animation испускает `animation_finished` для завершённого имени до запуска следующей queued animation.

## Validation

- `Animation.Length` должен быть finite и не меньше `0`.
- `AnimationPlayer.SpeedScale`, `Play(customSpeed)` и `Advance(delta)` принимают только finite values.
- Track index и key index должны находиться внутри range.
- `AddTrack()` поддерживает только value и method tracks.
- `TrackInsertKey()` разрешён только для value tracks.
- `MethodTrackInsertKey()` разрешён только для method tracks.
- `AnimationLibrary.AddAnimation()` возвращает `Error.InvalidParameter` для `null` animation или пустого имени, `Error.AlreadyExists` для duplicate name и `Error.Ok` при успехе.
- `AnimationPlayer.AddAnimationLibrary()` возвращает `Error.InvalidParameter` для `null` library, `Error.AlreadyExists` для duplicate library name и `Error.Ok` при успехе.

## Acceptance Criteria

- `Animation` хранит value tracks и method call tracks с deterministic key order.
- `Animation.ValueTrackInterpolate()` покрывает discrete и interpolated values.
- `AnimationLibrary` управляет именованными animations и отдаёт стабильный список имён.
- `AnimationPlayer` воспроизводит animation через `SceneTree.ProcessFrame()`, применяет property tracks по `NodePath`, вызывает method tracks и не ломает traversal при пропущенном target.
- Queue playback и сигнал `animation_finished` покрыты integration tests.
- Public API новых типов имеет полную XML documentation на каждом public member.
- Implementation documentation добавлена в `docs/animation/`.

## Фактическое состояние, ограничения и проверки

## Текущее состояние

`0.1-preview` теперь поддерживает ресурсные animation tracks:

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
