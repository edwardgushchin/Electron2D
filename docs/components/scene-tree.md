# Scene tree component

Last updated: 2026-09-24

## Scope

This Scene component provides the concrete [Main loop](main-loop.md), owns one active hierarchy and an optional current scene child, propagates typed input and system notifications, delivers lifecycle and pause changes, accepts direct or [Engine](engine-runtime.md)-scheduled process/physics frame boundaries, frame counters and events, tree-change events, reusable Node timers, lightweight one-shot timers, frame-driven [tweening](tweening.md), typed group operations, deferred work, and queued deletion. Per-node hierarchy and 2D state belong to the [Scene hierarchy component](scene-hierarchy.md).

## Owned types

| Type | Role |
| --- | --- |
| [`SceneTree`](../classes/SceneTree.md) | Concrete `MainLoop`, hierarchy owner, frame/system-notification dispatcher, group service, timer owner, deferred scheduler, and deletion queue |
| [`Timer`](../classes/Timer.md) | Reusable Node countdown with process/physics, pause, repeat, autostart, time-scale, event, and packed-configuration behavior |
| [`TimerProcessCallback`](../classes/TimerProcessCallback.md) | Stable choice between physics and process countdown lanes |
| [`SceneTreeTimer`](../classes/SceneTreeTimer.md) | Auto-disposed one-shot timer advanced by a selected tree frame lane |
| [`GroupCallFlags`](../classes/GroupCallFlags.md) | Order, deferral, and uniqueness policy for typed group operations |

Tween types are owned by the separate [Tweening component](tweening.md) but registered and advanced here.

## Runtime flow

Construction rejects calls made from inside a packed-scene node factory and roots still marked as unfinished by packed-scene instantiation. It otherwise validates the supplied root, initializes inherited loop state, enters every reachable node parent-first, and readies nodes child-first. Enter/ready callback failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready state, disposes activation-created timers, invalidates activation-created tweens, and clears queues while leaving the supplied hierarchy caller-owned. User side effects outside owned lifecycle state are not transactional.

A root with inherited auto-translation mode samples `ProjectSettings.RootNodeAutoTranslate` before entry; root and descendant entry then delivers translation-change notifications to nodes whose effective mode is enabled. An active root cannot switch back to inherited mode.

An inherited or wrapper frame increments its lane counter, raises its start event, reuses an owned snapshot buffer to priority-order nodes, invokes all still-eligible internal then public callbacks, advances matching lightweight tree timers, advances a captured matching tween batch, then runs one deferred/deletion safe point. `AnimatedSprite` runs through internal idle notifications in the node phase, using delivered scaled delta independently of ProcessEnabled; it shares the existing pause/process-mode and failure handling. Its resource changes reconcile on the tree owner thread. `Timer` nodes run in the node phase and can choose the Engine-supplied original process step in either lane; `SceneTreeTimer` runs in the later timer phase and selects the scaled or original lane delta from its creation policy; `Tween` may select either scaled or original lane delta. Timer, tween, node, event, deferred, and deletion failures do not prevent later phases and are reported together. Frame/flush execution and tree finalization/disposal are rejected during frame, flush, and node-lifecycle delivery before queue consumption or lifetime mutation.

On a nonzero physics frame, the tree resolves current Area gravity/damping fields and applies stored body force/torque after node callbacks and before stepping its internal 2D world, synchronizes simulated bodies, then commits Area monitoring snapshots and delivers their events before lightweight timers and tweens. Zero elapsed time leaves the world and area snapshots unchanged. Area event handlers may mutate scene membership because backend stepping has finished; failures are aggregated while later frame phases continue. The [physics bodies](physics-bodies.md) and [physics areas](physics-areas.md) components own the force, filtering, field and monitoring contracts.

SceneTree samples `ProjectSettings.PhysicsInterpolation` at construction and exposes a live tree-wide switch. When enabled, a physics frame captures eligible canvas and viewport camera history around its callbacks without allocating after warmup; the renderer later uses Engine's fractional tick progress. Node inheritance and Control's Off default select eligible items. Reset, pause, camera switching and process-time edits discard stale presentation history while logical transforms remain current.

