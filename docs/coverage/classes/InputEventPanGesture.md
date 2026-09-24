# InputEventPanGesture API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventPanGesture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventPanGesture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEventGesture](InputEventGesture.md). Electron2D type: [`public sealed class Electron2D.InputEventPanGesture`](../../classes/InputEventPanGesture.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventPanGesture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventPanGesture.xml) | [`public sealed class Electron2D.InputEventPanGesture`](../../classes/InputEventPanGesture.md) | Partial | The managed gesture hierarchy, stored values, inherited device default, duplication, positional transforms and representative localized text are executable; exact all-float text rounding remains Partial. The current SDL host delivers touch/drag but does not create magnify/pan events. Trigger: first native touch/trackpad recognition slice must supply gesture position, pan delta, scale and window/device identity with owner-thread delivery and native checks; inherited base rows retain their own gaps. |
| [`property Vector2 delta = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventPanGesture.xml) | [`public Electron2D.Vector2 Delta { get; set; }`](../../classes/InputEventPanGesture.md) | Implemented | The pinned setter stores its Vector2 delta directly. The C# property defaults to zero and retains non-finite source components; VerifyInputEvents checks duplication, while CanvasCoordinateTests checks that XformedBy preserves pan delta. Native producer remains on the type row. |
