# SubtweenTweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/SubtweenTweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SubtweenTweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Tweener](Tweener.md). Electron2D type: [`public sealed class Electron2D.SubtweenTweener`](../../classes/SubtweenTweener.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SubtweenTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SubtweenTweener.xml) | [`public sealed class Electron2D.SubtweenTweener`](../../classes/SubtweenTweener.md) | Implemented | SubtweenTweener is created only by TweenSubtween. VerifyTweenSubtweens checks source-tree detachment, parent pause/lane and combined speed, child reset per loop, finishing-frame wait, pinned unused-time forwarding, invalid/killed/disposed child handling and parallel failure continuation under ADR 0037. |
| [`method set_delay(float delay) -> SubtweenTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SubtweenTweener.xml) | [`public Electron2D.SubtweenTweener SetDelay(System.Double delay)`](../../classes/SubtweenTweener.md) | Implemented | Pinned set_delay stores signed seconds. VerifyTweenSubtweens checks self return, positive/negative delay, exact boundary delivery of the full child frame delta, live threshold changes, owner-thread rejection and non-finite rollback; typed finite validation follows ADR 0037. |
