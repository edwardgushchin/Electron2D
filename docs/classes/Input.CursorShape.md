# Input.CursorShape

Last updated: 2026-09-23

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Core/Input/Input.Pointer.cs`](../../src/Core/Input/Input.Pointer.cs)

**Declaration:** `public enum CursorShape` nested in [`Input`](Input.md)

## Description

Standard native cursor identities used by `Input.GetCurrentCursorShape`, `Input.SetDefaultCursorShape` and `Input.SetCustomMouseCursor`. Native themes may draw mapped shapes differently. There is no selectable terminal marker.

## Enumeration summary

| Value | Meaning |
| --- | --- |
| `Arrow = 0` | Ordinary arrow. |
| `IBeam = 1` | Text selection. |
| `PointingHand = 2` | Clickable link. |
| `Cross = 3` | Crosshair. |
| `Wait = 4` | Nonblocking wait. |
| `Busy = 5` | Blocking wait. |
| `Drag = 6` | Drag. |
| `CanDrop = 7` | Drop allowed. |
| `Forbidden = 8` | Drop forbidden. |
| `VSize = 9` | Vertical resize. |
| `HSize = 10` | Horizontal resize. |
| `BDiagSize = 11` | Northeast-southwest resize. |
| `FDiagSize = 12` | Northwest-southeast resize. |
| `Move = 13` | Move in any direction. |
| `VSplit = 14` | Vertical split resize. |
| `HSplit = 15` | Horizontal split resize. |
| `Help = 16` | Help. |

## Lifecycle and interactions

The enum is immutable. The active display owns the current native shape and custom image slots. Input retains the viewport default separately so a hovered Control can override it. Selecting an unknown numeric value fails before native mutation.

## Verification and limitations

The targeted Linux Wayland test selects all 17 values through Input and reads back the native cursor, including a shape set directly through DisplayServer. A separate native scene check covers Control precedence. Other platform cursor themes have not been natively checked.

## Relevant decisions

[ADR 0038](../decisions/input.md#adr-0038); see [Input coverage](../coverage/classes/Input.md).
