# InputEventGesture API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventGesture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventGesture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEventWithModifiers](InputEventWithModifiers.md). Electron2D type: [`public abstract class Electron2D.InputEventGesture`](../../classes/InputEventGesture.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventGesture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventGesture.xml) | [`public abstract class Electron2D.InputEventGesture`](../../classes/InputEventGesture.md) | Partial | The managed gesture hierarchy, stored values, inherited device default, duplication and positional transforms are executable. The current SDL host delivers touch/drag but does not create magnify/pan events. Trigger: first native touch/trackpad recognition slice must supply gesture position, pan delta, scale and window/device identity with owner-thread delivery and native checks; inherited base rows retain their own gaps. |
| [`property int device = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventGesture.xml) | [`public System.Int32 Device { get; set; }`](../../classes/InputEvent.md) | Implemented | The source overrides InputEvent device default to zero without a new storage slot. The inherited C# Device descriptor uses gesture runtime type to return and revert to zero; VerifyInputEvents checks both descendants, mutation and duplication. |
| [`property Vector2 position = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventGesture.xml) | [`public Electron2D.Vector2 Position { get; set; }`](../../classes/InputEventGesture.md) | Implemented | The pinned setter stores position directly. Gesture Position retains arbitrary source Vector2 components; VerifyInputEvents checks defaults, NaN storage and duplication. Positional XformedBy separately rejects non-finite coordinates before copying under ADR 0038. |
