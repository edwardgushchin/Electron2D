# SubViewport

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.SubViewport` · **Source:** [SubViewport.cs](../../src/Scene/Main/SubViewport.cs).

**Inherits:** [Viewport](Viewport.md).

## Description

Renders an independent scene canvas into a live viewport texture without creating a window.

An independent neutral Node with an RGBA8 canvas target, default Size=512 by 512, logical override zero/stretch false, clear Always and update WhenVisible. Size clamps each axis to at least two. Logical visible size uses a nonzero override vector; stretch requires both axes positive and composes before GlobalCanvasTransform. Committed override changes invalidate retained geometry, including externally targeted layers, and adjust StyleBoxFlat AA recording scale. Clear/update enums are shared with the server domain. Successful Once clear becomes Never and Once update becomes Disabled. Never preserves native pixels; Disabled and unsampled WhenVisible retain the completed image. WhenParentVisible follows a submitted parent viewport; Always/Once work with a hidden root window. Window children still reject, while offscreen children have a static PackedScene factory. Target dimensions above 16384 per axis reject in the current backend before allocation. ViewCount remains Blocked for real layered/multiview target storage and per-view sampling; no inert selector is exposed. Inherited non-root GUI context/container APIs retain their own exact gaps.

Native targets belong to RenderingServer, which reuses the active device. Completed/write image pairs avoid writable-attachment feedback; producers update before consumers and cycles observe available completed data. Scene input is isolated by viewport, with explicit PushInput removing that viewport final transform. Rendering/mutation/query uses the attached owner thread. No new clock, vendor patch or backend dependency is introduced. [The offscreen component](../components/canvas-rendering.md#offscreen-canvas-targets) records actual behavior and exact gaps.

## Example

This snippet requires the indicated live window/view and an authored child hierarchy; [SubViewportTests](../../tests/Electron2D.Tests/SubViewportTests.cs) exercises this public workflow.

```csharp
var view = new SubViewport
{
    Name = "preview",
    Size = new Vector2i(128, 128),
    TransparentBG = true,
    RenderTargetUpdateMode = ViewportUpdateMode.WhenVisible,
};
view.AddChild(previewRoot);
window.AddChild(view);
var sprite = new Sprite { Texture = view.GetTexture(), Centered = false };
window.AddChild(sprite);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public SubViewport()` | Creates a 512 by 512 offscreen viewport with Always clear and WhenVisible updates. |

## Constructor Descriptions

<a id="member-57b731c146eb"></a>
### .ctor

`public SubViewport()`

Creates a 512 by 512 offscreen viewport with Always clear and WhenVisible updates.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.ViewportClearMode RenderTargetClearMode { get; set; }` | Gets or sets clear policy; Once becomes Never only after a successful update. |
| `public Electron2D.ViewportUpdateMode RenderTargetUpdateMode { get; set; }` | Gets or sets update policy; Once becomes Disabled only after a successful update. |
| `public Electron2D.Vector2i Size { get; set; }` | Gets or sets native pixel dimensions, clamping each axis to at least two. |
| `public Electron2D.Vector2i Size2DOverride { get; set; }` | Gets or sets logical canvas size metadata; a zero vector uses native size. |
| `public System.Boolean Size2DOverrideStretch { get; set; }` | Gets or sets whether positive logical override axes scale rendering to native dimensions. |

## Property Descriptions

<a id="member-246a2ffb244c"></a>
### RenderTargetClearMode

`public Electron2D.ViewportClearMode RenderTargetClearMode { get; set; }`

Gets or sets clear policy; Once becomes Never only after a successful update.

Value: Always initially.

System.ArgumentOutOfRangeException: The enumeration is undefined.

System.ObjectDisposedException: The viewport is disposed.

System.InvalidOperationException: Mutation occurs off-owner or during native submission.

<a id="member-78300b34ec4b"></a>
### RenderTargetUpdateMode

`public Electron2D.ViewportUpdateMode RenderTargetUpdateMode { get; set; }`

Gets or sets update policy; Once becomes Disabled only after a successful update.

Value: WhenVisible initially.

System.ArgumentOutOfRangeException: The enumeration is undefined.

System.ObjectDisposedException: The viewport is disposed.

System.InvalidOperationException: Mutation occurs off-owner or during native submission.

<a id="member-49be82571be8"></a>
### Size

`public Electron2D.Vector2i Size { get; set; }`

Gets or sets native pixel dimensions, clamping each axis to at least two.

Value: 512 by 512 initially.

Remarks: Native target recreation is a cold operation. Backend size limits are checked before allocation. A committed size change notifies texture consumers and SizeChanged even after a failing observer.

System.ObjectDisposedException: The viewport is disposed.

System.InvalidOperationException: Mutation occurs off-owner or during native submission.

<a id="member-03ada55f6496"></a>
### Size2DOverride

`public Electron2D.Vector2i Size2DOverride { get; set; }`

Gets or sets logical canvas size metadata; a zero vector uses native size.

Value: Zero initially; finite signed integer axes retain their values.

Remarks: Stretch requires both override axes to be positive. Geometry, controls and camera bounds use the visible logical rectangle.

System.ObjectDisposedException: The viewport is disposed.

System.InvalidOperationException: Mutation occurs off-owner or during native submission.

<a id="member-4c235db882c6"></a>
### Size2DOverrideStretch

`public System.Boolean Size2DOverrideStretch { get; set; }`

Gets or sets whether positive logical override axes scale rendering to native dimensions.

Value: False initially.

System.ObjectDisposedException: The viewport is disposed.

System.InvalidOperationException: Mutation occurs off-owner or during native submission.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public override Electron2D.Rect2 GetVisibleRect()` | Returns the client rectangle in viewport coordinates. |

## Method Descriptions

<a id="member-c4a181f281dc"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-cf771e10d4ef"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-7ff22797905d"></a>
### GetVisibleRect

`public override Electron2D.Rect2 GetVisibleRect()`

Returns the client rectangle in viewport coordinates.

Returns: A zero-origin rectangle in client units, independent of desktop and node position.

System.ObjectDisposedException: The viewport is disposed.


## Verification and dependencies

SubViewportTests verifies guards/defaults/clamps, stable RID/texture identity, observer failures, scene-local copies, input/transform isolation, owner checks and nonunit AA recording invalidation. Two Linux Wayland GPU/two compatibility Engine.Run cycles check ten pixel phases; additional hosts check dependencies, external layers, feedback, stretch, hidden root and detached cleanup. Warm 64 active and 64 idle frames measure zero owner managed bytes separately from explicit readback. GPU checks uniform-only visibility/live sampling and server RID readback. Native/driver allocation totals, other platforms/browser, human acceptance, file/editor/container and multiview/layered storage remain unverified or absent. [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0008](../decisions/scene.md#adr-0008) define rendering and node roles.

A direct stretched [SubViewportContainer](SubViewportContainer.md) owns Size; manual Size writes reject while Stretch is true. The container configures child update/handled policy on entry/visibility, redraws its native texture, and forwards localized GUI/scene input. Child controls now have independent focus/hover/capture/tooltips; connected drag sections are shared. Native target ownership and resize remain with the renderer.
