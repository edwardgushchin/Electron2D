# Tween baseline

## Текущее состояние

`0.1.0 Preview` теперь поддерживает runtime `Tween` для коротких script-driven animations:

- `SceneTree.CreateTween()` создаёт valid tween и регистрирует его в processing list дерева;
- `Node.CreateTween()` создаёт tween через текущее дерево и привязывает его к node lifetime;
- `TweenProperty()` интерполирует public property или field;
- `TweenInterval()` добавляет шаг ожидания;
- `TweenCallback()` вызывает `Callable` в sequence order;
- `finished` и `step_finished` испускаются при completion sequence и отдельных шагов.

Standalone `new Tween()` остаётся invalid instance: он регистрирует signals, но не принимает tweeners и не двигается без `SceneTree`.

## Processing

`SceneTree.ProcessFrame(delta)` выполняет порядок:

1. node `_Process(delta)` callbacks;
2. internal process lifecycle handlers;
3. active tweens;
4. draw traversal.

Tweeners выполняются последовательно. Если шаг завершился ровно на границе frame delta и следующий шаг имеет нулевую длительность, следующий шаг будет выполнен на следующем process/custom step. Это сохраняет предсказуемую границу step completion.

`CustomStep(delta)` вручную двигает tween даже когда он paused, но всё ещё уважает bound node: если node больше не valid или не внутри дерева, tween остаётся valid и не продвигается.

## Property tweeners

`TweenProperty(object, property, finalVal, duration)` принимает `NodePath` как property path:

- `Position` задаёт весь public member `Position`;
- `Position:X` задаёт nested public member `X` внутри `Position`.

Lookup ищет public instance property или field сначала по точному ordinal name, затем по ordinal ignore-case fallback. Если member, conversion или target недоступны, конкретный step завершается без падения всего traversal.

Initial value захватывается при первом фактическом старте property tweener после delay. `Stop()` сбрасывает elapsed time и sequence index, но не откатывает target property и не захватывает initial value заново.

Поддержанная interpolation value set:

- integer;
- floating-point;
- `Vector2`;
- `Color`.

Для остальных Variant values step пишет final value только при completion.

## Easing и time control

`Tween.TransitionType` содержит linear и deterministic curved modes: `Sine`, `Quint`, `Quart`, `Quad`, `Expo`, `Elastic`, `Cubic`, `Circ`, `Bounce`, `Back`, `Spring`.

`Tween.EaseType` содержит `In`, `Out`, `InOut` и `OutIn`.

`SetTrans()` и `SetEase()` задают defaults для будущих property tweeners. `PropertyTweener.SetTrans()` и `PropertyTweener.SetEase()` переопределяют настройки конкретного шага.

`SetSpeedScale()` умножает frame delta перед advancing. `GetTotalElapsedTime()` возвращает уже scaled elapsed time.

## Pause, stop и cancellation

- `Pause()` останавливает automatic processing без сброса state.
- `Play()` возобновляет paused tween или запускает stopped tween с начала sequence.
- `Stop()` сбрасывает sequence index и elapsed time, но оставляет tween valid.
- `Kill()` invalidates tween, очищает pending tweeners и не испускает `finished`.
- После естественного завершения sequence tween становится invalid и испускает `finished`.

Bound tween, созданный через `Node.CreateTween()`, не двигается пока bound node detached from tree. `IsRunning()` в этот момент возвращает `false`, но `IsValid()` остаётся `true`.

## Проверки

- `TweenTests.SceneTreeCreateTweenInterpolatesPropertyWithEasingDuringProcess`
- `TweenTests.NodeCreateTweenBindsTweenAndStopsWhenBoundNodeLeavesTree`
- `TweenTests.PausePlayStopAndCustomStepUseDeterministicElapsedTime`
- `TweenTests.CompletionCallbacksSignalsAndStepSignalsRunAfterSequence`
- `TweenTests.KillCancelsTweenWithoutCompletionCallbacks`
