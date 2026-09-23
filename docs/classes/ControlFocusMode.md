# ControlFocusMode

Last updated: 2026-09-23

- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlFocusMode`
- **Source:** [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs)
- **Used by:** [Control.FocusMode](Control.md)

## Description

Controls whether an attached, visible Control can become the single keyboard input target of its scene tree. The enum is stored by `Control`.

| Value | Number | Behavior |
| --- | ---: | --- |
| `None` | 0 | Cannot take focus; default. |
| `Click` | 1 | Can take focus on a left-button press or `GrabFocus`. |

Keyboard/gamepad focus navigation and the corresponding All mode remain unimplemented.

For example, set `button.FocusMode = ControlFocusMode.Click` before adding a derived control to an active viewport; it can then receive focused keyboard input. The class remains partially implemented against the accepted GUI contract. [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs) check current focus delivery and loss.
