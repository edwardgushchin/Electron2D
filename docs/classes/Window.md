# Window

Last updated: 2026-10-20

**Inherits:** [Viewport](Viewport.md)

**Inherited By:** [Popup](Popup.md), [AcceptDialog](AcceptDialog.md)

- **Source:** [`Window.cs`](../../src/Scene/Main/Window.cs), [`Window.Native.cs`](../../src/Scene/Main/Window.Native.cs), [`Window.Placement.cs`](../../src/Scene/Main/Window.Placement.cs), [`Window.Children.cs`](../../src/Scene/Main/Window.Children.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class Window : Viewport`

## Description

Physics interpolation defaults to `On` at the inherited Node policy. Explicit `Inherit` follows the parent and `Off` disables presentation history for inheriting descendants; scene-wide enablement remains required. Stored scenes, resource duplication/copy and property reversion preserve this per-type default and explicit overrides. Logical transforms and input coordinates remain current.


A configurable native or embedded window with an independent scene viewport.

Pass a detached window to `Engine.Run(Window)` or `Engine.RunAsync(Window)`. The runtime opens its native window before scene entry and releases it after scene teardown. One native root owns the application session; visible desktop children may use independent native hosts. The client size uses pixels on Wayland, Android, iOS, tvOS and browsers, and native window units elsewhere. On mobile/TV/browser targets the actual native surface size replaces the initial requested size; nonzero configured minimum or maximum dimensions fail at startup. The root canvas renders after scene processing; embedded child windows execute beneath a viewport configured with GUIEmbedSubwindows; non-embedded desktop children use their own native presentation surfaces.

Native lifetime belongs to Engine.Run or Engine.RunAsync. Viewport inherits the neutral Node; canvas children supply their own transforms and visibility. Window.Position uses native desktop coordinates for native windows and embedding-viewport coordinates for embedded children. Direct SceneTree(Window) activation is rejected. Embedded Window children execute under GUIEmbedSubwindows, and SubViewport children supply offscreen targets; independent desktop Window children share the active engine session. The root canvas supports retained rectangles, lines, textures and GPU shader materials. Native popup surfaces execute with the limits below; native decoration policies and cross-window drag retain their separate dependencies; see the [coverage page](../coverage/classes/Window.md).

Inherited Node.GetViewport/GetWindow return this window only while it is active;
detached windows return null. Nested offscreen viewports preserve the containing
window through active viewport parents. See [active viewport ownership](../components/scene-hierarchy.md#active-viewport-ownership)
for lifecycle, borrowed-reference and processing-scope rules.

`SetIMEActive(true)` enables native committed-text and preedit events for this root window. During Engine.Run, Window forwards those already-committed DisplayServer updates into its SceneTree: the currently focused, visible and processing Control receives a whole committed string or typed preedit, while every live node receives `NotificationOsImeUpdate` for a composition change. A derived text control activates/deactivates native input with its focus lifecycle and manages its own caret and preedit state. Each native window resolves its own focused or embedded text receiver. Exclusive native children suppress parent committed text and nonempty preedit; empty composition updates still clear stale state. This bridge does not fabricate `InputEventKey.Unicode`.

## Examples

```csharp
var window = new Window { Title = "Game", Size = new Vector2i(960, 540) };
window.AddChild(scene); // caller-created Node
Engine.MaxFPS = 60;
int exitCode = Engine.Run(window);
```

Call `Tree!.Quit()` from a scene callback to exit. Run returns the requested code and consumes the supplied window hierarchy. The native window and RenderingServer are open and Engine.MainLoop exposes the tree before OnReady. Window construction initializes SnapTransformsToPixel and SnapVerticesToPixel from active project-setting overrides. Load project settings before constructing the window; later explicit property assignments take precedence and Engine.Run does not overwrite them.

## Constructors

| Member | Contract |
| --- | --- |
| [`public Window()`](#constructor) | Creates a detached visible window with an empty title and a 100 by 100 client area. |

## Properties

| Member | Contract |
| --- | --- |
| [`public Vector2i MaxSize { get; set; }`](#maxsize) | Gets or sets nonnegative maximum client dimensions; zero means no limit on that axis. |
| [`public Vector2i MinSize { get; set; }`](#minsize) | Gets or sets nonnegative minimum client dimensions; zero means no limit on that axis. |
| [`public Vector2i Position { get; set; }`](#position) | Gets or requests the client origin in native desktop or embedded-host coordinates. |
| [`public Vector2i Size { get; set; }`](#size) | Gets the observed client size or requests a positive client size. |
| [`public string Title { get; set; }`](#title) | Gets or sets the native window title. |
| [`public bool Visible { get; set; }`](#visible) | Gets or sets native or embedded visibility; hiding a native child retires its host. |
| [`public WindowMode Mode { get; set; }`](#mode) | Gets the observed native mode, or configures a presentation-mode request. |
| [`public int CurrentScreen { get; set; }`](#currentscreen) | Gets the observed display index, or requests placement on a zero-based display index. |
| [`public bool Unresizable { get; set; }`](#unresizable) | Gets or sets the policy preventing user border resizing. |
| [`public bool Borderless { get; set; }`](#borderless) | Gets or sets the policy removing native or embedded window borders and title bar. |
| [`public bool AlwaysOnTop { get; set; }`](#alwaysontop) | Gets or sets the policy requesting placement above ordinary windows. |
| [`public bool Unfocusable { get; set; }`](#unfocusable) | Gets or sets the policy preventing keyboard focus. |
| [`public bool ForceNative { get; set; }`](#forcenative) | Overrides a containing embedding viewport while hidden. |
| [`public bool TransientToFocused { get; set; }`](#transienttofocused) | Chooses the focused native transient owner on the next show. |

## Methods

Window owns `Show()` and `Hide()`; both assign Visible and preserve native failure/owner-thread/lifetime checks. `event Action? VisibilityChanged` fires synchronously after a committed visibility change. Its failure is aggregated with canvas-root visibility delivery, and disposal clears subscribers. These members are declared on Window rather than inherited from a canvas base.

| Member | Contract |
| --- | --- |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Returns a static factory for an exact Window. Derived types must supply their own factory. PackedScene stores title, size, size limits, mode, supported policies and inherited stored Node properties; Position and CurrentScreen are not stored. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends typed title, size, minimum/maximum size, mode and supported policy descriptors to inherited Node descriptors. |
| [`public override Rect2 GetVisibleRect()`](#getvisiblerect) | Returns the client rectangle in viewport coordinates. |
| [`public void Show()`](#show) | Shows this window; detached use only configures startup visibility. |
| [`public void Hide()`](#hide) | Hides this window without disposing it or its scene. |
| [`public int GetWindowID()`](#getwindowid) | Gets the native window identity while running. |
| [`public void GrabFocus()`](#grabfocus) | Requests keyboard focus and foreground placement from the native system. |
| [`public bool HasFocus()`](#hasfocus) | Reports whether the active native window has keyboard focus. |
| [`public void RequestAttention()`](#requestattention) | Requests a platform attention indication until this window is focused. |
| [`public bool GetFlag(Flags flag)`](#getflag) | Gets a configured window policy. |
| [`public void SetFlag(Flags flag, bool enabled)`](#setflag) | Configures an executable policy and requests its native application when active. |
| [`public bool IsMaximizeAllowed()`](#ismaximizeallowed) | Reports whether the current resize policy permits native maximization. |
| [`public Vector2i GetPositionWithDecorations()`](#getpositionwithdecorations) | Gets the outer window origin, including visible native or embedded decoration margins. |
| [`public Vector2i GetSizeWithDecorations()`](#getsizewithdecorations) | Gets the outer window size, including visible native or embedded decoration margins. |
| [`public void MoveToCenter()`](#movetocenter) | Requests centering of the active client area in its current screen's usable rectangle. |
| [`public void SetIMEActive(bool active)`](#setimeactive) | Enables or disables native text input for the active window. |
| [`public void SetIMEPosition(Vector2i position)`](#setimeposition) | Requests native IME candidate placement at a client-coordinate caret. |
| [`public void SetTaskbarProgressState(DisplayServer.ProgressState state)`](#settaskbarprogressstate) | Requests a native taskbar progress indication for the active window. |
| [`public void SetTaskbarProgressValue(float value)`](#settaskbarprogressvalue) | Requests a native taskbar progress fraction for the active window. |
| [`public static Window? GetFocusedWindow()`](#getfocusedwindow) | Returns the borrowed native or embedded focus identity. |
| [`public void SetUnparentWhenInvisible(bool unparent)`](#setunparentwheninvisible) | Detaches on a later hidden visibility notification without disposing. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action? VisibilityChanged`](#visibilitychanged) | Runs synchronously after the window visibility commits, before canvas propagation. |
| [`public event Action? CloseRequested`](#closerequested) | Occurs when the system requests closure of this root window. |
| [`public event Action? FocusEntered`](#focusentered) | Occurs when the window gains native keyboard focus. |
| [`public event Action? FocusExited`](#focusexited) | Occurs when the window loses native keyboard focus, before pressed input is released. |
| [`public event Action? TitleChanged`](#titlechanged) | Occurs after Title commits a different value. |
| [`public event Action? MouseEntered`](#mouseentered) | Occurs on an effective native pointer entry before subsequent frame callbacks. |
| [`public event Action? MouseExited`](#mouseexited) | Occurs on an effective native pointer exit before subsequent frame callbacks. |
| [`public event Action? DpiChanged`](#dpichanged) | Occurs when the native window's display content scale changes. |
| [`public event Action<IReadOnlyList<string>>? FilesDropped`](#filesdropped) | Occurs when a native file drop completes, with paths in arrival order. |

## Enumerations

| Type | Contract |
| --- | --- |
| [WindowMode](WindowMode.md) | Five native presentation requests, with observed-mode queries. |
| [WindowFlag](WindowFlag.md) | Individual policy indices, not a bit mask; four executable policies. |

## Constructor Descriptions

<a id="constructor"></a>
### `public Window()`

Creates a detached visible window with an empty title and a 100 by 100 client area.

No native resources are acquired until `Engine.Run(Window)`.

## Property Descriptions

<a id="maxsize"></a>
### `public Vector2i MaxSize { get; set; }`

Gets or sets nonnegative maximum client dimensions; zero means no limit on that axis.

Zero by default.

**ArgumentOutOfRangeException:** A component is negative or a nonzero maximum is below the minimum.

**NotSupportedException:** A nonzero Android, iOS, tvOS or browser limit is configured at native startup.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="minsize"></a>
### `public Vector2i MinSize { get; set; }`

Gets or sets nonnegative minimum client dimensions; zero means no limit on that axis.

Zero by default.

**ArgumentOutOfRangeException:** A component is negative or exceeds a nonzero maximum.

**NotSupportedException:** A nonzero Android, iOS, tvOS or browser limit is configured at native startup.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="position"></a>
### `public Vector2i Position { get; set; }`

Gets or requests the client origin in native desktop or embedded-host coordinates.

The configured position before startup, or zero if no position was requested.

Window has no Entity transform; inherited Viewport.CanvasTransform and GlobalCanvasTransform place its canvas drawing independently of native Position. Leaving Position unset lets the system place the window. A preconfigured position is applied at startup and can fail on an unsupported platform.

**NotSupportedException:** The active compositor does not expose or accept global window positions, including Wayland.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="size"></a>
### `public Vector2i Size { get; set; }`

Gets the observed client size or requests a positive client size.

100 by 100 before configuration or native activation.

Native changes may be asynchronous or constrained by the compositor and size limits. SizeChanged follows committed size changes; desktop position and child canvas transforms do not affect size. On Android, iOS, tvOS and browsers the native surface determines the initial observed pixel size, regardless of the requested size.

**ArgumentOutOfRangeException:** Either component is nonpositive.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="title"></a>
### `public string Title { get; set; }`

Gets or sets the native window title.

An empty string by default.

**ArgumentNullException:** The new title is null.

**ArgumentException:** The title contains a null character.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="visible"></a>
### `public bool Visible { get; set; }`

Gets or sets native or embedded visibility; hiding a native child retires its host.

`true` by default. Before Engine.Run this only configures startup visibility.

An active Window shows or hides its native surface before committing managed visibility; native failure leaves managed state unchanged. A committed change raises VisibilityChanged, then notifies each canvas root beneath the window, including roots separated by neutral Node objects. Each canvas root propagates to direct canvas descendants.

**InvalidOperationException:** Mutation occurs off the owner thread or a native request fails.

**ObjectDisposedException:** The window is disposed.

**AggregateException:** Window or canvas visibility callbacks fail after the change commits; later canvas roots are still attempted.

<a id="mode"></a>
### `public WindowMode Mode { get; set; }`

Gets the observed native mode, or configures a presentation-mode request.

**Value:** Windowed by default; the configured request while detached.

Native changes may be asynchronous or denied by the window manager. Wayland exclusive fullscreen becomes ordinary fullscreen; restoring from Wayland minimization is not guaranteed.

**ArgumentOutOfRangeException:** The mode is undefined.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="currentscreen"></a>
### `public int CurrentScreen { get; set; }`

Gets the observed display index, or requests placement on a zero-based display index.

**Value:** The requested index while detached, or zero before configuration. No startup move occurs when unset.

Not stored by PackedScene. Availability is checked at activation. Wayland accepts its current screen only; a request for another screen fails without moving the window.

**ArgumentOutOfRangeException:** The index is negative or unavailable on the active backend.

**NotSupportedException:** The compositor does not allow moving to the requested screen.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**AggregateException:** Moving fails and restoring the native state also fails.

**ObjectDisposedException:** The window is disposed.

<a id="unresizable"></a>
### `public bool Unresizable { get; set; }`

Gets or sets the policy preventing user border resizing.

**Value:** False by default. Programmatic Size requests remain allowed.

Uses GetFlag and SetFlag, including their lifecycle and failure contract.

<a id="borderless"></a>
### `public bool Borderless { get; set; }`

Gets or sets the policy removing native or embedded window borders and title bar.

**Value:** False by default.

Uses GetFlag and SetFlag, including their lifecycle and failure contract.

<a id="alwaysontop"></a>
### `public bool AlwaysOnTop { get; set; }`

Gets or sets the policy requesting placement above ordinary windows.

**Value:** False by default.

Uses GetFlag and SetFlag. Enabling it fails for an active Wayland top-level window or at startup when preconfigured; a rejected request leaves the policy unchanged.

<a id="unfocusable"></a>
### `public bool Unfocusable { get; set; }`

Gets or sets the policy preventing keyboard focus.

**Value:** False by default.

Uses GetFlag and SetFlag. Enabling it fails for an active Wayland top-level window or at startup when preconfigured; a rejected request leaves the policy unchanged.

## Method Descriptions

<a id="show"></a>
### `public void Show()`

Assigns Visible to true. The current Wayland Vulkan profile rejects remapping a previously presented root with NotSupportedException before mutation; native presentation-completion/unmap acknowledgement is the dependency. No native resources are acquired while detached. See [Visible](#visible) for owner-thread, native failure and callback exception behavior.

<a id="hide"></a>
### `public void Hide()`

Assigns Visible to false without disposing the window or its children. See [Visible](#visible) for owner-thread, native failure and callback exception behavior.

<a id="createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Returns a static factory for an exact Window. Derived types must supply their own factory. PackedScene stores title, size, size limits, mode, supported policies and inherited stored Node properties; Position and CurrentScreen are not stored.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends typed title, size, minimum/maximum size, mode and supported policy descriptors to inherited Node descriptors.

<a id="getvisiblerect"></a>
### `public override Rect2 GetVisibleRect()`

Returns the client rectangle in viewport coordinates.

**Returns:** A zero-origin rectangle in client units, independent of desktop and node position.

**ObjectDisposedException:** The viewport is disposed.

<a id="getwindowid"></a>
### `public int GetWindowID()`

Gets the native window identity while running.

**Returns:** Zero for the active root window; minus one while detached.

**ObjectDisposedException:** The window is disposed.

<a id="grabfocus"></a>
### `public void GrabFocus()`

Requests keyboard focus and foreground placement from the native system.

The operating system may deny focus. Wayland submits no foreground activation request under the current DisplayServer contract. Inspect HasFocus for the observed result.

**InvalidOperationException:** The window is inactive, accessed off-thread, or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="hasfocus"></a>
### `public bool HasFocus()`

Reports whether the active native window has keyboard focus.

**Returns:** The platform's current focus observation.

**InvalidOperationException:** The window is inactive or accessed off-thread.

**ObjectDisposedException:** The window is disposed.

<a id="requestattention"></a>
### `public void RequestAttention()`

Requests a platform attention indication until this window is focused.

**InvalidOperationException:** The window is inactive, accessed off-thread, or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="getflag"></a>
### `public bool GetFlag(Flags flag)`

Gets a configured window policy.

**`flag`:** One individual policy identifier.

**Returns:** The last accepted request, initially false; native window-manager overrides do not change it.

**ArgumentOutOfRangeException:** The identifier is undefined or Max.

**NotSupportedException:** The defined policy has no executable integration.

**ObjectDisposedException:** The window is disposed.

<a id="setflag"></a>
### `public void SetFlag(Flags flag, bool enabled)`

Configures an executable policy and requests its native application when active.

**`flag`:** One individual policy identifier.

**`enabled`:** The desired state.

An unchanged request does nothing. Failed native requests do not commit the configured policy. Detached configuration is checked against platform capabilities during Engine.Run startup.

**ArgumentOutOfRangeException:** The identifier is undefined or Max.

**NotSupportedException:** The policy has no executable integration or the native platform rejects it.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="ismaximizeallowed"></a>
### `public bool IsMaximizeAllowed()`

Reports whether the current resize policy permits native maximization.

**Returns:** Whether resizing is allowed; the compositor may impose further restrictions.

**InvalidOperationException:** An active window is accessed off-thread.

**ObjectDisposedException:** The window is disposed.

<a id="getpositionwithdecorations"></a>
### `public Vector2i GetPositionWithDecorations()`

Gets the outer window origin, including visible native or embedded decoration margins.

**Returns:** Native desktop coordinates or embedded host coordinates. Hidden/detached windows return Position. Embedded margins use maximum focused/unfocused content offsets, truncated before integer arithmetic.

**NotSupportedException:** The active Wayland compositor does not disclose global positions.

**InvalidOperationException:** The caller is not the owner or native geometry is unavailable.

**OverflowException:** The outer position exceeds integer coordinates.

**ObjectDisposedException:** The window is disposed.

<a id="getsizewithdecorations"></a>
### `public Vector2i GetSizeWithDecorations()`

Gets the outer window size, including visible native or embedded decoration margins.

**Returns:** Native units; a native Wayland root reports client pixels because decoration extents are unavailable. Visible embedded windows add the maximum focused/unfocused content minimum, truncated before integer arithmetic. Hidden/detached or borderless windows return Size.

**InvalidOperationException:** The caller is not the owner or native geometry is unavailable.

**OverflowException:** The outer size exceeds integer dimensions.

**ObjectDisposedException:** The window is disposed.

<a id="movetocenter"></a>
### `public void MoveToCenter()`

Requests centering of the active client area in its current screen's usable rectangle.

Requires global positioning. Wayland rejects the request.

**NotSupportedException:** Global positioning is unavailable.

**InvalidOperationException:** The window is inactive, accessed off-thread, or native geometry fails.

**OverflowException:** The computed position exceeds integer coordinates.

**ObjectDisposedException:** The window is disposed.

<a id="setimeactive"></a>
### `public void SetIMEActive(bool active)`

Enables or disables native text input for the active window.

**`active`:** Whether to accept committed text and composition updates.

Enable while a text field owns focus. Disabling clears native composition state.

**InvalidOperationException:** The window is inactive, accessed off-thread, or the request fails.

**ObjectDisposedException:** The window is disposed.

<a id="setimeposition"></a>
### `public void SetIMEPosition(Vector2i position)`

Requests native IME candidate placement at a client-coordinate caret.

**`position`:** Caret position in client pixels on Wayland and native window units elsewhere.

The native candidate area is one by ten logical units with zero cursor offset. Text input must be active for a popup to appear; placement remains subject to input-method policy.

**InvalidOperationException:** The window is inactive, accessed off-thread, or the request fails.

**OverflowException:** The position cannot be represented in native coordinates.

**ObjectDisposedException:** The window is disposed.

<a id="settaskbarprogressstate"></a>
### `public void SetTaskbarProgressState(DisplayServer.ProgressState state)`

Requests a native taskbar progress indication for the active window.

**`state`:** The progress indication to show.

**ArgumentOutOfRangeException:** The state is undefined.

**NotSupportedException:** Taskbar integration is unavailable, including Wayland.

**InvalidOperationException:** The window is inactive, accessed off-thread, or the request fails.

**ObjectDisposedException:** The window is disposed.

<a id="settaskbarprogressvalue"></a>
### `public void SetTaskbarProgressValue(float value)`

Requests a native taskbar progress fraction for the active window.

**`value`:** A finite fraction from zero to one, inclusive.

**ArgumentOutOfRangeException:** The fraction is nonfinite or outside zero through one.

**NotSupportedException:** Taskbar integration is unavailable, including Wayland.

**InvalidOperationException:** The window is inactive, accessed off-thread, or the request fails.

**ObjectDisposedException:** The window is disposed.

## Event Descriptions

<a id="visibilitychanged"></a>
### `public event Action? VisibilityChanged`

Delivered on the owner thread after a committed visibility change and before canvas visibility propagation. Native failures and assigning the current value do not raise it. Subscriber failures are aggregated with canvas-root delivery failures; disposal clears subscribers.

<a id="closerequested"></a>
### `public event Action? CloseRequested`

Occurs when the system requests closure of this root window.

Handlers may disable SceneTree.AutoAcceptQuit to keep running, or call SceneTree.Quit with an exit code. The default quit decision follows the signal. Handler failures terminate Engine.Run with cleanup.

<a id="focusentered"></a>
### `public event Action? FocusEntered`

Occurs when the window gains native keyboard focus.

<a id="focusexited"></a>
### `public event Action? FocusExited`

Occurs when the window loses native keyboard focus, before pressed input is released.

<a id="titlechanged"></a>
### `public event Action? TitleChanged`

Occurs after Title commits a different value.

<a id="mouseentered"></a>
### `public event Action? MouseEntered`

Occurs on an effective native pointer entry before subsequent frame callbacks.

Delivered synchronously on the owner thread. GUI occlusion and embedded viewports are absent.

<a id="mouseexited"></a>
### `public event Action? MouseExited`

Occurs on an effective native pointer exit before subsequent frame callbacks.

Delivered synchronously on the owner thread. GUI occlusion and embedded viewports are absent.

<a id="dpichanged"></a>
### `public event Action? DpiChanged`

Occurs when the native window's display content scale changes.

Delivered in native order on the owner thread; this is not a physical-DPI measurement.

<a id="filesdropped"></a>
### `public event Action<IReadOnlyList<string>>? FilesDropped`

Occurs when a native file drop completes, with paths in arrival order.

The managed snapshot remains valid after delivery. Subscribers run on the owner thread before subsequent frame callbacks. Failing subscribers do not prevent later queued native events; Run reports callback failures and releases its resources after the queue drains.

## Lifecycle, verification and limits

See the [Window runtime component](../components/window-runtime.md) for ownership, native startup/cleanup failure behavior and exact executable checks. WindowRuntimeTests passed with SDL dummy and native Wayland; native events were injected. Physical-input/visual acceptance of this new API, other platforms, root content scaling, layered offscreen targets, remaining GUI and nested native windows remain unverified or absent; independent single-layer targets have separate SubViewportTests evidence. Root viewport Control pointer/focus routing has managed checks; it has no native GUI acceptance yet. Native tests also cover mode/flag application, IME enable/disable, borrowed native drop-memory lifetime, managed path retention and window-signal failure cleanup. Dummy tests do not verify native flag changes because that backend accepts setters without applying them. Wayland rejects Position and may constrain geometry; focus requests obey compositor policy.

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028).

A changed Title requests configuration-warning refresh after native/managed title state commits. Equal assignments do nothing; subscriber errors propagate without restoring the previous title. PathRenderingTests verifies selection, repetition, committed native title and failures through Engine.Run.

Inherited [viewport transforms and pointer coordinates](Viewport.md#canvas-transforms-and-pointer-coordinates) participate in root rendering and native input. Root-connected pointer reads and all native warps resolve the native root through every embedding boundary; isolated sections retain local mouse points and headless roots poll emulated Input; the viewport composes the screen transform once, and unavailable native hosts or pointer capabilities reject explicitly. GetVisibleRect stays in client-sized viewport units. Native desktop position remains a platform capability; CanvasItem.GetScreenTransform needs it only when its popup-base chain reaches a native Window without an embedding boundary.

## Canvas render time

The root Window forwards the captured process step to SceneTree for canvas rendering. An invisible root skips rendering and clock advancement. The clock belongs to this renderer and starts at zero on a new Engine.Run. See [canvas timing](../components/canvas-rendering.md#animation-intervals-and-rectangles).

## Typed theme API

Source: [Window.Theme.cs](../../src/Scene/Main/Window.Theme.cs). All six data categories share [theme owner lookup](../components/themes.md), including borrowed Font resources and inherited defaults.

| Signature | Contract |
| --- | --- |
| `public Theme? Theme { get; set; }` | Borrowed branch resource, null initially. |
| `public string ThemeTypeVariation { get; set; }` | Empty initially. |
| `public const int NotificationThemeChanged = 32` | Refresh notification before cache clearing and dependent work. |
| `public event Action? ThemeChanged` | Raised during refresh before the previous lookup cache is cleared. |
| `protected override void OnNotification(int what)` | Preserves base notifications, then handles theme/parent lifecycle. |
| `public Color GetThemeColor(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeColor(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeColorOverride(string name)` | Local override slot only. |
| `public void AddThemeColorOverride(string name, Color color)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeColorOverride(string name)` | Refreshes even when no local slot existed. |
| `public int GetThemeConstant(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeConstant(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeConstantOverride(string name)` | Local override slot only. |
| `public void AddThemeConstantOverride(string name, int constant)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeConstantOverride(string name)` | Refreshes even when no local slot existed. |
| `public int GetThemeFontSize(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeFontSize(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeFontSizeOverride(string name)` | Local override slot only. |
| `public void AddThemeFontSizeOverride(string name, int fontSize)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeFontSizeOverride(string name)` | Refreshes even when no local slot existed. |
| `public Font? GetThemeFont(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeFont(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeFontOverride(string name)` | Local override slot only. |
| `public void AddThemeFontOverride(string name, Font font)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeFontOverride(string name)` | Refreshes even when no local slot existed. |
| `public Texture? GetThemeIcon(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeIcon(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeIconOverride(string name)` | Local override slot only. |
| `public void AddThemeIconOverride(string name, Texture texture)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeIconOverride(string name)` | Refreshes even when no local slot existed. |
| `public StyleBox? GetThemeStyleBox(string name, string themeType = "")` | Resolved typed value; resources remain borrowed. |
| `public bool HasThemeStyleBox(string name, string themeType = "")` | Matching override/theme data, excluding universal fallback. |
| `public bool HasThemeStyleBoxOverride(string name)` | Local override slot only. |
| `public void AddThemeStyleBoxOverride(string name, StyleBox styleBox)` | Commits local value; equal writes still refresh. |
| `public void RemoveThemeStyleBoxOverride(string name)` | Refreshes even when no local slot existed. |
| `public float GetThemeDefaultBaseScale()` | Nearest positive theme default, then built-in default, then universal fallback. |
| `public Font? GetThemeDefaultFont()` | Nearest nonnull theme default font, built-in default, then nullable universal fallback. |
| `public int GetThemeDefaultFontSize()` | Same priority for positive font-size defaults. |
| `public void BeginBulkThemeOverride()` | Enables boolean override batching; repeated Begin is idempotent. |
| `public void EndBulkThemeOverride()` | Ends a batch and publishes one refresh; no active batch throws. |

<a id="theme"></a><a id="themetypevariation"></a>
**Assignment and inheritance:** Theme replacement rejects disposed input, suppresses equal references and refreshes this node and live consecutive Control/Window descendants. ThemeTypeVariation suppresses equal values and refreshes this node. A neutral Node between theme owners ends inheritance; root Window themes therefore reach only consecutive theme-capable branches. Stored Theme references are borrowed. An externally disposed assigned Theme remains readable through the property, but owner lookup skips it after invalidation.

<a id="getthemecolor"></a><a id="hasthemecolor"></a><a id="hasthemecoloroverride"></a><a id="addthemecoloroverride"></a><a id="removethemecoloroverride"></a><a id="getthemeconstant"></a><a id="hasthemeconstant"></a><a id="hasthemeconstantoverride"></a><a id="addthemeconstantoverride"></a><a id="removethemeconstantoverride"></a><a id="getthemefont"></a><a id="hasthemefont"></a><a id="getthemefontsize"></a><a id="hasthemefontsize"></a><a id="hasthemefontoverride"></a><a id="addthemefontoverride"></a><a id="removethemefontoverride"></a><a id="hasthemefontsizeoverride"></a><a id="addthemefontsizeoverride"></a><a id="removethemefontsizeoverride"></a><a id="getthemeicon"></a><a id="hasthemeicon"></a><a id="hasthemeiconoverride"></a><a id="addthemeiconoverride"></a><a id="removethemeiconoverride"></a><a id="getthemestylebox"></a><a id="hasthemestylebox"></a><a id="hasthemestyleboxoverride"></a><a id="addthemestyleboxoverride"></a><a id="removethemestyleboxoverride"></a><a id="getthemedefaultbasescale"></a><a id="getthemedefaultfont"></a><a id="getthemedefaultfontsize"></a><a id="beginbulkthemeoverride"></a><a id="endbulkthemeoverride"></a><a id="notificationthemechanged"></a><a id="themechanged"></a>
**Get/Has resolution:** detached queries bypass resolved-value caches, so ancestor/global resource edits are immediately visible without notifications. Attached queries use the deferred invalidation contract below. Empty themeType means the current type and variation. A local override applies only to this implicit query, the node's own class name or its selected variation. An explicit different type bypasses local overrides. The nearest theme defining the variation supplies its dependency chain; native ancestry follows. Search each nearest-to-outer owner theme across that type order, then the built-in Theme. Get uses the universal category fallback when no match exists. Has excludes that universal fallback, but a nonnull Theme.DefaultFont or positive Theme.DefaultFontSize is itself a valid Font or FontSize match. GetThemeDefaultFont searches defaults directly and ignores item-specific overrides. A cycle in the active variation chain throws InvalidOperationException. Direct resource queries on Theme remain exact-type queries and do not perform this branch search.

**Keys and errors:** node-side names/type strings may be empty and otherwise contain arbitrary text except a null character. Null strings throw ArgumentNullException, null characters throw ArgumentException. Assigned colors must be finite. Font/icon/style overrides require non-null live resources; unlike a Theme's null placeholder slots, a local resource override cannot be null. Integer overrides retain signed values. Attached queries require the owner thread; mutation also observes capture/lifetime guards. A disposed stored override retains identity until removed/replaced, so its consumer can reject invalid use.

**Overrides and batching:** Add commits the local value; Remove clears any local slot. Both refresh even for equal/missing values. Resource overrides share one content-change/disposal subscription per distinct resource. Owner-thread override changes refresh synchronously when attached, while off-thread resource changes defer to the owner. Begin is a boolean suppression flag rather than a nesting counter; End without an active batch throws InvalidOperationException, and a valid End refreshes once even if the batch made no edits. Detached changes invalidate lookup state without emitting an attached theme notification.

<a id="onnotification"></a>
**OnNotification, timing and callbacks:** Theme resource content changes and global context/fallback replacement are deferred through SceneTree. NotificationThemeChanged invokes ThemeChanged before clearing the previous lookup cache, so an event handler may still see an already cached inherited/resource result; direct local overrides are checked before that cache. Required invalidation, redraw and dependent geometry work are attempted after event failures. Propagation continues to later live descendants and reports aggregated failures. Reentrant branch propagation repeats from a fresh captured set and rejects a nonsettling chain after 64 passes. Tree membership generations suppress stale deferred work. Virtual getters that replace theme state cannot refill an invalidated cache; a subsequent lookup recomputes. Deferred override-resource delivery rechecks the current bulk flag, preserving suppression when a batch begins after a worker callback was queued. Same-owner notification reentry also settles iteratively within the 64-pass bound.

**Stored state and cleanup:** Theme/variation are typed stored properties. Actual local overrides use typed descriptors; a null descriptor value means removal, not a null resource override. Fresh Control/Window instances reconstruct missing override descriptors only through the six reserved prefixes and exact captured value types; other unknown/mismatched schemas remain rejected. Exact scene packing follows existing resource graph/local-to-scene rules. Cleanup removes theme/global/resource subscriptions and cached references; externally owned resources are never disposed by node cleanup. First-use query keys and capacity growth may allocate; warmed behavior is measured separately.

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) verifies font defaults/overrides, deferred updates, failures, packing and 64 warmed cycles with zero managed bytes. Project Theme loading and remaining built-in GUI defaults remain separate under [ADR 0083](../decisions/rendering.md#adr-0083).

GetTexture/GetViewportRID supply stable borrowed identities for native root output. Root remains the only native window; SubViewport children have independent canvases. TransparentBG selects transparent target clearing; native compositor window transparency remains a separate display capability. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) for scope, tests and remaining prerequisites.

Native GPU visibility changes drain work and release the swapchain before hiding; supported showing recreates it; previously presented Wayland Vulkan surfaces reject remapping until native unmap acknowledgement is implemented. Independent offscreen targets stay available while root presentation is hidden. The dependency host verifies this on Linux Wayland.

On the current Linux Wayland Vulkan profile, remapping a previously presented root surface rejects with NotSupportedException before native/managed visibility changes. Wayland protocol traces show buffer state retained at xdg_surface recreation even after GPU idle, swapchain release and SDL.SyncWindow; trigger: an SDL-owned native presentation-completion/unmap acknowledgement bridge or verified backend/compositor correction. Hiding succeeds, offscreen targets continue, compatibility remapping executes. This is an explicit platform capability gap, not a completed Show path.

## Embedded popup integration

See [the component](../components/popup-windows.md) for input/target ownership, geometry, persistence and exact remaining dependencies.

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action AboutToPopup` | Occurs before popup positioning, sizing and visibility changes. |
| `public Electron2D.Vector2 GetContentsMinimumSize()` | Returns the maximum extent of direct child controls and their minimum sizes. |
| `public Electron2D.StyleBox GetThemeStyleBox(System.String name, System.String themeType = "")` | Gets a typed theme style box using local override, branch themes, native defaults and universal fallback. |
| `public System.Boolean IsEmbedded()` | Reports whether this window uses an attached viewport's embedded host. |
| `public System.Boolean IsLayoutRTL()` | Reports whether the resolved window layout direction is right to left. |
| `protected virtual Electron2D.Vector2 OnGetContentsMinimumSize()` | Computes the minimum dimensions required by direct child controls. |
| `public System.Void Popup(Electron2D.Rect2i rect = default)` | Shows this child window, applying an optional host-coordinate rectangle. |
| `public System.Void PopupCentered(Electron2D.Vector2i minSize = default)` | Shows this window centered in its embedding viewport. |
| `public System.Void PopupCenteredClamped(Electron2D.Vector2i minSize = default, System.Single fallbackRatio = 0.75f)` | Shows this window centered, limiting requested dimensions to a host-size ratio. |
| `public System.Void PopupCenteredRatio(System.Single ratio = 0.8f)` | Shows this window centered with dimensions proportional to its host. |
| `public System.Void PopupExclusive(Electron2D.Node fromNode, Electron2D.Rect2i rect = default)` | Parents a detached dialog to the last exclusive window and shows it with an optional host-coordinate rectangle. |
| `public System.Void PopupExclusiveCentered(Electron2D.Node fromNode, Electron2D.Vector2i minSize = default)` | Parents a detached dialog to the last exclusive window and shows it with centered dimensions. |
| `public System.Void PopupExclusiveCenteredClamped(Electron2D.Node fromNode, Electron2D.Vector2i minSize = default, System.Single fallbackRatio = 0.75f)` | Parents a detached dialog to the last exclusive window and shows it with clamped centered dimensions. |
| `public System.Void PopupExclusiveCenteredRatio(Electron2D.Node fromNode, System.Single ratio = 0.8f)` | Parents a detached dialog to the last exclusive window and shows it with a proportional host size. |
| `public System.Void PopupExclusiveOnParent(Electron2D.Node fromNode, Electron2D.Rect2i parentRect)` | Parents a detached dialog to the last exclusive window and shows it with a parent-relative rectangle. |
| `public System.Void PopupOnParent(Electron2D.Rect2i parentRect)` | Shows this window at a rectangle relative to its parent window. |
| `public System.Boolean Borderless { get; set; }` | Gets or sets the policy removing native or embedded window borders and title bar. |
| `public System.Boolean Exclusive { get; set; }` | Gets or sets whether input outside this window is blocked while visible. |
| `public Electron2D.LayoutDirection LayoutDirection { get; set; }` | Gets or sets the direction inherited by this window's controls. |
| `public System.Boolean MaximizeDisabled { get; set; }` | Gets or sets whether user maximization is disabled. |
| `public System.Boolean MinimizeDisabled { get; set; }` | Gets or sets whether user minimization is disabled. |
| `public System.Boolean PopupWMHint { get; set; }` | Gets or sets the popup window-manager hint, retained by the embedded host. |
| `public System.Boolean PopupWindow { get; set; }` | Gets or sets the popup input and outside-click close-request policy. |
| `public System.Boolean Transient { get; set; }` | Gets or sets whether this window belongs to its containing parent window's transient chain. |
| `public System.Boolean Transparent { get; set; }` | Gets or sets transparent embedded window composition. |
| `public System.Boolean Unresizable { get; set; }` | Gets or sets the policy preventing user border resizing. |
| `public System.Boolean Visible { get; set; }` | Gets or sets native-root or embedded-child visibility. |
| `public System.Boolean WrapControls { get; set; }` | Gets or sets whether child control minimums constrain this window's size. |

The [PopupMenu consumer](../components/popup-menus.md) uses all 37 declared menu theme keys, inherited Window popup hooks and an exact built-in scene/file factory. Internal item/search controls receive focus after visibility propagation; public runtime signatures of these owners are unchanged.

## Embedded title width

`public System.Boolean KeepTitleVisible { get; set; }` defaults to false and expands embedded width from its actual themed title font and close-button allowance, bounded by MaxSize. Title and theme changes recompute the minimum. AcceptDialog defaults to true. Native roots reject this enabled policy before acquiring resources; native title metrics remain a backend prerequisite. The policy is stored; it does not clamp the window position.

Internal native presentation interception lets FileDialog route Visible/Show/Popup and centered variants to an asynchronous chooser before custom visibility or geometry commit. Ordinary Window behavior keeps its existing embedded host. See the [file-dialog component](../components/file-dialogs.md) for its exercised flow and limits.

## Native pointer departure

A native mouse-exit event now clears GUI and physical hover for this specific
viewport before Window.MouseExited. Cleanup errors are aggregated without skipping
other exit notifications. PhysicsPickingNativeTests exercises the path under both
rendering methods and both physical backends on Linux.

## Project layout direction

Window and Control use one domain-aware resolver. An inherited node follows the nearest Window or Control only while ancestor translation domains match. With no such ancestor, `ProjectSettings.RootNodeLayoutDirection` selects 0 application locale, 1 LTR, 2 RTL or 3 system UI locale. `ForceRightToLeftLayoutDirection` forces inherited and application/system locale modes to RTL; explicit LTR/RTL remain authoritative. Each SceneTree samples both settings and feature overrides before entry. Detached nodes use current settings. Authored per-node directions survive scene packing; project defaults do not overwrite them.

Layout-direction and translation notifications reflow geometry and invalidate text; global locale/catalog edits require an explicit translation notification under ADR 0007. Existing locale direction still requires a matching registered catalog or fallback language. [ProjectLayoutDirectionTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionTests.cs) covers resolution, lifetime and reflow; [ProjectLayoutDirectionNativeTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionNativeTests.cs) checks GUI/text pixels on Linux Wayland GPU/compatibility. Foreign targets and owner visual acceptance remain unverified.

## Content scale

The stored `ContentScaleSize`, `ContentScaleMode`, `ContentScaleAspect`, `ContentScaleStretch` and `ContentScaleFactor` configure real canvas rasterization and presentation. Size defaults to zero; either zero axis disables resolution fitting. Mode defaults to Disabled, aspect to Ignore, stretch to Fractional and factor to one. Desired axes must be nonnegative; factor must be positive and finite; undefined enum values and unrepresentable calculated dimensions reject before committing. Equal writes are silent. Successful changes invalidate retained commands and notify viewport texture/SizeChanged observers; callback failure preserves the committed state.

Disabled retains client-resolution pixels while dividing the logical visible area by factor. CanvasItems fits the desired resolution, rasterizes geometry at the fitted client resolution and scales coordinates. Viewport rasterizes at the floored fitted logical resolution divided by factor, then enlarges that image. `GetVisibleRect()` reports logical coordinates; `GetTexture()` reports the actual raster resolution. `Size` remains the client size. `GetFinalTransform()` and `GetScreenTransform()` include presentation margins and scale for input, pointer and IME consumers; the renderer uses its separate raster transform.

Ignore fits both axes independently. Keep preserves the authored aspect with black margins. KeepWidth expands logical height for a taller client and otherwise uses margins; KeepHeight performs the corresponding horizontal policy. Expand grows the logical area to fill the client. Screen and logical fitting dimensions floor before margins are computed; half margins round away from zero. Integer stretch floors and stores factor, clamps it to at least one, and chooses a whole client-to-content ratio of at least one. Small clients clip the image instead of downscaling. A floored zero raster has a zero texture size and no image/device view; presentation remains a cleared client surface. Existing native target limits remain explicit backend errors.

Embedded windows fit inside their physical client rectangles and transform their input independently. Their raster density follows parent canvas stretching, including nested windows. Parent density edits invalidate affected glyph recordings and texture consumers. Automatic glyph oversampling follows the actual viewport raster scale; explicit draw/font oversampling takes priority. Viewport image enlargement preserves its coarse raster grid, while CanvasItems can rasterize glyphs at the larger pixel scale. Pixel snapping and canvas-follow centers operate at raster resolution.

Example inside application setup:

```csharp
var window = new Window
{
    Size = new Vector2i(1280, 720),
    ContentScaleSize = new Vector2i(640, 360),
    ContentScaleMode = ContentScaleMode.Viewport,
    ContentScaleAspect = ContentScaleAspect.Keep,
    ContentScaleStretch = ContentScaleStretch.Integer
};
Engine.Run(window);
```

[WindowContentScaleTests](../../tests/Electron2D.Tests/WindowContentScaleTests.cs) and [WindowContentScaleNativeTests](../../tests/Electron2D.Tests/WindowContentScaleNativeTests.cs) cover the executable Linux profile. Native allocator totals, foreign targets and owner visual acceptance remain unverified.

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

PackedScene captures Position only after an authored position request. An untouched root Window restores automatic native placement instead of silently requesting `(0,0)`; explicit zero remains stored and retains the platform coordinate gate. This permits saved default root windows to start on Wayland. CanvasModulateTests checks both forms, and its published scene consumer executes actual native output.

Engine.RunMovie/RunMovieAsync consume this root using a project movie profile. A recording's sampled MovieWriterDisableVSync request is retained between native display and renderer creation, subject to DisplayServer fallback. The display's destruction ends the request; ordinary later windows retain their backend defaults. See [project movie startup](../components/movie-writing.md#project-movie-startup).

## Active transient selection

Transient links a live window to its nearest containing parent Window. A visible
exclusive transient selects that parent's active exclusive child; the latest
conflicting activation wins with a diagnostic, and retirement does not restore an
older selection. Flag changes, visibility and tree membership update the link
before callbacks. `Node.GetLastExclusiveWindow()` traverses these links and every
`PopupExclusive*` method uses it. Attachment/popup failure attempts detachment;
combined failures retain both errors. See [the current popup contract](../components/popup-windows.md#active-exclusive-window-chains).

## Initial placement and minimum-size reset

`Window.WindowInitialPosition` retains these six values:

| Value | Native root selection |
| --- | --- |
| `Absolute = 0` | Use authored Position; an unset position keeps system placement. |
| `CenterPrimaryScreen = 1` | Primary screen. |
| `CenterMainWindowScreen = 2` | Main-window screen. |
| `CenterOtherScreen = 3` | Configured CurrentScreen, or screen zero. |
| `CenterScreenWithMouseFocus = 4` | DisplayServer pointer-focus selector. |
| `CenterScreenWithKeyboardFocus = 5` | DisplayServer keyboard-focus selector. |

`InitialPosition` defaults to Absolute and is stored by PackedScene and typed archives separately from Position. Undefined enum values fail before commitment. Every valid assignment emits PropertyListChanged after commitment, including equal values; observer failure leaves the new policy stored. Native roots request placement before scene entry, using full screen bounds. These are requests subject to native constraints and focus-selector capability limits. Wayland rejects explicit root positioning and releases the failed host; the default unset Absolute policy works normally. CurrentScreen retains its existing non-stored native configuration role.

Embedded windows center in their actual containing viewport for every non-absolute policy. Entry/show applies the policy, including showing through Popup; changing it while visible or resizing its parent does not move the window, and hide/show applies it again. Absolute keeps the authored local position. Independent native children apply the same native placement policies on creation and recreation.

`ResetSize()` requests the smallest constrained client extent. It combines MinSize, the title minimum when required, and `ceil(GetContentsMinimumSize() * ContentScaleFactor)` when WrapControls is enabled, then honors each nonzero MaxSize axis. The portable viewport retains at least one pixel per axis. Measurement failures and unrepresentable scaled dimensions leave the old size intact. Source content is sampled once by a reset. Actual native geometry and SizeChanged retain the normal asynchronous resize contract.

The same minimum calculation is used by Size and wrapped-content growth, including native-root requests. Access follows existing disposal, owner-thread and capture rules. Rendering, other-host and compositor acceptance are distinct from detached authoring checks.

Verification for this family: WindowPlacementTests covers all policies, invalid values, scaled fractional minima/maxima, overflow, failed/reentrant/disposal hooks, packed/archive storage and owner/capture/lifetime guards. Eleven native profiles pass on Linux x64: embedded pixels and actual native reset/readback on Wayland/X11, all six X11 root placements, and explicit Wayland root-position rejection with cleanup. A public-only consumer authors a scene in NativeAOT and reloads it in fresh JIT/NativeAOT processes, exercising five shown policy phases and green pixels on both drivers. AOT maps no CoreCLR. Popup and content-scale managed regressions also pass. Other operating systems, physical GPU and human acceptance remain separate.


## Window input reception and visibility notification

`WindowInput` is a typed `Action<InputEvent>` event for input arriving through the
window host, before ordinary viewport input. The native root sees client coordinates;
each forwarded embedded window sees coordinates with its position removed, before
its own final content transform. Nested windows each receive their own stage.
The borrowed event is live only during synchronous delivery. Edits to it participate
in subsequent localization; callers needing retained data must copy the values.
Internal cursor-refresh events are filtered. Process pause and GUIDisableInput do
not suppress this reception signal, while a disabled embedder stops child forwarding.
Direct `PushInput` bypasses the receiver's own WindowInput signal and can still
forward to embedded children. Popup actions run after reception and localization, on the final receiving popup only. Direct popup injection retains these actions without emitting its own WindowInput. Use Node/Control input callbacks for input consumption.

Handler failures are collected while scene input continues; recursive input and
cross-thread delivery reject use. Retiring a window during its reception callback
prevents later dispatch to that retired viewport. Engine-owned transformed events
are disposed after dispatch; nonpositional caller events remain borrowed.

`NotificationVisibilityChanged` has value 30. A real visibility assignment commits
state, notifies the window, then emits VisibilityChanged before canvas-root visibility
propagation. Assigning the current value is silent. Signal blocking retains numeric
notifications; notification failure cannot suppress the following event and canvas
cleanup. Disposal during notification prevents stale delivery. Native refusal occurs
before either phase, retaining the documented Wayland remapping limitation.

WindowInputTests covers managed routing and native queued input on software Vulkan.
Public-only saved-scene consumers exercise authoring, fresh load, actual GUI input,
rendered pixels and cleanup with JIT and NativeAOT. Their single-owner synchronization
context schedules injected Input.ParseInputEvent calls between RunAsync iterations;
input injection during scene callbacks, lifecycle delivery or rendering remains rejected. Physical input devices, hardware
GPU performance and foreign native hosts remain separate acceptance gates.

Hovered-window disposal clears routing ownership without invoking live mutation on the
retired window. Focus cleanup does not raise another departing child during subtree
removal or tree shutdown. Regression checks cover both cleanup paths.

## Embedded decoration themes

All thirteen Window theme items now have executing embedded consumers. The built-in theme supplies focused `embedded_border` and `embedded_unfocused_border` styles, borrowed `title_font` and fallback `title_font_size`, `title_color`, `title_outline_modulate`, `title_outline_size`, `title_height`, `resize_margin`, real DPITexture `close` and `close_pressed` icons, and `close_h_offset`/`close_v_offset`. Normal defaults are 16-pixel icons, offsets 18 and 24, title height 36, resize margin 4 and outline size zero. Resource and local override changes use ordinary deferred/direct ThemeOwner invalidation and batching. Subclasses inherit the Window item type chain; custom variation/owner/project themes remain supported.

The [complete chrome contract](../components/popup-windows.md#embedded-decoration-theme-and-input) covers retained shaped titles, pressed/canceled/released close capture, outside resizing, geometry queries, source-resource changes, scene persistence and verified limits.

## GUI control pixel policy

The inherited [GUISnapControlsToPixels](Viewport.md#guisnapcontrolstopixels) defaults true independently on this viewport and is stored in typed scenes. It rounds eligible Control rendering translations while preserving logical layout/input geometry. Nested viewports use their own flag. See [executing checks](../components/canvas-rendering.md#gui-control-pixel-snapping).

## Independent native window lifetime

Desktop children without an embedder create an independent native host when entering visible or becoming visible. The root remains identity zero; each child lifetime has a positive, monotonically increasing `GetWindowID()`. Hiding or detaching a child retires that ID and its presentation resources. Showing creates a new ID while preserving the Window object and viewport RID; hiding also preserves the completed viewport texture. The main session owns SDL and one RenderingServer. Child close requests deliver `CloseRequested` without implicitly quitting the application. Native child visibility remains independent when its scene parent hides.

### ForceNative

`public bool ForceNative { get; set; }` defaults to false and is stored by PackedScene. A true value overrides ancestor `GUIEmbedSubwindows`. Configure it before scene entry or while hidden; changing a displayed attached child throws InvalidOperationException. The active root retains its application host. Hidden policy changes rebuild embedded membership without creating an invisible native surface. Position retains its numeric value when host units change; assign embedded coordinates before showing a former native window. A native child requires an active desktop scene host; an absent host fails before visibility commits.

### TransientToFocused

`public bool TransientToFocused { get; set; }` defaults to false and is stored. With `Transient`, native activation selects the currently focused native Window, falling back to the nearest scene Window. Embedded children retain scene ownership. Changing the policy while visible affects the next show. Native SDL parent/modal requests preserve explicit backend errors; transient ownership must remain acyclic. `Exclusive` also blocks scene input directed to its parent. Native surface retirement clears dependent native parent links; recreation restores surviving scene links.

### GetFocusedWindow

`public static Window? GetFocusedWindow()` returns the borrowed scene Window most recently assigned focus by native or embedded focus events, or null. The identity commits before focus notifications/signals and retires on hide, detachment and disposal. Active scene owner checks apply; this is an event observation rather than a request to change focus.

### SetUnparentWhenInvisible

`public void SetUnparentWhenInvisible(bool unparent)` configures a runtime-only flag, initially false. A subsequent hidden visibility notification removes the Window from its parent before the VisibilityChanged signal. The object remains caller-owned and may be reattached or explicitly disposed. Equal hidden assignments do not synthesize another transition. Packing does not preserve this method-only request.

Each native context receives its own size, limits, title, mode, focus, pointer, text/IME and drop stream through the unique display service. GPU children share device targets. Compatibility children replay frozen canvas submissions on a renderer using the main driver's implementation, copying existing viewport history once on creation/recovery. Scene callbacks and simulation run once. Native popup surfaces, transparent top levels, native title metrics and unsupported decoration controls remain separate capabilities. The existing Wayland Vulkan root remap gap remains explicit; child hide/show uses destruction/recreation.

## Native popup geometry and retirement

Native desktop popups use real parent-relative SDL surfaces when `PopupWMHint` is enabled. `PopupOnParent` accepts parent-client geometry and works without fabricated Wayland desktop positions. Public native `Position`, explicit global `Popup(rect)` and display-centered placement retain the global-position capability guard; centered geometry uses the complete parent display, while fitting uses its usable area. Attached embedded children keep their existing viewport coordinates. Runtime control anchors compose the source viewport transform, including nested and scaled hosts.

The native role is fixed-size and borderless, cannot minimize/maximize/fullscreen, and follows its physical parent's display. Hide before changing native role or transparency. Native children recreate their identity after hide. Retiring or hiding a native parent first retires popup descendants; independent ordinary child windows survive parent hide. Popup visibility observer failures are reported after required retirement and committed visibility. SDL popup-done force-hides without emitting a user `CloseRequested`; ordinary close requests and scene cancellation retain their own behavior.

Transparent native PopupPanel/PopupMenu output executes through the compatibility backend. SDL 3.4.18's GPU API rejects transparent windows; GPU native menus require explicit `Transparent=false` before showing. The unsupported request releases its attempted native lifetime and preserves hidden state. Embedded transparent popups continue to execute on both renderers. Native titled-popup variants without the popup role, live embedding migration, foreign-platform acceptance and native system-menu service retain their exact coverage prerequisites.

`NativePopupWindowTests` uses `ELECTRON2D_TEST_NATIVE_POPUPS=gpu` or `compatibility` with the Release runner. It exercises native nested parenting, IDs, outside dismissal, owner-safe rectangles, no-focus and exclusive policies, forced retirement, callback failure, real menu/option/panel/context output, and fresh-process public scene loading. Active focus/render and idle/render measurements cover 64 owner-thread intervals after 32 warmup intervals. SDL event construction, cold model edits/readback, native allocator counts, compositor screenshot acceptance, physical input and other platforms are outside the measured interval.
