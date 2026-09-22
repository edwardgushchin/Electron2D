# DisplayServer.WindowFlag

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Windows.cs`](../../src/Servers/Display/DisplayServer.Windows.cs)

**Declaration:** `public enum WindowFlag` nested in [`DisplayServer`](DisplayServer.md)

## Description

A typed native display policy used by [`DisplayServer`](DisplayServer.md). Values retain their numeric display-server identities. The current SDL slice executes border and resize policies on Wayland; topmost and focus policies are available only on drivers that implement them. Other defined values remain usable as identifiers but `WindowGetFlag` and `WindowSetFlag` throw `NotSupportedException` until the named capability is integrated. `Max` is a terminal marker, not a policy.

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`ResizeDisabled = 0`](#value-resizedisabled) | Prevents resizing by dragging the border. |
| [`Borderless = 1`](#value-borderless) | Removes the native border and title bar. |
| [`AlwaysOnTop = 2`](#value-alwaysontop) | Keeps the window above ordinary windows. |
| [`Transparent = 3`](#value-transparent) | Requires transparent window creation and a renderer. |
| [`NoFocus = 4`](#value-nofocus) | Prevents the window from receiving keyboard focus. |
| [`Popup = 5`](#value-popup) | Requires multiple-window ownership. |
| [`ExtendToTitle = 6`](#value-extendtotitle) | Requires native title-bar integration. |
| [`MousePassthrough = 7`](#value-mousepassthrough) | Requires native hit-test integration. |
| [`SharpCorners = 8`](#value-sharpcorners) | Requires native window-style integration. |
| [`ExcludeFromCapture = 9`](#value-excludefromcapture) | Requires native capture-policy integration. |
| [`PopupWmHint = 10`](#value-popupwmhint) | Requires multiple-window ownership. |
| [`MinimizeDisabled = 11`](#value-minimizedisabled) | Requires native window-style integration. |
| [`MaximizeDisabled = 12`](#value-maximizedisabled) | Requires native window-style integration. |
| [`Max = 13`](#value-max) | Terminal marker; rejected as a policy. |

## Enumeration Descriptions

<a id="value-resizedisabled"></a>
### `ResizeDisabled = 0`

When true, requests that the window manager prevent user resizing through the window border. Programmatic size changes remain available. `WindowGetFlag` reports the inverse of SDL's observed resizable flag; a dummy or other backend may accept a request without changing the observed flag.

<a id="value-borderless"></a>
### `Borderless = 1`

When true, removes the native border and title bar.

<a id="value-alwaysontop"></a>
### `AlwaysOnTop = 2`

Whether the window stays above ordinary windows. The ordinary Wayland top-level has no SDL backend hook for this policy, so this flag throws `NotSupportedException` there; a supported native layer-shell or equivalent window integration would be required.

<a id="value-nofocus"></a>
### `NoFocus = 4`

When true, prevents the window from receiving keyboard focus. SDL can change focusability only for Wayland popup-menu windows, so this flag throws `NotSupportedException` for the current main window; a popup or other suitable native window role is required.

<a id="value-transparent"></a>
### `Transparent = 3`

Requires transparent window creation and compatible rendering. The current main window is created opaque.

<a id="value-popup"></a>
### `Popup = 5`

Requires a transient child-window model and popup input ownership.

<a id="value-extendtotitle"></a>
### `ExtendToTitle = 6`

Requires native title-bar geometry and drawing integration.

<a id="value-mousepassthrough"></a>
### `MousePassthrough = 7`

Requires native hit testing and an underlying application window.

<a id="value-sharpcorners"></a>
### `SharpCorners = 8`

Requires platform window-style integration.

<a id="value-excludefromcapture"></a>
### `ExcludeFromCapture = 9`

Requires platform capture-policy integration; its eventual behavior cannot guarantee protection against all capture methods.

<a id="value-popupwmhint"></a>
### `PopupWmHint = 10`

Requires a native popup window and window-manager policy integration.

<a id="value-minimizedisabled"></a>
### `MinimizeDisabled = 11`

Requires platform window-style integration.

<a id="value-maximizedisabled"></a>
### `MaximizeDisabled = 12`

Requires platform window-style integration.

<a id="value-max"></a>
### `Max = 13`

Counts the policy identifiers. Passing it to a flag getter or setter throws `ArgumentOutOfRangeException`.

## Lifecycle and invariants

Enum values are immutable. Unknown numeric values and `Max` are rejected before native mutation. Defined unsupported values fail explicitly. On Wayland, `AlwaysOnTop` and `NoFocus` are rejected before calling SDL, preventing an apparent success or an internal SDL flag change without a matching compositor policy. Executable native getters map reported SDL state; platform acceptance of requests may vary.

## Threading and interactions

The enum itself has no thread affinity. DisplayServer calls using it require the opening SDL main thread. The server owns native resources and event ordering.

## Verification and limitations

The SDL dummy-driver harness checks numeric identities, the observed resizable-flag polarity, explicit rejection of unsupported defined flags, and invalid-value rejection. A targeted visible Wayland run passed borderless and resize-disabled toggles plus topmost/focus rejection without native flag mutation. The full native smoke currently stops earlier at a separate screen-scale check. See the [complete DisplayServer reference](DisplayServer.md) and [coverage inventory](../coverage/classes/DisplayServer.md).

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040) and [ADR 0028](../decisions/rendering.md#adr-0028).
