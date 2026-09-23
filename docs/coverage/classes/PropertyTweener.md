# PropertyTweener API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/PropertyTweener.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Tweener](Tweener.md). Electron2D type: [`public sealed class Electron2D.PropertyTweener<TValue>`](../../classes/PropertyTweener.Generic.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public sealed class Electron2D.PropertyTweener<TValue>`](../../classes/PropertyTweener.Generic.md) | Partial | Typed C# type exists; inheritance, signatures and behavior require row-level audit. |
| [`method as_relative() -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> AsRelative()`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method from(Variant value) -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> From(TValue value)`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method from_current() -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> FromCurrent()`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method set_custom_interpolator(Callable interpolator_method) -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> SetCustomInterpolator(System.Func<System.Double, System.Double> interpolator)`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method set_delay(float delay) -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> SetDelay(System.Double delay)`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method set_ease(int ease [Tween.EaseType]) -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> SetEase(Electron2D.Tween.EaseType ease)`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method set_trans(int trans [Tween.TransitionType]) -> PropertyTweener`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PropertyTweener.xml) | [`public Electron2D.PropertyTweener<TValue> SetTrans(Electron2D.Tween.TransitionType transition)`](../../classes/PropertyTweener.Generic.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
