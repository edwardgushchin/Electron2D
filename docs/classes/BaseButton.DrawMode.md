# BaseButton.DrawMode

Last updated: 2026-09-27

**Declaration:** `public enum BaseButton.DrawMode` · **Source:** [BaseButton.cs](../../src/Scene/GUI/BaseButton.cs)

Returned by [BaseButton.GetDrawMode](BaseButton.md#getdrawmode) for concrete decoration.

| Member | Value | Behavior |
| --- | ---: | --- |
| Normal | 0 | Not currently drawing pressed or hovered. |
| Pressed | 1 | Drawing pressed without hover decoration. |
| Hover | 2 | Idle hover without selection. |
| Disabled | 3 | Disabled interaction; highest priority. |
| HoverPressed | 4 | Idle selected hover or active shortcut highlight. |

## Member descriptions

### Normal

No pressed decoration, including the inverse preview while holding an already selected toggle.

### Pressed

Persistent selection without hover or a live inside/KeepPressedOutside attempt, inverted by persistent toggle state.

### Hover

The pointer hovers while no attempt and no persistent selection are active.

### Disabled

Disabled takes precedence over persistent selection and shortcut highlight.

### HoverPressed

A selected button is hovered with no live attempt, or shortcut highlight remains active. Highlight does not mutate ButtonPressed.
