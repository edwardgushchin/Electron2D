# Window runtime component

Last updated: 2026-10-17


Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

## Scope and types

[Window](../classes/Window.md) derives from [Viewport](../classes/Viewport.md), which derives from the neutral Node. Window owns the [WindowMode](../classes/WindowMode.md) and [Flags](../classes/WindowFlag.md) identifiers. A consumer configures a root Window, adds scene children, and calls Engine.Run or Engine.RunAsync. The root window connects scene processing and retained canvas drawing through [RenderingServer](../classes/RenderingServer.md). Broader rendering and GUI APIs remain incomplete.

[ADR 0028](../decisions/rendering.md#adr-0028) selects HLSL and GLSL compilation at project import/build, followed by one SPIR-V validation, reflection and GPU-program path through SDL3-CS/SDL_shadercross. Compatible SPIR-V from third-party compilers uses the same path. Direct language support also requires diagnostics, material parameters, textures, consistent bindings and backend checks. The current [shader/material integration](shader-materials.md) executes typed fragment uniforms and sampled textures on Linux Wayland/Vulkan. The SDL_Renderer fallback rejects arbitrary shaders explicitly.

## Runtime flow

Engine reserves its idle state, opens the native window through DisplayServer and initializes RenderingServer before creating SceneTree. On Android, iOS, tvOS and browsers the native surface sets the observed pixel size. Default size-limit calls are omitted on these profiles; nonzero configured limits fail explicitly at startup. Browser startup fills its document. RunAsync yields between frames on the native owner thread and reserves Android startup before its internal activity enters the native video thread. It publishes the tree before ready, drives the native event pump before fixed/process frames, and submits the canvas after scene processing. Cleanup disposes the scene, rendering resources and display in that order. MaxFPS uses unscaled monotonic time; native events continue during bounded waits. SceneTree.Quit requests exit and returns its code from Run or the RunAsync task. Window.CloseRequested precedes the default AutoAcceptQuit decision.

Window properties configure title, positive client size, minimum/maximum constraints, optional desktop position and screen, mode, four executable policies and visibility. Mode queries report observed native state; flag queries retain accepted configuration and are stored in PackedScene. Unsupported policies reject use, and platform refusal does not commit a requested flag. Native calls inherit DisplayServer platform capability failures. Window.Position is the native desktop position and is rejected on Wayland. Window declares its own Visible, Show/Hide and VisibilityChanged API; it has no Entity transform or CanvasItem drawing surface; inherited viewport transforms place child canvases. GetVisibleRect uses a zero client origin. SizeChanged follows client-size updates, never mere desktop movement.

Interactive `Window.StartDrag` and `StartResize` remain blocked with their DisplayServer counterparts: SDL hit testing identifies draggable edges but does not start an interactive move or resize. An X11 window-manager request or Wayland xdg_toplevel request with the initiating pointer serial, followed by compositor verification, is required.

Window also forwards effective mouse entry/exit, content-scale changes and completed file-drop snapshots before frame callbacks. Native IME activation/caret placement, taskbar progress requests, decorated geometry, maximization capability and centering are available on Window with the existing platform limits. Signal failures remain visible after the native queue drains; scene and native cleanup still run.

While active, Window additionally subscribes to DisplayServer committed-text and composition callbacks. It forwards each whole string into the root SceneTree's focused Control route and sends composition updates through the existing scene-wide IME notification before the focused typed callback. It unsubscribes before closing the native display. Text widgets still control when `SetIMEActive` is enabled and own their caret/preedit lifecycle. The bridge does not infer a producing key from a later SDL text event or activate nested/cross-window routing.

Viewport shares SceneTree's current handled-input flag. PushInput accepts client input by default and viewport input with inLocalCoordinates true. Conversion removes the final transform, owns and finally disposes positional copies, and preserves caller input. Neither path changes Input polling state. Child nodes discover their Window/Viewport through ancestor lookup.

## Dependencies and invariants

- Engine.Run and Engine.RunAsync depend on SceneTree and Window; Window depends on DisplayServer and RenderingServer. SDL bindings stay internal to those backend implementations. All types remain in Electron2D.dll.
- One active native root is supported. Child Viewports are rejected before hierarchy mutation; direct SceneTree(Window) activation is rejected unless Engine.Run has opened that root.
- Attached mutation and native calls use the owner/main thread. Quit and MaxFPS configuration accept cross-thread calls.
- Native services opened directly through DisplayServer must finish before shutdown. Pending asynchronous file dialogs can reject native disposal under the existing DisplayServer contract; Run reports the cleanup failure and DisplayServer remains available for completion/release. Window exposes no asynchronous dialog API yet.
- Engine remains reserved throughout scene exit, disposal and native cleanup. Manual frame/stop/tree-disposal interference is rejected. All owned cleanup stages are attempted and failures remain observable.
- Validation/busy-engine rejection preserves caller ownership. After reservation, failed startup also disposes the transferred root. A later run uses a new Window.
- PackedScene stores the title and size/limit configuration plus inherited stored Node properties. Position is an optional platform startup request, not stored scene data. CurrentScreen follows the same optional-request rule. Mode and the four supported policies are stored; unset/default policies do not issue unsupported startup requests.

## Verification and limits

WindowRuntimeTests covers detached configuration/validation, packed reconstruction, native title/visibility, root discovery, input-before-frame ordering, viewport input borrowing/handling, frame limiting, quit/close policy, cross-thread quit, lifecycle interference, callback failure cleanup, startup ownership and repeated runs. Mode/flag startup and mutation, packed policy reconstruction, unsupported-flag rollback, screen validation, IME enable/disable, retained file-drop data and window-signal failure continuation are also covered. Dummy and native Wayland executions passed; dummy accepts native flag setters without applying them, so native policy assertions run on real backends. The preceding root-lifecycle slice also passed as a published self-contained linux-x64 test binary on Wayland with LD_LIBRARY_PATH unset; that packaging contains Electron2D.dll and native SDL through the runtime project. This controls extension was checked through dotnet run with LD_LIBRARY_PATH unset. Native Wayland tests use injected events, not physical user input or visual acceptance. Repeated SDL/GTK initialization emits a GTK locale warning. Canvas pixel and warmed render-allocation checks are recorded in [Canvas rendering](canvas-rendering.md). Root viewport Control pointer/focus routing has managed checks; native GUI acceptance, other platforms, full performance acceptance, content scaling, complete interactive GUI, offscreen targets and multiwindow behavior remain unverified or absent.

The `ELECTRON2D_TEST_WINDOW=1` integration profile requires a real native window driver such as Wayland; dummy does not implement its startup flag operations. Its pointer-event phase shows the window after checking hidden geometry, before injecting events that require visible content. Detached placement and content-scale checks have their own managed profiles.

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0016](../decisions/core-object-runtime.md#adr-0016), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028), [0040](../decisions/display.md#adr-0040). Per-member incomplete dependencies live in the [Window](../coverage/classes/Window.md) and [Viewport](../coverage/classes/Viewport.md) coverage pages.

Changed Window.Title values request selected-scene configuration-warning refresh before TitleChanged, after committing the native and stored title. Equal assignments do nothing. PathRenderingTests verifies this ordering and committed state after a throwing listener on Wayland and dummy. The base diagnostic API creates no editor UI.

CanvasTransform and GlobalCanvasTransform are runtime, non-stored Viewport descriptors. Root drawing and pointer/input conversion share the [canvas coordinate contract](canvas-rendering.md#viewport-coordinates); CanvasCoordinateTests and CanvasCoordinateRenderingTests cover matrix order, ownership, failures and native integration.

## Canvas render time

[Canvas animation intervals](canvas-rendering.md#animation-intervals-and-rectangles) use captured scaled process steps and a live typed rollover setting; ordered transform state is replayed alongside retained geometry. The clock is per Engine.Run and also supplies the optional GPU fragment [TIME built-in](shader-materials.md#render-time).

Embedded windows now expose KeepTitleVisible, expanding width from the actual title font and close-button allowance within MaxSize. Native roots reject this enabled policy before resource acquisition because backend title metrics remain unavailable. [Dialogs](dialogs.md) separately clamp their decorated position into the host. Viewport size/texture notifications enumerate delegates without allocating invocation arrays and continue required callbacks after failures.

Window content scaling now separates logical visible coordinates, raster target dimensions and client presentation. Both native renderers fit the completed content into a client-sized presentation surface with black margins; the borrowed viewport texture keeps its own raster resolution. Window/embedded input removes the complete presentation transform, while rendering excludes that transform and uses raster scale/density. Automatic text raster scale follows the viewport; explicit oversampling remains authoritative. See [Window content scale](../classes/Window.md#content-scale) and the dedicated unit/native checks for the exact policies and platform limits.

## Containing-window notifications

Window emits `NotificationWmMouseEnter` (1002), `NotificationWmMouseExit` (1003),
`NotificationWmWindowFocusIn` (1004), `NotificationWmWindowFocusOut` (1005),
`NotificationWmCloseRequest` (1006), `NotificationWmSizeChanged` (1008),
`NotificationWmDpiChange` (1009) and `NotificationWmPositionChanged` (1012).
Delivery is owner-thread, parent-first through ordinary and internal children;
it stops before each nested Window and its subtree, including physical nodes.
Size/content-policy updates notify only the owning Window, after SizeChanged.
Focus, close and DPI notifications precede their typed window signal. Pointer
entry notifies the containing window before content entry; content exit/hover cleanup
precedes the containing-window exit notification. Window decoration and content
have independent pointer identities; manual viewport-only notifications emit only
content signals. Embedded hit testing uses actual pointer position independently
of input capture, and hiding during entry prevents stale content entry. Direct-child
mutation/disposal rejects during traversal. Remaining receivers and signal delegates
are attempted after failures; errors are aggregated. Native close still applies
AutoAcceptQuit in its finally path; embedded close leaves host quit to the caller.

Public embedded focus/close and actual native/embedded pointer routes use the bridge.
Repeated pointer entry/exit is silent. Position emits on observed native rectangle
changes, not placement requests. SDL display-scale changes supply the DPI identity;
they do not measure physical DPI. Go-back and output-linear/HDR notification sources
remain absent. WindowNotificationTests checks physical descendants, nested boundaries,
mutation/thread errors, committed rectangles, all eight injected SDL dummy events,
later delegates/events after failure and teardown. Its 1024 warmed notification/event
operations require zero owner/all-thread managed bytes. Real compositor, GPU and
foreign-platform acceptance are not established by this check.

Configured recording uses Engine.RunMovie/RunMovieAsync and the typed MovieWriterFile/FPS settings. All recording entry points sample MovieWriterDisableVSync before startup; the root window retains an immediate-presentation request after display creation and before renderer creation when selected, subject to native fallback. The request ends with that display lifetime. See [movie writing](movie-writing.md#project-movie-startup) for precedence, ownership and verification limits.

[Exclusive transient selection](popup-windows.md#active-exclusive-window-chains) now
keeps popup parenting independent of display/focus order. Embedded focus routing
skips disposing windows during cleanup; the shared Node query follows active
links and preserves detached/null and processing-scope rules.

Containing-window reads now follow [active viewport ownership](scene-hierarchy.md#active-viewport-ownership)
through nested offscreen boundaries. Detached Window instances return null from
the inherited Node getters; attached hidden windows retain their association.

## Initial placement and constrained reset

[Window initial placement](../classes/Window.md#initial-placement-and-minimum-size-reset) supplies six stored policies through the existing DisplayServer screen selectors. Native root requests run before scene entry; embedded non-absolute policies center on entry/show. Policy edits do not move a visible window. Wayland retains its explicit native-position limitation while embedded centering executes. Native managers may restrict placement. ResetSize and ordinary Size share scaled wrapped-content, title and authored bounds; resizing a native root uses the actual native request/event path.


## Input reception and visibility phases

[Window input reception](../classes/Window.md#window-input-reception-and-visibility-notification)
connects the existing native event queue and embedded-window routing to WindowInput.
Input polling commits first; reception follows before final viewport localization,
Node callbacks and GUI handling. Window observers retain the existing execution
barrier and synchronous borrowed-event lifetime. NotificationVisibilityChanged (30)
precedes VisibilityChanged after a committed show/hide operation. Managed and native
checks cover failure continuation, pause/blocking, nested coordinates and retirement.
Popup policy belongs to the final localized input receiver. Direct popup injection
still applies acceptance/cancellation while bypassing its own WindowInput event;
forwarded nested popups do not close their ancestor popup. WindowInputTests checks
these paths and reception edits before popup policy; ColorPickerTests checks text
commit before acceptance and restoration on cancellation.

The native decoration producer for TitlebarChanged, custom nonclient input ownership
for NonclientWindowInput, and host go-back request/policy integration are distinct
prerequisites. TitleChanged already covers text changes and does not establish a
native decoration-change signal.

## Independent native windows

Ordinary desktop Window children now own independent native presentation surfaces under the active Engine.Run session. Native IDs are zero for main and increasing positive values for child lifetimes. Hidden/detached children retire their native ID; reactivation allocates a new one. Hiding preserves the viewport resource, texture and completed history. Native close requests affect the child signal and do not automatically quit. Native child surfaces remain visible when their scene parent hides.

The retained DisplayServer owns one native pump and private per-window contexts. Queue events resolve native handles to public IDs, accumulated motion flushes across window/device boundaries, and retired IDs no longer route to the scene. Window signals and GUI/text input address their own viewport. Touch indexes share a session pool; composition and pointer state remain window-specific. Static service identity and SDL initialization remain process-owned. Core per-window operations, borrowed graphics handles, VSync and file-dialog parents resolve the chosen context.

One RenderingServer captures and simulates the scene. GPU windows share its device and authoritative targets; each owns a separate claimed presentation surface. Compatibility windows replay the same frozen dependency-ordered submissions on separate renderers using the main driver. Cold activation/recovery imports retained current/backbuffer pixels once, including disabled/once and feedback history. Prepared frames introduce no CPU readback bridge. Compatibility draw/target storage scales with the number of native windows. Native lifecycle mutation rejects native submission; renderer shutdown also retires orphaned child contexts after callback failures.

ForceNative and TransientToFocused are stored typed policies. Focus observation and runtime-only automatic unparenting apply to native and embedded windows. Native popup subclasses still require their surface/grab, native transparency and decoration adapters; desktop ordinary windows do not claim those capabilities. Mobile/TV/browser hosts retain embedded windows until independent-surface integration exists. The existing Wayland Vulkan root unmap/presentation-acknowledgement prerequisite remains.

Native file-dialog requests pin their selected parent lifetime until native completion. Hiding/disposing that parent rejects before rendering ownership or native identity changes; the existing display-wide pending/completion guard still owns application shutdown. Native chooser interaction and cancellation remain their separate integration checks.

### Native child-window verification

NativeChildWindowTests managed mode verifies stored policies in a fresh process, owner/capture guards, missing-host rejection, embedded focus and automatic unparenting. Display mode uses dummy, X11 and Wayland native queues for metadata, invalid/retired IDs, per-window key/motion density/order, distinct contact indexes and held-input retirement. Application focus tracks an unknown initial state so a first focus-out is delivered without requiring a prior focus-in.

The GPU and compatibility profiles on X11/Wayland inspect actual presentation images independently of authoritative viewport textures. They cover cross-window sampling, once/disabled and ClearNever contents, cyclic history, creation failure/recovery, explicit attachments, hidden/recreated native parents, native transient/modal links, host-policy switching, committed text/IME and independent VSync/borrowed context identities. After 32 warmups, 64 unchanged and 64 changing frames measure owner-thread managed allocation and retained/active draw counts. Native/driver allocations, physical input, compositor icon output and other target platforms are not established by those checks. The pending native-dialog guard is tested without opening a chooser; actual chooser interaction retains its separate native gate.
