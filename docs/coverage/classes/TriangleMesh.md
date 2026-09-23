# TriangleMesh API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/TriangleMesh.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TriangleMesh.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TriangleMesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TriangleMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). |
| [`method create_from_faces(PackedVector3Array faces) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TriangleMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). |
| [`method get_faces() -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TriangleMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). |
| [`method intersect_ray(Vector3 begin, Vector3 dir) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TriangleMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method intersect_segment(Vector3 begin, Vector3 end) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TriangleMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
