# PropertyTweener\<TValue\>

Last updated: 2026-09-24

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PropertyTweener<TValue> : Tweener`

> Interpolates a typed property through explicit getter and setter delegates.

## Description

Interpolates a typed property through explicit getter and setter delegates.

Created only by `Tween.TweenProperty<TTarget,TValue>`, this owned task reads and writes one property through explicit typed delegates. Its declared public API is `From(TValue)`, `FromCurrent()`, `AsRelative()`, `SetCustomInterpolator(Func<double,double>)`, `SetDelay(double)`, `SetEase(Tween.EaseType)`, and `SetTrans(Tween.TransitionType)`; all return this tweener for fluent configuration. `Finished` and inherited object API come from [`Tweener`](Tweener.md).

Default execution captures the property when the step starts if the delay magnitude is below `0.00001` second, or when the delay ends otherwise. `From` sets an explicit start for each loop; a live `From` shifts intermediate built-in interpolation when its displacement is representable, while retaining the active step's fixed final write. A full-span integer displacement falls back to endpoint interpolation. `FromCurrent` disables continuation and retains the configured start, initially the append-time value; a preceding `From` value remains. `AsRelative` treats the configured final value as a delta and requires a supported addition operation. With delayed continuation, its final value is resolved from the append-time base at step start before the delay-end start recapture. Transition/ease are copied from the parent when appended and can be changed during playback. A custom interpolator receives the already-eased weight and may return overshoot, including on the final write.

The target is held while the parent exists, but logical target disposal completes the tweener without writing. Getter, setter, interpolator, arithmetic, and event exceptions invalidate the parent after parallel siblings are attempted. Duration and delay accept finite signed seconds; negative values finish or begin on the first positive step. Non-finite times and undefined enums are rejected. Calls use the parent owner thread. Built-in supported values and threading are documented in the [Tweening component](../components/tweening.md).

Built-in Int64 interpolation uses wide intermediate arithmetic and checked rounding, retaining valid near-limit property values without a false floating-point overflow.

The tweener completes without writing when its target has been disposed.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
PropertyTweener<Vector2> move = tween.TweenProperty(node, target => target.Position, (target, value) => target.Position = value, new Vector2(200f, 80f), 0.5);
```

## Methods

| Member | Description |
| --- | --- |
| [`public PropertyTweener<TValue> From(TValue value)`](#m-electron2d-propertytweener-1-from-0) | Uses the supplied value as the start of every sequence execution. |
| [`public PropertyTweener<TValue> FromCurrent()`](#m-electron2d-propertytweener-1-fromcurrent) | Uses the property value captured when this tweener was appended as its starting value. |
| [`public PropertyTweener<TValue> AsRelative()`](#m-electron2d-propertytweener-1-asrelative) | Interprets the configured final value as a delta from the captured start. |
| [`public PropertyTweener<TValue> SetCustomInterpolator(Func<double, double> interpolator)`](#m-electron2d-propertytweener-1-setcustominterpolator-system-func-system-double-system-double) | Sets a custom mapping applied after the configured transition and easing. |
| [`public PropertyTweener<TValue> SetDelay(double delay)`](#m-electron2d-propertytweener-1-setdelay-system-double) | Sets the delay before property interpolation begins. |
| [`public PropertyTweener<TValue> SetEase(Tween.EaseType ease)`](#m-electron2d-propertytweener-1-setease-electron2d-tween-easetype) | Overrides the owning tween's easing for this property. |
| [`public PropertyTweener<TValue> SetTrans(Tween.TransitionType transition)`](#m-electron2d-propertytweener-1-settrans-electron2d-tween-transitiontype) | Overrides the owning tween's transition for this property. |

## Method Descriptions

<a id="m-electron2d-propertytweener-1-from-0"></a>
### `public PropertyTweener<TValue> From(TValue value)`

Uses the supplied value as the start of every sequence execution.

**Parameters**

- `value`: The explicit starting value.

**Returns:** This tweener.

**Remarks:** A change during an active step shifts intermediate built-in interpolation while retaining that step's final value. An integer displacement outside its type's range keeps endpoint interpolation.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-fromcurrent"></a>
### `public PropertyTweener<TValue> FromCurrent()`

Disables continuation from the step-start property; the configured start initially equals the append-time value.

**Returns:** This tweener.

**Remarks:** A preceding [`From`](#m-electron2d-propertytweener-1-from-0) value remains configured.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-asrelative"></a>
### `public PropertyTweener<TValue> AsRelative()`

Interprets the configured final value as a delta from the captured start.

**Returns:** This tweener.

**Remarks:** For a delayed continuation, the relative final value is resolved at step start before the delay-end recapture.

**Exceptions**

- `NotSupportedException`: `TValue` has no built-in addition contract.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-setcustominterpolator-system-func-system-double-system-double"></a>
### `public PropertyTweener<TValue> SetCustomInterpolator(Func<double, double> interpolator)`

Sets a custom mapping applied after the configured transition and easing.

**Parameters**

- `interpolator`: Maps the eased weight to a final interpolation weight; overshoot values are allowed.

**Returns:** This tweener.

**Remarks:** The mapping is also called with weight one on the final step, so the final write may overshoot.

**Exceptions**

- `ArgumentNullException`: `interpolator` is `null`.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-setdelay-system-double"></a>
### `public PropertyTweener<TValue> SetDelay(double delay)`

Sets the delay before property interpolation begins.

**Parameters**

- `delay`: Finite seconds; a negative delay begins on the first positive step.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `delay` is NaN or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-setease-electron2d-tween-easetype"></a>
### `public PropertyTweener<TValue> SetEase(Tween.EaseType ease)`

Overrides the owning tween's easing for this property.

**Parameters**

- `ease`: The easing direction.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `ease` is undefined.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-settrans-electron2d-tween-transitiontype"></a>
### `public PropertyTweener<TValue> SetTrans(Tween.TransitionType transition)`

Overrides the owning tween's transition for this property.

**Parameters**

- `transition`: The transition curve.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `transition` is undefined.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
