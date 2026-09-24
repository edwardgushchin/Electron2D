# Tween

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class Tween : ElectronObject`

> Sequences typed property interpolation, method interpolation, callbacks, waits, and nested tweens.

## Description

Sequences typed property interpolation, method interpolation, callbacks, waits, and nested tweens.

`Tween` owns an ordered list of parallel step groups and coordinates interpolation, callbacks, event waits, nested tweens, looping, and completion. It is constructed only by `SceneTree.CreateTween()` or `Node.CreateTween()`. The SceneTree processes valid top-level tweens; a parent Tween processes a detached subtween. The object remains inspectable after invalidation until explicitly disposed, while its managed memory remains runtime-owned.

`SetParallel` changes the default grouping of later appends; `Parallel` joins only the next append, and `Chain` starts its next append in a new step. Transition and ease defaults are captured when each property or method tweener is appended. A zero or negative speed scale leaves the tween running without advancing its tweeners; negative speed reduces accumulated elapsed time. Bound pause mode follows the bound node's effective process policy; a detached bound node waits until it enters the owning tree.

A tween is created by [`SceneTree.CreateTween`](SceneTree.md#m-electron2d-scenetree-createtween) or [`Node.CreateTween`](Node.md#m-electron2d-node-createtween) and is processed by
that tree after node callbacks and lightweight timers in the selected frame lane. Tweeners are sequential unless
[`Tween.Parallel`](Tween.md#m-electron2d-tween-parallel) or [`Tween.SetParallel(Boolean)`](Tween.md#m-electron2d-tween-setparallel-system-boolean) groups them. A completed or killed tween is invalid and cannot
accept new tweeners. Tween mutation and processing use the creating tree's owner thread.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
Tween tween = node.CreateTween();
tween.TweenProperty(node, target => target.Position, (target, value) => target.Position = value, new Vector2(200f, 80f), 0.5);
```

## Methods

