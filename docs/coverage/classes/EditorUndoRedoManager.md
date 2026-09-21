# EditorUndoRedoManager API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/EditorUndoRedoManager.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class EditorUndoRedoManager`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`enum SpecialHistory`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`enum_value GLOBAL_HISTORY [SpecialHistory] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`enum_value INVALID_HISTORY [SpecialHistory] = -99`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`enum_value REMOTE_HISTORY [SpecialHistory] = -9`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method add_do_method(Object object, StringName method) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method add_do_property(Object object, StringName property, Variant value) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method add_do_reference(Object object) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method add_undo_method(Object object, StringName method) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method add_undo_property(Object object, StringName property, Variant value) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method add_undo_reference(Object object) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method clear_history(int id = -99, bool increase_version = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method commit_action(bool execute = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method create_action(String name, int merge_mode [UndoRedo.MergeMode] = 0, Object custom_context = null, bool backward_undo_ops = false, bool mark_unsaved = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method force_fixed_history() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method get_history_undo_redo(int id) -> UndoRedo`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method get_object_history_id(Object object) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`method is_committing_action() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`signal history_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
| [`signal version_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/EditorUndoRedoManager.xml) | — | Blocked | Trigger: first self-hosted editor executable slice under ADR 0027. |
