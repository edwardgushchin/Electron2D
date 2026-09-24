# InputEventJoypadMotion API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventJoypadMotion.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public sealed class Electron2D.InputEventJoypadMotion`](../../classes/InputEventJoypadMotion.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventJoypadMotion`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) | [`public sealed class Electron2D.InputEventJoypadMotion`](../../classes/InputEventJoypadMotion.md) | Partial | Axis matching and all ten text labels are audited in managed code. The typed axis/value setter edge semantics and native SDL gamepad lifecycle/delivery remain on their owning controller slice under ADR 0038; do not infer whole-type parity from AsText. |
| [`property int axis [JoyAxis] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) | [`public Electron2D.JoyAxis Axis { get; set; }`](../../classes/InputEventJoypadMotion.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property float axis_value = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadMotion.xml) | [`public System.Single AxisValue { get; set; }`](../../classes/InputEventJoypadMotion.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
