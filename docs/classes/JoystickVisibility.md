# JoystickVisibility

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum JoystickVisibility`. **Source:** [VirtualJoystickMode.cs](../../src/Scene/GUI/VirtualJoystickMode.cs). **Component:** [Touch game controls](../components/touch-game-controls.md).

Selects joystick decoration visibility. Example: `var policy = JoystickVisibility.Always;`. Values are stored as typed scene state. Undefined values reject on assignment.

| Declaration | Value | Contract |
| --- | --- | --- |
| `Always = 0` | 0 | Draw whenever the Control is visible. |
| `WhenTouched = 1` | 1 | Draw during contact; selected authoring mode also shows decoration. |

## Enumeration descriptions

### Always

`Always = 0`. Draw whenever the Control is visible.

### WhenTouched

`WhenTouched = 1`. Draw during contact; selected authoring mode also shows decoration.

Managed scene, gesture and invalid-value checks cover these domains; native drawing tests exercise their consumers. ADR 0051 keeps semantically different visibility domains distinct.
