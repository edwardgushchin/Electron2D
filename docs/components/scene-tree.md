# Scene tree component

Last updated: 2026-10-18

## Selected authoring context

EditedSceneRoot also selects Node.IsPartOfEditedScene and EditorStateChanged membership. The selected live root, including this tree's Root, and all its descendants participate. Detached nodes do not. Selection changes emit no Node authoring event and create no editor UI or simulation mode. See [Node authoring state](../classes/Node.md#scene-authoring-state).

## Scope

This Scene component provides the concrete [Main loop](main-loop.md), owns one active hierarchy and an optional current scene child, propagates typed input and system notifications, delivers lifecycle and pause changes, accepts direct or [Engine](engine-runtime.md)-scheduled process/physics frame boundaries, frame counters and events, tree-change events, reusable Node timers, lightweight one-shot timers, frame-driven [tweening](tweening.md), typed group operations, deferred work, and queued deletion. Per-node hierarchy and 2D state belong to the [Scene hierarchy component](scene-hierarchy.md).

Accepted Node.Name assignments, including unchanged names, notify active trees
after subtree path notifications and the node's own Renamed event. NodeRenamed
precedes TreeChanged; both phases are attempted after earlier failures. Detached
assignments only notify their subtree. See [child names](scene-hierarchy.md#child-name-collisions).

All engine objects expose inherited ElectronObject.IsQueuedForDeletion. Each explicit QueueDelete entry retains one independent mark until execution or discard, including duplicate and multiple-tree requests. Node cancellation cannot release an explicit entry. Claimed QueueFree stays observable through exit/pre-delete/cleanup; a failed validation releases the consumed claim for a later request. Terminal objects always read false.

A queued deletion detaches its node before Dispose. An inherited CancelDisposal veto consumes only that attempt: the node remains alive and detached, the claim/explicit mark is released, and a later request may retry. Duplicate explicit entries remain distinct attempts. A root veto cannot keep a finalized SceneTree alive; its detached subtree may be reactivated by a later tree.

## Owned types

| Type | Role |
| --- | --- |
| [`SceneTree`](../classes/SceneTree.md) | Concrete `MainLoop`, hierarchy owner, frame/system-notification dispatcher, group service, timer owner, deferred scheduler, and deletion queue |
| [`Timer`](../classes/Timer.md) | Reusable Node countdown with process/physics, pause, repeat, autostart, time-scale, event, and packed-configuration behavior |
| [`ProcessPhase`](../classes/ProcessPhase.md) | Stable choice between physics and process countdown lanes |
| [`SceneTreeTimer`](../classes/SceneTreeTimer.md) | Auto-disposed one-shot timer advanced by a selected tree frame lane |
| [`GroupCallFlags`](../classes/GroupCallFlags.md) | Order, deferral, and uniqueness policy for typed group operations |

Tween types are owned by the separate [Tweening component](tweening.md) but registered and advanced here.

Native committed-text delivery is now an owner-thread root-viewport phase separate from ordinary InputEvent traversal. Window calls SceneTree after the DisplayServer event pump has committed the text or preedit; a composition update first propagates `NotificationOsImeUpdate` across live nodes, then the eligible focused Control receives its typed hook/event. Text commits are delivered as one string even when they contain multiple scalars, without fabricating a key event. The phase rejects reentry and continues the focused callback after notification failure. [TextDeliveryTests](../../tests/Electron2D.Tests/TextDeliveryTests.cs) and [TextDeliveryNativeTests](../../tests/Electron2D.Tests/TextDeliveryNativeTests.cs) verify managed and Linux Wayland GPU/compatibility paths. Nested viewport routing and per-key Unicode remain separate dependencies.

## Runtime flow

Construction rejects calls made from inside a packed-scene node factory and roots still marked as unfinished by packed-scene instantiation. It otherwise validates the supplied root, initializes inherited loop state, enters every reachable node parent-first, and readies nodes child-first. Enter/ready callback failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready state, disposes activation-created timers, invalidates activation-created tweens, and clears queues while leaving the supplied hierarchy caller-owned. User side effects outside owned lifecycle state are not transactional.

A root with inherited auto-translation mode samples `ProjectSettings.RootNodeAutoTranslate` before entry; root and descendant entry then delivers translation-change notifications to nodes whose effective mode is enabled. An active root cannot switch back to inherited mode.

An inherited or wrapper frame increments its lane counter, raises its start event, reuses an owned priority-ordered snapshot per lane while hierarchy and priorities are unchanged, invokes all still-eligible internal then public callbacks, advances matching lightweight tree timers, advances a captured matching tween batch, then runs one deferred/deletion safe point. `AnimatedSprite` runs through internal idle notifications in the node phase, using delivered scaled delta independently of ProcessEnabled; it shares the existing pause/process-mode and failure handling. Its resource changes reconcile on the tree owner thread. `Timer` nodes run in the node phase and can choose the Engine-supplied original process step in either lane; `SceneTreeTimer` runs in the later timer phase and selects the scaled or original lane delta from its creation policy; `Tween` may select either scaled or original lane delta. Timer, tween, node, event, deferred, and deletion failures do not prevent later phases and are reported together. Frame/flush execution and tree finalization/disposal are rejected during frame, flush, and node-lifecycle delivery before queue consumption or lifetime mutation.

On a nonzero physics frame, the tree resolves current Area gravity/damping fields, applies stored body force/torque and prepares attached pin/groove constraints and spring impulses after node callbacks and before stepping its internal 2D world. It synchronizes simulated bodies and opt-in kinematic platform poses, then commits body contact/sleep and Area monitoring snapshots. Sleep/contact callbacks run before Area events and lightweight timers/tweens. Zero elapsed time leaves these snapshots unchanged. Contact and Area handlers may mutate scene membership because backend stepping has finished; failures are aggregated while later frame phases continue. The [physics bodies](physics-bodies.md), [joints](physics-joints.md) and [areas](physics-areas.md) components own the force, filtering, constraint, field and monitoring contracts.

SceneTree samples `ProjectSettings.PhysicsInterpolation` at construction and exposes a live tree-wide switch. When enabled, a physics frame captures eligible canvas and viewport camera history around its callbacks without allocating after warmup; the renderer later uses Engine's fractional tick progress. Node inheritance and Control's Off default select eligible items. Reset, pause, camera switching and process-time edits discard stale presentation history while logical transforms remain current. [TopLevel changes](canvas-rendering.md#toplevel-transition-resets-2026-10-18) now use the same recursive reset after canvas rebinding, including neutral and independently top-level descendant branches; failure does not skip later reset deliveries.

System notifications are snapshotted and propagated depth-first to every still-live attached node. Explicit inherited finalization and disposal both close work acceptance, release the hierarchy/timers, and invalidate active tweens; explicit finalization leaves only the tree object undisposed and terminal.

The stable Root can hold an optional CurrentScene direct child alongside persistent children such as UI overlays. Assigning CurrentScene only selects an existing direct child. ChangeSceneToNode accepts a live detached scene on the owner thread, removes the selected old scene immediately, and queues the new scene's entry for the next deferred safe point. ChangeSceneToPacked instantiates first, so instantiation failure preserves the old scene. Multiple requests before the safe point retain only the latest new scene; old and superseded scenes are disposed before that entry. SceneChanged follows successful entry and ready delivery. Failed attachment disposes the accepted pending scene; callback errors propagate after state transitions and later cleanup. UnloadCurrentScene immediately disposes only the selected scene. Finalization also disposes detached pending scenes that never reached a safe point. File-based change/reload waits for an asset loader.

Mouse hover and each isolated viewport's local pointer point publish before Node input callbacks. The hover route updates container/window descendants independently of input consumption; direct PushInput into an attached viewport keeps the enclosing section's pointer. Root-connected queries poll native or headless emulated input, while disconnected sections retain their own point. See [viewport coordinates](canvas-rendering.md#viewport-coordinates).

A Viewport root first localizes incoming client input through its inverse final transform. Positional temporary copies are disposed after dispatch, including errors; local-coordinate and non-positional input stays borrowed. Conversion validates execution/re-entry before copying and does not mutate Input polling. Parsed input uses a reusable reverse depth-first snapshot. Regular input runs first, root viewport hover routing and GUI input follow, then unhandled `ui_*` focus navigation, keyboard-only unhandled input and general unhandled input. GUI pointer targeting uses Control geometry, filter, layer/Z and direct-parent bubbling; keyboard/controller input reaches the focused Control. Focus transfer exits the old Control, raises Viewport.GUIFocusChanged, then enters the new Control; each Control notification precedes its event. Viewport.GetGUIFocusOwner reports the current target and ReleaseGUIFocus clears it. Focus callback failures are aggregated after later eligible callbacks, and reentrant release suppresses stale enter delivery. Pointer motion updates Control hover and native cursor even after handled regular input. Handled state stops later input stages; current membership, pause eligibility, lifetime, and per-stage enablement are revalidated before ordinary Node callbacks. Callback failures are aggregated after eligible delivery continues. Complete renderer ordering, clipping, stationary-pointer geometry changes, touch, exact directional ranking, scroll clipping and nested viewport routing remain absent.

Immediate typed group operations snapshot members in hierarchy or reverse order on the owner thread. Deferred operations resolve membership when their queued wrapper starts. `Unique` coalesces equal queued operations and retains the first setter value. String-based method/property dispatch is absent.

Queue acceptance uses one lock shared with finalization closure. A successful cross-thread enqueue is either executed by a later safe point or deliberately discarded by later finalization; an enqueue that reaches the closed tree is rejected. Finalization closes the queues, exits and recursively disposes the hierarchy, disposes timers, invalidates tweens, and clears subscribers while aggregating every failure.

Window-driven frames invoke the internal RenderingServer under the scene execution barrier after frame processing. Re-entry and mutation during protected phases remain guarded; the renderer itself owns native submission and cleanup.

The [Physics](../domains/physics.md) profile attaches scene bodies, areas and pin/groove/spring joints to one lazily created internal world. Physics node callbacks run before a four-substep Box2D step; solved transforms return before timers, tweens and interpolation end capture. Root disposal releases joint handles before body handles and then the world. Geometry and backend types remain owned by the Physics domain; SceneTree adds no public server or RID API.

## Dependencies

The component depends on Core's `MainLoop` and `EventConnection`, typed Input events, the neutral `Node` including its input/internal lanes and packed-scene construction barriers, `Timer`, the [Tweening component](tweening.md), the internal physics world, reusable lists, concurrent queues, and ordinary .NET synchronization. Core `Engine` may drive it through `MainLoop` and supplies scaled/original deltas. It has no SDL3-CS, clock, native input backend, audio, asset loader/serializer, networking, or editor dependency.

## Invariants

- The root is live, parentless, unattached, and not queued when construction begins.
- Tree activation cannot start inside a packed-scene node factory or with any node whose packed-scene instantiation barrier is active.
- The active root cannot be directly disposed or queued; tree disposal owns its complete lifetime.
- A current scene is a direct child of the stable root. Accepted scene changes own pending nodes; repeated requests supersede them, and finalization releases every still-pending node.
- Membership is never left partially attached after constructor failure or lifecycle callback failure.
- Lifecycle, immediate group work, timers, frames, flushing, hierarchy mutation, and disposal use the creating thread.
- Deferred actions, deferred group operations, generic queued deletion, and node deletion requests may originate on another thread.
- Enter is parent-first, ready is child-first, and exit is reverse-sibling child-first. TreeExited is a separate pass after complete subtree detachment and structural removal. All applicable lifecycle phases are attempted before failures are reported.
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

Implemented and covered by executable checks. Core Engine supplies host-driven time scaling, original process-step delivery for `Timer.IgnoreTimeScale` and original lane-delta delivery for `SceneTreeTimer` and `Tween.SetIgnoreTimeScale`, fixed-step scheduling, and the fraction used by the executable 2D interpolation path. Engine.Run supplies an owner-thread window clock/pump and frame wait. The owner-driven game loop uses the current GUI/input, multiplayer polling and selected body/Area/joint paths. Their complete contracts and platform limits remain on the owning domain pages. There is no background game-loop thread; permission requests, scene file reload, accessibility and editor capabilities retain their explicit dependency gates. In-memory current-scene replacement is available through detached nodes and PackedScene. The root viewport GUI input and focus path runs in this component. Blocked reference APIs and their missing domains are enumerated in the [`SceneTree`](../coverage/classes/SceneTree.md), [`Timer`](../coverage/classes/Timer.md), [`Tween`](../coverage/classes/Tween.md), and [ADR 0038](../decisions/input.md#deferred-coverage-and-exact-implementation-triggers); no placeholder surface is exposed for them.

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

Reusable [scene animation](scene-animation.md) uses Node internal idle/physics callbacks, before the node's public callback. Manual AnimationMixer mode does not create an automatic callback or separate clock.

[Embedded viewport containers and GUI](canvas-rendering.md#embedded-viewport-containers-and-gui) now execute native SubViewportContainer composition, stretch/shrink, independent GUI state and connected input/drag routing. Public input reentry remains rejected; native subwindows, multiview, editor/file workflows and other-platform acceptance remain separate.

[Typed scene multiplayer](scene-multiplayer.md) integrates default/borrowed branch interfaces before node entry, idle-frame polling before node callbacks, Node authority/configured RPC, structural path invalidation before observers and detach/default cleanup after node exit. Programmatic native WS/WSS tests execute client/server branches within one tree; this does not establish replication or editor/rendered/agent acceptance.

[Typed scene spawning and replication](scene-replication.md) now executes custom/automatic in-memory scene factories, pre-Ready state, late admission, visibility/RPC integration, property policies and real periodic/delta batching through SceneMultiplayer. Public branch replacement rebinds typed configurations after committed cleanup, including callback errors. File scene authoring and other acceptance gates remain separate.

## Independent internal processing

`public bool InternalProcessEnabled { get; set; }` and `public bool InternalPhysicsProcessEnabled { get; set; }` project each internal-lane setter/query pair. Plain Node defaults are false; built-in nodes may enable their own work. Each switch preserves the other lane and the ordinary ProcessEnabled/PhysicsProcessEnabled flags. Enabled internal notifications run before ordinary callbacks in their selected idle/fixed frame. An internal callback can enable or disable the ordinary callback for the same frame. Detach/dispose prevents further ordinary delivery; failures still attempt the remaining ordinary/sibling callbacks under the scene-tree cleanup policy.

The resolved ProcessMode and SceneTree pause policy gate both lanes. Internal processing is independent of ordinary callback enablement, and does not bypass pausing. Detached nodes retain the runtime switches but receive no frames. Setters require the attached tree's owner and reject scene capture; disposed access rejects. Typed property descriptors expose writable runtime state with IsStored false. Ordinary PackedScene capture omits the flags, while constructors rebuild built-in internal behavior. No hidden thread or scheduler is added.

Changing a built-in node's internal switches directly affects its implementation: for example it suspends and resumes Timer countdowns. Such nodes can overwrite the switch when their playback/callback policy changes. Do not disable internal work unless that is the intended effect. See [Node](../classes/Node.md) and [Timer](../classes/Timer.md).

NodeInternalProcessingTests verifies both lanes, order and current-state mutation, pause modes, reattachment, errors, ownership/disposal, runtime scene omission and actual idle/physics Timer consumers. Its 1024 warmed set/query/frame operations allocate zero managed owner bytes. Native GPU and compatibility checks verify red pixels, a frozen frame with the internal lane disabled, resumed blue pixels and fixed ticks. Foreign/editor/native allocator and broad load verification remain separate.

The exact SceneTree.Defer scheduler and SceneTree.SetDeferred now use a Scene-owned typed invocation provider. Each accepted call retains independent typed values in a slot and submits its immutable callback into the existing single FIFO queue. Slots recycle only after callback completion or discard, clearing argument/callback references even after failure. Work emitted during a captured batch waits for the next flush. Pool operations share the lifetime/work gate; user handlers run outside it. A weak-owner Core registry recognizes only the exact registered delegate, so Core has no Scene runtime dependency. Public signatures, borrowed reference ownership, event multiplicity and cancellation remain unchanged.

Prepared standard-scene delivery and typed setters have zero invocation allocation, including prepared one-shot first delivery. First type/capacity growth, subscription and allocations performed by handlers/accessors remain separate; native multicast removal with multiple listeners can allocate. Arbitrary custom schedulers preserve their independent captured Actions and may allocate. Deferred delivery never extends a borrowed input event lifetime.

EventConnectionPhysicsTests verifies mixed typed/untyped FIFO order, next-batch work, failures followed by later work, closed-scheduler subscription/emission policy, 256 concurrent producers, reference release on execution/failure/finalization/activation rollback, and 640 first one-shot callbacks for immediate and scene-deferred profiles with singleton accessors. Selected CPU/GPU full frames retain 128 callbacks per immediate and deferred arity plus body/shape transitions at strict zero owner/all-thread bytes. Real picking and legacy custom-scheduler/concurrency/error checks remain included. Software Vulkan is functional evidence; hardware/native/foreign/network/performance acceptance stays open.

## Shared node frame time (2026-10-14)

Node process/physics delta observations now read the attached tree's current or
most recent lane duration, including nodes without callbacks and disabled/paused
nodes. The tree commits time before frame-start observers; detachment returns zero
and reattachment adopts the receiving tree. Owner/lifetime checks apply. Manual
notifications do not advance time. CharacterBody.MoveAndSlide uses physics time
only during a physics frame and process time otherwise, with no cross-lane or
first-frame guess. Zero duration contributes no velocity travel; penetration
recovery remains executable.

PhysicsNodeDeltaTests uses controller-driven public character movement on selected
CPU/GPU ordinary/separate modes, node lifecycle/error observations and scaled/zero
Engine time. Two complete-frame brackets warm 128 and measure 128 at strict 0/0
owner/all-thread managed bytes. Existing CharacterBody, internal-lane/hierarchy,
surface, ray, snapshot and checkpoint checks retain their numerical assertions;
external callers explicitly prepare their process lane rather than relying on the
removed fallback. The initial CPU/GPU regression moved zero instead of three scene
units. Evidence is retained under bin/physics-node-delta/2026-10-14. Full inherited,
native, foreign and hardware/window/performance acceptance remains open.

The final retained Release runtime passes eleven fresh profiles: four node-delta
CPU/GPU ordinary/separate modes, paired complete shared-physics and state/surface
contracts, portable CPU/GPU snapshots, GPU common checkpoints and resident motion.
All eight new warm brackets report zero owner/all-thread managed bytes. CPU maps
no graphics driver; GPU maps only CPU Vulkan lavapipe. Seven build, scoped-format,
compiled-coverage and wiki test/generate/check gates pass. The compiled surface
remains 13,961 declarations, with 832 public types and 850 wiki files. Current
source/runtime manifests and process proofs are retained in
bin/physics-node-delta/2026-10-14/validation.json. These checks establish functional
behavior; native memory and hardware/window performance acceptance remain open.

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

The ten source-analysis blockers reached by the extended PackedScene consumer are now resolved. The current complete public-only consumer publishes with project references and strict NativeAOT analysis, without warning suppression or omitted tests. CodeEdit/GraphEdit inspect bound typed virtual delegates; tile defaults use exact existing array types for scalar, jagged and multidimensional application arrays, and string quoting uses prepared JSON metadata. Script source registration declares its actual file/PDB and untrimmed project metadata prerequisites and captures methods/events/literal fields once. OS instance launch no longer requires Assembly.Location.

The final cohort passes ten managed profiles and four NativeAOT CPU/software-GPU ordinary/separate profiles. Every native process also executes the GUI/array prerequisite fixture before full callbacks, PackedScene reconstruction and weak retirement. All eight physics brackets warm 128 and measure 128 at strict 0/0 owner/all-thread managed bytes. CPU processes load no GPU provider; GPU processes load lavapipe and no NVIDIA library, with actual solver thread counts zero/one. The six connected managed suites cover prerequisites, custom tiles, code/graph authoring, Script fresh-process storage and real OS process launch/capture/kill/pipe ownership. Runtime/test/consumer formats, compiled API and wiki gates pass at 13,999 declarations, 836 public types and 854 wiki files. Exact manifests, publication logs and results are retained in bin/physics-aot-prerequisites/2026-10-14/validation.json. Embedded NativeAOT Script source registration, full inherited/native/foreign coverage, hardware/window/performance and owner acceptance remain open.

The current retained JIT runtime passes ten fresh profiles, including four automatic
CPU/GPU ordinary/separate variants, paired delta/internal/hierarchy/character and
complete shared-physics suites, authored connections and paired physical picking.
The separate public-only consumer passes the complete extended fixture in four
more processes. Eight required runtime/test/consumer format, build, compiled
coverage and wiki test/generate/check gates pass. All eight new JIT allocation
brackets retain 0/0 owner/all-thread bytes; current sources and retained runtime
hashes remain unchanged. Parallel hostname-resolver integration is preserved;
the current total is 13,999 declarations, 836 public types and 854 wiki files.
Exact manifests, profile proofs, NativeAOT limits and gate results are retained in
bin/physics-auto-callbacks/2026-10-14/validation.json.

## Captured callback participants

The retained priority snapshots contain only public/internal participants of their selected lane. All four Node enablement setters invalidate subsequent capture when their value changes, including built-in internal switches and automatic ready preparation. A newly enabled absent node waits for the next capture; current flags and process-mode eligibility still gate an existing captured entry. Priority/order changes likewise wait for the next capture. Exit/reentry retires an old entry, including same-tree reentry and a node reentering from its own internal callback. Internal and public delivery remain per-node phases; callback failures continue later applicable callbacks and the physical step.

Public neutral/spatial/RigidBody consumers verify four-lane activation/deactivation, next-frame cached membership and priority, same-tree and self reentry, current eligibility, paired internal/public failure delivery and actual selected movement/query identity. Warm cycles repeatedly rebuild both lane snapshots while executing idle, physics and point queries with zero owner/all-thread managed bytes. The existing hierarchy fixture now explicitly disables its initially ready-enabled node before testing late activation; its old immediate-activation expectation is withdrawn. Evidence: `bin/physics-node-scheduling/2026-10-15/validation.json`. Process-thread groups/mailboxes, native allocator totals, hardware/window timing and wider inherited acceptance stay open.

## Default processing-group messages

Node's six typed call/set/notify thread-safe and deferred-thread-group operations use a distinct default-group FIFO. Both idle and physics frames drain it after frame-start/input/navigation preparation and before capturing enabled callback participants, then again after Node callbacks and before physical integration/timers/tweens. It runs with no enabled callbacks and while paused. Nested additions join the active FIFO until quiescence, so unbounded self-enqueue can prevent progress. Every failed message releases its retained slot and later entries/frame phases still execute. Explicit `FlushDeferred` keeps its ordinary captured action/deletion scope.

The group queue shares the existing generic invocation pool and atomic lifetime lock. Discard on constructor rollback, finalization or disposal clears pending delegates/values. Receiver samples borrow immutable weak identity; collected/disposed targets are skipped, while a live transferred receiver requeues to its current owner. This makes worker publication of actual body state safe without changing solver/renderer ownership. The caller remains responsible for its captured reference values/delegates.

[Thread message consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ThreadMessages.cs) execute all six APIs on current CPU/software-Vulkan/registered ordinary/separate solver profiles and strict NativeAOT consumers. Prepared sender and owner calls, group delivery, full physical steps and real point queries require zero sender/owner/all-thread managed bytes. Phase activation, FIFO recursion, partial failures, disposal/GC, cross-owner transfer and queue closure execute. Evidence: `bin/physics-thread-messages/2026-10-15/validation.json`. The current explicit-group profile below supersedes the earlier absent-group limitation; broader worker access, rendering/window timing, native allocation totals and foreign/owner acceptance remain separate obligations.

## Ordered processing groups

Explicit Node main/subthread groups now partition inherited descendants, retain per-lane sorted snapshots and destination queues, capture ascending group order and join worker stages before same-order main groups or later orders. Actual WorkerThreadPool callbacks run without captured caller context. Masks permit empty-group message work; active groups process queued messages independently of the mask. Main collection can mutate actual selected physics after worker completion. Group-local flags/priorities/deltas gain bounded access; general scene/render/backend operations retain their owner guards and broader inheritance remains Partial. Public group consumers also save portable settings and load them in actual fresh processes; ordinary/separate solver JIT/NativeAOT proofs and zero-allocation frames/queries are recorded in `bin/physics-process-groups/2026-10-15/validation.json`.

The frame retains its group comparison delegate: default sorting allocated 48 bytes per NativeAOT frame, while the retained comparator passes the same 128-frame zero owner/all-thread allocation bracket. That profile checked the six idle/physics controls. The complete metadata contract below extends current-group writes to input switches and permits coordinated stored-value reads; outside-group writes still reject. This measures managed allocations; native totals and hardware/window performance remain open.

## Process policy and physics pause

`ProcessMode` controls callback eligibility independently of each public/internal lane switch. All five modes resolve through the nearest explicit ancestor; internal children inherit normally and an explicit descendant starts another policy boundary. Detached nodes cannot process and change policy silently. A root entering with Inherit stores Pausable; an attached root rejects Inherit.

Changing an attached mode delivers eligibility notifications (`Paused` or `Unpaused`) followed by an effective disabled transition (`Disabled` or `Enabled`) parent-first. Tree pause reports only changed eligibility: Pausable/Inherit pause, WhenPaused unpauses, and Always/Disabled remain silent. Resume reverses those transitions. CollisionObject.DisableMode applies only to effective Disabled, preserving identity and query membership during ordinary pause. Delivery revisions prevent repeated notifications without consuming pending physical Enabled delivery when a parent callback selects an explicit child mode; nested transitions supersede stale outer notifications; retired or replaced attachments skip, and failures aggregate after remaining applicable delivery. Existing pause hierarchy mutation, once-per-node traversal and opposite-pause reentry guards remain active. Prepared traversal and per-depth mode snapshots retain capacity.

A paused `PhysicsFrame` still delivers eligible Always/WhenPaused callbacks, timers, messages and deferred work, but does not step any world through that tree. Resume advances the same bodies and force callbacks. Explicit `PhysicsServer.SpaceStep` of a caller-owned active space retains its own clock; a different unpaused tree still steps its own independent world. Tree pause does not globally disable PhysicsServer. Policy/CanProcess reads allow the owner or current processing group; writes retain owner-thread and physical-borrow checks. Internal resolution does not expose ancestor public metadata to workers.

[Public process-mode consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ProcessModes.cs) checks all 50 mode pairs across both pause states, root/detached/storage/invalid/lifetime contracts, explicit inheritance boundaries and internal children, notification order, nested mode/tree-pause and failing callbacks, actual worker isolation, solver movement and force callbacks, independent owned-space stepping, and 128 warmed cycles of mode edits, pause/resume and full moving/paused frames. CPU, private software Vulkan and registered implementations run serial/separate in managed and strict NativeAOT profiles with zero owner/all-thread managed bytes. Existing lifecycle failure, reparent/removal pause, scene internal-lane and collision disable-policy checks also run. Evidence: `bin/physics-process-modes/2026-10-16/validation.json`. Hardware GPU, native allocator totals, other platforms, real-window timing and full physics acceptance remain open.

The [complete node transition order](scene-hierarchy.md#entry-readiness-and-departure) now distinguishes managed enter/exit hooks, notifications, parent/tree events, per-attachment PostEnterTree and final TreeExited. NodeRemoved retains the departing node's Tree association until ChildExitingTree completes. Physical binding release occurs after TreeExiting, so handlers can inspect the live binding; a throwing hook cannot prevent cleanup. Ready pass state and attachment-qualified snapshots handle nested additions/reentry without repeated stale delivery.

The [membership and authoring-owner contract](scene-hierarchy.md#membership-and-authoring-ownership) preserves structural/tree identity, callback-failed reparent completion, borrowed owner/unique-name lifetime and cross-world physical RID/pose. Owner metadata does not select a physical space or wake a body. Public managed/NativeAOT consumers verify the bounded hot path; full native/platform/hardware acceptance remains open.

## Complete processing metadata and callbacks

All eight Node processing switches and both signed priority keys are local metadata. Live getters permit caller-coordinated reads without entering a processing group; these separate observations are not an atomic snapshot. Writes require the scene owner or current group, reject capture even for unchanged values, and run the derived validation hook. This applies to neutral, spatial, UI, body, Area and collision-shape descendants. Writing an input-stage switch does not dispatch input or change a collider. Physical, joint, transform, renderer, hierarchy and lifetime operations retain their separate owner/borrow guards.

Ready enables each actual typed callback override before OnReady. The six public callback switches and both priorities have stored descriptors, including ShortcutInputEnabled; the two internal switches remain runtime-only. Packed scenes restore configuration before automatic ready preparation. Older archives without a shortcut value keep its default false. All 64 public-switch combinations round-trip before activation. Existing snapshot membership, current disablement, retirement, priority and group barriers remain unchanged. Equal priorities retain tree order within a group; both priorities accept the complete signed integer range. Priorities do not reorder input delivery.

Idle and fixed callbacks execute in their selected processing group, with joined workers for subthread groups. They receive the lane duration; the fixed callback precedes the tree-owned numerical step and does not perform it. Input remains owner-delivered in reverse hierarchy order through input, GUI, shortcut, unhandled-key and unhandled stages. Current switch values, event kind, handling and process policy gate delivery. Callback errors preserve original exceptions while later applicable callbacks and committed frame work continue. Non-positional input stays borrowed.

The retained member-schedule Comparison delegate avoids NativeAOT's 48-byte default-sort adapter on each rebuilt worker lane. The original two-lane physical workload observed 0/12288 owner/all-thread bytes over 128 frames; the retained comparator removes this path without changing ordering or callback counts. Allocation assertions remain strict. A separate captured NativeAOT failure created a 40-byte Lock and 24-byte DeadEntryCollector in FlushProcessMessages when the shared work gate first inflated under real contention. Before its first worker stage, the tree now holds that gate until a temporary helper is actually blocked entering it, then releases and joins the helper. The helper invokes no game code, captures no execution context and leaves no persistent thread. This prepares the actual shared monitor and its contention wait state, independently of warmup length. A tree owned by a one-worker pool is explicitly exercised so preparation cannot depend on an idle pool worker.

The public --node-processing consumer checks ten metadata controls on ten neutral/canvas/physical roles, coordinated foreign reads, rejected foreign writes, actual group writes and protected physical operations, all 64 stored configurations, capture/lifetime guards, typed callbacks on neutral/spatial/RigidBody nodes, signed priority extremes/ties, input ordering/kinds/handling/failure and fresh-process scene reconstruction with selected motion. Its 128 warmup plus 128 measured complete idle/physics/input frames change all controls, rebuild both schedules and retain motion 1.28 within .002, stable RID, 128 force callbacks, 256 worker callbacks and 512 owner input callbacks enabled by worker writes, with 0/0 owner/all-thread managed bytes. CPU/software-Vulkan/registered, serial/separate, CoreCLR/strict NativeAOT evidence is in bin/physics-node-processing/2026-10-16/validation.json.

The metadata-only worker family explicitly disables built-in internal work before grouping. PhysicalBone's owner-bound reconciliation remains outside this worker profile; the original attempt is preserved. This is not general worker numerical/geometry/joint authoring acceptance. Full native/driver budgets, physical-device advantage, real-window FPS and the remaining inherited/extension contract stay open.

## Generation-qualified queued deletion

`Node.QueueFree` reserves a unique current request; `CancelFree` invalidates that request before an owner claims it. Reissue appends a new FIFO entry. A stale entry in the same or a previous tree cannot consume the replacement. A request created by a departure callback waits for the next captured deletion batch, while the body remains queryable and continues its next physical step before that new deletion. Once a request is claimed, cancellation cannot undo its disposal. Only the execution that acquired the claim may release it; a stale entry on another owner cannot clear an active destination claim. Child flags remain independent; disposing the parent still retires its subtree.

Publication rollback changes only the reserved generation. Node teardown atomically closes further publication while preserving observation of the request through cleanup. Terminal disposal makes IsQueuedForDeletion false; a failed validation releases a consumed claim for a later generation. Queue acceptance retains the existing tree lifetime gate. Explicit SceneTree.QueueDelete remains an independent uncancellable operation. This queue cancellation does not veto a Dispose already executing.

The tree swaps two prepared concurrent deletion queues under its work gate and returns a drained queue after all captured entries. New entries wait in the other queue; exceptions retain later work and prepared storage. Finalization discards pending references from both queues. No callback runs under the queue gate.

The public-only `--queued-deletion` consumer checks FIFO reissue, real body survival/query/RID identity across deletion boundaries, subtree flags/order, detached and transferred requests, inherited neutral/canvas/physical roles, concurrent cancel/publication/claims, error recovery and weak-reference release. CPU, software-Vulkan GPU and registered numerical profiles run with owner/separate solve policies. Each warm bracket cancels and reissues requests, executes ordinary deferred work and queries an integrating body for 128 complete frames at strict zero owner/all-thread managed bytes. Strict NativeAOT uses the same public consumer. Native memory totals, target-device advantage, real-window FPS and foreign acceptance remain open.
