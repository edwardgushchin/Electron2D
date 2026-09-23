# CanvasItem

Last updated: 2026-09-23

**Inherits:** [Node](Node.md)

**Inherited By:** [Entity](Entity.md)

- **Source:** [CanvasItem.cs](../../src/Scene/Main/CanvasItem.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract partial class CanvasItem : Node`

## Description

The abstract canvas base. Owns visibility, Z/Y order, behind-parent drawing, modulation, materials, retained drawing and transform queries/notifications. Entity supplies a concrete spatial placement model. A direct CanvasItem subclass can provide its own model through GetTransform and notify changes with NotifyLocalTransformChanged. The Control/UI branch is not yet implemented. Only direct canvas parents contribute transforms, modulation and materials; a neutral Node breaks those chains. TopLevel preserves the local transform while ending transform/material/modulation/Z inheritance. Visibility follows direct canvas parents, including TopLevel items, and the containing window.

Canvas roots follow scene order; a root's canvas subtree is ordered before the following root. TopLevel and neutral Node boundaries create separate canvas roots. Effective Z is always the primary draw key. At equal Z, children normally draw after their parent; ShowBehindParent draws a child subtree before it. YSortEnabled instead sorts the item itself (Y = 0) and its canvas children by local Y, merging nested enabled groups while keeping other child subtrees together. Drawing order does not change processing or input order.

Canvas attachment is part of actual SceneTree membership. Entry delivers NotificationEnterCanvas before the tree-enter callback, then visibility delivery when initially visible. Exit delivers NotificationExitCanvas after the tree-exit callback, with children exiting first. Changing TopLevel emits an exit/entry pair for this item and schedules redraw; failures are aggregated after the transition. These notifications use ordinary C# override/base dispatch under the engine's notification contract. Manual tree notifications do not attach or detach a canvas.

Local Visible changes notify the item, including while detached or below a hidden parent. Effective changes propagate only through locally visible direct canvas children; Hidden follows visibility delivery when becoming hidden in the tree. Tree exit does not emit Hidden. Visible entry and showing schedule redraw. A visibility notification delivered with Notify raises VisibilityChanged through the base handler without changing state.

ItemRectChanged reports local geometry changes independently of transform notifications. Derived geometry models call NotifyItemRectChanged after committing state; events remain synchronous while hidden or detached. See [Sprite](Sprite.md#itemrectchanged) for its exact triggers.

## Sampling model

The built-in texture sampler uses [TextureFilter](#texturefilter) and [TextureRepeat](#texturerepeat). Defaults inherit the direct canvas parent, stopping at neutral parents and TopLevel; unresolved values use the containing viewport (initially linear/clamp). Sampling also controls the reserved TEXTURE input of custom shaders. Named material textures retain their separate fixed profile. See the [backend matrix](../components/canvas-rendering.md#texture-sampling).

## Examples

The snippet uses the Electron2D namespace; attach the hierarchy to a SceneTree or an Engine.Run window to activate it.

```csharp
class PaintedNode : Entity
{
    protected override void OnDraw() => DrawRect(new Rect(0, 0, 32, 16), Colors.Cyan);
}
```

## Viewport and input coordinates

Source: [CanvasItem.Coordinates.cs](../../src/Scene/Main/CanvasItem.Coordinates.cs). These are logical floating-point transforms, independent of render pixel snapping and framebuffer density. With `C = Viewport.CanvasTransform`, `F = Viewport.GetFinalTransform()` and `G = GetGlobalTransform()`, local drawing reaches client coordinates through `F * C * G`.

| Declaration | Contract |
| --- | --- |
| `public Rect GetViewportRect()` | [Visible viewport rectangle](#getviewportrect) |
| `public Transform GetCanvasTransform()` | [Canvas-to-viewport transform](#getcanvastransform) |
| `public Transform GetGlobalTransformWithCanvas()` | [Local-to-viewport transform](#getglobaltransformwithcanvas) |
| `public Transform GetViewportTransform()` | [Canvas-to-client transform](#getviewporttransform) |
| `public Transform GetScreenTransform()` | [Local-to-desktop transform](#getscreentransform) |
| `public Vector2 MakeCanvasPositionLocal(Vector2 viewportPoint)` | [Convert a viewport point](#makecanvaspositionlocal) |
| `public InputEvent MakeInputLocal(InputEvent inputEvent)` | [Convert a viewport event](#makeinputlocal) |
| `public Vector2 GetGlobalMousePosition()` | [Pointer in canvas coordinates](#getglobalmouseposition) |
| `public Vector2 GetLocalMousePosition()` | [Pointer in local coordinates](#getlocalmouseposition) |

In an attached item's OnInput callback, convert a pointer position without allocating an event:

```csharp
if (inputEvent is InputEventMouse mouse)
{
    Vector2 local = MakeCanvasPositionLocal(mouse.Position);
    // Use local for the item's own interaction logic.
}
```

### GetViewportRect

`public Rect GetViewportRect()`

Returns the active containing viewport's GetVisibleRect, in viewport coordinates. Canvas/node transforms do not change its bounds.

### GetCanvasTransform

`public Transform GetCanvasTransform()`

Returns the active viewport's CanvasTransform. Independent CanvasLayer transforms are not implemented.

### GetGlobalTransformWithCanvas

`public Transform GetGlobalTransformWithCanvas()`

Returns `C * G` while attached, or the logical global transform while detached (even under a detached viewport ancestor). TopLevel and neutral parents break only the node transform chain. GlobalCanvasTransform is excluded. An attached item without a viewport fails explicitly.

### GetViewportTransform

`public Transform GetViewportTransform()`

Returns `F * C`. Does not include this item's transform or native desktop placement.

### GetScreenTransform

`public Transform GetScreenTransform()`

Returns native client-origin translation times viewport GetScreenTransform times `C * G`. Unlike Viewport.GetScreenTransform, this includes desktop placement. Wayland cannot report the native desktop origin and throws NotSupportedException; it does not invent zero desktop coordinates.

### MakeCanvasPositionLocal

`public Vector2 MakeCanvasPositionLocal(Vector2 viewportPoint)`

Applies the inverse of `C * G` to an already-localized viewport point. Nonfinite input throws ArgumentException; singular transforms throw InvalidOperationException. This is a value operation with no warmed allocation. As with Transform arithmetic, extreme finite values can overflow the floating-point result.

### MakeInputLocal

`public InputEvent MakeInputLocal(InputEvent inputEvent)`

Applies InputEvent.XformedBy with the inverse `C * G`. The live input must already be in viewport coordinates. Positional events return a distinct caller-owned copy; non-positional events return the same borrowed object. Dispose the result only when it differs from the input. Mouse GlobalPosition, screen motion vectors and pan delta stay unchanged. Null throws ArgumentNullException; disposed input throws ObjectDisposedException; transformed nonfinite coordinates throw ArgumentOutOfRangeException before allocating a copy. Singular inverses throw InvalidOperationException. A copy has a different InstanceID and does not retain the original event's by-event action-transition identity.

### GetGlobalMousePosition

`public Vector2 GetGlobalMousePosition()`

Returns inverse `C` times the viewport's current native pointer position. Requires an active native Window; does not use the last synthetic event. A singular canvas transform throws InvalidOperationException.

### GetLocalMousePosition

`public Vector2 GetLocalMousePosition()`

Applies inverse `G` to GetGlobalMousePosition. A singular logical global transform throws InvalidOperationException. The native window requirement and pointer semantics are inherited.

All coordinate queries require a live item and enforce its attached owner thread. Except the detached fallback of GetGlobalTransformWithCanvas, they require active viewport membership; missing membership/off-owner access throws InvalidOperationException and disposal throws ObjectDisposedException. Queries preserve logical node state and do not emit notifications.

Verification: [managed hierarchy, inverse, lifetime and input-copy checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs); [Wayland GPU/compatibility and dummy rendering/input checks](../../tests/Electron2D.Tests/CanvasCoordinateRenderingTests.cs). Native readback includes noncommuting viewport transforms, independent canvas roots, retained commands, pixel snapping and HLSL/GLSL materials. Camera tracking is implemented; CanvasLayer, GUI, nested viewports and content scaling remain absent; no physical-input/visual or other-platform acceptance is claimed.

## Constructors

| Member | Contract |
| --- | --- |
| [`protected CanvasItem()`](#m-electron2d-canvasitem-ctor) | Initializes a detached canvas item with visibility enabled, white modulation and no material. |

## Properties

| Member | Contract |
| --- | --- |
| [`public bool IsVisibleInTree { get; }`](#p-electron2d-canvasitem-isvisibleintree) | Gets whether this node is active and locally visible through its direct canvas ancestor chain. |
| [`public Material Material { get; set; }`](#p-electron2d-canvasitem-material) | Gets or sets the borrowed material for this node's canvas commands. |
| [`public Color Modulate { get; set; }`](#p-electron2d-canvasitem-modulate) | Gets or sets the color multiplier inherited by this node's descendants. |
| [`public bool NotifyLocalTransformChanges { get; set; }`](#p-electron2d-canvasitem-notifylocaltransformchanges) | Gets or sets whether local transform changes dispatch `CanvasItem.NotificationLocalTransformChanged`. |
| [`public bool NotifyTransformChanges { get; set; }`](#p-electron2d-canvasitem-notifytransformchanges) | Gets or sets whether global transform changes dispatch `CanvasItem.NotificationTransformChanged`. |
| [`public Color SelfModulate { get; set; }`](#p-electron2d-canvasitem-selfmodulate) | Gets or sets the color multiplier applied only to this node's drawing. |
| [`public bool ShowBehindParent { get; set; }`](#p-electron2d-canvasitem-showbehindparent) | Draws this canvas subtree before its parent at equal Z, unless the parent sorts it by Y. |
| [`public TextureFilterEnum TextureFilter { get; set; }`](#texturefilter) | Selects inherited, nearest, linear or mip/anisotropic filtering. |
| [`public TextureRepeatEnum TextureRepeat { get; set; }`](#texturerepeat) | Selects inherited, clamp, repeat or mirror addressing. |
| [`public bool TopLevel { get; set; }`](#p-electron2d-canvasitem-toplevel) | Gets or sets whether this node ignores its parent's transform. |
| [`public bool UseParentMaterial { get; set; }`](#p-electron2d-canvasitem-useparentmaterial) | Gets or sets whether this node uses its parent's effective material. |
| [`public bool Visible { get; set; }`](#p-electron2d-canvasitem-visible) | Gets or sets this node's local logical visibility. |
| [`public bool YSortEnabled { get; set; }`](#p-electron2d-canvasitem-ysortenabled) | Sorts this item and its canvas children by local Y at equal Z. |
| [`public bool ZAsRelative { get; set; }`](#p-electron2d-canvasitem-zasrelative) | Gets or sets whether effective Z order accumulates ancestor Z values. |
| [`public int ZIndex { get; set; }`](#p-electron2d-canvasitem-zindex) | Gets or sets this node's local Z-order value. |

## Sampling enums

- [TextureFilterEnum](CanvasItem.TextureFilterEnum.md): ParentNode = 0; Nearest = 1; Linear = 2; NearestWithMipmaps = 3; LinearWithMipmaps = 4; NearestWithMipmapsAnisotropic = 5; LinearWithMipmapsAnisotropic = 6; Max = 7.
- [TextureRepeatEnum](CanvasItem.TextureRepeatEnum.md): ParentNode = 0; Disabled = 1; Enabled = 2; Mirror = 3; Max = 4.

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-canvasitem-dispose-system-boolean) | Disposes the scene hierarchy and clears retained canvas commands and this layer's subscribers in a finally block. Borrowed resources remain caller-owned. |
| [`public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)`](#m-electron2d-canvasitem-drawline-electron2d-vector2-electron2d-vector2-electron2d-color-system-single-system-boolean) | Records a straight line during canvas recording. |
| [`public void DrawRect(Rect rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)`](#m-electron2d-canvasitem-drawrect-electron2d-rect-electron2d-color-system-boolean-system-single-system-boolean) | Records a filled rectangle or a centered rectangular outline during canvas recording. |
| [`public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)`](#m-electron2d-canvasitem-drawsettransform-electron2d-vector2-system-single-system-nullable-electron2d-vector2) | Sets an additional transform for subsequent commands in this canvas recording. |
| [`public void DrawSetTransformMatrix(Transform transform)`](#m-electron2d-canvasitem-drawsettransformmatrix-electron2d-transform) | Sets the full additional transform for subsequent commands in this canvas recording. |
| [`public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)`](#m-electron2d-canvasitem-drawtexture-electron2d-texture-electron2d-vector2-system-nullable-electron2d-color) | Draws a borrowed texture at its logical size during this item's canvas recording. |
| [`public void DrawTextureRect(Texture texture, Rect rect, bool tile, Color? modulate = null, bool transpose = false)`](#m-electron2d-canvasitem-drawtexturerect-electron2d-texture-electron2d-rect-system-boolean-system-nullable-electron2d-color-system-boolean) | Stretches or repeats a borrowed texture over a local rectangle during canvas recording. |
| [`public void DrawTextureRectRegion(Texture texture, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`](#m-electron2d-canvasitem-drawtexturerectregion-electron2d-texture-electron2d-rect-electron2d-rect-system-nullable-electron2d-color-system-boolean-system-boolean) | Stretches a source region of a borrowed texture over a local rectangle during canvas recording. |
| [`public Transform GetGlobalTransform()`](#m-electron2d-canvasitem-getglobaltransform) | Returns the transform composed through the direct canvas-parent chain. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-canvasitem-getpropertydescriptors) | Extends neutral descriptors with visibility, ordering, top-level state, modulation and borrowed materials. |
| [`public abstract Transform GetTransform()`](#m-electron2d-canvasitem-gettransform) | Returns the local transform supplied by this item's placement model. |
| [`public void Hide()`](#m-electron2d-canvasitem-hide) | Sets `CanvasItem.Visible` to `false`. |
| [`public void MoveToFront()`](#m-electron2d-canvasitem-movetofront) | Moves this node to the last position among its siblings. |
| [`protected void NotifyItemRectChanged(bool sizeChanged = true)`](#m-electron2d-canvasitem-notifyitemrectchanged-system-boolean) | Reports a possible local-bounds change, optionally requesting redraw first. |
| [`protected void NotifyLocalTransformChanged()`](#m-electron2d-canvasitem-notifylocaltransformchanged) | Delivers enabled transform notifications after a derived placement model changes. |
| [`protected virtual void OnDraw()`](#m-electron2d-canvasitem-ondraw) | Records this node's retained canvas commands before its first visible frame and after QueueRedraw. |
| [`protected override void OnNotification(int what)`](#m-electron2d-canvasitem-onnotification-system-int32) | Preserves inherited lifecycle dispatch, projects NotificationVisibilityChanged to its typed event and propagates transform changes at parent boundaries. Actual canvas attachment is owned by SceneTree membership, independently of manually dispatched tree notifications. |
| [`public void QueueRedraw()`](#m-electron2d-canvasitem-queueredraw) | Requests regeneration of this node's retained drawing commands before a later visible frame. |
| [`public void Show()`](#m-electron2d-canvasitem-show) | Sets `CanvasItem.Visible` to `true`. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action<CanvasItem>? Draw`](#e-electron2d-canvasitem-draw) | Synchronous recording event between NotificationDraw and OnDraw. |
| [`public event Action<CanvasItem>? Hidden`](#e-electron2d-canvasitem-hidden) | Effective transition to hidden, after visibility delivery. |
| [`public event Action<CanvasItem>? ItemRectChanged`](#e-electron2d-canvasitem-itemrectchanged) | Synchronous local geometry notification; does not propagate to children. |
| [`public event Action<CanvasItem>? LocalTransformChanged`](#e-electron2d-canvasitem-localtransformchanged) | Occurs after this node's local transform actually changes. |
| [`public event Action<CanvasItem>? TransformChanged`](#e-electron2d-canvasitem-transformchanged) | Occurs when this node's global transform is affected by a local or ancestor change. |
| [`public event Action<CanvasItem>? VisibilityChanged`](#e-electron2d-canvasitem-visibilitychanged) | Occurs after local or inherited logical visibility is propagated to this node. |

## Constants

| Member | Contract |
| --- | --- |
| [`public const int MaximumZIndex = 4096`](#f-electron2d-canvasitem-maximumzindex) | Specifies the largest supported local or effective Z index. |
| [`public const int MinimumZIndex = -4096`](#f-electron2d-canvasitem-minimumzindex) | Specifies the smallest supported local or effective Z index. |
| [`public const int NotificationDraw = 30`](#f-electron2d-canvasitem-notificationdraw) | Delivered before Draw and OnDraw while recording commands. |
| [`public const int NotificationEnterCanvas = 32`](#f-electron2d-canvasitem-notificationentercanvas) | Delivered on canvas attachment, parent-first during tree entry. |
| [`public const int NotificationExitCanvas = 33`](#f-electron2d-canvasitem-notificationexitcanvas) | Delivered on canvas detachment, child-first during tree exit; ordinary C# override/base dispatch applies. |
| [`public const int NotificationLocalTransformChanged = 35`](#f-electron2d-canvasitem-notificationlocaltransformchanged) | Identifies a local-transform change notification when local notification delivery is enabled. |
| [`public const int NotificationTransformChanged = 2000`](#f-electron2d-canvasitem-notificationtransformchanged) | Identifies a global-transform change notification when global notification delivery is enabled. |
| [`public const int NotificationVisibilityChanged = 31`](#f-electron2d-canvasitem-notificationvisibilitychanged) | Identifies the notification propagated after local or inherited visibility changes. |

## Constructor Descriptions

<a id="m-electron2d-canvasitem-ctor"></a>
### `protected CanvasItem()`

Initializes a detached canvas item with visibility enabled, white modulation and no material.

## Property Descriptions

<a id="p-electron2d-canvasitem-isvisibleintree"></a>
### `public bool IsVisibleInTree { get; }`

Gets whether this node is active and locally visible through its direct canvas ancestor chain.

**Value:** `true` only inside a tree when this node, its direct canvas ancestors and its window are visible.

**System.ObjectDisposedException:** This node or a queried ancestor is disposing on another thread, or has finished disposing.

<a id="p-electron2d-canvasitem-material"></a>
### `public Material Material { get; set; }`

Gets or sets the borrowed material for this node's canvas commands.

**Value:** Null uses ordinary source-alpha color drawing.

**Remarks:** Disposing the node does not dispose this shared resource. An assigned shader requires GPU rendering.

**System.InvalidOperationException:** An attached node is mutated off its owner thread or during scene capture.

**System.ObjectDisposedException:** The node or assigned material is disposed.

<a id="p-electron2d-canvasitem-modulate"></a>
### `public Color Modulate { get; set; }`

Gets or sets the color multiplier inherited by this node's descendants.

**Value:** Opaque white by default.

**System.ArgumentException:** The color is not finite.

**System.InvalidOperationException:** An attached node is mutated off its owner thread or during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-canvasitem-notifylocaltransformchanges"></a>
### `public bool NotifyLocalTransformChanges { get; set; }`

Gets or sets whether local transform changes dispatch `CanvasItem.NotificationLocalTransformChanged`.

**Value:** `false` by default. `CanvasItem.LocalTransformChanged` is raised regardless.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-canvasitem-notifytransformchanges"></a>
### `public bool NotifyTransformChanges { get; set; }`

Gets or sets whether global transform changes dispatch `CanvasItem.NotificationTransformChanged`.

**Value:** `false` by default. `CanvasItem.TransformChanged` is raised regardless.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-canvasitem-selfmodulate"></a>
### `public Color SelfModulate { get; set; }`

Gets or sets the color multiplier applied only to this node's drawing.

**Value:** Opaque white by default.

**System.ArgumentException:** The color is not finite.

**System.InvalidOperationException:** An attached node is mutated off its owner thread or during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-canvasitem-showbehindparent"></a>
### `public bool ShowBehindParent { get; set; }`

False by default. True draws this canvas subtree before its canvas parent when effective Z is equal. Effective Z takes precedence. An enabled Y-sorting parent orders participating children by Y instead of this flag. Neutral parents and TopLevel items have no canvas parent to draw behind.

Changes affect the next submission without QueueRedraw. The value is stored by PackedScene. Mutation off an attached tree's owner thread or during capture throws InvalidOperationException; access after disposal throws ObjectDisposedException.

<a id="p-electron2d-canvasitem-toplevel"></a>
### `public bool TopLevel { get; set; }`

Gets or sets whether this node ignores its parent's transform.

**Value:** `false` by default.

**Remarks:** The local transform stays unchanged; global coordinates are recomputed against the new canvas boundary. The item becomes a separate canvas root, drawn after the preceding root's entire canvas subtree at the same Z. Roots retain scene order; Z takes precedence. Logical visibility continues to follow direct canvas ancestors. An attached change emits NotificationExitCanvas with the old mode, then NotificationEnterCanvas with the committed new mode. It schedules redraw; recursive rebinding is rejected and callback failures are aggregated after the transition.

**System.InvalidOperationException:** Mutation occurs off the owner thread, during packed-scene capture, or recursively during canvas rebinding.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.AggregateException:** A canvas or transform callback fails; the mode transition still completes.

<a id="p-electron2d-canvasitem-useparentmaterial"></a>
### `public bool UseParentMaterial { get; set; }`

Gets or sets whether this node uses its parent's effective material.

**Value:** False by default. A root using its parent material uses ordinary color drawing.

**System.InvalidOperationException:** An attached node is mutated off its owner thread or during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-canvasitem-visible"></a>
### `public bool Visible { get; set; }`

Gets or sets this node's local logical visibility.

**Value:** `true` by default.

**Remarks:** A local change notifies this item. An effective tree-visibility change propagates through locally visible direct canvas children, including TopLevel items. Showing schedules redraw; hiding raises Hidden after visibility delivery.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A visibility notification or event handler throws after visibility changes.

<a id="p-electron2d-canvasitem-ysortenabled"></a>
### `public bool YSortEnabled { get; set; }`

False by default. True orders the item itself at Y = 0 and its direct canvas children in ascending Y in this item's local coordinate system. The root's global rotation or scale does not change the sorting coordinates. Nested enabled children join the same group using composed local transforms. A child with sorting disabled keeps its canvas subtree together at that child's Y; any deeper enabled group sorts independently within that subtree. Approximate ties use Mathf.IsEqualApprox and preserve scene order. Effective Z takes precedence over Y.

Invisible children do not participate. TopLevel children and children below neutral nodes are separate canvas roots. ShowBehindParent is ignored for items directly ordered by the Y group, but still applies inside unsorted subtrees. Processing and input order remain unchanged.

Changes affect the next submission without QueueRedraw. PackedScene stores the value. Mutation off an attached tree's owner thread or during capture throws InvalidOperationException; access after disposal throws ObjectDisposedException.

<a id="p-electron2d-canvasitem-zasrelative"></a>
### `public bool ZAsRelative { get; set; }`

Gets or sets whether effective Z order accumulates ancestor Z values.

**Value:** `true` by default.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-canvasitem-zindex"></a>
### `public int ZIndex { get; set; }`

Gets or sets this node's local Z-order value.

**Value:** An integer from `CanvasItem.MinimumZIndex` through `CanvasItem.MaximumZIndex`; the default is zero.

Every valid assignment commits the Z value and then requests configuration-warning refresh, including unchanged values. Invalid assignments do not emit. Subscriber failures propagate after the value is committed. SceneDiagnosticsTests verifies the selected-scene event.

**System.ArgumentOutOfRangeException:** The assigned value is outside the supported range.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

### TextureFilter

`public TextureFilterEnum TextureFilter { get; set; }`

ParentNode by default. Actual changes refresh attached inheriting descendants, request redraw, then raise PropertyListChanged on this item. Overrides and neutral parents stop propagation. Detached changes apply at entry; TopLevel and reparenting recompute inheritance. GPU supports all concrete modes; mipmaps/anisotropy fail on compatibility, and linear fails on software triangles. Base-level modes never sample stored lower mips. Anisotropy uses the viewport limit; mip interpolation uses the project startup setting. Missing image mips are not generated implicitly.

Invalid or Max values throw ArgumentOutOfRangeException before mutation. InvalidOperationException rejects off-owner/capture-time mutation; ObjectDisposedException rejects disposed access. A subscriber exception leaves the new value and pending redraw committed. Equal assignments do nothing. Stored by PackedScene.

### TextureRepeat

`public TextureRepeatEnum TextureRepeat { get; set; }`

ParentNode by default. Disabled clamps to edges, Enabled repeats, Mirror reflects alternate tiles. GPU supports all; compatibility rejects Mirror and checks native non-power-of-two wrapping. A tiled rectangle command forces Enabled. Inheritance, redraw, PropertyListChanged, no-ops, storage and errors match TextureFilter. This setting does not change the texture resource or arbitrary material parameter samplers.

## Method Descriptions

<a id="m-electron2d-canvasitem-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Disposes the scene hierarchy and clears retained canvas commands and this layer's subscribers in a finally block. Borrowed resources remain caller-owned.

<a id="m-electron2d-canvasitem-drawline-electron2d-vector2-electron2d-vector2-electron2d-color-system-single-system-boolean"></a>
### `public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)`

Records a straight line during canvas recording.

**Parameter `from`:** The finite starting point in local coordinates.

**Parameter `to`:** The finite ending point in local coordinates.

**Parameter `color`:** The finite drawing color.

**Parameter `width`:** Width in local units; a negative value uses one framebuffer pixel.

**Parameter `antialiased`:** Whether to feather the boundary over one framebuffer pixel.

**Remarks:** Lines use flat caps. Coincident endpoints or zero width draw nothing.

**System.ArgumentException:** Geometry, color or width is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawrect-electron2d-rect-electron2d-color-system-boolean-system-single-system-boolean"></a>
### `public void DrawRect(Rect rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)`

Records a filled rectangle or a centered rectangular outline during canvas recording.

**Parameter `rect`:** A finite local rectangle; negative dimensions are normalized.

**Parameter `color`:** The finite drawing color.

**Parameter `filled`:** Whether to fill the rectangle; true by default.

**Parameter `width`:** Outline width in local units; a negative value uses one framebuffer pixel.

**Parameter `antialiased`:** Whether to feather the boundary over one framebuffer pixel.

**Remarks:** Zero-area rectangles and zero-width outlines draw nothing. Outline widths larger than the rectangle collapse its hole. Drawing obeys this node's transform, visibility, Z order and modulation.

**System.ArgumentException:** Geometry, color or width is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawsettransform-electron2d-vector2-system-single-system-nullable-electron2d-vector2"></a>
### `public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)`

Sets an additional transform for subsequent commands in this canvas recording.

**Parameter `position`:** Translation in local units.

**Parameter `rotation`:** Rotation in radians, zero by default.

**Parameter `scale`:** Scale, or null for one on both axes.

**System.ArgumentException:** The transform is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawsettransformmatrix-electron2d-transform"></a>
### `public void DrawSetTransformMatrix(Transform transform)`

Sets the full additional transform for subsequent commands in this canvas recording.

**Parameter `transform`:** The finite local drawing transform.

**System.ArgumentException:** The transform is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawtexture-electron2d-texture-electron2d-vector2-system-nullable-electron2d-color"></a>
### `public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)`

Draws a borrowed texture at its logical size during this item's canvas recording.

**Parameter `texture`:** The live texture; its virtual Draw implementation supplies the command.

**Parameter `position`:** The finite local top-left position.

**Parameter `modulate`:** The finite color multiplier, or null for white.

**System.ArgumentNullException:** The texture is null.

**System.ArgumentException:** Position or modulation is not finite.

**System.InvalidOperationException:** Called outside the recording scope or off the owner thread.

**System.ObjectDisposedException:** The node or texture is disposed.

<a id="m-electron2d-canvasitem-drawtexturerect-electron2d-texture-electron2d-rect-system-boolean-system-nullable-electron2d-color-system-boolean"></a>
### `public void DrawTextureRect(Texture texture, Rect rect, bool tile, Color? modulate = null, bool transpose = false)`

Stretches or repeats a borrowed texture over a local rectangle during canvas recording.

**Parameter `texture`:** The live texture; its virtual DrawRect implementation supplies the command.

**Parameter `rect`:** The finite destination. Negative dimensions flip without moving its origin.

**Parameter `tile`:** Whether to repeat at the texture's logical pixel size.

**Parameter `modulate`:** The finite color multiplier, or null for white.

**Parameter `transpose`:** Whether to exchange texture axes and destination dimensions.

**System.ArgumentNullException:** The texture is null.

**System.ArgumentException:** Geometry or modulation is not finite.

**System.InvalidOperationException:** Called outside the recording scope or off the owner thread.

**System.ObjectDisposedException:** The node or texture is disposed.

<a id="m-electron2d-canvasitem-drawtexturerectregion-electron2d-texture-electron2d-rect-electron2d-rect-system-nullable-electron2d-color-system-boolean-system-boolean"></a>
### `public void DrawTextureRectRegion(Texture texture, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`

Stretches a source region of a borrowed texture over a local rectangle during canvas recording.

**Parameter `texture`:** The live texture; its virtual DrawRectRegion implementation supplies the command.

**Parameter `rect`:** The finite destination. Negative dimensions flip without moving its origin.

**Parameter `sourceRect`:** The finite region in logical texture pixels; negative dimensions toggle flipping.

**Parameter `modulate`:** The finite color multiplier, or null for white.

**Parameter `transpose`:** Whether to exchange texture axes and destination dimensions.

**Parameter `clipUV`:** Whether to constrain sampling to texel centers inside the source region.

**System.ArgumentNullException:** The texture is null.

**System.ArgumentException:** Geometry or modulation is not finite.

**System.InvalidOperationException:** Called outside the recording scope or off the owner thread.

**System.ObjectDisposedException:** The node or texture is disposed.

<a id="m-electron2d-canvasitem-getglobaltransform"></a>
### `public Transform GetGlobalTransform()`

Returns the transform composed through the direct canvas-parent chain.

**Returns:** The local transform when the parent is non-canvas or TopLevel is enabled.

**System.ObjectDisposedException:** The item is disposed.

<a id="m-electron2d-canvasitem-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends neutral descriptors with visibility, ordering, top-level state, modulation and borrowed materials.

<a id="m-electron2d-canvasitem-gettransform"></a>
### `public abstract Transform GetTransform()`

Returns the local transform supplied by this item's placement model.

**Returns:** The transform relative to the direct canvas parent.

**System.ObjectDisposedException:** The item is disposed.

<a id="m-electron2d-canvasitem-hide"></a>
### `public void Hide()`

Sets `CanvasItem.Visible` to `false`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A visibility notification or event handler throws after visibility changes.

<a id="m-electron2d-canvasitem-movetofront"></a>
### `public void MoveToFront()`

Moves this node to the last position among its siblings.

**Remarks:** A detached or hierarchy-root node is left unchanged.

**System.InvalidOperationException:** An attached parent is mutated off the owner thread.

**System.ObjectDisposedException:** This node or its parent is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more child-order or tree-change callbacks fail after the order changes.

<a id="m-electron2d-canvasitem-notifyitemrectchanged-system-boolean"></a>
### `protected void NotifyItemRectChanged(bool sizeChanged = true)`

Reports that an operation may affect local drawing bounds. Derived types commit their geometry before calling it; identical numeric bounds need not suppress an operation's event. When `sizeChanged` is true, calls QueueRedraw before delivery, with its detached, hidden and recording/coalescing rules. False delivers the event without requesting redraw.

Delivery is synchronous on the caller's thread, restricted to the scene owner while attached. Hidden, detached and processing-disabled items still deliver. The event is local and does not propagate to children. InvalidOperationException rejects off-owner or capture-time calls before delivery; ObjectDisposedException rejects disposed items. Subscriber exceptions propagate immediately, stopping later subscribers while retaining committed state and pending redraw.

<a id="m-electron2d-canvasitem-notifylocaltransformchanged"></a>
### `protected void NotifyLocalTransformChanged()`

Delivers enabled transform notifications after a derived placement model changes.

**Remarks:** The local transform must be committed first. Descendant delivery stops at neutral nodes and top-level canvas items. All affected items are attempted before callback failures are aggregated.

**System.InvalidOperationException:** The caller is not the scene owner or a capture is active.

**System.ObjectDisposedException:** The item is disposed.

**System.AggregateException:** A notification or event handler fails.

<a id="m-electron2d-canvasitem-ondraw"></a>
### `protected virtual void OnDraw()`

Records this node's retained canvas commands before its first visible frame and after QueueRedraw.

**Remarks:** Runs on the scene owner thread during rendering. The recording sequence is NotificationDraw, synchronous Draw handlers, then OnDraw. Geometry, texture and drawing-transform calls are valid in all three stages for this item. Commands and draw transform are reset before the notification. Failure aborts subsequent stages, clears partial commands and restores the dirty flag; the recording scope always closes.

<a id="m-electron2d-canvasitem-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Preserves inherited lifecycle dispatch, projects NotificationVisibilityChanged to its typed event and propagates transform changes at parent boundaries. Actual canvas attachment is owned by SceneTree membership, independently of manually dispatched tree notifications.

<a id="m-electron2d-canvasitem-queueredraw"></a>
### `public void QueueRedraw()`

Requests regeneration of this node's retained drawing commands before a later visible frame.

**Remarks:** Detached calls do nothing. Requests coalesce. Hidden nodes retain the request until visible. A request during this item's active recording coalesces with that recording and does not schedule another redraw. Internal resource invalidations arriving during recording remain pending for the next frame. Transforms, modulation and material changes need no redraw.

**System.InvalidOperationException:** An attached node is mutated off its owner thread or during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-show"></a>
### `public void Show()`

Sets `CanvasItem.Visible` to `true`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A visibility notification or event handler throws after visibility changes.

## Event Descriptions

<a id="e-electron2d-canvasitem-draw"></a>
### `public event Action<CanvasItem>? Draw`

Synchronous owner-thread event after NotificationDraw and before OnDraw. The argument is the recording item. Handlers may issue its drawing commands; deferred handlers run outside that scope. Failure aborts recording, clears partial commands and closes the scope before propagating through the host's cleanup.

<a id="e-electron2d-canvasitem-hidden"></a>
### `public event Action<CanvasItem>? Hidden`

Synchronous event after visibility notification/event delivery when this item becomes hidden in its tree. The argument is the affected item. Direct visible descendants participate, including TopLevel items; locally hidden branches stop propagation. Local changes below a hidden parent and tree exit do not emit Hidden. Callback failures are reported after remaining affected children are attempted.

<a id="e-electron2d-canvasitem-itemrectchanged"></a>
### `public event Action<CanvasItem>? ItemRectChanged`

Reports a possible local drawing-bounds change, with this item as sender. Derived geometry operations call NotifyItemRectChanged; Entity transforms do not emit it. The event is independent of visibility and processing. The owning node clears subscriptions on disposal. See [Sprite](Sprite.md#itemrectchanged) for event order, no-ops and resource notification behavior.

<a id="e-electron2d-canvasitem-localtransformchanged"></a>
### `public event Action<CanvasItem>? LocalTransformChanged`

Occurs after this node's local transform actually changes.

**Remarks:** The event is always enabled; numeric local-transform notification delivery is separately configurable.

<a id="e-electron2d-canvasitem-transformchanged"></a>
### `public event Action<CanvasItem>? TransformChanged`

Occurs when this node's global transform is affected by a local or ancestor change.

**Remarks:** Propagation stops at top-level descendants. The event is independent of numeric transform notifications.

<a id="e-electron2d-canvasitem-visibilitychanged"></a>
### `public event Action<CanvasItem>? VisibilityChanged`

Occurs after local or inherited logical visibility is propagated to this node.

**Remarks:** The base handler for `CanvasItem.NotificationVisibilityChanged` raises this event, including manual notifications. Actual effective visibility changes propagate through locally visible direct canvas descendants, and visible tree entry also notifies this item. Hiding by tree exit does not raise this event.

## Constant Descriptions

<a id="f-electron2d-canvasitem-maximumzindex"></a>
### `public const int MaximumZIndex = 4096`

Specifies the largest supported local or effective Z index.

<a id="f-electron2d-canvasitem-minimumzindex"></a>
### `public const int MinimumZIndex = -4096`

Specifies the smallest supported local or effective Z index.

<a id="f-electron2d-canvasitem-notificationlocaltransformchanged"></a>
### `public const int NotificationLocalTransformChanged = 35`

Identifies a local-transform change notification when local notification delivery is enabled.

<a id="f-electron2d-canvasitem-notificationtransformchanged"></a>
### `public const int NotificationTransformChanged = 2000`

Identifies a global-transform change notification when global notification delivery is enabled.

<a id="f-electron2d-canvasitem-notificationvisibilitychanged"></a>
### `public const int NotificationVisibilityChanged = 31`

Identifies the notification propagated after local or inherited visibility changes.

<a id="f-electron2d-canvasitem-notificationdraw"></a>
### `public const int NotificationDraw = 30`

Delivered before Draw and OnDraw while recording commands.

<a id="f-electron2d-canvasitem-notificationentercanvas"></a>
### `public const int NotificationEnterCanvas = 32`

Delivered on canvas attachment, parent-first during tree entry.

<a id="f-electron2d-canvasitem-notificationexitcanvas"></a>
### `public const int NotificationExitCanvas = 33`

Delivered on canvas detachment, child-first during tree exit; ordinary C# override/base dispatch applies.

## Ownership, errors and dependencies

The parent owns its children; SceneTree owns the active root. PackedScene capture uses explicit stored descriptors and static exact-type factories. Scene-local resources belong to the instantiated root; externally supplied textures/materials are borrowed. Mutations honor scene capture, lifetime and owner-thread guards. Callback failures are reported after the documented committed state; cleanup attempts every owned stage. See [Node](Node.md) for inherited lifecycle and [the scene hierarchy component](../components/scene-hierarchy.md) for cross-layer flow.

Drawing commands retain borrowed resources and are valid during NotificationDraw, synchronous Draw handlers and OnDraw. Deferred handlers execute outside the recording scope and cannot draw. QueueRedraw coalesces requests; resource-thread notifications only set an atomic flag, and recording runs on the owner thread. Global transform notifications stop at neutral and TopLevel children. Failed transform/visibility delivery does not skip later direct canvas siblings.

## Verification and limits

[SpriteTests](../../tests/Electron2D.Tests/SpriteTests.cs) checks rectangle-event triggers/order, committed bounds, no-ops, invalid changes, hidden/detached/disabled delivery, local-only propagation, redraw options, worker guards, reentry, callback failures and disposal.

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) verifies inheritance, neutral API boundaries, direct custom CanvasItem transforms, mixed parenting, notifications, timer/tween scheduling, packed factories/state, deletion and failure continuation. Existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) retain lifecycle, input, math and ownership coverage. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies mixed-tree pixels and a direct CanvasItem drawing texture through both GPU and compatibility backends on Linux Wayland. This does not establish visual owner acceptance or other platforms.

[CanvasOrderingTests](../../tests/Electron2D.Tests/CanvasOrderingTests.cs) verifies 31 framebuffer cases for behind-parent subtrees, effective Z, canvas-root order, local/nested Y groups, visibility and mutations from drawing callbacks on Wayland GPU/compatibility and dummy/software. Managed checks cover defaults, owner-thread guards and packed ordering flags; warmed rendering with nested Y groups allocates zero managed bytes in the measured interval.

[CanvasLifecycleTests](../../tests/Electron2D.Tests/CanvasLifecycleTests.cs) verifies lifecycle/visibility/recording order, manual notifications, reattachment, callback failures and recording recovery. Six-frame pixel sequences on Linux Wayland GPU/compatibility and dummy/software verify all three drawing stages plus redraw on showing, reattachment and TopLevel rebinding. The complete renderer allocation check still passes.

The hierarchy is implemented; complete reference API parity is not claimed. Missing GUI, canvas policies, rendering primitives, interpolation, scene-file authoring and other capabilities remain classified per member in [coverage](../coverage/index.md). No inert compatibility members are added.

## Relevant decisions

- [0008: Node, CanvasItem and Entity](../decisions/scene.md#adr-0008)
- [0004: Product scope and API correspondence](../decisions/product.md#adr-0004)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)

## Viewport pixel snapping

[Viewport](Viewport.md#pixel-snapping-properties) controls render-only transform and vertex rounding. GetTransform/GetGlobalTransform remain logical queries. Transform snapping participates in Y sorting; vertex snapping applies after DrawSetTransform and framebuffer scaling. Retained recording is not invalidated merely by changing these flags. See [the canvas contract](../components/canvas-rendering.md#pixel-snapping) for Sprite offsets and texture clipping.

Transform propagation uses pooled child snapshots, clears retained references on return, and revalidates each child after callback mutations. [PathTests](../../tests/Electron2D.Tests/PathTests.cs) verifies zero allocation for warmed follower movement with a child. Pool growth, exceptions and user callbacks can still allocate.
