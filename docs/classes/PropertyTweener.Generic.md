# PropertyTweener\<TValue\>

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public sealed class PropertyTweener<TValue> : Tweener`

## Responsibility, ownership, and API

Created only by `Tween.TweenProperty<TTarget,TValue>`, this owned task reads and writes one property through explicit typed delegates. Its declared public API is `From(TValue)`, `FromCurrent()`, `AsRelative()`, `SetCustomInterpolator(Func<double,double>)`, `SetDelay(double)`, `SetEase(Tween.EaseType)`, and `SetTrans(Tween.TransitionType)`; all return this tweener for fluent configuration. `Finished` and inherited object API come from [`Tweener`](Tweener.md).

Default execution captures the property when the step starts, or when a configured delay ends. `From` fixes an explicit start for every loop; `FromCurrent` fixes the value captured at append time. `AsRelative` treats the configured final value as a delta and requires a supported addition operation. Transition/ease are copied from the parent when appended and can be overridden. A custom interpolator receives the already-eased `0..1` weight and may return overshoot. Exact completion writes either the exact final value or the custom-weight result before raising `Finished`.

The target is held while the parent exists, but logical target disposal completes the tweener without writing. Getter, setter, interpolator, arithmetic, and event exceptions invalidate the parent after parallel siblings are attempted. Time and enum validation is explicit; calls use the parent owner thread. Built-in supported values and threading are documented in the [Tweening component](../components/tweening.md).

Tests verify current-value capture, restart recapture, typed vector writes, exact final values, target lifetime, sequencing, and failure aggregation. String property paths, reflection, nested member paths, dynamic conversion, and editor curve resources are absent. The current stable [`PropertyTweener`](https://docs.godotengine.org/en/stable/classes/class_propertytweener.html) method surface and target-lifetime behavior are implemented with typed accessors; untyped values/property paths are deliberately excluded.
