# DisplayServer.WindowMode

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Windows.cs`](../../src/Servers/Display/DisplayServer.Windows.cs)

**Declaration:** `public enum WindowMode` nested in [`DisplayServer`](DisplayServer.md)

## Description

A typed native display policy used by [`DisplayServer`](DisplayServer.md). Values are stable managed identities; SDL translation is private to the server.

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`Windowed = 0`](#value-windowed) | A decorated or undecorated floating window. |
| [`Minimized = 1`](#value-minimized) | A window hidden by the window manager and represented in its task list. |
| [`Maximized = 2`](#value-maximized) | A window expanded to the work area with its border retained. |
| [`Fullscreen = 3`](#value-fullscreen) | A borderless window covering its current display without a video-mode change. |
| [`ExclusiveFullscreen = 4`](#value-exclusivefullscreen) | A selected native video mode where supported; ordinary compositor fullscreen on Wayland. |

## Enumeration Descriptions

<a id="value-windowed"></a>
### `Windowed = 0`

A decorated or undecorated floating window.

<a id="value-minimized"></a>
### `Minimized = 1`

A window hidden by the window manager and represented in its task list.

<a id="value-maximized"></a>
### `Maximized = 2`

A window expanded to the work area with its border retained.

<a id="value-fullscreen"></a>
### `Fullscreen = 3`

A borderless window covering its current display without a video-mode change.

<a id="value-exclusivefullscreen"></a>
### `ExclusiveFullscreen = 4`

A native video mode selected for one fullscreen window where supported. On Wayland, the request becomes ordinary compositor fullscreen and `WindowGetMode` reports `Fullscreen` when accepted; no video mode is switched.

## Lifecycle and invariants

Enum values are immutable. Unknown numeric values are rejected by server setters before native mutation. Native getters map SDL state to these values; platform acceptance of requests may vary. Wayland cannot reliably restore a minimized window programmatically, and SDL can retain its minimized flag until the window regains focus.

## Threading and interactions

The enum itself has no thread affinity. DisplayServer calls using it require the opening SDL main thread. The server owns native resources and event ordering.

## Verification and limitations

The SDL dummy-driver harness checks numeric identities. Visible Wayland smoke runs exercised maximize/windowed, fullscreen/windowed, and exclusive-as-ordinary fullscreen transitions. An earlier run observed a minimized request; a later attempt to restore it programmatically exposed the protocol limitation, so the minimization check now runs last and awaits a full run past the separate clipboard test. Compositor denial and user restoration from minimized state remain unverified. See the [complete DisplayServer reference](DisplayServer.md) and [coverage inventory](../coverage/classes/DisplayServer.md).

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040) and [ADR 0028](../decisions/rendering.md#adr-0028).
