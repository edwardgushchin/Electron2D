# Tweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Tweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Tweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public abstract class Electron2D.Tweener`](../../classes/Tweener.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Tweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Tweener.xml) | [`public abstract class Electron2D.Tweener`](../../classes/Tweener.md) | Implemented | The abstract Tweener role has no public constructor and is instantiated only through six Tween task builders. Its only declared signal is audited across completion, loops, invalid targets and failure; VerifyTweenTypeLifetime checks owner-thread disposal, subscription cancellation and skipped disposed tasks. RefCounted ancestry uses ElectronObject managed lifetime under ADR 0037. |
| [`signal finished() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Tweener.xml) | [`public event System.Action<Electron2D.Tweener> Finished`](../../classes/Tweener.md) | Implemented | Shared Tweener.Finished passes the source after successful completion or direct target loss, once per loop execution; it is absent on Kill or callback failure. VerifyTweenCallbackIntervals checks sender, ordering, replay, invalid target and cancellation; VerifyTweens checks subscriber failure continuation under ADR 0037. |
