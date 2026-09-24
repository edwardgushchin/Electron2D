# WeakRef API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/WeakRef.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WeakRef.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public sealed class Electron2D.WeakRef<T>`](../../classes/WeakRef.Generic.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class WeakRef`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WeakRef.xml) | [`public sealed class Electron2D.WeakRef<T>`](../../classes/WeakRef.Generic.md) | Implemented | ADR 0050 supplies a typed ElectronObject weak-reference wrapper with ordinary managed disposal; WeakRefTests verifies target identity, disposal and collection. |
| [`method get_ref() -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WeakRef.xml) | [`public T GetRef()`](../../classes/WeakRef.Generic.md) | Implemented | ADR 0050 returns the same live object or null after target disposal or collection; WeakRefTests checks identity, null, deterministic disposal and GC collection. |
