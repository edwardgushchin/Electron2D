# Tweener

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public abstract class Tweener : ElectronObject`

## Responsibility and ownership

`Tweener` is the non-constructible public base for one task owned by a [`Tween`](Tween.md). Its sole declared public member is `event Action<Tweener>? Finished`, raised synchronously after successful task completion or loss/disposal of a task target. Killing a parent does not report unfinished tweeners as finished. Inherited `ElectronObject` API remains available.

The parent assigns ownership and controls start, elapsed time, stepping, cancellation, and disposal. A tweener resets when its step is replayed by a parent loop. Explicit disposal uses the parent owner thread, cancels subscriptions, clears subscribers, and makes later parent processing treat the tweener as inactive.

User completion subscriber exceptions propagate into the parent step. Parallel siblings are still attempted; the parent tween is then invalidated and SceneTree aggregates the failure. Tweener instances cannot be constructed or attached independently, moved between parents, or processed on another thread.

Dependencies are `ElectronObject` and `Tween`. Tests verify completion ordering, loops, invalid targets, parallel failure continuation, waits, nested cancellation, and owner-thread behavior. There is no public custom-Tweener extension point, serialization, reference-counted lifetime, or standalone scheduler.

Official-reference audit: the current stable [`Tweener`](https://docs.godotengine.org/en/stable/classes/class_tweener.html) abstract role and `finished` signal are implemented; reference-counted ownership is deliberately adapted to managed ownership and explicit disposal.
