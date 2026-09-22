# Viewport

Last updated: 2026-09-23

**Inherits:** [Node](Node.md)

**Inherited By:** [Window](Window.md)

- **Source:** [`src/Scene/Main/Viewport.cs`](../../src/Scene/Main/Viewport.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Viewport : Node`

## Description

Provides the root window's client rectangle and scene input boundary.

Only a root `Window` is currently supported. Render targets, canvas drawing, content scaling, and embedded viewports are not implemented. Input coordinates use the client area.

Native lifetime belongs to Engine.Run. Viewport inherits the neutral Node; canvas children supply their own transforms and visibility. Window.Position uses native desktop coordinates. Direct SceneTree(Window) activation and insertion of a Viewport as a child are rejected. Rendering and multiwindow behavior remain incomplete; see the [coverage page](../coverage/classes/Viewport.md).

## Examples

Inside a Node input callback (surrounding callback/event variables are supplied by the scene):

```csharp
if (inputEvent.IsActionPressed("confirm"))
    GetViewport()!.SetInputAsHandled();
```

This stops later scene input stages. It does not change Input polling state. `PushInput` borrows the caller's event and accepts client coordinates only.

## Methods

| Member | Contract |
| --- | --- |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown. |
| [`public abstract Rect GetVisibleRect()`](#getvisiblerect) | Returns the client rectangle in viewport coordinates. |
| [`public bool IsInputHandled()`](#isinputhandled) | Reports whether the current scene input event has been handled. |
| [`public void PushInput(InputEvent inputEvent)`](#pushinput) | Delivers a borrowed input event directly to this viewport's scene. |
| [`public void SetInputAsHandled()`](#setinputashandled) | Marks the scene input event currently being dispatched as handled. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action? SizeChanged`](#sizechanged) | Occurs after the client size changes, before subsequent frame callbacks. |

## Method Descriptions

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown.

<a id="getvisiblerect"></a>
### `public abstract Rect GetVisibleRect()`

Returns the client rectangle in viewport coordinates.

**Returns:** A zero-origin rectangle in client units, independent of desktop and node position.

**ObjectDisposedException:** The viewport is disposed.

<a id="isinputhandled"></a>
### `public bool IsInputHandled()`

Reports whether the current scene input event has been handled.

**Returns:** The active event's handled state; the state resets for each event.

**InvalidOperationException:** The viewport is detached, no input is being dispatched, or the caller is not the owner.

**ObjectDisposedException:** The viewport or scene tree is disposed.

<a id="pushinput"></a>
### `public void PushInput(InputEvent inputEvent)`

Delivers a borrowed input event directly to this viewport's scene.

**inputEvent:** A live event in client coordinates, retained and disposed by the caller.

Does not update global Input state or emulate pointer devices. Dispatch uses the existing scene input, unhandled-key, and unhandled-input stages. Nested dispatch is rejected.

**ArgumentNullException:** `inputEvent` is null.

**InvalidOperationException:** The viewport is detached, accessed off-thread, or the scene cannot accept input.

**ObjectDisposedException:** The event, viewport, or tree is disposed.

**AggregateException:** Scene callbacks fail after dispatch.

<a id="setinputashandled"></a>
### `public void SetInputAsHandled()`

Marks the scene input event currently being dispatched as handled.

Stops later scene input callbacks without changing the global polling state.

**InvalidOperationException:** The viewport is detached, no input is being dispatched, or the caller is not the owner.

**ObjectDisposedException:** The viewport or scene tree is disposed.

## Event Descriptions

<a id="sizechanged"></a>
### `public event Action? SizeChanged`

Occurs after the client size changes, before subsequent frame callbacks.

Subscribers run synchronously on the scene owner thread. Desktop movement does not notify.

## Lifecycle, verification and limits

See the [Window runtime component](../components/window-runtime.md) for ownership, native startup/cleanup failure behavior and exact executable checks. WindowRuntimeTests passed with SDL dummy and native Wayland; native events were injected. Physical-input/visual acceptance of this new API, other platforms, rendering, content scaling, offscreen targets, GUI and nested windows remain unverified or absent. Native Wayland rejects Position and may constrain geometry; focus requests obey compositor policy.

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028).
