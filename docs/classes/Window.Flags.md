# Window.Flags

Last updated: 2026-09-22

**Inherits:** System.Enum

- **Source:** [`Window.Native.cs`](../../src/Scene/Main/Window.Native.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Window.Flags`

## Description

Identifies individual policies by ordinal index. This is not a bit mask.

ResizeDisabled, Borderless, AlwaysOnTop and NoFocus are executable policies. Wayland top-level windows reject enabling AlwaysOnTop or NoFocus. Other identifiers require the integration named below and GetFlag/SetFlag reject them with NotSupportedException. Max and out-of-range values throw ArgumentOutOfRangeException. Declared IDs do not imply available capabilities.

## Examples

```csharp
window.SetFlag(Window.Flags.ResizeDisabled, true);
```

`window` is a caller-owned Window before Run, or the active scene root on its owner thread.

## Values

| Value | ID | Contract |
| --- | --- | --- |
| [ResizeDisabled](#resizedisabled) | 0 | Prevents resizing by dragging native borders. |
| [Borderless](#borderless) | 1 | Removes native borders and the title bar. |
| [AlwaysOnTop](#alwaysontop) | 2 | Requests placement above ordinary windows; unavailable for a Wayland top-level. |
| [Transparent](#transparent) | 3 | Requires transparent native creation and rendering. |
| [NoFocus](#nofocus) | 4 | Prevents keyboard focus; unavailable for a Wayland top-level. |
| [Popup](#popup) | 5 | Requires transient popup ownership. |
| [ExtendToTitle](#extendtotitle) | 6 | Requires drawing under an integrated native title bar. |
| [MousePassthrough](#mousepassthrough) | 7 | Requires native pointer hit-test integration. |
| [SharpCorners](#sharpcorners) | 8 | Requires native corner-style integration. |
| [ExcludeFromCapture](#excludefromcapture) | 9 | Requires native capture-policy integration. |
| [PopupWmHint](#popupwmhint) | 10 | Requires native popup window-manager hints. |
| [MinimizeDisabled](#minimizedisabled) | 11 | Requires native minimization-control integration. |
| [MaximizeDisabled](#maximizedisabled) | 12 | Requires native maximization-control integration. |
| [Max](#max) | 13 | The number of defined policy identifiers; not a selectable flag. |

## Value Descriptions

<a id="resizedisabled"></a>
### `ResizeDisabled = 0`

Prevents resizing by dragging native borders.

<a id="borderless"></a>
### `Borderless = 1`

Removes native borders and the title bar.

<a id="alwaysontop"></a>
### `AlwaysOnTop = 2`

Requests placement above ordinary windows; unavailable for a Wayland top-level.

<a id="transparent"></a>
### `Transparent = 3`

Requires transparent native creation and rendering.

<a id="nofocus"></a>
### `NoFocus = 4`

Prevents keyboard focus; unavailable for a Wayland top-level.

<a id="popup"></a>
### `Popup = 5`

Requires transient popup ownership.

<a id="extendtotitle"></a>
### `ExtendToTitle = 6`

Requires drawing under an integrated native title bar.

<a id="mousepassthrough"></a>
### `MousePassthrough = 7`

Requires native pointer hit-test integration.

<a id="sharpcorners"></a>
### `SharpCorners = 8`

Requires native corner-style integration.

<a id="excludefromcapture"></a>
### `ExcludeFromCapture = 9`

Requires native capture-policy integration.

<a id="popupwmhint"></a>
### `PopupWmHint = 10`

Requires native popup window-manager hints.

<a id="minimizedisabled"></a>
### `MinimizeDisabled = 11`

Requires native minimization-control integration.

<a id="maximizedisabled"></a>
### `MaximizeDisabled = 12`

Requires native maximization-control integration.

<a id="max"></a>
### `Max = 13`

The number of defined policy identifiers; not a selectable flag.

## Verification and dependencies

WindowRuntimeTests covers configured state, native Wayland application, invalid identifiers, capability rejection and packed reconstruction. Numeric IDs are audited in the coverage register. See [Window](Window.md) and the [window runtime component](../components/window-runtime.md).
