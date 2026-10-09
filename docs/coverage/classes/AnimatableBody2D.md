# AnimatableBody2D API coverage

Last updated: 2026-10-08

Godot source: [doc/classes/AnimatableBody2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimatableBody2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [StaticBody2D](StaticBody2D.md). Electron2D type: [`public sealed class Electron2D.AnimatableBody`](../../classes/AnimatableBody.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AnimatableBody2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimatableBody2D.xml) | [`public sealed class Electron2D.AnimatableBody`](../../classes/AnimatableBody.md) | Implemented | Kinematic target velocity moves rigid contacts while fixed-step synchronization controls scene presentation; AnimatableBodyTests now runs the same mode, rotation, zero-step, callback-failure, packing, reentry and warm active/idle allocation cases on public CPU/GPU worlds (ADR 0060). |
| [`property bool sync_to_physics = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimatableBody2D.xml) | [`public System.Boolean SyncToPhysics { get; set; }`](../../classes/AnimatableBody.md) | Implemented | Kinematic target velocity moves rigid contacts while fixed-step synchronization controls scene presentation; AnimatableBodyTests now runs the same mode, rotation, zero-step, callback-failure, packing, reentry and warm active/idle allocation cases on public CPU/GPU worlds (ADR 0060). |
