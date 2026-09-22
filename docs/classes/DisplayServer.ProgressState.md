# DisplayServer.ProgressState

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Windows.cs`](../../src/Servers/Display/DisplayServer.Windows.cs)

**Declaration:** `public enum ProgressState` nested in [`DisplayServer`](DisplayServer.md)

## Description

A typed native display policy used by [`DisplayServer`](DisplayServer.md). Values are stable managed identities; SDL translation is private to the server.

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`None = 0`](#value-none) | Removes taskbar progress. |
| [`Indeterminate = 1`](#value-indeterminate) | Shows activity without a numerical fraction. |
| [`Normal = 2`](#value-normal) | Shows normal progress. |
| [`Error = 3`](#value-error) | Shows an error state. |
| [`Paused = 4`](#value-paused) | Shows a paused state. |

## Enumeration Descriptions

<a id="value-none"></a>
### `None = 0`

Removes taskbar progress.

<a id="value-indeterminate"></a>
### `Indeterminate = 1`

Shows activity without a numerical fraction.

<a id="value-normal"></a>
### `Normal = 2`

Shows normal progress.

<a id="value-error"></a>
### `Error = 3`

Shows an error state.

<a id="value-paused"></a>
### `Paused = 4`

Shows a paused state.

## Lifecycle and invariants

Enum values are immutable. Unknown numeric values are rejected by the server setter before native mutation. On Wayland the setter rejects valid values until a desktop progress integration is verified. On other native drivers it delegates to SDL; no public progress getter exists.

## Threading and interactions

The enum itself has no thread affinity. DisplayServer calls using it require the opening SDL main thread. The server owns native resources and event ordering.

## Verification and limitations

The Wayland native smoke checks rejection of valid requests, invalid-value and invalid-window errors, and unchanged SDL progress state. Visible desktop taskbar progress has not been verified. See the [complete DisplayServer reference](DisplayServer.md) and [coverage inventory](../coverage/classes/DisplayServer.md).

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040) and [ADR 0028](../decisions/rendering.md#adr-0028).
