# GrooveJoint2D API coverage

Last updated: 2026-09-30

Godot source: [doc/classes/GrooveJoint2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GrooveJoint2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Joint2D](Joint2D.md). Electron2D type: [`public sealed class Electron2D.GrooveJoint`](../../classes/GrooveJoint.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class GrooveJoint2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GrooveJoint2D.xml) | [`public sealed class Electron2D.GrooveJoint`](../../classes/GrooveJoint.md) | Partial | Finite guide, free rotation and shared bias/vector correction/force caps execute under ADRs 0085/0087; physics debug drawing remains missing. |
| [`property float initial_offset = 25.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GrooveJoint2D.xml) | [`public System.Single InitialOffset { get; set; }`](../../classes/GrooveJoint.md) | Implemented | GrooveJointTests verifies actual finite sliding, rotation, signed/zero length, offset outside endpoints, live solver updates, packing, lifecycle, invalid rollback and warmed zero managed allocation under ADR 0085. |
| [`property float length = 50.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GrooveJoint2D.xml) | [`public System.Single Length { get; set; }`](../../classes/GrooveJoint.md) | Implemented | GrooveJointTests verifies actual finite sliding, rotation, signed/zero length, offset outside endpoints, live solver updates, packing, lifecycle, invalid rollback and warmed zero managed allocation under ADR 0085. |
