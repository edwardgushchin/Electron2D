# MouseButtonMask

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum MouseButtonMask`

> Defines simultaneously held mouse-button bits.

## Description

Defines simultaneously held mouse-button bits.

- Complete values: `None`, `Left`, `Right`, `Middle`, `XButton1`, `XButton2`.
- Responsibility: simultaneous held non-wheel buttons.
- Invariants/errors: InputEventMouse rejects unknown bits. Immutable allocation-free value and thread-safe snapshots from Input.
- Verification: validation, button press/release, motion replacement, and release-all are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = MouseButtonMask.None;
```

## Constants

| Member | Description |
| --- | --- |
| [`None = 0`](#f-electron2d-mousebuttonmask-none) | No mouse button is held. |
| [`Left = 1`](#f-electron2d-mousebuttonmask-left) | The left button is held. |
| [`Right = 2`](#f-electron2d-mousebuttonmask-right) | The right button is held. |
| [`Middle = 4`](#f-electron2d-mousebuttonmask-middle) | The middle button is held. |
| [`XButton1 = 128`](#f-electron2d-mousebuttonmask-xbutton1) | The first auxiliary thumb button is held. |
| [`XButton2 = 256`](#f-electron2d-mousebuttonmask-xbutton2) | The second auxiliary thumb button is held. |

## Constant Descriptions

<a id="f-electron2d-mousebuttonmask-none"></a>
### `None = 0`

No mouse button is held.

<a id="f-electron2d-mousebuttonmask-left"></a>
### `Left = 1`

The left button is held.

<a id="f-electron2d-mousebuttonmask-right"></a>
### `Right = 2`

The right button is held.

<a id="f-electron2d-mousebuttonmask-middle"></a>
### `Middle = 4`

The middle button is held.

<a id="f-electron2d-mousebuttonmask-xbutton1"></a>
### `XButton1 = 128`

The first auxiliary thumb button is held.

<a id="f-electron2d-mousebuttonmask-xbutton2"></a>
### `XButton2 = 256`

The second auxiliary thumb button is held.
