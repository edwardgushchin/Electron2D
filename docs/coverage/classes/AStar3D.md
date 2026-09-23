# AStar3D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AStar3D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AStar3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method _compute_cost(int from_id, int to_id) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method _estimate_cost(int from_id, int end_id) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method _filter_neighbor(int from_id, int neighbor_id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method add_point(int id, Vector3 position, float weight_scale = 1.0) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method are_points_connected(int id, int to_id, bool bidirectional = true) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method clear() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method connect_points(int id, int to_id, bool bidirectional = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method disconnect_points(int id, int to_id, bool bidirectional = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_available_point_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_closest_point(Vector3 to_position, bool include_disabled = false) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_closest_position_in_segment(Vector3 to_position) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_id_path(int from_id, int to_id, bool allow_partial_path = false) -> PackedInt64Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_capacity() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_connections(int id) -> PackedInt64Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_ids() -> PackedInt64Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_path(int from_id, int to_id, bool allow_partial_path = false) -> PackedVector3Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_position(int id) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_point_weight_scale(int id) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method has_point(int id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method is_point_disabled(int id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method remove_point(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method reserve_space(int num_nodes) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method set_point_disabled(int id, bool disabled = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method set_point_position(int id, Vector3 position) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method set_point_weight_scale(int id, float weight_scale) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool neighbor_filter_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
