# Viewport

Last updated: 2026-09-23

**Inherits:** [Node](Node.md)

**Inherited By:** [Window](Window.md)

- **Source:** [`src/Scene/Main/Viewport.cs`](../../src/Scene/Main/Viewport.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract partial class Viewport : Node`

## Description

Provides the root window's client rectangle and scene input boundary.

Only a root `Window` is currently supported. Offscreen render targets, content scaling, and embedded viewports are not implemented. Canvas sampling defaults are connected to the root renderer. Input coordinates use the client area.

Native lifetime belongs to Engine.Run. Viewport inherits the neutral Node; canvas children supply their own transforms and visibility. Window.Position uses native desktop coordinates. Direct SceneTree(Window) activation and insertion of a Viewport as a child are rejected. Rendering and multiwindow behavior remain incomplete; see the [coverage page](../coverage/classes/Viewport.md).

## Examples

Inside a Node input callback (surrounding callback/event variables are supplied by the scene):

```csharp
if (inputEvent.IsActionPressed("confirm"))
    GetViewport()!.SetInputAsHandled();
```

This stops later scene input stages. It does not change Input polling state. `PushInput` borrows the caller's event and accepts client coordinates only.

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
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds stored sampling and pixel-snapping properties to neutral node descriptors. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears this class's subscribers, then disposes inherited state. Overrides must call base. Engine.Run separately releases native ownership after scene teardown. |
| [`public abstract Rect GetVisibleRect()`](#getvisiblerect) | Returns the client rectangle in viewport coordinates. |
| [`public bool IsInputHandled()`](#isinputhandled) | Reports whether the current scene input event has been handled. |
| [`public void PushInput(InputEvent inputEvent)`](#pushinput) | Delivers a borrowed input event directly to this viewport's scene. |
| [`public void SetInputAsHandled()`](#setinputashandled) | Marks the scene input event currently being dispatched as handled. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action? SizeChanged`](#sizechanged) | Occurs after the client size changes, before subsequent frame callbacks. |

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends Node descriptors with three typed stored sampling properties and two pixel-snapping flags. Window adds its own properties through base chaining. Descriptors retain each property's validation and use the active project anisotropy default.

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

Sampling verification: [managed checks](../../tests/Electron2D.Tests/CanvasSamplingTests.cs), [native readback and rejection checks](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs).
