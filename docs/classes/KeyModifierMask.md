# KeyModifierMask

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum KeyModifierMask`

> Defines modifier bits that can be combined with a [`Key`](Key.md) value.

## Description

Defines modifier bits that can be combined with a [`Key`](Key.md) value.

- Complete values: `CodeMask`, `ModifierMask`, `CommandOrControl`, `Shift`, `Alt`, `Meta`, `Control`, `Keypad`, `GroupSwitch`.
- Responsibility: separates key-code bits from combinable modifier/layout bits. Command-or-control maps to Meta on macOS and Control elsewhere when used by modifier events.
- Invariants/threading: immutable allocation-free value; unknown bits may exist through casts but concrete event APIs only produce documented bits.
- Verification: mask production, exact/non-exact matching, platform mapping, and key-code composition are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = KeyModifierMask.CodeMask;
```

## Constants

| Member | Description |
| --- | --- |
| [`CodeMask = 8388607`](#f-electron2d-keymodifiermask-codemask) | Contains every non-modifier key-code bit. |
| [`ModifierMask = 2130706432`](#f-electron2d-keymodifiermask-modifiermask) | Contains every modifier bit. |
| [`CommandOrControl = 16777216`](#f-electron2d-keymodifiermask-commandorcontrol) | Requests Command on macOS and Control on other platforms. |
| [`Shift = 33554432`](#f-electron2d-keymodifiermask-shift) | Identifies Shift. |
| [`Alt = 67108864`](#f-electron2d-keymodifiermask-alt) | Identifies Alt or Option. |
| [`Meta = 134217728`](#f-electron2d-keymodifiermask-meta) | Identifies Meta, Command, Windows, or Super. |
| [`Control = 268435456`](#f-electron2d-keymodifiermask-control) | Identifies Control. |
| [`Keypad = 536870912`](#f-electron2d-keymodifiermask-keypad) | Identifies a numeric-keypad key. |
| [`GroupSwitch = 1073741824`](#f-electron2d-keymodifiermask-groupswitch) | Identifies a group-layout switch. |

## Constant Descriptions

<a id="f-electron2d-keymodifiermask-codemask"></a>
### `CodeMask = 8388607`

Contains every non-modifier key-code bit.

<a id="f-electron2d-keymodifiermask-modifiermask"></a>
### `ModifierMask = 2130706432`

Contains every modifier bit.

<a id="f-electron2d-keymodifiermask-commandorcontrol"></a>
### `CommandOrControl = 16777216`

Requests Command on macOS and Control on other platforms.

<a id="f-electron2d-keymodifiermask-shift"></a>
### `Shift = 33554432`

Identifies Shift.

<a id="f-electron2d-keymodifiermask-alt"></a>
### `Alt = 67108864`

Identifies Alt or Option.

<a id="f-electron2d-keymodifiermask-meta"></a>
### `Meta = 134217728`

Identifies Meta, Command, Windows, or Super.

<a id="f-electron2d-keymodifiermask-control"></a>
### `Control = 268435456`

Identifies Control.

<a id="f-electron2d-keymodifiermask-keypad"></a>
### `Keypad = 536870912`

Identifies a numeric-keypad key.

<a id="f-electron2d-keymodifiermask-groupswitch"></a>
### `GroupSwitch = 1073741824`

Identifies a group-layout switch.
