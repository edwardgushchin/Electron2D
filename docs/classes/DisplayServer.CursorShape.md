# DisplayServer.CursorShape

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Pointer.cs`](../../src/Servers/Display/DisplayServer.Pointer.cs)

**Declaration:** `public enum CursorShape` nested in [`DisplayServer`](DisplayServer.md)

## Description

Stable cursor shape identifiers for `CursorSetShape` and `CursorSetCustomImage`. All standard shapes map to SDL system cursors. The native theme may render a mapped shape differently; `Max` counts slots and cannot be selected.

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`Arrow = 0`](#value-arrow) | The default pointer arrow. |
| [`IBeam = 1`](#value-ibeam) | The text-selection I-beam. |
| [`PointingHand = 2`](#value-pointinghand) | The pointing hand for links. |
| [`Cross = 3`](#value-cross) | The crosshair for precise positioning. |
| [`Wait = 4`](#value-wait) | The nonblocking wait indicator, usually paired with an arrow. |
| [`Busy = 5`](#value-busy) | The blocking wait indicator, usually replacing the arrow. |
| [`Drag = 6`](#value-drag) | The dragging hand pointer. |
| [`CanDrop = 7`](#value-candrop) | The pointer indicating that a dragged item can be dropped. |
| [`Forbidden = 8`](#value-forbidden) | The pointer indicating that a dragged item cannot be dropped. |
| [`VSize = 9`](#value-vsize) | The vertical-resize pointer. |
| [`HSize = 10`](#value-hsize) | The horizontal-resize pointer. |
| [`BDiagSize = 11`](#value-bdiagsize) | The northeast-southwest diagonal-resize pointer. |
| [`FDiagSize = 12`](#value-fdiagsize) | The northwest-southeast diagonal-resize pointer. |
| [`Move = 13`](#value-move) | The four-direction move pointer. |
| [`VSplit = 14`](#value-vsplit) | The vertical split-resize pointer. |
| [`HSplit = 15`](#value-hsplit) | The horizontal split-resize pointer. |
| [`Help = 16`](#value-help) | The help pointer. |
| [`Max = 17`](#value-max) | The number of pointer shapes; not a selectable shape. |

## Enumeration Descriptions

<a id="value-arrow"></a>
### `Arrow = 0`

The default pointer arrow.

<a id="value-ibeam"></a>
### `IBeam = 1`

The text-selection I-beam.

<a id="value-pointinghand"></a>
### `PointingHand = 2`

The pointing hand for links.

<a id="value-cross"></a>
### `Cross = 3`

The crosshair for precise positioning.

<a id="value-wait"></a>
### `Wait = 4`

The nonblocking wait indicator, usually paired with an arrow.

<a id="value-busy"></a>
### `Busy = 5`

The blocking wait indicator, usually replacing the arrow.

<a id="value-drag"></a>
### `Drag = 6`

The dragging hand pointer.

<a id="value-candrop"></a>
### `CanDrop = 7`

The pointer indicating that a dragged item can be dropped.

<a id="value-forbidden"></a>
### `Forbidden = 8`

The pointer indicating that a dragged item cannot be dropped.

<a id="value-vsize"></a>
### `VSize = 9`

The vertical-resize pointer.

<a id="value-hsize"></a>
### `HSize = 10`

The horizontal-resize pointer.

<a id="value-bdiagsize"></a>
### `BDiagSize = 11`

The northeast-southwest diagonal-resize pointer.

<a id="value-fdiagsize"></a>
### `FDiagSize = 12`

The northwest-southeast diagonal-resize pointer.

<a id="value-move"></a>
### `Move = 13`

The four-direction move pointer.

<a id="value-vsplit"></a>
### `VSplit = 14`

The vertical split-resize pointer.

<a id="value-hsplit"></a>
### `HSplit = 15`

The horizontal split-resize pointer.

<a id="value-help"></a>
### `Help = 16`

The help pointer.

<a id="value-max"></a>
### `Max = 17`

The number of pointer shapes; not a selectable shape.

## Lifecycle and invariants

Values are immutable. `Max` and unknown numeric values are rejected before any native mutation. Cursor slot images are owned and released by `DisplayServer`.

## Threading and interactions

The enum itself has no thread affinity; native operations require the opening SDL main thread.

## Verification and limitations

The SDL dummy-driver harness checks numeric identities and rejects the terminal marker. Visible cursor appearance and platform capture behavior have not been verified.

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040); see the [coverage inventory](../coverage/classes/DisplayServer.md).
