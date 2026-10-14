# LinkButton.UnderlineMode

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum LinkButton.UnderlineMode`. **Source:** [LinkButton](../../src/Scene/GUI/LinkButton.cs). **Component:** [GUI buttons](../components/gui-buttons.md).

Selects the visible underline of [LinkButton](LinkButton.md).

| Value | Numeric identity | Description |
| --- | ---: | --- |
| `Always` | 0 | Normal, hovered, pressed and disabled text. |
| `OnHover` | 1 | Hovered and pressed states, including shortcut feedback. |
| `Never` | 2 | No underline. |

Unknown numeric values remain authored and use the active-state rule. The underline follows font metrics and the active font color; it adds no minimum-size padding.
