# WindowMode

Last updated: 2026-09-24

**Inherits:** System.Enum

- **Source:** [`Window.Native.cs`](../../src/Scene/Main/Window.Native.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum WindowMode`

## Description

Identifies native presentation modes.

Wayland treats ExclusiveFullscreen as ordinary Fullscreen. Other platforms and exclusive video-mode transitions have not been natively verified by this slice. The Mode property returns observed state once active; requests can be asynchronous.

## Examples

```csharp
window.Mode = WindowMode.Fullscreen;
```

`window` is a caller-owned Window before Run, or the active scene root on its owner thread.

## Values

| Value | ID | Contract |
| --- | --- | --- |
| [Windowed](#windowed) | 0 | A floating window with its configured decorations. |
| [Minimized](#minimized) | 1 | A window minimized by the window manager. |
| [Maximized](#maximized) | 2 | A window expanded to its screen's work area. |
| [Fullscreen](#fullscreen) | 3 | A borderless window covering its screen. |
| [ExclusiveFullscreen](#exclusivefullscreen) | 4 | A fullscreen window requesting a dedicated video mode where supported. |

## Value Descriptions

<a id="windowed"></a>
### `Windowed = 0`

A floating window with its configured decorations.

<a id="minimized"></a>
### `Minimized = 1`

A window minimized by the window manager.

<a id="maximized"></a>
### `Maximized = 2`

A window expanded to its screen's work area.

<a id="fullscreen"></a>
### `Fullscreen = 3`

A borderless window covering its screen.

<a id="exclusivefullscreen"></a>
### `ExclusiveFullscreen = 4`

A fullscreen window requesting a dedicated video mode where supported.

## Verification and dependencies

WindowRuntimeTests covers configured state, native Wayland application, invalid identifiers, capability rejection and packed reconstruction. Numeric IDs are audited in the coverage register. See [Window](Window.md) and the [window runtime component](../components/window-runtime.md).
