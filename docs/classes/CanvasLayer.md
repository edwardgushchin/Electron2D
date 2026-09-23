# CanvasLayer

Last updated: 2026-09-23

- Declaration: `public class CanvasLayer : Node`
- Source: [CanvasLayer.cs](../../src/Scene/Main/CanvasLayer.cs)
- Inherits: [Node](Node.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#canvas-layers)

## Description

A neutral scene node that selects an independent canvas for its descendants. It has no drawing commands or Entity transform inheritance. Nested layers are independent. CanvasItem.GetCanvasLayerNode returns the nearest active layer, searching across neutral and TopLevel nodes and stopping at a Viewport. Reparenting and actual tree entry/exit update membership; manual notification delivery cannot fake it.

Layer order precedes all item Z values. Canvases at the same layer remain separate groups: siblings use their current index, and otherwise equal keys use process-local canvas identity. Do not rely on an order between unrelated equal-layer canvases. The default canvas has layer/index zero. Z, behind-parent and Y ordering operate within each canvas. A layer under a hidden CanvasItem remains independent of that item's visibility and transforms.

Visible changes emit the layer event first, then reconcile direct CanvasItem children and their direct canvas descendants, including TopLevel. A neutral Node interrupts visibility propagation, although it does not interrupt canvas membership. Nested CanvasLayer visibility is independent. Equal writes are silent. Callback failures are aggregated after child reconciliation; callbacks may reenter, remove or dispose children and the latest committed state is used. Hidden windows suppress framebuffer submission; direct layer children retain the layer visibility contract for queries.

Transform stores a complete finite matrix, including shear and singular bases. Component queries decompose it lazily, representing negative determinant on the Y scale. Component writes rebuild rotation and scale without shear, including equal writes. Explicit Rotation values retain their turns until matrix decomposition. PackedScene stores the canonical Transform rather than redundant components; runtime viewport references, subscriptions and canvas membership are never packed.

All attached access uses the scene owner thread. Mutations reject capture and disposed state. Nonfinite configuration is rejected before commit; derived composition/decomposition overflow fails explicitly. Singular matrices are legal for drawing; inverse coordinate queries throw. Actual layer activation requires an active viewport in the same tree; failed activation uses ordinary Node rollback. Disposal releases owned children and borrowed references without owning a viewport or native graphics resource.

## Example

Configure the root Window before Engine.Run; Sprite descendants use this layer instead of the camera's default canvas:

```csharp
var window = new Window();
var hud = new CanvasLayer { Name = "HUD", Layer = 2, Offset = new Vector2(12, 12) };
window.AddChild(hud);
// Attach sprites or custom CanvasItem descendants to hud.
```

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public CanvasLayer()`](#constructor) | Creates a visible layer at index one with an identity transform and no viewport following. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public int Layer { get; set; }`](#layer) | Gets or sets the layer's primary drawing order. |
| [`public bool Visible { get; set; }`](#visible) | Gets or sets whether direct canvas children and their canvas descendants are visible. |
| [`public Transform Transform { get; set; }`](#transform) | Gets or sets the layer transform before viewport following. |
| [`public Vector2 Offset { get; set; }`](#offset) | Gets or sets the layer offset in canvas units. |
| [`public float Rotation { get; set; }`](#rotation) | Gets or sets the layer rotation in radians. |
| [`public Vector2 Scale { get; set; }`](#scale) | Gets or sets the layer scale. |
| [`public bool FollowViewportEnabled { get; set; }`](#followviewportenabled) | Gets or sets whether this layer follows the viewport's default canvas transform. |
| [`public float FollowViewportScale { get; set; }`](#followviewportscale) | Gets or sets the scale applied when viewport following is enabled. |
| [`public Node? CustomViewport { get; set; }`](#customviewport) | Gets or sets the borrowed viewport used instead of the containing viewport. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public Transform GetFinalTransform()`](#getfinaltransform) | Returns the logical transform from layer to viewport coordinates. |
| [`public void Show()`](#show) | Shows the layer's canvas children by setting Visible to true. |
| [`public void Hide()`](#hide) | Hides the layer's canvas children by setting Visible to false. |

## Events

| Declaration | Contract |
| --- | --- |
| [`public event Action<CanvasLayer>? VisibilityChanged`](#visibilitychanged) | Occurs after the visibility value changes and before child visibility propagation. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends typed layer properties. Transform is the canonical stored matrix; Offset/Rotation/Scale are non-stored views. CustomViewport is borrowed runtime state. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Supplies a static exact-type CanvasLayer factory for PackedScene. Derived types follow the inherited factory contract. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Disposes owned children through Node; finally clears subscriptions and borrowed viewport references without disposing targets. |

## Constructor Descriptions

<a id="constructor"></a>
### CanvasLayer

`public CanvasLayer()`

Creates a visible layer at index one with an identity transform and no viewport following.

## Property Descriptions

<a id="layer"></a>
### Layer

`public int Layer { get; set; }`

Gets or sets the layer's primary drawing order.

**Value:** One by default; the inclusive range is RenderingServer.CanvasLayerMin through RenderingServer.CanvasLayerMax. Lower layers draw first, regardless of item Z.

**Remarks:** Equal layers remain separate groups. Sibling order breaks ties; ordering between unrelated equal-index layers is not a portable guarantee. Changes affect retained drawing on the next frame.

**InvalidOperationException:** Access is off-owner or mutation occurs during scene capture.

**ObjectDisposedException:** The layer is disposed.

<a id="visible"></a>
### Visible

`public bool Visible { get; set; }`

Gets or sets whether direct canvas children and their canvas descendants are visible.

**Value:** True initially. Nested CanvasLayer and neutral Node boundaries do not inherit this flag.

**Remarks:** Commits the value, raises VisibilityChanged, then propagates to direct canvas children. All affected children are attempted even when callbacks fail. Reentrant changes use the latest value.

**InvalidOperationException:** Access is off-owner or mutation occurs during scene capture.

**AggregateException:** One or more visibility callbacks fail after the change.

**ObjectDisposedException:** The layer is disposed.

<a id="transform"></a>
### Transform

`public Transform Transform { get; set; }`

Gets or sets the layer transform before viewport following.

**Value:** Identity initially; finite singular and skewed transforms are accepted.

**Remarks:** Component queries lazily decompose this matrix. Setting Offset, Rotation or Scale afterward reconstructs rotation and scale, discarding skew. PackedScene stores this canonical matrix.

**ArgumentException:** The matrix is nonfinite.

**InvalidOperationException:** Access is off-owner or mutation occurs during scene capture.

**ObjectDisposedException:** The layer is disposed.

<a id="offset"></a>
### Offset

`public Vector2 Offset { get; set; }`

Gets or sets the layer offset in canvas units.

**Value:** Zero initially.

**Remarks:** Setting a component rebuilds the matrix without skew, even when the component value is unchanged.

**ArgumentException:** The value or rebuilt matrix is nonfinite.

**InvalidOperationException:** Access is off-owner, mutation occurs during scene capture, or component decomposition overflows.

**ObjectDisposedException:** The layer is disposed.

<a id="rotation"></a>
### Rotation

`public float Rotation { get; set; }`

Gets or sets the layer rotation in radians.

**Value:** Zero initially. Explicit finite angles are not normalized until matrix decomposition is needed.

**ArgumentException:** The value or rebuilt matrix is nonfinite.

**InvalidOperationException:** Access is off-owner, mutation occurs during scene capture, or component decomposition overflows.

**ObjectDisposedException:** The layer is disposed.

<a id="scale"></a>
### Scale

`public Vector2 Scale { get; set; }`

Gets or sets the layer scale.

**Value:** One on both axes initially; finite zero and negative values are accepted.

**Remarks:** Matrix decomposition represents a reflection on the Y axis. Zero scale renders degenerate geometry; coordinate queries requiring an inverse then fail explicitly.

**ArgumentException:** The value or rebuilt matrix is nonfinite.

**InvalidOperationException:** Access is off-owner, mutation occurs during scene capture, or component decomposition overflows.

**ObjectDisposedException:** The layer is disposed.

<a id="followviewportenabled"></a>
### FollowViewportEnabled

`public bool FollowViewportEnabled { get; set; }`

Gets or sets whether this layer follows the viewport's default canvas transform.

**Value:** False initially, keeping the layer fixed on screen independently of a camera.

**InvalidOperationException:** Access is off-owner or mutation occurs during scene capture.

**ObjectDisposedException:** The layer is disposed.

<a id="followviewportscale"></a>
### FollowViewportScale

`public float FollowViewportScale { get; set; }`

Gets or sets the scale applied when viewport following is enabled.

**Value:** One initially; any finite value, including zero and negative values, is accepted.

**Remarks:** Rendering scales around the viewport center; GetFinalTransform describes logical coordinates using a scale before the layer transform. These differ for nonunit follow scale.

**ArgumentException:** The scale is nonfinite.

**InvalidOperationException:** Access is off-owner or mutation occurs during scene capture.

**ObjectDisposedException:** The layer is disposed.

<a id="customviewport"></a>
### CustomViewport

`public Node? CustomViewport { get; set; }`

Gets or sets the borrowed viewport used instead of the containing viewport.

**Value:** Null initially. Null or a non-Viewport node restores the containing viewport.

**Remarks:** Runtime-only, not packed. Attached targets must be active in the same tree. Independent native windows, embedded viewports and offscreen targets are not integrated with the renderer.

**NotSupportedException:** The target is not active in this tree.

**InvalidOperationException:** Mutation is unavailable or no containing viewport exists.

**ObjectDisposedException:** The layer or assigned node is disposed.

## Method Descriptions

<a id="getfinaltransform"></a>
### GetFinalTransform

`public Transform GetFinalTransform()`

Returns the logical transform from layer to viewport coordinates.

**Returns:** The layer transform when not following; otherwise viewport CanvasTransform times follow scale times layer transform. A detached layer uses identity in place of the viewport transform.

**Remarks:** Excludes GlobalCanvasTransform, pixel snapping and the renderer's center-based follow scaling.

**InvalidOperationException:** The query is off-owner or composition overflows finite coordinates.

**ObjectDisposedException:** The layer is disposed.

<a id="show"></a>
### Show

`public void Show()`

Shows the layer's canvas children by setting Visible to true.

**InvalidOperationException:** Mutation is unavailable.

**AggregateException:** A visibility callback fails after the change.

**ObjectDisposedException:** The layer is disposed.

<a id="hide"></a>
### Hide

`public void Hide()`

Hides the layer's canvas children by setting Visible to false.

**InvalidOperationException:** Mutation is unavailable.

**AggregateException:** A visibility callback fails after the change.

**ObjectDisposedException:** The layer is disposed.

## Event Descriptions

<a id="visibilitychanged"></a>
### VisibilityChanged

`public event Action<CanvasLayer>? VisibilityChanged`

Occurs after the visibility value changes and before child visibility propagation.

**Remarks:** Synchronous on the scene owner while attached; equal assignments do not emit. Handler failure stops later handlers but does not prevent child state from being reconciled.

## Protected Hook Descriptions

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends typed layer properties. Transform is the canonical stored matrix; Offset/Rotation/Scale are non-stored views. CustomViewport is borrowed runtime state.

<a id="createsceneinstancefactory"></a>
### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Supplies a static exact-type CanvasLayer factory for PackedScene. Derived types follow the inherited factory contract.

<a id="dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Disposes owned children through Node; finally clears subscriptions and borrowed viewport references without disposing targets.

**Remarks:** Disposes owned children, then clears subscriptions and borrowed viewport references.

## Transform and source audit

The pinned [canvas_layer.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/canvas_layer.cpp), [canvas_item.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/canvas_item.cpp) and [renderer_viewport.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_viewport.cpp) distinguish logical queries from the render transform. Let `L` be the layer matrix, `C` the viewport camera/default canvas matrix, `G` the viewport final matrix, `S` uniform FollowViewportScale, `P` translation to the viewport center, and `F` framebuffer density scaling.

| Mode | GetFinalTransform / canvas-local queries | Rendering before item transforms |
| --- | --- | --- |
| Following disabled | `L` | `F * G * L` |
| Following enabled | `C * S * L` | `F * P * S * inverse(P) * G * C * L` |

For nonunit follow scale these intentionally differ, preserving both pinned behaviors. Input localization uses the logical query, including this difference; it does not infer a new inverse from framebuffer pixels. At scale one, camera-relative rendering and canvas input agree. The logical query excludes G and snapping; detached layers have no active C and use identity instead of retaining a stale viewport reference.

Transform snapping rounds each canvas translation using `ceil(position + bias)`, with bias -0.5 for even viewport dimensions and zero for odd dimensions. A following parent canvas is rounded after multiplying translation by S, then divided by S. Scale zero skips that division and yields degenerate geometry at the viewport center. Item translations still use their existing nearest-pixel rounding. Both the default canvas and layers use this shared viewport preparation; source Matrix/Entity values remain unchanged.

Typed exceptions replace native error fallthrough and reject arithmetic overflow. Null CustomViewport follows the XML-documented fallback, while the pinned native setter rejects null; non-Viewport Node also selects the default. Canonical matrix packing preserves shear instead of independently replaying redundant component properties. These adaptations use the existing typed ownership and scene-storage decisions.

## Verification and remaining dependencies

[CanvasLayerTests](../../tests/Electron2D.Tests/CanvasLayerTests.cs) covers defaults, decomposition, signed/singular transforms, packing, owner/capture/disposal guards, callback failure/reentry, nested/neutral/TopLevel membership, input copies, finite overflow, even/odd viewport snapping and zero-allocation queries. [CanvasLayerRenderingTests](../../tests/Electron2D.Tests/CanvasLayerRenderingTests.cs) covers six ordering/visibility frames and eight transform/camera/input/snapping frames per path. Native checks passed on Linux Wayland compatibility/GPU including HLSL/GLSL, and dummy/software; warmed layer rendering allocates zero managed bytes in the measured interval. Physical input, visual owner acceptance and other platforms are not claimed.

GetCanvas's opaque identity awaits the first backend-neutral renderer resource-identity/lifetime slice, together with CanvasItem.GetCanvas and the corresponding low-level server operations. No fake identity is exposed. CustomViewport remains partial until independent/offscreen/nested viewport rendering and multiple native windows are implemented. Editor UI and embedded-window stacking require those actual capabilities; layer 1024 alone does not implement them. The class remains Partial in coverage for these dependencies. Existing Node gaps remain recorded on their declaring type.

CanvasItem visibility masks inside every layer are tested against the destination Viewport.CanvasCullMask. The layer starts an independent canvas group, so masks on scene ancestors outside that group do not suppress it. Layer Visible remains a separate logical condition; see [mask culling](../components/canvas-rendering.md#canvas-visibility-masks).
