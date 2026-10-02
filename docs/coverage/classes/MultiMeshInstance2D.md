# MultiMeshInstance2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/MultiMeshInstance2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiMeshInstance2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class MultiMeshInstance2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiMeshInstance2D.xml) | — | Blocked | Trigger: typed 2D instance-buffer ownership, visible-count/color policies, physics-interpolated poses and repeated/instanced canvas submission over the now executable mesh surfaces; MultiMeshInstance enters that same first consumer slice (ADR 0092). |
| [`property MultiMesh multimesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiMeshInstance2D.xml) | — | Blocked | Trigger: typed 2D instance-buffer ownership, visible-count/color policies, physics-interpolated poses and repeated/instanced canvas submission over the now executable mesh surfaces; MultiMeshInstance enters that same first consumer slice (ADR 0092). |
| [`property Texture2D texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiMeshInstance2D.xml) | — | Blocked | Trigger: typed 2D instance-buffer ownership, visible-count/color policies, physics-interpolated poses and repeated/instanced canvas submission over the now executable mesh surfaces; MultiMeshInstance enters that same first consumer slice (ADR 0092). |
| [`signal texture_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiMeshInstance2D.xml) | — | Blocked | Trigger: typed 2D instance-buffer ownership, visible-count/color policies, physics-interpolated poses and repeated/instanced canvas submission over the now executable mesh surfaces; MultiMeshInstance enters that same first consumer slice (ADR 0092). |
