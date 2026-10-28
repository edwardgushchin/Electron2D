# Display domain

[Android system back](../components/window-runtime.md#system-back-navigation) uses actual framework callbacks and a retained display-owner request batch, including drop-input pumps and lifetime/error handling.

Last updated: 2026-10-27

Native driver identity is retained per opened display context for capability queries; device-dependent availability remains live. The [physics window allocation check](../components/physics-window-allocations.md) covers its passive-pointer hot path.

## Responsibility

[Native mouse input regions](../components/window-input-region.md) connect Window
authored policies and DisplayServer's per-window operation to actual native input
regions, including geometry/scale refresh and renderer surface replacement.

Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

Own the native display connection, borrowed operating-system display/window and supported graphics-context identities, and window/input event boundary for a portable 2D runtime. Rendering, clock policy, scene composition, and game object lifetime belong to their respective domains.

## Component inventory

| Component | Responsibility | Status |
| --- | --- | --- |
| [Display server](../components/display-server.md) | SDL video lifetime, main window, borrowed desktop display/window identities, display, system-theme and pointer queries, event translation | Partial native host; coverage and limits documented |

## Public surface

[DisplayWindowEvents](../classes/DisplayWindowEvents.md), obtained by static `DisplayServer.GetWindowEvents`, owns typed per-native-window subscriptions and retirement. Its parsed-input phase precedes scene delivery; native notifications follow existing window callbacks. See [the connected component](../components/display-server.md#per-window-typed-events).

[`DisplayServer`](../classes/DisplayServer.md) provides process singleton access, main-window and display queries, native light/dark-theme queries, [borrowed native display/window identities](../classes/DisplayServer.HandleType.md) through `WindowGetNativeHandle`, typed native notifications including `SystemThemeChanged`, `WindowDpiChanged`, and complete client-rectangle changes through `WindowRectChanged`, clipboard and IME state, pointer control, basic native dialogs, and deterministic disposal. The handle query is implemented in [`DisplayServer.Windows.cs`](../../src/Servers/Display/DisplayServer.Windows.cs) over SDL window properties. See the complete public member reference and [coverage inventory](../coverage/classes/DisplayServer.md).

The existing `TextInput` and `TextEditing` events remain the native full-string/preedit boundary. During Engine.Run, root Window forwards them to SceneTree after DisplayServer commits IME state; focused Control receives the typed text stream. DisplayServer does not synthesize or attribute an InputEventKey from a later SDL commit.

## Dependency direction

Display depends on Core object/math, Input, Image, and internally compiled SDL3-CS. On Linux Wayland and X11, the internal Settings capability probe also depends on system `libdbus-1.so.3`; it adds no managed package. On Wayland, optional GTK 3 supplies themed libdecor decoration and a temporary title-bar background correction for themes with a bottom border. Engine's scheduler owns neither display nor rendering logic. Run/RunAsync call the display pump before advancing the scene; manual consumers perform that ordering explicitly. The future rendering domain may use internal native window ownership but keeps SDL window and renderer handles private. The explicitly borrowed operating-system display/window and supported graphics-context identities in `DisplayServer` do not transfer SDL or renderer ownership.

## Domain-wide invariants

Taskbar progress is unavailable on the current Wayland backend and fails explicitly before native mutation. An application-host-owned desktop entry and a separately approved desktop-shell progress integration with observable native delivery are its implementation trigger. Other native drivers still delegate to SDL without a visible-progress guarantee.

The process has at most one active server and one owned native window. Calls, including native theme and handle queries, run on the opening SDL main thread except the singleton read and internal file-dialog completion callback. Game callbacks run on the owner thread. `WindowGetNativeHandle` validates the main-window ID and yields a nonzero borrowed value or fails explicitly; callers must not close or free it, must requery after native window state changes, and must stop using it after disposal. Native failures surface explicitly. Input state commits before callbacks and callback failure does not abandon later native events. System-theme notifications retain native queue order and callback failure does not abandon later events. Pending native file dialogs and undelivered results prevent disposal. The SDL window pointer and other SDL-owned handles remain internal and are deterministically released on the owner thread.

The Wayland keyboard-focus screen selector resolves to the primary display because the compositor does not expose process-wide keyboard focus.

Wayland `WindowMoveToForeground` validates its window ID and does not submit an activation request. `WindowRequestAttention` submits a serial-free activation token, leaving any visible urgency indication to compositor policy. The native protocol probe verifies those requests, not a particular shell animation.

Main-window display-content-scale notifications retain native queue order; events for other windows are ignored. A failing handler is reported after later queued events. Both axes of one wheel event are attempted even if an earlier axis callback fails, and both failures are retained. Refresh rate uses the current mode's precise numerator and denominator when available; an unavailable, nonpositive, or nonfinite rate is reported as `-1`.

`WindowRectChanged` commits a complete client rectangle before calling handlers on the opening thread, preserving native move/resize queue order. The rectangle's size is in physical pixels on Wayland, Android, iOS, tvOS and browsers, and platform-native client units elsewhere. Mouse and touch use this same native surface, with viewport transforms mapping input to game coordinates. Its position uses platform-native desktop coordinates, except that Wayland reports the conventional `(0, 0)` because no reliable global top-level position is available. Unchanged rectangles and foreign-window events do not notify; callback failures follow the pump's aggregate policy.

Desktop point hit testing ignores hidden/minimized windows. X11 uses half-open client bounds, excluding title and border; other supported desktop drivers use decorated bounds. Wayland rejects the query because it has no reliable global window position. SDL keyboard events supply logical, physical, and unmodified layout-label identities independently. Physical brackets, grave/ISO positions, modifiers, and keypad scancodes use explicit reference-aligned identities. The eight left/right control, shift, alt, and GUI scancodes set the matching key `Location`; other keys use `Unspecified`. SDL's Wayland keypad lookup ignores Num Lock and Shift for these conversion methods, so a native keymap/state bridge is required for complete logical-key and label parity. Committed text is delivered separately. SDL key events contain no produced text scalar, and text-input events can carry multiple scalars or an IME commit without a key-event identifier. Populating key-event `Unicode` requires a native per-key Unicode source with verified IME/composition semantics in the first native keyboard/text adapter slice.

Native key events exclude their own modifier family from the event mask while preserving other held families. The queued raw SDL modifier snapshot remains available for subsequent pointer events. Targeted synthetic SDL dummy and Wayland checks cover left and right Shift, Control, Alt, and GUI on press and release; physical keyboard delivery remains unverified.

Mouse motion, button and wheel translation validates finite native coordinates, derived velocity and wheel amounts before updating tracked button/timestamp state. The pump reports malformed events after continuing to later queue entries. A focused Wayland pointer run checks invalid-event continuation, fractional wheel factor, timed velocity and captured-mode zero velocity at pixel density one; fractional density and physical pen hardware remain unverified.

Touch translation validates finite native contact position, movement, pressure and velocity before contact/index/timestamp mutation. A duplicate beginning rejects without consuming an index; invalid motion/end leaves the contact available. A focused synthetic Wayland queue check verifies later valid drag/cancel and index reuse. Native double-tap and pen data remain absent.

Screen selection for the current window is a no-op. On desktop drivers with global coordinates, changing screens preserves a floating window's relative offset with work-area clamping and preserves maximized or fullscreen intent through native mode-aware transitions. Wayland cannot request relocation of a top-level window to another display.

On Wayland, output positions retain compositor coordinates and screen dimensions use physical pixels recovered from SDL mode pixel density. The screen rectangle can therefore combine a compositor position with a physical size, matching the native output contract rather than a uniform logical desktop rectangle.

## Current limitations

The first native Input adapter now has mouse/touch emulation, buffered mouse motion, pointer modifiers, and explicit input flush. File drops are delivered as ordered completed batches. System-theme queries expose SDL's light/dark/unknown preference. On Linux Wayland and X11, support is determined by a version-one-or-newer Settings portal rather than the current preference, and is cached when the server opens. Other drivers use known SDL light/dark state for support. The typed theme event forwards SDL change notifications, not every appearance-setting change; real theme transitions remain unverified. Pointer warping uses window-local coordinates; Wayland pointer position uses window-relative SDL state, while top-level window-position operations reject Wayland. Other coordinate values use platform-native units; exact pixel-space parity needs native target checks. `HasFeature` advertises only integrated capabilities. Unsupported but defined window flags reject calls explicitly; Wayland exclusive fullscreen requests ordinary compositor fullscreen, while other drivers select a native mode. Native message dialogs and basic asynchronous file/folder choosers exist. SDL cannot apply MIME-only filters or select a file or folder in one chooser; the first native Linux portal FileChooser bridge must implement MIME terms, while combined selection needs a separately approved chooser capability. Real Wayland dialog selection and cancellation remain unverified. Borrowed display identities are available on X11 and Wayland; borrowed window identities are available on X11, Wayland, Windows, and macOS. Unsupported video drivers reject native handle queries. Android display/Activity handles need a graphics host and JNI lifetime contract; iOS view-controller identity and macOS/iOS native views need UIKit/AppKit integration; Win32 view access needs a renderer/HDC policy. These are dependency-blocked under [ADR 0042](../decisions/display.md#adr-0042). The Linux compatibility renderer now supplies verified window-associated GL/EGL/GLX identities. The native query bridge captures exact context/config/visual identity at creation, preserving it across foreign context switches. GPU/software reject these identities; other-platform graphics paths and native views remain incomplete. Deprecated accessibility and global-menu members are excluded from DisplayServer under [ADR 0041](../decisions/display.md#adr-0041); their useful service capabilities remain dependency-blocked. The display server has no renderer or built-in frame clock. Engine.Run owns the ordinary clock and frame wait; its Window consumer receives the runtime project’s transitive Linux native package. A neutral initial Wayland surface makes its blank window visible; scene rendering has Linux Wayland GPU/compatibility checks; other-platform packaging remains unverified. Surface and semantic differences from the official API, service families requiring absent domains, and their exact triggers are listed in the coverage inventory.

`WindowDpiChanged` maps SDL's effective window content scale to the typed main-window event. The [native scale qualification](../components/window-runtime.md#native-dpi-scale-reproduction) verifies real transitions, committed viewport state, physical descendants and GPU/compatibility pixels across sixteen CLR/strict NativeAOT Wayland profiles. Physical display metrics and the separately reproduced standalone headless FIFO wait retain their own limits.

The full-rectangle callback has not been checked against real compositor move/resize sequences. Wayland's conventional zero position does not enable the global-position query or setter, which still reject that driver.

Texture-backed custom cursors use the Resource entry point, accepting Image or readable Texture and copying its original pixels into an owned native cursor. The temporary Texture.GetImage result is released on success and failure. Logical size overrides do not resize native cursors.

## Verification

The visible Wayland `ELECTRON2D_TEST_DISPLAY_WINDOW_EVENTS=1` probe received one compositor close request without destroying the window, plus two focus gains, one focus loss, one pointer entry, and one exit through typed events. Dummy and synthetic Wayland checks cover queue ordering, filtering, focus notification order, pressed-input release, and callback failure continuation. The `WindowEvent` aggregate remains dependency-blocked by future popup, Android, HDR, and macOS title-bar integrations.

The full Wayland native smoke passed the taskbar rejection and unchanged-SDL-state checks. A desktop taskbar indication remains unverified because this GNOME session has no confirmed progress notification path.

Wayland screen-wakefulness requests were checked against both public state and actual session-bus inhibition calls; physical monitor power transitions were outside this short probe.

Wayland and X11 theme capability were checked with a live version-two Settings portal and an unavailable session-bus address. A separate private Wayland session bus verified version zero, version one with an unset preference, and three native light/unknown/dark notifications through the typed event without changing the user's GNOME preference. A physical desktop preference transition remains unverified.

The Wayland probe found three monitors, including one at 1.25 compositor scale. SDL reported its 1536×864 logical bounds and 1.25 mode pixel density; `ScreenGetSize` and `ScreenGetUsableRect` returned 1920×1080 physical dimensions, while indexed `ScreenGetScale` and `ScreenGetMaxScale` returned two. A Wayland protocol trace independently confirmed output scale two there and one on the other monitors, plus matching SDL and `wl_output` origins. An initial window-scale query sometimes reported one despite an estimated 1.25 output, while later runs reported 1.25 and a 400×300 pixel buffer for a 320×240 logical window. A separate hidden-window probe observed that showing and presenting its first buffer can change both the selected output and its scale. A user-driven move between densities 1.25 and one triggered two native scale events with matching pixel size and constraints. The first renderer integration must verify initial presentation and scale-event ordering before claiming that initial window scale is settled.

`env -u LD_LIBRARY_PATH ELECTRON2D_TEST_DISPLAY=1 SDL_VIDEODRIVER=dummy dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-build` covers native-handle numeric IDs, invalid IDs and categories, unsupported-driver failure, theme-query mapping, synthetic theme-event order and callback-failure continuation, ordered pointer enter/exit and content-scale callbacks with foreign-window filtering and failure continuation, the unavailable refresh-rate fallback, and preservation of both wheel axes after a callback fails. A published Linux x64 test executable loaded its packaged SDL 3.4.16 library without `LD_LIBRARY_PATH`; its separate native smoke passed window and borrowed-handle checks under Wayland and XWayland. The actual desktop preference was not changed for the isolated theme test. Physical GNOME theme transitions, physical DPI behavior, and equivalent native checks on Windows, macOS, Android, iOS, a standalone Xorg session, and Web have not been verified. Other behavior and packaging limits are recorded in the [component verification](../components/display-server.md#verification). Before the Window/Engine.Run migration, the self-contained application host loaded the packaged Linux native library without developer environment settings, received real arrow-key input, updated a scene node, and exited cleanly through Escape and the window close control. Its selected GTK theme displayed without a transparent title-bar seam after the process-local correction. Other target platforms are not current release gates under [ADR 0021](../decisions/product.md#adr-0021).

The same dummy run verifies full client-rectangle snapshots after move and resize events, native queue order, unchanged and foreign-window suppression, off-thread rejection without consuming events, and callback-failure continuation. Real compositor move/resize behavior and Wayland's zero-position convention have not been verified.

An X11 native smoke checked client hit boundaries and title/border exclusion where SDL reported a nonzero border. The dummy suite checked scancode-derived label state separately from logical keys, release, non-Latin label conversion, and left/right/unspecified location for nine scancodes on both press and release. Other desktop-driver hit bounds and physical keyboard layouts remain unverified.

The X11 smoke also verified a normal-window transfer between two XWayland displays, including the observed screen, relative position, and retained mode. Dummy checks cover same-screen no-op and invalid selectors. Maximized and fullscreen transfers still need native multi-display verification.

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040), [ADR 0041](../decisions/display.md#adr-0041), [ADR 0042](../decisions/display.md#adr-0042), [ADR 0043](../decisions/display.md#adr-0043), [ADR 0044](../decisions/display.md#adr-0044), [ADR 0038](../decisions/input.md#adr-0038), [ADR 0028](../decisions/rendering.md#adr-0028), and [ADR 0021](../decisions/product.md#adr-0021).

Graphics handle verification uses `ELECTRON2D_TEST_RENDER_HANDLES=1`: Wayland EGL/GL and EGL/GLES, XWayland GLX, native identity checks, a foreign context/config, unchanged pixel output, thread/ID/disposal rejection and reopen. A forced X11/EGL GLES probe failed native surface creation before handle queries. Other platforms remain unverified.

GetSwapCancelOK exposes the retained DisplayServer platform dialog-order convention with active-service/owner guards. [Embedded dialogs](../components/dialogs.md) use the same internal policy for pre-startup authoring and combine it with the typed project override. Other-platform acceptance remains separately gated.

[File dialogs](../components/file-dialogs.md) now execute scoped browsing, five selection modes, typed custom options, filters, menus, overwrite/folder workflows and recoverable desktop Linux trash. The shared FileDialogMode identity spans the custom browser and DisplayServer; native-file-extra and foreign platform gates remain explicit. Six permanent ui_filedialog actions use the existing InputMap settings loader.


## Presentation policy

The main window exposes [DisplayServer.WindowSetVSyncMode and WindowGetVSyncMode](../classes/DisplayServer.md#presentation-synchronization), with [VSyncMode](../classes/DisplayServer.VSyncMode.md) identities. Native unsupported modes fall back to Enabled and the getter reports the applied mode. Engine.MaxFPS independently caps host frames. Managed video-driver environment edits are honored at display/compute initialization with the previous native hint restored afterward. The [WaterPlayground](../../examples/WaterPlayground/README.md) consumer verifies the connected policy, including XWayland immediate presentation.


Containing-window notifications now reach physical Node descendants in parent-first
order, stop before nested Window scopes and continue after callback failures.
WindowNotificationTests exercises eight event kinds through embedded/public and SDL
dummy routes plus 1024 zero-byte warmed operations. DPI/position platform limits and
missing go-back/HDR sources remain open; see [window runtime](../components/window-runtime.md#containing-window-notifications).

Ordinary desktop Window children now execute [independent native hosts](../components/window-runtime.md#independent-native-windows), sharing one display/event service and scene renderer. GPU device targets are shared; compatibility contexts replay frozen commands with cold history transfer. Native popup surfaces execute with the limits below; platform-specific decoration controls retain separate prerequisites.

## Native popup surfaces

Native desktop popups use real parent-relative SDL surfaces when `PopupWMHint` is enabled. `PopupOnParent` accepts parent-client geometry and works without fabricated Wayland desktop positions. Public native `Position`, explicit global `Popup(rect)` and display-centered placement retain the global-position capability guard; centered geometry uses the complete parent display, while fitting uses its usable area. Attached embedded children keep their existing viewport coordinates. Runtime control anchors compose the source viewport transform, including nested and scaled hosts.

The native role is fixed-size and borderless, cannot minimize/maximize/fullscreen, and follows its physical parent's display. Hide before changing native role or transparency. Native children recreate their identity after hide. Retiring or hiding a native parent first retires popup descendants; independent ordinary child windows survive parent hide. Popup visibility observer failures are reported after required retirement and committed visibility. SDL popup-done force-hides without emitting a user `CloseRequested`; ordinary close requests and scene cancellation retain their own behavior.

Transparent native PopupPanel/PopupMenu output executes through the compatibility backend. SDL 3.4.18's GPU API rejects transparent windows; GPU native menus require explicit `Transparent=false` before showing. The unsupported request releases its attempted native lifetime and preserves hidden state. Embedded transparent popups continue to execute on both renderers. Native titled-popup variants without the popup role, live embedding migration, foreign-platform acceptance and native system-menu service retain their exact coverage prerequisites.

`NativePopupWindowTests` uses `ELECTRON2D_TEST_NATIVE_POPUPS=gpu` or `compatibility` with the Release runner. It exercises native nested parenting, IDs, outside dismissal, owner-safe rectangles, no-focus and exclusive policies, forced retirement, callback failure, real menu/option/panel/context output, and fresh-process public scene loading. Active focus/render and idle/render measurements cover 64 owner-thread intervals after 32 warmup intervals. SDL event construction, cold model edits/readback, native allocator counts, compositor screenshot acceptance, physical input and other platforms are outside the measured interval.

Public [native relationship control](../classes/DisplayServer.md#native-window-relationships) uses the retained per-window native owner for parenting, pending/effective modality and attached scene instance identity. Cycle checks, retirement, rollback and scene-policy boundaries remain in the shared display path.

The [loading-image slice](../components/boot-images.md) now carries authored main-window transparency into native creation. Compatibility alpha and explicit GPU rejection/fallback use the existing native-window lifetime.

## Native semantic output

[Native accessibility](../components/accessibility.md) owns the static AccessibilityServer service, real Linux AT-SPI application registration, typed semantic tree and display-owner callbacks. Native windows own their root lifetime. Automatic scene output reuses GUI focus/activation and stored Control metadata; optional host libraries and unsupported target profiles remain explicit. [ADR 0114](../decisions/accessibility.md#adr-0114) defines its boundary.

Native numeric accessibility now publishes Slider/ScrollBar/SpinBox and progress state, typed numeric writes and small/page actions. Requests reuse ordinary Range mutation and shared notifications; progress displays and non-editable inputs are read-only. See [ADR 0114](../decisions/accessibility.md#adr-0114) and [the accessibility component](../components/accessibility.md) for exact protocol and verification limits.

Semantic relationships now connect stored Control paths and live radio groups to native direct/reciprocal links, validation errors, virtual descendant focus and internal hyperlink anchors. Ownership and exercised limits are in [native accessibility](../components/accessibility.md#semantic-relationships).

Native accessibility now includes [text and editing](../classes/AccessibilityServer.md#native-text) for LineEdit/TextEdit/CodeEdit, with typed manual producers and copied shaped text. The owning reference records exact native, allocation and platform limits.

Native accessibility scrolling now connects existing scrollable GUI sources to AT-SPI placement and typed actions. [The service contract](../classes/AccessibilityServer.md#native-scrolling) owns coordinate, clipping, lifetime and verification boundaries.

[Native collection accessibility](../classes/AccessibilityServer.md#native-collections) connects ItemList, Tree/TreeItem and TabBar/TabContainer source records to real Selection/Table/TableCell requests, expansion and ordinary GUI selection. The owning reference records identity, lifetime and exercised limits.

[Native live and popup accessibility](../classes/AccessibilityServer.md#native-live-and-popups) connects typed live policy, explicit tooltip/context actions and real PopupMenu/OptionButton selection. Authored policy survives scenes; runtime menu and native identities retain their own lifetime.

[Accessible GUI drag and custom native actions](../classes/AccessibilityServer.md#custom-actions-and-accessible-drag) connect focused keyboard commands, typed source/target payloads, preview ownership, source context and native callbacks. LineEdit rejects incomplete capacity before consuming data; text caret/history and intrinsic read-only guards remain active.

[Contextual native navigation](../classes/AccessibilityServer.md#contextual-navigation) connects custom container position/help, direct keybinding metadata from real button/menu sources, class names and role descriptions. The owning contract defines override, lifecycle and verification boundaries.

[Native hyperlink destinations](../classes/AccessibilityServer.md#native-hyperlinks) connect LinkButton URI authoring and generic producers to external AT-SPI consumers, retaining internal targets and the existing activation pipeline.


[Producer models](../classes/AccessibilityServer.md#producer-models) let native action handlers resolve the current typed model by element RID. The slot follows native element lifetime and stays outside authored scene storage; the owning reference records release, type and verification contracts.

## Clipboard images

Static `DisplayServer.ClipboardGetImage` and `ClipboardHasImage` connect native
MIME offers to existing owned Image decoders and ImageTexture consumers. The
[component](../components/clipboard-images.md) defines selection order, nullable
results, cold predicate decoding, data budgets, precision and owner/lifetime rules.
Native Wayland verification uses an isolated compositor and external producer.

[Native modal alerts](../components/native-alerts.md) share native-text validation with DialogShow and preserve unreported cancellation as -1; owned callbacks can show a nested alert.

[Linux portal file choosers](../components/native-file-portal.md) provide MIME/extension rules, exact returned-filter identity and local file paths through the existing DisplayServer/FileDialog lifecycle. Positive/absent/version probes and actual installed Wayland acceptance/cancellation execute; advanced native options and combined file/directory mode remain separate dependencies.
