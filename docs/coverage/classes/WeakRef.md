# WeakRef API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/WeakRef.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WeakRef.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class WeakRef`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WeakRef.xml) | — | Blocked | Trigger: an accepted public weak-reference contract beyond System.WeakReference<T>; Resource currently uses only an internal weak path cache (ADR 0013). |
| [`method get_ref() -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WeakRef.xml) | — | Blocked | Trigger: an accepted public weak-reference contract beyond System.WeakReference<T>; Resource currently uses only an internal weak path cache (ADR 0013). |
