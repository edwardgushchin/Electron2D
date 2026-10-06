# Scene domain

Last updated: 2026-10-07

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
- Sibling names are ordinal-unique, and path separators/reserved path tokens cannot be names.
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
- Packed-scene capture stores only root-owned branches, static exact-type factories, persistent groups, and explicitly storage-enabled typed properties. It stores no live source nodes or event subscribers.
- Packed-scene instances are reconstructed detached. Node factories and unfinished instances cannot activate a `SceneTree`; scene-local resource graphs preserve aliases/cycles, know their new root before setup, and are disposed with that root.
- Capture blocks source hierarchy mutation. Failed reconstruction attempts cleanup of every returned node and resource duplicate it acquired, reports cleanup failures, and never returns a partial result.
- `SceneTree` is initialized when construction succeeds, returns no quit request from its two inherited frame lanes, and releases all owned scene state from explicit finalization or disposal.
- System notifications are propagated depth-first to live attached nodes; native generation and platform-specific input effects belong to the SDL host/backend covered by ADR 0038.
- The warmed idle process and physics frame paths reuse scheduler/timer storage and do not allocate managed memory.

## Current limitations

- A caller may supply deltas directly through inherited `Process`/`PhysicsProcess` or wrappers. Core `Engine` can instead apply time scaling and fixed-step accumulation from host-supplied elapsed time. Engine.Run(Window) owns the implemented SDL pump, monotonic clock and frame-wait policy; no background scene thread is created.
- Canvas membership emits entry/exit notifications; local and inherited visibility delivery includes Hidden, and showing schedules redraw. Manual tree notifications do not mutate membership.
- Visibility and canvas-root, behind-parent, nested local Y and effective Z ordering govern retained commands. Rendering order does not change process/input scheduling.
- Root-window drawing and its input/client Viewport are integrated. Root viewport Control hit testing, mouse bubbling and focused keyboard delivery run; complete GUI routing remains absent. In-memory scene switching exists, but scene file loading/reloading does not. There is no independent offscreen viewport, wider physics server/area/joint API, RPC/multiplayer, accessibility backend, or scripting. Tweening is runtime-only and has no editor/serialization surface.
- Packed scenes support in-memory capture and typed archive persistence. Nested/inherited scene authoring, placeholders, editable instances, persistent event endpoint storage, general node-reference remapping and import integration, and every editor edit mode remain absent.
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

The [Window runtime component](../components/window-runtime.md) provides Window : Viewport : Node, root native ownership, presentation mode, four executable native policies, optional screen selection, client/decorated geometry, IME/taskbar requests, window events and scene input handling. WindowMode and WindowFlag describe the mode/policy identifiers. Capability failures stay explicit; declared policy IDs do not imply implemented native integration. Engine.Run consumes the configured window and children. The root canvas renders retained rectangles, lines, polylines, dashes, arcs, circles, ellipses, polygons, short primitives, textures and GPU shader materials after scene processing. Control rectangle layout and the first root viewport input path are present; remaining interactive GUI families, embedded viewport containers, nested native windows and root content scaling remain incomplete.

Pixel-snapping integration is described by [the canvas component](../components/canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

[PathTests](../../tests/Electron2D.Tests/PathTests.cs) verifies scene path sampling, lifecycle, copying, failures, worker delivery and warm movement allocation. [PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs) verifies descendant pixel movement on Linux Wayland GPU/compatibility and dummy/software; see the [component](../components/scene-paths.md) for remaining editor capabilities and verification limits.

Node now exposes typed configuration-warning queries/refresh requests. SceneTree owns the transient EditedSceneRoot selection, corresponding change event, and opt-in DebugPathsHint; these capabilities use the existing owner-thread/lifetime and canvas boundaries. See [scene paths](../components/scene-paths.md#configuration-diagnostics-and-path-drawing).

Timer and AnimatedSprite override the common warning query for short countdowns and missing frame libraries. ZIndex, timer duration/start, frame-library replacement and window title setters issue the corresponding refresh requests; SceneDiagnosticsTests and native PathRenderingTests cover their ordering and failures.

Root viewport canvas/final transforms are connected to retained rendering, scene input localization and CanvasItem coordinate/pointer queries. Logical node transforms stay unchanged. [Canvas coordinate integration](../components/canvas-rendering.md#viewport-coordinates) records ownership, singular/overflow behavior, runtime-only properties and Linux Wayland/dummy verification. Camera and CanvasLayer are integrated; root content scaling, embedded containers and nested native windows remain absent; single-layer SubViewport canvases execute.

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
