# InputEventFromWindow API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/InputEventFromWindow.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventFromWindow.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public abstract class Electron2D.InputEventFromWindow`](../../classes/InputEventFromWindow.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventFromWindow`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventFromWindow.xml) | [`public abstract class Electron2D.InputEventFromWindow`](../../classes/InputEventFromWindow.md) | Implemented | The pinned XML declares an abstract InputEvent child with one integer window identifier defaulting to zero. Its C# long mapping stores and duplicates signed 64-bit values without narrowing; VerifyInputEvents checks boundaries, typed descriptor/revert and committed change delivery. The current SDL main-window adapter assigns public ID zero; multiwindow ownership and descendant behavior remain separate coverage work. |
| [`property int window_id = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventFromWindow.xml) | [`public System.Int64 WindowID { get; set; }`](../../classes/InputEventFromWindow.md) | Implemented | The pinned XML declares an abstract InputEvent child with one integer window identifier defaulting to zero. Its C# long mapping stores and duplicates signed 64-bit values without narrowing; VerifyInputEvents checks boundaries, typed descriptor/revert and committed change delivery. The current SDL main-window adapter assigns public ID zero; multiwindow ownership and descendant behavior remain separate coverage work. |
