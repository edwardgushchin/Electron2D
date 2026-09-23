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
| `All` | 2 | Can also take focus through keyboard or controller action navigation. |

The root viewport uses `All` for automatic Tab and directional traversal. An explicit focus path may also select a `Click` control. Accessibility focus and nested viewport navigation remain unimplemented.

For example, set `button.FocusMode = ControlFocusMode.All` before adding a derived control to an active viewport; it can then receive focused keyboard input and be reached by navigation. A focus transition sends Control notifications before the corresponding events, and the root Viewport exposes its current focus owner. The class remains partially implemented against the accepted GUI contract. [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs) check focus delivery and loss; [ControlFocusNavigationTests](../../tests/Electron2D.Tests/ControlFocusNavigationTests.cs) checks managed navigation.
