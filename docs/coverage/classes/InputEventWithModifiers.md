# InputEventWithModifiers API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/InputEventWithModifiers.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEventFromWindow](InputEventFromWindow.md). Electron2D type: [`public abstract class Electron2D.InputEventWithModifiers`](../../classes/InputEventWithModifiers.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventWithModifiers`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public abstract class Electron2D.InputEventWithModifiers`](../../classes/InputEventWithModifiers.md) | Partial | Typed C# type exists; inheritance, signatures and behavior require row-level audit. |
| [`method get_modifiers_mask() -> int [KeyModifierMask]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public Electron2D.KeyModifierMask GetModifiersMask()`](../../classes/InputEventWithModifiers.md) | Partial | Typed derived input-event query/override; ADR 0038. |
| [`method is_command_or_control_pressed() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public System.Boolean IsCommandOrControlPressed()`](../../classes/InputEventWithModifiers.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property bool alt_pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public System.Boolean AltPressed { get; set; }`](../../classes/InputEventWithModifiers.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property bool command_or_control_autoremap = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public System.Boolean CommandOrControlAutoremap { get; set; }`](../../classes/InputEventWithModifiers.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property bool ctrl_pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public System.Boolean ControlPressed { get; set; }`](../../classes/InputEventWithModifiers.md) | Partial | Spelled-out C# control modifier corresponds to ctrl_pressed; ADR 0038. |
| [`property int device = 16`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete InputEventWithModifiers API slice. |
| [`property bool meta_pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public System.Boolean MetaPressed { get; set; }`](../../classes/InputEventWithModifiers.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property bool shift_pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventWithModifiers.xml) | [`public System.Boolean ShiftPressed { get; set; }`](../../classes/InputEventWithModifiers.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
