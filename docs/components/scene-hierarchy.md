# Scene hierarchy component

Last updated: 2026-09-23

## Scope and owned types

The accepted hierarchy is implemented under [ADR 0008](../decisions/scene.md#adr-0008). One ordered Node tree hosts neutral logic, canvas objects and spatial gameplay objects.

| Type | Base | Responsibility |
| --- | --- | --- |
| [Node](../classes/Node.md) | ElectronObject | Hierarchy, lifecycle, paths/groups, process/input callbacks, ownership and deletion. |
| [CanvasItem](../classes/CanvasItem.md) | Node | Abstract drawing base, visibility, Z/Y order, behind-parent drawing, modulation/materials and transform queries/notifications. |
| [Entity](../classes/Entity.md) | CanvasItem | Concrete position, rotation, scale, skew and spatial helpers. |
| [NodeProcessMode](../classes/NodeProcessMode.md) | enum | Pause-aware processing policy on Node. |

[Sprite](../classes/Sprite.md) derives from Entity. [Timer](../classes/Timer.md) and [Viewport](../classes/Viewport.md) derive from Node; [Window](../classes/Window.md) derives from Viewport. [Camera](../classes/Camera.md) derives from Entity and owns viewport tracking; CollisionShape remains a future spatial type. Control is the future CanvasItem UI branch, with Button reached through BaseButton. CollisionShape and the UI branch are not implemented yet.

## Runtime flow

Node owns ordered children of any Node subtype. SceneTree activates the tree parent-first, delivers ready child-first and exits child-first. Paths, groups, Owner metadata, process/input settings, typed child events and factories use Node. Engine supplies scaled/original deltas; Timer and Tween reuse these scheduling lanes. Frame/input/lifecycle mutation guards and failure-continuing cleanup remain in the neutral layer.

CanvasItem adds retained drawing and canvas state. A direct canvas parent contributes transform, modulation and material; a neutral Node breaks those chains. Global transform, Z accumulation and canvas sampler inheritance stop at TopLevel. CanvasItem filters/repeat resolve through direct canvas parents, then Viewport defaults; neutral nodes break canvas inheritance. Toggling TopLevel preserves local state and recomputes global coordinates. Visibility follows direct canvas parents, including TopLevel, and the containing window. Window owns native visibility independently; its changes notify canvas roots, including roots below neutral nodes.

Canvas roots follow scene order, each subtree before the next root. Effective Z takes precedence over behind-parent and nested local Y ordering. These canvas settings are stored by PackedScene and do not change process/input order. See [canvas rendering](canvas-rendering.md#canvas-ordering) for group boundaries.

Canvas membership has explicit entry/exit notifications independent of manual tree notifications. Entry is parent-first; exit is child-first. TopLevel rebinds only that item. Visibility delivery skips locally hidden branches, Hidden follows effective hide transitions, and showing schedules redraw. The [canvas component](canvas-rendering.md#canvas-lifecycle) records ordering and failure behavior.

CanvasItem.GetTransform is abstract. Entity implements it with an engine-owned Transform and adds the writable spatial properties. Direct CanvasItem subclasses can supply a different placement model; an Entity child consumes that parent's transform without requiring the parent to be a spatial Entity. Local geometry changes use the separate ItemRectChanged event and protected NotifyItemRectChanged helper; Entity transforms do not emit that event. Transform notifications stop at neutral and top-level children. Notification failures are collected while other direct canvas siblings are attempted.

Entity.Reparent overrides the neutral operation. It validates a destination inverse before mutation when retaining global state, preserves structural lifecycle checks, then restores its local transform after attachment. A neutral reparent has no spatial state to preserve. GetRelativeTransformToParent separately multiplies local transforms along an uninterrupted spatial-node chain. Translate adds in parent space; MoveLocalX/Y move along the current local basis.

RenderingServer traverses all Nodes and records only CanvasItems. NotificationDraw/Draw/OnDraw recording, QueueRedraw and rectangle/line/texture methods belong to CanvasItem; Texture draw methods accept that base. Retained resource notifications atomically schedule owner-thread recording. Commands borrow Texture and Material; renderer backends own native caches.

PackedScene captures any Node root. Each inheritance layer contributes only its own stored descriptors. Neutral and spatial nodes have separate default factories; derived types still supply an explicit static exact-type factory. Reconstruction is detached, shared resources remain borrowed, and the root owns scene-local duplicates.

## Dependencies and invariants

- All types remain in Electron2D.dll. Node depends on Core object lifetime, descriptors, input values, SceneTree scheduling and the narrow scene-local Resource contract. CanvasItem adds math and retained graphics; Entity adds concrete spatial state.
- No transform, visibility, material or drawing declarations are added to Node or Timer.
- Attached mutation uses the scene owner thread. Packed capture/instantiation and disposal retain the existing barriers; invalid lifecycle mutation is rejected before state changes.
- Children have one parent and unique nonempty names. Parenting rejects cycles. Owner is null or a strict ancestor. Scene roots own descendant disposal; callback failures do not stop remaining cleanup stages.
- Geometry/transform inputs are finite; inverse-dependent operations reject singular transforms. ZIndex is bounded by -4096..4096.
- Window.Position is native desktop position; Window does not inherit canvas state. Child viewports remain unsupported until their native/offscreen ownership is integrated.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) exercises inheritance, API boundaries, mixed-tree transforms/visibility/Z, custom canvas placement, reparenting, timer/tween lifecycle, neutral packing and callback failure continuation. The existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) cover inherited behavior and run without native dependencies. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies pixels on both GPU and compatibility backends under Linux Wayland; the complete rendering and Window runtime suites also pass there. [CanvasOrderingTests](../../tests/Electron2D.Tests/CanvasOrderingTests.cs) adds 31 cases for behind-parent, nested Y, canvas-root ordering and drawing-callback mutation on both native backends and dummy/software. Native tests use programmatic inputs/readback and do not establish physical-input, visual owner or other-platform acceptance.

This migration preserves and separates the executable surface; it does not finish every reference member. Control/layout, additional drawing primitives and canvas policies, interpolation, independent viewports, physics, file scene loading, nested/inherited authoring and other exact prerequisites remain in [coverage](../coverage/index.md). No compatibility aliases or inert placeholders stand in for missing behavior.

## Decisions

- [0008: Scene inheritance and API correspondence](../decisions/scene.md#adr-0008)
- [0006 and 0011: Tree scheduling and failure cleanup](../decisions/scene.md#adr-0006)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)
- [0031: Scene composition](../decisions/scene.md#adr-0031)

Pixel-snapping integration is described by [the canvas component](canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

Node.GetConfigurationWarnings and UpdateConfigurationWarnings provide typed diagnostics. SceneTree.EditedSceneRoot limits warning-change events to the selected live subtree, and DebugPathsHint drives optional path drawing. Selection is borrowed and cleared on exit; diagnostics do not create an editor. Contracts and executable evidence: [Scene paths diagnostics](scene-paths.md#configuration-diagnostics-and-path-drawing).
