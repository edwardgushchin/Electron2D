# Thread API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Thread.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Thread`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`enum Priority`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`enum_value PRIORITY_HIGH [Priority] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`enum_value PRIORITY_LOW [Priority] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`enum_value PRIORITY_NORMAL [Priority] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method get_id() -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method is_alive() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method is_main_thread() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method is_started() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method set_thread_safety_checks_enabled(bool enabled) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method start(Callable callable, int priority [Thread.Priority] = 1) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
| [`method wait_to_finish() -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Thread.xml) | — | Blocked | Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). |
