# BaseButton API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/BaseButton.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Control](Control.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class BaseButton`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum ActionMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum DrawMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value ACTION_MODE_BUTTON_PRESS [ActionMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value ACTION_MODE_BUTTON_RELEASE [ActionMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value DRAW_DISABLED [DrawMode] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value DRAW_HOVER [DrawMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value DRAW_HOVER_PRESSED [DrawMode] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value DRAW_NORMAL [DrawMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value DRAW_PRESSED [DrawMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _pressed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _toggled(bool toggled_on) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_draw_mode() -> int [BaseButton.DrawMode]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method is_hovered() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method set_pressed_no_signal(bool pressed) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property int action_mode [BaseButton.ActionMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property ButtonGroup button_group`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property int button_mask [MouseButtonMask] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool button_pressed = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool disabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property int focus_mode [Control.FocusMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool keep_pressed_outside = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property Shortcut shortcut`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool shortcut_feedback = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool shortcut_in_tooltip = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | Trigger: first typed 2D GUI and accessibility slice after rendering. |
| [`property bool toggle_mode = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`signal button_down() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`signal button_up() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`signal pressed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`signal toggled(bool toggled_on) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/BaseButton.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
