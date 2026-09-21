# CallbackTweener

Last updated: 2026-09-21

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class CallbackTweener : Tweener`

> Invokes a parameterless callback after an optional delay.

## Description

Invokes a parameterless callback after an optional delay.

`CallbackTweener` is created only by `Tween.TweenCallback(Action)` and invokes its callback once after its step begins and the optional delay expires. Its complete declared public API is `CallbackTweener SetDelay(double delay)`; `Finished` and object lifetime API are inherited from [`Tweener`](Tweener.md). Delay is finite, non-negative, speed-scaled, and zero by default. Any overshoot remains available to later sequential steps.

If the delegate's direct target is a disposed `ElectronObject`, the task finishes without invocation. The callback runs synchronously on the parent owner thread. A callback or completion-event exception leaves the task/parent coherent, allows parallel siblings and later SceneTree phases to run, then invalidates the parent and is aggregated. Explicit disposal cancels the task and clears subscribers.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
CallbackTweener callback = tween.TweenCallback(() => Console.WriteLine("Done"));
callback.SetDelay(0.25);
```

## Methods

| Member | Description |
| --- | --- |
| [`public CallbackTweener SetDelay(double delay)`](#m-electron2d-callbacktweener-setdelay-system-double) | Sets the delay before callback invocation. |

## Method Descriptions

<a id="m-electron2d-callbacktweener-setdelay-system-double"></a>
### `public CallbackTweener SetDelay(double delay)`

Sets the delay before callback invocation.

**Parameters**

- `delay`: Finite non-negative seconds.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `delay` is negative, NaN, or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
