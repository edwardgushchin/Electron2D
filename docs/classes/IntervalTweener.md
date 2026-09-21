# IntervalTweener

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public sealed class IntervalTweener : Tweener`

`IntervalTweener` is created only by `Tween.TweenInterval(double)` and consumes one finite non-negative duration without calling user code or modifying a value. It declares no public members beyond inherited [`Tweener.Finished`](Tweener.md) and `ElectronObject` lifetime API. It preserves overshoot for later steps, resets on each parent loop, and completes at an exact boundary.

Processing and disposal use the parent owner thread. Invalid durations are rejected before append. Tests cover sequential intervals, nested timelines, loops, exact boundaries, and timeout-like progression. There is no independent timer, wall clock, thread, configuration method, or standalone constructor. The current stable [`IntervalTweener`](https://docs.godotengine.org/en/stable/classes/class_intervaltweener.html) role and empty declared method surface are implemented.
