# CursorShape

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum CursorShape` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Identifies standard pointer shapes for controls, input defaults and the native display. Used by [Control](Control.md), [Input](Input.md), and [DisplayServer](DisplayServer.md). `Max` is a count marker and is rejected as a selected shape.

## Values

| Value | Meaning |
| --- | --- |
| `Arrow = 0` | The default pointer arrow. |
| `IBeam = 1` | The text-selection I-beam. |
| `PointingHand = 2` | The pointing hand for links. |
| `Cross = 3` | The crosshair for precise positioning. |
| `Wait = 4` | The nonblocking wait indicator, usually paired with an arrow. |
| `Busy = 5` | The blocking wait indicator, usually replacing the arrow. |
| `Drag = 6` | The dragging hand pointer. |
| `CanDrop = 7` | The pointer indicating that a dragged item can be dropped. |
| `Forbidden = 8` | The pointer indicating that a dragged item cannot be dropped. |
| `VSize = 9` | The vertical-resize pointer. |
| `HSize = 10` | The horizontal-resize pointer. |
| `BDiagSize = 11` | The northeast-southwest diagonal-resize pointer. |
| `FDiagSize = 12` | The northwest-southeast diagonal-resize pointer. |
| `Move = 13` | The four-direction move pointer. |
| `VSplit = 14` | The vertical split-resize pointer. |
| `HSplit = 15` | The horizontal split-resize pointer. |
| `Help = 16` | The help pointer. |
| `Max = 17` | The number of pointer shapes; not a selectable shape. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. Native display behavior remains subject to the platform limits documented by its owning APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
