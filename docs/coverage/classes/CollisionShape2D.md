# CollisionShape2D API coverage

Last updated: 2026-09-26

Godot source: [doc/classes/CollisionShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public sealed class Electron2D.CollisionShape`](../../classes/CollisionShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CollisionShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public sealed class Electron2D.CollisionShape`](../../classes/CollisionShape.md) | Partial | Body geometry, one-way contact side and margin-based motion recovery execute; physics debug color still requires renderer canvas integration. |
| [`property Color debug_color = Color(0, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | — | Blocked | Trigger: 2D physics debug drawing through the retained canvas renderer. |
| [`property bool disabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public System.Boolean Disabled { get; set; }`](../../classes/CollisionShape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
| [`property bool one_way_collision = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public System.Boolean OneWayCollision { get; set; }`](../../classes/CollisionShape.md) | Implemented | Body fixtures use ADR 0065 pre-solve side selection with local direction and contact-pair lifetime; OneWayCollisionTests covers opposite approaches, rotation, live updates, Area nonresponse, packing and warm allocation. |
| [`property Vector2 one_way_collision_direction = Vector2(0, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public Electron2D.Vector2 OneWayCollisionDirection { get; set; }`](../../classes/CollisionShape.md) | Implemented | Body fixtures use ADR 0065 pre-solve side selection with local direction and contact-pair lifetime; OneWayCollisionTests covers opposite approaches, rotation, live updates, Area nonresponse, packing and warm allocation. |
| [`property float one_way_collision_margin = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public System.Single OneWayCollisionMargin { get; set; }`](../../classes/CollisionShape.md) | Implemented | Finite nonnegative depth margin gates one-way body motion recovery and survives PackedScene (ADRs 0063 and 0065, PhysicsMotionTests). |
| [`property Shape2D shape`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionShape2D.xml) | [`public Electron2D.Shape Shape { get; set; }`](../../classes/CollisionShape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
