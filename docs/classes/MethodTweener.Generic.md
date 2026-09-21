# MethodTweener\<TValue\>

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public sealed class MethodTweener<TValue> : Tweener`

`MethodTweener<TValue>` is owned and created only by `Tween.TweenMethod<TValue>`. It interpolates from one typed value to another and invokes `Action<TValue>` on every active frame, including the exact final value. Its declared public API is `SetDelay(double)`, `SetEase(Tween.EaseType)`, and `SetTrans(Tween.TransitionType)`; each returns this tweener. `Finished` is inherited from [`Tweener`](Tweener.md).

Delay and duration use finite non-negative seconds. Transition/ease defaults are captured when appended. Built-in interpolation supports the values listed by the [Tweening component](../components/tweening.md), and the append method accepts an explicit interpolator for other types. If the delegate's direct target is an `ElectronObject` and becomes disposed, the tweener completes without another call. Delegate, interpolation, completion-event, and arithmetic failures invalidate the parent after parallel siblings are attempted.

Configuration, processing, and disposal use the parent owner thread. Tests verify scalar interpolation, exact boundaries, parallel sequencing, callback target/failure behavior, process lanes, pause, manual stepping, and allocation-free warmed delivery. There is no untyped callable, argument binding, reflection invocation, or background clock. The current stable [`MethodTweener`](https://docs.godotengine.org/en/stable/classes/class_methodtweener.html) surface is implemented as a generic delegate contract.