System notifications are snapshotted and propagated depth-first to every still-live attached node. Explicit inherited finalization and disposal both close work acceptance, release the hierarchy/timers, and invalidate active tweens; explicit finalization leaves only the tree object undisposed and terminal.

The stable Root can hold an optional CurrentScene direct child alongside persistent children such as UI overlays. Assigning CurrentScene only selects an existing direct child. ChangeSceneToNode accepts a live detached scene on the owner thread, removes the selected old scene immediately, and queues the new scene's entry for the next deferred safe point. ChangeSceneToPacked instantiates first, so instantiation failure preserves the old scene. Multiple requests before the safe point retain only the latest new scene; old and superseded scenes are disposed before that entry. SceneChanged follows successful entry and ready delivery. Failed attachment disposes the accepted pending scene; callback errors propagate after state transitions and later cleanup. UnloadCurrentScene immediately disposes only the selected scene. Finalization also disposes detached pending scenes that never reached a safe point. File-based change/reload waits for an asset loader.

A Viewport root first localizes incoming client input through its inverse final transform. Positional temporary copies are disposed after dispatch, including errors; local-coordinate and non-positional input stays borrowed. Conversion validates execution/re-entry before copying and does not mutate Input polling. Parsed input uses a reusable reverse depth-first snapshot. Regular input runs first, root viewport hover routing and GUI input follow, then unhandled `ui_*` focus navigation, keyboard-only unhandled input and general unhandled input. GUI pointer targeting uses Control geometry, filter, layer/Z and direct-parent bubbling; keyboard/controller input reaches the focused Control. Focus transfer exits the old Control, raises Viewport.GUIFocusChanged, then enters the new Control; each Control notification precedes its event. Viewport.GetGUIFocusOwner reports the current target and ReleaseGUIFocus clears it. Focus callback failures are aggregated after later eligible callbacks, and reentrant release suppresses stale enter delivery. Pointer motion updates Control hover and native cursor even after handled regular input. Handled state stops later input stages; current membership, pause eligibility, lifetime, and per-stage enablement are revalidated before ordinary Node callbacks. Callback failures are aggregated after eligible delivery continues. Complete renderer ordering, clipping, stationary-pointer geometry changes, touch, exact directional ranking, scroll clipping and nested viewport routing remain absent.

Immediate typed group operations snapshot members in hierarchy or reverse order on the owner thread. Deferred operations resolve membership when their queued wrapper starts. `Unique` coalesces equal queued operations and retains the first setter value. String-based method/property dispatch is absent.

Queue acceptance uses one lock shared with finalization closure. A successful cross-thread enqueue is either executed by a later safe point or deliberately discarded by later finalization; an enqueue that reaches the closed tree is rejected. Finalization closes the queues, exits and recursively disposes the hierarchy, disposes timers, invalidates tweens, and clears subscribers while aggregating every failure.

Window-driven frames invoke the internal RenderingServer under the scene execution barrier after frame processing. Re-entry and mutation during protected phases remain guarded; the renderer itself owns native submission and cleanup.

The first [Physics](../domains/physics.md) scene-body profile now attaches direct RigidBody/StaticBody descendants to one lazily created internal world. Physics node callbacks run before a four-substep Box2D step; solved transforms return before timers, tweens and interpolation end capture. Root disposal releases body handles and the world. Geometry and backend types remain owned by the Physics domain; SceneTree adds no public server or RID API.

## Dependencies

The component depends on Core's `MainLoop` and `EventConnection`, typed Input events, the neutral `Node` including its input/internal lanes and packed-scene construction barriers, `Timer`, the [Tweening component](tweening.md), reusable lists, concurrent queues, and ordinary .NET synchronization. Core `Engine` may drive it through `MainLoop` and supplies scaled/original deltas. It has no SDL3-CS, clock, native input backend, audio, collision-physics, asset loader/serializer, networking, or editor dependency.

## Invariants

