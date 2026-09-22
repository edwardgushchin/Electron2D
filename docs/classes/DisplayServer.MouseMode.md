# DisplayServer.MouseMode

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Pointer.cs`](../../src/Servers/Display/DisplayServer.Pointer.cs)

**Declaration:** `public enum MouseMode` nested in [`DisplayServer`](DisplayServer.md)

## Description

Stable pointer visibility and confinement identities for `MouseSetMode`. The native window manager may release capture when focus changes; `Max` counts modes and cannot be selected.

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`Visible = 0`](#value-visible) | The pointer is visible and free to leave the window. |
| [`Hidden = 1`](#value-hidden) | The pointer is hidden and free to leave the window. |
| [`Captured = 2`](#value-captured) | The pointer is hidden, captured, and reports relative motion. |
| [`Confined = 3`](#value-confined) | The visible pointer is confined to the main window. |
| [`ConfinedHidden = 4`](#value-confinedhidden) | The hidden pointer is confined to the main window. |
| [`Max = 5`](#value-max) | The number of pointer modes; not a selectable mode. |

## Enumeration Descriptions

<a id="value-visible"></a>
### `Visible = 0`

The pointer is visible and free to leave the window.

<a id="value-hidden"></a>
### `Hidden = 1`

The pointer is hidden and free to leave the window.

<a id="value-captured"></a>
### `Captured = 2`

The pointer is hidden, captured, and reports relative motion.

<a id="value-confined"></a>
### `Confined = 3`

The visible pointer is confined to the main window.

<a id="value-confinedhidden"></a>
### `ConfinedHidden = 4`

The hidden pointer is confined to the main window.

<a id="value-max"></a>
### `Max = 5`

The number of pointer modes; not a selectable mode.

## Lifecycle and invariants

Values are immutable. `Max` and unknown numeric values are rejected before any native mutation. Getters return the last successfully requested mode.

## Threading and interactions

The enum itself has no thread affinity; native operations require the opening SDL main thread.

## Verification and limitations

The SDL dummy-driver harness checks numeric identities and rejects the terminal marker. Visible cursor appearance and platform capture behavior have not been verified.

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040); see the [coverage inventory](../coverage/classes/DisplayServer.md).
