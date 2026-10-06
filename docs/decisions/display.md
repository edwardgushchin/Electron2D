# Display decisions

Last updated: 2026-10-06


Public process-wide service operations delegate statically to retained objects under [ADR 0095](singleton-services.md#adr-0095). Owning lifetime, threading and native resource contracts below continue to apply.

This bounded log owns native display connection, window, and event-pump decisions. See [the decision index](index.md) for routing.

<a id="adr-0040"></a>
## ADR 0040: One SDL display server and typed main-window host

Last updated: 2026-10-06

### Status

Accepted. The display server and first executable consumer are implemented; this record does not mark all obligations in ADR 0038 complete.

### Context

Electron2D previously had no native display host. Engine accepted elapsed time from an external host and Input accepted caller-owned typed events, but no engine-owned implementation created a window or delivered operating-system input. A renderer remains a separate dependency under ADR 0028.

### Decision

- `DisplayServer` is one process-owned, explicitly disposed `ElectronObject` in the runtime assembly. It owns SDL video and gamepad initialization, one high-density resizable main window, cursor and controller handles, and the native event pump. Public IDs use zero for the main window; native SDL handles stay private.
- `Open` is the explicit typed-C# library bootstrap for the reference display service; static operations address the retained active server, with `IsAvailable` exposing its presence under ADR 0095. Normal application orchestration belongs to Engine.Run through Window; a consumer may explicitly embed the lower-level display and manual engine lifecycle. These entry points are limited to making the existing display and scene lifecycle usable from a separate assembly, under the semantic public-API boundary in ADR 0004.
- `Open` and all native service operations, including disposal, require SDL's main thread and the managed opening thread. A singleton read may occur from another thread. Disposal is rejected during event dispatch.
- A visible Wayland window commits a neutral blank surface at creation so the compositor maps it before the rendering vertical slice. Pixel-size and display-scale events refresh that surface until a renderer replaces it. Hidden windows do not present. The surface remains window-owned; a future renderer must release it before creating its rendering context.
- When a Linux Wayland session or explicit Wayland video driver inherits `GDK_BACKEND=x11`, `Open` changes the native and managed environment to `wayland` before initializing video so libdecor's GTK plugin can provide native desktop decorations. Failed startup or a different selected video driver restores the previous value. The override stays process-wide after successful startup so later GTK use matches the selected display driver; other backend settings and video drivers are left alone.
- When GTK 3 is available for Wayland decorations, the display server installs a process-local style provider for default title bars before creating the window and removes it after destroying the window. It lets the title-bar background cover its border box. This preserves the selected GTK theme while preventing a transparent one-pixel seam with themes whose default title bar has a bottom border. Missing GTK leaves libdecor's fallback available; the engine never edits the user's theme or requires theme-specific launch settings.
- Native window and display getters report observed state. Setters submit native requests and expose rejection as typed C# exceptions rather than storing pretend success. SDL may apply requests asynchronously.
- `WindowMoveToForeground` remains a validated no-op on Wayland: its standard top-level protocol has no foreground request, and SDL's raise path could send an activation token with an input serial and change focus. `WindowRequestAttention` uses SDL's until-focused flash request; on Wayland SDL sends a serial-free activation token as the reference backend does. A native protocol probe checks request generation, while the compositor owns any visible urgency indication.
- Screen wakefulness is a process-wide native request through SDL. On the verified Wayland host, toggling it emitted matching `org.freedesktop.ScreenSaver` inhibition and release calls; actual monitor power policy remains owned by the desktop.
- The keyboard-focus screen query uses the focused main window where the backend can identify it. Wayland cannot expose process-wide keyboard focus or a primary display, so the primary, keyboard-focus, and mouse-focus selectors use screen index zero.
- The native keyboard adapter maps SDL scancodes to the reference physical identities explicitly where enum names differ: US bracket positions are `BraceLeft`/`BraceRight`, the grave position is `Section`, and the ISO key beside left Shift is `QuoteLeft`. Control-valued keys such as Escape resolve to special key identities before Unicode conversion; SDL's unknown key resolves to `None`. Modifier and keypad aliases use explicit maps. SDL 3.4.16 returns fixed keypad keycodes across Num Lock and Shift in a native Wayland probe, while the reference reads the live XKB symbol state. Complete keypad conversion, uncommon F25-F35 identities, and live Latin/non-Latin layout changes require a native Wayland keymap/state bridge and executable layout-switch test; a fixed US-layout table would give false results for custom layouts.
- Desktop and window coordinates use the platform's native units, which vary with DPI policy. Wayland does not supply global top-level window coordinates or programmatic top-level positioning, so operations requiring those coordinates reject use instead of returning SDL's synthetic positions. This is an accepted behavior difference from the pinned Wayland reference, which returns a conventional `(0, 0)` top-level position and silently ignores positioning requests. A native Wayland test verifies the explicit failures and same-screen no-op. Wayland, Android, iOS, tvOS and browser client sizes and pointer coordinates are exposed in pixels; size requests and bounds are converted to SDL logical units using the current window pixel density. A visible Wayland test consumer verified window size and limit conversion through user-driven 1.25-to-1-to-1.25 output transfers; macOS pixel-space parity remains unverified.
- On Wayland, SDL display bounds are logical while the reference's screen size is the output's physical mode. `ScreenGetSize` and the size of `ScreenGetUsableRect` multiply logical bounds by the current mode's pixel density; the position remains the native output position. `ScreenGetScale` uses the main window's current scale for its default selector and rounds the current mode's pixel density upward for an explicit index. The indexed result is two for a 1.25-scale output, matching the observed `wl_output.scale` value; `ScreenGetMaxScale` uses those indexed values. A new or hidden window can have a provisional scale until the compositor assigns a preferred fractional scale; the pinned reference also waits only for the initial xdg configure before returning from window creation. SDL `SyncWindow` is a window-state barrier, not a guarantee that the preferred scale or first buffer is ready. `ScreenGetRefreshRate` uses SDL's precise mode numerator and denominator where present; its rounded float would report 144 instead of the protocol's 143.997 Hz on two outputs of the verified host.
- Wayland output origins and physical mode sizes can produce overlapping public rectangles at mixed scale. The pinned reference backend uses the same combination and selects the greatest overlap directly; do not invent a per-output coordinate conversion. On this three-monitor host, a Wayland protocol trace confirmed that all SDL `xdg-output logical_position` values equal the corresponding `wl_output.geometry` positions, and the native smoke verified the physical mode sizes. Other compositors may report different position sources; this is a documented backend limit rather than a different geometry contract.
- `GetScreenFromRect` uses those public screen positions and physical sizes, truncates each overlap area to whole pixels, and keeps the first display on a tie. Public backend names use `Wayland` and `X11` rather than SDL's lowercase driver tokens. A repeated `MouseSetMode` request for the active mode does not submit a second native grab or cursor operation.
- Changing the main window's screen is a no-op when it is already on the selected display. A floating window retains its offset from the old display origin and is clamped into the target usable area so part remains visible. Fullscreen relocation uses the target display bounds; maximized relocation restores and reapplies maximization because SDL ignores position requests in that state. Wayland's exclusive-fullscreen request uses ordinary compositor fullscreen; other drivers select an exclusive display mode. Multi-display native behavior and compositor refusal remain observable integration concerns.
- Wayland window sizing clamps each requested dimension below one to one, converts requested client pixels to SDL logical units, and reads observed client pixels back. Decorated size reports client pixels. Minimum and maximum requests treat zero as unbounded on each axis, round lower bounds upward and upper bounds downward, reject incompatible native bounds before mutation, and are reapplied when SDL reports a window-scale change. The client rectangle follows SDL's pixel-size event. A native window at density 1.25 passed pixel-size and resize checks, then user-driven moves to density 1 and back passed native limit reapplication after each content-scale event. Wayland focus follows the SDL mouse-focused window; tablet and touch focus still need a native compositor check.
- `HasFeature` keeps stable capability IDs and advertises only integrated services for the active SDL driver and devices; native platform support alone does not imply an Electron2D capability.
- `SetIcon` applies a default icon to the current main window until `WindowSetIcon` successfully overrides it. Wayland accepts only square source images; a rejected SDL icon request leaves `Feature.Icon` false there, while a successful request establishes that capability for this server. The current one-window host does not propagate a default icon to future windows or set a desktop-launcher icon.
- Taskbar progress requests reject Wayland rather than treating SDL's stored progress state as visible progress. [SDL's Wayland guidance](https://github.com/libsdl-org/SDL/blob/main/docs/README-wayland.md) requires a desktop entry and a supporting desktop environment; this GNOME host has no confirmed listener. The separately approved Linux desktop taskbar integration must follow an application host that owns a stable application ID and installed `.desktop` entry, verify a supported desktop notification path, and observe the result natively. Other SDL video drivers currently delegate requests and still need native acceptance checks.
- Pointer mode and cursor shape identifiers retain the complete reference numeric vocabulary. `Max` markers are not selectable. Native cursor translation is explicit; wait/progress and drag/drop identities do not inherit SDL's unrelated numbering.
- Custom cursor images use owned native copies. Their public hotspot is `Vector2`; Wayland's reference implementation converts it to an integer pixel by truncation, matching SDL's integer hotspot. The 256×256 image limit is enforced before native replacement. The first texture/rendering slice supplies `CursorSetCustomImage(Resource?, ...)` for Image and readable Texture resources. Texture.GetImage supplies a temporary owned image; cursor limits and hotspots use its original pixel dimensions. Conversion releases the temporary on success or failure and rechecks display lifetime after custom capture callbacks. Sources stay caller-owned. Compressed cursor pixels remain dependent on the CPU decompression slice under ADR 0039.
- On Wayland, `MouseGetPosition` reports window-relative pointer state multiplied by SDL's pixel density because the compositor does not expose global pointer coordinates; motion, button, and wheel coordinates use the same client-pixel space. Real focused motion and left-button press/release matched the native SDL state. Focused capture produced relative motion; during a 25-second `Confined` check, the pointer reached all four client edges while repeated outward movement retained mouse focus. `HasFeature(MouseWarp)` is false on Wayland, so `WarpMouse` now throws before calling SDL; a prior focused request proved SDL can report a synthetic target without the compositor moving the physical pointer. A protocol-aware backend and compositor support are the trigger for Wayland warping. Advertised desktop backend warps still need physical verification.
- Touch availability includes `Input.EmulateTouchFromMouse` as well as native touch devices. The setting is read when queried rather than cached by the display host.
- `ClipboardHas` means the ordinary text clipboard returns a nonempty string; primary-selection contents do not affect it. A visible, keyboard-focused Wayland child process read the same stable nonempty clipboard and primary-selection text as its parent without changing either selection. The Wayland selection protocol sends offers only to the client with keyboard focus, so a hidden child cannot verify cross-process reads. A private headless Mutter compositor did not focus the SDL publisher or deliver an input serial; no setter publication was exercised. Cross-process publication requires an isolated compositor with an input-capable seat that focuses publisher and reader and supplies a recent input serial, then verifies both owned selections without changing the user's clipboard.
- Wayland IME uses SDL text input and a 1×10 native candidate area. Its public caret position is in client pixels and converts to SDL logical window coordinates using the current window pixel density, matching the pinned Wayland backend's scale conversion. Native readback passed at densities one and 1.25; a user moved the visible test window to the 1.25 output, and a public caret at (125,75) reached SDL at (100,60). Negative composition offsets become zero. Synthetic text-edit and commit events verify state-before-callback ordering; an actual input method and candidate popup remain unverified. `HasFeature(Ime)` currently advertises the integrated SDL path without proving text-input-v3 protocol availability; `HasFeature(NativeDialogFile)` likewise does not probe the FileChooser service. Non-UI availability probes belong respectively in the first native keyboard/text adapter and Linux portal FileChooser slices, with positive and absent-service checks before the aggregate query is complete.
- The host calls `ProcessEvents` before `Engine.AdvanceFrame`. The pump commits typed input through `Input.ParseInputEvent` before scene callbacks; it delivers window, quit, text, and IME notifications as typed events. Mouse/touch emulation is governed by `Input` policies. It does not own the clock, frame scheduling, renderer, or presentation.
- Root Window now subscribes to the already typed committed-text and composition callbacks during its native lifetime and forwards them to the focused scene Control under ADR 0038. DisplayServer remains the owner of SDL text/IME state and never guesses which key produced a later committed string. A real input method and candidate UI remain native acceptance work; per-key Unicode remains an InputEventKey gap rather than being closed by this string route.
- File-drop callback arguments use an ordered, owned path list for each completed drop, following the reference batch boundary. SDL file paths are copied while the native event is valid; a lone file event forms a one-item batch. The old per-path event shape was incorrect and is replaced before the first public release. Partial batches are discarded on window close or disposal, and batch state clears before invoking user code.
- Basic native message dialogs block the owner thread and invoke a typed callback before returning. SDL offers them on Linux Wayland although the pinned Wayland reference does not advertise `FEATURE_NATIVE_DIALOG`. A physical Wayland check selected button one and closed a one-button dialog through its title bar: the callbacks ran before return on the owner thread, and the close reported button zero. The native toolkit therefore cannot distinguish that dismissal from a click on the sole button. File and folder choosers use SDL's asynchronous callback, immediately copy native paths, and deliver the typed callback on the owner thread during a later event pump. Disposal is rejected while a chooser is pending or its result has not been delivered. `OpenAny` and MIME-only filters reject use explicitly. SDL accepts but ignores `showHidden` on Linux, matching the reference there. The first native Linux portal FileChooser bridge must apply MIME filters and verify selected-filter, acceptance, and cancellation behavior on Wayland; selecting files or directories in one chooser requires a separately approved native chooser capability. Advanced menus and option-bearing dialogs retain their separate native UI trigger.
- Native callback failures are aggregated after later queued events are delivered. Re-entry is rejected. Focus events invoke the typed window callback before the application notification; focus loss then releases pressed input even when either delivery fails, while touch indexes and stale modifiers clear before delivery. Input from the native queue is not discarded solely because a focus-loss event preceded it.
- A visible Wayland compositor session delivered a real close request to the typed callback without destroying the window, plus native focus gain/loss and pointer enter/exit notifications. The remaining `WindowEvent` identities depend on the first popup scene host, Android host, HDR output, and macOS title-bar integration respectively; the aggregate vocabulary is blocked until those owners exist.
- Desktop hit testing uses the main window's client rectangle on X11, excluding the native border and title bar. Other desktop drivers retain decorated-bounds approximation until native topmost hit testing exists; Wayland continues to reject unavailable global-position lookup. The SDL resizable flag is only a partial answer to whether the native window manager permits maximization; current fullscreen mode is not a permanent prohibition. A future SDL-owned Wayland capability bridge must observe xdg_toplevel wm_capabilities and integrate MaximizeDisabled.
- Native SDL ownership uses private `SafeHandle` wrappers and deterministic disposal. SDL thread-affine handles must not be released by a finalizer thread; the owning `DisplayServer` must be explicitly disposed.
- SDL3-CS core source is compiled internally into `Electron2D.dll` and refreshed by release tag with `tools/update-sdl3-cs.sh`. The Linux x64 example packages native SDL beside its executable; other target packages remain unverified under ADR 0021.
- Missing renderer, native menus and advanced dialogs, accessibility, speech, mobile, theme, scene window composition, and platform-specialized controls are recorded by exact triggers in the coverage inventory; no inert public methods represent them.

The normal game-facing owner is now Window, opened and closed by Engine.Run. DisplayServer remains available for lower-level services and embedding. Window delegates native operations here and exposes no SDL types; scene/input viewport behavior belongs to Scene.

### Consequences

The runtime can open a native SDL window and translate SDL keyboard, mouse, wheel, touch, window, file-drop, and text events, including a headless Linux dummy-driver verification path. Engine.Run and Engine.RunAsync own the example's monotonic clock, event/frame loop, exit request and teardown. The shared CharacterMovement scene executes through the retained canvas renderer. Mobile/TV/browser startup takes the actual native surface size and skips unsupported size limits; an explicit browser gesture enters document fullscreen. The native Input adapter satisfies ADR 0038's mouse/touch emulation policy. Pointer warping uses main-window client coordinates; defined but unavailable window flags reject requests explicitly. On Wayland, AlwaysOnTop has no SDL top-level hook and NoFocus is supported only for popup-menu windows; both are rejected before SDL changes any internal flag. Exclusive fullscreen selects a native display mode where supported and reports native refusal or missing modes. Wayland maps it to ordinary compositor fullscreen. The Wayland protocol cannot reliably restore a minimized window programmatically; SDL can retain its minimized flag until focus returns.

### Rejected alternatives

- Public SDL handles: violate ADR 0028's backend-neutral public surface.
- A parallel application scheduler inside DisplayServer: duplicates Engine's accepted host-driven timing model.
- Empty methods for unavailable services: would misreport executable platform behavior.

### Related decisions

- [0012: vendored SDL3-CS and Box2D.NET](product.md#adr-0012)
- [0015 and 0016: MainLoop and Engine host boundary](core-object-runtime.md#adr-0015)
- [0021: target platforms](product.md#adr-0021)
- [0028: renderer boundary](rendering.md#adr-0028)
- [0038: typed input adapter trigger](input.md#adr-0038)

<a id="adr-0041"></a>
## ADR 0041: Keep retired service APIs out of DisplayServer

Last updated: 2026-10-06

### Status

Accepted. This narrows the public surface selected by ADR 0040 without changing ownership of the SDL window or event pump.

### Context

The pinned 4.7.2 reference marks 72 `accessibility_*` methods, 96 accessibility enum values, 49 `global_menu_*` methods, and `FEATURE_GLOBAL_MENU` as deprecated. It directs callers to `AccessibilityServer` for the first group and `NativeMenu` or `PopupMenu` for the second. The seven enclosing accessibility enum types have no nondeprecated values. Duplicating these legacy entry points in a new engine would create two public owners for the same future services. Electron2D has no compatibility users requiring the retired names.

### Decision

- Permanently exclude those 225 retired declarations from Electron2D's `DisplayServer`: 72 accessibility methods, seven orphaned accessibility enum types with 96 values, 49 global-menu methods, and one feature value. Remove the previously declared `Feature.GlobalMenu = 0`; retain the remaining feature values at their established numeric IDs.
- This exclusion is confined to the legacy `DisplayServer` placement. The first executable accessibility-service slice owns an `AccessibilityServer` contract, semantic tree, focus and event bridge, and target checks. The first native-menu service slice owns typed `NativeMenu` or `PopupMenu` behavior, menu lifetime, callbacks, and target checks. Their public types and exact signatures require their own accepted decisions before implementation.
- Retain the four nondeprecated display queries for screen-reader activity and increased contrast or reduced animation/transparency as dependency-blocked `DisplayServer` work. Implement each in the first native accessibility/theme integration that can report its value on supported targets.
- A backend capability gap, absent domain, or platform-specific operation alone is not a permanent exclusion; those rows remain blocked with their exact trigger. Native handle interop is addressed separately by ADR 0042.

### Consequences

The coverage register separates 225 permanently excluded legacy declarations from service work that still needs implementation. Numbered feature IDs have a deliberate gap at zero. A future Godot reference update requires a fresh declaration-level classification rather than extrapolating this snapshot's counts.

### Rejected alternatives

- Legacy forwarding stubs in `DisplayServer`: they would permanently duplicate service ownership and claim an API before its behavior exists.
- Excluding all accessibility, menu, or platform-specific behavior: the product's portable 2D contract still needs those useful capabilities when their named integration slices exist.

### Related decisions

- [0040: SDL display server ownership and event pump](#adr-0040)
- [0021: runtime target platforms](product.md#adr-0021)

- [0028: backend-neutral rendering boundary](rendering.md#adr-0028)

<a id="adr-0042"></a>
## ADR 0042: Borrowed native window handles for external integration

Last updated: 2026-10-06

### Status

Accepted for the current desktop subset. This corrects the overbroad draft exclusion of all native handles in ADR 0041. It does not expose SDL's owned window pointer.

### Context

The reference `window_get_native_handle` contract returns operating-system display and window identities for plugins. Those are distinct from the private `SDL_Window*` and renderer handles barred by ADR 0040 and ADR 0028. SDL3-CS exposes SDL window properties for X11, Wayland, Win32, and Cocoa. The eight reference `HandleType` values also include one native view identity and five GL/EGL/GLX context identities. The executable compatibility renderer now owns an SDL renderer and its selected graphics context. [SDL GPU](https://wiki.libsdl.org/SDL3/SDL_CreateGPUDevice) uses Vulkan, Direct3D 12, or Metal, but the accepted [SDL_Renderer fallback](https://wiki.libsdl.org/SDL3/SDL_HINT_RENDER_DRIVER) may use OpenGL or OpenGL ES; its selected driver is observed at runtime.

### Decision

- `WindowGetNativeHandle(HandleType, int)` returns a borrowed pointer-sized `nint` and retains the reference numeric IDs `DisplayHandle = 0` and `WindowHandle = 1`. It validates the main-window ID, runs on the opening SDL main thread, and never returns the private `SDL_Window*`.
- `DisplayHandle` obtains the X11 `Display*` or Wayland `wl_display*`. `WindowHandle` obtains the X11 window ID, Wayland `wl_surface*`, Win32 `HWND`, or Cocoa `NSWindow*`. SDL properties supply these values; an absent value fails explicitly. The caller must not free, close, or retain ownership of a returned handle. It must requery after window state changes and stop using the value after server disposal.
- A driver with no matching native identity fails with `NotSupportedException`. This does not make the corresponding capability permanently excluded. Android `DisplayHandle` needs an EGL display from a real graphics host; Android `WindowHandle` needs an Android/JNI lifetime contract because SDL's Activity accessor returns a local reference that must be released. iOS requires a UIKit bridge to obtain the reference's view-controller identity rather than substituting SDL's `UIWindow*`. These belong in their first executable mobile-host slices and require native target tests.
- `WindowView = 2` remains dependency-blocked. The first native-view integration must specify Win32 renderer/HDC policy and obtain the AppKit/UIKit view with correct borrowed ownership, then verify on target hosts. It is not declared as a nonfunctional enum value now.
- The Linux compatibility renderer exposes `OpenGLContext = 3`, `EGLDisplay = 4`, `EGLConfig = 5`, `GLXVisualID = 6` and `GLXFBConfig = 7` when its selected GL/EGL/GLX driver provides each identity. These follow the same borrowed `nint`, owner-thread and explicit-unavailable exception contract. GPU and software renderers reject the graphics queries. Native graphics identities on other platforms remain incomplete; they are not permanently excluded.
- Capture the context immediately after SDL renderer creation, requiring the selected `opengl`/`opengles2` driver, nonzero context and `SDL_GL_GetCurrentWindow` equal to the owned SDL window. The pinned [SDL GL renderer](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/opengl/SDL_render_gl.c) and [GLES renderer](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/opengles2/SDL_render_gles2.c) create, make current and destroy their context with the renderer. Later getters return the captured identity without switching contexts. SDL handles themselves remain private.
- Validate EGL display and config against the captured native context through `eglGetCurrentContext`, `eglGetCurrentDisplay`, `eglQueryContext(EGL_CONFIG_ID)` and `eglGetConfigAttrib`. SDL 3.4.16's [EGL getters](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/video/SDL_video.c) read shared video-device data, including the most recently selected config; they do not establish window association and must not be forwarded on each query.
- On X11/GLX, use `glXQueryContext` to obtain that context's screen and FBConfig ID, then find the exact ID with `glXGetFBConfigs`/`glXGetFBConfigAttrib`; return its visual ID and borrowed FBConfig. Functions resolve through the existing SDL GL binding. The temporary config array is released with `XFree` from system `libX11.so.6`; the config itself remains display-owned. This native query bridge belongs to the first fallback slice and adds no managed or packaged native dependency. Missing functions/configuration reject the corresponding query rather than substituting an unrelated visual/config.
- Window startup registers the renderer's query with DisplayServer; renderer disposal clears captured identities and window cleanup removes the query. Returned graphics handles become invalid when that renderer closes, even if a caller retained the managed DisplayServer. Callers must not destroy, replace or mutate engine-owned context state. A foreign current context or a different SDL-global EGL config never changes the reported identity.

### Consequences

The desktop display/window interop path is executable and tested on this machine's Wayland and XWayland sessions with SDL 3.4.16. Windows and macOS implementations compile through SDL properties but require native target verification. Android, iOS and native views remain dependency-blocked. Linux graphics identities are executable: Wayland EGL passed with both desktop GL and GLES drivers, and XWayland GLX passed with the GL driver. Tests validate native config/visual identity, foreign-current-context isolation, unchanged rendering, invalid/thread/disposed access and reopen. Forced X11/EGL GLES surface creation failed on this host before handle capture; that path and other-platform graphics identities remain unverified/incomplete. The coverage register has 225 permanent exclusions in this class, all retired service declarations from ADR 0041.

### Rejected alternatives

- Excluding every native handle because SDL handles are private: OS display/window identities serve a distinct external-integration contract and are available through SDL without transferring ownership.
- Returning the SDL window pointer as a substitute: its type, ownership, and semantics differ from the requested operating-system identity.
- Returning an Android JNI local reference as an unqualified `nint`: its required release and lifetime cannot be conveyed safely by the current borrowed-handle contract.

### Related decisions

- [0040: SDL display server ownership](#adr-0040)
- [0041: retired service APIs](#adr-0041)
- [0021: runtime target platforms](product.md#adr-0021)
- [0028: SDL GPU and SDL_Renderer boundary](rendering.md#adr-0028)

<a id="adr-0043"></a>
## ADR 0043: System theme queries and typed change notification

Last updated: 2026-10-06

### Status

Accepted. This amends ADR 0040's theme gap for the three declarations covered here; other desktop theme services retain their separate integration trigger.

### Context

The accepted display host already owns SDL video initialization and event pumping. SDL exposes the current light, dark, or unknown system theme and a native system-theme-change event. On Linux Wayland and X11, however, SDL maps an unset portal preference to Unknown just like an unavailable portal, while the corresponding capability query depends on whether the Settings portal interface is supported. The value and capability therefore need distinct sources.

### Decision

- `IsDarkMode()` reads SDL's current theme and returns true only for Dark; on Linux Wayland and X11 it also requires portal support. `IsDarkModeSupported()` on Linux Wayland and X11 queries the session D-Bus for `org.freedesktop.portal.Settings` interface version at server creation, accepts version one or newer independently of the current color-scheme value, and caches the result. A missing bus, portal, or native D-Bus library returns false; reopening refreshes support. Other drivers retain the known-Light-or-Dark SDL criterion. Both methods follow the display server's opening-thread and disposal rules.
- The portal probe uses a bounded 250 ms native `libdbus-1.so.3` call. SDL remains the theme-value and change-event source; no second listener, runtime command, or managed package is added.
- `SystemThemeChanged` is a typed C# event in place of callback registration. `ProcessEvents` delivers SDL's system-theme-change notification in native queue order. Handler failures follow the existing pump policy: continue draining events and aggregate failures afterward.
- Classify these three reference declarations as implemented for the current Linux Wayland gate after an isolated native Settings portal test verifies version zero, an unset preference, and dark/light/unknown change notifications through SDL and the typed event. A physical GNOME preference change and other targets remain separate verification work. Accent/base colors, high-contrast and related preferences, and other desktop services remain blocked on their named native platform integration; no default color or inert event is exposed.

### Consequences

Applications can observe SDL's reported light/dark preference and react to reported changes. A Linux Wayland or X11 server reports Settings support even when its appearance preference is unset. The dummy-driver tests verify delegation, ordering, thread affinity, and callback failure handling. Native Wayland and X11 smokes verified that a version-two portal reports support and an unavailable session bus reports no support. A private Wayland session bus then verified version zero, an unset preference, and three real `SettingChanged` signals with state visible inside the typed callback. Physical desktop preference changes and other runtime targets remain unverified.

### Related decisions

- [0040: SDL display server ownership and event pump](#adr-0040)
- [0002: typed events](product.md#adr-0002)
- [0021: runtime target platforms](product.md#adr-0021)

<a id="adr-0044"></a>
## ADR 0044: Window notifications and event failure completeness

Last updated: 2026-10-06

### Status

Accepted. This extends ADR 0040's main-window notification and event-failure contract.

### Context

The pinned display reference includes a window DPI-change notification and requires a refresh-rate fallback of `-1.0` when no rate is known. SDL reports window content-scale changes, but its content scale is not a physical-DPI measurement; it also uses a zero refresh rate for an unspecified mode. The wheel dispatcher must deliver both axes even if a user callback fails. The reference rectangle-change callback receives a complete client-area rectangle on either move or resize. X11 supplies desktop client coordinates; Wayland does not expose global top-level positioning, and the reference's Wayland backend deliberately publishes position `(0, 0)`. SDL move and resize payloads contain their changed coordinate or dimension at event time, whereas querying the window later in the queue can return newer state.

### Decision

- Expose `WindowDpiChanged` as a typed main-window notification for SDL's `WindowDisplayScaleChanged` event. Deliver it through the existing owner-thread event pump in native queue order, filter foreign windows, and aggregate callback failures after later events. Classify the reference DPI-change identity as partial: this signal reports content-scale changes and requires real-host checks for DPI coverage. `ScreenGetDpi` remains blocked on a physical-DPI bridge.
- Normalize an unspecified, nonpositive, or nonfinite SDL display-mode refresh rate to `-1.0` in `ScreenGetRefreshRate`, preserving positive finite hertz values. This corrects the already documented fallback without adding a new public API.
- Deliver both axes of a single SDL wheel event even when one axis's callback fails. Preserve both failures for the existing event-pump aggregation policy; a pressed wheel button is still released when its press callback fails.
- Expose one typed `WindowRectChanged` event carrying a complete main-window client `Rect2i` after each effective native move or resize, in event-queue order. Cache the rectangle, update it from each SDL event payload before callbacks, and suppress unchanged notifications. On Wayland, the event position is the documented `(0, 0)` convention, not an authoritative global desktop position; ADR 0040's global-position operations remain unsupported there. Retain owner-thread delivery, foreign-window filtering, callback-failure aggregation, and re-entry rules. This single event adapts callback registration; no parallel split or inert registration API remains. Classify parity as partial pending native X11 and Wayland checks of compositor event coverage and ordering.

### Consequences

Dummy-driver tests cover scale-event order and foreign-window filtering, wheel callback failure across both axes, the unspecified refresh-rate fallback, and rectangle payload/order/failure semantics. They do not prove physical DPI behavior, compositor notification coverage, or rectangle coordinates on all targets. No renderer or other platform host is added.

### Related decisions

- [0040: SDL display server and typed main-window host](#adr-0040)
- [0021: runtime target platforms](product.md#adr-0021)
