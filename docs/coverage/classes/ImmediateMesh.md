# ImmediateMesh API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/ImmediateMesh.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Mesh](Mesh.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ImmediateMesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method clear_surfaces() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method surface_add_vertex(Vector3 vertex) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method surface_add_vertex_2d(Vector2 vertex) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method surface_begin(int primitive [Mesh.PrimitiveType], Material material = null) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method surface_end() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method surface_set_color(Color color) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method surface_set_normal(Vector3 normal) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method surface_set_tangent(Plane tangent) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method surface_set_uv(Vector2 uv) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method surface_set_uv2(Vector2 uv2) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImmediateMesh.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
