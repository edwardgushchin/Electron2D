# UndoRedo API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/UndoRedo.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class UndoRedo`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`enum MergeMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`enum_value MERGE_ALL [MergeMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`enum_value MERGE_DISABLE [MergeMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`enum_value MERGE_ENDS [MergeMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method add_do_method(Callable callable) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method add_do_property(Object object, StringName property, Variant value) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method add_do_reference(Object object) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method add_undo_method(Callable callable) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method add_undo_property(Object object, StringName property, Variant value) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method add_undo_reference(Object object) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method clear_history(bool increase_version = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method commit_action(bool execute = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method create_action(String name, int merge_mode [UndoRedo.MergeMode] = 0, bool backward_undo_ops = false) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method end_force_keep_in_merge_ends() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method get_action_name(int id) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method get_current_action() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method get_current_action_name() -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method get_history_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method get_version() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method has_redo() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method has_undo() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method is_committing_action() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method redo() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method start_force_keep_in_merge_ends() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`method undo() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`property int max_steps = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
| [`signal version_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/UndoRedo.xml) | — | Blocked | Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). |
