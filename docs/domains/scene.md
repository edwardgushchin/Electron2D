[Custom decoration input](../classes/Window.md#custom-decoration-input) connects stored Window rectangles to blocked-parent mouse reception across native and embedded modal children. Typed event lifetime, final viewport ownership and scene persistence share the existing scene pipeline.

[Linked runtime scenes](../components/linked-scenes.md) now reconstruct actual templates with selected typed overrides, local ownership, inserted positions and original node identities. Version-5 files preserve external/bundled links; root inheritance and editor instantiation modes remain separate.

# Scene domain

[Missing-node recovery](../classes/MissingNode.md) preserves unavailable compiled roles as bounded typed authoring data, with actual fresh-process restoration and separate missing-instance save consequences under ADR 0023.

[System back navigation](../components/window-runtime.md#system-back-navigation) now connects the native Android host to containing-window notifications, a typed Window event and independent SceneTree root quit policy. Owner-thread delivery, nested-window boundaries, signal blocking and teardown remain scene contracts.

[Process policy and physics pause](../components/scene-tree.md#process-policy-and-physics-pause) now preserve inherited eligibility notifications, root policy, nested failure handling and worker reads. Paused tree frames stop numerical integration while eligible callbacks continue; explicit owned spaces retain independent clocks. Public CPU/software-Vulkan/registered serial/separate managed and NativeAOT checks cover the slice. Full inherited physics, independent hardware GPU, networking and performance gates remain open.

Last updated: 2026-10-21

[Live-scene authoring saves](../components/resource-files.md#live-scene-authoring-save) now share pre/post notification, RESET-aware packing, atomic persistence and cleanup through ResourceSaver.SaveScene. The programmatic operation retains explicit owner/failure/fresh-process evidence and separate visual-editor/tooling gates.

[Window input reception](../classes/Window.md#window-input-reception-and-visibility-notification) now runs before viewport localization and scene callbacks for native and forwarded embedded events. Visibility changes also deliver notification 30 before their typed signal; input ownership and failure cleanup remain in the existing host/tree lifetime.

## Node scene-authoring metadata

[Node authoring state](../classes/Node.md#scene-authoring-state) supplies stored descriptions, transient folding, explicit edited-subtree membership and target-owned editable flags. [PackedScene](../classes/PackedScene.md#editable-ownership-capture) includes editable-owned physical children in its flattened graph and restores typed origin/flag metadata from fresh-process files. Authoring observations cover persistent connections and groups; physical owner/borrow/capture guards remain applicable. Linked nested/inherited override graphs, full editor UI, hardware GPU and full-goal acceptance remain open.

`Node.SceneFilePath` exposes writable nonstored source metadata through the shared typed descriptor. PackedScene retains nonroot origins separately and assigns the producing template path to reconstructed roots. [Scene source paths](../components/scene-hierarchy.md#scene-source-paths) describes authoring events, guards and actual scene reload.

## Runtime child names

[Child-name collision policy](../components/scene-hierarchy.md#child-name-collisions)
now supplies readable/generated insertion names, live numeric separators and
consistent rename/reparent/replacement behavior for all physical and scene roles.
Public assignments normalize all six reserved characters and notify detached
subtrees and unchanged names, with active node/tree events and continued error
delivery. Stored scene names retain exact internal identities. Retained path
snapshots preserve nested mutation and release retired references; warmed clean
assignments and complete physical frames allocate no managed memory.

Node inherits queued-deletion observation from ElectronObject. Explicit SceneTree.QueueDelete and cancellable Node.QueueFree remain visible until their request is consumed or cleanup finishes; cancellation affects only node work and parent requests do not mark children.

Inherited pre-delete cancellation is distinct from queued node cancellation. Rejected/vetoed children survive destruction of their owner as detached subtrees; each queued entry consumes one attempt, and a vetoed finalized root can enter a later tree.

## Orphan node diagnostics

[Debug orphan diagnostics](../components/scene-hierarchy.md#orphan-node-diagnostics)
sample detached nodes across all scene/physical roles without retaining their
hierarchies. ID snapshots and pre-captured diagnostic text follow lifetime and
tree membership independently of direct physics attachment. Release preserves
the empty/no-output contract and has no diagnostic registry.

## Exact inherited node paths

[Node path reads](../components/scene-hierarchy.md#exact-node-paths) now preserve active-only
absolute paths, immutable cache invalidation, detached relative/unique traversal and
typed lookup errors across physical nodes. Warm cached paths and exact lookups execute
without managed allocation; processing groups use the shared receiver access policy.

## Tile grid and terrain integration

[Tile resources and layers](../components/tiles.md) now connect square, isometric, half-offset and hexagonal atlas authoring and typed storage to actual merged static/kinematic bodies in CPU/GPU worlds, with tile-owner queries/events, runtime data callbacks and real retained-canvas output. All layouts, animated atlas storage/drawing and navigation now execute; terrain definitions, modes, weighted connect/path painting and fresh file graphs now execute; reusable pattern capture/storage/placement now execute; explicit typed proxy mapping now executes through placed cell consumers; occlusion retains its prerequisite under [ADR 0101](../decisions/tiles.md#adr-0101).

## Executable low-level primitive producers

Seventeen copied primitive/texture/indexed-triangle and command/order/culling operations now feed the existing owned/borrowed canvas graph. [The primitive contract](../components/canvas-rendering.md#low-level-primitive-commands) distinguishes source state, real palette/native replay, limits and explicit remaining shader/material/emitter prerequisites.

## Caller-owned canvas integration

Caller-owned canvases/items and borrowed scene canvas identities now support independent native parent/property state, retained Mesh/MultiMesh/nine-patch commands and simultaneous viewport attachments on the existing rendering backends. [The canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items) separates scene authoring, real rendering, lifetime and verification limits.

## Executable mesh skin integration

CanvasItem.GetCanvasItem now supplies a stable weak scene-owned identity for actual palette attachment. It survives tree exit/reentry, closes on disposal and is protected from renderer free. Attachments are transient and do not inherit or copy.

## Physical skeletal integration

[PhysicalBone and its modification consumer](../components/skeletal-animation.md#physics-driven-bones) now connect authored body/joint nodes to actual scene Bone poses. Weak path/ownership repair, parent-first startup and current-group physical following with owner native activation publication execute. Logical bone metadata is readable by active groups; editor generation remains separate.

## Jiggle controller integration

[Jiggle and held stacks](../components/skeletal-animation.md#jiggle-and-nested-stacks) add dynamic joint motion and reusable nested controllers. Existing Node/Entity pose and shared world ownership remain authoritative; direct child transactions, weak lease cleanup and bounded graph guards execute. Editor integration remains separate.

## Executable IK integration

[TwoBoneIK/CCDIK/FABRIK](../components/skeletal-animation.md#executable-ik-solvers) add target-driven limb and chain solving to the existing scene/animation consumer. Typed resource joint state, weak binding revisions, restored failed-pass requests and real weighted Polygon output execute; editor gizmos retain their own prerequisites.

## Skeletal scene execution

[Skeletal animation](../components/skeletal-animation.md) adds Skeleton/Bone, authored/rest pose separation, automatic endpoints, phase-aware overrides and executable LookAt stacks. Existing AnimationPlayer and canvas inheritance provide actual animation consumers; scene membership/path revisions prepare weak Polygon/target bindings. Editor integration retains its exact coverage dependency.

Active application locale changes now use MainLoop translation notifications and the existing scene propagation path. [Localization](../components/localization.md#application-translation-notifications) defines committed state, owner delivery, reentrancy and explicit independent-tree refresh.

## Bound node tween creation

`Node.CreateTween()` selects the attached tree, or the current application SceneTree through `Engine.MainLoop` for a detached node. Its binding suspends until that node joins the selected tree. Tree ownership, pause/lifetime and phase ordering stay with the existing tween scheduler; [Tweening](../components/tweening.md) records the executable checks.

## Responsibility

Scene owns Electron2D's primary Node-based game-object model, reusable typed in-memory scenes, and the active [`MainLoop`](../classes/MainLoop.md) implementation that delivers lifecycle, frame, pause, deferred-work, and deletion phases. A game object, composed subsystem, or complete world is represented by a Node hierarchy; the same hierarchy can be packed and instantiated for reuse. It is a 2D-only runtime domain for Windows, macOS, Linux (X11/Wayland), Android, iOS, Android TV, tvOS, and Web and compiles into the single `Electron2D.dll` assembly.

Its production sources live under `src/Scene/Main/`, `src/Scene/2D/`, `src/Scene/GUI/`, `src/Scene/Animation/`, and `src/Scene/Resources/`, matching their engine-module ownership without changing the flat public `Electron2D` namespace.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [Scene paths](../components/scene-paths.md) | Path curves and descendant movement by distance, offsets and tangent rotation | Runtime, configuration diagnostics and path visualization implemented; editor authoring remains pending |
| [Canvas rendering](../components/canvas-rendering.md) | Sprite texture/frame/region nodes, AnimatedSprite playback, Parallax scrolling and retained CanvasItem drawing | Executable; inherited canvas policies incomplete |
| [Window runtime](../components/window-runtime.md) | Native root window, presentation policies, platform events and client/input boundary | Implemented root slice; rendering and multiwindow incomplete |
| [Scene hierarchy](../components/scene-hierarchy.md) | Hierarchy, 2D transforms, paths, groups, visibility/Z state, process/input policy, lifecycle endpoints, and deletion requests | Implemented and verified |
| [Scene tree](../components/scene-tree.md) | Active-root ownership, exception-safe lifecycle, pause state, frame/input dispatch, events/counts, reusable scene timers, lightweight one-shot timers, typed group operations, deferred work, and deletion execution | Implemented and verified |
| [Tweening](../components/tweening.md) | Typed property/method interpolation, sequencing, callbacks, waits, nested timelines, loops, and frame policies | Implemented and verified |
| [Packed scenes](../components/packed-scenes.md) | Typed in-memory owned-hierarchy capture, live metadata, detached reconstruction, and per-instance local resources | Implemented and verified |

Production types include [`Polygon`](../classes/Polygon.md), [`Line`](../classes/Line.md), [`Parallax`](../classes/Parallax.md), [`ParallaxBackground`](../classes/ParallaxBackground.md), [`ParallaxLayer`](../classes/ParallaxLayer.md), [`Path`](../classes/Path.md), [`PathFollow`](../classes/PathFollow.md), [`RemoteTransform`](../classes/RemoteTransform.md), [`Node`](../classes/Node.md), [`CanvasItem`](../classes/CanvasItem.md), [`Control`](../classes/Control.md), [`LayoutPreset`](../classes/LayoutPreset.md), [`LayoutPresetMode`](../classes/LayoutPresetMode.md), [`LayoutDirection`](../classes/LayoutDirection.md), [`GrowDirection`](../classes/GrowDirection.md), [`CursorShape`](../classes/CursorShape.md), [`MouseFilter`](../classes/MouseFilter.md), [`FocusMode`](../classes/FocusMode.md), [`Sprite`](../classes/Sprite.md), [`AnimatedSprite`](../classes/AnimatedSprite.md), [`Window`](../classes/Window.md), [`Viewport`](../classes/Viewport.md), [`Entity`](../classes/Entity.md), [`ProcessMode`](../classes/ProcessMode.md), [`NodeAutoTranslateMode`](../classes/NodeAutoTranslateMode.md), [`PhysicsInterpolationMode`](../classes/PhysicsInterpolationMode.md), [`SceneTree`](../classes/SceneTree.md), [`Timer`](../classes/Timer.md), [`ProcessPhase`](../classes/ProcessPhase.md), [`SceneTreeTimer`](../classes/SceneTreeTimer.md), [`GroupCallFlags`](../classes/GroupCallFlags.md), [`Tween`](../classes/Tween.md), its four nested enum types, [`Tweener`](../classes/Tweener.md), its six concrete task types, [`PackedScene`](../classes/PackedScene.md), [`SceneState`](../classes/SceneState.md), and [`PackedSceneEditState`](../classes/PackedSceneEditState.md).

The root Window/SceneTree boundary now forwards native committed text and IME composition from DisplayServer to the eligible focused Control, while composition also reaches live nodes through `NotificationOsImeUpdate`. This is a separate string/preedit path and does not alter `InputEventKey` attribution.

## Public surface

The Control branch uses shared [`RecursiveBehavior`](../classes/RecursiveBehavior.md) values for its focus and pointer subtree policies; [ControlRecursiveBehaviorTests](../../tests/Electron2D.Tests/ControlRecursiveBehaviorTests.cs) checks their root viewport behavior.

- `Node`: neutral ordered hierarchy with ordinary and [front/back internal child partitions](../classes/Node.InternalMode.md), lifecycle, paths/groups including owner-scoped `%Name`, subtree replacement, inherited physics-interpolation policy/reset, depth-first diagnostics and notification propagation, processing/input, packed ownership and deletion.
- `CanvasItem : Node`: abstract retained drawing, visibility, materials, modulation, Z, shared transform queries, texture sampling policies and local geometry notifications through ItemRectChanged. Z ordering, borrowed material/modulation inheritance and draw-transform state now have managed and Wayland pixel audits; its local transform query is fulfilled by owner-guarded Entity/Control overrides. Other canvas rows retain their own status.
- `Entity : CanvasItem`: spatial position, rotation, scale, skew and helpers; Sprite, AnimatedSprite, Parallax, ParallaxLayer, Path, PathFollow and RemoteTransform derive directly from it. All 23 own spatial members and its type row have pinned semantic audits, including near-zero scale replacement, reflected-basis direction, point-only global translation, typed relative-chain errors and rotation/skew/angle behavior; inherited canvas and scene gaps remain on their own coverage rows.
- `RemoteTransform`: borrowed target-path binding and selected local/global spatial transfer through the scene transform-notification lanes.
- `Control : CanvasItem`: rectangular anchor/offset layout with sixteen typed anchor and offset presets, four size modes, offset-preserving position/size/global edits, paired edge edits, explicit/inherited LTR/RTL direction and locale-based fallback, virtual and custom minimum/maximum bounds, direct-child maximum propagation, three growth directions, absolute/relative pivot and optional visual-only offset transform, resize propagation, root viewport mouse routing, descendant clipping, hover cursor selection, keyboard focus notifications/events and action navigation; weighted box containers consume typed size flags under [ADR 0081](../decisions/rendering.md#adr-0081); themes, additional containers and complete GUI behavior remain gaps. Root `Viewport` exposes focus owner, release and change notification.
- `Sprite`: borrowed texture drawing, sheet frames, atlas regions, local bounds/opacity, change notifications and typed PackedScene state.
- `Line`: spatial polyline drawing with point edits, width/color/texture resources, cap and joint modes, and typed scene state.
- `Polygon`: spatial filled and inverted contours with color and texture coordinates, indexed subcontours, and copied packed state; skeletal deformation remains absent.
- `Parallax`: camera-relative spatial canvas subtree with manual and automatic scroll, limits, repeated drawing and stored scene settings.
- `ParallaxBackground : CanvasLayer` and `ParallaxLayer : Entity`: independent canvas and direct spatial children with camera/manual scroll, zoom policy, limits and one additional retained drawing copy per enabled mirror axis. The layer restores its original transform on exit.
- `Path` / `PathFollow`: borrowed Curve2D containment, attached direct-parent sampling, loop/clamp and ratio controls, offsets, rotation, deferred worker resource changes and packed state.
- `AnimatedSprite`: named SpriteFrames playback through the internal idle lane, reverse/custom speed, loop and progress events, retained texture/atlas drawing and packed state. Its [timing contract](../classes/AnimatedSprite.md#timing-contract-and-source-audit) includes exact-boundary and ping-pong details.
- `ProcessMode`: inherited, pausable, paused-only, always, and disabled process policies.
- `NodeAutoTranslateMode`: inherited, always-on, and disabled automatic translation policies.
- `PhysicsInterpolationMode`: inherited, On and Off 2D presentation policies, with Control defaulting Off.
- `SceneTree`: concrete main loop and active hierarchy owner with failure-safe lifecycle/finalization, in-memory current-scene replacement, typed input/system-notification propagation, pause state, caller-driven process/physics frames, frame/tree events and counters, typed group work, timers, deferred actions, and deletion flushing.
- `Timer`: reusable hierarchy-owned countdown with selected frame lane, one-shot/repeat, autostart, local/tree pause, optional time-scale bypass, and typed timeout event.
- `ProcessPhase`: stable physics/process lane selection for `Timer`.
- `SceneTreeTimer`: lightweight one-shot delay advanced by the selected frame lane with optional Engine time-scale bypass, and automatically disposed after timeout.
- `GroupCallFlags`: immediate/reverse/deferred/unique policy for typed group operations.
- `Animation`, `AnimationLibrary`, `AnimationMixer` and `AnimationPlayer`: reusable typed property keys, relative node bindings, named playback, reverse/seek/queues/sections and automatic phases. [Scene animation](../components/scene-animation.md) records managed and rendered checks; typed weighted mixing/capture, RESET and postprocessing now execute; other track families and persistence remain separate.
- `Tween` and tweeners: typed SceneTree-driven sequential/parallel interpolation, callbacks, waits, nested timelines, looping, pause/lane/time-scale policy, and completion events. Every own Tween and Tweener member and type row now has a pinned semantic audit, including 96 easing samples and near-limit Int64. Inherited lifetime roles, native cadence and other domains retain their separate coverage status.
- `PackedScene`: `Resource` that captures any reusable typed owned-node hierarchy, from one composed game object through a complete level, and reconstructs independent detached instances.
- `SceneState`: live read-only typed metadata view for current packed data.
- `PackedSceneEditState`: instantiation policy whose runtime `Disabled` value is implemented and whose editor values fail explicitly.

The [Physics domain](physics.md) supplies RigidBody, StaticBody, CollisionShape, CollisionPolygon and RayCast descendants on the existing Entity branch. An enabled RayCast samples through the internal physics callback before SceneTree steps the registered space; its result remains cached until another eligible frame or forced sample. SceneTree owns the space step before timers, tweens and interpolation end capture. Attached CanvasItems access the same space through [World](../classes/World.md); Physics owns RID/server/query behavior while Scene remains the scheduling and lifetime owner.

## Dependency direction

- Scene depends on Core's `Mathf`/`Vector2`/`Transform` math, Resources including `Resource`, and .NET collections and filesystem-name matching.
- Resources has a narrow reciprocal dependency on `Node` for `Resource.GetLocalScene()` under ADR 0023. This is an intentional in-assembly type cycle, not another managed assembly.
- Scene depends on the Input domain's typed event values and process-wide service boundary for propagation.
- Window now depends on the backend-neutral DisplayServer API for its native lifetime. Scene delegates drawing to the backend-neutral RenderingServer and fixed-step collision execution to the internal physics space; non-spatial AudioStreamPlayer now borrows audio resources and uses AudioServer for native playback lifetime. Scene nodes have no direct SDL3-CS dependency, general asset loading/saving, file serialization, scripting, networking, or Localization.
- Future gameplay, rendering and GUI input types may depend on Scene. Current physics bodies and areas use Scene's spatial hierarchy and fixed frame.
- Scene must not introduce 3D types. Non-spatial, canvas and spatial behavior belongs to Node, CanvasItem and Entity respectively under ADR 0008; these layers are implemented.
- Scene lifecycle and game-state semantics must not vary by target platform; native event generation remains a host boundary.

## Domain-wide invariants

- `Node` hierarchies are the primary public game-object and world model. Reusable objects and complete levels use the same `PackedScene` capture and instantiation boundary; Scene does not expose a competing entity hierarchy.
- Physics interpolation changes only presentation transforms. Logical spatial, viewport and input values remain current; enabled scene trees capture previous/current canvas and camera values around each fixed tick and reset stale history after pause, scene changes and process-time edits.
- A nonzero fixed frame resolves current area fields and applies stored body force/torque after node callbacks, advances rigid bodies and kinematic platforms, then commits body contact/sleep and area monitoring snapshots before timers and tweens. Contact and Area callbacks run after backend stepping so they can remove nodes; the Physics domain owns collision, force and field semantics.
- A node has at most one parent and one active `SceneTree`; cycles and cross-tree insertion are rejected before mutation.
- Replacing a node keeps the active tree root stable, preserves its old sibling index, moves children and eligible owners, and leaves the original node alive but detached. Scene-local resources transfer to the replacement root.
- An active root can be disposed only by its owning `SceneTree`.
- Sibling names are ordinal-unique. Public assignment normalizes `.`, `:`, `@`, `/`, `"` and `%` to `_`; blank inputs reject. Stored scene names preserve internal `@` identities.
- SceneTree-managed enter runs parent-first, ready runs child-first and once unless explicitly reset, and exit runs child-first. Lifecycle snapshots revalidate membership and lifecycle re-entry is rejected. Constructor failure terminally closes the failed tree, rolls membership and newly consumed ready state back, disposes activation-created timers, and invalidates activation-created tweens; later lifecycle failures complete their state transition and are aggregated. Manual `Notify(int)` dispatch is outside that state machine.
- Attached state mutation, lifecycle delivery, frame execution, flushing, and disposal use the tree's creating thread. Deferred and deletion requests may be enqueued from other threads.
- A non-top-level global transform is the ancestor global transform composed with the local transform. Transform inputs must be finite; operations needing an inverse reject singular transforms.
- Public and engine-internal process/physics callbacks are opt-in, synchronous, pause-aware, and ordered by their independent priority then captured tree order. Internal built-in work precedes the same node's public callback and receives both scaled and original Engine deltas.
- Input callbacks are opt-in, synchronous, pause-aware, reverse depth-first, ordered regular/key-unhandled/general-unhandled, membership-revalidated, failure-aggregating, and stoppable through current-dispatch handled state.
- Queue acceptance is atomic with tree-disposal closure. Deferred work queued during a flush waits for the next flush. Captured queued deletion runs after deferred actions, survives detachment, transfers safely between trees, and disposes the complete subtree despite detach callback failures.
- Frame and flush execution cannot be re-entered or started during lifecycle delivery. Reusable Timer nodes advance during internal node processing; lightweight tree timers advance after node callbacks and before deferred work. Pause delivery visits each eligible node at most once and rejects opposite re-entry.
- Tweens use one captured process/physics batch after lightweight timers. They are owner-thread mutable: a finished tween remains valid until its next eligible tree step; killing invalidates immediately but retains its registry entry until that sweep. Failures invalidate immediately. Parallel siblings are attempted on failure; no second scheduler or background clock is created.
- Typed group operations run in hierarchy/reverse order, revalidate membership, and can be deferred and coalesced without reflection or untyped values.
- Typed node events pass their publisher first when an additional payload is present; Core event connections can schedule handlers through `SceneTree.Defer`.
- Packed-scene capture stores root-owned and enabled descendant-owned branches, static exact-type factories, persistent groups, and explicitly storage-enabled typed properties. It stores no live source nodes or event subscribers.
- Packed-scene instances are reconstructed detached. Node factories and unfinished instances cannot activate a `SceneTree`; scene-local resource graphs preserve aliases/cycles, know their new root before setup, and are disposed with that root.
- Capture blocks source hierarchy mutation. Failed reconstruction attempts cleanup of every returned node and resource duplicate it acquired, reports cleanup failures, and never returns a partial result.
- `SceneTree` is initialized when construction succeeds, returns no quit request from its two inherited frame lanes, and releases all owned scene state from explicit finalization or disposal.
- System notifications are propagated depth-first to live attached nodes; native generation and platform-specific input effects belong to the SDL host/backend covered by ADR 0038.
- The warmed idle process and physics frame paths reuse scheduler/timer storage and do not allocate managed memory.

## Current limitations

- A caller may supply deltas directly through inherited `Process`/`PhysicsProcess` or wrappers. Core `Engine` can instead apply time scaling and fixed-step accumulation from host-supplied elapsed time. Engine.Run(Window) owns the implemented SDL pump, monotonic clock and frame-wait policy; no background scene thread is created.
- Canvas membership emits entry/exit notifications; local and inherited visibility delivery includes Hidden, and showing schedules redraw. Manual tree notifications do not mutate membership.
- Visibility and canvas-root, behind-parent, nested local Y and effective Z ordering govern retained commands. Rendering order does not change process/input scheduling.
- Root-window drawing and its input/client Viewport are integrated. Root viewport Control hit testing, mouse bubbling and focused keyboard delivery run; complete GUI routing remains absent. Scene switching and file loading/reloading execute through typed PackedScene templates and internal instance leases. There is no independent offscreen viewport, wider physics server/area/joint API, RPC/multiplayer, accessibility backend, or scripting. Tweening is runtime-only and has no editor/serialization surface.
- Packed scenes support in-memory capture and typed archive persistence. Programmatic Node metadata, flattened editable-owner capture and registered persistent event endpoints execute. Deferred materialization executes through [deferred scenes](../components/deferred-scenes.md). Root inheritance, import integration and non-Disabled editor edit modes remain absent.
- Paths are typed as `string`, not a separate `NodePath`; groups are strings; wildcard search covers names with `*` and `?`.
- A detached node may remember `QueueFree`, but deletion occurs only after attachment to a tree and a flush/frame boundary.
- There is no complete target host/package/test matrix; current executable verification is Linux-only.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies transform and hierarchy behavior, lifecycle order and failure rollback, cleanup continuation, inherited loop driving/finalization, typed input ordering/handled state/failures/re-entry/allocation, system-notification propagation, tree/frame events and counters, typed group operations, both timer models, typed tween sequencing/lifetime/failure behavior, paths/search/groups, visibility/Z, pause-aware internal/public process ordering, owner-thread enforcement, deferred batch isolation, concurrent enqueue/disposal stress, queued deletion, direct deterministic disposal, zero warmed idle/active-Timer/active-Tween/input allocations, packed owned-branch capture/state/instantiation, local resources, factory/capture rejection, and packed rollback. It does not prove renderer, SDL/native input, visual behavior, real-time cadence, disk scene compatibility, editor behavior, or large-scene performance.

## Relevant decisions

- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0004: 2D scene-oriented API in one Electron2D-owned assembly](../decisions/product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](../decisions/product.md#adr-0012)
- [0005: Notifications and typed editor properties](../decisions/core-object-runtime.md#adr-0005)
- [0006: Scene-tree deferred work and queued deletion](../decisions/scene.md#adr-0006)
- [0008: Node, CanvasItem and Entity responsibilities](../decisions/scene.md#adr-0008)
- [0010: Typed event connections](../decisions/core-object-runtime.md#adr-0010)
- [0011: SceneTree production contract](../decisions/scene.md#adr-0011)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
- [0026: Separate Transform foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0031: Node trees and reusable scenes as the primary game-object model](../decisions/scene.md#adr-0031)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
- [0036: Reusable scene timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0037: Typed SceneTree tween scheduling](../decisions/scene.md#adr-0037)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

## Windowed lifecycle

The [Window runtime component](../components/window-runtime.md) provides Window : Viewport : Node, root native ownership, presentation mode, four executable native policies, optional screen selection, client/decorated geometry, IME/taskbar requests, window events and scene input handling. WindowMode and WindowFlag describe the mode/policy identifiers. Capability failures stay explicit; declared policy IDs do not imply implemented native integration. Engine.Run consumes the configured window and children. The root canvas renders retained rectangles, lines, polylines, dashes, arcs, circles, ellipses, polygons, short primitives, textures and GPU shader materials after scene processing. Control rectangle layout and the first root viewport input path are present; embedded viewport containers and root content scaling execute; remaining GUI families and independent native child windows retain their owning coverage gates.

Pixel-snapping integration is described by [the canvas component](../components/canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

[PathTests](../../tests/Electron2D.Tests/PathTests.cs) verifies scene path sampling, lifecycle, copying, failures, worker delivery and warm movement allocation. [PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs) verifies descendant pixel movement on Linux Wayland GPU/compatibility and dummy/software; see the [component](../components/scene-paths.md) for remaining editor capabilities and verification limits.

Node now exposes typed configuration-warning queries/refresh requests. SceneTree owns the transient EditedSceneRoot selection, corresponding change event, and opt-in DebugPathsHint; these capabilities use the existing owner-thread/lifetime and canvas boundaries. See [scene paths](../components/scene-paths.md#configuration-diagnostics-and-path-drawing).

Timer and AnimatedSprite override the common warning query for short countdowns and missing frame libraries. ZIndex, timer duration/start, frame-library replacement and window title setters issue the corresponding refresh requests; SceneDiagnosticsTests and native PathRenderingTests cover their ordering and failures.

Root viewport canvas/final transforms are connected to retained rendering, scene input localization and CanvasItem coordinate/pointer queries. Logical node transforms stay unchanged. [Canvas coordinate integration](../components/canvas-rendering.md#viewport-coordinates) records ownership, singular/overflow behavior, runtime-only properties and Linux Wayland/dummy verification. Camera, CanvasLayer, root content scaling, embedded containers and embedded Window chains are integrated. Pointer reads select the root's native/emulated input or an isolated section's retained hover point before Node callbacks. Item screen queries separately target popup space, stopping at embedding viewports; viewport screen queries retain the full native-client chain. Warp requests resolve the native root after one screen projection and reject an absent host. Independent native child windows remain unavailable.

[Camera tracking](../components/canvas-rendering.md#camera-tracking) connects Camera : Entity to viewport selection, idle/physics updates, zoom/rotation, drag/limit policies and smoothing. It reuses scene ownership and canvas/input transforms. Inherited 2D physics interpolation now samples the camera on physics ticks and presents its viewport transform between ticks; editor preview remains absent.

[Canvas layers](../components/canvas-rendering.md#canvas-layers) provide independent drawing groups, transforms, visibility and viewport following through CanvasLayer : Node and CanvasItem.GetCanvasLayerNode. Opaque canvas identities and independent viewport rendering remain separate dependencies.

[Canvas visibility masks](../components/canvas-rendering.md#canvas-visibility-masks) connect stored CanvasItem.VisibilityLayer and Viewport.CanvasCullMask to retained submission, including parent pruning and nested Y sorting. Mask edits preserve logical visibility, callbacks and input. Native Wayland GPU/compatibility, HLSL/GLSL and dummy/software behavior is verified; independent single-layer SubViewport canvases now share these policies; layered targets retain their prerequisite.

[Transform invalidation and delivery](../components/scene-hierarchy.md#transform-invalidation-and-delivery) integrates cached canvas coordinates, coalesced global notifications, synchronous opted-in local notifications and ForceUpdateTransform. Camera and PathFollow follow the same timing; queues cancel on exit, disposal and activation rollback.

[Canvas animation intervals and rectangle geometry](../components/canvas-rendering.md#animation-intervals-and-rectangles) execute in the retained command path. The render clock follows captured scaled process steps and the active wrap setting; the same clock feeds the optional GPU fragment [TIME built-in](../components/shader-materials.md#render-time).

The [audio playback component](../components/audio-playback.md) supplies AudioStreamPlayer : Node with runtime autoplay, polyphony, owner-frame completion and retained cursor pause on tree exit. AudioStreamEmitter : Entity adds viewport listener selection, position-based gain/pan and Area bus routing. Native output belongs to the audio server and closes during engine teardown; PackedScene retains the borrowed stream and typed player configuration.

MultiMeshInstance inherits Entity transforms/canvas policies and stores typed borrowed MultiMesh/Texture properties through scene factories. SceneTree physics capture includes registered instance resources; presentation reset and inherited mode changes refresh their shared policy. [The mesh component](../components/meshes.md#repeated-instance-resources) records resource ownership, callback failure and rendered interpolation evidence. Scene-file persistence remains its existing separate dependency.

[Typed animation graphs](../components/scene-animation.md#animation-graphs) execute reusable BlendTree definitions, typed per-tree parameters, signed Blend/Add/Sub, time controls and clip timelines on the existing property mixer. Graph resources remain separate from scene Node; borrowed libraries/definitions and owner-thread execution follow ADR 0093. State-machine/grouped controllers now execute with typed flag/predicate conditions; persistence remains separate. [Action controllers](../components/scene-animation.md#action-controllers) now execute OneShot envelopes/requests and named Transition switching. [Blend spaces](../components/scene-animation.md#blend-spaces) now execute linear/triangle/discrete/carry mixing and synchronized per-tree clocks.

Typed scene animation also executes scalar Bézier property geometry and heterogeneous typed method keys through players/graphs, with prepared deferred capacity, safe-point callback lifetime and track-path filtering. See [Bézier and method tracks](../components/scene-animation.md#bézier-and-method-tracks). Audio keys and state machines also execute through this mixer; persistence retains its coverage trigger.

[Nested animation tracks](../components/scene-animation.md#nested-animation-tracks) orchestrate existing child players with clip-name keys, seek/stop, normal child phases and revision-protected cleanup. Direct/weighted caches prepare separately and remain reusable through recurring control changes.

[Audio tracks](../components/scene-animation.md#audio-tracks) execute borrowed sound cues on non-spatial and spatial receivers, including seek/trims/weights and bounded prepared starts. They share the animation clock and accepted FAudio transport.

[State machines](../components/scene-animation.md#state-machines) supply per-tree owned playback, weighted geometric travel, automatic typed conditions, synchronized/end transitions and grouped ancestry. Departed clips retire nested/audio output through existing bindings. Native PCM, rendered pixels, editor/disk and platform acceptance remain distinct checks.

SubViewport composes as a neutral Node with independently rendered children. PackedScene copies scene-local viewport textures against each reconstructed hierarchy. [The offscreen contract](../components/canvas-rendering.md#offscreen-canvas-targets) distinguishes render-to-texture and scene input isolation from non-root GUI/container/multiview work.

[CanvasGroup and BackBufferCopy](../components/canvas-rendering.md#group-composition-and-screen-snapshots) now execute native same-Z group composition and ordered screen snapshots. GPU screen-reading HLSL/GLSL and generated group mipmaps share existing materials; compatibility retains explicit shader/mipmap/software blend gates. [Canvas alpha masks](../components/canvas-rendering.md#canvas-alpha-masks) now execute through CanvasItem.ClipChildren; nested captures, writable screen reads and editor inspector integration retain explicit boundaries.

[Embedded viewport containers and GUI](../components/canvas-rendering.md#embedded-viewport-containers-and-gui) now execute native SubViewportContainer composition, stretch/shrink, independent GUI state and connected input/drag routing. Public input reentry remains rejected; native subwindows, multiview, editor/file workflows and other-platform acceptance remain separate.

## Text-field authoring

[LineEdit](../classes/LineEdit.md) has typed stored properties and an in-memory PackedScene factory. Runtime TextInput/IME uses the existing focused Control route, including embedded viewport focus. History, scalar selection and typed text dragging execute under the scene owner; closing the host does not enqueue minimum-size work. Editor/file authoring and absent popup/native keyboard services retain their own gates.

[Typed scene multiplayer](../components/scene-multiplayer.md) executes owner-thread Node/SceneTree branch assignment and process polling, typed RPC/authority/local policy, authentication/deadlines and WS/WSS original-sender relay. Concrete spawning/property-schema synchronization remains the next dependent producer; other-platform/routed/native allocator and human/editor/rendered acceptance stay separate.

[Typed scene spawning and property replication](../components/scene-replication.md) now supplies MultiplayerSpawner/Synchronizer/SceneReplicationConfig and concrete descriptor/factory codecs over WS/WSS, with actual pre-Ready/late/visibility/authority/batch semantics. In-memory PackedScene provenance enables automatic spawning. Disk scene format/loading/editor authoring and foreign/routed/native/human/rendered acceptance remain separate.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

[TabBar](../classes/TabBar.md) now executes separate selected/changed events, pointer/action/controller navigation, close requests, identity-stable typed group drags and foreign-drag hover switching through an owned internal Timer. Its stored indexed fields and built-in file factory reconstruct in a fresh process; metadata and transient interaction remain runtime state. See [tab strips](../components/tab-strips.md).

The [embedded popup window slice](../components/popup-windows.md) composes Window/Popup/PopupPanel targets through the existing GPU/compatibility renderer, routes independent window GUI/input and preserves typed scene/file factories. Independent native child windows and live embedding-policy migration remain explicit dependencies.

[Popup menus](../components/popup-menus.md) add the typed item/submenu consumer atop embedded Window presentation, shaped themed drawing, shared scroll/search controls and scene/file factories. Native/system menu services and dependent MenuButton/OptionButton/TabContainer/context-action consumers retain separate coverage.

[Tab panels](../components/tab-panels.md) combine TabContainer/TabBar selection with actual child Control visibility/layout, typed themes, popup input, group page drags and fresh scene factories. Warm selection/layout/visibility and native render checks pass on current Linux Wayland GPU/compatibility; inherited semantic-service/editor and other-platform acceptance remain separate.

[Dropdown choices](../components/dropdown-choices.md) integrate OptionButton selection, caption/radio state, real menu/search/shortcut input and fresh-process factories through current Button/PopupMenu and text/theme backends. Native keyboard/readback and warm selection checks pass on Linux Wayland GPU/compatibility; inherited semantic/editor/native-popup and broader platform limits remain distinct.

[Embedded acceptance and confirmation dialogs](../components/dialogs.md) compose Window, Label, Button and HBoxContainer with registered LineEdit input. Own public operations, event/hook order, deferred cancel/reopen protection, panel/button themes and fresh scenes execute. Independent native children and inherited accessibility/scaling retain separate prerequisites.

[Command menu buttons](../components/command-menu-buttons.md) execute commands and related hover switching through owned PopupMenu windows. Nested embedded input descends to the final target and choice/menu anchors use their actual embedder.

[File dialogs](../components/file-dialogs.md) now execute scoped browsing, five selection modes, typed custom options, filters, menus, overwrite/folder workflows and recoverable desktop Linux trash. The shared FileDialogMode identity spans the custom browser and DisplayServer; native-file-extra and foreign platform gates remain explicit. Six permanent ui_filedialog actions use the existing InputMap settings loader.

[Numeric input](../components/numeric-input.md) adds SpinBox formula/text/arrow/repeat/relative-drag authoring through shared Range and LineEdit, fresh scene factories and generated numeral localization. Current Wayland GPU/compatibility capture/input/pixels and prepared active rendering are exercised; precise pointer warp, inherited semantic/editor and foreign target gates remain separate.

[Color authoring](../components/color-authoring.md) connects spatial/numeric color editing, local swatches, typed palette files, owned popup buttons and completed application-viewport sampling. Native external capture and semantic/foreign-target gates remain separate.

The executable [multiline editing component](../components/multiline-editing.md) connects TextEdit documents, typed syntax resources, existing font/Control rendering, scene storage and input. Its cold/warm and target limits are recorded with the exercised workflow.

[Hierarchical cells](../components/hierarchical-cells.md) now connect Tree/TreeItem ownership, columns, typed metadata, checks/ranges/buttons, ordinary GUI input, embedded cell editors and real canvas drawing. Stored column configuration reloads in a fresh process; runtime item graphs and semantic/native/editor gates retain their explicit limits.

[Code authoring](../components/code-authoring.md) now specializes TextEdit through CodeEdit, with grouped multicaret source edits, delimiters/folds, gutter markers, typed application completion, hints and symbol callbacks. Fresh file scenes and current native rendered/input hosts execute; semantic/editor/physical/foreign gates remain explicit.

[Rich text](../components/rich-text.md) connects styled shaped paragraphs, inline images/tables/drop caps, typed links and glyph effects to the existing Control, canvas, font, input and scene-resource backends. Root queries and runtime manual stacks remain separate from stored source; native/current/foreign and advanced font/script/accessibility dependencies are explicit.

[Graph authoring](../components/graph-authoring.md) connects GraphElement, GraphNode, GraphFrame and GraphEdit through current canvas, font, input and scene storage. Typed ports, request-driven wiring, keyboard/remapped commands, nested frames and graph-scaled popup input execute; semantic accessibility, editor authoring and foreign/native allocation gates remain explicit.

[Compiled C# source assets](../components/scripting.md) register exact project Node/Resource factories and typed schemas, validate portable-PDB source/type association, load/save Script resources, and reconstruct actual user classes through scene archives. Project callbacks retain ordinary lifecycle and source-resource disposal does not unload their compiled class. RichTextLabel consumes owned compiled effect instances. Live reassignment, implementation reload/migration, scene RPC and foreign target source-symbol prerequisites remain explicit dependencies.

## CPU particle integration

[CPUParticles](../classes/CPUParticles.md) and [CPU particles](../components/cpu-particles.md) connect ordinary scene internal processing, scalar curves/gradients, borrowed textures/materials, typed file graphs and shared canvas output. Configured CPU simulation and sprite-sheet replay execute on current GPU/compatibility. World emission follows complete physics poses and uses a separate canvas basis for visible world quads. GPU compute/process materials, their conversion, foreign/native allocator and owner acceptance remain separate exact dependencies.

[Menu strips](../components/menu-strips.md) now present direct PopupMenu children with identity-preserving headers, pointer/keyboard/hover switching, shortcut gates and child-bound fresh scene/file factories. System global menus retain the precise NativeMenu decision/backend dependency in ADR 0041.

[Tile navigation layers](../components/tiles.md#navigation-layers) connect typed TileSet masks and borrowed TileData polygons to scene-owned NavigationServer regions, runtime cell data, root-local baking and retained diagnostic drawing. Global navigation diagnostics remain separate work.

[Atlas frame animation and padding](../components/tiles.md#atlas-frame-animation-and-padding) provide complete frame reservations/configuration, source-owned transient padded textures and retained timed TileMapLayer drawing. Physics/navigation geometry remains independent of visual frames.

[Scene collection tiles](../components/tiles.md#scene-collections) retain shared PackedScene graphs and compiled factory root validation, with deferred ordinary child placement and explicit editor/physics prerequisites.

Window content policies now execute logical-size fitting, raster target selection and presentation through both native canvas backends, including embedded input and automatic glyph raster scale. [Window](../classes/Window.md#content-scale) records state and verification limits.

[Animation key optimization](../components/scene-animation.md#key-optimization) reduces built-in scalar/Vector2 curves before existing player and graph execution, preserving discrete writes and non-linear curve contracts.

[Typed clip persistence](../components/scene-animation.md#typed-clip-persistence) loads property/Bézier Animation and AnimationLibrary assets through stable compiled property bindings, then executes existing player/mixer/graph consumers.

[Animation cue persistence](../components/scene-animation.md#cue-persistence) reconstructs all current method/audio/nested track kinds for existing players and graphs using compiled callback IDs and typed resource references.

[Controller scene files](../components/scene-animation.md#controller-scene-files) now preserve exact AnimationMixer/Player authored scene state and load Ready-time autoplay through existing scheduling, with library aliases across nodes. Graph definitions and reset-on-save capture transactions remain distinct dependencies.

Animation graph resources and AnimationTree scene controllers now persist authored topology, compiled writable parameter IDs and typed values through the existing archive. See [graph file persistence](../components/scene-animation.md#graph-file-persistence). Resource validation runs after all embedded payloads and before publication.

RESET save preparation now executes through AnimationMixer.ResetOnSave and PackedScene.PackForSave. Enabled RESET value/Bézier tracks run at zero on detached nodes and isolated copied resources, capturing coupled effects while preserving source preview values/clocks/queues. Ordinary Pack captures the actual pose. Preparation failures preserve prior packed data; temporary resources are cleaned or transferred to the prepared scene. Compiled factories/setters/postprocessing execute, while lifecycle, local-resource setup and method/audio/nested event tracks are suppressed. Custom authoring state must be stored and resource duplication supported. AnimationResetSaveTests exercises policies, all controller families, coupled values/resources, errors, lifetime and portable saved scenes.


Viewport interpolation now starts On independently of an Off ancestor; explicit
Inherit/Off and scene-wide enablement retain their meanings. Shared physical
CPU/GPU checks and nested Wayland renderer pixels validate body/camera history
without changing logical poses, plus packed/copy/duplicate/revert behavior and
zero warmed physical-step/presentation allocation. See
[the canvas contract](../components/canvas-rendering.md#physics-interpolation).

[Resource endpoints and subtree operations](../components/scene-hierarchy.md#resource-endpoints-and-subtree-operations) now execute typed resource-path resolution and compiled subtree operations through Node. Authored hierarchy/atlas edits survive saved scenes and reach native canvas pixels.

[Implicit physics world selection](../components/physics-backends.md#project-physics-selection) now captures typed project engine/fallback settings and manager DEFAULT priority independently of rendering. Caller parameterless and explicit creation retain their policies. CPU/GPU/registered motion, no-device startup and native pixels have scoped checks; separate-thread scheduling and complete physics/performance acceptance remain open.


Authored typed scene connections use registered compiled `SceneConnectionBinding` factories, copied payloads and dual endpoint cleanup. PackedScene captures/rebinds these subscriptions, including file archives v3. SceneState queries expose paths, diagnostic labels, flags, unbind count and typed payload snapshots. See [packed scenes](../components/packed-scenes.md) for ownership and persistence limits.

Viewport cursor refresh sends internal motion through the containing Window, respecting parent GUI overlap before nested viewport routing. Frame/input requests coalesce at a safe point; GUI and passive physics hover execute while Node input callbacks omit the synthetic event. See [input runtime](../components/input-runtime.md).

`Control.TranslationContext` is a stored local string, empty by default. `Atr`/`AtrN` use it when the call's context is null or empty; a nonempty call argument overrides it. `Tr`/`TrN` retain their explicit context. The property is not inherited from a parent. Set it before assigning text: changing the context alone sends no notification and preserves already prepared captions. `Notify(Node.NotificationTranslationChanged)` refreshes that control; `PropagateNotification` can refresh its subtree. Existing automatic mode, object enablement and inherited domain rules still apply.

## Independent internal processing

`public bool InternalProcessEnabled { get; set; }` and `public bool InternalPhysicsProcessEnabled { get; set; }` project each internal-lane setter/query pair. Plain Node defaults are false; built-in nodes may enable their own work. Each switch preserves the other lane and the ordinary ProcessEnabled/PhysicsProcessEnabled flags. Enabled internal notifications run before ordinary callbacks in their selected idle/fixed frame. An internal callback can enable or disable the ordinary callback for the same frame. Detach/dispose prevents further ordinary delivery; failures still attempt the remaining ordinary/sibling callbacks under the scene-tree cleanup policy.

The resolved ProcessMode and SceneTree pause policy gate both lanes. Internal processing is independent of ordinary callback enablement, and does not bypass pausing. Detached nodes retain the runtime switches but receive no frames. Setters require the attached tree's owner and reject scene capture; disposed access rejects. Typed property descriptors expose writable runtime state with IsStored false. Ordinary PackedScene capture omits the flags, while constructors rebuild built-in internal behavior. No hidden thread or scheduler is added.

Changing a built-in node's internal switches directly affects its implementation: for example it suspends and resumes Timer countdowns. Such nodes can overwrite the switch when their playback/callback policy changes. Do not disable internal work unless that is the intended effect. See [Node](../classes/Node.md) and [Timer](../classes/Timer.md).

NodeInternalProcessingTests verifies both lanes, order and current-state mutation, pause modes, reattachment, errors, ownership/disposal, runtime scene omission and actual idle/physics Timer consumers. Its 1024 warmed set/query/frame operations allocate zero managed owner bytes. Native GPU and compatibility checks verify red pixels, a frozen frame with the internal lane disabled, resumed blue pixels and fixed ticks. Foreign/editor/native allocator and broad load verification remain separate.


Containing-window notifications now reach physical Node descendants in parent-first
order, stop before nested Window scopes and continue after callback failures.
WindowNotificationTests exercises eight event kinds through embedded/public and SDL
dummy routes plus 1024 zero-byte warmed operations. DPI/position platform limits and
missing go-back/HDR sources remain open; see [window runtime](../components/window-runtime.md#containing-window-notifications).

The exact SceneTree.Defer scheduler and SceneTree.SetDeferred now use a Scene-owned typed invocation provider. Each accepted call retains independent typed values in a slot and submits its immutable callback into the existing single FIFO queue. Slots recycle only after callback completion or discard, clearing argument/callback references even after failure. Work emitted during a drain batch joins the FIFO and runs before that flush completes. Pool operations share the lifetime/work gate; user handlers run outside it. A weak-owner Core registry recognizes only the exact registered delegate, so Core has no Scene runtime dependency. Public signatures, borrowed reference ownership, event multiplicity and cancellation remain unchanged.

Prepared standard-scene delivery and typed setters have zero invocation allocation, including prepared one-shot first delivery. First type/capacity growth, subscription and allocations performed by handlers/accessors remain separate; native multicast removal with multiple listeners can allocate. Arbitrary custom schedulers preserve their independent captured Actions and may allocate. Deferred delivery never extends a borrowed input event lifetime.

EventConnectionPhysicsTests verifies mixed typed/untyped FIFO order, nested same-flush work, failures followed by later work, closed-scheduler subscription/emission policy, 256 concurrent producers, reference release on execution/failure/finalization/activation rollback, and 640 first one-shot callbacks for immediate and scene-deferred profiles with singleton accessors. Selected CPU/GPU full frames retain 128 callbacks per immediate and deferred arity plus body/shape transitions at strict zero owner/all-thread bytes. Real picking and legacy custom-scheduler/concurrency/error checks remain included. Software Vulkan is functional evidence; hardware/native/foreign/network/performance acceptance stays open.

## Prepared physical event delivery

Each emission leases an ordered immutable handler snapshot. Duplicate handlers and last-matching multicast subsequence removal retain ordinary delegate semantics. Reentrant/concurrent changes affect later emissions; user callbacks run outside the storage lock. Subscription prepares a spare buffer for one-shot removal. Capacity or overlapping-snapshot growth can allocate; arbitrary consumer event accessors and user callbacks keep their own allocation policy. Disposal clears these publisher-owned lists after teardown. Pointer events collect per-handler errors and retain eligibility checks; overlap/contact/sleep handlers stop the current event on failure.

EventHandlerTests compares 1,024 mixed delegate add/remove operations with native delegate lists, checks nested snapshot changes/errors, 256 concurrent producers and weak-reference release from disposed publishers. All 18 physical accessors each deliver 128 prepared first one-shot callbacks at strict zero owner/all-thread managed bytes. EventConnectionPhysicsTests adds a real full selected CPU/GPU Area transition with 128 one-shot listeners and a permanent listener. Serial/separate numerical profiles use CPU-only Vulkan for GPU kernels; native allocations, hardware performance, real-window FPS and foreign acceptance remain open.

See [typed handler storage](../classes/EventHandlers.Generic.md) and [ADR 0010](../decisions/core-object-runtime.md#adr-0010).

## Automatic typed callbacks (2026-10-14)

Ready notification now enables each overridden process, physics, input, shortcut,
unhandled-key and unhandled-input callback before OnReady. Detached defaults are
false; OnReady and later setters can disable the lanes. Consumed ready does not
repeat on ordinary reentry; RequestReady re-arms it. Manual ready notification
runs the same preparation without consuming managed readiness. Inherited overrides
participate, while hidden non-virtual methods and plain nodes stay disabled.

The runtime inspects only six strongly bound virtual delegate targets on cold
first-type preparation and caches a bit mask under weak CLR Type keys. It retains
no node/delegate, performs no name lookup/assembly scan/reflection invocation and
adds no frame work. Ordinary typed dispatch, pause/membership/owner/error guards,
internal lanes and selected CPU/GPU numerical ownership stay in place.

PhysicsAutomaticCallbackTests uses real public character motion and all input
stages, detached/ready flags, inherited/individual/hidden overrides, live disabling,
reentry, requested/manual ready, packed reconstruction, pause and weak retirement.
A complete physics/idle/input bracket warms 128 and measures 128 at strict zero
owner/all-thread managed bytes. A public-only standalone consumer supports the
same compiled test in ordinary and NativeAOT hosts. Evidence and exact limits are
retained under bin/physics-auto-callbacks/2026-10-14; full inherited, native/foreign
and hardware/window/performance acceptance remains open.

The NativeAOT prerequisite slice uses bound virtual delegates for CodeEdit/GraphEdit, exact existing types for empty custom-data arrays and prepared string JSON metadata. Script source registration retains its explicit file/PDB and untrimmed metadata prerequisites; metadata is captured once and queried through copied snapshots. Public-only executable evidence is recorded in bin/physics-aot-prerequisites/2026-10-14; this does not establish native rendering, foreign-platform or complete scripting acceptance.

Node scheduling now captures actual enabled public/internal lane membership, with next-frame activation of absent participants and immediate suppression of disabled captured callbacks. Exit/reentry cannot revive a stale entry. Public neutral/spatial/physical consumers exercise this alongside real selected frames and queries; the scene-tree component describes the current group/mailbox profile and wider acceptance gates.

The Node call/set/notify thread-safe and deferred-group family now publishes typed operations to the current main processing owner. A distinct FIFO runs before and after callbacks, preserving weak receiver lifetime, current-owner transfer, error continuation and prepared zero-byte delivery alongside actual physical frames. Explicit worker processing groups and message masks are implemented by the profile below; broader worker scene/backend access remains open.

Node processing now includes real inherited main/worker groups, ordered barriers, per-lane message-only masks, cached member schedules and typed cross-group dispatch. Public tests connect worker results to actual physics on the owner and retain general Node/CanvasItem/Entity/backend worker-access gaps as Partial.


[Owned name/type searches](../components/scene-hierarchy.md#owned-name-and-type-searches)
now preserve full branch ownership, internal scene order, case-sensitive Unicode
patterns and typed inheritance filters for every Node descendant, including
physics bodies and Areas. Repeated first-result/parent reads in actual physical
frames have their own allocation and lifetime/thread checks; remaining scene
API and platform acceptance remain separate.

The [exclusive-window chain](../components/popup-windows.md#active-exclusive-window-chains)
now backs Node.GetLastExclusiveWindow and every PopupExclusive entry point.
Transient ownership, flags, visibility and retirement determine selection;
processing-scope reads and attachment/cleanup failures preserve scene invariants.
Independent native child windows remain a separate capability.

[Active viewport ownership](../components/scene-hierarchy.md#active-viewport-ownership)
now backs Node.GetViewport/GetWindow across physical and ordinary nodes. Enter,
exit, reparent and cross-tree transfer publish/clear the retained association;
cached borrowed reads and own-group window lookup preserve their separate access
rules. Packed scenes reconstruct active links at activation.

[VideoStreamPlayer](../classes/VideoStreamPlayer.md) now provides authored video as a Control with first-frame timing, pause/speed/loop/completion, exact packed factories and initial detached owner transfer. Fresh-process scene load and real GPU/Compatibility rendering are separate verified paths.

[Node group membership](../components/node-groups.md) preserves first-add persistence, literal identifiers,
current processing-group access and exact physical/file consumers. Physical
mutations retain their owner guard before virtual consumer hooks. Runtime
Node duplication retains its separate ADR 0091 binding/identity prerequisite.

The [node lifecycle contract](../components/scene-hierarchy.md#entry-readiness-and-departure) now orders complete notifications, typed hooks and parent/tree signals, per-attachment post-enter and child-first ready, reverse sibling exit and a final detached-subtree TreeExited pass. Body/Area/joint membership retires after TreeExiting through private cleanup even after callback errors. Ready observations/requests allow current processing groups without waking physical bodies. Public physical and fresh-file consumers preserve the remaining native/platform/hardware and full physics acceptance boundaries.

The [membership and authoring-owner contract](../components/scene-hierarchy.md#membership-and-authoring-ownership) preserves structural/tree identity, callback-failed reparent completion, borrowed owner/unique-name lifetime and cross-world physical RID/pose. Owner metadata does not select a physical space or wake a body. Public managed/NativeAOT consumers verify the bounded hot path; full native/platform/hardware acceptance remains open.

The [complete Node processing contract](../components/scene-tree.md#complete-processing-metadata-and-callbacks) joins local metadata, worker configuration, six stored callback switches, signed priorities, typed frame/input delivery and retained worker schedule sorting. Public managed/NativeAOT CPU/software-Vulkan/registered checks include actual selected motion and zero warmed owner/all-thread bytes; broader native/hardware/inherited acceptance remains open.

## Generation-qualified queued deletion

`Node.QueueFree` reserves a unique current request; `CancelFree` invalidates that request before an owner claims it. Reissue appends a new FIFO entry. A stale entry in the same or a previous tree cannot consume the replacement. A request created by a departure callback waits for the next captured deletion batch, while the body remains queryable and continues its next physical step before that new deletion. Once a request is claimed, cancellation cannot undo its disposal. Child flags remain independent; disposing the parent still retires its subtree.

Publication rollback changes only the reserved generation. Node teardown atomically closes further publication and leaves IsQueuedForDeletion false. Queue acceptance retains the existing tree lifetime gate. Explicit SceneTree.QueueDelete remains an independent uncancellable operation. This queue cancellation does not veto a Dispose already executing.

The tree swaps two prepared concurrent deletion queues under its work gate and returns a drained queue after all captured entries. New entries wait in the other queue; exceptions retain later work and prepared storage. Finalization discards pending references from both queues. No callback runs under the queue gate.

The public-only `--queued-deletion` consumer checks FIFO reissue, real body survival/query/RID identity across deletion boundaries, subtree flags/order, detached and transferred requests, inherited neutral/canvas/physical roles, concurrent cancel/publication/claims, error recovery and weak-reference release. CPU, software-Vulkan GPU and registered numerical profiles run with owner/separate solve policies. Each warm bracket cancels and reissues requests, executes ordinary deferred work and queries an integrating body for 128 complete frames at strict zero owner/all-thread managed bytes. Strict NativeAOT uses the same public consumer. Native memory totals, target-device advantage, real-window FPS and foreign acceptance remain open.

Window initial placement and constrained ResetSize now use the existing native/embedded host paths. Stored placement is independent of authored Position; embedded centering runs on entry/show, while native root policies use DisplayServer screen selectors and retain compositor limits. See [the window runtime](../components/window-runtime.md#initial-placement-and-constrained-reset).

Process-group order and message-mask metadata now permit writes by the scene owner or actual current group on inherited Node descendants. Captured order changes on the next frame; mask changes affect later eligibility without interrupting active delivery. Synchronously helped nested pool jobs retain that worker's scene context, including the distinction between immediate safe calls and queued deferred calls. [The scene-tree contract](../components/scene-tree.md#ordered-processing-groups) records validation and physical consumer evidence; broader worker scene/render/backend mutations remain open.

[Logical transform observation](../components/scene-hierarchy.md#logical-transform-observation-from-processing-groups) now permits active groups to read local/global Entity and Control chains, including inherited physical nodes. Shared dirty matrices publish under the retained canvas-item gate after virtual getters return; notification coalescing, ordinary-worker rejection and current-group pose mutation guards remain distinct. Public parallel ordinary/pool-host physics consumers verify the bounded read contract. General worker mutation remains Partial.

[Transform authoring from processing groups](../components/scene-hierarchy.md#transform-authoring-from-processing-groups) now covers all Entity pose setters/helpers and inherited physical/drawable nodes. Cache revisions and a short notification queue gate preserve committed pose/error semantics; existing scene spaces are borrowed for metadata edits and released after every joined stage before selected queries/stepping. The original scene owner temporarily uses current-group rights while helping a parallel stage. General inherited worker operations and native physical writes retain separate coverage.

## Generated mesh geometry

MeshInstance and MultiMeshInstance consume [procedural mesh surfaces](../components/meshes.md#procedural-surfaces-without-positions). Generated positions receive the full item, command, instance and skin transforms. Author-owned visibility rectangles cover the output; CPU faces, input geometry and collision are not reconstructed from shader execution.

[Transform notification policies and forcing](../components/scene-hierarchy.md#transform-notification-policies-and-forcing-from-groups) now accept their processing-group contexts. Forced user notifications remain immediate; camera viewport and generated tile physics/navigation updates publish after the joined worker stage before owner queries/stepping. Shared queue cancellation and lifecycle accounting support concurrent forcing without holding gates across callbacks. Inherited interpolation reads permit active groups; mode mutation and reset permit the owner/current group. Reset callbacks remain immediate, with foreign branches reported and later siblings attempted. Ordered camera scroll/reset and borrowed renderer policies publish after the joined worker stage; see [interpolation publication](../components/scene-hierarchy.md#physics-interpolation-from-processing-groups).

General notification traversal now runs immediately in the owner/current processing group, including physical descendants, and preserves foreign-branch errors, callback reentry and protected geometry ownership. Reserved physics/GUI consumer effects retain their independent guards and open coverage. See [verification](../components/scene-hierarchy.md#general-notification-traversal-from-processing-groups).

[Physical canvas notification publication](../components/scene-hierarchy.md#physical-canvas-notifications-from-processing-groups) now preserves current-group body/Area/tile identity requests and effective participation through the joined owner phase. Logical canvas-layer reads accept active groups; attachment epochs and exact generated RID targets protect owner reentry. General reserved notification consumers remain Partial.

[Scene cast group access](../components/physics-queries.md#scene-cast-metadata-and-cached-observations-from-groups) now covers local options, exceptions and held results. Numerical sampling remains owner-only, as in the pinned server; it is no longer misclassified as an absent reserved notification consumer. Other reserved consumers remain Partial.

Prepared skeletal modifiers and local bone override requests now execute in their skeleton's current group, including propagated reserved notifications and direct public calls. All target Bone groups are checked before pose changes; metadata gates release before setup/execution callbacks. Configuration, binding/setup, palette replay and numerical collider queries retain their owner domains. See [verification and limits](../components/skeletal-animation.md#prepared-modifications-in-processing-groups).

## Embedded Window decorations

The complete thirteen-item Window theme family now executes focused/unfocused styles, retained shaped and outlined titles and captured normal/pressed close images. Outside resize margins preserve actual constrained opposite edges. [Window chrome](../components/popup-windows.md#embedded-decoration-theme-and-input) records scene/resource authoring, callback cleanup, native pointer/pixel verification and remaining independent native-host limits.

[Viewport GUI pixel snapping](../components/canvas-rendering.md#gui-control-pixel-snapping) now provides a stored independent policy for local Control visual origins, including offsets, rotation tolerance, nested targets and retained overrides. Logical layout/input/focus geometry stays fractional. Native GPU/compatibility pixels and prepared allocation intervals complement managed/fresh-scene checks.

Ordinary desktop Window children now execute [independent native hosts](../components/window-runtime.md#independent-native-windows), sharing one display/event service and scene renderer. GPU device targets are shared; compatibility contexts replay frozen commands with cold history transfer. Native popup surfaces execute with the limits below; platform-specific decoration controls retain separate prerequisites.

## Native popup surfaces

Native desktop popups use real parent-relative SDL surfaces when `PopupWMHint` is enabled. `PopupOnParent` accepts parent-client geometry and works without fabricated Wayland desktop positions. Public native `Position`, explicit global `Popup(rect)` and display-centered placement retain the global-position capability guard; centered geometry uses the complete parent display, while fitting uses its usable area. Attached embedded children keep their existing viewport coordinates. Runtime control anchors compose the source viewport transform, including nested and scaled hosts.

The native role is fixed-size and borderless, cannot minimize/maximize/fullscreen, and follows its physical parent's display. Hide before changing native role or transparency. Native children recreate their identity after hide. Retiring or hiding a native parent first retires popup descendants; independent ordinary child windows survive parent hide. Popup visibility observer failures are reported after required retirement and committed visibility. SDL popup-done force-hides without emitting a user `CloseRequested`; ordinary close requests and scene cancellation retain their own behavior.

Transparent native PopupPanel/PopupMenu output executes through the compatibility backend. SDL 3.4.18's GPU API rejects transparent windows; GPU native menus require explicit `Transparent=false` before showing. The unsupported request releases its attempted native lifetime and preserves hidden state. Embedded transparent popups continue to execute on both renderers. Native titled-popup variants without the popup role, live embedding migration, foreign-platform acceptance and native system-menu service retain their exact coverage prerequisites.

`NativePopupWindowTests` uses `ELECTRON2D_TEST_NATIVE_POPUPS=gpu` or `compatibility` with the Release runner. It exercises native nested parenting, IDs, outside dismissal, owner-safe rectangles, no-focus and exclusive policies, forced retirement, callback failure, real menu/option/panel/context output, and fresh-process public scene loading. Active focus/render and idle/render measurements cover 64 owner-thread intervals after 32 warmup intervals. SDL event construction, cold model edits/readback, native allocator counts, compositor screenshot acceptance, physical input and other platforms are outside the measured interval.

[Node hierarchy duplication](../components/node-duplication.md) executes the complete DuplicateFlags family through current scene factories, typed property storage and linked scene instances. Source/ancestor resource and file leases keep copied subtrees alive after original-scene disposal. Raw copies keep authoring ownership unset; linked contents retain template ownership. Authoring/fresh file loading, headless execution and native rendered evidence have separate verification scopes.

The [deferred call/setter contract](../components/scene-tree.md#complete-deferred-call-and-setter-contract) drains nested typed work at the current safe point before queued deletion. Selected physical frames retain exact tick counts and zero warmed managed allocation; platform and full-goal acceptance remain separate.
