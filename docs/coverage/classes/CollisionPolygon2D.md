# CollisionPolygon2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/CollisionPolygon2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CollisionPolygon2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
| [`enum BuildMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
| [`enum_value BUILD_SEGMENTS [BuildMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
| [`enum_value BUILD_SOLIDS [BuildMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
| [`property int build_mode [CollisionPolygon2D.BuildMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
| [`property bool disabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
| [`property bool one_way_collision = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: typed one-way pre-solve contact filtering with direction and margin on polygon-owner fixtures (ADR 0064). |
| [`property Vector2 one_way_collision_direction = Vector2(0, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: typed one-way pre-solve contact filtering with direction and margin on polygon-owner fixtures (ADR 0064). |
| [`property float one_way_collision_margin = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: typed one-way pre-solve contact filtering with direction and margin on polygon-owner fixtures (ADR 0064). |
| [`property PackedVector2Array polygon = PackedVector2Array()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CollisionPolygon2D.xml) | — | Blocked | Trigger: a direct CollisionObject polygon slot with live convex/paired-segment BuildMode conversion, transform ownership and PackedScene state; both shape resources now exist (ADR 0064). |
