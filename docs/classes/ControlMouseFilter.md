# ControlMouseFilter

Last updated: 2026-09-23

- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlMouseFilter`
- **Source:** [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs)
- **Used by:** [Control.MouseFilter](Control.md)

## Description

Controls pointer targeting and bubbling in a root viewport. The enum is stored by `Control` and has the numeric identities of the corresponding UI filter modes.

| Value | Number | Behavior |
| --- | ---: | --- |
| `Stop` | 0 | Receives pointer input and handles it automatically, except wheel events when the wheel-pass property is enabled. Default. |
| `Pass` | 1 | Receives pointer input and bubbles it through direct Control ancestors until handled. |
| `Ignore` | 2 | Receives no pointer events and does not obstruct lower controls. |

For example, set `overlay.MouseFilter = ControlMouseFilter.Ignore` to let a control underneath receive a click. This example assumes `overlay` is an attached Control.

Root viewport dispatch is covered by [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs). Hover, clipping, exact renderer order and nested viewport behavior remain incomplete; see [Control coverage](../coverage/classes/Control.md).
