# JoyAxis

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum JoyAxis`

> Identifies standardized game-controller axes.

## Description

Identifies standardized game-controller axes.

- Complete values: `Invalid = -1`, `LeftX = 0`, `LeftY = 1`, `RightX = 2`, `RightY = 3`, `TriggerLeft = 4`, `TriggerRight = 5`, `SdlMax = 6`, `Max = 10`.
- Responsibility: standardized first six controller axes plus raw indices through 9; sentinels are not event values.
- Invariants/errors: event/query APIs accept only numeric `0..9`. Immutable allocation-free value.
- Verification: standardized/raw boundaries, invalid sentinels, per-device state, direction, and deadzones are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = JoyAxis.Invalid;
```

## Constants

| Member | Description |
| --- | --- |
| [`Invalid = -1`](#f-electron2d-joyaxis-invalid) | Identifies an invalid axis. |
| [`LeftX = 0`](#f-electron2d-joyaxis-leftx) | Identifies the left stick horizontal axis. |
| [`LeftY = 1`](#f-electron2d-joyaxis-lefty) | Identifies the left stick vertical axis. |
| [`RightX = 2`](#f-electron2d-joyaxis-rightx) | Identifies the right stick horizontal axis. |
| [`RightY = 3`](#f-electron2d-joyaxis-righty) | Identifies the right stick vertical axis. |
| [`TriggerLeft = 4`](#f-electron2d-joyaxis-triggerleft) | Identifies the left trigger axis. |
| [`TriggerRight = 5`](#f-electron2d-joyaxis-triggerright) | Identifies the right trigger axis. |
| [`SdlMax = 6`](#f-electron2d-joyaxis-sdlmax) | Marks the count of standardized SDL controller axes. |
| [`Max = 10`](#f-electron2d-joyaxis-max) | Marks the conventional raw axis count and an accepted event-axis sentinel. |

## Constant Descriptions

<a id="f-electron2d-joyaxis-invalid"></a>
### `Invalid = -1`

Identifies an invalid axis.

<a id="f-electron2d-joyaxis-leftx"></a>
### `LeftX = 0`

Identifies the left stick horizontal axis.

<a id="f-electron2d-joyaxis-lefty"></a>
### `LeftY = 1`

Identifies the left stick vertical axis.

<a id="f-electron2d-joyaxis-rightx"></a>
### `RightX = 2`

Identifies the right stick horizontal axis.

<a id="f-electron2d-joyaxis-righty"></a>
### `RightY = 3`

Identifies the right stick vertical axis.

<a id="f-electron2d-joyaxis-triggerleft"></a>
### `TriggerLeft = 4`

Identifies the left trigger axis.

<a id="f-electron2d-joyaxis-triggerright"></a>
### `TriggerRight = 5`

Identifies the right trigger axis.

<a id="f-electron2d-joyaxis-sdlmax"></a>
### `SdlMax = 6`

Marks the count of standardized SDL controller axes.

<a id="f-electron2d-joyaxis-max"></a>
### `Max = 10`

Marks the conventional raw axis count and an accepted event-axis sentinel. Version-one project bindings stop at axis `9`.
