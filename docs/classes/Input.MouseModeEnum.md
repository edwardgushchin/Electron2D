# Input.MouseModeEnum

Last updated: 2026-09-23

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Core/Input/Input.Pointer.cs`](../../src/Core/Input/Input.Pointer.cs)

**Declaration:** `public enum MouseModeEnum` nested in [`Input`](Input.md)

## Description

Typed pointer visibility and confinement modes used by `Input.MouseMode`. The `Enum` suffix avoids a C# name collision with that property. The values match the display mode identities; `Max` is a sentinel and cannot be selected.

## Enumeration summary

| Value | Meaning |
| --- | --- |
| `Visible = 0` | Visible and unrestricted. |
| `Hidden = 1` | Hidden and unrestricted. |
| `Captured = 2` | Hidden with relative motion. |
| `Confined = 3` | Visible and confined to the window. |
| `ConfinedHidden = 4` | Hidden and confined to the window. |
| `Max = 5` | Number of modes; invalid as a setting. |

## Lifecycle and interactions

The enum is immutable. Reading or setting `Input.MouseMode` requires an active display on its owner thread. Native errors are reported by the display; a failed mode change attempts to restore the prior mode.

## Verification and limitations

Managed tests verify numeric identities and invalid no-display calls. A targeted Linux Wayland test verifies all five selectable modes against SDL relative motion, mouse grab and visibility, plus repeated settings. Other backends are not natively verified.

## Relevant decisions

[ADR 0038](../decisions/input.md#adr-0038); see [Input coverage](../coverage/classes/Input.md).
