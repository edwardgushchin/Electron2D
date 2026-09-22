# DisplayServer.HandleType

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Windows.cs`](../../src/Servers/Display/DisplayServer.Windows.cs)

**Declaration:** `public enum HandleType` nested in [`DisplayServer`](DisplayServer.md)

## Description

Selects a borrowed operating-system identity for [`DisplayServer.WindowGetNativeHandle`](DisplayServer.md#method-windowgetnativehandle). The numeric IDs retain the display and window identities used by the display contract. The returned `nint` is a pointer-sized value, not an owned `SafeHandle`; callers must not free it and must stop using it when the display server is disposed. Query again after a native window state change. This API is confined to the opening SDL main thread.

Linux compatibility rendering also exposes its borrowed GL/EGL/GLX identities. They are captured for the owned window when its renderer starts and remain valid only until that renderer closes. A getter never returns another window's current context or changes which context is current. GPU/software paths reject these queries. Native views and other-platform graphics identities remain incomplete under [ADR 0042](../decisions/display.md#adr-0042).

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
| [`OpenGLContext = 3`](#value-openglcontext) | Linux compatibility GL context. |
| [`EGLDisplay = 4`](#value-egldisplay) | Its EGL display, when EGL is used. |
| [`EGLConfig = 5`](#value-eglconfig) | Its EGL framebuffer configuration. |
| [`GLXVisualID = 6`](#value-glxvisualid) | Its X11/GLX visual ID. |
| [`GLXFBConfig = 7`](#value-glxfbconfig) | Its X11/GLX framebuffer configuration. |

## Enumeration Descriptions

<a id="value-displayhandle"></a>
### `DisplayHandle = 0`

Selects an X11 `Display*` or Wayland `wl_display*`. Other current drivers reject the query with `NotSupportedException`. Android's EGL display identity requires a real graphics host and is dependency-blocked; it must not be replaced by SDL's `ANativeWindow*`.

<a id="value-windowhandle"></a>
### `WindowHandle = 1`

Selects an X11 window ID, Wayland `wl_surface*`, Win32 `HWND`, or Cocoa `NSWindow*`. X11's value is numeric but returned as pointer-sized `nint` for one typed contract. Android's Activity `jobject` requires a JNI reference-lifetime boundary, and iOS's view-controller identity requires a UIKit bridge; neither is represented by another SDL pointer.

<a id="value-openglcontext"></a>
### `OpenGLContext = 3`

Returns the compatibility renderer's native EGLContext or GLXContext on Linux Wayland/X11. The window association is checked when the SDL renderer creates its context; later queries use that captured identity. Without a renderer, or with GPU/software rendering or an unsupported platform, the query throws `NotSupportedException`.

<a id="value-egldisplay"></a>
### `EGLDisplay = 4`

Returns the EGLDisplay verified for that context. Available only for an EGL-backed compatibility renderer; it is not the Wayland `wl_display*` returned by `DisplayHandle`.

<a id="value-eglconfig"></a>
### `EGLConfig = 5`

Returns the EGLConfig whose native config ID matches the renderer context. Creating another window can change SDL's global config; this query continues returning the original window's configuration.

<a id="value-glxvisualid"></a>
### `GLXVisualID = 6`

Returns the numeric visual ID from the X11/GLX context's framebuffer configuration. The result is carried as `nint`; it is not a `Visual*` pointer. Unavailable on EGL-backed renderers.

<a id="value-glxfbconfig"></a>
### `GLXFBConfig = 7`

Returns the display-owned GLXFBConfig matching the context's native config ID and screen. Temporary native enumeration memory is freed inside the engine. The caller must not release the borrowed config. Unavailable on EGL-backed renderers or if a matching config cannot be obtained.

## Lifecycle, invariants, and errors

Handles are borrowed and may be invalidated by native window changes or server disposal. Graphics identities additionally expire at renderer shutdown; callers must not destroy, replace or mutate the renderer context or graphics state. The server never transfers ownership. `WindowGetNativeHandle` validates the main-window ID and enum value, rejects unavailable drivers, and throws when a supported native property is absent. It does not return a false-success zero. See the parent method description for the exact exceptions and thread boundary.

## Dependencies and interactions

`DisplayServer` queries SDL window properties and hands borrowed operating-system identities to caller-owned platform code. It does not expose the SDL window pointer or create a renderer context.

## Verification

Dummy-driver tests cover numeric IDs, unsupported-driver failure, invalid arguments, wrong-thread rejection, and use after disposal. Temporary Wayland and XWayland consumers using SDL 3.4.16 confirmed nonzero display/window handles and owner-thread window lifecycle. Windows, macOS, Android, iOS, and a standalone Xorg session remain unverified. For the current Linux/Wayland gate, the borrowed-handle lifetime and failure behavior still need verification in a packaged production host; other platforms are later gates under [ADR 0021](../decisions/product.md#adr-0021).

## Known limitations and decisions

`WindowView` and mobile host identities remain dependency-blocked. Linux compatibility GL/EGL/GLX identities have an executable native bridge; other-platform graphics identities remain incomplete. [ADR 0042](../decisions/display.md#adr-0042) defines these boundaries and [ADR 0028](../decisions/rendering.md#adr-0028) defines the renderer.

Native graphics checks (`ELECTRON2D_TEST_RENDER_HANDLES=1`) cover Wayland EGL with GL/GLES, XWayland GLX, native config/visual queries, a foreign context with another config, getter side-effect freedom, two-frame readback, disposal and reopen. Dummy/software and Wayland GPU reject GL identities. Other platforms and X11/EGL surface creation are not verified.
