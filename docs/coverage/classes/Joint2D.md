# Joint2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/Joint2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Joint2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Unimplemented | Accepted 2D capability; trigger: typed scene-joint ownership, anchors and Box2D solver integration using the implemented physics world. |
| [`method get_rid() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Blocked | Trigger: public typed physics RID identity and joint/body/world handle lifetime before exposing a scene joint handle (ADR 0061). |
| [`property float bias = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Blocked | Trigger: verified per-joint positional bias mapping in Box2D's constraint solver; no equivalent public revolute parameter is present (ADR 0061). |
| [`property bool disable_collision = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Unimplemented | Accepted 2D capability; trigger: typed scene-joint ownership, anchors and Box2D solver integration using the implemented physics world. |
| [`property NodePath node_a = NodePath("")`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Unimplemented | Accepted 2D capability; trigger: typed scene-joint ownership, anchors and Box2D solver integration using the implemented physics world. |
| [`property NodePath node_b = NodePath("")`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Unimplemented | Accepted 2D capability; trigger: typed scene-joint ownership, anchors and Box2D solver integration using the implemented physics world. |
