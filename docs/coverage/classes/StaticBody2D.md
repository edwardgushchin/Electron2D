# StaticBody2D API coverage

Last updated: 2026-10-08

Godot source: [doc/classes/StaticBody2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [PhysicsBody2D](PhysicsBody2D.md). Electron2D type: [`public class Electron2D.StaticBody`](../../classes/StaticBody.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class StaticBody2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public class Electron2D.StaticBody`](../../classes/StaticBody.md) | Implemented | Own static-body surface members execute: material override and stored linear/angular virtual velocity, with CPU/GPU contacts, point queries, character carry, packing and lifecycle verified by PhysicsSurfaceVelocityTests. Inherited gaps remain on their declaring pages. |
| [`property float constant_angular_velocity = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public System.Single ConstantAngularVelocity { get; set; }`](../../classes/StaticBody.md) | Implemented | Finite stored surface velocity drives CPU scalar/SIMD and GPU contacts without pose integration; inherited kinematic target motion is additive. Point/contact queries, character carry, wakeup, packing, reentry and zero all-thread warmed allocation are covered on public CPU/GPU worlds and the historical stage host by PhysicsSurfaceVelocityTests (ADR 0075). |
| [`property Vector2 constant_linear_velocity = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public Electron2D.Vector2 ConstantLinearVelocity { get; set; }`](../../classes/StaticBody.md) | Implemented | Finite stored surface velocity drives CPU scalar/SIMD and GPU contacts without pose integration; inherited kinematic target motion is additive. Point/contact queries, character carry, wakeup, packing, reentry and zero all-thread warmed allocation are covered on public CPU/GPU worlds and the historical stage host by PhysicsSurfaceVelocityTests (ADR 0075). |
| [`property PhysicsMaterial physics_material_override`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StaticBody2D.xml) | [`public Electron2D.PhysicsMaterial PhysicsMaterialOverride { get; set; }`](../../classes/StaticBody.md) | Implemented | Borrowed material transfers to Box2D fixtures; live edits, packing, disposal and surface mixing verified by PhysicsMaterialTests. |
