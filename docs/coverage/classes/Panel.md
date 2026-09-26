# Panel API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Panel.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Panel.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Control](Control.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Panel`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Panel.xml) | — | Blocked | ADR 0082: StyleBoxFlat drawing is available. Trigger: complete Theme/default-skin lookup and invalidation followed by the first panel consumer slice, with real default panel drawing and PanelContainer content-margin layout. Do not substitute a fake empty default. |
| [`theme_item StyleBox panel`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Panel.xml) | — | Blocked | ADR 0082: StyleBoxFlat drawing is available. Trigger: complete Theme/default-skin lookup and invalidation followed by the first panel consumer slice, with real default panel drawing and PanelContainer content-margin layout. Do not substitute a fake empty default. |
