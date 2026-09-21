# MouseButton

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum MouseButton`

> Identifies mouse buttons and wheel directions.

## Description

Identifies mouse buttons and wheel directions.

- Complete values: `None = 0`, `Left = 1`, `Right = 2`, `Middle = 3`, `WheelUp = 4`, `WheelDown = 5`, `WheelLeft = 6`, `WheelRight = 7`, `XButton1 = 8`, `XButton2 = 9`.
- Responsibility: identifies one button or wheel direction. Wheel values are transient events, not held-mask bits.
- Invariants/errors: InputEventMouseButton rejects undefined assignments. Immutable allocation-free value.
- Verification: matching, held-mask conversion, wheel exclusion, text, and invalid values are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = MouseButton.None;
```

## Constants

| Member | Description |
| --- | --- |
| [`None = 0`](#f-electron2d-mousebutton-none) | Identifies no button. |
| [`Left = 1`](#f-electron2d-mousebutton-left) | Identifies the primary left button. |
| [`Right = 2`](#f-electron2d-mousebutton-right) | Identifies the secondary right button. |
| [`Middle = 3`](#f-electron2d-mousebutton-middle) | Identifies the middle button. |
| [`WheelUp = 4`](#f-electron2d-mousebutton-wheelup) | Identifies an upward wheel step. |
| [`WheelDown = 5`](#f-electron2d-mousebutton-wheeldown) | Identifies a downward wheel step. |
| [`WheelLeft = 6`](#f-electron2d-mousebutton-wheelleft) | Identifies a leftward wheel step. |
| [`WheelRight = 7`](#f-electron2d-mousebutton-wheelright) | Identifies a rightward wheel step. |
| [`XButton1 = 8`](#f-electron2d-mousebutton-xbutton1) | Identifies the first auxiliary thumb button. |
| [`XButton2 = 9`](#f-electron2d-mousebutton-xbutton2) | Identifies the second auxiliary thumb button. |

## Constant Descriptions

<a id="f-electron2d-mousebutton-none"></a>
### `None = 0`

Identifies no button.

<a id="f-electron2d-mousebutton-left"></a>
### `Left = 1`

Identifies the primary left button.

<a id="f-electron2d-mousebutton-right"></a>
### `Right = 2`

Identifies the secondary right button.

<a id="f-electron2d-mousebutton-middle"></a>
### `Middle = 3`

Identifies the middle button.

<a id="f-electron2d-mousebutton-wheelup"></a>
### `WheelUp = 4`

Identifies an upward wheel step.

<a id="f-electron2d-mousebutton-wheeldown"></a>
### `WheelDown = 5`

Identifies a downward wheel step.

<a id="f-electron2d-mousebutton-wheelleft"></a>
### `WheelLeft = 6`

Identifies a leftward wheel step.

<a id="f-electron2d-mousebutton-wheelright"></a>
### `WheelRight = 7`

Identifies a rightward wheel step.

<a id="f-electron2d-mousebutton-xbutton1"></a>
### `XButton1 = 8`

Identifies the first auxiliary thumb button.

<a id="f-electron2d-mousebutton-xbutton2"></a>
### `XButton2 = 9`

Identifies the second auxiliary thumb button.
