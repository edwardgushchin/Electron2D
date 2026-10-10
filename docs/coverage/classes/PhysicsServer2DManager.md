# PhysicsServer2DManager API coverage

Last updated: 2026-10-08

Godot source: [doc/classes/PhysicsServer2DManager.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PhysicsServer2DManager`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) | — | Blocked | Trigger: complete typed backend registration/factory and world-scoped implementation dispatch, shared RID lifecycle, callbacks, direct state/query result construction and CPU/GPU scene integration (ADRs 0054 and 0103); current facade is static/sealed and attachment/step paths select built-in implementations only. |
| [`method register_server(String name, Callable create_callback) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) | — | Blocked | Trigger: complete typed backend registration/factory and world-scoped implementation dispatch, shared RID lifecycle, callbacks, direct state/query result construction and CPU/GPU scene integration (ADRs 0054 and 0103); current facade is static/sealed and attachment/step paths select built-in implementations only. |
| [`method set_default_server(String name, int priority) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) | — | Blocked | Trigger: complete typed backend registration/factory and world-scoped implementation dispatch, shared RID lifecycle, callbacks, direct state/query result construction and CPU/GPU scene integration (ADRs 0054 and 0103); current facade is static/sealed and attachment/step paths select built-in implementations only. |
