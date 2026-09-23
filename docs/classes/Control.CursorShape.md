# Control.CursorShape

Last updated: 2026-09-23

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Scene/GUI/Control.Input.cs`](../../src/Scene/GUI/Control.Input.cs)

**Declaration:** `public enum CursorShape` nested in [`Control`](Control.md)

## Description

Cursor identities for `Control.MouseDefaultCursorShape` and the typed `OnGetCursorShape` override. Their numeric values match [`Input.CursorShape`](Input.CursorShape.md), so a hovered control can select the active display's native cursor.

## Enumeration summary

| Value | Meaning |
| --- | --- |
| `Arrow = 0` | Ordinary pointer. |
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

## Lifecycle and verification

Values are immutable. An undefined value is rejected before storing it or selecting a native shape. Managed tests check the stored property, override and packed-scene state; a targeted Linux Wayland run checks native selection. Other platforms and full GUI ordering remain unverified. See [Control coverage](../coverage/classes/Control.md) and [ADR 0038](../decisions/input.md#adr-0038).
