# AwaitTweener

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
Declaration: `public sealed class AwaitTweener : Tweener`

`AwaitTweener` is created by one of the three typed `Tween.TweenAwait` overloads and waits for a zero-, one-, or two-argument C# event. Its complete declared public API is `AwaitTweener SetTimeout(double timeout)`; `Finished` and lifetime API are inherited from [`Tweener`](Tweener.md).

Append establishes an owned `EventConnection`. Only an event observed while the wait is the active step releases it; observations before the step begins are cleared at start, and replayed parent loops begin a fresh wait. Event receipt atomically marks state and may occur on any thread. The next owner-thread advance completes the task and consumes that frame's remaining delta. A finite non-negative timeout can complete sooner and preserves overshoot. Disposing the publisher, losing the connection, parent completion/killing, or explicit disposal also ends or cancels the wait and disconnects.

Subscription/accessor and completion-event failures are explicit and participate in parent/SceneTree aggregation. Event payloads are intentionally ignored; they remain compile-time typed at the subscription boundary. Tests cover pre-start reset, active event receipt, consumed-frame ordering, timeout overshoot, source/tween lifetime, and cleanup. Events with more than two payload arguments require a future `EventConnection` overload; no untyped Signal or argument array is introduced. The current stable [`AwaitTweener`](https://docs.godotengine.org/en/stable/classes/class_awaittweener.html) wait/timeout/lifetime behavior is implemented through typed events.
