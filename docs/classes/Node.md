# Node

Last updated: 2026-09-23

**Inherits:** [CanvasItem](CanvasItem.md)

**Inherited By:** [Sprite](Sprite.md)

- **Source:** [Node.cs](../../src/Scene/2D/Node.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Node : CanvasItem`

## Description

A concrete spatial canvas item with engine-owned Vector2 and Transform values. Provides local/global position, rotation, scale and skew, spatial helpers and an identity default transform. It can be an empty spatial parent. Hierarchy, lifecycle and processing are inherited from SceneNode; drawing, visibility, Z and materials come from CanvasItem. A neutral parent resets the canvas transform chain. Control will be a sibling under CanvasItem, with its own rectangle/layout model.

## Examples

The snippet uses the Electron2D namespace; attach the hierarchy to a SceneTree or an Engine.Run window to activate it.

```csharp
using var parent = new Node { Position = new Vector2(20, 10) };
var child = new Sprite { Name = "Sprite", Position = new Vector2(4, 2) };
parent.AddChild(child);
Vector2 worldPosition = child.GlobalPosition; // (24, 12)
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public Node()`](#m-electron2d-node-ctor) | Creates a detached spatial node with an identity transform. |

## Properties

| Member | Contract |
| --- | --- |
| [`public Vector2 GlobalPosition { get; set; }`](#p-electron2d-node-globalposition) | Gets or sets translation in hierarchy-global coordinates. |
| [`public float GlobalRotation { get; set; }`](#p-electron2d-node-globalrotation) | Gets or sets hierarchy-global rotation in radians. |
| [`public float GlobalRotationDegrees { get; set; }`](#p-electron2d-node-globalrotationdegrees) | Gets or sets hierarchy-global rotation in degrees. |
| [`public Vector2 GlobalScale { get; set; }`](#p-electron2d-node-globalscale) | Gets or sets hierarchy-global scale. |
| [`public float GlobalSkew { get; set; }`](#p-electron2d-node-globalskew) | Gets or sets the hierarchy-global skew angle in radians. |
| [`public Transform GlobalTransform { get; set; }`](#p-electron2d-node-globaltransform) | Gets or sets the affine transform in hierarchy-global coordinates. |
| [`public Vector2 Position { get; set; }`](#p-electron2d-node-position) | Gets or sets local translation in pixels or other host-defined 2D units. |
| [`public float Rotation { get; set; }`](#p-electron2d-node-rotation) | Gets or sets local rotation in radians. |
| [`public float RotationDegrees { get; set; }`](#p-electron2d-node-rotationdegrees) | Gets or sets local rotation in degrees. |
| [`public Vector2 Scale { get; set; }`](#p-electron2d-node-scale) | Gets or sets local scale. |
| [`public float Skew { get; set; }`](#p-electron2d-node-skew) | Gets or sets the local skew angle in radians. |
| [`public Transform Transform { get; set; }`](#p-electron2d-node-transform) | Gets or sets the affine transform relative to the parent. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public void ApplyScale(Vector2 ratio)`](#m-electron2d-node-applyscale-electron2d-vector2) | Component-multiplies the local scale by a ratio. |
| [`protected override Func<SceneNode> CreateSceneInstanceFactory()`](#m-electron2d-node-createsceneinstancefactory) | Returns a static exact-type factory for Node. A derived type must provide its own factory to support packing. |
| [`public float GetAngleTo(Vector2 globalPoint)`](#m-electron2d-node-getangleto-electron2d-vector2) | Computes the signed local angle toward a global point, compensating for local scale. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-node-getpropertydescriptors) | Extends canvas descriptors with position, rotation in degrees, scale and skew. |
| [`public Transform GetRelativeTransformToParent(SceneNode parent)`](#m-electron2d-node-getrelativetransformtoparent-electron2d-scenenode) | Returns the product of local transforms up to a spatial ancestor. |
| [`public override Transform GetTransform()`](#m-electron2d-node-gettransform) | Returns the current local Transform through the common canvas query contract. |
| [`public void GlobalTranslate(Vector2 offset)`](#m-electron2d-node-globaltranslate-electron2d-vector2) | Moves this node by a hierarchy-global offset. |
| [`public void LookAt(Vector2 globalPoint)`](#m-electron2d-node-lookat-electron2d-vector2) | Rotates this node so its positive local X direction points at a global point. |
| [`public void MoveLocalX(float delta, bool scaled = false)`](#m-electron2d-node-movelocalx-system-single-system-boolean) | Moves this node along its local X basis axis. |
| [`public void MoveLocalY(float delta, bool scaled = false)`](#m-electron2d-node-movelocaly-system-single-system-boolean) | Moves this node along its local Y basis axis. |
| [`public override void Reparent(SceneNode newParent, bool keepGlobalTransform = true)`](#m-electron2d-node-reparent-electron2d-scenenode-system-boolean) | Moves under any SceneNode. With keepGlobalTransform enabled, validates the destination canvas inverse before mutation and restores spatial state after attachment, including failed callbacks. Structural lifecycle guards still apply. |
| [`public void Rotate(float radians)`](#m-electron2d-node-rotate-system-single) | Adds an angle to the local rotation. |
| [`public Vector2 ToGlobal(Vector2 localPoint)`](#m-electron2d-node-toglobal-electron2d-vector2) | Transforms a point from this node's local coordinates to hierarchy-global coordinates. |
| [`public Vector2 ToLocal(Vector2 globalPoint)`](#m-electron2d-node-tolocal-electron2d-vector2) | Transforms a point from hierarchy-global coordinates to this node's local coordinates. |
| [`public void Translate(Vector2 offset)`](#m-electron2d-node-translate-electron2d-vector2) | Adds an offset to this node's position in its parent coordinate space. |

## Constructor Descriptions

<a id="m-electron2d-node-ctor"></a>
### `public Node()`

Creates a detached spatial node with an identity transform.

## Property Descriptions

<a id="p-electron2d-node-globalposition"></a>
### `public Vector2 GlobalPosition { get; set; }`

Gets or sets translation in hierarchy-global coordinates.

**Value:** The translation component of `Node.GlobalTransform`.

**System.ArgumentOutOfRangeException:** An assigned component is NaN or infinite.

**System.InvalidOperationException:** The parent transform is singular, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the position changes.

<a id="p-electron2d-node-globalrotation"></a>
### `public float GlobalRotation { get; set; }`

Gets or sets hierarchy-global rotation in radians.

**Value:** The canonical rotation decomposed from `Node.GlobalTransform`.

**System.ArgumentOutOfRangeException:** The assigned angle is NaN or infinite.

**System.InvalidOperationException:** The parent transform is singular, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-globalrotationdegrees"></a>
### `public float GlobalRotationDegrees { get; set; }`

Gets or sets hierarchy-global rotation in degrees.

**Value:** `Node.GlobalRotation` converted between radians and degrees.

**System.ArgumentOutOfRangeException:** The assigned angle is NaN or infinite.

**System.InvalidOperationException:** The parent transform is singular, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-globalscale"></a>
### `public Vector2 GlobalScale { get; set; }`

Gets or sets hierarchy-global scale.

**Value:** The canonical scale decomposed from `Node.GlobalTransform`.

**Remarks:** Equivalent reflected matrices can decompose to a different but equivalent rotation, scale, and skew tuple.

**System.ArgumentOutOfRangeException:** An assigned component is NaN or infinite.

**System.InvalidOperationException:** The parent transform is singular, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the scale changes.

<a id="p-electron2d-node-globalskew"></a>
### `public float GlobalSkew { get; set; }`

Gets or sets the hierarchy-global skew angle in radians.

**Value:** The canonical skew decomposed from `Node.GlobalTransform`.

**System.ArgumentOutOfRangeException:** The assigned angle is NaN or infinite.

**System.InvalidOperationException:** The parent transform is singular, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the skew changes.

<a id="p-electron2d-node-globaltransform"></a>
### `public Transform GlobalTransform { get; set; }`

Gets or sets the affine transform in hierarchy-global coordinates.

**Value:** The local transform composed with non-top-level ancestors.

**System.ArgumentOutOfRangeException:** An assigned transform component is NaN or infinite.

**System.InvalidOperationException:** The parent transform is singular, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the transform changes.

<a id="p-electron2d-node-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets local translation in pixels or other host-defined 2D units.

**Value:** The translation component of `Node.Transform`.

**System.ArgumentOutOfRangeException:** An assigned component is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the position changes.

<a id="p-electron2d-node-rotation"></a>
### `public float Rotation { get; set; }`

Gets or sets local rotation in radians.

**Value:** The canonical rotation decomposed from `Node.Transform`.

**System.ArgumentOutOfRangeException:** The assigned angle is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-rotationdegrees"></a>
### `public float RotationDegrees { get; set; }`

Gets or sets local rotation in degrees.

**Value:** `Node.Rotation` converted between radians and degrees.

**System.ArgumentOutOfRangeException:** The assigned angle is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-scale"></a>
### `public Vector2 Scale { get; set; }`

Gets or sets local scale.

**Value:** The canonical scale decomposed from `Node.Transform`.

**Remarks:** Equivalent reflected matrices can decompose to a different but equivalent rotation, scale, and skew tuple.

**System.ArgumentOutOfRangeException:** An assigned component is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the scale changes.

<a id="p-electron2d-node-skew"></a>
### `public float Skew { get; set; }`

Gets or sets the local skew angle in radians.

**Value:** The canonical angle between the transformed basis axes relative to an unskewed basis.

**System.ArgumentOutOfRangeException:** The assigned angle is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the skew changes.

<a id="p-electron2d-node-transform"></a>
### `public Transform Transform { get; set; }`

Gets or sets the affine transform relative to the parent.

**Value:** A finite `Transform`; the default is `Transform.Identity`.

**System.ArgumentOutOfRangeException:** An assigned transform component is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated from a thread other than the tree owner.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the transform changes.

## Method Descriptions

<a id="m-electron2d-node-applyscale-electron2d-vector2"></a>
### `public void ApplyScale(Vector2 ratio)`

Component-multiplies the local scale by a ratio.

**Parameter `ratio`:** The finite X and Y scale ratios.

**System.ArgumentOutOfRangeException:** A ratio component is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the scale changes.

<a id="m-electron2d-node-createsceneinstancefactory"></a>
### `protected override Func<SceneNode> CreateSceneInstanceFactory()`

Returns a static exact-type factory for Node. A derived type must provide its own factory to support packing.

<a id="m-electron2d-node-getangleto-electron2d-vector2"></a>
### `public float GetAngleTo(Vector2 globalPoint)`

Computes the signed local angle toward a global point, compensating for local scale.

**Parameter `globalPoint`:** The finite point in hierarchy-global coordinates.

**Returns:** The angle of the inverse-transformed point multiplied by local scale, in radians.

**System.InvalidOperationException:** The global transform is singular.

**System.ArgumentOutOfRangeException:** A point component is NaN or infinite.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends canvas descriptors with position, rotation in degrees, scale and skew.

<a id="m-electron2d-node-getrelativetransformtoparent-electron2d-scenenode"></a>
### `public Transform GetRelativeTransformToParent(SceneNode parent)`

Returns the product of local transforms up to a spatial ancestor.

**Parameter `parent`:** This node itself or an ancestor connected through spatial nodes.

**Returns:** Identity for this node; otherwise the ordered product of local spatial transforms up to the ancestor.

**Remarks:** TopLevel does not interrupt this query. Neutral and non-spatial canvas ancestors do interrupt it; no matrix inversion is needed.

**System.ArgumentNullException:** `parent` is `null`.

**System.ArgumentException:** `parent` is not connected by an uninterrupted spatial-parent chain.

**System.ObjectDisposedException:** This node, `parent`, or a queried ancestor is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-gettransform"></a>
### `public override Transform GetTransform()`

Returns the current local Transform through the common canvas query contract.

<a id="m-electron2d-node-globaltranslate-electron2d-vector2"></a>
### `public void GlobalTranslate(Vector2 offset)`

Moves this node by a hierarchy-global offset.

**Parameter `offset`:** The finite global-space offset.

**System.ArgumentOutOfRangeException:** An offset component is NaN or infinite.

**System.InvalidOperationException:** The global transform is singular, or mutation occurs off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the position changes.

<a id="m-electron2d-node-lookat-electron2d-vector2"></a>
### `public void LookAt(Vector2 globalPoint)`

Rotates this node so its positive local X direction points at a global point.

**Parameter `globalPoint`:** The finite target point in hierarchy-global coordinates.

**Remarks:** A target equal to `Node.GlobalPosition` leaves rotation unchanged.

**System.ArgumentOutOfRangeException:** A point component is NaN or infinite.

**System.InvalidOperationException:** The global transform is singular, or mutation occurs off the owner thread.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the rotation changes.

<a id="m-electron2d-node-movelocalx-system-single-system-boolean"></a>
### `public void MoveLocalX(float delta, bool scaled = false)`

Moves this node along its local X basis axis.

**Parameter `delta`:** The finite signed distance.

**Parameter `scaled`:** Whether scale magnitude is retained. By default the axis is normalized.

**Remarks:** A near-zero normalized axis causes no movement.

**System.ArgumentOutOfRangeException:** `delta` is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the position changes.

<a id="m-electron2d-node-movelocaly-system-single-system-boolean"></a>
### `public void MoveLocalY(float delta, bool scaled = false)`

Moves this node along its local Y basis axis.

**Parameter `delta`:** The finite signed distance.

**Parameter `scaled`:** Whether scale magnitude is retained. By default the axis is normalized.

**Remarks:** A near-zero normalized axis causes no movement.

**System.ArgumentOutOfRangeException:** `delta` is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the position changes.

<a id="m-electron2d-node-reparent-electron2d-scenenode-system-boolean"></a>
### `public override void Reparent(SceneNode newParent, bool keepGlobalTransform = true)`

Moves under any SceneNode. With keepGlobalTransform enabled, validates the destination canvas inverse before mutation and restores spatial state after attachment, including failed callbacks. Structural lifecycle guards still apply.

<a id="m-electron2d-node-rotate-system-single"></a>
### `public void Rotate(float radians)`

Adds an angle to the local rotation.

**Parameter `radians`:** The finite angle in radians.

**System.ArgumentOutOfRangeException:** `radians` is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the rotation changes.

<a id="m-electron2d-node-toglobal-electron2d-vector2"></a>
### `public Vector2 ToGlobal(Vector2 localPoint)`

Transforms a point from this node's local coordinates to hierarchy-global coordinates.

**Parameter `localPoint`:** The finite local point.

**Returns:** The point transformed by `Node.GlobalTransform`.

**System.ArgumentOutOfRangeException:** A point component is NaN or infinite.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-tolocal-electron2d-vector2"></a>
### `public Vector2 ToLocal(Vector2 globalPoint)`

Transforms a point from hierarchy-global coordinates to this node's local coordinates.

**Parameter `globalPoint`:** The finite global point.

**Returns:** The point transformed by the inverse global transform.

**System.ArgumentOutOfRangeException:** A point component is NaN or infinite.

**System.InvalidOperationException:** The global transform is singular.

**System.ObjectDisposedException:** This node or an ancestor is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-translate-electron2d-vector2"></a>
### `public void Translate(Vector2 offset)`

Adds an offset to this node's position in its parent coordinate space.

**Parameter `offset`:** The finite local-space offset.

**Remarks:** Scale and skew do not affect the offset.

**System.ArgumentOutOfRangeException:** An offset component is NaN or infinite.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.Exception:** A transform notification or event handler throws after the position changes.

## Ownership, errors and dependencies

The parent owns its children; SceneTree owns the active root. PackedScene capture uses explicit stored descriptors and static exact-type factories. Scene-local resources belong to the instantiated root; externally supplied textures/materials are borrowed. Mutations honor scene capture, lifetime and owner-thread guards. Callback failures are reported after the documented committed state; cleanup attempts every owned stage. See [SceneNode](SceneNode.md) for inherited lifecycle and [the scene hierarchy component](../components/scene-hierarchy.md) for cross-layer flow.

Spatial inputs must be finite. Operations that invert a singular transform fail before changing that transform. Translate adds in parent coordinates; MoveLocalX/Y use the current basis. GetRelativeTransformToParent multiplies an uninterrupted spatial-parent chain independently of TopLevel. Matrix decomposition canonicalizes equivalent negative-scale/rotation tuples.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) verifies inheritance, neutral API boundaries, direct custom CanvasItem transforms, mixed parenting, notifications, timer/tween scheduling, packed factories/state, deletion and failure continuation. Existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) retain lifecycle, input, math and ownership coverage. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies mixed-tree pixels and a direct CanvasItem drawing texture through both GPU and compatibility backends on Linux Wayland. This does not establish visual owner acceptance or other platforms.

The hierarchy is implemented; complete reference API parity is not claimed. Missing GUI, canvas policies, rendering primitives, interpolation, scene-file authoring and other capabilities remain classified per member in [coverage](../coverage/index.md). No inert compatibility members are added.

## Relevant decisions

- [0008: SceneNode, CanvasItem and Node](../decisions/scene.md#adr-0008)
- [0004: Product scope and API correspondence](../decisions/product.md#adr-0004)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)
