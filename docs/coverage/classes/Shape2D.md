# Shape2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/Shape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public abstract class Electron2D.Shape`](../../classes/Shape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Shape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | [`public abstract class Electron2D.Shape`](../../classes/Shape.md) | Partial | First Box2D.NET scene-body profile executes collision geometry, fixed-step gravity, impulses, masks and lifetime; remaining own members retain operation-specific gaps on this page. |
| [`method collide(Transform2D local_xform, Shape2D with_shape, Transform2D shape_xform) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Shape2D API slice. |
| [`method collide_and_get_contacts(Transform2D local_xform, Shape2D with_shape, Transform2D shape_xform) -> PackedVector2Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Shape2D API slice. |
| [`method collide_with_motion(Transform2D local_xform, Vector2 local_motion, Shape2D with_shape, Transform2D shape_xform, Vector2 shape_motion) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Shape2D API slice. |
| [`method collide_with_motion_and_get_contacts(Transform2D local_xform, Vector2 local_motion, Shape2D with_shape, Transform2D shape_xform, Vector2 shape_motion) -> PackedVector2Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Shape2D API slice. |
| [`method draw(RID canvas_item, Color color) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Trigger: renderer RID canvas debug-draw integration for physics shapes; no public physics canvas handle exists yet. |
| [`method get_rect() -> Rect2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | [`public abstract Electron2D.Rect2 GetRect()`](../../classes/Shape.md) | Implemented | Pinned defaults and typed behavior execute through the Box2D.NET-backed scene-world slice; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. |
| [`property float custom_solver_bias = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Trigger: accepted per-shape solver-bias mapping and Box2D contact softness verification. |
