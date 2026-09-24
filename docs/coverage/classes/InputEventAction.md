# InputEventAction API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventAction.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventAction.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public sealed class Electron2D.InputEventAction`](../../classes/InputEventAction.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventAction`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventAction.xml) | [`public sealed class Electron2D.InputEventAction`](../../classes/InputEventAction.md) | Partial | All four declared value rows and direct action source identity are audited under ADR 0038. AsText selects the first concrete binding or falls back to the action name, but that concrete event's localized wording remains Partial on the inherited InputEvent::as_text row; do not infer full type parity from value storage. |
| [`property StringName action = &""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventAction.xml) | [`public System.String Action { get; set; }`](../../classes/InputEventAction.md) | Implemented | VerifyInputEventActionValues checks empty/ordinal names, null rejection, notifications, registered-binding invalidation, typed descriptors, revert and duplication. Direct IsAction name comparison is audited on the inherited row under ADR 0038. |
| [`property int event_index = -1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventAction.xml) | [`public System.Int32 EventIndex { get; set; }`](../../classes/InputEventAction.md) | Implemented | The pinned setter retains signed indexes, including int.MinValue and int.MaxValue, with typed copy/revert. Negative indexes select the post-binding slot; VerifyInput checks the 32-source rejection before dispatch mutation and two explicit indexes whose press/release contributions remain independent by action, device and index under ADR 0038. |
| [`property bool pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventAction.xml) | [`public System.Boolean Pressed { get; set; }`](../../classes/InputEventAction.md) | Implemented | The false default, raw press/release transitions, typed descriptor revert/copy and a logically pressed zero-strength source are checked by VerifyInputEventActionValues and VerifyInput under ADR 0038. |
| [`property float strength = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventAction.xml) | [`public System.Single Strength { get; set; }`](../../classes/InputEventAction.md) | Implemented | The 1.0 default, finite clamp to [0,1], NaN/infinity rejection before mutation, copy/revert and zero effective strength on release are checked by VerifyInputEventActionValues, VerifyInputMapMatching and indexed VerifyInput sources under ADR 0038. |
