# SkeletonModification2DPhysicalBones API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/SkeletonModification2DPhysicalBones.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [SkeletonModification2D](SkeletonModification2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SkeletonModification2DPhysicalBones`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
| [`method fetch_physical_bones() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
| [`method get_physical_bone_node(int joint_idx) -> NodePath`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
| [`method set_physical_bone_node(int joint_idx, NodePath physicalbone2d_node) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
| [`method start_simulation(StringName[] bones = []) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
| [`method stop_simulation(StringName[] bones = []) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
| [`property int physical_bone_chain_length = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DPhysicalBones.xml) | — | Blocked | Trigger: actual PhysicalBone/PhysicsBody synchronization, simulation selection/start/stop, joint ownership and native physics-driven skeletal output; the concrete IK resources do not supply physical bones (ADRs 0028/0092). |
