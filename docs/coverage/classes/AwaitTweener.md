# AwaitTweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AwaitTweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AwaitTweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Tweener](Tweener.md). Electron2D type: [`public sealed class Electron2D.AwaitTweener`](../../classes/AwaitTweener.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AwaitTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AwaitTweener.xml) | [`public sealed class Electron2D.AwaitTweener`](../../classes/AwaitTweener.md) | Implemented | AwaitTweener is created only by TweenAwait. VerifyTweenAwaits covers all three typed arities, subscription lifetime, event receipt on another thread with owner-thread completion, source disposal, timeout priority and overshoot, negative timeout, loops, cancellation and completion failure under ADR 0037. Independent publisher-side handler removal remains the explicit typed-event limit. |
| [`method set_timeout(float timeout) -> AwaitTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AwaitTweener.xml) | [`public Electron2D.AwaitTweener SetTimeout(System.Double timeout)`](../../classes/AwaitTweener.md) | Implemented | Pinned set_timeout stores signed seconds and checks timeout >= 0 before elapsed comparison. VerifyTweenAwaits checks self return, default indefinite wait, negative disable, zero/positive expiry, live changes, same-frame priority, non-finite rollback and owner-thread rejection under ADR 0037. |
