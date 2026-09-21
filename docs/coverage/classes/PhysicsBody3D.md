# PhysicsBody3D API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/PhysicsBody3D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [CollisionObject3D](CollisionObject3D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PhysicsBody3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method add_collision_exception_with(Node body) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_axis_lock(int axis [PhysicsServer3D.BodyAxis]) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_collision_exceptions() -> PhysicsBody3D[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_gravity() -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method move_and_collide(Vector3 motion, bool test_only = false, float safe_margin = 0.001, bool recovery_as_collision = false, int max_collisions = 1) -> KinematicCollision3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method remove_collision_exception_with(Node body) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method set_axis_lock(int axis [PhysicsServer3D.BodyAxis], bool lock) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method test_move(Transform3D from, Vector3 motion, KinematicCollision3D collision = null, float safe_margin = 0.001, bool recovery_as_collision = false, int max_collisions = 1) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`property bool axis_lock_angular_x = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool axis_lock_angular_y = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool axis_lock_angular_z = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool axis_lock_linear_x = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool axis_lock_linear_y = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool axis_lock_linear_z = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
