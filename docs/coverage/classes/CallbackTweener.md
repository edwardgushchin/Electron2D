# CallbackTweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/CallbackTweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CallbackTweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Tweener](Tweener.md). Electron2D type: [`public sealed class Electron2D.CallbackTweener`](../../classes/CallbackTweener.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CallbackTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CallbackTweener.xml) | [`public sealed class Electron2D.CallbackTweener`](../../classes/CallbackTweener.md) | Implemented | CallbackTweener is constructed only by TweenCallback. VerifyTweenCallbackIntervals covers every own behavior: default and live delay, signed first-step firing, direct-target disposal, per-loop replay, cancellation and failure; inherited Tweener event is classified separately under ADR 0037. |
| [`method set_delay(float delay) -> CallbackTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CallbackTweener.xml) | [`public Electron2D.CallbackTweener SetDelay(System.Double delay)`](../../classes/CallbackTweener.md) | Implemented | Pinned set_delay stores signed seconds; VerifyTweenCallbackIntervals checks self return, zero/default, negative first-step firing, live threshold change, non-finite rollback and owner-thread rejection. Parent step caps forwarded time at the delivered delta under ADR 0037. |
