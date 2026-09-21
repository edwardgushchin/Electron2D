# SubtweenTweener

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public sealed class SubtweenTweener : Tweener`

`SubtweenTweener` is created only by `Tween.TweenSubtween(Tween)` and runs another Tween as one parent step. Its complete declared public API is `SubtweenTweener SetDelay(double delay)`; completion and object API are inherited from [`Tweener`](Tweener.md).

Appending requires a valid same-tree tween, removes it from independent SceneTree processing, rejects self/multiple/cyclic/cross-tree nesting, and transfers final lifetime to the parent. Each parent-loop start resets and plays the child. Parent pause, process lane, speed, and delivered delta control the child; the child's own tree scheduling settings no longer schedule it separately. Parent completion, killing, disposal, or failure invalidates the child. A child failure invalidates the parent and is aggregated.

All configuration, stepping, and disposal use the common owner thread. Tests verify transfer from the tree list, ordered child execution, repeatability through parent flow, and shared invalidation. Nested completion intentionally consumes the current parent remainder, matching the reference nested-step boundary behavior. There is no cross-tree transfer or independently processed nested tween. The current stable [`SubtweenTweener`](https://docs.godotengine.org/en/stable/classes/class_subtweentweener.html) surface and lifecycle are implemented.
