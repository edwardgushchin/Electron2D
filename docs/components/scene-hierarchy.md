# Scene hierarchy component

Last updated: 2026-09-23

## Scope and owned types

The accepted hierarchy is implemented under [ADR 0008](../decisions/scene.md#adr-0008). One ordered SceneNode tree hosts neutral logic, canvas objects and spatial gameplay objects.

| Type | Base | Responsibility |
| --- | --- | --- |
| [SceneNode](../classes/SceneNode.md) | ElectronObject | Hierarchy, lifecycle, paths/groups, process/input callbacks, ownership and deletion. |
| [CanvasItem](../classes/CanvasItem.md) | SceneNode | Abstract drawing base, visibility, Z, modulation/materials and transform queries/notifications. |
| [Node](../classes/Node.md) | CanvasItem | Concrete position, rotation, scale, skew and spatial helpers. |
| [NodeProcessMode](../classes/NodeProcessMode.md) | enum | Pause-aware processing policy on SceneNode. |

[Sprite](../classes/Sprite.md) derives from Node. [Timer](../classes/Timer.md) and [Viewport](../classes/Viewport.md) derive from SceneNode; [Window](../classes/Window.md) derives from Viewport. Control is the accepted future CanvasItem UI branch and is not implemented yet.

## Runtime flow

SceneNode owns ordered children of any SceneNode subtype. SceneTree activates the tree parent-first, delivers ready child-first and exits child-first. Paths, groups, Owner metadata, process/input settings, typed child events and factories use SceneNode. Engine supplies scaled/original deltas; Timer and Tween reuse these scheduling lanes. Frame/input/lifecycle mutation guards and failure-continuing cleanup remain in the neutral layer.

CanvasItem adds retained drawing and canvas state. A direct canvas parent contributes transform, modulation and material; a neutral SceneNode breaks those chains. Global transform and Z accumulation stop at TopLevel. Toggling TopLevel preserves local state and recomputes global coordinates. Visibility follows direct canvas parents, including TopLevel, and the containing window. Window owns native visibility independently; its changes notify canvas roots, including roots below neutral nodes.

CanvasItem.GetTransform is abstract. Node implements it with an engine-owned Transform and adds the writable spatial properties. Direct CanvasItem subclasses can supply a different placement model; a Node child consumes that parent's transform without requiring the parent to be a spatial Node. Transform notifications stop at neutral and top-level children. Notification failures are collected while other direct canvas siblings are attempted.

Node.Reparent overrides the neutral operation. It validates a destination inverse before mutation when retaining global state, preserves structural lifecycle checks, then restores its local transform after attachment. A neutral reparent has no spatial state to preserve. GetRelativeTransformToParent separately multiplies local transforms along an uninterrupted spatial-node chain. Translate adds in parent space; MoveLocalX/Y move along the current local basis.

RenderingServer traverses all SceneNodes and records only CanvasItems. OnDraw/QueueRedraw and rectangle/line/texture methods belong to CanvasItem; Texture draw methods accept that base. Retained resource notifications atomically schedule owner-thread recording. Commands borrow Texture and Material; renderer backends own native caches.

PackedScene captures any SceneNode root. Each inheritance layer contributes only its own stored descriptors. Neutral and spatial nodes have separate default factories; derived types still supply an explicit static exact-type factory. Reconstruction is detached, shared resources remain borrowed, and the root owns scene-local duplicates.

## Dependencies and invariants

- All types remain in Electron2D.dll. SceneNode depends on Core object lifetime, descriptors, input values, SceneTree scheduling and the narrow scene-local Resource contract. CanvasItem adds math and retained graphics; Node adds concrete spatial state.
- No transform, visibility, material or drawing declarations are added to SceneNode or Timer.
- Attached mutation uses the scene owner thread. Packed capture/instantiation and disposal retain the existing barriers; invalid lifecycle mutation is rejected before state changes.
- Children have one parent and unique nonempty names. Parenting rejects cycles. Owner is null or a strict ancestor. Scene roots own descendant disposal; callback failures do not stop remaining cleanup stages.
- Geometry/transform inputs are finite; inverse-dependent operations reject singular transforms. ZIndex is bounded by -4096..4096.
- Window.Position is native desktop position; Window does not inherit canvas state. Child viewports remain unsupported until their native/offscreen ownership is integrated.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) exercises inheritance, API boundaries, mixed-tree transforms/visibility/Z, custom canvas placement, reparenting, timer/tween lifecycle, neutral packing and callback failure continuation. The existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) cover inherited behavior and run without native dependencies. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies pixels on both GPU and compatibility backends under Linux Wayland; the complete rendering and Window runtime suites also pass there. Native tests use programmatic inputs/readback and do not establish physical-input, visual owner or other-platform acceptance.

This migration preserves and separates the executable surface; it does not finish every reference member. Control/layout, additional drawing primitives and canvas policies, interpolation, independent viewports, physics, file scene loading, nested/inherited authoring and other exact prerequisites remain in [coverage](../coverage/index.md). No compatibility aliases or inert placeholders stand in for missing behavior.

## Decisions

- [0008: Scene inheritance and API correspondence](../decisions/scene.md#adr-0008)
- [0006 and 0011: Tree scheduling and failure cleanup](../decisions/scene.md#adr-0006)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)
- [0031: Scene composition](../decisions/scene.md#adr-0031)
