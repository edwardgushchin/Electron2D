# Window

Last updated: 2026-09-22

**Inherits:** [Viewport](Viewport.md)

**Inherited By:** —

- **Source:** [`src/Scene/Main/Window.cs`](../../src/Scene/Main/Window.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Window : Viewport`

## Description

A configurable native root window that owns scene children.

Pass a detached window to `Engine.Run(Window)`. The runtime opens its native window before scene entry and releases it after scene teardown. One root window is supported. The client size uses pixels on Wayland and native window units elsewhere. Rendering and embedded windows are not implemented.

Native lifetime belongs to Engine.Run. Children retain the unified Node transform and visibility rules. Desktop ScreenPosition is separate from scene Position. Direct SceneTree(Window) activation and insertion of a Viewport as a child are rejected. Rendering and multiwindow behavior remain incomplete; see the [coverage page](../coverage/classes/Window.md).

## Examples

```csharp
var window = new Window { Title = "Game", Size = new Vector2I(960, 540) };
window.AddChild(scene); // caller-created Node
Engine.Instance.MaxFps = 60;
int exitCode = Engine.Instance.Run(window);
```

Call `Tree!.Quit()` from a scene callback to exit. Run returns the requested code and consumes the supplied window hierarchy. The native window is open and Engine.MainLoop exposes the tree before OnReady.

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

## Methods

| Member | Contract |
| --- | --- |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Returns a static factory for an exact Window. Derived types must supply their own factory. PackedScene stores title, size, size limits and inherited stored Node properties; ScreenPosition is not stored. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends typed title, size, minimum and maximum size descriptors to inherited Node descriptors. |
| [`public override Rect GetVisibleRect()`](#getvisiblerect) | Returns the client rectangle in viewport coordinates. |
| [`public int GetWindowId()`](#getwindowid) | Gets the native window identity while running. |
| [`public void GrabFocus()`](#grabfocus) | Requests keyboard focus and foreground placement from the native system. |
| [`public bool HasFocus()`](#hasfocus) | Reports whether the active native window has keyboard focus. |
| [`public void RequestAttention()`](#requestattention) | Requests a platform attention indication until this window is focused. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action? CloseRequested`](#closerequested) | Occurs when the system requests closure of this root window. |
| [`public event Action? FocusEntered`](#focusentered) | Occurs when the window gains native keyboard focus. |
| [`public event Action? FocusExited`](#focusexited) | Occurs when the window loses native keyboard focus, before pressed input is released. |
| [`public event Action? TitleChanged`](#titlechanged) | Occurs after Title commits a different value. |

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

## Method Descriptions

<a id="createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Returns a static factory for an exact Window. Derived types must supply their own factory. PackedScene stores title, size, size limits and inherited stored Node properties; ScreenPosition is not stored.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends typed title, size, minimum and maximum size descriptors to inherited Node descriptors.

<a id="getvisiblerect"></a>
### `public override Rect GetVisibleRect()`

Returns the client rectangle in viewport coordinates.

**Returns:** A zero-origin rectangle in client units, independent of desktop and node position.

**ObjectDisposedException:** The viewport is disposed.

<a id="getwindowid"></a>
### `public int GetWindowId()`

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

## Lifecycle, verification and limits

See the [Window runtime component](../components/window-runtime.md) for ownership, native startup/cleanup failure behavior and exact executable checks. WindowRuntimeTests passed with SDL dummy and native Wayland; native events were injected. Physical-input/visual acceptance of this new API, other platforms, rendering, content scaling, offscreen targets, GUI and nested windows remain unverified or absent. Native Wayland rejects ScreenPosition and may constrain geometry; focus requests obey compositor policy.

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028).
