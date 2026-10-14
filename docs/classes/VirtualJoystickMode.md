# VirtualJoystickMode

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum VirtualJoystickMode`. **Source:** [VirtualJoystickMode.cs](../../src/Scene/GUI/VirtualJoystickMode.cs). **Component:** [Touch game controls](../components/touch-game-controls.md).

Selects base placement for VirtualJoystick. Example: `var policy = VirtualJoystickMode.Fixed;`. Values are stored as typed scene state. Undefined values reject on assignment.

| Declaration | Value | Contract |
| --- | --- | --- |
| `Fixed = 0` | 0 | Base remains at authored offset; initial contact must hit its rectangle. |
| `Dynamic = 1` | 1 | Base starts at accepted contact. |
| `Following = 2` | 2 | Base follows contact beyond clamp within the control. |

## Enumeration descriptions

### Fixed

`Fixed = 0`. Base remains at authored offset; initial contact must hit its rectangle.

### Dynamic

`Dynamic = 1`. Base starts at accepted contact.

### Following

`Following = 2`. Base follows contact beyond clamp within the control.

Managed scene, gesture and invalid-value checks cover these domains; native drawing tests exercise their consumers. ADR 0051 keeps semantically different visibility domains distinct.