- The root is live, parentless, unattached, and not queued when construction begins.
- Tree activation cannot start inside a packed-scene node factory or with any node whose packed-scene instantiation barrier is active.
- The active root cannot be directly disposed or queued; tree disposal owns its complete lifetime.
- A current scene is a direct child of the stable root. Accepted scene changes own pending nodes; repeated requests supersede them, and finalization releases every still-pending node.
- Membership is never left partially attached after constructor failure or lifecycle callback failure.
- Lifecycle, immediate group work, timers, frames, flushing, hierarchy mutation, and disposal use the creating thread.
- Deferred actions, deferred group operations, generic queued deletion, and node deletion requests may originate on another thread.
- Enter is parent-first, ready is child-first, and exit is child-first; all applicable lifecycle callbacks are attempted before failures are reported.
- Lifecycle snapshots revalidate current parent/tree membership; a node in enter/ready/exit delivery cannot be removed, reparented, or disposed re-entrantly.
- Pause traversal revalidates lifetime/membership and visits each node at most once; opposite or teardown-time transitions are rejected.
- One action batch is captured per flush; deletion is captured afterward, so deletion requested by a captured action runs in that flush while nested deferred actions wait.
- Reusable `Timer` nodes run through internal Node processing before their own public callbacks; lightweight tree timers run after nodes and before deferred actions. Expired tree timers are disposed even if timeout handlers fail.
- Matching registered tweens run after lightweight timers and before deferred actions. Tween processing uses a captured list, so tween-created tweens wait for the next frame. A normally finished tween remains registered but stopped until the next eligible frame removes it. A killed tween is invalid immediately but remains visible in `GetProcessedTweens()` until that sweep; failed tweens are invalidated while later tweens and phases continue.
- Queued node deletion reaches disposal even when detach callbacks fail or the node became detached; an old tree does not consume a request owned by a new tree.
- Tree events reflect completed lifecycle/structural stages; callback failures do not roll completed state back.
- MainLoop initialization is complete when construction returns; explicit finalization or disposal releases every owned scene object exactly once.
- System notifications use depth-first snapshots and revalidate each candidate before delivery.
- Input uses reverse depth-first snapshots, three ordered stages, owner-thread dispatch, handled short-circuiting, pause/membership revalidation, and failure aggregation after Input state is committed.
- Warmed idle, active-Timer, and active-Tween frame lanes do not allocate managed memory in the covered small-hierarchy paths.
- Warmed active physics frames with interpolation and canvas movement allocate zero managed bytes over the measured 128-tick owner-thread interval; native GPU/driver allocations were not measured.

## Current implementation status and exclusions

