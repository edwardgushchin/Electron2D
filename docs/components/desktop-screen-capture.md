# Desktop screen capture

Last updated: 2026-11-07

`DisplayServer.ScreenGetImage`, `ScreenGetImageRect` and `ScreenGetPixel` read the native desktop on Windows and supported X11 sessions. `HasFeature(ScreenCapture)` reports the active adapter. The calls run synchronously on the display owner thread. They use the same screen coordinates and selectors as `ScreenGetPosition` and `ScreenGetSize`.

A capture of a screen or rectangle returns an independent, caller-owned RGBA8 image with opaque alpha and no mipmaps. A rectangle must have positive dimensions and fit inside the desktop. A point must lie inside the desktop. Invalid selectors, coordinates and image dimensions throw `ArgumentOutOfRangeException`. An unsupported backend throws `NotSupportedException`. Native pixel acquisition errors throw `IOException`. Display ownership checks apply even on an unsupported backend.

- **Windows.** GDI copies the virtual desktop into a top-down DIB and converts its pixels to RGBA. The one-pixel path uses the same copy operation. Capture follows the operating system's [window capture exclusion](window-capture.md).
- **X11.** The adapter reads the root drawable using checked XCB requests on SDL's X11 connection. It validates the live root bounds and converts the visual masks, byte order and scanline padding. It supports TrueColor outputs in a shared X screen, including multiple monitors on that root. Independent roots lack a shared public coordinate space. Indexed and DirectColor visuals need their own color provider. XWayland and Wayland need an authorized compositor capture provider. macOS needs its native capture and permission bridge.

[ColorPicker desktop sampling](color-authoring.md#desktop-and-application-sampling) uses the X11 pixel reader or the Windows desktop reader and capture exclusion while the picker owns modal input.

## Verification

The public `DesktopScreenCaptureTests` profile renders an Electron2D fixture and checks its pixels through an independent Windows Graphics Capture monitor reader or Xlib root reader. It also checks the typed screen selectors, an odd-width crop, channels, orientation, alpha, native overlay composition, rejected input and image ownership after display shutdown. The Windows profile checks exclusion and restoration and confirms that repeated captures release GDI objects.

Run `ELECTRON2D_TEST_SCREEN_CAPTURE=windows` with the path to [the monitor reader](../../tests/Electron2D.Tests/Fixtures/ScreenCapture/monitor.cpp) as the test runner's argument. Build the reader from a Visual Studio developer shell with `cl /std:c++20 /EHsc monitor.cpp /link windowsapp.lib d3d11.lib user32.lib`. Run the renderer in an open Windows user session where the test window is visible. The reader verifies the known test pixels in a monitor frame and prints their values. It does not write the desktop frame to disk.

Run `ELECTRON2D_TEST_SCREEN_CAPTURE=x11` on a private Xvfb display with `SDL_VIDEODRIVER=x11`, `SDL_RENDER_DRIVER=software` and `ELECTRON2D_TEST_X11_PRIVATE_DISPLAY` matching `DISPLAY`. The native profile passes on Xvfb at depths 24 and 16. The `unavailable` profile passes on the dummy driver, nested Weston and WSLg XWayland.

The earlier Windows fixture failed to reach monitor composition. An independent monitor capture found no red test pixels and `WindowFromPoint` reported another window at the expected marker. The repeated control on the same machine kept a smaller rendered window active while a separate process read the 1920×1080 monitor. That control found all three known markers. The cause of the earlier failure remains unproven.

These checks cover the tested Windows desktop and private X11 server. Mixed-DPI and multi-monitor hardware, additional native backends and release-package qualification retain separate gates under ADR 0021. Viewport and single-window readback have their own contracts.
