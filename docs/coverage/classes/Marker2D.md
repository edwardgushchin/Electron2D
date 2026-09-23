# Marker2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Marker2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marker2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Marker2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marker2D.xml) | — | Blocked | The public Electron2D name is Marker : Entity under ADR 0004. A runtime-only anchor without the pinned editor cross would be an inert compatibility shell. Trigger: implement editor canvas gizmo drawing in the self-hosted editor, including configurable gizmo extents, then add Marker and verify the inherited spatial API; no runtime type exists yet. |
| [`property float gizmo_extents = 10.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marker2D.xml) | — | Blocked | The public Electron2D name is Marker : Entity under ADR 0004. A runtime-only anchor without the pinned editor cross would be an inert compatibility shell. Trigger: implement editor canvas gizmo drawing in the self-hosted editor, including configurable gizmo extents, then add Marker and verify the inherited spatial API; no runtime type exists yet. |
