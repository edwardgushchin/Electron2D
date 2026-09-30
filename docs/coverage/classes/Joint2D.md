# Joint2D API coverage

Last updated: 2026-09-30

Godot source: [doc/classes/Joint2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public abstract class Electron2D.Joint`](../../classes/Joint.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Joint2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | [`public abstract class Electron2D.Joint`](../../classes/Joint.md) | Partial | Scene-owned pin constraints, path resolution, collision policy, angular limits and motor execute under ADR 0084; Stable shared RID executes under ADR 0087; positional bias and pin-anchor softness retain exact blocked solver prerequisites. |
| [`method get_rid() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | [`public Electron2D.RID GetRID()`](../../classes/Joint.md) | Implemented | ADR 0087: shared scene/server RID, body-local frames and actual native pin/groove/spring kernels; PhysicsServerJointTests checks defaults, response, bidirectional settings, clear/replacement, pending/foreign/replacement worlds, lifetime/phase/thread rejection and warmed zero managed allocation. Pin permits an empty second body; groove/spring require two. Empty=3 adapts the actual unconfigured MAX return, not an additional solver role. Raw spring factory damping is 1.5; scene damping is 1 and automatic zero-rest differs from literal server zero. |
| [`property float bias = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | — | Blocked | Trigger: verified per-joint positional bias mapping in Box2D's constraint solver; no equivalent public revolute parameter is present (ADR 0061). |
| [`property bool disable_collision = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | [`public System.Boolean DisableCollision { get; set; }`](../../classes/Joint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property NodePath node_a = NodePath("")`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | [`public System.String NodeA { get; set; }`](../../classes/Joint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
| [`property NodePath node_b = NodePath("")`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Joint2D.xml) | [`public System.String NodeB { get; set; }`](../../classes/Joint.md) | Implemented | PinJointTests checks actual constraints, typed path resolution, connected-body contacts, angular limits, motor speed and torque, live edits, tree exit/reentry, packing and warmed zero managed allocation under ADR 0084. |
