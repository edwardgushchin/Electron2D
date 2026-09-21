# MethodTweener\<TValue\>

Last updated: 2026-09-21

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class MethodTweener<TValue> : Tweener`

> Interpolates a typed value and supplies it to a callback over time.

## Description

Interpolates a typed value and supplies it to a callback over time.

`MethodTweener<TValue>` is owned and created only by `Tween.TweenMethod<TValue>`. It interpolates from one typed value to another and invokes `Action<TValue>` on every active frame, including the exact final value. Its declared public API is `SetDelay(double)`, `SetEase(Tween.EaseType)`, and `SetTrans(Tween.TransitionType)`; each returns this tweener. `Finished` is inherited from [`Tweener`](Tweener.md).

Delay and duration use finite non-negative seconds. Transition/ease defaults are captured when appended. Built-in interpolation supports the values listed by the [Tweening component](../components/tweening.md), and the append method accepts an explicit interpolator for other types. If the delegate's direct target is an `ElectronObject` and becomes disposed, the tweener completes without another call. Delegate, interpolation, completion-event, and arithmetic failures invalidate the parent after parallel siblings are attempted.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
MethodTweener<float> fade = tween.TweenMethod<float>(SetOpacity, 0f, 1f, 0.5);
```

## Methods

| Member | Description |
| --- | --- |
| [`public MethodTweener<TValue> SetDelay(double delay)`](#m-electron2d-methodtweener-1-setdelay-system-double) | Sets the delay before callback interpolation begins. |
| [`public MethodTweener<TValue> SetEase(Tween.EaseType ease)`](#m-electron2d-methodtweener-1-setease-electron2d-tween-easetype) | Overrides the owning tween's easing for this callback. |
| [`public MethodTweener<TValue> SetTrans(Tween.TransitionType transition)`](#m-electron2d-methodtweener-1-settrans-electron2d-tween-transitiontype) | Overrides the owning tween's transition for this callback. |

## Method Descriptions

<a id="m-electron2d-methodtweener-1-setdelay-system-double"></a>
### `public MethodTweener<TValue> SetDelay(double delay)`

Sets the delay before callback interpolation begins.

**Parameters**

- `delay`: Finite non-negative seconds.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `delay` is negative, NaN, or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-methodtweener-1-setease-electron2d-tween-easetype"></a>
### `public MethodTweener<TValue> SetEase(Tween.EaseType ease)`

Overrides the owning tween's easing for this callback.

**Parameters**

- `ease`: The easing direction.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `ease` is undefined.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-methodtweener-1-settrans-electron2d-tween-transitiontype"></a>
### `public MethodTweener<TValue> SetTrans(Tween.TransitionType transition)`

Overrides the owning tween's transition for this callback.

**Parameters**

- `transition`: The transition curve.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `transition` is undefined.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
