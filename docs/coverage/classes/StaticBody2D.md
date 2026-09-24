# StaticBody2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/StaticBody2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [PhysicsBody2D](PhysicsBody2D.md). Electron2D type: [`public sealed class Electron2D.StaticBody`](../../classes/StaticBody.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class StaticBody2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public sealed class Electron2D.StaticBody`](../../classes/StaticBody.md) | Partial | First Box2D.NET scene-body profile executes collision geometry, fixed-step gravity, impulses, masks and lifetime; remaining own members retain operation-specific gaps on this page. |
| [`property float constant_angular_velocity = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete StaticBody2D API slice. |
| [`property Vector2 constant_linear_velocity = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete StaticBody2D API slice. |
| [`property PhysicsMaterial physics_material_override`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Blocked | Trigger: PhysicsMaterial resource with friction/restitution transfer into Box2D shape materials. |
