# CallbackTweener

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public sealed class CallbackTweener : Tweener`

`CallbackTweener` is created only by `Tween.TweenCallback(Action)` and invokes its callback once after its step begins and the optional delay expires. Its complete declared public API is `CallbackTweener SetDelay(double delay)`; `Finished` and object lifetime API are inherited from [`Tweener`](Tweener.md). Delay is finite, non-negative, speed-scaled, and zero by default. Any overshoot remains available to later sequential steps.

If the delegate's direct target is a disposed `ElectronObject`, the task finishes without invocation. The callback runs synchronously on the parent owner thread. A callback or completion-event exception leaves the task/parent coherent, allows parallel siblings and later SceneTree phases to run, then invalidates the parent and is aggregated. Explicit disposal cancels the task and clears subscribers.

Tests verify delay, parallel/sequential order, loops, callback failure continuation, and zero-duration infinite-loop protection. There is no untyped callable, bound argument list, reflection dispatch, or standalone construction. The current stable [`CallbackTweener`](https://docs.godotengine.org/en/stable/classes/class_callbacktweener.html) behavior and method surface are implemented through `Action`.
