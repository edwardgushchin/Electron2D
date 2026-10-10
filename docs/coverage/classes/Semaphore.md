# Semaphore API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Semaphore.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Semaphore.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Semaphore`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Semaphore.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method post(int count = 1) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Semaphore.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method try_wait() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Semaphore.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method wait() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Semaphore.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
