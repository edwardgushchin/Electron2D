# DisplayServer.HandleType

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Windows.cs`](../../src/Servers/Display/DisplayServer.Windows.cs)

**Declaration:** `public enum HandleType` nested in [`DisplayServer`](DisplayServer.md)

## Description

Selects a borrowed operating-system identity for [`DisplayServer.WindowGetNativeHandle`](DisplayServer.md#method-windowgetnativehandle). The numeric IDs retain the display and window identities used by the display contract. The returned `nint` is a pointer-sized value, not an owned `SafeHandle`; callers must not free it and must stop using it when the display server is disposed. Query again after a native window state change. This API is confined to the opening SDL main thread.

Only the display and window identities are declared. Native view integration is blocked on platform view ownership and renderer policy. Five GL/EGL/GLX context identities remain blocked until a real renderer selects a compatible driver and can provide each window-associated borrowed identity with verified ownership, thread, and lifetime; see [ADR 0042](../decisions/display.md#adr-0042).

## Example

```csharp
using Electron2D;

using var display = DisplayServer.Open("Native integration", new Vector2I(640, 480));
nint window = display.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
// Pass window to platform code while display remains alive; never release it.
```

This example requires an X11, Wayland, Windows, or macOS video driver. It is intentionally partial: the host must also provide its application loop and platform-specific interop call.

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`DisplayHandle = 0`](#value-displayhandle) | X11 or Wayland display connection. |
| [`WindowHandle = 1`](#value-windowhandle) | Native main-window identity. |

## Enumeration Descriptions

<a id="value-displayhandle"></a>
### `DisplayHandle = 0`

Selects an X11 `Display*` or Wayland `wl_display*`. Other current drivers reject the query with `NotSupportedException`. Android's EGL display identity requires a real graphics host and is dependency-blocked; it must not be replaced by SDL's `ANativeWindow*`.

<a id="value-windowhandle"></a>
### `WindowHandle = 1`

Selects an X11 window ID, Wayland `wl_surface*`, Win32 `HWND`, or Cocoa `NSWindow*`. X11's value is numeric but returned as pointer-sized `nint` for one typed contract. Android's Activity `jobject` requires a JNI reference-lifetime boundary, and iOS's view-controller identity requires a UIKit bridge; neither is represented by another SDL pointer.

## Lifecycle, invariants, and errors

Handles are borrowed and may be invalidated by native window changes or server disposal. The server never transfers ownership. `WindowGetNativeHandle` validates the main-window ID and enum value, rejects unavailable drivers, and throws when a supported native property is absent. It does not return a false-success zero. See the parent method description for the exact exceptions and thread boundary.

## Dependencies and interactions

`DisplayServer` queries SDL window properties and hands borrowed operating-system identities to caller-owned platform code. It does not expose the SDL window pointer or create a renderer context.

## Verification

Dummy-driver tests cover numeric IDs, unsupported-driver failure, invalid arguments, wrong-thread rejection, and use after disposal. Temporary Wayland and XWayland consumers using SDL 3.4.16 confirmed nonzero display/window handles and owner-thread window lifecycle. Windows, macOS, Android, iOS, and a standalone Xorg session remain unverified. For the current Linux/Wayland gate, the borrowed-handle lifetime and failure behavior still need verification in a packaged production host; other platforms are later gates under [ADR 0021](../decisions/product.md#adr-0021).

## Known limitations and decisions

`WindowView`, mobile host identities, and GL/EGL/GLX identities are dependency-blocked. The first executable SDL_Renderer fallback slice must check the actual GL/GLES driver and each graphics-context identity; GLX values also need an X11/GLX bridge. [ADR 0042](../decisions/display.md#adr-0042) defines these boundaries and [ADR 0028](../decisions/rendering.md#adr-0028) defines the renderer.
