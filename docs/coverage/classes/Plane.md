# Plane API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Plane.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: —. Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constant PLANE_XY = Plane(0, 0, 1, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constant PLANE_XZ = Plane(0, 1, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constant PLANE_YZ = Plane(1, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane() -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane(Plane from) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane(Vector3 normal) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane(Vector3 normal, Vector3 point) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane(Vector3 point1, Vector3 point2, Vector3 point3) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane(Vector3 normal, float d) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`constructor Plane(float a, float b, float c, float d) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method distance_to(Vector3 point) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_center() -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method has_point(Vector3 point, float tolerance = 1e-05) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method intersect_3(Plane b, Plane c) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method intersects_ray(Vector3 from, Vector3 dir) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method intersects_segment(Vector3 from, Vector3 to) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method is_equal_approx(Plane to_plane) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method is_finite() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method is_point_over(Vector3 point) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method normalized() -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method project(Vector3 point) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`operator operator !=(Plane right) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`operator operator *(Transform3D right) -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`operator operator ==(Plane right) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`operator operator unary+() -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`operator operator unary-() -> Plane`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property float d = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property Vector3 normal = Vector3(0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property float x = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property float y = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property float z = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Plane.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
