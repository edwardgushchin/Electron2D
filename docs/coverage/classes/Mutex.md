# Mutex API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Mutex.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Mutex.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Mutex`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Mutex.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method lock() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Mutex.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method try_lock() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Mutex.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method unlock() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Mutex.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
