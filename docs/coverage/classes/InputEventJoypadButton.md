# InputEventJoypadButton API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventJoypadButton.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public sealed class Electron2D.InputEventJoypadButton`](../../classes/InputEventJoypadButton.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventJoypadButton`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public sealed class Electron2D.InputEventJoypadButton`](../../classes/InputEventJoypadButton.md) | Partial | Button matching, 21 known text labels and safe numeric fallback through raw ID 127 are audited. Typed button/pressure setter edge parity and native SDL gamepad lifecycle/delivery remain on their owning controller slice under ADR 0038; do not infer whole-type parity from AsText. |
| [`property int button_index [JoyButton] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public Electron2D.JoyButton ButtonIndex { get; set; }`](../../classes/InputEventJoypadButton.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property bool pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public System.Boolean Pressed { get; set; }`](../../classes/InputEventJoypadButton.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property float pressure = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventJoypadButton.xml) | [`public System.Single Pressure { get; set; }`](../../classes/InputEventJoypadButton.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
