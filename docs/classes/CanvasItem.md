# CanvasItem

Last updated: 2026-10-07

**Inherits:** [Node](Node.md)

**Inherited By:** [Entity](Entity.md), [Control](Control.md)

- **Source:** [CanvasItem.cs](../../src/Scene/Main/CanvasItem.cs), [CanvasItem.PhysicsInterpolation.cs](../../src/Scene/Main/CanvasItem.PhysicsInterpolation.cs), [CanvasItem.World.cs](../../src/Scene/Main/CanvasItem.World.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract partial class CanvasItem : Node`

## Skeletal presentation

Internal GetInterpolatedGlobalVisualTransform composes the existing per-item physics presentation poses through the actual canvas-parent chain (including TopLevel). Polygon skinning uses it with the same frame fraction as ordinary canvas replay. Internal AttachLastPolygonSkin connects only the just-recorded retained polygon; no new public generic drawing selector is introduced.

## Description

The abstract canvas base. Owns visibility, Z/Y order, behind-parent drawing, modulation, materials, retained drawing and transform queries/notifications. Entity supplies a concrete spatial placement model; Control supplies a rectangular layout model in the separate UI branch. A direct CanvasItem subclass can provide its own model through GetTransform and notify changes with NotifyLocalTransformChanged. Only direct canvas parents contribute transforms, modulation and materials; a neutral Node breaks those chains. TopLevel preserves the local transform while ending transform/material/modulation/Z inheritance. Visibility follows direct canvas parents, including TopLevel items, and the containing window.

The renderer normally uses the same local transform as public logical queries. Control can add a visual-only offset matrix through an internal render-transform hook; `GetGlobalTransform` and GUI hit testing continue to use its logical `GetTransform`. When physics interpolation is active, the renderer instead blends each eligible local visual transform between fixed ticks using `Engine.PhysicsInterpolationFraction`. Logical queries and input retain the latest transform; the presentation matrix reaches descendants and Y-sorted traversal. First attachment, reset, detach and process-time edits discard stale history.

Canvas roots follow scene order; a root's canvas subtree is ordered before the following root. TopLevel and neutral Node boundaries create separate canvas roots. Effective Z is always the primary draw key. At equal Z, children normally draw after their parent; ShowBehindParent draws a child subtree before it. YSortEnabled instead sorts the item itself (Y = 0) and its canvas children by local Y, merging nested enabled groups while keeping other child subtrees together. Drawing order does not change processing or input order.

The Z and sibling-order audit closes `ZIndex`, `ZAsRelative` and `MoveToFront`. Local Z accepts -4096 through 4096; an invalid assignment throws and preserves state under ADR 0008. Equal valid Z assignments still request configuration-warning refresh. Relative Z follows direct canvas parents and stops at neutral/TopLevel boundaries. `MoveToFront` moves a child to the last sibling position, preserves the committed order after callback failure and leaves a root unchanged. Attached reads and moves enforce the scene owner thread. Managed checks and 31 Linux/Wayland pixel cases on compatibility and GPU pass; the class and other member rows retain their individual coverage states.

The appearance audit closes `Material`, `Modulate`, `SelfModulate` and `UseParentMaterial`. Material assignments store a borrowed resource and emit `PropertyListChanged` even for an equal assignment; callback failure leaves the assignment committed. Parent modulation reaches direct canvas descendants, while self modulation affects only this item's drawing. Parent material inheritance stops at neutral and TopLevel boundaries; a child's own material remains stored while inheritance is enabled. Managed checks and focused live pixel readback pass on Linux/Wayland compatibility and GPU, with modulation also checked in dummy/software. Editor UI, other platforms and owner visual acceptance remain separate.

`GetTransform` is the abstract local placement query; the production `Entity` and `Control` overrides enforce attached owner-thread reads. The draw-transform audit closes `DrawSetTransform` and `DrawSetTransformMatrix`: each records a replacement state for later commands, with the node transform multiplied on the left. Hidden animation intervals skip transform state, rejected finite input leaves earlier state intact, and each frame replays from identity. Managed affine/replay checks, Wayland compatibility/GPU/HLSL/GLSL timing pixels and dummy/software timing pixels pass. Other CanvasItem rows retain their own statuses.

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
    protected override void OnDraw() => DrawRect(new Rect2(0, 0, 32, 16), Colors.Cyan);
}
```

## Viewport and input coordinates

Source: [CanvasItem.Coordinates.cs](../../src/Scene/Main/CanvasItem.Coordinates.cs). These are logical floating-point transforms, independent of render pixel snapping and framebuffer density. With `C = Viewport.CanvasTransform`, `F = Viewport.GetFinalTransform()` and `G = GetGlobalTransform()`, local drawing reaches client coordinates through `F * C * G`.

| Declaration | Contract |
| --- | --- |
| `public CanvasLayer? GetCanvasLayerNode()` | [Borrowed associated layer](#getcanvaslayernode) |
| `public Rect2 GetViewportRect()` | [Visible viewport rectangle](#getviewportrect) |
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

### GetCanvasLayerNode

`public CanvasLayer? GetCanvasLayerNode()`

Returns the borrowed layer associated with actual canvas membership, or null while detached/on the default canvas. The nearest enclosing layer is used up to the containing Viewport; neutral nodes and TopLevel preserve that association. Reparenting/tree transitions refresh membership, while manual notifications do not. Attached off-owner access throws InvalidOperationException; disposed access throws ObjectDisposedException.

### GetViewportRect

`public Rect2 GetViewportRect()`

Returns the active containing viewport's GetVisibleRect, in viewport coordinates. Canvas/node transforms do not change its bounds.

### GetCanvasTransform

`public Transform GetCanvasTransform()`

Returns the associated CanvasLayer.GetFinalTransform, or the active viewport's CanvasTransform for the default canvas. Layer following uses the logical matrix documented in [CanvasLayer](CanvasLayer.md#transform-and-source-audit). Neutral nodes and TopLevel end item transform inheritance while preserving layer membership.

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

All coordinate queries require a live item and enforce its attached owner thread. Except GetCanvasLayerNode and the detached fallback of GetGlobalTransformWithCanvas, they require active viewport membership; missing membership/off-owner access throws InvalidOperationException and disposal throws ObjectDisposedException. Queries preserve logical node state and do not emit notifications.

Verification: [managed hierarchy, inverse, lifetime and input-copy checks](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs); [Wayland GPU/compatibility and dummy rendering/input checks](../../tests/Electron2D.Tests/CanvasCoordinateRenderingTests.cs). Native readback includes noncommuting viewport transforms, independent canvas roots, retained commands, pixel snapping and HLSL/GLSL materials. Camera and CanvasLayer are integrated; Control layout is present, while GUI input, themes, nested viewports and content scaling remain absent; no physical-input/visual or other-platform acceptance is claimed.

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
| [`public TextureFilter TextureFilter { get; set; }`](#texturefilter) | Selects inherited, nearest, linear or mip/anisotropic filtering. |
| [`public TextureRepeat TextureRepeat { get; set; }`](#texturerepeat) | Selects inherited, clamp, repeat or mirror addressing. |
| [`public bool TopLevel { get; set; }`](#p-electron2d-canvasitem-toplevel) | Gets or sets whether this node ignores its parent's transform. |
| [`public bool UseParentMaterial { get; set; }`](#p-electron2d-canvasitem-useparentmaterial) | Gets or sets whether this node uses its parent's effective material. |
| [`public bool Visible { get; set; }`](#p-electron2d-canvasitem-visible) | Gets or sets this node's local logical visibility. |
| [`public bool YSortEnabled { get; set; }`](#p-electron2d-canvasitem-ysortenabled) | Sorts this item and its canvas children by local Y at equal Z. |
| [`public bool ZAsRelative { get; set; }`](#p-electron2d-canvasitem-zasrelative) | Gets or sets whether effective Z order accumulates ancestor Z values. |
| [`public int ZIndex { get; set; }`](#p-electron2d-canvasitem-zindex) | Gets or sets this node's local Z-order value. |

## Sampling enums

- [TextureFilter](TextureFilter.md): ParentNode = 0; Nearest = 1; Linear = 2; NearestWithMipmaps = 3; LinearWithMipmaps = 4; NearestWithMipmapsAnisotropic = 5; LinearWithMipmapsAnisotropic = 6; Max = 7.
- [TextureRepeat](TextureRepeat.md): ParentNode = 0; Disabled = 1; Enabled = 2; Mirror = 3; Max = 4.

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-canvasitem-dispose-system-boolean) | Disposes the scene hierarchy and clears retained canvas commands, pooled polygon/stroke storage and this layer's subscribers in a finally block. Borrowed resources remain caller-owned. |
| [`public void DrawAnimationSlice(double animationLength, double sliceBegin, double sliceEnd, double offset = 0d)`](#drawanimationslice) | Restricts subsequent commands to a repeating render-time interval. |
| [`public void DrawEndAnimation()`](#drawendanimation) | Restores unrestricted drawing without resetting transforms. |
| [`public void DrawPolyline(ReadOnlySpan<Vector2> points, Color color, float width = -1f, bool antialiased = false)`](#drawpolyline) | Joined strip with a uniform color. |
| [`public void DrawPolylineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false)`](#drawpolylinecolors) | Joined strip with interpolated vertex colors. |
| [`public void DrawMultiline(ReadOnlySpan<Vector2> points, Color color, float width = -1f, bool antialiased = false)`](#drawmultiline) | Independent endpoint pairs with a uniform color. |
| [`public void DrawMultilineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false)`](#drawmultilinecolors) | Independent pairs with per-segment colors. |
| [`public void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width = -1f, float dash = 2f, bool aligned = true, bool antialiased = false)`](#drawdashedline) | Aligned or unaligned local dash pattern. |
| [`public void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)`](#drawarc) | Sampled circular arc. |
| [`public void DrawEllipseArc(Vector2 center, float major, float minor, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)`](#drawellipsearc) | Sampled elliptical arc. |
| [`public void DrawCircle(Vector2 position, float radius, Color color, bool filled = true, float width = -1f, bool antialiased = false)`](#drawcircle) | Filled circle or circular outline. |
| [`public void DrawEllipse(Vector2 position, float major, float minor, Color color, bool filled = true, float width = -1f, bool antialiased = false)`](#drawellipse) | Filled ellipse or elliptical outline. |
| [`public void DrawColoredPolygon(ReadOnlySpan<Vector2> points, Color color, ReadOnlySpan<Vector2> uvs = default, Texture? texture = null)`](#drawcoloredpolygon) | Filled contour with uniform color. |
| [`public void DrawPolygon(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs = default, Texture? texture = null)`](#drawpolygon) | Triangulated contour with interpolated colors and UVs. |
| [`public void DrawPrimitive(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Texture? texture = null)`](#drawprimitive) | Point, line, triangle or quad. |
| [`public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)`](#m-electron2d-canvasitem-drawline-electron2d-vector2-electron2d-vector2-electron2d-color-system-single-system-boolean) | Records a straight line during canvas recording. |
| [`public void DrawRect(Rect2 rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)`](#m-electron2d-canvasitem-drawrect-electron2d-rect2-electron2d-color-system-boolean-system-single-system-boolean) | Records a filled rectangle or a centered rectangular outline during canvas recording. |
| [`public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)`](#m-electron2d-canvasitem-drawsettransform-electron2d-vector2-system-single-system-nullable-electron2d-vector2) | Records an additional transform for subsequent commands. It executes only when its animation interval is visible; each replay starts with identity. |
| [`public void DrawSetTransformMatrix(Transform transform)`](#m-electron2d-canvasitem-drawsettransformmatrix-electron2d-transform) | Records the full additional transform for subsequent commands. It executes only when its animation interval is visible; DrawEndAnimation retains the last executed transform. |
| [`public void DrawStyleBox(StyleBox styleBox, Rect2 rect)`](#drawstylebox) | Records borrowed style decoration during this item's canvas recording. |
| [`public void DrawTexture(RID texture, Vector2 position, Color? modulate = null)`](#texture-rid-overloads) | Resolves a live texture RID and preserves virtual drawing. |
| [`public void DrawTextureRect(RID texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)`](#texture-rid-overloads) | Resolves a live texture RID and preserves virtual drawing. |
| [`public void DrawTextureRectRegion(RID texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`](#texture-rid-overloads) | Resolves a live texture RID and preserves virtual drawing. |
| [`public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)`](#m-electron2d-canvasitem-drawtexture-electron2d-texture-electron2d-vector2-system-nullable-electron2d-color) | Draws a borrowed texture at its logical size during this item's canvas recording. |
| [`public void DrawTextureRect(Texture texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)`](#m-electron2d-canvasitem-drawtexturerect-electron2d-texture-electron2d-rect2-system-boolean-system-nullable-electron2d-color-system-boolean) | Stretches or repeats a borrowed texture over a local rectangle during canvas recording. |
| [`public void DrawTextureRectRegion(Texture texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`](#m-electron2d-canvasitem-drawtexturerectregion-electron2d-texture-electron2d-rect2-electron2d-rect2-system-nullable-electron2d-color-system-boolean-system-boolean) | Stretches a source region of a borrowed texture over a local rectangle during canvas recording. |
| [`public void ForceUpdateTransform()`](#forceupdatetransform) | Immediately delivers this item's pending global notification. |
| [`public Transform GetGlobalTransform()`](#m-electron2d-canvasitem-getglobaltransform) | Returns the transform composed through the direct canvas-parent chain. |
| [`public World? GetWorld()`](#getworld) | Returns the selected viewport canvas/physics world, or null while detached. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-canvasitem-getpropertydescriptors) | Extends neutral descriptors with visibility, ordering, top-level state, modulation and borrowed materials. |
| [`public abstract Transform GetTransform()`](#m-electron2d-canvasitem-gettransform) | Returns the local transform supplied by this item's placement model. |
| [`public void Hide()`](#m-electron2d-canvasitem-hide) | Sets `CanvasItem.Visible` to `false`. |
| [`public void MoveToFront()`](#m-electron2d-canvasitem-movetofront) | Moves this node to the last position among its siblings. |
| [`protected void NotifyItemRectChanged(bool sizeChanged = true)`](#m-electron2d-canvasitem-notifyitemrectchanged-system-boolean) | Reports a possible local-bounds change, optionally requesting redraw first. |
| [`protected void NotifyLocalTransformChanged()`](#m-electron2d-canvasitem-notifylocaltransformchanged) | Invalidates global transforms and delivers an enabled local-transform notification after a derived placement model commits state. |
| [`protected virtual void OnDraw()`](#m-electron2d-canvasitem-ondraw) | Records this node's retained canvas commands before its first visible frame and after QueueRedraw. |
| [`protected override void OnNotification(int what)`](#m-electron2d-canvasitem-onnotification-system-int32) | Preserves inherited lifecycle dispatch, projects visibility and local/global transform notifications to their typed events and propagates transform changes at parent boundaries. Actual canvas attachment is owned by SceneTree membership, independently of manually dispatched tree notifications. |
| [`public void QueueRedraw()`](#m-electron2d-canvasitem-queueredraw) | Requests regeneration of this node's retained drawing commands before a later visible frame. |
| [`public void Show()`](#m-electron2d-canvasitem-show) | Sets `CanvasItem.Visible` to `true`. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action<CanvasItem>? Draw`](#e-electron2d-canvasitem-draw) | Synchronous recording event between NotificationDraw and OnDraw. |
| [`public event Action<CanvasItem>? Hidden`](#e-electron2d-canvasitem-hidden) | Effective transition to hidden, after visibility delivery. |
| [`public event Action<CanvasItem>? ItemRectChanged`](#e-electron2d-canvasitem-itemrectchanged) | Synchronous local geometry notification; does not propagate to children. |
| [`public event Action<CanvasItem>? LocalTransformChanged`](#e-electron2d-canvasitem-localtransformchanged) | Projects NotificationLocalTransformChanged with this item as sender. |
| [`public event Action<CanvasItem>? TransformChanged`](#e-electron2d-canvasitem-transformchanged) | Projects NotificationTransformChanged with this item as sender. |
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
| [`public const int NotificationTransformChanged = 2000`](#f-electron2d-canvasitem-notificationtransformchanged) | Identifies a global-transform notification queued on tree entry or on enabled global invalidation. |
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

**Remarks:** Disposing the node does not dispose this shared resource. [ShaderMaterial](ShaderMaterial.md) with an assigned shader requires GPU rendering. [CanvasItemMaterial](CanvasItemMaterial.md) selects fixed blending; SDL software rejects modes other than Mix before drawing. An externally disposed borrowed material remains readable but cannot be used for rendering until replaced. Every assignment, including an equal one, raises PropertyListChanged after storing the borrowed reference.

**System.InvalidOperationException:** Attached access is off the owner thread, or mutation occurs during scene capture.

**System.ObjectDisposedException:** The node is disposed, or the assigned value is already disposed.

**System.Exception:** A property-list subscriber throws after the material changes.

<a id="p-electron2d-canvasitem-modulate"></a>
### `public Color Modulate { get; set; }`

Gets or sets the color multiplier inherited by this node's descendants.

**Value:** Opaque white by default.

**System.ArgumentException:** The color is not finite.

**System.InvalidOperationException:** Attached access is off the owner thread, or mutation occurs during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-canvasitem-notifylocaltransformchanges"></a>
### `public bool NotifyLocalTransformChanges { get; set; }`

Gets or sets whether local transform changes dispatch `CanvasItem.NotificationLocalTransformChanged`.

**Value:** `false` by default. Enables both NotificationLocalTransformChanged and its typed LocalTransformChanged event while attached. Detached changes are silent. This runtime policy is not stored by PackedScene.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-canvasitem-notifytransformchanges"></a>
### `public bool NotifyTransformChanges { get; set; }`

Gets or sets whether global transform changes dispatch `CanvasItem.NotificationTransformChanged`.

**Value:** `false` by default. Enables queueing when the resolved global transform becomes invalid. Tree entry queues an initial notification regardless of this flag. Enabling while attached resolves the current global transform without queueing; disabling does not cancel an existing queued notification. This runtime policy is not stored by PackedScene. See [delivery details](#transform-notification-delivery).

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-canvasitem-selfmodulate"></a>
### `public Color SelfModulate { get; set; }`

Gets or sets the color multiplier applied only to this node's drawing.

**Value:** Opaque white by default.

**System.ArgumentException:** The color is not finite.

**System.InvalidOperationException:** Attached access is off the owner thread, or mutation occurs during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-canvasitem-showbehindparent"></a>
### `public bool ShowBehindParent { get; set; }`

False by default. True draws this canvas subtree before its canvas parent when effective Z is equal. Effective Z takes precedence within each canvas. An enabled Y-sorting parent orders participating children by Y instead of this flag. Neutral parents and TopLevel items have no canvas parent to draw behind.

Changes affect the next submission without QueueRedraw. The value is stored by PackedScene. Attached access off the owner thread or mutation during capture throws InvalidOperationException; access after disposal throws ObjectDisposedException.

<a id="p-electron2d-canvasitem-toplevel"></a>
### `public bool TopLevel { get; set; }`

Gets or sets whether this node ignores its parent's transform.

**Value:** `false` by default.

**Remarks:** The local transform stays unchanged; global coordinates are recomputed against the new canvas boundary. The item becomes a separate canvas root, drawn after the preceding root's entire canvas subtree at the same Z. Roots retain scene order; Z takes precedence within each canvas. Layer grouping precedes item Z. Logical visibility continues to follow direct canvas ancestors. An attached change emits NotificationExitCanvas with the old mode, then NotificationEnterCanvas with the committed new mode. It schedules redraw; recursive rebinding is rejected and callback failures are aggregated after the transition.

**System.InvalidOperationException:** Mutation occurs off the owner thread, during packed-scene capture, or recursively during canvas rebinding.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.AggregateException:** A canvas or transform callback fails; the mode transition still completes.

<a id="p-electron2d-canvasitem-useparentmaterial"></a>
### `public bool UseParentMaterial { get; set; }`

Gets or sets whether this node uses its parent's effective material.

**Value:** False by default. A root using its parent material uses ordinary color drawing.

**System.InvalidOperationException:** Attached access is off the owner thread, or mutation occurs during scene capture.

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

False by default. True orders the item itself at Y = 0 and its direct canvas children in ascending Y in this item's local coordinate system. The root's global rotation or scale does not change the sorting coordinates. Nested enabled children join the same group using composed local transforms. A child with sorting disabled keeps its canvas subtree together at that child's Y; any deeper enabled group sorts independently within that subtree. Approximate ties use Mathf.IsEqualApprox and preserve scene order. Effective Z takes precedence over Y within each canvas.

Invisible children do not participate. TopLevel children and children below neutral nodes are separate canvas roots. ShowBehindParent is ignored for items directly ordered by the Y group, but still applies inside unsorted subtrees. Processing and input order remain unchanged.

Changes affect the next submission without QueueRedraw. PackedScene stores the value. Attached access off the owner thread or mutation during capture throws InvalidOperationException; access after disposal throws ObjectDisposedException.

<a id="p-electron2d-canvasitem-zasrelative"></a>
### `public bool ZAsRelative { get; set; }`

Gets or sets whether effective Z order accumulates ancestor Z values.

**Value:** `true` by default.

**System.InvalidOperationException:** An attached node is read or mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-canvasitem-zindex"></a>
### `public int ZIndex { get; set; }`

Gets or sets this node's local Z-order value.

**Value:** An integer from `CanvasItem.MinimumZIndex` through `CanvasItem.MaximumZIndex`; the default is zero.

Every valid assignment commits the Z value and then requests configuration-warning refresh, including unchanged values. Invalid assignments do not emit. Subscriber failures propagate after the value is committed. SceneDiagnosticsTests verifies the selected-scene event.

**System.ArgumentOutOfRangeException:** The assigned value is outside the supported range.

**System.InvalidOperationException:** An attached node is read or mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

### TextureFilter

`public TextureFilter TextureFilter { get; set; }`

ParentNode by default. Actual changes refresh attached inheriting descendants, request redraw, then raise PropertyListChanged on this item. Overrides and neutral parents stop propagation. Detached changes apply at entry; TopLevel and reparenting recompute inheritance. GPU supports all concrete modes; mipmaps/anisotropy fail on compatibility, and linear fails on software triangles. Base-level modes never sample stored lower mips. Anisotropy uses the viewport limit; mip interpolation uses the project startup setting. Missing image mips are not generated implicitly.

Invalid or Max values throw ArgumentOutOfRangeException before mutation. InvalidOperationException rejects off-owner/capture-time mutation; ObjectDisposedException rejects disposed access. A subscriber exception leaves the new value and pending redraw committed. Equal assignments do nothing. Stored by PackedScene.

### TextureRepeat

`public TextureRepeat TextureRepeat { get; set; }`

ParentNode by default. Disabled clamps to edges, Enabled repeats, Mirror reflects alternate tiles. GPU supports all; compatibility rejects Mirror and checks native non-power-of-two wrapping. A tiled rectangle command forces Enabled. Inheritance, redraw, PropertyListChanged, no-ops, storage and errors match TextureFilter. This setting does not change the texture resource or arbitrary material parameter samplers.

## Method Descriptions

### DrawAnimationSlice

`public void DrawAnimationSlice(double animationLength, double sliceBegin, double sliceEnd, double offset = 0d)`

Records a state command that restricts subsequent drawing. All four arguments are finite double-precision seconds; `offset` defaults to zero. Every submitted frame computes `phase = Mathf.PosMod(renderTime - offset, animationLength)` and draws when `phase >= sliceBegin && phase < sliceEnd`. The begin boundary is inclusive, the end exclusive. Offset can be negative or positive; times before the origin wrap through the period. The bounds are not clamped or wrapped: reversed/equal bounds hide everything, and wider bounds can include the whole period. A zero period gives no visible phase; a negative period uses the divisor's signed phase range.

Each interval replaces the preceding one and is evaluated even if preceding commands are hidden. There is no interval stack. All geometry and transform commands inside a hidden interval are skipped before texture/material access. Recording still validates inputs and resources; only consumption is conditional. Interval state belongs to this item and resets to unrestricted at each replay; it does not propagate to child or sibling items. Retained commands animate without calling OnDraw or QueueRedraw every frame. Explicit redraw replaces the complete command sequence.

The renderer clock starts at zero for each Engine.Run. After FramePreDraw it advances by the captured scaled process step; changing Engine.TimeScale during processing affects subsequent steps. Tree pause does not stop it; TimeScale zero does. Disabled render loops and hidden root windows do not submit or advance this clock. [RenderingTimeRolloverSeconds](ProjectSettings.md#renderingtimerolloverseconds), default 3600, wraps it on each submitted frame and applies active project feature overrides. The same clock supplies the optional GPU fragment [TIME built-in](../components/shader-materials.md#render-time).

Requires this item's active NotificationDraw, synchronous Draw event or OnDraw scope. Nonfinite arguments throw ArgumentException; wrong thread/outside recording throws InvalidOperationException; disposed access throws ObjectDisposedException. Failed validation preserves previously recorded commands, while an uncaught callback exception clears the whole recording for retry.

### DrawEndAnimation

`public void DrawEndAnimation()`

Restores unrestricted subsequent drawing, equivalent to a length-one interval spanning phase zero through two. It preserves the most recent transform that actually executed during this frame. A transform recorded inside a hidden interval does not become active when the interval ends. This call is optional when no later geometry needs unrestricted visibility. Recording/thread/disposal guards are the same as DrawAnimationSlice.

#### Interval example and verification

Inside an Entity subclass (partial snippet):

```csharp
protected override void OnDraw()
{
    DrawAnimationSlice(1, 0, 0.5);
    DrawRect(new(0, 0, 24, 24), Colors.Red);
    DrawAnimationSlice(1, 0.5, 1);
    DrawCircle(new(12, 12), 12, Colors.Blue);
    DrawEndAnimation();
    DrawLine(new(0, 28), new(24, 28), Colors.White, 2);
}
```

[CanvasTimingTests](../../tests/Electron2D.Tests/CanvasTimingTests.cs) covers boundaries, offsets, signed/zero periods, state replacement, skipped transforms/resources, redraw/failure reset, owner/disposal guards and warmed allocation. [CanvasTimingRenderingTests](../../tests/Electron2D.Tests/CanvasTimingRenderingTests.cs) checks actual retained phase changes, time scaling, tree pause, disabled rendering, live rollover/feature overrides, state isolation, redraw and rectangle pixels. See [the component audit](../components/canvas-rendering.md#animation-intervals-and-rectangles).

### Stroke recording contract

All nine methods below return void and require this item's active NotificationDraw, synchronous Draw event or OnDraw scope. Off-owner or outside-scope calls throw InvalidOperationException; a disposed item throws ObjectDisposedException. Invalid point/color counts or nonfinite used geometry, colors and widths throw ArgumentException. Derived local coordinates are also checked for overflow before committing a command. An uncaught callback error discards the partial recording and leaves a redraw pending. A caught invalid call does not overwrite previously recorded commands.

Inputs and generated local geometry are retained in reusable storage until redraw. Item/draw transforms, inherited modulation and current material are applied during submission; changing item placement or modulation requires no redraw. Commands use the same GPU/compatibility triangle batches and HLSL/GLSL material interface as other canvas drawing. Transformed nonfinite positions/colors throw InvalidOperationException. Disposal releases the storage; no borrowed material is disposed.

Unless a filled shape ignores it, positive `width` is local, negative width is one framebuffer pixel regardless of scaling, and zero produces no stroke geometry. Antialiased positive widths compensate the opaque core: widths up to 2.5 are halved, widths from 2.5 to 5 interpolate toward a 0.625 reduction, and larger widths subtract 0.625. The local feather is 1.25 units, proportionally reduced for compensated widths below one. It scales with item/draw transforms. Negative-width polyline/multiline paths ignore `antialiased`; DrawLine and the short-dash fallback can still feather their thin cores. Final vertex snapping occurs after tessellation, including thin-line expansion. Exact hardware line raster coverage is not yet matched; thin lines currently use triangles.

### DrawPolyline

`public void DrawPolyline(ReadOnlySpan<Vector2> points, Color color, float width = -1f, bool antialiased = false)`

Copies at least two finite local `points` with the finite uniform `color`. The last endpoint approximately equal to the first closes the strip. Consecutive segments share miter vertices; bisector length is limited to three half-widths. Open endpoints use flat caps. Repeated points use adjacent nonzero directions; coincident geometry has no area. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawPolylineColors

`public void DrawPolylineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false)`

Uses the same point, join, width and lifetime contract as DrawPolyline. `colors` are interpolated along the strip: empty means white, one color is uniform, missing entries repeat the last supplied color, and entries beyond the point count are ignored. Only used colors must be finite. Colors are copied during recording. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawMultiline

`public void DrawMultiline(ReadOnlySpan<Vector2> points, Color color, float width = -1f, bool antialiased = false)`

Copies a nonempty even-length `points` sequence as independent endpoint pairs, all with the finite `color`. There is no connecting geometry across pairs. Each segment uses the shared straight-line positive-width geometry and flat caps; negative-width segments ignore antialiasing. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawMultilineColors

`public void DrawMultilineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false)`

Uses the independent-pair contract of DrawMultiline. `colors` contains either one finite uniform color or exactly one finite color per segment, not one per endpoint. Empty or other counts throw ArgumentException. Coincident endpoints contribute no geometry. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawDashedLine

`public void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width = -1f, float dash = 2f, bool aligned = true, bool antialiased = false)`

Draws from finite local `from` to `to` with finite `color`. `dash` is a positive finite local dash length, default 2. An odd number of alternating dash/gap steps is chosen. With `aligned = true`, partial end dashes are fitted symmetrically and touch both endpoints; false starts with a full dash and may leave an undrawn tail. A line shorter than `dash` calls DrawLine, including its negative-width antialias behavior. Nonpositive/nonfinite dash or a computed count beyond array capacity throws ArgumentOutOfRangeException; no unbounded integer conversion occurs. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawArc

`public void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)`

Circular counterpart of DrawEllipseArc: finite signed `radius` supplies both axes. `center` is local; `startAngle` and `endAngle` are finite radians. `pointCount` includes both endpoints and must be at least two, otherwise ArgumentOutOfRangeException. Sweep is clamped to plus or minus one full turn. The finite `color`, width, joins and antialiasing follow DrawPolyline. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawEllipseArc

`public void DrawEllipseArc(Vector2 center, float major, float minor, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)`

Samples an ellipse around finite local `center` with finite signed horizontal `major` and vertical `minor` radii. Finite `startAngle` and `endAngle` are radians; the signed difference clamps to one full turn. `pointCount` must be at least two and includes both endpoints. Clockwise/counterclockwise sweeps, zero sweep and signed radii are accepted. Resulting points follow DrawPolyline with the finite uniform `color`. Too few samples throw ArgumentOutOfRangeException; sampling overflow throws ArgumentException before a command is committed. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawCircle

`public void DrawCircle(Vector2 position, float radius, Color color, bool filled = true, float width = -1f, bool antialiased = false)`

Circular counterpart of DrawEllipse with finite local `position`, finite signed `radius` and finite `color`. `filled = true` uses a 64-segment fan and ignores finite width. An outline at least as wide as its diameter becomes a filled circle with radius increased by half the width. Other outlines use a closed 65-point strip. Feathering follows the ellipse rules below. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

### DrawEllipse

`public void DrawEllipse(Vector2 position, float major, float minor, Color color, bool filled = true, float width = -1f, bool antialiased = false)`

Uses finite local `position`, finite signed horizontal `major` and vertical `minor` radii, and finite `color`. The default `filled = true` emits a 64-segment fan and ignores finite width. An outline at least as wide as the larger diameter becomes a filled ellipse with both axes increased by half the width; other outlines use a closed strip. With antialiasing, filled core radii shrink by 0.3125 local units and clamp to zero; an outer 1.25-unit alpha ring scales down for subpixel axes. Outline feathering follows DrawPolyline. Signed radii are accepted, including the zero-clamping effect in the filled antialiased path. All shared guards, storage and width rules are defined in the [stroke recording contract](#stroke-recording-contract).

#### Stroke example and verification

Override on an Entity subclass, inside its draw callback:

```csharp
protected override void OnDraw()
{
    DrawPolyline([new(0, 0), new(32, 0), new(32, 24)], Colors.White, 3, true);
    DrawDashedLine(new(0, 40), new(64, 40), Colors.Yellow, 2, 6);
    DrawArc(new(96, 32), 20, 0, Mathf.Pi, 33, Colors.Green, 2);
    DrawEllipse(new(32, 80), 24, 12, Colors.Blue);
}
```

[CanvasStrokeTests](../../tests/Electron2D.Tests/CanvasStrokeTests.cs) checks joins, degenerate/closed paths, colors, snapshots, widths, arcs, dashes, exceptions, rollback, thread/disposal guards and zero managed allocation over 1,000 warmed redraw/replay iterations. [CanvasStrokeRenderingTests](../../tests/Electron2D.Tests/CanvasStrokeRenderingTests.cs) checks backend pixels and retained state over three frames. See [component verification and limits](../components/canvas-rendering.md#stroke-commands).

The software driver truncates fractional triangle positions before rasterization. The circle feather check therefore samples the adjacent inner pixel on software and explicitly checks its excluded outer pixel; hardware checks the fractional outer ring. This preserves the documented [fallback precision limit](../components/canvas-rendering.md#pixel-snapping), without claiming identical edge coverage.

### DrawColoredPolygon

Source: [CanvasItem.Polygons.cs](../../src/Scene/Main/CanvasItem.Polygons.cs).

`public void DrawColoredPolygon(ReadOnlySpan<Vector2> points, Color color, ReadOnlySpan<Vector2> uvs = default, Texture? texture = null)`

Records a convex or concave contour in local coordinates with a uniform finite color. Requires at least three finite points in either winding order; the single contour has no holes. Copies all used values and triangulates once at recording. Shared endpoints and collinear vertices are permitted when triangulation can complete, including degenerate triangles which cover no pixels. Self-intersections are unsupported; triangulation failure throws ArgumentException before recording a command. The triangulator is bounded ear clipping with a relaxed final attempt for collinear/duplicate vertices; it is not a contour repair operation.

UVs are normalized texture coordinates, either absent (zero at each vertex) or one per point. A null texture uses white sampling. Texture data is borrowed, so live pixel updates affect retained commands. Polygon input changes, atlas region changes and texture identity changes require QueueRedraw. The command does not subscribe to resources or own/dispose them. It follows the same material, filter/repeat, ordering, modulation, transform and pixel-snap paths as other canvas geometry; no antialias fringe is added.

### DrawPolygon

`public void DrawPolygon(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs = default, Texture? texture = null)`

Uses the same contour and UV contract as DrawColoredPolygon. Colors may be empty for white, one uniform finite color, or exactly one finite color per point. Colors and UVs interpolate across triangulated faces and multiply sampled texture colors. Nonuniform vertex attributes depend on the selected triangulation; this is not bilinear quad interpolation.

For AtlasTexture, supplied UVs map through its immediate stored Region divided by the immediate source's logical size. Fractional region coordinates are preserved. A zero Region.Size collapses corresponding UV axes instead of expanding them; Margin and FilterClip are ignored. Nested atlas regions are not recursively composed for this operation: only the immediate view remaps, and the ultimate full source is sampled. Without UVs, zero coordinates sample the full source. The source identity and mapping are captured atomically under the atlas graph gate, and later atlas disposal/replacement does not erase the recorded source. An empty atlas uses white sampling. Invalid/nonfinite atlas mapping throws before recording.

Both polygon methods require this item's active NotificationDraw, synchronous Draw event or OnDraw scope. Off-owner or outside-scope calls throw InvalidOperationException; disposed item/texture access throws ObjectDisposedException. Nonfinite points/colors/UVs or wrong counts throw ArgumentException. A custom atlas source's size callback can record commands or dispose the item; validation resumes after the callback and cannot overwrite its commands. Uncaught recording errors clear the whole partial recording and retain its redraw request. Native texture-format/filter restrictions are the existing [sampling capability contract](../components/canvas-rendering.md#texture-sampling).

### DrawPrimitive

`public void DrawPrimitive(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Texture? texture = null)`

Copies one to four local points: one point, a two-point line, a triangle, or a quad split along vertices 0–2. Zero points and more than four throw ArgumentException. No polygon triangulation is performed. Missing colors repeat the first supplied color, or white if empty; missing UVs are zero. Extra attributes are ignored. Used values must be finite. Texture borrowing, errors, scope, transforms and modulation follow DrawPolygon. Atlas primitives sample the full underlying image with unchanged UVs.

Points cover one framebuffer pixel; lines use a one-pixel flat-cap strip, even under nonuniform item or drawing scale. Coincident line endpoints draw nothing. These short primitives have no antialias fringe. Both backends consume triangle geometry; exact subpixel point/line coverage is subject to their rasterizer precision, including software coordinate truncation. Vertex snapping snaps transformed primitive endpoints before constructing the one-pixel footprint.

Example inside an Entity subclass (uses only the public API):

```csharp
protected override void OnDraw()
{
    DrawColoredPolygon([new(0, 0), new(32, 0), new(16, 12), new(0, 32)], Colors.Cyan);
    DrawPrimitive([new(40, 0), new(64, 0), new(40, 24)], [Colors.Red, Colors.Green, Colors.Blue], []);
}
```

[CanvasPolygonTests](../../tests/Electron2D.Tests/CanvasPolygonTests.cs) checks winding/area, duplicate and collinear points, snapshots, attributes, atlas mapping, callback reentry/disposal, owner/scope guards, recording rollback, reflected transforms, overflow and zero warmed allocation across 1,000 redraw/replay iterations. [CanvasPolygonRenderingTests](../../tests/Electron2D.Tests/CanvasPolygonRenderingTests.cs) checks five readback frames per backend/language. See the [component source audit](../components/canvas-rendering.md#polygon-commands).


<a id="m-electron2d-canvasitem-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Disposes the scene hierarchy and clears retained canvas commands, pooled polygon/stroke storage and this layer's subscribers in a finally block. Borrowed resources remain caller-owned.

<a id="m-electron2d-canvasitem-drawline-electron2d-vector2-electron2d-vector2-electron2d-color-system-single-system-boolean"></a>
### `public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)`

Records a straight line during canvas recording.

**Parameter `from`:** The finite starting point in local coordinates.

**Parameter `to`:** The finite ending point in local coordinates.

**Parameter `color`:** The finite drawing color.

**Parameter `width`:** Width in local units; a negative value uses one framebuffer pixel.

**Parameter `antialiased`:** Whether to add a local alpha feather with compensated core width.

**Remarks:** Lines use flat caps. Coincident endpoints or zero width draw nothing. The local feather scales with the item/draw transform. Unlike thin polylines, a negative-width DrawLine can still add local feather geometry around its one-pixel core. See the [stroke width contract](#stroke-recording-contract).

**System.ArgumentException:** Geometry, color or width is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawrect-electron2d-rect2-electron2d-color-system-boolean-system-single-system-boolean"></a>
### `public void DrawRect(Rect2 rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)`

Records a filled rectangle or a centered rectangular outline during canvas recording.

**Parameter `rect`:** A finite local rectangle; negative dimensions are normalized.

**Parameter `color`:** The finite drawing color.

**Parameter `filled`:** Whether to fill the rectangle; true by default.

**Parameter `width`:** Outline width in local units; a negative value uses one framebuffer pixel.

**Parameter `antialiased`:** Whether to use compensated local feather geometry; ignored for negative-width outlines.

**Remarks:** Negative sizes normalize before drawing. An outline at least as wide as either dimension becomes a fill expanded by half its width, including when the original rectangle has zero area. Other outlines use exactly the closed DrawPolyline path, including width compensation, miter joins, thin-width antialias policy and final vertex snapping. Ordinary zero-width outlines produce no geometry.

A filled antialiased rectangle shrinks its core by 0.3125 local units and adds 1.25-unit side and corner feathers. If the smaller adjusted core dimension lies between zero and one, feather width scales by that dimension. Very small cores may have negative adjusted dimensions; these are preserved, not clamped to zero. Filled width is ignored after finite validation. All geometry is recorded in reusable CanvasStroke storage and transforms/modulates on replay. Local overflow throws ArgumentException before committing the command. [CanvasTimingTests](../../tests/Electron2D.Tests/CanvasTimingTests.cs) and native [CanvasTimingRenderingTests](../../tests/Electron2D.Tests/CanvasTimingRenderingTests.cs) cover normalization, wide/degenerate shapes, feathers, backend pixels and zero warmed allocations. Exact thin-line hardware coverage remains Partial.

**System.ArgumentException:** Geometry, color or width is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawsettransform-electron2d-vector2-system-single-system-nullable-electron2d-vector2"></a>
### `public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)`

Records an additional transform for subsequent commands. It executes only when its animation interval is visible; each replay starts with identity.

**Parameter `position`:** Translation in local units.

**Parameter `rotation`:** Rotation in radians, zero by default.

**Parameter `scale`:** Scale, or null for one on both axes.

**System.ArgumentException:** The transform is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-canvasitem-drawsettransformmatrix-electron2d-transform"></a>
### `public void DrawSetTransformMatrix(Transform transform)`

Records the full additional transform for subsequent commands. It executes only when its animation interval is visible; DrawEndAnimation retains the last executed transform.

**Parameter `transform`:** The finite local drawing transform.

**System.ArgumentException:** The transform is not finite.

**System.InvalidOperationException:** Called outside this item's recording scope or off its owner thread.

**System.ObjectDisposedException:** The node is disposed.

<a id="drawstylebox"></a>
### `public void DrawStyleBox(StyleBox styleBox, Rect2 rect)`

Records a borrowed [StyleBox](StyleBox.md) using its typed Draw/OnDraw path during this item's recording scope. The style supplies empty, line, textured nine-patch, rounded flat or custom geometry in local pixels; the existing canvas transform, modulation, clipping and texture pipeline apply.

**Parameters:** `styleBox` is a live caller-owned style; `rect` is a finite local rectangle. Null style throws ArgumentNullException, disposed style/target throws ObjectDisposedException, and a wrong owner or inactive recording scope throws InvalidOperationException. Nonfinite geometry throws ArgumentException. The style is not retained as a live draw dependency: the commands capture its current values, while referenced textures remain borrowed.

The caller requests redraw after style changes, and minimum-size refresh when it uses the style's margins. StyleBox.GetCurrentItemDrawn returns this item throughout NotificationDraw, Draw and OnDraw on the recording thread; the context is restored in finally after success or failure. A failed recording drops partial commands under the existing canvas cleanup contract. Typed Theme lookup and current panel skin selection execute under [ADR 0083](../decisions/rendering.md#adr-0083); automatic GUI mask routing remains separate. See [ADR 0082](../decisions/rendering.md#adr-0082).

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

<a id="m-electron2d-canvasitem-drawtexturerect-electron2d-texture-electron2d-rect2-system-boolean-system-nullable-electron2d-color-system-boolean"></a>
### `public void DrawTextureRect(Texture texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)`

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

<a id="m-electron2d-canvasitem-drawtexturerectregion-electron2d-texture-electron2d-rect2-electron2d-rect2-system-nullable-electron2d-color-system-boolean-system-boolean"></a>
### `public void DrawTextureRectRegion(Texture texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`

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

<a id="getworld"></a>
### `public World? GetWorld()`

An attached item receives its nearest viewport's selected [World](World.md), or the SceneTree fallback in a viewport-free scene. Detached access returns null. World.Canvas, Space and DirectSpaceState share runtime identity. NotificationWorldChanged (36) follows committed viewport replacement in parent-first order, excludes independent nested viewports and continues after observer failures. See [the world contract](../components/worlds.md) and [WorldTests](../../tests/Electron2D.Tests/WorldTests.cs).

<a id="m-electron2d-canvasitem-getglobaltransform"></a>
### `public Transform GetGlobalTransform()`

Returns the transform composed through the direct canvas-parent chain.

**Returns:** The local transform when the parent is non-canvas or TopLevel is enabled. Results are cached until global invalidation; querying resolves invalidation but leaves queued notifications intact. Attached off-owner queries throw InvalidOperationException. See [global invalidation](#global-invalidation-and-cached-queries).

**System.ObjectDisposedException:** The item is disposed.

<a id="m-electron2d-canvasitem-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends neutral descriptors with visibility, ordering, top-level state, modulation and borrowed materials.

<a id="m-electron2d-canvasitem-gettransform"></a>
### `public abstract Transform GetTransform()`

Returns the local transform supplied by this item's placement model.

**Returns:** The transform relative to the direct canvas parent.

**System.InvalidOperationException:** An attached item is queried off its scene owner thread; concrete overrides enforce this guard.

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

**System.InvalidOperationException:** An attached item is accessed off the owner thread.

**System.ObjectDisposedException:** This node or its parent is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more child-order or tree-change callbacks fail after the order changes.

<a id="m-electron2d-canvasitem-notifyitemrectchanged-system-boolean"></a>
### `protected void NotifyItemRectChanged(bool sizeChanged = true)`

Reports that an operation may affect local drawing bounds. Derived types commit their geometry before calling it; identical numeric bounds need not suppress an operation's event. When `sizeChanged` is true, calls QueueRedraw before delivery, with its detached, hidden and recording/coalescing rules. False delivers the event without requesting redraw.

Delivery is synchronous on the caller's thread, restricted to the scene owner while attached. Hidden, detached and processing-disabled items still deliver. The event is local and does not propagate to children. InvalidOperationException rejects off-owner or capture-time calls before delivery; ObjectDisposedException rejects disposed items. Subscriber exceptions propagate immediately, stopping later subscribers while retaining committed state and pending redraw.

<a id="m-electron2d-canvasitem-notifylocaltransformchanged"></a>
### `protected void NotifyLocalTransformChanged()`

Invalidates global transforms and delivers an enabled local-transform notification after a derived placement model commits state.

**Remarks:** The local transform must be committed first. Global invalidation and queueing precede local callback delivery and stop at neutral nodes and TopLevel canvas items. Local notification and its typed event run synchronously only while attached and NotifyLocalTransformChanges is enabled; equal assignments notify too. A local callback failure leaves global invalidation and pending entries intact.

**System.InvalidOperationException:** The caller is not the scene owner or a capture is active.

**System.ObjectDisposedException:** The item is disposed.

**System.Exception:** A local notification callback fails after global invalidation.

<a id="m-electron2d-canvasitem-ondraw"></a>
### `protected virtual void OnDraw()`

Records this node's retained canvas commands before its first visible frame and after QueueRedraw.

**Remarks:** Runs on the scene owner thread during rendering. The recording sequence is NotificationDraw, synchronous Draw handlers, then OnDraw. Geometry, texture and drawing-transform calls are valid in all three stages for this item. Commands and draw transform are reset before the notification. Failure aborts subsequent stages, clears partial commands and restores the dirty flag; the recording scope always closes.

<a id="m-electron2d-canvasitem-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Preserves inherited lifecycle dispatch, projects visibility and local/global transform notifications to their typed events and propagates transform changes at parent boundaries. Actual canvas attachment is owned by SceneTree membership, independently of manually dispatched tree notifications.

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

Projects NotificationLocalTransformChanged with this item as sender.

**Remarks:** The base notification handler delivers this event. Automatic delivery is synchronous, attached-only and enabled by NotifyLocalTransformChanges; explicit manual notifications deliver regardless of that policy. A throwing subscriber stops later subscribers in that invocation.

<a id="e-electron2d-canvasitem-transformchanged"></a>
### `public event Action<CanvasItem>? TransformChanged`

Projects NotificationTransformChanged with this item as sender.

**Remarks:** The base notification handler delivers this event during queued, forced or manual global notification delivery. Initial tree entry also queues one notification. A throwing subscriber stops later subscribers in that invocation; a scene delivery pass continues other items and aggregates failures.

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

Identifies a global-transform notification queued on tree entry or on enabled global invalidation.

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

Drawing commands retain borrowed resources and are valid during NotificationDraw, synchronous Draw handlers and OnDraw. Deferred handlers execute outside the recording scope and cannot draw. QueueRedraw coalesces requests; resource-thread notifications only set an atomic flag, and recording runs on the owner thread. Global transform invalidation stops at neutral and TopLevel children. Failed queued transform delivery does not skip later pending items. Failed visibility delivery does not skip later direct canvas siblings.

## Verification and limits

[SpriteTests](../../tests/Electron2D.Tests/SpriteTests.cs) checks rectangle-event triggers/order, committed bounds, no-ops, invalid changes, hidden/detached/disabled delivery, local-only propagation, redraw options, worker guards, reentry, callback failures and disposal.

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) verifies inheritance, neutral API boundaries, direct custom CanvasItem transforms, mixed parenting, notifications, timer/tween scheduling, packed factories/state, deletion and failure continuation. Existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) retain lifecycle, input, math and ownership coverage. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies mixed-tree pixels and a direct CanvasItem drawing texture through both GPU and compatibility backends on Linux Wayland. This does not establish visual owner acceptance or other platforms.

[CanvasOrderingTests](../../tests/Electron2D.Tests/CanvasOrderingTests.cs) verifies 31 framebuffer cases for behind-parent subtrees, effective Z, canvas-root order, local/nested Y groups, visibility and mutations from drawing callbacks on Wayland GPU/compatibility and dummy/software. Managed checks cover defaults, owner-thread guards and packed ordering flags; warmed rendering with nested Y groups allocates zero managed bytes in the measured interval.

[CanvasLifecycleTests](../../tests/Electron2D.Tests/CanvasLifecycleTests.cs) verifies lifecycle/visibility/recording order, manual notifications, reattachment, callback failures and recording recovery. Six-frame pixel sequences on Linux Wayland GPU/compatibility and dummy/software verify all three drawing stages plus redraw on showing, reattachment and TopLevel rebinding. The complete renderer allocation check still passes.

The hierarchy is implemented; complete reference API parity is not claimed. [PhysicsInterpolationTests](../../tests/Electron2D.Tests/PhysicsInterpolationTests.cs) checks logical/presentation separation, reset and zero warmed managed allocations over 128 active ticks; [native pixels](../../tests/Electron2D.Tests/PhysicsInterpolationNativeTests.cs) pass on Linux dummy compatibility and Wayland compatibility/GPU. Missing GUI, canvas policies, rendering primitives, scene-file authoring and other capabilities remain classified per member in [coverage](../coverage/index.md). No inert compatibility members are added.

## Relevant decisions

- [0008: Node, CanvasItem and Entity](../decisions/scene.md#adr-0008)
- [0004: Product scope and API correspondence](../decisions/product.md#adr-0004)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)

## Viewport pixel snapping

[Viewport](Viewport.md#pixel-snapping-properties) controls render-only transform and vertex rounding. GetTransform/GetGlobalTransform remain logical queries. Transform snapping participates in Y sorting; vertex snapping applies after DrawSetTransform and framebuffer scaling. Retained recording is not invalidated merely by changing these flags. See [the canvas contract](../components/canvas-rendering.md#pixel-snapping) for Sprite offsets and texture clipping.

Transform propagation uses pooled child snapshots, clears retained references on return, and revalidates each child after callback mutations. [PathTests](../../tests/Electron2D.Tests/PathTests.cs) verifies zero allocation for warmed follower movement with a child. Pool growth, exceptions and user callbacks can still allocate.

## Rendering visibility masks

| Declaration | Contract |
| --- | --- |
| `public uint VisibilityLayer { get; set; }` | [Stored rendering mask](#visibilitylayer) |
| `public bool GetVisibilityLayerBit(int layer)` | [Read one bit](#getvisibilitylayerbit) |
| `public void SetVisibilityLayerBit(int layer, bool enabled)` | [Change one bit](#setvisibilitylayerbit) |

Example with an existing root window and item:

```csharp
item.VisibilityLayer = 0;
item.SetVisibilityLayerBit(3, true);
window.CanvasCullMask = 1u << 3;
```

### VisibilityLayer

`public uint VisibilityLayer { get; set; }`

Defaults to 1; all 32 bits, including zero and `uint.MaxValue`, are accepted and stored by PackedScene. Rendering requires a nonzero intersection with Viewport.CanvasCullMask. Each direct canvas ancestor must pass that same viewport mask independently; masks are not inherited or intersected with each other. A failed ancestor suppresses its direct canvas subtree, including nested Y-sort groups. TopLevel, neutral Node and CanvasLayer boundaries start independent canvas roots for mask culling.

Mask edits affect retained submission without QueueRedraw or visibility events. Visible and IsVisibleInTree stay unchanged, as do input and frame processing. Logically visible nodes still execute pending OnDraw, even with a zero mask; changes made there affect the current submission. Layer visibility and local Visible remain separate conditions. See the [source audit and backend checks](../components/canvas-rendering.md#canvas-visibility-masks).

Live detached access is allowed; attached access requires the scene owner thread. Off-owner access or mutation during scene capture throws InvalidOperationException; disposed access throws ObjectDisposedException. Mask changes do not acquire or release resources.

### GetVisibilityLayerBit

`public bool GetVisibilityLayerBit(int layer)`

Returns whether the zero-based bit 0 through 31 is set. Invalid indices throw ArgumentOutOfRangeException; the same query ownership/disposal guards as VisibilityLayer apply. Does not query actual on-screen visibility.

### SetVisibilityLayerBit

`public void SetVisibilityLayerBit(int layer, bool enabled)`

Sets or clears the zero-based bit 0 through 31, preserving every other bit. Invalid indices throw ArgumentOutOfRangeException before mutation. The same mutation guards and retained-command behavior as VisibilityLayer apply; repeating a value has no additional effect.

## Transform notification delivery

### ForceUpdateTransform

`public void ForceUpdateTransform()`

Requires a live item in an active tree on its owner thread. If this item has a pending global notification, removes it before dispatching NotificationTransformChanged and the typed TransformChanged event. Repeated calls without a pending entry do nothing. It neither flushes descendants nor forces a redraw. It does not resolve the global transform's invalidation flag: call GetGlobalTransform to read/resolve coordinates. A callback failure propagates with its entry already consumed; explicit reentrant forcing can consume a newly queued change.

Off-owner, detached or scene-capture calls throw InvalidOperationException. Disposed calls throw ObjectDisposedException. Delivery uses the scene execution barrier so callback attempts to dispose or run the tree recursively fail before state changes. Queue membership belongs to the current tree and is canceled on exit, disposal or failed activation.

```csharp
// Existing attached Entity; opt in before changing its placement.
entity.NotifyTransformChanges = true;
entity.Position += Vector2.One;
entity.ForceUpdateTransform();
```

### Global invalidation and cached queries

GetGlobalTransform computes and caches the direct canvas-parent composition until invalidated; neutral nodes and TopLevel end that chain. Querying resolves global invalidation without removing a queued notification. A setter invalidates the item and direct canvas descendants once; already-invalid items stop redundant traversal. Attached items with NotifyTransformChanges enabled queue once. Disabling the flag does not cancel an existing entry; re-enabling resolves the current transform so future writes can invalidate it again. Queries require the attached owner thread and reject disposed access.

Local notifications are attached-only, synchronous and enabled by NotifyLocalTransformChanges. Entity transform setters notify even when assigned an equal matrix. Global invalidation is committed first, so a local callback can read the new global transform or force the already queued notification. The typed events now project the numeric notifications, replacing the former unconditional synchronous event path.

[The component audit](../components/scene-hierarchy.md#transform-invalidation-and-delivery) records exact source semantics, safe phases, reentry policy and tests. Hidden, masked or processing-disabled canvas items can still receive queued notifications. No interpolation, physics backend or UI implementation is implied.

Retained screen regions now sample the same actual render transforms, layer/mask/clip/repetition and inherited alpha as submitted canvases. All states commit before queued screen events; failures continue later nodes and membership epochs reject stale delivery. [VisibleOnScreenNotifier](../classes/VisibleOnScreenNotifier.md) and [VisibleOnScreenEnabler](../classes/VisibleOnScreenEnabler.md) provide the current runtime API. Both Linux Wayland backends and 64 warmed active neutral-target transitions are verified by [ScreenVisibilityRenderingTests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs), under [ADR 0078](../decisions/rendering.md#adr-0078). Native allocations, other platforms, layered offscreen targets and editor gizmo drawing remain outside this verification; single-layer offscreen integration has separate SubViewportTests evidence.

## Font drawing

All text methods use the existing recording scope and delegate to the supplied borrowed [Font](Font.md). Position denotes the baseline in local canvas units. Paragraph shaping, fallback, Unicode scalar clusters, width/alignment, wrapping, outlines and oversampling follow the font contract. Text uses ordinary glyph textures, so inherited transform, modulation, sampling, material and clipping policies apply. The font must remain live while its recorded glyphs are consumed.

| Signature | Contract |
| --- | --- |
| `public void DrawChar(Font font, Vector2 position, string character, int fontSize = 16, Color? modulate = null, float oversampling = 0)` | [DrawChar](#drawchar) |
| `public void DrawCharOutline(Font font, Vector2 position, string character, int fontSize = 16, int size = -1, Color? modulate = null, float oversampling = 0)` | [DrawCharOutline](#drawcharoutline) |
| `public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawString](#drawstring) |
| `public void DrawStringOutline(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int size = 1, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawStringOutline](#drawstringoutline) |
| `public void DrawMultilineString(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawMultilineString](#drawmultilinestring) |
| `public void DrawMultilineStringOutline(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawMultilineStringOutline](#drawmultilinestringoutline) |

<a id="drawchar"></a>
### DrawChar

`public void DrawChar(Font font, Vector2 position, string character, int fontSize = 16, Color? modulate = null, float oversampling = 0)`

Draws one unshaped Unicode character during this item's recording.

`font`: The live borrowed font.; `position`: The finite local baseline position.; `character`: Exactly one Unicode scalar, encoded as one or two UTF-16 code units.; `fontSize`: Positive logical font size.; `modulate`: Finite color multiplier, or null for white.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The font or text is null.; `ArgumentException` — Text, geometry, color or an option is invalid.; `InvalidOperationException` — Called outside this item's recording scope or off the owner thread.; `ObjectDisposedException` — The node or required resource is disposed.

<a id="drawcharoutline"></a>
### DrawCharOutline

`public void DrawCharOutline(Font font, Vector2 position, string character, int fontSize = 16, int size = -1, Color? modulate = null, float oversampling = 0)`

Draws the outline of one unshaped Unicode character during this item's recording.

`font`: The live borrowed font.; `position`: The finite local baseline position.; `character`: Exactly one Unicode scalar, encoded as one or two UTF-16 code units.; `fontSize`: Positive logical font size.; `size`: Outline radius; nonpositive values draw an unexpanded glyph.; `modulate`: Finite color multiplier, or null for white.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The font or text is null.; `ArgumentException` — Text, geometry, color or an option is invalid.; `InvalidOperationException` — Called outside this item's recording scope or off the owner thread.; `ObjectDisposedException` — The node or required resource is disposed.

<a id="drawstring"></a>
### DrawString

`public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws one shaped line during this item's recording.

`font`: The live borrowed font.; `position`: The finite local baseline position.; `text`: The text to shape and draw.; `alignment`: Advance-axis alignment.; `width`: Available width; a negative value leaves width unconstrained.; `fontSize`: Positive logical font size.; `modulate`: Finite color multiplier, or null for white.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: The glyph advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The font or text is null.; `ArgumentException` — Text, geometry, color or an option is invalid.; `InvalidOperationException` — Called outside this item's recording scope or off the owner thread.; `ObjectDisposedException` — The node or required resource is disposed.

<a id="drawstringoutline"></a>
### DrawStringOutline

`public void DrawStringOutline(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int size = 1, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws the outline of one shaped line during this item's recording.

`font`: The live borrowed font.; `position`: The finite local baseline position.; `text`: The text to shape and draw.; `alignment`: Advance-axis alignment.; `width`: Available width; a negative value leaves width unconstrained.; `fontSize`: Positive logical font size.; `size`: Outline radius; nonpositive values draw an unexpanded glyph.; `modulate`: Finite color multiplier, or null for white.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: The glyph advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The font or text is null.; `ArgumentException` — Text, geometry, color or an option is invalid.; `InvalidOperationException` — Called outside this item's recording scope or off the owner thread.; `ObjectDisposedException` — The node or required resource is disposed.

<a id="drawmultilinestring"></a>
### DrawMultilineString

`public void DrawMultilineString(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws a shaped Unicode paragraph during this item's recording.

`font`: The live borrowed font.; `position`: The finite local baseline position.; `text`: The text to shape and draw.; `alignment`: Advance-axis alignment.; `width`: Available width; a negative value leaves width unconstrained.; `fontSize`: Positive logical font size.; `maxLines`: Maximum visible lines; a negative value draws every line.; `modulate`: Finite color multiplier, or null for white.; `breakFlags`: Unicode line-break rules.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: The glyph advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The font or text is null.; `ArgumentException` — Text, geometry, color or an option is invalid.; `InvalidOperationException` — Called outside this item's recording scope or off the owner thread.; `ObjectDisposedException` — The node or required resource is disposed.

<a id="drawmultilinestringoutline"></a>
### DrawMultilineStringOutline

`public void DrawMultilineStringOutline(Font font, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws the outline of a shaped Unicode paragraph during this item's recording.

`font`: The live borrowed font.; `position`: The finite local baseline position.; `text`: The text to shape and draw.; `alignment`: Advance-axis alignment.; `width`: Available width; a negative value leaves width unconstrained.; `fontSize`: Positive logical font size.; `maxLines`: Maximum visible lines; a negative value draws every line.; `size`: Outline radius; nonpositive values draw an unexpanded glyph.; `modulate`: Finite color multiplier, or null for white.; `breakFlags`: Unicode line-break rules.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: The glyph advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The font or text is null.; `ArgumentException` — Text, geometry, color or an option is invalid.; `InvalidOperationException` — Called outside this item's recording scope or off the owner thread.; `ObjectDisposedException` — The node or required resource is disposed.

[FontRenderingTests](../../tests/Electron2D.Tests/FontRenderingTests.cs) verifies all six entrypoints, independent raster pixels, fallback scripts, clipping, transforms, modulation and 64 warmed active frames without managed allocation on Linux Wayland GPU and compatibility. Other platforms, native allocator counts and owner acceptance remain unverified.

## Texture RID overloads

### DrawTexture with RID

`public void DrawTexture(RID texture, Vector2 position, Color? modulate = null)`

Resolves a live server-owned or resource-owned texture RID, then uses the corresponding Texture overload and its virtual drawing hook. Geometry, modulation, flips, transpose, UV clipping and owner/recording checks are unchanged.

### DrawTextureRect with RID

`public void DrawTextureRect(RID texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)`

Resolves a live server-owned or resource-owned texture RID, then uses the corresponding Texture overload and its virtual drawing hook. Geometry, modulation, flips, transpose, UV clipping and owner/recording checks are unchanged.

### DrawTextureRectRegion with RID

`public void DrawTextureRectRegion(RID texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)`

Resolves a live server-owned or resource-owned texture RID, then uses the corresponding Texture overload and its virtual drawing hook. Geometry, modulation, flips, transpose, UV clipping and owner/recording checks are unchanged.

These overloads borrow the resolved texture; they neither own nor free it. Empty/stale/wrong-kind identities throw ArgumentException. AtlasTexture.GetRID resolves the underlying source identity, so RID drawing uses that full texture; object-based atlas drawing retains its view. Empty ordinary resource drawing remains a no-op. Owned texture update/replacement changes pixels in retained commands without QueueRedraw. Freed/consumed server textures are skipped on subsequent replay; existing resource-disposal failure behavior is unchanged. Recorded geometry remains fixed after size changes. See [RenderingServer texture lifecycle](RenderingServer.md#texture-rid-verification) and [native/managed checks](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs).

RID texture proxies are resolved to their current live root at replay, so retargeting and source replacement update retained drawings without OnDraw. Disconnected proxies draw nothing and can reconnect; aliases share source backend storage. See [proxy integration](../components/canvas-rendering.md#texture-proxies).

## Retained mesh drawing

`DrawMesh(Mesh mesh, Texture? texture = null, Transform? transform = null, Color? modulate = null)` and `DrawMesh(RID mesh, RID texture = default, Transform? transform = null, Color? modulate = null)` record borrowed 2D surfaces during this item's drawing scope. Null transform/modulation mean identity/white. Resource RID resolution precedes the same typed path. Atlas views use the backing texture, matching primitive/full-image sampling. Live ArrayMesh updates remain visible without recording again; custom Mesh snapshots refresh on its Changed revision. Per-surface material overrides the item material; clip/order/sampling/transforms remain inherited. Mesh/resource disposal and unsupported backend shaders fail explicitly. See [mesh component](../components/meshes.md) for complete data/ownership/backend verification.

## Mesh methods

All methods require a live object; RenderingServer operations require the renderer owner thread. Server mutations additionally reject during submission or shutdown. Unknown/stale identities and surface indices reject before mutation; resource identities are borrowed and cannot be freed or mutated through the server.

| Complete signature | Contract |
| --- | --- |
| `public System.Void DrawMesh(Electron2D.Mesh mesh, Electron2D.Texture texture = null, System.Nullable<Electron2D.Transform> transform = null, System.Nullable<Electron2D.Color> modulate = null)` | Records a borrowed mesh with live surface geometry during canvas recording. |
| `public System.Void DrawMesh(Electron2D.RID mesh, Electron2D.RID texture = default, System.Nullable<Electron2D.Transform> transform = null, System.Nullable<Electron2D.Color> modulate = null)` | Records a borrowed mesh identity through the same typed canvas path. |

## Mesh method descriptions

### DrawMesh

`public System.Void DrawMesh(Electron2D.Mesh mesh, Electron2D.Texture texture = null, System.Nullable<Electron2D.Transform> transform = null, System.Nullable<Electron2D.Color> modulate = null)`

Summary: Records a borrowed mesh with live surface geometry during canvas recording.

mesh: Borrowed live two-dimensional mesh.

texture: Optional borrowed texture; atlas views use their full backing texture.

transform: Local mesh transform, identity when null.

modulate: Mesh color multiplier, white when null.

Remarks: Retained draws observe surface edits without rerecording. Per-surface materials override this item's material; transforms, clip, order, sampling and modulation remain inherited canvas policies.

System.ArgumentNullException: The mesh is null.

System.ArgumentException: Transform or color is nonfinite.

System.ObjectDisposedException: A borrowed resource is disposed.

System.InvalidOperationException: Called outside this item's recording scope.


### DrawMesh

`public System.Void DrawMesh(Electron2D.RID mesh, Electron2D.RID texture = default, System.Nullable<Electron2D.Transform> transform = null, System.Nullable<Electron2D.Color> modulate = null)`

Summary: Records a borrowed mesh identity through the same typed canvas path.

mesh: Logical live mesh RID.

texture: Optional live texture RID, empty for no texture.

transform: Local transform or identity.

modulate: Color multiplier or white.

System.ArgumentException: A supplied identity is absent or disposed.

## Instance integration

DrawMultiMesh records borrowed instance storage during OnDraw. Live packed edits replay through inherited canvas policies; raw custom fragment data, visible-prefix Rect2 culling and interpolation share the mesh path.

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `public System.Void DrawMultiMesh(Electron2D.MultiMesh multiMesh, Electron2D.Texture texture = null)` | Records a borrowed instance resource with live packed data and mesh surfaces. |
| `public System.Void DrawMultiMesh(Electron2D.RID multiMesh, Electron2D.RID texture = default)` | Records live instance and texture identities through the same retained path. |

## Methods and protected extension points descriptions

<a id="member-446522037feb"></a>
### DrawMultiMesh

`public System.Void DrawMultiMesh(Electron2D.MultiMesh multiMesh, Electron2D.Texture texture = null)`

Records a borrowed instance resource with live packed data and mesh surfaces.

multiMesh: Borrowed live instance storage.

texture: Optional borrowed surface texture.

Remarks: Replays the visible prefix in surface/instance order; transforms, instance colors, shader data and eligible physics interpolation are read during replay rather than copied into drawing commands.

System.InvalidOperationException: Called outside the drawing scope.

System.ObjectDisposedException: A supplied resource is disposed.

<a id="member-bfdc2f1ec175"></a>
### DrawMultiMesh

`public System.Void DrawMultiMesh(Electron2D.RID multiMesh, Electron2D.RID texture = default)`

Records live instance and texture identities through the same retained path.

multiMesh: Borrowed or owned logical instance identity.

texture: Optional live texture identity.

System.ArgumentException: An identity is missing or disposed.

Offscreen canvas selection and external CanvasLayer targets now choose their actual rendering viewport for default sampling and vertex snapping. Submitted masks/camera/final transforms and notifier bounds are per target. Native root visibility does not hide an independent offscreen canvas. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) for scope, tests and remaining prerequisites.

Retained drawing and mesh batching coalesce only ordinary Draw operations. Group/copy boundaries preserve order, and CanvasGroup consumes the same canvas transform/clip/mask/material path. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

## Alpha masks

`ClipChildren` uses [ClipChildrenMode](ClipChildrenMode.md) to select Disabled, Only or AndDraw. Only takes child/background color and this item's drawn alpha; AndDraw additionally draws the item before its children. Alpha includes command color, inherited modulation, SelfModulate and texture samples. Each overlapping owner primitive repeats alpha blending. Same-Z direct canvas ancestry participates; TopLevel, neutral nodes and independent canvases end capture. Parent Y-sort treats the mask as an atomic boundary and its internal Y-sort still executes. A mask without recorded commands leaves children ordinary; no captured same-Z child range leaves the owner ordinary. State-only recorded commands can capture children while supplying no drawable mask.

Only with a material uses that material's ordinary final owner shader, including SCREEN_TEXTURE over captured children. AndDraw keeps its built-in final mask; the material affects the early owner draw. Children and early owner draws cannot sample their writable screen buffer. CanvasGroup stores this policy without changing group behavior. BackBufferCopy inherits it; editor inspector hiding remains dependent on the first editor inspector slice. See [native composition](../components/canvas-rendering.md#canvas-alpha-masks) for exact capability and verification limits.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.ClipChildrenMode ClipChildren { get; set; }` | Gets or sets how drawn alpha masks same-Z canvas descendants. |

## Property Descriptions

<a id="member-476e9b667db3"></a>
### ClipChildren

`public Electron2D.ClipChildrenMode ClipChildren { get; set; }`

Gets or sets how drawn alpha masks same-Z canvas descendants.

Value: Disabled initially. Max is a sentinel and cannot be assigned.

Remarks: Only takes color from the children and alpha from this item's geometry, texture and tint. AndDraw additionally draws this item before children. Without recorded commands, children draw normally; without a captured same-Z child range, this item draws normally. CanvasGroup stores the policy but keeps its group compositor. Equal assignments are silent; changed assignments commit before warning refresh. Nested masks and groups share storage and cannot render together.

System.ArgumentOutOfRangeException: The mode is not an assignable enum value.

System.InvalidOperationException: Access is off-owner or mutation occurs during scene capture.

System.ObjectDisposedException: This node is disposed.

System.Exception: A warning-refresh observer throws after the value commits.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public override System.String[] GetConfigurationWarnings()` | Returns this node's current configuration warnings for tooling. |

## Method Descriptions

<a id="member-5ac3254f10f8"></a>
### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Returns this node's current configuration warnings for tooling.

Returns: A caller-owned snapshot including base warnings and, while attached and clipping (or a CanvasGroup), the first clipping ancestor and the first group ancestor. Physical Node ancestry is inspected even across neutral/TopLevel canvas boundaries. Disabled ordinary canvas items add no warnings.

Remarks: This query does not cache results, emit events or require an edited scene. Attached queries run on the scene owner thread. Consumers may call it after NodeConfigurationWarningChanged to refresh their display.

System.InvalidOperationException: An attached query runs off the scene owner thread.

System.ObjectDisposedException: This node is disposed.

## CPU particle commands

CPUParticles records ordinary quads with internal custom phase/lifetime channels. AppendCanvas reads current CanvasItemMaterial sheet settings on replay, keeping paused material changes visible. An internal world-coordinate drawing flag separates these quads from the emitter's logical transform; the renderer preserves its viewport/CanvasLayer basis, ordinary child inheritance and clipping. Complete interpolation tick hooks let this consumer track global emitter poses even when its own interpolation mode is Off. Existing public drawing signatures and transform queries retain their contracts; native and prepared evidence is in [CPU particles](../components/cpu-particles.md).

## Mesh skin API

| Complete signature | Contract |
| --- | --- |
| `public Electron2D.RID GetCanvasItem()` | Returns this scene-owned stable weak canvas identity, independent of native startup. |

## Mesh skin member descriptions

### GetCanvasItem

`public Electron2D.RID GetCanvasItem()`

Returns this scene-owned stable weak canvas identity, independent of native startup.

Returns: A borrowed RID valid until node disposal; renderer FreeRID cannot release scene ownership.

Throws `System.InvalidOperationException`: An attached node is read off-owner.

Throws `System.ObjectDisposedException`: The node is disposed.

The [mesh component](../components/meshes.md#server-palettes-and-foureight-skin-records) owns the actual storage, coordinate, lifetime, callback, archive and verification contract. New palette API uses the existing native-service availability/owner gate. Headless retained geometry and native rendered/backend acceptance remain separately recorded.

## Server canvas integration

`GetCanvas()` returns the borrowed canvas selected by authored tree membership, or empty while detached. Server state attached to `GetCanvasItem()` changes native replay independently of authored properties; matching source setters and redraw republish their own fields/commands. See [the executable canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items).

## Low-level primitive integration

Native indexed triangle and primitive commands replay through existing storage. Command clip-ignore switches scissor state without changing authored properties; native draw-index and visibility-mask setters likewise remain separate. Matching source structure/mask edits republish their own fields. See [the executable primitive contract](../components/canvas-rendering.md#low-level-primitive-commands).

## Shader and material RID integration

[The program RID contract](../components/shader-materials.md#shader-and-material-identities) documents caller-owned create/set/query/free operations and borrowed resource identities. `CanvasItemSetMaterial` changes native material state independently of authored `Material`; its matching source setter republishes the authored reference. Shader replacement and typed uniform/texture updates reuse retained commands.

`public const int NotificationWorldChanged = 36` reports committed world association; GetWorld observes the new canvas and physics space when it is delivered.
