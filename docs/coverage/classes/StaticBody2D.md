# StaticBody2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/StaticBody2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [PhysicsBody2D](PhysicsBody2D.md). Electron2D type: [`public class Electron2D.StaticBody`](../../classes/StaticBody.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class StaticBody2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public class Electron2D.StaticBody`](../../classes/StaticBody.md) | Partial | First Box2D.NET scene-body profile executes collision geometry, fixed-step gravity, impulses, masks and lifetime; remaining own members retain operation-specific gaps on this page. |
| [`property float constant_angular_velocity = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete StaticBody2D API slice. |
| [`property Vector2 constant_linear_velocity = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete StaticBody2D API slice. |
| [`property PhysicsMaterial physics_material_override`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public Electron2D.PhysicsMaterial PhysicsMaterialOverride { get; set; }`](../../classes/StaticBody.md) | Implemented | Borrowed material transfers to Box2D fixtures; live edits, packing, disposal and surface mixing verified by PhysicsMaterialTests. |
