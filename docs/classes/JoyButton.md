# JoyButton

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum JoyButton`

> Identifies standardized and extended game-controller buttons.

## Description

Identifies standardized and extended game-controller buttons.

- Complete values: `Invalid = -1`, `A`, `B`, `X`, `Y`, `Back`, `Guide`, `Start`, `LeftStick`, `RightStick`, `LeftShoulder`, `RightShoulder`, `DpadUp`, `DpadDown`, `DpadLeft`, `DpadRight`, `Misc1`, `Paddle1`, `Paddle2`, `Paddle3`, `Paddle4`, `Touchpad`, `Misc2`, `Misc3`, `Misc4`, `Misc5`, `Misc6`, `SdlMax = 26`, `Max = 128`.
- Responsibility: standardized controller buttons plus raw numeric indices through 127; sentinels are not event values.
- Invariants/errors: event/query APIs accept only numeric `0..127`. Immutable allocation-free value.
- Verification: standardized/raw boundaries, invalid sentinels, per-device held state, matching, and release-all are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = JoyButton.Invalid;
```

## Constants

| Member | Description |
| --- | --- |
| [`Invalid = -1`](#f-electron2d-joybutton-invalid) | Identifies an invalid button. |
| [`A = 0`](#f-electron2d-joybutton-a) | Identifies the bottom face button. |
| [`B = 1`](#f-electron2d-joybutton-b) | Identifies the right face button. |
| [`X = 2`](#f-electron2d-joybutton-x) | Identifies the left face button. |
| [`Y = 3`](#f-electron2d-joybutton-y) | Identifies the top face button. |
| [`Back = 4`](#f-electron2d-joybutton-back) | Identifies Back or Select. |
| [`Guide = 5`](#f-electron2d-joybutton-guide) | Identifies the guide or home button. |
| [`Start = 6`](#f-electron2d-joybutton-start) | Identifies Start. |
| [`LeftStick = 7`](#f-electron2d-joybutton-leftstick) | Identifies the left-stick button. |
| [`RightStick = 8`](#f-electron2d-joybutton-rightstick) | Identifies the right-stick button. |
| [`LeftShoulder = 9`](#f-electron2d-joybutton-leftshoulder) | Identifies the left shoulder button. |
| [`RightShoulder = 10`](#f-electron2d-joybutton-rightshoulder) | Identifies the right shoulder button. |
| [`DpadUp = 11`](#f-electron2d-joybutton-dpadup) | Identifies D-pad up. |
| [`DpadDown = 12`](#f-electron2d-joybutton-dpaddown) | Identifies D-pad down. |
| [`DpadLeft = 13`](#f-electron2d-joybutton-dpadleft) | Identifies D-pad left. |
| [`DpadRight = 14`](#f-electron2d-joybutton-dpadright) | Identifies D-pad right. |
| [`Misc1 = 15`](#f-electron2d-joybutton-misc1) | Identifies the first miscellaneous controller button. |
| [`Paddle1 = 16`](#f-electron2d-joybutton-paddle1) | Identifies paddle one. |
| [`Paddle2 = 17`](#f-electron2d-joybutton-paddle2) | Identifies paddle two. |
| [`Paddle3 = 18`](#f-electron2d-joybutton-paddle3) | Identifies paddle three. |
| [`Paddle4 = 19`](#f-electron2d-joybutton-paddle4) | Identifies paddle four. |
| [`Touchpad = 20`](#f-electron2d-joybutton-touchpad) | Identifies the touchpad button. |
| [`Misc2 = 21`](#f-electron2d-joybutton-misc2) | Identifies the second miscellaneous controller button. |
| [`Misc3 = 22`](#f-electron2d-joybutton-misc3) | Identifies the third miscellaneous controller button. |
| [`Misc4 = 23`](#f-electron2d-joybutton-misc4) | Identifies the fourth miscellaneous controller button. |
| [`Misc5 = 24`](#f-electron2d-joybutton-misc5) | Identifies the fifth miscellaneous controller button. |
| [`Misc6 = 25`](#f-electron2d-joybutton-misc6) | Identifies the sixth miscellaneous controller button. |
| [`SdlMax = 26`](#f-electron2d-joybutton-sdlmax) | Marks the count of standardized SDL controller buttons. |
| [`Max = 128`](#f-electron2d-joybutton-max) | Marks the conventional raw button count; event values can exceed it. |

## Constant Descriptions

<a id="f-electron2d-joybutton-invalid"></a>
### `Invalid = -1`

Identifies an invalid button.

<a id="f-electron2d-joybutton-a"></a>
### `A = 0`

Identifies the bottom face button.

<a id="f-electron2d-joybutton-b"></a>
### `B = 1`

Identifies the right face button.

<a id="f-electron2d-joybutton-x"></a>
### `X = 2`

Identifies the left face button.

<a id="f-electron2d-joybutton-y"></a>
### `Y = 3`

Identifies the top face button.

<a id="f-electron2d-joybutton-back"></a>
### `Back = 4`

Identifies Back or Select.

<a id="f-electron2d-joybutton-guide"></a>
### `Guide = 5`

Identifies the guide or home button.

<a id="f-electron2d-joybutton-start"></a>
### `Start = 6`

Identifies Start.

<a id="f-electron2d-joybutton-leftstick"></a>
### `LeftStick = 7`

Identifies the left-stick button.

<a id="f-electron2d-joybutton-rightstick"></a>
### `RightStick = 8`

Identifies the right-stick button.

<a id="f-electron2d-joybutton-leftshoulder"></a>
### `LeftShoulder = 9`

Identifies the left shoulder button.

<a id="f-electron2d-joybutton-rightshoulder"></a>
### `RightShoulder = 10`

Identifies the right shoulder button.

<a id="f-electron2d-joybutton-dpadup"></a>
### `DpadUp = 11`

Identifies D-pad up.

<a id="f-electron2d-joybutton-dpaddown"></a>
### `DpadDown = 12`

Identifies D-pad down.

<a id="f-electron2d-joybutton-dpadleft"></a>
### `DpadLeft = 13`

Identifies D-pad left.

<a id="f-electron2d-joybutton-dpadright"></a>
### `DpadRight = 14`

Identifies D-pad right.

<a id="f-electron2d-joybutton-misc1"></a>
### `Misc1 = 15`

Identifies the first miscellaneous controller button.

<a id="f-electron2d-joybutton-paddle1"></a>
### `Paddle1 = 16`

Identifies paddle one.

<a id="f-electron2d-joybutton-paddle2"></a>
### `Paddle2 = 17`

Identifies paddle two.

<a id="f-electron2d-joybutton-paddle3"></a>
### `Paddle3 = 18`

Identifies paddle three.

<a id="f-electron2d-joybutton-paddle4"></a>
### `Paddle4 = 19`

Identifies paddle four.

<a id="f-electron2d-joybutton-touchpad"></a>
### `Touchpad = 20`

Identifies the touchpad button.

<a id="f-electron2d-joybutton-misc2"></a>
### `Misc2 = 21`

Identifies the second miscellaneous controller button.

<a id="f-electron2d-joybutton-misc3"></a>
### `Misc3 = 22`

Identifies the third miscellaneous controller button.

<a id="f-electron2d-joybutton-misc4"></a>
### `Misc4 = 23`

Identifies the fourth miscellaneous controller button.

<a id="f-electron2d-joybutton-misc5"></a>
### `Misc5 = 24`

Identifies the fifth miscellaneous controller button.

<a id="f-electron2d-joybutton-misc6"></a>
### `Misc6 = 25`

Identifies the sixth miscellaneous controller button.

<a id="f-electron2d-joybutton-sdlmax"></a>
### `SdlMax = 26`

Marks the count of standardized SDL controller buttons.

<a id="f-electron2d-joybutton-max"></a>
### `Max = 128`

Marks the conventional raw button count; event values can exceed it. Version-one project bindings stop at button `127`.
