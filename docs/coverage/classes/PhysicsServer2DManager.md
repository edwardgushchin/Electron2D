# PhysicsServer2DManager API coverage

Last updated: 2026-10-08

Godot source: [doc/classes/PhysicsServer2DManager.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: [`public sealed class Electron2D.PhysicsServerManager`](../../classes/PhysicsServerManager.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PhysicsServer2DManager`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) | [`public sealed class Electron2D.PhysicsServerManager`](../../classes/PhysicsServerManager.md) | Implemented | Public fresh factory registration/default selection verified in a separate assembly: strict priority/ties, immutable lazy captures, explicit choices, null/disposed/reuse/recursive/throwing rejection and outside-lock worker registration. |
| [`method register_server(String name, Callable create_callback) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) | [`public static System.Void RegisterServer(System.String name, System.Func<Electron2D.PhysicsServerBackend> factory)`](../../classes/PhysicsServerManager.md) | Implemented | Public fresh factory registration/default selection verified in a separate assembly: strict priority/ties, immutable lazy captures, explicit choices, null/disposed/reuse/recursive/throwing rejection and outside-lock worker registration. |
| [`method set_default_server(String name, int priority) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsServer2DManager.xml) | [`public static System.Void SetDefaultServer(System.String name, System.Int32 priority)`](../../classes/PhysicsServerManager.md) | Implemented | Public fresh factory registration/default selection verified in a separate assembly: strict priority/ties, immutable lazy captures, explicit choices, null/disposed/reuse/recursive/throwing rejection and outside-lock worker registration. |
