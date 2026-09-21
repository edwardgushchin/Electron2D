# AnimationNodeBlendTree API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/AnimationNodeBlendTree.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AnimationRootNode](AnimationRootNode.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AnimationNodeBlendTree`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`constant CONNECTION_ERROR_CONNECTION_EXISTS = 5`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`constant CONNECTION_ERROR_NO_INPUT = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`constant CONNECTION_ERROR_NO_INPUT_INDEX = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`constant CONNECTION_ERROR_NO_OUTPUT = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`constant CONNECTION_ERROR_SAME_NODE = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`constant CONNECTION_OK = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method add_node(StringName name, AnimationNode node, Vector2 position = Vector2(0, 0)) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method connect_node(StringName input_node, int input_index, StringName output_node) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method disconnect_node(StringName input_node, int input_index) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_node(StringName name) -> AnimationNode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_node_list() -> StringName[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_node_position(StringName name) -> Vector2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method has_node(StringName name) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method remove_node(StringName name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method rename_node(StringName name, StringName new_name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method set_node_position(StringName name, Vector2 position) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property Vector2 graph_offset = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`signal node_changed(StringName node_name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeBlendTree.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
