# RectangleShape2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/RectangleShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RectangleShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.RectangleShape`](../../classes/RectangleShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RectangleShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RectangleShape2D.xml) | [`public sealed class Electron2D.RectangleShape`](../../classes/RectangleShape.md) | Implemented | Complete own shape dimensions and bounds execute; inherited Shape queries retain separate rows. |
| [`property Vector2 size = Vector2(20, 20)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RectangleShape2D.xml) | [`public Electron2D.Vector2 Size { get; set; }`](../../classes/RectangleShape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
