# StaticBody2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/StaticBody2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [PhysicsBody2D](PhysicsBody2D.md). Electron2D type: [`public class Electron2D.StaticBody`](../../classes/StaticBody.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class StaticBody2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public class Electron2D.StaticBody`](../../classes/StaticBody.md) | Partial | First Box2D.NET scene-body profile executes collision geometry, fixed-step gravity, impulses, masks and lifetime; remaining own members retain operation-specific gaps on this page. |
| [`property float constant_angular_velocity = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Blocked | Exact trigger: a stationary-surface velocity channel must supply both normal and tangential point velocity to contact constraints while leaving body pose stationary. Box2D static contacts select a zero dummy solver state (B2ContactSolvers), and material tangentSpeed supplies only a scalar tangent offset; the current bool pre-solve callback cannot supply a full surface velocity. Requires an engine-owned constraint/solver bridge or verified backend support; stored metadata or moving/restoring a kinematic pose would not close this behavior. |
| [`property Vector2 constant_linear_velocity = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | — | Blocked | Exact trigger: a stationary-surface velocity channel must supply both normal and tangential point velocity to contact constraints while leaving body pose stationary. Box2D static contacts select a zero dummy solver state (B2ContactSolvers), and material tangentSpeed supplies only a scalar tangent offset; the current bool pre-solve callback cannot supply a full surface velocity. Requires an engine-owned constraint/solver bridge or verified backend support; stored metadata or moving/restoring a kinematic pose would not close this behavior. |
| [`property PhysicsMaterial physics_material_override`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public Electron2D.PhysicsMaterial PhysicsMaterialOverride { get; set; }`](../../classes/StaticBody.md) | Implemented | Borrowed material transfers to Box2D fixtures; live edits, packing, disposal and surface mixing verified by PhysicsMaterialTests. |
