# PropertyTweener\<TValue\>

Last updated: 2026-09-21

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PropertyTweener<TValue> : Tweener`

> Interpolates a typed property through explicit getter and setter delegates.

## Description

Interpolates a typed property through explicit getter and setter delegates.

Created only by `Tween.TweenProperty<TTarget,TValue>`, this owned task reads and writes one property through explicit typed delegates. Its declared public API is `From(TValue)`, `FromCurrent()`, `AsRelative()`, `SetCustomInterpolator(Func<double,double>)`, `SetDelay(double)`, `SetEase(Tween.EaseType)`, and `SetTrans(Tween.TransitionType)`; all return this tweener for fluent configuration. `Finished` and inherited object API come from [`Tweener`](Tweener.md).

Default execution captures the property when the step starts, or when a configured delay ends. `From` fixes an explicit start for every loop; `FromCurrent` fixes the value captured at append time. `AsRelative` treats the configured final value as a delta and requires a supported addition operation. Transition/ease are copied from the parent when appended and can be overridden. A custom interpolator receives the already-eased `0..1` weight and may return overshoot. Exact completion writes either the exact final value or the custom-weight result before raising `Finished`.

The target is held while the parent exists, but logical target disposal completes the tweener without writing. Getter, setter, interpolator, arithmetic, and event exceptions invalidate the parent after parallel siblings are attempted. Time and enum validation is explicit; calls use the parent owner thread. Built-in supported values and threading are documented in the [Tweening component](../components/tweening.md).

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

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-fromcurrent"></a>
### `public PropertyTweener<TValue> FromCurrent()`

Uses the property value captured when this tweener was appended as its starting value.

**Returns:** This tweener.

**Exceptions**

- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-asrelative"></a>
### `public PropertyTweener<TValue> AsRelative()`

Interprets the configured final value as a delta from the captured start.

**Returns:** This tweener.

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

**Exceptions**

- `ArgumentNullException`: `interpolator` is `null`.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

<a id="m-electron2d-propertytweener-1-setdelay-system-double"></a>
### `public PropertyTweener<TValue> SetDelay(double delay)`

Sets the delay before property interpolation begins.

**Parameters**

- `delay`: Finite non-negative seconds.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `delay` is negative, NaN, or infinite.
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
