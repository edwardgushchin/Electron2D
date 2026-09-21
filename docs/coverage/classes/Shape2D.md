# Shape2D API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Shape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Shape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`method collide(Transform2D local_xform, Shape2D with_shape, Transform2D shape_xform) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`method collide_and_get_contacts(Transform2D local_xform, Shape2D with_shape, Transform2D shape_xform) -> PackedVector2Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`method collide_with_motion(Transform2D local_xform, Vector2 local_motion, Shape2D with_shape, Transform2D shape_xform, Vector2 shape_motion) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`method collide_with_motion_and_get_contacts(Transform2D local_xform, Vector2 local_motion, Shape2D with_shape, Transform2D shape_xform, Vector2 shape_motion) -> PackedVector2Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`method draw(RID canvas_item, Color color) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`method get_rect() -> Rect2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
| [`property float custom_solver_bias = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shape2D.xml) | — | Blocked | Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). |
