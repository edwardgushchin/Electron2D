# InputEventMouse API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/InputEventMouse.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMouse.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEventWithModifiers](InputEventWithModifiers.md). Electron2D type: [`public abstract class Electron2D.InputEventMouse`](../../classes/InputEventMouse.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventMouse`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMouse.xml) | [`public abstract class Electron2D.InputEventMouse`](../../classes/InputEventMouse.md) | Partial | Typed C# type exists; inheritance, signatures and behavior require row-level audit. |
| [`property int button_mask [MouseButtonMask] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMouse.xml) | [`public Electron2D.MouseButtonMask ButtonMask { get; set; }`](../../classes/InputEventMouse.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property int device = 32`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMouse.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete InputEventMouse API slice. |
| [`property Vector2 global_position = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMouse.xml) | [`public Electron2D.Vector2 GlobalPosition { get; set; }`](../../classes/InputEventMouse.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`property Vector2 position = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMouse.xml) | [`public Electron2D.Vector2 Position { get; set; }`](../../classes/InputEventMouse.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
