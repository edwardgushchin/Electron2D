# TouchButtonVisibility

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum TouchButtonVisibility`. **Source:** [VirtualJoystickMode.cs](../../src/Scene/GUI/VirtualJoystickMode.cs). **Component:** [Touch game controls](../components/touch-game-controls.md).

Selects touch-button device availability policy. Its meaning differs from joystick contact visibility. Example: `var policy = TouchButtonVisibility.Always;`. Values are stored as typed scene state. Undefined values reject on assignment.

| Declaration | Value | Contract |
| --- | --- | --- |
| `Always = 0` | 0 | Draw and accept input on every host. |
| `TouchscreenOnly = 1` | 1 | Draw/accept input only when a touchscreen is available; authoring decoration is shown. |

## Enumeration descriptions

### Always

`Always = 0`. Draw and accept input on every host.

### TouchscreenOnly

`TouchscreenOnly = 1`. Draw/accept input only when a touchscreen is available; authoring decoration is shown.

Managed scene, gesture and invalid-value checks cover these domains; native drawing tests exercise their consumers. ADR 0051 keeps semantically different visibility domains distinct.