Implemented and covered by executable checks. Core Engine supplies host-driven time scaling, original process-step delivery for `Timer.IgnoreTimeScale` and original lane-delta delivery for `SceneTreeTimer` and `Tween.SetIgnoreTimeScale`, fixed-step scheduling, and the fraction used by the executable 2D interpolation path. Engine.Run supplies an owner-thread window clock/pump and frame wait. There is no background game-loop thread, permission request implementation, operating-system focus synchronization, scene file loader/reload, complete GUI input consumption, multiplayer polling, accessibility backend, editor behavior, or wider physics server/area/joint simulation. In-memory current-scene replacement is available through detached nodes and PackedScene. The root viewport GUI input and focus path runs in this component. Blocked reference APIs and their missing domains are enumerated in the [`SceneTree`](../coverage/classes/SceneTree.md), [`Timer`](../coverage/classes/Timer.md), [`Tween`](../coverage/classes/Tween.md), and [ADR 0038](../decisions/input.md#deferred-coverage-and-exact-implementation-triggers); no placeholder surface is exposed for them.

## Verification

Tests cover valid and failing activation, packed-factory/unfinished-node activation rejection, inherited loop initialization/frames/explicit finalization, Engine attachment/finalization, typed input stage order/handled state/re-entry/failure continuation/allocation, system-notification propagation, escaped failed-tree references, ready/timer/tween rollback, stale lifecycle snapshots, lifecycle and tree-event order, pause re-entry/reparent/removal behavior, frame ordering/counters/events, lifecycle execution barriers, typed group order/setting/notification/uniqueness, reusable/lightweight timer behavior, typed tween behavior, deferred batching, generic/node/cross-tree deletion, owner-thread enforcement, failure-continuing teardown, re-entrant disposal mutation rejection, teardown mutation rejection, concurrent enqueue/disposal stress, and warmed idle/active-Timer/active-Tween/input allocation. SceneChangeTests covers selected-scene ownership, deferred entry, supersession, event order, callback failures and cleanup. The Timer audit against pinned timer.cpp/timer.h covers strict-negative expiry, retained negative overshoot, and process-step decrement in both lanes. Edited-scene autostart suppression awaits the editor runtime. Those established checks cover managed scene semantics; the focused interpolation pixels below add Linux rendering evidence. Real-time cadence, physical input, loaded-scene performance and other platforms remain unverified.

[PhysicsInterpolationTests](../../tests/Electron2D.Tests/PhysicsInterpolationTests.cs) covers construction sampling, runtime toggles, policy inheritance, first/repeated physics ticks, reset/pause, camera and Control behavior, invalid mode/owner guards, packed state, callback failures and zero warmed active-tick allocation. [PhysicsInterpolationNativeTests](../../tests/Electron2D.Tests/PhysicsInterpolationNativeTests.cs) checks moving canvas and default Idle camera pixels on dummy compatibility and Linux Wayland compatibility/GPU. Other platforms, physical timing variation and owner visual acceptance remain unverified.

## Decisions

- [0006: Scene-tree ownership, deferred work, and deletion](../decisions/scene.md#adr-0006)
- [0011: Exception-safe lifecycle, typed groups, and timers](../decisions/scene.md#adr-0011)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0037: Typed SceneTree tween scheduling](../decisions/scene.md#adr-0037)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

## Windowed lifecycle

SceneTree.Quit requests exit and supplies Engine.Run's return code; AutoAcceptQuit defaults to true and is evaluated after Window.CloseRequested. Both Process and PhysicsProcess report pending quit after completing their frame lane. Engine.Run publishes the tree before ready and reserves its finalization/disposal until runtime teardown. Ordinary Node roots still support manual/headless embedding.

Node.GetConfigurationWarnings and UpdateConfigurationWarnings provide typed diagnostics. SceneTree.EditedSceneRoot limits warning-change events to the selected live subtree, and DebugPathsHint drives optional path drawing. Selection is borrowed and cleared on exit; diagnostics do not create an editor. Contracts and executable evidence: [Scene paths diagnostics](scene-paths.md#configuration-diagnostics-and-path-drawing).

Existing Timer duration setters, AnimatedSprite library replacement, CanvasItem Z changes and Window title updates participate in selected-scene warning refresh. SceneDiagnosticsTests covers their own warning conditions and synchronous event ordering.

## Canvas transform phases

Canvas transform notifications use dedicated owner-thread queues. Physics delivers pending entries before PhysicsFrameStarted. Idle delivers after ProcessFrameStarted and again after node callbacks. Both lanes deliver after timers, tweens and the captured deferred-action batch, before queued deletion. Each pass follows pending-list order, capturing the next entry before invoking a callback. Reentrant additions behind an existing successor can be reached in that pass; an addition from the current tail waits for another pass. Cancellation advances the saved cursor before unlinking a pending entry, so force, exit and disposal cannot strand later items. Callback failures are aggregated after the other pending entries and later frame stages are attempted. Explicit FlushDeferred remains an action/deletion flush, not a transform flush. ForceUpdateTransform selects one item inside or outside a frame under the same execution barrier.

The queue reuses each CanvasItem's LinkedListNode and one tree-owned list; normal enqueue, cancellation, force and phase delivery allocate no per-change objects. Finalization and activation rollback clear the list and active delivery cursor. [Transform notification checks](../../tests/Electron2D.Tests/CanvasTransformNotificationTests.cs) cover ordering, exceptions, removal, reentry, ownership and allocation.
