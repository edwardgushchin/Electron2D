# InputEventJoypadButton API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventJoypadButton.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public sealed class Electron2D.InputEventJoypadButton`](../../classes/InputEventJoypadButton.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventJoypadButton`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public sealed class Electron2D.InputEventJoypadButton`](../../classes/InputEventJoypadButton.md) | Partial | All three declared values, button matching, 21 known text labels, arbitrary signed IDs and non-finite pressure are audited in managed code. SDL gamepad/raw-joystick lifecycle and button delivery pass virtual-device checks on dummy and Linux Wayland; physical buttons remain unverified. A warmed native adapter probe allocated 28672 managed bytes for 128 active events and zero for 128 idle pumps, so the realtime hot-path allocation gate remains open under ADR 0014. |
| [`property int button_index [JoyButton] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public Electron2D.JoyButton ButtonIndex { get; set; }`](../../classes/InputEventJoypadButton.md) | Implemented | The pinned setter stores arbitrary signed button identities and emits Changed. VerifyControllerValues checks int limits, safe text, copy and notification; native virtual-device delivery now runs while physical-device verification remains on the type row under ADR 0038. |
| [`property bool pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public System.Boolean Pressed { get; set; }`](../../classes/InputEventJoypadButton.md) | Implemented | The pinned setter stores the independent press flag without emitting Changed. VerifyControllerValues checks default, press/release, copy and no extra content notification; ADR 0038. |
| [`property float pressure = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public System.Single Pressure { get; set; }`](../../classes/InputEventJoypadButton.md) | Implemented | The pinned setter stores arbitrary float pressure without emitting Changed. VerifyControllerValues checks negative, NaN, infinity, copy, revert, text and notification behavior; ADR 0038. |
