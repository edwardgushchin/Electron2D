# Scene hierarchy component

Last updated: 2026-09-23

## Scope and owned types

The accepted hierarchy is implemented under [ADR 0008](../decisions/scene.md#adr-0008). One ordered Node tree hosts neutral logic, canvas objects and spatial gameplay objects.

| Type | Base | Responsibility |
| --- | --- | --- |
| [Node](../classes/Node.md) | ElectronObject | Hierarchy, lifecycle, paths/groups, process/input callbacks, ownership and deletion. |
| [CanvasItem](../classes/CanvasItem.md) | Node | Abstract drawing base, visibility, Z/Y order, behind-parent drawing, modulation/materials and transform queries/notifications. |
| [Entity](../classes/Entity.md) | CanvasItem | Concrete position, rotation, scale, skew and spatial helpers. |
| [Control](../classes/Control.md) | CanvasItem | Rectangular anchors, offsets, pivot transform and resize propagation; remaining GUI behavior is incomplete. |
| [NodeProcessMode](../classes/NodeProcessMode.md) | enum | Pause-aware processing policy on Node. |

[Sprite](../classes/Sprite.md) derives from Entity. [Timer](../classes/Timer.md) and [Viewport](../classes/Viewport.md) derive from Node; [Window](../classes/Window.md) derives from Viewport. [CanvasLayer](../classes/CanvasLayer.md) derives directly from Node and establishes an independent canvas. [Camera](../classes/Camera.md) derives from Entity and owns viewport tracking; CollisionShape remains a future spatial type. Control has executable layout and transform foundations; Button will be reached through the absent BaseButton. GUI focus, themes, input routing and containers remain unimplemented.

[RemoteTransform](../classes/RemoteTransform.md) also derives from Entity. It weakly targets another spatial node by path, resolves the target on tree entry or an explicit cache refresh, and transfers selected transform components through the existing queued global or synchronous local notification path. It rejects hierarchy feedback and remote-target cycles; PackedScene stores the path and policy, not the live target.

## Runtime flow

Node owns ordered children of any Node subtype. SceneTree activates the tree parent-first, delivers ready child-first and exits child-first. Paths, groups, Owner metadata, process/input settings, typed child events and factories use Node. Engine supplies scaled/original deltas; Timer and Tween reuse these scheduling lanes. Frame/input/lifecycle mutation guards and failure-continuing cleanup remain in the neutral layer.

CanvasItem adds retained drawing and canvas state. A direct canvas parent contributes transform, modulation and material; a neutral Node breaks those chains. Global transform, Z accumulation and canvas sampler inheritance stop at TopLevel. CanvasItem filters/repeat resolve through direct canvas parents, then Viewport defaults; neutral nodes break canvas inheritance. Toggling TopLevel preserves local state and recomputes global coordinates. Visibility follows direct canvas parents, including TopLevel, and the containing window. Window owns native visibility independently; its changes notify canvas roots, including roots below neutral nodes.

Canvas roots follow scene order, each subtree before the next root. Effective Z takes precedence over behind-parent and nested local Y ordering. These canvas settings are stored by PackedScene and do not change process/input order. See [canvas rendering](canvas-rendering.md#canvas-ordering) for group boundaries.

Canvas membership has explicit entry/exit notifications independent of manual tree notifications. Entry is parent-first; exit is child-first. TopLevel rebinds only that item. Visibility delivery skips locally hidden branches, Hidden follows effective hide transitions, and showing schedules redraw. The [canvas component](canvas-rendering.md#canvas-lifecycle) records ordering and failure behavior.

CanvasItem.GetTransform is abstract. Entity implements it with an engine-owned Transform and adds the writable spatial properties. Control implements it through a rectangle, pivot, rotation and scale. A direct Control child reflows when its parent rectangle changes; a root Control follows its viewport size. An Entity child consumes either parent's transform. Local geometry changes use the separate ItemRectChanged event and protected NotifyItemRectChanged helper; Entity transforms do not emit that event. Global transform invalidation stops at neutral and TopLevel children. Global notifications coalesce in the scene queue; a delivery failure does not skip later queued items. Local notifications run synchronously only while attached and enabled.

Entity.Reparent overrides the neutral operation. It validates a destination inverse before mutation when retaining global state, preserves structural lifecycle checks, then restores its local transform after attachment. A neutral reparent has no spatial state to preserve. GetRelativeTransformToParent separately multiplies local transforms along an uninterrupted spatial-node chain. Translate adds in parent space; MoveLocalX/Y move along the current local basis.

RenderingServer traverses all Nodes and records only CanvasItems. NotificationDraw/Draw/OnDraw recording, QueueRedraw and rectangle/line/polyline/dash/arc/circle/ellipse/polygon/primitive/texture methods belong to CanvasItem; Texture draw methods accept that base. Retained resource notifications atomically schedule owner-thread recording. Commands borrow Texture and Material; renderer backends own native caches.

PackedScene captures any Node root. Each inheritance layer contributes only its own stored descriptors. Neutral and spatial nodes have separate default factories; derived types still supply an explicit static exact-type factory. Reconstruction is detached, shared resources remain borrowed, and the root owns scene-local duplicates.

## Dependencies and invariants

- All types remain in Electron2D.dll. Node depends on Core object lifetime, descriptors, input values, SceneTree scheduling and the narrow scene-local Resource contract. CanvasItem adds math and retained graphics; Entity adds concrete spatial state.
- No transform, visibility, material or drawing declarations are added to Node or Timer.
- Attached mutation uses the scene owner thread. Packed capture/instantiation and disposal retain the existing barriers; invalid lifecycle mutation is rejected before state changes.
- Children have one parent and unique nonempty names. Parenting rejects cycles. Owner is null or a strict ancestor. Scene roots own descendant disposal; callback failures do not stop remaining cleanup stages.
- Geometry/transform inputs are finite; inverse-dependent operations reject singular transforms. ZIndex is bounded by -4096..4096.
- Window.Position is native desktop position; Window does not inherit canvas state. Child viewports remain unsupported until their native/offscreen ownership is integrated.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) exercises inheritance, API boundaries, mixed-tree transforms/visibility/Z, custom canvas placement, reparenting, timer/tween lifecycle, neutral packing and callback failure continuation. [RemoteTransformTests](../../tests/Electron2D.Tests/RemoteTransformTests.cs) covers transfer policy, path-cache lifetime, invalid targets, cycles, packing, callback failure and owner-thread rejection in managed execution. The existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) cover inherited behavior and run without native dependencies. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies pixels on both GPU and compatibility backends under Linux Wayland and dummy/software. The full Wayland rendering runtime suite in this run passed, including later scene-hierarchy stages. [CanvasOrderingTests](../../tests/Electron2D.Tests/CanvasOrderingTests.cs) adds 31 cases for behind-parent, nested Y, canvas-root ordering and drawing-callback mutation on both native backends and dummy/software. Native tests use programmatic inputs/readback and do not establish physical-input, visual owner or other-platform acceptance.

This hierarchy preserves separate responsibilities; it does not finish every reference member. Control focus, themes, container/minimum-size layout and GUI input, additional drawing primitives and canvas policies, interpolation, independent viewports, physics, file scene loading, nested/inherited authoring and other exact prerequisites remain in [coverage](../coverage/index.md). No compatibility aliases or inert placeholders stand in for missing behavior.

## Decisions

- [0008: Scene inheritance and API correspondence](../decisions/scene.md#adr-0008)
- [0006 and 0011: Tree scheduling and failure cleanup](../decisions/scene.md#adr-0006)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)
- [0031: Scene composition](../decisions/scene.md#adr-0031)

Pixel-snapping integration is described by [the canvas component](canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

Node.GetConfigurationWarnings and UpdateConfigurationWarnings provide typed diagnostics. SceneTree.EditedSceneRoot limits warning-change events to the selected live subtree, and DebugPathsHint drives optional path drawing. Selection is borrowed and cleared on exit; diagnostics do not create an editor. Contracts and executable evidence: [Scene paths diagnostics](scene-paths.md#configuration-diagnostics-and-path-drawing).

## Transform invalidation and delivery

CanvasItem.GetGlobalTransform caches the direct-parent composition; local edits invalidate the affected canvas chain, stopping at neutral and TopLevel boundaries. Queueing occurs only on a valid-to-invalid transition with NotifyTransformChanges enabled. Initial tree entry queues once regardless of that flag. Reading global state resolves invalidation without consuming the queue entry. ForceUpdateTransform consumes only the receiver's pending entry, before callback delivery, without recomputing coordinates, flushing children or requesting redraw. Local notifications are synchronous, attached-only and controlled by NotifyLocalTransformChanges; invalidation precedes them. Entity equal transform assignments are not suppressed. The two typed events project the corresponding numeric notifications through the base handler, including explicit manual notifications.

Audit sources: Godot 4.7.2-stable at `ed1daf0bf001b61586d9930840f2f1394092c079`, [canvas_item.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/canvas_item.cpp), [canvas_item.h](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/canvas_item.h), [node_2d.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/node_2d.cpp), and [scene_tree.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/scene_tree.cpp). The executable force method removes and notifies one queued item; the broader XML wording does not imply recursive forced flushing. Disabling notification policy does not cancel pending delivery, and force does not clear global invalidation.

Each delivery pass preserves the native pending-list order: capture the next entry before dispatch; an append behind an existing successor may run in this pass, while an append from the current tail waits for another pass. Cancellation advances the saved cursor before unlinking an entry, preventing removal or explicit forcing of the next item from stranding later callbacks. User callbacks can explicitly force a newly queued update; global invalidation still coalesces ordinary repeated writes. Callback exceptions retain consumed state and allow later pending items under ADR 0011. Mathematical values are current before delivery; there is no renderer delay for ordinary Entity geometry.

[CanvasTransformNotificationTests](../../tests/Electron2D.Tests/CanvasTransformNotificationTests.cs) covers policy defaults, entry/exit/rollback, equal assignments, cache invalidation and custom placement, force/coalescing, policy toggles, manual typed events, hierarchy boundaries, captures/threads/disposal, callback errors and reentry, phase ordering, timers/deferred/deletion and zero warm allocation. CameraTests, PathTests and SceneHierarchyTests consume the corrected event timing. [Native camera checks](../../tests/Electron2D.Tests/CanvasTransformNotificationRenderingTests.cs) distinguish a post-flush queued camera movement from an explicitly forced update in the same submission and verify retained drawing.

Managed tests and the focused native command `ELECTRON2D_TEST_RENDER=1 ELECTRON2D_TEST_TRANSFORM_NOTIFICATIONS=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` passed on GPU/default, GPU/HLSL, GPU/GLSL and compatibility. The same focus with `dummy` passed software compatibility. Other native platforms, physical input and owner visual acceptance are not claimed. Inherited physics interpolation and independent viewport rendering remain separate gaps.

Final self-contained linux-x64 verification: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-transform-publish`, then from that directory `env -u LD_LIBRARY_PATH PATH=/usr/bin:/bin ELECTRON2D_TEST_RENDER=1 SDL_VIDEODRIVER=wayland /tmp/electron2d-transform-publish/Electron2D.Tests`. The complete renderer suite, including forced/queued camera pixels and HLSL/GLSL, passed. The same executable with `ELECTRON2D_TEST_TRANSFORM_NOTIFICATIONS=1 SDL_VIDEODRIVER=dummy` passed software compatibility. Execution uses the packaged runtime and shader artifacts. Repeated SDL/GTK initialization still emits its existing nonfatal locale warning.

## Canvas render time

[Canvas animation intervals](canvas-rendering.md#animation-intervals-and-rectangles) use captured scaled process steps and a live typed rollover setting; ordered transform state is replayed alongside retained geometry. The clock is per Engine.Run and also supplies the optional GPU fragment [TIME built-in](shader-materials.md#render-time).
