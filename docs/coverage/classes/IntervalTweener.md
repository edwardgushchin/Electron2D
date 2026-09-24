# IntervalTweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/IntervalTweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IntervalTweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Tweener](Tweener.md). Electron2D type: [`public sealed class Electron2D.IntervalTweener`](../../classes/IntervalTweener.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class IntervalTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IntervalTweener.xml) | [`public sealed class Electron2D.IntervalTweener`](../../classes/IntervalTweener.md) | Implemented | IntervalTweener is constructed only by TweenInterval and has no own public methods. VerifyTweenCallbackIntervals checks positive/exact, zero, negative, overshoot and Finished delivery; inherited Tweener event remains on its declaring row under ADR 0037. |
