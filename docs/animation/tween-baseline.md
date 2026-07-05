# Tween baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1-preview` должен предоставить deterministic runtime baseline для коротких script-driven animations: `Tween` создаётся через scene tree или node, обновляется во время `SceneTree.ProcessFrame()`, интерполирует property values, поддерживает easing, pause/resume, stop/kill и callback после завершения.

Задача не реализует полную tweener hierarchy, subtween, await, physics process mode, pause modes scene-tree уровня, time scale, infinite loops и editor tooling. Эти части остаются отдельными задачами после базового runtime-среза.

## Публичный API

### Создание

- `SceneTree.CreateTween()` создаёт valid `Tween`, регистрирует его в tree processing и возвращает running instance.
- `Node.CreateTween()` создаёт tween через текущий `SceneTree` и вызывает `BindNode(this)`.
- `new Tween()` создаёт invalid instance. Такой instance можно проверять через `IsValid()`, но нельзя пополнять tweeners.

### Tween

- `Tween` наследуется от `RefCounted`.
- Конструктор регистрирует signals `finished` и `step_finished`.
- `TransitionType` содержит values `Linear`, `Sine`, `Quint`, `Quart`, `Quad`, `Expo`, `Elastic`, `Cubic`, `Circ`, `Bounce`, `Back`, `Spring`.
- `EaseType` содержит values `In`, `Out`, `InOut`, `OutIn`.
- `BindNode(Node node)` связывает tween с node. Если bound node освобождён или вышел из tree, runtime processing прекращает движение до восстановления валидного node.
- `TweenProperty(Object obj, NodePath property, Variant finalVal, double duration)` добавляет property tweener.
- `TweenCallback(Callable callback)` добавляет callback tweener.
- `TweenInterval(double time)` добавляет interval tweener.
- `SetTrans(TransitionType trans)` и `SetEase(EaseType ease)` задают defaults для следующих property tweeners.
- `Pause()` останавливает automatic processing без сброса state.
- `Play()` возобновляет paused/stopped tween.
- `Stop()` останавливает tween, сбрасывает elapsed time и возвращает sequence к первому step без invalidation.
- `Kill()` отменяет tween, очищает tweeners и делает instance invalid. `finished` при этом не испускается.
- `CustomStep(double delta)` вручную продвигает tween даже когда он paused и возвращает `true`, если tween всё ещё valid и не завершён.
- `IsRunning()` возвращает `true`, если tween valid, не paused/stopped и ещё содержит unfinished tweeners.
- `IsValid()` возвращает `true`, пока tween зарегистрирован в scene tree и не завершён/не killed.
- `HasTweeners()` возвращает `true`, если у valid tween есть хотя бы один tweener.
- `GetTotalElapsedTime()` возвращает accumulated time с учётом `SetSpeedScale()`.
- `SetSpeedScale(double speed)` задаёт finite non-negative speed multiplier.
- `InterpolateValue(Variant initialValue, Variant deltaValue, double elapsedTime, double duration, TransitionType transType, EaseType easeType)` вычисляет eased interpolation для поддержанных Variant values.

### Tweener classes

- `Tweener` наследуется от `RefCounted` и является public base type для результата tween methods.
- `PropertyTweener` поддерживает `SetDelay(double delay)`, `SetTrans(Tween.TransitionType trans)` и `SetEase(Tween.EaseType ease)`.
- `CallbackTweener` поддерживает `SetDelay(double delay)`.
- `IntervalTweener` представляет delay step без дополнительных public методов.

## Property behavior

- Property path использует `NodePath` subnames:
  - `Position` задаёт целое property.
  - `Position:X` задаёт вложенное property.
- Property lookup ищет public instance property или field сначала точным ordinal name, затем ordinal ignore-case fallback.
- Initial value property tweener берёт при первом фактическом старте tweener, после delay; `Stop()` сбрасывает elapsed time, но не заставляет tweener заново захватывать initial value.
- Duration `0` немедленно задаёт final value после delay.
- Unsupported value type, missing property, missing object или invalid conversion отменяют только конкретный tweener step; tree traversal продолжается.

## Processing behavior

- Tween automatic processing выполняется после node `_Process()` callbacks и до draw traversal.
- Tweeners выполняются последовательно.
- `TweenInterval(time)` занимает указанное время и не меняет target objects.
- `TweenCallback(callback)` вызывает callback один раз, когда до него дошла sequence.
- После последнего tweener `Tween` испускает `finished` и становится invalid.
- `step_finished` испускается после каждого completed tweener step с zero-based step index.
- `Pause()` сохраняет current step и elapsed time.
- `Play()` продолжает paused/stopped tween с текущей позиции.
- `Stop()` сбрасывает sequence в начало; target property values уже записанные до stop не откатываются.
- `Kill()` отменяет tween без callbacks и без `finished`.

## Easing behavior

- `TransitionType.Linear` возвращает линейный вес.
- `TransitionType.Quad`, `Cubic`, `Quart`, `Quint`, `Sine`, `Expo`, `Circ`, `Back`, `Bounce`, `Elastic` и `Spring` дают deterministic normalized curves.
- `EaseType.In`, `Out`, `InOut`, `OutIn` применяют transition curve к началу, концу или обеим половинам interpolation.
- `InterpolateValue()` поддерживает numeric Variant values, `Vector2` и `Color`. Unsupported Variant types возвращают initial value до конца duration и final value на completion.

## Validation

- `SceneTree.CreateTween()` всегда возвращает valid tween.
- `Node.CreateTween()` требует, чтобы node находился внутри `SceneTree`.
- `TweenProperty()`, `TweenCallback()` и `TweenInterval()` требуют valid tween.
- Duration, delay, speed и step delta должны быть finite; duration, delay и step delta не могут быть отрицательными; speed не может быть отрицательным.
- Easing enum values outside supported range produce `ArgumentOutOfRangeException`.

## Acceptance Criteria

- `SceneTree.CreateTween()` и `Node.CreateTween()` создают valid tween.
- Property tween интерполирует values через `SceneTree.ProcessFrame()` и применяет easing.
- `Pause()`, `Play()`, `Stop()` и `CustomStep()` дают deterministic timing.
- `TweenCallback()` и `finished` signal вызываются после завершения sequence.
- `Kill()` отменяет tween без completion callbacks.
- Public API новых типов имеет полную XML documentation на каждом public member.
- Integration tests покрывают property interpolation, easing, pause/resume, manual step, completion callback и cancellation.
- Implementation documentation добавлена в `docs/animation/`.

## Фактическое состояние, ограничения и проверки

## Текущее состояние

`0.1-preview` теперь поддерживает runtime `Tween` для коротких script-driven animations:

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
