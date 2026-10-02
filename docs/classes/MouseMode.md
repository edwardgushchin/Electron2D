# MouseMode

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum MouseMode` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Defines pointer visibility, capture and window confinement. Used by [Input](Input.md) and [DisplayServer](DisplayServer.md). `Max` is a count marker and is rejected as a selected mode.

## Values

| Value | Meaning |
| --- | --- |
| `Visible = 0` | The pointer is visible and free to leave the window. |
| `Hidden = 1` | The pointer is hidden and free to leave the window. |
| `Captured = 2` | The pointer is hidden, captured, and reports relative motion. |
| `Confined = 3` | The visible pointer is confined to the main window. |
| `ConfinedHidden = 4` | The hidden pointer is confined to the main window. |
| `Max = 5` | The number of pointer modes; not a selectable mode. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. Native display behavior remains subject to the platform limits documented by its owning APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
