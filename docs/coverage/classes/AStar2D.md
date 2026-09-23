# AStar2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AStar2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AStar2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method _compute_cost(int from_id, int to_id) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method _estimate_cost(int from_id, int end_id) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method _filter_neighbor(int from_id, int neighbor_id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method add_point(int id, Vector2 position, float weight_scale = 1.0) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method are_points_connected(int id, int to_id, bool bidirectional = true) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method clear() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method connect_points(int id, int to_id, bool bidirectional = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method disconnect_points(int id, int to_id, bool bidirectional = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_available_point_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_closest_point(Vector2 to_position, bool include_disabled = false) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_closest_position_in_segment(Vector2 to_position) -> Vector2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_id_path(int from_id, int to_id, bool allow_partial_path = false) -> PackedInt64Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_capacity() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_connections(int id) -> PackedInt64Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_ids() -> PackedInt64Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_path(int from_id, int to_id, bool allow_partial_path = false) -> PackedVector2Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_position(int id) -> Vector2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method get_point_weight_scale(int id) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method has_point(int id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method is_point_disabled(int id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method remove_point(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method reserve_space(int num_nodes) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method set_point_disabled(int id, bool disabled = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method set_point_position(int id, Vector2 position) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`method set_point_weight_scale(int id, float weight_scale) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
| [`property bool neighbor_filter_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AStar2D.xml) | — | Blocked | Navigation2D: trigger is the first 2D navigation slice. |
