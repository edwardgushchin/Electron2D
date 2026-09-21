# XRPositionalTracker API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/XRPositionalTracker.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [XRTracker](XRTracker.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class XRPositionalTracker`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum TrackerHand`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum_value TRACKER_HAND_LEFT [TrackerHand] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum_value TRACKER_HAND_MAX [TrackerHand] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum_value TRACKER_HAND_RIGHT [TrackerHand] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum_value TRACKER_HAND_UNKNOWN [TrackerHand] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_input(StringName name) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_pose(StringName name) -> XRPose`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method has_pose(StringName name) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method invalidate_pose(StringName name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method set_input(StringName name, Variant value) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method set_pose(StringName name, Transform3D transform, Vector3 linear_velocity, Vector3 angular_velocity, int tracking_confidence [XRPose.TrackingConfidence]) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`property int hand [XRPositionalTracker.TrackerHand] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property String profile = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal button_pressed(String action_name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal button_released(String action_name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal input_float_changed(String action_name, float value) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal input_vector2_changed(String action_name, Vector2 vector) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal pose_changed(XRPose pose) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal pose_lost_tracking(XRPose pose) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal profile_changed(String role) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/XRPositionalTracker.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
