# CollisionShape2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/CollisionShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public sealed class Electron2D.CollisionShape`](../../classes/CollisionShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CollisionShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public sealed class Electron2D.CollisionShape`](../../classes/CollisionShape.md) | Partial | First Box2D.NET scene-body profile executes collision geometry, fixed-step gravity, impulses, masks and lifetime; remaining own members retain operation-specific gaps on this page. |
| [`property Color debug_color = Color(0, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | — | Blocked | Trigger: 2D physics debug drawing through the retained canvas renderer. |
| [`property bool disabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public System.Boolean Disabled { get; set; }`](../../classes/CollisionShape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
| [`property bool one_way_collision = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | — | Blocked | Trigger: one-way contact filtering with direction/margin in a typed pre-solve physics callback. |
| [`property Vector2 one_way_collision_direction = Vector2(0, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | — | Blocked | Trigger: one-way contact filtering with direction/margin in a typed pre-solve physics callback. |
| [`property float one_way_collision_margin = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | — | Blocked | Trigger: one-way contact filtering with direction/margin in a typed pre-solve physics callback. |
| [`property Shape2D shape`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public Electron2D.Shape Shape { get; set; }`](../../classes/CollisionShape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
