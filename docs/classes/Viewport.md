# Viewport

Last updated: 2026-09-24

**Inherits:** [Node](Node.md)

**Inherited By:** [Window](Window.md)

- **Source:** [`src/Scene/Main/Viewport.cs`](../../src/Scene/Main/Viewport.cs), [`src/Scene/Main/Viewport.PhysicsInterpolation.cs`](../../src/Scene/Main/Viewport.PhysicsInterpolation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract partial class Viewport : Node`

## Description

Provides the root window's client rectangle and scene input boundary.

Only a root `Window` is currently supported. Offscreen render targets, content scaling, and embedded viewports are not implemented. Canvas transforms, sampling and pixel-snapping policies are connected to the root renderer. Incoming window input is converted to viewport coordinates.

Native lifetime belongs to Engine.Run. Viewport inherits the neutral Node; canvas children supply their own transforms and visibility. Current Camera updates notify attached [Parallax](Parallax.md) nodes of the adjusted screen origin. Window.Position uses native desktop coordinates. Direct SceneTree(Window) activation and insertion of a Viewport as a child are rejected. Rendering and multiwindow behavior remain incomplete; see the [coverage page](../coverage/classes/Viewport.md).

A current physics-interpolated Camera retains previous and current `CanvasTransform` values for the renderer. Public `CanvasTransform`, input conversion and camera logical queries keep the latest value. Camera switching, reset and scene-wide policy changes discard the historical presentation pose.

## Examples

Inside a Node input callback (surrounding callback/event variables are supplied by the scene):

```csharp
if (inputEvent.IsActionPressed("confirm"))
    GetViewport()!.SetInputAsHandled();
```

This stops later scene input stages. It does not change Input polling state. `PushInput` retains caller ownership and accepts client coordinates by default, or viewport coordinates with `inLocalCoordinates: true`. Positional conversion creates a temporary event owned by dispatch.

The root viewport owns keyboard focus for its controls. `GetGUIFocusOwner()` returns the current borrowed control; `ReleaseGUIFocus()` clears it. A new owner raises `GUIFocusChanged` before that control's focus notification and event. Unhandled Tab and directional actions can move focus to visible controls with `FocusMode.All` or an explicit path.

## Canvas transforms and pointer coordinates

Source: [Viewport.Transforms.cs](../../src/Scene/Main/Viewport.Transforms.cs).

| Declaration | Contract |
| --- | --- |
| `public Transform CanvasTransform { get; set; }` | [Default canvas placement](#canvastransform) |
| `public Transform GlobalCanvasTransform { get; set; }` | [Outer canvas placement](#globalcanvastransform) |
| `public Transform GetFinalTransform()` | [Viewport-to-client transform](#getfinaltransform) |
| `public Transform GetScreenTransform()` | [Viewport-to-window transform](#getscreentransform) |
| `public Vector2 GetMousePosition()` | [Native pointer in viewport units](#getmouseposition) |
| `public void WarpMouse(Vector2 position)` | [Request native pointer movement](#warpmouse) |

Example, configuring an existing live root window on its owner thread:

```csharp
window.CanvasTransform = new Transform(0, new Vector2(20, 10));
window.GlobalCanvasTransform = new Transform(new Vector2(2, 0), new Vector2(0, 2), Vector2.Zero);
```

### CanvasTransform

`public Transform CanvasTransform { get; set; }`

Identity by default. Maps the default canvas into viewport coordinates. Rendering composes framebuffer scale, final transform, CanvasTransform, then node/drawing transforms. Neutral parents and TopLevel do not remove the viewport transforms. Changes affect the next submission without rerecording retained commands or changing logical node transforms/notifications.

When a current camera uses physics interpolation, this property still returns its current logical transform; the renderer interpolates only its presentation copy. A caller assignment outside a physics tick resets that history.

### GlobalCanvasTransform

`public Transform GlobalCanvasTransform { get; set; }`

Identity by default. Maps viewport coordinates into the native client area after CanvasTransform. Its inverse localizes incoming window input; use CanvasItem.MakeInputLocal separately for canvas/node conversion. Both transform properties accept finite singular values for rendering. They are typed runtime descriptors, not stored by PackedScene; reconstructed windows start at identity. Nonfinite assignment throws ArgumentException; mutation during capture or off the attached owner thread throws InvalidOperationException. Queries enforce the attached owner; disposed access throws ObjectDisposedException.

### GetFinalTransform

`public Transform GetFinalTransform()`

Returns GlobalCanvasTransform. The supported root window has identity content stretch; CanvasTransform, desktop position and framebuffer density are excluded. Attached off-owner access throws InvalidOperationException; disposed access throws ObjectDisposedException.

### GetScreenTransform

`public Transform GetScreenTransform()`

Returns GetFinalTransform for the native root window. Screen here is the containing window coordinate space: it does not include desktop placement. The same query guards apply. Embedded/offscreen viewports remain absent.

### GetMousePosition

`public Vector2 GetMousePosition()`

Polls the native client pointer and applies the inverse GetScreenTransform; a singular transform returns zero. Requires an active native root Window and its owner thread, otherwise InvalidOperationException. Does not read the last PushInput event or update Input polling. Fractional client coordinates are retained; CanvasTransform is not removed. Disposed access throws ObjectDisposedException.

### WarpMouse

`public void WarpMouse(Vector2 position)`

Transforms finite viewport coordinates with GetScreenTransform, then truncates to native integer client units. CanvasTransform is not applied. Requires an active native Window and owner thread; capture mutation is rejected. Nonfinite or out-of-Int32 transformed coordinates throw ArgumentException before the native request. Unsupported warping throws NotSupportedException; platform policy can prevent actual movement even when a request is supported. Disposed access throws ObjectDisposedException.

Verification: [managed contracts](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs) and [native pixels, injected input and pointer queries](../../tests/Electron2D.Tests/CanvasCoordinateRenderingTests.cs). Linux Wayland GPU/compatibility and SDL dummy compatibility passed. Native pointer warp success and other platforms have not been verified; unsupported policies are checked explicitly. Camera and CanvasLayer are integrated; content stretch and nested viewport integration remain coverage gaps.

## Pixel snapping properties

Implemented in [Viewport.Rendering.cs](../../src/Scene/Main/Viewport.Rendering.cs).

| Declaration | Default | Contract |
| --- | --- | --- |
| `public bool SnapTransformsToPixel { get; set; }` | false | [Transform snapping](#snaptransformstopixel) |
| `public bool SnapVerticesToPixel { get; set; }` | false | [Vertex snapping](#snapverticestopixel) |

```csharp
var window = new Window { SnapTransformsToPixel = true };
```

### SnapTransformsToPixel

Rounds the local and accumulated parent translations using `floor(value + 0.5)` before rendering composition. Y sorting uses snapped local translations and retains the flattened group transform. Neutral nodes and TopLevel break the canvas chain. Logical Position/Transform and global queries remain unchanged. Attached Sprite bounds and opacity queries round their local drawing offset too; detached queries do not.

Changes apply to the next submission but do not request redraw or emit ItemRectChanged. Sprite commands retain their previously recorded local offset until QueueRedraw or another invalidation. To update a retained Sprite offset after a live policy change, request its redraw. Both positive and negative exact halves round toward positive infinity.

Canvas translations use a separate viewport preparation step: `ceil(position - 0.5)` for even dimensions and `ceil(position)` for odd dimensions. Following layers round their parent canvas in follow-scale units before center-based scaling. Logical matrices are unchanged; [CanvasLayer](CanvasLayer.md#transform-and-source-audit) describes the composition and zero-scale behavior.

### SnapVerticesToPixel

Rounds final primitive corners after node, drawing and framebuffer transforms. Does not affect Sprite bounds or source-opacity queries. Retained commands use the current flag at each submission. Texture clipping interpolation points remain inside the snapped triangles, preventing artificial cuts from collapsing or opening gaps. A small normalized UV offset follows the renderer precision convention. Degenerate snapped texture triangles emit no geometry.

Both properties are stored by PackedScene. Defaults are false on Viewport; construction of the explicit root Window reads active project overrides before caller configuration. Off-owner/capture mutation throws InvalidOperationException; disposed access throws ObjectDisposedException. Neither setter opens native resources. Both may be enabled, although combining them can make motion less smooth. GUI control snapping is a separate absent capability.

## Sampling properties and enums

Source: [Viewport.Sampling.cs](../../src/Scene/Main/Viewport.Sampling.cs).

| Declaration | Default | Contract |
| --- | --- | --- |
| `public DefaultCanvasItemTextureFilter CanvasItemDefaultTextureFilter { get; set; }` | Linear | [Default filtering](#canvasitemdefaulttexturefilter) |
| `public DefaultCanvasItemTextureRepeat CanvasItemDefaultTextureRepeat { get; set; }` | Disabled | [Default addressing](#canvasitemdefaulttexturerepeat) |
| `public AnisotropicFiltering AnisotropicFilteringLevel { get; set; }` | Project setting, normally Anisotropy4X | [Anisotropy](#anisotropicfilteringlevel) |

Enums: [DefaultCanvasItemTextureFilter](Viewport.DefaultCanvasItemTextureFilter.md), [DefaultCanvasItemTextureRepeat](Viewport.DefaultCanvasItemTextureRepeat.md), [AnisotropicFiltering](Viewport.AnisotropicFiltering.md). Their pages list all numeric values, including sentinels.

Example, during detached window construction:

```csharp
var window = new Window
{
    CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
    CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Disabled,
};
```

### CanvasItemDefaultTextureFilter

`public DefaultCanvasItemTextureFilter CanvasItemDefaultTextureFilter { get; set; }`

Linear by default. Chooses the final policy for canvas chains whose filter remains inherited. ParentNode resolves through a direct canvas/viewport parent, falling back to Linear. Actual changes request redraw through direct inheriting canvas children; independent canvas roots read the new viewport default at submission. Equal assignments do nothing; no PropertyListChanged is emitted here. Detached settings apply on entry. GPU supports mipmap choices; compatibility rejects them, and software triangles additionally reject Linear. Root linear/clamp defaults therefore require explicit nearest selection for software texture drawing.

### CanvasItemDefaultTextureRepeat

`public DefaultCanvasItemTextureRepeat CanvasItemDefaultTextureRepeat { get; set; }`

Disabled by default. Enabled repeats, Mirror reflects alternate tiles; ParentNode resolves through a direct canvas/viewport parent or falls back to Disabled. Redraw, no-op and entry behavior match the filter property. Tiled draw commands force ordinary repeat. Mirror requires GPU rendering; compatibility wrapping remains subject to the driver's capability.

### AnisotropicFilteringLevel

`public AnisotropicFiltering AnisotropicFilteringLevel { get; set; }`

Construction samples the active ProjectSettings.AnisotropicFilteringLevel override (normally 2 = Anisotropy4X). Later project changes do not mutate the viewport. Live changes affect the next submission. Only anisotropic CanvasItem filters use this limit; Disabled retains ordinary mip filtering. Supported limits are disabled, 2, 4, 8 and 16 samples. Precision depends on the GPU driver. Arbitrary material parameters retain their existing fixed sampler profile.

All three properties are stored by PackedScene. Undefined/negative/Max enum writes throw ArgumentOutOfRangeException before mutation; scene capture and off-owner writes throw InvalidOperationException; disposed access throws ObjectDisposedException. They neither allocate GPU resources nor open a window until normal rendering consumes the state. Nested/offscreen viewport activation remains unsupported.

## Methods

| Member | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds stored sampling/pixel-snapping properties and runtime canvas transforms to neutral node descriptors. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown. |
| [`protected override void OnNotification(int what)`](#onnotification) | Resets camera presentation history on the inherited interpolation notification. |
| [`public Camera? GetCamera()`](#getcamera) | Returns the borrowed active camera, or null. |
| [`public Control? GetGUIFocusOwner()`](#getguifocusowner) | Returns the root viewport's borrowed focused control, or null. |
| [`public abstract Rect2 GetVisibleRect()`](#getvisiblerect) | Returns the client rectangle in viewport coordinates. |
| [`public bool IsInputHandled()`](#isinputhandled) | Reports whether the current scene input event has been handled. |
| [`public void PushInput(InputEvent inputEvent, bool inLocalCoordinates = false)`](#pushinput) | Delivers a borrowed input event directly to this viewport's scene. |
| [`public void ReleaseGUIFocus()`](#releaseguifocus) | Releases this viewport's focused control, if any. |
| [`public void SetInputAsHandled()`](#setinputashandled) | Marks the scene input event currently being dispatched as handled. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action? SizeChanged`](#sizechanged) | Occurs after the client size changes, before subsequent frame callbacks. |
| [`public event Action<Control>? GUIFocusChanged`](#guifocuschanged) | Occurs when a new control gains keyboard focus; not raised on release. |

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends Node descriptors with three typed stored sampling properties, two stored pixel-snapping flags, and two non-stored runtime canvas transforms. Window adds its own properties through base chaining. Descriptors retain each property's validation and use the active project anisotropy default.

## Method Descriptions

<a id="onnotification"></a>
### `protected override void OnNotification(int what)`

Forwards inherited node notifications and resets the camera presentation snapshot before delivering `NotificationResetPhysicsInterpolation` to derived implementations. This preserves the current logical canvas transform.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown.

<a id="getvisiblerect"></a>
### `public abstract Rect2 GetVisibleRect()`

Returns the client rectangle in viewport coordinates.

**Returns:** A zero-origin rectangle in client units, independent of desktop and node position.

**ObjectDisposedException:** The viewport is disposed.

<a id="getguifocusowner"></a>
### `public Control? GetGUIFocusOwner()`

Returns the borrowed keyboard focus owner in the root viewport, or null while unfocused or detached. Attached access requires the scene owner thread; disposed access throws ObjectDisposedException. The result is a live Control reference, not a snapshot.

<a id="isinputhandled"></a>
### `public bool IsInputHandled()`

Reports whether the current scene input event has been handled.

**Returns:** The active event's handled state; the state resets for each event.

**InvalidOperationException:** The viewport is detached, no input is being dispatched, or the caller is not the owner.

**ObjectDisposedException:** The viewport or scene tree is disposed.

<a id="pushinput"></a>
### `public void PushInput(InputEvent inputEvent, bool inLocalCoordinates = false)`

Delivers a borrowed input event directly to this viewport's scene.

**inputEvent:** A live event retained and disposed by the caller.

**inLocalCoordinates:** False removes `GetFinalTransform()` from window-client coordinates; true borrows already-local viewport input unchanged. It does not remove `CanvasTransform`.

Does not update global Input state or emulate pointer devices. Dispatch uses the existing scene input, unhandled-key, and unhandled-input stages. Nested dispatch is rejected before conversion. Positional conversion uses `InputEvent.XformedBy`; mouse GlobalPosition is then set to localized Position. Relative/Velocity use the inverse basis; screen vectors and pan delta are unchanged. The temporary event is disposed in finally, including callback failure; callbacks must duplicate it to retain it. Non-positional events are borrowed unchanged. Copies have distinct InstanceID values: by-event action transition queries still identify the original parsed event.

**ArgumentNullException:** `inputEvent` is null.

**ArgumentOutOfRangeException:** Transformed event coordinates overflow finite values, before a copy is allocated.

**InvalidOperationException:** The viewport is detached, accessed off-thread, the scene cannot accept input, or the required final transform is singular.

**ObjectDisposedException:** The event, viewport, or tree is disposed.

**AggregateException:** Scene callbacks fail after dispatch.

<a id="releaseguifocus"></a>
### `public void ReleaseGUIFocus()`

Clears the root viewport's focused control and sends NotificationFocusExit, then FocusExited, to that control. It does nothing while detached or already unfocused. It does not raise GUIFocusChanged. Attached access requires the scene owner thread; disposed access throws ObjectDisposedException. A callback failure propagates as AggregateException after the state is cleared.

<a id="setinputashandled"></a>
### `public void SetInputAsHandled()`

Marks the scene input event currently being dispatched as handled.

Stops later scene input callbacks without changing the global polling state.

**InvalidOperationException:** The viewport is detached, no input is being dispatched, or the caller is not the owner.

**ObjectDisposedException:** The viewport or scene tree is disposed.

### GetCamera

`public Camera? GetCamera()`

Returns the borrowed active Camera for the default canvas, or null when none is selected. First enabled entry claims an empty viewport. Explicit MakeCurrent switches immediately; disabling/removing the current camera chooses the first enabled camera in current tree order, which updates at its next tracking call. When none remains, CanvasTransform resets to identity. GlobalCanvasTransform is unchanged. Attached off-owner access throws InvalidOperationException; disposed access throws ObjectDisposedException. See [Camera](Camera.md).

## Event Descriptions

<a id="guifocuschanged"></a>
### `public event Action<Control>? GUIFocusChanged`

Raised synchronously on the scene owner thread after the old control exits focus and before the new control receives NotificationFocusEnter and FocusEntered. The argument is the borrowed new owner. Explicit release and repeated focus on the same owner do not raise it. A throwing subscriber does not prevent the new control's focus notification and event; the failure is aggregated after the transition.

<a id="sizechanged"></a>
### `public event Action? SizeChanged`

Occurs after the client size changes, before subsequent frame callbacks.

Subscribers run synchronously on the scene owner thread. Desktop movement does not notify.

## Lifecycle, verification and limits

See the [Window runtime component](../components/window-runtime.md) for ownership, native startup/cleanup failure behavior and exact executable checks. WindowRuntimeTests passed with SDL dummy and native Wayland; native events were injected. [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs) covers managed root GUI focus ownership, transition order, release, callback failures and owner-thread rejection. Physical-input/visual acceptance and other platforms remain unverified. Root rendering, Control rectangle layout and root viewport pointer/focus routing are implemented; content scaling, offscreen targets, complete interactive GUI and nested windows remain absent. Native Wayland rejects Position and may constrain geometry; native window focus requests obey compositor policy.

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028).

Sampling verification: [managed checks](../../tests/Electron2D.Tests/CanvasSamplingTests.cs), [native readback and rejection checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs).

## Canvas culling mask

| Declaration | Contract |
| --- | --- |
| `public uint CanvasCullMask { get; set; }` | [Stored canvas selection](#canvascullmask) |
| `public bool GetCanvasCullMaskBit(int layer)` | [Read one bit](#getcanvascullmaskbit) |
| `public void SetCanvasCullMaskBit(int layer, bool enable)` | [Change one bit](#setcanvascullmaskbit) |

### CanvasCullMask

`public uint CanvasCullMask { get; set; }`

Defaults to `uint.MaxValue`; all 32 bits and zero are valid. Stored by PackedScene. The root renderer tests every canvas item and each direct canvas ancestor independently against this mask. Parent and child need not share bits with each other: layers 1 and 2 both pass viewport mask 3. CanvasLayer groups use the same viewport mask; TopLevel and neutral-node boundaries establish independent item roots.

Zero suppresses geometry while clear, frame callbacks and presentation continue. Culling does not change logical visibility, input, processing, scene order or pending OnDraw. Changes reuse retained commands and affect submission after drawing callbacks. Compatibility shader capability rejection applies only to submitted geometry, so a fully culled shader material does not fail until it is selected for drawing.

Live detached configuration is allowed. Attached access requires the owner thread; off-owner access or mutation during scene capture throws InvalidOperationException. Disposed access throws ObjectDisposedException. Independent/offscreen viewport rendering remains absent. See [CanvasItem.VisibilityLayer](CanvasItem.md#visibilitylayer) and the [source audit and verification](../components/canvas-rendering.md#canvas-visibility-masks).

### GetCanvasCullMaskBit

`public bool GetCanvasCullMaskBit(int layer)`

Returns whether the zero-based bit 0 through 31 is set. Invalid indices throw ArgumentOutOfRangeException. The same query owner/disposal guards as CanvasCullMask apply.

### SetCanvasCullMaskBit

`public void SetCanvasCullMaskBit(int layer, bool enable)`

Sets or clears the zero-based bit 0 through 31 without changing other bits. Invalid indices throw ArgumentOutOfRangeException before mutation. The same owner/capture/disposal guards and retained-command behavior as CanvasCullMask apply.

```csharp
window.CanvasCullMask = 0;
window.SetCanvasCullMaskBit(31, true);
```
