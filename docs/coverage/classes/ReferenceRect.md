# ReferenceRect API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ReferenceRect.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ReferenceRect.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Control](Control.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ReferenceRect`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ReferenceRect.xml) | — | Blocked | Trigger: actual editor execution-context reporting through Engine.IsEditorHint and editor/runtime border drawing under ADRs 0015/0027/0090. The current runtime has no editor-context producer; do not introduce a constant hint or inert EditorOnly switch. |
| [`property Color border_color = Color(1, 0, 0, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ReferenceRect.xml) | — | Blocked | Trigger: actual editor execution-context reporting through Engine.IsEditorHint and editor/runtime border drawing under ADRs 0015/0027/0090. The current runtime has no editor-context producer; do not introduce a constant hint or inert EditorOnly switch. |
| [`property float border_width = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ReferenceRect.xml) | — | Blocked | Trigger: actual editor execution-context reporting through Engine.IsEditorHint and editor/runtime border drawing under ADRs 0015/0027/0090. The current runtime has no editor-context producer; do not introduce a constant hint or inert EditorOnly switch. |
| [`property bool editor_only = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ReferenceRect.xml) | — | Blocked | Trigger: actual editor execution-context reporting through Engine.IsEditorHint and editor/runtime border drawing under ADRs 0015/0027/0090. The current runtime has no editor-context producer; do not introduce a constant hint or inert EditorOnly switch. |
