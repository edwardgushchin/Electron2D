# MethodTweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/MethodTweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MethodTweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Tweener](Tweener.md). Electron2D type: [`public sealed class Electron2D.MethodTweener<TValue>`](../../classes/MethodTweener.Generic.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class MethodTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MethodTweener.xml) | [`public sealed class Electron2D.MethodTweener<TValue>`](../../classes/MethodTweener.Generic.md) | Implemented | MethodTweener is created only through TweenMethod. VerifyTweenMethods covers typed interpolation, signed timing, captured defaults and live curve/delay overrides, exact final values, per-loop reset, invalid target completion and callback/interpolator failure; inherited Tweener lifetime and signals retain their declaring rows under ADR 0037. |
| [`method set_delay(float delay) -> MethodTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MethodTweener.xml) | [`public Electron2D.MethodTweener<TValue> SetDelay(System.Double delay)`](../../classes/MethodTweener.Generic.md) | Implemented | Pinned set_delay stores signed seconds. VerifyTweenMethods checks self return, default/positive/negative delay, exact start, live threshold change, non-finite rollback and owner-thread rejection; parent step caps forwarded time under ADR 0037. |
| [`method set_ease(int ease [Tween.EaseType]) -> MethodTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MethodTweener.xml) | [`public Electron2D.MethodTweener<TValue> SetEase(Electron2D.Tween.EaseType ease)`](../../classes/MethodTweener.Generic.md) | Implemented | VerifyTweens checks inherited ease captured on append; VerifyTweenMethods checks per-method Out override, live InOut change, self return, undefined enum rollback and owner-thread rejection. Final value bypasses curve extrapolation under ADR 0037. |
| [`method set_trans(int trans [Tween.TransitionType]) -> MethodTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MethodTweener.xml) | [`public Electron2D.MethodTweener<TValue> SetTrans(Electron2D.Tween.TransitionType transition)`](../../classes/MethodTweener.Generic.md) | Implemented | VerifyTweens checks inherited Quad default; VerifyTweenMethods checks per-method Cubic override, live Sine change, self return, undefined enum rollback and owner-thread rejection under ADR 0037. |
