# CircleShape2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/CircleShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CircleShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.CircleShape`](../../classes/CircleShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CircleShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CircleShape2D.xml) | [`public sealed class Electron2D.CircleShape`](../../classes/CircleShape.md) | Implemented | Complete own shape dimensions and bounds execute; inherited Shape queries retain separate rows. |
| [`property float radius = 10.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CircleShape2D.xml) | [`public System.Single Radius { get; set; }`](../../classes/CircleShape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
