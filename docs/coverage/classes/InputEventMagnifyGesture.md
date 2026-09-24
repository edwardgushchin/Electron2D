# InputEventMagnifyGesture API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventMagnifyGesture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMagnifyGesture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEventGesture](InputEventGesture.md). Electron2D type: [`public sealed class Electron2D.InputEventMagnifyGesture`](../../classes/InputEventMagnifyGesture.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventMagnifyGesture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMagnifyGesture.xml) | [`public sealed class Electron2D.InputEventMagnifyGesture`](../../classes/InputEventMagnifyGesture.md) | Partial | The managed gesture hierarchy, stored values, inherited device default, duplication, positional transforms and localized text execute with the shared numeric-format audit. The current SDL host delivers touch/drag but does not create magnify/pan events. Trigger: first native touch/trackpad recognition slice must supply gesture position, pan delta, scale and window/device identity with owner-thread delivery and native checks; inherited base rows retain their own gaps. |
| [`property float factor = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventMagnifyGesture.xml) | [`public System.Single Factor { get; set; }`](../../classes/InputEventMagnifyGesture.md) | Implemented | The pinned setter stores its real factor without a positivity or finiteness restriction. The C# float property defaults to one and retains negative, zero, infinity and NaN; VerifyInputEvents checks committed change delivery and duplication. Native producer remains on the type row. |
