# SceneTree

Last updated: 2026-09-21

## Declaration

- Source: [`SceneTree.cs`](../../src/Scene/Main/SceneTree.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class SceneTree : MainLoop`
- Domain: [Scene](../domains/scene.md)
- Component: [Scene tree](../components/scene-tree.md)

## Responsibility and ownership

`SceneTree` is the concrete [`MainLoop`](MainLoop.md) that owns one active root [`Node`](Node.md) hierarchy. It establishes lifecycle and owner-thread boundaries, accepts direct frame calls or scheduling through [`Engine`](Engine.md), propagates typed input and system notifications, manages pause state, reusable Node [`Timer`](Timer.md) scheduling, lightweight tree timers, [`Tween`](Tween.md) sequences, typed group operations, deferred actions, and queued deletion, and finalizes the complete hierarchy.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `SceneTree(Node root)` | Requires a live, fully constructed, parentless, unattached, non-queued root and rejects construction from inside a packed-scene node factory; initializes the MainLoop and activates the hierarchy; activation failure closes work acceptance, exits attached nodes, restores ready state, disposes created timers, invalidates created tweens, discards queued work, and leaves any escaped tree reference terminally disposed without taking root ownership |
| `Node Root { get; }` | Immutable owned root reference; the referenced hierarchy is disposed by finalization |
| `bool HasDeferredWork { get; }` | Concurrent advisory snapshot of pending action or deletion queues |
| `ulong ProcessFrameCount { get; }` | Number of valid process-frame attempts, including attempts that later reported callback failures |
| `ulong PhysicsFrameCount { get; }` | Number of valid physics-frame attempts, including attempts that later reported callback failures |
| `int NodeCount { get; }` | Owner-thread active hierarchy count including the root, or zero after finalization |
| `bool Paused { get; set; }` | Pause state; changes notify and revalidate the current hierarchy; an opposite re-entrant transition is rejected |
| `event Action<SceneTree, Node> NodeAdded` | Raised parent-first after each node enters |
| `event Action<SceneTree, Node> NodeRemoved` | Raised child-first after each node leaves and its `Tree` becomes `null` |
| `event Action<SceneTree, Node> NodeRenamed` | Raised after an active node's path notifications and local renamed event |
| `event Action<SceneTree> ProcessFrameStarted` | Raised before process candidates are captured |
| `event Action<SceneTree> PhysicsFrameStarted` | Raised before physics candidates are captured |
| `event Action<SceneTree> TreeChanged` | Raised after active structural insertion, removal, reorder, reparent stages, or rename |
| `void Defer(Action action)` | Atomically accepts typed work while the tree is live; one captured batch runs per flush |
| `void SetDeferred<T>(Action<T> setter, T value)` | Captures a typed setter and value without dynamic property lookup |
| `SceneTreeTimer CreateTimer(double timeSeconds, bool processAlways = true, bool processInPhysics = false)` | Creates an owned one-shot timer processed after nodes in the selected lane |
| `Tween CreateTween()` | Creates an unbound valid Tween registered for this tree's captured frame processing |
| `IReadOnlyList<Tween> GetProcessedTweens()` | Returns an owner-thread read-only snapshot of valid top-level tweens in creation order |
| `void ProcessFrame(double delta)` | Compatibility wrapper over inherited `Process(delta)`; raises the frame event, runs eligible process callbacks, advances tree timers and tweens, then flushes |
| `void PhysicsFrame(double delta)` | Compatibility wrapper over inherited `PhysicsProcess(delta)` for the physics lane |
| `void SetInputAsHandled()` | During active input dispatch, stops the current traversal and all later stages |
| `bool IsInputHandled()` | During active input dispatch, reports the current handled flag; outside dispatch it throws |
| `IReadOnlyList<Node> GetNodesInGroup(string group)` | Returns an owner-thread read-only pre-order snapshot |
| `Node? GetFirstNodeInGroup(string group)` | Returns the first pre-order member or `null` |
| `int GetNodeCountInGroup(string group)` | Returns the current group count |
| `bool HasGroup(string group)` | Reports whether a current member exists |
| `void CallGroup(string group, Action<Node> action, GroupCallFlags flags = Default)` | Invokes a typed action immediately or deferred, with order/uniqueness flags |
| `void SetGroup<T>(string group, Action<Node, T> setter, T value, GroupCallFlags flags = Default)` | Applies a typed value through a supplied setter |
| `void NotifyGroup(string group, int notification, GroupCallFlags flags = Default)` | Delivers a numeric notification to current members |
| `void QueueDelete(ElectronObject instance)` | Queues a live engine object for uncancellable, failure-continuing deletion after deferred actions |
| `void FlushDeferred()` | Runs one captured action batch and then one captured deletion batch |

Inherited `Process(double)` and `PhysicsProcess(double)` run the same pipelines and always return `false`; `Initialize()` is already consumed by successful construction. Inherited `FinalizeLoop()` closes the queues and releases the hierarchy/timers/tweens without yet marking the tree object disposed. The typed permission event and all other loop behavior follow [`MainLoop`](MainLoop.md); identity, notifications, properties, translation, and disposal follow [`ElectronObject`](ElectronObject.md).

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `OnProcess(double)` / `OnPhysicsProcess(double)` | Map inherited loop frames to the existing scene frame pipelines and return `false` |
| `OnNotification(int)` | Calls inherited handling and propagates system notifications depth-first to live attached nodes, aggregating callback failures |
| `ValidateFinalization()` | Rejects finalization before successful construction or during frame, flush, lifecycle, or pause callbacks |
| `ValidateDisposal()` | Requires the creating thread and rejects disposal re-entered from a frame, flush, node-lifecycle, or pause callback |
| `GetPropertyDescriptors()` | Appends typed root, queue, frame-count, node-count, and pause descriptors to inherited descriptors |
| `OnFinalize()` | Closes queues atomically, exits and disposes the root, disposes all timers, invalidates all tweens, clears subscribers, calls inherited finalization, and aggregates teardown failures after every stage is attempted |

The class is sealed, so these overrides document lifetime behavior rather than extension points.

## Frame, timer, and deferred flow

A valid inherited or wrapper frame increments its lane counter, raises the matching frame event, captures the then-current hierarchy in a reusable buffer, orders candidates by the lane's priority and captured pre-order, and revalidates membership, lifetime, pause eligibility, and public-or-internal enable state before every callback. Engine-internal node processing runs before the same node's independently enabled public callback. Failures are retained while later callbacks/phases are attempted; detachment or disposal during the internal phase skips that node's public phase.

Reusable `Timer` nodes advance inside node processing and can select Engine's original delta. Matching `SceneTreeTimer` instances are captured afterward. A tree timer created by a node callback may therefore advance in that frame; a tree timer created by another tree timer waits for the next matching frame. Expired tree timers are removed, notify synchronously, and are disposed even when a timeout handler fails. Matching Tweens are then captured and processed in creation order; a tween created by a node or tree-timer callback can enter that frame, while one created by another tween waits. Tweens may select the original Engine delta. Deferred actions then run from one captured batch. A nested `Defer` waits for a later flush. The deletion batch is captured after actions, so deletion requested by a captured action runs in the same flush. All phase failures are flattened into one `AggregateException`.

Frame and flush execution cannot be re-entered and cannot begin during node lifecycle or pause delivery. Calling `Dispose`, `FinalizeLoop`, any frame entry point, or `FlushDeferred` from frame, flush, lifecycle, or pause callbacks is rejected before queue consumption or tree lifetime changes; the outer operation continues and reports the failure.

## Input propagation

`Input.ParseInputEvent` first asks the active SceneTree to validate its owner thread and execution barrier, commits raw/action state only after that succeeds, and then dispatches through MainLoop's internal boundary. The tree captures pre-order once and visits the snapshot in reverse. It runs `Node.OnInput`, then for key events `Node.OnUnhandledKeyInput`, then `Node.OnUnhandledInput`. Each stage requires its matching enable flag and current `CanProcess()` eligibility. `SetInputAsHandled()` stops immediately. Removed, moved-to-another-tree, disposed, disabled, or newly added candidates are handled by snapshot revalidation. User callback failures are aggregated after other eligible callbacks run; committed Input state is not rolled back. Dispatch is owner-thread and non-reentrant and reuses buffers after warmup.

## Lifecycle and failure safety

Construction rejects a root still under packed-scene instantiation and rejects every `SceneTree` activation attempted while a packed-scene node factory is executing; neither case begins enter callbacks or transfers ownership. Otherwise enter is parent-first, post-enter follows descendant entry, ready is child-first, and exit is child-first. Each lifecycle stage attempts all applicable callbacks and events before reporting errors. Constructor activation failure first closes acceptance, terminally marks any escaped tree reference, exits attached nodes, resets ready flags, disposes activation-created timers, invalidates activation-created tweens, and leaves the hierarchy live and caller-owned. Entry/ready snapshots revalidate membership. Removing, reparenting, or disposing the node whose lifecycle is in progress is rejected, as is re-entrant child escape from a disposing parent. Side effects outside owned membership, ready state, timers, tweens, and queues are not reversible.

Pause traversal revalidates lifetime and membership before every notification. An opposite transition requested by a pause callback is rejected, so the outer transition retains one coherent state.

Explicit finalization or normal disposal first closes queue acceptance under the same lock used by cross-thread enqueue. Work accepted before closure is intentionally discarded; work racing after closure receives `ObjectDisposedException`. Exit, recursive node disposal, timer disposal, tween invalidation, queue cleanup, subscriber cleanup, and inherited finalization are all attempted even when earlier callbacks fail. `FinalizeLoop()` leaves the tree object alive but terminal after releasing ownership; `Dispose()` also publishes the final disposed state. Callback failure never permits a finalization retry.

System notifications `2009..2020` are snapshotted in depth-first order, revalidated before delivery, and attempted for every live attached node before failures are aggregated. Native creation of those notifications and focus-driven Input state changes remain outside SceneTree.

## Group semantics

Group names are nonblank and ordinal case-sensitive. Immediate group operations require the owner thread. Deferred group operations may be requested from another thread and resolve current membership when their queued callback begins. Candidate nodes are then revalidated before each invocation. Reverse order is the exact reverse of hierarchy pre-order. `Unique` is valid only with `Deferred`; equality uses operation kind, group, and delegate or notification identifier, ignores setter values, and retains the first accepted value.

## Threading guarantees and non-guarantees

The creating thread owns lifecycle, hierarchy reads and mutation, input dispatch/handled state, pause mutation, immediate group operations, timer/tween creation, mutation and disposal, frames, flushes, and tree disposal. `Defer`, `SetDeferred`, deferred group operations, `QueueDelete`, and `Node.QueueFree` are cross-thread request boundaries. Typed event receipt by `AwaitTweener` may also originate elsewhere, but continuation occurs on the owner thread. Their acceptance is serialized with disposal. `HasDeferredWork` is an advisory concurrent snapshot, not a barrier. User game state, event subscription, timer/tween reads, and node reads are not made thread-safe by the tree.

## Official reference coverage inventory

The stable reference API and its `MainLoop` inheritance chain were checked on 2026-09-21.

| Reference area | Electron2D disposition |
| --- | --- |
| Root, pause, node/group counts, group presence and lookup, frame counters | Implemented with explicit owner-thread rules; `PhysicsFrameCount` is per-tree valid-call count rather than a process-global counter, and `Root` is a general `Node` until the window domain exists |
| Group call/set/notify and call flags | Implemented as typed delegates plus [`GroupCallFlags`](GroupCallFlags.md); string method/property dispatch and `Variant` values are permanently excluded by ADR 0001 |
| One-shot tree timers | Implemented through [`SceneTreeTimer`](SceneTreeTimer.md); direct callers supply delta, while Engine-driven frames receive scaled deltas, and there is no independent ignore-time-scale lane yet |
| Reusable Node timers | Implemented through [`Timer`](Timer.md), internal Node lanes, typed timeout events, packed configuration, and scaled/original Engine deltas |
| Queue deletion | Implemented for `ElectronObject`; cancellable node deletion remains `Node.QueueFree`/`CancelFree` |
| Node added/removed/renamed, process/physics frame, tree changed signals | Implemented as typed C# events |
| Input parsing and handled propagation | Adapted to the root SceneTree because no Viewport domain exists: typed Input events run through three opt-in Node stages and `SetInputAsHandled`/`IsInputHandled` are valid only during dispatch |
| Main-loop initialization, processing, and finalization | Implemented through `MainLoop`: construction initializes, both inherited frame lanes dispatch scene work and return `false`, and explicit finalization or disposal releases ownership |
| Current scene, scene switching/unloading/reloading, and scene-changed signal | Deferred until a permanent window root and scene serialization/instantiation domain exist |
| Application quit/back policy and `quit` | Deferred until the application runtime and SDL event pump exist |
| File/packed-scene changes | Deferred until assets and scene serialization exist |
| Tween creation/query | Implemented through typed [`Tween`](Tween.md) sequences and `GetProcessedTweens()` snapshots |
| Multiplayer polling and path-specific multiplayer APIs | Deferred until networking exists |
| Physics interpolation | Deferred until renderer and collision-physics integration exists |
| OS/application notification constants and hierarchy propagation | Implemented through inherited IDs and depth-first live-node delivery; native event generation and focus-driven release policy begin with the SDL window/event-pump trigger in ADR 0038 |
| Permission-result signal | Inherited as a typed event; native permission requests/results remain blocked on SDL platform integration |
| Accessibility queries | Deferred until a platform accessibility component exists |
| Editor roots, debug visualization hints, configuration-warning and process-mode editor signals | Deferred until an editor and the corresponding renderer/navigation/physics domains exist; 3D portions are permanently excluded |

No dependency-blocked member is represented by an inert property or empty method.

## Dependencies and interactions

The class depends on [`MainLoop`](MainLoop.md), typed [`InputEvent`](InputEvent.md) values, `Node`, `NodeProcessMode`, [`Timer`](Timer.md), [`SceneTreeTimer`](SceneTreeTimer.md), [`Tween`](Tween.md), [`GroupCallFlags`](GroupCallFlags.md), reusable scheduler/input/timer/tween lists, concurrent queues, and a single queue-lifetime lock. `Node` supplies input/internal lanes plus the construction/factory barriers that keep [`PackedScene`](PackedScene.md) reconstruction detached. Core's [`Engine`](Engine.md) can drive the tree through the base contract, and [`EventConnection`](EventConnection.md) supports deferred delivery and typed tween waits. There is no SDL3-CS, native input backend, renderer, audio, collision-physics, asset loader/serializer, networking, or editor dependency.

## Verification and remaining limits

`tests/Electron2D.Tests/Program.cs` covers constructor validation, inherited-loop initialization/driving/finalization, Engine attachment/zero-delta scheduling/finalization, three-stage input ordering/handled state/re-entry/failure continuation/allocation, system-notification propagation, escaped-reference terminal state, timer/tween cleanup, and enter/ready rollback; stale lifecycle snapshots; lifecycle and tree-event order; exception-safe teardown and queued deletion; cross-tree deletion transfer; lifecycle execution barriers; pause re-entry/traversal/execution barriers; exiting/pre-delete/cleanup ownership guards; 256 concurrent QueueFree/flush iterations; a 64-iteration concurrent enqueue/disposal stress check; frame counters/events; public/internal process ordering, failure continuation, pause eligibility, and scaled/original deltas; group operations and invalid flags; both timer facilities; complete typed tween sequencing/lifetime/failure cases; generic queued object deletion; captured deferred batches; cancellation; recursive node disposal; and zero steady-state managed allocation across warmed idle, active-Timer, active-Tween, and input paths.

`SceneTree` itself has no automatic frame pump or elapsed-time source. Core [`Engine`](Engine.md) provides host-driven fixed-step accumulation, scaled/original delta delivery, time scaling, and interpolation state, but there is still no SDL clock/pump, frame waiting, current-scene switching, renderer synchronization, native/GUI input routing, physics simulation, loaded-scene performance benchmark, or exception logger. Allocation checks cover warmed empty and small active-Timer/Tween/input hierarchies, not large-scene performance; concurrency checks are local stress tests rather than formal proofs or platform-wide performance evidence. Input hardware gaps use ADR 0038's exact triggers.

## Related decision

- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
