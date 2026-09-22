# Window

Last updated: 2026-09-22

**Inherits:** [Viewport](Viewport.md)

**Inherited By:** —

- **Source:** [`Window.cs`](../../src/Scene/Main/Window.cs), [`Window.Native.cs`](../../src/Scene/Main/Window.Native.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class Window : Viewport`

## Description

A configurable native root window that owns scene children.

Pass a detached window to `Engine.Run(Window)`. The runtime opens its native window before scene entry and releases it after scene teardown. One root window is supported. The client size uses pixels on Wayland and native window units elsewhere. The root canvas renders after scene processing; embedded windows are not implemented.

Native lifetime belongs to Engine.Run. Children retain the unified Node transform and visibility rules. Desktop ScreenPosition is separate from scene Position. Direct SceneTree(Window) activation and insertion of a Viewport as a child are rejected. The root canvas supports retained rectangles, lines, textures and GPU shader materials. Offscreen and multiwindow rendering remain incomplete; see the [coverage page](../coverage/classes/Window.md).

## Examples

```csharp
var window = new Window { Title = "Game", Size = new Vector2I(960, 540) };
window.AddChild(scene); // caller-created Node
Engine.Instance.MaxFPS = 60;
int exitCode = Engine.Instance.Run(window);
```

Call `Tree!.Quit()` from a scene callback to exit. Run returns the requested code and consumes the supplied window hierarchy. The native window and RenderingServer are open and Engine.MainLoop exposes the tree before OnReady.

## Constructors

| Member | Contract |
| --- | --- |
| [`public Window()`](#constructor) | Creates a detached visible window with an empty title and a 100 by 100 client area. |

## Properties

| Member | Contract |
| --- | --- |
| [`public Vector2I MaxSize { get; set; }`](#maxsize) | Gets or sets nonnegative maximum client dimensions; zero means no limit on that axis. |
| [`public Vector2I MinSize { get; set; }`](#minsize) | Gets or sets nonnegative minimum client dimensions; zero means no limit on that axis. |
| [`public Vector2I ScreenPosition { get; set; }`](#screenposition) | Gets or requests the client origin in native desktop coordinates. |
| [`public Vector2I Size { get; set; }`](#size) | Gets the observed client size or requests a positive client size. |
| [`public string Title { get; set; }`](#title) | Gets or sets the native window title. |
| [`public override bool Visible { get; set; }`](#visible) | Gets or sets this node's local logical visibility. |
| [`public ModeEnum Mode { get; set; }`](#mode) | Gets the observed native mode, or configures a presentation-mode request. |
| [`public int CurrentScreen { get; set; }`](#currentscreen) | Gets the observed display index, or requests placement on a zero-based display index. |
| [`public bool Unresizable { get; set; }`](#unresizable) | Gets or sets the policy preventing user border resizing. |
| [`public bool Borderless { get; set; }`](#borderless) | Gets or sets the policy removing native window borders and title bar. |
| [`public bool AlwaysOnTop { get; set; }`](#alwaysontop) | Gets or sets the policy requesting placement above ordinary windows. |
| [`public bool Unfocusable { get; set; }`](#unfocusable) | Gets or sets the policy preventing keyboard focus. |

## Methods

| Member | Contract |
| --- | --- |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Returns a static factory for an exact Window. Derived types must supply their own factory. PackedScene stores title, size, size limits, mode, supported policies and inherited stored Node properties; ScreenPosition and CurrentScreen are not stored. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends typed title, size, minimum/maximum size, mode and supported policy descriptors to inherited Node descriptors. |
| [`public override Rect GetVisibleRect()`](#getvisiblerect) | Returns the client rectangle in viewport coordinates. |
| [`public int GetWindowID()`](#getwindowid) | Gets the native window identity while running. |
| [`public void GrabFocus()`](#grabfocus) | Requests keyboard focus and foreground placement from the native system. |
| [`public bool HasFocus()`](#hasfocus) | Reports whether the active native window has keyboard focus. |
| [`public void RequestAttention()`](#requestattention) | Requests a platform attention indication until this window is focused. |
| [`public bool GetFlag(Flags flag)`](#getflag) | Gets a configured window policy. |
| [`public void SetFlag(Flags flag, bool enabled)`](#setflag) | Configures an executable policy and requests its native application when active. |
| [`public bool IsMaximizeAllowed()`](#ismaximizeallowed) | Reports whether the current resize policy permits native maximization. |
| [`public Vector2I GetPositionWithDecorations()`](#getpositionwithdecorations) | Gets the outer window origin, including native borders when visible and active. |
| [`public Vector2I GetSizeWithDecorations()`](#getsizewithdecorations) | Gets the outer window size, including native borders when visible and active. |
| [`public void MoveToCenter()`](#movetocenter) | Requests centering of the active client area in its current screen's usable rectangle. |
| [`public void SetIMEActive(bool active)`](#setimeactive) | Enables or disables native text input for the active window. |
| [`public void SetIMEPosition(Vector2I position)`](#setimeposition) | Requests native IME candidate placement at a client-coordinate caret. |
| [`public void SetTaskbarProgressState(DisplayServer.ProgressState state)`](#settaskbarprogressstate) | Requests a native taskbar progress indication for the active window. |
| [`public void SetTaskbarProgressValue(float value)`](#settaskbarprogressvalue) | Requests a native taskbar progress fraction for the active window. |

## Events

| Member | Contract |
| --- | --- |
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
| [ModeEnum](Window.ModeEnum.md) | Five native presentation requests, with observed-mode queries. |
| [Flags](Window.Flags.md) | Individual policy indices, not a bit mask; four executable policies. |

## Constructor Descriptions

<a id="constructor"></a>
### `public Window()`

Creates a detached visible window with an empty title and a 100 by 100 client area.

No native resources are acquired until `Engine.Run(Window)`.

## Property Descriptions

<a id="maxsize"></a>
### `public Vector2I MaxSize { get; set; }`

Gets or sets nonnegative maximum client dimensions; zero means no limit on that axis.

Zero by default.

**ArgumentOutOfRangeException:** A component is negative or a nonzero maximum is below the minimum.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="minsize"></a>
### `public Vector2I MinSize { get; set; }`

Gets or sets nonnegative minimum client dimensions; zero means no limit on that axis.

Zero by default.

**ArgumentOutOfRangeException:** A component is negative or exceeds a nonzero maximum.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="screenposition"></a>
### `public Vector2I ScreenPosition { get; set; }`

Gets or requests the client origin in native desktop coordinates.

The configured position before startup, or zero if no position was requested.

This is independent of inherited `Node.Position`. Leaving it unset lets the system place the window. A preconfigured position is applied at startup and can fail on an unsupported platform.

**NotSupportedException:** The active compositor does not expose or accept global window positions, including Wayland.

**InvalidOperationException:** The caller is not the owner or the native request fails.

**ObjectDisposedException:** The window is disposed.

<a id="size"></a>
### `public Vector2I Size { get; set; }`

Gets the observed client size or requests a positive client size.

100 by 100 before configuration or native activation.

Native changes may be asynchronous or constrained by the compositor and size limits. SizeChanged follows committed size changes; desktop position and inherited node transforms do not affect size.

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
### `public override bool Visible { get; set; }`

Gets or sets this node's local logical visibility.

`true` by default.

An actual change synchronously propagates visibility notifications and events through all descendants.

**InvalidOperationException:** An attached node is mutated off the owner thread.

**ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**Exception:** A visibility notification or event handler throws after visibility changes.

An active Window also shows/hides its native surface before updating Node visibility. Native failure preserves managed visibility; inherited Show/Hide use this override.

<a id="mode"></a>
### `public ModeEnum Mode { get; set; }`

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

<a id="createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Returns a static factory for an exact Window. Derived types must supply their own factory. PackedScene stores title, size, size limits, mode, supported policies and inherited stored Node properties; ScreenPosition and CurrentScreen are not stored.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends typed title, size, minimum/maximum size, mode and supported policy descriptors to inherited Node descriptors.

<a id="getvisiblerect"></a>
### `public override Rect GetVisibleRect()`

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
### `public Vector2I GetPositionWithDecorations()`

Gets the outer window origin, including native borders when visible and active.

**Returns:** Desktop coordinates; ScreenPosition while hidden or detached.

**NotSupportedException:** The active Wayland compositor does not disclose global positions.

**InvalidOperationException:** The caller is not the owner or native geometry is unavailable.

**OverflowException:** The outer position exceeds integer coordinates.

**ObjectDisposedException:** The window is disposed.

<a id="getsizewithdecorations"></a>
### `public Vector2I GetSizeWithDecorations()`

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
### `public void SetIMEPosition(Vector2I position)`

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

See the [Window runtime component](../components/window-runtime.md) for ownership, native startup/cleanup failure behavior and exact executable checks. WindowRuntimeTests passed with SDL dummy and native Wayland; native events were injected. Physical-input/visual acceptance of this new API, other platforms, rendering, content scaling, offscreen targets, GUI and nested windows remain unverified or absent. Native tests also cover mode/flag application, IME enable/disable, borrowed native drop-memory lifetime, managed path retention and window-signal failure cleanup. Dummy tests do not verify native flag changes because that backend accepts setters without applying them. Wayland rejects ScreenPosition and may constrain geometry; focus requests obey compositor policy.

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028).
