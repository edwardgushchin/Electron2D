# Desktop screen capture prerequisite

Last updated: 2026-11-07

The three `screen_get_image`, `screen_get_image_rect` and `screen_get_pixel`
comparison rows remain Blocked. No public Electron2D screen-readback methods
are implemented. `Feature.ScreenCapture` remains unavailable. Existing engine
viewport/native-surface readback and [window capture exclusion](window-capture.md)
are separate capabilities.

The first native slice must return owned compositor pixels, define coordinates,
permission/failure behavior and image lifetime, and exercise actual screen
composition under ADRs 0028/0040/0090. Windows needs its native desktop producer;
native Xorg needs a checked producer and host verification, with the reference's
XWayland limitation accounted; macOS needs its capture/permission bridge.

## Attempt on 2026-11-07

A Windows GDI prototype compiled with three image/pixel operations and an
independent SDK consumer. It was removed from runtime sources after the real
fixture failed: its engine viewport held the red marker, but both owner and SDK
desktop GetPixel observed RGB `(210, 225, 235)`. WindowFromPoint identified a
different `Windows.UI.Core.CoreWindow` at that position. The owned window was
visible, not minimized or cloaked, and had neither capture exclusion nor mouse
passthrough enabled. Public foreground/topmost and scoped SDK asynchronous
topmost requests did not expose the marker. An independent Windows Graphics
Capture monitor frame was 1920×1080 and contained no red fixture pixels either.
The underlying host/composition cause is unproven; this is not evidence that a
GDI-only implementation is faulty or that the proposed public API is qualified.

The continuation prerequisite is a native desktop/host on which the controlled
owned fixture reaches monitor composition, followed by independent orientation,
channel/alpha, selector, exclusion/restoration, lifetime, failure and resource-drain
checks. Window/viewport capture alone cannot establish this desktop contract.
Only validated fixture crops should be persisted. Prototype code, failed logs and
`blocked-candidate.json` are retained under
`C:/Users/eduar/.codex/artifacts/Electron2D/screen-capture-20261107/`;
the unqualified source patch is not shipped.
