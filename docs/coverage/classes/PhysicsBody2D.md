# PhysicsBody2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/PhysicsBody2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [CollisionObject2D](CollisionObject2D.md). Electron2D type: [`public abstract class Electron2D.PhysicsBody`](../../classes/PhysicsBody.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PhysicsBody2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | [`public abstract class Electron2D.PhysicsBody`](../../classes/PhysicsBody.md) | Partial | First Box2D.NET scene-body profile executes collision geometry, fixed-step gravity, impulses, masks and lifetime; remaining own members retain operation-specific gaps on this page. |
| [`method add_collision_exception_with(Node body) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete PhysicsBody2D API slice. |
| [`method get_collision_exceptions() -> PhysicsBody2D[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete PhysicsBody2D API slice. |
| [`method get_gravity() -> Vector2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | [`public Electron2D.Vector2 GetGravity()`](../../classes/PhysicsBody.md) | Partial | RigidBody last-step area/world gravity and StaticBody zero execute; CharacterBody is absent, so its inherited area-gravity query remains a dependency. |
| [`method move_and_collide(Vector2 motion, bool test_only = false, float safe_margin = 0.08, bool recovery_as_collision = false) -> KinematicCollision2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | [`public Electron2D.KinematicCollision2D MoveAndCollide(Electron2D.Vector2 motion, System.Boolean testOnly = false, System.Single safeMargin = 0.08f, System.Boolean recoveryAsCollision = false)`](../../classes/PhysicsBody.md) | Implemented | Scene body movement and test-only probes execute with safe-margin recovery, earliest body contact, one-way surfaces and typed KinematicCollision snapshots (ADR 0063, PhysicsMotionTests). |
| [`method remove_collision_exception_with(Node body) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete PhysicsBody2D API slice. |
| [`method test_move(Transform2D from, Vector2 motion, KinematicCollision2D collision = null, float safe_margin = 0.08, bool recovery_as_collision = false) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | [`public System.Boolean TestMove(Electron2D.Transform from, Electron2D.Vector2 motion, Electron2D.KinematicCollision2D collision = null, System.Single safeMargin = 0.08f, System.Boolean recoveryAsCollision = false)`](../../classes/PhysicsBody.md) | Implemented | Scene body movement and test-only probes execute with safe-margin recovery, earliest body contact, one-way surfaces and typed KinematicCollision snapshots (ADR 0063, PhysicsMotionTests). |
| [`property bool input_pickable = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsBody2D.xml) | — | Blocked | Trigger: viewport physics picking and collision-object input eligibility. |
