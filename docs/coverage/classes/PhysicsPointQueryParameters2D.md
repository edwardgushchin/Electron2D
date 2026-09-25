# PhysicsPointQueryParameters2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/PhysicsPointQueryParameters2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public sealed class Electron2D.PhysicsPointQueryParameters2D`](../../classes/PhysicsPointQueryParameters2D.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PhysicsPointQueryParameters2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | [`public sealed class Electron2D.PhysicsPointQueryParameters2D`](../../classes/PhysicsPointQueryParameters2D.md) | Partial | Point, copied exclusions, mask and Area/body flags execute; CanvasInstanceID requires independent canvas/world identity (ADRs 0028 and 0063). |
| [`property int canvas_instance_id = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | — | Blocked | Trigger: independent viewport canvas instance identity and query filtering across multiple World2D resources (ADRs 0028 and 0063). |
| [`property bool collide_with_areas = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | [`public System.Boolean CollideWithAreas { get; set; }`](../../classes/PhysicsPointQueryParameters2D.md) | Implemented | Typed finite global point, copied RID exclusions, all-layer mask and Area/body flags execute for scene and server colliders (ADR 0063, PhysicsQueryTests). |
| [`property bool collide_with_bodies = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | [`public System.Boolean CollideWithBodies { get; set; }`](../../classes/PhysicsPointQueryParameters2D.md) | Implemented | Typed finite global point, copied RID exclusions, all-layer mask and Area/body flags execute for scene and server colliders (ADR 0063, PhysicsQueryTests). |
| [`property int collision_mask = 4294967295`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | [`public System.UInt32 CollisionMask { get; set; }`](../../classes/PhysicsPointQueryParameters2D.md) | Implemented | Typed finite global point, copied RID exclusions, all-layer mask and Area/body flags execute for scene and server colliders (ADR 0063, PhysicsQueryTests). |
| [`property RID[] exclude = []`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | [`public Electron2D.RID[] Exclude { get; set; }`](../../classes/PhysicsPointQueryParameters2D.md) | Implemented | Typed finite global point, copied RID exclusions, all-layer mask and Area/body flags execute for scene and server colliders (ADR 0063, PhysicsQueryTests). |
| [`property Vector2 position = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PhysicsPointQueryParameters2D.xml) | [`public Electron2D.Vector2 Position { get; set; }`](../../classes/PhysicsPointQueryParameters2D.md) | Implemented | Typed finite global point, copied RID exclusions, all-layer mask and Area/body flags execute for scene and server colliders (ADR 0063, PhysicsQueryTests). |