| Member | Description |
| --- | --- |
| [`public Tween BindNode(Node node)`](#m-electron2d-tween-bindnode-electron2d-node) | Binds processing and lifetime to a node. |
| [`public Tween Chain()`](#m-electron2d-tween-chain) | Makes the next appended tweener begin a new sequential step. |
| [`public bool CustomStep(double delta)`](#m-electron2d-tween-customstep-system-double) | Advances the tween manually by an elapsed duration. |
| [`public int GetLoopsLeft()`](#m-electron2d-tween-getloopsleft) | Gets the number of remaining sequence executions. |
| [`public double GetTotalElapsedTime()`](#m-electron2d-tween-gettotalelapsedtime) | Gets accumulated scaled processing time. |
| [`public bool HasTweeners()`](#m-electron2d-tween-hastweeners) | Gets whether this tween contains at least one tweener. |
| [`public static TValue InterpolateValue<TValue>(TValue initialValue, TValue deltaValue, double elapsedTime, double duration, Tween.TransitionType transition, Tween.EaseType ease)`](#m-electron2d-tween-interpolatevalue-1-0-0-system-double-system-double-electron2d-tween-transitiontype-electron2d-tween-easetype) | Interpolates a typed value from an initial value by a delta. |
| [`public bool IsRunning()`](#m-electron2d-tween-isrunning) | Gets whether the tween is currently playing. |
| [`public bool IsValid()`](#m-electron2d-tween-isvalid) | Gets whether this tween remains registered for scene-tree processing. |
| [`public void Kill()`](#m-electron2d-tween-kill) | Aborts all tweening operations and invalidates this tween. |
| [`public Tween Parallel()`](#m-electron2d-tween-parallel) | Makes only the next appended tweener join the preceding step. |
| [`public void Pause()`](#m-electron2d-tween-pause) | Pauses progression without resetting current tweener state. |
| [`public void Play()`](#m-electron2d-tween-play) | Resumes a paused or stopped tween. |
| [`public Tween SetEase(Tween.EaseType ease)`](#m-electron2d-tween-setease-electron2d-tween-easetype) | Sets the default easing for property and method tweeners appended afterward. |
| [`public Tween SetIgnoreTimeScale(bool ignore = true)`](#m-electron2d-tween-setignoretimescale-system-boolean) | Sets whether Engine time scaling is bypassed. |
| [`public Tween SetLoops(int loops = 0)`](#m-electron2d-tween-setloops-system-int32) | Sets how many times the complete sequence runs. |
| [`public Tween SetParallel(bool parallel = true)`](#m-electron2d-tween-setparallel-system-boolean) | Sets whether subsequently appended tweeners are parallel by default. |
| [`public Tween SetPauseMode(Tween.TweenPauseMode mode)`](#m-electron2d-tween-setpausemode-electron2d-tween-tweenpausemode) | Sets the behavior while the owning tree is paused. |
| [`public Tween SetProcessMode(Tween.TweenProcessMode mode)`](#m-electron2d-tween-setprocessmode-electron2d-tween-tweenprocessmode) | Sets the frame lane that advances this tween. |
| [`public Tween SetSpeedScale(double speed)`](#m-electron2d-tween-setspeedscale-system-double) | Sets a multiplier applied to time delivered to every tweener and delay. |
| [`public Tween SetTrans(Tween.TransitionType transition)`](#m-electron2d-tween-settrans-electron2d-tween-transitiontype) | Sets the default transition for property and method tweeners appended afterward. |
| [`public void Stop()`](#m-electron2d-tween-stop) | Stops progression and resets the sequence cursor and elapsed time. |
| [`public CallbackTweener TweenCallback(Action callback)`](#m-electron2d-tween-tweencallback-system-action) | Appends a callback that runs once after an optional delay. |
| [`public IntervalTweener TweenInterval(double time)`](#m-electron2d-tween-tweeninterval-system-double) | Appends a duration that changes no value. |
| [`public MethodTweener<TValue> TweenMethod<TValue>(Action<TValue> method, TValue from, TValue to, double duration, Func<TValue, TValue, double, TValue> interpolator = null)`](#m-electron2d-tween-tweenmethod-1-system-action-0-0-0-system-double-system-func-0-0-system-double-0) | Appends typed interpolation delivered to a callback. |
| [`public PropertyTweener<TValue> TweenProperty<TTarget, TValue>(TTarget target, Func<TTarget, TValue> getter, Action<TTarget, TValue> setter, TValue finalValue, double duration, Func<TValue, TValue, double, TValue> interpolator = null)`](#m-electron2d-tween-tweenproperty-2-0-system-func-0-1-system-action-0-1-1-system-double-system-func-1-1-system-double-1) | Appends typed interpolation of an engine object's property through explicit accessors. |
| [`public SubtweenTweener TweenSubtween(Tween subtween)`](#m-electron2d-tween-tweensubtween-electron2d-tween) | Appends a child tween as one step and removes it from independent tree processing. |
| [`public AwaitTweener TweenAwait(ElectronObject source, Action<Action> subscribe, Action<Action> unsubscribe)`](#m-electron2d-tween-tweenawait-electron2d-electronobject-system-action-system-action-system-action-system-action) | Appends a wait for a parameterless typed C# event. |
| [`public AwaitTweener TweenAwait<T>(ElectronObject source, Action<Action<T>> subscribe, Action<Action<T>> unsubscribe)`](#m-electron2d-tween-tweenawait-1-electron2d-electronobject-system-action-system-action-0-system-action-system-action-0) | Appends a wait for a one-argument typed C# event. |
| [`public AwaitTweener TweenAwait<T1, T2>(ElectronObject source, Action<Action<T1, T2>> subscribe, Action<Action<T1, T2>> unsubscribe)`](#m-electron2d-tween-tweenawait-2-electron2d-electronobject-system-action-system-action-0-1-system-action-system-action-0-1) | Appends a wait for a two-argument typed C# event. |
| [`protected override void ValidateDisposal()`](#m-electron2d-tween-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-tween-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<Tween> Finished`](#e-electron2d-tween-finished) | Occurs after every tweener in the final loop finishes. |
| [`public event Action<Tween, int> LoopFinished`](#e-electron2d-tween-loopfinished) | Occurs after a non-final loop completes. |
| [`public event Action<Tween, int> StepFinished`](#e-electron2d-tween-stepfinished) | Occurs when one sequential step or parallel step group completes. |

## Enumerations

| Member | Description |
| --- | --- |
| [`public enum Tween.TweenProcessMode`](#t-electron2d-tween-tweenprocessmode) | Selects the frame lane that advances a tween. |
| [`public enum Tween.TweenPauseMode`](#t-electron2d-tween-tweenpausemode) | Selects how tree pause affects a tween. |
| [`public enum Tween.TransitionType`](#t-electron2d-tween-transitiontype) | Selects the interpolation curve family. |
| [`public enum Tween.EaseType`](#t-electron2d-tween-easetype) | Selects where acceleration and deceleration occur within a transition. |

## Method Descriptions

<a id="m-electron2d-tween-bindnode-electron2d-node"></a>
### `public Tween BindNode(Node node)`

Binds processing and lifetime to a node.

**Parameters**

- `node`: The live node whose tree membership, process policy, and lifetime control this tween.

**Returns:** This tween.

**Exceptions**

- `ArgumentNullException`: `node` is `null`.
- `ArgumentException`: `node` is currently owned by another scene tree.
- `ObjectDisposedException`: The tween or node is disposing or disposed.
- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.

**Remarks:** A detached bound node halts the tween. Disposing the node kills and invalidates the tween.

<a id="m-electron2d-tween-chain"></a>
### `public Tween Chain()`

Makes the next appended tweener begin a new sequential step.

**Returns:** This tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

**Remarks:** This is primarily used after [`Tween.SetParallel(Boolean)`](Tween.md#m-electron2d-tween-setparallel-system-boolean) enables parallel appending.

<a id="m-electron2d-tween-customstep-system-double"></a>
### `public bool CustomStep(double delta)`

Advances the tween manually by an elapsed duration.

**Parameters**

- `delta`: Finite non-negative elapsed seconds before speed scaling.

**Returns:** `true` while unfinished; otherwise `false`.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is negative, NaN, or infinite.
- `InvalidOperationException`: The call is off the owner thread, re-enters processing, or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.
- `AggregateException`: One or more parallel callbacks or completion subscribers fail.

**Remarks:** This advances a paused tween but still honors a bound node being detached or disposed.

<a id="m-electron2d-tween-getloopsleft"></a>
### `public int GetLoopsLeft()`

Gets the number of remaining sequence executions.

**Returns:** `-1` for an infinite tween, zero after completion, or the positive remaining count.

**Exceptions**

- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-gettotalelapsedtime"></a>
### `public double GetTotalElapsedTime()`

Gets accumulated scaled processing time.

**Returns:** Seconds accumulated while the tween was actively advanced, including final-frame overshoot.

**Exceptions**

- `ObjectDisposedException`: The tween is disposing or disposed.

**Remarks:** [`Tween.Stop`](Tween.md#m-electron2d-tween-stop) resets this value.

<a id="m-electron2d-tween-hastweeners"></a>
### `public bool HasTweeners()`

Gets whether this tween contains at least one tweener.

**Returns:** `true` when at least one tweener was appended, including after invalidation.

**Exceptions**

- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-interpolatevalue-1-0-0-system-double-system-double-electron2d-tween-transitiontype-electron2d-tween-easetype"></a>
### `public static TValue InterpolateValue<TValue>(TValue initialValue, TValue deltaValue, double elapsedTime, double duration, Tween.TransitionType transition, Tween.EaseType ease)`

Interpolates a typed value from an initial value by a delta.

**Type parameters**

- `TValue`: A supported numeric or Electron2D math value type.

**Parameters**

- `initialValue`: The starting value.
- `deltaValue`: The change from the start to the final value.
- `elapsedTime`: Finite elapsed seconds; values outside the duration extrapolate.
- `duration`: Finite non-negative total duration.
- `transition`: The transition curve.
- `ease`: The easing direction.

**Returns:** The interpolated value; a zero duration always returns the final value.

**Exceptions**

- `ArgumentOutOfRangeException`: A time is non-finite, duration is negative, or an enum value is undefined.
- `NotSupportedException`: `TValue` has no built-in interpolation and addition contract.
- `OverflowException`: An integer result is outside its destination type.

<a id="m-electron2d-tween-isrunning"></a>
### `public bool IsRunning()`

Gets whether the tween is currently playing.

**Returns:** `true` while running, including before its first frame.

**Exceptions**

- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-isvalid"></a>
### `public bool IsValid()`

Gets whether this tween remains registered for scene-tree processing.

**Returns:** `true` before completion or killing; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-kill"></a>
### `public void Kill()`

Aborts all tweening operations and invalidates this tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tween is disposing or disposed.

**Remarks:** No completion events are raised. Nested tweens are killed as well.

<a id="m-electron2d-tween-parallel"></a>
### `public Tween Parallel()`

Makes only the next appended tweener join the preceding step.

**Returns:** This tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread, processing has started, or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-pause"></a>
### `public void Pause()`

Pauses progression without resetting current tweener state.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-play"></a>
### `public void Play()`

Resumes a paused or stopped tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread, the tween is invalid, or it already completed without being stopped first.
- `ObjectDisposedException`: The tween is disposing or disposed.

**Remarks:** A stopped tween restarts from step zero and property tweeners recapture their configured starting state.

<a id="m-electron2d-tween-setease-electron2d-tween-easetype"></a>
### `public Tween SetEase(Tween.EaseType ease)`

Sets the default easing for property and method tweeners appended afterward.

**Parameters**

- `ease`: The default easing direction.

**Returns:** This tween.

**Exceptions**

- `ArgumentOutOfRangeException`: `ease` is undefined.
- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-setignoretimescale-system-boolean"></a>
### `public Tween SetIgnoreTimeScale(bool ignore = true)`

Sets whether Engine time scaling is bypassed.

**Parameters**

- `ignore`: Whether to use the original host-supplied delta.

**Returns:** This tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-setloops-system-int32"></a>
### `public Tween SetLoops(int loops = 0)`

Sets how many times the complete sequence runs.

**Parameters**

- `loops`: A non-positive count for infinite repetition, or a positive total execution count.

**Returns:** This tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-setparallel-system-boolean"></a>
### `public Tween SetParallel(bool parallel = true)`

Sets whether subsequently appended tweeners are parallel by default.

**Parameters**

- `parallel`: Whether appends join the preceding step.

**Returns:** This tween.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread, processing has started, or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

**Remarks:** The tweener immediately preceding this call joins the same parallel step as the next append.

<a id="m-electron2d-tween-setpausemode-electron2d-tween-tweenpausemode"></a>
### `public Tween SetPauseMode(Tween.TweenPauseMode mode)`

Sets the behavior while the owning tree is paused.

**Parameters**

- `mode`: The pause policy.

**Returns:** This tween.

**Exceptions**

- `ArgumentOutOfRangeException`: `mode` is undefined.
- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-setprocessmode-electron2d-tween-tweenprocessmode"></a>
### `public Tween SetProcessMode(Tween.TweenProcessMode mode)`

Sets the frame lane that advances this tween.

**Parameters**

- `mode`: The process or physics lane.

**Returns:** This tween.

**Exceptions**

- `ArgumentOutOfRangeException`: `mode` is undefined.
- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-setspeedscale-system-double"></a>
### `public Tween SetSpeedScale(double speed)`

Sets a multiplier applied to time delivered to every tweener and delay.

**Parameters**

- `speed`: A finite multiplier. Zero or negative values do not advance tweeners; negative values still reduce accumulated elapsed time.

**Returns:** This tween.

**Exceptions**

- `ArgumentOutOfRangeException`: `speed` is NaN or infinite.
- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-settrans-electron2d-tween-transitiontype"></a>
### `public Tween SetTrans(Tween.TransitionType transition)`

Sets the default transition for property and method tweeners appended afterward.

**Parameters**

- `transition`: The default curve family.

**Returns:** This tween.

**Exceptions**

- `ArgumentOutOfRangeException`: `transition` is undefined.
- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-stop"></a>
### `public void Stop()`

Stops progression and resets the sequence cursor and elapsed time.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

**Remarks:** Appended tweeners remain. Animated targets are not restored. Call [`Tween.Play`](Tween.md#m-electron2d-tween-play) to restart.

<a id="m-electron2d-tween-tweencallback-system-action"></a>
### `public CallbackTweener TweenCallback(Action callback)`

Appends a callback that runs once after an optional delay.

**Parameters**

- `callback`: The callback to invoke synchronously on the owner thread.

**Returns:** The appended tweener.

**Exceptions**

- `ArgumentNullException`: `callback` is `null`.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-tweeninterval-system-double"></a>
### `public IntervalTweener TweenInterval(double time)`

Appends a duration that changes no value.

**Parameters**

- `time`: Finite non-negative seconds.

**Returns:** The appended tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `time` is negative, NaN, or infinite.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-tweenmethod-1-system-action-0-0-0-system-double-system-func-0-0-system-double-0"></a>
### `public MethodTweener<TValue> TweenMethod<TValue>(Action<TValue> method, TValue from, TValue to, double duration, Func<TValue, TValue, double, TValue> interpolator = null)`

Appends typed interpolation delivered to a callback.

**Type parameters**

- `TValue`: The value type.

**Parameters**

- `method`: Receives the current value each active frame and the exact final value at completion.
- `from`: The starting value.
- `to`: The final value.
- `duration`: Finite non-negative interpolation seconds.
- `interpolator`: Optional typed interpolation. Omit it for a supported built-in value type.

**Returns:** The appended typed method tweener.

**Exceptions**

- `ArgumentNullException`: `method` is `null`.
- `ArgumentOutOfRangeException`: `duration` is negative, NaN, or infinite.
- `NotSupportedException`: No interpolator is supplied for an unsupported type.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `ObjectDisposedException`: The tween is disposing or disposed.

<a id="m-electron2d-tween-tweenproperty-2-0-system-func-0-1-system-action-0-1-1-system-double-system-func-1-1-system-double-1"></a>
### `public PropertyTweener<TValue> TweenProperty<TTarget, TValue>(TTarget target, Func<TTarget, TValue> getter, Action<TTarget, TValue> setter, TValue finalValue, double duration, Func<TValue, TValue, double, TValue> interpolator = null)`

Appends typed interpolation of an engine object's property through explicit accessors.

**Type parameters**

- `TTarget`: The engine-object type containing the property.
- `TValue`: The property value type.

**Parameters**

- `target`: The live target object.
- `getter`: Reads the property from the target.
- `setter`: Writes the property on the target.
- `finalValue`: The final absolute value, or relative delta after [`PropertyTweener`1.AsRelative`](PropertyTweener.Generic.md#m-electron2d-propertytweener-1-asrelative).
- `duration`: Finite non-negative interpolation seconds.
- `interpolator`: Optional typed interpolation. Omit it for a supported built-in value type.

**Returns:** The appended typed property tweener.

**Exceptions**

- `ArgumentNullException`: A required argument is `null`.
- `ArgumentOutOfRangeException`: `duration` is negative, NaN, or infinite.
- `NotSupportedException`: No interpolator is supplied for an unsupported type.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `ObjectDisposedException`: The tween or target is disposing or disposed.
- `Exception`: The supplied getter throws.

**Remarks:** The getter is called immediately to capture the value used by [`PropertyTweener`1.FromCurrent`](PropertyTweener.Generic.md#m-electron2d-propertytweener-1-fromcurrent).

<a id="m-electron2d-tween-tweensubtween-electron2d-tween"></a>
### `public SubtweenTweener TweenSubtween(Tween subtween)`

Appends a child tween as one step and removes it from independent tree processing.

**Parameters**

- `subtween`: A valid tween owned by the same scene tree.

**Returns:** The appended nested-tween step.

**Exceptions**

- `ArgumentNullException`: `subtween` is `null`.
- `ArgumentException`: The tween is nested into itself, already nested, or would create a cycle.
- `InvalidOperationException`: The call is off the owner thread, parent processing has started, a tween is invalid or currently processing, or the tweens belong to different trees.
- `ObjectDisposedException`: Either tween is disposing or disposed.

<a id="m-electron2d-tween-tweenawait-electron2d-electronobject-system-action-system-action-system-action-system-action"></a>
### `public AwaitTweener TweenAwait(ElectronObject source, Action<Action> subscribe, Action<Action> unsubscribe)`

Appends a wait for a parameterless typed C# event.

**Parameters**

- `source`: The event publisher whose disposal also completes the wait.
- `subscribe`: Adds the supplied handler to the event.
- `unsubscribe`: Removes the supplied handler from the event.

**Returns:** The appended event-wait tweener.

**Exceptions**

- `ArgumentNullException`: A required argument is `null`.
- `ObjectDisposedException`: The tween or source is disposing or disposed.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `Exception`: A supplied event accessor throws.

<a id="m-electron2d-tween-tweenawait-1-electron2d-electronobject-system-action-system-action-0-system-action-system-action-0"></a>
### `public AwaitTweener TweenAwait<T>(ElectronObject source, Action<Action<T>> subscribe, Action<Action<T>> unsubscribe)`

Appends a wait for a one-argument typed C# event.

**Type parameters**

- `T`: The event argument type.

**Parameters**

- `source`: The event publisher whose disposal also completes the wait.
- `subscribe`: Adds the supplied handler to the event.
- `unsubscribe`: Removes the supplied handler from the event.

**Returns:** The appended event-wait tweener.

**Exceptions**

- `ArgumentNullException`: A required argument is `null`.
- `ObjectDisposedException`: The tween or source is disposing or disposed.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `Exception`: A supplied event accessor throws.

<a id="m-electron2d-tween-tweenawait-2-electron2d-electronobject-system-action-system-action-0-1-system-action-system-action-0-1"></a>
### `public AwaitTweener TweenAwait<T1, T2>(ElectronObject source, Action<Action<T1, T2>> subscribe, Action<Action<T1, T2>> unsubscribe)`

Appends a wait for a two-argument typed C# event.

**Type parameters**

- `T1`: The first event argument type.
- `T2`: The second event argument type.

**Parameters**

- `source`: The event publisher whose disposal also completes the wait.
- `subscribe`: Adds the supplied handler to the event.
- `unsubscribe`: Removes the supplied handler from the event.

**Returns:** The appended event-wait tweener.

**Exceptions**

- `ArgumentNullException`: A required argument is `null`.
- `ObjectDisposedException`: The tween or source is disposing or disposed.
- `InvalidOperationException`: The call is off the owner thread, processing started, or the tween is invalid.
- `Exception`: A supplied event accessor throws.

<a id="m-electron2d-tween-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread or occurs inside a tween callback.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

Rejects disposal during processing and requires the original scene-tree owner thread.

<a id="m-electron2d-tween-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Invalidates nested tweens, disposes owned tweener objects, clears subscribers, and calls the base implementation. Nested tween objects remain managed and inspectable until separately disposed.

## Event Descriptions

<a id="e-electron2d-tween-finished"></a>
### `public event Action<Tween> Finished`

Occurs after every tweener in the final loop finishes.

**Remarks:** The tween is stopped but remains valid during synchronous delivery; the tree invalidates it afterward. Killed tweens do not raise this event.

<a id="e-electron2d-tween-loopfinished"></a>
### `public event Action<Tween, int> LoopFinished`

Occurs after a non-final loop completes.

**Remarks:** The first argument is this tween and the second is the one-based completed-loop count.

<a id="e-electron2d-tween-stepfinished"></a>
### `public event Action<Tween, int> StepFinished`

Occurs when one sequential step or parallel step group completes.

**Remarks:** The first argument is this tween and the second is the zero-based step index.

## Enumeration Descriptions

<a id="t-electron2d-tween-tweenprocessmode"></a>
### `public enum Tween.TweenProcessMode`

Selects the frame lane that advances a tween.

<a id="t-electron2d-tween-tweenpausemode"></a>
### `public enum Tween.TweenPauseMode`

Selects how tree pause affects a tween.

<a id="t-electron2d-tween-transitiontype"></a>
### `public enum Tween.TransitionType`

Selects the interpolation curve family.

<a id="t-electron2d-tween-easetype"></a>
### `public enum Tween.EaseType`

Selects where acceleration and deceleration occur within a transition.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle, invariants, errors, and threading

An empty tween is valid on creation but fails and invalidates if any matching frame reaches it without appended tweeners, including a zero-delta frame. First processing captures property start values and freezes the append surface. `Stop()` reopens appending and resets cursor/time without changing targets; `Play()` restarts from the resulting state. Exact step completion with no remaining delta defers the next zero-duration step to a later positive-delta frame. Normal final delivery occurs before SceneTree invalidation. Kill, bound-node disposal, tree finalization, an empty sequence, and user callback failure do not raise `Finished`.

Every parallel tweener is attempted before failures are reported. Any processing failure invalidates the complete tween, cancels event subscriptions/nested work, unregisters the tween even when failure came from `CustomStep()`, and reaches SceneTree as an aggregate during frame processing while later tweens, deferred work, and deletion still run. Infinite zero-duration loops fail instead of hanging, even if callbacks mutate speed. Captured SceneTree entries revalidate lane and nested ownership before execution. Invalid durations, speed, loop counts, enum values, cross-tree binding/nesting, cyclic/multiple/in-progress nesting, unsupported built-in value types, recursive stepping, append-after-start, and off-owner-thread mutation fail explicitly.

All public mutation, processing, and disposal are owner-thread operations. Typed event notification may arrive on another thread; `AwaitTweener` only atomically records it. Reads are not a synchronization contract. Disposal is rejected during processing, invalidates nested tweens, disposes owned tweener objects, clears subscribers, and aggregates cleanup failures. A nested Tween object remains managed and inspectable until separately disposed.

## Dependencies, verification, and limitations

Depends on [`SceneTree`](SceneTree.md), [`Node`](Node.md), [`Tweener`](Tweener.md), Core math types, and `EventConnection`. The executable tests cover the behavior listed in the [Tweening component](../components/tweening.md). No string property path, dynamic value, reflection callable, independent clock/thread, editor serialization, or visual acceptance exists.
