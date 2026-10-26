# Native window mouse input regions

`Window.MousePassthrough` and `MousePassthroughPolygon` configure native mouse
reception under [ADR 0040](../decisions/display.md#adr-0040). The flag overrides
the retained polygon. An empty polygon restores ordinary reception; a nonempty
degenerate polygon accepts no area. The polygon uses the even-odd fill rule.

## Source and native state

Window copies vertices on assignment and retrieval. Its typed property descriptors
preserve both policies in duplication and PackedScene. Native children reapply
their authored state when shown with a new native identity. Embedded windows
retain the values without filtering embedded GUI input.

`DisplayServer.WindowSetMousePassthrough(region, windowId)` copies a native mask
for the selected live identity. It leaves the authored Window polygon unchanged.
The static WindowGetFlag/WindowSetFlag family shares the native boolean policy.
Off-owner calls, retired identities and nonfinite vertices fail before publication.
An unsupported driver rejects effective requests. Native requests and border
changes preserve the previous policy on failure; a failed rollback is aggregated.

## Native adapters and ownership

Internal [WindowInputRegion](../classes/WindowInputRegion.md) borrows each native
window identity. X11 uses the Shape input region with native even-odd polygon
filling and signed 16-bit vertices. Windows uses an owned-then-transferred window
region plus same-owner-thread transparent hit testing; its polygon also clips drawing.
macOS evaluates membership during the owner pump and sets NSWindow mouse ignoring.
X11 and macOS preserve pixels outside the polygon. Windows region coordinates
use the [native signed 27-bit range](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-createpolygonrgn).

Windows installs one native window subclass whose retained owner is released on
removal or native destruction. It passes full-window mouse hit tests through to
underlying windows on the same owner thread. The OS owns a successfully submitted
polygon region; failed submissions release the temporary region.

Wayland has no integrated adapter for this contract and rejects effective
passthrough requests. Empty/default operations remain valid. A future Wayland extension needs an accepted behavior contract and independent
compositor routing checks.

Polygon/geometry changes may allocate prepared storage. Unchanged frames and
prepared boolean policy updates keep the ordinary owner-frame managed allocation
boundary. Native input event delivery and native heap use are separate boundaries.

## Verification

[WindowInputRegionTests](../../tests/Electron2D.Tests/WindowInputRegionTests.cs)
covers authored state, copy/scene independence, invalid edits, retirement and
embedded routing. [WindowInputRegionNativeTests](../../tests/Electron2D.Tests/WindowInputRegionNativeTests.cs)
and its [kernel input client](../../tests/WindowInputRegionClient/probe.py) exercise
real window-system routing rather than filtering already-delivered scene events.
The Wayland profiles verify explicit failures, preserved state and startup cleanup.
The kernel client prepares XInput2 scroll baselines after pointer focus changes
before checking one delivered tick; [SDL's native parser](https://github.com/libsdl-org/SDL/blob/main/src/video/x11/SDL_x11xinput2.c) explicitly discards the initial valuator sample.
Button release follows its actual native destination after an input-region change.
The engine clears that button in all retained window masks, so later motion in
the former press owner cannot report a stuck button.
The public-only source consumer passed CLR and strict full-trim NativeAOT on
X11 with both software GPU and compatibility rendering. Physical kernel pointer,
button and prepared-wheel routing covers rectangle/self-crossing/degenerate
polygons, full override/reset, direct-service source separation, native reopening
and held-button release. Each X11 profile measured 64 idle and 64 active policy
frames at zero owner-thread managed bytes. Engine texture readback retains pixels
outside the input polygon. Four Wayland CLR/AOT profiles passed ten capability,
rollback and startup-retirement checks each.

The native queue regression pushes child press/main release/child motion without
focus loss: the old mask update reasserted pressed polling; the shared release
fix passes all three checks. Fifteen authored/embedded/copy checks pass separately.
Receipts are retained in `bin/window-input-region/2026-10-27/`. The NativeAOT binary
SHA-256 is `faa6a101d14fa302e94a9096b8eed80d50185075369022cc89f1d864afde1cc8`.
These are source-consumer checks with local native prerequisites. Windows/macOS
execution, native heap, isolated performance, package qualification and human
visual acceptance remain separate.
