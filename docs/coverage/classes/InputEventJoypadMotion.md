# InputEventJoypadMotion API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventJoypadMotion.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public sealed class Electron2D.InputEventJoypadMotion`](../../classes/InputEventJoypadMotion.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventJoypadMotion`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) | [`public sealed class Electron2D.InputEventJoypadMotion`](../../classes/InputEventJoypadMotion.md) | Partial | Both declared values, fixed raw-press threshold, matching and ten known text labels are audited in managed code. SDL gamepad/raw-joystick lifecycle and axis delivery pass virtual-device checks on dummy and Linux Wayland; physical axes remain unverified. A warmed native adapter probe allocated 28672 managed bytes for 128 active events and zero for 128 idle pumps, so the realtime hot-path allocation gate remains open under ADR 0014. |
| [`property int axis [JoyAxis] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) | [`public Electron2D.JoyAxis Axis { get; set; }`](../../classes/InputEventJoypadMotion.md) | Implemented | The pinned setter accepts the Invalid (-1) and Max (10) sentinels and rejects values outside them before mutation. VerifyControllerValues checks both boundaries, rejection, change count and safe unknown text; ADR 0038. |
| [`property float axis_value = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) | [`public System.Single AxisValue { get; set; }`](../../classes/InputEventJoypadMotion.md) | Implemented | The pinned setter stores arbitrary float values and derives raw pressed state from absolute magnitude >= 0.5 independently of action deadzones. VerifyControllerValues checks both threshold signs, NaN/infinity, copy, revert and observer failure after commit; ADR 0038. |
