# InputEventShortcut API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/InputEventShortcut.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventShortcut.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [InputEvent](InputEvent.md). Electron2D type: [`public sealed class Electron2D.InputEventShortcut`](../../classes/InputEventShortcut.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InputEventShortcut`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventShortcut.xml) | [`public sealed class Electron2D.InputEventShortcut`](../../classes/InputEventShortcut.md) | Implemented | ADR 0038: a real pressed non-action-binding event borrows Shortcut, routes through the scene shortcut stage and retains exact resource identity in event copies. ShortcutTests cover null/live/disposed references, matching, descriptions and shallow/deep ownership. Inherited InputEvent methods remain accounted on their declaring class. |
| [`property Shortcut shortcut`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InputEventShortcut.xml) | [`public Electron2D.Shortcut Shortcut { get; set; }`](../../classes/InputEventShortcut.md) | Implemented | ADR 0038: a real pressed non-action-binding event borrows Shortcut, routes through the scene shortcut stage and retains exact resource identity in event copies. ShortcutTests cover null/live/disposed references, matching, descriptions and shallow/deep ownership. Inherited InputEvent methods remain accounted on their declaring class. |
