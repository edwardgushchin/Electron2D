# Geometry3D API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Geometry3D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Geometry3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method build_box_planes(Vector3 extents) -> Plane[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method build_capsule_planes(float radius, float height, int sides, int lats, int axis [Vector3.Axis] = 2) -> Plane[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method build_cylinder_planes(float radius, float height, int sides, int axis [Vector3.Axis] = 2) -> Plane[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method clip_polygon(PackedVector3Array points, Plane plane) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method compute_convex_mesh_points(Plane[] planes) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_closest_point_to_segment(Vector3 point, Vector3 s1, Vector3 s2) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_closest_point_to_segment_uncapped(Vector3 point, Vector3 s1, Vector3 s2) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_closest_points_between_segments(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_triangle_barycentric_coords(Vector3 point, Vector3 a, Vector3 b, Vector3 c) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method ray_intersects_triangle(Vector3 from, Vector3 dir, Vector3 a, Vector3 b, Vector3 c) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method segment_intersects_convex(Vector3 from, Vector3 to, Plane[] planes) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method segment_intersects_cylinder(Vector3 from, Vector3 to, float height, float radius) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method segment_intersects_sphere(Vector3 from, Vector3 to, Vector3 sphere_position, float sphere_radius) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method segment_intersects_triangle(Vector3 from, Vector3 to, Vector3 a, Vector3 b, Vector3 c) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method tetrahedralize_delaunay(PackedVector3Array points) -> PackedInt32Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Geometry3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
