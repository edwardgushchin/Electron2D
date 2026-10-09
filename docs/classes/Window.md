# Window

Last updated: 2026-10-06

**Inherits:** [Viewport](Viewport.md)

**Inherited By:** [Popup](Popup.md), [AcceptDialog](AcceptDialog.md)

- **Source:** [`Window.cs`](../../src/Scene/Main/Window.cs), [`Window.Native.cs`](../../src/Scene/Main/Window.Native.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class Window : Viewport`

## Description

A configurable native root window that owns scene children.

Pass a detached window to `Engine.Run(Window)` or `Engine.RunAsync(Window)`. The runtime opens its native window before scene entry and releases it after scene teardown. One native root window is supported. The client size uses pixels on Wayland, Android, iOS, tvOS and browsers, and native window units elsewhere. On mobile/TV/browser targets the actual native surface size replaces the initial requested size; nonzero configured minimum or maximum dimensions fail at startup. The root canvas renders after scene processing; embedded child windows execute beneath a viewport configured with GUIEmbedSubwindows; independent native children remain absent.

Native lifetime belongs to Engine.Run or Engine.RunAsync. Viewport inherits the neutral Node; canvas children supply their own transforms and visibility. Window.Position uses native desktop coordinates. Direct SceneTree(Window) activation and insertion of a Viewport as a child are rejected. The root canvas supports retained rectangles, lines, textures and GPU shader materials. Offscreen and multiwindow rendering remain incomplete; see the [coverage page](../coverage/classes/Window.md).

`SetIMEActive(true)` enables native committed-text and preedit events for this root window. During Engine.Run, Window forwards those already-committed DisplayServer updates into its SceneTree: the currently focused, visible and processing Control receives a whole committed string or typed preedit, while every live node receives `NotificationOsImeUpdate` for a composition change. A derived text control activates/deactivates native input with its focus lifecycle and manages its own caret and preedit state. This bridge does not fabricate `InputEventKey.Unicode` or support nested/cross-window text routing.

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
| [`public Vector2i Position { get; set; }`](#position) | Gets or requests the client origin in native desktop coordinates. |
| [`public Vector2i Size { get; set; }`](#size) | Gets the observed client size or requests a positive client size. |
| [`public string Title { get; set; }`](#title) | Gets or sets the native window title. |
| [`public bool Visible { get; set; }`](#visible) | Gets or sets the root window's native visibility. |
| [`public WindowMode Mode { get; set; }`](#mode) | Gets the observed native mode, or configures a presentation-mode request. |
| [`public int CurrentScreen { get; set; }`](#currentscreen) | Gets the observed display index, or requests placement on a zero-based display index. |
| [`public bool Unresizable { get; set; }`](#unresizable) | Gets or sets the policy preventing user border resizing. |
| [`public bool Borderless { get; set; }`](#borderless) | Gets or sets the policy removing native window borders and title bar. |
| [`public bool AlwaysOnTop { get; set; }`](#alwaysontop) | Gets or sets the policy requesting placement above ordinary windows. |
| [`public bool Unfocusable { get; set; }`](#unfocusable) | Gets or sets the policy preventing keyboard focus. |

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
| [`public Vector2i GetPositionWithDecorations()`](#getpositionwithdecorations) | Gets the outer window origin, including native borders when visible and active. |
| [`public Vector2i GetSizeWithDecorations()`](#getsizewithdecorations) | Gets the outer window size, including native borders when visible and active. |
| [`public void MoveToCenter()`](#movetocenter) | Requests centering of the active client area in its current screen's usable rectangle. |
| [`public void SetIMEActive(bool active)`](#setimeactive) | Enables or disables native text input for the active window. |
| [`public void SetIMEPosition(Vector2i position)`](#setimeposition) | Requests native IME candidate placement at a client-coordinate caret. |
| [`public void SetTaskbarProgressState(DisplayServer.ProgressState state)`](#settaskbarprogressstate) | Requests a native taskbar progress indication for the active window. |
| [`public void SetTaskbarProgressValue(float value)`](#settaskbarprogressvalue) | Requests a native taskbar progress fraction for the active window. |

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

Gets or requests the client origin in native desktop coordinates.

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

Gets or sets the root window's native visibility.

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

Gets or sets the policy removing native window borders and title bar.

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

Gets the outer window origin, including native borders when visible and active.

**Returns:** Desktop coordinates; Position while hidden or detached.

**NotSupportedException:** The active Wayland compositor does not disclose global positions.

**InvalidOperationException:** The caller is not the owner or native geometry is unavailable.

**OverflowException:** The outer position exceeds integer coordinates.

**ObjectDisposedException:** The window is disposed.

<a id="getsizewithdecorations"></a>
### `public Vector2i GetSizeWithDecorations()`

Gets the outer window size, including native borders when visible and active.

**Returns:** Native window units; on Wayland, client pixels because decoration extents are unavailable. Hidden or detached windows return Size.

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

Inherited [viewport transforms and pointer coordinates](Viewport.md#canvas-transforms-and-pointer-coordinates) participate in root rendering and native input. GetVisibleRect stays in client-sized viewport units. Native desktop position remains a platform capability, including for CanvasItem.GetScreenTransform.

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
| `public System.Boolean Borderless { get; set; }` | Gets or sets the policy removing native window borders and title bar. |
| `public System.Boolean Exclusive { get; set; }` | Gets or sets whether input outside this window is blocked while visible. |
| `public Electron2D.LayoutDirection LayoutDirection { get; set; }` | Gets or sets the direction inherited by this window's controls. |
| `public System.Boolean MaximizeDisabled { get; set; }` | Gets or sets whether user maximization is disabled. |
| `public System.Boolean MinimizeDisabled { get; set; }` | Gets or sets whether user minimization is disabled. |
| `public System.Boolean PopupWMHint { get; set; }` | Gets or sets the popup window-manager hint, retained by the embedded host. |
| `public System.Boolean PopupWindow { get; set; }` | Gets or sets the popup input and outside-click close-request policy. |
| `public System.Boolean Transient { get; set; }` | Gets or sets whether this window returns focus to its parent when hidden. |
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
