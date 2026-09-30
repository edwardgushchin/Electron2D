# PinJoint2D API coverage

Last updated: 2026-09-30

Godot source: [doc/classes/PinJoint2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Joint2D](Joint2D.md). Electron2D type: [`public sealed class Electron2D.PinJoint`](../../classes/PinJoint.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PinJoint2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | [`public sealed class Electron2D.PinJoint`](../../classes/PinJoint.md) | Partial | Scene-owned pin constraints, path resolution, collision policy, angular limits and motor execute under ADR 0084; Stable shared RID executes under ADR 0087; positional bias and pin-anchor softness retain exact blocked solver prerequisites. |
| [`property bool angular_limit_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | [`public System.Boolean AngularLimitEnabled { get; set; }`](../../classes/PinJoint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property float angular_limit_lower = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | [`public System.Single AngularLimitLower { get; set; }`](../../classes/PinJoint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property float angular_limit_upper = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | [`public System.Single AngularLimitUpper { get; set; }`](../../classes/PinJoint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property bool motor_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | [`public System.Boolean MotorEnabled { get; set; }`](../../classes/PinJoint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property float motor_target_velocity = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | [`public System.Single MotorTargetVelocity { get; set; }`](../../classes/PinJoint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property float softness = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PinJoint2D.xml) | — | Blocked | Trigger: linear pin-anchor compliance mapping in Box2D; its revolute spring controls angle rather than the reference's positional softness (ADR 0061). |
