# CanvasItem

Last updated: 2026-09-23

**Inherits:** [SceneNode](SceneNode.md)

**Inherited By:** [Node](Node.md)

- **Source:** [CanvasItem.cs](../../src/Scene/Main/CanvasItem.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract partial class CanvasItem : SceneNode`

## Description

The abstract canvas base. Owns visibility, Z order, modulation, materials, retained drawing and transform queries/notifications. Node supplies a concrete spatial placement model. A direct CanvasItem subclass can provide its own model through GetTransform and notify changes with NotifyLocalTransformChanged. The Control/UI branch is not yet implemented. Only direct canvas parents contribute transforms, modulation and materials; a neutral SceneNode breaks those chains. TopLevel preserves the local transform while ending transform/material/modulation/Z inheritance. Visibility follows direct canvas parents, including TopLevel items, and the containing window.

## Examples

The snippet uses the Electron2D namespace; attach the hierarchy to a SceneTree or an Engine.Run window to activate it.

```csharp
class PaintedNode : Node
{
    protected override void OnDraw() => DrawRect(new Rect(0, 0, 32, 16), Colors.Cyan);
}
```

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
| [`public bool TopLevel { get; set; }`](#p-electron2d-canvasitem-toplevel) | Gets or sets whether this node ignores its parent's transform. |
| [`public bool UseParentMaterial { get; set; }`](#p-electron2d-canvasitem-useparentmaterial) | Gets or sets whether this node uses its parent's effective material. |
| [`public bool Visible { get; set; }`](#p-electron2d-canvasitem-visible) | Gets or sets this node's local logical visibility. |
| [`public bool ZAsRelative { get; set; }`](#p-electron2d-canvasitem-zasrelative) | Gets or sets whether effective Z order accumulates ancestor Z values. |
| [`public int ZIndex { get; set; }`](#p-electron2d-canvasitem-zindex) | Gets or sets this node's local Z-order value. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-canvasitem-dispose-system-boolean) | Disposes the scene hierarchy and clears retained canvas commands and this layer's subscribers in a finally block. Borrowed resources remain caller-owned. |
| [`public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)`](#m-electron2d-canvasitem-drawline-electron2d-vector2-electron2d-vector2-electron2d-color-system-single-system-boolean) | Records a straight line during OnDraw. |
| [`public void DrawRect(Rect rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)`](#m-electron2d-canvasitem-drawrect-electron2d-rect-electron2d-color-system-boolean-system-single-system-boolean) | Records a filled rectangle or a centered rectangular outline during OnDraw. |
| [`public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)`](#m-electron2d-canvasitem-drawsettransform-electron2d-vector2-system-single-system-nullable-electron2d-vector2) | Sets an additional transform for subsequent commands in this OnDraw callback. |
| [`public void DrawSetTransformMatrix(Transform transform)`](#m-electron2d-canvasitem-drawsettransformmatrix-electron2d-transform) | Sets the full additional transform for subsequent commands in this OnDraw callback. |
| [`public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)`](#m-electron2d-canvasitem-drawtexture-electron2d-texture-electron2d-vector2-system-nullable-electron2d-color) | Draws a borrowed texture at its logical size during this node's OnDraw callback. |
| [`public void DrawTextureRect(Texture texture, Rect rect, bool tile, Color? modulate = null, bool transpose = false)`](#m-electron2d-canvasitem-drawtexturerect-electron2d-texture-electron2d-rect-system-boolean-system-nullable-electron2d-color-system-boolean) | Stretches or repeats a borrowed texture over a local rectangle during OnDraw. |
| [`public void DrawTextureRectRegion(Texture texture, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`](#m-electron2d-canvasitem-drawtexturerectregion-electron2d-texture-electron2d-rect-electron2d-rect-system-nullable-electron2d-color-system-boolean-system-boolean) | Stretches a source region of a borrowed texture over a local rectangle during OnDraw. |
| [`public Transform GetGlobalTransform()`](#m-electron2d-canvasitem-getglobaltransform) | Returns the transform composed through the direct canvas-parent chain. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-canvasitem-getpropertydescriptors) | Extends neutral descriptors with visibility, ordering, top-level state, modulation and borrowed materials. |
| [`public abstract Transform GetTransform()`](#m-electron2d-canvasitem-gettransform) | Returns the local transform supplied by this item's placement model. |
| [`public void Hide()`](#m-electron2d-canvasitem-hide) | Sets `CanvasItem.Visible` to `false`. |
| [`public void MoveToFront()`](#m-electron2d-canvasitem-movetofront) | Moves this node to the last position among its siblings. |
| [`protected void NotifyLocalTransformChanged()`](#m-electron2d-canvasitem-notifylocaltransformchanged) | Delivers enabled transform notifications after a derived placement model changes. |
| [`protected virtual void OnDraw()`](#m-electron2d-canvasitem-ondraw) | Records this node's retained canvas commands before its first visible frame and after QueueRedraw. |
| [`protected override void OnNotification(int what)`](#m-electron2d-canvasitem-onnotification-system-int32) | Preserves inherited lifecycle dispatch and propagates transform/visibility changes at parent boundaries, collecting callback failures. |
| [`public void QueueRedraw()`](#m-electron2d-canvasitem-queueredraw) | Requests regeneration of this node's retained drawing commands before a later visible frame. |
| [`public void Show()`](#m-electron2d-canvasitem-show) | Sets `CanvasItem.Visible` to `true`. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action<CanvasItem>? LocalTransformChanged`](#e-electron2d-canvasitem-localtransformchanged) | Occurs after this node's local transform actually changes. |
| [`public event Action<CanvasItem>? TransformChanged`](#e-electron2d-canvasitem-transformchanged) | Occurs when this node's global transform is affected by a local or ancestor change. |
| [`public event Action<CanvasItem>? VisibilityChanged`](#e-electron2d-canvasitem-visibilitychanged) | Occurs after local or inherited logical visibility is propagated to this node. |

## Constants

| Member | Contract |
| --- | --- |
| [`public const int MaximumZIndex = 4096`](#f-electron2d-canvasitem-maximumzindex) | Specifies the largest supported local or effective Z index. |
| [`public const int MinimumZIndex = -4096`](#f-electron2d-canvasitem-minimumzindex) | Specifies the smallest supported local or effective Z index. |
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

<a id="p-electron2d-canvasitem-toplevel"></a>
### `public bool TopLevel { get; set; }`

Gets or sets whether this node ignores its parent's transform.

**Value:** `false` by default.

**Remarks:** The local transform stays unchanged; global coordinates are recomputed against the new canvas boundary.

**System.InvalidOperationException:** Mutation occurs off the owner thread or during packed-scene capture.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the mode changes.

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

**Remarks:** An actual change synchronously propagates visibility notifications and events through direct canvas descendants.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A visibility notification or event handler throws after visibility changes.

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

**System.ArgumentOutOfRangeException:** The assigned value is outside the supported range.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

## Method Descriptions

<a id="m-electron2d-canvasitem-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Disposes the scene hierarchy and clears retained canvas commands and this layer's subscribers in a finally block. Borrowed resources remain caller-owned.

<a id="m-electron2d-canvasitem-drawline-electron2d-vector2-electron2d-vector2-electron2d-color-system-single-system-boolean"></a>
### `public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)`

Records a straight line during OnDraw.

**Parameter `from`:** The finite starting point in local coordinates.

**Parameter `to`:** The finite ending point in local coordinates.

**Parameter `color`:** The finite drawing color.

**Parameter `width`:** Width in local units; a negative value uses one framebuffer pixel.

**Parameter `antialiased`:** Whether to feather the boundary over one framebuffer pixel.

**Remarks:** Lines use flat caps. Coincident endpoints or zero width draw nothing.

**System.ArgumentException:** Geometry, color or width is not finite.

**System.InvalidOperationException:** Called outside this node's OnDraw callback or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawrect-electron2d-rect-electron2d-color-system-boolean-system-single-system-boolean"></a>
### `public void DrawRect(Rect rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)`

Records a filled rectangle or a centered rectangular outline during OnDraw.

**Parameter `rect`:** A finite local rectangle; negative dimensions are normalized.

**Parameter `color`:** The finite drawing color.

**Parameter `filled`:** Whether to fill the rectangle; true by default.

**Parameter `width`:** Outline width in local units; a negative value uses one framebuffer pixel.

**Parameter `antialiased`:** Whether to feather the boundary over one framebuffer pixel.

**Remarks:** Zero-area rectangles and zero-width outlines draw nothing. Outline widths larger than the rectangle collapse its hole. Drawing obeys this node's transform, visibility, Z order and modulation.

**System.ArgumentException:** Geometry, color or width is not finite.

**System.InvalidOperationException:** Called outside this node's OnDraw callback or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawsettransform-electron2d-vector2-system-single-system-nullable-electron2d-vector2"></a>
### `public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)`

Sets an additional transform for subsequent commands in this OnDraw callback.

**Parameter `position`:** Translation in local units.

**Parameter `rotation`:** Rotation in radians, zero by default.

**Parameter `scale`:** Scale, or null for one on both axes.

**System.ArgumentException:** The transform is not finite.

**System.InvalidOperationException:** Called outside this node's OnDraw callback or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawsettransformmatrix-electron2d-transform"></a>
### `public void DrawSetTransformMatrix(Transform transform)`

Sets the full additional transform for subsequent commands in this OnDraw callback.

**Parameter `transform`:** The finite local drawing transform.

**System.ArgumentException:** The transform is not finite.

**System.InvalidOperationException:** Called outside this node's OnDraw callback or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawtexture-electron2d-texture-electron2d-vector2-system-nullable-electron2d-color"></a>
### `public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)`

Draws a borrowed texture at its logical size during this node's OnDraw callback.

**Parameter `texture`:** The live texture; its virtual Draw implementation supplies the command.

**Parameter `position`:** The finite local top-left position.

**Parameter `modulate`:** The finite color multiplier, or null for white.

**System.ArgumentNullException:** The texture is null.

**System.ArgumentException:** Position or modulation is not finite.

**System.InvalidOperationException:** Called outside OnDraw or off the owner thread.

**System.ObjectDisposedException:** The node or texture is disposed.

<a id="m-electron2d-canvasitem-drawtexturerect-electron2d-texture-electron2d-rect-system-boolean-system-nullable-electron2d-color-system-boolean"></a>
### `public void DrawTextureRect(Texture texture, Rect rect, bool tile, Color? modulate = null, bool transpose = false)`

Stretches or repeats a borrowed texture over a local rectangle during OnDraw.

**Parameter `texture`:** The live texture; its virtual DrawRect implementation supplies the command.

**Parameter `rect`:** The finite destination. Negative dimensions flip without moving its origin.

**Parameter `tile`:** Whether to repeat at the texture's logical pixel size.

**Parameter `modulate`:** The finite color multiplier, or null for white.

**Parameter `transpose`:** Whether to exchange texture axes and destination dimensions.

**System.ArgumentNullException:** The texture is null.

**System.ArgumentException:** Geometry or modulation is not finite.

**System.InvalidOperationException:** Called outside OnDraw or off the owner thread.

**System.ObjectDisposedException:** The node or texture is disposed.

<a id="m-electron2d-canvasitem-drawtexturerectregion-electron2d-texture-electron2d-rect-electron2d-rect-system-nullable-electron2d-color-system-boolean-system-boolean"></a>
### `public void DrawTextureRectRegion(Texture texture, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`

Stretches a source region of a borrowed texture over a local rectangle during OnDraw.

**Parameter `texture`:** The live texture; its virtual DrawRectRegion implementation supplies the command.

**Parameter `rect`:** The finite destination. Negative dimensions flip without moving its origin.

**Parameter `sourceRect`:** The finite region in logical texture pixels; negative dimensions toggle flipping.

**Parameter `modulate`:** The finite color multiplier, or null for white.

**Parameter `transpose`:** Whether to exchange texture axes and destination dimensions.

**Parameter `clipUV`:** Whether to constrain sampling to texel centers inside the source region.

**System.ArgumentNullException:** The texture is null.

**System.ArgumentException:** Geometry or modulation is not finite.

**System.InvalidOperationException:** Called outside OnDraw or off the owner thread.

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

**Remarks:** Runs on the scene owner thread during rendering. Geometry, texture and drawing-transform calls are valid only here. The command list is cleared and the drawing transform reset to identity before entry.

<a id="m-electron2d-canvasitem-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Preserves inherited lifecycle dispatch and propagates transform/visibility changes at parent boundaries, collecting callback failures.

<a id="m-electron2d-canvasitem-queueredraw"></a>
### `public void QueueRedraw()`

Requests regeneration of this node's retained drawing commands before a later visible frame.

**Remarks:** Requests coalesce. Hidden nodes retain the request until visible. A request inside OnDraw schedules the following frame; it does not re-enter drawing. Transforms, modulation and material changes need no redraw.

**System.InvalidOperationException:** An attached node is mutated off its owner thread or during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-show"></a>
### `public void Show()`

Sets `CanvasItem.Visible` to `true`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A visibility notification or event handler throws after visibility changes.

## Event Descriptions

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

**Remarks:** Delivery follows `CanvasItem.NotificationVisibilityChanged` and continues through descendants.

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

## Ownership, errors and dependencies

The parent owns its children; SceneTree owns the active root. PackedScene capture uses explicit stored descriptors and static exact-type factories. Scene-local resources belong to the instantiated root; externally supplied textures/materials are borrowed. Mutations honor scene capture, lifetime and owner-thread guards. Callback failures are reported after the documented committed state; cleanup attempts every owned stage. See [SceneNode](SceneNode.md) for inherited lifecycle and [the scene hierarchy component](../components/scene-hierarchy.md) for cross-layer flow.

Drawing commands are valid only during OnDraw and retain borrowed resources. QueueRedraw coalesces requests; resource-thread notifications only set an atomic flag, and recording runs on the owner thread. Global transform notifications stop at neutral and TopLevel children. Failed transform/visibility delivery does not skip later direct canvas siblings.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) verifies inheritance, neutral API boundaries, direct custom CanvasItem transforms, mixed parenting, notifications, timer/tween scheduling, packed factories/state, deletion and failure continuation. Existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) retain lifecycle, input, math and ownership coverage. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies mixed-tree pixels and a direct CanvasItem drawing texture through both GPU and compatibility backends on Linux Wayland. This does not establish visual owner acceptance or other platforms.

The hierarchy is implemented; complete reference API parity is not claimed. Missing GUI, canvas policies, rendering primitives, interpolation, scene-file authoring and other capabilities remain classified per member in [coverage](../coverage/index.md). No inert compatibility members are added.

## Relevant decisions

- [0008: SceneNode, CanvasItem and Node](../decisions/scene.md#adr-0008)
- [0004: Product scope and API correspondence](../decisions/product.md#adr-0004)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)
